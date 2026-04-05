using System.Numerics;
using System.Text.Json;
using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;
using HytaleAdmin.Rendering;
using HytaleAdmin.UI.Components;
using HytaleAdmin.UI.NodeEditor;
using Stride.Input;

namespace HytaleAdmin.UI;

public class AdventureView
{
    private readonly HytaleApiClient _client;
    private readonly IGraphBuilderStrategy _strategy = new SchemaGraphBuilder();

    // Plugin/schema state
    private PluginSchemaDto? _schema;
    private PluginEntitySummaryDto[]? _entities;
    private GraphDefinition? _graphDef;
    private bool _loading;
    private bool _loaded;
    private bool _hasRestoredLayout;
    private Vector2 _lastPan;
    private float _lastZoom;
    private float _saveTimer;
    private string? _error;

    // Node editor
    private NodeEditor<SchemaNode>? _editor;
    private AdventureContextMenu? _contextMenu;
    private SchemaNode? _selectedNode;

    // Tree view
    private TreeView<SchemaNode>? _treeView;
    private AdventureTreeDataProvider? _treeDataProvider;
    private AdventureTreeContextMenu? _treeContextMenu;
    private string? _selectedQuestLineId;
    private List<SchemaNode> _allNodes = new();
    private List<NodeLink> _allLinks = new();

    // Inline action form
    private bool _showActionForm;
    private PluginActionDto? _actionFormAction;
    private Dictionary<string, string> _actionFormValues = new();
    private Dictionary<string, string> _actionTextBuffers = new();
    private bool _actionExecuting;
    private string? _actionFormResult;

    // Detail panel state
    private readonly PluginPanelState _pluginState;
    private string _activeSubModule = "authoring";

    // Action feedback
    private string? _actionStatus;
    private float _actionStatusTimer;

    // Tutorial guide
    private bool _showTutorial;

    // Map picker + wizard
    private MapPickerDialog? _mapPicker;
    private QuestWizard? _questWizard;

    private static readonly Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly Vector4 AccentColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly Vector4 SaveColor = new(0.31f, 0.80f, 0.40f, 1f);
    private static readonly Vector4 DangerColor = new(0.65f, 0.18f, 0.25f, 1f);
    private static readonly Vector4 ButtonTextColor = new(0.10f, 0.10f, 0.12f, 1f);

    private const string PluginId = "hyadventure";

    private readonly InputManager _input;
    private readonly ClipboardService _clipboard;
    private readonly EntityDataService _entityData;

    public AdventureView(ServiceContainer services, MapRenderer mapRenderer)
    {
        _client = services.ApiClient;
        _input = services.Game.Input;
        _clipboard = services.Clipboard;
        _entityData = services.EntityData;
        _pluginState = new PluginPanelState(_client);
        _mapPicker = new MapPickerDialog(mapRenderer);
        _questWizard = new QuestWizard(_client, _entityData, mapRenderer);
        _questWizard.OnQuestCreated = () => { _loaded = false; _selectedNode = null; };
    }

    public void Draw(float availWidth, float availHeight, float logHeight)
    {
        if (!_loaded && !_loading)
            _ = Task.Run(LoadAsync);

        if (_loading)
        {
            ImGui.TextColored(DimColor, "Loading adventure data...");
            return;
        }

        if (_error != null)
        {
            ImGui.TextColored(DangerColor, _error);
            if (ImGui.SmallButton("Retry"))
            {
                _error = null;
                _loaded = false;
            }
            return;
        }

        if (_editor == null) return;

        // Toolbar
        DrawToolbar();
        ImGui.Separator();

        float contentHeight = availHeight - logHeight - ImGui.GetStyle().ItemSpacing.Y;
        float toolbarH = ImGui.GetCursorPosY() - ImGui.GetWindowPos().Y;
        // Subtract what we've already drawn
        float remainH = contentHeight - toolbarH + ImGui.GetWindowPos().Y;
        remainH = Math.Max(remainH, 200f);

        // Layout: tree (left) + canvas (center) + detail (right)
        float treeWidth = 250f;
        float detailWidth = _selectedNode != null ? 300f : 0f;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        float canvasWidth = availWidth - treeWidth - detailWidth
            - spacing  // tree–canvas gap
            - (_selectedNode != null ? spacing : 0f); // canvas–detail gap

        // Multi-delete from context menu
        if (_contextMenu?.PendingMultiDelete != null)
        {
            var ids = _contextMenu.PendingMultiDelete;
            _contextMenu.PendingMultiDelete = null;
            DeleteMultipleNodes(ids);
        }

        // Check if context menu triggered an action form (graph or tree)
        PluginActionDto? pendingAction = _contextMenu?.PendingAction ?? _treeContextMenu?.PendingAction;
        SchemaNode? pendingNode = _contextMenu?.PendingNode ?? _treeContextMenu?.PendingNode;
        if (pendingAction != null && !_showActionForm)
        {
            _actionFormAction = pendingAction;
            _actionFormValues = new();
            _actionTextBuffers = new();
            _actionFormResult = null;
            _actionExecuting = false;
            PreFillActionFromContext(_actionFormAction, pendingNode);
            _contextMenu?.ClearPending();
            _treeContextMenu?.ClearPending();
            _showActionForm = true;
        }

        // Left: tree panel
        // Tree panel with wizard button at top
        ImGui.BeginChild("TreeWithButton", new Vector2(treeWidth, remainH), ImGuiChildFlags.None);
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.40f, 0.70f, 0.95f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.Button("New Quest##wizard", new Vector2(-1, 0)))
            _questWizard?.Open();
        ImGui.PopStyleColor(2);
        float btnH = ImGui.GetItemRectSize().Y + ImGui.GetStyle().ItemSpacing.Y;
        _treeView?.Draw(treeWidth, remainH - btnH, _input);
        ImGui.EndChild();

        ImGui.SameLine();

        // Center: graph canvas
        _editor.Draw(canvasWidth, remainH, _input);

        // Right: detail panel
        if (_selectedNode != null)
        {
            ImGui.SameLine();
            ImGui.BeginChild("AdventureDetail", new Vector2(detailWidth, remainH), ImGuiChildFlags.Borders);
            DrawDetailPanel();
            ImGui.EndChild();
        }

