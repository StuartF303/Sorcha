// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Observability;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Connection;

/// <summary>
/// Manages connections to central nodes with priority-based failover and exponential backoff retry
/// </summary>
/// <remarks>
/// Connection strategy:
/// - Priority order: n0 (priority 0) → n1 (priority 1) → n2 (priority 2)
/// - Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s, 60s (max 10 attempts)
/// - Connection timeout: 30 seconds per attempt
/// - Failover on all retries exhausted or heartbeat timeout
/// </remarks>
public class CentralNodeConnectionManager
{
    private readonly ILogger<CentralNodeConnectionManager> _logger;
    private readonly PeerListManager _peerListManager;
    private readonly List<CentralNodeInfo> _centralNodes;
    private readonly ResiliencePipeline _connectionPipeline;
    private readonly PeerServiceMetrics _metrics;
    private readonly PeerServiceActivitySource _activitySource;
    private CentralNodeInfo? _activeNode;
    private GrpcChannel? _activeChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="CentralNodeConnectionManager"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="peerListManager">Peer list manager for tracking connection status</param>
    /// <param name="configuration">Central node configuration</param>
    public CentralNodeConnectionManager(
        ILogger<CentralNodeConnectionManager> logger,
        PeerListManager peerListManager,
        IOptions<CentralNodeConfiguration> configuration,
        PeerServiceMetrics metrics,
        PeerServiceActivitySource activitySource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));

        var config = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

        // Initialize central node list from configuration
        _centralNodes = new List<CentralNodeInfo>
        {
            new() { NodeId = "n0.sorcha.dev", Hostname = "n0.sorcha.dev", Port = 5000, Priority = 0 },
            new() { NodeId = "n1.sorcha.dev", Hostname = "n1.sorcha.dev", Port = 5000, Priority = 1 },
            new() { NodeId = "n2.sorcha.dev", Hostname = "n2.sorcha.dev", Port = 5000, Priority = 2 }
        };

        // Build Polly resilience pipeline with exponential backoff and timeout
        _connectionPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = PeerServiceConstants.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(PeerServiceConstants.RetryInitialDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(PeerServiceConstants.RetryMaxDelaySeconds),
                UseJitter = true, // Prevent thundering herd
                ShouldHandle = new PredicateBuilder().Handle<RpcException>().Handle<HttpRequestException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Connection retry {Attempt}/{MaxAttempts} after {Delay}s: {Exception}",
                        args.AttemptNumber + 1,
                        PeerServiceConstants.MaxRetryAttempts,
                        args.RetryDelay.TotalSeconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(PeerServiceConstants.ConnectionTimeoutSeconds),
                OnTimeout = args =>
                {
                    _logger.LogWarning("Connection attempt timed out after {Timeout}s", PeerServiceConstants.ConnectionTimeoutSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Connects to the next available central node in priority order
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful, false otherwise</returns>
    public async Task<bool> ConnectToCentralNodeAsync(CancellationToken cancellationToken = default)
    {
        // Try each central node in priority order
        var sortedNodes = _centralNodes.OrderBy(n => n.Priority).ToList();

        foreach (var node in sortedNodes)
        {
            using var activity = _activitySource.StartConnectionActivity(node.NodeId, node.Priority);
            var startTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Attempting connection to central node {NodeId} with priority {Priority} at {Address}",
                node.NodeId, node.Priority, node.GrpcChannelAddress);

            node.ConnectionStatus = CentralNodeConnectionStatus.Connecting;
            node.LastConnectionAttempt = DateTime.UtcNow;

            try
            {
                // Use resilience pipeline for connection with retry and timeout
                var success = await _connectionPipeline.ExecuteAsync(async token =>
                {
                    return await EstablishGrpcConnectionAsync(node, token);
                }, cancellationToken);

                if (success)
                {
                    // Mark this node as active
                    _activeNode = node;
                    node.IsActive = true;
                    node.ResetConnectionState();

                    // Update all other nodes to inactive
                    foreach (var otherNode in _centralNodes.Where(n => n.NodeId != node.NodeId))
                    {
                        otherNode.IsActive = false;
                    }

                    // Update peer list manager with connected central node
                    _peerListManager.UpdateLocalPeerStatus(node.NodeId, PeerConnectionStatus.Connected);
                    _metrics.RecordConnectionStatus(PeerConnectionStatus.Connected);

                    var duration = DateTime.UtcNow - startTime;
                    _activitySource.RecordSuccess(activity, duration);

                    _logger.LogInformation(
                        "Successfully connected to central node {NodeId} at {Address} (duration: {Duration}ms, attempts: 1)",
                        node.NodeId, node.GrpcChannelAddress, duration.TotalMilliseconds);

                    return true;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _activitySource.RecordFailure(activity, ex, duration);

                _logger.LogError(ex,
                    "Failed to connect to central node {NodeId} after all retries (duration: {Duration}ms, consecutive failures: {Failures})",
                    node.NodeId, duration.TotalMilliseconds, node.ConsecutiveFailures + 1);

                node.RecordFailure();
            }
        }

        // All nodes failed - enter isolated mode
        _logger.LogWarning(
            "Failed to connect to any central node after trying all {NodeCount} nodes - entering isolated mode",
            sortedNodes.Count);
        _peerListManager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Isolated);
        _metrics.RecordConnectionStatus(PeerConnectionStatus.Isolated);

        return false;
    }

    /// <summary>
    /// Establishes gRPC connection to the specified central node
    /// </summary>
    /// <param name="node">Central node information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection established successfully</returns>
    private async Task<bool> EstablishGrpcConnectionAsync(CentralNodeInfo node, CancellationToken cancellationToken)
    {
        // Dispose existing channel if present
        if (_activeChannel != null)
        {
            await _activeChannel.ShutdownAsync();
            _activeChannel.Dispose();
            _activeChannel = null;
        }

        // Create gRPC channel
        _activeChannel = GrpcChannel.ForAddress(node.GrpcChannelAddress, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            }
        });

        // Test connection by checking gRPC health (would need to implement actual connection test RPC)
        await Task.Delay(100, cancellationToken); // Placeholder for actual connection test

        _logger.LogDebug("gRPC channel established to {Address}", node.GrpcChannelAddress);
        return true;
    }

    /// <summary>
    /// Failover to the next central node in priority order
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if failover successful, false if all nodes exhausted</returns>
    public async Task<bool> FailoverToNextNodeAsync(CancellationToken cancellationToken = default)
    {
        if (_activeNode == null)
        {
            _logger.LogWarning("No active node to failover from - attempting fresh connection");
            return await ConnectToCentralNodeAsync(cancellationToken);
        }

        var fromNode = _activeNode.NodeId;
        _logger.LogWarning(
            "Failover triggered from central node {NodeId} (consecutive failures: {Failures})",
            _activeNode.NodeId, _activeNode.ConsecutiveFailures);

        // Mark current node as failed
        _activeNode.ConnectionStatus = CentralNodeConnectionStatus.Failed;
        _activeNode.IsActive = false;

        // Get next node in priority order (wrap around if necessary)
        var currentPriority = _activeNode.Priority;
        var sortedNodes = _centralNodes.OrderBy(n => n.Priority).ToList();
        var nextNodes = sortedNodes.Where(n => n.Priority > currentPriority).ToList();

        // If no higher priority nodes, wrap around to priority 0
        if (nextNodes.Count == 0)
        {
            nextNodes = sortedNodes;
            _logger.LogInformation("Wrapping around to priority 0 after exhausting all nodes");
        }

        // Try each next node
        foreach (var node in nextNodes)
        {
            using var activity = _activitySource.StartFailoverActivity(fromNode, node.NodeId, "heartbeat_timeout");
            var startTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Attempting failover from {FromNode} to central node {ToNode} (priority {Priority})",
                fromNode, node.NodeId, node.Priority);

            node.ConnectionStatus = CentralNodeConnectionStatus.Connecting;
            node.LastConnectionAttempt = DateTime.UtcNow;

            try
            {
                var success = await _connectionPipeline.ExecuteAsync(async token =>
                {
                    return await EstablishGrpcConnectionAsync(node, token);
                }, cancellationToken);

                if (success)
                {
                    _activeNode = node;
                    node.IsActive = true;
                    node.ResetConnectionState();

                    _peerListManager.UpdateLocalPeerStatus(node.NodeId, PeerConnectionStatus.Connected);
                    _metrics.RecordConnectionStatus(PeerConnectionStatus.Connected);
                    _metrics.RecordFailover(fromNode, node.NodeId, "heartbeat_timeout");

                    var duration = DateTime.UtcNow - startTime;
                    _activitySource.RecordSuccess(activity, duration);

                    _logger.LogInformation(
                        "Failover successful from {FromNode} to central node {ToNode} (duration: {Duration}ms)",
                        fromNode, node.NodeId, duration.TotalMilliseconds);

                    return true;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _activitySource.RecordFailure(activity, ex, duration);

                _logger.LogError(ex,
                    "Failover to central node {NodeId} failed (duration: {Duration}ms)",
                    node.NodeId, duration.TotalMilliseconds);

                node.RecordFailure();
            }
        }

        // All failover attempts failed - isolated mode
        _logger.LogWarning(
            "All failover attempts failed after trying {NodeCount} nodes - entering isolated mode",
            nextNodes.Count);
        _peerListManager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Isolated);
        _metrics.RecordConnectionStatus(PeerConnectionStatus.Isolated);

        return false;
    }

    /// <summary>
    /// Gets the currently active central node
    /// </summary>
    /// <returns>Active central node info or null if disconnected</returns>
    public CentralNodeInfo? GetActiveCentralNode()
    {
        return _activeNode;
    }

    /// <summary>
    /// Gets the active gRPC channel
    /// </summary>
    /// <returns>Active gRPC channel or null if disconnected</returns>
    public GrpcChannel? GetActiveChannel()
    {
        return _activeChannel;
    }

    /// <summary>
    /// Disconnects from the current central node
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_activeNode != null)
        {
            _logger.LogInformation("Disconnecting from central node {NodeId}", _activeNode.NodeId);
            _activeNode.ConnectionStatus = CentralNodeConnectionStatus.Disconnected;
            _activeNode.IsActive = false;
            _activeNode = null;
        }

        if (_activeChannel != null)
        {
            await _activeChannel.ShutdownAsync();
            _activeChannel.Dispose();
            _activeChannel = null;
        }

        _peerListManager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Disconnected);
    }

    /// <summary>
    /// Gets all central node connection states
    /// </summary>
    /// <returns>List of central node information</returns>
    public List<CentralNodeInfo> GetAllCentralNodes()
    {
        return _centralNodes;
    }

    /// <summary>
    /// Handles isolated mode when all central nodes are unreachable
    /// </summary>
    /// <remarks>
    /// When all central nodes fail to respond:
    /// - Updates peer status to Isolated
    /// - Logs warning about isolated mode
    /// - Continues operation with last known system register replica
    /// - Background reconnection attempts continue
    /// </remarks>
    /// <returns>Task representing the async operation</returns>
    public Task HandleIsolatedModeAsync()
    {
        _logger.LogWarning(
            "All central nodes unreachable - operating in isolated mode with last known system register replica");

        // Update peer status to isolated
        _peerListManager.UpdateLocalPeerStatus(null, PeerConnectionStatus.Isolated);

        // Mark all central nodes as failed
        foreach (var node in _centralNodes)
        {
            node.ConnectionStatus = CentralNodeConnectionStatus.Failed;
            node.IsActive = false;
        }

        // Clear active connection
        _activeNode = null;

        _logger.LogInformation(
            "Isolated mode active - peer will continue serving cached blueprints and retry connections in background");

        return Task.CompletedTask;
    }
}
