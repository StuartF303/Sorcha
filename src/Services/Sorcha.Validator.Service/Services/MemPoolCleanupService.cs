// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Background service that periodically cleans up expired transactions from memory pools
/// </summary>
public class MemPoolCleanupService : BackgroundService
{
    private readonly IMemPoolManager _memPoolManager;
    private readonly MemPoolConfiguration _config;
    private readonly ILogger<MemPoolCleanupService> _logger;

    public MemPoolCleanupService(
        IMemPoolManager memPoolManager,
        IOptions<MemPoolConfiguration> config,
        ILogger<MemPoolCleanupService> logger)
    {
        _memPoolManager = memPoolManager ?? throw new ArgumentNullException(nameof(memPoolManager));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MemPool cleanup service starting. Cleanup interval: {Interval}",
            _config.CleanupInterval);

        using var timer = new PeriodicTimer(_config.CleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogDebug("Running memory pool cleanup...");

                try
                {
                    await _memPoolManager.CleanupExpiredTransactionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during memory pool cleanup");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MemPool cleanup service stopping");
        }
    }
}
