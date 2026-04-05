using System.Numerics;

namespace HytaleAdmin.UI.NodeEditor;

public enum PortDirection { Input, Output }

public record PortDefinition(string Id, string Label, PortDirection Direction, string PortType)
{
    public uint Color { get; init; } = 0xFFFFFFFF;
}

public record NodeLink(string Id, string SourceNodeId, string SourcePortId, string TargetNodeId, string TargetPortId);

public interface INode
{
    string Id { get; }
    string NodeType { get; }
    string Title { get; }
    string? Subtitle { get; }
    Vector2 Position { get; set; }
    IReadOnlyList<PortDefinition> Ports { get; }
}
