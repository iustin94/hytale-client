using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class StatModifyRequest
{
    [JsonPropertyName("stat")] public string Stat { get; set; } = "";
    [JsonPropertyName("action")] public string Action { get; set; } = "set";
    [JsonPropertyName("value")] public float Value { get; set; }
}

public class TeleportRequest
{
    [JsonPropertyName("x")] public double X { get; set; }
    [JsonPropertyName("y")] public double Y { get; set; }
    [JsonPropertyName("z")] public double Z { get; set; }
    [JsonPropertyName("world")] public string? World { get; set; }
}

public class MessageRequest
{
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}
