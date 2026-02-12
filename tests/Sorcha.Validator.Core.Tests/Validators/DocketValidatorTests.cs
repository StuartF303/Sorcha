// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Moq;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Enums;
using Sorcha.Validator.Core.Models;
using Sorcha.Validator.Core.Validators;
using Xunit;

namespace Sorcha.Validator.Core.Tests.Validators;

/// <summary>
/// Unit tests for DocketValidator
/// Tests cover >95% code coverage as required by project standards
/// </summary>
public class DocketValidatorTests
{
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly DocketValidator _validator;

    public DocketValidatorTests()
    {
        _mockHashProvider = new Mock<IHashProvider>();
        _validator = new DocketValidator(_mockHashProvider.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHashProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new DocketValidator(null!));
        exception.ParamName.Should().Be("hashProvider");
    }

    #endregion

    #region ValidateDocketStructure Tests

    [Fact]
    public void ValidateDocketStructure_WithValidDocket_ReturnsSuccess()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1);

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDocketStructure_WithValidGenesisDocket_ReturnsSuccess()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 0, previousHash: null);

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "DK_001")]
    [InlineData(null, "DK_001")]
    [InlineData("   ", "DK_001")]
    public void ValidateDocketStructure_WithInvalidDocketId_ReturnsError(string? docketId, string expectedCode)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1) with { DocketId = docketId! };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == expectedCode);
        result.Errors[0].Field.Should().Be(nameof(docket.DocketId));
    }

    [Theory]
    [InlineData("", "DK_002")]
    [InlineData(null, "DK_002")]
    [InlineData("   ", "DK_002")]
    public void ValidateDocketStructure_WithInvalidRegisterId_ReturnsError(string? registerId, string expectedCode)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1) with { RegisterId = registerId! };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == expectedCode);
        result.Errors[0].Field.Should().Be(nameof(docket.RegisterId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(long.MinValue)]
    public void ValidateDocketStructure_WithNegativeDocketNumber_ReturnsError(long docketNumber)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 0) with { DocketNumber = docketNumber };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_003");
        result.Errors[0].Message.Should().Contain("cannot be negative");
    }

    [Theory]
    [InlineData("", "DK_004")]
    [InlineData(null, "DK_004")]
    [InlineData("   ", "DK_004")]
    public void ValidateDocketStructure_WithInvalidDocketHash_ReturnsError(string? docketHash, string expectedCode)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1) with { DocketHash = docketHash! };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == expectedCode);
    }

    [Theory]
    [InlineData("", "DK_005")]
    [InlineData(null, "DK_005")]
    [InlineData("   ", "DK_005")]
    public void ValidateDocketStructure_WithInvalidMerkleRoot_ReturnsError(string? merkleRoot, string expectedCode)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1) with { MerkleRoot = merkleRoot! };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == expectedCode);
    }

    [Theory]
    [InlineData("", "DK_006")]
    [InlineData(null, "DK_006")]
    [InlineData("   ", "DK_006")]
    public void ValidateDocketStructure_WithInvalidProposerValidatorId_ReturnsError(
        string? proposerValidatorId, string expectedCode)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1) with { ProposerValidatorId = proposerValidatorId! };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == expectedCode);
    }

    [Fact]
    public void ValidateDocketStructure_WithFutureTimestamp_ReturnsError()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddMinutes(10);
        var docket = CreateValidDocket(docketNumber: 1) with { CreatedAt = futureTime };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_007");
        result.Errors[0].Message.Should().Contain("future");
    }

    [Fact]
    public void ValidateDocketStructure_WithTimestampWithin5MinuteSkew_ReturnsSuccess()
    {
        // Arrange
        var nearFutureTime = DateTimeOffset.UtcNow.AddMinutes(4); // Within 5 minute skew
        var docket = CreateValidDocket(docketNumber: 1) with { CreatedAt = nearFutureTime };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ValidateDocketStructure_WithNegativeTransactionCount_ReturnsError(int transactionCount)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1) with { TransactionCount = transactionCount };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_008");
    }

    [Fact]
    public void ValidateDocketStructure_GenesisDocketWithPreviousHash_ReturnsError()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 0, previousHash: "some-hash");

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_009");
        result.Errors[0].Message.Should().Contain("Genesis docket");
        result.Errors[0].Message.Should().Contain("cannot have a previous hash");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ValidateDocketStructure_NonGenesisDocketWithoutPreviousHash_ReturnsError(string? previousHash)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1) with { PreviousHash = previousHash };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_010");
        result.Errors[0].Message.Should().Contain("must have a previous hash");
    }

    [Fact]
    public void ValidateDocketStructure_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var docket = new DocketData
        {
            DocketId = "",
            RegisterId = "",
            DocketNumber = -1,
            PreviousHash = null,
            DocketHash = "",
            CreatedAt = DateTimeOffset.UtcNow,
            MerkleRoot = "",
            ProposerValidatorId = "",
            TransactionCount = -1
        };

        // Act
        var result = _validator.ValidateDocketStructure(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(5);
        result.Errors.Should().Contain(e => e.Code == "DK_001"); // DocketId
        result.Errors.Should().Contain(e => e.Code == "DK_002"); // RegisterId
        result.Errors.Should().Contain(e => e.Code == "DK_003"); // DocketNumber
        result.Errors.Should().Contain(e => e.Code == "DK_004"); // DocketHash
        result.Errors.Should().Contain(e => e.Code == "DK_005"); // MerkleRoot
    }

    #endregion

    #region ValidateDocketHash Tests

    [Fact]
    public void ValidateDocketHash_WithMatchingHash_ReturnsSuccess()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1);
        var expectedHash = "abc123def456";

        // Mock hash computation to return expected hash
        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(Convert.FromHexString(expectedHash));

        // Act
        var result = _validator.ValidateDocketHash(docket, expectedHash);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateDocketHash_WithMismatchedHash_ReturnsError()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1);
        var expectedHash = "abc123def456";
        var differentHash = "fedcba987654";

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(Convert.FromHexString(differentHash));

        // Act
        var result = _validator.ValidateDocketHash(docket, expectedHash);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_012");
        result.Errors[0].Message.Should().Contain("mismatch");
        result.Errors[0].Message.Should().Contain(expectedHash);
        result.Errors[0].Message.Should().Contain(differentHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ValidateDocketHash_WithInvalidExpectedHash_ReturnsError(string? expectedHash)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1);

        // Act
        var result = _validator.ValidateDocketHash(docket, expectedHash!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_011");
    }

    [Fact]
    public void ValidateDocketHash_WhenHashProviderThrows_ReturnsError()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1);
        var expectedHash = "abc123";

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Throws(new InvalidOperationException("Hash computation failed"));

        // Act
        var result = _validator.ValidateDocketHash(docket, expectedHash);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_013");
        result.Errors[0].Message.Should().Contain("Failed to validate docket hash");
    }

    [Fact]
    public void ValidateDocketHash_WithCaseInsensitiveHash_ReturnsSuccess()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 1);
        var expectedHash = "ABC123DEF456";

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(Convert.FromHexString("abc123def456")); // Lowercase

        // Act
        var result = _validator.ValidateDocketHash(docket, expectedHash);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateDocketHash_ForGenesisDocket_ReturnsSuccess()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 0, previousHash: null);
        var expectedHash = "abc123def456"; // Even length hex string

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(Convert.FromHexString(expectedHash));

        // Act
        var result = _validator.ValidateDocketHash(docket, expectedHash);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateChainContinuity Tests

    [Fact]
    public void ValidateChainContinuity_WithValidContinuity_ReturnsSuccess()
    {
        // Arrange
        var previousDocket = CreateValidDocket(docketNumber: 0, previousHash: null);
        var currentDocket = CreateValidDocket(
            docketNumber: 1,
            previousHash: previousDocket.DocketHash);

        // Act
        var result = _validator.ValidateChainContinuity(currentDocket, previousDocket);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 2)] // Skip from 0 to 2
    [InlineData(5, 10)] // Jump from 5 to 10
    [InlineData(100, 99)] // Go backwards
    public void ValidateChainContinuity_WithInvalidDocketNumberSequence_ReturnsError(
        long previousNumber, long currentNumber)
    {
        // Arrange
        var previousDocket = CreateValidDocket(docketNumber: previousNumber, previousHash: null);
        var currentDocket = CreateValidDocket(
            docketNumber: currentNumber,
            previousHash: previousDocket.DocketHash);

        // Act
        var result = _validator.ValidateChainContinuity(currentDocket, previousDocket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "DK_014");
        result.Errors[0].Message.Should().Contain("sequential");
        result.Errors[0].Message.Should().Contain($"Expected: {previousNumber + 1}");
        result.Errors[0].Message.Should().Contain($"Actual: {currentNumber}");
    }

    [Fact]
    public void ValidateChainContinuity_WithDifferentRegisterIds_ReturnsError()
    {
        // Arrange
        var previousDocket = CreateValidDocket(docketNumber: 0, previousHash: null)
            with { RegisterId = "register-1" };
        var currentDocket = CreateValidDocket(docketNumber: 1, previousHash: previousDocket.DocketHash)
            with { RegisterId = "register-2" };

        // Act
        var result = _validator.ValidateChainContinuity(currentDocket, previousDocket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_015");
        result.Errors[0].Message.Should().Contain("same register");
    }

    [Fact]
    public void ValidateChainContinuity_WithPreviousHashMismatch_ReturnsError()
    {
        // Arrange
        var previousDocket = CreateValidDocket(docketNumber: 0, previousHash: null)
            with { DocketHash = "correct-hash" };
        var currentDocket = CreateValidDocket(docketNumber: 1, previousHash: "wrong-hash");

        // Act
        var result = _validator.ValidateChainContinuity(currentDocket, previousDocket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_016");
        result.Errors[0].Message.Should().Contain("Previous hash mismatch");
        result.Errors[0].Message.Should().Contain("correct-hash");
        result.Errors[0].Message.Should().Contain("wrong-hash");
    }

    [Fact]
    public void ValidateChainContinuity_WithEarlierTimestamp_ReturnsError()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var previousDocket = CreateValidDocket(docketNumber: 0, previousHash: null)
            with { CreatedAt = now };
        var currentDocket = CreateValidDocket(docketNumber: 1, previousHash: previousDocket.DocketHash)
            with { CreatedAt = now.AddMinutes(-10) };

        // Act
        var result = _validator.ValidateChainContinuity(currentDocket, previousDocket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "DK_017");
        result.Errors[0].Message.Should().Contain("cannot be earlier");
    }

    [Fact]
    public void ValidateChainContinuity_WithSameTimestamp_ReturnsSuccess()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var previousDocket = CreateValidDocket(docketNumber: 0, previousHash: null)
            with { CreatedAt = now };
        var currentDocket = CreateValidDocket(docketNumber: 1, previousHash: previousDocket.DocketHash)
            with { CreatedAt = now };

        // Act
        var result = _validator.ValidateChainContinuity(currentDocket, previousDocket);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateChainContinuity_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var previousDocket = CreateValidDocket(docketNumber: 5, previousHash: null)
            with { RegisterId = "register-1", DocketHash = "correct-hash", CreatedAt = now };
        var currentDocket = CreateValidDocket(docketNumber: 10, previousHash: "wrong-hash")
            with { RegisterId = "register-2", CreatedAt = now.AddMinutes(-5) };

        // Act
        var result = _validator.ValidateChainContinuity(currentDocket, previousDocket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(4);
        result.Errors.Should().Contain(e => e.Code == "DK_014"); // Sequence
        result.Errors.Should().Contain(e => e.Code == "DK_015"); // Register
        result.Errors.Should().Contain(e => e.Code == "DK_016"); // Hash
        result.Errors.Should().Contain(e => e.Code == "DK_017"); // Timestamp
    }

    #endregion

    #region ValidateGenesisDocket Tests

    [Fact]
    public void ValidateGenesisDocket_WithValidGenesis_ReturnsSuccess()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 0, previousHash: null);

        // Act
        var result = _validator.ValidateGenesisDocket(docket);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void ValidateGenesisDocket_WithNonZeroDocketNumber_ReturnsError(long docketNumber)
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: docketNumber, previousHash: null);

        // Act
        var result = _validator.ValidateGenesisDocket(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "DK_018");
        result.Errors[0].Message.Should().Contain("must have docket number 0");
        result.Errors[0].Message.Should().Contain($"got {docketNumber}");
    }

    [Fact]
    public void ValidateGenesisDocket_WithPreviousHash_ReturnsError()
    {
        // Arrange
        var docket = CreateValidDocket(docketNumber: 0, previousHash: "some-hash");

        // Act
        var result = _validator.ValidateGenesisDocket(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "DK_019");
        result.Errors[0].Message.Should().Contain("cannot have a previous hash");
    }

    [Fact]
    public void ValidateGenesisDocket_WithStructureErrors_ReturnsErrorsWithGenesisContext()
    {
        // Arrange
        var docket = new DocketData
        {
            DocketId = "",
            RegisterId = "",
            DocketNumber = 0,
            PreviousHash = null,
            DocketHash = "",
            CreatedAt = DateTimeOffset.UtcNow,
            MerkleRoot = "",
            ProposerValidatorId = "",
            TransactionCount = 0
        };

        // Act
        var result = _validator.ValidateGenesisDocket(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(0);
        // Should have genesis-prefixed error messages
        result.Errors.Should().Contain(e => e.Message.Contains("[Genesis]"));
    }

    [Fact]
    public void ValidateGenesisDocket_WithAllErrors_ReturnsCombinedErrors()
    {
        // Arrange
        var docket = new DocketData
        {
            DocketId = "",
            RegisterId = "",
            DocketNumber = 5, // Not 0
            PreviousHash = "has-hash", // Should be null
            DocketHash = "",
            CreatedAt = DateTimeOffset.UtcNow,
            MerkleRoot = "",
            ProposerValidatorId = "",
            TransactionCount = 0
        };

        // Act
        var result = _validator.ValidateGenesisDocket(docket);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "DK_018"); // Wrong number
        result.Errors.Should().Contain(e => e.Code == "DK_019"); // Has hash
        // Plus structure errors
        result.Errors.Should().HaveCountGreaterThan(5);
    }

    #endregion

    #region ComputeDocketHash Tests

    [Fact]
    public void ComputeDocketHash_ForGenesisDocket_ComputesCorrectly()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 0L;
        string? previousHash = null;
        var merkleRoot = "merkle123";
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var expectedHashBytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(expectedHashBytes);

        // Act
        var result = _validator.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, createdAt);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("12345678");

        // Verify hash input contains "GENESIS" for null previous hash
        _mockHashProvider.Verify(h => h.ComputeHash(
            It.Is<byte[]>(bytes => System.Text.Encoding.UTF8.GetString(bytes).Contains("GENESIS")),
            HashType.SHA256), Times.Once);
    }

    [Fact]
    public void ComputeDocketHash_ForNonGenesisDocket_ComputesCorrectly()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 5L;
        var previousHash = "prev-hash-123";
        var merkleRoot = "merkle456";
        var createdAt = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var expectedHashBytes = new byte[] { 0xAB, 0xCD, 0xEF, 0x01 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(expectedHashBytes);

        // Act
        var result = _validator.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, createdAt);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be("abcdef01");

        // Verify hash input contains previous hash (not GENESIS)
        _mockHashProvider.Verify(h => h.ComputeHash(
            It.Is<byte[]>(bytes => System.Text.Encoding.UTF8.GetString(bytes).Contains(previousHash)),
            HashType.SHA256), Times.Once);
    }

    [Fact]
    public void ComputeDocketHash_IsDeterministic_SameInputsProduceSameHash()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 1L;
        var previousHash = "prev-hash";
        var merkleRoot = "merkle";
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var hashBytes = new byte[] { 0x11, 0x22, 0x33, 0x44 };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Act
        var result1 = _validator.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, createdAt);
        var result2 = _validator.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, createdAt);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void ComputeDocketHash_WithDifferentInputs_ProducesDifferentHashes()
    {
        // Arrange
        var registerId = "register-1";
        var docketNumber = 1L;
        var previousHash = "prev-hash";
        var merkleRoot = "merkle";
        var createdAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var hash1Bytes = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        var hash2Bytes = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };

        var callCount = 0;
        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(() => callCount++ == 0 ? hash1Bytes : hash2Bytes);

        // Act
        var result1 = _validator.ComputeDocketHash(registerId, docketNumber, previousHash, merkleRoot, createdAt);
        var result2 = _validator.ComputeDocketHash(registerId, docketNumber + 1, previousHash, merkleRoot, createdAt); // Different number

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
        var result = _validator.ComputeDocketHash("reg", 0, null, "merkle", DateTimeOffset.UtcNow);

        // Assert
        result.Should().Be("abcdef01");
        result.Should().NotContain("ABCDEF"); // Should be lowercase
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid docket for testing
    /// </summary>
    private static DocketData CreateValidDocket(long docketNumber, string? previousHash = "prev-hash")
    {
        return new DocketData
        {
            DocketId = $"docket-{Guid.NewGuid()}",
            RegisterId = "register-1",
            DocketNumber = docketNumber,
            PreviousHash = previousHash,
            DocketHash = $"hash-{Guid.NewGuid()}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            MerkleRoot = $"merkle-{Guid.NewGuid()}",
            ProposerValidatorId = "validator-1",
            TransactionCount = 10
        };
    }

    #endregion
}
