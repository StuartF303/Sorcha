// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Thread-safe provider for system wallet access
/// </summary>
public class SystemWalletProvider : ISystemWalletProvider
{
    private readonly ValidatorConfiguration _config;
    private readonly ILogger<SystemWalletProvider> _logger;
    private string? _systemWalletId;
    private readonly object _lock = new();

    public SystemWalletProvider(
        IOptions<ValidatorConfiguration> config,
        ILogger<SystemWalletProvider> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize with configured value if present
        if (!string.IsNullOrWhiteSpace(_config.SystemWalletAddress))
        {
            _systemWalletId = _config.SystemWalletAddress;
            _logger.LogDebug("System wallet initialized from configuration: {WalletId}", _systemWalletId);
        }
    }

    public bool IsInitialized
    {
        get
        {
            lock (_lock)
            {
                return !string.IsNullOrWhiteSpace(_systemWalletId);
            }
        }
    }

    public string? GetSystemWalletId()
    {
        lock (_lock)
        {
            return _systemWalletId;
        }
    }

    public void SetSystemWalletId(string walletId)
    {
        if (string.IsNullOrWhiteSpace(walletId))
        {
            throw new ArgumentException("Wallet ID cannot be null or empty", nameof(walletId));
        }

        lock (_lock)
        {
            _systemWalletId = walletId;
            _logger.LogInformation("System wallet ID set to: {WalletId}", walletId);
        }
    }
}
