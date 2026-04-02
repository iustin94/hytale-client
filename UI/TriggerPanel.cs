using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

/// <summary>
/// Panel for creating and managing area-based triggers.
/// Triggers define: condition (area/manual) → action (play sound, spawn entity, change ambiance).
/// </summary>
public class TriggerPanel
{
    private readonly ServiceContainer _services;

    private readonly List<TriggerDefinition> _triggers = new();
    private int _selectedTriggerIdx = -1;

    // New trigger creation state
    private string _newTriggerName = "New Trigger";
    private int _conditionType;
    private int _actionType;
    private string _areaMinX = "", _areaMinZ = "", _areaMaxX = "", _areaMaxZ = "";
    private string _soundId = "";
    private string _entityType = "";
    private string _posX = "", _posY = "64", _posZ = "";
    private string _ambientInterval = "5";

    // Indicates if the panel is currently defining an area via map interaction
    public bool IsDefiningArea { get; set; }
    public (float MinX, float MinZ, float MaxX, float MaxZ)? PendingArea { get; set; }

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 TriggerColor = new(0.80f, 0.60f, 0.20f, 1f);
    private static readonly System.Numerics.Vector4 LabelColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly System.Numerics.Vector4 EnabledColor = new(0.31f, 0.80f, 0.40f, 1f);
    private static readonly System.Numerics.Vector4 DisabledColor = new(0.55f, 0.55f, 0.55f, 1f);
    private static readonly System.Numerics.Vector4 ActionBtnColor = new(0.22f, 0.22f, 0.30f, 1f);
    private static readonly System.Numerics.Vector4 DangerBtnColor = new(0.65f, 0.18f, 0.25f, 1f);
    private static readonly System.Numerics.Vector4 ExecuteColor = new(0.80f, 0.60f, 0.20f, 1f);

    private static readonly string[] ConditionLabels = ["Player Enters Area", "Manual"];
    private static readonly string[] ActionLabels = ["Play Sound", "Spawn Entity", "Start Ambient", "Stop Ambient"];

    public TriggerPanel(ServiceContainer services)
    {
        _services = services;
    }

    public void Draw()
    {
        ImGui.TextColored(TriggerColor, "Triggers");
        ImGui.Separator();

        // Trigger list
        DrawTriggerList();

        ImGui.Separator();

        // Create / Edit
        if (_selectedTriggerIdx >= 0 && _selectedTriggerIdx < _triggers.Count)
            DrawTriggerEditor(_triggers[_selectedTriggerIdx]);
        else
            DrawNewTriggerForm();
    }

    /// <summary>
    /// Returns all triggers for rendering on the map.
    /// </summary>
    public IReadOnlyList<TriggerDefinition> Triggers => _triggers;

    // ─── Trigger List ─────────────────────────────────────────────

    private void DrawTriggerList()
    {
        if (_triggers.Count == 0)
        {
            ImGui.TextColored(DimColor, "No triggers defined.\nCreate one below.");
            return;
        }

        if (ImGui.BeginChild("TriggerList", new System.Numerics.Vector2(0, Math.Min(_triggers.Count * 24 + 8, 150))))
        {
            for (int i = 0; i < _triggers.Count; i++)
            {
                var trigger = _triggers[i];
                var statusColor = trigger.Enabled ? EnabledColor : DisabledColor;
                ImGui.TextColored(statusColor, trigger.Enabled ? "●" : "○");
                ImGui.SameLine();

                bool selected = i == _selectedTriggerIdx;
                if (ImGui.Selectable($"{trigger.Name}##trig{i}", selected))
                    _selectedTriggerIdx = i;
            }
            ImGui.EndChild();
        }
    }

    // ─── Trigger Editor (existing trigger) ────────────────────────

