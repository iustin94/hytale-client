using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

/// <summary>
/// Generic success/error response used by most mutating endpoints.
/// </summary>
public class ApiResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}

/// <summary>
/// Response from stat modification endpoints.
/// </summary>
public class StatModifyResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("stat")] public string? Stat { get; set; }
    [JsonPropertyName("current")] public float Current { get; set; }
    [JsonPropertyName("max")] public float Max { get; set; }
}
