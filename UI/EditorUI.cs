using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Domain;
using HytaleAdmin.Rendering;
using Stride.CommunityToolkit.ImGui;
using Stride.Core;
using Stride.Engine;
using Stride.Games;

namespace HytaleAdmin.UI;

public class EditorUI : GameSystem
{
    private readonly ServiceContainer _services;
    private readonly MapRenderer _mapRenderer;
    private readonly EntityRenderer _entityRenderer;
    private readonly SelectionRenderer _selectionRenderer;

    private HeaderBar? _headerBar;
    private AssetBrowserPanel? _assetBrowser;
    private InspectorPanel? _inspector;
    private TriggerPanel? _triggerPanel;
    private LogPanel _logPanel = new();

    // Left panel tab state
    private int _leftTab; // 0 = Assets, 1 = Triggers

    private ImGuiSystem? _imGui;

    public TriggerPanel? Triggers => _triggerPanel;
    public LogPanel Log => _logPanel;

    public EditorUI(IServiceRegistry services, ServiceContainer appServices,
        MapRenderer mapRenderer, EntityRenderer entityRenderer, SelectionRenderer selectionRenderer,
        Func<Task> loadMapCallback)
        : base(services)
    {
        _services = appServices;
        _mapRenderer = mapRenderer;
        _entityRenderer = entityRenderer;
        _selectionRenderer = selectionRenderer;

        _headerBar = new HeaderBar(appServices, loadMapCallback);
        _assetBrowser = new AssetBrowserPanel(appServices);
        _inspector = new InspectorPanel(appServices);
        _triggerPanel = new TriggerPanel(appServices);

        Game.GameSystems.Add(this);
        Enabled = true;
    }

    public override void Update(GameTime gameTime)
    {
        _imGui ??= Services.GetService<ImGuiSystem>();
        if (_imGui == null) return;

        if (UpdateOrder <= _imGui.UpdateOrder)
        {
            UpdateOrder = _imGui.UpdateOrder + 1;
            return;
        }

        try
        {
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);

            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

            if (ImGui.Begin("Editor", flags))
            {
                Draw();
            }
            ImGui.End();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorUI] Update error: {ex}");
        }
    }

    private void Draw()
    {
        // Header region
        _headerBar?.Draw();
        ImGui.Separator();

        // Calculate panel sizes
        float panelWidth = 270f;
        var avail = ImGui.GetContentRegionAvail();

        // ─── Left panel: Assets / Triggers ────────────────────────

        ImGui.BeginChild("LeftPanel", new System.Numerics.Vector2(panelWidth, avail.Y), ImGuiChildFlags.Borders);

        // Tab buttons for left panel
        var accentColor = new System.Numerics.Vector4(0.91f, 0.27f, 0.38f, 1f);
        var triggerColor = new System.Numerics.Vector4(0.80f, 0.60f, 0.20f, 1f);
        var defaultBtnColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];

        ImGui.PushStyleColor(ImGuiCol.Button, _leftTab == 0 ? accentColor : defaultBtnColor);
        if (ImGui.SmallButton("Assets##ltab")) _leftTab = 0;
        ImGui.PopStyleColor();

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Button, _leftTab == 1 ? triggerColor : defaultBtnColor);
        if (ImGui.SmallButton("Triggers##ltab")) _leftTab = 1;
        ImGui.PopStyleColor();

        ImGui.Separator();

        if (_leftTab == 0)
            _assetBrowser?.Draw();
        else
            _triggerPanel?.Draw();

        ImGui.EndChild();

        ImGui.SameLine();

        // ─── Center column: Map + Log ─────────────────────────────

        float centerWidth = avail.X - panelWidth * 2 - ImGui.GetStyle().ItemSpacing.X * 2;
        float logHeight = 150f;
        float spacing = ImGui.GetStyle().ItemSpacing.Y;
        float mapHeight = avail.Y - logHeight - spacing;

        ImGui.BeginGroup();

        // Map view
        ImGui.BeginChild("CenterPanel", new System.Numerics.Vector2(centerWidth, mapHeight),
            ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var drawList = _mapRenderer.Draw();
        _selectionRenderer.DrawOverlays(drawList);
        _entityRenderer.DrawOverlays(drawList);

        if (_triggerPanel != null)
            DrawTriggerZones(drawList);

        ImGui.EndChild();

        // Log panel
        ImGui.BeginChild("LogPanel", new System.Numerics.Vector2(centerWidth, logHeight), ImGuiChildFlags.Borders);
        _logPanel.Draw();
        ImGui.EndChild();

        ImGui.EndGroup();

        ImGui.SameLine();

        // ─── Right panel: Inspector ───────────────────────────────

        ImGui.BeginChild("RightPanel", new System.Numerics.Vector2(panelWidth, avail.Y), ImGuiChildFlags.Borders);
        _inspector?.Draw();
        ImGui.EndChild();
    }

    private void DrawTriggerZones(ImDrawListPtr drawList)
    {
        if (_triggerPanel == null) return;

        var triggerFill = new System.Numerics.Vector4(0.80f, 0.60f, 0.20f, 0.15f);
        var triggerBorder = new System.Numerics.Vector4(0.80f, 0.60f, 0.20f, 0.5f);
        var triggerDisabledFill = new System.Numerics.Vector4(0.50f, 0.50f, 0.50f, 0.08f);
        var triggerDisabledBorder = new System.Numerics.Vector4(0.50f, 0.50f, 0.50f, 0.3f);

        foreach (var trigger in _triggerPanel.Triggers)
        {
            if (trigger.Condition.Type != TriggerConditionType.PlayerEntersArea) continue;

            var fill = trigger.Enabled ? triggerFill : triggerDisabledFill;
            var border = trigger.Enabled ? triggerBorder : triggerDisabledBorder;

            var minScreen = _mapRenderer.WorldToScreen(trigger.Condition.MinX, trigger.Condition.MinZ);
            var maxScreen = _mapRenderer.WorldToScreen(trigger.Condition.MaxX, trigger.Condition.MaxZ);

            if (minScreen == null || maxScreen == null) continue;

            var fillU32 = ImGui.ColorConvertFloat4ToU32(fill);
            var borderU32 = ImGui.ColorConvertFloat4ToU32(border);

            float x1 = Math.Min(minScreen.Value.X, maxScreen.Value.X);
            float y1 = Math.Min(minScreen.Value.Y, maxScreen.Value.Y);
            float x2 = Math.Max(minScreen.Value.X, maxScreen.Value.X);
            float y2 = Math.Max(minScreen.Value.Y, maxScreen.Value.Y);

            drawList.AddRectFilled(new System.Numerics.Vector2(x1, y1), new System.Numerics.Vector2(x2, y2), fillU32);
            drawList.AddRect(new System.Numerics.Vector2(x1, y1), new System.Numerics.Vector2(x2, y2), borderU32);

            // Label
            var labelU32 = ImGui.ColorConvertFloat4ToU32(border with { W = 0.9f });
            drawList.AddText(new System.Numerics.Vector2(x1 + 2, y1 + 1), labelU32, trigger.Name);
        }
    }

    public async Task LoadAssetsAsync()
    {
        try
        {
            if (_assetBrowser != null)
                await _assetBrowser.LoadAssetsAsync(_services.ApiClient);

            if (_headerBar != null)
            {
                await _headerBar.LoadEntityTypesAsync(_services.ApiClient, _services.Config);
                await _headerBar.LoadServerInfoAsync(_services.ApiClient);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EditorUI] LoadAssetsAsync error: {ex}");
        }
    }

    public void SetStatus(string message) => _headerBar?.SetStatus(message);

}
