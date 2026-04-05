using Hexa.NET.ImGui;
using HytaleAdmin.Services;
using Stride.Core.Mathematics;
using Stride.CommunityToolkit.ImGui;
using Stride.Graphics;

namespace HytaleAdmin.Rendering;

public class MapRenderer
{
    private readonly GraphicsDevice _graphicsDevice;

    // Texture state
    private Texture? _mapTexture;
    private int _originX;
    private int _originZ;
    private int _texWidth;
    private int _texHeight;

    // Pan/zoom state
    private float _panX;
    private float _panZ;
    private float _zoom = 4f;
    private bool _isPanning;
    private bool _wasLeftDown;
    private System.Numerics.Vector2 _panStart;
    private System.Numerics.Vector2 _panStartOffset;

    private const float MinZoom = 0.5f;
    private const float MaxZoom = 40f;
    private const float ZoomSpeed = 1.2f;

    /// <summary>Set by EditorScene to show cursor position info at bottom-right of map.</summary>
    public string? CursorInfoText { get; set; }

    // ImGui window position/size for coordinate conversion
    private System.Numerics.Vector2 _windowPos;
    private System.Numerics.Vector2 _windowSize;

    public MapRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public bool IsPanning => _isPanning;
    public float PanDistance { get; private set; }
    public System.Numerics.Vector2 WindowPos => _windowPos;
    public System.Numerics.Vector2 WindowSize => _windowSize;
    public Texture? MapTexture => _mapTexture;
    public int OriginX => _originX;
    public int OriginZ => _originZ;
    public int TexWidth => _texWidth;
    public int TexHeight => _texHeight;

