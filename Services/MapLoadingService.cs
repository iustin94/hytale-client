using HytaleAdmin.Models.Domain;
using HytaleAdmin.Rendering;

namespace HytaleAdmin.Services;

/// <summary>
/// Loads surface data from the API and updates the map renderer.
/// </summary>
public class MapLoadingService
{
    private readonly HytaleApiClient _client;
    private readonly MapDataService _mapData;
    private readonly MapRenderer _mapRenderer;
    private readonly EntityDataService _entityData;

    public MapLoadingService(HytaleApiClient client, MapDataService mapData,
        MapRenderer mapRenderer, EntityDataService entityData)
    {
        _client = client;
        _mapData = mapData;
        _mapRenderer = mapRenderer;
        _entityData = entityData;
    }

    public async Task LoadAsync(EditorConfig config)
    {
        var resp = await _client.GetSurfaceAsync(config.WorldId, config.CenterX, config.CenterZ, config.Radius);
        if (resp?.Surface != null)
        {
            _mapData.Merge(resp);
            _mapRenderer.LookAt(config.CenterX, config.CenterZ);
        }

        _entityData.StartPolling(_client, config);
    }
}
