// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Sorcha.Integration.Tests;

/// <summary>
/// End-to-end tests for Register Service integration
/// Tests the complete workflow: Blueprint → Wallet → Register
/// </summary>
public class RegisterServiceEndToEndTests : IAsyncLifetime
{
    private HttpClient? _client;
    private const string BaseUrl = "http://localhost:5000"; // API Gateway
    private string? _testRegisterId;

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
    public async Task CompleteWorkflow_CreateRegister_SubmitTransaction_Seal_ShouldSucceed()
    {
        // Arrange - Create a register
        var createRegisterRequest = new
        {
            title = "E2E Test Register",
            description = "Register for end-to-end testing"
        };

        // Act 1 - Create register
        var createResponse = await _client!.PostAsJsonAsync("/api/registers", createRegisterRequest);
        createResponse.EnsureSuccessStatusCode();

        var register = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        _testRegisterId = register.GetProperty("id").GetString();
        _testRegisterId.Should().NotBeNullOrEmpty();

        // Act 2 - Submit a transaction
        var transaction = new
        {
            transactionType = "Action",
            senderAddress = "wallet-test-001",
            payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Test transaction payload")),
            metadata = new Dictionary<string, object>
            {
                ["blueprintId"] = "blueprint-test-001",
                ["actionId"] = "0"
            }
        };

        var submitResponse = await _client.PostAsJsonAsync($"/api/registers/{_testRegisterId}/transactions", transaction);
        submitResponse.EnsureSuccessStatusCode();

        var submittedTx = await submitResponse.Content.ReadFromJsonAsync<JsonElement>();
        var txId = submittedTx.GetProperty("transactionId").GetString();
        txId.Should().NotBeNullOrEmpty();

        // Act 3 - Retrieve transaction
        var getResponse = await _client.GetAsync($"/api/registers/{_testRegisterId}/transactions/{txId}");
        getResponse.EnsureSuccessStatusCode();

        var retrievedTx = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        retrievedTx.GetProperty("transactionId").GetString().Should().Be(txId);

        // Act 4 - Seal the docket (block)
        var sealResponse = await _client.PostAsync($"/api/registers/{_testRegisterId}/dockets/seal", null);
        sealResponse.EnsureSuccessStatusCode();

        var sealedDocket = await sealResponse.Content.ReadFromJsonAsync<JsonElement>();
        var docketId = sealedDocket.GetProperty("docketId").GetString();
        docketId.Should().NotBeNullOrEmpty();

        // Assert - Verify chain integrity
        var chainResponse = await _client.GetAsync($"/api/registers/{_testRegisterId}/chain");
        chainResponse.EnsureSuccessStatusCode();

        var chain = await chainResponse.Content.ReadFromJsonAsync<JsonElement>();
        chain.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }

    [Fact(Skip = "Requires running services")]
    public async Task SubmitMultipleTransactions_QueryByTimeRange_ShouldSucceed()
    {
        // Arrange - Create register
        var registerResponse = await _client!.PostAsJsonAsync("/api/registers", new
        {
            title = "Query Test Register"
        });
        registerResponse.EnsureSuccessStatusCode();

        var register = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var registerId = register.GetProperty("id").GetString();

        // Act - Submit multiple transactions
        var startTime = DateTimeOffset.UtcNow;
        var transactionIds = new List<string>();

        for (int i = 0; i < 5; i++)
        {
            var tx = new
            {
                transactionType = "Action",
                senderAddress = $"wallet-{i}",
                payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"Transaction {i}"))
            };

            var response = await _client.PostAsJsonAsync($"/api/registers/{registerId}/transactions", tx);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            transactionIds.Add(result.GetProperty("transactionId").GetString()!);

