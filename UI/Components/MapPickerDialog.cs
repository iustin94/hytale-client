using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Rendering;
using HytaleAdmin.Services;
using Stride.CommunityToolkit.ImGui;

namespace HytaleAdmin.UI.Components;

/// <summary>
/// Generic map dialog for picking coordinates or selecting existing entities.
/// Has its own pan/zoom state — doesn't interfere with the main MapRenderer.
///
/// Two modes:
/// - Coordinate mode: click to pick a world position
/// - Entity mode: shows filtered entities as markers, click to select one
/// </summary>
public class MapPickerDialog
{
    private readonly MapRenderer _mapRenderer;

    private bool _open;
    private string _title = "Pick Location";

    // Coordinate picking state
    private Vector2 _selectedWorldPos;
    private bool _hasCoordSelection;
    private Action<float, float>? _onCoordPicked;

    // Entity picking state
    private EntityDto? _selectedEntity;
    private Action<EntityDto>? _onEntityPicked;
    private Func<EntityDto, bool>? _entityFilter;
    private EntityDto[]? _entities;
    private EntityDataService? _entityData;
    private bool _entityMode;

    // Own pan/zoom state
    private float _panX, _panZ;
    private float _zoom = 2f;
    private bool _isPanning;
    private Vector2 _panStart;
    private Vector2 _panStartOffset;
    private bool _initialized;

    private static readonly uint MarkerColorU32 = 0xFF_3050F0;
    private static readonly uint SelectedMarkerU32 = 0xFF_20E060;
    private static readonly uint EntityMarkerU32 = 0xFF_F0A030;
    private static readonly uint CrosshairColorU32 = 0x99_FFFFFF;
    private static readonly uint BgColorU32 = 0xFF_211311;

