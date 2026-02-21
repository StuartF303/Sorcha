// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Registers;
using Xunit;

namespace Sorcha.UI.Core.Tests.Models.Registers;

/// <summary>
/// Tests for TransactionViewModel.TransactionType computed property,
/// including MetadataTransactionType enum mapping and heuristic fallback.
/// </summary>
public class TransactionViewModelTests
{
    private static TransactionViewModel CreateVm(
        int? metadataTransactionType = null,
        uint? actionId = null,
        string? blueprintId = null)
    {
        return new TransactionViewModel
        {
            TxId = "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            RegisterId = "reg123",
            SenderWallet = "wallet123",
            Signature = "sig123",
            MetadataTransactionType = metadataTransactionType,
            ActionId = actionId,
            BlueprintId = blueprintId
        };
    }

    [Fact]
    public void TransactionType_MetadataControl_ReturnsControl()
    {
        var vm = CreateVm(metadataTransactionType: 0);
        vm.TransactionType.Should().Be("Control");
    }

    [Fact]
    public void TransactionType_MetadataAction_ReturnsAction()
    {
        var vm = CreateVm(metadataTransactionType: 1);
        vm.TransactionType.Should().Be("Action");
    }

    [Fact]
    public void TransactionType_MetadataDocket_ReturnsDocket()
    {
        var vm = CreateVm(metadataTransactionType: 2);
        vm.TransactionType.Should().Be("Docket");
    }

    [Fact]
    public void TransactionType_MetadataParticipant_ReturnsParticipant()
    {
        var vm = CreateVm(metadataTransactionType: 3);
        vm.TransactionType.Should().Be("Participant");
    }

    [Fact]
    public void TransactionType_MetadataOverridesHeuristic()
    {
        // Even though ActionId is set, metadata enum takes precedence
        var vm = CreateVm(metadataTransactionType: 3, actionId: 1);
        vm.TransactionType.Should().Be("Participant");
    }

    [Fact]
    public void TransactionType_FallbackWithActionId_ReturnsAction()
    {
        var vm = CreateVm(actionId: 1);
        vm.TransactionType.Should().Be("Action");
    }

    [Fact]
    public void TransactionType_FallbackWithBlueprintId_ReturnsBlueprint()
    {
        var vm = CreateVm(blueprintId: "bp-123");
        vm.TransactionType.Should().Be("Blueprint");
    }

    [Fact]
    public void TransactionType_FallbackNoMetadata_ReturnsTransfer()
    {
        var vm = CreateVm();
        vm.TransactionType.Should().Be("Transfer");
    }

    [Fact]
    public void TransactionType_UnknownEnumValue_FallsBackToHeuristic()
    {
        // Unknown enum value (99) should fall through to heuristic
        var vm = CreateVm(metadataTransactionType: 99, blueprintId: "bp-456");
        vm.TransactionType.Should().Be("Blueprint");
    }

    [Fact]
    public void TransactionType_ActionIdPrioritizedOverBlueprintId_InFallback()
    {
        var vm = CreateVm(actionId: 1, blueprintId: "bp-123");
        vm.TransactionType.Should().Be("Action");
    }

    [Fact]
    public void MetadataTransactionType_DefaultsToNull()
    {
        var vm = new TransactionViewModel
        {
            TxId = "tx", RegisterId = "reg", SenderWallet = "w", Signature = "s"
        };
        vm.MetadataTransactionType.Should().BeNull();
    }
}
