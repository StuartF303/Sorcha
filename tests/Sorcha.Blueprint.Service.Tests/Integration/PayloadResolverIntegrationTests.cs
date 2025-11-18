// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Service.Clients;
using Sorcha.Blueprint.Service.Services.Implementation;
using Sorcha.Register.Models;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace Sorcha.Blueprint.Service.Tests.Integration;

/// <summary>
/// Integration tests for PayloadResolverService with Wallet and Register service clients
/// </summary>
public class PayloadResolverIntegrationTests
{
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly Mock<IRegisterServiceClient> _mockRegisterClient;
    private readonly PayloadResolverService _payloadResolver;
    private readonly ILogger<PayloadResolverService> _logger;

    public PayloadResolverIntegrationTests()
    {
        _mockWalletClient = new Mock<IWalletServiceClient>();
        _mockRegisterClient = new Mock<IRegisterServiceClient>();
        _logger = Mock.Of<ILogger<PayloadResolverService>>();

        _payloadResolver = new PayloadResolverService(
            _logger,
            _mockWalletClient.Object,
            _mockRegisterClient.Object);
    }

    [Fact]
    public async Task CreateEncryptedPayloads_WithMultipleParticipants_EncryptsForEach()
    {
        // Arrange
        var disclosureResults = new Dictionary<string, object>
        {
            ["participant1"] = new { name = "Alice", age = 30 },
            ["participant2"] = new { name = "Bob", age = 25 }
        };

        var participantWallets = new Dictionary<string, string>
        {
            ["participant1"] = "wallet-alice",
            ["participant2"] = "wallet-bob"
        };

        var senderWallet = "wallet-sender";

        // Setup mock to return encrypted payloads
        _mockWalletClient
            .Setup(x => x.EncryptPayloadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string wallet, byte[] payload, CancellationToken ct) =>
            {
                // Simulate encryption by prefixing with wallet address
                var prefix = Encoding.UTF8.GetBytes($"ENCRYPTED_FOR_{wallet}:");
                var result = new byte[prefix.Length + payload.Length];
                Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
                Buffer.BlockCopy(payload, 0, result, prefix.Length, payload.Length);
                return result;
            });

        // Act
        var result = await _payloadResolver.CreateEncryptedPayloadsAsync(
            disclosureResults,
            participantWallets,
            senderWallet);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("wallet-alice");
        result.Should().ContainKey("wallet-bob");

