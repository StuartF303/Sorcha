// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Xunit;
using FluentAssertions;
using Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Models.JsonLd;
using System;
using System.Text.Json;

namespace Sorcha.Blueprint.Models.Tests;

public class TransactionReferenceTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsTransactionReference()
    {
        // Arrange
        var registerId = "550e8400-e29b-41d4-a716-446655440000";
        var txId = "abc123def456";
        var timestamp = DateTime.UtcNow;

        // Act
        var reference = TransactionReference.Create(registerId, txId, timestamp);

        // Assert
        reference.Should().NotBeNull();
        reference.RegisterId.Should().Be(registerId);
        reference.TxId.Should().Be(txId);
        reference.Timestamp.Should().Be(timestamp);
        reference.Id.Should().Contain("did:sorcha:register:");
        reference.Type.Should().Be("TransactionReference");
    }

    [Theory]
    [InlineData(null, "abc123")]
    [InlineData("", "abc123")]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", null)]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", "")]
    public void Create_WithInvalidInputs_ThrowsArgumentException(string? registerId, string? txId)
    {
        // Act & Assert
        var act = () => TransactionReference.Create(registerId!, txId!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromDidUri_WithValidUri_ReturnsTransactionReference()
    {
        // Arrange
        var registerId = "550e8400-e29b-41d4-a716-446655440000";
        var txId = "abc123def456";
        var didUri = $"did:sorcha:register:{registerId}/tx/{txId}";

        // Act
        var reference = TransactionReference.FromDidUri(didUri);

        // Assert
        reference.Should().NotBeNull();
        reference!.RegisterId.Should().Be(registerId);
        reference.TxId.Should().Be(txId);
        reference.Id.Should().Be(didUri);
    }

    [Theory]
    [InlineData("invalid-uri")]
    [InlineData("did:other:register:123/tx/456")]
    [InlineData("")]
    public void FromDidUri_WithInvalidUri_ReturnsNull(string didUri)
    {
        // Act
        var reference = TransactionReference.FromDidUri(didUri);

        // Assert
        reference.Should().BeNull();
    }

    [Fact]
    public void IsValid_WithValidReference_ReturnsTrue()
    {
        // Arrange
        var reference = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "abc123");

        // Act
        var isValid = reference.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidReference_ReturnsFalse()
    {
        // Arrange
        var reference = new TransactionReference
        {
            Id = "invalid-id",
            RegisterId = "",
            TxId = "abc123"
        };

        // Act
        var isValid = reference.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void RegenerateDidUri_UpdatesIdProperty()
    {
        // Arrange
        var reference = new TransactionReference
        {
            RegisterId = "550e8400-e29b-41d4-a716-446655440000",
            TxId = "abc123",
            Id = "old-id"
        };

        // Act
        reference.RegenerateDidUri();

        // Assert
        reference.Id.Should().StartWith("did:sorcha:register:");
        reference.Id.Should().Contain("550e8400-e29b-41d4-a716-446655440000");
        reference.Id.Should().Contain("abc123");
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var reference1 = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "abc123");
        var reference2 = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "abc123");

        // Act & Assert
        reference1.Should().Be(reference2);
        (reference1 == reference2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var reference1 = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "abc123");
        var reference2 = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "def456");

        // Act & Assert
        reference1.Should().NotBe(reference2);
        (reference1 != reference2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ReturnsDidUri()
    {
        // Arrange
        var reference = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "abc123");

        // Act
        var str = reference.ToString();

        // Assert
        str.Should().Be(reference.Id);
        str.Should().StartWith("did:sorcha:register:");
    }

    [Fact]
    public void Serialization_RoundTrip_PreservesData()
    {
        // Arrange
        var original = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "abc123");

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TransactionReference>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.RegisterId.Should().Be(original.RegisterId);
        deserialized.TxId.Should().Be(original.TxId);
        deserialized.Type.Should().Be(original.Type);
    }

    [Fact]
    public void Serialization_IncludesJsonLdProperties()
    {
        // Arrange
        var reference = TransactionReference.Create("550e8400-e29b-41d4-a716-446655440000", "abc123");

        // Act
        var json = JsonSerializer.Serialize(reference);

        // Assert
        json.Should().Contain("@context");
        json.Should().Contain("@type");
        json.Should().Contain("@id");
        json.Should().Contain("TransactionReference");
        json.Should().Contain("did:sorcha:register:");
    }
}
