using System.Text.Json;
using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

/// <summary>
/// Full-width plugin view with entity list on the left and form in the center.
/// Replaces the cramped left-panel PluginPanel when in Plugin view mode.
/// </summary>
public class PluginView
{
    private readonly PluginPanelState _state;
    private string _entitySearch = "";

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 PluginColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly System.Numerics.Vector4 LabelColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly System.Numerics.Vector4 SaveColor = new(0.31f, 0.80f, 0.40f, 1f);
    private static readonly System.Numerics.Vector4 DirtyColor = new(0.95f, 0.75f, 0.20f, 1f);
    private static readonly System.Numerics.Vector4 DangerColor = new(0.65f, 0.18f, 0.25f, 1f);
    private static readonly System.Numerics.Vector4 ActionColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly System.Numerics.Vector4 ButtonTextColor = new(0.10f, 0.10f, 0.12f, 1f);

    public PluginView(ServiceContainer services)
    {
        _state = new PluginPanelState(services.ApiClient);
    }

    public void Draw(float availWidth, float availHeight, float logHeight)
    {
        // Load plugins on first draw
        if (_state.Plugins == null && !_state.PluginsLoading)
            _ = Task.Run(() => _state.LoadPluginsAsync());

        if (_state.PluginsLoading)
        {
            ImGui.TextColored(DimColor, "Loading plugins...");
            return;
        }

        if (_state.Plugins == null || _state.Plugins.Length == 0)
        {
            ImGui.TextColored(DimColor, "No plugins registered. Make sure the server is running.");
            if (ImGui.SmallButton("Refresh"))
                _ = Task.Run(() => _state.LoadPluginsAsync());
            return;
        }

        // Auto-select single plugin
        if (_state.SelectedPluginIdx < 0 && _state.Plugins.Length == 1)
            _state.SelectedPluginIdx = 0;

        // Load schema if needed
        if (_state.SelectedPluginIdx >= 0)
        {
            var plugin = _state.Plugins[_state.SelectedPluginIdx];
            if (_state.CachedSchemaPluginId != plugin.PluginId)
                _ = Task.Run(() => _state.LoadSchemaAsync(plugin.PluginId));
        }

        // Plugin selector bar (if multiple plugins)
        if (_state.Plugins.Length > 1)
        {
            DrawPluginSelectorBar();
            ImGui.Separator();
        }

        if (_state.SelectedPluginIdx < 0)
        {
            ImGui.TextColored(DimColor, "Select a plugin above.");
            return;
        }

        if (_state.SchemaLoading)
        {
            ImGui.TextColored(DimColor, "Loading schema...");
            return;
        }

        if (_state.Schema == null)
        {
            ImGui.TextColored(DimColor, "Schema unavailable.");
            return;
        }

        var pluginId = _state.Plugins[_state.SelectedPluginIdx].PluginId;

        // Load entities if needed
        if (_state.Entities == null && !_state.EntitiesLoading)
            _ = Task.Run(() => _state.LoadEntitiesAsync(pluginId));

        // Two-column layout: left entity list, center form
        float leftPanelWidth = 280f;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        float centerWidth = availWidth - leftPanelWidth - spacing;
        float contentHeight = availHeight - logHeight - ImGui.GetStyle().ItemSpacing.Y;

        // ─── Left panel: Entity list + action buttons ─────────────
        ImGui.BeginChild("PluginLeftPanel", new System.Numerics.Vector2(leftPanelWidth, contentHeight),
            ImGuiChildFlags.Borders);
        DrawLeftPanel(pluginId);
        ImGui.EndChild();

        ImGui.SameLine();

        // ─── Center panel: Form ───────────────────────────────────
        ImGui.BeginChild("PluginCenterPanel", new System.Numerics.Vector2(centerWidth, contentHeight),
            ImGuiChildFlags.Borders);
        DrawCenterPanel(pluginId);
        ImGui.EndChild();
    }

    // ─── Plugin Selector Bar ──────────────────────────────────────

    private void DrawPluginSelectorBar()
    {
        var defaultBtnColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        for (int i = 0; i < _state.Plugins!.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            bool isSelected = i == _state.SelectedPluginIdx;
            ImGui.PushStyleColor(ImGuiCol.Button, isSelected ? PluginColor : defaultBtnColor);
            if (isSelected) ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
            if (ImGui.Button($"{_state.Plugins[i].PluginName}##plugsel{i}"))
            {
                _state.SelectedPluginIdx = i;
                _state.SelectedEntityIdx = -1;
                _state.Entities = null;
                _state.LoadedEntityId = null;
                _state.Mode = FormMode.None;
            }
            if (isSelected) ImGui.PopStyleColor();
            ImGui.PopStyleColor();
        }
    }

    // ─── Left Panel ───────────────────────────────────────────────