        // Verify encryption was called for each wallet
        _mockWalletClient.Verify(
            x => x.EncryptPayloadAsync("wallet-alice", It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockWalletClient.Verify(
            x => x.EncryptPayloadAsync("wallet-bob", It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateEncryptedPayloads_SkipsParticipantsWithoutWallets()
    {
        // Arrange
        var disclosureResults = new Dictionary<string, object>
        {
            ["participant1"] = new { name = "Alice" },
            ["participant2"] = new { name = "Bob" },
            ["participant3"] = new { name = "Charlie" }
        };

        var participantWallets = new Dictionary<string, string>
        {
            ["participant1"] = "wallet-alice",
            ["participant2"] = "wallet-bob"
            // participant3 has no wallet
        };

        _mockWalletClient
            .Setup(x => x.EncryptPayloadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string wallet, byte[] payload, CancellationToken ct) =>
                Encoding.UTF8.GetBytes($"encrypted-{wallet}"));

        // Act
        var result = await _payloadResolver.CreateEncryptedPayloadsAsync(
            disclosureResults,
            participantWallets,
            "wallet-sender");

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("wallet-alice");
        result.Should().ContainKey("wallet-bob");
        result.Should().NotContainKey("wallet-charlie");

        // Verify encryption was only called for participants with wallets
        _mockWalletClient.Verify(
            x => x.EncryptPayloadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AggregateHistoricalData_WithMultipleTransactions_MergesData()
    {
        // Arrange
        var registerAddress = "register-123";
        var transactionIds = new[] { "tx-1", "tx-2", "tx-3" };
        var wallet = "wallet-user";

        // Setup mock transactions with encrypted payloads
        SetupTransactionWithPayload("tx-1", wallet, new { field1 = "value1", field2 = "value2" });
        SetupTransactionWithPayload("tx-2", wallet, new { field2 = "updated2", field3 = "value3" });
        SetupTransactionWithPayload("tx-3", wallet, new { field1 = "updated1", field4 = "value4" });

        // Act
        var result = await _payloadResolver.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet);

        // Assert
        result.Should().HaveCount(4);
        result["field1"].ToString().Should().Be("updated1"); // Latest value wins
        result["field2"].ToString().Should().Be("updated2"); // Latest value wins
        result["field3"].ToString().Should().Be("value3");
        result["field4"].ToString().Should().Be("value4");

        // Verify all transactions were retrieved
        _mockRegisterClient.Verify(
            x => x.GetTransactionAsync(registerAddress, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        // Verify all payloads were decrypted
        _mockWalletClient.Verify(
            x => x.DecryptPayloadAsync(wallet, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task AggregateHistoricalData_WithDisclosureRules_FiltersFields()
    {
        // Arrange
        var registerAddress = "register-123";
        var transactionIds = new[] { "tx-1" };
        var wallet = "wallet-user";
        var disclosureRules = new[] { "field1", "field3" }; // Only allow field1 and field3

        SetupTransactionWithPayload("tx-1", wallet, new
        {
            field1 = "value1",
            field2 = "value2",
            field3 = "value3",
            field4 = "value4"
        });

        // Act
        var result = await _payloadResolver.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet,
            disclosureRules);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("field1");
        result.Should().ContainKey("field3");
        result.Should().NotContainKey("field2");
        result.Should().NotContainKey("field4");
    }

    [Fact]
    public async Task AggregateHistoricalData_SkipsTransactionsNotFound()
    {
        // Arrange
        var registerAddress = "register-123";
        var transactionIds = new[] { "tx-1", "tx-missing", "tx-2" };
        var wallet = "wallet-user";

        SetupTransactionWithPayload("tx-1", wallet, new { field1 = "value1" });
        // tx-missing returns null
        _mockRegisterClient
            .Setup(x => x.GetTransactionAsync(registerAddress, "tx-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionModel?)null);
        SetupTransactionWithPayload("tx-2", wallet, new { field2 = "value2" });

        // Act
        var result = await _payloadResolver.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("field1");
        result.Should().ContainKey("field2");

        // Verify only 2 decrypt calls (tx-missing was skipped)
        _mockWalletClient.Verify(
            x => x.DecryptPayloadAsync(wallet, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AggregateHistoricalData_SkipsTransactionsWithoutPayloadForWallet()
    {
        // Arrange
        var registerAddress = "register-123";
        var transactionIds = new[] { "tx-1", "tx-2" };
        var wallet = "wallet-user";

        // tx-1 has payload for wallet-user
        SetupTransactionWithPayload("tx-1", wallet, new { field1 = "value1" });

        // tx-2 has payload for a different wallet
        var tx2 = new TransactionModel
        {
            TxId = "tx-2",
            RegisterId = registerAddress,
            Payloads = new List<PayloadModel>
            {
                new PayloadModel
                {
                    Data = Encoding.UTF8.GetBytes("encrypted-data"),
                    Recipients = new[] { "different-wallet" }
                }
            }
        };

        _mockRegisterClient
            .Setup(x => x.GetTransactionAsync(registerAddress, "tx-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx2);

        // Act
        var result = await _payloadResolver.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("field1");

        // Verify only 1 decrypt call (tx-2 was skipped)
        _mockWalletClient.Verify(
            x => x.DecryptPayloadAsync(wallet, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AggregateHistoricalData_WithEmptyTransactionList_ReturnsEmpty()
    {
        // Arrange
        var registerAddress = "register-123";
        var transactionIds = Array.Empty<string>();
        var wallet = "wallet-user";

        // Act
        var result = await _payloadResolver.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet);

        // Assert
        result.Should().BeEmpty();

        // Verify no service calls were made
        _mockRegisterClient.Verify(
            x => x.GetTransactionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockWalletClient.Verify(
            x => x.DecryptPayloadAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #region Helper Methods

    private void SetupTransactionWithPayload(string txId, string wallet, object data)
    {
        var dataJson = JsonSerializer.SerializeToUtf8Bytes(data);
        var encryptedData = Encoding.UTF8.GetBytes($"encrypted-{txId}");

        var transaction = new TransactionModel
        {
            TxId = txId,
            RegisterId = "register-123",
            Payloads = new List<PayloadModel>
            {
                new PayloadModel
                {
                    Data = encryptedData,
                    Recipients = new[] { wallet }
                }
            }
        };

        _mockRegisterClient
            .Setup(x => x.GetTransactionAsync("register-123", txId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _mockWalletClient
            .Setup(x => x.DecryptPayloadAsync(wallet, encryptedData, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataJson);
    }

    #endregion
}
