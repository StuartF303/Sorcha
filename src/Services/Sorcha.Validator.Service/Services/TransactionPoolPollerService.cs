// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Services.Interfaces;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Background service that polls unverified transactions from Redis
/// and feeds them into the validation pipeline.
/// </summary>
public class TransactionPoolPollerService : BackgroundService
{
    private readonly ITransactionPoolPoller _poller;
    private readonly IMemPoolManager _memPool;
    private readonly TransactionPoolPollerConfiguration _config;
    private readonly ILogger<TransactionPoolPollerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // Active registers being polled
    private readonly HashSet<string> _activeRegisters = [];
    private readonly object _registerLock = new();

    public TransactionPoolPollerService(
        ITransactionPoolPoller poller,
        IMemPoolManager memPool,
        IOptions<TransactionPoolPollerConfiguration> config,
        ILogger<TransactionPoolPollerService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _poller = poller ?? throw new ArgumentNullException(nameof(poller));
        _memPool = memPool ?? throw new ArgumentNullException(nameof(memPool));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <summary>
    /// Register a register ID to start polling transactions for
    /// </summary>
    public void RegisterForPolling(string registerId)
    {
        lock (_registerLock)
        {
            if (_activeRegisters.Add(registerId))
            {
                _logger.LogInformation("Started polling transactions for register {RegisterId}", registerId);
            }
        }
    }

    /// <summary>
    /// Unregister a register ID to stop polling transactions for
    /// </summary>
    public void UnregisterFromPolling(string registerId)
    {
        lock (_registerLock)
        {
            if (_activeRegisters.Remove(registerId))
            {
                _logger.LogInformation("Stopped polling transactions for register {RegisterId}", registerId);
            }
        }
    }

    /// <summary>
    /// Get list of currently active registers
    /// </summary>
    public IReadOnlyList<string> GetActiveRegisters()
    {
        lock (_registerLock)
        {
            return _activeRegisters.ToList();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Transaction pool poller is disabled");
            return;
        }

        _logger.LogInformation(
            "Transaction pool poller started with batch size {BatchSize} and interval {Interval}ms",
            _config.BatchSize, _config.PollingInterval.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAllRegistersAsync(stoppingToken);
                await Task.Delay(_config.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction pool poller loop");
                await Task.Delay(_config.PollingInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Transaction pool poller stopped");
    }

    private async Task PollAllRegistersAsync(CancellationToken ct)
    {
        List<string> registers;
        lock (_registerLock)
        {
            registers = _activeRegisters.ToList();
        }

        if (registers.Count == 0)
            return;

        // Poll each register in parallel
        var tasks = registers.Select(registerId => PollRegisterAsync(registerId, ct));
        await Task.WhenAll(tasks);
    }

    private async Task PollRegisterAsync(string registerId, CancellationToken ct)
    {
        try
        {
            // Poll transactions from Redis
            var transactions = await _poller.PollTransactionsAsync(
                registerId,
                _config.BatchSize,
                ct);

            if (transactions.Count == 0)
                return;

            _logger.LogDebug(
                "Polled {Count} transactions for register {RegisterId}, adding to mempool",
                transactions.Count, registerId);

            // Add to in-memory mempool for validation
            // In the full implementation, we would validate here before adding
            // For now, we just add to the mempool
            var added = 0;
            foreach (var transaction in transactions)
            {
                if (await _memPool.AddTransactionAsync(registerId, transaction, ct))
                {
                    added++;
                }
            }

            if (added > 0)
            {
                _logger.LogInformation(
                    "Added {Added}/{Total} polled transactions to mempool for register {RegisterId}",
                    added, transactions.Count, registerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to poll register {RegisterId}", registerId);
        }
    }
}
