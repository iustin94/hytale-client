using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Rendering;

namespace HytaleAdmin.UI.CanvasView.Presenters;

/// <summary>
/// Renders an area entity (sound zone, region) as a circle or rectangle.
/// Shows bounding area with fill + border, resizes with zoom.
/// </summary>
public class AreaEntityPresenter : IMapEntityPresenter
{
    private readonly uint _fillColor;
    private readonly uint _borderColor;
    private readonly uint _labelColor;
    private readonly Func<IMapEntity, string?>? _tooltipBuilder;

    private static readonly uint HoverBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.85f, 0.30f, 0.9f));
    private static readonly uint HoverFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.85f, 0.30f, 0.15f));
    private static readonly uint SelectedBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.75f, 0.20f, 1f));
    private static readonly uint SelectedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.75f, 0.20f, 0.20f));

    public AreaEntityPresenter(uint fillColor, uint borderColor, uint labelColor,
        Func<IMapEntity, string?>? tooltipBuilder = null)
    {
        _fillColor = fillColor;
        _borderColor = borderColor;
        _labelColor = labelColor;
        _tooltipBuilder = tooltipBuilder;
    }

    public void DrawNormal(ImDrawListPtr dl, IMapEntity e, Vector2 sp, float zoom)
    {
        DrawArea(dl, e, sp, zoom, _fillColor, _borderColor, 1f);
        dl.AddText(sp + new Vector2(4, -14), _labelColor, e.Label);
    }

    public void DrawHovered(ImDrawListPtr dl, IMapEntity e, Vector2 sp, float zoom)
    {
        DrawArea(dl, e, sp, zoom, HoverFill, HoverBorder, 2.5f);
        dl.AddText(sp + new Vector2(4, -14), HoverBorder, e.Label);
    }

    public void DrawSelected(ImDrawListPtr dl, IMapEntity e, Vector2 sp, float zoom)
    {
        DrawArea(dl, e, sp, zoom, SelectedFill, SelectedBorder, 2.5f);
        dl.AddText(sp + new Vector2(4, -14), SelectedBorder, e.Label);
    }

    public bool HitTest(IMapEntity e, Vector2 sp, Vector2 mouse, float zoom)
    {
        if (e is IMapAreaEntity area)
        {
            float halfW = (area.MaxX - area.MinX) / 2f * zoom;
            float halfH = (area.MaxZ - area.MinZ) / 2f * zoom;
            return mouse.X >= sp.X - halfW && mouse.X <= sp.X + halfW &&
                   mouse.Y >= sp.Y - halfH && mouse.Y <= sp.Y + halfH;
        }
        return Vector2.Distance(sp, mouse) <= 20f;
    }

    public string? GetTooltip(IMapEntity e) => _tooltipBuilder?.Invoke(e);

    private void DrawArea(ImDrawListPtr dl, IMapEntity e, Vector2 sp, float zoom,
        uint fill, uint border, float borderW)
    {
        if (e is IMapAreaEntity area)
        {
            float halfW = (area.MaxX - area.MinX) / 2f * zoom;
            float halfH = (area.MaxZ - area.MinZ) / 2f * zoom;
            var min = sp - new Vector2(halfW, halfH);
            var max = sp + new Vector2(halfW, halfH);

            // Circle inscribed in the area
            float radius = Math.Max(halfW, halfH);
            dl.AddCircleFilled(sp, radius, fill, 32);
            dl.AddCircle(sp, radius, border, 32, borderW);
        }
        else
        {
            dl.AddCircleFilled(sp, 10f, fill, 32);
            dl.AddCircle(sp, 10f, border, 32, borderW);
        }
    }
}
