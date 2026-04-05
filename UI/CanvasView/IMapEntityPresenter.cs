using System.Numerics;
using Hexa.NET.ImGui;

namespace HytaleAdmin.UI.CanvasView;

/// <summary>
/// Renders a specific entity type on the canvas in three visual states.
/// Each entity type gets its own presenter.
/// </summary>
public interface IMapEntityPresenter
{
    void DrawNormal(ImDrawListPtr drawList, IMapEntity entity, Vector2 screenPos, float zoom);
    void DrawHovered(ImDrawListPtr drawList, IMapEntity entity, Vector2 screenPos, float zoom);
    void DrawSelected(ImDrawListPtr drawList, IMapEntity entity, Vector2 screenPos, float zoom);
    bool HitTest(IMapEntity entity, Vector2 screenPos, Vector2 mousePos, float zoom);
    string? GetTooltip(IMapEntity entity);
}
