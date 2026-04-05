namespace HytaleAdmin.Models.Domain;

/// <summary>
/// Rotation: 0=North(default), 90=East, 180=South, 270=West
/// </summary>
public record SelectedAsset(string Category, string Id, int SizeX = 1, int SizeZ = 1, int Rotation = 0);
