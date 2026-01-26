// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Models;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using ActionModel = Sorcha.Blueprint.Models.Action;

namespace Sorcha.Validator.Service.Tests.Services;

public class ValidationEngineTests
{
    private readonly Mock<IBlueprintCache> _blueprintCacheMock;
    private readonly Mock<IHashProvider> _hashProviderMock;
    private readonly Mock<ICryptoModule> _cryptoModuleMock;
    private readonly Mock<ILogger<ValidationEngine>> _loggerMock;
    private readonly ValidationEngineConfiguration _config;
    private readonly ValidationEngine _engine;

    public ValidationEngineTests()
    {
        _blueprintCacheMock = new Mock<IBlueprintCache>();
        _hashProviderMock = new Mock<IHashProvider>();
        _cryptoModuleMock = new Mock<ICryptoModule>();
        _loggerMock = new Mock<ILogger<ValidationEngine>>();

        _config = new ValidationEngineConfiguration
        {
            EnableSchemaValidation = true,
            EnableSignatureVerification = true,
            EnableChainValidation = true,
            EnableParallelValidation = false,
            MaxClockSkew = TimeSpan.FromMinutes(5),
            MaxTransactionAge = TimeSpan.FromHours(1)
        };

        _engine = new ValidationEngine(
            Options.Create(_config),
            _blueprintCacheMock.Object,
            _hashProviderMock.Object,
            _cryptoModuleMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        var act = () => new ValidationEngine(
            null!,
            _blueprintCacheMock.Object,
            _hashProviderMock.Object,
            _cryptoModuleMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithNullBlueprintCache_ThrowsArgumentNullException()
    {
        var act = () => new ValidationEngine(
            Options.Create(_config),
            null!,
            _hashProviderMock.Object,
            _cryptoModuleMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("blueprintCache");
    }

    [Fact]
    public void Constructor_WithNullHashProvider_ThrowsArgumentNullException()
    {
        var act = () => new ValidationEngine(
            Options.Create(_config),
            _blueprintCacheMock.Object,
            null!,
            _cryptoModuleMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("hashProvider");
    }

    [Fact]
    public void Constructor_WithNullCryptoModule_ThrowsArgumentNullException()
    {
        var act = () => new ValidationEngine(
            Options.Create(_config),
            _blueprintCacheMock.Object,
            _hashProviderMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cryptoModule");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ValidationEngine(
            Options.Create(_config),
            _blueprintCacheMock.Object,
            _hashProviderMock.Object,
            _cryptoModuleMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void ValidateStructure_WithValidTransaction_ReturnsSuccess()
    {
        // Arrange
        var tx = CreateValidTransaction();

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateStructure_WithMissingTransactionId_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(transactionId: "");

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_001");
    }

    [Fact]
    public void ValidateStructure_WithMissingRegisterId_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(registerId: "");

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_002");
    }

    [Fact]
    public void ValidateStructure_WithMissingBlueprintId_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(blueprintId: "");

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_003");
    }

    [Fact]
    public void ValidateStructure_WithMissingSignatures_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(signatures: []);

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_007");
    }

    [Fact]
    public void ValidateStructure_WithMissingActionId_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(actionId: "");

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_004");
    }

    [Fact]
    public void ValidateStructure_WithMissingPayloadHash_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(payloadHash: "");

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_006");
    }

    [Fact]
    public void ValidateStructure_WithSignatureMissingPublicKey_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(signatures:
        [
            new Signature
            {
                PublicKey = [],
                SignatureValue = new byte[64],
                Algorithm = "ED25519",
                SignedAt = DateTimeOffset.UtcNow
            }
        ]);

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_008");
    }

    [Fact]
    public void ValidateStructure_WithSignatureMissingSignatureValue_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(signatures:
        [
            new Signature
            {
                PublicKey = new byte[32],
                SignatureValue = [],
                Algorithm = "ED25519",
                SignedAt = DateTimeOffset.UtcNow
            }
        ]);

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_009");
    }

    [Fact]
    public void ValidateStructure_WithSignatureMissingAlgorithm_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(signatures:
        [
            new Signature
            {
                PublicKey = new byte[32],
                SignatureValue = new byte[64],
                Algorithm = "",
                SignedAt = DateTimeOffset.UtcNow
            }
        ]);

        // Act
        var result = _engine.ValidateStructure(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_STRUCT_010");
    }

