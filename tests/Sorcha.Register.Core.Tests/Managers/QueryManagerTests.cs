// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Models;
using Sorcha.Register.Storage.InMemory;
using Xunit;

namespace Sorcha.Register.Core.Tests.Managers;

public class QueryManagerTests
{
    private readonly InMemoryRegisterRepository _repository;
    private readonly InMemoryEventPublisher _eventPublisher;
    private readonly QueryManager _queryManager;
    private readonly RegisterManager _registerManager;
    private readonly TransactionManager _transactionManager;

    public QueryManagerTests()
    {
        _repository = new InMemoryRegisterRepository();
        _eventPublisher = new InMemoryEventPublisher();
        _queryManager = new QueryManager(_repository);
        _registerManager = new RegisterManager(_repository, _eventPublisher);
        _transactionManager = new TransactionManager(_repository, _eventPublisher);
    }

    private async Task<string> CreateTestRegisterAsync()
    {
        var register = await _registerManager.CreateRegisterAsync("TestRegister", "tenant-123");
        return register.Id;
    }

    private async Task<TransactionModel> CreateTransactionAsync(
        string registerId,
        char txIdChar,
        string sender,
        string[] recipients,
        string? blueprintId = null,
        string? instanceId = null)
    {
        var tx = new TransactionModel
        {
            RegisterId = registerId,
            TxId = new string(txIdChar, 64),
            SenderWallet = sender,
            RecipientsWallets = recipients,
            Signature = $"sig-{txIdChar}",
            PayloadCount = 0,
            Payloads = Array.Empty<PayloadModel>()
        };

        if (blueprintId != null)
        {
            tx.MetaData = new TransactionMetaData
            {
                RegisterId = registerId,
                BlueprintId = blueprintId,
                InstanceId = instanceId
            };
        }

        return await _transactionManager.StoreTransactionAsync(tx);
    }

    [Fact]
    public async Task QueryTransactionsAsync_WithPredicate_ShouldFilterCorrectly()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet1", new[] { "rec1" });
        await CreateTransactionAsync(registerId, 'b', "wallet2", new[] { "rec2" });
        await CreateTransactionAsync(registerId, 'c', "wallet1", new[] { "rec3" });

        // Act
        var result = await _queryManager.QueryTransactionsAsync(
            registerId,
            t => t.SenderWallet == "wallet1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.SenderWallet.Should().Be("wallet1"));
    }

    [Fact]
    public async Task GetQueryableTransactionsAsync_ShouldReturnQueryable()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet1", new[] { "rec1" });
        await CreateTransactionAsync(registerId, 'b', "wallet2", new[] { "rec2" });

        // Act
        var queryable = await _queryManager.GetQueryableTransactionsAsync(registerId);

        // Assert
        queryable.Should().NotBeNull();
        queryable.Count().Should().Be(2);

        // Should be able to use LINQ on it
        var filtered = queryable.Where(t => t.SenderWallet == "wallet1").ToList();
        filtered.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTransactionsPaginatedAsync_FirstPage_ShouldReturnCorrectItems()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        // Create 25 transactions
        for (int i = 0; i < 25; i++)
        {
            await CreateTransactionAsync(registerId, (char)('a' + (i % 26)), $"wallet-{i}", new[] { $"rec-{i}" });
            await Task.Delay(10); // Ensure different timestamps
        }

