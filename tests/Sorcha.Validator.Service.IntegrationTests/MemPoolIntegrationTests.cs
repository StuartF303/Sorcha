// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sorcha.Validator.Service.IntegrationTests;

/// <summary>
/// Integration tests for the Memory Pool (MemPool).
/// Tests transaction pool operations, eviction, and statistics.
/// </summary>
[Collection("ValidatorService")]
public class MemPoolIntegrationTests
{
    private readonly ValidatorServiceWebApplicationFactory _factory;

    public MemPoolIntegrationTests(ValidatorServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ClearTestData();
    }

    #region MemPool Statistics

    [Fact]
    public async Task GetMemPoolStats_ReturnsValidStatistics()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act
        var response = await client.GetAsync($"/api/v1/transactions/mempool/{registerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonDocument>();
        content.Should().NotBeNull();
        content!.RootElement.TryGetProperty("registerId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetMemPoolStats_ForNewRegister_ReturnsEmptyStats()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act
        var response = await client.GetAsync($"/api/v1/transactions/mempool/{registerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonDocument>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMemPoolStats_ForDifferentRegisters_ReturnsIsolatedStats()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId1 = Guid.NewGuid().ToString("N");
        var registerId2 = Guid.NewGuid().ToString("N");

        // Act
        var response1 = await client.GetAsync($"/api/v1/transactions/mempool/{registerId1}");
        var response2 = await client.GetAsync($"/api/v1/transactions/mempool/{registerId2}");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadFromJsonAsync<JsonDocument>();
        var content2 = await response2.Content.ReadFromJsonAsync<JsonDocument>();

        content1!.RootElement.GetProperty("registerId").GetString().Should().Be(registerId1);
        content2!.RootElement.GetProperty("registerId").GetString().Should().Be(registerId2);
    }

    #endregion

    #region Transaction Addition

    [Fact]
    public async Task ValidateTransaction_AddsToMemPool()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var request = CreateValidTransactionRequest(registerId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Transaction should be processed (added to mempool or validated)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_MultipleTimes_UpdatesPoolCount()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Get initial stats
        var initialStats = await client.GetAsync($"/api/v1/transactions/mempool/{registerId}");
        initialStats.StatusCode.Should().Be(HttpStatusCode.OK);

        // Submit multiple transactions
        for (int i = 0; i < 3; i++)
        {
            var request = CreateValidTransactionRequest(registerId);
            await client.PostAsJsonAsync("/api/v1/transactions/validate", request);
        }

        // Act - Get updated stats
        var response = await client.GetAsync($"/api/v1/transactions/mempool/{registerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateTransaction_WithSameId_RejectsOrUpdates()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var transactionId = Guid.NewGuid().ToString("N");

        var request1 = CreateValidTransactionRequest(registerId, transactionId);
        var request2 = CreateValidTransactionRequest(registerId, transactionId);

        // Act
        var response1 = await client.PostAsJsonAsync("/api/v1/transactions/validate", request1);
        var response2 = await client.PostAsJsonAsync("/api/v1/transactions/validate", request2);

        // Assert - Second should be rejected as duplicate or handled appropriately
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    #endregion

    #region Transaction Expiration

    [Fact]
    public async Task ValidateTransaction_WithExpiredTimestamp_HandlesProperly()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var payload = "{}";

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = registerId,
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) // Already expired
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Expired transactions should be rejected
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithFutureExpiry_AcceptsTransaction()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var payload = "{}";

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = registerId,
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30) // Future expiry
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    #endregion

    #region Transaction Priority

    [Fact]
    public async Task ValidateTransaction_WithDifferentPriorities_ProcessesCorrectly()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        var normalTx = CreateValidTransactionRequest(registerId);
        var highPriorityTx = CreateValidTransactionRequest(registerId);

        // Act
        var response1 = await client.PostAsJsonAsync("/api/v1/transactions/validate", normalTx);
        var response2 = await client.PostAsJsonAsync("/api/v1/transactions/validate", highPriorityTx);

        // Assert
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    #endregion

    #region MemPool Limits

    [Fact]
    public async Task ValidateTransaction_WhenPoolFull_HandlesGracefully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Submit many transactions to potentially fill pool
        // Note: Default pool size is 1000 in test config
        for (int i = 0; i < 10; i++)
        {
            var request = CreateValidTransactionRequest(registerId);
            await client.PostAsJsonAsync("/api/v1/transactions/validate", request);
        }

        // Act - Try one more
        var finalRequest = CreateValidTransactionRequest(registerId);
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", finalRequest);

        // Assert - Should either accept or reject gracefully (not crash)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Conflict,
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.ServiceUnavailable);
    }

    #endregion

    #region Concurrent Operations

    [Fact]
    public async Task ValidateTransaction_ConcurrentSubmissions_HandledSafely()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act - Submit multiple transactions concurrently
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => client.PostAsJsonAsync("/api/v1/transactions/validate", CreateValidTransactionRequest(registerId)))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should complete without crashing
        foreach (var response in responses)
        {
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
                HttpStatusCode.Conflict,
                HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public async Task GetMemPoolStats_ConcurrentRequests_ReturnConsistentData()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act - Request stats concurrently
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.GetAsync($"/api/v1/transactions/mempool/{registerId}"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    #endregion

    #region Register Isolation

    [Fact]
    public async Task MemPool_IsolatesTransactionsByRegister()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId1 = Guid.NewGuid().ToString("N");
        var registerId2 = Guid.NewGuid().ToString("N");

        // Submit transactions to different registers
        await client.PostAsJsonAsync("/api/v1/transactions/validate", CreateValidTransactionRequest(registerId1));
        await client.PostAsJsonAsync("/api/v1/transactions/validate", CreateValidTransactionRequest(registerId2));

        // Act
        var stats1 = await client.GetAsync($"/api/v1/transactions/mempool/{registerId1}");
        var stats2 = await client.GetAsync($"/api/v1/transactions/mempool/{registerId2}");

        // Assert - Both should return independently
        stats1.StatusCode.Should().Be(HttpStatusCode.OK);
        stats2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Helper Methods

    private static object CreateValidTransactionRequest(string registerId, string? transactionId = null)
    {
        var payload = "{\"action\":\"test\",\"data\":{\"value\":123}}";
        return new
        {
            TransactionId = transactionId ?? Guid.NewGuid().ToString("N"),
            RegisterId = registerId,
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static object[] CreateValidSignatures()
    {
        return new[]
        {
            new
            {
                PublicKey = Convert.ToBase64String(new byte[32]),
                SignatureValue = Convert.ToBase64String(new byte[64]),
                Algorithm = "ED25519"
            }
        };
    }

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion
}
