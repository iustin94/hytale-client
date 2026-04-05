using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI.Components.Forms;

/// <summary>Factory for creating the right task form based on task type.</summary>
public static class TaskFormFactory
{
    public static IEntityForm Create(string taskType, HytaleApiClient client)
    {
        return taskType switch
        {
            "GATHER" => new GatherTaskForm(client),
            "CRAFT" => new CraftTaskForm(client),
            "KILL" => new KillTaskForm(client),
            "KILL_SPAWN_MARKER" => new KillSpawnMarkerTaskForm(client),
            "REACH_LOCATION" => new ReachLocationTaskForm(client),
            "USE_ENTITY" => new UseEntityTaskForm(client),
            "BOUNTY" => new BountyTaskForm(client),
            _ => new GatherTaskForm(client),
        };
    }
}

/// <summary>GATHER: collect N of an item.</summary>
public class GatherTaskForm : IEntityForm
{
    private readonly SearchableDropdown _itemPicker;
    private int _count = 5;

    public GatherTaskForm(HytaleApiClient client)
    {
        _itemPicker = new SearchableDropdown("gather_item", async () =>
        {
            var entities = await client.GetAssetEntitiesAllPagesAsync("items");
            return entities.Select(e => e.Label).Distinct().OrderBy(s => s).ToArray();
        });
    }

    public bool IsValid => _itemPicker.HasSelection && _count > 0;

    public void Draw()
    {
        _itemPicker.Draw("Item to gather");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Count");
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##gather_count", ref _count);
        if (_count < 1) _count = 1;
    }

    public Dictionary<string, string> GetValues() => new()
    {
        ["type"] = "GATHER",
        ["blockTagOrItemId"] = _itemPicker.Selected,
        ["count"] = _count.ToString(),
    };

    public void Reset() { _itemPicker.Reset(); _count = 5; }
}

/// <summary>CRAFT: craft N of an item.</summary>
public class CraftTaskForm : IEntityForm
{
    private readonly SearchableDropdown _itemPicker;
    private int _count = 1;

    public CraftTaskForm(HytaleApiClient client)
    {
        _itemPicker = new SearchableDropdown("craft_item", async () =>
        {
            var entities = await client.GetAssetEntitiesAllPagesAsync("items");
            return entities.Select(e => e.Label).Distinct().OrderBy(s => s).ToArray();
        });
    }

    public bool IsValid => _itemPicker.HasSelection && _count > 0;

    public void Draw()
    {
        _itemPicker.Draw("Item to craft");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Count");
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##craft_count", ref _count);
        if (_count < 1) _count = 1;
    }

    public Dictionary<string, string> GetValues() => new()
    {
        ["type"] = "CRAFT",
        ["blockTagOrItemId"] = _itemPicker.Selected,
        ["count"] = _count.ToString(),
    };

    public void Reset() { _itemPicker.Reset(); _count = 1; }
}

/// <summary>KILL: defeat N enemies from an NPC group.</summary>
public class KillTaskForm : IEntityForm
{
    private readonly SearchableDropdown _groupPicker;
    private int _count = 10;

    public KillTaskForm(HytaleApiClient client)
    {
        _groupPicker = new SearchableDropdown("kill_group", async () =>
        {
            var entities = await client.GetAssetEntitiesAllPagesAsync("npcs");
            return entities.Select(e => e.Label).Distinct().OrderBy(s => s).ToArray();
        });
    }

    public bool IsValid => _groupPicker.HasSelection && _count > 0;

    public void Draw()
    {
        _groupPicker.Draw("NPC Group");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Count");
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##kill_count", ref _count);
        if (_count < 1) _count = 1;
    }

    public Dictionary<string, string> GetValues() => new()
    {
        ["type"] = "KILL",
        ["npcGroupId"] = _groupPicker.Selected,
        ["count"] = _count.ToString(),
    };

    public void Reset() { _groupPicker.Reset(); _count = 10; }
}

/// <summary>KILL_SPAWN_MARKER: clear enemies at marked locations.</summary>
public class KillSpawnMarkerTaskForm : IEntityForm
{
    private readonly SearchableDropdown _groupPicker;
    private int _count = 5;
    private string _spawnMarkerIds = "";
    private float _radius = 10f;

    public KillSpawnMarkerTaskForm(HytaleApiClient client)
    {
        _groupPicker = new SearchableDropdown("ksm_group", async () =>
        {
            var entities = await client.GetAssetEntitiesAllPagesAsync("npcs");
            return entities.Select(e => e.Label).Distinct().OrderBy(s => s).ToArray();
        });
    }

    public bool IsValid => _groupPicker.HasSelection;

    public void Draw()
    {
        _groupPicker.Draw("NPC Group");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Count");
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##ksm_count", ref _count);
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Spawn Marker IDs (comma-separated)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##ksm_markers", ref _spawnMarkerIds, 256);
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Radius");
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("##ksm_radius", ref _radius, 1f, 5f, "%.1f");
    }

    public Dictionary<string, string> GetValues() => new()
    {
        ["type"] = "KILL_SPAWN_MARKER",
        ["npcGroupId"] = _groupPicker.Selected,
        ["count"] = _count.ToString(),
        ["spawnMarkerIds"] = _spawnMarkerIds,
        ["radius"] = _radius.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
    };

    public void Reset() { _groupPicker.Reset(); _count = 5; _spawnMarkerIds = ""; _radius = 10f; }
}

/// <summary>REACH_LOCATION: reach a specific location.</summary>
public class ReachLocationTaskForm : IEntityForm
{
    private readonly SearchableDropdown _locationPicker;
    private readonly HytaleApiClient _client;

