using System.Text.Json;
using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

public class AssetBrowserPanel
{
    private readonly ServiceContainer _services;

    private Dictionary<string, Dictionary<string, string[]>> _allData = new();
    private HashSet<string> _loadedTabs = new();
    private HashSet<string> _loadingTabs = new();
    private string _activeTab = "prefabs";
    private string _searchFilter = "";

    // Asset detail state
    private string? _detailId;
    private string? _detailText;

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 LabelColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);

    private static readonly string[] TabKeys = ["blocks", "items", "prefabs", "npcs", "sounds", "models"];
    private static readonly string[] TabLabels = ["Blocks", "Items", "Prefabs", "NPCs", "Sounds", "Models"];

    // Maps tab key → API category name for plugin entities
    private static readonly Dictionary<string, string> TabToCategory = new()
    {
        ["blocks"] = "Blocks", ["items"] = "Items", ["prefabs"] = "Prefabs",
        ["npcs"] = "NPCs", ["models"] = "Models", ["sounds"] = "Sounds"
    };

    public AssetBrowserPanel(ServiceContainer services)
    {
        _services = services;
    }

    public void Draw()
    {
        ImGui.TextColored(AccentColor, "Assets");
        ImGui.Separator();

        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##search", ref _searchFilter, 128);

        // Tabs
        string prevTab = _activeTab;
        if (ImGui.BeginTabBar("AssetTabs"))
        {
            for (int i = 0; i < TabKeys.Length; i++)
            {
                var flags = TabKeys[i] == "prefabs" ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
                // Only force-select on first frame
                if (_loadedTabs.Count > 0) flags = ImGuiTabItemFlags.None;

                if (ImGui.BeginTabItem(TabLabels[i], flags))
                {
                    _activeTab = TabKeys[i];
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        // Lazy load on tab switch
        if (!_loadedTabs.Contains(_activeTab) && !_loadingTabs.Contains(_activeTab))
        {
            _ = LoadTabAsync(_activeTab);
        }

        // Tree content
        if (ImGui.BeginChild("AssetTree", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing() * 2)))
        {
            DrawTree();
            ImGui.EndChild();
        }

        DrawAssetDetail();

        var asset = _services.Selection.SelectedAsset;
        if (asset != null)
            ImGui.TextColored(AccentColor, $"{asset.Category}: {GetDisplayName(asset.Id)}");
        else
            ImGui.TextColored(LabelColor, "Click asset to select");
    }

    private void DrawTree()
    {
        if (_loadingTabs.Contains(_activeTab))
        {
            ImGui.TextColored(DimColor, "Loading...");
            return;
        }

        if (!_allData.TryGetValue(_activeTab, out var groups))
        {
            ImGui.TextColored(DimColor, "No data loaded");
            return;
        }

        bool hasFilter = !string.IsNullOrEmpty(_searchFilter);
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
                        _ = LoadAssetDetailAndSize(_activeTab, id);
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

    /// <summary>
    /// Called on startup — loads only prefabs tab initially.
    /// </summary>
    public async Task LoadAssetsAsync(HytaleApiClient api)
    {
        await LoadTabAsync("prefabs");
    }

    /// <summary>
    /// Lazily loads a single tab's assets from the plugin API.
    /// Sounds use the old sound handler endpoint.
    /// </summary>
    private async Task LoadTabAsync(string tabKey)
    {
        if (_loadedTabs.Contains(tabKey) || _loadingTabs.Contains(tabKey)) return;
        _loadingTabs.Add(tabKey);

        try
        {
            if (tabKey == "sounds")
            {
                var sounds = await _services.ApiClient.GetSoundListAsync();
                if (sounds != null)
                    _allData["sounds"] = sounds;
            }
            else
            {
                var categoryLabel = TabToCategory.GetValueOrDefault(tabKey, tabKey);
                var entities = await _services.ApiClient.GetAssetEntitiesAllPagesAsync(categoryLabel);

                var forTab = entities
                    .GroupBy(e => e.Subgroup ?? "Other")
                    .ToDictionary(
                        sg => sg.Key,
                        sg => sg.Select(e => e.Label).OrderBy(l => l).ToArray()
                    );

                if (forTab.Count > 0)
                    _allData[tabKey] = forTab;
            }

            _loadedTabs.Add(tabKey);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AssetBrowser] Load {tabKey} failed: {ex.Message}");
        }
        finally
        {
            _loadingTabs.Remove(tabKey);
        }
    }

    private async Task LoadAssetDetailAndSize(string category, string id)
    {
        _detailId = id;
        try
        {
            var detail = await _services.ApiClient.GetAssetDetailAsync(category, id);
            if (detail?.Values != null && detail.Values.Count > 0)
            {
                // Extract size for footprint rendering
                int sizeX = 1, sizeZ = 1;
                if (detail.Values.TryGetValue("sizeX", out var sx) && sx.ValueKind == JsonValueKind.Number)
                    sizeX = Math.Max(1, sx.GetInt32());
                if (detail.Values.TryGetValue("sizeZ", out var sz) && sz.ValueKind == JsonValueKind.Number)
                    sizeZ = Math.Max(1, sz.GetInt32());

                // Update selection with size for footprint rendering
                _services.Selection.UpdateAssetSize(sizeX, sizeZ);

                var parts = new List<string>();
                foreach (var kv in detail.Values)
                {
                    if (kv.Key is "id" or "category" or "group") continue;
                    parts.Add($"{kv.Key}: {FormatValue(kv.Value)}");
                }
                _detailText = parts.Count > 0 ? string.Join(" | ", parts) : null;
            }
            else
            {
                _detailText = null;
            }
        }
        catch { _detailText = null; }
    }

    private static string FormatValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i.ToString() : element.GetDouble().ToString("F1"),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => element.ToString()
        };
    }

    private static string CategoryKey(string? groupLabel)
    {
        if (string.IsNullOrEmpty(groupLabel)) return "other";
        return groupLabel.ToLowerInvariant();
    }

    private static string GetDisplayName(string id)
    {
        // Strip .json extension for display
        if (id.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            id = id[..^5];
        if (id.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            id = id[..^7];

        var lastSlash = id.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < id.Length - 1)
            return id[(lastSlash + 1)..];
        return id;
    }
}
