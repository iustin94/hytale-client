using HytaleAdmin.Models.Api;

namespace HytaleAdmin.UI.NodeEditor;

public interface IGraphBuilderStrategy
{
    bool CanHandle(PluginSchemaDto schema);

    GraphDefinition BuildDefinition(PluginSchemaDto schema);

    List<SchemaNode> CreateNodes(
        GraphDefinition definition,
        PluginEntitySummaryDto[] entities,
        Func<string, Dictionary<string, string>?> resolveValues);

    List<NodeLink> ExtractLinks(GraphDefinition definition, List<SchemaNode> nodes);
}
