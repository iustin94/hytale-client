using System.Numerics;

namespace HytaleAdmin.UI.CanvasView;

/// <summary>
/// Manages selection, hover, and box-select state for canvas entities.
/// </summary>
public class CanvasSelectionState
{
    public HashSet<string> SelectedIds { get; } = new();
    public string? HoveredId { get; set; }
    public bool IsBoxSelecting { get; set; }
    public Vector2 BoxStart { get; set; }
    public Vector2 BoxEnd { get; set; }

    public bool IsSelected(string id) => SelectedIds.Contains(id);
    public bool IsHovered(string id) => HoveredId == id;

    public void Select(string id)
    {
        SelectedIds.Clear();
        SelectedIds.Add(id);
    }

    public void ToggleSelect(string id)
    {
        if (!SelectedIds.Remove(id))
            SelectedIds.Add(id);
    }

    public void ClearSelection()
    {
        SelectedIds.Clear();
    }
}
