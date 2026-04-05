using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class GraphHintsDto
{
    [JsonPropertyName("nodeTypes")] public GraphNodeTypeHint[] NodeTypes { get; set; } = [];
    [JsonPropertyName("connectionRules")] public GraphConnectionRule[] ConnectionRules { get; set; } = [];
}

public class GraphNodeTypeHint
{
    [JsonPropertyName("groupId")] public string GroupId { get; set; } = "";
    [JsonPropertyName("entityPrefix")] public string EntityPrefix { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("headerColor")] public string HeaderColor { get; set; } = "#334455";
    [JsonPropertyName("ports")] public GraphPortHint[] Ports { get; set; } = [];
}

public class GraphPortHint
{
    [JsonPropertyName("fieldId")] public string FieldId { get; set; } = "";
    [JsonPropertyName("portId")] public string PortId { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("direction")] public string Direction { get; set; } = "output";
    [JsonPropertyName("portType")] public string PortType { get; set; } = "";
    [JsonPropertyName("color")] public string Color { get; set; } = "#FFFFFF";
    [JsonPropertyName("multiLink")] public bool MultiLink { get; set; } = true;
}

public class GraphConnectionRule
{
    [JsonPropertyName("outputType")] public string OutputType { get; set; } = "";
    [JsonPropertyName("inputType")] public string InputType { get; set; } = "";
}
