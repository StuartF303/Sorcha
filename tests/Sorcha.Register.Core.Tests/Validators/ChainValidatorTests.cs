// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Core.Validators;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;
using Sorcha.Register.Storage.InMemory;
using Xunit;

namespace Sorcha.Register.Core.Tests.Validators;

public class ChainValidatorTests
{
    private readonly InMemoryRegisterRepository _repository;
    private readonly InMemoryEventPublisher _eventPublisher;
    private readonly ChainValidator _validator;
    private readonly RegisterManager _registerManager;
    private readonly TransactionManager _transactionManager;
    private readonly DocketManager _docketManager;

    public ChainValidatorTests()
    {
        _repository = new InMemoryRegisterRepository();
        _eventPublisher = new InMemoryEventPublisher();
        _registerManager = new RegisterManager(_repository, _eventPublisher);
        _transactionManager = new TransactionManager(_repository, _eventPublisher);
        _docketManager = new DocketManager(_repository, _eventPublisher);
        _validator = new ChainValidator(_repository, _docketManager);
    }

    private async Task<string> CreateTestRegisterAsync()
    {
        var register = await _registerManager.CreateRegisterAsync("TestRegister", "tenant-123");
        return register.Id;
    }

    private async Task<TransactionModel> CreateTransactionAsync(string registerId, char txIdChar)
    {
        var tx = new TransactionModel
        {
            RegisterId = registerId,
            TxId = new string(txIdChar, 64),
            SenderWallet = $"wallet-{txIdChar}",
            RecipientsWallets = new[] { $"recipient-{txIdChar}" },
            Signature = $"sig-{txIdChar}",
            PayloadCount = 0,
            Payloads = Array.Empty<PayloadModel>()
        };
        return await _transactionManager.StoreTransactionAsync(tx);
    }

    private async Task<Docket> CreateAndSealDocketAsync(string registerId, List<string> txIds)
    {
        var docket = await _docketManager.CreateDocketAsync(registerId, txIds);
        docket.State = DocketState.Accepted;
        return await _docketManager.SealDocketAsync(docket);
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithNoDockets_ShouldReturnValidWithInfo()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Info.Should().Contain(i => i.Contains("No dockets found"));
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithValidChain_ShouldReturnValid()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a');
        var tx2 = await CreateTransactionAsync(registerId, 'b');
        await CreateAndSealDocketAsync(registerId, new List<string> { tx1.TxId, tx2.TxId });

        var tx3 = await CreateTransactionAsync(registerId, 'c');
        await CreateAndSealDocketAsync(registerId, new List<string> { tx3.TxId });

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithInvalidFirstDocketId_ShouldAddError()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var tx = await CreateTransactionAsync(registerId, 'a');

        var docket = new Docket
        {
            Id = 5, // Should be 1
            RegisterId = registerId,
            Hash = "test-hash",
            TransactionIds = new List<string> { tx.TxId },
            State = DocketState.Sealed
        };
        await _repository.InsertDocketAsync(docket);

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("First docket ID should be 1"));
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithNonEmptyFirstPreviousHash_ShouldAddWarning()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();
        var tx = await CreateTransactionAsync(registerId, 'a');

