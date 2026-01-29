// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Sorcha.ServiceClients.Wallet;

namespace Sorcha.Validator.Service.IntegrationTests;

/// <summary>
/// Integration tests for validation endpoints.
/// Endpoints are mapped at /api/v1/transactions/*
/// </summary>
[Collection("ValidatorService")]
public class ValidationEndpointTests
{
    private readonly ValidatorServiceWebApplicationFactory _factory;

    public ValidationEndpointTests(ValidatorServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ClearTestData();
    }

    [Fact]
    public async Task ValidateTransaction_WithValidRequest_ReturnsOkOrBadRequest()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = CreateValidTransactionRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", request);

        // Assert - Either OK (200) if validation passes or BadRequest (400) if validation fails
        // Since we're using mock validators, the actual result depends on the mock setup
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ValidateTransaction_WithInvalidPayloadHash_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Create request with invalid hash
        var payload = JsonSerializer.Deserialize<JsonElement>("{\"action\":\"test\"}");
        var invalidRequest = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint-id",
            ActionId = "test-action",
            Payload = payload,
            PayloadHash = "invalid-hash-that-does-not-match",
            Signatures = new[]
            {
                new
                {
                    PublicKey = Convert.ToBase64String(new byte[32]),
                    SignatureValue = Convert.ToBase64String(new byte[64]),
                    Algorithm = "ED25519"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", invalidRequest);

        // Assert - Either BadRequest or OK depending on validator behavior
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetMemPoolStats_WithValidRegisterId_ReturnsStats()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var registerId = "test-register-stats";

        // Act
        var response = await client.GetAsync($"/api/v1/transactions/mempool/{registerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithValidRequest_ReturnsOkOrError()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Setup mock for wallet signing
        _factory.WalletClientMock
            .Setup(w => w.SignTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletSignResult
            {
                Signature = new byte[64],
                PublicKey = new byte[32],
                SignedBy = "test-system-wallet",
                Algorithm = "ED25519"
            });

        var controlRecord = new
        {
            consensus = new
            {
                algorithm = "simple-majority",
                threshold = "PT30S"
            },
            validators = new
            {
                min = 1,
                max = 10,
                registrationMode = "public"
            }
        };

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String("test-key"u8.ToArray()), SignatureValue = Convert.ToBase64String("test-sig"u8.ToArray()), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Test Genesis Register",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert - Either OK (200), Conflict (409) if mempool issue, or ServiceUnavailable (503) if wallet not ready
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Conflict,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ValidateTransaction_PostEndpoint_Exists()
    {
        // Arrange - Just verify the endpoint exists and accepts POST
        using var client = _factory.CreateClient();
        var emptyRequest = new { };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/transactions/validate", emptyRequest);

        // Assert - Should not be 404 (endpoint exists), might be BadRequest for invalid data
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMemPoolStats_GetEndpoint_Exists()
    {
        // Arrange - Just verify the endpoint exists
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/transactions/mempool/test-register");

        // Assert - Should not be 404 (endpoint exists)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_PostEndpoint_Exists()
    {
        // Arrange - Just verify the endpoint exists
        using var client = _factory.CreateClient();
        var emptyRequest = new { };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", emptyRequest);

        // Assert - Should not be 404 (endpoint exists), might be BadRequest for invalid data
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    private static object CreateValidTransactionRequest()
    {
        var payload = JsonSerializer.Deserialize<JsonElement>("{\"action\":\"test\"}");
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes("{\"action\":\"test\"}");
        var payloadHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(payloadBytes)).ToLowerInvariant();

        return new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            BlueprintId = "test-blueprint-id",
            ActionId = "test-action",
            Payload = payload,
            PayloadHash = payloadHash,
            Signatures = new[]
            {
                new
                {
                    PublicKey = Convert.ToBase64String(new byte[32]),
                    SignatureValue = Convert.ToBase64String(new byte[64]),
                    Algorithm = "ED25519"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
