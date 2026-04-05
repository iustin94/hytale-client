using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI.Components.Forms;

/// <summary>
/// Self-contained editor for dialog steps and choices.
/// Fetches step data from server, renders inline editor, saves changes via API.
/// </summary>
public class DialogStepsEditor
{
    private readonly HytaleApiClient _client;
    private const string PluginId = "hyadventure";

    // Step data (loaded from server detail response)
    private List<StepData> _steps = new();
    private string? _dialogId;
    private bool _loaded;
    private bool _loading;

    // New step form
    private string _newStepSpeaker = "";
    private string _newStepText = "";

    // New choice form
    private string _newChoiceLabel = "";
    private string _newChoiceNextStep = "";
    private int _addChoiceStepIdx = -1;
    private string? _statusMessage;
    private bool _statusIsError;

    private static readonly Vector4 StepColor = new(0.83f, 0.66f, 0.26f, 1f);
    private static readonly Vector4 ChoiceColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly Vector4 DimColor = new(0.55f, 0.55f, 0.63f, 1f);

    public DialogStepsEditor(HytaleApiClient client)
    {
        _client = client;
    }

    public void SetDialog(string dialogId, Dictionary<string, string> values)
    {
        if (_dialogId == dialogId && _loaded) return;
        _dialogId = dialogId;
        _loaded = false;
        _loading = false;
        _steps.Clear();

        // Parse steps from entity values if available
        // Steps come as JSON in the values — for now, use the API to add/view
    }

