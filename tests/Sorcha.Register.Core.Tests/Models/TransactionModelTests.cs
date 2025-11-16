// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Models;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Sorcha.Register.Core.Tests.Models;

public class TransactionModelTests
{
    [Fact]
    public void TransactionModel_DefaultConstructor_ShouldSetDefaultValues()
    {
        // Act
        var transaction = new TransactionModel();

        // Assert
        transaction.Context.Should().Be("https://sorcha.dev/contexts/blockchain/v1.jsonld");
        transaction.Type.Should().Be("Transaction");
        transaction.Id.Should().BeNull();
        transaction.RegisterId.Should().BeEmpty();
        transaction.TxId.Should().BeEmpty();
        transaction.PrevTxId.Should().BeEmpty();
        transaction.BlockNumber.Should().BeNull();
        transaction.Version.Should().Be(1u);
        transaction.SenderWallet.Should().BeEmpty();
        transaction.RecipientsWallets.Should().BeEmpty();
        transaction.TimeStamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        transaction.MetaData.Should().BeNull();
        transaction.PayloadCount.Should().Be(0ul);
        transaction.Payloads.Should().BeEmpty();
        transaction.Signature.Should().BeEmpty();
    }

    [Fact]
    public void TransactionModel_WithValidProperties_ShouldCreateSuccessfully()
    {
        // Arrange
        var registerId = Guid.NewGuid().ToString("N");
        var txId = new string('a', 64);
        var prevTxId = new string('b', 64);
        var senderWallet = "wallet123";
        var recipients = new[] { "wallet456", "wallet789" };
        var signature = "signature123";

        // Act
        var transaction = new TransactionModel
        {
            RegisterId = registerId,
            TxId = txId,
            PrevTxId = prevTxId,
            BlockNumber = 5,
            Version = 2,
            SenderWallet = senderWallet,
            RecipientsWallets = recipients,
            Signature = signature,
            PayloadCount = 2,
            Payloads = new[] { new PayloadModel(), new PayloadModel() }
        };

        // Assert
        transaction.RegisterId.Should().Be(registerId);
        transaction.TxId.Should().Be(txId);
        transaction.PrevTxId.Should().Be(prevTxId);
        transaction.BlockNumber.Should().Be(5ul);
        transaction.Version.Should().Be(2u);
        transaction.SenderWallet.Should().Be(senderWallet);
        transaction.RecipientsWallets.Should().BeEquivalentTo(recipients);
        transaction.Signature.Should().Be(signature);
        transaction.PayloadCount.Should().Be(2ul);
        transaction.Payloads.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TransactionModel_WithInvalidRegisterId_ShouldFailValidation(string? invalidRegisterId)
    {
        // Arrange
        var transaction = new TransactionModel
        {
            RegisterId = invalidRegisterId!,
            TxId = new string('a', 64),
            SenderWallet = "wallet123",
            Signature = "sig123"
        };

        // Act
        var validationResults = ValidateModel(transaction);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("RegisterId"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TransactionModel_WithInvalidTxId_ShouldFailValidation(string? invalidTxId)
    {
        // Arrange
        var transaction = new TransactionModel
        {
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = invalidTxId!,
            SenderWallet = "wallet123",
            Signature = "sig123"
        };

        // Act
        var validationResults = ValidateModel(transaction);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("TxId"));
    }

    [Fact]
    public void TransactionModel_WithTxIdNot64Characters_ShouldFailValidation()
    {
        // Arrange
        var transaction = new TransactionModel
        {
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = "tooshort",
            SenderWallet = "wallet123",
            Signature = "sig123"
        };

        // Act
        var validationResults = ValidateModel(transaction);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("TxId"));
    }

    [Fact]
    public void TransactionModel_WithPrevTxIdNot64Characters_ShouldFailValidation()
    {
        // Arrange
        var transaction = new TransactionModel
        {
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = new string('a', 64),
            PrevTxId = "tooshort",
            SenderWallet = "wallet123",
            Signature = "sig123"
        };

        // Act
        var validationResults = ValidateModel(transaction);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("PrevTxId"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TransactionModel_WithInvalidSenderWallet_ShouldFailValidation(string? invalidSender)
    {
        // Arrange
        var transaction = new TransactionModel
        {
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = new string('a', 64),
            SenderWallet = invalidSender!,
            Signature = "sig123"
        };

        // Act
        var validationResults = ValidateModel(transaction);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("SenderWallet"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TransactionModel_WithInvalidSignature_ShouldFailValidation(string? invalidSignature)
    {
        // Arrange
        var transaction = new TransactionModel
        {
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = new string('a', 64),
            SenderWallet = "wallet123",
            Signature = invalidSignature!
        };

        // Act
        var validationResults = ValidateModel(transaction);

        // Assert
        validationResults.Should().ContainSingle(v => v.MemberNames.Contains("Signature"));
    }

    [Fact]
    public void GenerateDidUri_ShouldCreateCorrectFormat()
    {
        // Arrange
        var registerId = "abc123";
        var txId = new string('a', 64);
        var transaction = new TransactionModel
        {
            RegisterId = registerId,
            TxId = txId,
            SenderWallet = "wallet123",
            Signature = "sig123"
        };

        // Act
        var didUri = transaction.GenerateDidUri();

        // Assert
        didUri.Should().Be($"did:sorcha:register:{registerId}/tx/{txId}");
    }

    [Fact]
    public void TransactionModel_WithMetaData_ShouldStoreCorrectly()
    {
        // Arrange
        var metadata = new TransactionMetaData
        {
            RegisterId = "reg123",
            BlueprintId = "blueprint456",
            InstanceId = "instance789",
            ActionId = 1
        };

        var transaction = new TransactionModel
        {
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = new string('a', 64),
            SenderWallet = "wallet123",
            Signature = "sig123",
            MetaData = metadata
        };

        // Act & Assert
        transaction.MetaData.Should().NotBeNull();
        transaction.MetaData!.BlueprintId.Should().Be("blueprint456");
        transaction.MetaData.InstanceId.Should().Be("instance789");
        transaction.MetaData.ActionId.Should().Be(1u);
    }

    [Fact]
    public void TransactionModel_WithMultipleRecipients_ShouldStoreAll()
    {
        // Arrange
        var recipients = new[] { "wallet1", "wallet2", "wallet3", "wallet4" };
        var transaction = new TransactionModel
        {
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = new string('a', 64),
            SenderWallet = "sender123",
            RecipientsWallets = recipients,
            Signature = "sig123"
        };

        // Act & Assert
        transaction.RecipientsWallets.Should().HaveCount(4);
        transaction.RecipientsWallets.Should().Contain("wallet1");
        transaction.RecipientsWallets.Should().Contain("wallet4");
    }

    [Fact]
    public void TransactionModel_JsonLdProperties_ShouldBeAccessible()
    {
        // Arrange
        var transaction = new TransactionModel
        {
            RegisterId = "reg123",
            TxId = new string('a', 64),
            SenderWallet = "wallet123",
            Signature = "sig123"
        };

        // Act
        transaction.Context = "custom-context";
        transaction.Type = "CustomTransaction";
        transaction.Id = "custom-id";

        // Assert
        transaction.Context.Should().Be("custom-context");
        transaction.Type.Should().Be("CustomTransaction");
        transaction.Id.Should().Be("custom-id");
    }

    [Fact]
    public void TransactionModel_FullyPopulated_ShouldPassValidation()
    {
        // Arrange
        var transaction = new TransactionModel
        {
            Context = "https://sorcha.dev/contexts/blockchain/v1.jsonld",
            Type = "Transaction",
            Id = "did:sorcha:register:reg123/tx/abc",
            RegisterId = Guid.NewGuid().ToString("N"),
            TxId = new string('a', 64),
            PrevTxId = new string('b', 64),
            BlockNumber = 10,
            Version = 1,
            SenderWallet = "sender-wallet-address",
            RecipientsWallets = new[] { "recipient1", "recipient2" },
            TimeStamp = DateTime.UtcNow,
            MetaData = new TransactionMetaData
            {
                RegisterId = "reg123",
                BlueprintId = "bp123"
            },
            PayloadCount = 1,
            Payloads = new[] { new PayloadModel() },
            Signature = "valid-signature"
        };

        // Act
        var validationResults = ValidateModel(transaction);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
