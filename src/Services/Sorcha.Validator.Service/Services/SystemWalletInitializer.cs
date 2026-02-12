// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.ServiceClients.Wallet;
using Sorcha.Validator.Service.Configuration;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Background service that ensures the system wallet exists on startup
/// </summary>
public class SystemWalletInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISystemWalletProvider _walletProvider;
    private readonly ValidatorConfiguration _config;
    private readonly ILogger<SystemWalletInitializer> _logger;

    public SystemWalletInitializer(
        IServiceProvider serviceProvider,
        ISystemWalletProvider walletProvider,
        IOptions<ValidatorConfiguration> config,
        ILogger<SystemWalletInitializer> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _walletProvider = walletProvider ?? throw new ArgumentNullException(nameof(walletProvider));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing system wallet for validator {ValidatorId}", _config.ValidatorId);

        try
        {
            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var walletClient = scope.ServiceProvider.GetRequiredService<IWalletServiceClient>();

            // Check if system wallet is already configured
            var existingWalletId = _walletProvider.GetSystemWalletId();
            if (!string.IsNullOrWhiteSpace(existingWalletId))
            {
                // Verify it exists
                try
                {
                    var existingWallet = await walletClient.GetWalletAsync(existingWalletId, cancellationToken);
                    if (existingWallet != null)
                    {
                        _logger.LogInformation(
                            "System wallet {WalletId} already exists and verified (Address: {Address})",
                            existingWalletId, existingWallet.Address);
                        return;
                    }
                }
                catch (Exception)
                {
                    // Wallet doesn't exist, continue to create new one
                    _logger.LogWarning("Configured system wallet {WalletId} not found, will create new wallet", existingWalletId);
                }
            }

            // Create or retrieve system wallet using the dedicated endpoint
            _logger.LogInformation("Creating or retrieving system wallet for validator {ValidatorId}", _config.ValidatorId);

            var walletAddress = await walletClient.CreateOrRetrieveSystemWalletAsync(
                _config.ValidatorId,
                cancellationToken);

            // Store the wallet address in the provider
            _walletProvider.SetSystemWalletId(walletAddress);

            _logger.LogInformation(
                "System wallet initialized successfully - Address: {Address}",
                walletAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system wallet - genesis docket creation will fail");
            // Don't throw - allow service to start even if wallet creation fails
            // This allows manual wallet creation or troubleshooting
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("System wallet initializer stopping");
        return Task.CompletedTask;
    }
}

