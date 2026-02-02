// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.Blueprint.Service.Services.Implementation;
using Sorcha.Register.Models;
using System.Text;
using System.Text.Json;

namespace Sorcha.Blueprint.Service.Tests.Services;

public class PayloadResolverServiceTests
{
    private readonly Mock<ILogger<PayloadResolverService>> _mockLogger;
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly Mock<IRegisterServiceClient> _mockRegisterClient;
    private readonly PayloadResolverService _service;

    public PayloadResolverServiceTests()
    {
        _mockLogger = new Mock<ILogger<PayloadResolverService>>();
        _mockWalletClient = new Mock<IWalletServiceClient>();
        _mockRegisterClient = new Mock<IRegisterServiceClient>();
        _service = new PayloadResolverService(
            _mockLogger.Object,
            _mockWalletClient.Object,
            _mockRegisterClient.Object);
    }

    [Fact]
    public async Task CreateEncryptedPayloadsAsync_WithValidData_ReturnsEncryptedPayloads()
    {
        // Arrange
        var disclosureResults = new Dictionary<string, object>
        {
            ["participant-1"] = new { name = "Alice", age = 30 },
            ["participant-2"] = new { name = "Bob", age = 25 }
        };

        var participantWallets = new Dictionary<string, string>
        {
            ["participant-1"] = "wallet-alice",
            ["participant-2"] = "wallet-bob"
        };

        var senderWallet = "wallet-sender";

        // Mock wallet encryption
        _mockWalletClient
            .Setup(x => x.EncryptPayloadAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string wallet, byte[] data, CancellationToken _) =>
                Encoding.UTF8.GetBytes($"ENCRYPTED_FOR:{wallet}:{Encoding.UTF8.GetString(data)}"));

        // Act
        var result = await _service.CreateEncryptedPayloadsAsync(
            disclosureResults,
            participantWallets,
            senderWallet);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("wallet-alice");
        result.Should().ContainKey("wallet-bob");
        result["wallet-alice"].Should().NotBeEmpty();
        result["wallet-bob"].Should().NotBeEmpty();

