using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;

namespace HytaleAdmin.Services;

public class MapDataService
{
    private readonly Dictionary<(int x, int z), BlockCell> _blocks = new();
    private volatile bool _pendingUpdate;

    public event Action? MapUpdated;

    public IReadOnlyDictionary<(int x, int z), BlockCell> Blocks => _blocks;

    public void Merge(SurfaceResponse response)
    {
        foreach (var b in response.Surface)
        {
            var cell = new BlockCell
            {
                X = b.X, Z = b.Z, Y = b.Y, Block = b.Block,
                R = b.R ?? GetFallbackColor(b.Block).r,
                G = b.G ?? GetFallbackColor(b.Block).g,
                B = b.B ?? GetFallbackColor(b.Block).b
            };
            _blocks[(b.X, b.Z)] = cell;
        }
        _pendingUpdate = true;
    }

    /// <summary>
    /// Call from the main game loop to safely fire MapUpdated
    /// (texture/entity creation must happen on the main thread).
    /// </summary>
    public void FlushOnMainThread()
    {
        if (!_pendingUpdate) return;
        _pendingUpdate = false;
        MapUpdated?.Invoke();
    }

    public BlockCell? TryGetBlock(int x, int z)
    {
        _blocks.TryGetValue((x, z), out var cell);
        return cell;
    }

    public (int minX, int minZ, int maxX, int maxZ) GetBounds()
    {
        if (_blocks.Count == 0) return (0, 0, 0, 0);
        int minX = int.MaxValue, minZ = int.MaxValue;
        int maxX = int.MinValue, maxZ = int.MinValue;
        foreach (var key in _blocks.Keys)
        {
            if (key.x < minX) minX = key.x;
            if (key.x > maxX) maxX = key.x;
            if (key.z < minZ) minZ = key.z;
            if (key.z > maxZ) maxZ = key.z;
        }
        return (minX, minZ, maxX + 1, maxZ + 1);
    }

    private static (byte r, byte g, byte b) GetFallbackColor(string blockId)
    {
        // Prefix-match lookup for common block types
        if (blockId.StartsWith("Soil_Grass")) return (90, 140, 60);
        if (blockId.StartsWith("Soil_Snow")) return (230, 235, 240);
        if (blockId.StartsWith("Soil_Gravel")) return (140, 120, 100);
        if (blockId.StartsWith("Soil")) return (120, 85, 58);
        if (blockId.StartsWith("Sand_White")) return (220, 215, 195);
        if (blockId.StartsWith("Sand_Red")) return (180, 120, 80);
        if (blockId.StartsWith("Sand")) return (210, 195, 150);
        if (blockId.StartsWith("Rock_Ice")) return (180, 210, 240);
        if (blockId.StartsWith("Rock_Basalt")) return (60, 60, 65);
        if (blockId.StartsWith("Rock")) return (128, 128, 128);
        if (blockId.StartsWith("Leaves")) return (60, 120, 40);
        if (blockId.StartsWith("Log")) return (110, 80, 45);
        if (blockId.StartsWith("Wood")) return (140, 110, 60);
        if (blockId.StartsWith("Water")) return (40, 100, 180);
        if (blockId.StartsWith("Ice")) return (170, 210, 240);
        if (blockId.StartsWith("Grass")) return (85, 140, 55);

        // Hash fallback for unknown blocks
        var hash = blockId.GetHashCode();
        return (
            (byte)(60 + Math.Abs(hash % 140)),
            (byte)(60 + Math.Abs((hash >> 8) % 140)),
            (byte)(60 + Math.Abs((hash >> 16) % 140))
        );
    }
}