    public MapPickerDialog(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    /// <summary>Open in coordinate picking mode.</summary>
    public void Open(string title, Action<float, float> onPicked, float? initialX = null, float? initialZ = null)
    {
        _title = title;
        _onCoordPicked = onPicked;
        _onEntityPicked = null;
        _entityMode = false;
        _entityFilter = null;
        _entities = null;
        _selectedEntity = null;
        _open = true;
        _hasCoordSelection = initialX.HasValue && initialZ.HasValue;
        if (_hasCoordSelection)
            _selectedWorldPos = new Vector2(initialX!.Value, initialZ!.Value);
        _initialized = false;
    }

    /// <summary>Open in entity selection mode with a filter predicate.</summary>
    public void OpenEntityPicker(string title, EntityDataService entityData,
        Func<EntityDto, bool>? filter, Action<EntityDto> onEntityPicked,
        Action<float, float>? onCoordFallback = null)
    {
        _title = title;
        _entityData = entityData;
        _entityFilter = filter;
        _onEntityPicked = onEntityPicked;
        _onCoordPicked = onCoordFallback;
        _entityMode = true;
        _selectedEntity = null;
        _hasCoordSelection = false;
        _open = true;
        _initialized = false;
        RefreshEntities();
    }

    public bool IsOpen => _open;

    private void RefreshEntities()
    {
        if (_entityData == null) return;
        var all = _entityData.Entities;
        _entities = _entityFilter != null
            ? all.Where(_entityFilter).ToArray()
            : all;
    }

    public void Draw()
    {
        if (!_open) return;

        var tex = _mapRenderer.MapTexture;
        if (tex == null)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 100));
            bool o = true;
            if (ImGui.Begin($"{_title}##MapPicker", ref o, ImGuiWindowFlags.NoCollapse))
                ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.3f, 1f), "No map data loaded. Load the map first.");
            ImGui.End();
            if (!o) _open = false;
            return;
        }

        if (!_initialized)
        {
            _zoom = 2f;
            if (_hasCoordSelection)
            {
                _panX = 250 - (_selectedWorldPos.X - _mapRenderer.OriginX) * _zoom;
                _panZ = 200 - (_selectedWorldPos.Y - _mapRenderer.OriginZ) * _zoom;
            }
            else
            {
                _panX = 250 - _mapRenderer.TexWidth * _zoom / 2f;
                _panZ = 200 - _mapRenderer.TexHeight * _zoom / 2f;
            }
            _initialized = true;
        }

        ImGui.SetNextWindowSize(new Vector2(550, 450), ImGuiCond.FirstUseEver);

        bool open = true;
        if (ImGui.Begin($"{_title}##MapPicker", ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
        {
            var avail = ImGui.GetContentRegionAvail();
            float mapHeight = avail.Y - 35;

            ImGui.BeginChild("MiniMapArea", new Vector2(avail.X, mapHeight),
                ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            var winPos = ImGui.GetCursorScreenPos();
            var winSize = ImGui.GetContentRegionAvail();
            var drawList = ImGui.GetWindowDrawList();
            var clipMax = winPos + winSize;

            drawList.AddRectFilled(winPos, clipMax, BgColorU32);

            // Render map texture
            float imgW = _mapRenderer.TexWidth * _zoom;
            float imgH = _mapRenderer.TexHeight * _zoom;
            var imgPos = new Vector2(winPos.X + _panX, winPos.Y + _panZ);

            drawList.PushClipRect(winPos, clipMax);
            ImGui.SetCursorScreenPos(imgPos);
            ImGuiExtension.Image(tex, (int)imgW, (int)imgH);

            // Input
            var io = ImGui.GetIO();
            var mousePos = io.MousePos;
            bool hovered = ImGui.IsWindowHovered();

            // Pan
            bool middleDown = io.MouseDown[2];
            if (hovered && middleDown && !_isPanning)
            {
                _isPanning = true;
                _panStart = mousePos;
                _panStartOffset = new Vector2(_panX, _panZ);
            }
            if (_isPanning)
            {
                _panX = _panStartOffset.X + (mousePos.X - _panStart.X);
                _panZ = _panStartOffset.Y + (mousePos.Y - _panStart.Y);
                if (!middleDown) _isPanning = false;
            }

            // Zoom
            if (hovered && MathF.Abs(io.MouseWheel) > 0.001f)
            {
                float oldZoom = _zoom;
                _zoom *= io.MouseWheel > 0 ? 1.15f : 1f / 1.15f;
                _zoom = Math.Clamp(_zoom, 0.5f, 20f);
                var localMouse = new Vector2(mousePos.X - winPos.X, mousePos.Y - winPos.Y);
                float wx = (localMouse.X - _panX) / oldZoom;
                float wz = (localMouse.Y - _panZ) / oldZoom;
                _panX = localMouse.X - wx * _zoom;
                _panZ = localMouse.Y - wz * _zoom;
            }

            // Draw entities if in entity mode
            if (_entityMode && _entities != null)
            {
                RefreshEntities(); // Update positions each frame
                DrawEntityMarkers(drawList, winPos);
            }

            // Crosshair at cursor
            if (hovered)
            {
                drawList.AddLine(mousePos - new Vector2(10, 0), mousePos + new Vector2(10, 0), CrosshairColorU32);
                drawList.AddLine(mousePos - new Vector2(0, 10), mousePos + new Vector2(0, 10), CrosshairColorU32);
            }

            // Click handling
            if (hovered && io.MouseClicked[0] && !_isPanning)
            {
                var local = new Vector2(mousePos.X - winPos.X, mousePos.Y - winPos.Y);
                float wx = (local.X - _panX) / _zoom + _mapRenderer.OriginX;
                float wz = (local.Y - _panZ) / _zoom + _mapRenderer.OriginZ;

                if (_entityMode)
                {
                    // Try to hit-test an entity first
                    var hit = HitTestEntity(mousePos, winPos);
                    if (hit != null)
                    {
                        _selectedEntity = hit;
                        _hasCoordSelection = false;
                    }
                    else if (_onCoordPicked != null)
                    {
                        // Fallback to coordinate pick
                        _selectedWorldPos = new Vector2(wx, wz);
                        _hasCoordSelection = true;
                        _selectedEntity = null;
                    }
                }
                else
                {
                    _selectedWorldPos = new Vector2(wx, wz);
                    _hasCoordSelection = true;
                }
            }

            // Draw coordinate selection marker
            if (_hasCoordSelection && !_entityMode)
            {
                DrawCoordMarker(drawList, winPos, _selectedWorldPos, MarkerColorU32);
            }

            drawList.PopClipRect();
            ImGui.EndChild();

            // Bottom bar
            DrawBottomBar(avail);
        }
        ImGui.End();

        if (!open) _open = false;
    }

    // ─── Entity rendering ────────────────────────────────────────

    private void DrawEntityMarkers(ImDrawListPtr drawList, Vector2 winPos)
    {
        if (_entities == null) return;

        foreach (var entity in _entities)
        {
            var sp = WorldToLocal(entity.X, entity.Z, winPos);
            bool isSelected = _selectedEntity != null && _selectedEntity.Uuid == entity.Uuid;
            uint color = isSelected ? SelectedMarkerU32 : EntityMarkerU32;
            float size = isSelected ? 8f : 5f;

            // Diamond marker
            drawList.AddQuadFilled(
                sp + new Vector2(0, -size), sp + new Vector2(size, 0),
                sp + new Vector2(0, size), sp + new Vector2(-size, 0), color);

            // Label
            var name = entity.Name ?? entity.Type ?? "Entity";
            if (isSelected)
                name = $"> {name} <";
            drawList.AddText(sp + new Vector2(size + 4, -7), color, name);
        }
    }

    private EntityDto? HitTestEntity(Vector2 mousePos, Vector2 winPos)
    {
        if (_entities == null) return null;
        float bestDist = 15f; // pixel hit threshold
        EntityDto? best = null;

        foreach (var entity in _entities)
        {
            var sp = WorldToLocal(entity.X, entity.Z, winPos);
            float dist = Vector2.Distance(mousePos, sp);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = entity;
            }
        }
        return best;
    }

    // ─── Coordinate marker ───────────────────────────────────────

    private void DrawCoordMarker(ImDrawListPtr drawList, Vector2 winPos, Vector2 worldPos, uint color)
    {
        var sp = WorldToLocal(worldPos.X, worldPos.Y, winPos);
        drawList.AddLine(sp - new Vector2(14, 0), sp + new Vector2(14, 0), color, 2f);
        drawList.AddLine(sp - new Vector2(0, 14), sp + new Vector2(0, 14), color, 2f);
        float d = 7f;
        drawList.AddQuad(sp + new Vector2(0, -d), sp + new Vector2(d, 0),
            sp + new Vector2(0, d), sp + new Vector2(-d, 0), color, 2f);
        drawList.AddText(sp + new Vector2(12, -8), color, $"({worldPos.X:F0}, {worldPos.Y:F0})");
    }

    // ─── Bottom bar ──────────────────────────────────────────────

    private void DrawBottomBar(Vector2 avail)
    {
        // Status text
        if (_entityMode && _selectedEntity != null)
        {
            var name = _selectedEntity.Name ?? _selectedEntity.Type ?? "Entity";
            ImGui.TextColored(new Vector4(0.30f, 0.90f, 0.40f, 1f),
                $"Selected: {name} ({_selectedEntity.X:F0}, {_selectedEntity.Z:F0})");
            ImGui.SameLine();
        }
        else if (_hasCoordSelection)
        {
            ImGui.TextColored(new Vector4(0.95f, 0.30f, 0.35f, 1f),
                $"Position: ({_selectedWorldPos.X:F0}, {_selectedWorldPos.Y:F0})");
            ImGui.SameLine();
        }

        if (_entityMode && _entities != null)
        {
            ImGui.TextColored(new Vector4(0.55f, 0.55f, 0.63f, 1f), $"({_entities.Length} entities)");
            ImGui.SameLine();
        }

        float btnWidth = 80;
        ImGui.SetCursorPosX(avail.X - btnWidth * 2 - ImGui.GetStyle().ItemSpacing.X);

        bool canConfirm = _entityMode ? _selectedEntity != null : _hasCoordSelection;
        if (!canConfirm) ImGui.BeginDisabled();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.31f, 0.80f, 0.40f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.10f, 0.10f, 0.12f, 1f));
        if (ImGui.Button("Confirm", new Vector2(btnWidth, 0)))
        {
            if (_entityMode && _selectedEntity != null)
                _onEntityPicked?.Invoke(_selectedEntity);
            else if (_hasCoordSelection)
                _onCoordPicked?.Invoke(_selectedWorldPos.X, _selectedWorldPos.Y);
            _open = false;
        }
        ImGui.PopStyleColor(2);
        if (!canConfirm) ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(btnWidth, 0)))
            _open = false;
    }

    // ─── Coordinate conversion ───────────────────────────────────

    private Vector2 WorldToLocal(float worldX, float worldZ, Vector2 winPos)
    {
        float sx = (worldX - _mapRenderer.OriginX) * _zoom + _panX + winPos.X;
        float sz = (worldZ - _mapRenderer.OriginZ) * _zoom + _panZ + winPos.Y;
        return new Vector2(sx, sz);
    }
}
