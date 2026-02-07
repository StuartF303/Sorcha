// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;
using Sorcha.Validator.Service.Services.Interfaces;
using StackExchange.Redis;

namespace Sorcha.Validator.Service.Tests.Services;

/// <summary>
/// Unit tests for TransactionPoolPoller
/// Tests cover Redis-backed transaction pool operations
/// </summary>
public class TransactionPoolPollerTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<TransactionPoolPoller>> _mockLogger;
    private readonly TransactionPoolPollerConfiguration _config;
    private readonly IOptions<TransactionPoolPollerConfiguration> _options;

    public TransactionPoolPollerTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<TransactionPoolPoller>>();

        _mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _config = new TransactionPoolPollerConfiguration
        {
            KeyPrefix = "test:validator:unverified:",
            BatchSize = 50,
            PollingInterval = TimeSpan.FromMilliseconds(100),
            TransactionTtl = TimeSpan.FromHours(1),
            MaxRetries = 3,
            Enabled = true
        };
        _options = Options.Create(_config);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Assert
        poller.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullRedis_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new TransactionPoolPoller(null!, _options, _mockLogger.Object));
        exception.ParamName.Should().Be("redis");
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new TransactionPoolPoller(_mockRedis.Object, null!, _mockLogger.Object));
        exception.ParamName.Should().Be("config");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => new TransactionPoolPoller(_mockRedis.Object, _options, null!));
        exception.ParamName.Should().Be("logger");
    }

    #endregion

    #region SubmitTransactionAsync Tests

    [Fact]
    public async Task SubmitTransactionAsync_WithValidTransaction_ReturnsTrue()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");

        var mockTransaction = new Mock<ITransaction>();
        _mockDatabase.Setup(x => x.CreateTransaction(It.IsAny<object>()))
            .Returns(mockTransaction.Object);

        _mockDatabase.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        mockTransaction.Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await poller.SubmitTransactionAsync("register-1", transaction);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitTransactionAsync_WithExistingTransaction_ReturnsFalse()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");

        _mockDatabase.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await poller.SubmitTransactionAsync("register-1", transaction);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitTransactionAsync_WithNullRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => poller.SubmitTransactionAsync(null!, transaction));
    }

    [Fact]
    public async Task SubmitTransactionAsync_WithEmptyRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => poller.SubmitTransactionAsync("", transaction));
    }

    [Fact]
    public async Task SubmitTransactionAsync_WithNullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => poller.SubmitTransactionAsync("register-1", null!));
    }

    [Fact]
    public async Task SubmitTransactionAsync_WithExpiredTransaction_ReturnsFalse()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-10));

        _mockDatabase.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await poller.SubmitTransactionAsync("register-1", transaction);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region PollTransactionsAsync Tests

    [Fact]
    public async Task PollTransactionsAsync_WithEmptyQueue_ReturnsEmptyList()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.ListRightPopAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await poller.PollTransactionsAsync("register-1", 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PollTransactionsAsync_WithTransactions_ReturnsTransactions()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");
        var json = JsonSerializer.Serialize(transaction, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var callCount = 0;
        _mockDatabase.Setup(x => x.ListRightPopAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? (RedisValue)"tx-1" : RedisValue.Null;
            });

        _mockDatabase.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(json);

        _mockDatabase.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SortedSetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await poller.PollTransactionsAsync("register-1", 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].TransactionId.Should().Be("tx-1");
    }

    [Fact]
    public async Task PollTransactionsAsync_WithZeroMaxCount_ReturnsEmpty()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act
        var result = await poller.PollTransactionsAsync("register-1", 0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PollTransactionsAsync_WithNullRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => poller.PollTransactionsAsync(null!, 10));
    }

    #endregion

    #region GetUnverifiedCountAsync Tests

    [Fact]
    public async Task GetUnverifiedCountAsync_ReturnsQueueLength()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(42);

        // Act
        var result = await poller.GetUnverifiedCountAsync("register-1");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task GetUnverifiedCountAsync_WithNullRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => poller.GetUnverifiedCountAsync(null!));
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingTransaction_ReturnsTrue()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await poller.ExistsAsync("register-1", "tx-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentTransaction_ReturnsFalse()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await poller.ExistsAsync("register-1", "tx-1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNullRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => poller.ExistsAsync(null!, "tx-1"));
    }

    [Fact]
    public async Task ExistsAsync_WithNullTransactionId_ThrowsArgumentException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => poller.ExistsAsync("register-1", null!));
    }

    #endregion

    #region RemoveTransactionAsync Tests

    [Fact]
    public async Task RemoveTransactionAsync_WithExistingTransaction_ReturnsTrue()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.ListRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        _mockDatabase.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SortedSetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await poller.RemoveTransactionAsync("register-1", "tx-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveTransactionAsync_WithNonExistentTransaction_ReturnsFalse()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.ListRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(0);

        _mockDatabase.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        _mockDatabase.Setup(x => x.SortedSetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var result = await poller.RemoveTransactionAsync("register-1", "tx-1");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetStatsAsync Tests

    [Fact]
    public async Task GetStatsAsync_ReturnsStats()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(10);

        _mockDatabase.Setup(x => x.SortedSetRangeByRankWithScoresAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<long>(),
            It.IsAny<long>(),
            It.IsAny<Order>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<SortedSetEntry>());

        // Act
        var stats = await poller.GetStatsAsync("register-1");

        // Assert
        stats.RegisterId.Should().Be("register-1");
        stats.TotalTransactions.Should().Be(10);
    }

    [Fact]
    public async Task GetStatsAsync_WithNullRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => poller.GetStatsAsync(null!));
    }

    #endregion

    #region ReturnTransactionsAsync Tests

    [Fact]
    public async Task ReturnTransactionsAsync_WithEmptyList_DoesNothing()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act - Should not throw
        await poller.ReturnTransactionsAsync("register-1", new List<Transaction>());

        // Assert - No database operations should have been performed
        _mockDatabase.Verify(x => x.CreateTransaction(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ReturnTransactionsAsync_WithNullList_DoesNothing()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        // Act - Should not throw
        await poller.ReturnTransactionsAsync("register-1", null!);

        // Assert - No database operations should have been performed
        _mockDatabase.Verify(x => x.CreateTransaction(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ReturnTransactionsAsync_IncrementsRetryCount()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);
        var transaction = CreateValidTransaction("tx-1");
        transaction.RetryCount = 1;

        var mockTransaction = new Mock<ITransaction>();
        _mockDatabase.Setup(x => x.CreateTransaction(It.IsAny<object>()))
            .Returns(mockTransaction.Object);

        _mockDatabase.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        mockTransaction.Setup(x => x.ExecuteAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var transactions = new List<Transaction> { transaction };

        // Act
        await poller.ReturnTransactionsAsync("register-1", transactions);

        // Assert
        transaction.RetryCount.Should().Be(2);
    }

    #endregion

    #region CleanupExpiredAsync Tests

    [Fact]
    public async Task CleanupExpiredAsync_WithExpiredTransactions_RemovesThem()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        var expiredEntries = new[]
        {
            new SortedSetEntry("tx-1", DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()),
            new SortedSetEntry("tx-2", DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds())
        };

        _mockDatabase.Setup(x => x.SortedSetRangeByScoreAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<double>(),
            It.IsAny<double>(),
            It.IsAny<Exclude>(),
            It.IsAny<Order>(),
            It.IsAny<long>(),
            It.IsAny<long>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(expiredEntries.Select(e => e.Element).ToArray());

        _mockDatabase.Setup(x => x.ListRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        _mockDatabase.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase.Setup(x => x.SortedSetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await poller.CleanupExpiredAsync("register-1");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithNoExpiredTransactions_ReturnsZero()
    {
        // Arrange
        var poller = new TransactionPoolPoller(_mockRedis.Object, _options, _mockLogger.Object);

        _mockDatabase.Setup(x => x.SortedSetRangeByScoreAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<double>(),
            It.IsAny<double>(),
            It.IsAny<Exclude>(),
            It.IsAny<Order>(),
            It.IsAny<long>(),
            It.IsAny<long>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<RedisValue>());

        // Act
        var result = await poller.CleanupExpiredAsync("register-1");

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Helper Methods

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
                    PublicKey = System.Text.Encoding.UTF8.GetBytes("public-key-1"),
                    SignatureValue = System.Text.Encoding.UTF8.GetBytes("signature-value-1"),
                    Algorithm = "ED25519",
                    SignedAt = DateTimeOffset.UtcNow
                }
            },
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresAt = expiresAt,
            Priority = priority
        };
    }

    #endregion
}
