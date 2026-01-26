// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Background service that periodically cleans up expired transactions
/// from the verified transaction queue.
/// </summary>
public class VerifiedQueueCleanupService : BackgroundService
{
    private readonly IVerifiedTransactionQueue _queue;
    private readonly VerifiedQueueConfiguration _config;
    private readonly ILogger<VerifiedQueueCleanupService> _logger;

    public VerifiedQueueCleanupService(
        IVerifiedTransactionQueue queue,
        IOptions<VerifiedQueueConfiguration> config,
        ILogger<VerifiedQueueCleanupService> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Verified queue cleanup service starting with interval {Interval}",
            _config.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.CleanupInterval, stoppingToken);

                var removed = _queue.CleanupExpired();

                if (removed > 0)
                {
                    var stats = _queue.GetStats();
                    _logger.LogInformation(
                        "Cleanup completed: removed {Removed} expired transactions, {Remaining} remaining",
                        removed, stats.TotalTransactions);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during verified queue cleanup");
            }
        }

        _logger.LogInformation("Verified queue cleanup service stopping");
    }
}
