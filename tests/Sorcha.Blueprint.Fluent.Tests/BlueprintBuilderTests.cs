// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Fluent;

namespace Sorcha.Blueprint.Fluent.Tests;

public class BlueprintBuilderTests
{
    [Fact]
    public void Create_ShouldReturnBuilderInstance()
    {
        // Act
        var builder = BlueprintBuilder.Create();

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithoutTitle_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithDescription("Test description");

        // Act & Assert
        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*title*");
    }

    [Fact]
    public void Build_WithoutDescription_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Title");

        // Act & Assert
        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*description*");
    }

    [Fact]
    public void Build_WithTooShortTitle_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("ab")
            .WithDescription("Valid description");

        // Act & Assert
        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*title*");
    }

    [Fact]
    public void Build_WithTooShortDescription_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Valid Title")
            .WithDescription("abcd");

        // Act & Assert
        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*description*");
    }

    [Fact]
    public void Build_WithLessThanTwoParticipants_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test description")
            .AddParticipant("p1", p => p.Named("Participant 1"));

        // Act & Assert
        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 2 participants*");
    }

    [Fact]
    public void Build_WithNoActions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test description")
            .AddParticipant("p1", p => p.Named("Participant 1"))
            .AddParticipant("p2", p => p.Named("Participant 2"));

        // Act & Assert
        builder.Invoking(b => b.Build())
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 1 action*");
    }

    [Fact]
    public void Build_WithValidMinimumData_ShouldReturnBlueprint()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("A test blueprint for validation")
            .AddParticipant("buyer", p => p.Named("Buyer"))
            .AddParticipant("seller", p => p.Named("Seller"))
            .AddAction(0, a => a
                .WithTitle("Submit Order")
                .SentBy("buyer"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Should().NotBeNull();
        blueprint.Title.Should().Be("Test Blueprint");
        blueprint.Description.Should().Be("A test blueprint for validation");
        blueprint.Participants.Should().HaveCount(2);
        blueprint.Actions.Should().HaveCount(1);
    }

    [Fact]
    public void WithId_ShouldSetCustomId()
    {
        // Arrange
        var customId = "custom-blueprint-id";
        var builder = BlueprintBuilder.Create()
            .WithId(customId)
            .WithTitle("Test Blueprint")
            .WithDescription("Test description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a.WithTitle("Action").SentBy("p1"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Id.Should().Be(customId);
    }

    [Fact]
    public void WithTitle_ShouldSetTitle()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Purchase Order")
            .WithDescription("Test description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a.WithTitle("Action").SentBy("p1"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Title.Should().Be("Purchase Order");
    }

    [Fact]
    public void WithDescription_ShouldSetDescription()
    {
        // Arrange
        var description = "A comprehensive purchase order workflow";
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Title")
            .WithDescription(description)
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a.WithTitle("Action").SentBy("p1"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Description.Should().Be(description);
    }

    [Fact]
    public void WithVersion_ShouldSetCustomVersion()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Title")
            .WithDescription("Test description")
            .WithVersion(5)
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a.WithTitle("Action").SentBy("p1"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Version.Should().Be(5);
    }

    [Fact]
    public void WithMetadata_ShouldAddMetadata()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Title")
            .WithDescription("Test description")
            .WithMetadata("author", "John Doe")
            .WithMetadata("category", "Finance")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a.WithTitle("Action").SentBy("p1"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Metadata.Should().NotBeNull();
        blueprint.Metadata.Should().ContainKey("author").WhoseValue.Should().Be("John Doe");
        blueprint.Metadata.Should().ContainKey("category").WhoseValue.Should().Be("Finance");
    }

    [Fact]
    public void AddParticipant_ShouldAddParticipantToBlueprint()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Title")
            .WithDescription("Test description")
            .AddParticipant("buyer", p => p
                .Named("Buyer Organization")
                .FromOrganisation("ORG-123"))
            .AddParticipant("seller", p => p
                .Named("Seller Organization"))
            .AddAction(0, a => a.WithTitle("Action").SentBy("buyer"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Participants.Should().HaveCount(2);
        var buyer = blueprint.Participants.FirstOrDefault(p => p.Id == "buyer");
        buyer.Should().NotBeNull();
        buyer!.Name.Should().Be("Buyer Organization");
        buyer.Organisation.Should().Be("ORG-123");
    }

    [Fact]
    public void AddAction_ShouldAddActionToBlueprint()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Title")
            .WithDescription("Test description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a
                .WithTitle("Submit Application")
                .WithDescription("Submit the application for review")
                .SentBy("p1"))
            .AddAction(1, a => a
                .WithTitle("Review Application")
                .SentBy("p2"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions.Should().HaveCount(2);
        var firstAction = blueprint.Actions.FirstOrDefault(a => a.Id == 0);
        firstAction.Should().NotBeNull();
        firstAction!.Title.Should().Be("Submit Application");
        firstAction.Description.Should().Be("Submit the application for review");
        firstAction.Sender.Should().Be("p1");
    }

    [Fact]
    public void BuildDraft_ShouldReturnBlueprintWithoutValidation()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Draft Blueprint");

        // Act
        var blueprint = builder.BuildDraft();

        // Assert
        blueprint.Should().NotBeNull();
        blueprint.Title.Should().Be("Draft Blueprint");
        blueprint.Participants.Should().BeEmpty();
        blueprint.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldSetUpdatedAt()
    {
        // Arrange
        var beforeBuild = DateTimeOffset.UtcNow;
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test Title")
            .WithDescription("Test description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a.WithTitle("Action").SentBy("p1"));

        // Act
        var blueprint = builder.Build();
        var afterBuild = DateTimeOffset.UtcNow;

        // Assert
        blueprint.UpdatedAt.Should().BeOnOrAfter(beforeBuild).And.BeOnOrBefore(afterBuild);
    }

    [Fact]
    public void FluentChaining_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithId("test-id")
            .WithTitle("Complete Blueprint")
            .WithDescription("A complete blueprint with all features")
            .WithVersion(2)
            .WithMetadata("author", "Test Author")
            .WithMetadata("version", "1.0")
            .AddParticipant("participant1", p => p
                .Named("First Participant")
                .FromOrganisation("Org 1"))
            .AddParticipant("participant2", p => p
                .Named("Second Participant")
                .FromOrganisation("Org 2"))
            .AddAction(0, a => a
                .WithTitle("Action 1")
                .WithDescription("First action")
                .SentBy("participant1")
                .RouteToNext("participant2"))
            .AddAction(1, a => a
                .WithTitle("Action 2")
                .SentBy("participant2"))
            .Build();

        // Assert
        blueprint.Should().NotBeNull();
        blueprint.Id.Should().Be("test-id");
        blueprint.Title.Should().Be("Complete Blueprint");
        blueprint.Description.Should().Be("A complete blueprint with all features");
        blueprint.Version.Should().Be(2);
        blueprint.Metadata.Should().HaveCount(2);
        blueprint.Participants.Should().HaveCount(2);
        blueprint.Actions.Should().HaveCount(2);
    }
}
