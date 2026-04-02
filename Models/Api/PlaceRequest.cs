using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class PlaceRequest
{
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("world")] public string World { get; set; } = "default";
    [JsonPropertyName("x")] public float X { get; set; }
    [JsonPropertyName("y")] public float Y { get; set; }
    [JsonPropertyName("z")] public float Z { get; set; }
}

public class PlaceResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}
