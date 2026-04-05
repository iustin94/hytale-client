using System.Numerics;
using Hexa.NET.ImGui;

namespace HytaleAdmin.UI.Components.Forms;

/// <summary>
/// Reusable searchable dropdown: text filter + scrollable selectable list.
/// Loads options asynchronously via a provider function.
/// </summary>
public class SearchableDropdown
{
    private readonly string _id;
    private readonly Func<Task<string[]>> _loadOptions;

    private string[] _options = [];
    private bool _loading;
    private bool _loaded;
    private string _filter = "";
    private string _selected = "";

    public string Selected => _selected;
    public bool HasSelection => !string.IsNullOrEmpty(_selected);

    public SearchableDropdown(string id, Func<Task<string[]>> loadOptions)
    {
        _id = id;
        _loadOptions = loadOptions;
    }

    public void SetSelected(string value) => _selected = value;

    public void Reset()
    {
        _selected = "";
        _filter = "";
    }

    public void Reload()
    {
        _loaded = false;
        _loading = false;
    }

    public void Draw(string label, float listHeight = 120f)
    {
        // Load on first draw
        if (!_loaded && !_loading)
        {
            _loading = true;
            _ = Task.Run(async () =>
            {
                try { _options = await _loadOptions(); }
                catch { _options = []; }
                finally { _loading = false; _loaded = true; }
            });
        }

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), label);

        ImGui.SetNextItemWidth(-1);
        ImGui.InputText($"##filter_{_id}", ref _filter, 128);

        if (_loading)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.6f, 1f), "Loading...");
        }
        else if (_options.Length > 0)
        {
            if (ImGui.BeginChild($"list_{_id}", new Vector2(-1, listHeight)))
            {
                var filterLower = _filter.ToLowerInvariant();
                foreach (var opt in _options)
                {
                    if (!string.IsNullOrEmpty(_filter) &&
                        !opt.Contains(filterLower, StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool sel = opt == _selected;
                    if (ImGui.Selectable(opt, sel))
                        _selected = opt;
                }
            }
            ImGui.EndChild();
        }
        else if (_loaded)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.6f, 1f), "No options available");
        }

        if (HasSelection)
            ImGui.TextColored(new Vector4(0.31f, 0.80f, 0.40f, 1f), $"Selected: {_selected}");
    }
}
