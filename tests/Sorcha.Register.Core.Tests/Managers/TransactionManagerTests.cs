// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Core.Events;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Models;
using Sorcha.Register.Storage.InMemory;
using Xunit;

namespace Sorcha.Register.Core.Tests.Managers;

public class TransactionManagerTests
{
    private readonly InMemoryRegisterRepository _repository;
    private readonly InMemoryEventPublisher _eventPublisher;
    private readonly TransactionManager _manager;
    private readonly RegisterManager _registerManager;
    private string _testRegisterId = string.Empty;

    public TransactionManagerTests()
    {
        _repository = new InMemoryRegisterRepository();
        _eventPublisher = new InMemoryEventPublisher();
        _manager = new TransactionManager(_repository, _eventPublisher);
        _registerManager = new RegisterManager(_repository, _eventPublisher);
    }

    private async Task<string> CreateTestRegisterAsync()
    {
        var register = await _registerManager.CreateRegisterAsync("TestRegister", "tenant-123");
        _testRegisterId = register.Id;
        return register.Id;
    }

    private TransactionModel CreateValidTransaction(string? registerId = null)
    {
        return new TransactionModel
        {
            RegisterId = registerId ?? _testRegisterId,
            TxId = new string('a', 64),
            SenderWallet = "sender-wallet-address",
            RecipientsWallets = new[] { "recipient1", "recipient2" },
            Signature = "valid-signature",
            PayloadCount = 0,
            Payloads = Array.Empty<PayloadModel>()
        };
    }

    [Fact]
    public async Task StoreTransactionAsync_WithValidTransaction_ShouldStore()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        _eventPublisher.Clear();

