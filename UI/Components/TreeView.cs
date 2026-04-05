using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.UI.NodeEditor;
using Stride.Input;

namespace HytaleAdmin.UI.Components;

public class TreeView<TItem> where TItem : class
{
    private readonly ITreeDataProvider<TItem> _provider;

    private string _filter = "";
    private string? _selectedId;

    // Context menu state (same pattern as NodeEditor)
    private bool _contextMenuOpen;
    private int _contextMenuAge;
    private Vector2 _contextMenuScreenPos;
    private TreeContextRequest<TItem>? _contextMenuRequest;
    private InputManager? _strideInput;
    private bool _wasRightDown;

    private static readonly Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly Vector4 HeaderColor = new(0.55f, 0.55f, 0.63f, 1f);
    private static readonly Vector4 BadgeColor = new(0.45f, 0.45f, 0.53f, 1f);
    private static readonly Vector4 SelectedColor = new(0.95f, 0.75f, 0.20f, 0.25f);

    public ITreeContextMenu<TItem>? ContextMenu { get; set; }
    public Action<TItem>? OnItemSelected;
    public Action<TItem>? OnItemDoubleClicked;
    public string? SelectedId => _selectedId;

    public TreeView(ITreeDataProvider<TItem> provider)
    {
        _provider = provider;
    }

    public void SelectItem(string? id) => _selectedId = id;

    public void Draw(float width, float height, InputManager? input = null)
    {
        _strideInput = input;
        bool rightClicked = input?.IsMouseButtonPressed(MouseButton.Right) ?? false;

        ImGui.BeginChild("TreeViewPanel", new Vector2(width, height), ImGuiChildFlags.None);

        // Search bar
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##treeFilter", ref _filter, 128);

        ImGui.Separator();

        // Scrollable tree content
        if (ImGui.BeginChild("TreeContent"))
        {
            var groups = _provider.GetGroups();
            if (groups != null && groups.Count > 0)
            {
                foreach (var group in groups)
                    DrawGroup(group, rightClicked);
            }
            else
            {
                DrawItems(_provider.GetItems(null), rightClicked, 0);
            }

            // Right-click on background
            if (rightClicked && ImGui.IsWindowHovered() && !_contextMenuOpen)
            {
                _contextMenuRequest = new TreeContextRequest<TItem>
                {
                    Target = TreeContextTarget.Background,
                };
                _contextMenuScreenPos = ImGui.GetIO().MousePos;
                _contextMenuOpen = true;
                _contextMenuAge = 0;
            }
        }
        ImGui.EndChild();

        ImGui.EndChild();

        // Context menu rendered outside child
        DrawContextMenuWindow();

        _wasRightDown = input?.IsMouseButtonDown(MouseButton.Right) ?? false;
    }

    // ─── Groups ──────────────────────────────────────────────────

    private void DrawGroup(TreeGroup group, bool rightClicked)
    {
        var items = _provider.GetItems(group.Id);
        var filtered = FilterItems(items);
        if (filtered.Count == 0 && !string.IsNullOrEmpty(_filter)) return;

        bool hasFilter = !string.IsNullOrEmpty(_filter);
        var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (hasFilter) flags |= ImGuiTreeNodeFlags.DefaultOpen;

        if (group.Color.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, group.Color.Value);

        bool open = ImGui.TreeNodeEx($"{group.Label} ({filtered.Count})##{group.Id}", flags);

        if (group.Color.HasValue)
            ImGui.PopStyleColor();

        // Right-click on group header
        if (rightClicked && ImGui.IsItemHovered())
        {
            _contextMenuRequest = new TreeContextRequest<TItem>
            {
                Target = TreeContextTarget.Group,
                GroupId = group.Id,
            };
            _contextMenuScreenPos = ImGui.GetIO().MousePos;
            _contextMenuOpen = true;
            _contextMenuAge = 0;
        }

        if (open)
        {
            DrawItems(filtered, rightClicked, 0);
            ImGui.TreePop();
        }
    }

    // ─── Items ───────────────────────────────────────────────────

    private void DrawItems(IReadOnlyList<TItem> items, bool rightClicked, int depth)
    {
        foreach (var item in items)
        {
            DrawItem(item, rightClicked, depth);
        }
    }

