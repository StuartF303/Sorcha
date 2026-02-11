// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Models;
using Sorcha.Register.Models;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;
using Sorcha.Register.Models.Enums;
using Sorcha.ServiceClients.Register;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;
using Xunit;

namespace Sorcha.Validator.Service.Tests.Services;

public class BlueprintVersionResolverTests
{
    private readonly Mock<IRegisterServiceClient> _registerClientMock;
    private readonly Mock<IBlueprintCache> _blueprintCacheMock;
    private readonly Mock<ILogger<BlueprintVersionResolver>> _loggerMock;
    private readonly BlueprintVersionResolver _resolver;

    public BlueprintVersionResolverTests()
    {
        _registerClientMock = new Mock<IRegisterServiceClient>();
        _blueprintCacheMock = new Mock<IBlueprintCache>();
        _loggerMock = new Mock<ILogger<BlueprintVersionResolver>>();

        _resolver = new BlueprintVersionResolver(
            _registerClientMock.Object,
            _blueprintCacheMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRegisterClient_ThrowsArgumentNullException()
    {
        var act = () => new BlueprintVersionResolver(
            null!,
            _blueprintCacheMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("registerClient");
    }

    [Fact]
    public void Constructor_WithNullBlueprintCache_ThrowsArgumentNullException()
    {
        var act = () => new BlueprintVersionResolver(
            _registerClientMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("blueprintCache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new BlueprintVersionResolver(
            _registerClientMock.Object,
            _blueprintCacheMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region ResolveForActionAsync Tests

    [Fact]
    public async Task ResolveForActionAsync_WithDirectBlueprintReference_ReturnsVersion()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";
        var blueprintTxId = "bp-tx-001";
        var actionPrevTxId = blueprintTxId; // Action directly references blueprint

        var blueprintTx = CreateBlueprintPublicationTransaction(blueprintTxId, blueprintId);
        var blueprint = CreateTestBlueprint(blueprintId);

        _registerClientMock.Setup(c => c.GetTransactionAsync(registerId, blueprintTxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprintTx);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 1,
                Transactions = [blueprintTx]
            });

        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        // Act
        var result = await _resolver.ResolveForActionAsync(registerId, blueprintId, actionPrevTxId);

        // Assert
        result.Should().NotBeNull();
        result!.BlueprintId.Should().Be(blueprintId);
        result.PublicationTransactionId.Should().Be(blueprintTxId);
        result.Blueprint.Should().Be(blueprint);
    }

    [Fact]
    public async Task ResolveForActionAsync_WithChainedActions_FollowsChain()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";
        var blueprintTxId = "bp-tx-001";
        var action0TxId = "action-0-tx";
        var action1TxId = "action-1-tx";

        var blueprintTx = CreateBlueprintPublicationTransaction(blueprintTxId, blueprintId);
        var action0Tx = CreateActionTransaction(action0TxId, blueprintId, blueprintTxId, actionId: 0);
        var action1Tx = CreateActionTransaction(action1TxId, blueprintId, action0TxId, actionId: 1);
        var blueprint = CreateTestBlueprint(blueprintId);

        // Action 1's previousId is Action 0's txId
        _registerClientMock.Setup(c => c.GetTransactionAsync(registerId, action0TxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(action0Tx);

        // Action 0's previousId is Blueprint's txId
        _registerClientMock.Setup(c => c.GetTransactionAsync(registerId, blueprintTxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprintTx);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 1,
                Transactions = [blueprintTx]
            });

        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        // Act - Resolve from action1's previousId (which is action0)
        var result = await _resolver.ResolveForActionAsync(registerId, blueprintId, action0TxId);

        // Assert
        result.Should().NotBeNull();
        result!.BlueprintId.Should().Be(blueprintId);
        result.PublicationTransactionId.Should().Be(blueprintTxId);
    }

    [Fact]
    public async Task ResolveForActionAsync_WithMissingTransaction_ReturnsNull()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";
        var missingTxId = "missing-tx";

        _registerClientMock.Setup(c => c.GetTransactionAsync(registerId, missingTxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionModel?)null);

        // Act
        var result = await _resolver.ResolveForActionAsync(registerId, blueprintId, missingTxId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveForActionAsync_WithEmptyParameters_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _resolver.ResolveForActionAsync("", "bp-1", "tx-1"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _resolver.ResolveForActionAsync("reg-1", "", "tx-1"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _resolver.ResolveForActionAsync("reg-1", "bp-1", ""));
    }

    #endregion

    #region GetVersionHistoryAsync Tests

    [Fact]
    public async Task GetVersionHistoryAsync_WithMultipleVersions_ReturnsOrderedList()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";

        var v1Tx = CreateBlueprintPublicationTransaction("bp-v1-tx", blueprintId, prevTxId: "", timestamp: DateTime.UtcNow.AddHours(-2));
        var v2Tx = CreateBlueprintPublicationTransaction("bp-v2-tx", blueprintId, prevTxId: "bp-v1-tx", timestamp: DateTime.UtcNow.AddHours(-1));
        var v3Tx = CreateBlueprintPublicationTransaction("bp-v3-tx", blueprintId, prevTxId: "bp-v2-tx", timestamp: DateTime.UtcNow);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 3,
                Transactions = [v1Tx, v2Tx, v3Tx]
            });

        // Act
        var result = await _resolver.GetVersionHistoryAsync(registerId, blueprintId);

        // Assert
        result.Should().HaveCount(3);
        result[0].VersionNumber.Should().Be(1);
        result[0].PublicationTransactionId.Should().Be("bp-v1-tx");
        result[0].IsLatest.Should().BeFalse();

        result[1].VersionNumber.Should().Be(2);
        result[1].PublicationTransactionId.Should().Be("bp-v2-tx");
        result[1].IsLatest.Should().BeFalse();

        result[2].VersionNumber.Should().Be(3);
        result[2].PublicationTransactionId.Should().Be("bp-v3-tx");
        result[2].IsLatest.Should().BeTrue();
    }

    [Fact]
    public async Task GetVersionHistoryAsync_WithNoVersions_ReturnsEmptyList()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-nonexistent";

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 0,
                Transactions = []
            });

