// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Models.JsonLd;
using System.Text.Json.Nodes;
using Xunit;

namespace Sorcha.Blueprint.Fluent.Tests.JsonLd;

public class ActionBuilderJsonLdTests
{
    [Fact]
    public void AsCreateAction_ShouldSetCreateActionType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle("Submit Application")
                .SentBy("p1")
                .AsCreateAction())
            .Build();

        // Assert
        Assert.Equal(JsonLdTypes.CreateAction, blueprint.Actions[0].JsonLdType);
    }

    [Fact]
    public void AsAcceptAction_ShouldSetAcceptActionType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle("Approve Request")
                .SentBy("p1")
                .AsAcceptAction())
            .Build();

        // Assert
        Assert.Equal(JsonLdTypes.AcceptAction, blueprint.Actions[0].JsonLdType);
    }

    [Fact]
    public void AsRejectAction_ShouldSetRejectActionType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle("Reject Application")
                .SentBy("p1")
                .AsRejectAction())
            .Build();

        // Assert
        Assert.Equal(JsonLdTypes.RejectAction, blueprint.Actions[0].JsonLdType);
    }

    [Fact]
    public void AsUpdateAction_ShouldSetUpdateActionType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle("Update Profile")
                .SentBy("p1")
                .AsUpdateAction())
            .Build();

        // Assert
        Assert.Equal(JsonLdTypes.UpdateAction, blueprint.Actions[0].JsonLdType);
    }

    [Theory]
    [InlineData("Submit Application", "as:Create")]
    [InlineData("Create Order", "as:Create")]
    [InlineData("Approve Request", "as:Accept")]
    [InlineData("Reject Application", "as:Reject")]
    [InlineData("Update Profile", "as:Update")]
    [InlineData("Review Document", "as:Activity")]
    public void Build_WithoutExplicitType_ShouldAutoSetType(string title, string expectedType)
    {
        // Arrange & Act - JSON-LD mode must be enabled for auto-setting to occur
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLd()
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle(title)
                .SentBy("p1"))
            .Build();

        // Assert
        Assert.Equal(expectedType, blueprint.Actions[0].JsonLdType);
    }

    [Fact]
    public void WithTarget_ShouldSetTargetParticipant()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle("Send Request")
                .SentBy("p1")
                .WithTarget("p2"))
            .Build();

        // Assert
        Assert.Equal("p2", blueprint.Actions[0].Target);
    }

    [Fact]
    public void PublishedAt_ShouldSetTimestamp()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle("Submit")
                .SentBy("p1")
                .PublishedAt(timestamp))
            .Build();

        // Assert
        Assert.Equal(timestamp, blueprint.Actions[0].Published);
    }

    [Fact]
    public void WithAdditionalProperty_ShouldAddCustomProperty()
    {
        // Arrange
        var customValue = JsonNode.Parse(@"{""key"": ""value""}")!;

        // Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a
                .WithTitle("Test")
                .SentBy("p1")
                .WithAdditionalProperty("customField", customValue))
            .Build();

        // Assert
        var action = blueprint.Actions[0];
        Assert.NotNull(action.AdditionalProperties);
        Assert.Contains("customField", action.AdditionalProperties);
    }

    [Fact]
    public void ActionWithActivityStreams_ShouldSerializeCorrectly()
    {
        // Arrange
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLd()
            .AddParticipant("applicant", p => p
                .Named("John Doe")
                .FromOrganisation("Self")
                .WithDidUri("did:example:applicant"))
            .AddParticipant("officer", p => p
                .Named("Loan Officer")
                .FromOrganisation("Bank Corp")
                .WithDidUri("did:example:officer"))
            .AddAction(0, a => a
                .WithTitle("Submit Application")
                .WithDescription("Applicant submits loan application")
                .SentBy("applicant")
                .WithTarget("officer")
                .AsCreateAction()
                .PublishedAt(DateTimeOffset.UtcNow))
            .Build();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(blueprint, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Assert
        Assert.Contains("as:Create", json);
        Assert.Contains("\"target\":", json);
        Assert.Contains("\"published\":", json);
        Assert.Contains("\"@context\"", json);
    }

    [Fact]
    public void ComplexBlueprintWithJsonLd_ShouldBuildSuccessfully()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Loan Application Workflow")
            .WithDescription("Multi-party loan application with semantic annotations")
            .WithJsonLd("finance")
            .AddParticipant("applicant", p => p
                .Named("John Doe")
                .FromOrganisation("Self")
                .WithDidUri("did:example:applicant-123")
                .WithWallet("0x1234567890abcdef")
                .AsPerson())
            .AddParticipant("officer", p => p
                .Named("Loan Department")
                .FromOrganisation("Community Bank")
                .WithDidUri("did:example:bank-456")
                .WithWallet("0xabcdef1234567890")
                .AsOrganization())
            .AddAction(0, a => a
                .WithTitle("Submit Loan Application")
                .WithDescription("Applicant submits application with required documents")
                .SentBy("applicant")
                .WithTarget("officer")
                .AsCreateAction()
                .PublishedAt(DateTimeOffset.UtcNow))
            .AddAction(1, a => a
                .WithTitle("Review and Decide")
                .WithDescription("Loan officer reviews and makes decision")
                .SentBy("officer")
                .WithTarget("applicant"))
            .Build();

        // Assert
        Assert.NotNull(blueprint.JsonLdContext);
        Assert.Equal(JsonLdTypes.Blueprint, blueprint.JsonLdType);
        Assert.Equal(2, blueprint.Participants.Count);
        Assert.Equal(2, blueprint.Actions.Count);
        Assert.Equal(JsonLdTypes.Person, blueprint.Participants[0].JsonLdType);
        Assert.Equal(JsonLdTypes.Organization, blueprint.Participants[1].JsonLdType);
        Assert.Equal(JsonLdTypes.CreateAction, blueprint.Actions[0].JsonLdType);
    }
}
