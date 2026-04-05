using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI.Components.MapActions;

public class CreateLocationAction : IMapAction
{
    private readonly HytaleApiClient _client;
    private string _label = "";
    private float _radius = 5f;

    public string Label => "Create Location";
    public bool IsValid => true; // label auto-generated if empty

    public CreateLocationAction(HytaleApiClient client)
    {
        _client = client;
    }

    public void DrawForm()
    {
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Label (optional)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##loc_label", ref _label, 128);
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Radius");
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("##loc_radius", ref _radius, 1f, 5f, "%.1f");
        if (_radius < 1) _radius = 1;
    }

    public async Task<MapActionResult> ExecuteAsync(float worldX, float worldY, float worldZ)
    {
        string label = string.IsNullOrWhiteSpace(_label)
            ? $"Location ({worldX:F0}, {worldZ:F0})"
            : _label;

        var result = await _client.ExecutePluginActionAsync("hyadventure", "createLocation", null,
            new Dictionary<string, string>
            {
                ["label"] = label,
                ["x"] = worldX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                ["y"] = worldY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                ["z"] = worldZ.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
                ["radius"] = _radius.ToString("F1", System.Globalization.CultureInfo.InvariantCulture),
            });

        return new MapActionResult(
            result?.Success == true,
            result?.Success == true ? $"Created: {label}" : $"Failed: {result?.Errors?.FirstOrDefault() ?? "Unknown"}",
            result?.EntityId);
    }
}
