using HytaleAdmin.UI.NodeEditor;

namespace HytaleAdmin.UI.Components;

public enum TreeContextTarget { Background, Group, Item }

public class TreeContextRequest<TItem> where TItem : class
{
    public TreeContextTarget Target { get; init; }
    public TItem? Item { get; init; }
    public string? GroupId { get; init; }
}

public interface ITreeContextMenu<TItem> where TItem : class
{
    List<ContextMenuItem> GetMenuItems(TreeContextRequest<TItem> request);
    void OnItemSelected(ContextMenuItem item, TreeContextRequest<TItem> request);
}
