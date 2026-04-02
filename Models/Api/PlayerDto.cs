using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class PlayerDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("uuid")] public string Uuid { get; set; } = "";
    [JsonPropertyName("x")] public float X { get; set; }
    [JsonPropertyName("y")] public float Y { get; set; }
    [JsonPropertyName("z")] public float Z { get; set; }
    [JsonPropertyName("world")] public string? World { get; set; }
}
