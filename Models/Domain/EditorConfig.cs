namespace HytaleAdmin.Models.Domain;

public class EditorConfig
{
    public string ApiBaseUrl { get; set; } = "http://localhost:8080";
    public string WorldId { get; set; } = "default";
    public int CenterX { get; set; }
    public int CenterZ { get; set; }
    public int Radius { get; set; } = 64;
    public int RefreshRateMs { get; set; } = 10000;
    public string? EntityFilter { get; set; } = "HyCitizens";
}
