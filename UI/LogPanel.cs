using Hexa.NET.ImGui;

namespace HytaleAdmin.UI;

/// <summary>
/// Scrollable log panel for debugging API requests and responses.
/// </summary>
public class LogPanel
{
    private readonly List<LogEntry> _entries = new();
    private bool _autoScroll = true;
    private const int MaxEntries = 500;

    private static readonly System.Numerics.Vector4 TimeColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly System.Numerics.Vector4 InfoColor = new(0.78f, 0.78f, 0.82f, 1f);
    private static readonly System.Numerics.Vector4 SuccessColor = new(0.31f, 0.80f, 0.40f, 1f);
    private static readonly System.Numerics.Vector4 ErrorColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 WarnColor = new(0.94f, 0.78f, 0.03f, 1f);
    private static readonly System.Numerics.Vector4 RequestColor = new(0.23f, 0.53f, 1f, 1f);
    private static readonly System.Numerics.Vector4 HeaderColor = new(0.63f, 0.63f, 0.71f, 1f);

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        _entries.Add(new LogEntry(DateTime.Now, message, level));
        if (_entries.Count > MaxEntries)
            _entries.RemoveAt(0);
    }

    public void LogRequest(string method, string url)
    {
        Log($"{method} {url}", LogLevel.Request);
    }

    public void LogResponse(string url, bool success, string? detail = null)
    {
        var msg = success ? $"OK {url}" : $"FAIL {url}";
        if (detail != null) msg += $" — {detail}";
        Log(msg, success ? LogLevel.Success : LogLevel.Error);
    }

    public void Draw()
    {
        ImGui.TextColored(HeaderColor, "Log");
        ImGui.SameLine();
        if (ImGui.SmallButton("Clear"))
            _entries.Clear();
        ImGui.SameLine();
        ImGui.Checkbox("Auto-scroll", ref _autoScroll);
        ImGui.Separator();

        if (ImGui.BeginChild("LogScroll", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None))
        {
            foreach (var entry in _entries)
            {
                ImGui.TextColored(TimeColor, entry.Time.ToString("HH:mm:ss"));
                ImGui.SameLine();

                var color = entry.Level switch
                {
                    LogLevel.Success => SuccessColor,
                    LogLevel.Error => ErrorColor,
                    LogLevel.Warn => WarnColor,
                    LogLevel.Request => RequestColor,
                    _ => InfoColor
                };
                ImGui.TextColored(color, entry.Message);
            }

            if (_autoScroll && _entries.Count > 0)
                ImGui.SetScrollHereY(1.0f);

            ImGui.EndChild();
        }
    }

    private record LogEntry(DateTime Time, string Message, LogLevel Level);
}

public enum LogLevel
{
    Info,
    Success,
    Error,
    Warn,
    Request
}