        // Auto-save layout on pan/zoom change (debounced)
        if (_editor != null)
        {
            if (_editor.Pan != _lastPan || Math.Abs(_editor.Zoom - _lastZoom) > 0.001f)
            {
                _lastPan = _editor.Pan;
                _lastZoom = _editor.Zoom;
                _saveTimer = 1.0f; // save after 1s of inactivity
            }
            if (_saveTimer > 0)
            {
                _saveTimer -= 0.016f;
                if (_saveTimer <= 0) SaveLayout();
            }
        }

        // Floating windows
        DrawActionFormPopup();
        DrawTutorialWindow();
        _mapPicker?.Draw();
        _questWizard?.Draw();
    }

    // ─── Data loading ────────────────────────────────────────────

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _schema = await _client.GetPluginSchemaAsync(PluginId);
            if (_schema == null)
            {
                _error = "HyAdventure plugin not found. Is server running?";
                return;
            }

            if (!_strategy.CanHandle(_schema))
            {
                _error = "Schema has no graph hints. Update server plugin.";
                return;
            }

            var entityResp = await _client.GetPluginEntitiesAsync(PluginId);
            _entities = entityResp?.Data;
            if (_entities == null)
            {
                _error = "Failed to load entities.";
                return;
            }

            BuildGraph();
            _loaded = true;
        }
        catch (Exception ex)
        {
            _error = $"Load failed: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private void BuildGraph()
    {
        _graphDef = _strategy.BuildDefinition(_schema!);

        // Resolve entity values for graph nodes
        var valuesCache = new Dictionary<string, Dictionary<string, string>>();
        var nodes = _strategy.CreateNodes(_graphDef, _entities!, entityId =>
        {
            if (valuesCache.TryGetValue(entityId, out var cached))
                return cached;

            // Synchronous fetch for initial load — values needed for link extraction
            try
            {
                var dto = _client.GetPluginEntityValuesAsync(PluginId, entityId).GetAwaiter().GetResult();
                if (dto?.Values != null)
                {
                    var flat = FlattenValues(dto.Values);
                    valuesCache[entityId] = flat;
                    return flat;
                }
            }
            catch { }
            return null;
        });

        var links = _strategy.ExtractLinks(_graphDef, nodes);
        _allNodes = nodes;
        _allLinks = links;

        // Tree view
        _treeDataProvider = new AdventureTreeDataProvider();
        _treeDataProvider.SetNodes(nodes, links);
        _treeView = new TreeView<SchemaNode>(_treeDataProvider);
        _treeContextMenu = new AdventureTreeContextMenu(_schema!);
        _treeView.ContextMenu = _treeContextMenu;
        _treeView.OnItemSelected = OnTreeItemSelected;

        // Create editor with connection rules
        _editor = new NodeEditor<SchemaNode>(_graphDef.ConnectionRules);

        // Apply styles
        foreach (var (nodeType, style) in _graphDef.Styles)
            _editor.SetStyle(nodeType, style);

        // Add nodes + links
        foreach (var node in nodes) _editor.AddNode(node);
        foreach (var link in links) _editor.AddLink(link);

        // Context menu
        _contextMenu = new AdventureContextMenu(_client, _schema!, _graphDef);
        _contextMenu.OnGraphMutated = () =>
        {
            _loaded = false;
            _selectedNode = null;
        };
        _editor.ContextMenu = _contextMenu;

        // Wire callbacks
        _editor.OnNodeSelected = OnNodeSelected;
        _editor.OnLinkCreated = OnLinkCreated;
        _editor.OnLinkRemoved = OnLinkRemoved;
        _editor.OnCanvasDoubleClick = OnCanvasDoubleClick;
        _editor.OnNodeMoved = _ => SaveLayout();
        _editor.OnMultiDelete = ids => DeleteMultipleNodes(new HashSet<string>(ids));

        // Custom content: show task/reward counts inside objective nodes
        _editor.DrawNodeContent = DrawNodeBody;
        _editor.MeasureContentHeight = MeasureNodeBody;
        _editor.DrawOverlay = DrawCanvasOverlay;

        // Restore saved layout or center on first load
        var layout = GraphLayoutState.Load("adventure");
        if (layout.HasData)
        {
            layout.ApplyTo(_editor);
            _hasRestoredLayout = true;
        }
        else
        {
            _editor.CenterOnNodes();
        }
    }

    private static Dictionary<string, string> FlattenValues(Dictionary<string, JsonElement> raw)
    {
        var flat = new Dictionary<string, string>();
        foreach (var kv in raw)
        {
            flat[kv.Key] = kv.Value.ValueKind switch
            {
                JsonValueKind.String => kv.Value.GetString() ?? "",
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => kv.Value.GetRawText(),
                JsonValueKind.Array => kv.Value.GetRawText(),
                _ => kv.Value.GetRawText()
            };
        }
        return flat;
    }

    // ─── Toolbar ─────────────────────────────────────────────────

    private void DrawToolbar()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, AccentColor);
        ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.SmallButton("Refresh"))
        {
            _loaded = false;
            _selectedNode = null;
        }
        ImGui.PopStyleColor(2);

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Button, AccentColor);
        ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.SmallButton("Center"))
            _editor?.CenterOnNodes();
        ImGui.PopStyleColor(2);

        ImGui.SameLine();

        var rebuildColor = new Vector4(0.85f, 0.45f, 0.20f, 1f);
        ImGui.PushStyleColor(ImGuiCol.Button, rebuildColor);
        ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.SmallButton("Rebuild"))
            ExecuteRebuild();
        ImGui.PopStyleColor(2);

        ImGui.SameLine();


        if (_selectedQuestLineId != null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, SaveColor);
            ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
            if (ImGui.SmallButton("Show All"))
                ClearFilter();
            ImGui.PopStyleColor(2);
            ImGui.SameLine();
        }


        if (_actionStatus != null)
        {
            var statusColor = _actionStatus.StartsWith("Error") ? DangerColor : SaveColor;
            ImGui.SameLine();
            ImGui.TextColored(statusColor, _actionStatus);
            _actionStatusTimer -= 0.016f; // ~60fps
            if (_actionStatusTimer <= 0) _actionStatus = null;
        }
    }

    // ─── Detail panel ────────────────────────────────────────────

    // Detail panel dirty tracking
    private string? _detailEditingNodeId;
    private Dictionary<string, string> _detailEditedValues = new();
    private Dictionary<string, string> _detailTextBuffers = new();
    private HashSet<string> _detailDirtyFields = new();
    private bool _detailSaving;

    private void DrawDetailPanel()
    {
        if (_selectedNode == null) return;

        var nodeType = _graphDef?.NodeTypes.FirstOrDefault(t => t.GroupId == _selectedNode.NodeType);
        var group = _schema?.Groups.FirstOrDefault(g => g.Id == _selectedNode.NodeType);

        // Reset edit state when node changes
        if (_detailEditingNodeId != _selectedNode.Id)
        {
            _detailEditingNodeId = _selectedNode.Id;
            _detailEditedValues = new Dictionary<string, string>(_selectedNode.Values);
            _detailTextBuffers = new();
            _detailDirtyFields = new();
        }

        ImGui.TextColored(AccentColor, _selectedNode.Title);
        if (nodeType != null)
        {
            ImGui.SameLine();
            ImGui.TextColored(DimColor, nodeType.Label);
        }
        ImGui.Separator();

        if (ImGui.BeginChild("DetailFields", new Vector2(0, -30)))
        {
            if (group != null)
            {
                foreach (var field in group.Fields)
                {
                    if (!_detailEditedValues.TryGetValue(field.Id, out var val))
                        val = _selectedNode.Values.GetValueOrDefault(field.Id, "");

                    if (field.ReadOnly)
                    {
                        ImGui.TextColored(DimColor, field.Label);
                        ImGui.TextWrapped(val);
                    }
                    else
                    {
                        bool isDirty = _detailDirtyFields.Contains(field.Id);
                        if (isDirty) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.95f, 0.75f, 0.20f, 1f));
                        ImGui.Text(field.Label);
                        if (isDirty) ImGui.PopStyleColor();

                        ImGui.SetNextItemWidth(-1);
                        DrawDetailField(field, val);
                    }
                    ImGui.Spacing();
                }
            }
            else
            {
                foreach (var (key, val) in _selectedNode.Values)
                {
                    ImGui.TextColored(DimColor, key);
                    ImGui.SameLine();
                    ImGui.TextWrapped(val);
                }
            }

            // Location node: pick coordinates on map
            if (_selectedNode.EntityPrefix == "loc")
            {
                ImGui.Spacing();
                var mapBtnColor = new Vector4(0.36f, 0.55f, 0.85f, 1f);
                ImGui.PushStyleColor(ImGuiCol.Button, mapBtnColor);
                ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
                if (ImGui.Button("Pick Location on Map", new Vector2(-1, 0)))
                {
                    float? initX = null, initZ = null;
                    if (_selectedNode.Values.TryGetValue("loc_x", out var xStr) && float.TryParse(xStr, System.Globalization.CultureInfo.InvariantCulture, out var px))
                        initX = px;
                    if (_selectedNode.Values.TryGetValue("loc_z", out var zStr) && float.TryParse(zStr, System.Globalization.CultureInfo.InvariantCulture, out var pz))
                        initZ = pz;
                    var capturedNode = _selectedNode;
                    _mapPicker?.Open($"Pick Location: {_selectedNode.Title}", (x, z) =>
                        OnMapLocationPicked(capturedNode, x, z), initX, initZ);
                }
                ImGui.PopStyleColor(2);
            }

            // Quest line node: assign quest giver via map picker
            if (_selectedNode.EntityPrefix == "auth-line")
            {
                ImGui.Spacing();
                var qgColor = new Vector4(0.77f, 0.29f, 0.55f, 1f);
                ImGui.PushStyleColor(ImGuiCol.Button, qgColor);
                ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
                if (ImGui.Button("Assign Quest Giver (pick NPC)", new Vector2(-1, 0)))
                {
                    var capturedNode = _selectedNode;
                    _mapPicker?.OpenEntityPicker(
                        "Pick Quest Giver NPC",
                        _entityData,
                        null,
                        entity => OnAssignQuestGiverFromMap(capturedNode, entity));
                }
                ImGui.PopStyleColor(2);
            }
        }
        ImGui.EndChild();

        // Save bar
        DrawDetailSaveBar();
    }

    private void DrawDetailField(FieldDefinitionDto field, string val)
    {
        switch (field.Type)
        {
            case "enum":
            {
                var options = field.EnumValues ?? [];
                int selected = Array.IndexOf(options, val);
                if (selected < 0) selected = 0;
                if (ImGui.Combo($"##{field.Id}_detail", ref selected, options, options.Length))
                {
                    _detailEditedValues[field.Id] = options[selected];
                    _detailDirtyFields.Add(field.Id);
                }
                break;
            }
            case "bool":
            {
                bool v = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                if (ImGui.Checkbox($"##{field.Id}_detail", ref v))
                {
                    _detailEditedValues[field.Id] = v ? "true" : "false";
                    _detailDirtyFields.Add(field.Id);
                }
                break;
            }
            case "int":
            {
                int v = int.TryParse(val, out var p) ? p : 0;
                bool changed = field.Min.HasValue && field.Max.HasValue
                    ? ImGui.SliderInt($"##{field.Id}_detail", ref v, (int)field.Min.Value, (int)field.Max.Value)
                    : ImGui.InputInt($"##{field.Id}_detail", ref v);
                if (changed)
                {
                    _detailEditedValues[field.Id] = v.ToString();
                    _detailDirtyFields.Add(field.Id);
                }
                break;
            }
            default: // string, float, etc.
            {
                if (!_detailTextBuffers.ContainsKey(field.Id))
                    _detailTextBuffers[field.Id] = val;
                var buf = _detailTextBuffers[field.Id];
                if (ImGui.InputText($"##{field.Id}_detail", ref buf, 256))
                {
                    _detailTextBuffers[field.Id] = buf;
                    _detailEditedValues[field.Id] = buf;
                    _detailDirtyFields.Add(field.Id);
                }
                break;
            }
        }
    }

    private void DrawDetailSaveBar()
    {
        if (_detailDirtyFields.Count > 0 && _selectedNode != null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, SaveColor);
            ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
            if (ImGui.Button($"Save ({_detailDirtyFields.Count} changes)##detailSave", new Vector2(-1, 0)) && !_detailSaving)
            {
                _detailSaving = true;
                var entityId = _selectedNode.EntityId;
                var dirty = _detailDirtyFields.ToDictionary(f => f, f => _detailEditedValues.GetValueOrDefault(f, ""));
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // POST directly — UpdatePluginEntityAsync wraps in {values:{...}}
                        var result = await _client.UpdatePluginEntityAsync(PluginId, entityId, dirty);
                        if (result?.Success == true)
                        {
                            // Update node's cached values
                            foreach (var (k, v) in dirty)
                                _selectedNode.Values[k] = v;
                            _detailDirtyFields.Clear();
                            _detailTextBuffers.Clear();
                            SetStatus("Saved", false);
                        }
                        else
                        {
                            // Try raw HTTP to get better error info
                            SetStatus($"Error: {result?.Error ?? "Update rejected by server"}", true);
                            Console.Error.WriteLine($"[AdventureView] Save failed for {entityId}: {result?.Error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        SetStatus($"Error: {ex.Message}", true);
                        Console.Error.WriteLine($"[AdventureView] Save exception: {ex}");
                    }
                    finally { _detailSaving = false; }
                });
            }
            ImGui.PopStyleColor(2);
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.Button("No changes##detailSave", new Vector2(-1, 0));
            ImGui.EndDisabled();
        }
    }


    // ─── Node body content ───────────────────────────────────────

    private void DrawNodeBody(SchemaNode node, ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        float y = min.Y;
        float lineH = 14f;
        var textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.60f, 0.60f, 0.68f, 1f));

        // Show key fields as compact text
        var displayFields = GetBodyDisplayFields(node);
        foreach (var (label, value) in displayFields)
        {
            if (y + lineH > max.Y) break;
            drawList.AddText(new Vector2(min.X, y), textColor, $"{label}: {value}");
            y += lineH;
        }
    }

    private float MeasureNodeBody(SchemaNode node)
    {
        var fields = GetBodyDisplayFields(node);
        return fields.Count * 14f + 4f;
    }

    private List<(string label, string value)> GetBodyDisplayFields(SchemaNode node)
    {
        var result = new List<(string, string)>();

        if (node.EntityPrefix == "auth-line")
        {
            if (node.Values.TryGetValue("line_category", out var cat))
                result.Add(("Category", cat));
            if (node.Values.TryGetValue("line_titleKey", out var title) && !string.IsNullOrEmpty(title))
                result.Add(("Title", title));
        }
        else if (node.EntityPrefix == "auth-obj")
        {
            if (node.Values.TryGetValue("aobj_category", out var cat))
                result.Add(("Category", cat));
            if (node.Values.TryGetValue("aobj_taskSetCount", out var phases))
                result.Add(("Phases", phases));
            if (node.Values.TryGetValue("aobj_completionCount", out var rewards))
                result.Add(("Rewards", rewards));
        }
        else if (node.EntityPrefix == "npc-assign")
        {
            if (node.Values.TryGetValue("npc_role", out var role) && !string.IsNullOrEmpty(role))
                result.Add(("Role", role));
            if (node.Values.TryGetValue("npc_type", out var type))
                result.Add(("Type", type));
        }
        else if (node.EntityPrefix == "dlg")
        {
            if (node.Values.TryGetValue("dlg_entityNameText", out var nameText) && !string.IsNullOrEmpty(nameText))
                result.Add(("Name", nameText));
            else if (node.Values.TryGetValue("dlg_entityNameKey", out var name) && !string.IsNullOrEmpty(name))
                result.Add(("Name", name));

            if (node.Values.TryGetValue("dlg_dialogText", out var dlgText) && !string.IsNullOrEmpty(dlgText))
            {
                var preview = dlgText.Length > 30 ? dlgText[..30] + "..." : dlgText;
                result.Add(("Text", preview));
            }
        }
        else if (node.EntityPrefix == "loc")
        {
            if (node.Values.TryGetValue("loc_id", out var locId))
                result.Add(("ID", locId));
        }

        return result;
    }

    // ─── Canvas overlay (guide button) ─────────────────────────

    private void DrawCanvasOverlay(ImDrawListPtr drawList, Vector2 origin, Vector2 size)
    {
        // "? Guide" button in top-right corner of canvas
        float btnW = 60f;
        float btnH = 22f;
        float pad = 8f;
        var btnMin = new Vector2(origin.X + size.X - btnW - pad, origin.Y + pad);
        var btnMax = btnMin + new Vector2(btnW, btnH);

        var mousePos = ImGui.GetIO().MousePos;
        bool hovered = mousePos.X >= btnMin.X && mousePos.X <= btnMax.X &&
                       mousePos.Y >= btnMin.Y && mousePos.Y <= btnMax.Y;

        uint bgColor = hovered
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.40f, 0.70f, 0.95f, 0.9f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.25f, 0.33f, 0.85f));
        uint textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, hovered ? 1f : 0.8f));

        drawList.AddRectFilled(btnMin, btnMax, bgColor, 4f);
        drawList.AddRect(btnMin, btnMax,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.45f, 0.45f, 0.55f, 0.6f)), 4f);

        var labelSize = ImGui.CalcTextSize("? Guide");
        var textPos = btnMin + (new Vector2(btnW, btnH) - labelSize) * 0.5f;
        drawList.AddText(textPos, textColor, "? Guide");

        // Click detection
        if (hovered && ImGui.GetIO().MouseClicked[0])
            _showTutorial = !_showTutorial;
    }

    private void DrawTutorialWindow()
    {
        if (!_showTutorial) return;

        ImGui.SetNextWindowSize(new Vector2(360, 0));
        ImGui.SetNextWindowFocus();

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.12f, 0.12f, 0.17f, 0.97f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.40f, 0.70f, 0.95f, 0.5f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 12));

        bool open = true;
        if (ImGui.Begin("Quest Line Guide", ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            var accent = AccentColor;
            var dim = DimColor;
            var green = SaveColor;

            ImGui.TextColored(accent, "Building a Quest Line");
            ImGui.Spacing();

            DrawStep(1, "Create a Quest Line",
                "Right-click the tree panel or graph canvas\nand select Create > Quest Line.\nGive it an ID, category, and title key.");

            DrawStep(2, "Create Objectives",
                "Right-click and create Objectives.\nEach objective represents a goal\nthe player must complete.");

            DrawStep(3, "Link Objectives to Line",
                "Drag from Quest Line's green\n'Objectives' port to an Objective's\n'From Line' input port.");

            DrawStep(4, "Add Tasks to Objectives",
                "Right-click an Objective node and\nchoose Add Phase, then Add Task.\nTask types: Gather, Craft, Kill, etc.");

            DrawStep(5, "Add Rewards",
                "Right-click an Objective and choose\nAdd Reward. Types: Give Items,\nClear Items, or Reputation.");

            DrawStep(6, "Assign Quest Giver NPC",
                "Create an NPC Assignment node,\nselect a role from the dropdown.\nDrag Quest Line's pink 'Quest Giver'\nport to the NPC node.");

            DrawStep(7, "Add Dialog",
                "Create a Dialog node with name\nand text translation keys.\nDrag from NPC's gold 'Dialog' port\nto the Dialog node.");

            DrawStep(8, "Branch Quest Lines",
                "Drag from Quest Line's orange\n'Next Lines' port to another Quest Line's\n'From' port to create follow-up paths.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(dim, "Tips");
            ImGui.BulletText("Middle-click drag to pan");
            ImGui.BulletText("Scroll to zoom");
            ImGui.BulletText("Click a quest line in the tree\nto filter the graph");
        }
        ImGui.End();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);

        if (!open) _showTutorial = false;
    }

    private static void DrawStep(int num, string title, string body)
    {
        var stepColor = new Vector4(0.40f, 0.70f, 0.95f, 1f);
        var titleColor = new Vector4(0.90f, 0.90f, 0.95f, 1f);
        var bodyColor = new Vector4(0.63f, 0.63f, 0.71f, 1f);

        ImGui.TextColored(stepColor, $"{num}.");
        ImGui.SameLine();
        ImGui.TextColored(titleColor, title);
        ImGui.PushStyleColor(ImGuiCol.Text, bodyColor);
        ImGui.TextWrapped(body);
        ImGui.PopStyleColor();
        ImGui.Spacing();
    }

    // ─── Action form popup ──────────────────────────────────────

    private void PreFillActionFromContext(PluginActionDto action, SchemaNode? node)
    {
        // Initialize all fields with defaults
        foreach (var group in action.Groups)
        foreach (var field in group.Fields)
        {
            _actionFormValues[field.Id] = field.EnumValues is { Length: > 0 }
                ? field.EnumValues[0] : "";
        }

        // Auto-generate ID for creation actions
        if (IsCreationAction(action.Id))
        {
            string autoId = GenerateEntityId(action.Id, node);
            _actionFormValues["id"] = autoId;
        }

        if (node == null) return;
        string rawId = StripPrefix(node.EntityId);

        // Pre-fill entity reference fields based on node type
        foreach (var group in action.Groups)
        foreach (var field in group.Fields)
        {
            if (field.Id is "questLineId" && node.EntityPrefix == "auth-line")
                _actionFormValues[field.Id] = rawId;
            else if (field.Id is "objectiveId" && node.EntityPrefix == "auth-obj")
                _actionFormValues[field.Id] = rawId;
            else if (field.Id is "npcAssignmentId" && node.EntityPrefix == "npc-assign")
                _actionFormValues[field.Id] = rawId;
        }
    }

    private static bool IsCreationAction(string actionId) =>
        actionId is "createFullQuest" or "createQuestLine" or "createObjective" or "createNpcAssignment" or "createDialog" or "createLocation";

    private static readonly HashSet<string> _hiddenFormFields = new() { "id" };

    private bool IsHiddenField(PluginActionDto action, FieldDefinitionDto field)
    {
        // Hide auto-generated IDs
        if (IsCreationAction(action.Id) && _hiddenFormFields.Contains(field.Id))
            return true;

        // Contextual task fields — only show fields relevant to selected task type
        if (action.Id == "addTask")
            return !IsFieldRelevantForTaskType(field.Id);

        return false;
    }

    private bool IsFieldRelevantForTaskType(string fieldId)
    {
        // Always show these
        if (fieldId is "type" or "count" or "objectiveId" or "taskSetIndex")
            return true;

        string taskType = _actionFormValues.GetValueOrDefault("type", "GATHER");
        return taskType switch
        {
            "GATHER" => fieldId is "blockTagOrItemId",
            "CRAFT" => fieldId is "blockTagOrItemId",
            "USE_BLOCK" => fieldId is "blockTagOrItemId",
            "USE_ENTITY" => fieldId is "taskId" or "animationIdToPlay" or "dialogEntityNameKey" or "dialogKey",
            "REACH_LOCATION" => fieldId is "targetLocationId",
            "KILL" => fieldId is "npcGroupId",
            "KILL_SPAWN_MARKER" => fieldId is "npcGroupId" or "spawnMarkerIds" or "radius",
            "BOUNTY" => fieldId is "npcId",
            "TREASURE_MAP" => false, // no extra fields
            _ => true, // unknown type — show all
        };
    }

    private string GenerateEntityId(string actionId, SchemaNode? contextNode)
    {
        string parentPrefix = "";
        if (contextNode != null)
        {
            string rawParentId = StripPrefix(contextNode.EntityId);
            parentPrefix = ToSnakeCase(rawParentId) + "_";
        }

        string typePrefix = actionId switch
        {
            "createQuestLine" => "quest_line",
            "createObjective" => parentPrefix + "objective",
            "createNpcAssignment" => parentPrefix + "npc",
            "createDialog" => parentPrefix + "dialog",
            "createFullQuest" => "quest",
            "createLocation" => "location",
            _ => "entity",
        };

        // Append short unique suffix
        int suffix = (int)(DateTime.UtcNow.Ticks % 10000);
        return $"{typePrefix}_{suffix}";
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var result = new System.Text.StringBuilder();
        foreach (char c in input)
        {
            if (c is ' ' or '-' or '.' or ':')
                result.Append('_');
            else if (char.IsUpper(c) && result.Length > 0 && result[^1] != '_')
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
                result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

    private void DrawActionFormPopup()
    {
        if (!_showActionForm || _actionFormAction == null) return;

        ImGui.SetNextWindowSize(new Vector2(460, 0));
        bool open = true;
        var formTitle = string.IsNullOrEmpty(_actionFormAction.Label) ? "Action" : _actionFormAction.Label;
        if (ImGui.Begin($"{formTitle}##ActionForm", ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.AlwaysAutoResize))
        {
            // Render fields (skip auto-generated hidden fields)
            foreach (var group in _actionFormAction.Groups)
            {
                foreach (var field in group.Fields)
                {
                    if (IsHiddenField(_actionFormAction, field)) continue;
                    DrawActionFormField(field);
                }
            }

            ImGui.Spacing();

            // Result message
            if (_actionFormResult != null)
            {
                var color = _actionFormResult.StartsWith("Error") ? DangerColor : SaveColor;
                ImGui.TextColored(color, _actionFormResult);
                ImGui.Spacing();
            }

            // Buttons
            float btnWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;

            bool disabled = _actionExecuting;
            if (disabled) ImGui.BeginDisabled();
            ImGui.PushStyleColor(ImGuiCol.Button, AccentColor);
            ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
            if (ImGui.Button(_actionExecuting ? "Executing..." : _actionFormAction.Label,
                    new Vector2(btnWidth, 0)))
            {
                ExecuteActionForm();
            }
            ImGui.PopStyleColor(2);
            if (disabled) ImGui.EndDisabled();

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(btnWidth, 0)))
            {
                _showActionForm = false;
                _actionFormAction = null;
            }
        }
        ImGui.End();

        if (!open)
        {
            _showActionForm = false;
            _actionFormAction = null;
        }
    }

    private void DrawActionFormField(FieldDefinitionDto field)
    {
        if (!_actionFormValues.TryGetValue(field.Id, out var val))
            val = "";

        // Label line (before the widget)
        if (field.Required)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.91f, 0.27f, 0.38f, 1f));
            ImGui.Text("*");
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }
        ImGui.TextColored(DimColor, field.Label);

        // Widget on next line, full width
        ImGui.SetNextItemWidth(-1);

        switch (field.Type)
        {
            case "string":
            {
                if (!_actionTextBuffers.ContainsKey(field.Id))
                    _actionTextBuffers[field.Id] = val;
                var buf = _actionTextBuffers[field.Id];
                if (ImGui.InputText($"##{field.Id}", ref buf, 256))
                {
                    _actionTextBuffers[field.Id] = buf;
                    _actionFormValues[field.Id] = buf;
                }
                break;
            }
            case "int":
            {
                int v = int.TryParse(val, out var p) ? p : 0;
                bool changed = field.Min.HasValue && field.Max.HasValue
                    ? ImGui.SliderInt($"##{field.Id}", ref v, (int)field.Min.Value, (int)field.Max.Value)
                    : ImGui.InputInt($"##{field.Id}", ref v);
                if (changed) _actionFormValues[field.Id] = v.ToString();
                break;
            }
            case "float":
            {
                float v = float.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0f;
                bool changed = ImGui.InputFloat($"##{field.Id}", ref v, 0.1f, 1f, "%.2f");
                if (changed) _actionFormValues[field.Id] = v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                break;
            }
            case "bool":
            {
                bool v = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                if (ImGui.Checkbox($"##{field.Id}", ref v))
                    _actionFormValues[field.Id] = v ? "true" : "false";
                break;
            }
            case "enum":
            {
                var options = field.EnumValues ?? [];
                int selected = Array.IndexOf(options, val);
                if (selected < 0) selected = 0;
                if (ImGui.Combo($"##{field.Id}", ref selected, options, options.Length))
                    _actionFormValues[field.Id] = options[selected];
                break;
            }
            default:
            {
                if (!_actionTextBuffers.ContainsKey(field.Id))
                    _actionTextBuffers[field.Id] = val;
                var buf = _actionTextBuffers[field.Id];
                if (ImGui.InputText($"##{field.Id}", ref buf, 256))
                {
                    _actionTextBuffers[field.Id] = buf;
                    _actionFormValues[field.Id] = buf;
                }
                break;
            }
        }

        ImGui.Spacing();
    }

    private void ExecuteActionForm()
    {
        if (_actionFormAction == null) return;

        _actionExecuting = true;
        _actionFormResult = null;

        var actionId = _actionFormAction.Id;
        var values = new Dictionary<string, string>(_actionFormValues);

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await _client.ExecutePluginActionAsync(PluginId, actionId, null, values);
                if (result?.Success == true)
                {
                    _actionFormResult = result.Message ?? "Success";
                    // Refresh graph after creation/deletion
                    _loaded = false;
                    _selectedNode = null;
                    _showActionForm = false;
                    _actionFormAction = null;
                }
                else
                {
                    _actionFormResult = $"Error: {string.Join(", ", result?.Errors ?? ["Failed"])}";
                }
            }
            catch (Exception ex)
            {
                _actionFormResult = $"Error: {ex.Message}";
            }
            finally
            {
                _actionExecuting = false;
            }
        });
    }

    // ─── Callbacks ───────────────────────────────────────────────

    private void OnNodeSelected(SchemaNode node)
    {
        _selectedNode = node;
    }

    private void OnTreeItemSelected(SchemaNode item)
    {
        if (item.EntityPrefix == "auth-line")
        {
            _selectedQuestLineId = item.Id;
            ApplyQuestLineFilter(item.Id);
        }
        else
        {
            // Clicked an objective — select it in graph
            _editor?.SelectNode(item.Id);
            _selectedNode = item;
        }
    }

    private void ApplyQuestLineFilter(string? lineId)
    {
        if (lineId == null || _editor == null)
        {
            _editor!.NodeFilter = null;
            return;
        }

        // Collect visible node IDs: the line itself + linked objectives + branch lines
        var visibleIds = new HashSet<string> { lineId };
        foreach (var link in _allLinks)
        {
            if (link.SourceNodeId == lineId)
                visibleIds.Add(link.TargetNodeId);
        }
        // Also include branch targets' objectives (1 level deep)
        var branchLines = visibleIds.Where(id => id != lineId && _allNodes.Any(n => n.Id == id && n.EntityPrefix == "auth-line")).ToList();
        foreach (var branchId in branchLines)
        {
            foreach (var link in _allLinks)
            {
                if (link.SourceNodeId == branchId)
                    visibleIds.Add(link.TargetNodeId);
            }
        }

        _editor.NodeFilter = node => visibleIds.Contains(node.Id);
        _editor.CenterOnNodes();
    }

    private void PasteNpcFromClipboard()
    {
        var entity = _clipboard.CopiedEntity;
        if (entity == null) return;

        string role = entity.Type ?? "unknown";
        string id = $"npc_{ToSnakeCase(role)}_{DateTime.UtcNow.Ticks % 10000}";
        string name = entity.Name ?? role;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await _client.ExecutePluginActionAsync(PluginId, "createNpcAssignment", null,
                    new Dictionary<string, string>
                    {
                        ["id"] = id,
                        ["npcRole"] = role,
                        ["assignmentType"] = "QUEST_GIVER",
                    });

                if (result?.Success == true)
                {
                    SetStatus($"Pasted NPC: {name}", false);
                    _clipboard.Clear();
                    _loaded = false;
                    _selectedNode = null;
                }
                else
                {
                    SetStatus($"Error: {string.Join(", ", result?.Errors ?? ["Paste failed"])}", true);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
            }
        });
    }

    private void DeleteMultipleNodes(HashSet<string> nodeIds)
    {
        _ = Task.Run(async () =>
        {
            int deleted = 0;
            foreach (var nodeId in nodeIds)
            {
                var node = _allNodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;

                string rawId = StripPrefix(node.EntityId);
                string? actionId = node.EntityPrefix switch
                {
                    "auth-line" => "deleteQuestLine",
                    "auth-obj" => "deleteObjective",
                    "npc-assign" => "deleteNpcAssignment",
                    "dlg" => "deleteDialog",
                    "loc" => "deleteLocation",
                    _ => null,
                };
                string? paramKey = node.EntityPrefix switch
                {
                    "auth-line" => "questLineId",
                    "auth-obj" => "objectiveId",
                    "npc-assign" => "npcAssignmentId",
                    "dlg" => "dialogId",
                    "loc" => "locationId",
                    _ => null,
                };

                if (actionId == null || paramKey == null) continue;

                try
                {
                    var result = await _client.ExecutePluginActionAsync(PluginId, actionId, null,
                        new Dictionary<string, string> { [paramKey] = rawId });
                    if (result?.Success == true) deleted++;
                }
                catch { }
            }

            SetStatus($"Deleted {deleted}/{nodeIds.Count} nodes", deleted == nodeIds.Count);
            _loaded = false;
            _selectedNode = null;
        });
    }

    private void OnAssignQuestGiverFromMap(SchemaNode questLineNode, EntityDto entity)
    {
        string npcRole = entity.Type ?? "";
        if (string.IsNullOrEmpty(npcRole))
        {
            SetStatus("Selected entity has no type", true);
            return;
        }

        string questLineId = StripPrefix(questLineNode.EntityId);
        string npcId = $"npc_{ToSnakeCase(npcRole)}_{DateTime.UtcNow.Ticks % 10000}";

        _ = Task.Run(async () =>
        {
            try
            {
                // 1. Create NPC Assignment with the selected entity's role
                var createResult = await _client.ExecutePluginActionAsync(PluginId, "createNpcAssignment", null,
                    new Dictionary<string, string>
                    {
                        ["id"] = npcId,
                        ["npcRole"] = npcRole,
                        ["assignmentType"] = "QUEST_GIVER",
                    });

                if (createResult?.Success != true)
                {
                    SetStatus($"Failed to create NPC: {string.Join(", ", createResult?.Errors ?? ["Unknown"])}", true);
                    return;
                }

                // 2. Link NPC to quest line as quest giver
                var linkResult = await _client.ExecutePluginActionAsync(PluginId, "setQuestGiver", null,
                    new Dictionary<string, string>
                    {
                        ["questLineId"] = questLineId,
                        ["npcAssignmentId"] = npcId,
                    });

                if (linkResult?.Success == true)
                {
                    SetStatus($"Quest giver assigned: {entity.Name ?? npcRole}", false);
                }
                else
                {
                    SetStatus($"NPC created but linking failed: {linkResult?.Message ?? string.Join(", ", linkResult?.Errors ?? ["Unknown"])}", true);
                }

                // Refresh graph to show new NPC node + link
                _loaded = false;
                _selectedNode = null;
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
            }
        });
    }

    private void OnEntitySelectedForNpc(SchemaNode node, EntityDto entity)
    {
        // Update the NPC assignment's role to match the selected entity
        string newRole = entity.Type ?? "";
        if (string.IsNullOrEmpty(newRole))
        {
            SetStatus("Selected entity has no type", true);
            return;
        }

        _ = Task.Run(async () =>
        {
            var result = await _client.UpdatePluginEntityAsync(PluginId, node.EntityId,
                new Dictionary<string, string> { ["npc_role"] = newRole });
            if (result?.Success == true)
            {
                node.Values["npc_role"] = newRole;
                node.Title = $"NPC: {entity.Name ?? newRole}";
                SetStatus($"NPC set to: {entity.Name ?? newRole}", false);
                _detailEditingNodeId = null;
            }
            else
            {
                SetStatus($"Failed to update NPC role", true);
            }
        });
    }

    private void OnMapLocationPicked(SchemaNode node, float worldX, float worldZ)
    {
        _ = Task.Run(async () =>
        {
            // Resolve surface Y
            float y = 64;
            try
            {
                var resp = await _client.GetSurfaceAsync("default", (int)worldX, (int)worldZ, 0);
                if (resp?.Surface is { Length: > 0 })
                    y = resp.Surface[0].Y + 1;
            }
            catch { }

            if (node.EntityPrefix == "loc")
            {
                // Update location entity coordinates
                var values = new Dictionary<string, string>
                {
                    ["loc_x"] = worldX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                    ["loc_y"] = y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                    ["loc_z"] = worldZ.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                };
                var result = await _client.UpdatePluginEntityAsync(PluginId, node.EntityId, values);
                if (result?.Success == true)
                {
                    foreach (var (k, v) in values) node.Values[k] = v;
                    SetStatus($"Location set: ({worldX:F0}, {y:F0}, {worldZ:F0})", false);
                }
                else
                {
                    SetStatus("Failed to update location", true);
                }
            }
            else if (node.EntityPrefix == "npc-assign")
            {
                // Spawn the NPC at the picked location using its role
                string npcRole = node.Values.GetValueOrDefault("npc_role", "");
                if (string.IsNullOrEmpty(npcRole))
                {
                    SetStatus("NPC has no role assigned — select a role first", true);
                    return;
                }

                // Find existing NPCs with this role
                var entities = await _client.GetEntitiesAsync("default", npcRole);
                var withUuid = entities?.Where(e => e.Uuid != null).ToArray();

                if (withUuid is { Length: > 0 })
                {
                    // Find the closest NPC to the picked position
                    var closest = withUuid
                        .OrderBy(e => MathF.Sqrt(
                            MathF.Pow(e.X - worldX, 2) + MathF.Pow(e.Z - worldZ, 2)))
                        .First();

                    var teleportResult = await _client.TeleportEntityAsync(closest.Uuid!, new Models.Api.TeleportRequest
                    {
                        X = worldX + 0.5, Y = y, Z = worldZ + 0.5
                    });

                    string extra = withUuid.Length > 1 ? $" ({withUuid.Length} total with this role, moved closest)" : "";
                    SetStatus(teleportResult?.Success == true
                        ? $"Teleported {npcRole} to ({worldX:F0}, {y:F0}, {worldZ:F0}){extra}"
                        : $"Teleport failed: {teleportResult?.Error ?? "Unknown"}", teleportResult?.Success != true);
                }
                else
                {
                    // No existing NPC — spawn a new one
                    var spawnResult = await _client.SpawnEntityAsync(new Models.Api.EntitySpawnRequest
                    {
                        Type = npcRole,
                        World = "default",
                        X = worldX + 0.5, Y = y, Z = worldZ + 0.5
                    });
                    SetStatus(spawnResult?.Success == true
                        ? $"Spawned {npcRole} at ({worldX:F0}, {y:F0}, {worldZ:F0})"
                        : $"Spawn failed: {spawnResult?.Error ?? "Unknown"}", spawnResult?.Success != true);
                }
            }
        });
    }

    private void ExecuteAction(string actionId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await _client.ExecutePluginActionAsync(PluginId, actionId, null,
                    new Dictionary<string, string>());
                if (result?.Success == true)
                {
                    SetStatus(result.Message ?? "Done", false);
                    _loaded = false;
                    _selectedNode = null;
                }
                else
                {
                    SetStatus($"Failed: {string.Join(", ", result?.Errors ?? ["Unknown error"])}", true);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
            }
        });
    }

    private void ExecuteRebuild() => ExecuteAction("rebuild");

    private void SaveLayout()
    {
        if (_editor == null) return;
        var state = new GraphLayoutState();
        state.CaptureFrom(_editor);
        GraphLayoutState.Save("adventure", state);
    }

    private void ClearFilter()
    {
        _selectedQuestLineId = null;
        _treeView?.SelectItem(null);
        if (_editor != null)
        {
            _editor.NodeFilter = null;
            _editor.CenterOnNodes();
        }
    }

    private void OnLinkCreated(NodeLink link)
    {
        var srcNode = _editor?.GetNode(link.SourceNodeId);
        if (srcNode == null) return;

        var srcPort = srcNode.Ports.FirstOrDefault(p => p.Id == link.SourcePortId);
        if (srcPort == null) return;

        var tgtNode = _editor?.GetNode(link.TargetNodeId);

        // Pre-validation with specific error messages
        if (srcPort.PortType == "npc_questgiver" && tgtNode?.EntityPrefix == "npc-assign")
        {
            string npcRole = tgtNode.Values.GetValueOrDefault("npc_role", "");
            if (string.IsNullOrEmpty(npcRole))
            {
                SetStatus("NPC has no role assigned. Select an NPC from the map first.", true);
                _editor?.RemoveLink(link.Id);
                return;
            }
        }

        string rawSrcId = StripPrefix(srcNode.EntityId);
        string rawTgtId = StripPrefix(link.TargetNodeId);

        _ = Task.Run(async () =>
        {
            ActionResultDto? result = null;

            if (srcPort.PortType == "objective_link")
            {
                // addObjectiveToLine
                result = await _client.ExecutePluginActionAsync(PluginId, "addObjectiveToLine", null,
                    new Dictionary<string, string>
                    {
                        ["questLineId"] = rawSrcId,
                        ["objectiveId"] = rawTgtId,
                        ["position"] = "-1"
                    });
            }
            else if (srcPort.PortType == "questflow")
            {
                // addBranch
                result = await _client.ExecutePluginActionAsync(PluginId, "addBranch", null,
                    new Dictionary<string, string>
                    {
                        ["questLineId"] = rawSrcId,
                        ["nextLineId"] = rawTgtId,
                    });
            }
            else if (srcPort.PortType == "npc_questgiver")
            {
                result = await _client.ExecutePluginActionAsync(PluginId, "setQuestGiver", null,
                    new Dictionary<string, string>
                    {
                        ["questLineId"] = rawSrcId,
                        ["npcAssignmentId"] = rawTgtId,
                    });
            }
            else if (srcPort.PortType == "dialog_link")
            {
                result = await _client.ExecutePluginActionAsync(PluginId, "linkDialogToNpc", null,
                    new Dictionary<string, string>
                    {
                        ["npcAssignmentId"] = rawSrcId,
                        ["dialogId"] = rawTgtId,
                    });
            }
            else if (srcPort.PortType == "location_link")
            {
                // Location link — update entity field directly
                var fieldId = srcNode.EntityPrefix == "npc-assign" ? "npc_locationId" : "targetLocationId";
                var apiResult = await _client.UpdatePluginEntityAsync(PluginId, srcNode.EntityId,
                    new Dictionary<string, string> { [fieldId] = rawTgtId });
                result = new ActionResultDto { Success = apiResult?.Success ?? false, Errors = apiResult?.Success == false ? [apiResult?.Error ?? "Failed"] : null };
            }

            if (result?.Success == true)
            {
                SetStatus("Link saved", false);
            }
            else
            {
                SetStatus($"Error: {string.Join(", ", result?.Errors ?? ["Failed"])}", true);
                // Remove link from editor on failure
                _editor?.RemoveLink(link.Id);
            }
        });
    }

    private void OnLinkRemoved(NodeLink link)
    {
        // TODO: implement removeObjectiveFromLine / removeBranch when API supports it
        SetStatus("Link removed (local only)", false);
    }

    private void OnCanvasDoubleClick(Vector2 canvasPos)
    {
        // Could open a "create node" dialog here
    }

    private void SetStatus(string msg, bool isError)
    {
        _actionStatus = msg;
        _actionStatusTimer = 3f;
    }

    private static string StripPrefix(string entityId)
    {
        int sep = entityId.IndexOf(':');
        return sep >= 0 ? entityId[(sep + 1)..] : entityId;
    }
}
