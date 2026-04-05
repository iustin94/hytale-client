using HytaleAdmin.Models.Api;
using HytaleAdmin.Rendering;

namespace HytaleAdmin.Services;

public class PluginSchemaCache
{
    private readonly HytaleApiClient _api;
    private Dictionary<string, PluginSchemaDto> _schemas = new();
    private Dictionary<string, IPluginMapPresenter> _presenters = new();
    private PluginSummaryDto[] _plugins = [];
    private bool _loaded;

    public PluginSchemaCache(HytaleApiClient api)
    {
        _api = api;
    }

    public bool IsLoaded => _loaded;

    public async Task RefreshAsync()
    {
        _plugins = await _api.GetPluginsAsync() ?? [];
        _schemas.Clear();
        _presenters.Clear();

        foreach (var p in _plugins)
        {
            if (!p.Available) continue;
            var schema = await _api.GetPluginSchemaAsync(p.PluginId);
            if (schema != null)
            {
                _schemas[p.PluginId] = schema;
                _presenters[p.PluginId] = PluginMapPresenterFactory.Create(schema.MapPresenter);
            }
        }

        _loaded = true;
    }

    /// <summary>
    /// Returns spatial actions — discovered from schemas that declare spatialFields.
    /// </summary>
    public List<SpatialAction> GetSpatialActions()
    {
        var result = new List<SpatialAction>();

        foreach (var (pluginId, schema) in _schemas)
        {
            if (schema.SpatialFields == null || schema.SpatialFields.Count == 0) continue;

            foreach (var action in schema.Actions ?? [])
            {
                if (action.RequiresEntity) continue;
                result.Add(new SpatialAction
                {
                    PluginId = pluginId,
                    PluginName = schema.PluginName,
                    Action = action,
                    SpatialFields = schema.SpatialFields
                });
            }
        }

        return result;
    }

    public string[] GetSpatialPluginIds()
    {
        return _schemas.Keys.ToArray();
    }

    public IPluginMapPresenter GetPresenter(string pluginId)
    {
        return _presenters.GetValueOrDefault(pluginId, new NullMapPresenter());
    }

    public Dictionary<string, IPluginMapPresenter> GetAllPresenters() => _presenters;
}

public class SpatialAction
{
    public required string PluginId { get; init; }
    public required string PluginName { get; init; }
    public required PluginActionDto Action { get; init; }
    public Dictionary<string, string> SpatialFields { get; init; } = new();
}
