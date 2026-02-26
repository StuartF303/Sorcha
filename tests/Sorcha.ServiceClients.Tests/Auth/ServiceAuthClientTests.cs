// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.Auth;

namespace Sorcha.ServiceClients.Tests.Auth;

/// <summary>
/// Tests for ServiceAuthClient configurable scopes feature.
/// Verifies that scopes from configuration are sent in token requests,
/// and that the default scope is used when configuration is missing.
/// </summary>
public class ServiceAuthClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;

    public ServiceAuthClientTests()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://tenant-service")
        };
    }

    [Fact]
    public async Task GetTokenAsync_WithConfiguredScopes_SendsConfiguredScopes()
    {
        // Arrange
        var config = BuildConfig(scopes: "registers:write validators:notify");
        _handler.SetResponse(CreateTokenResponse("test-token", 3600));
        var client = CreateClient(config);

        // Act
        await client.GetTokenAsync();

        // Assert
        var requestContent = _handler.LastRequestContent;
        requestContent.Should().NotBeNull();
        requestContent.Should().Contain("scope=registers%3Awrite+validators%3Anotify");
    }

    [Fact]
    public async Task GetTokenAsync_WithoutConfiguredScopes_FallsBackToDefaultScope()
    {
        // Arrange
        var config = BuildConfig(scopes: null);
        _handler.SetResponse(CreateTokenResponse("test-token", 3600));
        var client = CreateClient(config);

        // Act
        await client.GetTokenAsync();

        // Assert
        var requestContent = _handler.LastRequestContent;
        requestContent.Should().NotBeNull();
        requestContent.Should().Contain("scope=wallets%3Asign");
    }

    [Fact]
    public async Task GetTokenAsync_WithSingleScope_SendsSingleScope()
    {
        // Arrange
        var config = BuildConfig(scopes: "wallets:sign");
        _handler.SetResponse(CreateTokenResponse("test-token", 3600));
        var client = CreateClient(config);

        // Act
        await client.GetTokenAsync();

        // Assert
        var requestContent = _handler.LastRequestContent;
        requestContent.Should().NotBeNull();
        requestContent.Should().Contain("scope=wallets%3Asign");
    }

    [Fact]
    public async Task GetTokenAsync_WithMultipleScopes_SendsAllScopes()
    {
        // Arrange
        var config = BuildConfig(scopes: "wallets:sign registers:write blueprints:manage");
        _handler.SetResponse(CreateTokenResponse("test-token", 3600));
        var client = CreateClient(config);

        // Act
        await client.GetTokenAsync();

        // Assert
        var requestContent = _handler.LastRequestContent;
        requestContent.Should().NotBeNull();
        requestContent.Should().Contain("scope=wallets%3Asign+registers%3Awrite+blueprints%3Amanage");
    }

    [Fact]
    public async Task GetTokenAsync_SendsCorrectClientCredentials()
    {
        // Arrange
        var config = BuildConfig(clientId: "my-service", clientSecret: "my-secret");
        _handler.SetResponse(CreateTokenResponse("test-token", 3600));
        var client = CreateClient(config);

        // Act
        await client.GetTokenAsync();

        // Assert
        var requestContent = _handler.LastRequestContent;
        requestContent.Should().Contain("grant_type=client_credentials");
        requestContent.Should().Contain("client_id=my-service");
        requestContent.Should().Contain("client_secret=my-secret");
    }

    [Fact]
    public async Task GetTokenAsync_CachesTokenOnSubsequentCalls()
    {
        // Arrange
        var config = BuildConfig();
        _handler.SetResponse(CreateTokenResponse("cached-token", 3600));
        var client = CreateClient(config);

        // Act
        var token1 = await client.GetTokenAsync();
        var token2 = await client.GetTokenAsync();

        // Assert
        token1.Should().Be("cached-token");
        token2.Should().Be("cached-token");
        _handler.RequestCount.Should().Be(1, "token should be cached after first call");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private ServiceAuthClient CreateClient(IConfiguration config)
    {
        var logger = new LoggerFactory().CreateLogger<ServiceAuthClient>();
        return new ServiceAuthClient(_httpClient, config, logger);
    }

    private static IConfiguration BuildConfig(
        string clientId = "test-service",
        string clientSecret = "test-secret",
        string? scopes = "wallets:sign")
    {
        var configData = new Dictionary<string, string?>
        {
            ["ServiceAuth:ClientId"] = clientId,
            ["ServiceAuth:ClientSecret"] = clientSecret,
            ["ServiceClients:TenantService:Address"] = "http://tenant-service"
        };

        if (scopes is not null)
        {
            configData["ServiceAuth:Scopes"] = scopes;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private static string CreateTokenResponse(string accessToken, int expiresIn)
    {
        return JsonSerializer.Serialize(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = expiresIn,
            scope = "wallets:sign"
        });
    }

    /// <summary>
    /// Mock HTTP handler that captures requests and returns configured responses.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage _response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        public string? LastRequestContent { get; private set; }
        public int RequestCount { get; private set; }

        public void SetResponse(string jsonContent)
        {
            _response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            if (request.Content is not null)
            {
                LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return _response;
        }
    }
}
