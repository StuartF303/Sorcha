// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Service.Services;

namespace Sorcha.Blueprint.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BlueprintToolExecutor"/>.
/// </summary>
public class BlueprintToolExecutorTests
{
    private readonly BlueprintToolExecutor _executor;
    private readonly Mock<ILogger<BlueprintToolExecutor>> _loggerMock;

    public BlueprintToolExecutorTests()
    {
        _loggerMock = new Mock<ILogger<BlueprintToolExecutor>>();
        _executor = new BlueprintToolExecutor(_loggerMock.Object);
    }

    #region GetToolDefinitions Tests

    [Fact]
    public void GetToolDefinitions_ReturnsAllTools()
    {
        // Act
        var tools = _executor.GetToolDefinitions();

        // Assert
        tools.Should().HaveCount(8);
        tools.Select(t => t.Name).Should().BeEquivalentTo(new[]
        {
            "create_blueprint",
            "add_participant",
            "remove_participant",
            "add_action",
            "update_action",
            "set_disclosure",
            "add_routing",
            "validate_blueprint"
        });
    }

    [Fact]
    public void GetToolDefinitions_AllToolsHaveDescriptions()
    {
        // Act
        var tools = _executor.GetToolDefinitions();

        // Assert
        foreach (var tool in tools)
        {
            tool.Description.Should().NotBeNullOrWhiteSpace($"Tool {tool.Name} should have a description");
        }
    }

    [Fact]
    public void GetToolDefinitions_AllToolsHaveInputSchemas()
    {
        // Act
        var tools = _executor.GetToolDefinitions();

        // Assert
        foreach (var tool in tools)
        {
            tool.InputSchema.Should().NotBeNull($"Tool {tool.Name} should have an input schema");
            tool.InputSchema.RootElement.TryGetProperty("type", out var typeProp).Should().BeTrue();
            typeProp.GetString().Should().Be("object");
        }
    }

    #endregion

    #region create_blueprint Tests

