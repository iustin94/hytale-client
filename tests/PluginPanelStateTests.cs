using System.Net;
using HytaleAdmin.Models.Api;
using HytaleAdmin.Services;
using Xunit;

namespace HytaleAdmin.Tests;

/// <summary>
/// Tests for the PluginPanel's state machine logic — loading states,
/// error recovery, and state transitions — extracted from the UI layer.
///
/// These tests exercise PluginPanelState directly, which is the
/// extracted state logic from PluginPanel (no ImGui dependency).
/// </summary>
public class PluginPanelStateTests
{
    // ─── Plugin Loading ───────────────────────────────────────────

    [Fact]
    public async Task LoadPlugins_SetsPluginsOnSuccess()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins", """
                [{ "pluginId": "hycitizens", "pluginName": "HyCitizens", "version": "1.6.1",
                   "entityType": "citizen", "entityLabel": "Citizens", "available": true }]
            """);
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadPluginsAsync();

        Assert.False(state.PluginsLoading);
        Assert.NotNull(state.Plugins);
        Assert.Single(state.Plugins);
        Assert.Equal("hycitizens", state.Plugins[0].PluginId);
        Assert.Equal(0, state.SelectedPluginIdx); // auto-selected single plugin
    }

    [Fact]
    public async Task LoadPlugins_ClearsLoadingOnNull()
    {
        var handler = new MockHttpHandler(); // no mock -> 404 -> null
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadPluginsAsync();

        Assert.False(state.PluginsLoading);
        Assert.Null(state.Plugins);
    }

    [Fact]
    public async Task LoadPlugins_ClearsLoadingOnException()
    {
        var handler = new ThrowingHttpHandler();
        using var client = new HytaleApiClient(new HttpClient(handler))
            { BaseUrl = "http://test:8080" };
        var state = new PluginPanelState(client);

        await state.LoadPluginsAsync();

        // Must NOT stay stuck in loading state
        Assert.False(state.PluginsLoading);
        Assert.Null(state.Plugins);
    }

    [Fact]
    public async Task LoadPlugins_DoesNotAutoSelectWhenMultiple()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins", """
                [
                    { "pluginId": "a", "pluginName": "A", "version": "1", "entityType": "x",
                      "entityLabel": "Xs", "available": true },
                    { "pluginId": "b", "pluginName": "B", "version": "1", "entityType": "y",
                      "entityLabel": "Ys", "available": true }
                ]
            """);
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadPluginsAsync();

        Assert.Equal(2, state.Plugins!.Length);
        Assert.Equal(-1, state.SelectedPluginIdx); // no auto-select
    }

    // ─── Schema Loading ───────────────────────────────────────────

    [Fact]
    public async Task LoadSchema_CachesAndResets()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/schema", """
                {
                    "pluginId": "hycitizens", "pluginName": "HyCitizens", "version": "1.6.1",
                    "entityType": "citizen", "entityLabel": "Citizens",
                    "groups": [{ "id": "identity", "label": "Identity", "order": 0, "fields": [] }]
                }
            """);
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadSchemaAsync("hycitizens");

        Assert.False(state.SchemaLoading);
        Assert.NotNull(state.Schema);
        Assert.Equal("hycitizens", state.CachedSchemaPluginId);
        Assert.Null(state.Entities); // cleared on schema load
        Assert.Equal(-1, state.SelectedEntityIdx);
    }

    [Fact]
    public async Task LoadSchema_ClearsLoadingOnFailure()
    {
        var handler = new MockHttpHandler(); // 404
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadSchemaAsync("nonexistent");

        Assert.False(state.SchemaLoading);
        Assert.Null(state.Schema);
    }

    // ─── Entity Loading ───────────────────────────────────────────

    [Fact]
    public async Task LoadEntities_PopulatesEntityList()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/entities", """
                {
                    "total": 2, "offset": 0, "limit": 500,
                    "data": [
                        { "id": "a", "label": "Alpha", "group": "G", "x": 1, "y": 2, "z": 3 },
                        { "id": "b", "label": "Beta", "x": 4, "y": 5, "z": 6 }
                    ]
                }
            """);
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadEntitiesAsync("hycitizens");

        Assert.False(state.EntitiesLoading);
        Assert.NotNull(state.Entities);
        Assert.Equal(2, state.Entities.Length);
    }

    [Fact]
    public async Task LoadEntities_ClearsLoadingOnFailure()
    {
        var handler = new MockHttpHandler(); // 404
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadEntitiesAsync("bad");

        Assert.False(state.EntitiesLoading);
        Assert.Null(state.Entities);
    }

    // ─── Entity Values Loading ────────────────────────────────────

    [Fact]
    public async Task LoadValues_PopulatesCurrentAndEditedValues()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/entities/abc", """
                {
                    "entityId": "abc", "entityLabel": "Test",
                    "values": { "name": "Test", "scale": 1.5, "takesDamage": true }
                }
            """);
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadEntityValuesAsync("hycitizens", "abc");

        Assert.False(state.ValuesLoading);
        Assert.Equal("abc", state.LoadedEntityId);
        Assert.Equal("Test", state.CurrentValues["name"]);
        Assert.Equal("1.5", state.CurrentValues["scale"]);
        Assert.Equal("true", state.CurrentValues["takesDamage"]);

        // Edited starts as a copy
        Assert.Equal(state.CurrentValues.Count, state.EditedValues.Count);
        Assert.Empty(state.DirtyFields);
    }

    [Fact]
    public async Task LoadValues_ClearsLoadingOnFailure()
    {
        var handler = new MockHttpHandler(); // 404
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        await state.LoadEntityValuesAsync("hycitizens", "nonexistent");

        Assert.False(state.ValuesLoading);
    }

    [Fact]
    public async Task LoadValues_ClearsPreviousState()
    {
        var handler = new MockHttpHandler()
            .Respond("/api/plugins/hycitizens/entities/abc", """
                { "entityId": "abc", "entityLabel": "Test", "values": { "name": "Test" } }
            """);
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        // Simulate prior dirty state
        state.DirtyFields.Add("oldField");
        state.TextBuffers["oldField"] = "oldValue";

        await state.LoadEntityValuesAsync("hycitizens", "abc");

        Assert.Empty(state.DirtyFields);
        Assert.Empty(state.TextBuffers);
        Assert.Null(state.SaveStatus);
    }

    // ─── Dirty Tracking ───────────────────────────────────────────

    [Fact]
    public void UpdateDirty_MarksFieldAsDirty()
    {
        var state = new PluginPanelState(null!);
        state.CurrentValues["name"] = "Original";
        state.EditedValues["name"] = "Changed";

        state.UpdateDirty("name");

        Assert.Contains("name", state.DirtyFields);
    }

    [Fact]
    public void UpdateDirty_ClearsDirtyWhenRevertedToOriginal()
    {
        var state = new PluginPanelState(null!);
        state.CurrentValues["name"] = "Original";
        state.EditedValues["name"] = "Changed";
        state.UpdateDirty("name");
        Assert.Contains("name", state.DirtyFields);

        // Revert
        state.EditedValues["name"] = "Original";
        state.UpdateDirty("name");

        Assert.DoesNotContain("name", state.DirtyFields);
    }

    [Fact]
    public void UpdateDirty_TracksMultipleFields()
    {
        var state = new PluginPanelState(null!);
        state.CurrentValues["name"] = "A";
        state.CurrentValues["scale"] = "1.0";
        state.EditedValues["name"] = "B";
        state.EditedValues["scale"] = "2.0";

        state.UpdateDirty("name");
        state.UpdateDirty("scale");

        Assert.Equal(2, state.DirtyFields.Count);

        // Revert one
        state.EditedValues["name"] = "A";
        state.UpdateDirty("name");
        Assert.Single(state.DirtyFields);
        Assert.Contains("scale", state.DirtyFields);
    }

    // ─── Save ─────────────────────────────────────────────────────

    [Fact]
    public async Task Save_SendsDirtyFieldsOnly()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post, """{"success":true}""");
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        state.LoadedEntityId = "abc";
        state.CurrentValues["name"] = "Old";
        state.CurrentValues["scale"] = "1.0";
        state.EditedValues["name"] = "New";
        state.EditedValues["scale"] = "1.0"; // not dirty
        state.DirtyFields.Add("name");

        await state.SaveChangesAsync("hycitizens");

        Assert.False(state.Saving);
        Assert.Equal("Saved successfully", state.SaveStatus);
        Assert.Empty(state.DirtyFields);
        // CurrentValues updated to match
        Assert.Equal("New", state.CurrentValues["name"]);
    }

    [Fact]
    public async Task Save_SetsErrorStatusOnFailure()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post,
                """{"success":false,"error":"Unknown field"}""");
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        state.LoadedEntityId = "abc";
        state.EditedValues["bad"] = "val";
        state.DirtyFields.Add("bad");

        await state.SaveChangesAsync("hycitizens");

        Assert.False(state.Saving);
        Assert.StartsWith("Error", state.SaveStatus);
    }

    // ─── Actions ──────────────────────────────────────────────────

    [Fact]
    public void BeginAction_SwitchesToActionMode()
    {
        var state = new PluginPanelState(null!);
        var action = new PluginActionDto
        {
            Id = "create",
            Label = "New Citizen",
            RequiresEntity = false,
            Groups = [new FieldGroupDto
            {
                Id = "identity", Label = "Identity", Order = 0,
                Fields = [
                    new FieldDefinitionDto { Id = "name", Label = "Name", Type = "string" },
                    new FieldDefinitionDto { Id = "x", Label = "X", Type = "float" }
                ]
            }]
        };

        state.BeginAction(action);

        Assert.Equal(FormMode.Action, state.Mode);
        Assert.Equal("create", state.ActiveActionId);
        Assert.Same(action, state.ActiveAction);
        Assert.Equal(2, state.ActionFormValues.Count);
        Assert.Equal("", state.ActionFormValues["name"]);
        Assert.Equal("", state.ActionFormValues["x"]);
        Assert.Null(state.ActionResult);
    }

    [Fact]
    public async Task ExecuteAction_CreateReturnsEntityId()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post &&
                            req.RequestUri!.ToString().Contains("/actions/create"),
                """{"success":true,"entityId":"new-123","message":"Created"}""")
            .Respond(req => req.Method == HttpMethod.Get &&
                            req.RequestUri!.ToString().Contains("/entities"),
                """{"total":1,"offset":0,"limit":500,"data":[{"id":"new-123","label":"Test","x":0,"y":64,"z":0}]}""");
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        state.ActiveActionId = "create";
        state.ActiveAction = new PluginActionDto { Id = "create", RequiresEntity = false };
        state.ActionFormValues = new() { ["name"] = "Test" };
        state.Mode = FormMode.Action;

        await state.ExecuteActionAsync("hycitizens");

        Assert.False(state.ActionExecuting);
        Assert.NotNull(state.ActionResult);
        Assert.True(state.ActionResult.Success);
        Assert.Equal("new-123", state.ActionResult.EntityId);
    }

    [Fact]
    public async Task ExecuteAction_DeleteClearsSelection()
    {
        var handler = new MockHttpHandler()
            .Respond(req => req.Method == HttpMethod.Post &&
                            req.RequestUri!.ToString().Contains("/actions/delete"),
                """{"success":true,"message":"Deleted"}""")
            .Respond("/api/plugins/hycitizens/entities", """
                {"total":0,"offset":0,"limit":500,"data":[]}
            """);
        using var client = CreateClient(handler);
        var state = new PluginPanelState(client);

        state.SelectedEntityIdx = 0;
        state.LoadedEntityId = "abc";
        state.ActiveActionId = "delete";
        state.ActiveAction = new PluginActionDto { Id = "delete", RequiresEntity = true };
        state.ActionFormValues = new();

        await state.ExecuteActionAsync("hycitizens", "abc");

        Assert.True(state.ActionResult!.Success);
        Assert.Equal(-1, state.SelectedEntityIdx);
        Assert.Null(state.LoadedEntityId);
        Assert.Equal(FormMode.None, state.Mode);
    }

    [Fact]
    public void CancelAction_ReturnsToEditMode()
    {
        var state = new PluginPanelState(null!);
        state.LoadedEntityId = "abc";
        state.Mode = FormMode.Action;
        state.ActiveActionId = "create";

        state.CancelAction();

        Assert.Null(state.ActiveActionId);
        Assert.Equal(FormMode.EditEntity, state.Mode);
    }

    [Fact]
    public void CancelAction_ReturnsToNoneWithoutEntity()
    {
        var state = new PluginPanelState(null!);
        state.LoadedEntityId = null;
        state.Mode = FormMode.Action;
        state.ActiveActionId = "create";

        state.CancelAction();

        Assert.Equal(FormMode.None, state.Mode);
    }

    // ─── Helpers ──────────────────────────────────────────────────

    private static HytaleApiClient CreateClient(MockHttpHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://test:8080") };
        return new HytaleApiClient(http) { BaseUrl = "http://test:8080" };
    }
}

/// <summary>
/// HttpMessageHandler that always throws, simulating a network failure.
/// </summary>
public class ThrowingHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection refused");
    }
}
