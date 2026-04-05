using HytaleAdmin.Models.Api;
using HytaleAdmin.Models.Domain;

namespace HytaleAdmin.Services;

public class SelectionService
{
    public SelectedAsset? SelectedAsset { get; private set; }
    public EntityDto? SelectedEntity { get; private set; }
    public PlayerDto? SelectedPlayer { get; private set; }
    public SoundZoneDto? SelectedZone { get; private set; }
    public BlockCell? HoveredBlock { get; set; }

    // Area selection (shift+drag for sound zones)
    public (float startX, float startZ, float endX, float endZ)? AreaSelection { get; set; }

    public event Action? SelectionChanged;

    public void SelectAsset(string category, string id)
    {
        if (SelectedAsset?.Category == category && SelectedAsset?.Id == id)
        {
            SelectedAsset = null; // toggle off
        }
        else
        {
            SelectedAsset = new SelectedAsset(category, id);
        }
        ClearMapSelection();
        SelectionChanged?.Invoke();
    }

    public void UpdateAssetSize(int sizeX, int sizeZ)
    {
        if (SelectedAsset != null)
        {
            SelectedAsset = SelectedAsset with { SizeX = sizeX, SizeZ = sizeZ };
            SelectionChanged?.Invoke();
        }
    }

    public void RotateAsset()
    {
        if (SelectedAsset != null)
        {
            int newRot = (SelectedAsset.Rotation + 90) % 360;
            // Swap X/Z sizes on 90/270 rotations
            SelectedAsset = newRot % 180 == 0
                ? SelectedAsset with { Rotation = newRot }
                : SelectedAsset with { Rotation = newRot, SizeX = SelectedAsset.SizeZ, SizeZ = SelectedAsset.SizeX };
            SelectionChanged?.Invoke();
        }
    }

    public void DeselectAsset()
    {
        SelectedAsset = null;
        SelectionChanged?.Invoke();
    }

    public void SelectEntity(EntityDto entity)
    {
        SelectedEntity = entity;
        SelectedPlayer = null;
        SelectedZone = null;
        SelectionChanged?.Invoke();
    }

    public void SelectPlayer(PlayerDto player)
    {
        SelectedPlayer = player;
        SelectedEntity = null;
        SelectedZone = null;
        SelectionChanged?.Invoke();
    }

    public void SelectZone(SoundZoneDto zone)
    {
        SelectedZone = zone;
        SelectedEntity = null;
        SelectedPlayer = null;
        SelectionChanged?.Invoke();
    }

    public void ClearMapSelection()
    {
        SelectedEntity = null;
        SelectedPlayer = null;
        SelectedZone = null;
    }
}
