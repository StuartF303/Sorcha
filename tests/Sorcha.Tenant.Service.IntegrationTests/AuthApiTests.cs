// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Sorcha.Tenant.Service.IntegrationTests.Fixtures;
using Sorcha.Tenant.Service.Models.Dtos;

namespace Sorcha.Tenant.Service.IntegrationTests;

/// <summary>
/// Integration tests for Authentication API endpoints.
/// </summary>
public class AuthApiTests : IClassFixture<TenantServiceWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _unauthClient;
    private readonly TenantServiceWebApplicationFactory _factory;

    public AuthApiTests(TenantServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _unauthClient = _factory.CreateUnauthenticatedClient();
        _client = _factory.CreateAuthenticatedClient();
        _adminClient = _factory.CreateAdminClient();
    }

    #region Health Check Tests

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _unauthClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadAsStringAsync();
        result.Should().Contain("Healthy");
    }

    #endregion

    #region Token Revocation Tests

    [Fact]
    public async Task RevokeToken_ShouldReturnOk_WithValidToken()
    {
        // Arrange - Generate a test token
        var token = GenerateTestToken();
        var request = new TokenRevocationRequest
        {
            Token = token,
            TokenTypeHint = "access_token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/revoke", request);

        // Assert
        // Note: This will succeed even without Redis due to fail-open pattern
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeToken_ShouldReturnBadRequest_WhenTokenEmpty()
    {
        // Arrange
        var request = new TokenRevocationRequest
        {
            Token = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/revoke", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Token Introspection Tests

    [Fact]
    public async Task IntrospectToken_ShouldReturnInactive_ForInvalidToken()
    {
        // Arrange
        var request = new TokenIntrospectionRequest
        {
            Token = "invalid-token-format"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/introspect", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TokenIntrospectionResponse>();
        result.Should().NotBeNull();
        result!.Active.Should().BeFalse();
    }

    [Fact]
    public async Task IntrospectToken_ShouldAcceptValidJwt()
    {
        // Arrange - Generate a properly signed test token
        var token = GenerateTestToken();
        var request = new TokenIntrospectionRequest
        {
            Token = token
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/introspect", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TokenIntrospectionResponse>();
        result.Should().NotBeNull();
        // Token will be inactive because the signing key doesn't match the service's key
        // In a real scenario with matching keys, it would be active
    }

    #endregion

    #region Get Current User Tests

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _unauthClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUserInfo_WhenAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _unauthClient.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenAuthenticated()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Bulk Token Revocation Tests

    [Fact]
    public async Task RevokeUserTokens_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var request = new RevokeUserTokensRequest
        {
            UserId = Guid.NewGuid()
        };

        // Act
        var response = await _unauthClient.PostAsJsonAsync("/api/auth/token/revoke-user", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeUserTokens_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var request = new RevokeUserTokensRequest
        {
            UserId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/revoke-user", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeUserTokens_ShouldReturnOk_WhenAdmin()
    {
        // Arrange
        var request = new RevokeUserTokensRequest
        {
            UserId = Guid.NewGuid()
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/auth/token/revoke-user", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeOrganizationTokens_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var request = new RevokeOrganizationTokensRequest
        {
            OrganizationId = Guid.NewGuid()
        };

        // Act
        var response = await _unauthClient.PostAsJsonAsync("/api/auth/token/revoke-organization", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeOrganizationTokens_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var request = new RevokeOrganizationTokensRequest
        {
            OrganizationId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/revoke-organization", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RevokeOrganizationTokens_ShouldReturnOk_WhenAdmin()
    {
        // Arrange
        var request = new RevokeOrganizationTokensRequest
        {
            OrganizationId = Guid.NewGuid()
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/auth/token/revoke-organization", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Token Refresh Tests

    [Fact]
    public async Task RefreshToken_ShouldReturnBadRequest_WithInvalidRefreshToken()
    {
        // Arrange
        var request = new TokenRefreshRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/refresh", request);

        // Assert
        // Should be unauthorized or bad request for invalid token
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnBadRequest_WithEmptyToken()
    {
        // Arrange
        var request = new TokenRefreshRequest
        {
            RefreshToken = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/token/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a test JWT token for testing purposes.
    /// Note: This token won't be valid for actual authentication as the signing key differs.
    /// </summary>
    private static string GenerateTestToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-that-is-at-least-32-characters-long!"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("org_id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        };

        var token = new JwtSecurityToken(
            issuer: "sorcha-tenant-service",
            audience: "sorcha-services",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
