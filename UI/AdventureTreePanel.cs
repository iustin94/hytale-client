using System.Numerics;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;
using HytaleAdmin.UI.Components;
using HytaleAdmin.UI.NodeEditor;

namespace HytaleAdmin.UI;

public class AdventureTreeDataProvider : ITreeDataProvider<SchemaNode>
{
    private List<SchemaNode> _lines = new();
    private List<SchemaNode> _objectives = new();
    private Dictionary<string, List<SchemaNode>> _childrenByLine = new();
    private Dictionary<string, List<SchemaNode>> _childrenByNode = new();
    private HashSet<string> _categories = new();

    private static readonly Vector4 LineColor = new(0.40f, 0.70f, 0.95f, 1f);
    private static readonly Vector4 ObjColor = new(0.31f, 0.80f, 0.50f, 1f);
    private static readonly Vector4 NpcColor = new(0.77f, 0.29f, 0.55f, 1f);
    private static readonly Vector4 DlgColor = new(0.83f, 0.66f, 0.26f, 1f);
    private static readonly Vector4 LocColor = new(0.36f, 0.55f, 0.85f, 1f);

    public void SetNodes(List<SchemaNode> nodes, List<NodeLink> links)
    {
        _lines = nodes.Where(n => n.EntityPrefix == "auth-line").ToList();
        _objectives = nodes.Where(n => n.EntityPrefix == "auth-obj").ToList();
        _childrenByLine.Clear();
        _childrenByNode.Clear();
        _categories.Clear();

        var nodeById = nodes.ToDictionary(n => n.Id);

        foreach (var line in _lines)
        {
            string cat = line.Values.GetValueOrDefault("line_category", "custom");
            _categories.Add(cat);

            var children = new List<SchemaNode>();
            foreach (var link in links)
            {
                if (link.SourceNodeId != line.Id) continue;
                if (nodeById.TryGetValue(link.TargetNodeId, out var target))
                    children.Add(target);
            }
            _childrenByLine[line.Id] = children;
        }

        // Build generic children map for NPC → Dialog links
        foreach (var node in nodes)
        {
            if (node.EntityPrefix is not ("npc-assign" or "auth-obj")) continue;
            var children = new List<SchemaNode>();
            foreach (var link in links)
            {
                if (link.SourceNodeId != node.Id) continue;
                if (nodeById.TryGetValue(link.TargetNodeId, out var target))
                    children.Add(target);
            }
            if (children.Count > 0)
                _childrenByNode[node.Id] = children;
        }
    }

    public IReadOnlyList<TreeGroup>? GetGroups()
    {
        if (_categories.Count <= 1) return null;
        var order = new[] { "main_quest", "side_quest", "daily", "custom" };
        return order
            .Where(c => _categories.Contains(c))
            .Concat(_categories.Where(c => !order.Contains(c)))
            .Select(c => new TreeGroup(c, FormatCategory(c)))
            .ToList();
    }

    public IReadOnlyList<SchemaNode> GetItems(string? groupId)
    {
        if (groupId == null) return _lines;
        return _lines.Where(l => l.Values.GetValueOrDefault("line_category", "custom") == groupId).ToList();
    }

    public IReadOnlyList<SchemaNode> GetChildren(SchemaNode parent)
    {
        if (parent.EntityPrefix == "auth-line")
            return _childrenByLine.GetValueOrDefault(parent.Id) ?? (IReadOnlyList<SchemaNode>)[];
        return _childrenByNode.GetValueOrDefault(parent.Id) ?? (IReadOnlyList<SchemaNode>)[];
    }

    public string GetId(SchemaNode item) => item.Id;
    public string GetLabel(SchemaNode item) => item.Title;

    public Vector4? GetColor(SchemaNode item)
    {
        return item.EntityPrefix switch
        {
            "auth-line" => LineColor,
            "auth-obj" => ObjColor,
            "npc-assign" => NpcColor,
            "dlg" => DlgColor,
            "loc" => LocColor,
            _ => null,
        };
    }

    public string? GetBadge(SchemaNode item)
    {
        if (item.EntityPrefix != "auth-line") return null;
        int objCount = _childrenByLine.GetValueOrDefault(item.Id)?.Count ?? 0;
        int branchCount = 0;
        if (item.Values.TryGetValue("line_nextQuestLineIds", out var branches) && !string.IsNullOrEmpty(branches))
        {
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(branches);
                branchCount = arr?.Length ?? 0;
            }
            catch { }
        }
        var parts = new List<string>();
        if (objCount > 0) parts.Add($"{objCount} obj");
        if (branchCount > 0) parts.Add($"{branchCount} branch");
        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public string? GetTooltip(SchemaNode item)
    {
        if (item.Values.TryGetValue("line_titleKey", out var title) && !string.IsNullOrEmpty(title))
            return title;
        if (item.Values.TryGetValue("aobj_titleKey", out var objTitle) && !string.IsNullOrEmpty(objTitle))
            return objTitle;
        return null;
    }

    public bool IsExpandable(SchemaNode item) => item.EntityPrefix is "auth-line" or "npc-assign";

    public bool MatchesFilter(SchemaNode item, string filter)
    {
        var lower = filter.ToLowerInvariant();
        return item.Title.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
               item.Id.Contains(lower, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatCategory(string cat) => cat switch
    {
        "main_quest" => "Main Quests",
        "side_quest" => "Side Quests",
        "daily" => "Daily",
        "custom" => "Custom",
        _ => cat,
    };
}

public class AdventureTreeContextMenu : ITreeContextMenu<SchemaNode>
{
    private readonly PluginSchemaDto _schema;

    public PluginActionDto? PendingAction { get; private set; }
    public SchemaNode? PendingNode { get; private set; }

    public AdventureTreeContextMenu(PluginSchemaDto schema)
    {
        _schema = schema;
    }

    public void ClearPending()
    {
        PendingAction = null;
        PendingNode = null;
    }

    public List<ContextMenuItem> GetMenuItems(TreeContextRequest<SchemaNode> request)
    {
        // No background menu — tree is browse-only. Wizard button handles creation.
        if (request.Target != TreeContextTarget.Item || request.Item == null)
            return new();

        // Node right-click: delete only
        string? deleteId = request.Item.EntityPrefix switch
        {
            "auth-line" => "deleteQuestLine",
            "auth-obj" => "deleteObjective",
            "npc-assign" => "deleteNpcAssignment",
            "dlg" => "deleteDialog",
            "loc" => "deleteLocation",
            _ => null,
        };

        if (deleteId == null) return new();

        return [new ContextMenuItem { Id = deleteId, Label = "Delete", Color = 0xFF_3A2DA6 }];
    }

    public void OnItemSelected(ContextMenuItem item, TreeContextRequest<SchemaNode> request)
    {
        var action = _schema.Actions.FirstOrDefault(a => a.Id == item.Id);
        if (action != null)
        {
            PendingAction = action;
            PendingNode = request.Item;
        }
    }
}
