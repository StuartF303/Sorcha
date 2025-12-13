// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;
using Serilog.Events;
using Sorcha.Tenant.Service.Data;
using Sorcha.Tenant.Service.Services;
using StackExchange.Redis;

namespace Sorcha.Tenant.Service.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing of the Tenant Service.
/// Provides in-memory database, mock Redis, and test authentication.
/// </summary>
public class TenantServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Configure test services to use in-memory database and mock Redis.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configure Serilog to prevent "logger frozen" errors during tests
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateLogger();

        builder.ConfigureTestServices(services =>
        {
            // Remove PostgreSQL DbContext and replace with InMemory
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TenantDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove the existing DbContext
            services.RemoveAll<TenantDbContext>();

            // Add InMemory DbContext
            services.AddDbContext<TenantDbContext>(options =>
            {
                options.UseInMemoryDatabase("TenantServiceTests");
                options.EnableSensitiveDataLogging();
            });

            // Remove existing Redis connection
            services.RemoveAll<IConnectionMultiplexer>();

            // Mock Redis connection
            var mockRedis = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();

            mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            mockDatabase.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            mockDatabase.Setup(d => d.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            mockDatabase.Setup(d => d.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);

            services.AddSingleton(mockRedis.Object);

            // Remove database initializer (we'll seed manually in tests)
            services.RemoveAll<IHostedService>();

            // Add test authentication scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Create an HTTP client with test authentication configured for an admin user.
    /// </summary>
    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Administrator");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", TestDataSeeder.AdminUserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Organization-Id", TestDataSeeder.TestOrganizationId.ToString());
        return client;
    }

    /// <summary>
    /// Create an HTTP client with test authentication configured for a regular member.
    /// </summary>
    public HttpClient CreateMemberClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Member");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", TestDataSeeder.MemberUserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Organization-Id", TestDataSeeder.TestOrganizationId.ToString());
        return client;
    }

    /// <summary>
    /// Create an unauthenticated HTTP client.
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient();
    }

    /// <summary>
    /// Seed test data into the database.
    /// </summary>
    public async Task SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        await TestDataSeeder.SeedAsync(context);
    }
}
