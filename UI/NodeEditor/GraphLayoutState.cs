using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HytaleAdmin.UI.NodeEditor;

public class GraphLayoutState
{
    [JsonPropertyName("pan")] public float[] Pan { get; set; } = [0, 0];
    [JsonPropertyName("zoom")] public float Zoom { get; set; } = 1f;
    [JsonPropertyName("nodes")] public Dictionary<string, float[]> NodePositions { get; set; } = new();

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".hytale-admin");

    public static GraphLayoutState Load(string graphId)
    {
        try
        {
            var path = GetPath(graphId);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<GraphLayoutState>(json) ?? new();
            }
        }
        catch { }
        return new();
    }

    public static void Save(string graphId, GraphLayoutState state)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(GetPath(graphId), json);
        }
        catch { }
    }

    private static string GetPath(string graphId) => Path.Combine(Dir, $"graph_{graphId}.json");

    // ─── Helpers ─────────────────────────────────────────────────

    public void CaptureFrom<TNode>(NodeEditor<TNode> editor) where TNode : class, INode
    {
        Pan = [editor.Pan.X, editor.Pan.Y];
        Zoom = editor.Zoom;
        NodePositions.Clear();
        foreach (var node in editor.Nodes)
            NodePositions[node.Id] = [node.Position.X, node.Position.Y];
    }

    public void ApplyTo<TNode>(NodeEditor<TNode> editor) where TNode : class, INode
    {
        if (Pan.Length == 2)
            editor.Pan = new Vector2(Pan[0], Pan[1]);
        editor.Zoom = Zoom;

        foreach (var node in editor.Nodes)
        {
            if (NodePositions.TryGetValue(node.Id, out var pos) && pos.Length == 2)
                node.Position = new Vector2(pos[0], pos[1]);
        }
    }

    public bool HasData => NodePositions.Count > 0;
}
