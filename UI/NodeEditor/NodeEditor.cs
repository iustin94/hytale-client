using System.Numerics;
using Hexa.NET.ImGui;
using Stride.Input;

namespace HytaleAdmin.UI.NodeEditor;

public class NodeEditor<TNode> where TNode : class, INode
{
    private readonly List<TNode> _nodes = new();
    private readonly List<NodeLink> _links = new();
    private readonly PortTypeMap _portTypes;
    private readonly Dictionary<string, NodeStyle> _styles = new();
    private readonly NodeStyle _defaultStyle = new();

    // Canvas state
    private Vector2 _pan;
    private float _zoom = 1f;
    private Vector2 _canvasOrigin;
    private Vector2 _canvasSize;

    // Interaction state
    private string? _selectedNodeId;
    private string? _hoveredNodeId;
    private string? _dragNodeId;
    private Vector2 _dragOffset;
    private bool _isPanning;
    private Vector2 _panAnchor;

    // Multi-selection
    private readonly HashSet<string> _selectedNodeIds = new();
    private bool _isBoxSelecting;
    private Vector2 _boxSelectStart;
    private Vector2 _boxSelectEnd;
    private Dictionary<string, Vector2> _multiDragOffsets = new();

    // Link dragging
    private string? _linkSourceNodeId;
    private string? _linkSourcePortId;
    private string? _linkSourcePortType;
    private PortDirection _linkSourceDir;
    private Vector2 _linkDragEnd;
    private bool _isDraggingLink;

    // Hover
    private string? _hoveredPortNodeId;
    private string? _hoveredPortId;

    // Click tracking (Hexa.NET.ImGui uses io.MouseDown[] directly)
    private bool _wasLeftDown;
    private bool _wasRightDown;
    private float _lastLeftClickTime;
    private Vector2 _lastLeftClickPos;
    private const float DoubleClickTime = 0.3f;
    private const float DoubleClickDist = 6f;

    // Port position cache (rebuilt each frame)
    private readonly Dictionary<(string nodeId, string portId), Vector2> _portScreenPositions = new();

    // Context menu state
    private bool _contextMenuOpen;
    private int _contextMenuAge; // frames since opened
    private Vector2 _contextMenuScreenPos;
    private ContextMenuRequest<TNode>? _contextMenuRequest;
    private InputManager? _strideInput; // cached for context menu dismiss check

    // Callbacks
    public Action<TNode>? OnNodeSelected;
    public Action<NodeLink>? OnLinkCreated;
    public Action<NodeLink>? OnLinkRemoved;
    public Action<TNode>? OnNodeMoved;
    public Action<Vector2>? OnCanvasDoubleClick;
    public Action<TNode>? OnNodeDoubleClick;
    public Action<TNode>? OnNodeContextMenu;

    // Context menu provider (externally defined)
    public IContextMenuProvider<TNode>? ContextMenu { get; set; }

    // Custom content rendering inside node body
    public Action<TNode, ImDrawListPtr, Vector2, Vector2>? DrawNodeContent;
    public Func<TNode, float>? MeasureContentHeight;

    // Node visibility filter (set externally to hide nodes not matching criteria)
    public Func<TNode, bool>? NodeFilter { get; set; }

    // Overlay drawn on top of the canvas (buttons, guides, etc.)
    public Action<ImDrawListPtr, Vector2, Vector2>? DrawOverlay;

    // Grid config
    public float GridSize { get; set; } = 32f;
    public float MinZoom { get; set; } = 0.15f;
    public float MaxZoom { get; set; } = 3f;
    public float LinkThickness { get; set; } = 2.5f;
    public float LinkCurvature { get; set; } = 0.5f;

    public string? SelectedNodeId => _selectedNodeId;
    public IReadOnlySet<string> SelectedNodeIds => _selectedNodeIds;
    public IReadOnlyList<TNode> Nodes => _nodes;
    public IReadOnlyList<NodeLink> Links => _links;
    public Vector2 Pan { get => _pan; set => _pan = value; }
    public float Zoom { get => _zoom; set => _zoom = Math.Clamp(value, MinZoom, MaxZoom); }
    public Action<IReadOnlySet<string>>? OnMultiDelete;

    public NodeEditor(PortTypeMap portTypes)
    {
        _portTypes = portTypes;
    }

    // ─── Data management ─────────────────────────────────────────

    public void SetStyle(string nodeType, NodeStyle style) => _styles[nodeType] = style;

    public void AddNode(TNode node)
    {
        if (_nodes.Any(n => n.Id == node.Id)) return;
        _nodes.Add(node);
    }