        // Act
        var result = await _manager.StoreTransactionAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.TxId.Should().Be(transaction.TxId);
        result.RegisterId.Should().Be(registerId);
        result.SenderWallet.Should().Be("sender-wallet-address");
        result.RecipientsWallets.Should().Contain("recipient1");
    }

    [Fact]
    public async Task StoreTransactionAsync_ShouldGenerateDidUri()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.Id = null; // Ensure ID is null

        // Act
        var result = await _manager.StoreTransactionAsync(transaction);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        result.Id.Should().Be($"did:sorcha:register:{registerId}/tx/{transaction.TxId}");
    }

    [Fact]
    public async Task StoreTransactionAsync_ShouldSetTimestamp()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.TimeStamp = default;

        // Act
        var result = await _manager.StoreTransactionAsync(transaction);

        // Assert
        result.TimeStamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task StoreTransactionAsync_ShouldPublishTransactionConfirmedEvent()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        _eventPublisher.Clear();

        // Act
        await _manager.StoreTransactionAsync(transaction);

        // Assert
        var events = _eventPublisher.GetPublishedEvents<TransactionConfirmedEvent>();
        events.Should().ContainSingle();
        var confirmedEvent = events.First();
        confirmedEvent.TransactionId.Should().Be(transaction.TxId);
        confirmedEvent.RegisterId.Should().Be(registerId);
        confirmedEvent.SenderWallet.Should().Be("sender-wallet-address");
        confirmedEvent.ToWallets.Should().Contain("recipient1");
    }

    [Fact]
    public async Task StoreTransactionAsync_WithNullTransaction_ShouldThrowException()
    {
        // Act
        var act = () => _manager.StoreTransactionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task StoreTransactionAsync_WithInvalidRegisterId_ShouldThrowException()
    {
        // Arrange
        var transaction = CreateValidTransaction("non-existing-register");

        // Act
        var act = () => _manager.StoreTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task StoreTransactionAsync_WithEmptyTxId_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.TxId = string.Empty;

        // Act
        var act = () => _manager.StoreTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*TxId is required*");
    }

    [Fact]
    public async Task StoreTransactionAsync_WithInvalidTxIdLength_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.TxId = "tooshort";

        // Act
        var act = () => _manager.StoreTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be 64 characters*");
    }

    [Fact]
    public async Task StoreTransactionAsync_WithEmptySenderWallet_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.SenderWallet = string.Empty;

        // Act
        var act = () => _manager.StoreTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*SenderWallet is required*");
    }

    [Fact]
    public async Task StoreTransactionAsync_WithEmptySignature_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.Signature = string.Empty;

        // Act
        var act = () => _manager.StoreTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Signature is required*");
    }

    [Fact]
    public async Task StoreTransactionAsync_WithMismatchedPayloadCount_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.PayloadCount = 5;
        transaction.Payloads = new[] { new PayloadModel(), new PayloadModel() }; // Only 2 payloads

        // Act
        var act = () => _manager.StoreTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*PayloadCount*does not match*");
    }

    [Fact]
    public async Task StoreTransactionAsync_WithPayloads_ShouldValidateCount()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        transaction.PayloadCount = 3;
        transaction.Payloads = new[]
        {
            new PayloadModel { Hash = "hash1", Data = "data1" },
            new PayloadModel { Hash = "hash2", Data = "data2" },
            new PayloadModel { Hash = "hash3", Data = "data3" }
        };

        // Act
        var result = await _manager.StoreTransactionAsync(transaction);

        // Assert
        result.Payloads.Should().HaveCount(3);
        result.PayloadCount.Should().Be(3);
    }

    [Fact]
    public async Task GetTransactionAsync_WithExistingTransaction_ShouldReturn()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var transaction = CreateValidTransaction(registerId);
        await _manager.StoreTransactionAsync(transaction);

        // Act
        var result = await _manager.GetTransactionAsync(registerId, transaction.TxId);

        // Assert
        result.Should().NotBeNull();
        result!.TxId.Should().Be(transaction.TxId);
    }

    [Fact]
    public async Task GetTransactionAsync_WithNonExistingTransaction_ShouldReturnNull()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        // Act
        var result = await _manager.GetTransactionAsync(registerId, new string('x', 64));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTransactionsAsync_ShouldReturnAllTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = CreateValidTransaction(registerId);
        tx1.TxId = new string('a', 64);
        await _manager.StoreTransactionAsync(tx1);

        var tx2 = CreateValidTransaction(registerId);
        tx2.TxId = new string('b', 64);
        await _manager.StoreTransactionAsync(tx2);

        var tx3 = CreateValidTransaction(registerId);
        tx3.TxId = new string('c', 64);
        await _manager.StoreTransactionAsync(tx3);

        // Act
        var result = await _manager.GetTransactionsAsync(registerId);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTransactionsBySenderAsync_ShouldReturnOnlySenderTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = CreateValidTransaction(registerId);
        tx1.TxId = new string('a', 64);
        tx1.SenderWallet = "wallet-A";
        await _manager.StoreTransactionAsync(tx1);

        var tx2 = CreateValidTransaction(registerId);
        tx2.TxId = new string('b', 64);
        tx2.SenderWallet = "wallet-B";
        await _manager.StoreTransactionAsync(tx2);

        var tx3 = CreateValidTransaction(registerId);
        tx3.TxId = new string('c', 64);
        tx3.SenderWallet = "wallet-A";
        await _manager.StoreTransactionAsync(tx3);

        // Act
        var result = await _manager.GetTransactionsBySenderAsync(registerId, "wallet-A");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.SenderWallet.Should().Be("wallet-A"));
    }

    [Fact]
    public async Task GetTransactionsByRecipientAsync_ShouldReturnOnlyRecipientTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = CreateValidTransaction(registerId);
        tx1.TxId = new string('a', 64);
        tx1.RecipientsWallets = new[] { "wallet-X", "wallet-Y" };
        await _manager.StoreTransactionAsync(tx1);

        var tx2 = CreateValidTransaction(registerId);
        tx2.TxId = new string('b', 64);
        tx2.RecipientsWallets = new[] { "wallet-Z" };
        await _manager.StoreTransactionAsync(tx2);

        var tx3 = CreateValidTransaction(registerId);
        tx3.TxId = new string('c', 64);
        tx3.RecipientsWallets = new[] { "wallet-X" };
        await _manager.StoreTransactionAsync(tx3);

        // Act
        var result = await _manager.GetTransactionsByRecipientAsync(registerId, "wallet-X");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.RecipientsWallets.Should().Contain("wallet-X"));
    }

    [Fact]
    public async Task GetTransactionsByDocketAsync_ShouldReturnOnlyDocketTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = CreateValidTransaction(registerId);
        tx1.TxId = new string('a', 64);
        tx1.BlockNumber = 1;
        await _manager.StoreTransactionAsync(tx1);

        var tx2 = CreateValidTransaction(registerId);
        tx2.TxId = new string('b', 64);
        tx2.BlockNumber = 2;
        await _manager.StoreTransactionAsync(tx2);

        var tx3 = CreateValidTransaction(registerId);
        tx3.TxId = new string('c', 64);
        tx3.BlockNumber = 1;
        await _manager.StoreTransactionAsync(tx3);

        // Act
        var result = await _manager.GetTransactionsByDocketAsync(registerId, 1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.BlockNumber.Should().Be(1ul));
    }

    [Fact]
    public async Task GetTransactionsByBlueprintAsync_ShouldReturnOnlyBlueprintTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = CreateValidTransaction(registerId);
        tx1.TxId = new string('a', 64);
        tx1.MetaData = new TransactionMetaData { RegisterId = registerId, BlueprintId = "bp-123" };
        await _manager.StoreTransactionAsync(tx1);

        var tx2 = CreateValidTransaction(registerId);
        tx2.TxId = new string('b', 64);
        tx2.MetaData = new TransactionMetaData { RegisterId = registerId, BlueprintId = "bp-456" };
        await _manager.StoreTransactionAsync(tx2);

        var tx3 = CreateValidTransaction(registerId);
        tx3.TxId = new string('c', 64);
        tx3.MetaData = new TransactionMetaData { RegisterId = registerId, BlueprintId = "bp-123" };
        await _manager.StoreTransactionAsync(tx3);

        // Act
        var result = await _manager.GetTransactionsByBlueprintAsync(registerId, "bp-123");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.MetaData!.BlueprintId.Should().Be("bp-123"));
    }

    [Fact]
    public async Task GetTransactionsByInstanceAsync_ShouldReturnOnlyInstanceTransactions()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = CreateValidTransaction(registerId);
        tx1.TxId = new string('a', 64);
        tx1.MetaData = new TransactionMetaData { RegisterId = registerId, InstanceId = "inst-001" };
        await _manager.StoreTransactionAsync(tx1);

        var tx2 = CreateValidTransaction(registerId);
        tx2.TxId = new string('b', 64);
        tx2.MetaData = new TransactionMetaData { RegisterId = registerId, InstanceId = "inst-002" };
        await _manager.StoreTransactionAsync(tx2);

        var tx3 = CreateValidTransaction(registerId);
        tx3.TxId = new string('c', 64);
        tx3.MetaData = new TransactionMetaData { RegisterId = registerId, InstanceId = "inst-001" };
        await _manager.StoreTransactionAsync(tx3);

        // Act
        var result = await _manager.GetTransactionsByInstanceAsync(registerId, "inst-001");

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.MetaData!.InstanceId.Should().Be("inst-001"));
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowException()
    {
        // Act
        var act = () => new TransactionManager(null!, _eventPublisher);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowException()
    {
        // Act
        var act = () => new TransactionManager(_repository, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventPublisher");
    }
}