    [Fact]
    public async Task ExecuteAsync_CreateBlueprint_CreatesWithTitleAndDescription()
    {
        // Arrange
        var builder = BlueprintBuilder.Create();
        var args = CreateArgs(new { title = "Test Blueprint", description = "A test description" });

        // Act
        var result = await _executor.ExecuteAsync("create_blueprint", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        result.BlueprintChanged.Should().BeTrue();
        var draft = builder.BuildDraft();
        draft.Title.Should().Be("Test Blueprint");
        draft.Description.Should().Be("A test description");
    }

    [Fact]
    public async Task ExecuteAsync_CreateBlueprint_SetsDefaultsForMissingFields()
    {
        // Arrange
        var builder = BlueprintBuilder.Create();
        var args = CreateArgs(new { title = "Minimal", description = "" });

        // Act
        var result = await _executor.ExecuteAsync("create_blueprint", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        var draft = builder.BuildDraft();
        draft.Title.Should().Be("Minimal");
    }

    #endregion

    #region add_participant Tests

    [Fact]
    public async Task ExecuteAsync_AddParticipant_AddsPersonParticipant()
    {
        // Arrange
        var builder = BlueprintBuilder.Create().WithTitle("Test");
        var args = CreateArgs(new { id = "alice", name = "Alice Smith" });

        // Act
        var result = await _executor.ExecuteAsync("add_participant", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        result.BlueprintChanged.Should().BeTrue();
        var draft = builder.BuildDraft();
        draft.Participants.Should().HaveCount(1);
        draft.Participants[0].Id.Should().Be("alice");
        draft.Participants[0].Name.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task ExecuteAsync_AddParticipant_AddsOrganizationWithRole()
    {
        // Arrange
        var builder = BlueprintBuilder.Create().WithTitle("Test");
        var args = CreateArgs(new { id = "acme", name = "ACME Corp", organisation = "ACME Inc", role = "organization" });

        // Act
        var result = await _executor.ExecuteAsync("add_participant", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        var draft = builder.BuildDraft();
        draft.Participants.Should().HaveCount(1);
        draft.Participants[0].Organisation.Should().Be("ACME Inc");
    }

    [Fact]
    public async Task ExecuteAsync_AddParticipant_AddsMultipleParticipants()
    {
        // Arrange
        var builder = BlueprintBuilder.Create().WithTitle("Test");

        // Act
        await _executor.ExecuteAsync("add_participant", CreateArgs(new { id = "alice", name = "Alice" }), builder);
        await _executor.ExecuteAsync("add_participant", CreateArgs(new { id = "bob", name = "Bob" }), builder);

        // Assert
        var draft = builder.BuildDraft();
        draft.Participants.Should().HaveCount(2);
    }

    #endregion

    #region remove_participant Tests

    [Fact]
    public async Task ExecuteAsync_RemoveParticipant_ReturnsNotSupportedForExistingParticipant()
    {
        // Arrange - remove_participant is not fully implemented in the tool executor
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .AddParticipant("alice", p => p.Named("Alice"))
            .AddParticipant("bob", p => p.Named("Bob"));
        var args = CreateArgs(new { id = "alice" });

        // Act
        var result = await _executor.ExecuteAsync("remove_participant", args, builder);

        // Assert - The tool indicates it's not supported
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not yet supported");
    }

    [Fact]
    public async Task ExecuteAsync_RemoveParticipant_ReturnsNotSupported()
    {
        // Arrange
        var builder = BlueprintBuilder.Create().WithTitle("Test");
        var args = CreateArgs(new { id = "nonexistent" });

        // Act
        var result = await _executor.ExecuteAsync("remove_participant", args, builder);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not yet supported");
    }

    #endregion

    #region add_action Tests

    [Fact]
    public async Task ExecuteAsync_AddAction_CreatesBasicAction()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .AddParticipant("alice", p => p.Named("Alice"));
        var args = CreateArgs(new
        {
            id = 1,  // Integer, not string
            title = "Submit Request",
            sender = "alice",
            isStartingAction = true
        });

        // Act
        var result = await _executor.ExecuteAsync("add_action", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        result.BlueprintChanged.Should().BeTrue();
        var draft = builder.BuildDraft();
        draft.Actions.Should().HaveCount(1);
        draft.Actions[0].Title.Should().Be("Submit Request");
        draft.Actions[0].IsStartingAction.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AddAction_CreatesActionWithDataFields()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .AddParticipant("alice", p => p.Named("Alice"));

        var dataFields = new object[]
        {
            new { name = "amount", type = "number", isRequired = true, minimum = 1000, maximum = 50000 },
            new { name = "email", type = "string", isRequired = true, format = "email" },
            new { name = "notes", type = "string", maxLength = 500 }
        };

        var args = CreateArgs(new
        {
            id = 1,  // Integer, not string
            title = "Loan Application",
            sender = "alice",
            dataFields
        });

        // Act
        var result = await _executor.ExecuteAsync("add_action", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        var draft = builder.BuildDraft();
        draft.Actions[0].DataSchemas.Should().NotBeNull();
        draft.Actions[0].DataSchemas.Should().HaveCount(1);

        var schema = draft.Actions[0].DataSchemas!.First();
        schema.RootElement.TryGetProperty("properties", out var props).Should().BeTrue();
        props.TryGetProperty("amount", out _).Should().BeTrue();
        props.TryGetProperty("email", out _).Should().BeTrue();
        props.TryGetProperty("notes", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AddAction_CreatesActionWithEnumField()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .AddParticipant("alice", p => p.Named("Alice"));

        var dataFields = new object[]
        {
            new { name = "status", type = "string", enumValues = new[] { "approved", "rejected", "pending" } }
        };

        var args = CreateArgs(new
        {
            id = 1,  // Integer, not string
            title = "Status Update",
            sender = "alice",
            dataFields
        });

        // Act
        var result = await _executor.ExecuteAsync("add_action", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        var draft = builder.BuildDraft();
        var schema = draft.Actions[0].DataSchemas!.First();
        schema.RootElement.GetProperty("properties").GetProperty("status")
            .TryGetProperty("enum", out var enumProp).Should().BeTrue();
        enumProp.GetArrayLength().Should().Be(3);
    }

    #endregion

    #region update_action Tests

    [Fact]
    public async Task ExecuteAsync_UpdateAction_ReturnsNotSupported()
    {
        // Arrange - update_action is not fully implemented yet
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .AddParticipant("alice", p => p.Named("Alice"))
            .AddAction(1, a => a.WithTitle("Original Title").SentBy("alice"));
        var args = CreateArgs(new { actionId = "1", title = "Updated Title" });

        // Act
        var result = await _executor.ExecuteAsync("update_action", args, builder);

        // Assert - Tool indicates it's not fully supported
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not yet fully supported");
    }

    [Fact]
    public async Task ExecuteAsync_UpdateAction_ReturnsNotSupportedForNonexistent()
    {
        // Arrange
        var builder = BlueprintBuilder.Create().WithTitle("Test");
        var args = CreateArgs(new { actionId = "nonexistent", title = "New Title" });

        // Act
        var result = await _executor.ExecuteAsync("update_action", args, builder);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not yet fully supported");
    }

    #endregion

    #region set_disclosure Tests

    [Fact]
    public async Task ExecuteAsync_SetDisclosure_AddsDisclosureRule()
    {
        // Arrange - use tool to add action first so the action exists
        var builder = BlueprintBuilder.Create();
        await _executor.ExecuteAsync("create_blueprint",
            CreateArgs(new { title = "Test", description = "Test blueprint" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "alice", name = "Alice" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "bob", name = "Bob" }), builder);
        await _executor.ExecuteAsync("add_action",
            CreateArgs(new { id = 1, title = "Submit", sender = "alice" }), builder);

        var args = CreateArgs(new
        {
            actionId = 1,  // Integer, not string
            participantId = "bob",
            fields = new[] { "/amount", "/description" }  // 'fields' not 'disclosedFields'
        });

        // Act
        var result = await _executor.ExecuteAsync("set_disclosure", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        result.BlueprintChanged.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SetDisclosure_ReturnsErrorForInvalidAction()
    {
        // Arrange
        var builder = BlueprintBuilder.Create();
        await _executor.ExecuteAsync("create_blueprint",
            CreateArgs(new { title = "Test", description = "Test blueprint" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "alice", name = "Alice" }), builder);

        var args = CreateArgs(new
        {
            actionId = 999,  // Non-existent action
            participantId = "alice",
            fields = new[] { "/field" }
        });

        // Act
        var result = await _executor.ExecuteAsync("set_disclosure", args, builder);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region add_routing Tests

    [Fact]
    public async Task ExecuteAsync_AddRouting_AddsRoutingRule()
    {
        // Arrange - use tool executor to build blueprint
        var builder = BlueprintBuilder.Create();
        await _executor.ExecuteAsync("create_blueprint",
            CreateArgs(new { title = "Test", description = "Test blueprint" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "alice", name = "Alice" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "bob", name = "Bob" }), builder);
        await _executor.ExecuteAsync("add_action",
            CreateArgs(new { id = 1, title = "Submit", sender = "alice" }), builder);
        await _executor.ExecuteAsync("add_action",
            CreateArgs(new { id = 2, title = "Review", sender = "bob" }), builder);

        // add_routing uses conditions array with field, operator, value, routeTo
        var args = CreateArgs(new
        {
            actionId = 1,
            conditions = new[]
            {
                new { field = "amount", @operator = "greaterThan", value = 1000, routeTo = "bob" }
            }
        });

        // Act
        var result = await _executor.ExecuteAsync("add_routing", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        result.BlueprintChanged.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AddRouting_SupportsEqualsOperator()
    {
        // Arrange
        var builder = BlueprintBuilder.Create();
        await _executor.ExecuteAsync("create_blueprint",
            CreateArgs(new { title = "Test", description = "Test blueprint" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "alice", name = "Alice" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "bob", name = "Bob" }), builder);
        await _executor.ExecuteAsync("add_action",
            CreateArgs(new { id = 1, title = "Submit", sender = "alice" }), builder);
        await _executor.ExecuteAsync("add_action",
            CreateArgs(new { id = 2, title = "Approved", sender = "bob" }), builder);

        var args = CreateArgs(new
        {
            actionId = 1,
            conditions = new[]
            {
                new { field = "status", @operator = "equals", value = "approved", routeTo = "bob" }
            }
        });

        // Act
        var result = await _executor.ExecuteAsync("add_routing", args, builder);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AddRouting_ReturnsErrorForInvalidAction()
    {
        // Arrange
        var builder = BlueprintBuilder.Create();
        await _executor.ExecuteAsync("create_blueprint",
            CreateArgs(new { title = "Test", description = "Test blueprint" }), builder);

        // actionId must be an integer, not a string - use a non-existent integer ID
        var args = CreateArgs(new
        {
            actionId = 999,  // Non-existent action
            conditions = new[]
            {
                new { field = "x", @operator = "equals", value = "y", routeTo = "someone" }
            }
        });

        // Act
        var result = await _executor.ExecuteAsync("add_routing", args, builder);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    #endregion

    #region validate_blueprint Tests

    [Fact]
    public async Task ExecuteAsync_ValidateBlueprint_ReturnsValidForCompleteBlueprint()
    {
        // Arrange - use the tool executor to build a complete blueprint
        var builder = BlueprintBuilder.Create();

        await _executor.ExecuteAsync("create_blueprint",
            CreateArgs(new { title = "Valid Blueprint", description = "A complete blueprint" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "alice", name = "Alice" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "bob", name = "Bob" }), builder);
        await _executor.ExecuteAsync("add_action",
            CreateArgs(new
            {
                id = 1,  // Integer, not string
                title = "Submit",
                description = "Submit data",
                sender = "alice",
                isStartingAction = true
            }), builder);

        var args = CreateArgs(new { });

        // Act
        var result = await _executor.ExecuteAsync("validate_blueprint", args, builder);

        // Assert
        result.Success.Should().BeTrue();
        result.BlueprintChanged.Should().BeFalse();

        var resultJson = result.Result!.RootElement.ToString();
        var output = JsonSerializer.Deserialize<ValidationOutput>(resultJson);
        output!.isValid.Should().BeTrue();
        output.errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ValidateBlueprint_ReturnsErrorsForMissingParticipants()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test blueprint");
        var args = CreateArgs(new { });

        // Act
        var result = await _executor.ExecuteAsync("validate_blueprint", args, builder);

        // Assert
        result.Success.Should().BeTrue(); // Tool execution succeeded
        var resultJson = result.Result!.RootElement.ToString();
        var output = JsonSerializer.Deserialize<ValidationOutput>(resultJson);
        output!.isValid.Should().BeFalse();
        output.errors.Should().Contain(e => e.code == "MIN_PARTICIPANTS");
    }

    [Fact]
    public async Task ExecuteAsync_ValidateBlueprint_ReturnsErrorsForMissingActions()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Test blueprint")
            .AddParticipant("alice", p => p.Named("Alice"))
            .AddParticipant("bob", p => p.Named("Bob"));
        var args = CreateArgs(new { });

        // Act
        var result = await _executor.ExecuteAsync("validate_blueprint", args, builder);

        // Assert
        var resultJson = result.Result!.RootElement.ToString();
        var output = JsonSerializer.Deserialize<ValidationOutput>(resultJson);
        output!.isValid.Should().BeFalse();
        output.errors.Should().Contain(e => e.code == "MIN_ACTIONS");
    }

    [Fact]
    public async Task ExecuteAsync_ValidateBlueprint_ReturnsWarningForNoStartingAction()
    {
        // Arrange - use tool to add action without starting flag
        var builder = BlueprintBuilder.Create();
        await _executor.ExecuteAsync("create_blueprint",
            CreateArgs(new { title = "Test Blueprint", description = "Test blueprint" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "alice", name = "Alice" }), builder);
        await _executor.ExecuteAsync("add_participant",
            CreateArgs(new { id = "bob", name = "Bob" }), builder);
        await _executor.ExecuteAsync("add_action",
            CreateArgs(new
            {
                id = 1,  // Integer, not string
                title = "Action",
                description = "An action",
                sender = "alice",
                isStartingAction = false  // Not marked as starting
            }), builder);

        var args = CreateArgs(new { });

        // Act
        var result = await _executor.ExecuteAsync("validate_blueprint", args, builder);

        // Assert
        var resultJson = result.Result!.RootElement.ToString();
        var output = JsonSerializer.Deserialize<ValidationOutput>(resultJson);
        output!.warnings.Should().Contain(w => w.code == "NO_STARTING_ACTION");
    }

    #endregion

    #region Unknown Tool Tests

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ReturnsFailed()
    {
        // Arrange
        var builder = BlueprintBuilder.Create();
        var args = CreateArgs(new { });

        // Act
        var result = await _executor.ExecuteAsync("unknown_tool", args, builder);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Unknown tool");
    }

    #endregion

    #region Helpers

    private static JsonDocument CreateArgs(object args)
    {
        var json = JsonSerializer.Serialize(args);
        return JsonDocument.Parse(json);
    }

    private record ValidationOutput(bool isValid, ValidationError[] errors, ValidationWarning[] warnings);
    private record ValidationError(string code, string message, string? location);
    private record ValidationWarning(string code, string message, string? location);

    #endregion
}
