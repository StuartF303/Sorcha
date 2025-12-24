// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Connection;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Monitoring;
using Sorcha.Peer.Service.Network;

namespace Sorcha.Peer.Service;

/// <summary>
/// Background service that manages peer-to-peer networking functionality
/// </summary>
public class PeerService : BackgroundService
{
    private readonly ILogger<PeerService> _logger;
    private readonly PeerServiceConfiguration _configuration;
    private readonly PeerListManager _peerListManager;
    private readonly NetworkAddressService _networkAddressService;
    private readonly PeerDiscoveryService _peerDiscoveryService;
    private readonly HealthMonitorService _healthMonitorService;
    private readonly HubNodeDiscoveryService _centralNodeDiscoveryService;
    private readonly HubNodeConnectionManager _centralNodeConnectionManager;
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
        IOptions<PeerServiceConfiguration> configuration,
        PeerListManager peerListManager,
        NetworkAddressService networkAddressService,
        PeerDiscoveryService peerDiscoveryService,
        HealthMonitorService healthMonitorService,
        HubNodeDiscoveryService centralNodeDiscoveryService,
        HubNodeConnectionManager centralNodeConnectionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
        _networkAddressService = networkAddressService ?? throw new ArgumentNullException(nameof(networkAddressService));
        _peerDiscoveryService = peerDiscoveryService ?? throw new ArgumentNullException(nameof(peerDiscoveryService));
        _healthMonitorService = healthMonitorService ?? throw new ArgumentNullException(nameof(healthMonitorService));
        _centralNodeDiscoveryService = centralNodeDiscoveryService ?? throw new ArgumentNullException(nameof(centralNodeDiscoveryService));
        _centralNodeConnectionManager = centralNodeConnectionManager ?? throw new ArgumentNullException(nameof(centralNodeConnectionManager));
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

        // Detect if this is a hub node or peer node
        var isHubNode = _centralNodeDiscoveryService.IsHubNode();
        _logger.LogInformation("Node type detected: {NodeType}", isHubNode ? "Central Node" : "Peer Node");

        if (isHubNode)
        {
            // Hub node initialization
            _logger.LogInformation("Running as hub node - ready to accept peer connections");
            _logger.LogInformation("Hostname: {Hostname}", _centralNodeDiscoveryService.GetHostname());
            // HeartbeatMonitorService will NOT start for hub nodes (it's a peer-side service)
        }
        else
        {
            // Peer node initialization
            _logger.LogInformation("Running as peer node - will connect to hub nodes");

            // Detect external address
            var externalAddress = await _networkAddressService.GetExternalAddressAsync(cancellationToken);
            _logger.LogInformation("External address: {Address}", externalAddress ?? "unknown");

            // Load peers from database
            await _peerListManager.LoadPeersFromDatabaseAsync(cancellationToken);
            _logger.LogInformation("Loaded {Count} peers from database", _peerListManager.GetAllPeers().Count);

            // Perform initial peer discovery
            if (_configuration.PeerDiscovery.BootstrapNodes.Count > 0)
            {
                _logger.LogInformation("Performing initial peer discovery");
                var discoveredCount = await _peerDiscoveryService.DiscoverPeersAsync(cancellationToken);
                _logger.LogInformation("Discovered {Count} peers", discoveredCount);
            }

            // Connect to hub node (priority order: n0 -> n1 -> n2)
            _logger.LogInformation("Attempting to connect to hub node infrastructure");
            var connected = await _centralNodeConnectionManager.ConnectToHubNodeAsync(cancellationToken);

            if (connected)
            {
                var activeNode = _centralNodeConnectionManager.GetActiveHubNode();
                _logger.LogInformation("Successfully connected to hub node {NodeId}", activeNode?.NodeId);
            }
            else
            {
                _logger.LogWarning("Failed to connect to any hub node - operating in isolated mode");
                _logger.LogWarning("Will continue attempting to connect in background");
            }
        }

        // Determine initial status
        Status = _healthMonitorService.DetermineServiceStatus();
        _logger.LogInformation("Peer service initialized successfully with status: {Status}", Status);
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
    /// Runs peer discovery loop
    /// </summary>
    private async Task RunPeerDiscoveryAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Peer discovery task started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the configured interval
                await Task.Delay(TimeSpan.FromMinutes(_configuration.PeerDiscovery.RefreshIntervalMinutes), cancellationToken);

                // Perform peer discovery
                _logger.LogDebug("Running periodic peer discovery");
                var discoveredCount = await _peerDiscoveryService.DiscoverPeersAsync(cancellationToken);
                _logger.LogInformation("Discovered {Count} new peers", discoveredCount);

                // Update service status based on peer count
                var previousStatus = Status;
                Status = _healthMonitorService.DetermineServiceStatus();

                if (previousStatus != Status)
                {
                    _logger.LogInformation("Service status changed from {Old} to {New}", previousStatus, Status);
                }
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
    /// Runs health check loop
    /// </summary>
    private async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Health check task started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the health check interval
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                // Perform health check
                _logger.LogDebug("Running health check");
                var result = await _healthMonitorService.PerformHealthCheckAsync(cancellationToken);

                _logger.LogDebug("Health check: {Alive}/{Checked} alive, {Healthy} healthy",
                    result.AlivePeers, result.CheckedPeers, result.HealthyPeers);

                // Update service status based on health check results
                var previousStatus = Status;
                Status = _healthMonitorService.DetermineServiceStatus();

                if (previousStatus != Status)
                {
                    _logger.LogWarning("Service status changed from {Old} to {New} after health check",
                        previousStatus, Status);
                }
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

        // Dispose peer list manager
        _peerListManager?.Dispose();

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
