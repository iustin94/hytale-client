using Hexa.NET.ImGui;
using HytaleAdmin.Models.Api;

namespace HytaleAdmin.Rendering;

public class CircleMapPresenter : IPluginMapPresenter
{
    private readonly uint _fillColor;
    private readonly uint _borderColor;
    private readonly uint _labelColor;
    private readonly bool _showLabel;

    private static readonly uint BoundaryColor = 0xCCFFFFFF; // white, semi-transparent
    private static readonly uint BoundaryFillColor = 0x10FFFFFF; // very faint white fill

    public CircleMapPresenter(MapPresenterDto config)
    {
        _fillColor = ParseColor(config.FillColor, 0x4050d1c5);
        _borderColor = ParseColor(config.BorderColor, 0xb350d1c5);
        _labelColor = ParseColor(config.LabelColor, 0xb3ffffff);
        _showLabel = config.ShowLabel;
    }

    public void Draw(ImDrawListPtr drawList, MapRenderer mapRenderer, PluginEntitySummaryDto[] entities)
    {
        foreach (var e in entities)
        {
            if (!e.Enabled) continue;
            if (e.MinX == null || e.MaxX == null || e.MinZ == null || e.MaxZ == null) continue;

            float cx = (e.MinX.Value + e.MaxX.Value) / 2f;
            float cz = (e.MinZ.Value + e.MaxZ.Value) / 2f;
            float radiusWorld = (e.MaxX.Value - e.MinX.Value) / 2f;

            var center = mapRenderer.WorldToScreen(cx, cz);
            var edge = mapRenderer.WorldToScreen(cx + radiusWorld, cz);
            if (center == null || edge == null) continue;

            float radiusScreen = MathF.Abs(edge.Value.X - center.Value.X);
            var c = new System.Numerics.Vector2(center.Value.X, center.Value.Y);

            // Normal fill + border
            drawList.AddCircleFilled(c, radiusScreen, _fillColor, 32);
            drawList.AddCircle(c, radiusScreen, _borderColor, 32);

            // Boundary cube overlay (top-down square inscribed in circle)
            if (e.ShowBoundary)
            {
                float half = radiusScreen;
                var tl = new System.Numerics.Vector2(c.X - half, c.Y - half);
                var br = new System.Numerics.Vector2(c.X + half, c.Y + half);

                // Faint fill
                drawList.AddRectFilled(tl, br, BoundaryFillColor);

                // Dashed border — draw as segments
                DrawDashedRect(drawList, tl, br, BoundaryColor, 1.5f, 6f, 4f);

                // Corner markers
                float corner = MathF.Min(half * 0.15f, 8f);
                DrawCorner(drawList, tl.X, tl.Y, corner, 1, 1, BoundaryColor);
                DrawCorner(drawList, br.X, tl.Y, corner, -1, 1, BoundaryColor);
                DrawCorner(drawList, tl.X, br.Y, corner, 1, -1, BoundaryColor);
                DrawCorner(drawList, br.X, br.Y, corner, -1, -1, BoundaryColor);
            }

            if (_showLabel)
                drawList.AddText(new System.Numerics.Vector2(c.X + 2, c.Y - radiusScreen - 14),
                    _labelColor, e.Label);
        }
    }

    private static void DrawDashedRect(ImDrawListPtr drawList,
        System.Numerics.Vector2 tl, System.Numerics.Vector2 br,
        uint color, float thickness, float dashLen, float gapLen)
    {
        DrawDashedLine(drawList, tl, new(br.X, tl.Y), color, thickness, dashLen, gapLen);
        DrawDashedLine(drawList, new(br.X, tl.Y), br, color, thickness, dashLen, gapLen);
        DrawDashedLine(drawList, br, new(tl.X, br.Y), color, thickness, dashLen, gapLen);
        DrawDashedLine(drawList, new(tl.X, br.Y), tl, color, thickness, dashLen, gapLen);
    }

    private static void DrawDashedLine(ImDrawListPtr drawList,
        System.Numerics.Vector2 a, System.Numerics.Vector2 b,
        uint color, float thickness, float dashLen, float gapLen)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1f) return;
        float nx = dx / len, ny = dy / len;
        float pos = 0;
        bool drawing = true;
        while (pos < len)
        {
            float seg = drawing ? dashLen : gapLen;
            float end = MathF.Min(pos + seg, len);
            if (drawing)
            {
                drawList.AddLine(
                    new(a.X + nx * pos, a.Y + ny * pos),
                    new(a.X + nx * end, a.Y + ny * end),
                    color, thickness);
            }
            pos = end;
            drawing = !drawing;
        }
    }

    private static void DrawCorner(ImDrawListPtr drawList, float x, float y,
        float size, int dirX, int dirY, uint color)
    {
        drawList.AddLine(new(x, y), new(x + size * dirX, y), color, 2f);
        drawList.AddLine(new(x, y), new(x, y + size * dirY), color, 2f);
    }

    private static uint ParseColor(string? hex, uint fallback)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length != 8) return fallback;
        try { return Convert.ToUInt32(hex, 16); }
        catch { return fallback; }
    }
}