        var docket = new Docket
        {
            Id = 1,
            RegisterId = registerId,
            PreviousHash = "should-be-empty",
            Hash = "test-hash",
            TransactionIds = new List<string> { tx.TxId },
            State = DocketState.Sealed
        };
        await _repository.InsertDocketAsync(docket);

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("should have empty PreviousHash"));
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithBrokenChainLink_ShouldAddError()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a');
        var docket1 = await CreateAndSealDocketAsync(registerId, new List<string> { tx1.TxId });

        // Manually create docket 2 with wrong ID (should be 2, but is 3)
        var tx2 = await CreateTransactionAsync(registerId, 'b');
        var docket2 = new Docket
        {
            Id = 3, // Gap! Should be 2
            RegisterId = registerId,
            PreviousHash = docket1.Hash,
            Hash = "hash2",
            TransactionIds = new List<string> { tx2.TxId },
            State = DocketState.Sealed
        };
        await _repository.InsertDocketAsync(docket2);

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Docket chain break"));
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithWrongPreviousHash_ShouldAddError()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a');
        await CreateAndSealDocketAsync(registerId, new List<string> { tx1.TxId });

        // Manually create docket 2 with wrong previous hash
        var tx2 = await CreateTransactionAsync(registerId, 'b');
        var docket2 = new Docket
        {
            Id = 2,
            RegisterId = registerId,
            PreviousHash = "wrong-hash",
            Hash = "hash2",
            TransactionIds = new List<string> { tx2.TxId },
            State = DocketState.Sealed
        };
        await _repository.InsertDocketAsync(docket2);

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("PreviousHash does not match"));
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithUnsealedDocket_ShouldAddWarning()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a');
        await CreateAndSealDocketAsync(registerId, new List<string> { tx1.TxId });

        var tx2 = await CreateTransactionAsync(registerId, 'b');
        var docket = await _docketManager.CreateDocketAsync(registerId, new List<string> { tx2.TxId });
        docket.State = DocketState.Proposed; // Not sealed
        await _repository.InsertDocketAsync(docket);

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("is not sealed"));
    }

    [Fact]
    public async Task ValidateDocketChainAsync_WithMismatchedHeight_ShouldAddError()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a');
        await CreateAndSealDocketAsync(registerId, new List<string> { tx1.TxId });

        // Manually mess with register height
        var register = await _registerManager.GetRegisterAsync(registerId);
        register!.Height = 999; // Wrong height
        await _registerManager.UpdateRegisterAsync(register);

        // Act
        var result = await _validator.ValidateDocketChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Register height") && e.Contains("does not match"));
    }

    [Fact]
    public async Task ValidateTransactionChainAsync_WithNoTransactions_ShouldReturnValidWithInfo()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        // Act
        var result = await _validator.ValidateTransactionChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Info.Should().Contain(i => i.Contains("No transactions found"));
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTransactionChainAsync_WithValidTransactions_ShouldReturnValid()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        await CreateTransactionAsync(registerId, 'a');
        await CreateTransactionAsync(registerId, 'b');
        await CreateTransactionAsync(registerId, 'c');

        // Act
        var result = await _validator.ValidateTransactionChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTransactionChainAsync_WithMissingPreviousTx_ShouldAddWarning()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx = await CreateTransactionAsync(registerId, 'a');
        var txWithBadPrev = await CreateTransactionAsync(registerId, 'b');
        txWithBadPrev.PrevTxId = new string('z', 64); // Non-existent

        // Act
        var result = await _validator.ValidateTransactionChainAsync(registerId);

        // Assert
        result.Warnings.Should().Contain(w => w.Contains("references non-existent previous transaction"));
    }

    [Fact]
    public async Task ValidateTransactionChainAsync_WithOrphanedTransactions_ShouldAddInfo()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a');
        var tx2 = await CreateTransactionAsync(registerId, 'b');
        var tx3 = await CreateTransactionAsync(registerId, 'c');

        // Seal only one transaction in a docket
        await CreateAndSealDocketAsync(registerId, new List<string> { tx1.TxId });

        // tx2 and tx3 are orphaned

        // Act
        var result = await _validator.ValidateTransactionChainAsync(registerId);

        // Assert
        result.Info.Should().Contain(i => i.Contains("orphaned transactions"));
        result.Info.Should().Contain(i => i.Contains("2")); // 2 orphaned
    }

    [Fact]
    public async Task ValidateTransactionChainAsync_WithDocketReferencingNonExistentTx_ShouldAddError()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx = await CreateTransactionAsync(registerId, 'a');
        var docket = new Docket
        {
            Id = 1,
            RegisterId = registerId,
            Hash = "test-hash",
            TransactionIds = new List<string> { tx.TxId, "non-existent-tx-id" },
            State = DocketState.Sealed
        };
        await _repository.InsertDocketAsync(docket);

        // Act
        var result = await _validator.ValidateTransactionChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("references non-existent transaction"));
    }

    [Fact]
    public async Task ValidateCompleteChainAsync_ShouldCombineBothValidations()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx1 = await CreateTransactionAsync(registerId, 'a');
        await CreateAndSealDocketAsync(registerId, new List<string> { tx1.TxId });

        var tx2 = await CreateTransactionAsync(registerId, 'b'); // Orphaned

        // Act
        var result = await _validator.ValidateCompleteChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeTrue(); // Still valid, just has info
        result.Info.Should().Contain(i => i.Contains("orphaned"));
    }

    [Fact]
    public async Task ValidateCompleteChainAsync_WithErrors_ShouldCombineErrors()
    {
        // Arrange
        var registerId = await CreateTestRegisterAsync();

        var tx = await CreateTransactionAsync(registerId, 'a');

        // Create invalid docket (wrong ID)
        var docket = new Docket
        {
            Id = 5,
            RegisterId = registerId,
            Hash = "test-hash",
            TransactionIds = new List<string> { "non-existent-tx" },
            State = DocketState.Sealed
        };
        await _repository.InsertDocketAsync(docket);

        // Act
        var result = await _validator.ValidateCompleteChainAsync(registerId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1); // Errors from both validations
        result.Errors.Should().Contain(e => e.Contains("First docket ID"));
        result.Errors.Should().Contain(e => e.Contains("references non-existent transaction"));
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowException()
    {
        // Act
        var act = () => new ChainValidator(null!, _docketManager);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullDocketManager_ShouldThrowException()
    {
        // Act
        var act = () => new ChainValidator(_repository, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("docketManager");
    }

    [Fact]
    public void ChainValidationResult_AddError_ShouldSetIsValidToFalse()
    {
        // Arrange
        var result = new ChainValidationResult { RegisterId = "test", IsValid = true };

        // Act
        result.AddError("Test error");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Test error");
    }

    [Fact]
    public void ChainValidationResult_AddWarning_ShouldNotChangeIsValid()
    {
        // Arrange
        var result = new ChainValidationResult { RegisterId = "test", IsValid = true };

        // Act
        result.AddWarning("Test warning");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("Test warning");
    }

    [Fact]
    public void ChainValidationResult_AddInfo_ShouldNotChangeIsValid()
    {
        // Arrange
        var result = new ChainValidationResult { RegisterId = "test", IsValid = true };

        // Act
        result.AddInfo("Test info");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Info.Should().Contain("Test info");
    }

    [Fact]
    public void ChainValidationResult_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var result = new ChainValidationResult { RegisterId = "test-register" };
        result.AddError("Error 1");
        result.AddWarning("Warning 1");
        result.AddInfo("Info 1");

        // Act
        var output = result.ToString();

        // Assert
        output.Should().Contain("test-register");
        output.Should().Contain("INVALID"); // Has error, so invalid
        output.Should().Contain("Error 1");
        output.Should().Contain("Warning 1");
        output.Should().Contain("Info 1");
    }

    [Fact]
    public void ChainValidationResult_ToString_WhenValid_ShouldShowValid()
    {
        // Arrange
        var result = new ChainValidationResult { RegisterId = "test-register", IsValid = true };

        // Act
        var output = result.ToString();

        // Assert
        output.Should().Contain("test-register");
        output.Should().Contain("VALID");
    }
}
