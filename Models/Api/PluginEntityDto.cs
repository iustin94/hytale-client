using System.Text.Json;
using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class PluginEntitySummaryDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("group")] public string? Group { get; set; }
    [JsonPropertyName("x")] public float X { get; set; }
    [JsonPropertyName("y")] public float Y { get; set; }
    [JsonPropertyName("z")] public float Z { get; set; }
    [JsonPropertyName("minX")] public float? MinX { get; set; }
    [JsonPropertyName("maxX")] public float? MaxX { get; set; }
    [JsonPropertyName("minZ")] public float? MinZ { get; set; }
    [JsonPropertyName("maxZ")] public float? MaxZ { get; set; }
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("subgroup")] public string? Subgroup { get; set; }
    [JsonPropertyName("placeable")] public bool Placeable { get; set; }
    [JsonPropertyName("showBoundary")] public bool ShowBoundary { get; set; }
}

public class PluginEntityListResponse
{
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("offset")] public int Offset { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("data")] public PluginEntitySummaryDto[] Data { get; set; } = [];
}

public class PluginEntityValuesDto
{
    [JsonPropertyName("entityId")] public string EntityId { get; set; } = "";
    [JsonPropertyName("entityLabel")] public string EntityLabel { get; set; } = "";
    [JsonPropertyName("values")] public Dictionary<string, JsonElement> Values { get; set; } = new();
}
