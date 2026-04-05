using System.Numerics;
using Hexa.NET.ImGui;

namespace HytaleAdmin.UI.CanvasView.Presenters;

/// <summary>
/// Renders a point entity (player, NPC, location marker) as a colored dot/shape.
/// Configurable color, size, shape, and tooltip format.
/// </summary>
public class PointEntityPresenter : IMapEntityPresenter
{
    private readonly uint _color;
    private readonly uint _labelColor;
    private readonly float _radius;
    private readonly Shape _shape;
    private readonly Func<IMapEntity, string?>? _tooltipBuilder;

    private static readonly uint HoverBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.85f, 0.30f, 0.9f));
    private static readonly uint HoverFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.85f, 0.30f, 0.3f));
    private static readonly uint SelectedFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.75f, 0.20f, 0.6f));
    private static readonly uint SelectedBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.75f, 0.20f, 1f));

    public enum Shape { Circle, Diamond }

    public PointEntityPresenter(uint color, uint labelColor, float radius = 5f,
        Shape shape = Shape.Circle, Func<IMapEntity, string?>? tooltipBuilder = null)
    {
        _color = color;
        _labelColor = labelColor;
        _radius = radius;
        _shape = shape;
        _tooltipBuilder = tooltipBuilder;
    }

    public void DrawNormal(ImDrawListPtr dl, IMapEntity e, Vector2 sp, float zoom)
    {
        DrawShape(dl, sp, _radius, _color, 0, 1f);
        dl.AddText(sp + new Vector2(_radius + 4, -7), _labelColor, e.Label);
    }

    public void DrawHovered(ImDrawListPtr dl, IMapEntity e, Vector2 sp, float zoom)
    {
        DrawShape(dl, sp, _radius * 1.4f, _color, HoverBorder, 2.5f);
        dl.AddText(sp + new Vector2(_radius + 6, -7), HoverBorder, e.Label);
    }

    public void DrawSelected(ImDrawListPtr dl, IMapEntity e, Vector2 sp, float zoom)
    {
        DrawShape(dl, sp, _radius * 1.3f, SelectedFill, SelectedBorder, 2.5f);
        dl.AddText(sp + new Vector2(_radius + 6, -7), SelectedBorder, e.Label);
    }

    public bool HitTest(IMapEntity e, Vector2 sp, Vector2 mouse, float zoom)
    {
        return Vector2.Distance(sp, mouse) <= (_radius + 6f);
    }

    public string? GetTooltip(IMapEntity e) => _tooltipBuilder?.Invoke(e);

    private void DrawShape(ImDrawListPtr dl, Vector2 sp, float r, uint fill, uint border, float borderW)
    {
        if (_shape == Shape.Circle)
        {
            dl.AddCircleFilled(sp, r, fill);
            if (border != 0) dl.AddCircle(sp, r + 2f, border, 0, borderW);
        }
        else // Diamond
        {
            dl.AddQuadFilled(
                sp + new Vector2(0, -r), sp + new Vector2(r, 0),
                sp + new Vector2(0, r), sp + new Vector2(-r, 0), fill);
            if (border != 0)
                dl.AddQuad(
                    sp + new Vector2(0, -r), sp + new Vector2(r, 0),
                    sp + new Vector2(0, r), sp + new Vector2(-r, 0), border, borderW);
        }
    }
}
