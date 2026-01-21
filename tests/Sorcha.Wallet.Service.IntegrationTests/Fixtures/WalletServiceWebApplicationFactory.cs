// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Sorcha.Wallet.Service.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for Wallet Service integration tests.
/// Configures test authentication and in-memory services.
/// </summary>
public class WalletServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                // JWT Settings for authentication
                ["JwtSettings:Issuer"] = "https://test.sorcha.io",
                ["JwtSettings:Audiences:0"] = "https://test-api.sorcha.io",
                ["JwtSettings:SigningKey"] = "test-signing-key-for-integration-tests-minimum-32-characters-required",
                ["JwtSettings:AccessTokenLifetimeMinutes"] = "60",
                ["JwtSettings:RefreshTokenLifetimeHours"] = "24",
                ["JwtSettings:ServiceTokenLifetimeHours"] = "8",
                ["JwtSettings:ClockSkewMinutes"] = "5",
                ["JwtSettings:ValidateIssuer"] = "false",
                ["JwtSettings:ValidateAudience"] = "false",
                ["JwtSettings:ValidateIssuerSigningKey"] = "false",
                ["JwtSettings:ValidateLifetime"] = "false",
                // Disable PostgreSQL for in-memory testing
                ["ConnectionStrings:WalletDatabase"] = ""
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Remove all existing authentication schemes and handlers
            services.RemoveAll<IAuthenticationService>();
            services.RemoveAll<IAuthenticationHandlerProvider>();
            services.RemoveAll<IAuthenticationSchemeProvider>();

            // Add test authentication as the default scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>
    /// Creates an HttpClient configured for a regular authenticated user.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    /// <summary>
    /// Creates an HttpClient configured for an administrator user.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with no authentication headers.
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient();
    }

    /// <summary>
    /// Creates an HttpClient configured for a specific user.
    /// </summary>
    public HttpClient CreateClientForUser(string userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        return client;
    }
}

/// <summary>
/// Collection definition for shared test context.
/// </summary>
[CollectionDefinition("WalletService")]
public class WalletServiceCollection : ICollectionFixture<WalletServiceWebApplicationFactory>
{
}