    public void RemoveNode(string nodeId)
    {
        _nodes.RemoveAll(n => n.Id == nodeId);
        _links.RemoveAll(l => l.SourceNodeId == nodeId || l.TargetNodeId == nodeId);
        if (_selectedNodeId == nodeId) _selectedNodeId = null;
    }

    public void AddLink(NodeLink link)
    {
        if (_links.Any(l => l.Id == link.Id)) return;
        _links.Add(link);
    }

    public void RemoveLink(string linkId)
    {
        var link = _links.FirstOrDefault(l => l.Id == linkId);
        if (link != null)
        {
            _links.Remove(link);
            OnLinkRemoved?.Invoke(link);
        }
    }

    public void Clear()
    {
        _nodes.Clear();
        _links.Clear();
        _selectedNodeId = null;
    }

    public void SelectNode(string? nodeId) => _selectedNodeId = nodeId;

    public TNode? GetNode(string nodeId) => _nodes.FirstOrDefault(n => n.Id == nodeId);

    private bool IsNodeVisible(TNode node) => NodeFilter == null || NodeFilter(node);

    public void CenterOnNodes()
    {
        var visible = _nodes.Where(IsNodeVisible).ToList();
        if (visible.Count == 0) return;
        var avg = visible.Aggregate(Vector2.Zero, (sum, n) => sum + n.Position) / visible.Count;
        _pan = _canvasSize / 2f - avg * _zoom;
    }

    // ─── Coordinate transforms ───────────────────────────────────

    private Vector2 CanvasToScreen(Vector2 canvasPos) => _canvasOrigin + canvasPos * _zoom + _pan;
    private Vector2 ScreenToCanvas(Vector2 screenPos) => (screenPos - _canvasOrigin - _pan) / _zoom;

    // ─── Main draw ───────────────────────────────────────────────

