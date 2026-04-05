using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Rendering;
using HytaleAdmin.Services;
using HytaleAdmin.UI.Components.Forms;

namespace HytaleAdmin.UI.Components;

/// <summary>
/// Multi-step quest creation wizard.
/// Step 1: Name + Category
/// Step 2: Task type + target
/// Step 3: Pick NPC quest giver from map
/// Creates everything server-side, refreshes graph.
/// </summary>
public class QuestWizard
{
    private readonly HytaleApiClient _client;
    private readonly EntityDataService _entityData;
    private readonly MapRenderer _mapRenderer;

    private bool _open;
    private int _step;
    private bool _executing;
    private string? _error;

    // Step 1
    private string _questName = "";
    private int _categoryIdx;
    private static readonly string[] Categories = ["main_quest", "side_quest", "daily", "custom"];
    private static readonly string[] CategoryLabels = ["Main Quest", "Side Quest", "Daily", "Custom"];

    // Step 2
    private int _taskTypeIdx;
    private IEntityForm? _taskForm;
    private int _lastTaskTypeIdx = -1;
    private static readonly string[] TaskTypes = ["GATHER", "CRAFT", "KILL", "REACH_LOCATION", "USE_ENTITY", "BOUNTY"];
    private static readonly string[] TaskTypeLabels = ["Gather Items", "Craft Items", "Kill Enemies", "Reach Location", "Talk to NPC", "Bounty Hunt"];

    // Step 3
    private MapPickerDialog? _mapPicker;
    private EntityDto? _pickedNpc;
    private bool _skipNpc;

    // Spawn new NPC sub-flow
    private bool _showSpawnFlow;
    private string[]? _npcTypes;
    private bool _npcTypesLoading;
    private string _npcTypeFilter = "";
    private string? _selectedSpawnType;

    // Result
    public Action? OnQuestCreated;
    private const string PluginId = "hyadventure";

    public QuestWizard(HytaleApiClient client, EntityDataService entityData, MapRenderer mapRenderer)
    {
        _client = client;
        _entityData = entityData;
        _mapRenderer = mapRenderer;
        _mapPicker = new MapPickerDialog(mapRenderer);
    }

    public bool IsOpen => _open;

    public void Open()
    {
        _open = true;
        _step = 0;
        _executing = false;
        _error = null;
        _questName = "";
        _categoryIdx = 0;
        _taskTypeIdx = 0;
        _taskForm = null;
        _lastTaskTypeIdx = -1;
        _pickedNpc = null;
        _skipNpc = false;
    }

    public void Draw()
    {
        if (!_open) return;

        ImGui.SetNextWindowSize(new Vector2(420, 0), ImGuiCond.Once);

        bool open = true;
        if (ImGui.Begin("Quest Wizard##QuestWizard", ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            // Progress indicator
            var stepLabels = new[] { "Quest Info", "Task Setup", "Quest Giver" };
            for (int i = 0; i < stepLabels.Length; i++)
            {
                if (i > 0) { ImGui.SameLine(); ImGui.TextColored(new Vector4(0.4f, 0.4f, 0.5f, 1f), ">"); ImGui.SameLine(); }
                var color = i == _step ? new Vector4(0.40f, 0.70f, 0.95f, 1f)
                    : i < _step ? new Vector4(0.31f, 0.80f, 0.40f, 1f)
                    : new Vector4(0.45f, 0.45f, 0.55f, 1f);
                ImGui.TextColored(color, stepLabels[i]);
            }
            ImGui.Separator();
            ImGui.Spacing();

            switch (_step)
            {
                case 0: DrawStep1(); break;
                case 1: DrawStep2(); break;
                case 2: DrawStep3(); break;
            }

            if (_error != null)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.3f, 1f), _error);
            }

