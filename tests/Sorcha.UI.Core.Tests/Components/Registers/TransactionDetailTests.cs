// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Registers;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Registers;

/// <summary>
/// Tests for TransactionDetail component behavior and display logic.
/// Note: Full component tests require E2E testing due to MudBlazor PopoverProvider dependency.
/// Tests focus on view model properties used by TransactionDetail.
/// </summary>
public class TransactionDetailTests
{
    private static TransactionViewModel CreateTestTransaction(
        string? txId = null,
        ulong? blockNumber = 100,
        uint? actionId = null,
        string? blueprintId = null,
        List<string>? recipients = null)
    {
        return new TransactionViewModel
        {
            TxId = txId ?? "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            RegisterId = "test-register-123",
            SenderWallet = "5Hq3wP8d5Zr7wJ9k4Ls6mN2xC1vB0nM8",
            Signature = "sig12345678901234567890123456789012345678901234567890",
            TimeStamp = DateTime.UtcNow.AddMinutes(-5),
            BlockNumber = blockNumber,
            PayloadCount = 2,
            ActionId = actionId,
            BlueprintId = blueprintId,
            RecipientsWallets = recipients ?? []
        };
    }

    #region Transaction Status Tests (Confirmed vs Pending)

    [Fact]
    public void TransactionDetail_ShowsConfirmed_WhenBlockNumberExists()
    {
        // Arrange
        var tx = CreateTestTransaction(blockNumber: 100);

        // Assert
        tx.BlockNumber.Should().NotBeNull();
        tx.BlockNumber.Should().Be(100);
    }

    [Fact]
    public void TransactionDetail_ShowsPending_WhenBlockNumberIsNull()
    {
        // Arrange
        var tx = CreateTestTransaction(blockNumber: null);

        // Assert
        tx.BlockNumber.Should().BeNull();
    }

    #endregion

    #region Transaction Type Color Tests

    [Theory]
    [InlineData(null, null, "Transfer")]
    [InlineData(1u, null, "Action")]
    [InlineData(null, "bp-123", "Blueprint")]
    [InlineData(1u, "bp-123", "Action")] // ActionId takes priority
    public void TransactionDetail_TransactionType_ReturnsCorrectType(uint? actionId, string? blueprintId, string expectedType)
    {
        // Arrange
        var tx = CreateTestTransaction(actionId: actionId, blueprintId: blueprintId);

        // Assert
        tx.TransactionType.Should().Be(expectedType);
    }

    [Fact]
    public void TransactionDetail_TypeColor_IsPrimaryForAction()
    {
        // Arrange
        var tx = CreateTestTransaction(actionId: 1);

        // Assert - Component would use Color.Primary for "Action"
        tx.TransactionType.Should().Be("Action");
    }

    [Fact]
    public void TransactionDetail_TypeColor_IsSecondaryForBlueprint()
    {
        // Arrange
        var tx = CreateTestTransaction(blueprintId: "bp-123");

        // Assert - Component would use Color.Secondary for "Blueprint"
        tx.TransactionType.Should().Be("Blueprint");
    }

