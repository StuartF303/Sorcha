using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Sorcha.TransactionHandler.Core;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Models;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Enums;

namespace Sorcha.TransactionHandler.Tests.Integration;

/// <summary>
/// Integration tests for multi-recipient transaction scenarios.
/// </summary>
public class MultiRecipientTests
{
    private readonly CryptoModule _cryptoModule;
    private readonly HashProvider _hashProvider;
    private readonly SymmetricCrypto _symmetricCrypto;

    public MultiRecipientTests()
    {
        _cryptoModule = new CryptoModule();
        _hashProvider = new HashProvider();
        _symmetricCrypto = new SymmetricCrypto();
    }

    [Fact]
    public async Task Transaction_WithMultipleRecipients_ShouldSucceed()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var recipients = new[]
        {
            "ws1recipient1",
            "ws1recipient2",
            "ws1recipient3",
            "ws1recipient4",
            "ws1recipient5"
        };

        // Act
        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients(recipients)
            .WithMetadata("{\"type\": \"multi_transfer\"}")
            .SignAsync(wallet.PrivateKeyWif);

        var transactionResult = builderResult.Build();

        // Assert
        Assert.True(transactionResult.IsSuccess);
        var transaction = transactionResult.Value!;
        Assert.Equal(5, transaction.Recipients?.Length);
        Assert.All(recipients, r => Assert.Contains(r, transaction.Recipients!));
    }

    [Fact]
    public async Task Payload_WithMultipleRecipients_ShouldCreateSeparateAccess()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var recipients = new[]
        {
            "ws1recipient1",
            "ws1recipient2",
            "ws1recipient3"
        };

        var payloadData = System.Text.Encoding.UTF8.GetBytes("Sensitive data for multiple recipients");

        // Act
        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients(recipients)
            .AddPayload(payloadData, recipients)
            .SignAsync(wallet.PrivateKeyWif);

        var transactionResult = builderResult.Build();

        // Assert
        Assert.True(transactionResult.IsSuccess);
        var transaction = transactionResult.Value!;

        var payloads = await transaction.PayloadManager.GetAllAsync();
        var payload = payloads.First();
        var info = payload.GetInfo();

        Assert.NotNull(info.AccessibleBy);
        Assert.Equal(3, info.AccessibleBy.Length);
    }

    [Fact]
    public async Task MultiplePayloads_WithDifferentRecipients_ShouldSucceed()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var allRecipients = new[] { "ws1recipient1", "ws1recipient2", "ws1recipient3" };
        var payload1Recipients = new[] { "ws1recipient1" };
        var payload2Recipients = new[] { "ws1recipient2", "ws1recipient3" };
        var payload3Recipients = new[] { "ws1recipient1", "ws1recipient2", "ws1recipient3" };

        var payload1Data = System.Text.Encoding.UTF8.GetBytes("Data for recipient 1 only");
        var payload2Data = System.Text.Encoding.UTF8.GetBytes("Data for recipients 2 and 3");
        var payload3Data = System.Text.Encoding.UTF8.GetBytes("Data for all recipients");

        // Act
        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients(allRecipients)
            .AddPayload(payload1Data, payload1Recipients)
            .AddPayload(payload2Data, payload2Recipients)
            .AddPayload(payload3Data, payload3Recipients)
            .SignAsync(wallet.PrivateKeyWif);

        var transactionResult = builderResult.Build();

        // Assert
        Assert.True(transactionResult.IsSuccess);
        var transaction = transactionResult.Value!;

        var payloads = await transaction.PayloadManager.GetAllAsync();
        var payloadList = payloads.ToList();

        Assert.Equal(3, payloadList.Count);

        // Verify first payload accessibility
        var payload1Info = payloadList[0].GetInfo();
        Assert.Single(payload1Info.AccessibleBy);
        Assert.Contains("ws1recipient1", payload1Info.AccessibleBy);

        // Verify second payload accessibility
        var payload2Info = payloadList[1].GetInfo();
        Assert.Equal(2, payload2Info.AccessibleBy.Length);
        Assert.Contains("ws1recipient2", payload2Info.AccessibleBy);
        Assert.Contains("ws1recipient3", payload2Info.AccessibleBy);

        // Verify third payload accessibility
        var payload3Info = payloadList[2].GetInfo();
        Assert.Equal(3, payload3Info.AccessibleBy.Length);
    }

    [Fact]
    public async Task LargeNumberOfRecipients_ShouldHandleCorrectly()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        // Create 100 recipients
        var recipients = Enumerable.Range(1, 100)
            .Select(i => $"ws1recipient{i:D3}")
            .ToArray();

        // Act
        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients(recipients)
            .WithMetadata("{\"type\": \"bulk_transfer\"}")
            .SignAsync(wallet.PrivateKeyWif);

        var transactionResult = builderResult.Build();

        // Assert
        Assert.True(transactionResult.IsSuccess);
        var transaction = transactionResult.Value!;
        Assert.Equal(100, transaction.Recipients?.Length);
    }

    [Fact]
    public async Task Payload_WithSingleRecipient_ShouldWorkCorrectly()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var singleRecipient = new[] { "ws1singlerecipient" };
        var payloadData = System.Text.Encoding.UTF8.GetBytes("Private data for single recipient");

        // Act
        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients(singleRecipient)
            .AddPayload(payloadData, singleRecipient)
            .SignAsync(wallet.PrivateKeyWif);

        var transactionResult = builderResult.Build();

        // Assert
        Assert.True(transactionResult.IsSuccess);
        var transaction = transactionResult.Value!;

        var payloads = await transaction.PayloadManager.GetAllAsync();
        var payload = payloads.First();
        var info = payload.GetInfo();

        Assert.Single(info.AccessibleBy);
        Assert.Equal("ws1singlerecipient", info.AccessibleBy[0]);
    }

    [Fact]
    public async Task Transaction_WithDuplicateRecipients_ShouldHandleGracefully()
    {
        // Arrange
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var recipientsWithDuplicates = new[]
        {
            "ws1recipient1",
            "ws1recipient2",
            "ws1recipient1", // Duplicate
            "ws1recipient3"
        };

        // Act
        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients(recipientsWithDuplicates)
            .SignAsync(wallet.PrivateKeyWif);

        var transactionResult = builderResult.Build();

        // Assert
        Assert.True(transactionResult.IsSuccess);
        var transaction = transactionResult.Value!;
        Assert.Equal(4, transaction.Recipients?.Length); // Should contain all entries including duplicate
    }

    [Fact]
    public async Task ComplexMultiRecipientScenario_ShouldSucceed()
    {
        // Arrange - Simulate a real-world multi-party contract
        var builder = new TransactionBuilder(_cryptoModule, _hashProvider, _symmetricCrypto);
        var wallet = await TestHelpers.GenerateTestWalletAsync(WalletNetworks.ED25519);

        var contractParties = new[] { "ws1party1", "ws1party2", "ws1party3" };
        var auditors = new[] { "ws1auditor1", "ws1auditor2" };
        var allRecipients = contractParties.Concat(auditors).ToArray();

        var contractData = System.Text.Encoding.UTF8.GetBytes("Contract terms and conditions");
        var auditData = System.Text.Encoding.UTF8.GetBytes("Audit trail data");
        var publicData = System.Text.Encoding.UTF8.GetBytes("Public announcement");

        // Act
        var builderResult = await builder
            .Create(TransactionVersion.V1)
            .WithRecipients(allRecipients)
            .WithMetadata("{\"type\": \"smart_contract\", \"parties\": 3, \"auditors\": 2}")
            .AddPayload(contractData, contractParties) // Only parties see contract
            .AddPayload(auditData, auditors) // Only auditors see audit data
            .AddPayload(publicData, allRecipients) // Everyone sees public data
            .SignAsync(wallet.PrivateKeyWif);

        var transactionResult = builderResult.Build();

        // Assert
        Assert.True(transactionResult.IsSuccess);
        var transaction = transactionResult.Value!;
        Assert.Equal(5, transaction.Recipients?.Length);

        var payloads = await transaction.PayloadManager.GetAllAsync();
        Assert.Equal(3, payloads.Count());

        // Verify access control
        var payloadList = payloads.ToList();
        Assert.Equal(3, payloadList[0].GetInfo().AccessibleBy.Length); // Contract parties
        Assert.Equal(2, payloadList[1].GetInfo().AccessibleBy.Length); // Auditors
        Assert.Equal(5, payloadList[2].GetInfo().AccessibleBy.Length); // All recipients
    }
}
