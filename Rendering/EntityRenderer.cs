using Hexa.NET.ImGui;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;

namespace HytaleAdmin.Rendering;

public class EntityRenderer
{
    private readonly MapRenderer _mapRenderer;

    private static readonly uint PlayerColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.91f, 0.27f, 0.38f, 1f));
    private static readonly uint NpcColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.23f, 0.53f, 1f, 1f));
    private static readonly uint ZoneColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.31f, 0.80f, 0.77f, 0.25f));
    private static readonly uint ZoneBorderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.31f, 0.80f, 0.77f, 0.6f));
    private static readonly uint LabelColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1f, 1f, 1f, 0.9f));
    private static readonly uint NpcLabelColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.23f, 0.53f, 1f, 0.9f));
    private static readonly uint LocationFillColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.36f, 0.55f, 0.85f, 0.5f));
    private static readonly uint LocationBorderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.36f, 0.55f, 0.85f, 0.9f));
    private static readonly uint LocationLabelColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.50f, 0.70f, 0.95f, 0.9f));

    private EntityDataService? _lastData;
    private Dictionary<string, IPluginMapPresenter> _presenters = new();

    public EntityRenderer(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    public void SetPresenters(Dictionary<string, IPluginMapPresenter> presenters)
    {
        _presenters = presenters;
    }

    public void Refresh(EntityDataService data)
    {
        _lastData = data;
    }

    public void DrawOverlays(ImDrawListPtr drawList)
    {
        if (_lastData == null) return;

        var winPos = _mapRenderer.WindowPos;
        var winSize = _mapRenderer.WindowSize;
        drawList.PushClipRect(winPos, new System.Numerics.Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y));

        // Sound zones (from hytale-plugin SoundHandler) — circular
        foreach (var z in _lastData.SoundZones)
        {
            float cx = (z.MinX + z.MaxX) / 2f;
            float cz = (z.MinZ + z.MaxZ) / 2f;
            float radiusWorld = (z.MaxX - z.MinX) / 2f;

            var center = _mapRenderer.WorldToScreen(cx, cz);
            var edge = _mapRenderer.WorldToScreen(cx + radiusWorld, cz);
            if (center == null || edge == null) continue;

            float radiusScreen = MathF.Abs(edge.Value.X - center.Value.X);
            var c = new System.Numerics.Vector2(center.Value.X, center.Value.Y);
            drawList.AddCircleFilled(c, radiusScreen, ZoneColor, 32);
            drawList.AddCircle(c, radiusScreen, ZoneBorderColor, 32);

            var displayName = z.Sound ?? "Zone";
            var lastSlash = displayName.LastIndexOf('/');
            if (lastSlash >= 0) displayName = displayName[(lastSlash + 1)..];
            drawList.AddText(new System.Numerics.Vector2(c.X + 2, c.Y - radiusScreen - 14), NpcLabelColor, displayName);
        }

        // Plugin entities — delegate to schema-driven presenters
        if (_lastData.PluginEntities.Length > 0)
        {
            // Group by plugin (using Group field which holds pluginId/worldId)
            // Presenters are keyed by pluginId from schema cache
            foreach (var (pluginId, presenter) in _presenters)
            {
                // All plugin entities are mixed — filter by checking if presenter handles them
                // For now render all with each presenter (presenters skip entities they can't render)
                presenter.Draw(drawList, _mapRenderer, _lastData.PluginEntities);
            }
        }

        // Players
        foreach (var p in _lastData.Players)
        {
            var screen = _mapRenderer.WorldToScreen(p.X, p.Z);
            if (screen == null) continue;
            var pos = new System.Numerics.Vector2(screen.Value.X, screen.Value.Y);
            drawList.AddCircleFilled(pos, 5f, PlayerColor);
            drawList.AddText(new System.Numerics.Vector2(pos.X - 20, pos.Y - 16), LabelColor, p.Name ?? "Player");
        }

        // NPCs
        foreach (var n in _lastData.Entities)
        {
            var screen = _mapRenderer.WorldToScreen(n.X, n.Z);
            if (screen == null) continue;
            var pos = new System.Numerics.Vector2(screen.Value.X, screen.Value.Y);
            drawList.AddCircleFilled(pos, 4f, NpcColor);
            drawList.AddText(new System.Numerics.Vector2(pos.X - 20, pos.Y - 14), NpcLabelColor, GetEntityDisplayName(n));
        }

        // Location markers (from adventure plugin entities with "loc:" prefix)
        foreach (var e in _lastData.PluginEntities)
        {
            if (!e.Id.StartsWith("loc:")) continue;
            if (e.X == 0 && e.Z == 0) continue;

            var screen = _mapRenderer.WorldToScreen(e.X, e.Z);
            if (screen == null) continue;
            var pos = new System.Numerics.Vector2(screen.Value.X, screen.Value.Y);

            // Diamond marker shape
            float size = 6f;
            drawList.AddQuadFilled(
                new(pos.X, pos.Y - size),
                new(pos.X + size, pos.Y),
                new(pos.X, pos.Y + size),
                new(pos.X - size, pos.Y),
                LocationFillColor);
            drawList.AddQuad(
                new(pos.X, pos.Y - size),
                new(pos.X + size, pos.Y),
                new(pos.X, pos.Y + size),
                new(pos.X - size, pos.Y),
                LocationBorderColor);

            var label = e.Label ?? e.Id.Replace("loc:", "");
            drawList.AddText(new System.Numerics.Vector2(pos.X + 8, pos.Y - 7), LocationLabelColor, label);
        }

        drawList.PopClipRect();
    }

    public void Clear()
    {
        _lastData = null;
    }

    private static string GetEntityDisplayName(EntityDto entity)
    {
        if (!string.IsNullOrEmpty(entity.Name))
            return entity.Name;

        var type = entity.Type ?? "NPC";
        var bracket = type.IndexOf('[');
        if (bracket > 0) type = type[..bracket];
        var dot = type.LastIndexOf('.');
        if (dot >= 0 && dot < type.Length - 1) type = type[(dot + 1)..];
        return type;
    }
}