    private void DrawTriggerEditor(TriggerDefinition trigger)
    {
        ImGui.TextColored(LabelColor, "Edit Trigger");
        ImGui.Spacing();

        // Toggle enabled
        bool enabled = trigger.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            trigger.Enabled = enabled;

        ImGui.Spacing();

        // Show condition summary
        ImGui.TextColored(LabelColor, "Condition:");
        ImGui.Text(trigger.Condition.Type == TriggerConditionType.PlayerEntersArea
            ? $"Area ({trigger.Condition.MinX:F0},{trigger.Condition.MinZ:F0}) → ({trigger.Condition.MaxX:F0},{trigger.Condition.MaxZ:F0})"
            : "Manual activation");

        ImGui.Spacing();

        // Show action summary
        ImGui.TextColored(LabelColor, "Action:");
        ImGui.Text(trigger.Action.Type switch
        {
            TriggerActionType.PlaySound => $"Play: {trigger.Action.SoundId}",
            TriggerActionType.SpawnEntity => $"Spawn: {trigger.Action.EntityType}",
            TriggerActionType.StartAmbient => $"Ambient: {trigger.Action.AmbientSoundId}",
            TriggerActionType.StopAmbient => "Stop ambient",
            _ => "Unknown"
        });

        ImGui.Spacing();

        // Execute button
        ImGui.PushStyleColor(ImGuiCol.Button, ExecuteColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ExecuteColor with { W = 0.85f });
        if (ImGui.Button("Execute Now", new System.Numerics.Vector2(-1, 0)))
            _ = ExecuteTrigger(trigger);
        ImGui.PopStyleColor(2);

        ImGui.Spacing();

        // Delete
        ImGui.PushStyleColor(ImGuiCol.Button, DangerBtnColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, DangerBtnColor with { W = 0.85f });
        if (ImGui.Button("Delete Trigger", new System.Numerics.Vector2(-1, 0)))
        {
            _triggers.RemoveAt(_selectedTriggerIdx);
            _selectedTriggerIdx = -1;
        }
        ImGui.PopStyleColor(2);
    }

    // ─── New Trigger Form ─────────────────────────────────────────

    private void DrawNewTriggerForm()
    {
        ImGui.TextColored(LabelColor, "New Trigger");
        ImGui.Spacing();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("Name##trig", ref _newTriggerName, 64);

        ImGui.Spacing();

        // Condition
        ImGui.TextColored(LabelColor, "When:");
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo("##condition", ref _conditionType, ConditionLabels, ConditionLabels.Length);

        if (_conditionType == 0) // Area
        {
            if (PendingArea.HasValue)
            {
                _areaMinX = PendingArea.Value.MinX.ToString("F0");
                _areaMinZ = PendingArea.Value.MinZ.ToString("F0");
                _areaMaxX = PendingArea.Value.MaxX.ToString("F0");
                _areaMaxZ = PendingArea.Value.MaxZ.ToString("F0");
                PendingArea = null;
            }

            float w2 = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;
            ImGui.SetNextItemWidth(w2);
            ImGui.InputText("MinX", ref _areaMinX, 16);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(w2);
            ImGui.InputText("MinZ", ref _areaMinZ, 16);

            ImGui.SetNextItemWidth(w2);
            ImGui.InputText("MaxX", ref _areaMaxX, 16);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(w2);
            ImGui.InputText("MaxZ", ref _areaMaxZ, 16);

            // "Draw area on map" button
            ImGui.PushStyleColor(ImGuiCol.Button, IsDefiningArea ? AccentColor : ActionBtnColor);
            if (ImGui.Button(IsDefiningArea ? "Drawing... (Shift+Drag)" : "Draw Area on Map", new System.Numerics.Vector2(-1, 0)))
                IsDefiningArea = !IsDefiningArea;
            ImGui.PopStyleColor();
        }

        ImGui.Spacing();

        // Action
        ImGui.TextColored(LabelColor, "Then:");
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo("##action", ref _actionType, ActionLabels, ActionLabels.Length);

        switch ((TriggerActionType)_actionType)
        {
            case TriggerActionType.PlaySound:
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("Sound ID##act", ref _soundId, 128);
                DrawPositionInputs();
                break;

            case TriggerActionType.SpawnEntity:
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("Entity Type##act", ref _entityType, 128);
                DrawPositionInputs();
                break;

            case TriggerActionType.StartAmbient:
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("Sound ID##amb", ref _soundId, 128);
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("Interval (s)", ref _ambientInterval, 8);
                break;

            case TriggerActionType.StopAmbient:
                ImGui.TextColored(DimColor, "Stops all ambient in area");
                break;
        }

        ImGui.Spacing();

        // Create button
        ImGui.PushStyleColor(ImGuiCol.Button, TriggerColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, TriggerColor with { W = 0.85f });
        if (ImGui.Button("Create Trigger", new System.Numerics.Vector2(-1, 0)))
            CreateTrigger();
        ImGui.PopStyleColor(2);
    }

