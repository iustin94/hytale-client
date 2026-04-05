using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;
using HytaleAdmin.UI.Components.Forms;

namespace HytaleAdmin.UI.Components.MapActions;

public class CreateSoundZoneAction : IMapAction
{
    private readonly HytaleApiClient _client;
    private readonly SearchableDropdown _soundPicker;
    private float _radius = 20f;
    private int _interval = 5;

    public string Label => "Create Sound Zone";
    public bool IsValid => _soundPicker.HasSelection && _radius > 0;

    public CreateSoundZoneAction(HytaleApiClient client)
    {
        _client = client;
        _soundPicker = new SearchableDropdown("sound_zone", async () =>
        {
            var sounds = await client.GetSoundListAsync();
            if (sounds == null) return [];
            return sounds.Values.SelectMany(v => v).Distinct().OrderBy(s => s).ToArray();
        });
    }

    public void DrawForm()
    {
        _soundPicker.Draw("Sound");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Radius (blocks)");
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("##szone_radius", ref _radius, 5f, 10f, "%.0f");
        if (_radius < 1) _radius = 1;
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.78f, 1f), "Interval (seconds)");
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##szone_interval", ref _interval);
        if (_interval < 1) _interval = 1;
    }

    public async Task<MapActionResult> ExecuteAsync(float worldX, float worldY, float worldZ)
    {
        var result = await _client.StartAmbientAsync(new SoundAmbientRequest
        {
            Sound = _soundPicker.Selected,
            World = "default",
            X = worldX, Y = worldY, Z = worldZ,
            MinX = worldX - _radius, MinZ = worldZ - _radius,
            MaxX = worldX + _radius, MaxZ = worldZ + _radius,
            Interval = _interval,
        });
        return new MapActionResult(
            result?.Success == true,
            result?.Success == true ? $"Created sound zone: {_soundPicker.Selected}" : $"Failed: {result?.Error ?? "Unknown"}",
            result?.Key);
    }
}
