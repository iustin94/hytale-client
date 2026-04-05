using System.Text.Json;
using HytaleAdmin.Models.Api;
using Xunit;

namespace HytaleAdmin.Tests;

/// <summary>
/// Tests that the DTO classes correctly deserialize JSON matching
/// the schema format produced by the Java CitizenSchemaProvider.
/// This validates the contract between server and client.
/// </summary>
public class SchemaDeserializationTests
{
    /// <summary>
    /// Full round-trip test with a realistic schema matching what
    /// CitizenSchemaProvider.buildSchema() produces.
    /// </summary>
    [Fact]
    public void PluginSchemaDto_DeserializesFullSchema()
    {
        var json = """
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
                        { "id": "modelId", "label": "Model ID", "type": "string" },
                        { "id": "attitude", "label": "Attitude", "type": "enum",
                          "enumValues": ["PASSIVE", "NEUTRAL", "AGGRESSIVE"] },
                        { "id": "scale", "label": "Scale", "type": "float", "min": 0.1, "max": 10.0 },
                        { "id": "position", "label": "Position", "type": "vector3d", "readOnly": true }
                    ]
                },
                {
                    "id": "appearance",
                    "label": "Appearance",
                    "order": 1,
                    "fields": [
                        { "id": "isPlayerModel", "label": "Player Model", "type": "bool" },
                        { "id": "hideNametag", "label": "Hide Nametag", "type": "bool" }
                    ]
                },
                {
                    "id": "health",
                    "label": "Health & Damage",
                    "order": 7,
                    "fields": [
                        { "id": "takesDamage", "label": "Takes Damage", "type": "bool" },
                        { "id": "healthAmount", "label": "Health Amount", "type": "float",
                          "min": 0.0, "max": 10000.0 }
                    ]
                }
            ]
        }
        """;

        var schema = JsonSerializer.Deserialize<PluginSchemaDto>(json);

        Assert.NotNull(schema);
        Assert.Equal("hycitizens", schema.PluginId);
        Assert.Equal(3, schema.Groups.Length);

        // Groups are ordered correctly
        Assert.Equal(0, schema.Groups[0].Order);
        Assert.Equal(1, schema.Groups[1].Order);
        Assert.Equal(7, schema.Groups[2].Order);

        // Field types are preserved
        Assert.Equal("string", schema.Groups[0].Fields[0].Type);
        Assert.Equal("enum", schema.Groups[0].Fields[2].Type);
        Assert.Equal("float", schema.Groups[0].Fields[3].Type);
        Assert.Equal("vector3d", schema.Groups[0].Fields[4].Type);
        Assert.Equal("bool", schema.Groups[1].Fields[0].Type);
    }

    [Fact]
    public void FieldDefinitionDto_HandlesNullOptionalFields()
    {
        var json = """{ "id": "name", "label": "Name", "type": "string" }""";
        var field = JsonSerializer.Deserialize<FieldDefinitionDto>(json);

        Assert.NotNull(field);
        Assert.Null(field.Min);
        Assert.Null(field.Max);
        Assert.Null(field.EnumValues);
        Assert.False(field.ReadOnly);
    }

    [Fact]
    public void FieldDefinitionDto_DeserializesAllConstraints()
    {
        var json = """
        {
            "id": "combat.blockProbability",
            "label": "Block Probability",
            "type": "int",
            "min": 0,
            "max": 100,
            "readOnly": false
        }
        """;
        var field = JsonSerializer.Deserialize<FieldDefinitionDto>(json);

        Assert.NotNull(field);
        Assert.Equal("int", field.Type);
        Assert.Equal(0f, field.Min);
        Assert.Equal(100f, field.Max);
    }

