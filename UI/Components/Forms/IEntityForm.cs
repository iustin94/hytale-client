namespace HytaleAdmin.UI.Components.Forms;

/// <summary>
/// Self-contained form for editing/creating a specific entity type.
/// Handles its own data loading, rendering, and value collection.
/// </summary>
public interface IEntityForm
{
    /// <summary>Draw the form fields. Call every frame.</summary>
    void Draw();

    /// <summary>Whether all required fields are filled.</summary>
    bool IsValid { get; }

    /// <summary>Collect current form values as key-value pairs for API submission.</summary>
    Dictionary<string, string> GetValues();

    /// <summary>Reset form to defaults.</summary>
    void Reset();
}
