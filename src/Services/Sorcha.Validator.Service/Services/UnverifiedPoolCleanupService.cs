// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Background service that periodically cleans up expired transactions from the unverified Redis pool
/// </summary>
public class UnverifiedPoolCleanupService : BackgroundService
{
    private readonly ITransactionPoolPoller _poller;
    private readonly TransactionPoolPollerService _pollerService;
    private readonly TransactionPoolPollerConfiguration _config;
    private readonly ILogger<UnverifiedPoolCleanupService> _logger;

    /// <summary>
    /// Cleanup interval - runs less frequently than the main cleanup
    /// </summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    public UnverifiedPoolCleanupService(
        ITransactionPoolPoller poller,
        TransactionPoolPollerService pollerService,
        IOptions<TransactionPoolPollerConfiguration> config,
        ILogger<UnverifiedPoolCleanupService> logger)
    {
        _poller = poller ?? throw new ArgumentNullException(nameof(poller));
        _pollerService = pollerService ?? throw new ArgumentNullException(nameof(pollerService));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Unverified pool cleanup service is disabled (poller disabled)");
            return;
        }

        _logger.LogInformation(
            "Unverified pool cleanup service starting. Cleanup interval: {Interval}",
            CleanupInterval);

        using var timer = new PeriodicTimer(CleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogDebug("Running unverified pool cleanup...");

                try
                {
                    await CleanupAllRegistersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during unverified pool cleanup");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Unverified pool cleanup service stopping");
        }
    }

    private async Task CleanupAllRegistersAsync(CancellationToken ct)
    {
        var registers = _pollerService.GetActiveRegisters();

        if (registers.Count == 0)
            return;

        var totalExpired = 0;
        foreach (var registerId in registers)
        {
            try
            {
                var expired = await _poller.CleanupExpiredAsync(registerId, ct);
                totalExpired += expired;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired transactions for register {RegisterId}", registerId);
            }
        }

        if (totalExpired > 0)
        {
            _logger.LogInformation(
                "Unverified pool cleanup completed. Removed {TotalExpired} expired transactions across {RegisterCount} registers",
                totalExpired, registers.Count);
        }
    }
}
