// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Validator.Core.Models;
using Sorcha.Validator.Core.Validators;
using Xunit;

namespace Sorcha.Validator.Core.Tests.Validators;

public class ChainValidatorCoreTests
{
    private readonly ChainValidatorCore _validator;

    public ChainValidatorCoreTests()
    {
        _validator = new ChainValidatorCore();
    }

    #region DetermineTransactionType Tests

    [Fact]
    public void DetermineTransactionType_NoPreviousId_ReturnsGenesis()
    {
        // Arrange
        var tx = CreateTransactionChainData(previousId: null);

        // Act
        var result = _validator.DetermineTransactionType(tx, null);

        // Assert
        result.Should().Be(ChainTransactionType.Genesis);
    }

    [Fact]
    public void DetermineTransactionType_BlueprintIdNullActionId_ReturnsBlueprintPublication()
    {
        // Arrange
        var tx = CreateTransactionChainData(
            previousId: "prev-tx",
            blueprintId: "bp-1",
            actionId: null);

        // Act
        var result = _validator.DetermineTransactionType(tx, null);

        // Assert
        result.Should().Be(ChainTransactionType.BlueprintPublication);
    }

    [Fact]
    public void DetermineTransactionType_ActionIdZero_ReturnsInstanceInitiation()
    {
        // Arrange
        var tx = CreateTransactionChainData(
            previousId: "prev-tx",
            blueprintId: "bp-1",
            actionId: 0);

        // Act
        var result = _validator.DetermineTransactionType(tx, null);

        // Assert
        result.Should().Be(ChainTransactionType.InstanceInitiation);
    }

    [Fact]
    public void DetermineTransactionType_ActionIdGreaterThanZero_ReturnsInstanceAction()
    {
        // Arrange
        var tx = CreateTransactionChainData(
            previousId: "prev-tx",
            blueprintId: "bp-1",
            actionId: 5);

        // Act
        var result = _validator.DetermineTransactionType(tx, null);

        // Assert
        result.Should().Be(ChainTransactionType.InstanceAction);
    }

