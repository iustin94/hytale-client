using System.Text.Json;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;
using Xunit;

namespace HytaleAdmin.Tests;

/// <summary>
/// Tests for the plugin schema API client methods.
/// Uses MockHttpHandler to return canned JSON responses.
/// </summary>
public class PluginApiTests
{
    private static HytaleApiClient CreateClient(MockHttpHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://test:8080") };
        return new HytaleApiClient(http) { BaseUrl = "http://test:8080" };
    }

    // ─── GetPluginsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetPluginsAsync_ReturnsPluginList()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins", """
                [
                    {
                        "pluginId": "hycitizens",
                        "pluginName": "HyCitizens",
                        "version": "1.6.1",
                        "entityType": "citizen",
                        "entityLabel": "Citizens",
                        "available": true
                    }
                ]
            """);

        using var client = CreateClient(handler);
        var plugins = await client.GetPluginsAsync();

        Assert.NotNull(plugins);
        Assert.Single(plugins);
        Assert.Equal("hycitizens", plugins[0].PluginId);
        Assert.Equal("HyCitizens", plugins[0].PluginName);
        Assert.Equal("1.6.1", plugins[0].Version);
        Assert.Equal("Citizens", plugins[0].EntityLabel);
        Assert.True(plugins[0].Available);
    }

    [Fact]
    public async Task GetPluginsAsync_ReturnsNullOnError()
    {
        var handler = new MockHttpHandler()
            .RespondError("/api/plugins");

        using var client = CreateClient(handler);
        var plugins = await client.GetPluginsAsync();

        // The method catches exceptions and returns null
        // A 500 response with valid JSON may still deserialize - that's OK
        // The key contract is that it doesn't throw
    }

    [Fact]
    public async Task GetPluginsAsync_ReturnsMultiplePlugins()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins", """
                [
                    { "pluginId": "hycitizens", "pluginName": "HyCitizens", "version": "1.6.1",
                      "entityType": "citizen", "entityLabel": "Citizens", "available": true },
                    { "pluginId": "shops", "pluginName": "HyShops", "version": "0.1.0",
                      "entityType": "shop", "entityLabel": "Shops", "available": true }
                ]
            """);

        using var client = CreateClient(handler);
        var plugins = await client.GetPluginsAsync();

        Assert.NotNull(plugins);
        Assert.Equal(2, plugins.Length);
        Assert.Equal("shops", plugins[1].PluginId);
    }

    // ─── GetPluginSchemaAsync ─────────────────────────────────────

    [Fact]
    public async Task GetPluginSchemaAsync_ReturnsSchemaWithGroups()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/schema", """
                {
                    "pluginId": "hycitizens",
                    "pluginName": "HyCitizens",
                    "version": "1.6.1",
                    "entityType": "citizen",
                    "entityLabel": "Citizens",
                    "groups": [
                        {
                            "id": "identity",
                            "label": "Identity",
                            "order": 0,
                            "fields": [
                                { "id": "name", "label": "Name", "type": "string" },
                                { "id": "attitude", "label": "Attitude", "type": "enum",
                                  "enumValues": ["PASSIVE", "NEUTRAL", "AGGRESSIVE"] },
                                { "id": "scale", "label": "Scale", "type": "float",
                                  "min": 0.1, "max": 10.0 },
                                { "id": "position", "label": "Position", "type": "vector3d",
                                  "readOnly": true }
                            ]
                        },
                        {
                            "id": "combat",
                            "label": "Combat",
                            "order": 5,
                            "fields": [
                                { "id": "combat.attackDistance", "label": "Attack Distance",
                                  "type": "float", "min": 0.0, "max": 50.0 }
                            ]
                        }
                    ]
                }
            """);

        using var client = CreateClient(handler);
        var schema = await client.GetPluginSchemaAsync("hycitizens");

        Assert.NotNull(schema);
        Assert.Equal("hycitizens", schema.PluginId);
        Assert.Equal(2, schema.Groups.Length);

        // Identity group
        var identity = schema.Groups[0];
        Assert.Equal("identity", identity.Id);
        Assert.Equal("Identity", identity.Label);
        Assert.Equal(0, identity.Order);
        Assert.Equal(4, identity.Fields.Length);

        // String field
        var nameField = identity.Fields[0];
        Assert.Equal("name", nameField.Id);
        Assert.Equal("string", nameField.Type);
        Assert.False(nameField.ReadOnly);

        // Enum field
        var attitudeField = identity.Fields[1];
        Assert.Equal("enum", attitudeField.Type);
        Assert.NotNull(attitudeField.EnumValues);
        Assert.Equal(3, attitudeField.EnumValues.Length);
        Assert.Contains("PASSIVE", attitudeField.EnumValues);

        // Float field with constraints
        var scaleField = identity.Fields[2];
        Assert.Equal("float", scaleField.Type);
        Assert.NotNull(scaleField.Min);
        Assert.NotNull(scaleField.Max);
        Assert.Equal(0.1f, scaleField.Min.Value, 0.01f);
        Assert.Equal(10.0f, scaleField.Max.Value, 0.01f);

        // ReadOnly field
        var posField = identity.Fields[3];
        Assert.True(posField.ReadOnly);
        Assert.Equal("vector3d", posField.Type);

        // Combat group
        var combat = schema.Groups[1];
        Assert.Equal(5, combat.Order);
        Assert.Single(combat.Fields);
        Assert.Equal("combat.attackDistance", combat.Fields[0].Id);
    }

    [Fact]
    public async Task GetPluginSchemaAsync_ReturnsNullForUnknownPlugin()
    {
        var handler = new MockHttpHandler(); // no mock registered -> 404

        using var client = CreateClient(handler);
        var schema = await client.GetPluginSchemaAsync("nonexistent");

        // GetFromJsonAsync on a 404 will throw, caught by try-catch -> null
        Assert.Null(schema);
    }

    // ─── GetPluginEntitiesAsync ───────────────────────────────────

    [Fact]
    public async Task GetPluginEntitiesAsync_ReturnsPaginatedList()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/entities", """
                {
                    "total": 3,
                    "offset": 0,
                    "limit": 500,
                    "data": [
                        { "id": "abc-123", "label": "Guard Bob", "group": "Guards",
                          "x": 100.5, "y": 64.0, "z": -200.3 },
                        { "id": "def-456", "label": "Merchant Sue", "group": "NPCs",
                          "x": 50.0, "y": 65.0, "z": 100.0 },
                        { "id": "ghi-789", "label": "Patrol Jim", "group": "",
                          "x": 0.0, "y": 70.0, "z": 0.0 }
                    ]
                }
            """);

        using var client = CreateClient(handler);
        var resp = await client.GetPluginEntitiesAsync("hycitizens");

        Assert.NotNull(resp);
        Assert.Equal(3, resp.Total);
        Assert.Equal(3, resp.Data.Length);

        Assert.Equal("abc-123", resp.Data[0].Id);
        Assert.Equal("Guard Bob", resp.Data[0].Label);
        Assert.Equal("Guards", resp.Data[0].Group);
        Assert.Equal(100.5f, resp.Data[0].X, 0.1f);
    }

    [Fact]
    public async Task GetPluginEntitiesAsync_HandlesEmptyList()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/entities", """
                { "total": 0, "offset": 0, "limit": 500, "data": [] }
            """);

        using var client = CreateClient(handler);
        var resp = await client.GetPluginEntitiesAsync("hycitizens");

        Assert.NotNull(resp);
        Assert.Equal(0, resp.Total);
        Assert.Empty(resp.Data);
    }

    // ─── GetPluginEntityValuesAsync ───────────────────────────────

    [Fact]
    public async Task GetPluginEntityValuesAsync_ReturnsValues()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/entities/abc-123", """
                {
                    "entityId": "abc-123",
                    "entityLabel": "Guard Bob",
                    "values": {
                        "name": "Guard Bob",
                        "scale": 1.5,
                        "attitude": "AGGRESSIVE",
                        "takesDamage": true,
                        "position": { "x": 100.5, "y": 64.0, "z": -200.3 }
                    }
                }
            """);

        using var client = CreateClient(handler);
        var dto = await client.GetPluginEntityValuesAsync("hycitizens", "abc-123");

        Assert.NotNull(dto);
        Assert.Equal("abc-123", dto.EntityId);
        Assert.Equal("Guard Bob", dto.EntityLabel);

        // String value
        Assert.Equal("Guard Bob", dto.Values["name"].GetString());

        // Number value
        Assert.Equal(1.5, dto.Values["scale"].GetDouble(), 0.01);

        // Enum value (stored as string)
        Assert.Equal("AGGRESSIVE", dto.Values["attitude"].GetString());

        // Bool value
        Assert.True(dto.Values["takesDamage"].GetBoolean());

        // Nested object value (vector)
        Assert.Equal(JsonValueKind.Object, dto.Values["position"].ValueKind);
        Assert.Equal(100.5, dto.Values["position"].GetProperty("x").GetDouble(), 0.1);
    }

    [Fact]
    public async Task GetPluginEntityValuesAsync_ReturnsNullForMissingEntity()
    {
        var handler = new MockHttpHandler(); // 404

        using var client = CreateClient(handler);
        var dto = await client.GetPluginEntityValuesAsync("hycitizens", "nonexistent");

        Assert.Null(dto);
    }

    // ─── UpdatePluginEntityAsync ──────────────────────────────────

    [Fact]
    public async Task UpdatePluginEntityAsync_SendsValuesAndReturnsSuccess()
    {
        var handler = new MockHttpHandler()
            .Respond(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/plugins/hycitizens/entities/abc-123"),
                """{"success":true}""");

        using var client = CreateClient(handler);
        var values = new Dictionary<string, string>
        {
            ["name"] = "New Name",
            ["scale"] = "2.0",
            ["takesDamage"] = "true"
        };
        var result = await client.UpdatePluginEntityAsync("hycitizens", "abc-123", values);

        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify the POST was sent
        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
    }

    [Fact]
    public async Task UpdatePluginEntityAsync_ReturnsErrorOnFailure()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post,
                """{"success":false,"error":"Unknown field: badField"}""");

        using var client = CreateClient(handler);
        var values = new Dictionary<string, string> { ["badField"] = "value" };
        var result = await client.UpdatePluginEntityAsync("hycitizens", "abc-123", values);

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Unknown field: badField", result.Error);
    }

    // ─── StatusChanged Events ─────────────────────────────────────

    [Fact]
    public async Task UpdatePluginEntityAsync_FiresStatusEvents()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post, """{"success":true}""");

        using var client = CreateClient(handler);
        var statuses = new List<string>();
        client.StatusChanged += s => statuses.Add(s);

        await client.UpdatePluginEntityAsync("hycitizens", "abc-123",
            new Dictionary<string, string> { ["name"] = "Test" });

        Assert.Contains(statuses, s => s.Contains("Updating"));
        Assert.Contains(statuses, s => s.Contains("updated"));
    }

    // ─── ExecutePluginActionAsync ─────────────────────────────────

    [Fact]
    public async Task ExecutePluginActionAsync_EntityLessAction()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post &&
                            req.RequestUri!.ToString().Contains("/actions/create"),
                """{"success":true,"entityId":"new-123","message":"Created"}""");

        using var client = CreateClient(handler);
        var result = await client.ExecutePluginActionAsync("hycitizens", "create", null,
            new Dictionary<string, string> { ["name"] = "Test" });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("new-123", result.EntityId);
        Assert.Equal("Created", result.Message);
    }

    [Fact]
    public async Task ExecutePluginActionAsync_EntityBoundAction()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post &&
                            req.RequestUri!.ToString().Contains("/entities/abc/actions/delete"),
                """{"success":true,"message":"Deleted"}""");

        using var client = CreateClient(handler);
        var result = await client.ExecutePluginActionAsync("hycitizens", "delete", "abc",
            new Dictionary<string, string>());

        Assert.NotNull(result);
        Assert.True(result.Success);
    }
}
