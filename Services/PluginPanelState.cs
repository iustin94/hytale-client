using System.Text.Json;
using HytaleAdmin.Models.Api;

namespace HytaleAdmin.Services;

public enum FormMode { None, EditEntity, Action }

/// <summary>
/// State and async logic for the plugin panel, extracted from UI for testability.
/// PluginPanel/PluginView delegates all data operations to this class.
/// </summary>
public class PluginPanelState
{
    private readonly HytaleApiClient _client;

    // Plugin discovery
    public PluginSummaryDto[]? Plugins { get; set; }
    public bool PluginsLoading { get; set; }
    public int SelectedPluginIdx { get; set; } = -1;

    // Schema (cached per plugin)
    public string? CachedSchemaPluginId { get; set; }
    public PluginSchemaDto? Schema { get; set; }
    public bool SchemaLoading { get; set; }

    // Entity list
    public PluginEntitySummaryDto[]? Entities { get; set; }
    public bool EntitiesLoading { get; set; }
    public int SelectedEntityIdx { get; set; } = -1;

    // Form mode
    public FormMode Mode { get; set; } = FormMode.None;

    // Entity values (edit form state)
    public string? LoadedEntityId { get; set; }
    public Dictionary<string, string> CurrentValues { get; set; } = new();
    public Dictionary<string, string> EditedValues { get; set; } = new();
    public HashSet<string> DirtyFields { get; set; } = new();
    public Dictionary<string, string> TextBuffers { get; set; } = new();
    public bool ValuesLoading { get; set; }
    public bool Saving { get; set; }
    public string? SaveStatus { get; set; }

    // Action state
    public string? ActiveActionId { get; set; }
    public PluginActionDto? ActiveAction { get; set; }
    public Dictionary<string, string> ActionFormValues { get; set; } = new();
    public bool ActionExecuting { get; set; }
    public ActionResultDto? ActionResult { get; set; }
    public bool ShowConfirmDialog { get; set; }

    public PluginPanelState(HytaleApiClient client)
    {
        _client = client;
    }

    // ─── Plugin Loading ───────────────────────────────────────────

    public async Task LoadPluginsAsync()
    {
        PluginsLoading = true;
        try
        {
            Plugins = await _client.GetPluginsAsync();
            if (Plugins is { Length: 1 })
                SelectedPluginIdx = 0;
        }
        catch
        {
            Plugins = null;
        }
        finally
        {
            PluginsLoading = false;
        }
    }

    public async Task LoadSchemaAsync(string pluginId)
    {
        SchemaLoading = true;
        CachedSchemaPluginId = pluginId;
        Entities = null;
        SelectedEntityIdx = -1;
        LoadedEntityId = null;
        Mode = FormMode.None;
        try
        {
            Schema = await _client.GetPluginSchemaAsync(pluginId);
        }
        catch
        {
            Schema = null;
        }
        finally
        {
            SchemaLoading = false;
        }
    }

    public async Task LoadEntitiesAsync(string pluginId)
    {
        EntitiesLoading = true;
        try
        {
            var resp = await _client.GetPluginEntitiesAsync(pluginId);
            Entities = resp?.Data;
        }
        catch
        {
            Entities = []; // empty array, not null — prevents retry loop
        }
        finally
        {
            EntitiesLoading = false;
        }
    }

    // ─── Entity Edit ──────────────────────────────────────────────

    public async Task LoadEntityValuesAsync(string pluginId, string entityId)
    {
        ValuesLoading = true;
        LoadedEntityId = entityId;
        Mode = FormMode.EditEntity;
        DirtyFields.Clear();
        TextBuffers.Clear();
        SaveStatus = null;
        try
        {
            var dto = await _client.GetPluginEntityValuesAsync(pluginId, entityId);
            if (dto?.Values != null)
            {
                var flat = new Dictionary<string, string>();
                foreach (var kv in dto.Values)
                {
                    flat[kv.Key] = kv.Value.ValueKind switch
                    {
                        JsonValueKind.String => kv.Value.GetString() ?? "",
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Number => kv.Value.GetRawText(),
                        JsonValueKind.Object => kv.Value.GetRawText(),
                        _ => kv.Value.GetRawText()
                    };
                }
                CurrentValues = new Dictionary<string, string>(flat);
                EditedValues = new Dictionary<string, string>(flat);
            }
        }
        catch
        {
            // Values stay empty
        }
        finally
        {
            ValuesLoading = false;
        }
    }

