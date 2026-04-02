using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

public class AssetBrowserPanel
{
    private readonly ServiceContainer _services;

    private Dictionary<string, Dictionary<string, string[]>> _allData = new();
    private string _activeTab = "blocks";
    private string _searchFilter = "";

    // API search state
    private string _apiSearchQuery = "";
    private AssetSearchResponse? _searchResults;
    private bool _searching;

    // Asset detail state
    private string? _detailId;
    private string? _detailText;

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 LabelColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);

    private static readonly string[] TabKeys = ["blocks", "items", "prefabs", "npcs", "sounds", "models"];
    private static readonly string[] TabLabels = ["Blocks", "Items", "Prefabs", "NPCs", "Sounds", "Models"];

    public AssetBrowserPanel(ServiceContainer services)
    {
        _services = services;
    }

    public void Draw()
    {
        ImGui.TextColored(AccentColor, "Assets");
        ImGui.Separator();

        // Search bar with API search toggle
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##search", ref _searchFilter, 128))
        {
            _apiSearchQuery = _searchFilter;
            if (_searchFilter.Length >= 2)
                _ = RunApiSearch();
            else
                _searchResults = null;
        }

        if (_searchResults != null && !string.IsNullOrEmpty(_searchFilter))
        {
            DrawSearchResults();
            return; // Show search results instead of tabs
        }

        // Tabs
        if (ImGui.BeginTabBar("AssetTabs"))
        {
            for (int i = 0; i < TabKeys.Length; i++)
            {
                if (ImGui.BeginTabItem(TabLabels[i]))
                {
                    _activeTab = TabKeys[i];
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        // Tree content
        if (ImGui.BeginChild("AssetTree", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing() * 2)))
        {
            DrawTree();
            ImGui.EndChild();
        }

        // Asset detail preview
        DrawAssetDetail();

        // Selection info
        var asset = _services.Selection.SelectedAsset;
        if (asset != null)
            ImGui.TextColored(AccentColor, $"{asset.Category}: {GetDisplayName(asset.Id)}");
        else
            ImGui.TextColored(LabelColor, "Click an asset to select");
    }

    private void DrawSearchResults()
    {
        if (_searching)
        {
            ImGui.TextColored(DimColor, "Searching...");
            return;
        }

        if (ImGui.BeginChild("SearchResults", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing())))
        {
            DrawSearchCategory("Blocks", "blocks", _searchResults!.Blocks);
            DrawSearchCategory("Items", "items", _searchResults.Items);
            DrawSearchCategory("NPCs", "npcs", _searchResults.Npcs);
            DrawSearchCategory("Models", "models", _searchResults.Models);
            DrawSearchCategory("Sounds", "sounds", _searchResults.Sounds);
            ImGui.EndChild();
        }

        var asset = _services.Selection.SelectedAsset;
        if (asset != null)
            ImGui.TextColored(AccentColor, $"{asset.Category}: {GetDisplayName(asset.Id)}");
    }

    private void DrawSearchCategory(string label, string category, string[]? results)
    {
        if (results == null || results.Length == 0) return;

        if (ImGui.TreeNodeEx($"{label} ({results.Length})", ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach (var id in results)
            {
                bool isSelected = _services.Selection.SelectedAsset?.Id == id;
                if (ImGui.Selectable(GetDisplayName(id) + $"##{category}_{id}", isSelected))
                {
                    _services.Selection.SelectAsset(category, id);
                    _ = LoadAssetDetail(category, id);
                }
            }
            ImGui.TreePop();
        }
    }

    private void DrawTree()
    {
        if (!_allData.TryGetValue(_activeTab, out var groups))
        {
            ImGui.TextColored(DimColor, "No data loaded");
            return;
        }

        bool hasFilter = !string.IsNullOrEmpty(_searchFilter) && _searchResults == null;
        bool anyMatch = false;

        foreach (var (groupName, items) in groups.OrderBy(g => g.Key))
        {
            var filtered = hasFilter
                ? items.Where(id => GetDisplayName(id).Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
                                 || id.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)).ToArray()
                : items;

            if (filtered.Length == 0) continue;
            anyMatch = true;

            var flags = hasFilter ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            if (ImGui.TreeNodeEx(groupName, flags))
            {
                foreach (var id in filtered)
                {
                    var displayName = GetDisplayName(id);
                    bool isSelected = _services.Selection.SelectedAsset?.Category == _activeTab
                                   && _services.Selection.SelectedAsset?.Id == id;

                    if (ImGui.Selectable(displayName, isSelected))
                    {
                        _services.Selection.SelectAsset(_activeTab, id);
                        _ = LoadAssetDetail(_activeTab, id);
                    }
                }
                ImGui.TreePop();
            }
        }

        if (!anyMatch)
            ImGui.TextColored(DimColor, "No matches");
    }

    private void DrawAssetDetail()
    {
        if (_detailText != null && _detailId != null)
        {
            ImGui.Separator();
            ImGui.TextColored(LabelColor, _detailText);
        }
    }

    public async Task LoadAssetsAsync(HytaleApiClient api)
    {
        var assets = await api.GetAssetsAsync();
        if (assets != null)
            foreach (var kv in assets)
                _allData[kv.Key] = kv.Value;

        var sounds = await api.GetSoundListAsync();
        if (sounds != null)
            _allData["sounds"] = sounds;

        // Load models
        var models = await api.GetModelsAsync();
        if (models != null)
            _allData["models"] = models;
    }

    private async Task RunApiSearch()
    {
        _searching = true;
        _searchResults = await _services.ApiClient.SearchAssetsAsync(_apiSearchQuery);
        _searching = false;
    }

    private async Task LoadAssetDetail(string category, string id)
    {
        _detailId = id;
        try
        {
            switch (category)
            {
                case "blocks":
                    var block = await _services.ApiClient.GetBlockDetailAsync(id);
                    if (block != null)
                    {
                        var pc = block.ParticleColor;
                        _detailText = pc != null ? $"Color: ({pc.R},{pc.G},{pc.B})" : "No color data";
                    }
                    break;
                case "items":
                    var item = await _services.ApiClient.GetItemDetailAsync(id);
                    if (item != null)
                        _detailText = $"Stack: {item.MaxStack} | Block: {item.HasBlockType} | Consume: {item.IsConsumable}";
                    break;
                case "npcs":
                    var npc = await _services.ApiClient.GetNpcDetailAsync(id);
                    if (npc != null)
                        _detailText = $"HP: {npc.MaxHealth:F0} | KB: {npc.KnockbackScale:F1}";
                    break;
                default:
                    _detailText = null;
                    break;
            }
        }
        catch { _detailText = null; }
    }

    private static string GetDisplayName(string id)
    {
        var lastSlash = id.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < id.Length - 1)
            return id[(lastSlash + 1)..];
        return id;
    }
}