    /// <summary>
    /// Draw the entire map panel contents. Call between BeginChild/EndChild in the parent.
    /// Draws background, texture, handles input, and returns the draw list for overlays.
    /// </summary>
    public ImDrawListPtr Draw()
    {
        _windowPos = ImGui.GetCursorScreenPos();
        _windowSize = ImGui.GetContentRegionAvail();

        var drawList = ImGui.GetWindowDrawList();
        var clipMax = new System.Numerics.Vector2(_windowPos.X + _windowSize.X, _windowPos.Y + _windowSize.Y);

        // Background
        drawList.AddRectFilled(_windowPos, clipMax,
            ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.07f, 0.07f, 0.13f, 1f)));

        // Map texture
        if (_mapTexture != null)
        {
            float imgW = _texWidth * _zoom;
            float imgH = _texHeight * _zoom;
            var imgPos = new System.Numerics.Vector2(_windowPos.X + _panX, _windowPos.Y + _panZ);

            drawList.PushClipRect(_windowPos, clipMax);
            ImGui.SetCursorScreenPos(imgPos);
            ImGuiExtension.Image(_mapTexture, (int)imgW, (int)imgH);
            drawList.PopClipRect();

            // Status text at bottom
            var statusY = _windowPos.Y + _windowSize.Y - 20;
            var yellowU32 = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1f, 1f, 0f, 1f));
            drawList.AddText(
                new System.Numerics.Vector2(_windowPos.X + 8, statusY),
                yellowU32,
                $"Map: {_texWidth}x{_texHeight}  Zoom: {_zoom * 100 / 4f:F0}%  Center: ({ViewCenterX:F0}, {ViewCenterZ:F0})");

            // Cursor info (right-aligned)
            if (!string.IsNullOrEmpty(CursorInfoText))
            {
                var infoSize = ImGui.CalcTextSize(CursorInfoText);
                var lightGrayU32 = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.78f, 0.78f, 0.82f, 1f));
                drawList.AddText(
                    new System.Numerics.Vector2(_windowPos.X + _windowSize.X - infoSize.X - 8, statusY),
                    lightGrayU32,
                    CursorInfoText);
            }
        }

        // Handle input
        if (ImGui.IsWindowHovered())
            HandleImGuiInput();
        else if (_isPanning)
            _isPanning = false;

        return drawList;
    }

    private void HandleImGuiInput()
    {
        var io = ImGui.GetIO();
        var mousePos = io.MousePos;
        var localMouse = new System.Numerics.Vector2(mousePos.X - _windowPos.X, mousePos.Y - _windowPos.Y);

        // Pan with middle mouse button
        bool middleDown = io.MouseDown[2];
        bool middleClicked = middleDown && !_wasLeftDown;
        bool middleReleased = !middleDown && _wasLeftDown;

        if (middleClicked)
        {
            _isPanning = true;
            _panStart = localMouse;
            _panStartOffset = new System.Numerics.Vector2(_panX, _panZ);
            PanDistance = 0;
        }

        if (_isPanning && middleDown)
        {
            var delta = new System.Numerics.Vector2(localMouse.X - _panStart.X, localMouse.Y - _panStart.Y);
            _panX = _panStartOffset.X + delta.X;
            _panZ = _panStartOffset.Y + delta.Y;
            PanDistance = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        }

        if (middleReleased)
            _isPanning = false;

        _wasLeftDown = middleDown;

        // Zoom: scroll wheel toward cursor
        if (MathF.Abs(io.MouseWheel) > 0.001f)
        {
            var worldBefore = ScreenToWorldInternal(localMouse);

            float factor = io.MouseWheel > 0 ? ZoomSpeed : 1f / ZoomSpeed;
            _zoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);

            if (worldBefore != null)
            {
                var screenAfter = WorldToScreenLocal(worldBefore.Value.X, worldBefore.Value.Y);
                _panX += localMouse.X - screenAfter.X;
                _panZ += localMouse.Y - screenAfter.Y;
            }
        }
    }

    public void UpdateTexture(MapDataService mapData)
    {
        var bounds = mapData.GetBounds();
        int width = bounds.maxX - bounds.minX;
        int height = bounds.maxZ - bounds.minZ;
        if (width <= 0 || height <= 0) return;

        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var cell in mapData.Blocks.Values)
        {
            if (cell.Y < minY) minY = cell.Y;
            if (cell.Y > maxY) maxY = cell.Y;
        }
        float yRange = Math.Max(maxY - minY, 1);

        var pixels = new Color[width * height];
        var bgColor = new Color(26, 26, 46);
        Array.Fill(pixels, bgColor);

        foreach (var cell in mapData.Blocks.Values)
        {
            int px = cell.X - bounds.minX;
            int pz = cell.Z - bounds.minZ;
            if (px < 0 || px >= width || pz < 0 || pz >= height) continue;

            float heightFactor = 0.7f + 0.3f * ((cell.Y - minY) / yRange);
            var r = (byte)Math.Min(255, (int)(cell.R * heightFactor));
            var g = (byte)Math.Min(255, (int)(cell.G * heightFactor));
            var b = (byte)Math.Min(255, (int)(cell.B * heightFactor));

            pixels[pz * width + px] = new Color(r, g, b);
        }

        _originX = bounds.minX;
        _originZ = bounds.minZ;
        _texWidth = width;
        _texHeight = height;

        _mapTexture?.Dispose();
        _mapTexture = Texture.New2D(_graphicsDevice, width, height, PixelFormat.R8G8B8A8_UNorm,
            pixels, TextureFlags.ShaderResource, GraphicsResourceUsage.Default);
    }

    public void LookAt(float worldX, float worldZ)
    {
        _panX = _windowSize.X / 2f - (worldX - _originX) * _zoom;
        _panZ = _windowSize.Y / 2f - (worldZ - _originZ) * _zoom;
    }

    public Vector2? ScreenToWorld(Vector2 screenPos)
    {
        if (_mapTexture == null) return null;
        var local = new System.Numerics.Vector2(screenPos.X - _windowPos.X, screenPos.Y - _windowPos.Y);
        return ScreenToWorldInternal(local);
    }

    private Vector2? ScreenToWorldInternal(System.Numerics.Vector2 localPos)
    {
        if (_mapTexture == null) return null;
        float worldX = (localPos.X - _panX) / _zoom + _originX;
        float worldZ = (localPos.Y - _panZ) / _zoom + _originZ;
        return new Vector2(worldX, worldZ);
    }

    public Vector2? WorldToScreen(float worldX, float worldZ)
    {
        if (_mapTexture == null) return null;
        var local = WorldToScreenLocal(worldX, worldZ);
        return new Vector2(_windowPos.X + local.X, _windowPos.Y + local.Y);
    }

    private System.Numerics.Vector2 WorldToScreenLocal(float worldX, float worldZ)
    {
        float sx = (worldX - _originX) * _zoom + _panX;
        float sz = (worldZ - _originZ) * _zoom + _panZ;
        return new System.Numerics.Vector2(sx, sz);
    }

    public float BlockScreenSize => _zoom;
    public float ViewCenterX => (_windowSize.X / 2f - _panX) / _zoom + _originX;
    public float ViewCenterZ => (_windowSize.Y / 2f - _panZ) / _zoom + _originZ;

    public void Clear()
    {
        _mapTexture?.Dispose();
        _mapTexture = null;
    }
}