    public void Draw(float width, float height, InputManager? input = null)
    {
        ImGui.BeginChild("NodeEditorCanvas", new Vector2(width, height),
            ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        _canvasOrigin = ImGui.GetCursorScreenPos();
        _canvasSize = ImGui.GetContentRegionAvail();
        var drawList = ImGui.GetWindowDrawList();

        // Clip to canvas area
        drawList.PushClipRect(_canvasOrigin, _canvasOrigin + _canvasSize);

        HandleInput(input);
        DrawGrid(drawList);

        _portScreenPositions.Clear();

        // Pre-compute all port positions (visible nodes only)
        foreach (var node in _nodes)
        {
            if (IsNodeVisible(node))
                CachePortPositions(node);
        }

        DrawLinks(drawList);
        DrawLinkDrag(drawList);
        DrawNodes(drawList);

        // Box selection rectangle
        if (_isBoxSelecting)
        {
            var boxMin = new Vector2(Math.Min(_boxSelectStart.X, _boxSelectEnd.X), Math.Min(_boxSelectStart.Y, _boxSelectEnd.Y));
            var boxMax = new Vector2(Math.Max(_boxSelectStart.X, _boxSelectEnd.X), Math.Max(_boxSelectStart.Y, _boxSelectEnd.Y));
            var fillColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.40f, 0.70f, 0.95f, 0.12f));
            var borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.40f, 0.70f, 0.95f, 0.5f));
            drawList.AddRectFilled(boxMin, boxMax, fillColor);
            drawList.AddRect(boxMin, boxMax, borderColor);
        }

        // Overlay (buttons, guides) drawn on top of everything
        DrawOverlay?.Invoke(drawList, _canvasOrigin, _canvasSize);

        drawList.PopClipRect();
        ImGui.EndChild();

        // Context menu rendered outside child window to avoid clipping
        DrawContextMenu();
    }

    // ─── Input handling ──────────────────────────────────────────

    private void HandleInput(InputManager? strideInput)
    {
        _strideInput = strideInput;
        var io = ImGui.GetIO();
        var mousePos = io.MousePos;

        // Manual bounds check — ImGui.IsWindowHovered() fails when context menu or
        // other overlay windows are on top of the canvas child window
        bool hovered = mousePos.X >= _canvasOrigin.X && mousePos.X <= _canvasOrigin.X + _canvasSize.X &&
                       mousePos.Y >= _canvasOrigin.Y && mousePos.Y <= _canvasOrigin.Y + _canvasSize.Y &&
                       !_contextMenuOpen;

        bool leftDown = io.MouseDown[0];
        // Right-click via Stride input — ImGui IO doesn't receive right mouse events
        bool rightDown = strideInput?.IsMouseButtonDown(MouseButton.Right) ?? io.MouseDown[1];
        bool leftClicked = leftDown && !_wasLeftDown;
        bool leftReleased = !leftDown && _wasLeftDown;
        bool rightClicked = strideInput?.IsMouseButtonPressed(MouseButton.Right) ?? (rightDown && !_wasRightDown);

        // Double-click detection
        bool leftDoubleClicked = false;
        if (leftClicked)
        {
            float now = (float)io.DeltaTime; // approximate — use cumulative
            float elapsed = now; // simplified; real impl would track absolute time
            if (Vector2.Distance(mousePos, _lastLeftClickPos) < DoubleClickDist &&
                (io.DeltaTime < DoubleClickTime || _lastLeftClickTime > 0))
            {
                // Use ImGui's built-in double-click detection via MouseDoubleClicked array
            }
            _lastLeftClickPos = mousePos;
        }
        // Use io.MouseDoubleClicked if available, otherwise fallback
        leftDoubleClicked = leftClicked && io.MouseDoubleClicked[0];

        // Zoom
        if (hovered && Math.Abs(io.MouseWheel) > 0.001f)
        {
            float oldZoom = _zoom;
            _zoom *= io.MouseWheel > 0 ? 1.1f : 1f / 1.1f;
            _zoom = Math.Clamp(_zoom, MinZoom, MaxZoom);

            // Zoom toward mouse
            var mouseCanvas = (mousePos - _canvasOrigin - _pan) / oldZoom;
            _pan = mousePos - _canvasOrigin - mouseCanvas * _zoom;
        }

        // Pan (middle mouse only — right-click reserved for context menu)
        bool panButton = io.MouseDown[2];
        if (hovered && panButton && !_isPanning && !_isDraggingLink)
        {
            _isPanning = true;
            _panAnchor = mousePos - _pan;
        }
        if (_isPanning)
        {
            _pan = mousePos - _panAnchor;
            if (!panButton) _isPanning = false;
        }

        // Double-click on canvas background
        if (hovered && leftDoubleClicked && _hoveredNodeId == null)
        {
            OnCanvasDoubleClick?.Invoke(ScreenToCanvas(mousePos));
        }

        // Node interaction
        HandleNodeInput(mousePos, hovered, leftDown, leftClicked, leftReleased, leftDoubleClicked, rightClicked, io);

        _wasLeftDown = leftDown;
        _wasRightDown = rightDown;
    }

    private void HandleNodeInput(Vector2 mousePos, bool canvasHovered,
        bool leftDown, bool leftClicked, bool leftReleased, bool leftDoubleClicked,
        bool rightClicked, ImGuiIOPtr io)
    {
        _hoveredNodeId = null;
        _hoveredPortNodeId = null;
        _hoveredPortId = null;

        // Hit-test nodes (reverse order = top-most first, visible only)
        for (int i = _nodes.Count - 1; i >= 0; i--)
        {
            var node = _nodes[i];
            if (!IsNodeVisible(node)) continue;
            var style = GetStyle(node);
            var nodeMin = CanvasToScreen(node.Position);
            var nodeMax = nodeMin + GetNodeSize(node, style) * _zoom;

            if (mousePos.X >= nodeMin.X && mousePos.X <= nodeMax.X &&
                mousePos.Y >= nodeMin.Y && mousePos.Y <= nodeMax.Y)
            {
                _hoveredNodeId = node.Id;

                // Check port hover
                foreach (var port in node.Ports)
                {
                    if (!_portScreenPositions.TryGetValue((node.Id, port.Id), out var portPos)) continue;
                    float dist = Vector2.Distance(mousePos, portPos);
                    if (dist <= style.PortRadius * _zoom + 4f)
                    {
                        _hoveredPortNodeId = node.Id;
                        _hoveredPortId = port.Id;
                    }
                }
                break;
            }
        }

        // Also check port hover beyond node bounds
        if (_hoveredPortId == null)
        {
            foreach (var node in _nodes)
            {
                if (!IsNodeVisible(node)) continue;
                var style = GetStyle(node);
                foreach (var port in node.Ports)
                {
                    if (!_portScreenPositions.TryGetValue((node.Id, port.Id), out var portPos)) continue;
                    float dist = Vector2.Distance(mousePos, portPos);
                    if (dist <= style.PortRadius * _zoom + 4f)
                    {
                        _hoveredPortNodeId = node.Id;
                        _hoveredPortId = port.Id;
                        break;
                    }
                }
                if (_hoveredPortId != null) break;
            }
        }

        // Shift key for multi-select
        bool shiftHeld = _strideInput?.IsMouseButtonDown(Stride.Input.MouseButton.Middle) == false
            && (_strideInput?.IsKeyDown(Stride.Input.Keys.LeftShift) == true || _strideInput?.IsKeyDown(Stride.Input.Keys.RightShift) == true);

        // Left click
        if (canvasHovered && leftClicked && !_isPanning)
        {
            // Port click → start link drag
            if (_hoveredPortId != null && _hoveredPortNodeId != null)
            {
                var node = GetNode(_hoveredPortNodeId);
                var port = node?.Ports.FirstOrDefault(p => p.Id == _hoveredPortId);
                if (node != null && port != null)
                {
                    _linkSourceNodeId = node.Id;
                    _linkSourcePortId = port.Id;
                    _linkSourcePortType = port.PortType;
                    _linkSourceDir = port.Direction;
                    _linkDragEnd = mousePos;
                    _isDraggingLink = true;
                }
            }
            // Node click → select + begin drag
            else if (_hoveredNodeId != null)
            {
                if (shiftHeld)
                {
                    // Toggle multi-selection
                    if (_selectedNodeIds.Contains(_hoveredNodeId))
                        _selectedNodeIds.Remove(_hoveredNodeId);
                    else
                        _selectedNodeIds.Add(_hoveredNodeId);
                }
                else
                {
                    // Single selection
                    if (!_selectedNodeIds.Contains(_hoveredNodeId))
                    {
                        _selectedNodeIds.Clear();
                        _selectedNodeIds.Add(_hoveredNodeId);
                    }
                }

                _selectedNodeId = _hoveredNodeId;
                OnNodeSelected?.Invoke(GetNode(_hoveredNodeId)!);

                // Begin drag (multi or single)
                _dragNodeId = _hoveredNodeId;
                var node = GetNode(_hoveredNodeId)!;
                _dragOffset = mousePos - CanvasToScreen(node.Position);
                _multiDragOffsets.Clear();
                foreach (var id in _selectedNodeIds)
                {
                    var n = GetNode(id);
                    if (n != null)
                        _multiDragOffsets[id] = mousePos - CanvasToScreen(n.Position);
                }
            }
            // Background click → start box selection or deselect
            else
            {
                if (!shiftHeld)
                {
                    _selectedNodeId = null;
                    _selectedNodeIds.Clear();
                }
                _isBoxSelecting = true;
                _boxSelectStart = mousePos;
                _boxSelectEnd = mousePos;
            }
        }

        // Node double-click
        if (canvasHovered && leftDoubleClicked && _hoveredNodeId != null)
        {
            OnNodeDoubleClick?.Invoke(GetNode(_hoveredNodeId)!);
        }

        // Right-click context menu (node or canvas)
        if (canvasHovered && rightClicked && !_isPanning)
        {
            if (_hoveredNodeId != null)
            {
                OnNodeContextMenu?.Invoke(GetNode(_hoveredNodeId)!);
                _contextMenuRequest = new ContextMenuRequest<TNode>
                {
                    Target = ContextMenuTarget.Node,
                    CanvasPosition = ScreenToCanvas(mousePos),
                    Node = GetNode(_hoveredNodeId),
                    SelectedNodeIds = _selectedNodeIds.Count > 1 ? new HashSet<string>(_selectedNodeIds) : null,
                };
            }
            else
            {
                _contextMenuRequest = new ContextMenuRequest<TNode>
                {
                    Target = ContextMenuTarget.Canvas,
                    CanvasPosition = ScreenToCanvas(mousePos),
                    Node = null,
                    SelectedNodeIds = _selectedNodeIds.Count > 1 ? new HashSet<string>(_selectedNodeIds) : null,
                };
            }
            _contextMenuScreenPos = mousePos;
            _contextMenuOpen = true;
            _contextMenuAge = 0;
        }

        // Close context menu on left-click anywhere
        if (_contextMenuOpen && leftClicked)
        {
            // Don't close immediately — DrawContextMenu handles item clicks first
        }

        // Node dragging (multi-selection aware)
        if (_dragNodeId != null && !_isBoxSelecting)
        {
            if (leftDown)
            {
                if (_selectedNodeIds.Count > 1)
                {
                    // Move all selected nodes
                    foreach (var (id, offset) in _multiDragOffsets)
                    {
                        var n = GetNode(id);
                        if (n != null)
                            n.Position = ScreenToCanvas(mousePos - offset);
                    }
                }
                else
                {
                    var node = GetNode(_dragNodeId);
                    if (node != null)
                        node.Position = ScreenToCanvas(mousePos - _dragOffset);
                }
            }
            else
            {
                var node = GetNode(_dragNodeId);
                if (node != null) OnNodeMoved?.Invoke(node);
                _dragNodeId = null;
                _multiDragOffsets.Clear();
            }
        }

        // Box selection
        if (_isBoxSelecting)
        {
            _boxSelectEnd = mousePos;
            if (!leftDown)
            {
                // Finish box selection — select all nodes inside the rectangle
                float minX = Math.Min(_boxSelectStart.X, _boxSelectEnd.X);
                float minY = Math.Min(_boxSelectStart.Y, _boxSelectEnd.Y);
                float maxX = Math.Max(_boxSelectStart.X, _boxSelectEnd.X);
                float maxY = Math.Max(_boxSelectStart.Y, _boxSelectEnd.Y);

                // Only select if dragged a meaningful distance (>5px), else treat as deselect click
                if (maxX - minX > 5 || maxY - minY > 5)
                {
                    if (!shiftHeld) _selectedNodeIds.Clear();
                    foreach (var node in _nodes)
                    {
                        if (!IsNodeVisible(node)) continue;
                        var screenPos = CanvasToScreen(node.Position);
                        var nodeSize = GetNodeSize(node, GetStyle(node)) * _zoom;
                        var nodeCenter = screenPos + nodeSize / 2f;
                        if (nodeCenter.X >= minX && nodeCenter.X <= maxX &&
                            nodeCenter.Y >= minY && nodeCenter.Y <= maxY)
                        {
                            _selectedNodeIds.Add(node.Id);
                        }
                    }
                }
                _isBoxSelecting = false;
            }
        }

        // Link dragging
        if (_isDraggingLink)
        {
            _linkDragEnd = mousePos;
            if (!leftDown)
            {
                TryCompleteLink();
                _isDraggingLink = false;
                _linkSourceNodeId = null;
                _linkSourcePortId = null;
            }
        }

        // Delete selected nodes
        if (canvasHovered && _strideInput?.IsKeyPressed(Stride.Input.Keys.Delete) == true)
        {
            if (_selectedNodeIds.Count > 0)
            {
                OnMultiDelete?.Invoke(_selectedNodeIds);
                foreach (var id in _selectedNodeIds.ToList())
                    RemoveNode(id);
                _selectedNodeIds.Clear();
                _selectedNodeId = null;
            }
            else if (_selectedNodeId != null)
            {
                RemoveNode(_selectedNodeId);
                _selectedNodeId = null;
            }
        }

    }

    private void TryCompleteLink()
    {
        if (_hoveredPortNodeId == null || _hoveredPortId == null) return;
        if (_linkSourceNodeId == null || _linkSourcePortId == null) return;
        if (_hoveredPortNodeId == _linkSourceNodeId) return;

        var targetNode = GetNode(_hoveredPortNodeId);
        var targetPort = targetNode?.Ports.FirstOrDefault(p => p.Id == _hoveredPortId);
        if (targetNode == null || targetPort == null) return;

        if (targetPort.Direction == _linkSourceDir) return;

        string srcNodeId, srcPortId, tgtNodeId, tgtPortId;
        string srcType, tgtType;
        if (_linkSourceDir == PortDirection.Output)
        {
            srcNodeId = _linkSourceNodeId;
            srcPortId = _linkSourcePortId;
            srcType = _linkSourcePortType!;
            tgtNodeId = _hoveredPortNodeId;
            tgtPortId = _hoveredPortId;
            tgtType = targetPort.PortType;
        }
        else
        {
            srcNodeId = _hoveredPortNodeId;
            srcPortId = _hoveredPortId;
            srcType = targetPort.PortType;
            tgtNodeId = _linkSourceNodeId;
            tgtPortId = _linkSourcePortId;
            tgtType = _linkSourcePortType!;
        }

        if (!_portTypes.CanConnect(srcType, tgtType)) return;

        if (_links.Any(l => l.SourceNodeId == srcNodeId && l.SourcePortId == srcPortId &&
                            l.TargetNodeId == tgtNodeId && l.TargetPortId == tgtPortId))
            return;

        var link = new NodeLink($"{srcNodeId}:{srcPortId}->{tgtNodeId}:{tgtPortId}",
            srcNodeId, srcPortId, tgtNodeId, tgtPortId);
        _links.Add(link);
        OnLinkCreated?.Invoke(link);
    }

    // ─── Context Menu ────────────────────────────────────────────

    private void DrawContextMenu()
    {
        if (!_contextMenuOpen || ContextMenu == null || _contextMenuRequest == null) return;

        var items = ContextMenu.GetMenuItems(_contextMenuRequest);
        if (items.Count == 0)
        {
            _contextMenuOpen = false;
            return;
        }

        // Position at right-click location, force to front
        ImGui.SetNextWindowPos(_contextMenuScreenPos);
        ImGui.SetNextWindowSize(new Vector2(200, 0));
        ImGui.SetNextWindowFocus();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.14f, 0.14f, 0.19f, 0.97f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.35f, 0.35f, 0.45f, 0.8f));

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings;

        if (ImGui.Begin("Menu##NodeEditorCtxMenu", flags))
        {
            _contextMenuAge++;

            bool clicked = DrawMenuItems(items);
            if (clicked)
                _contextMenuOpen = false;

            // Close if clicked outside — skip first 2 frames to avoid
            // the opening right-click from immediately dismissing the menu
            if (_contextMenuAge > 2 &&
                !ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
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

    /// <returns>true if an item was clicked</returns>
    private bool DrawMenuItems(List<ContextMenuItem> items, int depth = 0)
    {
        bool anyClicked = false;

        foreach (var item in items)
        {
            if (item.Separator)
            {
                ImGui.Separator();
                continue;
            }

            bool hasColor = item.Color != 0xFFFFFFFF;
            if (hasColor)
            {
                var vec = ImGui.ColorConvertU32ToFloat4(item.Color);
                ImGui.PushStyleColor(ImGuiCol.Text, vec);
            }

            if (item.Children is { Count: > 0 })
            {
                // Render as header + indented children
                var headerColor = new Vector4(0.55f, 0.55f, 0.63f, 1f);
                if (!hasColor) ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
                ImGui.Text(string.IsNullOrEmpty(item.Label) ? "..." : item.Label);
                if (!hasColor) ImGui.PopStyleColor();

                ImGui.Indent(12);
                if (DrawMenuItems(item.Children, depth + 1))
                    anyClicked = true;
                ImGui.Unindent(12);
            }
            else
            {
                var label = item.Icon != null ? $"{item.Icon}  {item.Label}" : item.Label;
                if (string.IsNullOrEmpty(label)) label = "???";
                if (ImGui.Selectable(label))
                {
                    ContextMenu!.OnItemSelected(item, _contextMenuRequest!);
                    anyClicked = true;
                }
            }

            if (hasColor) ImGui.PopStyleColor();
        }

        return anyClicked;
    }

    // ─── Grid ────────────────────────────────────────────────────

    private void DrawGrid(ImDrawListPtr drawList)
    {
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.10f, 0.13f, 1f));
        drawList.AddRectFilled(_canvasOrigin, _canvasOrigin + _canvasSize, bgColor);

        float gridStep = GridSize * _zoom;
        if (gridStep < 4f) return;

        var gridColorMinor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.18f, 0.22f, 1f));
        var gridColorMajor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.22f, 0.22f, 0.28f, 1f));

        float offsetX = _pan.X % gridStep;
        float offsetY = _pan.Y % gridStep;

        int lineIdx = 0;
        for (float x = offsetX; x < _canvasSize.X; x += gridStep, lineIdx++)
        {
            var color = lineIdx % 4 == 0 ? gridColorMajor : gridColorMinor;
            drawList.AddLine(
                _canvasOrigin + new Vector2(x, 0),
                _canvasOrigin + new Vector2(x, _canvasSize.Y),
                color);
        }

        lineIdx = 0;
        for (float y = offsetY; y < _canvasSize.Y; y += gridStep, lineIdx++)
        {
            var color = lineIdx % 4 == 0 ? gridColorMajor : gridColorMinor;
            drawList.AddLine(
                _canvasOrigin + new Vector2(0, y),
                _canvasOrigin + new Vector2(_canvasSize.X, y),
                color);
        }
    }

    // ─── Port position caching ───────────────────────────────────

    private void CachePortPositions(TNode node)
    {
        var style = GetStyle(node);
        var screenPos = CanvasToScreen(node.Position);
        var nodeSize = GetNodeSize(node, style) * _zoom;

        var inputs = node.Ports.Where(p => p.Direction == PortDirection.Input).ToList();
        var outputs = node.Ports.Where(p => p.Direction == PortDirection.Output).ToList();

        float headerH = style.HeaderHeight * _zoom;
        float portSpacing = style.PortSpacing * _zoom;

        for (int i = 0; i < inputs.Count; i++)
        {
            float y = screenPos.Y + headerH + portSpacing * (i + 0.5f);
            _portScreenPositions[(node.Id, inputs[i].Id)] = new Vector2(screenPos.X, y);
        }

        for (int i = 0; i < outputs.Count; i++)
        {
            float y = screenPos.Y + headerH + portSpacing * (i + 0.5f);
            _portScreenPositions[(node.Id, outputs[i].Id)] = new Vector2(screenPos.X + nodeSize.X, y);
        }
    }

    // ─── Links ───────────────────────────────────────────────────

    private void DrawLinks(ImDrawListPtr drawList)
    {
        foreach (var link in _links)
        {
            if (!_portScreenPositions.TryGetValue((link.SourceNodeId, link.SourcePortId), out var start)) continue;
            if (!_portScreenPositions.TryGetValue((link.TargetNodeId, link.TargetPortId), out var end)) continue;

            var srcNode = GetNode(link.SourceNodeId);
            var srcPort = srcNode?.Ports.FirstOrDefault(p => p.Id == link.SourcePortId);
            uint color = srcPort?.Color ?? 0xFFFFFFFF;

            DrawBezierLink(drawList, start, end, color, LinkThickness * _zoom);
        }
    }

    private void DrawLinkDrag(ImDrawListPtr drawList)
    {
        if (!_isDraggingLink || _linkSourceNodeId == null || _linkSourcePortId == null) return;
        if (!_portScreenPositions.TryGetValue((_linkSourceNodeId, _linkSourcePortId), out var start)) return;

        bool canDrop = _hoveredPortId != null && _hoveredPortNodeId != null &&
                       _hoveredPortNodeId != _linkSourceNodeId;

        uint color = canDrop
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.40f, 0.90f, 0.40f, 0.9f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.90f, 0.90f, 0.90f, 0.5f));

        var end = _linkDragEnd;
        if (_linkSourceDir == PortDirection.Input)
            DrawBezierLink(drawList, end, start, color, LinkThickness * _zoom);
        else
            DrawBezierLink(drawList, start, end, color, LinkThickness * _zoom);
    }

    private void DrawBezierLink(ImDrawListPtr drawList, Vector2 start, Vector2 end, uint color, float thickness)
    {
        float dx = Math.Abs(end.X - start.X) * LinkCurvature;
        float minCurve = 50f * _zoom;
        dx = Math.Max(dx, minCurve);

        var cp1 = new Vector2(start.X + dx, start.Y);
        var cp2 = new Vector2(end.X - dx, end.Y);
        drawList.AddBezierCubic(start, cp1, cp2, end, color, thickness);
    }

    // ─── Nodes ───────────────────────────────────────────────────

    private void DrawNodes(ImDrawListPtr drawList)
    {
        foreach (var node in _nodes)
        {
            if (IsNodeVisible(node))
                DrawNode(drawList, node);
        }
    }

    private void DrawNode(ImDrawListPtr drawList, TNode node)
    {
        var style = GetStyle(node);
        var screenPos = CanvasToScreen(node.Position);
        var nodeSize = GetNodeSize(node, style) * _zoom;
        var nodeMax = screenPos + nodeSize;
        bool isSelected = node.Id == _selectedNodeId || _selectedNodeIds.Contains(node.Id);
        bool isHovered = node.Id == _hoveredNodeId;

        float rounding = style.Rounding * _zoom;
        float headerH = style.HeaderHeight * _zoom;

        // Shadow
        var shadowOffset = new Vector2(3, 3) * _zoom;
        var shadowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.3f));
        drawList.AddRectFilled(screenPos + shadowOffset, nodeMax + shadowOffset, shadowColor, rounding);

        // Body
        var bodyColor = ImGui.ColorConvertFloat4ToU32(style.BodyColor);
        drawList.AddRectFilled(screenPos, nodeMax, bodyColor, rounding);

        // Header
        var headerColor = ImGui.ColorConvertFloat4ToU32(
            isHovered ? style.HeaderColor with { W = 1f } : style.HeaderColor);
        drawList.AddRectFilled(screenPos, new Vector2(nodeMax.X, screenPos.Y + headerH), headerColor, rounding,
            ImDrawFlags.RoundCornersTop);

        // Border
        var borderColor = isSelected ? style.SelectedBorderColor : style.BorderColor;
        float borderThick = isSelected ? style.SelectedBorderThickness * _zoom : style.BorderThickness * _zoom;
        drawList.AddRect(screenPos, nodeMax, ImGui.ColorConvertFloat4ToU32(borderColor), rounding, 0, borderThick);

        // Header separator
        var sepColor = ImGui.ColorConvertFloat4ToU32(style.BorderColor with { W = 0.4f });
        drawList.AddLine(
            new Vector2(screenPos.X, screenPos.Y + headerH),
            new Vector2(nodeMax.X, screenPos.Y + headerH),
            sepColor);

        // Title
        float fontSize = Math.Max(13f * _zoom, 8f);
        var titleColor = ImGui.ColorConvertFloat4ToU32(style.TitleColor);
        var titlePos = screenPos + new Vector2(style.BodyPadding * _zoom, (headerH - fontSize) / 2f);
        drawList.AddText(titlePos, titleColor, node.Title);

        // Subtitle (right-aligned in header)
        if (!string.IsNullOrEmpty(node.Subtitle))
        {
            var subtitleColor = ImGui.ColorConvertFloat4ToU32(style.SubtitleColor);
            var subtitleSize = ImGui.CalcTextSize(node.Subtitle);
            var subtitlePos = new Vector2(nodeMax.X - subtitleSize.X - style.BodyPadding * _zoom,
                screenPos.Y + (headerH - fontSize) / 2f);
            drawList.AddText(subtitlePos, subtitleColor, node.Subtitle);
        }

        // Ports
        DrawPorts(drawList, node, style, screenPos, nodeSize);

        // Custom content callback
        if (DrawNodeContent != null)
        {
            var contentMin = new Vector2(screenPos.X + style.BodyPadding * _zoom,
                screenPos.Y + headerH + GetPortsHeight(node, style) * _zoom);
            var contentMax = new Vector2(nodeMax.X - style.BodyPadding * _zoom, nodeMax.Y - style.BodyPadding * _zoom);
            if (contentMax.Y > contentMin.Y)
                DrawNodeContent(node, drawList, contentMin, contentMax);
        }
    }

    private void DrawPorts(ImDrawListPtr drawList, TNode node, NodeStyle style, Vector2 screenPos, Vector2 nodeSize)
    {
        float portRadius = style.PortRadius * _zoom;
        float labelOffset = style.PortLabelOffset * _zoom;
        float fontSize = Math.Max(11f * _zoom, 7f);

        foreach (var port in node.Ports)
        {
            if (!_portScreenPositions.TryGetValue((node.Id, port.Id), out var center)) continue;

            bool isHoveredPort = _hoveredPortNodeId == node.Id && _hoveredPortId == port.Id;
            float radius = isHoveredPort ? portRadius * 1.4f : portRadius;

            // Port circle
            var portBgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.10f, 0.13f, 1f));
            drawList.AddCircleFilled(center, radius + 1.5f * _zoom, portBgColor);
            drawList.AddCircleFilled(center, radius, port.Color);

            // Connected indicator (filled) vs empty (ring)
            bool connected = _links.Any(l =>
                (l.SourceNodeId == node.Id && l.SourcePortId == port.Id) ||
                (l.TargetNodeId == node.Id && l.TargetPortId == port.Id));
            if (!connected)
            {
                drawList.AddCircleFilled(center, radius * 0.5f, portBgColor);
            }

            // Label
            var labelColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.70f, 0.70f, 0.78f, 1f));
            if (port.Direction == PortDirection.Input)
            {
                drawList.AddText(center + new Vector2(labelOffset, -fontSize / 2f), labelColor, port.Label);
            }
            else
            {
                var textSize = ImGui.CalcTextSize(port.Label);
                drawList.AddText(center - new Vector2(labelOffset + textSize.X, fontSize / 2f), labelColor, port.Label);
            }
        }
    }

    // ─── Sizing helpers ──────────────────────────────────────────

    private NodeStyle GetStyle(TNode node)
    {
        return _styles.TryGetValue(node.NodeType, out var s) ? s : _defaultStyle;
    }

    private Vector2 GetNodeSize(TNode node, NodeStyle style)
    {
        float portsH = GetPortsHeight(node, style);
        float contentH = MeasureContentHeight?.Invoke(node) ?? 0f;
        float totalH = style.HeaderHeight + portsH + contentH + style.BodyPadding;
        return new Vector2(style.MinWidth, totalH);
    }

    private float GetPortsHeight(TNode node, NodeStyle style)
    {
        int inputCount = node.Ports.Count(p => p.Direction == PortDirection.Input);
        int outputCount = node.Ports.Count(p => p.Direction == PortDirection.Output);
        int maxPorts = Math.Max(inputCount, outputCount);
        return maxPorts > 0 ? maxPorts * style.PortSpacing : style.PortSpacing;
    }
}
