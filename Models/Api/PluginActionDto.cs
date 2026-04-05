using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class PluginActionDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("requiresEntity")] public bool RequiresEntity { get; set; }
    [JsonPropertyName("confirm")] public string? Confirm { get; set; }
    [JsonPropertyName("subModule")] public string? SubModule { get; set; }
    [JsonPropertyName("groups")] public FieldGroupDto[] Groups { get; set; } = [];
}

public class ActionResultDto
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("entityId")] public string? EntityId { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("errors")] public string[]? Errors { get; set; }
}