    private void DrawItem(TItem item, bool rightClicked, int depth)
    {
        string id = _provider.GetId(item);
        string label = _provider.GetLabel(item);
        string? badge = _provider.GetBadge(item);
        bool expandable = _provider.IsExpandable(item);
        bool selected = id == _selectedId;
        var color = _provider.GetColor(item);

        if (color.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, color.Value);

        if (expandable)
        {
            var flags = ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.OpenOnArrow;
            if (selected) flags |= ImGuiTreeNodeFlags.Selected;

            string nodeLabel = badge != null ? $"{label}  " : label;
            bool open = ImGui.TreeNodeEx($"{nodeLabel}##{id}", flags);

            // Badge rendered after label in dim color
            if (badge != null)
            {
                ImGui.SameLine();
                if (color.HasValue) ImGui.PopStyleColor();
                ImGui.PushStyleColor(ImGuiCol.Text, BadgeColor);
                ImGui.Text($"({badge})");
                ImGui.PopStyleColor();
                if (color.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                _selectedId = id;
                OnItemSelected?.Invoke(item);
            }

            if (ImGui.IsItemHovered() && ImGui.GetIO().MouseDoubleClicked[0])
                OnItemDoubleClicked?.Invoke(item);

            // Right-click
            if (rightClicked && ImGui.IsItemHovered())
            {
                _contextMenuRequest = new TreeContextRequest<TItem>
                {
                    Target = TreeContextTarget.Item,
                    Item = item,
                };
                _contextMenuScreenPos = ImGui.GetIO().MousePos;
                _contextMenuOpen = true;
                _contextMenuAge = 0;
            }

            // Tooltip
            if (ImGui.IsItemHovered())
            {
                var tooltip = _provider.GetTooltip(item);
                if (tooltip != null) ImGui.SetTooltip(tooltip);
            }

            if (open)
            {
                var children = _provider.GetChildren(item);
                DrawItems(children, rightClicked, depth + 1);
                ImGui.TreePop();
            }
        }
        else
        {
            // Leaf node — selectable
            if (ImGui.Selectable($"{label}##{id}", selected))
            {
                _selectedId = id;
                OnItemSelected?.Invoke(item);
            }

            if (ImGui.IsItemHovered() && ImGui.GetIO().MouseDoubleClicked[0])
                OnItemDoubleClicked?.Invoke(item);

            if (rightClicked && ImGui.IsItemHovered())
            {
                _contextMenuRequest = new TreeContextRequest<TItem>
                {
                    Target = TreeContextTarget.Item,
                    Item = item,
                };
                _contextMenuScreenPos = ImGui.GetIO().MousePos;
                _contextMenuOpen = true;
                _contextMenuAge = 0;
            }

            if (ImGui.IsItemHovered())
            {
                var tooltip = _provider.GetTooltip(item);
                if (tooltip != null) ImGui.SetTooltip(tooltip);
            }
        }

        if (color.HasValue)
            ImGui.PopStyleColor();
    }

    // ─── Filtering ───────────────────────────────────────────────

    private IReadOnlyList<TItem> FilterItems(IReadOnlyList<TItem> items)
    {
        if (string.IsNullOrEmpty(_filter)) return items;
        return items.Where(i => _provider.MatchesFilter(i, _filter)).ToList();
    }

    // ─── Context Menu ────────────────────────────────────────────

    private void DrawContextMenuWindow()
    {
        if (!_contextMenuOpen || ContextMenu == null || _contextMenuRequest == null) return;

        var items = ContextMenu.GetMenuItems(_contextMenuRequest);
        if (items.Count == 0)
        {
            _contextMenuOpen = false;
            return;
        }

        ImGui.SetNextWindowPos(_contextMenuScreenPos);
        ImGui.SetNextWindowSize(new Vector2(200, 0));
        ImGui.SetNextWindowFocus();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.14f, 0.14f, 0.19f, 0.97f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.35f, 0.35f, 0.45f, 0.8f));

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings;

        if (ImGui.Begin("Menu##TreeCtxMenu", flags))
        {
            _contextMenuAge++;

            bool clicked = false;
            foreach (var item in items)
            {
                if (item.Separator)
                {
                    ImGui.Separator();
                    continue;
                }

                bool hasColor = item.Color != 0xFFFFFFFF;
                if (hasColor)
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertU32ToFloat4(item.Color));

                if (item.Children is { Count: > 0 })
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, HeaderColor);
                    ImGui.Text(string.IsNullOrEmpty(item.Label) ? "..." : item.Label);
                    ImGui.PopStyleColor();
                    ImGui.Indent(12);
                    foreach (var child in item.Children)
                    {
                        if (child.Separator) { ImGui.Separator(); continue; }
                        bool childColor = child.Color != 0xFFFFFFFF;
                        if (childColor)
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertU32ToFloat4(child.Color));
                        var childLabel = string.IsNullOrEmpty(child.Label) ? "???" : child.Label;
                        if (ImGui.Selectable(childLabel))
                        {
                            ContextMenu.OnItemSelected(child, _contextMenuRequest);
                            clicked = true;
                        }
                        if (childColor) ImGui.PopStyleColor();
                    }
                    ImGui.Unindent(12);
                }
                else
                {
                    if (ImGui.Selectable(item.Label))
                    {
                        ContextMenu.OnItemSelected(item, _contextMenuRequest);
                        clicked = true;
                    }
                }

                if (hasColor) ImGui.PopStyleColor();
            }

            if (clicked) _contextMenuOpen = false;

            // Dismiss on click outside
            if (_contextMenuAge > 2 &&
                !ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
            {
                var io = ImGui.GetIO();
                bool rightPressed = _strideInput?.IsMouseButtonPressed(MouseButton.Right) ?? false;
                if (io.MouseDown[0] || rightPressed)
                    _contextMenuOpen = false;
            }
        }
        ImGui.End();

        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(2);
    }
}