    public void UpdateDirty(string fieldId)
    {
        if (CurrentValues.TryGetValue(fieldId, out var original) && EditedValues[fieldId] == original)
            DirtyFields.Remove(fieldId);
        else
            DirtyFields.Add(fieldId);
    }

    public async Task SaveChangesAsync(string pluginId)
    {
        if (LoadedEntityId == null) return;

        Saving = true;
        SaveStatus = null;
        var toSend = new Dictionary<string, string>();
        foreach (var fieldId in DirtyFields)
        {
            if (EditedValues.TryGetValue(fieldId, out var val))
                toSend[fieldId] = val;
        }

        try
        {
            var result = await _client.UpdatePluginEntityAsync(pluginId, LoadedEntityId, toSend);
            if (result?.Success == true)
            {
                SaveStatus = "Saved successfully";
                foreach (var kv in toSend)
                    CurrentValues[kv.Key] = kv.Value;
                DirtyFields.Clear();
            }
            else
            {
                SaveStatus = $"Error: {result?.Error ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            SaveStatus = $"Error: {ex.Message}";
        }
        finally
        {
            Saving = false;
        }
    }

    // ─── Actions ──────────────────────────────────────────────────

    /// <summary>
    /// Start an action form. Switches to Action mode and initializes empty form values.
    /// </summary>
    public void BeginAction(PluginActionDto action)
    {
        ActiveActionId = action.Id;
        ActiveAction = action;
        Mode = FormMode.Action;
        ActionFormValues = new Dictionary<string, string>();
        ActionResult = null;
        TextBuffers.Clear();
        DirtyFields.Clear();
        SaveStatus = null;
        ShowConfirmDialog = false;

        // Pre-populate with defaults — enum fields use first value
        foreach (var group in action.Groups)
        foreach (var field in group.Fields)
            ActionFormValues[field.Id] = field.EnumValues is { Length: > 0 }
                ? field.EnumValues[0]
                : "";
    }

    /// <summary>
    /// Execute the active action (or a parameterless entity-bound action).
    /// </summary>
    public async Task ExecuteActionAsync(string pluginId, string? entityId = null)
    {
        if (ActiveActionId == null) return;

        ActionExecuting = true;
        ActionResult = null;
        try
        {
            var result = await _client.ExecutePluginActionAsync(
                pluginId, ActiveActionId, entityId, ActionFormValues);
            ActionResult = result;

            if (result?.Success == true)
            {
                // Refresh entity list after create/delete
                await LoadEntitiesAsync(pluginId);

                // If a new entity was created, select it and load its edit form
                if (result.EntityId != null && Entities != null)
                {
                    for (int i = 0; i < Entities.Length; i++)
                    {
                        if (Entities[i].Id == result.EntityId)
                        {
                            SelectedEntityIdx = i;
                            await LoadEntityValuesAsync(pluginId, result.EntityId);
                            break;
                        }
                    }
                }
                // If it was a delete, clear selection
                else if (ActiveAction?.RequiresEntity == true)
                {
                    SelectedEntityIdx = -1;
                    LoadedEntityId = null;
                    Mode = FormMode.None;
                }
            }
        }
        catch (Exception ex)
        {
            ActionResult = new ActionResultDto
            {
                Success = false,
                Errors = [ex.Message]
            };
        }
        finally
        {
            ActionExecuting = false;
        }
    }

    /// <summary>
    /// Cancel the current action and return to the previous state.
    /// </summary>
    public void CancelAction()
    {
        ActiveActionId = null;
        ActiveAction = null;
        ActionFormValues.Clear();
        ActionResult = null;
        TextBuffers.Clear();
        ShowConfirmDialog = false;

        // Return to edit mode if an entity was selected, otherwise none
        Mode = LoadedEntityId != null ? FormMode.EditEntity : FormMode.None;
    }
}
