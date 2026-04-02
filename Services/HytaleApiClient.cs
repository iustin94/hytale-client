using System.Net.Http.Json;
using HytaleAdmin.Models.Api;

namespace HytaleAdmin.Services;

public class HytaleApiClient : IDisposable
{
    private readonly HttpClient _http = new();

    public event Action<string>? StatusChanged;
    public event Action<string, string>? RequestLogged; // method, url
    public event Action<string, bool, string?>? ResponseLogged; // url, success, detail

    public string BaseUrl { get; set; } = "http://localhost:8080";

    private void SetStatus(string msg) => StatusChanged?.Invoke(msg);
    private void LogReq(string method, string url) => RequestLogged?.Invoke(method, url);
    private void LogResp(string url, bool ok, string? detail = null) => ResponseLogged?.Invoke(url, ok, detail);

    // ─── Surface / Map ────────────────────────────────────────────

    public async Task<SurfaceResponse?> GetSurfaceAsync(string world, int x, int z, int radius)
    {
        SetStatus("Loading surface...");
        try
        {
            var resp = await _http.GetFromJsonAsync<SurfaceResponse>(
                $"{BaseUrl}/api/worlds/{world}/surface?x={x}&z={z}&radius={radius}");
            SetStatus(resp?.Error != null ? $"Error: {resp.Error}" : $"Loaded {resp?.Surface.Length ?? 0} blocks");
            return resp;
        }
        catch (Exception ex) { SetStatus($"Surface failed: {ex.Message}"); return null; }
    }

    // ─── Players ──────────────────────────────────────────────────

    public async Task<PlayerDto[]?> GetPlayersAsync()
    {
        try
        {
            var url = $"{BaseUrl}/api/players";
            var json = await _http.GetStringAsync(url);
            json = json.TrimStart();
            if (json.StartsWith('['))
                return System.Text.Json.JsonSerializer.Deserialize<PlayerDto[]>(json);
            var paginated = System.Text.Json.JsonSerializer.Deserialize<PaginatedResponse<PlayerDto>>(json);
            return paginated?.Data;
        }
        catch { return null; }
    }

    public async Task<PlayerDto?> GetPlayerAsync(string uuid)
    {
        try { return await _http.GetFromJsonAsync<PlayerDto>($"{BaseUrl}/api/players/{uuid}"); }
        catch { return null; }
    }

    public async Task<EntityStatsDto?> GetPlayerStatsAsync(string uuid)
    {
        try { return await _http.GetFromJsonAsync<EntityStatsDto>($"{BaseUrl}/api/players/{uuid}/stats"); }
        catch { return null; }
    }

