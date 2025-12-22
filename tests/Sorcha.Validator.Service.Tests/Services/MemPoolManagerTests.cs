// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;

namespace Sorcha.Validator.Service.Tests.Services;

/// <summary>
/// Unit tests for MemPoolManager
/// Tests cover >85% code coverage as required by project standards
/// </summary>
public class MemPoolManagerTests
{
    private readonly Mock<ILogger<MemPoolManager>> _mockLogger;
    private readonly MemPoolConfiguration _config;
    private readonly IOptions<MemPoolConfiguration> _options;

    public MemPoolManagerTests()
    {
        _mockLogger = new Mock<ILogger<MemPoolManager>>();
        _config = new MemPoolConfiguration
        {
            MaxSize = 100,
            DefaultTTL = TimeSpan.FromHours(1),
            HighPriorityQuota = 0.10, // 10%
            CleanupInterval = TimeSpan.FromMinutes(5)
        };
        _options = Options.Create(_config);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Assert
        manager.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new MemPoolManager(null!, _mockLogger.Object));
        exception.ParamName.Should().Be("config");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new MemPoolManager(_options, null!));
        exception.ParamName.Should().Be("logger");
    }

    #endregion

    #region AddTransactionAsync Tests

    [Fact]
    public async Task AddTransactionAsync_WithValidTransaction_ReturnsTrue()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var transaction = CreateValidTransaction(priority: TransactionPriority.Normal);

        // Act
        var result = await manager.AddTransactionAsync("register-1", transaction);

        // Assert
        result.Should().BeTrue();
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddTransactionAsync_WithNullRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var transaction = CreateValidTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.AddTransactionAsync(null!, transaction));
    }

    [Fact]
    public async Task AddTransactionAsync_WithEmptyRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var transaction = CreateValidTransaction();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.AddTransactionAsync("", transaction));
    }

    [Fact]
    public async Task AddTransactionAsync_WithNullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => manager.AddTransactionAsync("register-1", null!));
    }

    [Fact]
    public async Task AddTransactionAsync_WithDuplicateTransaction_ReturnsFalse()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");

        // Act
        var firstAdd = await manager.AddTransactionAsync("register-1", transaction);
        var secondAdd = await manager.AddTransactionAsync("register-1", transaction);

        // Assert
        firstAdd.Should().BeTrue();
        secondAdd.Should().BeFalse();
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddTransactionAsync_WithFullPool_EvictsOldestLowPriority()
    {
        // Arrange
        var smallConfig = new MemPoolConfiguration { MaxSize = 5, HighPriorityQuota = 0.20 };
        var manager = new MemPoolManager(Options.Create(smallConfig), _mockLogger.Object);

        // Fill pool with low priority transactions
        for (int i = 0; i < 5; i++)
        {
            var tx = CreateValidTransaction($"tx-{i}", priority: TransactionPriority.Low);
            await manager.AddTransactionAsync("register-1", tx);
        }

        // Act - Add normal priority transaction when pool is full
        var newTx = CreateValidTransaction("tx-new", priority: TransactionPriority.Normal);
        var result = await manager.AddTransactionAsync("register-1", newTx);

        // Assert
        result.Should().BeTrue();
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(5); // Still at max size after eviction
    }

    [Fact]
    public async Task AddTransactionAsync_WithHighPriorityExceedingQuota_DowngradesToNormal()
    {
        // Arrange
        var config = new MemPoolConfiguration { MaxSize = 100, HighPriorityQuota = 0.10 }; // 10 high-priority max
        var manager = new MemPoolManager(Options.Create(config), _mockLogger.Object);

        // Fill high-priority quota
        for (int i = 0; i < 10; i++)
        {
            var tx = CreateValidTransaction($"tx-high-{i}", priority: TransactionPriority.High);
            await manager.AddTransactionAsync("register-1", tx);
        }

        // Act - Try to add another high-priority transaction
        var newTx = CreateValidTransaction("tx-high-new", priority: TransactionPriority.High);
        var result = await manager.AddTransactionAsync("register-1", newTx);

        // Assert
        result.Should().BeTrue();
        newTx.Priority.Should().Be(TransactionPriority.Normal); // Should be downgraded
    }

    [Fact]
    public async Task AddTransactionAsync_SetsAddedToPoolAt()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var transaction = CreateValidTransaction();
        var beforeAdd = DateTimeOffset.UtcNow;

        // Act
        await manager.AddTransactionAsync("register-1", transaction);

        // Assert
        transaction.AddedToPoolAt.Should().BeCloseTo(beforeAdd, precision: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task AddTransactionAsync_WithMultipleRegisters_KeepsSeparatePools()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var tx1 = CreateValidTransaction("tx-1");
        var tx2 = CreateValidTransaction("tx-2");

        // Act
        await manager.AddTransactionAsync("register-1", tx1);
        await manager.AddTransactionAsync("register-2", tx2);

        // Assert
        var count1 = await manager.GetTransactionCountAsync("register-1");
        var count2 = await manager.GetTransactionCountAsync("register-2");
        count1.Should().Be(1);
        count2.Should().Be(1);
    }

    #endregion

    #region RemoveTransactionAsync Tests

    [Fact]
    public async Task RemoveTransactionAsync_WithExistingTransaction_ReturnsTrueAndRemoves()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");
        await manager.AddTransactionAsync("register-1", transaction);

        // Act
        var result = await manager.RemoveTransactionAsync("register-1", "tx-1");

        // Assert
        result.Should().BeTrue();
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(0);
    }

    [Fact]
    public async Task RemoveTransactionAsync_WithNonExistentTransaction_ReturnsFalse()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act
        var result = await manager.RemoveTransactionAsync("register-1", "non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveTransactionAsync_WithNonExistentRegister_ReturnsFalse()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act
        var result = await manager.RemoveTransactionAsync("non-existent-register", "tx-1");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetPendingTransactionsAsync Tests

    [Fact]
    public async Task GetPendingTransactionsAsync_WithEmptyPool_ReturnsEmptyList()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act
        var transactions = await manager.GetPendingTransactionsAsync("register-1", 10);

        // Assert
        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingTransactionsAsync_ReturnsPriorityOrdered()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Add transactions in mixed order
        var lowTx = CreateValidTransaction("tx-low", priority: TransactionPriority.Low);
        var normalTx = CreateValidTransaction("tx-normal", priority: TransactionPriority.Normal);
        var highTx = CreateValidTransaction("tx-high", priority: TransactionPriority.High);

        await manager.AddTransactionAsync("register-1", lowTx);
        await manager.AddTransactionAsync("register-1", normalTx);
        await manager.AddTransactionAsync("register-1", highTx);

        // Act
        var transactions = await manager.GetPendingTransactionsAsync("register-1", 10);

        // Assert
        transactions.Should().HaveCount(3);
        transactions[0].TransactionId.Should().Be("tx-high");
        transactions[1].TransactionId.Should().Be("tx-normal");
        transactions[2].TransactionId.Should().Be("tx-low");
    }

    [Fact]
    public async Task GetPendingTransactionsAsync_RespectsMaxCount()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Add 10 transactions
        for (int i = 0; i < 10; i++)
        {
            var tx = CreateValidTransaction($"tx-{i}");
            await manager.AddTransactionAsync("register-1", tx);
        }

        // Act
        var transactions = await manager.GetPendingTransactionsAsync("register-1", 5);

        // Assert
        transactions.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPendingTransactionsAsync_IsFIFOWithinPriority()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Add multiple normal priority transactions
        var tx1 = CreateValidTransaction("tx-1", priority: TransactionPriority.Normal);
        var tx2 = CreateValidTransaction("tx-2", priority: TransactionPriority.Normal);
        var tx3 = CreateValidTransaction("tx-3", priority: TransactionPriority.Normal);

        await manager.AddTransactionAsync("register-1", tx1);
        await Task.Delay(10); // Ensure time difference
        await manager.AddTransactionAsync("register-1", tx2);
        await Task.Delay(10);
        await manager.AddTransactionAsync("register-1", tx3);

        // Act
        var transactions = await manager.GetPendingTransactionsAsync("register-1", 10);

        // Assert
        transactions.Should().HaveCount(3);
        transactions[0].TransactionId.Should().Be("tx-1"); // First added
        transactions[1].TransactionId.Should().Be("tx-2");
        transactions[2].TransactionId.Should().Be("tx-3"); // Last added
    }

    #endregion

    #region GetTransactionCountAsync Tests

    [Fact]
    public async Task GetTransactionCountAsync_WithEmptyPool_ReturnsZero()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act
        var count = await manager.GetTransactionCountAsync("register-1");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetTransactionCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        for (int i = 0; i < 5; i++)
        {
            var tx = CreateValidTransaction($"tx-{i}");
            await manager.AddTransactionAsync("register-1", tx);
        }

        // Act
        var count = await manager.GetTransactionCountAsync("register-1");

        // Assert
        count.Should().Be(5);
    }

    #endregion

    #region GetStatsAsync Tests

    [Fact]
    public async Task GetStatsAsync_WithEmptyPool_ReturnsZeroStats()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act
        var stats = await manager.GetStatsAsync("register-1");

        // Assert
        stats.RegisterId.Should().Be("register-1");
        stats.TotalTransactions.Should().Be(0);
        stats.HighPriorityCount.Should().Be(0);
        stats.NormalPriorityCount.Should().Be(0);
        stats.LowPriorityCount.Should().Be(0);
        stats.MaxSize.Should().Be(_config.MaxSize);
        stats.FillPercentage.Should().Be(0);
        stats.OldestTransactionTime.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsAccurateStats()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        var highTx = CreateValidTransaction("tx-high", priority: TransactionPriority.High);
        var normalTx1 = CreateValidTransaction("tx-normal-1", priority: TransactionPriority.Normal);
        var normalTx2 = CreateValidTransaction("tx-normal-2", priority: TransactionPriority.Normal);
        var lowTx = CreateValidTransaction("tx-low", priority: TransactionPriority.Low);

        await manager.AddTransactionAsync("register-1", highTx);
        await manager.AddTransactionAsync("register-1", normalTx1);
        await manager.AddTransactionAsync("register-1", normalTx2);
        await manager.AddTransactionAsync("register-1", lowTx);

        // Act
        var stats = await manager.GetStatsAsync("register-1");

        // Assert
        stats.TotalTransactions.Should().Be(4);
        stats.HighPriorityCount.Should().Be(1);
        stats.NormalPriorityCount.Should().Be(2);
        stats.LowPriorityCount.Should().Be(1);
        stats.FillPercentage.Should().Be(4.0); // 4/100 * 100
        stats.OldestTransactionTime.Should().NotBeNull();
    }

    #endregion

    #region CleanupExpiredTransactionsAsync Tests

    [Fact]
    public async Task CleanupExpiredTransactionsAsync_RemovesExpiredTransactions()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        var expiredTx = CreateValidTransaction("tx-expired", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-10)); // Expired 10 min ago
        var validTx = CreateValidTransaction("tx-valid", expiresAt: DateTimeOffset.UtcNow.AddMinutes(10)); // Expires in 10 min

        await manager.AddTransactionAsync("register-1", expiredTx);
        await manager.AddTransactionAsync("register-1", validTx);

        // Act
        await manager.CleanupExpiredTransactionsAsync();

        // Assert
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(1); // Only valid transaction remains

        var stats = await manager.GetStatsAsync("register-1");
        stats.TotalExpired.Should().Be(1);
    }

    [Fact]
    public async Task CleanupExpiredTransactionsAsync_WithNoExpiredTransactions_DoesNothing()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        var validTx1 = CreateValidTransaction("tx-1");
        var validTx2 = CreateValidTransaction("tx-2");

        await manager.AddTransactionAsync("register-1", validTx1);
        await manager.AddTransactionAsync("register-1", validTx2);

        // Act
        await manager.CleanupExpiredTransactionsAsync();

        // Assert
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(2);
    }

    [Fact]
    public async Task CleanupExpiredTransactionsAsync_WithMultipleRegisters_CleansAll()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        var expiredTx1 = CreateValidTransaction("tx-expired-1", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        var expiredTx2 = CreateValidTransaction("tx-expired-2", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-10));

        await manager.AddTransactionAsync("register-1", expiredTx1);
        await manager.AddTransactionAsync("register-2", expiredTx2);

        // Act
        await manager.CleanupExpiredTransactionsAsync();

        // Assert
        var count1 = await manager.GetTransactionCountAsync("register-1");
        var count2 = await manager.GetTransactionCountAsync("register-2");
        count1.Should().Be(0);
        count2.Should().Be(0);
    }

    #endregion

    #region ReturnTransactionsAsync Tests

    [Fact]
    public async Task ReturnTransactionsAsync_WithValidTransactions_AddsThemBack()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        var tx1 = CreateValidTransaction("tx-1");
        var tx2 = CreateValidTransaction("tx-2");
        var transactions = new List<Transaction> { tx1, tx2 };

        // Act
        await manager.ReturnTransactionsAsync("register-1", transactions);

        // Assert
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(2);
    }

    [Fact]
    public async Task ReturnTransactionsAsync_WithNullList_DoesNothing()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act
        await manager.ReturnTransactionsAsync("register-1", null!);

        // Assert
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(0);
    }

    [Fact]
    public async Task ReturnTransactionsAsync_WithEmptyList_DoesNothing()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        // Act
        await manager.ReturnTransactionsAsync("register-1", new List<Transaction>());

        // Assert
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(0);
    }

    [Fact]
    public async Task ReturnTransactionsAsync_PreservesOriginalPriorityAndTimestamp()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);

        var originalTime = DateTimeOffset.UtcNow.AddMinutes(-30);
        var tx = CreateValidTransaction("tx-1", priority: TransactionPriority.High);
        tx.AddedToPoolAt = originalTime;

        var transactions = new List<Transaction> { tx };

        // Act
        await manager.ReturnTransactionsAsync("register-1", transactions);

        // Assert
        var retrieved = await manager.GetPendingTransactionsAsync("register-1", 10);
        retrieved.Should().HaveCount(1);
        retrieved[0].Priority.Should().Be(TransactionPriority.High);
        // Note: AddedToPoolAt will be updated when added back to pool
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public async Task MemPoolManager_WithHighConcurrency_HandlesCorrectly()
    {
        // Arrange
        var manager = new MemPoolManager(_options, _mockLogger.Object);
        var tasks = new List<Task<bool>>();

        // Act - Add 50 transactions concurrently
        for (int i = 0; i < 50; i++)
        {
            var tx = CreateValidTransaction($"tx-{i}");
            tasks.Add(manager.AddTransactionAsync("register-1", tx));
        }

        await Task.WhenAll(tasks);

        // Assert
        var count = await manager.GetTransactionCountAsync("register-1");
        count.Should().Be(50);
        tasks.All(t => t.Result).Should().BeTrue();
    }

    [Fact]
    public async Task MemPoolManager_EvictionStatistics_AreTracked()
    {
        // Arrange
        var smallConfig = new MemPoolConfiguration { MaxSize = 3, HighPriorityQuota = 0.30 };
        var manager = new MemPoolManager(Options.Create(smallConfig), _mockLogger.Object);

        // Fill pool
        for (int i = 0; i < 3; i++)
        {
            var tx = CreateValidTransaction($"tx-{i}", priority: TransactionPriority.Low);
            await manager.AddTransactionAsync("register-1", tx);
        }

        // Act - Force eviction by adding more transactions
        var newTx1 = CreateValidTransaction("tx-new-1", priority: TransactionPriority.Normal);
        var newTx2 = CreateValidTransaction("tx-new-2", priority: TransactionPriority.Normal);
        await manager.AddTransactionAsync("register-1", newTx1);
        await manager.AddTransactionAsync("register-1", newTx2);

        // Assert
        var stats = await manager.GetStatsAsync("register-1");
        stats.TotalEvictions.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid transaction for testing
    /// </summary>
    private static Transaction CreateValidTransaction(
        string? transactionId = null,
        TransactionPriority priority = TransactionPriority.Normal,
        DateTimeOffset? expiresAt = null)
    {
        return new Transaction
        {
            TransactionId = transactionId ?? $"tx-{Guid.NewGuid()}",
            RegisterId = "register-1",
            BlueprintId = "blueprint-1",
            ActionId = "action-1",
            Payload = JsonDocument.Parse("{\"data\":\"test\"}").RootElement,
            PayloadHash = "abc123def456",
            Signatures = new List<Signature>
            {
                new Signature
                {
                    PublicKey = "public-key-1",
                    SignatureValue = "signature-value-1",
                    Algorithm = "ED25519"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresAt = expiresAt,
            Priority = priority
        };
    }

    #endregion
}
