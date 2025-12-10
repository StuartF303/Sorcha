// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Polly;
using Serilog;
using Sorcha.Tenant.Service.Data;
using Sorcha.Tenant.Service.IntegrationTests.Configuration;
using StackExchange.Redis;
using Testcontainers.PostgreSql;

namespace Sorcha.Tenant.Service.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for Tenant Service integration tests.
/// Supports both InMemory database (fast, default) and PostgreSQL via Testcontainers (realistic).
/// Set environment variable TEST_DATABASE_MODE=PostgreSQL to use Testcontainers.
/// </summary>
public class TenantServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName;
    private PostgreSqlContainer? _postgresContainer;
    private string? _connectionString;
    private bool _seeded;
    private bool _containerInitialized;
    private readonly object _containerLock = new();

    public TenantServiceWebApplicationFactory()
    {
        // Use a unique database name per factory instance for isolation
        _databaseName = $"TenantServiceIntegrationTests_{Guid.NewGuid()}";
    }

    /// <summary>
    /// Ensures the PostgreSQL container is started before configuration.
    /// Called synchronously from ConfigureWebHost.
    /// </summary>
    private void EnsureContainerStarted()
    {
        if (!TestConfiguration.UsePostgreSQL || _containerInitialized)
            return;

        lock (_containerLock)
        {
            if (_containerInitialized)
                return;

            // Start PostgreSQL Testcontainer synchronously
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase(_databaseName)
                .WithUsername("sorcha_test")
                .WithPassword("sorcha_test_pass")
                .WithCleanUp(true)
                .Build();

            _postgresContainer.StartAsync().GetAwaiter().GetResult();
            _connectionString = _postgresContainer.GetConnectionString();

            Console.WriteLine($"[TEST] PostgreSQL container started");
            Console.WriteLine($"[TEST] Connection string: {_connectionString}");
            Console.WriteLine($"[TEST] Host: {_postgresContainer.Hostname}");
            Console.WriteLine($"[TEST] Port: {_postgresContainer.GetMappedPublicPort(5432)}");

            _containerInitialized = true;
        }
    }

    /// <summary>
    /// Initializes the test infrastructure (starts PostgreSQL container if needed).
    /// Called automatically by xUnit before any tests run.
    /// </summary>
    public Task InitializeAsync()
    {
        // Container is already started in ConfigureWebHost, nothing more to do
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up test infrastructure (stops PostgreSQL container if needed).
    /// Called automatically by xUnit after all tests complete.
    /// </summary>
    public new async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }

        await base.DisposeAsync();
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

        // Ensure PostgreSQL container is started before configuration (if needed)
        EnsureContainerStarted();

        // Configure connection string and JWT settings for tests
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                // JWT Settings for TokenService
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
                ["JwtSettings:ValidateLifetime"] = "false"
            };

            if (TestConfiguration.UsePostgreSQL)
            {
                // Use PostgreSQL Testcontainer connection string
                testConfig["ConnectionStrings:TenantDatabase"] = _connectionString;
            }
            else
            {
                // Clear connection string to force InMemory database usage
                testConfig["ConnectionStrings:TenantDatabase"] = null!;
            }

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations for both InMemory and PostgreSQL modes
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TenantDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var genericDbContextOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions));
            if (genericDbContextOptionsDescriptor != null)
                services.Remove(genericDbContextOptionsDescriptor);

            services.RemoveAll<TenantDbContext>();

            var efServiceTypes = services.Where(d =>
                d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                d.ImplementationType?.FullName?.StartsWith("Npgsql") == true).ToList();
            foreach (var efService in efServiceTypes)
                services.Remove(efService);

            if (TestConfiguration.UseInMemory)
            {
                // Configure InMemory database using AddDbContext for proper EF Core service registration
                services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                });

                Console.WriteLine($"[TEST] Configured InMemory DbContext with database name: {_databaseName}");
            }
            else if (TestConfiguration.UsePostgreSQL)
            {
                // Configure PostgreSQL with Testcontainers connection string
                services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
                {
                    options.UseNpgsql(_connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    });
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                });

                Console.WriteLine($"[TEST] Configured DbContext with connection string: {_connectionString}");
            }

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

        // Run migrations if using PostgreSQL
        if (TestConfiguration.UsePostgreSQL)
        {
            await using var scope = Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            await context.Database.MigrateAsync();
        }

        // Seed test data
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
