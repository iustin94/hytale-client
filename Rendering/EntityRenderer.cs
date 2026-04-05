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

    // Hover highlight colors
    private static readonly uint HoverBorderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.95f, 0.85f, 0.30f, 0.9f));
    private static readonly uint HoverFillColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.95f, 0.85f, 0.30f, 0.15f));
    private static readonly uint TooltipBgColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.10f, 0.10f, 0.15f, 0.92f));
    private static readonly uint TooltipBorderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.95f, 0.85f, 0.30f, 0.6f));
    private static readonly uint TooltipTextColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.90f, 0.90f, 0.95f, 1f));
    private static readonly uint TooltipDimColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.60f, 0.60f, 0.68f, 1f));

    private EntityDataService? _lastData;
    private Dictionary<string, IPluginMapPresenter> _presenters = new();
    private Services.SelectionService? _selection;

    // Hover state
    private string? _hoveredTooltipText;
    private System.Numerics.Vector2 _tooltipPos;

    public EntityRenderer(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    public void SetSelectionService(Services.SelectionService selection)
    {
        _selection = selection;
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

        var mousePos = ImGui.GetIO().MousePos;
        _hoveredTooltipText = null;

        // Sound zones
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

            // Hover detection
            float distToMouse = System.Numerics.Vector2.Distance(mousePos, c);
            bool isHovered = distToMouse <= radiusScreen;

            drawList.AddCircleFilled(c, radiusScreen, isHovered ? HoverFillColor : ZoneColor, 32);
            drawList.AddCircle(c, radiusScreen, isHovered ? HoverBorderColor : ZoneBorderColor, 32, isHovered ? 2.5f : 1f);

            var displayName = z.Sound ?? "Zone";
            var lastSlash = displayName.LastIndexOf('/');
            if (lastSlash >= 0) displayName = displayName[(lastSlash + 1)..];
            drawList.AddText(new System.Numerics.Vector2(c.X + 2, c.Y - radiusScreen - 14),
                isHovered ? HoverBorderColor : NpcLabelColor, displayName);

            if (isHovered)
            {
                float w = z.MaxX - z.MinX;
                float h = z.MaxZ - z.MinZ;
                _hoveredTooltipText = $"Sound Zone: {displayName}\n" +
                    $"Center: ({cx:F1}, {z.Y:F1}, {cz:F1})\n" +
                    $"Size: {w:F0} x {h:F0} blocks\n" +
                    $"Interval: {z.Interval}s";
                _tooltipPos = mousePos;
            }
        }

        // Plugin entities
        if (_lastData.PluginEntities.Length > 0)
        {
            foreach (var (pluginId, presenter) in _presenters)
            {
                presenter.Draw(drawList, _mapRenderer, _lastData.PluginEntities);
            }
        }

        // Players
        foreach (var p in _lastData.Players)
        {
            var screen = _mapRenderer.WorldToScreen(p.X, p.Z);
            if (screen == null) continue;
            var pos = new System.Numerics.Vector2(screen.Value.X, screen.Value.Y);
            bool isHovered = System.Numerics.Vector2.Distance(mousePos, pos) <= 10f;

            drawList.AddCircleFilled(pos, isHovered ? 7f : 5f, PlayerColor);
            drawList.AddText(new System.Numerics.Vector2(pos.X - 20, pos.Y - 16),
                isHovered ? HoverBorderColor : LabelColor, p.Name ?? "Player");

            if (isHovered)
            {
                drawList.AddCircle(pos, 9f, HoverBorderColor, 0, 2f);
                _hoveredTooltipText = $"Player: {p.Name}\n" +
                    $"Position: ({p.X:F1}, {p.Y:F1}, {p.Z:F1})\n" +
                    $"UUID: {p.Uuid}";
                _tooltipPos = mousePos;
            }
        }

        // NPCs
        foreach (var n in _lastData.Entities)
        {
            var screen = _mapRenderer.WorldToScreen(n.X, n.Z);
            if (screen == null) continue;
            var pos = new System.Numerics.Vector2(screen.Value.X, screen.Value.Y);
            bool isHovered = System.Numerics.Vector2.Distance(mousePos, pos) <= 10f;

            drawList.AddCircleFilled(pos, isHovered ? 6f : 4f, NpcColor);
            drawList.AddText(new System.Numerics.Vector2(pos.X - 20, pos.Y - 14),
                isHovered ? HoverBorderColor : NpcLabelColor, GetEntityDisplayName(n));

            if (isHovered)
            {
                drawList.AddCircle(pos, 8f, HoverBorderColor, 0, 2f);
                _hoveredTooltipText = $"NPC: {GetEntityDisplayName(n)}\n" +
                    $"Type: {n.Type}\n" +
                    $"Position: ({n.X:F1}, {n.Y:F1}, {n.Z:F1})\n" +
                    $"UUID: {n.Uuid}";
                _tooltipPos = mousePos;
            }
        }

        // Location markers
        foreach (var e in _lastData.PluginEntities)
        {
            if (!e.Id.StartsWith("loc:")) continue;
            if (e.X == 0 && e.Z == 0) continue;

            var screen = _mapRenderer.WorldToScreen(e.X, e.Z);
            if (screen == null) continue;
            var pos = new System.Numerics.Vector2(screen.Value.X, screen.Value.Y);
            bool isHovered = System.Numerics.Vector2.Distance(mousePos, pos) <= 12f;

            float size = isHovered ? 8f : 6f;
            uint fillColor = isHovered ? HoverFillColor : LocationFillColor;
            uint borderColor = isHovered ? HoverBorderColor : LocationBorderColor;

            drawList.AddQuadFilled(
                new(pos.X, pos.Y - size), new(pos.X + size, pos.Y),
                new(pos.X, pos.Y + size), new(pos.X - size, pos.Y), fillColor);
            drawList.AddQuad(
                new(pos.X, pos.Y - size), new(pos.X + size, pos.Y),
                new(pos.X, pos.Y + size), new(pos.X - size, pos.Y),
                borderColor, isHovered ? 2.5f : 1f);

            var label = e.Label ?? e.Id.Replace("loc:", "");
            drawList.AddText(new System.Numerics.Vector2(pos.X + 8, pos.Y - 7),
                isHovered ? HoverBorderColor : LocationLabelColor, label);

            if (isHovered)
            {
                _hoveredTooltipText = $"Location: {label}\n" +
                    $"Position: ({e.X:F1}, {e.Y:F1}, {e.Z:F1})";
                _tooltipPos = mousePos;
            }
        }

        // Draw tooltip overlay
        if (_hoveredTooltipText != null)
        {
            DrawTooltip(drawList, _tooltipPos, _hoveredTooltipText);
        }

        drawList.PopClipRect();
    }

    private void DrawTooltip(ImDrawListPtr drawList, System.Numerics.Vector2 pos, string text)
    {
        var lines = text.Split('\n');
        float lineH = 15f;
        float padX = 10f, padY = 6f;

        // Measure width
        float maxW = 0;
        foreach (var line in lines)
        {
            var size = ImGui.CalcTextSize(line);
            if (size.X > maxW) maxW = size.X;
        }

        float tooltipW = maxW + padX * 2;
        float tooltipH = lines.Length * lineH + padY * 2;

        // Position below cursor, offset right
        var tooltipPos = new System.Numerics.Vector2(pos.X + 16, pos.Y + 16);
        var tooltipMax = tooltipPos + new System.Numerics.Vector2(tooltipW, tooltipH);

        drawList.AddRectFilled(tooltipPos, tooltipMax, TooltipBgColor, 4f);
        drawList.AddRect(tooltipPos, tooltipMax, TooltipBorderColor, 4f);

        float y = tooltipPos.Y + padY;
        for (int i = 0; i < lines.Length; i++)
        {
            uint color = i == 0 ? TooltipTextColor : TooltipDimColor;
            drawList.AddText(new System.Numerics.Vector2(tooltipPos.X + padX, y), color, lines[i]);
            y += lineH;
        }
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
        return bracket > 0 ? type[..bracket] : type;
    }
}
