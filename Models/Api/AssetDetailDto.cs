using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class BlockDetailDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("particleColor")] public ColorDto? ParticleColor { get; set; }
    [JsonPropertyName("tintUp")] public ColorDto? TintUp { get; set; }
}

public class ItemDetailDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("maxStack")] public int MaxStack { get; set; }
    [JsonPropertyName("hasBlockType")] public bool HasBlockType { get; set; }
    [JsonPropertyName("isConsumable")] public bool IsConsumable { get; set; }
}

public class NpcDetailDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("maxHealth")] public float MaxHealth { get; set; }
    [JsonPropertyName("appearance")] public string? Appearance { get; set; }
    [JsonPropertyName("invulnerable")] public bool Invulnerable { get; set; }
    [JsonPropertyName("breathesInAir")] public bool BreathesInAir { get; set; }
    [JsonPropertyName("breathesInWater")] public bool BreathesInWater { get; set; }
    [JsonPropertyName("knockbackScale")] public float KnockbackScale { get; set; }
    [JsonPropertyName("inertia")] public float Inertia { get; set; }
    [JsonPropertyName("canLeadFlock")] public bool CanLeadFlock { get; set; }
    [JsonPropertyName("flockInfluenceRange")] public float FlockInfluenceRange { get; set; }
    [JsonPropertyName("nameTranslationKey")] public string? NameTranslationKey { get; set; }
    [JsonPropertyName("dropListId")] public string? DropListId { get; set; }
    [JsonPropertyName("flockAllowedRoles")] public string[]? FlockAllowedRoles { get; set; }
}

public class ColorDto
{
    [JsonPropertyName("r")] public int R { get; set; }
    [JsonPropertyName("g")] public int G { get; set; }
    [JsonPropertyName("b")] public int B { get; set; }
    [JsonPropertyName("a")] public int? A { get; set; }
}

public class AssetSearchResponse
{
    [JsonPropertyName("blocks")] public string[]? Blocks { get; set; }
    [JsonPropertyName("items")] public string[]? Items { get; set; }
    [JsonPropertyName("npcs")] public string[]? Npcs { get; set; }
    [JsonPropertyName("models")] public string[]? Models { get; set; }
    [JsonPropertyName("sounds")] public string[]? Sounds { get; set; }
}
