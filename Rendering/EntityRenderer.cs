using Hexa.NET.ImGui;
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

    private EntityDataService? _lastData;

    public EntityRenderer(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
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

        // Sound zones (draw first, behind markers)
        foreach (var z in _lastData.SoundZones)
        {
            var topLeft = _mapRenderer.WorldToScreen(z.MinX, z.MinZ);
            var bottomRight = _mapRenderer.WorldToScreen(z.MaxX, z.MaxZ);
            if (topLeft == null || bottomRight == null) continue;

            var tl = new System.Numerics.Vector2(topLeft.Value.X, topLeft.Value.Y);
            var br = new System.Numerics.Vector2(bottomRight.Value.X, bottomRight.Value.Y);
            drawList.AddRectFilled(tl, br, ZoneColor);
            drawList.AddRect(tl, br, ZoneBorderColor);

            var displayName = z.Sound ?? "Zone";
            var lastSlash = displayName.LastIndexOf('/');
            if (lastSlash >= 0) displayName = displayName[(lastSlash + 1)..];
            drawList.AddText(new System.Numerics.Vector2(tl.X + 2, tl.Y + 2), NpcLabelColor, displayName);
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

        drawList.PopClipRect();
    }

    private static string GetEntityDisplayName(Models.Api.EntityDto entity)
    {
        if (!string.IsNullOrEmpty(entity.Name))
            return entity.Name;

        var type = entity.Type ?? "NPC";

        // Strip UUID suffix: "com.hytale.entity.HyCitizen[abc-123]" → "com.hytale.entity.HyCitizen"
        var bracketIdx = type.IndexOf('[');
        if (bracketIdx > 0) type = type[..bracketIdx];

        // Strip namespace: "com.hytale.entity.HyCitizen" → "HyCitizen"
        var dotIdx = type.LastIndexOf('.');
        if (dotIdx >= 0 && dotIdx < type.Length - 1) type = type[(dotIdx + 1)..];

        return type;
    }

    public void Clear()
    {
        _lastData = null;
    }
}
