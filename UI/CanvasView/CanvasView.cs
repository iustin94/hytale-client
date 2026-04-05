using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Rendering;
using HytaleAdmin.UI.NodeEditor;
using Stride.Input;

namespace HytaleAdmin.UI.CanvasView;

/// <summary>
/// Generic 2D canvas component for rendering and interacting with spatial entities.
/// Handles pan/zoom, entity rendering via presenters, selection (single/multi/box),
/// hover highlighting, tooltips, and context menus.
/// </summary>
public class CanvasView
{
    private readonly MapRenderer _renderer;
    private readonly CanvasSelectionState _selection = new();
    private readonly Dictionary<string, IMapEntityPresenter> _presenters = new();
    private readonly List<IMapEntity> _entities = new();

    // Input state
    private InputManager? _strideInput;
    private bool _wasLeftDown;
    private bool _wasRightDown;
    private Vector2 _dragStartScreen;
    private bool _isDragging;

    // Context menu state
    private bool _contextMenuOpen;
    private int _contextMenuAge;
    private Vector2 _contextMenuScreenPos;
    private float _contextMenuWorldX, _contextMenuWorldZ;
    private IMapEntity? _contextMenuEntity;

    // Tooltip
    private string? _tooltipText;
    private Vector2 _tooltipPos;

    // Public API
    public IMapContextMenuProvider? ContextMenu { get; set; }
    public Action<IMapEntity>? OnEntitySelected;
    public Action<IMapEntity>? OnEntityDoubleClicked;
    public Action<IReadOnlySet<string>>? OnMultiDelete;
    public Func<IMapEntity, bool>? EntityFilter;

    public CanvasSelectionState Selection => _selection;
    public IReadOnlyList<IMapEntity> Entities => _entities;