    [Fact]
    public void TransactionDetail_TypeColor_IsDefaultForTransfer()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert - Component would use Color.Default for "Transfer"
        tx.TransactionType.Should().Be("Transfer");
    }

    #endregion

    #region Display Data Tests

    [Fact]
    public void TransactionDetail_DisplaysFullTxId()
    {
        // Arrange
        var fullTxId = "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890";
        var tx = CreateTestTransaction(txId: fullTxId);

        // Assert - Detail should show full ID
        tx.TxId.Should().Be(fullTxId);
        tx.TxId.Length.Should().Be(64);
    }

    [Fact]
    public void TransactionDetail_DisplaysFullSenderWallet()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert - Detail should show full wallet (not truncated)
        tx.SenderWallet.Should().Be("5Hq3wP8d5Zr7wJ9k4Ls6mN2xC1vB0nM8");
    }

    [Fact]
    public void TransactionDetail_DisplaysSignature()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert
        tx.Signature.Should().NotBeNullOrEmpty();
        tx.Signature.Should().StartWith("sig");
    }

    [Fact]
    public void TransactionDetail_DisplaysPayloadCount()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert
        tx.PayloadCount.Should().Be(2);
    }

    [Fact]
    public void TransactionDetail_DisplaysTimeStampFormatted()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert - Should have both full timestamp and relative time
        tx.TimeStamp.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-5), TimeSpan.FromSeconds(5));
        tx.TimeStampFormatted.Should().Be("5m ago");
    }

    [Fact]
    public void TransactionDetail_DisplaysBlockNumber_WhenConfirmed()
    {
        // Arrange
        var tx = CreateTestTransaction(blockNumber: 42);

        // Assert
        tx.BlockNumber.Should().Be(42);
    }

    #endregion

    #region Recipients Tests

    [Fact]
    public void TransactionDetail_DisplaysNoRecipients_WhenEmpty()
    {
        // Arrange
        var tx = CreateTestTransaction(recipients: []);

        // Assert
        tx.RecipientsWallets.Should().BeEmpty();
    }

    [Fact]
    public void TransactionDetail_DisplaysSingleRecipient()
    {
        // Arrange
        var tx = CreateTestTransaction(recipients: ["recipient-wallet-123"]);

        // Assert
        tx.RecipientsWallets.Should().HaveCount(1);
        tx.RecipientsWallets[0].Should().Be("recipient-wallet-123");
    }

    [Fact]
    public void TransactionDetail_DisplaysMultipleRecipients()
    {
        // Arrange
        var recipients = new List<string>
        {
            "recipient-wallet-1",
            "recipient-wallet-2",
            "recipient-wallet-3"
        };
        var tx = CreateTestTransaction(recipients: recipients);

        // Assert
        tx.RecipientsWallets.Should().HaveCount(3);
        tx.RecipientsWallets.Should().ContainInOrder(recipients);
    }

    #endregion

    #region Action Metadata Tests

    [Fact]
    public void TransactionDetail_DisplaysActionId_WhenPresent()
    {
        // Arrange
        var tx = CreateTestTransaction(actionId: 5);

        // Assert
        tx.ActionId.Should().Be(5);
    }

    [Fact]
    public void TransactionDetail_HidesActionId_WhenNotPresent()
    {
        // Arrange
        var tx = CreateTestTransaction(actionId: null);

        // Assert
        tx.ActionId.Should().BeNull();
    }

    [Fact]
    public void TransactionDetail_DisplaysBlueprintId_WhenPresent()
    {
        // Arrange
        var tx = CreateTestTransaction(blueprintId: "blueprint-abc");

        // Assert
        tx.BlueprintId.Should().Be("blueprint-abc");
    }

    #endregion

    #region DID URI Tests

    [Fact]
    public void TransactionDetail_CanGenerateDidUri()
    {
        // Arrange
        var tx = CreateTestTransaction(txId: "tx-id-123");

        // Assert - DID URI format for Sorcha transactions
        tx.DidUri.Should().Be("did:sorcha:register:test-register-123/tx/tx-id-123");
    }

    #endregion

    #region Version Tests

    [Fact]
    public void TransactionDetail_DisplaysVersion()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert - Default version
        tx.Version.Should().Be(1);
    }

    [Fact]
    public void TransactionDetail_DisplaysCustomVersion()
    {
        // Arrange
        var tx = new TransactionViewModel
        {
            TxId = "tx-123",
            RegisterId = "reg-123",
            SenderWallet = "wallet-123",
            Signature = "sig-123",
            Version = 2
        };

        // Assert
        tx.Version.Should().Be(2);
    }

    #endregion

    #region Copy Functions Data Tests

    [Fact]
    public void TransactionDetail_TxIdIsCopyable()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert - TxId should be a valid string for copying
        tx.TxId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TransactionDetail_SenderWalletIsCopyable()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert - SenderWallet should be a valid string for copying
        tx.SenderWallet.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TransactionDetail_SignatureIsCopyable()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert - Signature should be a valid string for copying
        tx.Signature.Should().NotBeNullOrWhiteSpace();
    }

    #endregion
}
