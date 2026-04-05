using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Input;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;
using HytaleAdmin.Rendering;
using HytaleAdmin.Services;
using HytaleAdmin.UI;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace HytaleAdmin.Scenes;

public class EditorScene : IGameScene
{
    private readonly ServiceContainer _services;

    private MapRenderer? _mapRenderer;
    private EntityRenderer? _entityRenderer;
    private SelectionRenderer? _selectionRenderer;
    private EditorUI? _editorUi;

    // Area selection state
    private bool _areaSelecting;
    private Vector2 _areaStart;
    private bool _wasMouseDown;

    // Right-click context menu state
    private Vector2 _rightClickWorldPos;
    private EntityDto? _rightClickEntity;
    private bool _contextMenuRequested;

    // NPC type cache for spawn menu
    private string[]? _npcTypes;
    private bool _npcTypesLoading;
    private string _npcTypeFilter = "";

    public EditorScene(ServiceContainer services)
    {
        _services = services;
    }

    public void Load(Scene rootScene, IServiceRegistry services)
    {
        _mapRenderer = new MapRenderer(_services.Game.GraphicsDevice);
        _entityRenderer = new EntityRenderer(_mapRenderer);
        _selectionRenderer = new SelectionRenderer(_mapRenderer);

        _editorUi = new EditorUI(services, _services,
            _mapRenderer, _entityRenderer, _selectionRenderer, LoadMapAsync);

        // Wire context menu delegate
        _editorUi.DrawMapContextMenu = DrawContextMenu;

        // Wire events
        _services.ApiClient.StatusChanged += msg => _editorUi.SetStatus(msg);
        _services.ApiClient.RequestLogged += (method, url) => _editorUi.Log.LogRequest(method, url);
        _services.ApiClient.ResponseLogged += (url, ok, detail) => _editorUi.Log.LogResponse(url, ok, detail);

        _services.MapData.MapUpdated += () =>
        {
            _mapRenderer?.UpdateTexture(_services.MapData);
        };

        _services.EntityData.DataUpdated += () =>
        {
            _entityRenderer?.Refresh(_services.EntityData);
        };

        // Load asset catalog and server info in background
        _ = _editorUi.LoadAssetsAsync();

        // Auto-load map on startup
        _ = LoadMapAsync();
    }

    public void Update(GameTime time)
    {
        // Flush any pending data updates on the main thread
        _services.MapData.FlushOnMainThread();
        _services.EntityData.FlushOnMainThread();

        var input = _services.Game.Input;

        // Keyboard shortcuts
        if (InputMap.IsPressed(input, InputAction.LoadMap))
        {
            _ = LoadMapAsync();
        }

        if (InputMap.IsPressed(input, InputAction.RotateAsset))
        {
            _services.Selection.RotateAsset();
        }

        if (InputMap.IsPressed(input, InputAction.Cancel))
        {
            _services.Selection.DeselectAsset();
            _selectionRenderer?.HideHover();

            // Cancel trigger area definition
            if (_editorUi?.Triggers?.IsDefiningArea == true)
                _editorUi.Triggers.IsDefiningArea = false;
        }

        // Mouse world position
        var screenSize = new Vector2(
            _services.Game.GraphicsDevice.Presenter?.BackBuffer?.Width ?? 1280,
            _services.Game.GraphicsDevice.Presenter?.BackBuffer?.Height ?? 720);
        var mouseScreen = input.MousePosition * screenSize;
        var worldPos = _mapRenderer?.ScreenToWorld(mouseScreen);

        // Hover highlight with asset footprint
        _selectionRenderer?.UpdateSelectedAsset(_services.Selection.SelectedAsset);
        if (_services.Selection.SelectedAsset != null && worldPos != null)
        {
            _selectionRenderer?.UpdateHoverHighlight(worldPos);
            UpdateHoverInfo(worldPos.Value);
        }
        else
        {
            _selectionRenderer?.HideHover();
            if (worldPos != null) UpdateHoverInfo(worldPos.Value);
        }

        // Show cursor info on map panel bottom bar
        var hovered = _services.Selection.HoveredBlock;
        if (hovered != null)
            _mapRenderer!.CursorInfoText = $"X: {hovered.X}  Z: {hovered.Z}  Y: {hovered.Y}  Block: {hovered.Block}";
        else
            _mapRenderer!.CursorInfoText = null;

        // Area selection (shift+drag for sound zones or trigger areas)
        HandleAreaSelection(input, worldPos);

        // Click-to-place or click-to-select
        HandleClick(input, worldPos);

        // Right-click context menu
        HandleRightClick(input, worldPos);
    }

