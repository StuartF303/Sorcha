// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;
using Sorcha.Register.Storage.InMemory;
using Xunit;

namespace Sorcha.Register.Core.Tests.Managers;

public class QueryManagerTests
{
    private readonly InMemoryRegisterRepository _repository;
    private readonly QueryManager _manager;
    private readonly string _testRegisterId;

    public QueryManagerTests()
    {
        _repository = new InMemoryRegisterRepository();
        _manager = new QueryManager(_repository);

        // Create a test register
        var register = new Models.Register
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test Register",
            TenantId = "tenant123",
            Height = 0,
            Status = RegisterStatus.Offline
        };
        _testRegisterId = register.Id;
        _repository.InsertRegisterAsync(register).Wait();
    }

    [Fact]
    public async Task GetTransactionsByWalletAsync_ShouldReturnTransactionsForSenderOrRecipient()
    {
        // Arrange
        var walletAddress = "wallet123";
        await SeedTransactionsAsync(walletAddress);

        // Act
        var result = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterThan(0);
        result.Items.Should().OnlyContain(t =>
            t.SenderWallet == walletAddress ||
            t.RecipientsWallets.Contains(walletAddress));
    }

    [Fact]
    public async Task GetTransactionsByWalletAsync_ShouldPaginateResults()
    {
        // Arrange
        var walletAddress = "wallet123";
        await SeedTransactionsAsync(walletAddress);

        // Act
        var page1 = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 1, 2);
        var page2 = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 2, 2);

        // Assert
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCountGreaterThan(0);
        page1.Page.Should().Be(1);
        page2.Page.Should().Be(2);
        page1.Items.Should().NotIntersectWith(page2.Items);
    }

    [Fact]
    public async Task GetTransactionsByWalletAsync_ShouldCalculateTotalPages()
    {
        // Arrange
        var walletAddress = "wallet123";
        await SeedTransactionsAsync(walletAddress, count: 5);

        // Act
        var result = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 1, 2);

        // Assert
        result.TotalPages.Should().Be(3); // 5 transactions / 2 per page = 3 pages
        result.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetTransactionsByWalletAsync_ShouldSetHasNextAndPreviousPage()
    {
        // Arrange
        var walletAddress = "wallet123";
        await SeedTransactionsAsync(walletAddress, count: 5);

        // Act
        var page1 = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 1, 2);
        var page2 = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 2, 2);
        var page3 = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 3, 2);

        // Assert
        page1.HasPreviousPage.Should().BeFalse();
        page1.HasNextPage.Should().BeTrue();

        page2.HasPreviousPage.Should().BeTrue();
        page2.HasNextPage.Should().BeTrue();

        page3.HasPreviousPage.Should().BeTrue();
        page3.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetTransactionsBySenderAsync_ShouldReturnOnlySenderTransactions()
    {
        // Arrange
        var senderAddress = "sender123";
        await _repository.InsertTransactionAsync(CreateTransaction(senderAddress, "recipient1"));
        await _repository.InsertTransactionAsync(CreateTransaction(senderAddress, "recipient2"));
        await _repository.InsertTransactionAsync(CreateTransaction("otherSender", "recipient3"));

        // Act
        var result = await _manager.GetTransactionsBySenderAsync(_testRegisterId, senderAddress, 1, 20);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(t => t.SenderWallet == senderAddress);
    }

    [Fact]
    public async Task GetTransactionsBySenderAsync_ShouldOrderByTimestampDescending()
    {
        // Arrange
        var senderAddress = "sender123";
        var tx1 = CreateTransaction(senderAddress, "recipient1");
        tx1.TimeStamp = DateTime.UtcNow.AddMinutes(-10);
        await _repository.InsertTransactionAsync(tx1);

        var tx2 = CreateTransaction(senderAddress, "recipient2");
        tx2.TimeStamp = DateTime.UtcNow.AddMinutes(-5);
        await _repository.InsertTransactionAsync(tx2);

        var tx3 = CreateTransaction(senderAddress, "recipient3");
        tx3.TimeStamp = DateTime.UtcNow;
        await _repository.InsertTransactionAsync(tx3);

        // Act
        var result = await _manager.GetTransactionsBySenderAsync(_testRegisterId, senderAddress, 1, 20);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].TimeStamp.Should().BeAfter(result.Items[1].TimeStamp);
        result.Items[1].TimeStamp.Should().BeAfter(result.Items[2].TimeStamp);
    }

    [Fact]
    public async Task GetTransactionsByBlueprintAsync_ShouldReturnBlueprintTransactions()
    {
        // Arrange
        var blueprintId = "blueprint123";
        await SeedBlueprintTransactionsAsync(blueprintId);

        // Act
        var result = await _manager.GetTransactionsByBlueprintAsync(_testRegisterId, blueprintId, null, 1, 20);

        // Assert
        result.Items.Should().HaveCountGreaterThan(0);
        result.Items.Should().OnlyContain(t => t.MetaData?.BlueprintId == blueprintId);
    }

    [Fact]
    public async Task GetTransactionsByBlueprintAsync_WithInstanceId_ShouldFilterByInstance()
    {
        // Arrange
        var blueprintId = "blueprint123";
        var instanceId = "instance456";
        await SeedBlueprintTransactionsAsync(blueprintId, instanceId);

        // Act
        var result = await _manager.GetTransactionsByBlueprintAsync(_testRegisterId, blueprintId, instanceId, 1, 20);

        // Assert
        result.Items.Should().HaveCountGreaterThan(0);
        result.Items.Should().OnlyContain(t =>
            t.MetaData?.BlueprintId == blueprintId &&
            t.MetaData?.InstanceId == instanceId);
    }

    [Fact]
    public async Task GetTransactionStatisticsAsync_ShouldReturnCorrectStatistics()
    {
        // Arrange
        await SeedTransactionsForStatistics();

        // Act
        var result = await _manager.GetTransactionStatisticsAsync(_testRegisterId);

        // Assert
        result.Should().NotBeNull();
        result.TotalTransactions.Should().BeGreaterThan(0);
        result.UniqueWallets.Should().BeGreaterThan(0);
        result.UniqueSenders.Should().BeGreaterThan(0);
        result.UniqueRecipients.Should().BeGreaterThan(0);
        result.TotalPayloads.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTransactionStatisticsAsync_ShouldCalculateUniqueWallets()
    {
        // Arrange
        await _repository.InsertTransactionAsync(CreateTransaction("sender1", "recipient1"));
        await _repository.InsertTransactionAsync(CreateTransaction("sender1", "recipient2"));
        await _repository.InsertTransactionAsync(CreateTransaction("sender2", "recipient1"));

        // Act
        var result = await _manager.GetTransactionStatisticsAsync(_testRegisterId);

        // Assert
        result.UniqueSenders.Should().Be(2); // sender1, sender2
        result.UniqueRecipients.Should().Be(2); // recipient1, recipient2
        result.UniqueWallets.Should().Be(3); // sender1, sender2, recipient1, recipient2 (deduplicated)
    }

    [Fact]
    public async Task GetTransactionStatisticsAsync_ShouldSetEarliestAndLatestTransactions()
    {
        // Arrange
        var earliest = CreateTransaction("sender1", "recipient1");
        earliest.TimeStamp = DateTime.UtcNow.AddDays(-10);
        await _repository.InsertTransactionAsync(earliest);

        var latest = CreateTransaction("sender2", "recipient2");
        latest.TimeStamp = DateTime.UtcNow;
        await _repository.InsertTransactionAsync(latest);

        // Act
        var result = await _manager.GetTransactionStatisticsAsync(_testRegisterId);

        // Assert
        result.EarliestTransaction.Should().BeCloseTo(earliest.TimeStamp, TimeSpan.FromSeconds(1));
        result.LatestTransaction.Should().BeCloseTo(latest.TimeStamp, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetTransactionStatisticsAsync_WithNoTransactions_ShouldReturnZeroStatistics()
    {
        // Act
        var result = await _manager.GetTransactionStatisticsAsync(_testRegisterId);

        // Assert
        result.TotalTransactions.Should().Be(0);
        result.UniqueWallets.Should().Be(0);
        result.UniqueSenders.Should().Be(0);
        result.UniqueRecipients.Should().Be(0);
        result.TotalPayloads.Should().Be(0);
        result.EarliestTransaction.Should().BeNull();
        result.LatestTransaction.Should().BeNull();
    }

    [Fact]
    public async Task GetTransactionsByWalletAsync_ShouldDeduplicateResults()
    {
        // Arrange
        var walletAddress = "wallet123";

        // Transaction where wallet123 is both sender and recipient
        var tx = CreateTransaction(walletAddress, walletAddress);
        await _repository.InsertTransactionAsync(tx);

        // Act
        var result = await _manager.GetTransactionsByWalletAsync(_testRegisterId, walletAddress, 1, 20);

        // Assert
        result.Items.Should().HaveCount(1); // Should only appear once, not twice
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTransactionsByWalletAsync_WithInvalidPage_ShouldThrowArgumentException(int invalidPage)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _manager.GetTransactionsByWalletAsync(_testRegisterId, "wallet123", invalidPage, 20));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)] // Max is 100
    public async Task GetTransactionsByWalletAsync_WithInvalidPageSize_ShouldThrowArgumentException(int invalidPageSize)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _manager.GetTransactionsByWalletAsync(_testRegisterId, "wallet123", 1, invalidPageSize));
    }

    [Fact]
    public async Task GetTransactionStatisticsAsync_ShouldCountTotalPayloads()
    {
        // Arrange
        var tx1 = CreateTransaction("sender1", "recipient1");
        tx1.PayloadCount = 3;
        tx1.Payloads = new[]
        {
            new PayloadModel { Hash = "hash1", Data = "data1", WalletAccess = Array.Empty<string>(), PayloadSize = 100 },
            new PayloadModel { Hash = "hash2", Data = "data2", WalletAccess = Array.Empty<string>(), PayloadSize = 100 },
            new PayloadModel { Hash = "hash3", Data = "data3", WalletAccess = Array.Empty<string>(), PayloadSize = 100 }
        };
        await _repository.InsertTransactionAsync(tx1);

        var tx2 = CreateTransaction("sender2", "recipient2");
        tx2.PayloadCount = 2;
        tx2.Payloads = new[]
        {
            new PayloadModel { Hash = "hash4", Data = "data4", WalletAccess = Array.Empty<string>(), PayloadSize = 100 },
            new PayloadModel { Hash = "hash5", Data = "data5", WalletAccess = Array.Empty<string>(), PayloadSize = 100 }
        };
        await _repository.InsertTransactionAsync(tx2);

        // Act
        var result = await _manager.GetTransactionStatisticsAsync(_testRegisterId);

        // Assert
        result.TotalPayloads.Should().Be(5);
    }

    private async Task SeedTransactionsAsync(string walletAddress, int count = 3)
    {
        for (int i = 0; i < count; i++)
        {
            var tx = CreateTransaction(
                i % 2 == 0 ? walletAddress : $"sender{i}",
                i % 2 == 0 ? $"recipient{i}" : walletAddress);
            await _repository.InsertTransactionAsync(tx);
        }
    }

    private async Task SeedBlueprintTransactionsAsync(string blueprintId, string? instanceId = null)
    {
        for (int i = 0; i < 3; i++)
        {
            var tx = CreateTransaction($"sender{i}", $"recipient{i}");
            tx.MetaData = new TransactionMetaData
            {
                RegisterId = _testRegisterId,
                TransactionType = TransactionType.Action,
                BlueprintId = blueprintId,
                InstanceId = instanceId ?? $"instance{i}",
                ActionId = (uint)i
            };
            await _repository.InsertTransactionAsync(tx);
        }
    }

    private async Task SeedTransactionsForStatistics()
    {
        await _repository.InsertTransactionAsync(CreateTransaction("sender1", "recipient1"));
        await _repository.InsertTransactionAsync(CreateTransaction("sender1", "recipient2"));
        await _repository.InsertTransactionAsync(CreateTransaction("sender2", "recipient1"));
    }

    private TransactionModel CreateTransaction(string senderWallet, string recipientWallet)
    {
        var txId = Guid.NewGuid().ToString("N") + new string('0', 64);
        txId = txId.Substring(0, 64);

        return new TransactionModel
        {
            RegisterId = _testRegisterId,
            TxId = txId,
            PrevTxId = string.Empty,
            Version = 1,
            SenderWallet = senderWallet,
            RecipientsWallets = new[] { recipientWallet },
            TimeStamp = DateTime.UtcNow,
            PayloadCount = 1,
            Payloads = new[]
            {
                new PayloadModel
                {
                    WalletAccess = new[] { senderWallet },
                    PayloadSize = 1024,
                    Hash = "payload_hash",
                    Data = "encrypted_data"
                }
            },
            Signature = "signature"
        };
    }
}
