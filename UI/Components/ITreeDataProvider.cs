using System.Numerics;

namespace HytaleAdmin.UI.Components;

public record TreeGroup(string Id, string Label, Vector4? Color = null);

public interface ITreeDataProvider<TItem> where TItem : class
{
    IReadOnlyList<TreeGroup>? GetGroups();
    IReadOnlyList<TItem> GetItems(string? groupId);
    IReadOnlyList<TItem> GetChildren(TItem parent);

    string GetId(TItem item);
    string GetLabel(TItem item);
    Vector4? GetColor(TItem item);
    string? GetBadge(TItem item);
    string? GetTooltip(TItem item);

    bool IsExpandable(TItem item);
    bool MatchesFilter(TItem item, string filter);
}