    [Fact]
    public void ValidateStructure_WithNullTransaction_ThrowsArgumentNullException()
    {
        var act = () => _engine.ValidateStructure(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("transaction");
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithValidTransaction_ReturnsSuccess()
    {
        // Arrange
        var tx = CreateValidTransaction();
        SetupSuccessfulValidation(tx);

        // Act
        var result = await _engine.ValidateTransactionAsync(tx);

        // Assert
        result.IsValid.Should().BeTrue();
        result.TransactionId.Should().Be(tx.TransactionId);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithInvalidPayloadHash_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(payloadHash: "invalid_hash");

        // Setup hash computation to return different hash
        _hashProviderMock.Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns([0x01, 0x02, 0x03]);

        // Act
        var result = await _engine.ValidateTransactionAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_HASH_001");
    }

    [Fact]
    public async Task ValidateSchemaAsync_WhenBlueprintNotFound_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction();
        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(tx.BlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlueprintModel?)null);

        // Act
        var result = await _engine.ValidateSchemaAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_SCHEMA_001");
    }

    [Fact]
    public async Task ValidateSchemaAsync_WhenActionNotFound_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(actionId: "999");
        var blueprint = CreateTestBlueprint(tx.BlueprintId);

        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(tx.BlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        // Act
        var result = await _engine.ValidateSchemaAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_SCHEMA_003");
    }

