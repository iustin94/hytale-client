using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Services;
using HytaleAdmin.UI.NodeEditor;
using Stride.Input;

namespace HytaleAdmin.UI.Components;

/// <summary>
/// Dialog flow sub-graph editor. Each dialog step is a node, each choice is an output port
/// connecting to the next step's input port. Reuses NodeEditor&lt;DialogStepNode&gt;.
/// Opens as a floating window when user double-clicks a Dialog node in the quest graph.
/// </summary>
public class DialogFlowEditor
{
    private readonly HytaleApiClient _client;
    private readonly NodeEditor<DialogStepNode> _editor;

    private bool _open;
    private string? _dialogId;
    private string _dialogTitle = "";
    private List<DialogStepNode> _nodes = new();

    private static readonly PortTypeMap PortTypes = new PortTypeMap().Allow("dialog_flow", "dialog_flow");
    private const string PluginId = "hyadventure";

    // New step form
    private bool _showAddStep;
    private string _newSpeaker = "";
    private string _newText = "";

    public bool IsOpen => _open;

    public DialogFlowEditor(HytaleApiClient client)
    {
        _client = client;
        _editor = new NodeEditor<DialogStepNode>(PortTypes);

        _editor.SetStyle("step", new NodeStyle
        {
            HeaderColor = new Vector4(0.83f, 0.66f, 0.26f, 1f),
            BodyColor = new Vector4(0.14f, 0.14f, 0.19f, 0.95f),
            BorderColor = new Vector4(0.83f, 0.66f, 0.26f, 0.5f),
            MinWidth = 220f,
        });

        _editor.SetStyle("start", new NodeStyle
        {
            HeaderColor = new Vector4(0.31f, 0.80f, 0.40f, 1f),
            BodyColor = new Vector4(0.14f, 0.14f, 0.19f, 0.95f),
            BorderColor = new Vector4(0.31f, 0.80f, 0.40f, 0.5f),
            MinWidth = 220f,
        });

        _editor.DrawNodeContent = DrawStepContent;
        _editor.MeasureContentHeight = MeasureStepContent;

        _editor.OnLinkCreated = OnLinkCreated;
        _editor.ContextMenu = new DialogFlowContextMenu(this);
    }

    public void Open(string dialogId, string dialogTitle, List<StepData> steps)
    {
        _dialogId = dialogId;
        _dialogTitle = dialogTitle;
        _open = true;
        _showAddStep = false;

        _editor.Clear();
        _nodes.Clear();

        // Build nodes from steps
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var ports = new List<PortDefinition>
            {
                new("in", "In", PortDirection.Input, "dialog_flow") { Color = 0xFF_43A8D4 },
            };

            // Each choice becomes an output port
            for (int c = 0; c < step.Choices.Count; c++)
            {
                ports.Add(new PortDefinition(
                    $"choice_{c}", step.Choices[c].Label, PortDirection.Output, "dialog_flow")
                { Color = 0xFF_3AA0C5 });
            }

            // Default "next" port if no choices but has nextStepId
            if (step.Choices.Count == 0)
            {
                ports.Add(new PortDefinition("next", "Continue", PortDirection.Output, "dialog_flow")
                { Color = 0xFF_43A8D4 });
            }

            var node = new DialogStepNode
            {
                Id = step.StepId,
                NodeType = i == 0 ? "start" : "step",
                Title = $"Step {i + 1}",
                Subtitle = Truncate(step.SpeakerName, 15),
                Position = new Vector2(i * 280f, 0),
                Ports = ports,
                SpeakerName = step.SpeakerName,
                DialogText = step.DialogText,
                StepIndex = i,
                Choices = step.Choices,
            };

            _nodes.Add(node);
            _editor.AddNode(node);
        }

        // Build links from nextStepId references
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var node = _nodes[i];

            if (step.Choices.Count == 0 && !string.IsNullOrEmpty(step.NextStepId))
            {
                // "Continue" link
                var targetNode = _nodes.FirstOrDefault(n => n.Id == step.NextStepId);
                if (targetNode != null)
                {
                    _editor.AddLink(new NodeLink(
                        $"{node.Id}:next->{targetNode.Id}:in",
                        node.Id, "next", targetNode.Id, "in"));
                }
            }

