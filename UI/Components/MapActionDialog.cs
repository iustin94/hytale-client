using System.Numerics;
using Hexa.NET.ImGui;
using HytaleAdmin.Rendering;
using HytaleAdmin.Services;
using HytaleAdmin.UI.Components.MapActions;
using Stride.CommunityToolkit.ImGui;

namespace HytaleAdmin.UI.Components;

/// <summary>
/// Popup dialog combining a map picker (left) with an action form (right).
/// The user configures what to create in the form, picks where on the map, then confirms.
/// </summary>
public class MapActionDialog
{
    private readonly MapRenderer _mapRenderer;
    private readonly HytaleApiClient _client;

    private bool _open;
    private IMapAction? _action;
    private IMapAction[]? _actionChoices;
    private int _selectedActionIdx;

    // Map state (own pan/zoom, same as MapPickerDialog)
    private float _panX, _panZ;
    private float _zoom = 2f;
    private bool _isPanning;
    private Vector2 _panStart, _panStartOffset;
    private bool _initialized;

    // Picked location
    private Vector2 _pickedPos;
    private bool _hasPick;
    private bool _executing;
    private string? _resultMessage;
    private bool _resultIsError;

    // Callback after execution
    public Action<MapActionResult>? OnExecuted;

    public bool IsOpen => _open;

    public MapActionDialog(MapRenderer mapRenderer, HytaleApiClient client)
    {
        _mapRenderer = mapRenderer;
        _client = client;
    }

    /// <summary>Open with a single action.</summary>
    public void Open(IMapAction action)
    {
        _action = action;
        _actionChoices = null;
        Reset();
    }

    /// <summary>Open with multiple action choices (user picks from tabs).</summary>
    public void Open(IMapAction[] actions)
    {
        _actionChoices = actions;
        _selectedActionIdx = 0;
        _action = actions.Length > 0 ? actions[0] : null;
        Reset();
    }

    private void Reset()
    {
        _open = true;
        _hasPick = false;
        _executing = false;
        _resultMessage = null;
        _initialized = false;
    }

