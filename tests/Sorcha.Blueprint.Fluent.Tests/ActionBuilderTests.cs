// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Fluent.Tests;

public class ActionBuilderTests
{
    [Fact]
    public void ActionBuilder_WithTitle_ShouldSetTitle()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a
                .WithTitle("Submit Order")
                .SentBy("p1"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].Title.Should().Be("Submit Order");
    }

    [Fact]
    public void ActionBuilder_WithDescription_ShouldSetDescription()
    {
        // Arrange
        var description = "This action submits the order for processing";
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a
                .WithTitle("Submit Order")
                .WithDescription(description)
                .SentBy("p1"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].Description.Should().Be(description);
    }

    [Fact]
    public void ActionBuilder_SentBy_ShouldSetSender()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("buyer", p => p.Named("Buyer"))
            .AddParticipant("seller", p => p.Named("Seller"))
            .AddAction(0, a => a
                .WithTitle("Order")
                .SentBy("buyer"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].Sender.Should().Be("buyer");
    }

    [Fact]
    public void ActionBuilder_SentBy_WithInvalidParticipant_ShouldThrowException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"));

        // Act & Assert
        builder.Invoking(b => b.AddAction(0, a => a
                .WithTitle("Order")
                .SentBy("nonexistent")))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Participant 'nonexistent' not found*");
    }

    [Fact]
    public void ActionBuilder_RouteToNext_ShouldSetParticipants()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a
                .WithTitle("Action")
                .SentBy("p1")
                .RouteToNext("p2"));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].Participants.Should().NotBeNull();
        blueprint.Actions[0].Participants.Should().Contain(p => p.Principal == "p2");
    }

    [Fact]
    public void ActionBuilder_RouteToNext_WithInvalidParticipant_ShouldThrowException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"));

        // Act & Assert
        builder.Invoking(b => b.AddAction(0, a => a
                .WithTitle("Action")
                .SentBy("p1")
                .RouteToNext("nonexistent")))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Participant 'nonexistent' not found*");
    }

    [Fact]
    public void ActionBuilder_Disclose_ShouldAddDisclosure()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("buyer", p => p.Named("Buyer"))
            .AddParticipant("seller", p => p.Named("Seller"))
            .AddAction(0, a => a
                .WithTitle("Order")
                .SentBy("buyer")
                .Disclose("seller", d => d
                    .Field("/itemName")
                    .Field("/quantity")));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].Disclosures.Should().HaveCount(1);
        var disclosure = blueprint.Actions[0].Disclosures.First();
        disclosure.ParticipantAddress.Should().Be("seller");
        disclosure.DataPointers.Should().Contain("/itemName");
        disclosure.DataPointers.Should().Contain("/quantity");
    }

    [Fact]
    public void ActionBuilder_Disclose_WithInvalidParticipant_ShouldThrowException()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"));

        // Act & Assert
        builder.Invoking(b => b.AddAction(0, a => a
                .WithTitle("Action")
                .SentBy("p1")
                .Disclose("nonexistent", d => d.Field("/field"))))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*Participant 'nonexistent' not found*");
    }

    [Fact]
    public void ActionBuilder_RequiresData_ShouldAddDataSchema()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a
                .WithTitle("Order")
                .SentBy("p1")
                .RequiresData(d => d
                    .AddString("itemName")
                    .AddInteger("quantity")));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].DataSchemas.Should().NotBeNull();
        blueprint.Actions[0].DataSchemas.Should().HaveCount(1);
    }

    [Fact]
    public void ActionBuilder_Calculate_ShouldAddCalculation()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a
                .WithTitle("Order")
                .SentBy("p1")
                .Calculate("total", c => c
                    .Multiply(5, 10)));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].Calculations.Should().NotBeNull();
        blueprint.Actions[0].Calculations.Should().ContainKey("total");
    }

    [Fact]
    public void ActionBuilder_WithForm_ShouldSetForm()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Test")
            .WithDescription("Description")
            .AddParticipant("p1", p => p.Named("P1"))
            .AddParticipant("p2", p => p.Named("P2"))
            .AddAction(0, a => a
                .WithTitle("Order")
                .SentBy("p1")
                .WithForm(f => f
                    .WithLayout(LayoutTypes.VerticalLayout)
                    .AddControl(c => c
                        .OfType(ControlTypes.TextLine)
                        .WithTitle("Name")
                        .BoundTo("#/name"))));

        // Act
        var blueprint = builder.Build();

        // Assert
        blueprint.Actions[0].Form.Should().NotBeNull();
    }

    [Fact]
    public void ActionBuilder_ComplexAction_ShouldBuildCorrectly()
    {
        // Arrange
        var builder = BlueprintBuilder.Create()
            .WithTitle("Purchase Order")
            .WithDescription("A purchase order workflow")
            .AddParticipant("buyer", p => p.Named("Buyer"))
            .AddParticipant("seller", p => p.Named("Seller"))
            .AddAction(0, a => a
                .WithTitle("Submit Order")
                .WithDescription("Buyer submits purchase order")
                .SentBy("buyer")
                .RequiresData(d => d
                    .AddString("itemName")
                    .AddInteger("quantity")
                    .AddNumber("price"))
                .Disclose("seller", d => d
                    .Field("/itemName")
                    .Field("/quantity")
                    .Field("/price"))
                .Calculate("total", c => c
                    .Multiply("quantity", "price"))
                .RouteToNext("seller"));

        // Act
        var blueprint = builder.Build();

        // Assert
        var action = blueprint.Actions[0];
        action.Title.Should().Be("Submit Order");
        action.Description.Should().Be("Buyer submits purchase order");
        action.Sender.Should().Be("buyer");
        action.DataSchemas.Should().HaveCount(1);
        action.Disclosures.Should().HaveCount(1);
        action.Calculations.Should().ContainKey("total");
        action.Participants.Should().Contain(p => p.Principal == "seller");
    }
}