    [Fact]
    public void PluginEntityValuesDto_DeserializesMixedValueTypes()
    {
        var json = """
        {
            "entityId": "abc-123",
            "entityLabel": "Guard Bob",
            "values": {
                "name": "Guard Bob",
                "scale": 1.5,
                "takesDamage": true,
                "healthAmount": 100.0,
                "combat.combatStrafeWeight": 10,
                "position": { "x": 10.5, "y": 64.0, "z": -20.3 },
                "movement.type": "WANDER"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<PluginEntityValuesDto>(json);

        Assert.NotNull(dto);
        Assert.Equal("abc-123", dto.EntityId);
        Assert.Equal(7, dto.Values.Count);

        // String
        Assert.Equal(JsonValueKind.String, dto.Values["name"].ValueKind);
        Assert.Equal("Guard Bob", dto.Values["name"].GetString());

        // Float
        Assert.Equal(JsonValueKind.Number, dto.Values["scale"].ValueKind);

        // Bool
        Assert.Equal(JsonValueKind.True, dto.Values["takesDamage"].ValueKind);

        // Integer
        Assert.Equal(10, dto.Values["combat.combatStrafeWeight"].GetInt32());

        // Nested object (vector)
        var pos = dto.Values["position"];
        Assert.Equal(JsonValueKind.Object, pos.ValueKind);
        Assert.Equal(10.5, pos.GetProperty("x").GetDouble(), 0.01);
        Assert.Equal(64.0, pos.GetProperty("y").GetDouble(), 0.01);
        Assert.Equal(-20.3, pos.GetProperty("z").GetDouble(), 0.01);

        // Dot-path key with enum value
        Assert.Equal("WANDER", dto.Values["movement.type"].GetString());
    }

    [Fact]
    public void PluginEntityListResponse_DeserializesPaginated()
    {
        var json = """
        {
            "total": 25,
            "offset": 10,
            "limit": 5,
            "data": [
                { "id": "a", "label": "Alpha", "group": "G1", "x": 1.0, "y": 2.0, "z": 3.0 },
                { "id": "b", "label": "Beta", "x": 4.0, "y": 5.0, "z": 6.0 }
            ]
        }
        """;

        var resp = JsonSerializer.Deserialize<PluginEntityListResponse>(json);

        Assert.NotNull(resp);
        Assert.Equal(25, resp.Total);
        Assert.Equal(10, resp.Offset);
        Assert.Equal(5, resp.Limit);
        Assert.Equal(2, resp.Data.Length);

        Assert.Equal("Alpha", resp.Data[0].Label);
        Assert.Equal("G1", resp.Data[0].Group);
        Assert.Null(resp.Data[1].Group); // optional field
    }

    [Fact]
    public void PluginSummaryDto_DeserializesCorrectly()
    {
        var json = """
        {
            "pluginId": "hycitizens",
            "pluginName": "HyCitizens",
            "version": "1.6.1",
            "entityType": "citizen",
            "entityLabel": "Citizens",
            "available": true
        }
        """;

        var dto = JsonSerializer.Deserialize<PluginSummaryDto>(json);

        Assert.NotNull(dto);
        Assert.Equal("hycitizens", dto.PluginId);
        Assert.Equal("citizen", dto.EntityType);
        Assert.True(dto.Available);
    }

    // ─── Action Schema Tests ──────────────────────────────────────

    [Fact]
    public void PluginSchemaDto_DeserializesActions()
    {
        var json = """
        {
            "pluginId": "hycitizens",
            "pluginName": "HyCitizens",
            "version": "1.6.1",
            "entityType": "citizen",
            "entityLabel": "Citizens",
            "groups": [],
            "actions": [
                {
                    "id": "create",
                    "label": "New Citizen",
                    "description": "Create a new NPC",
                    "requiresEntity": false,
                    "groups": [
                        {
                            "id": "identity", "label": "Identity", "order": 0,
                            "fields": [
                                { "id": "name", "label": "Name", "type": "string", "required": true },
                                { "id": "modelId", "label": "Model ID", "type": "string", "required": true }
                            ]
                        },
                        {
                            "id": "location", "label": "Spawn Location", "order": 1,
                            "fields": [
                                { "id": "x", "label": "X", "type": "float", "required": true },
                                { "id": "y", "label": "Y", "type": "float", "required": true },
                                { "id": "z", "label": "Z", "type": "float", "required": true }
                            ]
                        }
                    ]
                },
                {
                    "id": "delete",
                    "label": "Delete Citizen",
                    "requiresEntity": true,
                    "confirm": "Are you sure?",
                    "groups": []
                }
            ]
        }
        """;

        var schema = JsonSerializer.Deserialize<PluginSchemaDto>(json);

        Assert.NotNull(schema);
        Assert.Equal(2, schema.Actions.Length);

        // Create action (entity-less, has form groups)
        var create = schema.Actions[0];
        Assert.Equal("create", create.Id);
        Assert.Equal("New Citizen", create.Label);
        Assert.Equal("Create a new NPC", create.Description);
        Assert.False(create.RequiresEntity);
        Assert.Null(create.Confirm);
        Assert.Equal(2, create.Groups.Length);
        Assert.Equal("identity", create.Groups[0].Id);
        Assert.Equal(2, create.Groups[0].Fields.Length);
        Assert.True(create.Groups[0].Fields[0].Required);

        // Delete action (entity-bound, no form, has confirm)
        var delete = schema.Actions[1];
        Assert.Equal("delete", delete.Id);
        Assert.True(delete.RequiresEntity);
        Assert.Equal("Are you sure?", delete.Confirm);
        Assert.Empty(delete.Groups);
    }

    [Fact]
    public void ActionResultDto_DeserializesSuccess()
    {
        var json = """{"success":true,"entityId":"new-123","message":"Citizen created"}""";
        var result = JsonSerializer.Deserialize<ActionResultDto>(json);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("new-123", result.EntityId);
        Assert.Equal("Citizen created", result.Message);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void ActionResultDto_DeserializesErrors()
    {
        var json = """{"success":false,"errors":["Name is required","Position X is required"]}""";
        var result = JsonSerializer.Deserialize<ActionResultDto>(json);

        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.Errors);
        Assert.Equal(2, result.Errors.Length);
    }

    [Fact]
    public void PluginSchemaDto_BackwardCompatible_NoActions()
    {
        var json = """
        {
            "pluginId": "old",
            "pluginName": "Old Plugin",
            "version": "1.0",
            "entityType": "thing",
            "entityLabel": "Things",
            "groups": []
        }
        """;

        var schema = JsonSerializer.Deserialize<PluginSchemaDto>(json);
        Assert.NotNull(schema);
        Assert.Empty(schema.Actions);
    }
}