    /// <summary>
    /// Optional callback to open a map picker for creating a new location.
    /// Set by the caller (wizard, graph, etc.) to wire up the map dialog.
    /// Callback receives (locationId) after creation so the picker can select it.
    /// </summary>
    public Action<Action<string>>? OnCreateLocation { get; set; }

    public ReachLocationTaskForm(HytaleApiClient client)
    {
        _client = client;
        _locationPicker = new SearchableDropdown("reach_loc", LoadLocations);
    }

    private async Task<string[]> LoadLocations()
    {
        var resp = await _client.GetPluginEntitiesAsync("hyadventure");
        if (resp?.Data == null) return [];
        return resp.Data
            .Where(e => e.Id.StartsWith("loc:"))
            .Select(e => e.Label)
            .OrderBy(s => s)
            .ToArray();
    }

    public bool IsValid => _locationPicker.HasSelection;

    public void Draw()
    {
        // Label row with "+" button
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Target Location");
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.36f, 0.55f, 0.85f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
        if (ImGui.SmallButton("+##add_loc"))
        {
            OnCreateLocation?.Invoke(newLocId =>
            {
                _locationPicker.SetSelected(newLocId);
                _locationPicker.Reload();
            });
        }
        ImGui.PopStyleColor(2);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Create new location on map");

        // Dropdown (draws its own label as empty since we already drew it)
        _locationPicker.Draw("");
    }

    public Dictionary<string, string> GetValues() => new()
    {
        ["type"] = "REACH_LOCATION",
        ["targetLocationId"] = _locationPicker.Selected,
        ["count"] = "1",
    };

    public void Reset() => _locationPicker.Reset();
}

/// <summary>USE_ENTITY: interact with an NPC. User picks a dialog, task ID is auto-generated.</summary>
public class UseEntityTaskForm : IEntityForm
{
    private readonly HytaleApiClient _client;
    private readonly SearchableDropdown _dialogPicker;
    private string _npcDisplayName = "";
    private string _dialogText = "";
    private bool _createInline;

    /// <summary>
    /// Optional callback to create a new dialog on the fly.
    /// Caller wires this to their dialog creation mechanism.
    /// Callback receives (dialogId) after creation.
    /// </summary>
    public Action<Action<string>>? OnCreateDialog { get; set; }

    public UseEntityTaskForm(HytaleApiClient client)
    {
        _client = client;
        _dialogPicker = new SearchableDropdown("ue_dialog", async () =>
        {
            var resp = await client.GetPluginEntitiesAsync("hyadventure");
            if (resp?.Data == null) return [];
            return resp.Data
                .Where(e => e.Id.StartsWith("dlg:"))
                .Select(e => e.Label)
                .OrderBy(s => s)
                .ToArray();
        });
    }

    public bool IsValid => _dialogPicker.HasSelection ||
        (_createInline && (!string.IsNullOrWhiteSpace(_npcDisplayName) || !string.IsNullOrWhiteSpace(_dialogText)));

    public void Draw()
    {
        // Label row with "+" button
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Dialog");
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.83f, 0.66f, 0.26f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
        if (ImGui.SmallButton("+##add_dlg"))
        {
            _createInline = true;
        }
        ImGui.PopStyleColor(2);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Create new dialog inline");

        // Dialog picker (no label — we drew it above)
        _dialogPicker.Draw("");

        // Inline dialog creation
        if (_createInline && !_dialogPicker.HasSelection)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.83f, 0.66f, 0.26f, 1f), "New Dialog");

            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "NPC Display Name");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##ue_npcname", ref _npcDisplayName, 256);

            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Dialog Text");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##ue_dlgtext", ref _dialogText, 512);
        }
    }

    public Dictionary<string, string> GetValues()
    {
        string taskId = "interact_" + DateTime.UtcNow.Ticks % 100000;

        if (_dialogPicker.HasSelection)
        {
            // Using existing dialog — keys come from server, just pass the dialog reference
            return new()
            {
                ["type"] = "USE_ENTITY",
                ["taskId"] = taskId,
                ["count"] = "1",
                // The dialog's name/text keys are resolved server-side from the dialog entity
                ["dialogEntityNameKey"] = _dialogPicker.Selected + "_name",
                ["dialogKey"] = _dialogPicker.Selected + "_text",
            };
        }
        else
        {
            // Inline creation — pass display text, server generates keys
            return new()
            {
                ["type"] = "USE_ENTITY",
                ["taskId"] = taskId,
                ["count"] = "1",
                ["dialogEntityNameKey"] = _npcDisplayName,
                ["dialogKey"] = _dialogText,
                ["_createDialog"] = "true",
                ["_dialogNpcName"] = _npcDisplayName,
                ["_dialogText"] = _dialogText,
            };
        }
    }

    public void Reset()
    {
        _dialogPicker.Reset();
        _npcDisplayName = "";
        _dialogText = "";
        _createInline = false;
    }
}

/// <summary>BOUNTY: hunt a specific NPC.</summary>
public class BountyTaskForm : IEntityForm
{
    private readonly SearchableDropdown _npcPicker;

    public BountyTaskForm(HytaleApiClient client)
    {
        _npcPicker = new SearchableDropdown("bounty_npc", async () =>
        {
            var entities = await client.GetAssetEntitiesAllPagesAsync("npcs");
            return entities.Select(e => e.Label).Distinct().OrderBy(s => s).ToArray();
        });
    }

    public bool IsValid => _npcPicker.HasSelection;

    public void Draw()
    {
        _npcPicker.Draw("Bounty Target NPC");
    }

    public Dictionary<string, string> GetValues() => new()
    {
        ["type"] = "BOUNTY",
        ["npcId"] = _npcPicker.Selected,
        ["count"] = "1",
    };

    public void Reset() => _npcPicker.Reset();
}
