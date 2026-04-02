using Hexa.NET.ImGui;
using Stride.Core.Mathematics;

namespace HytaleAdmin.Rendering;

public class SelectionRenderer
{
    private readonly MapRenderer _mapRenderer;

    private static readonly uint HoverColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.91f, 0.27f, 0.38f, 0.4f));
    private static readonly uint AreaColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.31f, 0.80f, 0.77f, 0.3f));
    private static readonly uint AreaBorderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.31f, 0.80f, 0.77f, 0.7f));

    private Vector2? _hoverWorld;
    private Vector2? _areaStart;
    private Vector2? _areaEnd;

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

        // Hover highlight
        if (_hoverWorld != null)
        {
            float snappedX = MathF.Floor(_hoverWorld.Value.X);
            float snappedZ = MathF.Floor(_hoverWorld.Value.Y);

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

    public void Clear()
    {
        _hoverWorld = null;
        _areaStart = null;
        _areaEnd = null;
    }
}
