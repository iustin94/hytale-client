using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;
using HytaleAdmin.UI.Components.Forms;

namespace HytaleAdmin.UI.Components.MapActions;

public class SpawnNpcAction : IMapAction
{
    private readonly HytaleApiClient _client;
    private readonly SearchableDropdown _typePicker;

    public string Label => "Spawn NPC";
    public bool IsValid => _typePicker.HasSelection;

    public SpawnNpcAction(HytaleApiClient client)
    {
        _client = client;
        _typePicker = new SearchableDropdown("spawn_npc_type", async () =>
        {
            var types = await client.GetEntityTypesAsync("default");
            return types?.Where(t => t != "HyCitizens").OrderBy(t => t).ToArray() ?? [];
        });
    }

    public void DrawForm() => _typePicker.Draw("NPC Type");

    public async Task<MapActionResult> ExecuteAsync(float worldX, float worldY, float worldZ)
    {
        var result = await _client.SpawnEntityAsync(new EntitySpawnRequest
        {
            Type = _typePicker.Selected,
            World = "default",
            X = worldX + 0.5, Y = worldY, Z = worldZ + 0.5,
        });
        return new MapActionResult(
            result?.Success == true,
            result?.Success == true ? $"Spawned {_typePicker.Selected}" : $"Failed: {result?.Error ?? "Unknown"}");
    }
}
