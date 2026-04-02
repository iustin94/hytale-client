using Hexa.NET.ImGui;
using HytaleAdmin.Core;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;

namespace HytaleAdmin.UI;

public class InspectorPanel
{
    private readonly ServiceContainer _services;

    private static readonly System.Numerics.Vector4 AccentColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 HealthColor = new(0.91f, 0.27f, 0.38f, 1f);
    private static readonly System.Numerics.Vector4 StaminaColor = new(0.94f, 0.78f, 0.03f, 1f);
    private static readonly System.Numerics.Vector4 ManaColor = new(0.23f, 0.53f, 1f, 1f);
    private static readonly System.Numerics.Vector4 OxygenColor = new(0.31f, 0.80f, 0.77f, 1f);
    private static readonly System.Numerics.Vector4 LabelColor = new(0.63f, 0.63f, 0.71f, 1f);
    private static readonly System.Numerics.Vector4 DimColor = new(0.47f, 0.47f, 0.55f, 1f);
    private static readonly System.Numerics.Vector4 ActionBtnColor = new(0.22f, 0.22f, 0.30f, 1f);
    private static readonly System.Numerics.Vector4 DangerBtnColor = new(0.65f, 0.18f, 0.25f, 1f);
    private static readonly System.Numerics.Vector4 SuccessBtnColor = new(0.18f, 0.55f, 0.34f, 1f);

    // Teleport input state
    private string _teleX = "", _teleY = "", _teleZ = "";
    private string _messageText = "";
    private string _statValue = "20";
    private int _selectedStat;
    private int _selectedAction;

    private static readonly string[] StatNames = ["health", "stamina", "mana", "oxygen"];
    private static readonly string[] ActionNames = ["set", "add", "subtract", "maximize", "reset"];

    public InspectorPanel(ServiceContainer services)
    {
        _services = services;
    }

    public void Draw()
    {
        ImGui.TextColored(AccentColor, "Inspector");
        ImGui.Separator();

        if (_services.Selection.SelectedPlayer != null)
            DrawPlayerView(_services.Selection.SelectedPlayer);
        else if (_services.Selection.SelectedEntity != null)
            DrawEntityView(_services.Selection.SelectedEntity);
        else if (_services.Selection.SelectedZone != null)
            DrawZoneView(_services.Selection.SelectedZone);
        else
            ImGui.TextColored(DimColor, "Click an entity, player,\nor sound zone on the map");
    }

    // ─── Player ───────────────────────────────────────────────────

    private void DrawPlayerView(PlayerDto player)
    {
        ImGui.Text(player.Name ?? "Unknown Player");
        ImGui.Separator();
        DrawRow("Type", "Player");
        if (player.Uuid != null) DrawRow("UUID", TruncateUuid(player.Uuid));
        DrawRow("Position", $"{player.X:F1}, {player.Y:F1}, {player.Z:F1}");
        if (player.World != null) DrawRow("World", TruncateUuid(player.World));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(LabelColor, "Actions");
        ImGui.Spacing();

        // Teleport
        if (ImGui.CollapsingHeader("Teleport##player"))
        {
            DrawTeleportInputs();
            if (DrawActionButton("Teleport", ActionBtnColor))
            {
                if (TryParseTeleport(out var x, out var y, out var z))
                    _ = _services.ApiClient.TeleportPlayerAsync(player.Uuid!, new TeleportRequest { X = x, Y = y, Z = z });
            }
        }

        // Message
        if (ImGui.CollapsingHeader("Message##player"))
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##msg", ref _messageText, 256);
            if (DrawActionButton("Send", SuccessBtnColor) && !string.IsNullOrEmpty(_messageText))
                _ = _services.ApiClient.SendMessageAsync(player.Uuid!, _messageText);
        }

        // Stats
        if (ImGui.CollapsingHeader("Modify Stat##player"))
        {
            DrawStatModifier();
            if (DrawActionButton("Apply", ActionBtnColor))
            {
                if (float.TryParse(_statValue, out var val))
                    _ = _services.ApiClient.ModifyPlayerStatAsync(player.Uuid!, new StatModifyRequest
                    {
                        Stat = StatNames[_selectedStat],
                        Action = ActionNames[_selectedAction],
                        Value = val
                    });
            }
        }

        ImGui.Spacing();

