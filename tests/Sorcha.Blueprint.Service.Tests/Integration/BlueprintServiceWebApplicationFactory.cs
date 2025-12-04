// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Moq.Protected;
using Sorcha.Blueprint.Service.Clients;
using StackExchange.Redis;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Sorcha.Blueprint.Service.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for integration testing that configures
/// in-memory services and mock HTTP clients
/// </summary>
public class BlueprintServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<HttpMessageHandler> MockWalletHttpHandler { get; } = new();
    public Mock<HttpMessageHandler> MockRegisterHttpHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Add test configuration for Redis connection string
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration with Redis connection string to satisfy Aspire requirements
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = "localhost:6379,abortConnect=false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real Redis output caching
            services.RemoveAll<IDistributedCache>();

            // Add in-memory distributed cache
            services.AddDistributedMemoryCache();

            // Remove Redis-based output cache store and replace with no-op
            services.RemoveAll<IOutputCacheStore>();
            services.AddSingleton<IOutputCacheStore, NoOpOutputCacheStore>();

            // Remove Redis connection multiplexer if registered
            services.RemoveAll<IConnectionMultiplexer>();

            // Remove the real HTTP clients for Wallet and Register services
            // and add mock implementations
            RemoveHttpClientServices(services);
            AddMockHttpClients(services);
        });

        builder.UseEnvironment("Testing");
    }

    private void RemoveHttpClientServices(IServiceCollection services)
    {
        // Remove existing HttpClient registrations for our service clients
        var walletClientDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IWalletServiceClient));
        if (walletClientDescriptor != null)
        {
            services.Remove(walletClientDescriptor);
        }

        var registerClientDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IRegisterServiceClient));
        if (registerClientDescriptor != null)
        {
            services.Remove(registerClientDescriptor);
        }
    }

    private void AddMockHttpClients(IServiceCollection services)
    {
        // Setup default mock responses for wallet service
        SetupDefaultWalletResponses();

        // Setup default mock responses for register service
        SetupDefaultRegisterResponses();

        // Create HttpClient with mock handler for wallet service
        var walletHttpClient = new HttpClient(MockWalletHttpHandler.Object)
        {
            BaseAddress = new Uri("http://walletservice")
        };

        // Create HttpClient with mock handler for register service
        var registerHttpClient = new HttpClient(MockRegisterHttpHandler.Object)
        {
            BaseAddress = new Uri("http://registerservice")
        };

        // Register mock clients
        services.AddSingleton<IWalletServiceClient>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WalletServiceClient>>();
            return new WalletServiceClient(walletHttpClient, logger);
        });

        services.AddSingleton<IRegisterServiceClient>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RegisterServiceClient>>();
            return new RegisterServiceClient(registerHttpClient, logger);
        });
    }

    private void SetupDefaultWalletResponses()
    {
        // Default encrypt response
        MockWalletHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains("/encrypt")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var response = new
                {
                    EncryptedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-test-data")),
                    RecipientAddress = "wallet-test",
                    EncryptedAt = DateTime.UtcNow
                };
                return CreateJsonResponse(response);
            });

        // Default decrypt response
        MockWalletHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains("/decrypt")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var response = new
                {
                    DecryptedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")),
                    DecryptedBy = "wallet-test",
                    DecryptedAt = DateTime.UtcNow
                };
                return CreateJsonResponse(response);
            });

        // Default sign response
        MockWalletHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.Contains("/sign")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var response = new
                {
                    Signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-signature")),
                    SignedBy = "wallet-test",
                    SignedAt = DateTime.UtcNow
                };
                return CreateJsonResponse(response);
            });

        // Default get wallet response
        MockWalletHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.PathAndQuery.Contains("/wallets/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var response = new WalletInfo
                {
                    Address = "wallet-test",
                    Name = "Test Wallet",
                    PublicKey = "test-public-key",
                    Algorithm = "ED25519",
                    Status = "Active",
                    Owner = "test-owner",
                    Tenant = "test-tenant",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                };
                return CreateJsonResponse(response);
            });
    }

    private void SetupDefaultRegisterResponses()
    {
        // Default submit transaction response
        MockRegisterHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.PathAndQuery.Contains("/transactions")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var response = new Sorcha.Register.Models.TransactionModel
                {
                    TxId = Guid.NewGuid().ToString(),
                    RegisterId = "test-register",
                    SenderWallet = "wallet-test",
                    TimeStamp = DateTime.UtcNow
                };
                return CreateJsonResponse(response, HttpStatusCode.Created);
            });

        // Default get transaction response
        MockRegisterHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.PathAndQuery.Contains("/transactions/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var response = new Sorcha.Register.Models.TransactionModel
                {
                    TxId = "tx-123",
                    RegisterId = "test-register",
                    SenderWallet = "wallet-test",
                    TimeStamp = DateTime.UtcNow
                };
                return CreateJsonResponse(response);
            });

        // Default get register response
        MockRegisterHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.PathAndQuery.Contains("/registers/")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var response = new Sorcha.Register.Models.Register
                {
                    Id = "test-register",
                    Name = "Test Register",
                    TenantId = "test-tenant",
                    Status = Sorcha.Register.Models.Enums.RegisterStatus.Online
                };
                return CreateJsonResponse(response);
            });
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}

/// <summary>
/// No-op output cache store for testing that doesn't require Redis
/// </summary>
internal class NoOpOutputCacheStore : IOutputCacheStore
{
    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<byte[]?>(null);
    }

    public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
