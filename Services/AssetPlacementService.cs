using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;

namespace HytaleAdmin.Services;

/// <summary>
/// Places assets, spawns NPCs, and plays sounds at world coordinates.
/// Handles Y coordinate resolution from surface data.
/// </summary>
public class AssetPlacementService
{
    private readonly HytaleApiClient _client;
    private readonly MapDataService _mapData;
    private readonly EntityDataService _entityData;
    private readonly EditorConfig _config;

    public event Action<string>? StatusChanged;

    public AssetPlacementService(HytaleApiClient client, MapDataService mapData,
        EntityDataService entityData, EditorConfig config)
    {
        _client = client;
        _mapData = mapData;
        _entityData = entityData;
        _config = config;
    }

    public async Task PlaceAssetAsync(SelectedAsset asset, float worldX, float worldZ)
    {
        int blockX = (int)MathF.Floor(worldX);
        int blockZ = (int)MathF.Floor(worldZ);
        var block = _mapData.TryGetBlock(blockX, blockZ);

        if (block == null)
        {
            StatusChanged?.Invoke("No surface data — load map first");
            return;
        }

        if (asset.Category == "sounds")
        {
            await _client.PlaySoundAsync(new SoundPlayRequest
            {
                Sound = asset.Id,
                World = _config.WorldId,
                X = blockX + 0.5f, Y = block.Y + 1, Z = blockZ + 0.5f
            });
        }
        else if (asset.Category == "npcs")
        {
            await SpawnEntityAsync(asset.Id, blockX + 0.5f, block.Y + 1, blockZ + 0.5f);
        }
        else
        {
            await PlaceBlockAssetAsync(asset.Category, asset.Id, blockX + 0.5f, block.Y, blockZ + 0.5f, asset.Rotation);
        }
    }

    public async Task SpawnEntityAsync(string type, float x, float y, float z)
    {
        var result = await _client.SpawnEntityAsync(new EntitySpawnRequest
        {
            Type = type,
            World = _config.WorldId,
            X = x, Y = y, Z = z
        });

        if (result?.Success == true)
        {
            StatusChanged?.Invoke($"Spawned {type}");
            await _entityData.PollAsync(_client, _config);
        }
        else
        {
            StatusChanged?.Invoke($"Spawn failed: {result?.Error ?? "Unknown"}");
        }
    }

    public async Task SpawnAtWorldPosAsync(string type, float worldX, float worldZ)
    {
        int blockX = (int)MathF.Floor(worldX);
        int blockZ = (int)MathF.Floor(worldZ);
        var block = _mapData.TryGetBlock(blockX, blockZ);

        float y;
        if (block != null)
        {
            y = block.Y + 1;
        }
        else
        {
            try
            {
                var resp = await _client.GetSurfaceAsync(_config.WorldId, blockX, blockZ, 0);
                y = resp?.Surface is { Length: > 0 } ? resp.Surface[0].Y + 1 : 64;
            }
            catch { y = 64; }
        }

        await SpawnEntityAsync(type, blockX + 0.5f, y, blockZ + 0.5f);
    }

    private async Task PlaceBlockAssetAsync(string category, string id, float x, float y, float z, int rotation)
    {
        StatusChanged?.Invoke($"Placing {category}:{id}...");
        try
        {
            var result = await _client.PlaceAssetAsync(category, id, _config.WorldId, x, y, z, rotation);
            StatusChanged?.Invoke(result?.Success == true
                ? $"Placed {id}"
                : $"Failed: {result?.Errors?.FirstOrDefault() ?? "Unknown"}");

            if (result?.Success == true)
            {
                var surface = await _client.GetSurfaceAsync(_config.WorldId,
                    (int)MathF.Floor(x), (int)MathF.Floor(z), 2);
                if (surface?.Surface != null) _mapData.Merge(surface);
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Place failed: {ex.Message}");
        }
    }
}
