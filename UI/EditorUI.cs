using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Domain;
using HytaleAdmin.Rendering;
using Stride.CommunityToolkit.ImGui;
using Stride.Core;
using Stride.Engine;
using Stride.Games;

namespace HytaleAdmin.UI;

public enum ViewMode { Map, Plugins, Ambience, Adventure }

public class EditorUI : GameSystem
{
    private readonly ServiceContainer _services;
    private readonly MapRenderer _mapRenderer;
    private readonly EntityRenderer _entityRenderer;
    private readonly SelectionRenderer _selectionRenderer;

    private HeaderBar? _headerBar;
    private AssetBrowserPanel? _assetBrowser;
    private ProjectTreePanel? _projectTree;
    private AmbienceBrowserPanel? _ambienceBrowser;
    private InspectorPanel? _inspector;
    private TriggerPanel? _triggerPanel;
    private PluginView? _pluginView;
    private AdventureView? _adventureView;
    private LogPanel _logPanel = new();

    // View mode: Map or Plugins
    private ViewMode _viewMode = ViewMode.Map;

    // Left panel tab state (map mode only)
    private int _leftTab; // 0 = World Objects, 1 = Assets, 2 = Triggers

    private ImGuiSystem? _imGui;

    /// <summary>Set by EditorScene to render the right-click context menu inside the map window.</summary>
    public Action? DrawMapContextMenu { get; set; }

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
        _projectTree = new ProjectTreePanel(appServices, mapRenderer);
        _ambienceBrowser = new AmbienceBrowserPanel(appServices);
        _inspector = new InspectorPanel(appServices);
        _triggerPanel = new TriggerPanel(appServices);
        _pluginView = new PluginView(appServices);
        _adventureView = new AdventureView(appServices, mapRenderer);

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
        // ─── Header: View mode toggle + header bar ────────────────
        DrawViewModeToggle();
        ImGui.SameLine();

        if (_viewMode == ViewMode.Map)
            _headerBar?.Draw();
        else
            _headerBar?.DrawMinimal();

        ImGui.Separator();