        // Kick
        if (DrawActionButton("Kick Player", DangerBtnColor))
            _ = _services.ApiClient.KickPlayerAsync(player.Uuid!);
    }

    // ─── Entity ───────────────────────────────────────────────────

    private void DrawEntityView(EntityDto entity)
    {
        ImGui.Text(entity.Name ?? entity.Type ?? "Unknown");
        ImGui.Separator();
        if (entity.Type != null) DrawRow("Type", entity.Type);
        if (entity.Role != null) DrawRow("Role", entity.Role);
        if (entity.Uuid != null) DrawRow("UUID", TruncateUuid(entity.Uuid));
        DrawRow("Position", $"{entity.X:F1}, {entity.Y:F1}, {entity.Z:F1}");

        if (entity.MaxHealth.HasValue)
            DrawRow("Max Health", $"{entity.MaxHealth.Value:F0}");

        if (entity.Stats != null)
        {
            ImGui.Spacing();
            if (entity.Stats.Health != null)
                DrawStatBar("Health", entity.Stats.Health.Current, entity.Stats.Health.Max, HealthColor);
            if (entity.Stats.Stamina != null)
                DrawStatBar("Stamina", entity.Stats.Stamina.Current, entity.Stats.Stamina.Max, StaminaColor);
            if (entity.Stats.Mana != null)
                DrawStatBar("Mana", entity.Stats.Mana.Current, entity.Stats.Mana.Max, ManaColor);
            if (entity.Stats.Oxygen != null)
                DrawStatBar("Oxygen", entity.Stats.Oxygen.Current, entity.Stats.Oxygen.Max, OxygenColor);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(LabelColor, "Actions");
        ImGui.Spacing();

        // Teleport
        if (ImGui.CollapsingHeader("Teleport##entity"))
        {
            DrawTeleportInputs();
            if (DrawActionButton("Teleport", ActionBtnColor))
            {
                if (TryParseTeleport(out var x, out var y, out var z) && entity.Uuid != null)
                    _ = _services.ApiClient.TeleportEntityAsync(entity.Uuid, new TeleportRequest { X = x, Y = y, Z = z },
                        _services.Config.WorldId);
            }
        }

        // Stats
        if (ImGui.CollapsingHeader("Modify Stat##entity"))
        {
            DrawStatModifier();
            if (DrawActionButton("Apply", ActionBtnColor) && entity.Uuid != null)
            {
                if (float.TryParse(_statValue, out var val))
                    _ = _services.ApiClient.ModifyEntityStatAsync(entity.Uuid, new StatModifyRequest
                    {
                        Stat = StatNames[_selectedStat],
                        Action = ActionNames[_selectedAction],
                        Value = val
                    }, _services.Config.WorldId);
            }
        }

        ImGui.Spacing();

        // Delete
        if (entity.Uuid != null && DrawActionButton("Remove Entity", DangerBtnColor))
        {
            _ = DeleteEntityAsync(entity.Uuid);
        }
    }

    // ─── Sound Zone ───────────────────────────────────────────────

    private void DrawZoneView(SoundZoneDto zone)
    {
        ImGui.Text(zone.Sound ?? "Sound Zone");
        ImGui.Separator();
        DrawRow("Sound", zone.Sound ?? "—");
        DrawRow("Key", zone.Key ?? "—");
        DrawRow("Bounds X", $"{zone.MinX:F1} → {zone.MaxX:F1}");
        DrawRow("Bounds Z", $"{zone.MinZ:F1} → {zone.MaxZ:F1}");
        DrawRow("Center", $"{zone.X:F1}, {zone.Y:F1}, {zone.Z:F1}");
        DrawRow("Interval", $"{zone.Interval}s");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(LabelColor, "Actions");
        ImGui.Spacing();

        if (zone.Key != null && DrawActionButton("Stop Zone", DangerBtnColor))
            _ = StopZoneAsync(zone.Key);
    }

    // ─── Shared Widgets ───────────────────────────────────────────

    private void DrawTeleportInputs()
    {
        float w = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3f;
        ImGui.SetNextItemWidth(w);
        ImGui.InputText("X##tele", ref _teleX, 16);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(w);
        ImGui.InputText("Y##tele", ref _teleY, 16);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(w);
        ImGui.InputText("Z##tele", ref _teleZ, 16);
    }

    private void DrawStatModifier()
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo("##stat", ref _selectedStat, StatNames, StatNames.Length);
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo("##action", ref _selectedAction, ActionNames, ActionNames.Length);
        if (_selectedAction < 3) // set, add, subtract need value
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("Value##stat", ref _statValue, 16);
        }
    }

    private static bool DrawActionButton(string label, System.Numerics.Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color with { W = 0.85f });
        bool clicked = ImGui.Button(label, new System.Numerics.Vector2(-1, 0));
        ImGui.PopStyleColor(2);
        return clicked;
    }

    private static void DrawRow(string label, string value)
    {
        ImGui.TextColored(LabelColor, $"{label}:");
        ImGui.SameLine();
        ImGui.Text(value);
    }

    private static void DrawStatBar(string label, float current, float max, System.Numerics.Vector4 color)
    {
        ImGui.TextColored(LabelColor, label);
        float pct = max > 0 ? Math.Clamp(current / max, 0, 1) : 0;
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
        ImGui.ProgressBar(pct, new System.Numerics.Vector2(-1, 14), $"{current:F0}/{max:F0}");
        ImGui.PopStyleColor();
    }

    private bool TryParseTeleport(out double x, out double y, out double z)
    {
        x = y = z = 0;
        return double.TryParse(_teleX, out x)
            && double.TryParse(_teleY, out y)
            && double.TryParse(_teleZ, out z);
    }

    private static string TruncateUuid(string uuid)
    {
        return uuid.Length > 12 ? uuid[..8] + "..." : uuid;
    }

    private async Task DeleteEntityAsync(string uuid)
    {
        await _services.ApiClient.DeleteEntityAsync(uuid, _services.Config.WorldId);
        _services.Selection.ClearMapSelection();
        await _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
    }

    private async Task StopZoneAsync(string key)
    {
        await _services.ApiClient.StopAmbientAsync(key);
        _services.Selection.ClearMapSelection();
        await _services.EntityData.PollAsync(_services.ApiClient, _services.Config);
    }
}
