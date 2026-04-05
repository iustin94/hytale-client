namespace HytaleAdmin.UI.Components.MapActions;

/// <summary>
/// Self-contained spatial action that can be placed on the map.
/// Each implementation handles its own form rendering, validation, data loading, and API execution.
/// Used standalone or inside MapActionDialog.
/// </summary>
public interface IMapAction
{
    /// <summary>Display name shown in menus and dialog title.</summary>
    string Label { get; }

    /// <summary>Render the configuration form (type picker, fields, etc.).</summary>
    void DrawForm();

    /// <summary>Whether all required fields are filled and action can execute.</summary>
    bool IsValid { get; }

    /// <summary>Execute the action at the given world coordinates.</summary>
    Task<MapActionResult> ExecuteAsync(float worldX, float worldY, float worldZ);
}

public record MapActionResult(bool Success, string Message, string? EntityId = null);
