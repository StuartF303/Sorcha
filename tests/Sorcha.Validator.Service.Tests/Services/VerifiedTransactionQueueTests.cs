// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;

namespace Sorcha.Validator.Service.Tests.Services;

public class VerifiedTransactionQueueTests
{
    private readonly Mock<ILogger<VerifiedTransactionQueue>> _loggerMock;
    private readonly VerifiedQueueConfiguration _config;
    private readonly VerifiedTransactionQueue _queue;

    public VerifiedTransactionQueueTests()
    {
        _loggerMock = new Mock<ILogger<VerifiedTransactionQueue>>();

        _config = new VerifiedQueueConfiguration
        {
            MaxTransactionsPerRegister = 100,
            MaxTotalTransactions = 500,
            TransactionTtl = TimeSpan.FromMinutes(30),
            MaxRegisters = 10
        };

        _queue = new VerifiedTransactionQueue(
            Options.Create(_config),
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new VerifiedTransactionQueue(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new VerifiedTransactionQueue(
            Options.Create(_config),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Enqueue_WithValidTransaction_ReturnsTrue()
    {
        // Arrange
        var registerId = "test-register";
        var transaction = CreateTestTransaction("tx-1");

        // Act
        var result = _queue.Enqueue(registerId, transaction);

        // Assert
        result.Should().BeTrue();
        _queue.GetCount(registerId).Should().Be(1);
    }

    [Fact]
    public void Enqueue_WithEmptyRegisterId_ThrowsArgumentException()
    {
        // Arrange
        var transaction = CreateTestTransaction("tx-1");

        // Act
        var act = () => _queue.Enqueue("", transaction);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Enqueue_WithNullTransaction_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _queue.Enqueue("test-register", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enqueue_WhenAtRegisterLimit_ReturnsFalse()
    {
        // Arrange
        var registerId = "test-register";
        for (var i = 0; i < _config.MaxTransactionsPerRegister; i++)
        {
            _queue.Enqueue(registerId, CreateTestTransaction($"tx-{i}"));
        }

        // Act
        var result = _queue.Enqueue(registerId, CreateTestTransaction("tx-overflow"));

        // Assert
        result.Should().BeFalse();
        _queue.GetCount(registerId).Should().Be(_config.MaxTransactionsPerRegister);
    }

    [Fact]
    public void Enqueue_DuplicateTransactionId_ReturnsFalse()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));

        // Act
        var result = _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));

        // Assert
        result.Should().BeFalse();
        _queue.GetCount(registerId).Should().Be(1);
    }

    [Fact]
    public void Dequeue_ReturnsTransactionsInPriorityOrder()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-low"), priority: 1);
        _queue.Enqueue(registerId, CreateTestTransaction("tx-high"), priority: 10);
        _queue.Enqueue(registerId, CreateTestTransaction("tx-medium"), priority: 5);

        // Act
        var result = _queue.Dequeue(registerId, 3);

        // Assert
        result.Should().HaveCount(3);
        result[0].TransactionId.Should().Be("tx-high");
        result[1].TransactionId.Should().Be("tx-medium");
        result[2].TransactionId.Should().Be("tx-low");
    }