        // Act
        var result = await _resolver.GetVersionHistoryAsync(registerId, blueprintId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVersionHistoryAsync_CachesResult()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";
        var v1Tx = CreateBlueprintPublicationTransaction("bp-v1-tx", blueprintId);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 1,
                Transactions = [v1Tx]
            });

        // Act - Call twice
        var result1 = await _resolver.GetVersionHistoryAsync(registerId, blueprintId);
        var result2 = await _resolver.GetVersionHistoryAsync(registerId, blueprintId);

        // Assert - Should only call register client once due to caching
        _registerClientMock.Verify(
            c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()),
            Times.Once);

        result1.Should().BeEquivalentTo(result2);
    }

    #endregion

    #region GetLatestVersionAsync Tests

    [Fact]
    public async Task GetLatestVersionAsync_WithVersions_ReturnsLatest()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";

        var v1Tx = CreateBlueprintPublicationTransaction("bp-v1-tx", blueprintId, prevTxId: "", timestamp: DateTime.UtcNow.AddHours(-1));
        var v2Tx = CreateBlueprintPublicationTransaction("bp-v2-tx", blueprintId, prevTxId: "bp-v1-tx", timestamp: DateTime.UtcNow);
        var blueprint = CreateTestBlueprint(blueprintId);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 2,
                Transactions = [v1Tx, v2Tx]
            });

        _registerClientMock.Setup(c => c.GetTransactionAsync(registerId, "bp-v2-tx", It.IsAny<CancellationToken>()))
            .ReturnsAsync(v2Tx);

        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        // Act
        var result = await _resolver.GetLatestVersionAsync(registerId, blueprintId);

        // Assert
        result.Should().NotBeNull();
        result!.VersionNumber.Should().Be(2);
        result.PublicationTransactionId.Should().Be("bp-v2-tx");
        result.IsLatest.Should().BeTrue();
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithNoVersions_ReturnsNull()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-nonexistent";

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 0,
                Transactions = []
            });

        // Act
        var result = await _resolver.GetLatestVersionAsync(registerId, blueprintId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetVersionAsOfAsync Tests

    [Fact]
    public async Task GetVersionAsOfAsync_WithValidTime_ReturnsCorrectVersion()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";
        var baseTime = DateTime.UtcNow.AddHours(-3);

        var v1Tx = CreateBlueprintPublicationTransaction("bp-v1-tx", blueprintId, prevTxId: "", timestamp: baseTime);
        var v2Tx = CreateBlueprintPublicationTransaction("bp-v2-tx", blueprintId, prevTxId: "bp-v1-tx", timestamp: baseTime.AddHours(1));
        var v3Tx = CreateBlueprintPublicationTransaction("bp-v3-tx", blueprintId, prevTxId: "bp-v2-tx", timestamp: baseTime.AddHours(2));
        var blueprint = CreateTestBlueprint(blueprintId);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 3,
                Transactions = [v1Tx, v2Tx, v3Tx]
            });

        _registerClientMock.Setup(c => c.GetTransactionAsync(registerId, "bp-v2-tx", It.IsAny<CancellationToken>()))
            .ReturnsAsync(v2Tx);

        _blueprintCacheMock.Setup(c => c.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blueprint);

        // Act - Get version as of 90 minutes after base (should be v2)
        var asOfTime = new DateTimeOffset(baseTime.AddMinutes(90), TimeSpan.Zero);
        var result = await _resolver.GetVersionAsOfAsync(registerId, blueprintId, asOfTime);

        // Assert
        result.Should().NotBeNull();
        result!.VersionNumber.Should().Be(2);
        result.PublicationTransactionId.Should().Be("bp-v2-tx");
    }

    [Fact]
    public async Task GetVersionAsOfAsync_BeforeFirstVersion_ReturnsNull()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";

        var v1Tx = CreateBlueprintPublicationTransaction("bp-v1-tx", blueprintId, timestamp: DateTime.UtcNow);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 1,
                Transactions = [v1Tx]
            });

        // Act - Query for time before the first version
        var asOfTime = new DateTimeOffset(DateTime.UtcNow.AddHours(-1), TimeSpan.Zero);
        var result = await _resolver.GetVersionAsOfAsync(registerId, blueprintId, asOfTime);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task InvalidateCacheForBlueprint_ClearsSpecificBlueprintCache()
    {
        // Arrange
        var registerId = "test-register";
        var blueprintId = "bp-1";
        var v1Tx = CreateBlueprintPublicationTransaction("bp-v1-tx", blueprintId);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 1,
                Transactions = [v1Tx]
            });

        // Populate cache
        await _resolver.GetVersionHistoryAsync(registerId, blueprintId);

        // Act - Invalidate cache
        _resolver.InvalidateCacheForBlueprint(registerId, blueprintId);

        // Call again
        await _resolver.GetVersionHistoryAsync(registerId, blueprintId);

        // Assert - Should have called twice (once before, once after invalidation)
        _registerClientMock.Verify(
            c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ClearCache_ClearsAllCaches()
    {
        // Arrange
        var registerId = "test-register";
        var blueprint1Id = "bp-1";
        var blueprint2Id = "bp-2";

        var bp1Tx = CreateBlueprintPublicationTransaction("bp1-tx", blueprint1Id);
        var bp2Tx = CreateBlueprintPublicationTransaction("bp2-tx", blueprint2Id);

        _registerClientMock.Setup(c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPage
            {
                Page = 1,
                PageSize = 100,
                Total = 2,
                Transactions = [bp1Tx, bp2Tx]
            });

        // Populate caches
        await _resolver.GetVersionHistoryAsync(registerId, blueprint1Id);
        await _resolver.GetVersionHistoryAsync(registerId, blueprint2Id);

        // Act - Clear all caches
        _resolver.ClearCache();

        // Call again for both
        await _resolver.GetVersionHistoryAsync(registerId, blueprint1Id);
        await _resolver.GetVersionHistoryAsync(registerId, blueprint2Id);

        // Assert - Should have called 4 times total (2 before, 2 after clear)
        _registerClientMock.Verify(
            c => c.GetTransactionsAsync(registerId, 1, 100, It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    #endregion

    #region Helper Methods

    private static TransactionModel CreateBlueprintPublicationTransaction(
        string txId,
        string blueprintId,
        string? prevTxId = null,
        DateTime? timestamp = null)
    {
        return new TransactionModel
        {
            TxId = txId,
            RegisterId = "test-register",
            PrevTxId = prevTxId ?? string.Empty,
            TimeStamp = timestamp ?? DateTime.UtcNow,
            SenderWallet = "sender-wallet",
            Signature = "signature",
            MetaData = new TransactionMetaData
            {
                BlueprintId = blueprintId,
                ActionId = null, // null ActionId indicates blueprint publication
                TransactionType = TransactionType.Control
            }
        };
    }

    private static TransactionModel CreateActionTransaction(
        string txId,
        string blueprintId,
        string prevTxId,
        uint actionId,
        DateTime? timestamp = null)
    {
        return new TransactionModel
        {
            TxId = txId,
            RegisterId = "test-register",
            PrevTxId = prevTxId,
            TimeStamp = timestamp ?? DateTime.UtcNow,
            SenderWallet = "sender-wallet",
            Signature = "signature",
            MetaData = new TransactionMetaData
            {
                BlueprintId = blueprintId,
                ActionId = actionId,
                TransactionType = TransactionType.Action
            }
        };
    }

    private static BlueprintModel CreateTestBlueprint(string blueprintId)
    {
        return new BlueprintModel
        {
            Id = blueprintId,
            Title = "Test Blueprint",
            Version = 1,
            Participants = [],
            Actions = []
        };
    }

    #endregion
}
