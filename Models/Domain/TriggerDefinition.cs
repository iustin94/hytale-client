namespace HytaleAdmin.Models.Domain;

/// <summary>
/// Defines an area-based trigger: when a condition is met in a zone, execute an action.
/// Triggers are client-side configurations that can be activated manually.
/// </summary>
public class TriggerDefinition
{
    public string Name { get; set; } = "New Trigger";
    public TriggerCondition Condition { get; set; } = new();
    public TriggerAction Action { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

public class TriggerCondition
{
    public TriggerConditionType Type { get; set; } = TriggerConditionType.PlayerEntersArea;

    // Area bounds (for area-based conditions)
    public float MinX { get; set; }
    public float MinZ { get; set; }
    public float MaxX { get; set; }
    public float MaxZ { get; set; }
}

public enum TriggerConditionType
{
    PlayerEntersArea,
    Manual
}

public class TriggerAction
{
    public TriggerActionType Type { get; set; } = TriggerActionType.PlaySound;

    // Sound action
    public string SoundId { get; set; } = "";

    // Spawn action
    public string EntityType { get; set; } = "";

    // Ambient action
    public string AmbientSoundId { get; set; } = "";
    public int AmbientInterval { get; set; } = 5;

    // Position (for spawn/sound at specific point)
    public float X { get; set; }
    public float Y { get; set; } = 64;
    public float Z { get; set; }
}

public enum TriggerActionType
{
    PlaySound,
    SpawnEntity,
    StartAmbient,
    StopAmbient
}