    [Fact]
    public void DetermineTransactionType_ExplicitTypeSet_ReturnsExplicitType()
    {
        // Arrange
        var tx = new TransactionChainData
        {
            TransactionId = "tx-1",
            RegisterId = "reg-1",
            PreviousId = "prev-tx",
            TransactionType = ChainTransactionType.ControlAction,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = _validator.DetermineTransactionType(tx, null);

        // Assert
        result.Should().Be(ChainTransactionType.ControlAction);
    }

    #endregion

    #region ValidateChainLink Tests

    [Fact]
    public void ValidateChainLink_GenesisTransaction_ReturnsSuccess()
    {
        // Arrange
        var tx = CreateTransactionChainData(previousId: null);

        // Act
        var result = _validator.ValidateChainLink(tx, null);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateChainLink_GenesisWithPreviousId_ReturnsFailed()
    {
        // Arrange
        var tx = CreateTransactionChainData(previousId: "some-id");
        tx = tx with { TransactionType = ChainTransactionType.Genesis };

        // Act
        var result = _validator.ValidateChainLink(tx, null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_506"); // GenesisViolation
    }

    [Fact]
    public void ValidateChainLink_ActionWithNoPreviousId_ReturnsFailed()
    {
        // Arrange
        var tx = CreateTransactionChainData(
            previousId: null,
            blueprintId: "bp-1",
            actionId: 0);
        tx = tx with { TransactionType = ChainTransactionType.InstanceInitiation };

        // Act
        var result = _validator.ValidateChainLink(tx, null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_500"); // InvalidPreviousId
    }

    [Fact]
    public void ValidateChainLink_ActionWithMissingPreviousTransaction_ReturnsFailed()
    {
        // Arrange
        var tx = CreateTransactionChainData(
            previousId: "missing-tx",
            blueprintId: "bp-1",
            actionId: 0);

        // Act
        var result = _validator.ValidateChainLink(tx, null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_500"); // InvalidPreviousId
    }

    [Fact]
    public void ValidateChainLink_ValidActionChain_ReturnsSuccess()
    {
        // Arrange
        var blueprintTx = CreateTransactionChainData(
            txId: "bp-tx",
            previousId: "genesis",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var actionTx = CreateTransactionChainData(
            txId: "action-tx",
            previousId: "bp-tx",
            blueprintId: "bp-1",
            actionId: 0);

        // Act
        var result = _validator.ValidateChainLink(actionTx, blueprintTx);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateChainLink_RegisterMismatch_ReturnsFailed()
    {
        // Arrange
        var prevTx = CreateTransactionChainData(
            txId: "prev-tx",
            registerId: "register-1",
            transactionType: ChainTransactionType.InstanceAction);

        var tx = CreateTransactionChainData(
            txId: "tx",
            registerId: "register-2",
            previousId: "prev-tx",
            blueprintId: "bp-1",
            actionId: 1);

        // Act
        var result = _validator.ValidateChainLink(tx, prevTx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_501"); // BrokenChain
    }

    [Fact]
    public void ValidateChainLink_TimestampBeforePrevious_ReturnsFailed()
    {
        // Arrange
        var prevTx = CreateTransactionChainData(
            txId: "prev-tx",
            timestamp: DateTimeOffset.UtcNow,
            transactionType: ChainTransactionType.BlueprintPublication,
            blueprintId: "bp-1");

        var tx = CreateTransactionChainData(
            txId: "tx",
            previousId: "prev-tx",
            blueprintId: "bp-1",
            actionId: 0,
            timestamp: DateTimeOffset.UtcNow.AddHours(-1)); // Earlier than previous

        // Act
        var result = _validator.ValidateChainLink(tx, prevTx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_508"); // InvalidTimestamp
    }

    #endregion

    #region ValidateBlueprintChain Tests

    [Fact]
    public void ValidateBlueprintChain_NewBlueprintFromGenesis_ReturnsSuccess()
    {
        // Arrange
        var genesis = CreateTransactionChainData(
            txId: "genesis",
            transactionType: ChainTransactionType.Genesis);

        var blueprintTx = CreateTransactionChainData(
            previousId: "genesis",
            blueprintId: "bp-1");

        // Act
        var result = _validator.ValidateBlueprintChain(blueprintTx, genesis);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateBlueprintChain_VersionUpdateSameBlueprint_ReturnsSuccess()
    {
        // Arrange
        var v1 = CreateTransactionChainData(
            txId: "bp-v1",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var v2 = CreateTransactionChainData(
            previousId: "bp-v1",
            blueprintId: "bp-1");

        // Act
        var result = _validator.ValidateBlueprintChain(v2, v1);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateBlueprintChain_VersionUpdateDifferentBlueprint_ReturnsFailed()
    {
        // Arrange
        var v1 = CreateTransactionChainData(
            txId: "bp-v1",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var v2 = CreateTransactionChainData(
            previousId: "bp-v1",
            blueprintId: "bp-2"); // Different blueprint ID

        // Act
        var result = _validator.ValidateBlueprintChain(v2, v1);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_503"); // InvalidBlueprintVersion
    }

    [Fact]
    public void ValidateBlueprintChain_MissingBlueprintId_ReturnsFailed()
    {
        // Arrange
        var genesis = CreateTransactionChainData(
            txId: "genesis",
            transactionType: ChainTransactionType.Genesis);

        var blueprintTx = CreateTransactionChainData(
            previousId: "genesis",
            blueprintId: null); // Missing

        // Act
        var result = _validator.ValidateBlueprintChain(blueprintTx, genesis);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_509"); // MissingBlueprintId
    }

    [Fact]
    public void ValidateBlueprintChain_ReferencingAction_ReturnsFailed()
    {
        // Arrange
        var actionTx = CreateTransactionChainData(
            txId: "action-tx",
            blueprintId: "bp-1",
            actionId: 0,
            transactionType: ChainTransactionType.InstanceInitiation);

        var blueprintTx = CreateTransactionChainData(
            previousId: "action-tx",
            blueprintId: "bp-1");

        // Act
        var result = _validator.ValidateBlueprintChain(blueprintTx, actionTx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_501"); // BrokenChain
    }

    #endregion

    #region ValidateActionChain Tests

    [Fact]
    public void ValidateActionChain_Action0ReferencingBlueprint_ReturnsSuccess()
    {
        // Arrange
        var blueprintTx = CreateTransactionChainData(
            txId: "bp-tx",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var actionTx = CreateTransactionChainData(
            previousId: "bp-tx",
            blueprintId: "bp-1",
            actionId: 0);

        // Act
        var result = _validator.ValidateActionChain(actionTx, blueprintTx);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateActionChain_Action0NotReferencingBlueprint_ReturnsFailed()
    {
        // Arrange
        var priorAction = CreateTransactionChainData(
            txId: "prior-action",
            blueprintId: "bp-1",
            actionId: 0,
            transactionType: ChainTransactionType.InstanceInitiation);

        var actionTx = CreateTransactionChainData(
            previousId: "prior-action",
            blueprintId: "bp-1",
            actionId: 0); // Action 0 should reference blueprint, not another action

        // Act
        var result = _validator.ValidateActionChain(actionTx, priorAction);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_402"); // InvalidActionSequence
    }

    [Fact]
    public void ValidateActionChain_Action0BlueprintMismatch_ReturnsFailed()
    {
        // Arrange
        var blueprintTx = CreateTransactionChainData(
            txId: "bp-tx",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var actionTx = CreateTransactionChainData(
            previousId: "bp-tx",
            blueprintId: "bp-2", // Different blueprint
            actionId: 0);

        // Act
        var result = _validator.ValidateActionChain(actionTx, blueprintTx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_501"); // BrokenChain
    }

    [Fact]
    public void ValidateActionChain_SubsequentActionReferencingPrior_ReturnsSuccess()
    {
        // Arrange
        var priorAction = CreateTransactionChainData(
            txId: "action-1",
            blueprintId: "bp-1",
            actionId: 1,
            transactionType: ChainTransactionType.InstanceAction);

        var actionTx = CreateTransactionChainData(
            previousId: "action-1",
            blueprintId: "bp-1",
            actionId: 2);

        // Act
        var result = _validator.ValidateActionChain(actionTx, priorAction);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateActionChain_SubsequentActionReferencingBlueprint_ReturnsFailed()
    {
        // Arrange
        var blueprintTx = CreateTransactionChainData(
            txId: "bp-tx",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var actionTx = CreateTransactionChainData(
            previousId: "bp-tx",
            blueprintId: "bp-1",
            actionId: 5); // Non-zero action referencing blueprint

        // Act
        var result = _validator.ValidateActionChain(actionTx, blueprintTx);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_402"); // InvalidActionSequence
    }

    #endregion

    #region ValidatePreviousData Tests

    [Fact]
    public void ValidatePreviousData_NoPreviousDataClaimed_ReturnsSuccess()
    {
        // Arrange & Act
        var result = _validator.ValidatePreviousData(null, "actual-data");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatePreviousData_MatchingData_ReturnsSuccess()
    {
        // Arrange & Act
        var result = _validator.ValidatePreviousData("data-hash", "data-hash");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatePreviousData_CaseInsensitiveMatch_ReturnsSuccess()
    {
        // Arrange & Act
        var result = _validator.ValidatePreviousData("DATA-HASH", "data-hash");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatePreviousData_DataMismatch_ReturnsFailed()
    {
        // Arrange & Act
        var result = _validator.ValidatePreviousData("claimed-hash", "actual-hash");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_502"); // PreviousDataMismatch
    }

    [Fact]
    public void ValidatePreviousData_ClaimedButActualEmpty_ReturnsFailed()
    {
        // Arrange & Act
        var result = _validator.ValidatePreviousData("claimed-hash", null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_502"); // PreviousDataMismatch
    }

    #endregion

    #region Multi-Blueprint Support Tests

    [Fact]
    public void ValidateChainLink_MultipleBlueprintsInRegister_ReturnsSuccess()
    {
        // Arrange - Two different blueprints can exist in the same register
        var genesis = CreateTransactionChainData(
            txId: "genesis",
            transactionType: ChainTransactionType.Genesis);

        var blueprint1 = CreateTransactionChainData(
            txId: "bp-1-tx",
            previousId: "genesis",
            blueprintId: "blueprint-A",
            transactionType: ChainTransactionType.BlueprintPublication);

        var blueprint2 = CreateTransactionChainData(
            txId: "bp-2-tx",
            previousId: "genesis",
            blueprintId: "blueprint-B",
            transactionType: ChainTransactionType.BlueprintPublication);

        // Act
        var result1 = _validator.ValidateChainLink(blueprint1, genesis);
        var result2 = _validator.ValidateChainLink(blueprint2, genesis);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateActionChain_CrossBlueprintReference_ReturnsFailed()
    {
        // Arrange - Action in blueprint A referencing action in blueprint B
        var actionFromBlueprintA = CreateTransactionChainData(
            txId: "action-A",
            blueprintId: "blueprint-A",
            actionId: 0,
            transactionType: ChainTransactionType.InstanceInitiation);

        var actionFromBlueprintB = CreateTransactionChainData(
            previousId: "action-A",
            blueprintId: "blueprint-B", // Different blueprint
            actionId: 1);

        // Act
        var result = _validator.ValidateActionChain(actionFromBlueprintB, actionFromBlueprintA);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "VAL_501"); // BrokenChain
    }

    #endregion

    #region Blueprint Versioning Tests

    [Fact]
    public void ValidateBlueprintChain_VersionChain_ReturnsSuccess()
    {
        // Arrange - Blueprint version chain: genesis -> v1 -> v2 -> v3
        var genesis = CreateTransactionChainData(
            txId: "genesis",
            transactionType: ChainTransactionType.Genesis);

        var v1 = CreateTransactionChainData(
            txId: "bp-v1",
            previousId: "genesis",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var v2 = CreateTransactionChainData(
            txId: "bp-v2",
            previousId: "bp-v1",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var v3 = CreateTransactionChainData(
            txId: "bp-v3",
            previousId: "bp-v2",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        // Act
        var result1 = _validator.ValidateBlueprintChain(v1, genesis);
        var result2 = _validator.ValidateBlueprintChain(v2, v1);
        var result3 = _validator.ValidateBlueprintChain(v3, v2);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
        result3.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateActionChain_ActionReferencesLatestBlueprintVersion_ReturnsSuccess()
    {
        // Arrange - New instance should reference latest blueprint version
        var blueprintV2 = CreateTransactionChainData(
            txId: "bp-v2",
            blueprintId: "bp-1",
            transactionType: ChainTransactionType.BlueprintPublication);

        var action = CreateTransactionChainData(
            previousId: "bp-v2",
            blueprintId: "bp-1",
            actionId: 0);

        // Act
        var result = _validator.ValidateActionChain(action, blueprintV2);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static TransactionChainData CreateTransactionChainData(
        string? txId = null,
        string? registerId = null,
        string? previousId = null,
        string? blueprintId = null,
        int? actionId = null,
        DateTimeOffset? timestamp = null,
        ChainTransactionType transactionType = default,
        string? dataHash = null)
    {
        return new TransactionChainData
        {
            TransactionId = txId ?? $"tx-{Guid.NewGuid():N}",
            RegisterId = registerId ?? "test-register",
            PreviousId = previousId,
            BlueprintId = blueprintId,
            ActionId = actionId,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            TransactionType = transactionType,
            DataHash = dataHash
        };
    }

    #endregion
}
