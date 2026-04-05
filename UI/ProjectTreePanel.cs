using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Rendering;

namespace HytaleAdmin.UI;

public class ProjectTreePanel
{
    private readonly ServiceContainer _services;
    private readonly MapRenderer _mapRenderer;
    private string _filter = "";

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 NpcColor = new(0.23f, 0.53f, 1f, 1f);
    private static readonly System.Numerics.Vector4 ZoneColor = new(0.31f, 0.80f, 0.77f, 1f);
    private static readonly System.Numerics.Vector4 PluginColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);

    public ProjectTreePanel(ServiceContainer services, MapRenderer mapRenderer)
    {
        _services = services;
        _mapRenderer = mapRenderer;
    }

    public void Draw()
    {
        ImGui.TextColored(AccentColor, "World Objects");
        ImGui.Separator();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##treeFilter", ref _filter, 128);

        if (ImGui.BeginChild("ProjectTree"))
        {
            DrawPlayers();
            DrawEntities();
            DrawSoundZones();
            DrawPluginEntities();
            ImGui.EndChild();
        }
    }

    private void DrawPlayers()
    {
        var players = _services.EntityData.Players;
        if (players.Length == 0) return;

        if (ImGui.TreeNodeEx($"Players ({players.Length})", ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach (var p in players)
            {
                var name = p.Name ?? "Player";
                if (!MatchesFilter(name)) continue;

                bool selected = _services.Selection.SelectedPlayer?.Uuid == p.Uuid;
                if (ImGui.Selectable($"{name}##p_{p.Uuid}", selected))
                {
                    _services.Selection.SelectPlayer(p);
                    _mapRenderer.LookAt(p.X, p.Z);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"({p.X:F0}, {p.Z:F0})");
            }
            ImGui.TreePop();
        }
    }

    private void DrawEntities()
    {
        var entities = _services.EntityData.Entities;
        if (entities.Length == 0) return;

        if (ImGui.TreeNodeEx($"NPCs ({entities.Length})"))
        {
            foreach (var e in entities)
            {
                var name = GetEntityName(e);
                if (!MatchesFilter(name)) continue;

                bool selected = _services.Selection.SelectedEntity?.Uuid == e.Uuid;
                ImGui.PushStyleColor(ImGuiCol.Text, NpcColor);
                if (ImGui.Selectable($"{name}##e_{e.Uuid}", selected))
                {
                    _services.Selection.SelectEntity(e);
                    _mapRenderer.LookAt(e.X, e.Z);
                }
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"{e.Type}\n({e.X:F0}, {e.Z:F0})");
            }
            ImGui.TreePop();
        }
    }

    private void DrawSoundZones()
    {
        var zones = _services.EntityData.SoundZones;
        if (zones.Length == 0) return;

        if (ImGui.TreeNodeEx($"Sound Zones ({zones.Length})"))
        {
            foreach (var z in zones)
            {
                var name = z.Sound ?? z.Key ?? "Zone";
                var lastSlash = name.LastIndexOf('/');
                if (lastSlash >= 0) name = name[(lastSlash + 1)..];
                if (!MatchesFilter(name)) continue;

                float cx = (z.MinX + z.MaxX) / 2f;
                float cz = (z.MinZ + z.MaxZ) / 2f;

                bool selected = _services.Selection.SelectedZone?.Key == z.Key;
                ImGui.PushStyleColor(ImGuiCol.Text, ZoneColor);
                if (ImGui.Selectable($"{name}##z_{z.Key}", selected))
                {
                    _services.Selection.SelectZone(z);
                    _mapRenderer.LookAt(cx, cz);
                }
                ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"({cx:F0}, {cz:F0})");
            }
            ImGui.TreePop();
        }
    }

    private void DrawPluginEntities()
    {
        var entities = _services.EntityData.PluginEntities;
        if (entities.Length == 0) return;

        // Group by plugin (Group field often holds world/plugin context)
        var groups = entities
            .Where(e => e.Enabled)
            .GroupBy(e => e.Group ?? "Plugin");

        foreach (var group in groups)
        {
            if (ImGui.TreeNodeEx($"{group.Key} ({group.Count()})"))
            {
                foreach (var pe in group)
                {
                    if (!MatchesFilter(pe.Label)) continue;

                    float cx = pe.X;
                    float cz = pe.Z;
                    if (pe.MinX != null && pe.MaxX != null)
                    {
                        cx = (pe.MinX.Value + pe.MaxX.Value) / 2f;
                        cz = (pe.MinZ.Value + pe.MaxZ.Value) / 2f;
                    }

                    ImGui.PushStyleColor(ImGuiCol.Text, PluginColor);
                    if (ImGui.Selectable($"{pe.Label}##pe_{pe.Id}", false))
                    {
                        _mapRenderer.LookAt(cx, cz);
                    }
                    ImGui.PopStyleColor();
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"({cx:F0}, {cz:F0})");
                }
                ImGui.TreePop();
            }
        }
    }

    private bool MatchesFilter(string name)
    {
        if (string.IsNullOrEmpty(_filter)) return true;
        return name.Contains(_filter, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetEntityName(EntityDto entity)
    {
        if (!string.IsNullOrEmpty(entity.Name)) return entity.Name;
        var type = entity.Type ?? "NPC";
        var bracket = type.IndexOf('[');
        if (bracket > 0) type = type[..bracket];
        var dot = type.LastIndexOf('.');
        if (dot >= 0 && dot < type.Length - 1) type = type[(dot + 1)..];
        return type;
    }
}
