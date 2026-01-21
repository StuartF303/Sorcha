// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Xunit;
using FluentAssertions;
using Sorcha.Blueprint.Models.JsonLd;
using System;
using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Models.Tests.JsonLd;

public class BlockchainContextTests
{
    [Fact]
    public void GenerateDidUri_WithValidInputs_ReturnsCorrectFormat()
    {
        // Arrange
        var registerId = "550e8400-e29b-41d4-a716-446655440000";
        var txId = "abc123def456";

        // Act
        var didUri = BlockchainContext.GenerateDidUri(registerId, txId);

        // Assert
        didUri.Should().Be($"did:sorcha:register:{registerId}/tx/{txId}");
    }

    [Theory]
    [InlineData(null, "abc123")]
    [InlineData("", "abc123")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", null)]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", "")]
    public void GenerateDidUri_WithInvalidInputs_ThrowsArgumentException(string? registerId, string? txId)
    {
        // Act & Assert
        var act = () => BlockchainContext.GenerateDidUri(registerId!, txId!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseDidUri_WithValidUri_ReturnsComponents()
    {
        // Arrange
        var registerId = "550e8400-e29b-41d4-a716-446655440000";
        var txId = "abc123def456";
        var didUri = $"did:sorcha:register:{registerId}/tx/{txId}";

        // Act
        var result = BlockchainContext.ParseDidUri(didUri);

        // Assert
        result.Should().NotBeNull();
        result.Value.registerId.Should().Be(registerId);
        result.Value.txId.Should().Be(txId);
    }

    [Theory]
    [InlineData("invalid-uri")]
    [InlineData("did:other:register:123/tx/456")]
    [InlineData("did:sorcha:register:123")]
    [InlineData("did:sorcha:register:123/invalid")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseDidUri_WithInvalidUri_ReturnsNull(string? didUri)
    {
        // Act
        var result = BlockchainContext.ParseDidUri(didUri!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IsValidDidUri_WithValidUri_ReturnsTrue()
    {
        // Arrange
        var didUri = "did:sorcha:register:550e8400-e29b-41d4-a716-446655440000/tx/abc123";

        // Act
        var isValid = BlockchainContext.IsValidDidUri(didUri);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("did:other:register:123/tx/456")]
    [InlineData("")]
    public void IsValidDidUri_WithInvalidUri_ReturnsFalse(string didUri)
    {
        // Act
        var isValid = BlockchainContext.IsValidDidUri(didUri);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTransactionReference_ReturnsValidJsonObject()
    {
        // Arrange
        var registerId = "550e8400-e29b-41d4-a716-446655440000";
        var txId = "abc123def456";
        var timestamp = DateTime.UtcNow;

        // Act
        var reference = BlockchainContext.CreateTransactionReference(registerId, txId, timestamp);

        // Assert
        reference.Should().NotBeNull();
        reference["@context"]?.ToString().Should().Be(BlockchainContext.ContextUrl);
        reference["@type"]?.ToString().Should().Be("TransactionReference");
        reference["@id"]?.ToString().Should().Contain(registerId);
        reference["@id"]?.ToString().Should().Contain(txId);
        reference["registerId"]?.ToString().Should().Be(registerId);
        reference["txId"]?.ToString().Should().Be(txId);
        reference["timestamp"]?.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TransactionContext_IsValidJsonLd()
    {
        // Act
        var context = BlockchainContext.TransactionContext;

        // Assert
        context.Should().NotBeNull();
        context["@context"].Should().NotBeNull();

        var contextObj = context["@context"]?.AsObject();
        contextObj.Should().NotBeNull();
        contextObj!["@version"]?.ToString().Should().Be("1.1");
        contextObj["@vocab"]?.ToString().Should().Contain("sorcha.dev/blockchain");
    }

    [Fact]
    public void ContextUrl_ReturnsExpectedValue()
    {
        // Act
        var url = BlockchainContext.ContextUrl;

        // Assert
        url.Should().Be("https://sorcha.dev/contexts/blockchain/v1.jsonld");
    }

    [Fact]
    public void MergeWithBlueprintContext_ReturnsArrayWithBothContexts()
    {
        // Arrange
        var blueprintContext = JsonNode.Parse(@"{""@vocab"": ""https://sorcha.dev/blueprint/v1#""}")!;

        // Act
        JsonArray merged = BlockchainContext.MergeWithBlueprintContext(blueprintContext);

        // Assert - verify behavior rather than exact type (FluentAssertions/STJ compatibility)
        merged.Should().NotBeNull();
        merged.Count.Should().Be(2);
        merged[1]?.ToString().Should().Be(BlockchainContext.ContextUrl);
    }

    [Fact]
    public void HasBlockchainContext_WithBlockchainContext_ReturnsTrue()
    {
        // Arrange
        var node = JsonNode.Parse($@"{{""@context"": ""{BlockchainContext.ContextUrl}""}}")!;

        // Act
        var hasContext = BlockchainContext.HasBlockchainContext(node);

        // Assert
        hasContext.Should().BeTrue();
    }

    [Fact]
    public void HasBlockchainContext_WithoutBlockchainContext_ReturnsFalse()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""@context"": ""https://other.org/context""}")!;

        // Act
        var hasContext = BlockchainContext.HasBlockchainContext(node);

        // Assert
        hasContext.Should().BeFalse();
    }

    [Fact]
    public void HasBlockchainContext_WithContextArray_ReturnsTrue()
    {
        // Arrange
        var node = JsonNode.Parse($@"{{""@context"": [""https://other.org/context"", ""{BlockchainContext.ContextUrl}""]}}")!;

        // Act
        var hasContext = BlockchainContext.HasBlockchainContext(node);

        // Assert
        hasContext.Should().BeTrue();
    }
}
