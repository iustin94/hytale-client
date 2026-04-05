using System.Numerics;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;
using HytaleAdmin.UI.NodeEditor;

namespace HytaleAdmin.UI;

/// <summary>
/// Standard graph editor context menu. Two modes:
/// - Canvas: create node submenu
/// - Node: type-specific actions + delete
/// </summary>
public class AdventureContextMenu : IContextMenuProvider<SchemaNode>
{
    private readonly HytaleApiClient _client;
    private readonly PluginSchemaDto _schema;
    private readonly GraphDefinition _graphDef;

    public Action? OnGraphMutated { get; set; }
    public PluginActionDto? PendingAction { get; private set; }
    public SchemaNode? PendingNode { get; private set; }
    public HashSet<string>? PendingMultiDelete { get; set; }

    private const string PluginId = "hyadventure";

    public AdventureContextMenu(HytaleApiClient client, PluginSchemaDto schema, GraphDefinition graphDef)
    {
        _client = client;
        _schema = schema;
        _graphDef = graphDef;
    }

    public List<ContextMenuItem> GetMenuItems(ContextMenuRequest<SchemaNode> request)
    {
        return request.Target == ContextMenuTarget.Canvas
            ? CanvasMenu()
            : NodeMenu(request);
    }

    public void OnItemSelected(ContextMenuItem item, ContextMenuRequest<SchemaNode> request)
    {
        // Multi-delete
        if (item.Id == "_delete_selected" && request.SelectedNodeIds != null)
        {
            PendingMultiDelete = new HashSet<string>(request.SelectedNodeIds);
            return;
        }

        // Schema action
        var action = _schema.Actions.FirstOrDefault(a => a.Id == item.Id);
        if (action != null)
        {
            PendingAction = action;
            PendingNode = request.Node;
        }
    }

    public void ClearPending()
    {
        PendingAction = null;
        PendingNode = null;
    }

    // ─── Canvas: create node ─────────────────────────────────────

    private List<ContextMenuItem> CanvasMenu()
    {
        return
        [
            new() { Id = "createQuestLine", Label = "Quest Line", Color = C("#3A6BC5") },
            new() { Id = "createObjective", Label = "Objective", Color = C("#2FA85A") },
            new() { Id = "createNpcAssignment", Label = "NPC Assignment", Color = C("#C54B8C") },
            new() { Id = "createDialog", Label = "Dialog", Color = C("#D4A843") },
            new() { Id = "createLocation", Label = "Location", Color = C("#5B8DD9") },
        ];
    }

    // ─── Node: type-specific actions + delete ────────────────────

    private List<ContextMenuItem> NodeMenu(ContextMenuRequest<SchemaNode> request)
    {
        var items = new List<ContextMenuItem>();
        var node = request.Node;
        if (node == null) return items;

        // Multi-delete at top
        if (request.SelectedNodeIds is { Count: > 1 })
        {
            items.Add(new ContextMenuItem
            {
                Id = "_delete_selected",
                Label = $"Delete Selected ({request.SelectedNodeIds.Count})",
                Color = C("#A62D3A"),
            });
            items.Add(new ContextMenuItem { Id = "_s1", Label = "-", Separator = true });
        }

        // Per-type actions
        switch (node.EntityPrefix)
        {
            case "auth-line":
                A(items, "addObjectiveToLine", "Link Objective");
                A(items, "addBranch", "Add Branch");
                items.Add(new ContextMenuItem { Id = "_s2", Label = "-", Separator = true });
                items.Add(new ContextMenuItem { Id = "deleteQuestLine", Label = "Delete Quest Line", Color = C("#A62D3A") });
                break;
            case "auth-obj":
                A(items, "addTaskSet", "Add Phase");
                A(items, "addTask", "Add Task");
                A(items, "addCompletion", "Add Reward");
                items.Add(new ContextMenuItem { Id = "_s2", Label = "-", Separator = true });
                items.Add(new ContextMenuItem { Id = "deleteObjective", Label = "Delete Objective", Color = C("#A62D3A") });
                break;
            case "npc-assign":
                items.Add(new ContextMenuItem { Id = "deleteNpcAssignment", Label = "Delete NPC", Color = C("#A62D3A") });
                break;
            case "dlg":
                items.Add(new ContextMenuItem { Id = "deleteDialog", Label = "Delete Dialog", Color = C("#A62D3A") });
                break;
            case "loc":
                items.Add(new ContextMenuItem { Id = "deleteLocation", Label = "Delete Location", Color = C("#A62D3A") });
                break;
        }

        return items;
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private void A(List<ContextMenuItem> items, string actionId, string label)
    {
        if (_schema.Actions.Any(a => a.Id == actionId))
            items.Add(new ContextMenuItem { Id = actionId, Label = label });
    }

    private static uint C(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return 0xFFFFFFFF;
        uint rgb = Convert.ToUInt32(hex, 16);
        uint r = (rgb >> 16) & 0xFF;
        uint g = (rgb >> 8) & 0xFF;
        uint b = rgb & 0xFF;
        return 0xFF000000 | (b << 16) | (g << 8) | r;
    }
}
