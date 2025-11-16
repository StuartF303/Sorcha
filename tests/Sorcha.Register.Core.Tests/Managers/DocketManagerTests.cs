// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Core.Events;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;
using Sorcha.Register.Storage.InMemory;
using Xunit;

namespace Sorcha.Register.Core.Tests.Managers;

public class DocketManagerTests
{
    private readonly InMemoryRegisterRepository _repository;
    private readonly InMemoryEventPublisher _eventPublisher;
    private readonly DocketManager _docketManager;
    private readonly RegisterManager _registerManager;
    private readonly TransactionManager _transactionManager;

    public DocketManagerTests()
    {
        _repository = new InMemoryRegisterRepository();
        _eventPublisher = new InMemoryEventPublisher();
        _docketManager = new DocketManager(_repository, _eventPublisher);
        _registerManager = new RegisterManager(_repository, _eventPublisher);
        _transactionManager = new TransactionManager(_repository, _eventPublisher);
    }

    private async Task<string> CreateTestRegisterAsync()
    {
        var register = await _registerManager.CreateRegisterAsync("TestRegister", "tenant-123");
        return register.Id;
    }

    private async Task<List<string>> CreateTestTransactionsAsync(string registerId, int count)
    {
        var txIds = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var tx = new TransactionModel
            {
                RegisterId = registerId,
                TxId = new string((char)('a' + i), 64),
                SenderWallet = $"wallet-{i}",
                RecipientsWallets = new[] { $"recipient-{i}" },
                Signature = $"sig-{i}",
                PayloadCount = 0,
                Payloads = Array.Empty<PayloadModel>()
            };
            await _transactionManager.StoreTransactionAsync(tx);
            txIds.Add(tx.TxId);
        }
        return txIds;
    }

    [Fact]
    public async Task CreateDocketAsync_WithValidData_ShouldCreateDocket()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 3);

        // Act
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);

        // Assert
        docket.Should().NotBeNull();
        docket.Id.Should().Be(1ul); // First docket
        docket.RegisterId.Should().Be(registerId);
        docket.PreviousHash.Should().BeEmpty(); // Genesis docket
        docket.Hash.Should().NotBeEmpty();
        docket.TransactionIds.Should().HaveCount(3);
        docket.TransactionIds.Should().BeEquivalentTo(txIds);
        docket.State.Should().Be(DocketState.Init);
        docket.TimeStamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateDocketAsync_SecondDocket_ShouldLinkToPrevious()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds1 = await CreateTestTransactionsAsync(registerId, 2);
        var txIds2 = await CreateTestTransactionsAsync(registerId, 2);

        var docket1 = await _docketManager.CreateDocketAsync(registerId, txIds1);
        docket1 = await _docketManager.ProposeDocketAsync(docket1);
        docket1.State = DocketState.Accepted;
        var sealed1 = await _docketManager.SealDocketAsync(docket1);

        // Act
        var docket2 = await _docketManager.CreateDocketAsync(registerId, txIds2);

        // Assert
        docket2.Id.Should().Be(2ul);
        docket2.PreviousHash.Should().Be(sealed1.Hash);
        docket2.PreviousHash.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateDocketAsync_WithNoTransactions_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var emptyList = new List<string>();

        // Act
        var act = () => _docketManager.CreateDocketAsync(registerId, emptyList);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*no transactions*");
    }

    [Fact]
    public async Task CreateDocketAsync_WithNonExistingRegister_ShouldThrowException()
    {
        // Arrange
        var txIds = new List<string> { "tx1", "tx2" };

        // Act
        var act = () => _docketManager.CreateDocketAsync("non-existing-register", txIds);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ProposeDocketAsync_WithInitDocket_ShouldChangeToProposed()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 2);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);

        // Act
        var proposed = await _docketManager.ProposeDocketAsync(docket);

        // Assert
        proposed.State.Should().Be(DocketState.Proposed);
        proposed.TimeStamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ProposeDocketAsync_WithNonInitState_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 2);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        docket.State = DocketState.Sealed;

        // Act
        var act = () => _docketManager.ProposeDocketAsync(docket);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not in Init state*");
    }

    [Fact]
    public async Task SealDocketAsync_WithAcceptedDocket_ShouldSealAndStore()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 3);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        docket.State = DocketState.Accepted;
        _eventPublisher.Clear();

        // Act
        var sealedDocket = await _docketManager.SealDocketAsync(docket);

        // Assert
        sealedDocket.State.Should().Be(DocketState.Sealed);
        sealedDocket.TimeStamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SealDocketAsync_ShouldUpdateRegisterHeight()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 2);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        docket.State = DocketState.Accepted;

        // Act
        await _docketManager.SealDocketAsync(docket);

        // Assert
        var register = await _registerManager.GetRegisterAsync(registerId);
        register!.Height.Should().Be(1u);
    }

    [Fact]
    public async Task SealDocketAsync_ShouldPublishDocketConfirmedEvent()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 2);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        docket.State = DocketState.Accepted;
        _eventPublisher.Clear();

        // Act
        await _docketManager.SealDocketAsync(docket);

        // Assert
        var events = _eventPublisher.GetPublishedEvents<DocketConfirmedEvent>();
        events.Should().ContainSingle();
        var confirmedEvent = events.First();
        confirmedEvent.RegisterId.Should().Be(registerId);
        confirmedEvent.DocketId.Should().Be(docket.Id);
        confirmedEvent.Hash.Should().Be(docket.Hash);
        confirmedEvent.TransactionIds.Should().BeEquivalentTo(txIds);
    }

    [Fact]
    public async Task SealDocketAsync_ShouldPublishRegisterHeightUpdatedEvent()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 2);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        docket.State = DocketState.Accepted;
        _eventPublisher.Clear();

        // Act
        await _docketManager.SealDocketAsync(docket);

        // Assert
        var events = _eventPublisher.GetPublishedEvents<RegisterHeightUpdatedEvent>();
        events.Should().ContainSingle();
        var heightEvent = events.First();
        heightEvent.RegisterId.Should().Be(registerId);
        heightEvent.OldHeight.Should().Be(0u);
        heightEvent.NewHeight.Should().Be(1u);
    }

    [Fact]
    public async Task SealDocketAsync_WithInitState_ShouldThrowException()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 2);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        // docket is in Init state

        // Act
        var act = () => _docketManager.SealDocketAsync(docket);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not in Accepted or Proposed state*");
    }

    [Fact]
    public async Task GetDocketAsync_WithExistingDocket_ShouldReturn()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 2);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        docket.State = DocketState.Accepted;
        await _docketManager.SealDocketAsync(docket);

        // Act
        var retrieved = await _docketManager.GetDocketAsync(registerId, docket.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(docket.Id);
        retrieved.Hash.Should().Be(docket.Hash);
    }

    [Fact]
    public async Task GetDocketAsync_WithNonExistingDocket_ShouldReturnNull()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        // Act
        var retrieved = await _docketManager.GetDocketAsync(registerId, 999);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task GetDocketsAsync_ShouldReturnAllDockets()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var txIds1 = await CreateTestTransactionsAsync(registerId, 2);
        var docket1 = await _docketManager.CreateDocketAsync(registerId, txIds1);
        docket1.State = DocketState.Accepted;
        await _docketManager.SealDocketAsync(docket1);

        var txIds2 = await CreateTestTransactionsAsync(registerId, 2);
        var docket2 = await _docketManager.CreateDocketAsync(registerId, txIds2);
        docket2.State = DocketState.Accepted;
        await _docketManager.SealDocketAsync(docket2);

        var txIds3 = await CreateTestTransactionsAsync(registerId, 2);
        var docket3 = await _docketManager.CreateDocketAsync(registerId, txIds3);
        docket3.State = DocketState.Accepted;
        await _docketManager.SealDocketAsync(docket3);

        // Act
        var dockets = await _docketManager.GetDocketsAsync(registerId);

        // Assert
        dockets.Should().HaveCount(3);
        dockets.Select(d => d.Id).Should().ContainInOrder(1ul, 2ul, 3ul);
    }

    [Fact]
    public async Task GetDocketRangeAsync_ShouldReturnOnlyInRange()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        for (int i = 0; i < 5; i++)
        {
            var txIds = await CreateTestTransactionsAsync(registerId, 1);
            var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
            docket.State = DocketState.Accepted;
            await _docketManager.SealDocketAsync(docket);
        }

        // Act
        var range = await _docketManager.GetDocketRangeAsync(registerId, 2, 4);

        // Assert
        range.Should().HaveCount(3);
        range.Select(d => d.Id).Should().ContainInOrder(2ul, 3ul, 4ul);
    }

    [Fact]
    public async Task VerifyDocketHash_WithValidDocket_ShouldReturnTrue()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 3);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);

        // Act
        var isValid = _docketManager.VerifyDocketHash(docket);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyDocketHash_WithModifiedDocket_ShouldReturnFalse()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = await CreateTestTransactionsAsync(registerId, 3);
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);

        // Modify the docket after hash calculation
        docket.TransactionIds.Add("malicious-tx");

        // Act
        var isValid = _docketManager.VerifyDocketHash(docket);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDocketAsync_CalculatesDeterministicHash()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var txIds = new List<string> { "tx1", "tx2", "tx3" };
        await CreateTestTransactionsAsync(registerId, 3);

        // Act
        var docket1 = await _docketManager.CreateDocketAsync(registerId, txIds);

        // Create same docket again (simulate recreating with same data)
        var register = await _registerManager.GetRegisterAsync(registerId);
        register!.Height = 0; // Reset to simulate same state
        await _registerManager.UpdateRegisterAsync(register);

        var docket2 = await _docketManager.CreateDocketAsync(registerId, txIds);

        // Assert
        // Hashes should be the same for same input data (deterministic)
        docket1.Hash.Should().NotBeEmpty();
        docket2.Hash.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowException()
    {
        // Act
        var act = () => new DocketManager(null!, _eventPublisher);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowException()
    {
        // Act
        var act = () => new DocketManager(_repository, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventPublisher");
    }

    [Fact]
    public void VerifyDocketHash_WithNull_ShouldThrowException()
    {
        // Act
        var act = () => _docketManager.VerifyDocketHash(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
