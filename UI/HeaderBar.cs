using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

public class HeaderBar
{
    private readonly ServiceContainer _services;
    private readonly Func<Task> _loadMapCallback;

    private string _worldId;
    private string _xText;
    private string _zText;
    private string _radiusText;

    private string _activeFilter;
    private int _activeFilterIdx;
    private int _activeRefreshMs;
    private string[]? _entityTypes;
    private string[] _filterLabels = ["All", "None"];
    private string[] _filterValues = ["", "__none__"];

    private string _statusText = "Press Enter or Load Map";

    // Server info
    private ServerInfoDto? _serverInfo;
    private WorldDto[]? _worlds;
    private int _selectedWorldIdx;
    private bool _connected;

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 StatusColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 ConnectedColor = new(0.31f, 0.80f, 0.40f, 1f);
    private static readonly System.Numerics.Vector4 DisconnectedColor = new(0.65f, 0.18f, 0.25f, 1f);

    private static readonly (string Label, int Ms)[] RefreshOptions =
        [("1s", 1000), ("2s", 2000), ("5s", 5000), ("10s", 10000), ("Off", 0)];

    public HeaderBar(ServiceContainer services, Func<Task> loadMapCallback)
    {
        _services = services;
        _loadMapCallback = loadMapCallback;
        _activeFilter = services.Config.EntityFilter ?? "";
        _activeRefreshMs = services.Config.RefreshRateMs;

        _worldId = services.Config.WorldId;
        _xText = services.Config.CenterX.ToString();
        _zText = services.Config.CenterZ.ToString();
        _radiusText = services.Config.Radius.ToString();
    }

    public void Draw()
    {
        var config = _services.Config;

        // ─── Row 1: Connection + World + Map Controls ─────────────

        // Connection indicator
        var connColor = _connected ? ConnectedColor : DisconnectedColor;
        ImGui.TextColored(connColor, _connected ? "●" : "○");
        ImGui.SameLine();

        // Server info
        if (_serverInfo != null)
        {
            ImGui.TextColored(StatusColor, $"{_serverInfo.PlayerCount}P");
            ImGui.SameLine();
        }

        ImGui.Text("|");
        ImGui.SameLine();

        // World selector
        if (_worlds != null && _worlds.Length > 0)
        {
            ImGui.SetNextItemWidth(100);
            var worldNames = _worlds.Select(w => w.Id).ToArray();
            if (ImGui.Combo("##world", ref _selectedWorldIdx, worldNames, worldNames.Length))
            {
                _worldId = worldNames[_selectedWorldIdx];
                config.WorldId = _worldId;
            }
        }
        else
        {
            ImGui.SetNextItemWidth(80);
            if (ImGui.InputText("World", ref _worldId, 64))
                config.WorldId = _worldId;
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(50);
        if (ImGui.InputText("X", ref _xText, 16))
            if (int.TryParse(_xText, out var x)) config.CenterX = x;

        ImGui.SameLine();
        ImGui.SetNextItemWidth(50);
        if (ImGui.InputText("Z", ref _zText, 16))
            if (int.TryParse(_zText, out var z)) config.CenterZ = z;

        ImGui.SameLine();
        ImGui.SetNextItemWidth(50);
        if (ImGui.InputText("R", ref _radiusText, 16))
            if (int.TryParse(_radiusText, out var r)) config.Radius = Math.Clamp(r, 1, 256);

        ImGui.SameLine();
        if (ImGui.Button("Load Map"))
            _ = _loadMapCallback();

        ImGui.SameLine();
        ImGui.Text("|");
        ImGui.SameLine();

        // ─── Filter dropdown ──────────────────────────────────────

        ImGui.Text("Filter:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(130);
        if (ImGui.Combo("##filter", ref _activeFilterIdx, _filterLabels, _filterLabels.Length))
        {
            _activeFilter = _filterValues[_activeFilterIdx];
            _services.Config.EntityFilter = _activeFilter == "" ? null : (_activeFilter == "__none__" ? "__none__" : _activeFilter);
            _ = _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
        }

        ImGui.SameLine();
        ImGui.Text("|");
        ImGui.SameLine();

        // ─── Refresh rate ─────────────────────────────────────────

        ImGui.Text("Refresh:");
        ImGui.SameLine();

        var defaultBtnColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];
        foreach (var (label, ms) in RefreshOptions)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, _activeRefreshMs == ms ? AccentColor : defaultBtnColor);
            if (ImGui.SmallButton(label))
            {
                _activeRefreshMs = ms;
                config.RefreshRateMs = ms;
                _services.EntityData.StopPolling();
                if (ms > 0)
                    _services.EntityData.StartPolling(_services.ApiClient, config);
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }

        // ─── Row 2: Status + Hover ───────────────────────────────

        ImGui.TextColored(StatusColor, _statusText);

    }

    public async Task LoadEntityTypesAsync(HytaleApiClient api, EditorConfig config)
    {
        _entityTypes = await api.GetEntityTypesAsync(config.WorldId);
        RebuildFilterArrays();
    }

    private void RebuildFilterArrays()
    {
        var labels = new List<string> { "All", "None" };
        var values = new List<string> { "", "__none__" };

        if (_entityTypes != null)
        {
            foreach (var type in _entityTypes)
            {
                labels.Add(type);
                values.Add(type);
            }
        }

        _filterLabels = labels.ToArray();
        _filterValues = values.ToArray();

        // Sync selected index
        _activeFilterIdx = Array.IndexOf(_filterValues, _activeFilter);
        if (_activeFilterIdx < 0) _activeFilterIdx = 0;
    }

    public async Task LoadServerInfoAsync(HytaleApiClient api)
    {
        try
        {
            _serverInfo = await api.GetServerInfoAsync();
            _connected = _serverInfo != null;

            _worlds = await api.GetWorldsAsync();
            if (_worlds != null && _worlds.Length > 0)
            {
                _selectedWorldIdx = Array.FindIndex(_worlds, w => w.Id == _services.Config.WorldId);
                if (_selectedWorldIdx < 0) _selectedWorldIdx = 0;
            }
        }
        catch
        {
            _connected = false;
        }
    }

    public void SetStatus(string message) => _statusText = message;

}
