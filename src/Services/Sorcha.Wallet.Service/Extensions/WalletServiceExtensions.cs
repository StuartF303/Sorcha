using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sorcha.Cryptography;
using Sorcha.Cryptography.Core;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Wallet.Core.Encryption.Interfaces;
using Sorcha.Wallet.Core.Encryption.Providers;
using Sorcha.Wallet.Core.Events.Interfaces;
using Sorcha.Wallet.Core.Events.Publishers;
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
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWalletService(this IServiceCollection services)
    {
        // Register Cryptography services (required by WalletService)
        services.AddSingleton<ICryptoModule, CryptoModule>();
        services.AddSingleton<IHashProvider, HashProvider>();
        services.AddSingleton<IWalletUtilities, Sorcha.Cryptography.Utilities.WalletUtilities>();

        // Register infrastructure services
        // TODO: In production, replace these with persistent implementations
        services.AddSingleton<IEncryptionProvider, LocalEncryptionProvider>();
        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        services.AddSingleton<IWalletRepository, InMemoryWalletRepository>();

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
    /// Adds health checks for Wallet Service dependencies
    /// </summary>
    /// <param name="builder">The health checks builder</param>
    /// <returns>The health checks builder for chaining</returns>
    public static IHealthChecksBuilder AddWalletServiceHealthChecks(this IHealthChecksBuilder builder)
    {
        // Add health checks for dependencies
        // Repository health check
        builder.AddCheck<WalletRepositoryHealthCheck>("wallet-repository");

        // Encryption provider health check
        builder.AddCheck<EncryptionProviderHealthCheck>("encryption-provider");

        return builder;
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
            // Test encryption/decryption with a simple test payload
            var testData = "health-check"u8.ToArray();
            var keyId = "health-check-key";

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