    private void HandleAreaSelection(InputManager input, Vector2? worldPos)
    {
        if (worldPos == null) return;

        var asset = _services.Selection.SelectedAsset;
        bool isSoundAsset = asset?.Category == "sounds";
        bool isTriggerArea = _editorUi?.Triggers?.IsDefiningArea == true;

        bool shouldStartArea = (isSoundAsset || isTriggerArea)
            && InputMap.IsShiftDown(input)
            && input.IsMouseButtonPressed(MouseButton.Left);

        if (shouldStartArea)
        {
            _areaSelecting = true;
            _areaStart = worldPos.Value;
        }

        if (_areaSelecting && input.IsMouseButtonDown(MouseButton.Left))
        {
            _selectionRenderer?.UpdateAreaSelection(_areaStart, worldPos.Value);
        }

        if (_areaSelecting && input.IsMouseButtonReleased(MouseButton.Left))
        {
            _areaSelecting = false;
            _selectionRenderer?.HideAreaSelection();

            float minX = Math.Min(_areaStart.X, worldPos.Value.X);
            float maxX = Math.Max(_areaStart.X, worldPos.Value.X);
            float minZ = Math.Min(_areaStart.Y, worldPos.Value.Y);
            float maxZ = Math.Max(_areaStart.Y, worldPos.Value.Y);

            if (maxX - minX > 2 || maxZ - minZ > 2)
            {
                if (isTriggerArea && _editorUi?.Triggers != null)
                {
                    // Pass area to trigger panel
                    _editorUi.Triggers.PendingArea = (minX, minZ, maxX, maxZ);
                    _editorUi.Triggers.IsDefiningArea = false;
                }
                else if (isSoundAsset)
                {
                    // Original sound zone placement
                    float cx = (minX + maxX) / 2f;
                    float cz = (minZ + maxZ) / 2f;
                    var block = _services.MapData.TryGetBlock((int)cx, (int)cz);
                    float cy = block?.Y + 1 ?? 64;

                    _ = _services.ApiClient.StartAmbientAsync(new SoundAmbientRequest
                    {
                        Sound = asset!.Id,
                        World = _services.Config.WorldId,
                        X = cx, Y = cy, Z = cz,
                        MinX = minX, MinZ = minZ, MaxX = maxX, MaxZ = maxZ
                    });
                }
            }
        }
    }

    private void HandleClick(InputManager input, Vector2? worldPos)
    {
        if (worldPos == null) return;
        if (_areaSelecting) return;

        var io = ImGui.GetIO();
        bool leftReleased = input.IsMouseButtonReleased(MouseButton.Left)
                     || (!io.MouseDown[0] && _wasMouseDown);
        _wasMouseDown = io.MouseDown[0];

        if (!leftReleased) return;

        // Left-click: place asset if selected, otherwise select entity
        if (_services.Selection.SelectedAsset != null)
        {
            PlaceSelectedAsset(worldPos.Value);
        }
        else
        {
            TrySelectAtPosition(worldPos.Value);
        }
    }

    private void PlaceSelectedAsset(Vector2 worldPos)
    {
        var asset = _services.Selection.SelectedAsset!;
        var blockX = (int)MathF.Floor(worldPos.X);
        var blockZ = (int)MathF.Floor(worldPos.Y);
        var block = _services.MapData.TryGetBlock(blockX, blockZ);

        if (block == null)
        {
            _editorUi?.SetStatus("No surface data — load map first");
            return;
        }

        if (asset.Category == "sounds")
        {
            _ = _services.ApiClient.PlaySoundAsync(new SoundPlayRequest
            {
                Sound = asset.Id,
                World = _services.Config.WorldId,
                X = blockX + 0.5f, Y = block.Y + 1, Z = blockZ + 0.5f
            });
        }
        else if (asset.Category == "npcs")
        {
            _ = SpawnEntityAsync(asset.Id, blockX + 0.5f, block.Y + 1, blockZ + 0.5f);
        }
        else
        {
            _ = PlaceAssetAsync(asset.Category, asset.Id, blockX + 0.5f, block.Y, blockZ + 0.5f, asset.Rotation);
        }
    }