    [Fact]
    public void Dequeue_RemovesTransactionsFromQueue()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));
        _queue.Enqueue(registerId, CreateTestTransaction("tx-2"));

        // Act
        var result = _queue.Dequeue(registerId, 1);

        // Assert
        result.Should().HaveCount(1);
        _queue.GetCount(registerId).Should().Be(1);
    }

    [Fact]
    public void Dequeue_FromEmptyQueue_ReturnsEmptyList()
    {
        // Act
        var result = _queue.Dequeue("test-register", 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Dequeue_WithNonExistentRegister_ReturnsEmptyList()
    {
        // Act
        var result = _queue.Dequeue("nonexistent", 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Peek_ReturnsTransactionsWithoutRemoving()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));
        _queue.Enqueue(registerId, CreateTestTransaction("tx-2"));

        // Act
        var peeked = _queue.Peek(registerId, 2);

        // Assert
        peeked.Should().HaveCount(2);
        _queue.GetCount(registerId).Should().Be(2);
    }

    [Fact]
    public void ReturnToQueue_ReturnsTransactionsToQueue()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));
        var dequeued = _queue.Dequeue(registerId, 1);
        _queue.GetCount(registerId).Should().Be(0);

        // Act
        _queue.ReturnToQueue(registerId, dequeued);

        // Assert
        _queue.GetCount(registerId).Should().Be(1);
    }

    [Fact]
    public void Remove_ExistingTransaction_ReturnsTrue()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));

        // Act
        var result = _queue.Remove(registerId, "tx-1");

        // Assert
        result.Should().BeTrue();
        _queue.Contains(registerId, "tx-1").Should().BeFalse();
    }

    [Fact]
    public void Remove_NonExistingTransaction_ReturnsFalse()
    {
        // Act
        var result = _queue.Remove("test-register", "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_ExistingTransaction_ReturnsTrue()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));

        // Act
        var result = _queue.Contains(registerId, "tx-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_NonExistingTransaction_ReturnsFalse()
    {
        // Act
        var result = _queue.Contains("test-register", "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetCount_ReturnsCorrectCount()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));
        _queue.Enqueue(registerId, CreateTestTransaction("tx-2"));
        _queue.Enqueue(registerId, CreateTestTransaction("tx-3"));

        // Act
        var count = _queue.GetCount(registerId);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void GetTotalCount_ReturnsCountAcrossAllRegisters()
    {
        // Arrange
        _queue.Enqueue("register-1", CreateTestTransaction("tx-1"));
        _queue.Enqueue("register-1", CreateTestTransaction("tx-2"));
        _queue.Enqueue("register-2", CreateTestTransaction("tx-3"));

        // Act
        var total = _queue.GetTotalCount();

        // Assert
        total.Should().Be(3);
    }

    [Fact]
    public void GetStats_ReturnsCorrectStatistics()
    {
        // Arrange
        _queue.Enqueue("register-1", CreateTestTransaction("tx-1"));
        _queue.Enqueue("register-2", CreateTestTransaction("tx-2"));
        _queue.Dequeue("register-1", 1);

        // Act
        var stats = _queue.GetStats();

        // Assert
        stats.TotalEnqueued.Should().Be(2);
        stats.TotalDequeued.Should().Be(1);
        stats.TotalTransactions.Should().Be(1);
        stats.ActiveRegisters.Should().Be(1);
    }

    [Fact]
    public void GetRegisterStats_ReturnsCorrectStatistics()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"), priority: 5);
        _queue.Enqueue(registerId, CreateTestTransaction("tx-2"), priority: 10);

        // Act
        var stats = _queue.GetRegisterStats(registerId);

        // Assert
        stats.RegisterId.Should().Be(registerId);
        stats.TransactionCount.Should().Be(2);
        stats.AveragePriority.Should().Be(7.5);
    }

    [Fact]
    public void Clear_RemovesAllTransactionsForRegister()
    {
        // Arrange
        var registerId = "test-register";
        _queue.Enqueue(registerId, CreateTestTransaction("tx-1"));
        _queue.Enqueue(registerId, CreateTestTransaction("tx-2"));
        _queue.Enqueue("other-register", CreateTestTransaction("tx-3"));

        // Act
        var cleared = _queue.Clear(registerId);

        // Assert
        cleared.Should().Be(2);
        _queue.GetCount(registerId).Should().Be(0);
        _queue.GetCount("other-register").Should().Be(1);
    }

    [Fact]
    public void ClearAll_RemovesAllTransactions()
    {
        // Arrange
        _queue.Enqueue("register-1", CreateTestTransaction("tx-1"));
        _queue.Enqueue("register-2", CreateTestTransaction("tx-2"));

        // Act
        var cleared = _queue.ClearAll();

        // Assert
        cleared.Should().Be(2);
        _queue.GetTotalCount().Should().Be(0);
    }

    [Fact]
    public void Enqueue_WhenMaxRegistersReached_ReturnsFalse()
    {
        // Arrange - Fill up to max registers
        for (var i = 0; i < _config.MaxRegisters; i++)
        {
            _queue.Enqueue($"register-{i}", CreateTestTransaction($"tx-{i}"));
        }

        // Act - Try to add to a new register
        var result = _queue.Enqueue("new-register", CreateTestTransaction("tx-new"));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Enqueue_ToExistingRegisterWhenMaxRegistersReached_ReturnsTrue()
    {
        // Arrange - Fill up to max registers
        for (var i = 0; i < _config.MaxRegisters; i++)
        {
            _queue.Enqueue($"register-{i}", CreateTestTransaction($"tx-{i}"));
        }

        // Act - Add to an existing register
        var result = _queue.Enqueue("register-0", CreateTestTransaction("tx-new"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Enqueue_WithPriority_OrdersCorrectly()
    {
        // Arrange
        var registerId = "test-register";

        // Act - Enqueue in random order with different priorities
        _queue.Enqueue(registerId, CreateTestTransaction("tx-normal"), priority: 0);
        _queue.Enqueue(registerId, CreateTestTransaction("tx-urgent"), priority: 100);
        _queue.Enqueue(registerId, CreateTestTransaction("tx-high"), priority: 50);
        _queue.Enqueue(registerId, CreateTestTransaction("tx-low"), priority: -10);

        // Assert - Dequeue should return in priority order
        var dequeued = _queue.Dequeue(registerId, 4);
        dequeued[0].TransactionId.Should().Be("tx-urgent");
        dequeued[1].TransactionId.Should().Be("tx-high");
        dequeued[2].TransactionId.Should().Be("tx-normal");
        dequeued[3].TransactionId.Should().Be("tx-low");
    }

    [Fact]
    public void Dequeue_SamePriority_ReturnsFifoOrder()
    {
        // Arrange
        var registerId = "test-register";

        // Enqueue with same priority - should maintain FIFO
        _queue.Enqueue(registerId, CreateTestTransaction("tx-first"), priority: 5);
        Thread.Sleep(10); // Small delay to ensure different timestamps
        _queue.Enqueue(registerId, CreateTestTransaction("tx-second"), priority: 5);
        Thread.Sleep(10);
        _queue.Enqueue(registerId, CreateTestTransaction("tx-third"), priority: 5);

        // Act
        var dequeued = _queue.Dequeue(registerId, 3);

        // Assert - Should be in FIFO order within same priority
        dequeued[0].TransactionId.Should().Be("tx-first");
        dequeued[1].TransactionId.Should().Be("tx-second");
        dequeued[2].TransactionId.Should().Be("tx-third");
    }

    #region Helper Methods

    private static Transaction CreateTestTransaction(string id)
    {
        return new Transaction
        {
            TransactionId = id,
            RegisterId = "test-register",
            BlueprintId = "bp-1",
            ActionId = "action-1",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            CreatedAt = DateTimeOffset.UtcNow,
            Signatures = [],
            PayloadHash = "hash-" + id
        };
    }

    #endregion
}