            ImGui.Spacing();
            DrawButtons();
        }
        ImGui.End();

        _mapPicker?.Draw();

        if (!open) _open = false;
    }

    private void DrawStep1()
    {
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Quest Name");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##wiz_name", ref _questName, 128);

        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Category");
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo("##wiz_category", ref _categoryIdx, CategoryLabels, CategoryLabels.Length);
    }

    private void DrawStep2()
    {
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Task Type");
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##wiz_tasktype", ref _taskTypeIdx, TaskTypeLabels, TaskTypeLabels.Length))
        {
            _lastTaskTypeIdx = -1; // force form recreation
        }

        // Create/swap form when task type changes
        if (_lastTaskTypeIdx != _taskTypeIdx)
        {
            _lastTaskTypeIdx = _taskTypeIdx;
            _taskForm = TaskFormFactory.Create(TaskTypes[_taskTypeIdx], _client);
        }

        ImGui.Spacing();
        _taskForm?.Draw();
    }

    private void DrawStep3()
    {
        if (_pickedNpc != null)
        {
            ImGui.TextColored(new Vector4(0.31f, 0.80f, 0.40f, 1f),
                $"Quest Giver: {_pickedNpc.Name ?? _pickedNpc.Type}");
            ImGui.Spacing();
            if (ImGui.Button("Change##wiz_change", new Vector2(-1, 0)))
            {
                _pickedNpc = null;
                _showSpawnFlow = false;
                _selectedSpawnType = null;
            }
        }
        else if (_showSpawnFlow)
        {
            DrawSpawnNewNpcFlow();
        }
        else
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Choose a quest giver NPC:");
            ImGui.Spacing();

            // Option 1: pick existing from map
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.77f, 0.29f, 0.55f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
            if (ImGui.Button("Pick Existing NPC on Map##wiz_pick", new Vector2(-1, 0)))
            {
                _mapPicker?.OpenEntityPicker("Pick Quest Giver", _entityData, null, entity =>
                {
                    _pickedNpc = entity;
                });
            }
            ImGui.PopStyleColor(2);

            ImGui.Spacing();

            // Option 2: spawn new
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.40f, 0.70f, 0.95f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
            if (ImGui.Button("Spawn New NPC##wiz_spawn", new Vector2(-1, 0)))
            {
                _showSpawnFlow = true;
                _selectedSpawnType = null;
                if (_npcTypes == null && !_npcTypesLoading)
                {
                    _npcTypesLoading = true;
                    _ = Task.Run(async () =>
                    {
                        _npcTypes = await _client.GetEntityTypesAsync("default");
                        _npcTypesLoading = false;
                    });
                }
            }
            ImGui.PopStyleColor(2);

            ImGui.Spacing();
            if (ImGui.Checkbox("Skip (no quest giver)##wiz_skip", ref _skipNpc)) { }
        }
    }

    private void DrawSpawnNewNpcFlow()
    {
        if (_selectedSpawnType == null)
        {
            // Step 3a: pick NPC type from list
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Select NPC type to spawn:");
            ImGui.Spacing();

            if (_npcTypesLoading)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.6f, 1f), "Loading NPC types...");
                return;
            }

            if (_npcTypes == null || _npcTypes.Length == 0)
            {
                ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.3f, 1f), "No NPC types available");
                if (ImGui.Button("Back##wiz_spawn_back", new Vector2(-1, 0)))
                    _showSpawnFlow = false;
                return;
            }

            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##wiz_npcfilter", ref _npcTypeFilter, 128);
            ImGui.Spacing();

            if (ImGui.BeginChild("NpcTypeList##wiz", new Vector2(-1, 150)))
            {
                var filterLower = _npcTypeFilter.ToLowerInvariant();
                foreach (var npcType in _npcTypes)
                {
                    if (!string.IsNullOrEmpty(_npcTypeFilter) &&
                        !npcType.Contains(filterLower, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (ImGui.Selectable(npcType))
                        _selectedSpawnType = npcType;
                }
            }
            ImGui.EndChild();

            ImGui.Spacing();
            if (ImGui.Button("Cancel##wiz_spawn_cancel", new Vector2(-1, 0)))
                _showSpawnFlow = false;
        }
        else
        {
            // Step 3b: pick location on map to spawn
            ImGui.TextColored(new Vector4(0.40f, 0.70f, 0.95f, 1f), $"Spawning: {_selectedSpawnType}");
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Pick a location on the map to spawn this NPC.");
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.40f, 0.70f, 0.95f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
            if (ImGui.Button("Pick Spawn Location##wiz_spawnloc", new Vector2(-1, 0)))
            {
                var spawnType = _selectedSpawnType;
                _mapPicker?.Open("Pick Spawn Location", (x, z) =>
                {
                    SpawnNpcAndUse(spawnType, x, z);
                });
            }
            ImGui.PopStyleColor(2);

            ImGui.Spacing();
            if (ImGui.Button("Back (change type)##wiz_spawn_back2", new Vector2(-1, 0)))
                _selectedSpawnType = null;
        }
    }

    private void SpawnNpcAndUse(string npcType, float worldX, float worldZ)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Get surface Y
                float y = 64;
                try
                {
                    var resp = await _client.GetSurfaceAsync("default", (int)worldX, (int)worldZ, 0);
                    if (resp?.Surface is { Length: > 0 })
                        y = resp.Surface[0].Y + 1;
                }
                catch { }

                // Spawn the NPC
                var spawnResult = await _client.SpawnEntityAsync(new Models.Api.EntitySpawnRequest
                {
                    Type = npcType,
                    World = "default",
                    X = worldX + 0.5, Y = y, Z = worldZ + 0.5,
                });

                if (spawnResult?.Success == true)
                {
                    // Create a fake EntityDto to represent the spawned NPC
                    _pickedNpc = new EntityDto
                    {
                        Type = npcType,
                        Name = npcType,
                        X = (float)(worldX + 0.5),
                        Y = (float)y,
                        Z = (float)(worldZ + 0.5),
                    };
                    _showSpawnFlow = false;
                    _selectedSpawnType = null;
                }
                else
                {
                    _error = $"Spawn failed: {spawnResult?.Error ?? "Unknown"}";
                }
            }
            catch (Exception ex)
            {
                _error = $"Spawn error: {ex.Message}";
            }
        });
    }

    private void DrawButtons()
    {
        float btnW = 100;
        float spacing = ImGui.GetStyle().ItemSpacing.X;

        // Back button
        if (_step > 0)
        {
            if (ImGui.Button("Back##wiz_back", new Vector2(btnW, 0)))
                _step--;
            ImGui.SameLine();
        }

        // Position next/finish on the right
        float rightX = ImGui.GetContentRegionAvail().X - btnW;
        if (_step > 0) rightX -= btnW + spacing; // account for Back button
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + rightX);

        if (_step < 2)
        {
            bool canNext = _step == 0
                ? !string.IsNullOrWhiteSpace(_questName)
                : _step == 1 ? (_taskForm?.IsValid ?? false) : true;
            if (!canNext) ImGui.BeginDisabled();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.40f, 0.70f, 0.95f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
            if (ImGui.Button("Next##wiz_next", new Vector2(btnW, 0)))
            {
                _error = null;
                _step++;
            }
            ImGui.PopStyleColor(2);
            if (!canNext) ImGui.EndDisabled();
        }
        else
        {
            bool canFinish = _pickedNpc != null || _skipNpc;
            if (!canFinish) ImGui.BeginDisabled();
            if (_executing) ImGui.BeginDisabled();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.31f, 0.80f, 0.40f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
            if (ImGui.Button(_executing ? "Creating..." : "Finish##wiz_finish", new Vector2(btnW, 0)))
                ExecuteWizard();
            ImGui.PopStyleColor(2);
            if (_executing) ImGui.EndDisabled();
            if (!canFinish) ImGui.EndDisabled();
        }
    }

    private void ExecuteWizard()
    {
        _executing = true;
        _error = null;

        _ = Task.Run(async () =>
        {
            try
            {
                // 1. Create quest — merge wizard fields with task form values
                var questParams = new Dictionary<string, string>
                {
                    ["name"] = _questName,
                    ["category"] = Categories[_categoryIdx],
                };

                // Add task form values (type, target, count, etc.)
                if (_taskForm != null)
                {
                    foreach (var (k, v) in _taskForm.GetValues())
                    {
                        if (k == "type") questParams["taskType"] = v;
                        else if (k == "count") questParams["taskCount"] = v;
                        else if (k is "blockTagOrItemId" or "npcGroupId" or "npcId" or "targetLocationId" or "taskId")
                            questParams["taskTarget"] = v;
                    }
                }

                var questResult = await _client.ExecutePluginActionAsync(PluginId, "createFullQuest", null, questParams);

                if (questResult?.Success != true)
                {
                    _error = $"Failed: {string.Join(", ", questResult?.Errors ?? ["Unknown"])}";
                    _executing = false;
                    return;
                }

                string questId = questResult.EntityId ?? "";

                // 2. Assign NPC if picked
                if (_pickedNpc != null && !string.IsNullOrEmpty(questId))
                {
                    string npcRole = _pickedNpc.Type ?? "";
                    string npcId = $"npc_{System.DateTime.UtcNow.Ticks % 100000}";

                    await _client.ExecutePluginActionAsync(PluginId, "createNpcAssignment", null,
                        new Dictionary<string, string>
                        {
                            ["id"] = npcId,
                            ["npcRole"] = npcRole,
                            ["assignmentType"] = "QUEST_GIVER",
                        });

                    await _client.ExecutePluginActionAsync(PluginId, "setQuestGiver", null,
                        new Dictionary<string, string>
                        {
                            ["questLineId"] = questId,
                            ["npcAssignmentId"] = npcId,
                        });
                }

                _open = false;
                OnQuestCreated?.Invoke();
            }
            catch (Exception ex)
            {
                _error = $"Error: {ex.Message}";
            }
            finally
            {
                _executing = false;
            }
        });
    }
}
