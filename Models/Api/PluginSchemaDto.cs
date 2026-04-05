using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class PluginSummaryDto
{
    [JsonPropertyName("pluginId")] public string PluginId { get; set; } = "";
    [JsonPropertyName("pluginName")] public string PluginName { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = "";
    [JsonPropertyName("entityLabel")] public string EntityLabel { get; set; } = "";
    [JsonPropertyName("available")] public bool Available { get; set; }
}

public class PluginSchemaDto
{
    [JsonPropertyName("pluginId")] public string PluginId { get; set; } = "";
    [JsonPropertyName("pluginName")] public string PluginName { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = "";
    [JsonPropertyName("entityLabel")] public string EntityLabel { get; set; } = "";
    [JsonPropertyName("groups")] public FieldGroupDto[] Groups { get; set; } = [];
    [JsonPropertyName("actions")] public PluginActionDto[] Actions { get; set; } = [];
    [JsonPropertyName("spatialFields")] public Dictionary<string, string>? SpatialFields { get; set; }
    [JsonPropertyName("mapPresenter")] public MapPresenterDto? MapPresenter { get; set; }
    [JsonPropertyName("subModules")] public SubModuleDto[]? SubModules { get; set; }
    [JsonPropertyName("graphHints")] public GraphHintsDto? GraphHints { get; set; }
}

public class SubModuleDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("entityType")] public string EntityType { get; set; } = "";
    [JsonPropertyName("entityLabel")] public string EntityLabel { get; set; } = "";
}

public class MapPresenterDto
{
    [JsonPropertyName("shape")] public string Shape { get; set; } = "circle";
    [JsonPropertyName("fillColor")] public string? FillColor { get; set; }
    [JsonPropertyName("borderColor")] public string? BorderColor { get; set; }
    [JsonPropertyName("labelColor")] public string? LabelColor { get; set; }
    [JsonPropertyName("radiusField")] public string? RadiusField { get; set; }
    [JsonPropertyName("showLabel")] public bool ShowLabel { get; set; } = true;
}

public class FieldGroupDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("order")] public int Order { get; set; }
    [JsonPropertyName("fields")] public FieldDefinitionDto[] Fields { get; set; } = [];
}

public class FieldDefinitionDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "string";
    [JsonPropertyName("min")] public float? Min { get; set; }
    [JsonPropertyName("max")] public float? Max { get; set; }
    [JsonPropertyName("enumValues")] public string[]? EnumValues { get; set; }
    [JsonPropertyName("readOnly")] public bool ReadOnly { get; set; }
    [JsonPropertyName("required")] public bool Required { get; set; }
}
