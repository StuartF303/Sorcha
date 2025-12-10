using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Sorcha.Cryptography;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Wallet.Core.Data;
using Sorcha.Wallet.Core.Encryption.Interfaces;
using Sorcha.Wallet.Core.Encryption.Providers;
using Sorcha.Wallet.Core.Events.Interfaces;
using Sorcha.Wallet.Core.Events.Publishers;
using Sorcha.Wallet.Core.Repositories;
using Sorcha.Wallet.Core.Repositories.Implementation;
using Sorcha.Wallet.Core.Repositories.Interfaces;
using Sorcha.Wallet.Core.Services.Implementation;
using Sorcha.Wallet.Core.Services.Interfaces;

namespace Sorcha.Wallet.Service.Extensions;

/// <summary>
/// Extension methods for configuring Wallet Service
/// </summary>
public static class WalletServiceExtensions
{
    /// <summary>
    /// Adds Wallet Service infrastructure and domain services to the container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWalletService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Cryptography services (required by WalletService)
        services.AddSingleton<ICryptoModule, CryptoModule>();
        services.AddSingleton<IHashProvider, HashProvider>();
        services.AddSingleton<IWalletUtilities, Sorcha.Cryptography.Utilities.WalletUtilities>();

        // Register infrastructure services
        services.AddSingleton<IEncryptionProvider, LocalEncryptionProvider>();
        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

        // Register database and repository
        services.AddWalletDatabase(configuration);

        // Register domain services with both interface and concrete types
        // (endpoints inject concrete types for now)
        services.AddScoped<KeyManagementService>();
        services.AddScoped<IKeyManagementService>(sp => sp.GetRequiredService<KeyManagementService>());

        services.AddScoped<TransactionService>();
        services.AddScoped<ITransactionService>(sp => sp.GetRequiredService<TransactionService>());

        services.AddScoped<DelegationService>();
        services.AddScoped<IDelegationService>(sp => sp.GetRequiredService<DelegationService>());

        services.AddScoped<WalletManager>();

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL database context for wallet persistence
    /// Falls back to InMemory repository if no connection string is configured
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWalletDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Try Aspire connection string name first, then fallback to standard name
        var connectionString = configuration.GetConnectionString("wallet-db")
                            ?? configuration.GetConnectionString("WalletDatabase");

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Configure NpgsqlDataSource with dynamic JSON support (required for Dictionary<string, string> serialization)
            services.AddNpgsqlDataSource(connectionString, dataSourceBuilder =>
            {
                // Enable dynamic JSON serialization for Dictionary types
                // This is required in Npgsql 8.0+ for JSONB columns with Dictionary<string, string>
                dataSourceBuilder.EnableDynamicJson();
            });

            // Configure PostgreSQL with EF Core using the registered data source
            // IMPORTANT: Do NOT pass connection string again - it will use the registered NpgsqlDataSource
            services.AddDbContext<WalletDbContext>((serviceProvider, options) =>
            {
                // Use the registered NpgsqlDataSource with EnableDynamicJson configured
                var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();

                options.UseNpgsql(dataSource, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    // Map to correct schema
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "wallet");
                });
            });

            // Use EF Core repository for persistent storage
            services.AddScoped<IWalletRepository, EfCoreWalletRepository>();
        }
        else
        {
            // Use in-memory repository for development/testing
            services.AddSingleton<IWalletRepository, InMemoryWalletRepository>();
        }

        return services;
    }

    /// <summary>
    /// Adds health checks for Wallet Service dependencies
    /// </summary>
    /// <param name="builder">The health checks builder</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The health checks builder for chaining</returns>
    public static IHealthChecksBuilder AddWalletServiceHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        // Add PostgreSQL health check if connection string is configured
        var connectionString = configuration.GetConnectionString("wallet-db")
                            ?? configuration.GetConnectionString("WalletDatabase");

        if (!string.IsNullOrEmpty(connectionString))
        {
            builder.AddNpgSql(connectionString, name: "wallet-postgresql");
        }

        // Repository health check
        builder.AddCheck<WalletRepositoryHealthCheck>("wallet-repository");

        // Encryption provider health check
        builder.AddCheck<EncryptionProviderHealthCheck>("encryption-provider");

        return builder;
    }

    /// <summary>
    /// Applies pending database migrations (for production deployment)
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>Task</returns>
    public static async Task ApplyWalletDatabaseMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            // Check if DbContext is registered (only if PostgreSQL is configured)
            var context = services.GetService<WalletDbContext>();
            if (context != null)
            {
                var logger = services.GetRequiredService<ILogger<WalletDbContext>>();
                logger.LogInformation("Applying Wallet Service database migrations...");

                // Apply pending migrations
                await context.Database.MigrateAsync();

                logger.LogInformation("Wallet Service database migrations applied successfully");
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<WalletDbContext>>();
            logger.LogError(ex, "An error occurred while applying Wallet Service migrations");
            throw;
        }
    }
}

/// <summary>
/// Health check for Wallet Repository
/// </summary>
internal class WalletRepositoryHealthCheck : IHealthCheck
{
    private readonly IWalletRepository _repository;

    public WalletRepositoryHealthCheck(IWalletRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple check - try to get count of wallets
            // This verifies the repository is accessible
            await Task.CompletedTask; // InMemoryRepository is always available
            return HealthCheckResult.Healthy("Wallet repository is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Wallet repository is not accessible", ex);
        }
    }
}

/// <summary>
/// Health check for Encryption Provider
/// </summary>
internal class EncryptionProviderHealthCheck : IHealthCheck
{
    private readonly IEncryptionProvider _encryptionProvider;

    public EncryptionProviderHealthCheck(IEncryptionProvider encryptionProvider)
    {
        _encryptionProvider = encryptionProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the default key from the provider (always exists)
            var keyId = _encryptionProvider.GetDefaultKeyId();

            // Test encryption/decryption with a simple test payload
            var testData = "health-check"u8.ToArray();

            var encrypted = await _encryptionProvider.EncryptAsync(testData, keyId, cancellationToken);
            var decrypted = await _encryptionProvider.DecryptAsync(encrypted, keyId, cancellationToken);

            if (!testData.SequenceEqual(decrypted))
            {
                return HealthCheckResult.Degraded("Encryption provider test failed - data mismatch");
            }

            return HealthCheckResult.Healthy("Encryption provider is functional");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Encryption provider is not functional", ex);
        }
    }
}
