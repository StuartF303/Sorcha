// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Sorcha.ServiceClients.Auth;

namespace Sorcha.ServiceClients.Tests.Auth;

/// <summary>
/// Unit tests for DelegationTokenClient.
/// Tests delegation token acquisition, claim verification, and error handling.
/// </summary>
public class DelegationTokenClientTests : IDisposable
{
    private const string TestSigningKey = "test-signing-key-for-unit-tests-minimum-32-characters-long";
    private const string TestIssuer = "https://test.sorcha.io";
    private const string TestAudience = "https://test-api.sorcha.io";

    private readonly MockHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IServiceAuthClient> _mockServiceAuth;

    public DelegationTokenClientTests()
    {
        _handler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://tenant-service") };
        _mockServiceAuth = new Mock<IServiceAuthClient>();
        _mockServiceAuth.Setup(x => x.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("mock-service-token");
    }

    /// <summary>
    /// AS-3.1: Delegation token acquired with valid service and user tokens.
    /// </summary>
    [Fact]
    public async Task GetDelegationTokenAsync_WithValidTokens_ReturnsDelegationToken()
    {
        // Arrange
        var delegationJwt = GenerateDelegationToken("service-blueprint", "user-123", "org-456", "wallets:sign");
        _handler.SetResponse(CreateDelegationResponse(delegationJwt, "user-123", "org-456"));
        var client = CreateClient();

        // Act
        var token = await client.GetDelegationTokenAsync("valid-user-token", ["wallets:sign"]);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// AS-3.2: Delegation token contains both service and user identity claims.
    /// </summary>
    [Fact]
    public async Task GetDelegationTokenAsync_ResultContainsServiceAndUserIdentity()
    {
        // Arrange
        var delegationJwt = GenerateDelegationToken("service-blueprint", "user-123", "org-456", "wallets:sign");
        _handler.SetResponse(CreateDelegationResponse(delegationJwt, "user-123", "org-456"));
        var client = CreateClient();

        // Act
        var token = await client.GetDelegationTokenAsync("valid-user-token", ["wallets:sign"]);

        // Assert
        token.Should().NotBeNull();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Service identity
        jwtToken.Claims.Should().Contain(c =>
            c.Type == TokenClaimConstants.TokenType &&
            c.Value == TokenClaimConstants.TokenTypeService);
        jwtToken.Claims.Should().Contain(c =>
            c.Type == TokenClaimConstants.ServiceName &&
            c.Value == "service-blueprint");

        // Delegated user identity
        jwtToken.Claims.Should().Contain(c =>
            c.Type == TokenClaimConstants.DelegatedUserId &&
            c.Value == "user-123");
        jwtToken.Claims.Should().Contain(c =>
            c.Type == TokenClaimConstants.DelegatedOrgId &&
            c.Value == "org-456");
    }

    /// <summary>
    /// AS-3.3: Scope mismatch (Tenant Service rejects) returns null.
    /// </summary>
    [Fact]
    public async Task GetDelegationTokenAsync_ScopeMismatch_ReturnsNull()
    {
        // Arrange: Tenant Service returns 403 for unauthorized scopes
        _handler.SetErrorResponse(HttpStatusCode.Forbidden, "insufficient_scope", "Requested scope not authorized");
        var client = CreateClient();

        // Act
        var token = await client.GetDelegationTokenAsync("valid-user-token", ["admin:all"]);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// AS-3.4: Expired delegation (Tenant Service rejects expired user token) returns null.
    /// </summary>
    [Fact]
    public async Task GetDelegationTokenAsync_ExpiredUserToken_ReturnsNull()
    {
        // Arrange: Tenant Service returns 401 for expired user token
        _handler.SetErrorResponse(HttpStatusCode.Unauthorized, "invalid_grant", "User access token has expired");
        var client = CreateClient();

        // Act
        var token = await client.GetDelegationTokenAsync("expired-user-token", ["wallets:sign"]);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// AS-3.5: Revoked user token causes delegation failure.
    /// </summary>
    [Fact]
    public async Task GetDelegationTokenAsync_RevokedUserToken_ReturnsNull()
    {
        // Arrange: Tenant Service returns 401 for revoked user token
        _handler.SetErrorResponse(HttpStatusCode.Unauthorized, "invalid_grant", "User access token has been revoked");
        var client = CreateClient();

        // Act
        var token = await client.GetDelegationTokenAsync("revoked-user-token", ["wallets:sign"]);

        // Assert
        token.Should().BeNull();
    }

    /// <summary>
    /// When service token acquisition fails, delegation request is not made.
    /// </summary>
    [Fact]
    public async Task GetDelegationTokenAsync_ServiceTokenFails_ReturnsNull()
    {
        // Arrange: service auth returns null
        _mockServiceAuth.Setup(x => x.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        var client = CreateClient();

        // Act
        var token = await client.GetDelegationTokenAsync("valid-user-token", ["wallets:sign"]);

        // Assert
        token.Should().BeNull();
        _handler.RequestCount.Should().Be(0, "no HTTP request should be made if service token fails");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private DelegationTokenClient CreateClient()
    {
        var logger = new LoggerFactory().CreateLogger<DelegationTokenClient>();
        return new DelegationTokenClient(_httpClient, _mockServiceAuth.Object, logger);
    }

    private static string GenerateDelegationToken(
        string serviceId, string userId, string orgId, string scopes)
    {
        var keyBytes = Encoding.UTF8.GetBytes(TestSigningKey);
        if (keyBytes.Length < 32) Array.Resize(ref keyBytes, 32);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(TokenClaimConstants.TokenType, TokenClaimConstants.TokenTypeService),
            new Claim(TokenClaimConstants.ServiceName, serviceId),
            new Claim(TokenClaimConstants.DelegatedUserId, userId),
            new Claim(TokenClaimConstants.DelegatedOrgId, orgId),
            new Claim(TokenClaimConstants.Scope, scopes),
            new Claim(ClaimTypes.NameIdentifier, serviceId)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateDelegationResponse(string accessToken, string userId, string orgId)
    {
        return JsonSerializer.Serialize(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = 300,
            delegated_user_id = userId,
            delegated_org_id = orgId,
            scope = "wallets:sign"
        });
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage _response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        public int RequestCount { get; private set; }

        public void SetResponse(string jsonContent)
        {
            _response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
        }

        public void SetErrorResponse(HttpStatusCode statusCode, string error, string description)
        {
            _response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { error, error_description = description }),
                    Encoding.UTF8,
                    "application/json")
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_response);
        }
    }
}
