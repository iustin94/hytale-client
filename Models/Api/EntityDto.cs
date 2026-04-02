using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class EntityDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("uuid")] public string? Uuid { get; set; }
    [JsonPropertyName("role")] public string? Role { get; set; }
    [JsonPropertyName("x")] public float X { get; set; }
    [JsonPropertyName("y")] public float Y { get; set; }
    [JsonPropertyName("z")] public float Z { get; set; }
    [JsonPropertyName("maxHealth")] public float? MaxHealth { get; set; }
    [JsonPropertyName("stats")] public EntityStatsDto? Stats { get; set; }
}

public class EntityStatsDto
{
    [JsonPropertyName("health")] public StatValue? Health { get; set; }
    [JsonPropertyName("stamina")] public StatValue? Stamina { get; set; }
    [JsonPropertyName("mana")] public StatValue? Mana { get; set; }
    [JsonPropertyName("oxygen")] public StatValue? Oxygen { get; set; }
}

public class StatValue
{
    [JsonPropertyName("current")] public float Current { get; set; }
    [JsonPropertyName("max")] public float Max { get; set; }
}
