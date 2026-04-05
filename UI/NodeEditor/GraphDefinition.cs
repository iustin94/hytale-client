using HytaleAdmin.Models.Api;

namespace HytaleAdmin.UI.NodeEditor;

public class GraphDefinition
{
    public List<GraphNodeTypeConfig> NodeTypes { get; set; } = new();
    public PortTypeMap ConnectionRules { get; set; } = new();
    public Dictionary<string, NodeStyle> Styles { get; set; } = new();
}

public class GraphNodeTypeConfig
{
    public required string Id { get; set; }
    public required string GroupId { get; set; }
    public required string EntityPrefix { get; set; }
    public required string Label { get; set; }
    public List<GraphPortConfig> Ports { get; set; } = new();
    public List<FieldDefinitionDto> DisplayFields { get; set; } = new();
}

public class GraphPortConfig
{
    public required string PortId { get; set; }
    public required string FieldId { get; set; }
    public required string Label { get; set; }
    public PortDirection Direction { get; set; }
    public required string PortType { get; set; }
    public uint Color { get; set; } = 0xFFFFFFFF;
    public bool MultiLink { get; set; } = true;
}
