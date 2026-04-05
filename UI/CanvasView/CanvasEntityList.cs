using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Rendering;

namespace HytaleAdmin.UI.CanvasView;

/// <summary>
/// Left-sidebar list of entities currently on the canvas/map.
/// Filterable by type and searchable by name.
/// Click → selects on canvas + pans map to entity.
/// </summary>
public class CanvasEntityList
{
    private readonly CanvasView _canvasView;
    private readonly MapRenderer _mapRenderer;

    private string _search = "";
    private int _typeFilter; // 0=All, 1=Players, 2=NPCs, 3=Zones, 4=Locations
    private static readonly string[] TypeFilters = ["All", "Players", "NPCs", "Sound Zones", "Locations"];

    private static readonly Vector4 DimColor = new(0.55f, 0.55f, 0.63f, 1f);
    private static readonly Vector4 SelectedColor = new(0.95f, 0.75f, 0.20f, 1f);

    private static readonly Dictionary<string, Vector4> TypeColors = new()
    {
        ["player"] = new(0.91f, 0.27f, 0.38f, 1f),
        ["npc"] = new(0.23f, 0.53f, 1f, 1f),
        ["soundzone"] = new(0.31f, 0.80f, 0.77f, 1f),
        ["location"] = new(0.36f, 0.55f, 0.85f, 1f),
    };

    public CanvasEntityList(CanvasView canvasView, MapRenderer mapRenderer)
    {
        _canvasView = canvasView;
        _mapRenderer = mapRenderer;
    }

    public void Draw(float width, float height)
    {
        // Type filter
        ImGui.SetNextItemWidth(width - 16);
        ImGui.Combo("##entityTypeFilter", ref _typeFilter, TypeFilters, TypeFilters.Length);

        // Search
        ImGui.SetNextItemWidth(width - 16);
        ImGui.InputText("##entitySearch", ref _search, 128);

        ImGui.Separator();

        // Entity count
        var entities = _canvasView.Entities;
        var filtered = FilterEntities(entities);
        ImGui.TextColored(DimColor, $"{filtered.Count} entities");

        // Scrollable list
        if (ImGui.BeginChild("EntityListScroll"))
        {
            foreach (var entity in filtered)
            {
                bool isSelected = _canvasView.Selection.IsSelected(entity.Id);
                var typeColor = TypeColors.GetValueOrDefault(entity.EntityType, DimColor);

                if (isSelected) ImGui.PushStyleColor(ImGuiCol.Text, SelectedColor);
                else ImGui.PushStyleColor(ImGuiCol.Text, typeColor);

                string prefix = entity.EntityType switch
                {
                    "player" => "[P]",
                    "npc" => "[N]",
                    "soundzone" => "[S]",
                    "location" => "[L]",
                    _ => "[?]",
                };

                if (ImGui.Selectable($"{prefix} {entity.Label}##{entity.Id}", isSelected))
                {
                    _canvasView.Selection.Select(entity.Id);
                    _canvasView.OnEntitySelected?.Invoke(entity);
                    _mapRenderer.LookAt(entity.WorldX, entity.WorldZ);
                }

                ImGui.PopStyleColor();

                // Tooltip on hover
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.TextColored(typeColor, entity.Label);
                    ImGui.TextColored(DimColor, $"({entity.WorldX:F0}, {entity.WorldY:F0}, {entity.WorldZ:F0})");
                    ImGui.EndTooltip();
                }
            }
        }
        ImGui.EndChild();
    }

    private List<IMapEntity> FilterEntities(IReadOnlyList<IMapEntity> entities)
    {
        var result = new List<IMapEntity>();
        string typeMatch = _typeFilter switch
        {
            1 => "player",
            2 => "npc",
            3 => "soundzone",
            4 => "location",
            _ => "",
        };

        var searchLower = _search.ToLowerInvariant();

        foreach (var e in entities)
        {
            if (!string.IsNullOrEmpty(typeMatch) && e.EntityType != typeMatch) continue;
            if (!string.IsNullOrEmpty(_search) &&
                !e.Label.Contains(searchLower, StringComparison.OrdinalIgnoreCase) &&
                !e.Id.Contains(searchLower, StringComparison.OrdinalIgnoreCase))
                continue;
            result.Add(e);
        }

        return result;
    }
}
