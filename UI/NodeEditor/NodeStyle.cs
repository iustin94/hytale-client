using System.Numerics;

namespace HytaleAdmin.UI.NodeEditor;

public record NodeStyle
{
    public Vector4 HeaderColor { get; init; } = new(0.20f, 0.20f, 0.28f, 1f);
    public Vector4 BodyColor { get; init; } = new(0.14f, 0.14f, 0.19f, 0.95f);
    public Vector4 BorderColor { get; init; } = new(0.35f, 0.35f, 0.45f, 1f);
    public Vector4 SelectedBorderColor { get; init; } = new(0.95f, 0.75f, 0.20f, 1f);
    public Vector4 TitleColor { get; init; } = new(1f, 1f, 1f, 1f);
    public Vector4 SubtitleColor { get; init; } = new(0.60f, 0.60f, 0.68f, 1f);
    public float Rounding { get; init; } = 6f;
    public float BorderThickness { get; init; } = 1.5f;
    public float SelectedBorderThickness { get; init; } = 2.5f;
    public float MinWidth { get; init; } = 180f;
    public float HeaderHeight { get; init; } = 30f;
    public float PortRadius { get; init; } = 5f;
    public float PortSpacing { get; init; } = 24f;
    public float PortLabelOffset { get; init; } = 14f;
    public float BodyPadding { get; init; } = 8f;
}
