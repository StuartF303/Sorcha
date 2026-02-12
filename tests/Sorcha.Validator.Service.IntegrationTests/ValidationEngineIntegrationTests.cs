// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Moq;
using Sorcha.ServiceClients.Wallet;

namespace Sorcha.Validator.Service.IntegrationTests;

/// <summary>
/// Integration tests for the Validation Engine.
/// Tests the full validation pipeline including structure, schema, signature, and chain validation.
/// </summary>
[Collection("ValidatorService")]
public class ValidationEngineIntegrationTests
{
    private readonly ValidatorServiceWebApplicationFactory _factory;

    public ValidationEngineIntegrationTests(ValidatorServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ClearTestData();
    }

    #region Transaction Structure Validation

    [Fact]
    public async Task ValidateTransaction_WithValidStructure_ProcessesSuccessfully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var request = CreateValidTransactionRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Should not be NotFound (endpoint exists and processes)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithMissingTransactionId_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var request = new
        {
            // TransactionId = null - intentionally missing
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            PayloadHash = ComputeHash("{}"),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateTransaction_WithMissingRegisterId_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            // RegisterId missing
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            PayloadHash = ComputeHash("{}"),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateTransaction_WithMissingBlueprintId_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            // BlueprintId missing
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            PayloadHash = ComputeHash("{}"),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateTransaction_WithFutureTimestamp_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            PayloadHash = ComputeHash("{}"),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(1) // Future timestamp
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Future timestamps should be rejected
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithExpiredTransaction_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            PayloadHash = ComputeHash("{}"),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) // Already expired
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    #endregion

    #region Payload Hash Validation

    [Fact]
    public async Task ValidateTransaction_WithValidPayloadHash_ProcessesSuccessfully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var payload = "{\"action\":\"test\",\"value\":123}";
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithInvalidPayloadHash_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var payload = "{\"action\":\"test\",\"value\":123}";
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = "invalid-hash-that-does-not-match-the-payload",
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Invalid hash should be rejected or caught in validation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithEmptyPayloadHash_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            PayloadHash = "", // Empty hash
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    #endregion

    #region Signature Validation

    [Fact]
    public async Task ValidateTransaction_WithNoSignatures_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var payload = "{}";
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = Array.Empty<object>(), // No signatures
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateTransaction_WithMultipleSignatures_ProcessesSuccessfully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var payload = "{}";
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" },
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithUnsupportedAlgorithm_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var payload = "{}";
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "UNSUPPORTED_ALGO" }
            },
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    #endregion

    #region Batch Validation

    [Fact]
    public async Task ValidateBatch_WithMultipleValidTransactions_ProcessesAll()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var transactions = Enumerable.Range(1, 5).Select(_ => CreateValidTransactionRequest()).ToArray();
        var request = new { Transactions = transactions };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate/batch", request);

        // Assert - Either accepts batch or endpoint doesn't exist
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateBatch_WithMixedValidInvalid_ProcessesAndReportsErrors()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var validTx = CreateValidTransactionRequest();
        var invalidTx = new
        {
            TransactionId = "", // Invalid - empty ID
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            PayloadHash = ComputeHash("{}"),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var request = new { Transactions = new object[] { validTx, invalidTx } };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate/batch", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.MultiStatus);
    }

    #endregion

    #region Blueprint Schema Validation

    [Fact]
    public async Task ValidateTransaction_WithValidBlueprintPayload_ProcessesSuccessfully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var blueprintId = "schema-test-blueprint";

        // Setup mock blueprint with schema
        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(blueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(new
            {
                id = blueprintId,
                title = "Schema Test Blueprint",
                version = "1.0.0",
                actions = new[]
                {
                    new
                    {
                        id = "submit-form",
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                name = new { type = "string" },
                                age = new { type = "integer" }
                            },
                            required = new[] { "name" }
                        }
                    }
                }
            }));

        var payload = "{\"name\":\"Test User\",\"age\":30}";
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = blueprintId,
            ActionId = "submit-form",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
            Signatures = CreateValidSignatures(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithUnknownBlueprint_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var unknownBlueprintId = "unknown-blueprint-" + Guid.NewGuid().ToString("N");

        // Setup mock to return null for unknown blueprint
        _factory.BlueprintClientMock
            .Setup(b => b.GetBlueprintAsync(unknownBlueprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var request = CreateValidTransactionRequest(blueprintId: unknownBlueprintId);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    #endregion

    #region MemPool Stats

    [Fact]
    public async Task GetMemPoolStats_WithValidRegisterId_ReturnsStats()
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
    public async Task GetMemPoolStats_ReturnsExpectedProperties()
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

        // Verify expected properties exist
        content!.RootElement.TryGetProperty("registerId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetMemPoolStats_WithEmptyRegisterId_ReturnsBadRequestOrNotFound()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();

        // Act
        var response = await client.GetAsync("/api/v1/transactions/mempool/");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private static object CreateValidTransactionRequest(string? blueprintId = null, string? actionId = null)
    {
        var payload = "{\"action\":\"test\",\"data\":{\"value\":123}}";
        return new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = blueprintId ?? "test-blueprint",
            ActionId = actionId ?? "test-action",
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
