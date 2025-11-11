// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Models.JsonLd;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Sorcha.Blueprint.Fluent.Tests.JsonLd;

public class BlueprintBuilderJsonLdTests
{
    [Fact]
    public void WithJsonLd_ShouldAddDefaultContext()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLd()
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.NotNull(blueprint.JsonLdContext);
        Assert.Equal(JsonLdTypes.Blueprint, blueprint.JsonLdType);

        var contextObj = blueprint.JsonLdContext as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("@vocab", contextObj);
        Assert.Contains("schema", contextObj);
    }

    [Theory]
    [InlineData("finance")]
    [InlineData("supply-chain")]
    [InlineData("loan")]
    public void WithJsonLd_WithCategory_ShouldAddCategoryContext(string category)
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLd(category)
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.NotNull(blueprint.JsonLdContext);
        Assert.Equal(JsonLdTypes.Blueprint, blueprint.JsonLdType);
        Assert.NotNull(blueprint.Metadata);
        Assert.Equal(category, blueprint.Metadata["category"]);
    }

    [Fact]
    public void WithJsonLdContext_ShouldSetCustomContext()
    {
        // Arrange
        var customContext = JsonNode.Parse(@"{""custom"": ""value"", ""test"": ""data""}")!;

        // Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLdContext(customContext)
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.NotNull(blueprint.JsonLdContext);
        var contextObj = blueprint.JsonLdContext as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("custom", contextObj);
        Assert.Contains("test", contextObj);
    }

    [Fact]
    public void WithJsonLdType_ShouldSetCustomType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLdType("CustomType")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.Equal("CustomType", blueprint.JsonLdType);
    }

    [Fact]
    public void WithAdditionalJsonLdContext_ShouldMergeContexts()
    {
        // Arrange
        var additionalContext = JsonNode.Parse(@"{""additional"": ""field""}")!;

        // Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLd()
            .WithAdditionalJsonLdContext(additionalContext)
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.NotNull(blueprint.JsonLdContext);
        var contextObj = blueprint.JsonLdContext as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("@vocab", contextObj); // From default
        Assert.Contains("additional", contextObj); // From additional
    }

    [Fact]
    public void Blueprint_WithJsonLd_ShouldSerializeCorrectly()
    {
        // Arrange
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLd("finance")
            .AddParticipant("applicant", p => p
                .Named("John Doe")
                .FromOrganisation("Self")
                .WithDidUri("did:example:123"))
            .AddParticipant("officer", p => p
                .Named("Loan Officer")
                .FromOrganisation("Bank Corp")
                .WithDidUri("did:example:456"))
            .AddAction(0, a => a
                .WithTitle("Submit Application")
                .SentBy("applicant")
                .AsCreateAction())
            .Build();

        // Act
        var json = JsonSerializer.Serialize(blueprint, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Assert
        Assert.Contains("\"@context\"", json);
        Assert.Contains("\"@type\"", json);
        Assert.Contains("Blueprint", json);
    }

    [Fact]
    public void Blueprint_WithoutJsonLd_ShouldNotIncludeContext()
    {
        // Arrange
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p.Named("Alice").FromOrganisation("Acme"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Act
        var json = JsonSerializer.Serialize(blueprint);

        // Assert
        Assert.DoesNotContain("\"@context\"", json);
        Assert.DoesNotContain("\"@type\"", json);
    }
}