    // Colors
    private static readonly uint BoxSelectFill = ImGui.ColorConvertFloat4ToU32(new Vector4(0.40f, 0.70f, 0.95f, 0.12f));
    private static readonly uint BoxSelectBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(0.40f, 0.70f, 0.95f, 0.5f));
    private static readonly uint TooltipBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.10f, 0.15f, 0.92f));
    private static readonly uint TooltipBorder = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.85f, 0.30f, 0.6f));
    private static readonly uint TooltipText = ImGui.ColorConvertFloat4ToU32(new Vector4(0.90f, 0.90f, 0.95f, 1f));
    private static readonly uint TooltipDim = ImGui.ColorConvertFloat4ToU32(new Vector4(0.60f, 0.60f, 0.68f, 1f));

    public CanvasView(MapRenderer renderer)
    {
        _renderer = renderer;
    }

    public void RegisterPresenter(string entityType, IMapEntityPresenter presenter)
    {
        _presenters[entityType] = presenter;
    }

    public void SetEntities(IEnumerable<IMapEntity> entities)
    {
        _entities.Clear();
        _entities.AddRange(entities);
    }

    public void Draw(float width, float height, InputManager? input = null)
    {
        _strideInput = input;
        var io = ImGui.GetIO();
        var mousePos = io.MousePos;
        bool leftDown = io.MouseDown[0];
        bool leftClicked = leftDown && !_wasLeftDown;
        bool leftReleased = !leftDown && _wasLeftDown;
        bool rightClicked = input?.IsMouseButtonPressed(MouseButton.Right) ?? false;
        bool shiftHeld = input?.IsKeyDown(Keys.LeftShift) == true || input?.IsKeyDown(Keys.RightShift) == true;
        bool deletePressed = input?.IsKeyPressed(Keys.Delete) == true;

        // Draw map (pan/zoom handled internally by MapRenderer)
        var drawList = _renderer.Draw();
        var winPos = _renderer.WindowPos;
        var winSize = _renderer.WindowSize;
        bool hovered = mousePos.X >= winPos.X && mousePos.X <= winPos.X + winSize.X &&
                       mousePos.Y >= winPos.Y && mousePos.Y <= winPos.Y + winSize.Y;

        drawList.PushClipRect(winPos, winPos + winSize);

        _tooltipText = null;
        _selection.HoveredId = null;

        // Render entities + hit-test hover
        foreach (var entity in _entities)
        {
            if (EntityFilter != null && !EntityFilter(entity)) continue;

            var presenter = GetPresenter(entity);
            if (presenter == null) continue;

            var screenPos = _renderer.WorldToScreen(entity.WorldX, entity.WorldZ);
            if (screenPos == null) continue;
            var sp = new Vector2(screenPos.Value.X, screenPos.Value.Y);

            // Hit-test for hover
            bool isHovered = hovered && presenter.HitTest(entity, sp, mousePos, _renderer.BlockScreenSize);
            bool isSelected = _selection.IsSelected(entity.Id);

            if (isHovered)
            {
                _selection.HoveredId = entity.Id;
                var tooltip = presenter.GetTooltip(entity);
                if (tooltip != null)
                {
                    _tooltipText = tooltip;
                    _tooltipPos = mousePos;
                }
            }

            // Draw in appropriate state
            if (isSelected)
                presenter.DrawSelected(drawList, entity, sp, _renderer.BlockScreenSize);
            else if (isHovered)
                presenter.DrawHovered(drawList, entity, sp, _renderer.BlockScreenSize);
            else
                presenter.DrawNormal(drawList, entity, sp, _renderer.BlockScreenSize);
        }

        // Input handling
        if (hovered && !_contextMenuOpen)
        {
            // Left click
            if (leftClicked)
            {
                if (_selection.HoveredId != null)
                {
                    if (shiftHeld)
                        _selection.ToggleSelect(_selection.HoveredId);
                    else
                        _selection.Select(_selection.HoveredId);

                    var selected = _entities.FirstOrDefault(e => e.Id == _selection.HoveredId);
                    if (selected != null) OnEntitySelected?.Invoke(selected);
                }
                else
                {
                    // Start box selection or deselect
                    if (!shiftHeld) _selection.ClearSelection();
                    _selection.IsBoxSelecting = true;
                    _selection.BoxStart = mousePos;
                    _selection.BoxEnd = mousePos;
                }
            }

            // Double-click
            if (leftClicked && io.MouseDoubleClicked[0] && _selection.HoveredId != null)
            {
                var entity = _entities.FirstOrDefault(e => e.Id == _selection.HoveredId);
                if (entity != null) OnEntityDoubleClicked?.Invoke(entity);
            }

            // Right-click context menu
            if (rightClicked)
            {
                var worldPos = _renderer.ScreenToWorld(new Stride.Core.Mathematics.Vector2(mousePos.X, mousePos.Y));
                _contextMenuWorldX = worldPos?.X ?? 0;
                _contextMenuWorldZ = worldPos?.Y ?? 0;
                _contextMenuEntity = _selection.HoveredId != null
                    ? _entities.FirstOrDefault(e => e.Id == _selection.HoveredId)
                    : null;
                _contextMenuScreenPos = mousePos;
                _contextMenuOpen = true;
                _contextMenuAge = 0;
            }

            // Delete key
            if (deletePressed && _selection.SelectedIds.Count > 0)
            {
                OnMultiDelete?.Invoke(_selection.SelectedIds);
            }
        }

        // Box selection update
        if (_selection.IsBoxSelecting)
        {
            _selection.BoxEnd = mousePos;

            if (leftReleased)
            {
                _selection.IsBoxSelecting = false;
                float minX = Math.Min(_selection.BoxStart.X, _selection.BoxEnd.X);
                float minY = Math.Min(_selection.BoxStart.Y, _selection.BoxEnd.Y);
                float maxX = Math.Max(_selection.BoxStart.X, _selection.BoxEnd.X);
                float maxY = Math.Max(_selection.BoxStart.Y, _selection.BoxEnd.Y);

                if (maxX - minX > 5 || maxY - minY > 5)
                {
                    if (!shiftHeld) _selection.ClearSelection();
                    foreach (var entity in _entities)
                    {
                        if (EntityFilter != null && !EntityFilter(entity)) continue;
                        var sp = _renderer.WorldToScreen(entity.WorldX, entity.WorldZ);
                        if (sp == null) continue;
                        if (sp.Value.X >= minX && sp.Value.X <= maxX &&
                            sp.Value.Y >= minY && sp.Value.Y <= maxY)
                        {
                            _selection.SelectedIds.Add(entity.Id);
                        }
                    }
                }
            }
        }

        // Box selection rectangle
        if (_selection.IsBoxSelecting)
        {
            var boxMin = new Vector2(Math.Min(_selection.BoxStart.X, _selection.BoxEnd.X),
                Math.Min(_selection.BoxStart.Y, _selection.BoxEnd.Y));
            var boxMax = new Vector2(Math.Max(_selection.BoxStart.X, _selection.BoxEnd.X),
                Math.Max(_selection.BoxStart.Y, _selection.BoxEnd.Y));
            drawList.AddRectFilled(boxMin, boxMax, BoxSelectFill);
            drawList.AddRect(boxMin, boxMax, BoxSelectBorder);
        }

        // Tooltip
        if (_tooltipText != null)
            DrawTooltip(drawList, _tooltipPos, _tooltipText);

        drawList.PopClipRect();

        // Context menu (outside clip rect)
        DrawContextMenu();

        _wasLeftDown = leftDown;
        _wasRightDown = rightClicked;
    }

    // ─── Context menu ────────────────────────────────────────────

    private void DrawContextMenu()
    {
        if (!_contextMenuOpen) return;
        if (ContextMenu == null) { _contextMenuOpen = false; return; }

        List<ContextMenuItem> items;
        if (_selection.SelectedIds.Count > 1)
            items = ContextMenu.GetMultiSelectMenu(_selection.SelectedIds);
        else if (_contextMenuEntity != null)
            items = ContextMenu.GetEntityMenu(_contextMenuEntity);
        else
            items = ContextMenu.GetBackgroundMenu(_contextMenuWorldX, _contextMenuWorldZ);

        if (items.Count == 0) { _contextMenuOpen = false; return; }

        ImGui.SetNextWindowPos(_contextMenuScreenPos);
        ImGui.SetNextWindowSize(new Vector2(220, 0));
        if (_contextMenuAge == 0) ImGui.SetNextWindowFocus();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.14f, 0.14f, 0.19f, 0.97f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.35f, 0.35f, 0.45f, 0.8f));

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings;

        if (ImGui.Begin("Menu##CanvasCtx", flags))
        {
            _contextMenuAge++;
            bool clicked = false;

            foreach (var item in items)
            {
                if (item.Separator) { ImGui.Separator(); continue; }

                bool hasColor = item.Color != 0xFFFFFFFF;
                if (hasColor) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertU32ToFloat4(item.Color));

                var label = string.IsNullOrEmpty(item.Label) ? "???" : item.Label;
                if (ImGui.Selectable(label))
                {
                    ContextMenu.OnItemSelected(item, _contextMenuEntity, _contextMenuWorldX, _contextMenuWorldZ);
                    clicked = true;
                }

                if (hasColor) ImGui.PopStyleColor();
            }

            if (clicked) _contextMenuOpen = false;

            if (_contextMenuAge > 2 && !ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows))
            {
                var io = ImGui.GetIO();
                bool rightPressed = _strideInput?.IsMouseButtonPressed(MouseButton.Right) ?? false;
                if (io.MouseDown[0] || rightPressed)
                    _contextMenuOpen = false;
            }
        }
        ImGui.End();

        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(2);
    }

    // ─── Tooltip ─────────────────────────────────────────────────

    private void DrawTooltip(ImDrawListPtr drawList, Vector2 pos, string text)
    {
        var lines = text.Split('\n');
        float lineH = 15f;
        float padX = 10f, padY = 6f;

        float maxW = 0;
        foreach (var line in lines)
        {
            var size = ImGui.CalcTextSize(line);
            if (size.X > maxW) maxW = size.X;
        }

        var tooltipPos = pos + new Vector2(16, 16);
        var tooltipMax = tooltipPos + new Vector2(maxW + padX * 2, lines.Length * lineH + padY * 2);

        drawList.AddRectFilled(tooltipPos, tooltipMax, TooltipBg, 4f);
        drawList.AddRect(tooltipPos, tooltipMax, TooltipBorder, 4f);

        float y = tooltipPos.Y + padY;
        for (int i = 0; i < lines.Length; i++)
        {
            drawList.AddText(new Vector2(tooltipPos.X + padX, y), i == 0 ? TooltipText : TooltipDim, lines[i]);
            y += lineH;
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private IMapEntityPresenter? GetPresenter(IMapEntity entity)
    {
        return _presenters.TryGetValue(entity.EntityType, out var p) ? p : null;
    }
}
