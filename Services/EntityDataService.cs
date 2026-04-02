using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;

namespace HytaleAdmin.Services;

public class EntityDataService : IDisposable
{
    private Timer? _pollTimer;
    private volatile bool _pendingUpdate;

    public PlayerDto[] Players { get; private set; } = [];
    public EntityDto[] Entities { get; private set; } = [];
    public SoundZoneDto[] SoundZones { get; private set; } = [];

    public event Action? DataUpdated;

    public async Task PollAsync(HytaleApiClient api, EditorConfig config)
    {
        var playersTask = api.GetPlayersAsync();
        var entitiesTask = api.GetEntitiesAsync(config.WorldId, config.EntityFilter);
        var zonesTask = api.GetSoundZonesAsync(config.WorldId);

        await Task.WhenAll(playersTask, entitiesTask, zonesTask);

        Players = playersTask.Result ?? [];
        Entities = entitiesTask.Result ?? [];
        SoundZones = zonesTask.Result ?? [];

        _pendingUpdate = true;
    }

    /// <summary>
    /// Call from the main game loop to flush pending updates safely.
    /// This avoids modifying the scene from a background timer thread.
    /// </summary>
    public void FlushOnMainThread()
    {
        if (!_pendingUpdate) return;
        _pendingUpdate = false;
        DataUpdated?.Invoke();
    }

    public void StartPolling(HytaleApiClient api, EditorConfig config)
    {
        StopPolling();
        if (config.RefreshRateMs <= 0) return;

        _pollTimer = new Timer(async _ =>
        {
            await PollAsync(api, config);
        }, null, config.RefreshRateMs, config.RefreshRateMs);
    }

    public void StopPolling()
    {
        _pollTimer?.Dispose();
        _pollTimer = null;
    }

    public void Dispose() => StopPolling();
}
