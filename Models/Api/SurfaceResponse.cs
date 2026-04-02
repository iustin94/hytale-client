using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class SurfaceResponse
{
    [JsonPropertyName("world")] public string World { get; set; } = "";
    [JsonPropertyName("centerX")] public int CenterX { get; set; }
    [JsonPropertyName("centerZ")] public int CenterZ { get; set; }
    [JsonPropertyName("radius")] public int Radius { get; set; }
    [JsonPropertyName("surface")] public SurfaceBlock[] Surface { get; set; } = [];
    [JsonPropertyName("error")] public string? Error { get; set; }
}

public class SurfaceBlock
{
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("z")] public int Z { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("block")] public string Block { get; set; } = "";
    [JsonPropertyName("r")] public byte? R { get; set; }
    [JsonPropertyName("g")] public byte? G { get; set; }
    [JsonPropertyName("b")] public byte? B { get; set; }
}
