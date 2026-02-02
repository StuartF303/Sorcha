// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.ServiceClients.Register;
using Sorcha.Register.Models;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Background service that monitors memory pools and triggers docket building
/// based on hybrid conditions (time threshold OR size threshold)
/// </summary>
public class DocketBuildTriggerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRegisterMonitoringRegistry _registry;
    private readonly DocketBuildConfiguration _config;
    private readonly ILogger<DocketBuildTriggerService> _logger;

    // Track last build time per register
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastBuildTimes = new();

    public DocketBuildTriggerService(
        IServiceScopeFactory scopeFactory,
        IRegisterMonitoringRegistry registry,
        IOptions<DocketBuildConfiguration> config,
        ILogger<DocketBuildTriggerService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Docket build trigger service starting. Time threshold: {TimeThreshold}, Size threshold: {SizeThreshold}",
            _config.TimeThreshold, _config.SizeThreshold);

        // Use time threshold as the check interval (or minimum of 1 second)
        var checkInterval = _config.TimeThreshold > TimeSpan.FromSeconds(1)
            ? _config.TimeThreshold
            : TimeSpan.FromSeconds(1);

        using var timer = new PeriodicTimer(checkInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var activeRegisters = _registry.GetAll().ToList();
                _logger.LogTrace("Checking docket build triggers for {RegisterCount} active registers",
                    activeRegisters.Count);

                // Check each active register
                foreach (var registerId in activeRegisters)
                {
                    try
                    {
                        await CheckAndBuildDocketAsync(registerId, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking/building docket for register {RegisterId}", registerId);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Docket build trigger service stopping");
        }
    }

    /// <summary>
    /// Checks if a register should build a docket and triggers build if needed
    /// </summary>
    private async Task CheckAndBuildDocketAsync(string registerId, CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services
        using var scope = _scopeFactory.CreateScope();
        var docketBuilder = scope.ServiceProvider.GetRequiredService<IDocketBuilder>();

        // Get last build time (or use epoch if never built)
        var lastBuildTime = _lastBuildTimes.GetOrAdd(registerId, DateTimeOffset.UnixEpoch);

        // Check if we should build
        var shouldBuild = await docketBuilder.ShouldBuildDocketAsync(registerId, lastBuildTime, cancellationToken);

        if (!shouldBuild)
        {
            _logger.LogTrace("Register {RegisterId} does not meet build thresholds yet", registerId);
            return;
        }

        _logger.LogInformation("Triggering docket build for register {RegisterId}", registerId);

        // Build docket
        var docket = await docketBuilder.BuildDocketAsync(registerId, forceBuild: false, cancellationToken);

        if (docket != null)
        {
            _logger.LogInformation("Successfully built docket {DocketNumber} for register {RegisterId}",
                docket.DocketNumber, registerId);

            // Update last build time
            _lastBuildTimes[registerId] = DateTimeOffset.UtcNow;

            // TODO Phase 5: Trigger consensus process here
            // For now, directly write docket to Register Service (bypass consensus for MVP)
            await WriteDocketAndTransactionsAsync(scope, docket, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Failed to build docket for register {RegisterId}", registerId);
        }
    }

    /// <summary>
    /// Manually triggers a docket build for a register
    /// </summary>
    public async Task<bool> TriggerManualBuildAsync(string registerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual docket build triggered for register {RegisterId}", registerId);

        // Create a scope to resolve scoped services
        using var scope = _scopeFactory.CreateScope();
        var docketBuilder = scope.ServiceProvider.GetRequiredService<IDocketBuilder>();

        var docket = await docketBuilder.BuildDocketAsync(registerId, forceBuild: true, cancellationToken);

        if (docket != null)
        {
            _lastBuildTimes[registerId] = DateTimeOffset.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Writes docket and transactions to Register Service after successful build
    /// </summary>
    private async Task WriteDocketAndTransactionsAsync(
        IServiceScope scope,
        Sorcha.Validator.Service.Models.Docket docket,
        CancellationToken cancellationToken)
    {
        var registerClient = scope.ServiceProvider.GetRequiredService<IRegisterServiceClient>();
        var memPoolManager = scope.ServiceProvider.GetRequiredService<IMemPoolManager>();

        try
        {
            // Convert docket to DocketModel  (Sorcha.ServiceClients.Register.DocketModel)
            // Note: Transactions list is simplified - Register Service will fetch full transaction data
            var docketModel = new DocketModel
            {
                DocketId = docket.DocketId,
                RegisterId = docket.RegisterId,
                DocketNumber = docket.DocketNumber,
                PreviousHash = docket.PreviousHash,
                DocketHash = docket.DocketHash,
                CreatedAt = docket.CreatedAt,
                Transactions = docket.Transactions.Select(t => new Sorcha.Register.Models.TransactionModel
                {
                    TxId = t.TransactionId,
                    RegisterId = docket.RegisterId,
                    TimeStamp = docket.CreatedAt.DateTime,
                    SenderWallet = "system",  // Will be populated by Register Service
                    RecipientsWallets = [],
                    Payloads = [],
                    PayloadCount = 0,
                    Signature = string.Empty
                }).ToList(),
                ProposerValidatorId = docket.ProposerValidatorId,
                MerkleRoot = docket.MerkleRoot
            };

            // Write docket to Register Service
            var written = await registerClient.WriteDocketAsync(docketModel, cancellationToken);

            if (!written)
            {
                _logger.LogError("Failed to write docket {DocketNumber} to Register Service for register {RegisterId}",
                    docket.DocketNumber, docket.RegisterId);
                return;
            }

            _logger.LogInformation("Wrote docket {DocketNumber} to Register Service for register {RegisterId}",
                docket.DocketNumber, docket.RegisterId);

            // Remove transactions from memory pool (they're now persisted in Register Service via docket)
            foreach (var tx in docket.Transactions)
            {
                await memPoolManager.RemoveTransactionAsync(docket.RegisterId, tx.TransactionId, cancellationToken);
                _logger.LogDebug("Removed transaction {TransactionId} from memory pool", tx.TransactionId);
            }

            _logger.LogInformation("Moved {Count} transactions from memory pool to register {RegisterId} docket {DocketNumber}",
                docket.Transactions.Count, docket.RegisterId, docket.DocketNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write docket {DocketNumber} and transactions to Register Service",
                docket.DocketNumber);
            throw;
        }
    }
}