    public void Draw()
    {
        if (!_open || _action == null) return;

        var tex = _mapRenderer.MapTexture;
        if (tex == null)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 100));
            bool o = true;
            if (ImGui.Begin($"{_action.Label}##MapActionDlg", ref o, ImGuiWindowFlags.NoCollapse))
                ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.3f, 1f), "No map data loaded. Load the map first.");
            ImGui.End();
            if (!o) _open = false;
            return;
        }

        if (!_initialized)
        {
            _zoom = 2f;
            _panX = 300 - _mapRenderer.TexWidth * _zoom / 2f;
            _panZ = 200 - _mapRenderer.TexHeight * _zoom / 2f;
            _initialized = true;
        }

        ImGui.SetNextWindowSize(new Vector2(750, 480), ImGuiCond.Once);

        bool open = true;
        string title = _action.Label;
        if (ImGui.Begin($"{title}##MapActionDlg", ref open,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
        {
            var avail = ImGui.GetContentRegionAvail();
            float formWidth = 250f;
            float mapWidth = avail.X - formWidth - ImGui.GetStyle().ItemSpacing.X;
            float contentH = avail.Y - 30;

            // Left: map
            ImGui.BeginChild("ActionMap", new Vector2(mapWidth, contentH),
                ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            DrawMap(tex, mapWidth, contentH);
            ImGui.EndChild();

            ImGui.SameLine();

            // Right: form
            ImGui.BeginChild("ActionForm", new Vector2(formWidth, contentH), ImGuiChildFlags.Borders);

            // Action chooser tabs (if multiple actions)
            if (_actionChoices is { Length: > 1 })
            {
                for (int i = 0; i < _actionChoices.Length; i++)
                {
                    if (i > 0) ImGui.SameLine();
                    bool sel = i == _selectedActionIdx;
                    if (sel) ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.40f, 0.70f, 0.95f, 1f));
                    if (ImGui.SmallButton($"{_actionChoices[i].Label}##act{i}"))
                    {
                        _selectedActionIdx = i;
                        _action = _actionChoices[i];
                    }
                    if (sel) ImGui.PopStyleColor();
                }
                ImGui.Separator();
                ImGui.Spacing();
            }

            _action.DrawForm();

            ImGui.EndChild();

            // Bottom bar
            DrawBottomBar(avail.X);
        }
        ImGui.End();

        if (!open) _open = false;
    }

    private void DrawMap(Stride.Graphics.Texture tex, float width, float height)
    {
        var winPos = ImGui.GetCursorScreenPos();
        var winSize = new Vector2(width, height);
        var drawList = ImGui.GetWindowDrawList();
        var clipMax = winPos + winSize;

        drawList.AddRectFilled(winPos, clipMax, 0xFF_211311);

        float imgW = _mapRenderer.TexWidth * _zoom;
        float imgH = _mapRenderer.TexHeight * _zoom;
        var imgPos = new Vector2(winPos.X + _panX, winPos.Y + _panZ);

        drawList.PushClipRect(winPos, clipMax);
        ImGui.SetCursorScreenPos(imgPos);
        ImGuiExtension.Image(tex, (int)imgW, (int)imgH);

        // Input
        var io = ImGui.GetIO();
        var mousePos = io.MousePos;
        bool hovered = ImGui.IsWindowHovered();

        // Pan
        bool mid = io.MouseDown[2];
        if (hovered && mid && !_isPanning) { _isPanning = true; _panStart = mousePos; _panStartOffset = new Vector2(_panX, _panZ); }
        if (_isPanning) { _panX = _panStartOffset.X + (mousePos.X - _panStart.X); _panZ = _panStartOffset.Y + (mousePos.Y - _panStart.Y); if (!mid) _isPanning = false; }

        // Zoom
        if (hovered && MathF.Abs(io.MouseWheel) > 0.001f)
        {
            float old = _zoom;
            _zoom *= io.MouseWheel > 0 ? 1.15f : 1f / 1.15f;
            _zoom = Math.Clamp(_zoom, 0.5f, 20f);
            var lm = mousePos - winPos;
            float wx = (lm.X - _panX) / old;
            float wz = (lm.Y - _panZ) / old;
            _panX = lm.X - wx * _zoom;
            _panZ = lm.Y - wz * _zoom;
        }

        // Crosshair
        if (hovered)
        {
            drawList.AddLine(mousePos - new Vector2(10, 0), mousePos + new Vector2(10, 0), 0x99_FFFFFF);
            drawList.AddLine(mousePos - new Vector2(0, 10), mousePos + new Vector2(0, 10), 0x99_FFFFFF);
        }

        // Click to pick
        if (hovered && io.MouseClicked[0] && !_isPanning)
        {
            var local = mousePos - winPos;
            _pickedPos = new Vector2(
                (local.X - _panX) / _zoom + _mapRenderer.OriginX,
                (local.Y - _panZ) / _zoom + _mapRenderer.OriginZ);
            _hasPick = true;
        }

        // Draw pick marker
        if (_hasPick)
        {
            float sx = (_pickedPos.X - _mapRenderer.OriginX) * _zoom + _panX + winPos.X;
            float sy = (_pickedPos.Y - _mapRenderer.OriginZ) * _zoom + _panZ + winPos.Y;
            var sp = new Vector2(sx, sy);
            uint mc = 0xFF_3050F0;
            drawList.AddLine(sp - new Vector2(14, 0), sp + new Vector2(14, 0), mc, 2f);
            drawList.AddLine(sp - new Vector2(0, 14), sp + new Vector2(0, 14), mc, 2f);
            float d = 7f;
            drawList.AddQuad(sp + new Vector2(0, -d), sp + new Vector2(d, 0), sp + new Vector2(0, d), sp + new Vector2(-d, 0), mc, 2f);
            drawList.AddText(sp + new Vector2(12, -8), mc, $"({_pickedPos.X:F0}, {_pickedPos.Y:F0})");
        }

        drawList.PopClipRect();
    }

    private void DrawBottomBar(float totalWidth)
    {
        if (_hasPick)
        {
            ImGui.TextColored(new Vector4(0.95f, 0.30f, 0.35f, 1f),
                $"Position: ({_pickedPos.X:F0}, {_pickedPos.Y:F0})");
            ImGui.SameLine();
        }

        if (_resultMessage != null)
        {
            var color = _resultIsError ? new Vector4(0.9f, 0.3f, 0.3f, 1f) : new Vector4(0.31f, 0.80f, 0.40f, 1f);
            ImGui.TextColored(color, _resultMessage);
            ImGui.SameLine();
        }

        float btnW = 80;
        ImGui.SetCursorPosX(totalWidth - btnW * 2 - ImGui.GetStyle().ItemSpacing.X);

        bool canExec = _hasPick && (_action?.IsValid ?? false) && !_executing;
        if (!canExec) ImGui.BeginDisabled();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.31f, 0.80f, 0.40f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.1f, 0.12f, 1f));
        if (ImGui.Button(_executing ? "..." : "Confirm", new Vector2(btnW, 0)))
            Execute();
        ImGui.PopStyleColor(2);
        if (!canExec) ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(btnW, 0)))
            _open = false;
    }

    private void Execute()
    {
        if (_action == null || !_hasPick) return;
        _executing = true;
        _resultMessage = null;

        float wx = _pickedPos.X;
        float wz = _pickedPos.Y;

        _ = Task.Run(async () =>
        {
            try
            {
                // Resolve surface Y
                float y = 64;
                try
                {
                    var resp = await _client.GetSurfaceAsync("default", (int)wx, (int)wz, 0);
                    if (resp?.Surface is { Length: > 0 }) y = resp.Surface[0].Y + 1;
                }
                catch { }

                var result = await _action.ExecuteAsync(wx, y, wz);
                _resultMessage = result.Message;
                _resultIsError = !result.Success;
                if (result.Success)
                {
                    OnExecuted?.Invoke(result);
                    _open = false;
                }
            }
            catch (Exception ex)
            {
                _resultMessage = $"Error: {ex.Message}";
                _resultIsError = true;
            }
            finally { _executing = false; }
        });
    }
}
