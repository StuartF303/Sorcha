// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Sorcha.Blueprint.Schemas.Models;
using Sorcha.Blueprint.Schemas.Repositories;
using StackExchange.Redis;

namespace Sorcha.Blueprint.Service.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for Blueprint Service integration tests.
/// Uses in-memory services for fast testing.
/// </summary>
public class BlueprintServiceWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public BlueprintServiceWebApplicationFactory()
    {
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        return base.CreateHost(builder);
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
                ["JwtSettings:ValidateLifetime"] = "false"
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Remove Redis connection and use a mock for testing
            services.RemoveAll<IConnectionMultiplexer>();

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
            mockDatabase.Setup(d => d.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(0);

            var mockSubscriber = new Mock<ISubscriber>();
            mockSubscriber.Setup(s => s.SubscribeAsync(It.IsAny<RedisChannel>(), It.IsAny<Action<RedisChannel, RedisValue>>(), It.IsAny<CommandFlags>()))
                .Returns(Task.CompletedTask);

            var mockMultiplexer = new Mock<IConnectionMultiplexer>();
            mockMultiplexer.Setup(m => m.IsConnected).Returns(true);
            mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);
            mockMultiplexer.Setup(m => m.GetSubscriber(It.IsAny<object>()))
                .Returns(mockSubscriber.Object);

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

            // Add in-memory schema repository for testing CRUD operations
            services.RemoveAll<ISchemaRepository>();
            services.AddSingleton<ISchemaRepository, InMemorySchemaRepository>();
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
    /// Creates an HttpClient configured for an organization member (with org_id claim).
    /// </summary>
    public HttpClient CreateOrganizationMemberClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token");
        // Uses default organization from TestAuthHandler (test-org-id)
        return client;
    }
}

/// <summary>
/// Collection definition for shared test context.
/// </summary>
[CollectionDefinition("BlueprintService")]
public class BlueprintServiceCollection : ICollectionFixture<BlueprintServiceWebApplicationFactory>
{
}

/// <summary>
/// In-memory implementation of ISchemaRepository for testing.
/// </summary>
internal sealed class InMemorySchemaRepository : ISchemaRepository
{
    private readonly ConcurrentDictionary<string, SchemaEntry> _schemas = new();

    private static string GetKey(string identifier, string? organizationId)
        => $"{organizationId ?? "global"}:{identifier}";

    public Task<SchemaEntry?> GetByIdentifierAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        // First try organization-scoped
        if (organizationId is not null && _schemas.TryGetValue(GetKey(identifier, organizationId), out var orgSchema))
        {
            return Task.FromResult<SchemaEntry?>(orgSchema);
        }

        // Then try global
        if (_schemas.TryGetValue(GetKey(identifier, null), out var globalSchema))
        {
            return Task.FromResult<SchemaEntry?>(globalSchema);
        }

        return Task.FromResult<SchemaEntry?>(null);
    }

    public Task<(IReadOnlyList<SchemaEntry> Schemas, int TotalCount, string? NextCursor)> ListAsync(
        SchemaCategory? category = null,
        SchemaStatus? status = null,
        string? search = null,
        string? organizationId = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var query = _schemas.Values.AsEnumerable();

        if (category.HasValue)
        {
            query = query.Where(s => s.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Identifier.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(organizationId))
        {
            query = query.Where(s =>
                s.OrganizationId == organizationId ||
                s.IsGloballyPublished ||
                s.Category == SchemaCategory.System ||
                s.Category == SchemaCategory.External);
        }

        var list = query.Take(limit).ToList();
        return Task.FromResult<(IReadOnlyList<SchemaEntry>, int, string?)>((list, list.Count, null));
    }

    public Task<SchemaEntry> CreateAsync(SchemaEntry entry, CancellationToken cancellationToken = default)
    {
        var key = GetKey(entry.Identifier, entry.OrganizationId);
        if (!_schemas.TryAdd(key, entry))
        {
            throw new InvalidOperationException($"Schema '{entry.Identifier}' already exists.");
        }
        return Task.FromResult(entry);
    }

    public Task<SchemaEntry> UpdateAsync(SchemaEntry entry, CancellationToken cancellationToken = default)
    {
        var key = GetKey(entry.Identifier, entry.OrganizationId);
        if (!_schemas.ContainsKey(key))
        {
            throw new KeyNotFoundException($"Schema '{entry.Identifier}' not found.");
        }
        _schemas[key] = entry;
        return Task.FromResult(entry);
    }

    public Task<bool> DeleteAsync(
        string identifier,
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(identifier, organizationId);
        return Task.FromResult(_schemas.TryRemove(key, out _));
    }

    public Task<bool> ExistsAsync(
        string identifier,
        string? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_schemas.ContainsKey(GetKey(identifier, organizationId)));
    }

    public Task<bool> ExistsGloballyAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_schemas.Values.Any(s =>
            s.Identifier == identifier &&
            (s.IsGloballyPublished || s.Category == SchemaCategory.External)));
    }
}