    public void Draw()
    {
        if (_dialogId == null) return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(StepColor, "Dialog Steps");
        ImGui.Spacing();

        // Existing steps (if any loaded)
        if (_steps.Count > 0)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                DrawStep(i);
            }
        }
        else
        {
            ImGui.TextColored(DimColor, "No steps yet. Add one below.");
        }

        ImGui.Spacing();

        // Add step form
        DrawAddStepForm();
    }

    private void DrawStep(int idx)
    {
        var step = _steps[idx];
        var flags = ImGuiTreeNodeFlags.DefaultOpen;

        ImGui.PushStyleColor(ImGuiCol.Text, StepColor);
        bool open = ImGui.TreeNodeEx($"Step {idx + 1}: {Truncate(step.DialogText, 30)}##step_{idx}", flags);
        ImGui.PopStyleColor();

        if (open)
        {
            ImGui.TextColored(DimColor, "Speaker:");
            ImGui.SameLine();
            ImGui.Text(step.SpeakerName);

            ImGui.TextColored(DimColor, "Text:");
            ImGui.TextWrapped(step.DialogText);

            // Choices
            if (step.Choices.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(ChoiceColor, "Choices:");
                ImGui.Indent(8);
                for (int c = 0; c < step.Choices.Count; c++)
                {
                    var choice = step.Choices[c];
                    ImGui.BulletText($"{choice.Label}");
                    if (!string.IsNullOrEmpty(choice.NextStepId))
                    {
                        ImGui.SameLine();
                        ImGui.TextColored(DimColor, $"-> {choice.NextStepId}");
                    }
                }
                ImGui.Unindent(8);
            }

            // Add choice button
            if (_addChoiceStepIdx == idx)
            {
                DrawAddChoiceForm(idx);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ChoiceColor);
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
                if (ImGui.SmallButton($"+ Choice##addchoice_{idx}"))
                {
                    _addChoiceStepIdx = idx;
                    _newChoiceLabel = "";
                    _newChoiceNextStep = "";
                }
                ImGui.PopStyleColor(2);
            }

            ImGui.TreePop();
        }
    }

    private void DrawAddStepForm()
    {
        ImGui.TextColored(StepColor, "Add Step");

        ImGui.TextColored(DimColor, "Speaker Name");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##newstep_speaker", ref _newStepSpeaker, 128);

        ImGui.TextColored(DimColor, "Dialog Text");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##newstep_text", ref _newStepText, 512);

        ImGui.Spacing();

        bool canAdd = !string.IsNullOrWhiteSpace(_newStepText);
        if (!canAdd) ImGui.BeginDisabled();
        ImGui.PushStyleColor(ImGuiCol.Button, StepColor);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
        if (ImGui.Button("Add Step##addstep", new Vector2(-1, 0)))
        {
            AddStep();
        }
        ImGui.PopStyleColor(2);
        if (!canAdd) ImGui.EndDisabled();

        // Status message
        if (_statusMessage != null)
        {
            ImGui.Spacing();
            var color = _statusIsError ? new Vector4(0.9f, 0.3f, 0.3f, 1f) : new Vector4(0.31f, 0.80f, 0.40f, 1f);
            ImGui.TextColored(color, _statusMessage);
        }
    }

    private void DrawAddChoiceForm(int stepIdx)
    {
        ImGui.Spacing();
        ImGui.TextColored(ChoiceColor, "New Choice");

        ImGui.TextColored(DimColor, "Label");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText($"##newchoice_label_{stepIdx}", ref _newChoiceLabel, 128);

        ImGui.TextColored(DimColor, "Go to step (optional)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText($"##newchoice_next_{stepIdx}", ref _newChoiceNextStep, 64);

        ImGui.Spacing();

        bool canAdd = !string.IsNullOrWhiteSpace(_newChoiceLabel);
        if (!canAdd) ImGui.BeginDisabled();
        if (ImGui.SmallButton($"Add##confirmchoice_{stepIdx}"))
        {
            AddChoice(stepIdx);
            _addChoiceStepIdx = -1;
        }
        if (!canAdd) ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.SmallButton($"Cancel##cancelchoice_{stepIdx}"))
            _addChoiceStepIdx = -1;
    }

    private void AddStep()
    {
        if (_dialogId == null) return;

        string rawDialogId = _dialogId;
        if (rawDialogId.StartsWith("dlg:"))
            rawDialogId = rawDialogId[4..];

        string stepId = $"step_{_steps.Count}";
        string speaker = _newStepSpeaker;
        string text = _newStepText;

        // Optimistic local update — step appears immediately
        _steps.Add(new StepData
        {
            SpeakerName = speaker,
            DialogText = text,
            StepId = stepId,
        });
        _newStepSpeaker = "";
        _newStepText = "";
        _statusMessage = $"Added step {_steps.Count}";
        _statusIsError = false;

        // Sync to server
        _ = Task.Run(async () =>
        {
            var result = await _client.ExecutePluginActionAsync(PluginId, "addDialogStep", null,
                new Dictionary<string, string>
                {
                    ["dialogId"] = rawDialogId,
                    ["speakerNameText"] = speaker,
                    ["dialogText"] = text,
                    ["stepId"] = stepId,
                });

            if (result?.Success != true)
            {
                _statusMessage = $"Server error: {string.Join(", ", result?.Errors ?? ["Unknown"])}";
                _statusIsError = true;
            }
        });
    }

    private void AddChoice(int stepIdx)
    {
        if (_dialogId == null || stepIdx >= _steps.Count) return;

        string rawDialogId = _dialogId;
        if (rawDialogId.StartsWith("dlg:"))
            rawDialogId = rawDialogId[4..];

        var step = _steps[stepIdx];
        string label = _newChoiceLabel;
        string nextStep = _newChoiceNextStep;

        // Optimistic local update
        step.Choices.Add(new ChoiceData { Label = label, NextStepId = nextStep });
        _newChoiceLabel = "";
        _newChoiceNextStep = "";

        _ = Task.Run(async () =>
        {
            var result = await _client.ExecutePluginActionAsync(PluginId, "addDialogChoice", null,
                new Dictionary<string, string>
                {
                    ["dialogId"] = rawDialogId,
                    ["stepId"] = step.StepId,
                    ["labelText"] = label,
                    ["nextStepId"] = nextStep,
                });

            if (result?.Success != true)
            {
                _statusMessage = $"Choice save failed: {string.Join(", ", result?.Errors ?? ["Unknown"])}";
                _statusIsError = true;
            }
        });
    }

    public List<UI.Components.StepData> GetSteps() => _steps.Select(s => new UI.Components.StepData
    {
        StepId = s.StepId,
        SpeakerName = s.SpeakerName,
        DialogText = s.DialogText,
        Choices = s.Choices.Select(c => new UI.Components.StepChoiceData
        {
            Label = c.Label,
            NextStepId = c.NextStepId,
        }).ToList(),
    }).ToList();

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    private class StepData
    {
        public string StepId = "";
        public string SpeakerName = "";
        public string DialogText = "";
        public List<ChoiceData> Choices = new();
    }

    private class ChoiceData
    {
        public string Label = "";
        public string NextStepId = "";
    }
}