    public async Task<StatModifyResponse?> ModifyPlayerStatAsync(string uuid, StatModifyRequest request)
    {
        SetStatus($"Modifying {request.Stat}...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/players/{uuid}/stats", request);
            var result = await resp.Content.ReadFromJsonAsync<StatModifyResponse>();
            SetStatus(result?.Success == true ? $"Set {request.Stat}" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Stat modify failed: {ex.Message}"); return null; }
    }

    public async Task<ApiResponse?> TeleportPlayerAsync(string uuid, TeleportRequest request)
    {
        SetStatus("Teleporting player...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/players/{uuid}/teleport", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse>();
            SetStatus(result?.Success == true ? "Player teleported" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Teleport failed: {ex.Message}"); return null; }
    }

    public async Task<ApiResponse?> SendMessageAsync(string uuid, string message)
    {
        SetStatus("Sending message...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/players/{uuid}/message", new MessageRequest { Message = message });
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse>();
            SetStatus(result?.Success == true ? "Message sent" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Message failed: {ex.Message}"); return null; }
    }

    public async Task<ApiResponse?> KickPlayerAsync(string uuid)
    {
        SetStatus("Kicking player...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/players/{uuid}/kick", new { });
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse>();
            SetStatus(result?.Success == true ? "Player kicked" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Kick failed: {ex.Message}"); return null; }
    }

    // ─── Entities ─────────────────────────────────────────────────

    public async Task<EntityDto[]?> GetEntitiesAsync(string world, string? type = null)
    {
        var url = $"{BaseUrl}/api/entities?world={world}";
        if (!string.IsNullOrEmpty(type)) url += $"&type={Uri.EscapeDataString(type)}";
        try
        {
            var json = await _http.GetStringAsync(url);
            json = json.TrimStart();
            if (json.StartsWith('['))
                return System.Text.Json.JsonSerializer.Deserialize<EntityDto[]>(json);
            var paginated = System.Text.Json.JsonSerializer.Deserialize<PaginatedResponse<EntityDto>>(json);
            return paginated?.Data;
        }
        catch { return null; }
    }

    public async Task<string[]?> GetEntityTypesAsync(string world)
    {
        try { return await _http.GetFromJsonAsync<string[]>($"{BaseUrl}/api/entities/types?world={world}"); }
        catch { return null; }
    }

    public async Task<EntityDto?> GetEntityAsync(string uuid, string world = "default")
    {
        try { return await _http.GetFromJsonAsync<EntityDto>($"{BaseUrl}/api/entities/{uuid}?world={world}"); }
        catch { return null; }
    }

    public async Task<ApiResponse?> DeleteEntityAsync(string uuid, string world = "default")
    {
        SetStatus("Removing entity...");
        try
        {
            var resp = await _http.DeleteAsync($"{BaseUrl}/api/entities/{uuid}?world={world}");
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse>();
            SetStatus(result?.Success == true ? "Entity removed" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Delete failed: {ex.Message}"); return null; }
    }

    public async Task<ApiResponse?> SpawnEntityAsync(EntitySpawnRequest request)
    {
        SetStatus($"Spawning {request.Type}...");
        LogReq("POST", $"/api/entities/spawn  type={request.Type} pos=({request.X:F1},{request.Y:F1},{request.Z:F1})");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/entities/spawn", request);
            var body = await resp.Content.ReadAsStringAsync();
            LogResp("/api/entities/spawn", resp.IsSuccessStatusCode, $"[{(int)resp.StatusCode}] {body}");
            var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse>(body);
            SetStatus(result?.Success == true ? $"Spawned {request.Type}" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex)
        {
            LogResp("/api/entities/spawn", false, ex.Message);
            SetStatus($"Spawn failed: {ex.Message}");
            return null;
        }
    }

    public async Task<EntityStatsDto?> GetEntityStatsAsync(string uuid, string world = "default")
    {
        try { return await _http.GetFromJsonAsync<EntityStatsDto>($"{BaseUrl}/api/entities/{uuid}/stats?world={world}"); }
        catch { return null; }
    }

    public async Task<StatModifyResponse?> ModifyEntityStatAsync(string uuid, StatModifyRequest request, string world = "default")
    {
        SetStatus($"Modifying entity {request.Stat}...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/entities/{uuid}/stats?world={world}", request);
            var result = await resp.Content.ReadFromJsonAsync<StatModifyResponse>();
            SetStatus(result?.Success == true ? $"Set {request.Stat}" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Stat modify failed: {ex.Message}"); return null; }
    }

    public async Task<ApiResponse?> TeleportEntityAsync(string uuid, TeleportRequest request, string world = "default")
    {
        SetStatus("Teleporting entity...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/entities/{uuid}/teleport?world={world}", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse>();
            SetStatus(result?.Success == true ? "Entity teleported" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Teleport failed: {ex.Message}"); return null; }
    }

    // ─── Worlds ───────────────────────────────────────────────────

    public async Task<WorldDto[]?> GetWorldsAsync()
    {
        try { return await _http.GetFromJsonAsync<WorldDto[]>($"{BaseUrl}/api/worlds"); }
        catch { return null; }
    }

    public async Task<WorldDto?> GetWorldAsync(string worldId)
    {
        try { return await _http.GetFromJsonAsync<WorldDto>($"{BaseUrl}/api/worlds/{worldId}"); }
        catch { return null; }
    }

    public async Task<BlockDto?> GetBlockAsync(string world, int x, int y, int z)
    {
        try { return await _http.GetFromJsonAsync<BlockDto>($"{BaseUrl}/api/worlds/{world}/blocks?x={x}&y={y}&z={z}"); }
        catch { return null; }
    }

    public async Task<ApiResponse?> SetBlockAsync(string world, SetBlockRequest request)
    {
        SetStatus($"Setting block {request.BlockType}...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/worlds/{world}/blocks", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse>();
            SetStatus(result?.Success == true ? "Block set" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Set block failed: {ex.Message}"); return null; }
    }

    public async Task<ApiResponse?> PlacePrefabAsync(string world, PlacePrefabRequest request)
    {
        SetStatus($"Placing prefab...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/worlds/{world}/prefabs", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse>();
            SetStatus(result?.Success == true ? "Prefab placed" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Prefab failed: {ex.Message}"); return null; }
    }

    // ─── Assets ───────────────────────────────────────────────────

    public async Task<Dictionary<string, Dictionary<string, string[]>>?> GetAssetsAsync()
    {
        try { return await _http.GetFromJsonAsync<Dictionary<string, Dictionary<string, string[]>>>($"{BaseUrl}/api/assets"); }
        catch { return null; }
    }

    public async Task<BlockDetailDto?> GetBlockDetailAsync(string id)
    {
        try { return await _http.GetFromJsonAsync<BlockDetailDto>($"{BaseUrl}/api/assets/blocks/{Uri.EscapeDataString(id)}"); }
        catch { return null; }
    }

    public async Task<ItemDetailDto?> GetItemDetailAsync(string id)
    {
        try { return await _http.GetFromJsonAsync<ItemDetailDto>($"{BaseUrl}/api/assets/items/{Uri.EscapeDataString(id)}"); }
        catch { return null; }
    }

    public async Task<NpcDetailDto?> GetNpcDetailAsync(string name)
    {
        try { return await _http.GetFromJsonAsync<NpcDetailDto>($"{BaseUrl}/api/assets/npcs/{Uri.EscapeDataString(name)}"); }
        catch { return null; }
    }

    public async Task<Dictionary<string, string[]>?> GetModelsAsync()
    {
        try { return await _http.GetFromJsonAsync<Dictionary<string, string[]>>($"{BaseUrl}/api/assets/models"); }
        catch { return null; }
    }

    public async Task<AssetSearchResponse?> SearchAssetsAsync(string query, string? category = null, int limit = 50)
    {
        var url = $"{BaseUrl}/api/assets/search?q={Uri.EscapeDataString(query)}&limit={limit}";
        if (!string.IsNullOrEmpty(category)) url += $"&category={category}";
        try { return await _http.GetFromJsonAsync<AssetSearchResponse>(url); }
        catch { return null; }
    }

    // ─── Sound ────────────────────────────────────────────────────

    public async Task<Dictionary<string, string[]>?> GetSoundListAsync()
    {
        try { return await _http.GetFromJsonAsync<Dictionary<string, string[]>>($"{BaseUrl}/api/sound/list"); }
        catch { return null; }
    }

    public async Task<string[]?> GetSoundCategoriesAsync()
    {
        try { return await _http.GetFromJsonAsync<string[]>($"{BaseUrl}/api/sound/categories"); }
        catch { return null; }
    }

    public async Task<SoundZoneDto[]?> GetSoundZonesAsync(string world)
    {
        try { return await _http.GetFromJsonAsync<SoundZoneDto[]>($"{BaseUrl}/api/sound/zones?world={world}"); }
        catch { return null; }
    }

    public async Task<SoundResponse?> PlaySoundAsync(SoundPlayRequest request)
    {
        SetStatus($"Playing {request.Sound}...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/sound/play", request);
            var result = await resp.Content.ReadFromJsonAsync<SoundResponse>();
            SetStatus(result?.Success == true ? $"Played {request.Sound}" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Sound failed: {ex.Message}"); return null; }
    }

    public async Task<SoundResponse?> StartAmbientAsync(SoundAmbientRequest request)
    {
        SetStatus($"Starting ambient {request.Sound}...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/sound/ambient", request);
            var result = await resp.Content.ReadFromJsonAsync<SoundResponse>();
            SetStatus(result?.Success == true ? $"Ambient started: {request.Sound}" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Ambient failed: {ex.Message}"); return null; }
    }

    public async Task<SoundResponse?> StopAmbientAsync(string? key = null)
    {
        SetStatus("Stopping ambient...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/sound/ambient/stop", key != null ? new { key } : new object());
            var result = await resp.Content.ReadFromJsonAsync<SoundResponse>();
            SetStatus(result?.Success == true ? "Ambient stopped" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Stop ambient failed: {ex.Message}"); return null; }
    }

    // ─── Place (legacy) ───────────────────────────────────────────

    public async Task<PlaceResponse?> PlaceAsync(PlaceRequest request)
    {
        var url = $"{BaseUrl}/api/place";
        SetStatus($"Placing {request.Id}...");
        LogReq("POST", $"/api/place  cat={request.Category} id={request.Id} pos=({request.X:F1},{request.Y:F1},{request.Z:F1})");
        try
        {
            var resp = await _http.PostAsJsonAsync(url, request);
            var body = await resp.Content.ReadAsStringAsync();
            LogResp("/api/place", resp.IsSuccessStatusCode, $"[{(int)resp.StatusCode}] {body}");
            var result = System.Text.Json.JsonSerializer.Deserialize<PlaceResponse>(body);
            SetStatus(result?.Success == true ? $"Placed {request.Id}" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex)
        {
            LogResp("/api/place", false, ex.Message);
            SetStatus($"Place failed: {ex.Message}");
            return null;
        }
    }

    // ─── Server ───────────────────────────────────────────────────

    public async Task<ServerInfoDto?> GetServerInfoAsync()
    {
        try { return await _http.GetFromJsonAsync<ServerInfoDto>($"{BaseUrl}/api/server/info"); }
        catch { return null; }
    }

    // ─── Commands ─────────────────────────────────────────────────

    public async Task<CommandListEntry[]?> GetCommandsAsync()
    {
        try { return await _http.GetFromJsonAsync<CommandListEntry[]>($"{BaseUrl}/api/commands/list"); }
        catch { return null; }
    }

    public async Task<CommandResponse?> ExecuteCommandAsync(CommandRequest request)
    {
        SetStatus($"Executing {request.Command}...");
        try
        {
            var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/commands/execute", request);
            var result = await resp.Content.ReadFromJsonAsync<CommandResponse>();
            SetStatus(result?.Success == true ? $"Command executed" : $"Failed: {result?.Error}");
            return result;
        }
        catch (Exception ex) { SetStatus($"Command failed: {ex.Message}"); return null; }
    }

    public void Dispose() => _http.Dispose();
}
