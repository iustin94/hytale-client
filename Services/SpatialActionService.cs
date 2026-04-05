using HytaleAdmin.Models.Domain;

namespace HytaleAdmin.Services;

/// <summary>
/// Executes plugin spatial actions at world coordinates.
/// Resolves spatial field semantics (x, z, surfaceY, worldId, autoName).
/// </summary>
public class SpatialActionService
{
    private readonly HytaleApiClient _client;
    private readonly MapDataService _mapData;
    private readonly EditorConfig _config;
    private readonly PluginSchemaCache _schemaCache;

    public event Action<string>? StatusChanged;

    public SpatialActionService(HytaleApiClient client, MapDataService mapData,
        EditorConfig config, PluginSchemaCache schemaCache)
    {
        _client = client;
        _mapData = mapData;
        _config = config;
        _schemaCache = schemaCache;
    }

    public async Task ExecuteAsync(SpatialAction sa, float worldX, float worldZ, string? entityName = null)
    {
        int blockX = (int)MathF.Floor(worldX);
        int blockZ = (int)MathF.Floor(worldZ);
        var block = _mapData.TryGetBlock(blockX, blockZ);
        float surfaceY = block?.Y ?? 64f;

        var parameters = new Dictionary<string, string>();

        // Resolve parameters from action schema + spatial fields
        foreach (var group in sa.Action.Groups)
        {
            foreach (var field in group.Fields)
            {
                if (sa.SpatialFields != null &&
                    sa.SpatialFields.TryGetValue(field.Id, out var semantic))
                {
                    parameters[field.Id] = ResolveSemantic(semantic, worldX, worldZ, surfaceY, entityName);
                }
                else if (field.EnumValues is { Length: > 0 })
                {
                    parameters[field.Id] = field.EnumValues[0];
                }
                else
                {
                    parameters[field.Id] = "";
                }
            }
        }

        StatusChanged?.Invoke($"Executing {sa.Action.Label}...");

        try
        {
            var result = await _client.ExecutePluginActionAsync(
                sa.PluginId, sa.Action.Id, null, parameters);
            StatusChanged?.Invoke(result?.Success == true
                ? result.Message ?? "Done"
                : $"Failed: {result?.Errors?.FirstOrDefault() ?? "Unknown"}");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Error: {ex.Message}");
        }
    }

    private string ResolveSemantic(string semantic, float worldX, float worldZ, float surfaceY, string? entityName)
    {
        return semantic switch
        {
            "x" => worldX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
            "z" => worldZ.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
            "surfaceY" => surfaceY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
            "worldId" => _config.WorldId,
            "autoName" => entityName ?? $"Region at {worldX:F0}, {worldZ:F0}",
            _ when semantic.StartsWith("default:") => semantic[8..],
            _ => "",
        };
    }
}
