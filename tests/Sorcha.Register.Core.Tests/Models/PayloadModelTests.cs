// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Sorcha.Register.Core.Tests.Models;

public class PayloadModelTests
{
    [Fact]
    public void PayloadModel_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var payload = new PayloadModel();

        // Assert
        payload.WalletAccess.Should().BeEmpty();
        payload.PayloadSize.Should().Be(0ul);
        payload.Hash.Should().BeEmpty();
        payload.Data.Should().BeEmpty();
        payload.PayloadFlags.Should().BeNull();
        payload.IV.Should().BeNull();
        payload.Challenges.Should().BeNull();
    }

    [Fact]
    public void PayloadModel_WithValidProperties_ShouldCreateSuccessfully()
    {
        // Arrange
        var walletAccess = new[] { "wallet1", "wallet2" };
        var hash = "sha256-hash";
        var data = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var payloadSize = 1024ul;

        // Act
        var payload = new PayloadModel
        {
            WalletAccess = walletAccess,
            PayloadSize = payloadSize,
            Hash = hash,
            Data = data,
            PayloadFlags = "encrypted"
        };

        // Assert
        payload.WalletAccess.Should().HaveCount(2);
        payload.WalletAccess.Should().Contain("wallet1");
        payload.PayloadSize.Should().Be(payloadSize);
        payload.Hash.Should().Be(hash);
        payload.Data.Should().Be(data);
        payload.PayloadFlags.Should().Be("encrypted");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PayloadModel_WithInvalidHash_ShouldFailValidation(string? invalidHash)
    {
        // Arrange
        var payload = new PayloadModel
        {
            Hash = invalidHash!,
            Data = "some-data"
        };

        // Act
        var validationResults = ValidateModel(payload);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Hash"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PayloadModel_WithInvalidData_ShouldFailValidation(string? invalidData)
    {
        // Arrange
        var payload = new PayloadModel
        {
            Hash = "valid-hash",
            Data = invalidData!
        };

        // Act
        var validationResults = ValidateModel(payload);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Data"));
    }

    [Fact]
    public void PayloadModel_WithBase64Data_ShouldStoreCorrectly()
    {
        // Arrange
        var originalBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var base64Data = Convert.ToBase64String(originalBytes);

        var payload = new PayloadModel
        {
            Hash = "hash123",
            Data = base64Data,
            PayloadSize = (ulong)originalBytes.Length
        };

        // Act
        var decodedBytes = Convert.FromBase64String(payload.Data);

        // Assert
        decodedBytes.Should().BeEquivalentTo(originalBytes);
        payload.PayloadSize.Should().Be((ulong)originalBytes.Length);
    }

    [Fact]
    public void PayloadModel_WithMultipleWalletAccess_ShouldStoreAll()
    {
        // Arrange
        var wallets = new[] { "wallet1", "wallet2", "wallet3", "wallet4", "wallet5" };
        var payload = new PayloadModel
        {
            Hash = "hash123",
            Data = "data123",
            WalletAccess = wallets
        };

        // Act & Assert
        payload.WalletAccess.Should().HaveCount(5);
        payload.WalletAccess.Should().Contain("wallet1");
        payload.WalletAccess.Should().Contain("wallet5");
    }

    [Fact]
    public void PayloadModel_WithIV_ShouldStoreCorrectly()
    {
        // Arrange
        var iv = new Challenge
        {
            Data = "iv-data",
            Address = "address123"
        };

        var payload = new PayloadModel
        {
            Hash = "hash123",
            Data = "data123",
            IV = iv
        };

        // Act & Assert
        payload.IV.Should().NotBeNull();
        payload.IV!.Data.Should().Be("iv-data");
        payload.IV.Address.Should().Be("address123");
    }

    [Fact]
    public void PayloadModel_WithChallenges_ShouldStoreAll()
    {
        // Arrange
        var challenges = new[]
        {
            new Challenge { Data = "challenge1", Address = "wallet1" },
            new Challenge { Data = "challenge2", Address = "wallet2" },
            new Challenge { Data = "challenge3", Address = "wallet3" }
        };

        var payload = new PayloadModel
        {
            Hash = "hash123",
            Data = "data123",
            Challenges = challenges
        };

        // Act & Assert
        payload.Challenges.Should().NotBeNull();
        payload.Challenges.Should().HaveCount(3);
        payload.Challenges![0].Data.Should().Be("challenge1");
        payload.Challenges[2].Address.Should().Be("wallet3");
    }

    [Fact]
    public void PayloadModel_PayloadSize_ShouldAcceptUInt64Values()
    {
        // Arrange
        var payload = new PayloadModel
        {
            Hash = "hash123",
            Data = "data123"
        };

        // Act & Assert
        payload.PayloadSize = 0;
        payload.PayloadSize.Should().Be(0ul);

        payload.PayloadSize = 1024;
        payload.PayloadSize.Should().Be(1024ul);

        payload.PayloadSize = ulong.MaxValue;
        payload.PayloadSize.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void PayloadModel_WithPayloadFlags_ShouldStoreCorrectly()
    {
        // Arrange
        var payload = new PayloadModel
        {
            Hash = "hash123",
            Data = "data123",
            PayloadFlags = "encrypted,compressed,signed"
        };

        // Act & Assert
        payload.PayloadFlags.Should().Be("encrypted,compressed,signed");
    }

    [Fact]
    public void PayloadModel_EmptyWalletAccess_ShouldMeanPublicPayload()
    {
        // Arrange - Empty wallet access could mean payload is public
        var payload = new PayloadModel
        {
            Hash = "hash123",
            Data = "public-data",
            WalletAccess = Array.Empty<string>()
        };

        // Act & Assert
        payload.WalletAccess.Should().BeEmpty();
    }

    [Fact]
    public void PayloadModel_FullyPopulated_ShouldPassValidation()
    {
        // Arrange
        var payload = new PayloadModel
        {
            WalletAccess = new[] { "wallet1", "wallet2", "wallet3" },
            PayloadSize = 2048,
            Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            Data = Convert.ToBase64String(new byte[2048]),
            PayloadFlags = "encrypted,authenticated",
            IV = new Challenge
            {
                Data = "initialization-vector",
                Address = "iv-address"
            },
            Challenges = new[]
            {
                new Challenge { Data = "challenge1", Address = "wallet1" },
                new Challenge { Data = "challenge2", Address = "wallet2" },
                new Challenge { Data = "challenge3", Address = "wallet3" }
            }
        };

        // Act
        var validationResults = ValidateModel(payload);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void PayloadModel_WithLargeData_ShouldHandle()
    {
        // Arrange
        var largeBytes = new byte[1024 * 1024]; // 1MB
        var random = new Random(42);
        random.NextBytes(largeBytes);
        var largeBase64 = Convert.ToBase64String(largeBytes);

        var payload = new PayloadModel
        {
            Hash = "hash-for-large-data",
            Data = largeBase64,
            PayloadSize = (ulong)largeBytes.Length
        };

        // Act
        var decoded = Convert.FromBase64String(payload.Data);

        // Assert
        decoded.Should().HaveCount(largeBytes.Length);
        payload.PayloadSize.Should().Be((ulong)largeBytes.Length);
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