        // ─── Body: depends on view mode ───────────────────────────
        switch (_viewMode)
        {
            case ViewMode.Map: DrawMapMode(); break;
            case ViewMode.Plugins: DrawPluginMode(); break;
            case ViewMode.Ambience: DrawAmbienceMode(); break;
            case ViewMode.Adventure: DrawAdventureMode(); break;
        }
    }

    private static readonly System.Numerics.Vector4 ButtonTextColor = new(0.10f, 0.10f, 0.12f, 1f);

    private void DrawViewModeToggle()
    {
        var mapColor = new System.Numerics.Vector4(0.91f, 0.27f, 0.38f, 1f);
        var pluginColor = new System.Numerics.Vector4(0.40f, 0.70f, 0.95f, 1f);
        var defaultBtnColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];

        bool isMap = _viewMode == ViewMode.Map;
        ImGui.PushStyleColor(ImGuiCol.Button, isMap ? mapColor : defaultBtnColor);
        if (isMap) ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.SmallButton("Map")) _viewMode = ViewMode.Map;
        if (isMap) ImGui.PopStyleColor();
        ImGui.PopStyleColor();

        ImGui.SameLine();

        bool isPlugins = _viewMode == ViewMode.Plugins;
        ImGui.PushStyleColor(ImGuiCol.Button, isPlugins ? pluginColor : defaultBtnColor);
        if (isPlugins) ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.SmallButton("Plugins")) _viewMode = ViewMode.Plugins;
        if (isPlugins) ImGui.PopStyleColor();
        ImGui.PopStyleColor();

        ImGui.SameLine();

        var ambienceColor = new System.Numerics.Vector4(0.31f, 0.80f, 0.77f, 1f);
        bool isAmbience = _viewMode == ViewMode.Ambience;
        ImGui.PushStyleColor(ImGuiCol.Button, isAmbience ? ambienceColor : defaultBtnColor);
        if (isAmbience) ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.SmallButton("Ambience")) _viewMode = ViewMode.Ambience;
        if (isAmbience) ImGui.PopStyleColor();
        ImGui.PopStyleColor();

        ImGui.SameLine();

        var adventureColor = new System.Numerics.Vector4(0.85f, 0.55f, 0.20f, 1f);
        bool isAdventure = _viewMode == ViewMode.Adventure;
        ImGui.PushStyleColor(ImGuiCol.Button, isAdventure ? adventureColor : defaultBtnColor);
        if (isAdventure) ImGui.PushStyleColor(ImGuiCol.Text, ButtonTextColor);
        if (ImGui.SmallButton("Adventure")) _viewMode = ViewMode.Adventure;
        if (isAdventure) ImGui.PopStyleColor();
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.Text("|");
    }

    // ─── Map Mode (original layout) ──────────────────────────────

    private void DrawMapMode()
    {
        float panelWidth = 270f;
        var avail = ImGui.GetContentRegionAvail();

        // Left panel: Assets / Triggers
        ImGui.BeginChild("LeftPanel", new System.Numerics.Vector2(panelWidth, avail.Y), ImGuiChildFlags.Borders);

        _projectTree?.Draw();

        ImGui.EndChild();

        ImGui.SameLine();

        // Center column: Map + Log
        float centerWidth = avail.X - panelWidth * 2 - ImGui.GetStyle().ItemSpacing.X * 2;
        float logHeight = 150f;
        float spacing = ImGui.GetStyle().ItemSpacing.Y;
        float mapHeight = avail.Y - logHeight - spacing;

        ImGui.BeginGroup();

        ImGui.BeginChild("CenterPanel", new System.Numerics.Vector2(centerWidth, mapHeight),
            ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var drawList = _mapRenderer.Draw();
        _selectionRenderer.DrawOverlays(drawList);
        _entityRenderer.DrawOverlays(drawList);

        if (_triggerPanel != null)
            DrawTriggerZones(drawList);

        DrawMapContextMenu?.Invoke();

        ImGui.EndChild();

        ImGui.BeginChild("LogPanel", new System.Numerics.Vector2(centerWidth, logHeight), ImGuiChildFlags.Borders);
        _logPanel.Draw();
        ImGui.EndChild();

        ImGui.EndGroup();

        ImGui.SameLine();

        // Right panel: Inspector
        ImGui.BeginChild("RightPanel", new System.Numerics.Vector2(panelWidth, avail.Y), ImGuiChildFlags.Borders);
        _inspector?.Draw();
        ImGui.EndChild();
    }

    // ─── Plugin Mode (full-width) ────────────────────────────────

    private void DrawPluginMode()
    {
        var avail = ImGui.GetContentRegionAvail();
        float logHeight = 150f;

        // Plugin view takes the full width, with log below
        float contentHeight = avail.Y - logHeight - ImGui.GetStyle().ItemSpacing.Y;

        _pluginView?.Draw(avail.X, avail.Y, logHeight);

        // Log panel
        ImGui.BeginChild("PluginLogPanel", new System.Numerics.Vector2(avail.X, logHeight), ImGuiChildFlags.Borders);
        _logPanel.Draw();
        ImGui.EndChild();
    }

    private void DrawAmbienceMode()
    {
        var avail = ImGui.GetContentRegionAvail();
        float logHeight = 150f;
        float contentHeight = avail.Y - logHeight - ImGui.GetStyle().ItemSpacing.Y;

        _ambienceBrowser?.Draw(avail.X, contentHeight);

        ImGui.BeginChild("AmbienceLogPanel", new System.Numerics.Vector2(avail.X, logHeight), ImGuiChildFlags.Borders);
        _logPanel.Draw();
        ImGui.EndChild();
    }

    private void DrawAdventureMode()
    {
        var avail = ImGui.GetContentRegionAvail();
        float logHeight = 150f;

        _adventureView?.Draw(avail.X, avail.Y, logHeight);

        ImGui.BeginChild("AdventureLogPanel", new System.Numerics.Vector2(avail.X, logHeight), ImGuiChildFlags.Borders);
        _logPanel.Draw();
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