            for (int c = 0; c < step.Choices.Count; c++)
            {
                var choice = step.Choices[c];
                if (string.IsNullOrEmpty(choice.NextStepId)) continue;
                var targetNode = _nodes.FirstOrDefault(n => n.Id == choice.NextStepId);
                if (targetNode != null)
                {
                    _editor.AddLink(new NodeLink(
                        $"{node.Id}:choice_{c}->{targetNode.Id}:in",
                        node.Id, $"choice_{c}", targetNode.Id, "in"));
                }
            }
        }

        _editor.CenterOnNodes();
    }

    public void Draw(InputManager? input = null)
    {
        if (!_open) return;

        ImGui.SetNextWindowSize(new Vector2(700, 450), ImGuiCond.Once);

        bool open = true;
        if (ImGui.Begin($"Dialog Flow: {_dialogTitle}##DialogFlow", ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
        {
            var avail = ImGui.GetContentRegionAvail();

            if (_showAddStep)
            {
                DrawAddStepForm(avail);
            }
            else
            {
                _editor.Draw(avail.X, avail.Y - 4, input);
            }
        }
        ImGui.End();

        if (!open) _open = false;
    }

    // ─── Node content rendering ──────────────────────────────────

    private void DrawStepContent(DialogStepNode node, ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        float y = min.Y;
        float lineH = 13f;
        uint textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.65f, 0.65f, 0.73f, 1f));

        // Dialog text preview
        var preview = Truncate(node.DialogText, 35);
        drawList.AddText(new Vector2(min.X, y), textColor, preview);
    }

    private float MeasureStepContent(DialogStepNode node)
    {
        return 16f;
    }

    // ─── Link creation ───────────────────────────────────────────

    private void OnLinkCreated(NodeLink link)
    {
        // A link from a choice port to another step's input = setting nextStepId
        // For now, store locally — server sync on close
        var srcNode = _nodes.FirstOrDefault(n => n.Id == link.SourceNodeId);
        if (srcNode == null) return;

        string targetStepId = link.TargetNodeId;

        if (link.SourcePortId == "next")
        {
            // Update step's nextStepId
            // TODO: sync to server
        }
        else if (link.SourcePortId.StartsWith("choice_"))
        {
            int choiceIdx = int.Parse(link.SourcePortId.Replace("choice_", ""));
            if (choiceIdx < srcNode.Choices.Count)
            {
                srcNode.Choices[choiceIdx].NextStepId = targetStepId;
                // TODO: sync to server
            }
        }
    }

    // ─── Add step ────────────────────────────────────────────────

    internal void ShowAddStep() => _showAddStep = true;

    private void DrawAddStepForm(Vector2 avail)
    {
        ImGui.TextColored(new Vector4(0.83f, 0.66f, 0.26f, 1f), "Add Dialog Step");
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Speaker Name");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##dlgflow_speaker", ref _newSpeaker, 128);

        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Dialog Text");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##dlgflow_text", ref _newText, 512);

        ImGui.Spacing();

        float btnW = 100;
        bool canAdd = !string.IsNullOrWhiteSpace(_newText);
        if (!canAdd) ImGui.BeginDisabled();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.83f, 0.66f, 0.26f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
        if (ImGui.Button("Add##dlgflow_add", new Vector2(btnW, 0)))
        {
            ExecuteAddStep();
        }
        ImGui.PopStyleColor(2);
        if (!canAdd) ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Cancel##dlgflow_cancel", new Vector2(btnW, 0)))
            _showAddStep = false;
    }

    private void ExecuteAddStep()
    {
        if (_dialogId == null) return;

        string rawDialogId = _dialogId;
        if (rawDialogId.StartsWith("dlg:"))
            rawDialogId = rawDialogId[4..];

        string stepId = $"step_{_nodes.Count}";
        string speaker = _newSpeaker;
        string text = _newText;

        // Add node locally
        var ports = new List<PortDefinition>
        {
            new("in", "In", PortDirection.Input, "dialog_flow") { Color = 0xFF_43A8D4 },
            new("next", "Continue", PortDirection.Output, "dialog_flow") { Color = 0xFF_43A8D4 },
        };

        var node = new DialogStepNode
        {
            Id = stepId,
            NodeType = _nodes.Count == 0 ? "start" : "step",
            Title = $"Step {_nodes.Count + 1}",
            Subtitle = Truncate(speaker, 15),
            Position = new Vector2(_nodes.Count * 280f, 0),
            Ports = ports,
            SpeakerName = speaker,
            DialogText = text,
            StepIndex = _nodes.Count,
            Choices = new(),
        };

        _nodes.Add(node);
        _editor.AddNode(node);
        _newSpeaker = "";
        _newText = "";
        _showAddStep = false;

        // Sync to server
        _ = Task.Run(async () =>
        {
            await _client.ExecutePluginActionAsync(PluginId, "addDialogStep", null,
                new Dictionary<string, string>
                {
                    ["dialogId"] = rawDialogId,
                    ["speakerNameText"] = speaker,
                    ["dialogText"] = text,
                    ["stepId"] = stepId,
                });
        });
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}

// ─── Dialog step node ────────────────────────────────────────

public class DialogStepNode : INode
{
    public required string Id { get; set; }
    public required string NodeType { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public Vector2 Position { get; set; }
    public IReadOnlyList<PortDefinition> Ports { get; set; } = [];

    public string SpeakerName { get; set; } = "";
    public string DialogText { get; set; } = "";
    public int StepIndex { get; set; }
    public List<StepChoiceData> Choices { get; set; } = new();
}

public class StepChoiceData
{
    public string Label { get; set; } = "";
    public string NextStepId { get; set; } = "";
    public string StartObjectiveId { get; set; } = "";
}

// Shared step data (used by both DialogStepsEditor and DialogFlowEditor)
public class StepData
{
    public string StepId { get; set; } = "";
    public string SpeakerName { get; set; } = "";
    public string DialogText { get; set; } = "";
    public string NextStepId { get; set; } = "";
    public List<StepChoiceData> Choices { get; set; } = new();
}

// ─── Context menu ────────────────────────────────────────────

internal class DialogFlowContextMenu : IContextMenuProvider<DialogStepNode>
{
    private readonly DialogFlowEditor _editor;

    public DialogFlowContextMenu(DialogFlowEditor editor) => _editor = editor;

    public List<ContextMenuItem> GetMenuItems(ContextMenuRequest<DialogStepNode> request)
    {
        if (request.Target == ContextMenuTarget.Canvas)
        {
            return [new ContextMenuItem { Id = "_add_step", Label = "Add Step" }];
        }
        return [];
    }

    public void OnItemSelected(ContextMenuItem item, ContextMenuRequest<DialogStepNode> request)
    {
        if (item.Id == "_add_step")
            _editor.ShowAddStep();
    }
}
