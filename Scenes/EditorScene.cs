using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Input;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;
using HytaleAdmin.Rendering;
using HytaleAdmin.Services;
using HytaleAdmin.UI;
using HytaleAdmin.UI.CanvasView;
using HytaleAdmin.UI.CanvasView.Adapters;
using HytaleAdmin.UI.CanvasView.Presenters;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace HytaleAdmin.Scenes;

/// <summary>
/// Thin scene — wires services, renderers, and UI.
/// All logic delegated to dedicated services.
/// </summary>
public class EditorScene : IGameScene
{
    private readonly ServiceContainer _services;

    private MapRenderer? _mapRenderer;
    private EntityRenderer? _entityRenderer;
    private SelectionRenderer? _selectionRenderer;
    private EditorUI? _editorUi;

    // Services
    private MapLoadingService? _mapLoader;
    private AssetPlacementService? _placement;
    private SpatialActionService? _spatialActions;
    private UI.Components.MapActionDialog? _mapActionDialog;
    private CanvasView? _canvasView;

    // Input state
    private bool _areaSelecting;
    private Vector2 _areaStart;
    private bool _wasMouseDown;
    private Vector2 _rightClickWorldPos;
    private EntityDto? _rightClickEntity;
    private bool _contextMenuRequested;

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

        // Canvas view — replaces EntityRenderer
        _canvasView = new CanvasView(_mapRenderer);
        _canvasView.RegisterPresenter("player", new PointEntityPresenter(
            0xFF_3D45E8, 0xFF_FFFFFF, 5f, PointEntityPresenter.Shape.Circle,
            e => $"Player: {e.Label}\nPosition: ({e.WorldX:F1}, {e.WorldY:F1}, {e.WorldZ:F1})"));
        _canvasView.RegisterPresenter("npc", new PointEntityPresenter(
            0xFF_FF8737, 0xFF_FF8737, 4f, PointEntityPresenter.Shape.Circle,
            e => { var n = e as NpcMapEntity; return $"NPC: {e.Label}\nType: {n?.Dto.Type}\nPosition: ({e.WorldX:F1}, {e.WorldY:F1}, {e.WorldZ:F1})\nUUID: {e.Id}"; }));
        _canvasView.RegisterPresenter("soundzone", new AreaEntityPresenter(
            0x40_C5D150, 0x99_C5D150, 0xE6_C5D150,
            e => { var z = e as SoundZoneMapEntity; return z != null ? $"Sound Zone: {e.Label}\nCenter: ({e.WorldX:F1}, {e.WorldY:F1}, {e.WorldZ:F1})\nSize: {z.MaxX - z.MinX:F0} x {z.MaxZ - z.MinZ:F0}\nInterval: {z.Dto.Interval}s" : e.Label; }));
        _canvasView.RegisterPresenter("location", new PointEntityPresenter(
            0xFF_D98D5B, 0xFF_D98D5B, 6f, PointEntityPresenter.Shape.Diamond,
            e => $"Location: {e.Label}\nPosition: ({e.WorldX:F1}, {e.WorldY:F1}, {e.WorldZ:F1})"));
        _canvasView.OnEntitySelected = entity =>
        {
            if (entity is NpcMapEntity npc) _services.Selection.SelectEntity(npc.Dto);
            else if (entity is PlayerMapEntity player) _services.Selection.SelectPlayer(player.Dto);
            else if (entity is SoundZoneMapEntity zone) _services.Selection.SelectZone(zone.Dto);
        };
        _editorUi.SetCanvasView(_canvasView);

        _mapActionDialog = new UI.Components.MapActionDialog(_mapRenderer, _services.ApiClient);
        _editorUi.DrawMapContextMenu = () => { DrawContextMenu(); _mapActionDialog?.Draw(); };

        // Events
        _services.ApiClient.StatusChanged += msg => _editorUi.SetStatus(msg);
        _services.ApiClient.RequestLogged += (m, u) => _editorUi.Log.LogRequest(m, u);
        _services.ApiClient.ResponseLogged += (u, ok, d) => _editorUi.Log.LogResponse(u, ok, d);
        _services.MapData.MapUpdated += () => _mapRenderer?.UpdateTexture(_services.MapData);
        _services.EntityData.DataUpdated += () => RefreshCanvasEntities();

        // Services
        _mapLoader = new MapLoadingService(_services.ApiClient, _services.MapData, _mapRenderer, _services.EntityData);
        _placement = new AssetPlacementService(_services.ApiClient, _services.MapData, _services.EntityData, _services.Config);
        _placement.StatusChanged += msg => _editorUi?.SetStatus(msg);
        _spatialActions = new SpatialActionService(_services.ApiClient, _services.MapData, _services.Config, _services.PluginSchemas);
        _spatialActions.StatusChanged += msg => _editorUi?.SetStatus(msg);

