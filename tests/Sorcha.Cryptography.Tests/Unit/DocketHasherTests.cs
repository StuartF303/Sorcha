// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Moq;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Utilities;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit;

/// <summary>
/// Unit tests for DocketHasher
/// Critical for blockchain docket integrity
/// </summary>
public class DocketHasherTests
{
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly DocketHasher _hasher;

    public DocketHasherTests()
    {
        _mockHashProvider = new Mock<IHashProvider>();
        _hasher = new DocketHasher(_mockHashProvider.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHashProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new DocketHasher(null!));
        exception.ParamName.Should().Be("hashProvider");
    }

    #endregion

    #region ComputeDocketHash Tests

    [Fact]
    public void ComputeDocketHash_WithValidInputs_ReturnsHash()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 5L;
        var previousHash = "prev-hash-123";
        var merkleRoot = "merkle-456";
        var timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var expectedHashBytes = new byte[] { 0xAB, 0xCD, 0xEF, 0x01 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(expectedHashBytes);

        // Act
        var result = _hasher.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, timestamp);

        // Assert
        result.Should().Be("abcdef01");
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ComputeDocketHash_ForGenesisDocket_UsesEmptyPreviousHash()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 0L;
        string? previousHash = null;
        var merkleRoot = "merkle-genesis";
        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var hashBytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, timestamp);

        // Assert
        result.Should().Be("12345678");

        // Verify the JSON contains empty string for PreviousHash
        _mockHashProvider.Verify(h => h.ComputeHash(
            It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b).Contains("\"PreviousHash\":\"\"")),
            HashType.SHA256), Times.Once);
    }

    [Fact]
    public void ComputeDocketHash_IsDeterministic_SameInputsSameOutput()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 1L;
        var previousHash = "prev";
        var merkleRoot = "merkle";
        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var hashBytes = new byte[] { 0x11, 0x22, 0x33, 0x44 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result1 = _hasher.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, timestamp);
        var result2 = _hasher.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, timestamp);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void ComputeDocketHash_WithDifferentInputs_ProducesDifferentHashes()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 1L;
        var previousHash = "prev";
        var merkleRoot = "merkle";
        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var hash1Bytes = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var hash2Bytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

        var callCount = 0;
        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(() => callCount++ == 0 ? hash1Bytes : hash2Bytes);

        // Act
        var result1 = _hasher.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, timestamp);
        var result2 = _hasher.ComputeDocketHash(registerId, docketNumber + 1, previousHash, merkleRoot, timestamp);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void ComputeDocketHash_ReturnsLowercaseHex()
    {
        // Arrange
        var hashBytes = new byte[] { 0xAB, 0xCD, 0xEF, 0x01 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.ComputeDocketHash("reg", 0, null, "merkle", DateTimeOffset.UtcNow);

        // Assert
        result.Should().Be("abcdef01");
        result.Should().NotContain("ABCDEF");
    }

    [Fact]
    public void ComputeDocketHash_UsesUnixTimestampForDeterminism()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2025, 1, 1, 12, 30, 45, TimeSpan.Zero);
        var hashBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.ComputeDocketHash("reg", 1, "prev", "merkle", timestamp);

        // Assert
        // Verify the JSON contains unix timestamp in milliseconds
        var expectedTimestamp = timestamp.ToUnixTimeMilliseconds();
        _mockHashProvider.Verify(h => h.ComputeHash(
            It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b).Contains($"\"Timestamp\":{expectedTimestamp}")),
            HashType.SHA256), Times.Once);
    }

    #endregion

    #region ComputePayloadHash Tests

    [Fact]
    public void ComputePayloadHash_WithValidPayload_ReturnsHash()
    {
        // Arrange
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"data\":\"test\"}");
        var hashBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        _mockHashProvider
            .Setup(h => h.ComputeHash(payload, HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.ComputePayloadHash(payload);

        // Assert
        result.Should().Be("deadbeef");
    }

    [Fact]
    public void ComputePayloadHash_WithNullPayload_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _hasher.ComputePayloadHash(null!));
        exception.ParamName.Should().Be("payload");
        exception.Message.Should().Contain("cannot be null or empty");
    }

    [Fact]
    public void ComputePayloadHash_WithEmptyPayload_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _hasher.ComputePayloadHash(Array.Empty<byte>()));
        exception.ParamName.Should().Be("payload");
    }

    [Fact]
    public void ComputePayloadHash_ReturnsLowercaseHex()
    {
        // Arrange
        var payload = new byte[] { 0x01, 0x02 };
        var hashBytes = new byte[] { 0xAB, 0xCD };

        _mockHashProvider
            .Setup(h => h.ComputeHash(payload, HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.ComputePayloadHash(payload);

        // Assert
        result.Should().Be("abcd");
    }

    #endregion

    #region ComputeTransactionHash Tests

    [Fact]
    public void ComputeTransactionHash_WithValidInputs_ReturnsHash()
    {
        // Arrange
        var transactionId = "tx-123";
        var payloadHash = "payload-abc";
        var timestamp = new DateTimeOffset(2025, 1, 1, 10, 30, 0, TimeSpan.Zero);
        var hashBytes = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.ComputeTransactionHash(transactionId, payloadHash, timestamp);

        // Assert
        result.Should().Be("cafebabe");
    }

    [Fact]
    public void ComputeTransactionHash_IsDeterministic_SameInputsSameOutput()
    {
        // Arrange
        var transactionId = "tx-123";
        var payloadHash = "payload-abc";
        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var hashBytes = new byte[] { 0x11, 0x22 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result1 = _hasher.ComputeTransactionHash(transactionId, payloadHash, timestamp);
        var result2 = _hasher.ComputeTransactionHash(transactionId, payloadHash, timestamp);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void ComputeTransactionHash_UsesUnixTimestamp()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2025, 1, 1, 15, 45, 30, TimeSpan.Zero);
        var hashBytes = new byte[] { 0x01, 0x02 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.ComputeTransactionHash("tx", "payload", timestamp);

        // Assert
        var expectedTimestamp = timestamp.ToUnixTimeMilliseconds();
        _mockHashProvider.Verify(h => h.ComputeHash(
            It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b).Contains($"\"Timestamp\":{expectedTimestamp}")),
            HashType.SHA256), Times.Once);
    }

    #endregion

    #region VerifyDocketHash Tests

    [Fact]
    public void VerifyDocketHash_WithMatchingHash_ReturnsTrue()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 1L;
        var previousHash = "prev";
        var merkleRoot = "merkle";
        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedHash = "abc123def456";

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(Convert.FromHexString(expectedHash));

        // Act
        var result = _hasher.VerifyDocketHash(
            registerId, docketNumber, previousHash, merkleRoot, timestamp, expectedHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyDocketHash_WithMismatchedHash_ReturnsFalse()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 1L;
        var previousHash = "prev";
        var merkleRoot = "merkle";
        var timestamp = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedHash = "abc123def456";
        var differentHash = "fedcba654321";

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(Convert.FromHexString(differentHash));

        // Act
        var result = _hasher.VerifyDocketHash(
            registerId, docketNumber, previousHash, merkleRoot, timestamp, expectedHash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyDocketHash_IsCaseInsensitive()
    {
        // Arrange
        var expectedHash = "ABC123DEF456";
        var hashBytes = Convert.FromHexString("abc123def456");

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result = _hasher.VerifyDocketHash(
            "reg", 0, null, "merkle", DateTimeOffset.UtcNow, expectedHash);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
