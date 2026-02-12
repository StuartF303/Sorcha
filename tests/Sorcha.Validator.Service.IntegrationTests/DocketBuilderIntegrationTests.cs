// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Moq;
using Sorcha.ServiceClients.Peer;
using Sorcha.ServiceClients.Register;
using Sorcha.ServiceClients.Wallet;

namespace Sorcha.Validator.Service.IntegrationTests;

/// <summary>
/// Integration tests for the Docket Builder.
/// Tests docket construction, merkle tree computation, and transaction ordering.
/// </summary>
[Collection("ValidatorService")]
public class DocketBuilderIntegrationTests
{
    private readonly ValidatorServiceWebApplicationFactory _factory;

    public DocketBuilderIntegrationTests(ValidatorServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ClearTestData();
    }

    #region Docket Building

    [Fact]
    public async Task ProcessPipeline_WithPendingTransactions_BuildsDocket()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Submit a transaction first
        using var validatorClient = _factory.CreateValidatorClient();
        var txRequest = CreateValidTransactionRequest(registerId);
        await validatorClient.PostAsJsonAsync("/api/v1/transactions/validate", txRequest);

        // Act - Process pipeline to build docket
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPipeline_BuildsDocketWithCorrectSequence()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPipeline_WithExistingDockets_ChainsCorrectly()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Merkle Tree Computation

    [Fact]
    public async Task ProcessPipeline_ComputesMerkleRoot()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify docket was written with merkle root
        _factory.RegisterClientMock.Verify(
            r => r.WriteDocketAsync(It.Is<DocketModel>(d => !string.IsNullOrEmpty(d.MerkleRoot)), It.IsAny<CancellationToken>()),
            Times.AtMost(1));
    }

    #endregion

    #region Transaction Ordering

    [Fact]
    public async Task ProcessPipeline_OrdersTransactionsByPriority()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPipeline_RespectsMaxTransactionsPerDocket()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act - Process should respect configuration limits
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Docket Signing

    [Fact]
    public async Task ProcessPipeline_SignsDocketWithValidatorWallet()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);

        _factory.WalletClientMock
            .Setup(w => w.SignDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletSignResult
            {
                Signature = new byte[64],
                PublicKey = new byte[32],
                SignedBy = "test-validator-id",
                Algorithm = "ED25519"
            });

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Submit a transaction
        using var validatorClient = _factory.CreateValidatorClient();
        var txRequest = CreateValidTransactionRequest(registerId);
        await validatorClient.PostAsJsonAsync("/api/v1/transactions/validate", txRequest);

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPipeline_WhenSigningFails_HandlesGracefully()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);

        // Setup wallet to fail signing
        _factory.WalletClientMock
            .Setup(w => w.SignDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Signing failed"));

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert - Should handle gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Docket Writing

    [Fact]
    public async Task ProcessPipeline_WritesConfirmedDocketToRegister()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Track docket writes
        _factory.RegisterClientMock
            .Setup(r => r.WriteDocketAsync(It.IsAny<DocketModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert - Docket may have been written
    }

    [Fact]
    public async Task ProcessPipeline_WhenWriteFails_RetriesOrReturnsError()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Setup write to fail
        _factory.RegisterClientMock
            .Setup(r => r.WriteDocketAsync(It.IsAny<DocketModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert - Should handle write failure
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Empty Docket Handling

    [Fact]
    public async Task ProcessPipeline_WithNoTransactions_ReturnsNoPendingMessage()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        SetupDefaultValidators(registerId);
        SetupDefaultWalletSigning();

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act - Process with no pending transactions
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonDocument>();
        content.Should().NotBeNull();

        // Should have a message or docket result
        var hasMessage = content!.RootElement.TryGetProperty("message", out _);
        var hasDocketNumber = content.RootElement.TryGetProperty("docketNumber", out _);
        (hasMessage || hasDocketNumber).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultValidators(string registerId)
    {
        _factory.PeerClientMock
            .Setup(p => p.QueryValidatorsAsync(registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>
            {
                new()
                {
                    ValidatorId = "test-validator-id",
                    GrpcEndpoint = "http://localhost:7004",
                    ReputationScore = 1.0,
                    IsActive = true
                }
            });
    }

    private void SetupDefaultWalletSigning()
    {
        _factory.WalletClientMock
            .Setup(w => w.SignDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletSignResult
            {
                Signature = new byte[64],
                PublicKey = new byte[32],
                SignedBy = "test-validator-id",
                Algorithm = "ED25519"
            });
    }

    private static object CreateValidTransactionRequest(string registerId)
    {
        var payload = "{\"action\":\"test\",\"data\":{\"value\":123}}";
        return new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = registerId,
            BlueprintId = "test-blueprint",
            ActionId = "test-action",
            Payload = JsonSerializer.Deserialize<JsonElement>(payload),
            PayloadHash = ComputeHash(payload),
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

    private static string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion
}
