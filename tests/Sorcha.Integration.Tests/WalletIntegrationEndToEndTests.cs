// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Sorcha.Integration.Tests;

/// <summary>
/// End-to-end tests for Wallet Service integration with Blueprint Service
/// Tests the full workflow: Blueprint → Wallet → Transaction Signing
/// </summary>
public class WalletIntegrationEndToEndTests : IAsyncLifetime
{
    private HttpClient? _client;
    private const string BaseUrl = "http://localhost:5000"; // API Gateway
    private string? _testWalletId;

    public async ValueTask InitializeAsync()
    {
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        await Task.Delay(1000); // Wait for services
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact(Skip = "Requires running services")]
    public async Task CreateWallet_SignTransaction_ShouldSucceed()
    {
        // Arrange - Create a new wallet
        var createWalletRequest = new
        {
            title = "E2E Test Wallet",
            description = "Wallet for end-to-end testing",
            keyType = "ED25519"
        };

        // Act 1 - Create wallet
        var createResponse = await _client!.PostAsJsonAsync("/api/wallets", createWalletRequest);
        createResponse.EnsureSuccessStatusCode();

        var wallet = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        _testWalletId = wallet.GetProperty("id").GetString();
        _testWalletId.Should().NotBeNullOrEmpty();

        // Act 2 - Get wallet details
        var getResponse = await _client.GetAsync($"/api/wallets/{_testWalletId}");
        getResponse.EnsureSuccessStatusCode();

        var walletDetails = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        walletDetails.GetProperty("title").GetString().Should().Be("E2E Test Wallet");

        // Act 3 - Sign a transaction
        var signRequest = new
        {
            data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Test transaction data")),
            algorithm = "ED25519"
        };

        var signResponse = await _client.PostAsJsonAsync($"/api/wallets/{_testWalletId}/sign", signRequest);
        signResponse.EnsureSuccessStatusCode();

        var signResult = await signResponse.Content.ReadFromJsonAsync<JsonElement>();
        var signature = signResult.GetProperty("signature").GetString();
        signature.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Requires running services")]
    public async Task EncryptDecrypt_Payload_ShouldSucceed()
    {
        // This test validates payload encryption/decryption workflow
        // Arrange - Create wallet (reuse if exists)
        var createWalletRequest = new
        {
            title = "Encryption Test Wallet",
            keyType = "ED25519"
        };

        var createResponse = await _client!.PostAsJsonAsync("/api/wallets", createWalletRequest);
        createResponse.EnsureSuccessStatusCode();

        var wallet = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var walletId = wallet.GetProperty("id").GetString();

        // Act 1 - Encrypt payload
        var payloadData = new Dictionary<string, object>
        {
            ["itemName"] = "Confidential Item",
            ["price"] = 999.99,
            ["secret"] = "top-secret-data"
        };

        var encryptRequest = new
        {
            data = JsonSerializer.Serialize(payloadData),
            recipientWalletId = walletId
        };

        var encryptResponse = await _client.PostAsJsonAsync($"/api/wallets/{walletId}/encrypt", encryptRequest);
        encryptResponse.EnsureSuccessStatusCode();

        var encryptResult = await encryptResponse.Content.ReadFromJsonAsync<JsonElement>();
        var encryptedData = encryptResult.GetProperty("encryptedData").GetString();
        encryptedData.Should().NotBeNullOrEmpty();

        // Act 2 - Decrypt payload
        var decryptRequest = new
        {
            encryptedData = encryptedData
        };

        var decryptResponse = await _client.PostAsJsonAsync($"/api/wallets/{walletId}/decrypt", decryptRequest);
        decryptResponse.EnsureSuccessStatusCode();

        var decryptResult = await decryptResponse.Content.ReadFromJsonAsync<JsonElement>();
        var decryptedData = decryptResult.GetProperty("data").GetString();
        decryptedData.Should().NotBeNullOrEmpty();

        // Assert - Decrypted data should match original
        var recovered = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(decryptedData!);
        recovered.Should().NotBeNull();
        recovered!["itemName"].GetString().Should().Be("Confidential Item");
    }

    [Fact(Skip = "Requires running services")]
    public async Task BlueprintAction_WithWalletSigning_ShouldSucceed()
    {
        // This test validates the complete Blueprint → Wallet integration
        // Arrange - Create wallet
        var createWalletResponse = await _client!.PostAsJsonAsync("/api/wallets", new
        {
            title = "Blueprint Integration Wallet",
            keyType = "ED25519"
        });
        createWalletResponse.EnsureSuccessStatusCode();

        var wallet = await createWalletResponse.Content.ReadFromJsonAsync<JsonElement>();
        var walletId = wallet.GetProperty("id").GetString();
        var walletAddress = wallet.GetProperty("walletAddress").GetString();

        // Create a test blueprint
        var blueprint = new
        {
            title = "Wallet Integration Test Blueprint",
            participants = new[]
            {
                new { id = "sender", name = "Sender" },
                new { id = "receiver", name = "Receiver" }
            },
            actions = new[]
            {
                new
                {
                    id = "0",
                    title = "Submit Data",
                    sender = "sender"
                }
            }
        };

        var blueprintResponse = await _client.PostAsJsonAsync("/api/blueprints", blueprint);
        blueprintResponse.EnsureSuccessStatusCode();

        var createdBlueprint = await blueprintResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blueprintId = createdBlueprint.GetProperty("id").GetString();

        // Publish blueprint
        await _client.PostAsync($"/api/blueprints/{blueprintId}/publish", null);

        // Submit action with wallet
        var actionRequest = new
        {
            blueprintId = blueprintId,
            actionId = "0",
            senderWallet = walletAddress,
            registerAddress = "register-test-001",
            payloadData = new Dictionary<string, object>
            {
                ["data"] = "test data"
            }
        };

        var actionResponse = await _client.PostAsJsonAsync("/api/actions", actionRequest);
        actionResponse.EnsureSuccessStatusCode();

        var actionResult = await actionResponse.Content.ReadFromJsonAsync<JsonElement>();
        var txHash = actionResult.GetProperty("transactionHash").GetString();
        txHash.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Requires running services")]
    public async Task MultipleWallets_DifferentKeyTypes_ShouldSucceed()
    {
        // Test creating wallets with different cryptographic algorithms
        var keyTypes = new[] { "ED25519", "NISTP256", "RSA" };
        var createdWallets = new List<string>();

        foreach (var keyType in keyTypes)
        {
            var request = new
            {
                title = $"{keyType} Test Wallet",
                keyType = keyType
            };

            var response = await _client!.PostAsJsonAsync("/api/wallets", request);
            response.EnsureSuccessStatusCode();

            var wallet = await response.Content.ReadFromJsonAsync<JsonElement>();
            var walletId = wallet.GetProperty("id").GetString();
            walletId.Should().NotBeNullOrEmpty();
            createdWallets.Add(walletId!);
        }

        // Assert
        createdWallets.Should().HaveCount(3);
        createdWallets.Should().OnlyHaveUniqueItems();
    }

    [Fact(Skip = "Requires running services")]
    public async Task WalletDelegation_GrantAndRevoke_ShouldSucceed()
    {
        // Test wallet delegation functionality
        // Create primary wallet
        var primaryWalletResponse = await _client!.PostAsJsonAsync("/api/wallets", new
        {
            title = "Primary Wallet",
            keyType = "ED25519"
        });
        primaryWalletResponse.EnsureSuccessStatusCode();

        var primaryWallet = await primaryWalletResponse.Content.ReadFromJsonAsync<JsonElement>();
        var primaryWalletId = primaryWallet.GetProperty("id").GetString();

        // Create delegate wallet
        var delegateWalletResponse = await _client.PostAsJsonAsync("/api/wallets", new
        {
            title = "Delegate Wallet",
            keyType = "ED25519"
        });
        delegateWalletResponse.EnsureSuccessStatusCode();

        var delegateWallet = await delegateWalletResponse.Content.ReadFromJsonAsync<JsonElement>();
        var delegateWalletId = delegateWallet.GetProperty("id").GetString();

        // Grant delegation
        var grantRequest = new
        {
            delegateWalletId = delegateWalletId,
            permissions = new[] { "sign", "encrypt" }
        };

        var grantResponse = await _client.PostAsJsonAsync($"/api/wallets/{primaryWalletId}/delegations", grantRequest);
        grantResponse.EnsureSuccessStatusCode();

        // Verify delegation exists
        var delegationsResponse = await _client.GetAsync($"/api/wallets/{primaryWalletId}/delegations");
        delegationsResponse.EnsureSuccessStatusCode();

        var delegations = await delegationsResponse.Content.ReadFromJsonAsync<JsonElement>();
        delegations.GetArrayLength().Should().BeGreaterThan(0);

        // Revoke delegation
        var revokeResponse = await _client.DeleteAsync($"/api/wallets/{primaryWalletId}/delegations/{delegateWalletId}");
        revokeResponse.EnsureSuccessStatusCode();
    }
}
