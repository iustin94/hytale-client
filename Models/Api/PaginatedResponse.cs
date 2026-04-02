using System.Text.Json.Serialization;

namespace HytaleAdmin.Models.Api;

public class PaginatedResponse<T>
{
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("offset")] public int Offset { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; }
    [JsonPropertyName("data")] public T[] Data { get; set; } = [];
}