    private async Task SpawnEntityAsync(string type, float x, float y, float z)
    {
        var result = await _services.ApiClient.SpawnEntityAsync(new EntitySpawnRequest
        {
            Type = type,
            World = _services.Config.WorldId,
            X = x, Y = y, Z = z
        });

        if (result?.Success == true)
        {
            await _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
        }
    }

    private async Task PlaceAssetAsync(string category, string id, float x, float y, float z, int rotation = 0)
    {
        try
        {
            _editorUi?.SetStatus($"Placing {category}:{id}...");
            Console.WriteLine($"[Place] {category}:{id} at ({x:F1},{y:F1},{z:F1}) world={_services.Config.WorldId}");

            var result = await _services.ApiClient.PlaceAssetAsync(
                category, id, _services.Config.WorldId, x, y, z, rotation);

            if (result == null)
            {
                _editorUi?.SetStatus($"Place {category}:{id} — no response");
                Console.WriteLine($"[Place] null result");
            }
            else if (result.Success)
            {
                _editorUi?.SetStatus($"Placed {category}:{id} — reloading map...");
                Console.WriteLine($"[Place] success: {result.Message}");
                await _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
                await LoadMapAsync();
            }
            else
            {
                var err = result.Errors?.FirstOrDefault() ?? result.Message ?? "unknown error";
                _editorUi?.SetStatus($"Place failed: {err}");
                Console.WriteLine($"[Place] failed: {err}");
            }
        }
        catch (Exception ex)
        {
            _editorUi?.SetStatus($"Place error: {ex.Message}");
            Console.Error.WriteLine($"[Place] exception: {ex}");
        }
    }

    private void TrySelectAtPosition(Vector2 worldPos)
    {
        const float threshold = 3f;

        foreach (var p in _services.EntityData.Players)
        {
            var dist = Vector2.Distance(new(p.X, p.Z), worldPos);
            if (dist < threshold) { _services.Selection.SelectPlayer(p); return; }
        }

        foreach (var e in _services.EntityData.Entities)
        {
            var dist = Vector2.Distance(new(e.X, e.Z), worldPos);
            if (dist < threshold) { _services.Selection.SelectEntity(e); return; }
        }

        foreach (var z in _services.EntityData.SoundZones)
        {
            if (worldPos.X >= z.MinX && worldPos.X <= z.MaxX &&
                worldPos.Y >= z.MinZ && worldPos.Y <= z.MaxZ)
            {
                _services.Selection.SelectZone(z);
                return;
            }
        }
    }

    private void HandleRightClick(InputManager input, Vector2? worldPos)
    {
        if (worldPos == null) return;

        if (input.IsMouseButtonPressed(MouseButton.Right))
        {
            _rightClickWorldPos = worldPos.Value;

            // Hit-test: find entity under cursor
            _rightClickEntity = null;
            const float threshold = 3f;
            foreach (var e in _services.EntityData.Entities)
            {
                var dist = Vector2.Distance(new(e.X, e.Z), worldPos.Value);
                if (dist < threshold)
                {
                    _rightClickEntity = e;
                    break;
                }
            }

            _contextMenuRequested = true;

            // Ensure schemas are loaded
            if (!_services.PluginSchemas.IsLoaded)
            {
                _ = _services.PluginSchemas.RefreshAsync().ContinueWith(t =>
                {
                    _services.EntityData.SpatialPluginIds =
                        new List<string>(_services.PluginSchemas.GetSpatialPluginIds());
                    _entityRenderer?.SetPresenters(_services.PluginSchemas.GetAllPresenters());
                });
            }
        }
    }

