// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;
using ValidatorStatusEnum = Sorcha.Validator.Service.Services.Interfaces.ValidatorStatus;

namespace Sorcha.Validator.Service.Tests.Services;

public class ConsensusFailureHandlerTests
{
    private readonly Mock<IOptions<ConsensusConfiguration>> _consensusConfigMock;
    private readonly Mock<ISignatureCollector> _signatureCollectorMock;
    private readonly Mock<IValidatorRegistry> _validatorRegistryMock;
    private readonly Mock<IGenesisConfigService> _genesisConfigServiceMock;
    private readonly Mock<IMemPoolManager> _memPoolManagerMock;
    private readonly Mock<IPendingDocketStore> _pendingDocketStoreMock;
    private readonly Mock<ILogger<ConsensusFailureHandler>> _loggerMock;
    private readonly ConsensusFailureHandler _handler;

    public ConsensusFailureHandlerTests()
    {
        _consensusConfigMock = new Mock<IOptions<ConsensusConfiguration>>();
        _signatureCollectorMock = new Mock<ISignatureCollector>();
        _validatorRegistryMock = new Mock<IValidatorRegistry>();
        _genesisConfigServiceMock = new Mock<IGenesisConfigService>();
        _memPoolManagerMock = new Mock<IMemPoolManager>();
        _pendingDocketStoreMock = new Mock<IPendingDocketStore>();
        _loggerMock = new Mock<ILogger<ConsensusFailureHandler>>();

        _consensusConfigMock.Setup(x => x.Value).Returns(new ConsensusConfiguration
        {
            VoteTimeout = TimeSpan.FromSeconds(5),
            MaxRetries = 3,
            ApprovalThreshold = 0.67
        });

        _handler = new ConsensusFailureHandler(
            _consensusConfigMock.Object,
            _signatureCollectorMock.Object,
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            _memPoolManagerMock.Object,
            _pendingDocketStoreMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullConsensusConfig_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ConsensusFailureHandler(
            null!,
            _signatureCollectorMock.Object,
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            _memPoolManagerMock.Object,
            _pendingDocketStoreMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("consensusConfig");
    }

    [Fact]
    public void Constructor_NullSignatureCollector_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ConsensusFailureHandler(
            _consensusConfigMock.Object,
            null!,
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            _memPoolManagerMock.Object,
            _pendingDocketStoreMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("signatureCollector");
    }

    [Fact]
    public void Constructor_NullValidatorRegistry_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ConsensusFailureHandler(
            _consensusConfigMock.Object,
            _signatureCollectorMock.Object,
            null!,
            _genesisConfigServiceMock.Object,
            _memPoolManagerMock.Object,
            _pendingDocketStoreMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validatorRegistry");
    }

    [Fact]
    public void Constructor_NullGenesisConfigService_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ConsensusFailureHandler(
            _consensusConfigMock.Object,
            _signatureCollectorMock.Object,
            _validatorRegistryMock.Object,
            null!,
            _memPoolManagerMock.Object,
            _pendingDocketStoreMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("genesisConfigService");
    }

    [Fact]
    public void Constructor_NullMemPoolManager_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ConsensusFailureHandler(
            _consensusConfigMock.Object,
            _signatureCollectorMock.Object,
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            null!,
            _pendingDocketStoreMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("memPoolManager");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ConsensusFailureHandler(
            _consensusConfigMock.Object,
            _signatureCollectorMock.Object,
            _validatorRegistryMock.Object,
            _genesisConfigServiceMock.Object,
            _memPoolManagerMock.Object,
            _pendingDocketStoreMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region HandleFailureAsync Tests

    [Fact]
    public async Task HandleFailureAsync_NullDocket_ThrowsArgumentNullException()
    {
        // Arrange
        var result = CreateSignatureCollectionResult(thresholdMet: false);

        // Act
        var act = () => _handler.HandleFailureAsync(null!, result);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("docket");
    }

    [Fact]
    public async Task HandleFailureAsync_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var docket = CreateDocket();

        // Act
        var act = () => _handler.HandleFailureAsync(docket, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public async Task HandleFailureAsync_ThresholdAlreadyMet_ReturnsNoActionNeeded()
    {
        // Arrange
        var docket = CreateDocket();
        var result = CreateSignatureCollectionResult(thresholdMet: true);

        // Act
        var recovery = await _handler.HandleFailureAsync(docket, result);

        // Assert
        recovery.Action.Should().Be(ConsensusRecoveryAction.NoActionNeeded);
        recovery.Succeeded.Should().BeTrue();
        recovery.RetryAttempts.Should().Be(0);
    }

    [Fact]
    public async Task HandleFailureAsync_MaxRetriesExceeded_ReturnsAbandon()
    {
        // Arrange
        var docket = CreateDocket(retryCount: 5);
        var result = CreateSignatureCollectionResult(thresholdMet: false);

        // Act
        var recovery = await _handler.HandleFailureAsync(docket, result);

        // Assert
        recovery.Action.Should().Be(ConsensusRecoveryAction.Abandon);
        recovery.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleFailureAsync_MaxRetriesExceeded_ReturnsTransactionsToPool()
    {
        // Arrange
        var docket = CreateDocket(retryCount: 5, transactionCount: 3);
        var result = CreateSignatureCollectionResult(thresholdMet: false);

        _memPoolManagerMock
            .Setup(x => x.ReturnTransactionsAsync(
                It.IsAny<string>(),
                It.IsAny<List<Transaction>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var recovery = await _handler.HandleFailureAsync(docket, result);

        // Assert
        recovery.TransactionsReturnedToPool.Should().Be(3);
        _memPoolManagerMock.Verify(
            x => x.ReturnTransactionsAsync(
                docket.RegisterId,
                It.IsAny<List<Transaction>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleFailureAsync_FirstRetry_AttemptsRetry()
    {
        // Arrange
        var docket = CreateDocket(retryCount: 0);
        var failedResult = CreateSignatureCollectionResult(thresholdMet: false);
        var retryResult = CreateSignatureCollectionResult(thresholdMet: true);

        SetupRetryMocks(retryResult);

        // Act
        var recovery = await _handler.HandleFailureAsync(docket, failedResult);

        // Assert
        recovery.Action.Should().Be(ConsensusRecoveryAction.Retry);
        recovery.RetryAttempts.Should().Be(1);
    }

    [Fact]
    public async Task HandleFailureAsync_RetrySucceeds_ReturnsSuccess()
    {
        // Arrange
        var docket = CreateDocket(retryCount: 0);
        var failedResult = CreateSignatureCollectionResult(thresholdMet: false);
        var retryResult = CreateSignatureCollectionResult(thresholdMet: true);

        SetupRetryMocks(retryResult);

        // Act
        var recovery = await _handler.HandleFailureAsync(docket, failedResult);

        // Assert
        recovery.Succeeded.Should().BeTrue();
        recovery.UpdatedDocket.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleFailureAsync_RetryFails_ReturnsFailedRetry()
    {
        // Arrange
        var docket = CreateDocket(retryCount: 0);
        var failedResult = CreateSignatureCollectionResult(thresholdMet: false, approvals: 1);
        var retryResult = CreateSignatureCollectionResult(thresholdMet: false, approvals: 2);

        SetupRetryMocks(retryResult);

        // Act
        var recovery = await _handler.HandleFailureAsync(docket, failedResult);

        // Assert
        recovery.Succeeded.Should().BeFalse();
        recovery.RetryAttempts.Should().Be(1);
    }

    [Fact]
    public async Task HandleFailureAsync_RecordsRecoveryDuration()
    {
        // Arrange
        var docket = CreateDocket();
        var result = CreateSignatureCollectionResult(thresholdMet: true);

        // Act
        var recovery = await _handler.HandleFailureAsync(docket, result);

        // Assert
        recovery.RecoveryDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    #endregion

    #region AbandonDocketAsync Tests

    [Fact]
    public async Task AbandonDocketAsync_NullDocket_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _handler.AbandonDocketAsync(null!, "test reason");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("docket");
    }

    [Fact]
    public async Task AbandonDocketAsync_NullReason_ThrowsArgumentException()
    {
        // Arrange
        var docket = CreateDocket();

        // Act
        var act = () => _handler.AbandonDocketAsync(docket, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("reason");
    }

    [Fact]
    public async Task AbandonDocketAsync_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var docket = CreateDocket();

        // Act
        var act = () => _handler.AbandonDocketAsync(docket, "  ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("reason");
    }

    [Fact]
    public async Task AbandonDocketAsync_ValidInput_SetsDocketStatusToRejected()
    {
        // Arrange
        var docket = CreateDocket();

        // Act
        await _handler.AbandonDocketAsync(docket, "Max retries exceeded");

        // Assert
        docket.Status.Should().Be(DocketStatus.Rejected);
    }

    #endregion

    #region ReturnTransactionsToPoolAsync Tests

    [Fact]
    public async Task ReturnTransactionsToPoolAsync_NullDocket_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _handler.ReturnTransactionsToPoolAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("docket");
    }

    [Fact]
    public async Task ReturnTransactionsToPoolAsync_NoTransactions_ReturnsZero()
    {
        // Arrange
        var docket = CreateDocket(transactionCount: 0);

        // Act
        var count = await _handler.ReturnTransactionsToPoolAsync(docket);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task ReturnTransactionsToPoolAsync_WithTransactions_ReturnsAll()
    {
        // Arrange
        var docket = CreateDocket(transactionCount: 5);

        _memPoolManagerMock
            .Setup(x => x.ReturnTransactionsAsync(
                It.IsAny<string>(),
                It.IsAny<List<Transaction>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var count = await _handler.ReturnTransactionsToPoolAsync(docket);

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public async Task ReturnTransactionsToPoolAsync_PoolFails_ReturnsZero()
    {
        // Arrange
        var docket = CreateDocket(transactionCount: 3);

        _memPoolManagerMock
            .Setup(x => x.ReturnTransactionsAsync(
                It.IsAny<string>(),
                It.IsAny<List<Transaction>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Pool error"));

        // Act
        var count = await _handler.ReturnTransactionsToPoolAsync(docket);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    #region GetStats Tests

    [Fact]
    public void GetStats_InitialState_ReturnsZeroCounts()
    {
        // Act
        var stats = _handler.GetStats();

        // Assert
        stats.TotalFailures.Should().Be(0);
        stats.SuccessfulRecoveries.Should().Be(0);
        stats.DocketsAbandoned.Should().Be(0);
        stats.TotalRetryAttempts.Should().Be(0);
        stats.TransactionsReturnedToPool.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_AfterFailure_IncrementsTotalFailures()
    {
        // Arrange
        var docket = CreateDocket();
        var result = CreateSignatureCollectionResult(thresholdMet: true);

        // Act
        await _handler.HandleFailureAsync(docket, result);
        var stats = _handler.GetStats();

        // Assert
        stats.TotalFailures.Should().Be(1);
    }

    [Fact]
    public void GetStats_RecoveryRate_ReturnsZeroWhenNoFailures()
    {
        // Act
        var stats = _handler.GetStats();

        // Assert
        stats.RecoveryRate.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private void SetupRetryMocks(SignatureCollectionResult retryResult)
    {
        _validatorRegistryMock
            .Setup(x => x.GetActiveValidatorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>
            {
                new()
                {
                    ValidatorId = "validator-1",
                    PublicKey = "pubkey-1",
                    GrpcEndpoint = "https://validator-1:5000",
                    RegisteredAt = DateTimeOffset.UtcNow,
                    Status = ValidatorStatusEnum.Active
                }
            });

        _genesisConfigServiceMock
            .Setup(x => x.GetConsensusConfigAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConsensusConfig
            {
                SignatureThresholdMin = 1,
                SignatureThresholdMax = 5,
                MaxSignaturesPerDocket = 10,
                MaxTransactionsPerDocket = 100,
                DocketTimeout = TimeSpan.FromSeconds(5),
                DocketBuildInterval = TimeSpan.FromSeconds(10)
            });

        _signatureCollectorMock
            .Setup(x => x.CollectSignaturesAsync(
                It.IsAny<Docket>(),
                It.IsAny<ConsensusConfig>(),
                It.IsAny<IReadOnlyList<ValidatorInfo>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(retryResult);
    }

    private static Docket CreateDocket(int retryCount = 0, int transactionCount = 0)
    {
        var transactions = new List<Transaction>();
        for (var i = 0; i < transactionCount; i++)
        {
            transactions.Add(CreateTransaction($"tx-{i}"));
        }

        var metadata = new Dictionary<string, string>();
        if (retryCount > 0)
        {
            metadata["retryCount"] = retryCount.ToString();
        }

        return new Docket
        {
            DocketId = $"docket-{Guid.NewGuid():N}",
            RegisterId = "register-1",
            DocketNumber = 1,
            DocketHash = "test-hash",
            ProposerValidatorId = "validator-1",
            ProposerSignature = new Signature
            {
                PublicKey = "test-key"u8.ToArray(),
                SignatureValue = "test-sig"u8.ToArray(),
                Algorithm = "ED25519",
                SignedAt = DateTimeOffset.UtcNow,
                SignedBy = "validator-1"
            },
            MerkleRoot = "test-merkle-root",
            Status = DocketStatus.Proposed,
            CreatedAt = DateTimeOffset.UtcNow,
            Transactions = transactions,
            Metadata = metadata
        };
    }

    private static Transaction CreateTransaction(string transactionId)
    {
        return new Transaction
        {
            TransactionId = transactionId,
            RegisterId = "register-1",
            BlueprintId = "blueprint-1",
            ActionId = "action-1",
            Payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement,
            PayloadHash = $"payload-hash-{transactionId}",
            Signatures = new List<Signature>
            {
                new()
                {
                    PublicKey = "public-key"u8.ToArray(),
                    SignatureValue = "signature-value"u8.ToArray(),
                    Algorithm = "ED25519",
                    SignedAt = DateTimeOffset.UtcNow
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            Priority = TransactionPriority.Normal
        };
    }

    private static SignatureCollectionResult CreateSignatureCollectionResult(
        bool thresholdMet,
        int approvals = 3,
        int totalValidators = 5)
    {
        return new SignatureCollectionResult
        {
            Signatures = [],
            ThresholdMet = thresholdMet,
            TimedOut = false,
            TotalValidators = totalValidators,
            ResponsesReceived = approvals,
            Approvals = approvals,
            Rejections = 0,
            Duration = TimeSpan.FromMilliseconds(100),
            NonResponders = [],
            RejectionDetails = new Dictionary<string, string>()
        };
    }

    #endregion
}
