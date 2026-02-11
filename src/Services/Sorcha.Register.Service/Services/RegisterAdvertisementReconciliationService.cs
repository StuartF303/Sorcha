// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Register.Core.Storage;
using Sorcha.ServiceClients.Peer;

namespace Sorcha.Register.Service.Services;

/// <summary>
/// Background service that re-advertises all public registers to the Peer Service on startup.
/// Solves the problem where the Peer Service loses in-memory advertisements on restart.
/// </summary>
public class RegisterAdvertisementReconciliationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegisterAdvertisementReconciliationService> _logger;

    /// <summary>
    /// Delay before first reconciliation to allow services to warm up.
    /// </summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Interval between periodic reconciliation runs.
    /// </summary>
    private static readonly TimeSpan ReconciliationInterval = TimeSpan.FromMinutes(5);

    public RegisterAdvertisementReconciliationService(
        IServiceScopeFactory scopeFactory,
        ILogger<RegisterAdvertisementReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for services to warm up before first reconciliation
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAdvertisementsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Register advertisement reconciliation failed, will retry");
            }

            await Task.Delay(ReconciliationInterval, stoppingToken);
        }
    }

    private async Task ReconcileAdvertisementsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var registerRepository = scope.ServiceProvider.GetRequiredService<IRegisterRepository>();
        var peerClient = scope.ServiceProvider.GetRequiredService<IPeerServiceClient>();

        var allRegisters = await registerRepository.GetRegistersAsync(cancellationToken);
        var advertisedRegisters = allRegisters.Where(r => r.Advertise).ToList();

        if (advertisedRegisters.Count == 0)
        {
            _logger.LogDebug("No advertised registers to reconcile");
            return;
        }

        _logger.LogInformation(
            "Reconciling {Count} advertised registers with Peer Service",
            advertisedRegisters.Count);

        var successCount = 0;
        foreach (var register in advertisedRegisters)
        {
            try
            {
                await peerClient.AdvertiseRegisterAsync(register.Id, isPublic: true, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "Failed to re-advertise register {RegisterId} — Peer Service may be unavailable",
                    register.Id);
            }
        }

        _logger.LogInformation(
            "Register advertisement reconciliation complete: {Success}/{Total} registers advertised",
            successCount, advertisedRegisters.Count);
    }
}