    private string _activeSubModule = "";

    private void DrawLeftPanel(string pluginId)
    {
        // Sub-module tabs (if schema has them)
        var subModules = _state.Schema!.SubModules;
        if (subModules is { Length: > 0 })
        {
            if (ImGui.BeginTabBar("SubModuleTabs"))
            {
                foreach (var sm in subModules)
                {
                    if (ImGui.BeginTabItem(sm.Label))
                    {
                        _activeSubModule = sm.Id;
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }
        }
        else
        {
            _activeSubModule = "";
        }

        ImGui.TextColored(PluginColor, _state.Schema!.EntityLabel);
        ImGui.SameLine();
        if (ImGui.SmallButton("Refresh##entities"))
            _ = Task.Run(() => _state.LoadEntitiesAsync(pluginId));

        // Entity-less action buttons (filtered by active sub-module)
        foreach (var action in _state.Schema.Actions ?? [])
        {
            if (action.RequiresEntity) continue;
            if (!string.IsNullOrEmpty(_activeSubModule) &&
                !string.Equals(action.SubModule, _activeSubModule, StringComparison.OrdinalIgnoreCase))
                continue;
            ImGui.PushStyleColor(ImGuiCol.Button, ActionColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ActionColor with { W = 0.85f });
            ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
            if (ImGui.Button($"+ {action.Label}##action_{action.Id}", new System.Numerics.Vector2(-1, 0)))
            {
                _state.SelectedEntityIdx = -1;
                _state.BeginAction(action);
            }
            ImGui.PopStyleColor(3);
        }

        ImGui.Separator();

        if (_state.EntitiesLoading)
        {
            ImGui.TextColored(DimColor, "Loading...");
            return;
        }

        if (_state.Entities == null || _state.Entities.Length == 0)
        {
            ImGui.TextColored(DimColor, $"No {_state.Schema.EntityLabel.ToLower()} found.");
            return;
        }

        // Search
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##entitySearch", ref _entitySearch, 128);

        // Entity list
        if (ImGui.BeginChild("EntityListScroll"))
        {
            var searchLower = _entitySearch.ToLowerInvariant();
            for (int i = 0; i < _state.Entities.Length; i++)
            {
                var e = _state.Entities[i];

                // Filter by active sub-module tab
                if (!string.IsNullOrEmpty(_activeSubModule) &&
                    !string.Equals(e.Group, _activeSubModule, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(_entitySearch) &&
                    !e.Label.Contains(searchLower, StringComparison.OrdinalIgnoreCase) &&
                    !(e.Group?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;

                bool selected = i == _state.SelectedEntityIdx && _state.Mode == FormMode.EditEntity;
                var label = string.IsNullOrEmpty(e.Group) ? e.Label : $"[{e.Group}] {e.Label}";
                if (ImGui.Selectable($"{label}##ent{i}", selected))
                {
                    _state.SelectedEntityIdx = i;
                    _ = Task.Run(() => _state.LoadEntityValuesAsync(pluginId, e.Id));
                }
            }
        }
        ImGui.EndChild();
    }

    // ─── Center Panel ─────────────────────────────────────────────

    private void DrawCenterPanel(string pluginId)
    {
        switch (_state.Mode)
        {
            case FormMode.None:
                DrawEmptyState();
                break;
            case FormMode.EditEntity:
                DrawEditForm(pluginId);
                break;
            case FormMode.Action:
                DrawActionForm(pluginId);
                break;
        }
    }

    private void DrawEmptyState()
    {
        var avail = ImGui.GetContentRegionAvail();
        ImGui.SetCursorPos(new System.Numerics.Vector2(avail.X / 2 - 80, avail.Y / 2 - 10));
        ImGui.TextColored(DimColor, "Select an entity or action");
    }

    // ─── Edit Form ────────────────────────────────────────────────

    private void DrawEditForm(string pluginId)
    {
        if (_state.ValuesLoading)
        {
            ImGui.TextColored(DimColor, "Loading values...");
            return;
        }

        if (_state.LoadedEntityId == null || _state.Schema == null) return;

        // Entity header
        ImGui.TextColored(PluginColor, _state.EditedValues.GetValueOrDefault("name", "Entity"));
        ImGui.SameLine();
        ImGui.TextColored(DimColor, $"({_state.LoadedEntityId})");

        // Entity-bound action buttons (e.g., "Delete")
        foreach (var action in _state.Schema.Actions)
        {
            if (!action.RequiresEntity) continue;
            ImGui.SameLine();
            var color = action.Id == "delete" ? DangerColor : ActionColor;
            ImGui.PushStyleColor(ImGuiCol.Button, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color with { W = 0.85f });
            ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
            if (ImGui.SmallButton($"{action.Label}##eaction_{action.Id}"))
            {
                if (action.Confirm != null)
                {
                    _state.ActiveAction = action;
                    _state.ActiveActionId = action.Id;
                    _state.ShowConfirmDialog = true;
                }
                else if (action.Groups.Length == 0)
                {
                    _state.ActiveActionId = action.Id;
                    _state.ActiveAction = action;
                    _state.ActionFormValues = new();
                    _ = Task.Run(() => _state.ExecuteActionAsync(pluginId, _state.LoadedEntityId));
                }
                else
                {
                    _state.BeginAction(action);
                }
            }
            ImGui.PopStyleColor(3);
        }

        ImGui.Separator();

        // Confirmation dialog
        if (_state.ShowConfirmDialog && _state.ActiveAction != null)
        {
            DrawConfirmDialog(pluginId);
            return;
        }

        // Edit form groups — only show groups that have matching fields in current entity values
        var groups = _state.Schema.Groups.OrderBy(g => g.Order).ToArray();
        if (ImGui.BeginChild("EditFormScroll", new System.Numerics.Vector2(0, -30)))
        {
            foreach (var group in groups)
            {
                if (group.Fields.Length == 0) continue;
                bool hasMatchingField = group.Fields.Any(f => _state.EditedValues.ContainsKey(f.Id));
                if (!hasMatchingField) continue;
                if (ImGui.CollapsingHeader(group.Label, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Indent(8);
                    foreach (var field in group.Fields)
                        DrawField(field, _state.EditedValues, _state.DirtyFields);
                    ImGui.Unindent(8);
                }
            }
        }
        ImGui.EndChild();

        // Save bar
        DrawSaveBar(pluginId);
    }

    // ─── Action Form ──────────────────────────────────────────────

    private void DrawActionForm(string pluginId)
    {
        if (_state.ActiveAction == null) return;

        ImGui.TextColored(ActionColor, _state.ActiveAction.Label);
        if (_state.ActiveAction.Description != null)
        {
            ImGui.SameLine();
            ImGui.TextColored(DimColor, $"— {_state.ActiveAction.Description}");
        }
        ImGui.Separator();

        // Action form groups
        var groups = _state.ActiveAction.Groups.OrderBy(g => g.Order).ToArray();
        if (groups.Length > 0)
        {
            if (ImGui.BeginChild("ActionFormScroll", new System.Numerics.Vector2(0, -30)))
            {
                foreach (var group in groups)
                {
                    if (group.Fields.Length == 0) continue;
                    if (ImGui.CollapsingHeader(group.Label, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.Indent(8);
                        foreach (var field in group.Fields)
                            DrawActionField(field);
                        ImGui.Unindent(8);
                    }
                }
            }
            ImGui.EndChild();
        }

        // Action result message
        if (_state.ActionResult != null)
        {
            var color = _state.ActionResult.Success ? SaveColor : AccentColor;
            var msg = _state.ActionResult.Success
                ? _state.ActionResult.Message ?? "Success"
                : string.Join(", ", _state.ActionResult.Errors ?? ["Unknown error"]);
            ImGui.TextColored(color, msg);
        }

        // Action buttons
        float btnWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;

        ImGui.PushStyleColor(ImGuiCol.Button, ActionColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ActionColor with { W = 0.85f });
        ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        bool disabled = _state.ActionExecuting;
        if (disabled) ImGui.BeginDisabled();
        if (ImGui.Button(_state.ActionExecuting ? "Executing..." : _state.ActiveAction.Label,
                new System.Numerics.Vector2(btnWidth, 0)))
        {
            string? entityId = _state.ActiveAction.RequiresEntity ? _state.LoadedEntityId : null;
            _ = Task.Run(() => _state.ExecuteActionAsync(pluginId, entityId));
        }
        if (disabled) ImGui.EndDisabled();
        ImGui.PopStyleColor(3);

        ImGui.SameLine();

        if (ImGui.Button("Cancel", new System.Numerics.Vector2(btnWidth, 0)))
            _state.CancelAction();
    }

    // ─── Confirm Dialog ───────────────────────────────────────────

    private void DrawConfirmDialog(string pluginId)
    {
        ImGui.Spacing();
        ImGui.TextColored(DangerColor, _state.ActiveAction!.Confirm ?? "Are you sure?");
        ImGui.Spacing();

        float btnWidth = 120;
        ImGui.PushStyleColor(ImGuiCol.Button, DangerColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, DangerColor with { W = 0.85f });
        ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.Button("Confirm", new System.Numerics.Vector2(btnWidth, 0)))
        {
            _state.ShowConfirmDialog = false;
            _state.ActionFormValues = new();
            _ = Task.Run(() => _state.ExecuteActionAsync(pluginId, _state.LoadedEntityId));
        }
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        if (ImGui.Button("Cancel##confirm", new System.Numerics.Vector2(btnWidth, 0)))
        {
            _state.ShowConfirmDialog = false;
            _state.ActiveActionId = null;
            _state.ActiveAction = null;
        }
    }

    // ─── Field Rendering ──────────────────────────────────────────

    private void DrawField(FieldDefinitionDto field, Dictionary<string, string> values, HashSet<string> dirtyFields)
    {
        if (!values.TryGetValue(field.Id, out var currentVal))
            currentVal = "";

        bool isDirty = dirtyFields.Contains(field.Id);
        if (isDirty) ImGui.PushStyleColor(ImGuiCol.Text, DirtyColor);
        if (field.ReadOnly) ImGui.BeginDisabled();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.6f);
        RenderWidget(field, field.Id, currentVal, val =>
        {
            values[field.Id] = val;
            _state.UpdateDirty(field.Id);
        });

        if (field.ReadOnly) ImGui.EndDisabled();
        if (isDirty) ImGui.PopStyleColor();
    }

    private void DrawActionField(FieldDefinitionDto field)
    {
        if (!_state.ActionFormValues.TryGetValue(field.Id, out var currentVal))
            currentVal = "";

        if (field.Required)
        {
            ImGui.TextColored(AccentColor, "*");
            ImGui.SameLine();
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.6f);
        RenderWidget(field, $"act_{field.Id}", currentVal, val =>
        {
            _state.ActionFormValues[field.Id] = val;
        });
    }

    private void RenderWidget(FieldDefinitionDto field, string imguiId, string val, Action<string> onChange)
    {
        switch (field.Type)
        {
            case "string":
            {
                if (!_state.TextBuffers.ContainsKey(imguiId))
                    _state.TextBuffers[imguiId] = val;
                var buf = _state.TextBuffers[imguiId];
                if (ImGui.InputText($"{field.Label}##{imguiId}", ref buf, 256))
                {
                    _state.TextBuffers[imguiId] = buf;
                    onChange(buf);
                }
                break;
            }
            case "int":
            {
                int v = int.TryParse(val, out var p) ? p : 0;
                bool changed = field.Min.HasValue && field.Max.HasValue
                    ? ImGui.SliderInt($"{field.Label}##{imguiId}", ref v, (int)field.Min.Value, (int)field.Max.Value)
                    : ImGui.InputInt($"{field.Label}##{imguiId}", ref v);
                if (changed) onChange(v.ToString());
                break;
            }
            case "float":
            {
                float v = float.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0f;
                bool changed = field.Min.HasValue && field.Max.HasValue
                    ? ImGui.SliderFloat($"{field.Label}##{imguiId}", ref v, field.Min.Value, field.Max.Value, "%.2f")
                    : ImGui.InputFloat($"{field.Label}##{imguiId}", ref v, 0.1f, 1.0f, "%.2f");
                if (changed) onChange(v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                break;
            }
            case "bool":
            {
                bool v = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                if (ImGui.Checkbox($"{field.Label}##{imguiId}", ref v))
                    onChange(v ? "true" : "false");
                break;
            }
            case "enum":
            {
                var options = field.EnumValues ?? [];
                int selected = Array.IndexOf(options, val);
                if (selected < 0) selected = 0;
                if (ImGui.Combo($"{field.Label}##{imguiId}", ref selected, options, options.Length))
                    onChange(options[selected]);
                break;
            }
            case "vector3d":
            case "vector3f":
            {
                float x = 0, y = 0, z = 0;
                try
                {
                    if (val.StartsWith('{'))
                    {
                        var doc = JsonDocument.Parse(val);
                        x = doc.RootElement.GetProperty("x").GetSingle();
                        y = doc.RootElement.GetProperty("y").GetSingle();
                        z = doc.RootElement.GetProperty("z").GetSingle();
                    }
                }
                catch { }
                ImGui.Text(field.Label);
                ImGui.TextColored(DimColor, $"  X: {x:F2}  Y: {y:F2}  Z: {z:F2}");
                break;
            }
            default:
                ImGui.Text($"{field.Label}: {val}");
                break;
        }
    }

    private void DrawSaveBar(string pluginId)
    {
        int dirtyCount = _state.DirtyFields.Count;

        if (dirtyCount > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, SaveColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SaveColor with { W = 0.85f });
            ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
            if (ImGui.Button($"Save ({dirtyCount} changes)", new System.Numerics.Vector2(-1, 0)) && !_state.Saving)
                _ = Task.Run(() => _state.SaveChangesAsync(pluginId));
            ImGui.PopStyleColor(3);
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.Button("No changes", new System.Numerics.Vector2(-1, 0));
            ImGui.EndDisabled();
        }

        if (_state.SaveStatus != null)
            ImGui.TextColored(_state.SaveStatus.StartsWith("Error") ? AccentColor : SaveColor, _state.SaveStatus);
    }
}