    /// <summary>
    /// Called from EditorUI inside the map ImGui window to render the context menu popup.
    /// </summary>
    public void DrawContextMenu()
    {
        if (_contextMenuRequested)
        {
            ImGui.OpenPopup("map_context_menu");
            _contextMenuRequested = false;
        }

        if (ImGui.BeginPopup("map_context_menu"))
        {
            // Header
            if (_rightClickEntity != null)
            {
                var name = !string.IsNullOrEmpty(_rightClickEntity.Name)
                    ? _rightClickEntity.Name : _rightClickEntity.Type ?? "Entity";
                ImGui.TextDisabled($"Entity: {name}");
            }
            else
            {
                ImGui.TextDisabled($"Position: {_rightClickWorldPos.X:F1}, {_rightClickWorldPos.Y:F1}");
            }
            ImGui.Separator();

            // Copy entity to clipboard
            if (_rightClickEntity != null)
            {
                if (ImGui.MenuItem("Copy to Clipboard"))
                {
                    _services.Clipboard.CopyEntity(_rightClickEntity);
                    _editorUi?.SetStatus($"Copied: {_rightClickEntity.Name ?? _rightClickEntity.Type}");
                }
                ImGui.Separator();
            }

            // Create location at this position
            if (ImGui.MenuItem("Create Location Here"))
            {
                _ = CreateLocationAtAsync(_rightClickWorldPos);
            }

            // Spawn NPC submenu
            DrawSpawnNpcMenu();

            ImGui.Separator();

            // Plugin spatial actions
            if (_services.PluginSchemas.IsLoaded)
            {
                var actions = _services.PluginSchemas.GetSpatialActions();
                if (actions.Count == 0)
                {
                    ImGui.TextDisabled("No spatial actions available");
                }
                else
                {
                    foreach (var sa in actions)
                    {
                        if (ImGui.MenuItem($"{sa.PluginName}: {sa.Action.Label}"))
                        {
                            _ = ExecuteSpatialActionAsync(sa);
                        }
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("Loading plugins...");
            }

            ImGui.EndPopup();
        }
    }

    private void DrawSpawnNpcMenu()
    {
        if (ImGui.BeginMenu("Spawn NPC"))
        {
            // Load types on first open
            if (_npcTypes == null && !_npcTypesLoading)
            {
                _npcTypesLoading = true;
                _ = Task.Run(async () =>
                {
                    _npcTypes = await _services.ApiClient.GetEntityTypesAsync(_services.Config.WorldId);
                    _npcTypesLoading = false;
                });
            }

            if (_npcTypesLoading)
            {
                ImGui.TextDisabled("Loading NPC types...");
            }
            else if (_npcTypes == null || _npcTypes.Length == 0)
            {
                ImGui.TextDisabled("No NPC types available");
            }
            else
            {
                // Search filter
                ImGui.SetNextItemWidth(200);
                ImGui.InputText("##npcTypeFilter", ref _npcTypeFilter, 128);

                ImGui.Separator();

                // Show filtered types (limit to 20 to avoid huge menus)
                int shown = 0;
                var filterLower = _npcTypeFilter.ToLowerInvariant();
                foreach (var npcType in _npcTypes)
                {
                    if (!string.IsNullOrEmpty(_npcTypeFilter) &&
                        !npcType.Contains(filterLower, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (ImGui.MenuItem(npcType))
                    {
                        _ = SpawnNpcAtAsync(npcType, _rightClickWorldPos);
                    }

                    if (++shown >= 20)
                    {
                        ImGui.TextDisabled($"... and {_npcTypes.Length - shown} more (use filter)");
                        break;
                    }
                }
            }

            ImGui.EndMenu();
        }
    }

    private async Task SpawnNpcAtAsync(string npcType, System.Numerics.Vector2 worldPos)
    {
        var blockX = (int)MathF.Floor(worldPos.X);
        var blockZ = (int)MathF.Floor(worldPos.Y);
        var block = _services.MapData.TryGetBlock(blockX, blockZ);

        float y = block != null ? block.Y + 1 : 0;
        bool needsServerY = block == null;

        _editorUi?.SetStatus($"Spawning {npcType}...");

        // If no map data, ask server for surface height
        if (needsServerY)
        {
            try
            {
                var resp = await _services.ApiClient.GetSurfaceAsync(
                    _services.Config.WorldId, blockX, blockZ, 0);
                if (resp?.Surface != null && resp.Surface.Length > 0)
                    y = resp.Surface[0].Y + 1;
                else
                    y = 64;
            }
            catch { y = 64; }
        }

        var result = await _services.ApiClient.SpawnEntityAsync(new EntitySpawnRequest
        {
            Type = npcType,
            World = _services.Config.WorldId,
            X = blockX + 0.5f, Y = y, Z = blockZ + 0.5f
        });

        if (result?.Success == true)
        {
            _editorUi?.SetStatus($"Spawned {npcType} at ({blockX}, {block.Y + 1}, {blockZ})");
            await _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
        }
        else
        {
            _editorUi?.SetStatus($"Spawn failed: {result?.Error ?? "Unknown error"}");
        }
    }

    private async Task CreateLocationAtAsync(System.Numerics.Vector2 worldPos)
    {
        string id = $"location_{DateTime.UtcNow.Ticks % 100000}";
        try
        {
            var result = await _services.ApiClient.ExecutePluginActionAsync(
                "hyadventure", "createLocation", null,
                new Dictionary<string, string>
                {
                    ["id"] = id,
                    ["label"] = $"Location ({worldPos.X:F0}, {worldPos.Y:F0})",
                    ["x"] = worldPos.X.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                    ["y"] = "64",
                    ["z"] = worldPos.Y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                    ["radius"] = "5.0",
                });
            _editorUi?.SetStatus(result?.Success == true
                ? $"Location created: {id}"
                : $"Failed: {string.Join(", ", result?.Errors ?? ["Unknown error"])}");
        }
        catch (Exception ex)
        {
            _editorUi?.SetStatus($"Error: {ex.Message}");
        }
    }

    private async Task ExecuteSpatialActionAsync(Services.SpatialAction sa)
    {
        float x = _rightClickWorldPos.X;
        float z = _rightClickWorldPos.Y; // Note: worldPos.Y is Z in world space
        float defaultRadius = 20f;
        var surfaceBlock = _services.MapData.TryGetBlock((int)MathF.Floor(x), (int)MathF.Floor(z));
        float surfaceY = surfaceBlock?.Y ?? 70f;

        var parameters = new Dictionary<string, string>();
        var spatialFields = sa.SpatialFields;

        // Auto-fill from schema-declared spatial field semantics
        foreach (var group in sa.Action.Groups)
        {
            foreach (var field in group.Fields)
            {
                if (spatialFields.TryGetValue(field.Id, out var semantic))
                {
                    parameters[field.Id] = ResolveSemantic(semantic, x, z, surfaceY);
                }
                else if (field.EnumValues is { Length: > 0 })
                {
                    parameters[field.Id] = field.EnumValues[0];
                }
            }
        }

        var result = await _services.ApiClient.ExecutePluginActionAsync(
            sa.PluginId, sa.Action.Id, null, parameters);

        if (result?.Success == true)
        {
            _editorUi?.SetStatus($"{sa.Action.Label} created successfully");
            await _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
        }
    }

    private string ResolveSemantic(string semantic, float x, float z, float surfaceY)
    {
        if (semantic == "x") return x.ToString("F1");
        if (semantic == "z") return z.ToString("F1");
        if (semantic == "surfaceY") return surfaceY.ToString("F1");
        if (semantic == "worldId") return _services.Config.WorldId;
        if (semantic == "autoName")
            return _rightClickEntity != null
                ? $"Region: {_rightClickEntity.Name ?? _rightClickEntity.Type}"
                : $"Region at {x:F0},{z:F0}";
        if (semantic.StartsWith("default:")) return semantic[8..];
        return "";
    }

    private void UpdateHoverInfo(Vector2 worldPos)
    {
        var blockX = (int)MathF.Floor(worldPos.X);
        var blockZ = (int)MathF.Floor(worldPos.Y);
        var block = _services.MapData.TryGetBlock(blockX, blockZ);
        _services.Selection.HoveredBlock = block;
    }

    private async Task LoadMapAsync()
    {
        var config = _services.Config;
        var surface = await _services.ApiClient.GetSurfaceAsync(
            config.WorldId, config.CenterX, config.CenterZ, config.Radius);
        if (surface != null)
        {
            _services.MapData.Merge(surface);
            _mapRenderer?.LookAt(config.CenterX, config.CenterZ);
            await _services.EntityData.PollAsync(_services.ApiClient, config);
            _services.EntityData.StartPolling(_services.ApiClient, config);
        }
    }

    public void Unload(Scene rootScene)
    {
        _services.EntityData.StopPolling();
        _mapRenderer?.Clear();
        _entityRenderer?.Clear();
        _selectionRenderer?.Clear();
    }
}