            await Task.Delay(100); // Small delay between transactions
        }

        var endTime = DateTimeOffset.UtcNow;

        // Query transactions by time range
        var queryResponse = await _client.GetAsync(
            $"/api/registers/{registerId}/transactions?startTime={startTime:O}&endTime={endTime:O}");
        queryResponse.EnsureSuccessStatusCode();

        var transactions = await queryResponse.Content.ReadFromJsonAsync<JsonElement>();
        transactions.GetArrayLength().Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact(Skip = "Requires running services")]
    public async Task SubmitTransaction_VerifyChainIntegrity_ShouldSucceed()
    {
        // Create register and submit transactions to build a chain
        var registerResponse = await _client!.PostAsJsonAsync("/api/registers", new
        {
            title = "Chain Integrity Test Register"
        });
        registerResponse.EnsureSuccessStatusCode();

        var register = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var registerId = register.GetProperty("id").GetString();

        // Submit transactions and seal dockets to build a chain
        for (int i = 0; i < 3; i++)
        {
            // Submit transaction
            var tx = new
            {
                transactionType = "Action",
                senderAddress = $"wallet-{i}",
                payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"Chain transaction {i}"))
            };

            await _client.PostAsJsonAsync($"/api/registers/{registerId}/transactions", tx);

            // Seal docket
            await _client.PostAsync($"/api/registers/{registerId}/dockets/seal", null);
        }

        // Verify chain
        var validationResponse = await _client.GetAsync($"/api/registers/{registerId}/chain");
        validationResponse.EnsureSuccessStatusCode();

        var validation = await validationResponse.Content.ReadFromJsonAsync<JsonElement>();
        validation.GetProperty("isValid").GetBoolean().Should().BeTrue();
        validation.GetProperty("docketCount").GetInt32().Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact(Skip = "Requires running services")]
    public async Task QueryTransactions_BySender_ShouldReturnFiltered()
    {
        // Create register
        var registerResponse = await _client!.PostAsJsonAsync("/api/registers", new
        {
            title = "Query Filter Test Register"
        });
        registerResponse.EnsureSuccessStatusCode();

        var register = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var registerId = register.GetProperty("id").GetString();

        // Submit transactions from different senders
        var targetSender = "wallet-target";
        await _client.PostAsJsonAsync($"/api/registers/{registerId}/transactions", new
        {
            transactionType = "Action",
            senderAddress = targetSender,
            payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Target transaction 1"))
        });

        await _client.PostAsJsonAsync($"/api/registers/{registerId}/transactions", new
        {
            transactionType = "Action",
            senderAddress = "wallet-other",
            payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Other transaction"))
        });

        await _client.PostAsJsonAsync($"/api/registers/{registerId}/transactions", new
        {
            transactionType = "Action",
            senderAddress = targetSender,
            payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Target transaction 2"))
        });

        // Query by sender
        var queryResponse = await _client.GetAsync(
            $"/api/registers/{registerId}/transactions?senderAddress={targetSender}");
        queryResponse.EnsureSuccessStatusCode();

        var transactions = await queryResponse.Content.ReadFromJsonAsync<JsonElement>();
        var items = transactions.EnumerateArray().ToList();

        items.Should().HaveCountGreaterThanOrEqualTo(2);
        items.Should().OnlyContain(tx =>
            tx.GetProperty("senderAddress").GetString() == targetSender);
    }

    [Fact(Skip = "Requires running services")]
    public async Task FullWorkflow_Blueprint_Wallet_Register_ShouldIntegrate()
    {
        // This is the ultimate E2E test: Blueprint → Wallet → Register

        // Step 1: Create Wallet
        var walletResponse = await _client!.PostAsJsonAsync("/api/wallets", new
        {
            title = "Full Integration Wallet",
            keyType = "ED25519"
        });
        walletResponse.EnsureSuccessStatusCode();

        var wallet = await walletResponse.Content.ReadFromJsonAsync<JsonElement>();
        var walletAddress = wallet.GetProperty("walletAddress").GetString();

        // Step 2: Create Register
        var registerResponse = await _client.PostAsJsonAsync("/api/registers", new
        {
            title = "Full Integration Register"
        });
        registerResponse.EnsureSuccessStatusCode();

        var register = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var registerId = register.GetProperty("id").GetString();

        // Step 3: Create and Publish Blueprint
        var blueprint = new
        {
            title = "Full Integration Blueprint",
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
                    title = "Send Data",
                    sender = "sender"
                }
            }
        };

        var blueprintResponse = await _client.PostAsJsonAsync("/api/blueprints", blueprint);
        blueprintResponse.EnsureSuccessStatusCode();

        var createdBlueprint = await blueprintResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blueprintId = createdBlueprint.GetProperty("id").GetString();

        await _client.PostAsync($"/api/blueprints/{blueprintId}/publish", null);

        // Step 4: Submit Action (Blueprint Service creates transaction)
        var actionResponse = await _client.PostAsJsonAsync("/api/actions", new
        {
            blueprintId = blueprintId,
            actionId = "0",
            senderWallet = walletAddress,
            registerAddress = registerId,
            payloadData = new Dictionary<string, object>
            {
                ["message"] = "Full integration test"
            }
        });
        actionResponse.EnsureSuccessStatusCode();

        var action = await actionResponse.Content.ReadFromJsonAsync<JsonElement>();
        var actionTxHash = action.GetProperty("transactionHash").GetString();

        // Step 5: Verify transaction exists in action store
        var actionDetailsResponse = await _client.GetAsync(
            $"/api/actions/{walletAddress}/{registerId}/{actionTxHash}");
        actionDetailsResponse.EnsureSuccessStatusCode();

        // Step 6: In a real scenario, the transaction would be submitted to the register
        // For now, we verify the action was created successfully
        var actionDetails = await actionDetailsResponse.Content.ReadFromJsonAsync<JsonElement>();
        actionDetails.GetProperty("blueprintId").GetString().Should().Be(blueprintId);
        actionDetails.GetProperty("actionId").GetString().Should().Be("0");
        actionDetails.GetProperty("senderWallet").GetString().Should().Be(walletAddress);
        actionDetails.GetProperty("registerAddress").GetString().Should().Be(registerId);
    }

    [Fact(Skip = "Requires running services")]
    public async Task RegisterService_ODataQuery_ShouldSupportFiltering()
    {
        // Test OData V4 query capabilities
        var registerResponse = await _client!.PostAsJsonAsync("/api/registers", new
        {
            title = "OData Test Register"
        });
        registerResponse.EnsureSuccessStatusCode();

        var register = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var registerId = register.GetProperty("id").GetString();

        // Submit test transactions
        for (int i = 0; i < 10; i++)
        {
            await _client.PostAsJsonAsync($"/api/registers/{registerId}/transactions", new
            {
                transactionType = i % 2 == 0 ? "Action" : "Rejection",
                senderAddress = $"wallet-{i}",
                payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"OData test {i}"))
            });
        }

        // Query with OData filters
        var odataResponse = await _client.GetAsync(
            $"/api/registers/{registerId}/transactions?$filter=transactionType eq 'Action'&$top=5");
        odataResponse.EnsureSuccessStatusCode();

        var results = await odataResponse.Content.ReadFromJsonAsync<JsonElement>();
        results.GetArrayLength().Should().BeLessThanOrEqualTo(5);
    }
}