        _ = _editorUi.LoadAssetsAsync();
        _ = LoadMapAsync();
    }

    public void Update(GameTime time)
    {
        _services.MapData.FlushOnMainThread();
        _services.EntityData.FlushOnMainThread();

        var input = _services.Game.Input;
        if (InputMap.IsPressed(input, InputAction.LoadMap)) _ = LoadMapAsync();
        if (_mapRenderer == null) return;

        var mouseScreen = new Vector2(
            input.MousePosition.X * _services.Game.Window.ClientBounds.Width,
            input.MousePosition.Y * _services.Game.Window.ClientBounds.Height);
        var worldPos = _mapRenderer.ScreenToWorld(mouseScreen);

        if (worldPos != null)
        {
            int bx = (int)MathF.Floor(worldPos.Value.X);
            int bz = (int)MathF.Floor(worldPos.Value.Y);
            var block = _services.MapData.TryGetBlock(bx, bz);
            _mapRenderer.CursorInfoText = block != null
                ? $"X: {bx}  Z: {bz}  Y: {block.Y}  Block: {block.Block}"
                : $"X: {bx}  Z: {bz}";
        }

        HandleAreaSelection(input, worldPos);
        HandleClick(input, worldPos);
        HandleRightClick(input, worldPos);

        if (worldPos != null && _services.Selection.SelectedAsset != null)
        {
            _selectionRenderer?.UpdateHoverHighlight(worldPos);
            _selectionRenderer?.UpdateSelectedAsset(_services.Selection.SelectedAsset);
        }
        else
        {
            _selectionRenderer?.HideHover();
        }
    }

    // ─── Area selection ──────────────────────────────────────────

    private void HandleAreaSelection(InputManager input, Vector2? worldPos)
    {
        if (_services.Selection.SelectedAsset?.Category != "sounds" || worldPos == null) return;

        if (InputMap.IsShiftDown(input) && input.IsMouseButtonPressed(MouseButton.Left))
        { _areaSelecting = true; _areaStart = worldPos.Value; }

        if (_areaSelecting && input.IsMouseButtonDown(MouseButton.Left))
            _selectionRenderer?.UpdateAreaSelection(_areaStart, worldPos.Value);

        if (_areaSelecting && input.IsMouseButtonReleased(MouseButton.Left))
        {
            _areaSelecting = false;
            _selectionRenderer?.HideAreaSelection();
            float minX = Math.Min(_areaStart.X, worldPos.Value.X), maxX = Math.Max(_areaStart.X, worldPos.Value.X);
            float minZ = Math.Min(_areaStart.Y, worldPos.Value.Y), maxZ = Math.Max(_areaStart.Y, worldPos.Value.Y);
            if (maxX - minX > 2 || maxZ - minZ > 2)
            {
                var asset = _services.Selection.SelectedAsset;
                if (asset != null)
                {
                    float cx = (minX + maxX) / 2f, cz = (minZ + maxZ) / 2f;
                    var block = _services.MapData.TryGetBlock((int)cx, (int)cz);
                    _ = _services.ApiClient.StartAmbientAsync(new SoundAmbientRequest
                    {
                        Sound = asset.Id, World = _services.Config.WorldId,
                        X = cx, Y = block?.Y ?? 64, Z = cz,
                        MinX = minX, MinZ = minZ, MaxX = maxX, MaxZ = maxZ, Interval = 5
                    });
                }
            }
        }
    }

    // ─── Click ───────────────────────────────────────────────────

    private void HandleClick(InputManager input, Vector2? worldPos)
    {
        bool leftReleased = input.IsMouseButtonReleased(MouseButton.Left)
                            || (!input.IsMouseButtonDown(MouseButton.Left) && _wasMouseDown);
        _wasMouseDown = input.IsMouseButtonDown(MouseButton.Left);
        if (!leftReleased || _areaSelecting || worldPos == null) return;
        if (_mapRenderer != null && _mapRenderer.IsPanning && _mapRenderer.PanDistance > 3) return;

        var asset = _services.Selection.SelectedAsset;
        if (asset != null)
            _ = _placement?.PlaceAssetAsync(asset, worldPos.Value.X, worldPos.Value.Y);
        else
            TrySelectAtPosition(worldPos.Value);
    }

    private void TrySelectAtPosition(Vector2 worldPos)
    {
        const float threshold = 3f;
        foreach (var p in _services.EntityData.Players)
            if (Vector2.Distance(new(p.X, p.Z), worldPos) < threshold) { _services.Selection.SelectPlayer(p); return; }
        foreach (var e in _services.EntityData.Entities)
            if (Vector2.Distance(new(e.X, e.Z), worldPos) < threshold) { _services.Selection.SelectEntity(e); return; }
        foreach (var z in _services.EntityData.SoundZones)
            if (worldPos.X >= z.MinX && worldPos.X <= z.MaxX && worldPos.Y >= z.MinZ && worldPos.Y <= z.MaxZ)
            { _services.Selection.SelectZone(z); return; }
        _services.Selection.ClearMapSelection();
    }

    // ─── Right-click ─────────────────────────────────────────────

    private void HandleRightClick(InputManager input, Vector2? worldPos)
    {
        if (!input.IsMouseButtonPressed(MouseButton.Right) || worldPos == null) return;
        _rightClickWorldPos = worldPos.Value;
        _rightClickEntity = null;
        const float threshold = 3f;
        foreach (var e in _services.EntityData.Entities)
            if (Vector2.Distance(new(e.X, e.Z), worldPos.Value) < threshold) { _rightClickEntity = e; break; }
        _contextMenuRequested = true;
    }

    public void DrawContextMenu()
    {
        if (_contextMenuRequested) { ImGui.OpenPopup("map_context_menu"); _contextMenuRequested = false; }

        if (ImGui.BeginPopup("map_context_menu"))
        {
            if (_rightClickEntity != null)
                ImGui.TextDisabled($"Entity: {_rightClickEntity.Name ?? _rightClickEntity.Type ?? "Entity"}");
            else
                ImGui.TextDisabled($"Position: {_rightClickWorldPos.X:F1}, {_rightClickWorldPos.Y:F1}");
            ImGui.Separator();

            if (_rightClickEntity != null)
            {
                if (ImGui.MenuItem("Copy to Clipboard"))
                {
                    _services.Clipboard.CopyEntity(_rightClickEntity);
                    _editorUi?.SetStatus($"Copied: {_rightClickEntity.Name ?? _rightClickEntity.Type}");
                }
                ImGui.Separator();
            }

            if (ImGui.MenuItem("Spawn NPC"))
                _mapActionDialog?.Open(new UI.Components.MapActions.SpawnNpcAction(_services.ApiClient));
            if (ImGui.MenuItem("Create Location"))
                _mapActionDialog?.Open(new UI.Components.MapActions.CreateLocationAction(_services.ApiClient));
            if (ImGui.MenuItem("Place Prefab"))
                _mapActionDialog?.Open(new UI.Components.MapActions.PlacePrefabAction(_services.ApiClient));
            if (ImGui.MenuItem("Create Sound Zone"))
                _mapActionDialog?.Open(new UI.Components.MapActions.CreateSoundZoneAction(_services.ApiClient));

            ImGui.Separator();

            if (_services.PluginSchemas.IsLoaded)
            {
                foreach (var sa in _services.PluginSchemas.GetSpatialActions())
                    if (ImGui.MenuItem($"{sa.PluginName}: {sa.Action.Label}"))
                        _ = _spatialActions?.ExecuteAsync(sa, _rightClickWorldPos.X, _rightClickWorldPos.Y, _rightClickEntity?.Name);
            }

            ImGui.EndPopup();
        }
    }

    // ─── Map loading ─────────────────────────────────────────────

    private void RefreshCanvasEntities()
    {
        if (_canvasView == null || _services.EntityData == null) return;

        var entities = new List<IMapEntity>();
        foreach (var p in _services.EntityData.Players)
            entities.Add(new PlayerMapEntity(p));
        foreach (var e in _services.EntityData.Entities)
            entities.Add(new NpcMapEntity(e));
        foreach (var z in _services.EntityData.SoundZones)
            entities.Add(new SoundZoneMapEntity(z));
        foreach (var pe in _services.EntityData.PluginEntities)
        {
            if (pe.Id.StartsWith("loc:") && (pe.X != 0 || pe.Z != 0))
                entities.Add(new LocationMapEntity(pe));
        }

        _canvasView.SetEntities(entities);
    }

    public void Unload(Scene rootScene) { }

    private async Task LoadMapAsync()
    {
        if (_mapLoader == null) return;
        _editorUi?.SetStatus("Loading map...");
        await _mapLoader.LoadAsync(_services.Config);
        _editorUi?.SetStatus("Map loaded");

        if (!_services.PluginSchemas.IsLoaded)
        {
            await _services.PluginSchemas.RefreshAsync();
            _entityRenderer?.SetPresenters(_services.PluginSchemas.GetAllPresenters());
            _services.EntityData.SpatialPluginIds = _services.PluginSchemas.GetSpatialPluginIds().ToList();
        }
    }
}
