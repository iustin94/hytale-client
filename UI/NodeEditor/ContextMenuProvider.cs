using System.Numerics;

namespace HytaleAdmin.UI.NodeEditor;

public class ContextMenuItem
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? Icon { get; init; }
    public uint Color { get; init; } = 0xFFFFFFFF;
    public bool Separator { get; init; }
    public List<ContextMenuItem>? Children { get; init; }
}

public enum ContextMenuTarget { Canvas, Node }

public class ContextMenuRequest<TNode> where TNode : class, INode
{
    public ContextMenuTarget Target { get; init; }
    public Vector2 CanvasPosition { get; init; }
    public TNode? Node { get; init; }
    public IReadOnlySet<string>? SelectedNodeIds { get; init; }
}

public interface IContextMenuProvider<TNode> where TNode : class, INode
{
    List<ContextMenuItem> GetMenuItems(ContextMenuRequest<TNode> request);
    void OnItemSelected(ContextMenuItem item, ContextMenuRequest<TNode> request);
}
