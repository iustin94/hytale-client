using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class ServerInfoDto
{
    [JsonPropertyName("playerCount")] public int PlayerCount { get; set; }
    [JsonPropertyName("pluginVersion")] public string PluginVersion { get; set; } = "";
}
