// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service;

/// <summary>
/// Background service that manages peer-to-peer networking functionality
/// </summary>
public class PeerService : BackgroundService
{
    private readonly ILogger<PeerService> _logger;
    private readonly PeerServiceConfiguration _configuration;
    private PeerServiceStatus _status = PeerServiceStatus.Offline;
    private string _nodeId = string.Empty;
    private readonly object _statusLock = new();
    private readonly CancellationTokenSource _internalCts = new();
    private Task? _discoveryTask;
    private Task? _healthCheckTask;
    private Task? _transactionProcessingTask;

    /// <summary>
    /// Gets the current operational status of the peer service
    /// </summary>
    public PeerServiceStatus Status
    {
        get
        {
            lock (_statusLock)
            {
                return _status;
            }
        }
        private set
        {
            lock (_statusLock)
            {
                if (_status != value)
                {
                    var oldStatus = _status;
                    _status = value;
                    _logger.LogInformation("Peer service status changed: {OldStatus} -> {NewStatus}", oldStatus, value);
                }
            }
        }
    }

    /// <summary>
    /// Gets the unique identifier for this node
    /// </summary>
    public string NodeId => _nodeId;

    public PeerService(
        ILogger<PeerService> logger,
        IOptions<PeerServiceConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Initializes the peer service when starting
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.Enabled)
        {
            _logger.LogInformation("Peer service is disabled in configuration");
            return;
        }

        try
        {
            await InitializeAsync(stoppingToken);
            await RunServiceLoopAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Peer service is shutting down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in peer service");
            throw;
        }
        finally
        {
            await CleanupAsync();
        }
    }

    /// <summary>
    /// Initializes the service components
    /// </summary>
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing peer service");

        // Generate or load node ID
        _nodeId = _configuration.NodeId ?? GenerateNodeId();
        _logger.LogInformation("Node ID: {NodeId}", _nodeId);

        // Initialize components (placeholders for future sprints)
        await Task.CompletedTask;

        Status = PeerServiceStatus.Offline;
        _logger.LogInformation("Peer service initialized successfully");
    }

    /// <summary>
    /// Main service loop
    /// </summary>
    private async Task RunServiceLoopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting peer service main loop");

        // Link external and internal cancellation tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _internalCts.Token);
        var linkedToken = linkedCts.Token;

        // Start background tasks (placeholders for future sprints)
        _discoveryTask = RunPeerDiscoveryAsync(linkedToken);
        _healthCheckTask = RunHealthChecksAsync(linkedToken);
        _transactionProcessingTask = RunTransactionProcessingAsync(linkedToken);

        // Wait for cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, linkedToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Service loop cancelled");
        }
    }

    /// <summary>
    /// Runs peer discovery loop (placeholder for Sprint 2)
    /// </summary>
    private async Task RunPeerDiscoveryAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Peer discovery task started (placeholder)");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Sprint 2 - Implement peer discovery
                await Task.Delay(TimeSpan.FromMinutes(_configuration.PeerDiscovery.RefreshIntervalMinutes), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in peer discovery loop");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        _logger.LogDebug("Peer discovery task stopped");
    }

    /// <summary>
    /// Runs health check loop (placeholder for Sprint 2)
    /// </summary>
    private async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Health check task started (placeholder)");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Sprint 2 - Implement health checks
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check loop");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        _logger.LogDebug("Health check task stopped");
    }

    /// <summary>
    /// Runs transaction processing loop (placeholder for Sprint 4)
    /// </summary>
    private async Task RunTransactionProcessingAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Transaction processing task started (placeholder)");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Sprint 4 - Implement transaction processing
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction processing loop");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        _logger.LogDebug("Transaction processing task stopped");
    }

    /// <summary>
    /// Cleans up resources when stopping
    /// </summary>
    private async Task CleanupAsync()
    {
        _logger.LogInformation("Cleaning up peer service");

        Status = PeerServiceStatus.Offline;

        // Wait for background tasks to complete
        var tasks = new List<Task>();
        if (_discoveryTask != null) tasks.Add(_discoveryTask);
        if (_healthCheckTask != null) tasks.Add(_healthCheckTask);
        if (_transactionProcessingTask != null) tasks.Add(_transactionProcessingTask);

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        _logger.LogInformation("Peer service cleanup complete");
    }

    /// <summary>
    /// Generates a unique node ID
    /// </summary>
    private static string GenerateNodeId()
    {
        return $"node_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Stops the service gracefully
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping peer service");
        _internalCts.Cancel();
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public override void Dispose()
    {
        _internalCts?.Cancel();
        _internalCts?.Dispose();
        base.Dispose();
    }
}
