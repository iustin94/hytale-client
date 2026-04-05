using HytaleAdmin.UI.NodeEditor;

namespace HytaleAdmin.UI.CanvasView;

/// <summary>Context menu provider for the canvas view.</summary>
public interface IMapContextMenuProvider
{
    List<ContextMenuItem> GetBackgroundMenu(float worldX, float worldZ);
    List<ContextMenuItem> GetEntityMenu(IMapEntity entity);
    List<ContextMenuItem> GetMultiSelectMenu(IReadOnlySet<string> selectedIds);
    void OnItemSelected(ContextMenuItem item, IMapEntity? entity, float worldX, float worldZ);
}
