// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Polly;
using Serilog;
using Sorcha.Tenant.Service.Data;
using StackExchange.Redis;

namespace Sorcha.Tenant.Service.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for Tenant Service integration tests.
/// Uses in-memory database, mock Redis, and test authentication for testing.
/// Automatically seeds test data including a test organization and users.
/// </summary>
public class TenantServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;
    private bool _seeded;

    public TenantServiceWebApplicationFactory()
    {
        // Use a unique database name per factory instance for isolation
        _databaseName = $"TenantServiceIntegrationTests_{Guid.NewGuid()}";
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Disable Serilog to avoid the "logger is already frozen" issue
        builder.UseSerilog((_, _) => { }, preserveStaticLogger: true);
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core related services to avoid provider conflicts
            // This must be done thoroughly since EF Core registers many internal services
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TenantDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // Also remove the generic DbContextOptions
            var genericDbContextOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions));
            if (genericDbContextOptionsDescriptor != null)
                services.Remove(genericDbContextOptionsDescriptor);

            // Remove TenantDbContext registrations
            services.RemoveAll<TenantDbContext>();

            // Remove all EF Core internal services (this handles provider conflicts)
            var efServiceTypes = services.Where(d =>
                d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                d.ImplementationType?.FullName?.StartsWith("Npgsql") == true).ToList();
            foreach (var efService in efServiceTypes)
                services.Remove(efService);

            // Add in-memory database with consistent name for this factory instance
            services.AddDbContext<TenantDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Remove Redis connection and use a mock for testing
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IAsyncPolicy>();

            // Register a null policy for circuit breaker (no-op in tests)
            services.AddSingleton<IAsyncPolicy>(Policy.NoOpAsync());

            // Create a mock Redis connection multiplexer using Moq
            var mockDatabase = new Mock<IDatabase>();
            mockDatabase.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            mockDatabase.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);
            mockDatabase.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            mockDatabase.Setup(d => d.SetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            mockDatabase.Setup(d => d.SetContainsAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(false);

            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            mockMultiplexer.Setup(m => m.IsConnected).Returns(true);
            mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            services.AddSingleton(mockMultiplexer.Object);

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
    /// Ensures the database is seeded with test data.
    /// Call this method after creating the factory to seed the database.
    /// Thread-safe for use in parallel test execution.
    /// </summary>
    public async Task EnsureSeededAsync()
    {
        if (_seeded) return;

        await TestDataSeeder.SeedAsync(Services);
        _seeded = true;
    }

    /// <summary>
    /// Creates an HttpClient configured for a regular authenticated user.
    /// Uses the seeded test member user.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        return client;
    }

    /// <summary>
    /// Creates an HttpClient configured for an administrator user.
    /// Uses the seeded test admin user.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", TestDataSeeder.TestAdminUserId.ToString());
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with no authentication headers.
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient();
    }
}

/// <summary>
/// Collection definition for shared test context.
/// </summary>
[CollectionDefinition("TenantService")]
public class TenantServiceCollection : ICollectionFixture<TenantServiceWebApplicationFactory>
{
}
