using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Services;
using HytaleAdmin.UI.Components.Forms;

namespace HytaleAdmin.UI.Components.MapActions;

public class PlacePrefabAction : IMapAction
{
    private readonly HytaleApiClient _client;
    private readonly SearchableDropdown _prefabPicker;
    private int _rotation;

    public string Label => "Place Prefab";
    public bool IsValid => _prefabPicker.HasSelection;

    public PlacePrefabAction(HytaleApiClient client)
    {
        _client = client;
        _prefabPicker = new SearchableDropdown("place_prefab", async () =>
        {
            var entities = await client.GetAssetEntitiesAllPagesAsync("Prefabs");
            return entities.Select(e => e.Label).Distinct().OrderBy(s => s).ToArray();
        });
    }

    public void DrawForm()
    {
        _prefabPicker.Draw("Prefab");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Rotation");
        ImGui.SetNextItemWidth(100);
        string[] rotations = ["0", "90", "180", "270"];
        ImGui.Combo("##prefab_rot", ref _rotation, rotations, rotations.Length);
    }

    public async Task<MapActionResult> ExecuteAsync(float worldX, float worldY, float worldZ)
    {
        int rot = _rotation * 90;
        var result = await _client.PlaceAssetAsync("Prefabs", _prefabPicker.Selected, "default",
            worldX + 0.5f, worldY, worldZ + 0.5f, rot);
        return new MapActionResult(
            result?.Success == true,
            result?.Success == true ? $"Placed {_prefabPicker.Selected}" : $"Failed: {result?.Errors?.FirstOrDefault() ?? "Unknown"}");
    }
}