        // Verify encryption was called
        var alicePayload = Encoding.UTF8.GetString(result["wallet-alice"]);
        alicePayload.Should().Contain("ENCRYPTED_FOR:wallet-alice:");
    }

    [Fact]
    public async Task CreateEncryptedPayloadsAsync_WithMissingWallet_SkipsParticipant()
    {
        // Arrange
        var disclosureResults = new Dictionary<string, object>
        {
            ["participant-1"] = new { name = "Alice" },
            ["participant-2"] = new { name = "Bob" }
        };

        var participantWallets = new Dictionary<string, string>
        {
            ["participant-1"] = "wallet-alice"
            // participant-2 wallet missing
        };

        var senderWallet = "wallet-sender";

        // Mock wallet encryption
        _mockWalletClient
            .Setup(x => x.EncryptPayloadAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string wallet, byte[] data, CancellationToken _) =>
                Encoding.UTF8.GetBytes($"ENCRYPTED:{wallet}"));

        // Act
        var result = await _service.CreateEncryptedPayloadsAsync(
            disclosureResults,
            participantWallets,
            senderWallet);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey("wallet-alice");
        result.Should().NotContainKey("wallet-bob");
    }

    [Fact]
    public async Task CreateEncryptedPayloadsAsync_WithNullDisclosureResults_ThrowsArgumentNullException()
    {
        // Arrange
        var participantWallets = new Dictionary<string, string>();
        var senderWallet = "wallet-sender";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateEncryptedPayloadsAsync(null!, participantWallets, senderWallet));
    }

    [Fact]
    public async Task CreateEncryptedPayloadsAsync_WithNullParticipantWallets_ThrowsArgumentNullException()
    {
        // Arrange
        var disclosureResults = new Dictionary<string, object>();
        var senderWallet = "wallet-sender";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateEncryptedPayloadsAsync(disclosureResults, null!, senderWallet));
    }

    [Fact]
    public async Task CreateEncryptedPayloadsAsync_WithNullSenderWallet_ThrowsArgumentException()
    {
        // Arrange
        var disclosureResults = new Dictionary<string, object>();
        var participantWallets = new Dictionary<string, string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateEncryptedPayloadsAsync(disclosureResults, participantWallets, null!));
    }

    [Fact]
    public async Task AggregateHistoricalDataAsync_WithValidTransactions_ReturnsAggregatedData()
    {
        // Arrange
        var registerAddress = "register-1";
        var transactionIds = new[] { "tx-1", "tx-2", "tx-3" };
        var wallet = "wallet-alice";

        // Mock register client to return transactions with payloads
        _mockRegisterClient
            .Setup(x => x.GetTransactionAsync(registerAddress, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string reg, string txId, CancellationToken _) => new TransactionModel
            {
                TxId = txId,
                Payloads = new[]
                {
                    new PayloadModel
                    {
                        WalletAccess = new[] { wallet },
                        Data = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"previousTxId\":\"{txId}\"}}"))
                    }
                }
            });

        // Mock wallet client to return decrypted data
        _mockWalletClient
            .Setup(x => x.DecryptPayloadAsync(wallet, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string w, byte[] data, CancellationToken _) => data);

        // Act
        var result = await _service.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("previousTxId");
    }

    [Fact]
    public async Task AggregateHistoricalDataAsync_WithEmptyTransactionIds_ReturnsEmpty()
    {
        // Arrange
        var registerAddress = "register-1";
        var transactionIds = Array.Empty<string>();
        var wallet = "wallet-alice";

        // Act
        var result = await _service.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregateHistoricalDataAsync_WithNullRegisterAddress_ThrowsArgumentException()
    {
        // Arrange
        var transactionIds = new[] { "tx-1" };
        var wallet = "wallet-alice";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AggregateHistoricalDataAsync(null!, transactionIds, wallet));
    }

    [Fact]
    public async Task AggregateHistoricalDataAsync_WithNullTransactionIds_ThrowsArgumentNullException()
    {
        // Arrange
        var registerAddress = "register-1";
        var wallet = "wallet-alice";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AggregateHistoricalDataAsync(registerAddress, null!, wallet));
    }

    [Fact]
    public async Task AggregateHistoricalDataAsync_WithNullWallet_ThrowsArgumentException()
    {
        // Arrange
        var registerAddress = "register-1";
        var transactionIds = new[] { "tx-1" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AggregateHistoricalDataAsync(registerAddress, transactionIds, null!));
    }

    [Fact]
    public async Task AggregateHistoricalDataAsync_WithDisclosureRules_FiltersData()
    {
        // Arrange
        var registerAddress = "register-1";
        var transactionIds = new[] { "tx-1" };
        var wallet = "wallet-alice";
        var disclosureRules = new[] { "name", "email" };

        // Mock register client
        _mockRegisterClient
            .Setup(x => x.GetTransactionAsync(registerAddress, "tx-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionModel
            {
                TxId = "tx-1",
                Payloads = new[]
                {
                    new PayloadModel
                    {
                        WalletAccess = new[] { wallet },
                        Data = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"name\":\"Alice\",\"email\":\"alice@test.com\",\"secret\":\"hidden\"}"))
                    }
                }
            });

        // Mock wallet client
        _mockWalletClient
            .Setup(x => x.DecryptPayloadAsync(wallet, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string w, byte[] data, CancellationToken _) => data);

        // Act
        var result = await _service.AggregateHistoricalDataAsync(
            registerAddress,
            transactionIds,
            wallet,
            disclosureRules);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("name");
        result.Should().ContainKey("email");
        result.Should().NotContainKey("secret"); // Filtered out
    }

    [Fact]
    public async Task CreateEncryptedPayloadsAsync_WithComplexData_SerializesCorrectly()
    {
        // Arrange
        var complexData = new
        {
            person = new { name = "Alice", age = 30 },
            items = new[] { "item1", "item2" },
            nested = new { deep = new { value = 42 } }
        };

        var disclosureResults = new Dictionary<string, object>
        {
            ["participant-1"] = complexData
        };

        var participantWallets = new Dictionary<string, string>
        {
            ["participant-1"] = "wallet-alice"
        };

        var senderWallet = "wallet-sender";

        // Mock wallet encryption to return the data with a prefix
        _mockWalletClient
            .Setup(x => x.EncryptPayloadAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string wallet, byte[] data, CancellationToken _) =>
                Encoding.UTF8.GetBytes($"ENCRYPTED_FOR:{wallet}:{Encoding.UTF8.GetString(data)}"));

        // Act
        var result = await _service.CreateEncryptedPayloadsAsync(
            disclosureResults,
            participantWallets,
            senderWallet);

        // Assert
        result.Should().ContainKey("wallet-alice");
        var payload = result["wallet-alice"];
        payload.Should().NotBeEmpty();

        // Verify it contains serialized JSON (after stub prefix)
        var payloadStr = Encoding.UTF8.GetString(payload);
        payloadStr.Should().Contain("Alice");
        payloadStr.Should().Contain("item1");
    }
}
