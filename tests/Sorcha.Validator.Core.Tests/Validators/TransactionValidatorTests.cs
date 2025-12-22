// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Moq;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Enums;
using Sorcha.Validator.Core.Models;
using Sorcha.Validator.Core.Validators;
using Xunit;
using static Sorcha.Validator.Core.Validators.TransactionSignature;

namespace Sorcha.Validator.Core.Tests.Validators;

/// <summary>
/// Unit tests for TransactionValidator
/// Tests cover >95% code coverage as required by project standards
/// </summary>
public class TransactionValidatorTests
{
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly TransactionValidator _validator;

    public TransactionValidatorTests()
    {
        _mockHashProvider = new Mock<IHashProvider>();
        _validator = new TransactionValidator(_mockHashProvider.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHashProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new TransactionValidator(null!));
        exception.ParamName.Should().Be("hashProvider");
    }

    #endregion

    #region ValidateTransactionStructure Tests

    [Fact]
    public void ValidateTransactionStructure_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var registerId = Guid.NewGuid().ToString();
        var blueprintId = Guid.NewGuid().ToString();
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var payloadHash = "abc123def456";
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("public-key-1", "signature-value-1", "ED25519")
        };
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        var result = _validator.ValidateTransactionStructure(
            transactionId, registerId, blueprintId, payload, payloadHash, signatures, createdAt);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "register-1", "blueprint-1", "TX_001", "transactionId")]
    [InlineData(null, "register-1", "blueprint-1", "TX_001", "transactionId")]
    [InlineData("   ", "register-1", "blueprint-1", "TX_001", "transactionId")]
    public void ValidateTransactionStructure_WithInvalidTransactionId_ReturnsError(
        string? transactionId, string registerId, string blueprintId, string expectedCode, string expectedField)
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var payloadHash = "abc123";
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "ED25519")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            transactionId!, registerId, blueprintId, payload, payloadHash, signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(expectedCode);
        result.Errors[0].Field.Should().Be(expectedField);
    }

    [Theory]
    [InlineData("", "TX_002", "registerId")]
    [InlineData(null, "TX_002", "registerId")]
    [InlineData("   ", "TX_002", "registerId")]
    public void ValidateTransactionStructure_WithInvalidRegisterId_ReturnsError(
        string? registerId, string expectedCode, string expectedField)
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "ED25519")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", registerId!, "blueprint-1", payload, "hash", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(expectedCode);
        result.Errors[0].Field.Should().Be(expectedField);
    }

    [Theory]
    [InlineData("", "TX_003", "blueprintId")]
    [InlineData(null, "TX_003", "blueprintId")]
    [InlineData("   ", "TX_003", "blueprintId")]
    public void ValidateTransactionStructure_WithInvalidBlueprintId_ReturnsError(
        string? blueprintId, string expectedCode, string expectedField)
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "ED25519")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", blueprintId!, payload, "hash", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(expectedCode);
        result.Errors[0].Field.Should().Be(expectedField);
    }

    [Fact]
    public void ValidateTransactionStructure_WithNullPayload_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("null").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "ED25519")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_004");
        result.Errors[0].Field.Should().Be("payload");
    }

    [Theory]
    [InlineData("", "TX_005")]
    [InlineData(null, "TX_005")]
    [InlineData("   ", "TX_005")]
    public void ValidateTransactionStructure_WithInvalidPayloadHash_ReturnsError(
        string? payloadHash, string expectedCode)
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "ED25519")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, payloadHash!, signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == expectedCode);
    }

    [Fact]
    public void ValidateTransactionStructure_WithNullSignatures_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", null!, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_006");
    }

    [Fact]
    public void ValidateTransactionStructure_WithEmptySignatures_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>();

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_006");
    }

    [Fact]
    public void ValidateTransactionStructure_WithSignatureMissingPublicKey_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("", "sig", "ED25519")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_007");
        result.Errors[0].Message.Should().Contain("Signature 0");
    }

    [Fact]
    public void ValidateTransactionStructure_WithSignatureMissingSignatureValue_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "", "ED25519")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_008");
    }

    [Fact]
    public void ValidateTransactionStructure_WithSignatureMissingAlgorithm_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_009");
    }

    [Fact]
    public void ValidateTransactionStructure_WithFutureTimestamp_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "ED25519")
        };
        var futureTime = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", signatures, futureTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_010");
        result.Errors[0].Message.Should().Contain("future");
    }

    [Fact]
    public void ValidateTransactionStructure_WithTimestampWithin5MinuteSkew_ReturnsSuccess()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", "ED25519")
        };
        var nearFutureTime = DateTimeOffset.UtcNow.AddMinutes(4); // Within 5 minute skew

        // Act
        var result = _validator.ValidateTransactionStructure(
            "tx-1", "register-1", "blueprint-1", payload, "hash", signatures, nearFutureTime);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTransactionStructure_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var payload = JsonDocument.Parse("null").RootElement;
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("", "", "")
        };

        // Act
        var result = _validator.ValidateTransactionStructure(
            "", "", "", payload, "", signatures, DateTimeOffset.UtcNow);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(5); // Multiple validation errors
        result.Errors.Should().Contain(e => e.Code == "TX_001"); // Transaction ID
        result.Errors.Should().Contain(e => e.Code == "TX_002"); // Register ID
        result.Errors.Should().Contain(e => e.Code == "TX_003"); // Blueprint ID
    }

    #endregion

    #region ValidatePayloadHash Tests

    [Fact]
    public void ValidatePayloadHash_WithMatchingHash_ReturnsSuccess()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var expectedHash = "abc123def456";
        var computedHashBytes = Convert.FromHexString(expectedHash);

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(computedHashBytes);

        // Act
        var result = _validator.ValidatePayloadHash(payload, expectedHash);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidatePayloadHash_WithMismatchedHash_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var expectedHash = "abc123def456";
        var differentHash = "fedcba987654"; // Valid hex but different
        var computedHashBytes = Convert.FromHexString(differentHash); // Different hash

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(computedHashBytes);

        // Act
        var result = _validator.ValidatePayloadHash(payload, expectedHash);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_012");
        result.Errors[0].Message.Should().Contain("mismatch");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ValidatePayloadHash_WithInvalidExpectedHash_ReturnsError(string? expectedHash)
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;

        // Act
        var result = _validator.ValidatePayloadHash(payload, expectedHash!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_011");
    }

    [Fact]
    public void ValidatePayloadHash_WhenHashProviderThrows_ReturnsError()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var expectedHash = "abc123";

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Throws(new InvalidOperationException("Hash computation failed"));

        // Act
        var result = _validator.ValidatePayloadHash(payload, expectedHash);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_013");
        result.Errors[0].Message.Should().Contain("Failed to validate payload hash");
    }

    [Fact]
    public void ValidatePayloadHash_WithCaseInsensitiveHash_ReturnsSuccess()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement;
        var expectedHash = "ABC123DEF456";
        var computedHashBytes = Convert.FromHexString("abc123def456"); // Lowercase

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(computedHashBytes);

        // Act
        var result = _validator.ValidatePayloadHash(payload, expectedHash);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateSignatures Tests

    [Fact]
    public void ValidateSignatures_WithValidSignatures_ReturnsSuccess()
    {
        // Arrange
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key-1", "sig-1", "ED25519"),
            new TransactionSignature("key-2", "sig-2", "NIST-P256")
        };

        // Act
        var result = _validator.ValidateSignatures(signatures, "tx-123");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSignatures_WithNullSignatures_ReturnsError()
    {
        // Act
        var result = _validator.ValidateSignatures(null!, "tx-123");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_014");
    }

    [Fact]
    public void ValidateSignatures_WithEmptySignatures_ReturnsError()
    {
        // Act
        var result = _validator.ValidateSignatures(new List<TransactionSignature>(), "tx-123");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_014");
    }

    [Fact]
    public void ValidateSignatures_WithMissingPublicKey_ReturnsError()
    {
        // Arrange
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("", "sig", "ED25519")
        };

        // Act
        var result = _validator.ValidateSignatures(signatures, "tx-123");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_015");
    }

    [Fact]
    public void ValidateSignatures_WithMissingSignatureValue_ReturnsError()
    {
        // Arrange
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "", "ED25519")
        };

        // Act
        var result = _validator.ValidateSignatures(signatures, "tx-123");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "TX_016");
    }

    [Theory]
    [InlineData("InvalidAlgorithm")]
    [InlineData("SHA256")]
    [InlineData("AES")]
    [InlineData("")]
    public void ValidateSignatures_WithUnsupportedAlgorithm_ReturnsError(string algorithm)
    {
        // Arrange
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", algorithm)
        };

        // Act
        var result = _validator.ValidateSignatures(signatures, "tx-123");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "TX_017" || e.Code == "TX_009");
    }

    [Theory]
    [InlineData("ED25519")]
    [InlineData("NIST-P256")]
    [InlineData("RSA-4096")]
    [InlineData("ed25519")] // Case insensitive
    [InlineData("nist-p256")] // Case insensitive
    public void ValidateSignatures_WithSupportedAlgorithms_ReturnsSuccess(string algorithm)
    {
        // Arrange
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("key", "sig", algorithm)
        };

        // Act
        var result = _validator.ValidateSignatures(signatures, "tx-123");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignatures_WithMultipleErrorsInDifferentSignatures_ReturnsAllErrors()
    {
        // Arrange
        var signatures = new List<TransactionSignature>
        {
            new TransactionSignature("", "sig1", "ED25519"),
            new TransactionSignature("key2", "", "ED25519"),
            new TransactionSignature("key3", "sig3", "InvalidAlgo")
        };

        // Act
        var result = _validator.ValidateSignatures(signatures, "tx-123");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.Code == "TX_015"); // Missing public key
        result.Errors.Should().Contain(e => e.Code == "TX_016"); // Missing signature value
        result.Errors.Should().Contain(e => e.Code == "TX_017"); // Invalid algorithm
    }

    #endregion
}

