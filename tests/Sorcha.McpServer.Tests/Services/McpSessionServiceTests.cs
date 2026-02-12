// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;

namespace Sorcha.McpServer.Tests.Services;

public class McpSessionServiceTests
{
    private readonly Mock<ILogger<McpSessionService>> _loggerMock;
    private readonly Mock<ILogger<JwtValidationHandler>> _jwtLoggerMock;
    private readonly string _signingKey = "this-is-a-test-signing-key-that-is-long-enough";

    public McpSessionServiceTests()
    {
        _loggerMock = new Mock<ILogger<McpSessionService>>();
        _jwtLoggerMock = new Mock<ILogger<JwtValidationHandler>>();
    }

    private string GenerateTestToken(
        string userId = "user-123",
        string? orgId = "org-456",
        string[]? roles = null,
        string? email = "test@example.com",
        DateTime? expires = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (orgId != null)
        {
            claims.Add(new Claim("org_id", orgId));
            claims.Add(new Claim("org_name", "Test Organization"));
        }

        if (email != null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        }

        claims.Add(new Claim("name", "Test User"));
        claims.Add(new Claim("token_type", "user"));

        foreach (var role in roles ?? ["sorcha:participant"])
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var keyBytes = Encoding.UTF8.GetBytes(_signingKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            IssuedAt = DateTime.UtcNow,
            Issuer = "https://tenant.sorcha.io",
            Audience = "https://api.sorcha.io",
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private McpSessionService CreateService(bool skipValidation = false)
    {
        var jwtOptions = new JwtOptions
        {
            Issuer = "https://tenant.sorcha.io",
            Audiences = ["https://api.sorcha.io"],
            SigningKey = _signingKey,
            SkipValidation = skipValidation
        };

        var jwtHandler = new JwtValidationHandler(
            Options.Create(jwtOptions),
            _jwtLoggerMock.Object);

        return new McpSessionService(jwtHandler, _loggerMock.Object);
    }

    [Fact]
    public void InitializeFromToken_ValidToken_ExtractsAllClaims()
    {
        // Arrange
        var service = CreateService();
        var token = GenerateTestToken(
            userId: "user-123",
            orgId: "org-456",
            roles: ["sorcha:admin", "sorcha:designer"],
            email: "admin@example.com");

        // Act
        service.InitializeFromToken(token);

        // Assert
        service.CurrentSession.Should().NotBeNull();
        service.CurrentSession!.UserId.Should().Be("user-123");
        service.CurrentSession.TenantId.Should().Be("org-456");
        service.CurrentSession.OrganizationName.Should().Be("Test Organization");
        service.CurrentSession.Email.Should().Be("admin@example.com");
        service.CurrentSession.DisplayName.Should().Be("Test User");
        service.CurrentSession.Roles.Should().Contain("sorcha:admin");
        service.CurrentSession.Roles.Should().Contain("sorcha:designer");
        service.CurrentSession.TokenType.Should().Be("user");
    }

    [Fact]
    public void InitializeFromToken_ValidToken_SetsExpirationCorrectly()
    {
        // Arrange
        var service = CreateService();
        var expiry = DateTime.UtcNow.AddHours(2);
        var token = GenerateTestToken(expires: expiry);

        // Act
        service.InitializeFromToken(token);

        // Assert
        service.CurrentSession.Should().NotBeNull();
        // Allow for some clock skew
        service.CurrentSession!.ExpiresAt.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void InitializeFromToken_NoOrgId_UsesDefaultTenant()
    {
        // Arrange
        var service = CreateService();
        var token = GenerateTestToken(orgId: null);

        // Act
        service.InitializeFromToken(token);

        // Assert
        service.CurrentSession.Should().NotBeNull();
        service.CurrentSession!.TenantId.Should().Be("default");
    }

    [Fact]
    public void InitializeFromToken_MapsStandardRolesToMcpRoles()
    {
        // Arrange
        var service = CreateService();
        var token = GenerateTestToken(roles: ["Admin", "Designer", "User"]);

        // Act
        service.InitializeFromToken(token);

        // Assert
        service.CurrentSession.Should().NotBeNull();
        service.CurrentSession!.Roles.Should().Contain("sorcha:admin");
        service.CurrentSession.Roles.Should().Contain("sorcha:designer");
        service.CurrentSession.Roles.Should().Contain("sorcha:participant");
    }

    [Fact]
    public void IsTokenExpired_NotExpired_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var token = GenerateTestToken(expires: DateTime.UtcNow.AddHours(1));
        service.InitializeFromToken(token);

        // Act
        var result = service.IsTokenExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenExpired_Expired_ReturnsTrue()
    {
        // Arrange - create a token that expires in 1 second
        var service = CreateService();
        var token = GenerateTestToken(expires: DateTime.UtcNow.AddSeconds(1));
        service.InitializeFromToken(token);

        // Wait for token to expire
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Act
        var result = service.IsTokenExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_NoSession_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsTokenExpired();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InitializeFromToken_NullToken_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.InitializeFromToken(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InitializeFromToken_EmptyToken_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.InitializeFromToken("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CurrentSession_HasRoleHelpers_WorkCorrectly()
    {
        // Arrange
        var service = CreateService();
        var token = GenerateTestToken(roles: ["sorcha:admin", "sorcha:designer"]);
        service.InitializeFromToken(token);

        // Act & Assert
        service.CurrentSession!.IsAdmin.Should().BeTrue();
        service.CurrentSession.IsDesigner.Should().BeTrue();
        service.CurrentSession.IsParticipant.Should().BeFalse();
        service.CurrentSession.HasRole("sorcha:admin").Should().BeTrue();
        service.CurrentSession.HasRole("sorcha:unknown").Should().BeFalse();
    }
}
