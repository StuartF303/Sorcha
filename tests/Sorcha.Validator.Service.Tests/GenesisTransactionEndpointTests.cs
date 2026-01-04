// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sorcha.Validator.Service.Services;
using Xunit;

namespace Sorcha.Validator.Service.Tests;

/// <summary>
/// Integration tests for genesis transaction endpoint
/// </summary>
public class GenesisTransactionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GenesisTransactionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithValidRequest_ShouldReturn200Ok()
    {
        // Arrange
        var mockMemPoolManager = new Mock<IMemPoolManager>();
        mockMemPoolManager
            .Setup(m => m.AddTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<Models.Transaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemPoolManager));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => mockMemPoolManager.Object);
            });
        }).CreateClient();

        var request = new
        {
            transactionId = "genesis-test-register-001",
            registerId = "testreg001",
            controlRecordPayload = JsonDocument.Parse(@"{
                ""registerId"": ""testreg001"",
                ""name"": ""Test Genesis Register"",
                ""tenantId"": ""tenant-001"",
                ""createdAt"": ""2025-01-01T00:00:00Z"",
                ""attestations"": [
                    {
                        ""role"": ""Owner"",
                        ""subject"": ""did:sorcha:user-001"",
                        ""publicKey"": """ + Convert.ToBase64String(new byte[32]) + @""",
                        ""signature"": """ + Convert.ToBase64String(new byte[64]) + @""",
                        ""algorithm"": ""ED25519"",
                        ""grantedAt"": ""2025-01-01T00:00:00Z""
                    }
                ]
            }").RootElement,
            payloadHash = "abcd1234567890abcdef1234567890abcdef1234567890abcdef1234567890ab",
            signatures = new[]
            {
                new
                {
                    publicKey = Convert.ToBase64String(new byte[32]),
                    signatureValue = Convert.ToBase64String(new byte[64]),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow,
            registerName = "Test Genesis Register",
            tenantId = "tenant-001"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("transactionId").GetString().Should().Be("genesis-test-register-001");
        result.GetProperty("registerId").GetString().Should().Be("testreg001");
        result.GetProperty("message").GetString().Should().Contain("accepted");

        mockMemPoolManager.Verify(
            m => m.AddTransactionAsync(
                "testreg001",
                It.Is<Models.Transaction>(t =>
                    t.TransactionId == "genesis-test-register-001" &&
                    t.RegisterId == "testreg001" &&
                    t.BlueprintId == "genesis" &&
                    t.Priority == Models.TransactionPriority.High),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_ShouldSetHighPriority()
    {
        // Arrange
        Models.Transaction? capturedTransaction = null;
        var mockMemPoolManager = new Mock<IMemPoolManager>();
        mockMemPoolManager
            .Setup(m => m.AddTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<Models.Transaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Models.Transaction, CancellationToken>((_, tx, _) =>
                capturedTransaction = tx)
            .ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemPoolManager));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => mockMemPoolManager.Object);
            });
        }).CreateClient();

        var request = new
        {
            transactionId = "genesis-priority-test",
            registerId = "reg-priority-001",
            controlRecordPayload = JsonDocument.Parse(@"{
                ""registerId"": ""reg-priority-001"",
                ""name"": ""Priority Test"",
                ""tenantId"": ""tenant-001"",
                ""createdAt"": ""2025-01-01T00:00:00Z"",
                ""attestations"": []
            }").RootElement,
            payloadHash = "hash123",
            signatures = new[]
            {
                new
                {
                    publicKey = Convert.ToBase64String(new byte[32]),
                    signatureValue = Convert.ToBase64String(new byte[64]),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow,
            registerName = "Priority Test",
            tenantId = "tenant-001"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.Priority.Should().Be(Models.TransactionPriority.High);
        capturedTransaction.Metadata["Type"].Should().Be("Genesis");
        capturedTransaction.ExpiresAt.Should().BeNull("Genesis transactions should not expire");
    }

    [Fact]
    public async Task SubmitGenesisTransaction_ShouldIncludeMetadata()
    {
        // Arrange
        Models.Transaction? capturedTransaction = null;
        var mockMemPoolManager = new Mock<IMemPoolManager>();
        mockMemPoolManager
            .Setup(m => m.AddTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<Models.Transaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Models.Transaction, CancellationToken>((_, tx, _) =>
                capturedTransaction = tx)
            .ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemPoolManager));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => mockMemPoolManager.Object);
            });
        }).CreateClient();

        var request = new
        {
            transactionId = "genesis-metadata-test",
            registerId = "reg-metadata-001",
            controlRecordPayload = JsonDocument.Parse(@"{
                ""registerId"": ""reg-metadata-001"",
                ""name"": ""Metadata Test Register"",
                ""tenantId"": ""tenant-metadata-001"",
                ""createdAt"": ""2025-01-01T00:00:00Z"",
                ""attestations"": []
            }").RootElement,
            payloadHash = "hash456",
            signatures = new[]
            {
                new
                {
                    publicKey = Convert.ToBase64String(new byte[32]),
                    signatureValue = Convert.ToBase64String(new byte[64]),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow,
            registerName = "Metadata Test Register",
            tenantId = "tenant-metadata-001"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.Metadata.Should().ContainKey("Type");
        capturedTransaction.Metadata["Type"].Should().Be("Genesis");
        capturedTransaction.Metadata.Should().ContainKey("RegisterName");
        capturedTransaction.Metadata["RegisterName"].Should().Be("Metadata Test Register");
        capturedTransaction.Metadata.Should().ContainKey("TenantId");
        capturedTransaction.Metadata["TenantId"].Should().Be("tenant-metadata-001");
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WhenMemPoolFull_ShouldReturn409Conflict()
    {
        // Arrange
        var mockMemPoolManager = new Mock<IMemPoolManager>();
        mockMemPoolManager
            .Setup(m => m.AddTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<Models.Transaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Memory pool full

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemPoolManager));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => mockMemPoolManager.Object);
            });
        }).CreateClient();

        var request = new
        {
            transactionId = "genesis-mempool-full",
            registerId = "reg-full-001",
            controlRecordPayload = JsonDocument.Parse(@"{
                ""registerId"": ""reg-full-001"",
                ""name"": ""Test"",
                ""tenantId"": ""tenant-001"",
                ""createdAt"": ""2025-01-01T00:00:00Z"",
                ""attestations"": []
            }").RootElement,
            payloadHash = "hash789",
            signatures = new[]
            {
                new
                {
                    publicKey = Convert.ToBase64String(new byte[32]),
                    signatureValue = Convert.ToBase64String(new byte[64]),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeFalse();
        result.GetProperty("message").GetString().Should().Contain("pool full");
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithMissingFields_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new
        {
            // Missing transactionId
            registerId = "reg-missing-001",
            payloadHash = "hash123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_ShouldConvertSignaturesToBytes()
    {
        // Arrange
        Models.Transaction? capturedTransaction = null;
        var mockMemPoolManager = new Mock<IMemPoolManager>();
        mockMemPoolManager
            .Setup(m => m.AddTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<Models.Transaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Models.Transaction, CancellationToken>((_, tx, _) =>
                capturedTransaction = tx)
            .ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemPoolManager));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => mockMemPoolManager.Object);
            });
        }).CreateClient();

        var publicKeyBytes = new byte[32];
        var signatureBytes = new byte[64];
        for (int i = 0; i < 32; i++) publicKeyBytes[i] = (byte)i;
        for (int i = 0; i < 64; i++) signatureBytes[i] = (byte)(i % 256);

        var request = new
        {
            transactionId = "genesis-signature-test",
            registerId = "reg-sig-001",
            controlRecordPayload = JsonDocument.Parse(@"{
                ""registerId"": ""reg-sig-001"",
                ""name"": ""Signature Test"",
                ""tenantId"": ""tenant-001"",
                ""createdAt"": ""2025-01-01T00:00:00Z"",
                ""attestations"": []
            }").RootElement,
            payloadHash = "hash999",
            signatures = new[]
            {
                new
                {
                    publicKey = Convert.ToBase64String(publicKeyBytes),
                    signatureValue = Convert.ToBase64String(signatureBytes),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.Signatures.Should().HaveCount(1);
        capturedTransaction.Signatures[0].PublicKey.Should().BeEquivalentTo(publicKeyBytes);
        capturedTransaction.Signatures[0].SignatureValue.Should().BeEquivalentTo(signatureBytes);
        capturedTransaction.Signatures[0].Algorithm.Should().Be("ED25519");
    }

    [Fact]
    public async Task SubmitGenesisTransaction_ShouldSetSpecialBlueprintId()
    {
        // Arrange
        Models.Transaction? capturedTransaction = null;
        var mockMemPoolManager = new Mock<IMemPoolManager>();
        mockMemPoolManager
            .Setup(m => m.AddTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<Models.Transaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Models.Transaction, CancellationToken>((_, tx, _) =>
                capturedTransaction = tx)
            .ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemPoolManager));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => mockMemPoolManager.Object);
            });
        }).CreateClient();

        var request = new
        {
            transactionId = "genesis-blueprint-test",
            registerId = "reg-blueprint-001",
            controlRecordPayload = JsonDocument.Parse(@"{
                ""registerId"": ""reg-blueprint-001"",
                ""name"": ""Blueprint Test"",
                ""tenantId"": ""tenant-001"",
                ""createdAt"": ""2025-01-01T00:00:00Z"",
                ""attestations"": []
            }").RootElement,
            payloadHash = "hash111",
            signatures = new[]
            {
                new
                {
                    publicKey = Convert.ToBase64String(new byte[32]),
                    signatureValue = Convert.ToBase64String(new byte[64]),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.BlueprintId.Should().Be("genesis");
        capturedTransaction.ActionId.Should().Be("register-creation");
    }
}
