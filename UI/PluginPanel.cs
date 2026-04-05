using System.Text.Json;
using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

/// <summary>
/// Generic panel that discovers registered dashboard schema providers,
/// lists their entities, and renders a dynamic form from the schema.
/// Delegates all state management and async logic to PluginPanelState.
/// </summary>
public class PluginPanel
{
    private readonly PluginPanelState _state;
    private string _entitySearch = "";

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 PluginColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly System.Numerics.Vector4 LabelColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly System.Numerics.Vector4 SaveColor = new(0.31f, 0.80f, 0.40f, 1f);
    private static readonly System.Numerics.Vector4 DirtyColor = new(0.95f, 0.75f, 0.20f, 1f);

    public PluginPanel(ServiceContainer services)
    {
        _state = new PluginPanelState(services.ApiClient);
    }

    public void Draw()
    {
        ImGui.TextColored(PluginColor, "Plugins");
        ImGui.Separator();

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
            ImGui.TextColored(DimColor, "No plugins registered.");
            if (ImGui.SmallButton("Refresh"))
                _ = Task.Run(() => _state.LoadPluginsAsync());
            return;
        }

        // Plugin selector
        DrawPluginSelector();

        if (_state.SelectedPluginIdx < 0 || _state.SelectedPluginIdx >= _state.Plugins.Length)
            return;

        var selectedPlugin = _state.Plugins[_state.SelectedPluginIdx];
        ImGui.Separator();

        // Load schema if needed
        if (_state.CachedSchemaPluginId != selectedPlugin.PluginId)
            _ = Task.Run(() => _state.LoadSchemaAsync(selectedPlugin.PluginId));

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

        // Entity list + form
        DrawEntityList(selectedPlugin.PluginId);

