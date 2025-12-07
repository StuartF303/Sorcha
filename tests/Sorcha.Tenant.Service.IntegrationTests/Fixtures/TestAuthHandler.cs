// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sorcha.Tenant.Service.IntegrationTests.Fixtures;

/// <summary>
/// Test authentication handler that bypasses JWT validation for integration tests.
/// Uses seeded test user data when available via X-Test-User-Id header.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if we should authenticate (based on Authorization header presence)
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            // Return no result for endpoints that allow anonymous access
            // This will cause authentication to fail for endpoints that require auth
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Check for test header to determine role
        var isAdmin = Request.Headers.TryGetValue("X-Test-Role", out var roleHeader) &&
                      roleHeader.ToString().Contains("Administrator");

        // Use provided user ID or generate a new one
        var userId = Request.Headers.TryGetValue("X-Test-User-Id", out var userIdHeader)
            ? userIdHeader.ToString()
            : isAdmin
                ? TestDataSeeder.TestAdminUserId.ToString()
                : TestDataSeeder.TestMemberUserId.ToString();

        var email = isAdmin ? TestDataSeeder.TestAdminEmail : TestDataSeeder.TestMemberEmail;
        var displayName = isAdmin ? TestDataSeeder.TestAdminName : TestDataSeeder.TestMemberName;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email),
            new("sub", userId),
            new("org_id", TestDataSeeder.TestOrganizationId.ToString()),
            new("org_name", TestDataSeeder.TestOrganizationName),
            new("token_type", "user")
        };

        // Add appropriate role
        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "Member"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
