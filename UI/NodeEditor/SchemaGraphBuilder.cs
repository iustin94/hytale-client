using System.Numerics;
using HytaleAdmin.Models.Api;

namespace HytaleAdmin.UI.NodeEditor;

public class SchemaGraphBuilder : IGraphBuilderStrategy
{
    public bool CanHandle(PluginSchemaDto schema) => schema.GraphHints?.NodeTypes.Length > 0;

    public GraphDefinition BuildDefinition(PluginSchemaDto schema)
    {
        var hints = schema.GraphHints!;
        var def = new GraphDefinition();

        // Build port type connection rules
        foreach (var rule in hints.ConnectionRules)
            def.ConnectionRules.Allow(rule.OutputType, rule.InputType);

        // Build node types from hints
        foreach (var hint in hints.NodeTypes)
        {
            var group = schema.Groups.FirstOrDefault(g => g.Id == hint.GroupId);
            var nodeType = new GraphNodeTypeConfig
            {
                Id = hint.GroupId,
                GroupId = hint.GroupId,
                EntityPrefix = hint.EntityPrefix,
                Label = hint.Label,
            };

            // Ports from hint
            foreach (var ph in hint.Ports)
            {
                nodeType.Ports.Add(new GraphPortConfig
                {
                    PortId = ph.PortId,
                    FieldId = ph.FieldId,
                    Label = ph.Label,
                    Direction = ph.Direction == "input" ? PortDirection.Input : PortDirection.Output,
                    PortType = ph.PortType,
                    Color = ParseHexColor(ph.Color),
                    MultiLink = ph.MultiLink,
                });
            }

            // Display fields = all non-port fields from the group
            if (group != null)
            {
                var portFieldIds = hint.Ports.Select(p => p.FieldId).ToHashSet();
                nodeType.DisplayFields = group.Fields
                    .Where(f => !portFieldIds.Contains(f.Id))
                    .ToList();
            }

            def.NodeTypes.Add(nodeType);

            // Build visual style from hint color
            var headerColor = ParseHexColorVec4(hint.HeaderColor);
            def.Styles[hint.GroupId] = new NodeStyle
            {
                HeaderColor = headerColor,
                BodyColor = new Vector4(0.14f, 0.14f, 0.19f, 0.95f),
                BorderColor = headerColor with { W = 0.6f },
                SelectedBorderColor = new Vector4(0.95f, 0.75f, 0.20f, 1f),
                TitleColor = new Vector4(1f, 1f, 1f, 1f),
                MinWidth = 200f,
            };
        }

        return def;
    }

    public List<SchemaNode> CreateNodes(
        GraphDefinition definition,
        PluginEntitySummaryDto[] entities,
        Func<string, Dictionary<string, string>?> resolveValues)
    {
        var nodes = new List<SchemaNode>();
        float xSpacing = 280f;
        float ySpacing = 160f;

        foreach (var nodeType in definition.NodeTypes)
        {
            var matching = entities
                .Where(e => e.Id.StartsWith(nodeType.EntityPrefix + ":"))
                .ToList();

            for (int i = 0; i < matching.Count; i++)
            {
                var entity = matching[i];
                var values = resolveValues(entity.Id) ?? new Dictionary<string, string>();

                var ports = BuildPortDefinitions(nodeType);

                var node = new SchemaNode
                {
                    Id = entity.Id,
                    EntityId = entity.Id,
                    EntityPrefix = nodeType.EntityPrefix,
                    NodeType = nodeType.GroupId,
                    Title = entity.Label,
                    Subtitle = nodeType.Label,
                    Position = AutoLayout(definition, nodeType, i, matching.Count),
                    Ports = ports,
                    Values = values,
                };

                nodes.Add(node);
            }
        }

        return nodes;
    }

