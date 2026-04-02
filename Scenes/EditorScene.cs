using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Input;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;
using HytaleAdmin.Rendering;
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

        // Hover highlight
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
        if (_mapRenderer is { IsPanning: true }) return;

        // Use both Stride and ImGui mouse detection for robustness
        var io = ImGui.GetIO();
        bool released = input.IsMouseButtonReleased(MouseButton.Left)
                     || (!io.MouseDown[0] && _wasMouseDown);
        _wasMouseDown = io.MouseDown[0];

        if (!released) return;
        if (_mapRenderer != null && _mapRenderer.PanDistance > 5) return;

        var asset = _services.Selection.SelectedAsset;

        if (asset != null)
        {
            var blockX = (int)MathF.Floor(worldPos.Value.X);
            var blockZ = (int)MathF.Floor(worldPos.Value.Y);
            var block = _services.MapData.TryGetBlock(blockX, blockZ);

            if (block == null)
            {
                _editorUi?.SetStatus("No surface data here — load map first");
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
            else if (asset.Category == "prefabs")
            {
                _editorUi?.Log.Log($"Click → place prefab at ({blockX}, {block.Y + 1}, {blockZ})");
                _ = PlaceAssetAsync("prefabs", asset.Id, blockX + 0.5f, block.Y + 1, blockZ + 0.5f);
            }
            else
            {
                _ = PlaceAssetAsync(asset.Category, asset.Id, blockX + 0.5f, block.Y + 1, blockZ + 0.5f);
            }
        }
        else
        {
            TrySelectAtPosition(worldPos.Value);
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

    private async Task PlaceAssetAsync(string category, string id, float x, float y, float z)
    {
        _editorUi?.SetStatus($"Placing {category}: {id}...");
        var result = await _services.ApiClient.PlaceAsync(new PlaceRequest
        {
            Category = category, Id = id,
            World = _services.Config.WorldId,
            X = x, Y = y, Z = z
        });

        if (result?.Success == true)
        {
            await _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
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
