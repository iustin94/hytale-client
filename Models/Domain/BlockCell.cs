namespace HytaleAdmin.Models.Domain;

public class BlockCell
{
    public int X { get; init; }
    public int Z { get; init; }
    public int Y { get; init; }
    public string Block { get; init; } = "";
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
}