        // Act
        var result = await _queryManager.GetTransactionsPaginatedAsync(registerId, page: 1, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetTransactionsPaginatedAsync_LastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        for (int i = 0; i < 25; i++)
        {
            await CreateTransactionAsync(registerId, (char)('a' + (i % 26)), $"wallet-{i}", new[] { $"rec-{i}" });
        }

        // Act
        var result = await _queryManager.GetTransactionsPaginatedAsync(registerId, page: 3, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(5); // 25 total, 10 on page 1, 10 on page 2, 5 on page 3
        result.Page.Should().Be(3);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetTransactionsPaginatedAsync_WithFilter_ShouldApplyFilter()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet-A", new[] { "rec1" });
        await CreateTransactionAsync(registerId, 'b', "wallet-B", new[] { "rec2" });
        await CreateTransactionAsync(registerId, 'c', "wallet-A", new[] { "rec3" });
        await CreateTransactionAsync(registerId, 'd', "wallet-B", new[] { "rec4" });

        // Act
        var result = await _queryManager.GetTransactionsPaginatedAsync(
            registerId,
            page: 1,
            pageSize: 10,
            filter: t => t.SenderWallet == "wallet-A");

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(t => t.SenderWallet.Should().Be("wallet-A"));
    }

    [Fact]
    public async Task GetTransactionsPaginatedAsync_WithInvalidPage_ShouldDefaultToOne()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet1", new[] { "rec1" });

        // Act
        var result = await _queryManager.GetTransactionsPaginatedAsync(registerId, page: 0, pageSize: 10);

        // Assert
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetTransactionsPaginatedAsync_WithInvalidPageSize_ShouldDefaultToTwenty()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet1", new[] { "rec1" });

        // Act - Test too small
        var result1 = await _queryManager.GetTransactionsPaginatedAsync(registerId, page: 1, pageSize: 0);
        result1.PageSize.Should().Be(20);

        // Act - Test too large
        var result2 = await _queryManager.GetTransactionsPaginatedAsync(registerId, page: 1, pageSize: 150);
        result2.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetTransactionsByWalletPaginatedAsync_AsSender_ShouldReturnSenderTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet-X", new[] { "rec1" });
        await CreateTransactionAsync(registerId, 'b', "wallet-Y", new[] { "rec2" });
        await CreateTransactionAsync(registerId, 'c', "wallet-X", new[] { "rec3" });

        // Act
        var result = await _queryManager.GetTransactionsByWalletPaginatedAsync(
            registerId,
            "wallet-X",
            page: 1,
            pageSize: 10,
            asSender: true,
            asRecipient: false);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(t => t.SenderWallet.Should().Be("wallet-X"));
    }

