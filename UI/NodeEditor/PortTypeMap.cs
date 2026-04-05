namespace HytaleAdmin.UI.NodeEditor;

public class PortTypeMap
{
    private readonly Dictionary<string, HashSet<string>> _rules = new();

    public PortTypeMap Allow(string outputType, string inputType)
    {
        if (!_rules.TryGetValue(outputType, out var set))
            _rules[outputType] = set = new();
        set.Add(inputType);
        return this;
    }

    public bool CanConnect(string outputType, string inputType)
    {
        return _rules.TryGetValue(outputType, out var set) && set.Contains(inputType);
    }
}
