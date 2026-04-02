using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class CommandRequest
{
    [JsonPropertyName("command")] public string Command { get; set; } = "";
    [JsonPropertyName("args")] public string[]? Args { get; set; }
}

public class CommandResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("output")] public string? Output { get; set; }
}

public class CommandListEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
}
