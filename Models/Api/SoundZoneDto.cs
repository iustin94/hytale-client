using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class SoundZoneDto
{
    [JsonPropertyName("key")] public string Key { get; set; } = "";
    [JsonPropertyName("sound")] public string Sound { get; set; } = "";
    [JsonPropertyName("world")] public string World { get; set; } = "";
    [JsonPropertyName("x")] public float X { get; set; }
    [JsonPropertyName("y")] public float Y { get; set; }
    [JsonPropertyName("z")] public float Z { get; set; }
    [JsonPropertyName("minX")] public float MinX { get; set; }
    [JsonPropertyName("minZ")] public float MinZ { get; set; }
    [JsonPropertyName("maxX")] public float MaxX { get; set; }
    [JsonPropertyName("maxZ")] public float MaxZ { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
}