    public List<NodeLink> ExtractLinks(GraphDefinition definition, List<SchemaNode> nodes)
    {
        var links = new List<NodeLink>();
        var nodeById = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            var nodeType = definition.NodeTypes.FirstOrDefault(t => t.GroupId == node.NodeType);
            if (nodeType == null) continue;

            foreach (var portConfig in nodeType.Ports)
            {
                if (portConfig.Direction != PortDirection.Output) continue;
                if (!node.Values.TryGetValue(portConfig.FieldId, out var rawValue)) continue;
                if (string.IsNullOrEmpty(rawValue)) continue;

                // Parse stringList: could be JSON array or comma-separated
                var targetIds = ParseStringList(rawValue);

                // Find target entity prefix from matching input port type
                var targetPrefix = FindTargetPrefix(definition, portConfig.PortType);

                foreach (var targetId in targetIds)
                {
                    // Try matching with prefix variations
                    var targetNodeId = ResolveTargetNode(nodeById, targetId, targetPrefix);
                    if (targetNodeId == null) continue;

                    var targetNode = nodeById[targetNodeId];
                    var targetPort = FindInputPort(definition, targetNode.NodeType, portConfig.PortType);
                    if (targetPort == null) continue;

                    var linkId = $"{node.Id}:{portConfig.PortId}->{targetNodeId}:{targetPort.PortId}";
                    if (links.Any(l => l.Id == linkId)) continue;

                    links.Add(new NodeLink(linkId, node.Id, portConfig.PortId, targetNodeId, targetPort.PortId));
                }
            }
        }

        return links;
    }

    // ─── Internal helpers ────────────────────────────────────────

    private static List<PortDefinition> BuildPortDefinitions(GraphNodeTypeConfig nodeType)
    {
        return nodeType.Ports.Select(p => new PortDefinition(
            p.PortId, p.Label, p.Direction, p.PortType) { Color = p.Color }
        ).ToList();
    }

    private Vector2 AutoLayout(GraphDefinition def, GraphNodeTypeConfig nodeType, int index, int total)
    {
        int typeIndex = def.NodeTypes.IndexOf(nodeType);
        float x = typeIndex * 300f;
        float y = index * 180f;
        return new Vector2(x, y);
    }

    private static List<string> ParseStringList(string raw)
    {
        raw = raw.Trim();

        // JSON array: ["a", "b"]
        if (raw.StartsWith('['))
        {
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(raw);
                return arr?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new();
            }
            catch { }
        }

        // Comma-separated fallback
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    private static string? FindTargetPrefix(GraphDefinition def, string portType)
    {
        // Find node types that have an input port matching this port type
        foreach (var nt in def.NodeTypes)
        {
            foreach (var p in nt.Ports)
            {
                if (p.Direction == PortDirection.Input && def.ConnectionRules.CanConnect(portType, p.PortType))
                    return nt.EntityPrefix;
            }
        }
        return null;
    }

    private static GraphPortConfig? FindInputPort(GraphDefinition def, string nodeTypeId, string sourcePortType)
    {
        var nodeType = def.NodeTypes.FirstOrDefault(t => t.GroupId == nodeTypeId);
        return nodeType?.Ports.FirstOrDefault(p =>
            p.Direction == PortDirection.Input && def.ConnectionRules.CanConnect(sourcePortType, p.PortType));
    }

    private static string? ResolveTargetNode(Dictionary<string, SchemaNode> nodeById, string targetId, string? expectedPrefix)
    {
        // Direct match (already prefixed)
        if (nodeById.ContainsKey(targetId))
            return targetId;

        // Try with expected prefix
        if (expectedPrefix != null)
        {
            var prefixed = $"{expectedPrefix}:{targetId}";
            if (nodeById.ContainsKey(prefixed))
                return prefixed;
        }

        // Brute search by raw ID suffix
        foreach (var (key, node) in nodeById)
        {
            int sep = key.IndexOf(':');
            if (sep >= 0 && key[(sep + 1)..] == targetId)
                return key;
        }

        return null;
    }

    private static uint ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            uint rgb = Convert.ToUInt32(hex, 16);
            uint r = (rgb >> 16) & 0xFF;
            uint g = (rgb >> 8) & 0xFF;
            uint b = rgb & 0xFF;
            return 0xFF000000 | (b << 16) | (g << 8) | r; // ABGR for ImGui
        }
        return 0xFFFFFFFF;
    }

    private static Vector4 ParseHexColorVec4(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            uint rgb = Convert.ToUInt32(hex, 16);
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8) & 0xFF) / 255f;
            float b = (rgb & 0xFF) / 255f;
            return new Vector4(r, g, b, 1f);
        }
        return new Vector4(0.3f, 0.3f, 0.4f, 1f);
    }
}
