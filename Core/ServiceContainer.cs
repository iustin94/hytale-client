using HytaleAdmin.Models.Domain;
using HytaleAdmin.Services;
using Stride.Engine;
using Stride.Games;

namespace HytaleAdmin.Core;

public class ServiceContainer
{
    public required HytaleApiClient ApiClient { get; init; }
    public required MapDataService MapData { get; init; }
    public required EntityDataService EntityData { get; init; }
    public required SelectionService Selection { get; init; }
    public required EditorConfig Config { get; init; }
    public required Game Game { get; init; }
    public required PluginSchemaCache PluginSchemas { get; init; }
    public required ClipboardService Clipboard { get; init; }
}
