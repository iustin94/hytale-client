using System.Text.Json;
using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

public class AmbienceBrowserPanel
{
    private readonly ServiceContainer _services;

    // Data
    private JsonElement[]? _presets;
    private JsonElement[]? _soundEvents;
    private JsonElement[]? _audioCategories;
    private JsonElement[]? _blockSoundSets;
    private JsonElement? _selectedDetail;
    private string _selectedPresetId = "";

    // State
    private int _tab; // 0=Presets, 1=SoundEvents, 2=Categories, 3=BlockSoundSets
    private string _filter = "";
    private bool _loading;

    private static readonly System.Numerics.Vector4 AccentColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly System.Numerics.Vector4 LabelColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly System.Numerics.Vector4 ValueColor = new(0.85f, 0.85f, 0.90f, 1f);

    private static readonly string[] TabLabels = ["Presets", "Sounds", "Categories", "BlockSounds"];

    public AmbienceBrowserPanel(ServiceContainer services)
    {
        _services = services;
    }

    public void Draw(float width, float height)
    {
        ImGui.BeginChild("AmbienceBrowser", new System.Numerics.Vector2(width, height), ImGuiChildFlags.Borders);

        ImGui.TextColored(AccentColor, "AmbienceFX Browser");
        ImGui.Separator();

        // Tabs
        if (ImGui.BeginTabBar("AmbienceTabs"))
        {
            for (int i = 0; i < TabLabels.Length; i++)
            {
                if (ImGui.BeginTabItem(TabLabels[i]))
                {
                    if (_tab != i) { _tab = i; _selectedDetail = null; _selectedPresetId = ""; }
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        // Filter
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##ambienceFilter", ref _filter, 128);

        if (_loading)
        {
            ImGui.TextColored(DimColor, "Loading...");
            ImGui.EndChild();
            return;
        }

        // Load on first draw per tab
        EnsureLoaded();

        float listWidth = width * 0.4f;
        float detailWidth = width - listWidth - ImGui.GetStyle().ItemSpacing.X - 16;
        float contentHeight = height - ImGui.GetCursorPosY() - 8;

        // Left: list
        ImGui.BeginChild("AmbList", new System.Numerics.Vector2(listWidth, contentHeight), ImGuiChildFlags.Borders);
        switch (_tab)
        {
            case 0: DrawPresetList(); break;
            case 1: DrawSoundEventList(); break;
            case 2: DrawCategoryList(); break;
            case 3: DrawBlockSoundSetList(); break;
        }
        ImGui.EndChild();

        ImGui.SameLine();

        // Right: detail
        ImGui.BeginChild("AmbDetail", new System.Numerics.Vector2(detailWidth, contentHeight), ImGuiChildFlags.Borders);
        DrawDetail();
        ImGui.EndChild();

        ImGui.EndChild();
    }

    private void EnsureLoaded()
    {
        switch (_tab)
        {
            case 0 when _presets == null: _ = LoadPresetsAsync(); break;
            case 1 when _soundEvents == null: _ = LoadSoundEventsAsync(); break;
            case 2 when _audioCategories == null: _ = LoadCategoriesAsync(); break;
            case 3 when _blockSoundSets == null: _ = LoadBlockSoundSetsAsync(); break;
        }
    }

    // ─── List Renderers ──────────────────────────────────────────

    private void DrawPresetList()
    {
        if (_presets == null) { ImGui.TextColored(DimColor, "No data"); return; }
        foreach (var p in _presets)
        {
            string id = p.GetProperty("id").GetString() ?? "";
            if (!MatchesFilter(id)) continue;

            int priority = p.TryGetProperty("priority", out var pv) ? pv.GetInt32() : 0;
            int sounds = p.TryGetProperty("soundCount", out var sc) ? sc.GetInt32() : 0;
            bool hasBed = p.TryGetProperty("hasAmbientBed", out var hab) && hab.GetBoolean();
            bool hasMusic = p.TryGetProperty("hasMusic", out var hm) && hm.GetBoolean();

            string label = $"{id}";
            string extra = $"P:{priority} S:{sounds}{(hasBed ? " Bed" : "")}{(hasMusic ? " Music" : "")}";

            bool selected = _selectedPresetId == id;
            if (ImGui.Selectable($"{label}##preset_{id}", selected))
            {
                _selectedPresetId = id;
                _ = LoadPresetDetailAsync(id);
            }
            ImGui.SameLine();
            ImGui.TextColored(DimColor, extra);
        }
    }

    private void DrawSoundEventList()
    {
        if (_soundEvents == null) { ImGui.TextColored(DimColor, "No data"); return; }
        foreach (var s in _soundEvents)
        {
            string id = s.GetProperty("id").GetString() ?? "";
            if (!MatchesFilter(id)) continue;

            float vol = s.TryGetProperty("volume", out var v) ? v.GetSingle() : 0;
            float dist = s.TryGetProperty("maxDistance", out var d) ? d.GetSingle() : 0;
            bool loop = s.TryGetProperty("looping", out var l) && l.GetBoolean();

            if (ImGui.Selectable($"{id}##se_{id}", false))
            {
                _selectedDetail = s;
                _selectedPresetId = "";
            }
            ImGui.SameLine();
            ImGui.TextColored(DimColor, $"V:{vol:F1} D:{dist:F0}{(loop ? " Loop" : "")}");
        }
    }

    private void DrawCategoryList()
    {
        if (_audioCategories == null) { ImGui.TextColored(DimColor, "No data"); return; }
        foreach (var c in _audioCategories)
        {
            string id = c.GetProperty("id").GetString() ?? "";
            if (!MatchesFilter(id)) continue;

            float vol = c.TryGetProperty("volume", out var v) ? v.GetSingle() : 0;
            if (ImGui.Selectable($"{id}##cat_{id}", false))
            {
                _selectedDetail = c;
                _selectedPresetId = "";
            }
            ImGui.SameLine();
            ImGui.TextColored(DimColor, $"Vol:{vol:F2}");
        }
    }

    private void DrawBlockSoundSetList()
    {
        if (_blockSoundSets == null) { ImGui.TextColored(DimColor, "No data"); return; }
        foreach (var b in _blockSoundSets)
        {
            string id = b.GetProperty("id").GetString() ?? "";
            if (!MatchesFilter(id)) continue;
            ImGui.Selectable($"{id}##bss_{id}");
        }
    }

    // ─── Detail Renderer ─────────────────────────────────────────

    private void DrawDetail()
    {
        if (_selectedDetail == null || _selectedDetail.Value.ValueKind == JsonValueKind.Undefined)
        {
            ImGui.TextColored(DimColor, "Select an item");
            return;
        }

        DrawJsonRecursive(_selectedDetail.Value, 0);
    }

    private void DrawJsonRecursive(JsonElement el, int depth)
    {
        string indent = new(' ', depth * 2);
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        if (ImGui.TreeNodeEx($"{indent}{prop.Name}", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            DrawJsonRecursive(prop.Value, depth + 1);
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        ImGui.TextColored(LabelColor, $"{indent}{prop.Name}:");
                        ImGui.SameLine();
                        ImGui.TextColored(ValueColor, FormatValue(prop.Value));
                    }
                }
                break;
            case JsonValueKind.Array:
                int i = 0;
                foreach (var item in el.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        if (ImGui.TreeNodeEx($"{indent}[{i}]"))
                        {
                            DrawJsonRecursive(item, depth + 1);
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        ImGui.TextColored(ValueColor, $"{indent}  {FormatValue(item)}");
                    }
                    i++;
                }
                break;
            default:
                ImGui.TextColored(ValueColor, FormatValue(el));
                break;
        }
    }

    private static string FormatValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Number => el.TryGetInt32(out var i) ? i.ToString() : el.GetDouble().ToString("F2"),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => el.ToString()
    };

    // ─── Data Loading ────────────────────────────────────────────

    private async Task LoadPresetsAsync()
    {
        _loading = true;
        try
        {
            var json = await FetchJson("/api/ambience");
            _presets = ParseArray(json);
        }
        catch { _presets = []; }
        _loading = false;
    }

    private async Task LoadSoundEventsAsync()
    {
        _loading = true;
        try
        {
            var json = await FetchJson("/api/ambience/sounds");
            _soundEvents = ParseArray(json);
        }
        catch { _soundEvents = []; }
        _loading = false;
    }

    private async Task LoadCategoriesAsync()
    {
        _loading = true;
        try
        {
            var json = await FetchJson("/api/ambience/categories");
            _audioCategories = ParseArray(json);
        }
        catch { _audioCategories = []; }
        _loading = false;
    }

    private async Task LoadBlockSoundSetsAsync()
    {
        _loading = true;
        try
        {
            var json = await FetchJson("/api/ambience/blocksoundsets");
            _blockSoundSets = ParseArray(json);
        }
        catch { _blockSoundSets = []; }
        _loading = false;
    }

    private async Task LoadPresetDetailAsync(string id)
    {
        try
        {
            var json = await FetchJson($"/api/ambience/{Uri.EscapeDataString(id)}");
            _selectedDetail = JsonDocument.Parse(json).RootElement;
        }
        catch { _selectedDetail = null; }
    }

    private async Task<string> FetchJson(string path)
    {
        var http = new HttpClient();
        return await http.GetStringAsync($"{_services.ApiClient.BaseUrl}{path}");
    }

    private static JsonElement[] ParseArray(string json)
    {
        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
        var list = new List<JsonElement>();
        foreach (var el in doc.RootElement.EnumerateArray())
            list.Add(el);
        return list.ToArray();
    }

    private bool MatchesFilter(string text)
    {
        if (string.IsNullOrEmpty(_filter)) return true;
        return text.Contains(_filter, StringComparison.OrdinalIgnoreCase);
    }
}
