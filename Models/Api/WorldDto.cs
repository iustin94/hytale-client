using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class WorldDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("playerCount")] public int PlayerCount { get; set; }
}

public class BlockDto
{
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("z")] public int Z { get; set; }
    [JsonPropertyName("blockType")] public string BlockType { get; set; } = "";
    [JsonPropertyName("fluidId")] public int FluidId { get; set; }
}

public class SetBlockRequest
{
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("z")] public int Z { get; set; }
    [JsonPropertyName("blockType")] public string BlockType { get; set; } = "";
}

public class PlacePrefabRequest
{
    [JsonPropertyName("prefab")] public string Prefab { get; set; } = "";
    [JsonPropertyName("x")] public double X { get; set; }
    [JsonPropertyName("y")] public double Y { get; set; }
    [JsonPropertyName("z")] public double Z { get; set; }
}