        if (_state.SelectedEntityIdx >= 0 && _state.Entities != null &&
            _state.SelectedEntityIdx < _state.Entities.Length)
        {
            ImGui.Separator();
            DrawEntityForm(selectedPlugin.PluginId);
        }
    }

    // ─── Plugin Selector ──────────────────────────────────────────

    private void DrawPluginSelector()
    {
        if (_state.Plugins!.Length == 1)
        {
            ImGui.TextColored(LabelColor, _state.Plugins[0].PluginName);
            _state.SelectedPluginIdx = 0;
            return;
        }

        for (int i = 0; i < _state.Plugins.Length; i++)
        {
            var p = _state.Plugins[i];
            bool selected = i == _state.SelectedPluginIdx;
            if (ImGui.Selectable($"{p.PluginName} ({p.EntityLabel})##plugin{i}", selected))
            {
                _state.SelectedPluginIdx = i;
                _state.SelectedEntityIdx = -1;
                _state.Entities = null;
                _state.LoadedEntityId = null;
            }
        }
    }

    // ─── Entity List ──────────────────────────────────────────────

    private void DrawEntityList(string pluginId)
    {
        if (_state.Entities == null && !_state.EntitiesLoading)
            _ = Task.Run(() => _state.LoadEntitiesAsync(pluginId));

        ImGui.TextColored(LabelColor, _state.Schema!.EntityLabel);
        ImGui.SameLine();
        if (ImGui.SmallButton("Refresh##entities"))
            _ = Task.Run(() => _state.LoadEntitiesAsync(pluginId));

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

        float listHeight = Math.Min(_state.Entities.Length * 22 + 8, 160);
        if (ImGui.BeginChild("EntityList", new System.Numerics.Vector2(0, listHeight)))
        {
            var searchLower = _entitySearch.ToLowerInvariant();
            for (int i = 0; i < _state.Entities.Length; i++)
            {
                var e = _state.Entities[i];
                if (!string.IsNullOrEmpty(_entitySearch) &&
                    !e.Label.Contains(searchLower, StringComparison.OrdinalIgnoreCase) &&
                    !(e.Group?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;

                bool selected = i == _state.SelectedEntityIdx;
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

    // ─── Dynamic Form ─────────────────────────────────────────────

    private void DrawEntityForm(string pluginId)
    {
        if (_state.ValuesLoading)
        {
            ImGui.TextColored(DimColor, "Loading values...");
            return;
        }

        if (_state.LoadedEntityId == null || _state.Schema == null)
            return;

        // Sort groups by order
        var groups = _state.Schema.Groups.OrderBy(g => g.Order).ToArray();

        if (ImGui.BeginChild("EntityForm", new System.Numerics.Vector2(0, -30)))
        {
            foreach (var group in groups)
            {
                if (group.Fields.Length == 0) continue;

                if (ImGui.CollapsingHeader(group.Label, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach (var field in group.Fields)
                    {
                        DrawField(field);
                    }
                }
            }
        }
        ImGui.EndChild();

        // Save bar
        DrawSaveBar(pluginId);
    }

    private void DrawField(FieldDefinitionDto field)
    {
        if (!_state.EditedValues.TryGetValue(field.Id, out var currentVal))
            currentVal = "";

        bool isDirty = _state.DirtyFields.Contains(field.Id);
        if (isDirty)
            ImGui.PushStyleColor(ImGuiCol.Text, DirtyColor);

        bool readOnly = field.ReadOnly;
        if (readOnly)
            ImGui.BeginDisabled();

        ImGui.SetNextItemWidth(-1);

        switch (field.Type)
        {
            case "string":
                DrawStringField(field.Id, field.Label, currentVal);
                break;
            case "int":
                DrawIntField(field.Id, field.Label, currentVal, field.Min, field.Max);
                break;
            case "float":
                DrawFloatField(field.Id, field.Label, currentVal, field.Min, field.Max);
                break;
            case "bool":
                DrawBoolField(field.Id, field.Label, currentVal);
                break;
            case "enum":
                DrawEnumField(field.Id, field.Label, currentVal, field.EnumValues ?? []);
                break;
            case "vector3d":
            case "vector3f":
                DrawVectorField(field.Id, field.Label, currentVal);
                break;
            default:
                ImGui.Text($"{field.Label}: {currentVal}");
                break;
        }

        if (readOnly)
            ImGui.EndDisabled();
        if (isDirty)
            ImGui.PopStyleColor();
    }

    private void DrawStringField(string id, string label, string val)
    {
        if (!_state.TextBuffers.ContainsKey(id))
            _state.TextBuffers[id] = val;

        var buf = _state.TextBuffers[id];
        if (ImGui.InputText($"{label}##{id}", ref buf, 256))
        {
            _state.TextBuffers[id] = buf;
            _state.EditedValues[id] = buf;
            _state.UpdateDirty(id);
        }
    }

    private void DrawIntField(string id, string label, string val, float? min, float? max)
    {
        int v = int.TryParse(val, out var parsed) ? parsed : 0;

        bool changed;
        if (min.HasValue && max.HasValue)
            changed = ImGui.SliderInt($"{label}##{id}", ref v, (int)min.Value, (int)max.Value);
        else
            changed = ImGui.InputInt($"{label}##{id}", ref v);

        if (changed)
        {
            _state.EditedValues[id] = v.ToString();
            _state.UpdateDirty(id);
        }
    }

    private void DrawFloatField(string id, string label, string val, float? min, float? max)
    {
        float v = float.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 0f;

        bool changed;
        if (min.HasValue && max.HasValue)
            changed = ImGui.SliderFloat($"{label}##{id}", ref v, min.Value, max.Value, "%.2f");
        else
            changed = ImGui.InputFloat($"{label}##{id}", ref v, 0.1f, 1.0f, "%.2f");

        if (changed)
        {
            _state.EditedValues[id] = v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            _state.UpdateDirty(id);
        }
    }

    private void DrawBoolField(string id, string label, string val)
    {
        bool v = val.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (ImGui.Checkbox($"{label}##{id}", ref v))
        {
            _state.EditedValues[id] = v ? "true" : "false";
            _state.UpdateDirty(id);
        }
    }

    private void DrawEnumField(string id, string label, string val, string[] options)
    {
        int selected = Array.IndexOf(options, val);
        if (selected < 0) selected = 0;

        if (ImGui.Combo($"{label}##{id}", ref selected, options, options.Length))
        {
            _state.EditedValues[id] = options[selected];
            _state.UpdateDirty(id);
        }
    }

    private void DrawVectorField(string id, string label, string val)
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
        catch { /* ignore parse errors */ }

        ImGui.Text(label);
        ImGui.TextColored(DimColor, $"  X: {x:F2}  Y: {y:F2}  Z: {z:F2}");
    }

    private void DrawSaveBar(string pluginId)
    {
        int dirtyCount = _state.DirtyFields.Count;

        if (dirtyCount > 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, SaveColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SaveColor with { W = 0.85f });
            if (ImGui.Button($"Save ({dirtyCount} changes)", new System.Numerics.Vector2(-1, 0)) && !_state.Saving)
                _ = Task.Run(() => _state.SaveChangesAsync(pluginId));
            ImGui.PopStyleColor(2);
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.Button("No changes", new System.Numerics.Vector2(-1, 0));
            ImGui.EndDisabled();
        }

        if (_state.SaveStatus != null)
        {
            ImGui.TextColored(_state.SaveStatus.StartsWith("Error") ? AccentColor : SaveColor, _state.SaveStatus);
        }
    }
}
