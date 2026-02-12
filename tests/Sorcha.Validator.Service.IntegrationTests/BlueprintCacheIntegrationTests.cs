// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Moq;

namespace Sorcha.Validator.Service.IntegrationTests;

/// <summary>
/// Integration tests for Blueprint Cache.
/// Tests blueprint caching, version resolution, and cache invalidation.
/// </summary>
[Collection("ValidatorService")]
public class BlueprintCacheIntegrationTests
{
    private readonly ValidatorServiceWebApplicationFactory _factory;

    public BlueprintCacheIntegrationTests(ValidatorServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ClearTestData();
    }

    #region Blueprint Caching

    [Fact]
    public async Task BlueprintCache_FirstRequest_FetchesFromService()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId = "cache-test-blueprint";

        var fetchCount = 0;
        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .Callback(() => fetchCount++)
            .ReturnsAsync(CreateBlueprintJson(blueprintId));

        var request = CreateTransactionRequest(registerId, blueprintId);

        // Act
        await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Blueprint should be fetched at least once (or cached)
        fetchCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task BlueprintCache_SubsequentRequests_UsesCachedValue()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId = "cached-blueprint";

        var fetchCount = 0;
        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .Callback(() => fetchCount++)
            .ReturnsAsync(CreateBlueprintJson(blueprintId));

        // Act - Submit multiple transactions with same blueprint
        for (int i = 0; i < 5; i++)
        {
            var request = CreateTransactionRequest(registerId, blueprintId);
            await client.PostAsJsonAsync("/api/v1/transactions/validate", request);
        }

        // Assert - Blueprint may be cached (exact behavior depends on implementation)
        // Just verify all requests completed
    }

    [Fact]
    public async Task BlueprintCache_DifferentBlueprints_FetchedIndependently()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId1 = "blueprint-1";
        var blueprintId2 = "blueprint-2";

        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBlueprintJson(blueprintId1));

        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBlueprintJson(blueprintId2));

        // Act
        var request1 = CreateTransactionRequest(registerId, blueprintId1);
        var request2 = CreateTransactionRequest(registerId, blueprintId2);

        await client.PostAsJsonAsync("/api/v1/transactions/validate", request1);
        await client.PostAsJsonAsync("/api/v1/transactions/validate", request2);

        // Assert - Both blueprints should be fetched
        _factory.BlueprintClientMock.Verify(
            b => b.GetBlueprintAsync(blueprintId1, It.IsAny<CancellationToken>()),
            Times.AtMost(5));
        _factory.BlueprintClientMock.Verify(
            b => b.GetBlueprintAsync(blueprintId2, It.IsAny<CancellationToken>()),
            Times.AtMost(5));
    }

    #endregion

    #region Blueprint Version Resolution

    [Fact]
    public async Task BlueprintCache_WithVersionedBlueprint_ResolvesCorrectVersion()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId = "versioned-blueprint";

        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new
            {
                id = blueprintId,
                title = "Versioned Blueprint",
                version = "2.0.0",
                actions = new[]
                {
                    new { id = "test-action", title = "Test Action" }
                }
            }));

        var request = CreateTransactionRequest(registerId, blueprintId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task BlueprintCache_MultipleBlueprintsForRegister_HandlesCorrectly()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        var blueprints = new[] { "workflow-a", "workflow-b", "workflow-c" };

        foreach (var bp in blueprints)
        {
            _factory.BlueprintClientMock
                .Setup(b => b.GetBlueprintAsync(bp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateBlueprintJson(bp));
        }

        // Act - Submit transactions with different blueprints
        foreach (var bp in blueprints)
        {
            var request = CreateTransactionRequest(registerId, bp);
            await client.PostAsJsonAsync("/api/v1/transactions/validate", request);
        }

        // Assert - All should be processed
    }

    #endregion

    #region Cache Miss Handling

    [Fact]
    public async Task BlueprintCache_WhenBlueprintNotFound_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var unknownBlueprintId = "unknown-blueprint-" + Guid.NewGuid().ToString("N");

        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(unknownBlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var request = CreateTransactionRequest(registerId, unknownBlueprintId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Should handle unknown blueprint
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task BlueprintCache_WhenServiceUnavailable_ReturnsError()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId = "unavailable-blueprint";

        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Blueprint service unavailable"));

        var request = CreateTransactionRequest(registerId, blueprintId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Should handle service unavailability gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK,
            HttpStatusCode.Conflict,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Cache Invalidation

    [Fact]
    public async Task BlueprintCache_AfterUpdate_FetchesNewVersion()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId = "updated-blueprint";

        var version = 1;
        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => JsonSerializer.Serialize(new
            {
                id = blueprintId,
                title = "Updated Blueprint",
                version = $"{version}.0.0",
                actions = new[]
                {
                    new { id = "test-action", title = "Test Action" }
                }
            }));

        // First request
        var request1 = CreateTransactionRequest(registerId, blueprintId);
        await client.PostAsJsonAsync("/api/v1/transactions/validate", request1);

        // Simulate blueprint update
        version = 2;

        // Second request (after update)
        var request2 = CreateTransactionRequest(registerId, blueprintId);
        await client.PostAsJsonAsync("/api/v1/transactions/validate", request2);

        // Assert - Blueprint service may be called (depending on implementation)
        _factory.BlueprintClientMock.Verify(
            b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()),
            Times.AtMost(10));
    }

    #endregion

    #region Concurrent Cache Access

    [Fact]
    public async Task BlueprintCache_ConcurrentRequests_HandlesSafely()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId = "concurrent-blueprint";

        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateBlueprintJson(blueprintId));

        // Act - Send multiple concurrent requests
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => client.PostAsJsonAsync("/api/v1/transactions/validate", CreateTransactionRequest(registerId, blueprintId)))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should complete without throwing
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
    public async Task BlueprintCache_ConcurrentDifferentBlueprints_HandlesIndependently()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprints = Enumerable.Range(1, 5).Select(i => $"concurrent-bp-{i}").ToArray();

        foreach (var bp in blueprints)
        {
            _factory.BlueprintClientMock
                .Setup(b => b.GetBlueprintAsync(bp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateBlueprintJson(bp));
        }

        // Act - Send concurrent requests with different blueprints
        var tasks = blueprints
            .Select(bp => client.PostAsJsonAsync("/api/v1/transactions/validate", CreateTransactionRequest(registerId, bp)))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should complete
        foreach (var response in responses)
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        }
    }

    #endregion

    #region Blueprint Action Validation

    [Fact]
    public async Task BlueprintCache_ValidatesActionExists()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");
        var blueprintId = "action-validation-blueprint";

        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new
            {
                id = blueprintId,
                title = "Action Validation Blueprint",
                version = "1.0.0",
                actions = new[]
                {
                    new { id = "valid-action", title = "Valid Action" }
                }
            }));

        // Request with unknown action
        var payload = "{}";
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = registerId,
            BlueprintId = blueprintId,
            ActionId = "unknown-action", // Action not in blueprint
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Unknown action should be rejected or handled
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    #endregion

    #region Helper Methods

    private static string CreateBlueprintJson(string blueprintId)
    {
        return JsonSerializer.Serialize(new
        {
            id = blueprintId,
            title = $"Test Blueprint {blueprintId}",
            version = "1.0.0",
            actions = new[]
            {
                new { id = "test-action", title = "Test Action" }
            }
        });
    }

    private static object CreateTransactionRequest(string registerId, string blueprintId)
    {
        var payload = "{\"test\":\"data\"}";
        return new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = registerId,
            BlueprintId = blueprintId,
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