    [Fact]
    public async Task ValidateBatchAsync_ValidatesAllTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateValidTransaction("tx-1"),
            CreateValidTransaction("tx-2"),
            CreateValidTransaction("tx-3")
        };

        foreach (var tx in transactions)
        {
            SetupSuccessfulValidation(tx);
        }

        // Act
        var results = await _engine.ValidateBatchAsync(transactions);

        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.IsValid);
    }

    [Fact]
    public async Task GetStats_ReturnsCorrectStatistics()
    {
        // Arrange - Validate some transactions using the async method which records stats
        var validTx = CreateValidTransaction();
        SetupSuccessfulValidation(validTx);
        await _engine.ValidateTransactionAsync(validTx);

        var invalidTx = CreateValidTransaction(transactionId: "");
        await _engine.ValidateTransactionAsync(invalidTx);

        // Act
        var stats = _engine.GetStats();

        // Assert
        stats.TotalValidated.Should().BeGreaterThan(0);
        stats.TotalSuccessful.Should().BeGreaterThan(0);
        stats.TotalFailed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithFutureTimestamp_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(createdAt: DateTimeOffset.UtcNow.AddHours(1));
        SetupSuccessfulValidation(tx);

        // Act
        var result = await _engine.ValidateTransactionAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_TIME_001");
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithExpiredTransaction_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(expiresAt: DateTimeOffset.UtcNow.AddHours(-1));
        SetupSuccessfulValidation(tx);

        // Act
        var result = await _engine.ValidateTransactionAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_TIME_003");
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithOldTransaction_ReturnsFailed()
    {
        // Arrange - transaction older than MaxTransactionAge
        var tx = CreateValidTransaction(createdAt: DateTimeOffset.UtcNow.AddHours(-2));
        SetupSuccessfulValidation(tx);

        // Act
        var result = await _engine.ValidateTransactionAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_TIME_002");
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithNullTransaction_ThrowsArgumentNullException()
    {
        var act = async () => await _engine.ValidateTransactionAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("transaction");
    }

    [Fact]
    public async Task VerifySignaturesAsync_WithValidSignature_ReturnsSuccess()
    {
        // Arrange
        var tx = CreateValidTransaction();
        _hashProviderMock.Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);
        _cryptoModuleMock.Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoStatus.Success);

        // Act
        var result = await _engine.VerifySignaturesAsync(tx);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifySignaturesAsync_WithInvalidSignature_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction();
        _hashProviderMock.Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);
        _cryptoModuleMock.Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoStatus.InvalidSignature);

        // Act
        var result = await _engine.VerifySignaturesAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_SIG_002");
    }

    [Fact]
    public async Task VerifySignaturesAsync_WithUnsupportedAlgorithm_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(signatures:
        [
            new Signature
            {
                PublicKey = new byte[32],
                SignatureValue = new byte[64],
                Algorithm = "UNKNOWN_ALGO",
                SignedAt = DateTimeOffset.UtcNow
            }
        ]);
        _hashProviderMock.Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        // Act
        var result = await _engine.VerifySignaturesAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_SIG_001");
    }

    [Fact]
    public async Task ValidateSchemaAsync_WithInvalidActionIdFormat_ReturnsFailed()
    {
        // Arrange
        var tx = CreateValidTransaction(actionId: "not-a-number");
        var blueprint = CreateTestBlueprint(tx.BlueprintId);

        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(tx.BlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        // Act
        var result = await _engine.ValidateSchemaAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_SCHEMA_002");
    }

    [Fact]
    public async Task ValidateBatchAsync_WithEmptyList_ReturnsEmptyResults()
    {
        // Arrange
        var transactions = new List<Transaction>();

        // Act
        var results = await _engine.ValidateBatchAsync(transactions);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateBatchAsync_WithNullTransactions_ThrowsArgumentNullException()
    {
        var act = async () => await _engine.ValidateBatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("transactions");
    }

    [Fact]
    public async Task ValidateBatchAsync_WithMixedResults_ReturnsAllResults()
    {
        // Arrange
        var validTx = CreateValidTransaction("valid-tx");
        var invalidTx = CreateValidTransaction("invalid-tx", payloadHash: "bad_hash");
        var transactions = new List<Transaction> { validTx, invalidTx };

        SetupSuccessfulValidation(validTx);
        _hashProviderMock.Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns((byte[] data, HashType _) =>
            {
                // Return correct hash only for valid tx
                var payloadHash = Convert.FromHexString(validTx.PayloadHash);
                return payloadHash;
            });

        // Act
        var results = await _engine.ValidateBatchAsync(transactions);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.TransactionId == "valid-tx" && r.IsValid);
        results.Should().Contain(r => r.TransactionId == "invalid-tx" && !r.IsValid);
    }

    [Fact]
    public async Task ValidateTransactionAsync_WithStructureErrors_DoesNotContinueValidation()
    {
        // Arrange - Transaction with empty ID should fail structure validation
        var tx = CreateValidTransaction(transactionId: "");

        // Act
        var result = await _engine.ValidateTransactionAsync(tx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Category == ValidationErrorCategory.Structure);
        // Blueprint cache should NOT be called because structure validation failed
        _blueprintCacheMock.Verify(
            c => c.GetBlueprintAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateChainAsync_ReturnsSuccess()
    {
        // Arrange
        var tx = CreateValidTransaction();

        // Act
        var result = await _engine.ValidateChainAsync(tx);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateChainAsync_WithNullTransaction_ThrowsArgumentNullException()
    {
        var act = async () => await _engine.ValidateChainAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("transaction");
    }

    [Fact]
    public void GetStats_InitialState_ReturnsZeroCounts()
    {
        // Act
        var stats = _engine.GetStats();

        // Assert
        stats.TotalValidated.Should().Be(0);
        stats.TotalSuccessful.Should().Be(0);
        stats.TotalFailed.Should().Be(0);
        stats.InProgress.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_SuccessRate_CalculatesCorrectly()
    {
        // Arrange - Need to process some transactions first
        var validTx = CreateValidTransaction();
        SetupSuccessfulValidation(validTx);
        await _engine.ValidateTransactionAsync(validTx);

        var invalidTx = CreateValidTransaction(transactionId: "");
        await _engine.ValidateTransactionAsync(invalidTx);

        // Act
        var stats = _engine.GetStats();

        // Assert
        stats.SuccessRate.Should().BeApproximately(0.5, 0.01);
    }

    #region Helper Methods

    private static Transaction CreateValidTransaction(
        string? transactionId = null,
        string? registerId = null,
        string? blueprintId = null,
        string? actionId = null,
        string? payloadHash = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? expiresAt = null,
        List<Signature>? signatures = null)
    {
        var payloadJson = "{}";
        var defaultPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; // SHA256 of {}

        return new Transaction
        {
            TransactionId = transactionId ?? $"tx-{Guid.NewGuid():N}",
            RegisterId = registerId ?? "test-register",
            BlueprintId = blueprintId ?? "bp-1",
            ActionId = actionId ?? "1",
            Payload = JsonSerializer.Deserialize<JsonElement>(payloadJson),
            PayloadHash = payloadHash ?? defaultPayloadHash,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            Signatures = signatures ??
            [
                new Signature
                {
                    PublicKey = new byte[32],
                    SignatureValue = new byte[64],
                    Algorithm = "ED25519",
                    SignedAt = DateTimeOffset.UtcNow
                }
            ]
        };
    }

    private void SetupSuccessfulValidation(Transaction tx)
    {
        // Setup hash provider to return matching hash
        var hashBytes = Convert.FromHexString(tx.PayloadHash);
        _hashProviderMock.Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(hashBytes);

        // Setup blueprint cache
        var blueprint = CreateTestBlueprint(tx.BlueprintId);
        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(tx.BlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        // Setup signature verification
        _cryptoModuleMock.Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoStatus.Success);
    }

    private static BlueprintModel CreateTestBlueprint(string blueprintId)
    {
        return new BlueprintModel
        {
            Id = blueprintId,
            Title = "Test Blueprint",
            Participants =
            [
                new Sorcha.Blueprint.Models.Participant
                {
                    Id = "participant-1",
                    Name = "Test Participant"
                }
            ],
            Actions =
            [
                new ActionModel
                {
                    Id = 1,
                    Title = "Test Action",
                    Sender = "participant-1"
                }
            ]
        };
    }

    #endregion
}