    [Fact]
    public async Task GetTransactionsByWalletPaginatedAsync_AsRecipient_ShouldReturnRecipientTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "sender1", new[] { "wallet-R" });
        await CreateTransactionAsync(registerId, 'b', "sender2", new[] { "wallet-S" });
        await CreateTransactionAsync(registerId, 'c', "sender3", new[] { "wallet-R", "wallet-S" });

        // Act
        var result = await _queryManager.GetTransactionsByWalletPaginatedAsync(
            registerId,
            "wallet-R",
            page: 1,
            pageSize: 10,
            asSender: false,
            asRecipient: true);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(t => t.RecipientsWallets.Should().Contain("wallet-R"));
    }

    [Fact]
    public async Task GetTransactionsByWalletPaginatedAsync_BothRoles_ShouldRemoveDuplicates()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet-X", new[] { "other" });
        await CreateTransactionAsync(registerId, 'b', "other", new[] { "wallet-X" });
        await CreateTransactionAsync(registerId, 'c', "wallet-X", new[] { "wallet-X" }); // Both sender and recipient

        // Act
        var result = await _queryManager.GetTransactionsByWalletPaginatedAsync(
            registerId,
            "wallet-X",
            page: 1,
            pageSize: 10,
            asSender: true,
            asRecipient: true);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3); // Should not double-count transaction 'c'
    }

    [Fact]
    public async Task GetTransactionsByBlueprintAsync_WithoutInstance_ShouldReturnAllBlueprintTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet1", new[] { "rec1" }, "bp-123", "inst-1");
        await CreateTransactionAsync(registerId, 'b', "wallet2", new[] { "rec2" }, "bp-456", "inst-2");
        await CreateTransactionAsync(registerId, 'c', "wallet3", new[] { "rec3" }, "bp-123", "inst-3");

        // Act
        var result = await _queryManager.GetTransactionsByBlueprintAsync(registerId, "bp-123");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.MetaData!.BlueprintId.Should().Be("bp-123"));
    }

    [Fact]
    public async Task GetTransactionsByBlueprintAsync_WithInstance_ShouldFilterByInstance()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        await CreateTransactionAsync(registerId, 'a', "wallet1", new[] { "rec1" }, "bp-123", "inst-1");
        await CreateTransactionAsync(registerId, 'b', "wallet2", new[] { "rec2" }, "bp-123", "inst-2");
        await CreateTransactionAsync(registerId, 'c', "wallet3", new[] { "rec3" }, "bp-123", "inst-1");

        // Act
        var result = await _queryManager.GetTransactionsByBlueprintAsync(registerId, "bp-123", "inst-1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t =>
        {
            t.MetaData!.BlueprintId.Should().Be("bp-123");
            t.MetaData.InstanceId.Should().Be("inst-1");
        });
    }

    [Fact]
    public async Task GetTransactionStatisticsAsync_ShouldCalculateCorrectStats()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a', "wallet-A", new[] { "wallet-X", "wallet-Y" });
        tx1.PayloadCount = 2;
        await Task.Delay(100);

        var tx2 = await CreateTransactionAsync(registerId, 'b', "wallet-B", new[] { "wallet-Z" });
        tx2.PayloadCount = 1;
        await Task.Delay(100);

        var tx3 = await CreateTransactionAsync(registerId, 'c', "wallet-A", new[] { "wallet-X" });
        tx3.PayloadCount = 3;

        // Act
        var stats = await _queryManager.GetTransactionStatisticsAsync(registerId);

        // Assert
        stats.TotalTransactions.Should().Be(3);
        stats.UniqueSenders.Should().Be(2); // wallet-A, wallet-B
        stats.UniqueRecipients.Should().Be(3); // wallet-X, wallet-Y, wallet-Z
        stats.UniqueWallets.Should().Be(5); // wallet-A, wallet-B, wallet-X, wallet-Y, wallet-Z
        stats.TotalPayloads.Should().Be(6); // 2 + 1 + 3
        stats.EarliestTransaction.Should().NotBeNull();
        stats.LatestTransaction.Should().NotBeNull();
        stats.LatestTransaction.Should().BeAfter(stats.EarliestTransaction!.Value);
    }

    [Fact]
    public async Task GetTransactionStatisticsAsync_WithNoTransactions_ShouldReturnZeros()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        // Act
        var stats = await _queryManager.GetTransactionStatisticsAsync(registerId);

        // Assert
        stats.TotalTransactions.Should().Be(0);
        stats.UniqueSenders.Should().Be(0);
        stats.UniqueRecipients.Should().Be(0);
        stats.UniqueWallets.Should().Be(0);
        stats.TotalPayloads.Should().Be(0);
        stats.EarliestTransaction.Should().BeNull();
        stats.LatestTransaction.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowException()
    {
        // Act
        var act = () => new QueryManager(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void PaginatedResult_HasPreviousPage_ShouldReturnCorrectValue()
    {
        // Arrange
        var result1 = new PaginatedResult<TransactionModel> { Page = 1, TotalPages = 3 };
        var result2 = new PaginatedResult<TransactionModel> { Page = 2, TotalPages = 3 };

        // Assert
        result1.HasPreviousPage.Should().BeFalse();
        result2.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResult_HasNextPage_ShouldReturnCorrectValue()
    {
        // Arrange
        var result1 = new PaginatedResult<TransactionModel> { Page = 1, TotalPages = 3 };
        var result2 = new PaginatedResult<TransactionModel> { Page = 3, TotalPages = 3 };

        // Assert
        result1.HasNextPage.Should().BeTrue();
        result2.HasNextPage.Should().BeFalse();
    }
}
