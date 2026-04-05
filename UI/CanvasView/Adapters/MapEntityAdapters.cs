using HytaleAdmin.Models.Api;

namespace HytaleAdmin.UI.CanvasView.Adapters;

public class PlayerMapEntity : IMapEntity
{
    private readonly PlayerDto _dto;
    public PlayerMapEntity(PlayerDto dto) => _dto = dto;
    public string Id => _dto.Uuid ?? "";
    public string EntityType => "player";
    public string Label => _dto.Name ?? "Player";
    public float WorldX => _dto.X;
    public float WorldZ => _dto.Z;
    public float WorldY => _dto.Y;
    public PlayerDto Dto => _dto;
}

public class NpcMapEntity : IMapEntity
{
    private readonly EntityDto _dto;
    public NpcMapEntity(EntityDto dto) => _dto = dto;
    public string Id => _dto.Uuid ?? "";
    public string EntityType => "npc";
    public string Label => !string.IsNullOrEmpty(_dto.Name) ? _dto.Name : _dto.Type ?? "NPC";
    public float WorldX => _dto.X;
    public float WorldZ => _dto.Z;
    public float WorldY => _dto.Y;
    public EntityDto Dto => _dto;
}

public class SoundZoneMapEntity : IMapAreaEntity
{
    private readonly SoundZoneDto _dto;
    public SoundZoneMapEntity(SoundZoneDto dto) => _dto = dto;
    public string Id => _dto.Key;
    public string EntityType => "soundzone";
    public string Label { get { var s = _dto.Sound ?? "Zone"; var i = s.LastIndexOf('/'); return i >= 0 ? s[(i + 1)..] : s; } }
    public float WorldX => (_dto.MinX + _dto.MaxX) / 2f;
    public float WorldZ => (_dto.MinZ + _dto.MaxZ) / 2f;
    public float WorldY => _dto.Y;
    public float MinX => _dto.MinX;
    public float MinZ => _dto.MinZ;
    public float MaxX => _dto.MaxX;
    public float MaxZ => _dto.MaxZ;
    public SoundZoneDto Dto => _dto;
}

public class LocationMapEntity : IMapEntity
{
    private readonly PluginEntitySummaryDto _dto;
    public LocationMapEntity(PluginEntitySummaryDto dto) => _dto = dto;
    public string Id => _dto.Id;
    public string EntityType => "location";
    public string Label => _dto.Label ?? _dto.Id.Replace("loc:", "");
    public float WorldX => _dto.X;
    public float WorldZ => _dto.Z;
    public float WorldY => _dto.Y;
    public PluginEntitySummaryDto Dto => _dto;
}
