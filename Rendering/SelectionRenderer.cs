using Hexa.NET.ImGui;
using HytaleAdmin.Models.Domain;
using Stride.Core.Mathematics;

namespace HytaleAdmin.Rendering;

public class SelectionRenderer
{
    private readonly MapRenderer _mapRenderer;

    private static readonly uint HoverColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.91f, 0.27f, 0.38f, 0.4f));
    private static readonly uint FootprintColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.2f, 0.6f, 1.0f, 0.35f));
    private static readonly uint FootprintBorderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.2f, 0.6f, 1.0f, 0.7f));
    private static readonly uint ArrowColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1f, 1f, 0.2f, 0.9f));
    private static readonly uint AreaColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.31f, 0.80f, 0.77f, 0.3f));
    private static readonly uint AreaBorderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.31f, 0.80f, 0.77f, 0.7f));

    private Vector2? _hoverWorld;
    private Vector2? _areaStart;
    private Vector2? _areaEnd;
    private SelectedAsset? _selectedAsset;

    public SelectionRenderer(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    public void UpdateHoverHighlight(Vector2? worldPos)
    {
        _hoverWorld = worldPos;
    }

    public void HideHover()
    {
        _hoverWorld = null;
    }

    public void UpdateSelectedAsset(SelectedAsset? asset)
    {
        _selectedAsset = asset;
    }

    public void UpdateAreaSelection(Vector2 startWorld, Vector2 endWorld)
    {
        _areaStart = startWorld;
        _areaEnd = endWorld;
    }

    public void HideAreaSelection()
    {
        _areaStart = null;
        _areaEnd = null;
    }

    public void DrawOverlays(ImDrawListPtr drawList)
    {
        var winPos = _mapRenderer.WindowPos;
        var winSize = _mapRenderer.WindowSize;
        drawList.PushClipRect(winPos, new System.Numerics.Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y));

        // Hover highlight — shows asset footprint if selected
        if (_hoverWorld != null)
        {
            float snappedX = MathF.Floor(_hoverWorld.Value.X);
            float snappedZ = MathF.Floor(_hoverWorld.Value.Y);

            int sizeX = _selectedAsset?.SizeX ?? 1;
            int sizeZ = _selectedAsset?.SizeZ ?? 1;

            if (sizeX > 1 || sizeZ > 1)
            {
                float halfX = sizeX / 2f;
                float halfZ = sizeZ / 2f;
                var topLeft = _mapRenderer.WorldToScreen(snappedX - halfX + 0.5f, snappedZ - halfZ + 0.5f);
                var bottomRight = _mapRenderer.WorldToScreen(snappedX + halfX + 0.5f, snappedZ + halfZ + 0.5f);
                if (topLeft != null && bottomRight != null)
                {
                    var tl = new System.Numerics.Vector2(topLeft.Value.X, topLeft.Value.Y);
                    var br = new System.Numerics.Vector2(bottomRight.Value.X, bottomRight.Value.Y);
                    drawList.AddRectFilled(tl, br, FootprintColor);
                    drawList.AddRect(tl, br, FootprintBorderColor);

                    // Arrow showing rotation direction
                    DrawRotationArrow(drawList, tl, br, _selectedAsset?.Rotation ?? 0);
                }
            }
            else
            {
                // Default 1x1 block highlight
                var topLeft = _mapRenderer.WorldToScreen(snappedX, snappedZ);
                var bottomRight = _mapRenderer.WorldToScreen(snappedX + 1, snappedZ + 1);
                if (topLeft != null && bottomRight != null)
                {
                    drawList.AddRectFilled(
                        new System.Numerics.Vector2(topLeft.Value.X, topLeft.Value.Y),
                        new System.Numerics.Vector2(bottomRight.Value.X, bottomRight.Value.Y),
                        HoverColor);
                }
            }
        }

        // Area selection
        if (_areaStart != null && _areaEnd != null)
        {
            float minX = Math.Min(_areaStart.Value.X, _areaEnd.Value.X);
            float maxX = Math.Max(_areaStart.Value.X, _areaEnd.Value.X);
            float minZ = Math.Min(_areaStart.Value.Y, _areaEnd.Value.Y);
            float maxZ = Math.Max(_areaStart.Value.Y, _areaEnd.Value.Y);

            var tl = _mapRenderer.WorldToScreen(minX, minZ);
            var br = _mapRenderer.WorldToScreen(maxX, maxZ);
            if (tl != null && br != null)
            {
                drawList.AddRectFilled(
                    new System.Numerics.Vector2(tl.Value.X, tl.Value.Y),
                    new System.Numerics.Vector2(br.Value.X, br.Value.Y),
                    AreaColor);
                drawList.AddRect(
                    new System.Numerics.Vector2(tl.Value.X, tl.Value.Y),
                    new System.Numerics.Vector2(br.Value.X, br.Value.Y),
                    AreaBorderColor);
            }
        }

        drawList.PopClipRect();
    }

    private static void DrawRotationArrow(ImDrawListPtr drawList,
        System.Numerics.Vector2 tl, System.Numerics.Vector2 br, int rotation)
    {
        float cx = (tl.X + br.X) / 2f;
        float cy = (tl.Y + br.Y) / 2f;
        float halfW = (br.X - tl.X) / 2f;
        float halfH = (br.Y - tl.Y) / 2f;
        float arrowLen = MathF.Min(halfW, halfH) * 0.7f;
        float headSize = MathF.Max(arrowLen * 0.3f, 4f);

        // Direction vector (screen space: Y increases downward)
        // 0=North(up), 90=East(right), 180=South(down), 270=West(left)
        float rad = rotation * MathF.PI / 180f;
        float dx = MathF.Sin(rad);
        float dy = -MathF.Cos(rad); // negative because screen Y is flipped

        // Arrow line from center to edge
        var from = new System.Numerics.Vector2(cx - dx * arrowLen * 0.3f, cy - dy * arrowLen * 0.3f);
        var to = new System.Numerics.Vector2(cx + dx * arrowLen, cy + dy * arrowLen);
        drawList.AddLine(from, to, ArrowColor, 2.5f);

        // Arrowhead triangle
        float perpX = -dy;
        float perpY = dx;
        var tip = to;
        var left = new System.Numerics.Vector2(to.X - dx * headSize + perpX * headSize * 0.5f,
                                                to.Y - dy * headSize + perpY * headSize * 0.5f);
        var right = new System.Numerics.Vector2(to.X - dx * headSize - perpX * headSize * 0.5f,
                                                 to.Y - dy * headSize - perpY * headSize * 0.5f);
        drawList.AddTriangleFilled(tip, left, right, ArrowColor);
    }

    public void Clear()
    {
        _hoverWorld = null;
        _areaStart = null;
        _areaEnd = null;
        _selectedAsset = null;
    }
}
