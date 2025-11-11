// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Fluent;
using Sorcha.Blueprint.Models.JsonLd;
using System.Text.Json.Nodes;
using Xunit;

namespace Sorcha.Blueprint.Fluent.Tests.JsonLd;

public class ParticipantBuilderJsonLdTests
{
    [Fact]
    public void AsPerson_ShouldSetPersonType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p
                .Named("Alice")
                .FromOrganisation("Self")
                .AsPerson())
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.Equal(JsonLdTypes.Person, blueprint.Participants[0].JsonLdType);
    }

    [Fact]
    public void AsOrganization_ShouldSetOrganizationType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p
                .Named("Acme Corp")
                .FromOrganisation("Acme Corporation")
                .AsOrganization())
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.Equal(JsonLdTypes.Organization, blueprint.Participants[0].JsonLdType);
    }

    [Fact]
    public void AsJsonLdType_ShouldSetCustomType()
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p
                .Named("Alice")
                .FromOrganisation("Acme")
                .AsJsonLdType("CustomParticipantType"))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.Equal("CustomParticipantType", blueprint.Participants[0].JsonLdType);
    }

    [Theory]
    [InlineData("Self", "schema:Person")]
    [InlineData("Individual", "schema:Person")]
    [InlineData("Acme Corp", "schema:Organization")]
    [InlineData("Global Bank", "schema:Organization")]
    public void Build_WithoutExplicitType_ShouldAutoSetType(string organisation, string expectedType)
    {
        // Arrange & Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p
                .Named("Test")
                .FromOrganisation(organisation))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        Assert.Equal(expectedType, blueprint.Participants[0].JsonLdType);
    }

    [Fact]
    public void WithVerifiableCredential_ShouldAddCredential()
    {
        // Arrange
        var credential = JsonNode.Parse(@"
        {
          ""@context"": ""https://www.w3.org/2018/credentials/v1"",
          ""type"": [""VerifiableCredential"", ""BusinessCredential""],
          ""issuer"": ""did:example:issuer"",
          ""credentialSubject"": {
            ""id"": ""did:example:123"",
            ""businessRole"": ""Procurement Officer""
          }
        }")!;

        // Act
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .AddParticipant("p1", p => p
                .Named("Alice")
                .FromOrganisation("Acme")
                .WithDidUri("did:example:123")
                .WithVerifiableCredential(credential))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        var participant = blueprint.Participants[0];
        Assert.NotNull(participant.VerifiableCredential);

        var credObj = participant.VerifiableCredential as JsonObject;
        Assert.NotNull(credObj);
        Assert.Contains("@context", credObj);
        Assert.Contains("credentialSubject", credObj);
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
            .AddParticipant("p1", p => p
                .Named("Alice")
                .FromOrganisation("Acme")
                .WithAdditionalProperty("customField", customValue))
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Assert
        var participant = blueprint.Participants[0];
        Assert.NotNull(participant.AdditionalProperties);
        Assert.Contains("customField", participant.AdditionalProperties);
    }

    [Fact]
    public void ParticipantWithDID_ShouldSerializeCorrectly()
    {
        // Arrange
        var blueprint = BlueprintBuilder.Create()
            .WithTitle("Test Blueprint")
            .WithDescription("Test Description")
            .WithJsonLd()
            .AddParticipant("p1", p => p
                .Named("Alice Smith")
                .FromOrganisation("Acme Corp")
                .WithDidUri("did:example:123456789abcdefghi")
                .WithWallet("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb")
                .AsOrganization())
            .AddParticipant("p2", p => p.Named("Bob").FromOrganisation("Beta"))
            .AddAction(0, a => a.WithTitle("Test").SentBy("p1"))
            .Build();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(blueprint, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Assert
        Assert.Contains("did:example:123456789abcdefghi", json);
        Assert.Contains("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb", json);
        Assert.Contains("schema:Organization", json);
    }
}
