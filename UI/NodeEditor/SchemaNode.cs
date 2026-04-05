using System.Numerics;

namespace HytaleAdmin.UI.NodeEditor;

public class SchemaNode : INode
{
    public required string Id { get; set; }
    public required string NodeType { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public Vector2 Position { get; set; }
    public IReadOnlyList<PortDefinition> Ports { get; set; } = [];

    // Entity backing data
    public required string EntityId { get; set; }
    public string? EntityPrefix { get; set; }
    public Dictionary<string, string> Values { get; set; } = new();
}
