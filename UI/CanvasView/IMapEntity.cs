namespace HytaleAdmin.UI.CanvasView;

/// <summary>Point entity on a 2D canvas with world coordinates.</summary>
public interface IMapEntity
{
    string Id { get; }
    string EntityType { get; }
    string Label { get; }
    float WorldX { get; }
    float WorldZ { get; }
    float WorldY { get; }
}

/// <summary>Area entity with bounding box (sound zones, regions).</summary>
public interface IMapAreaEntity : IMapEntity
{
    float MinX { get; }
    float MinZ { get; }
    float MaxX { get; }
    float MaxZ { get; }
}
