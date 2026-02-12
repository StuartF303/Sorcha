// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Registers;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Registers;

/// <summary>
/// Tests for the TransactionListResponse model and related functionality.
/// Note: Full component tests require E2E testing due to MudBlazor PopoverProvider dependency.
/// </summary>
public class TransactionListTests
{
    private static TransactionViewModel CreateTestTransaction(string? txId = null)
    {
        return new TransactionViewModel
        {
            TxId = txId ?? "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            RegisterId = "reg123456789012345678901234567890",
            SenderWallet = "5Hq3wP8d5Zr7wJ9k4Ls6mN2xC1vB0nM8",
            Signature = "sig12345678901234567890123456789012345678901234567890",
            TimeStamp = DateTime.UtcNow.AddMinutes(-5),
            DocketNumber = 100,
            PayloadCount = 2
        };
    }

    [Fact]
    public void TransactionListResponse_HasMore_ReturnsTrueWhenMorePagesExist()
    {
        // Arrange
        var response = new TransactionListResponse
        {
            Page = 1,
            PageSize = 20,
            Total = 50,
            Transactions = Enumerable.Range(1, 20)
                .Select(i => CreateTestTransaction($"tx{i:D64}"))
                .ToList()
        };

        // Assert
        response.HasMore.Should().BeTrue();
        response.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TransactionListResponse_HasMore_ReturnsFalseOnLastPage()
    {
        // Arrange
        var response = new TransactionListResponse
        {
            Page = 3,
            PageSize = 20,
            Total = 50,
            Transactions = Enumerable.Range(1, 10)
                .Select(i => CreateTestTransaction($"tx{i:D64}"))
                .ToList()
        };

        // Assert
        response.HasMore.Should().BeFalse();
    }

    [Fact]
    public void TransactionListResponse_HasMore_ReturnsFalseWhenEmpty()
    {
        // Arrange
        var response = new TransactionListResponse
        {
            Page = 1,
            PageSize = 20,
            Total = 0,
            Transactions = []
        };

        // Assert
        response.HasMore.Should().BeFalse();
        response.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TransactionListResponse_TotalPages_CalculatesCorrectly()
    {
        // Arrange & Act
        var response1 = new TransactionListResponse { Total = 100, PageSize = 20 };
        var response2 = new TransactionListResponse { Total = 101, PageSize = 20 };
        var response3 = new TransactionListResponse { Total = 5, PageSize = 20 };

        // Assert
        response1.TotalPages.Should().Be(5);
        response2.TotalPages.Should().Be(6);
        response3.TotalPages.Should().Be(1);
    }

    [Fact]
    public void TransactionListResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new TransactionListResponse();

        // Assert
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
        response.Total.Should().Be(0);
        response.Transactions.Should().BeEmpty();
        response.HasMore.Should().BeFalse();
    }

    [Fact]
    public void TransactionViewModel_ComputedProperties_WorkCorrectly()
    {
        // Arrange
        var tx = CreateTestTransaction();

        // Assert
        tx.TxIdTruncated.Should().Be("12345678...");
        tx.SenderTruncated.Should().Be("5Hq3wP8d...0nM8");
        tx.TransactionType.Should().Be("Transfer");
    }

    [Fact]
    public void TransactionViewModel_TransactionType_ReturnsAction_WhenActionIdSet()
    {
        // Arrange
        var tx = new TransactionViewModel
        {
            TxId = "tx123456789012345678901234567890123456789012345678901234567890123",
            RegisterId = "reg123",
            SenderWallet = "addr123",
            Signature = "sig123",
            ActionId = 1
        };

        // Assert
        tx.TransactionType.Should().Be("Action");
    }

    [Fact]
    public void TransactionViewModel_TransactionType_ReturnsBlueprint_WhenBlueprintIdSet()
    {
        // Arrange
        var tx = new TransactionViewModel
        {
            TxId = "tx123456789012345678901234567890123456789012345678901234567890123",
            RegisterId = "reg123",
            SenderWallet = "addr123",
            Signature = "sig123",
            BlueprintId = "bp-123"
        };

        // Assert
        tx.TransactionType.Should().Be("Blueprint");
    }

    [Fact]
    public void TransactionViewModel_IsRecent_ReturnsTrueForRecentTransaction()
    {
        // Arrange
        var tx = new TransactionViewModel
        {
            TxId = "tx123456789012345678901234567890123456789012345678901234567890123",
            RegisterId = "reg123",
            SenderWallet = "addr123",
            Signature = "sig123",
            TimeStamp = DateTime.UtcNow.AddSeconds(-2)
        };

        // Assert
        tx.IsRecent.Should().BeTrue();
    }

    [Fact]
    public void TransactionViewModel_IsRecent_ReturnsFalseForOldTransaction()
    {
        // Arrange
        var tx = new TransactionViewModel
        {
            TxId = "tx123456789012345678901234567890123456789012345678901234567890123",
            RegisterId = "reg123",
            SenderWallet = "addr123",
            Signature = "sig123",
            TimeStamp = DateTime.UtcNow.AddSeconds(-10)
        };

        // Assert
        tx.IsRecent.Should().BeFalse();
    }

    [Fact]
    public void TransactionViewModel_DidUri_FormatsCorrectly()
    {
        // Arrange
        var tx = new TransactionViewModel
        {
            TxId = "abcdef123456",
            RegisterId = "reg-id-123",
            SenderWallet = "addr123",
            Signature = "sig123"
        };

        // Assert
        tx.DidUri.Should().Be("did:sorcha:register:reg-id-123/tx/abcdef123456");
    }

    [Theory]
    [InlineData(-30, "just now")]
    [InlineData(-120, "2m ago")]
    [InlineData(-300, "5m ago")]
    public void TransactionViewModel_TimeStampFormatted_FormatsRelativeTime(int secondsAgo, string expected)
    {
        // Arrange
        var tx = new TransactionViewModel
        {
            TxId = "tx123456789012345678901234567890123456789012345678901234567890123",
            RegisterId = "reg123",
            SenderWallet = "addr123",
            Signature = "sig123",
            TimeStamp = DateTime.UtcNow.AddSeconds(secondsAgo)
        };

        // Assert
        tx.TimeStampFormatted.Should().Be(expected);
    }
}