    private void DrawPositionInputs()
    {
        float w3 = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3f;
        ImGui.SetNextItemWidth(w3);
        ImGui.InputText("X##pos", ref _posX, 16);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(w3);
        ImGui.InputText("Y##pos", ref _posY, 16);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(w3);
        ImGui.InputText("Z##pos", ref _posZ, 16);
    }

    private void CreateTrigger()
    {
        var trigger = new TriggerDefinition
        {
            Name = _newTriggerName,
            Condition = new TriggerCondition
            {
                Type = (TriggerConditionType)_conditionType,
            },
            Action = new TriggerAction
            {
                Type = (TriggerActionType)_actionType,
                SoundId = _soundId,
                EntityType = _entityType,
                AmbientSoundId = _soundId,
            }
        };

        // Parse area bounds
        if (float.TryParse(_areaMinX, out var minX)) trigger.Condition.MinX = minX;
        if (float.TryParse(_areaMinZ, out var minZ)) trigger.Condition.MinZ = minZ;
        if (float.TryParse(_areaMaxX, out var maxX)) trigger.Condition.MaxX = maxX;
        if (float.TryParse(_areaMaxZ, out var maxZ)) trigger.Condition.MaxZ = maxZ;

        // Parse position
        if (float.TryParse(_posX, out var px)) trigger.Action.X = px;
        if (float.TryParse(_posY, out var py)) trigger.Action.Y = py;
        if (float.TryParse(_posZ, out var pz)) trigger.Action.Z = pz;

        if (int.TryParse(_ambientInterval, out var interval)) trigger.Action.AmbientInterval = interval;

        _triggers.Add(trigger);
        _selectedTriggerIdx = _triggers.Count - 1;
        _newTriggerName = "New Trigger";
        IsDefiningArea = false;
    }

    // ─── Execute ──────────────────────────────────────────────────

    private async Task ExecuteTrigger(TriggerDefinition trigger)
    {
        var api = _services.ApiClient;
        var world = _services.Config.WorldId;

        switch (trigger.Action.Type)
        {
            case TriggerActionType.PlaySound:
                await api.PlaySoundAsync(new SoundPlayRequest
                {
                    Sound = trigger.Action.SoundId,
                    World = world,
                    X = trigger.Action.X, Y = trigger.Action.Y, Z = trigger.Action.Z
                });
                break;

            case TriggerActionType.SpawnEntity:
                await api.SpawnEntityAsync(new EntitySpawnRequest
                {
                    Type = trigger.Action.EntityType,
                    World = world,
                    X = trigger.Action.X, Y = trigger.Action.Y, Z = trigger.Action.Z
                });
                await _services.EntityData.PollAsync(api, _services.Config);
                break;

            case TriggerActionType.StartAmbient:
                var cond = trigger.Condition;
                float cx = (cond.MinX + cond.MaxX) / 2f;
                float cz = (cond.MinZ + cond.MaxZ) / 2f;
                await api.StartAmbientAsync(new SoundAmbientRequest
                {
                    Sound = trigger.Action.AmbientSoundId,
                    World = world,
                    X = cx, Y = trigger.Action.Y, Z = cz,
                    MinX = cond.MinX, MinZ = cond.MinZ,
                    MaxX = cond.MaxX, MaxZ = cond.MaxZ,
                    Interval = trigger.Action.AmbientInterval
                });
                await _services.EntityData.PollAsync(api, _services.Config);
                break;

            case TriggerActionType.StopAmbient:
                await api.StopAmbientAsync();
                await _services.EntityData.PollAsync(api, _services.Config);
                break;
        }
    }
}
