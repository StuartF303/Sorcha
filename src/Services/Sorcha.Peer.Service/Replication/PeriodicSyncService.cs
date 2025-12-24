// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Sorcha.Peer.Service.Connection;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Protos;
using ProtoSyncCheckpoint = Sorcha.Peer.Service.Protos.SyncCheckpoint;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Background service that performs periodic synchronization of the system register every 5 minutes
/// </summary>
/// <remarks>
/// This service runs only on peer nodes (not hub nodes).
/// It maintains a sync checkpoint and triggers incremental sync at regular intervals
/// to ensure the local system register replica stays up to date with the hub nodes.
///
/// Sync strategy:
/// - Interval: 5 minutes (configurable via PeerServiceConstants.PeriodicSyncIntervalMinutes)
/// - Method: Incremental sync (only new blueprints since last checkpoint)
/// - Fallback: On sync failure, retries with exponential backoff
/// - Offline mode: Continues running but skips sync when disconnected
/// </remarks>
public class PeriodicSyncService : BackgroundService
{
    private readonly ILogger<PeriodicSyncService> _logger;
    private readonly HubNodeDiscoveryService _centralNodeDiscoveryService;
    private readonly HubNodeConnectionManager _connectionManager;
    private readonly SystemRegisterCache _cache;
    private Core.SyncCheckpoint _checkpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeriodicSyncService"/> class
    /// </summary>
    public PeriodicSyncService(
        ILogger<PeriodicSyncService> logger,
        HubNodeDiscoveryService centralNodeDiscoveryService,
        HubNodeConnectionManager connectionManager,
        SystemRegisterCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _centralNodeDiscoveryService = centralNodeDiscoveryService ?? throw new ArgumentNullException(nameof(centralNodeDiscoveryService));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        // Initialize checkpoint
        _checkpoint = new Core.SyncCheckpoint
        {
            PeerId = "unknown", // Will be set on first sync
            CurrentVersion = 0,
            LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            TotalBlueprints = 0,
            HubNodeId = string.Empty,
            NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes)
        };
    }

    /// <summary>
    /// Executes the periodic sync service
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Don't run on hub nodes
        if (_centralNodeDiscoveryService.IsHubNode())
        {
            _logger.LogInformation("Periodic sync service disabled - running as hub node");
            return;
        }

        _logger.LogInformation("Periodic sync service starting (interval: {Interval} minutes)",
            PeerServiceConstants.PeriodicSyncIntervalMinutes);

        // Wait for initial startup
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for next interval
                await timer.WaitForNextTickAsync(stoppingToken);

                // Check if sync is due
                if (DateTime.UtcNow < _checkpoint.NextSyncDue)
                {
                    _logger.LogDebug("Skipping sync - not yet due (next: {NextDue})",
                        _checkpoint.NextSyncDue);
                    continue;
                }

                // Perform incremental sync
                await PerformIncrementalSyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Periodic sync service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in periodic sync loop - will retry on next interval");
            }
        }

        _logger.LogInformation("Periodic sync service stopped");
    }

    /// <summary>
    /// Performs incremental synchronization with the active hub node
    /// </summary>
    private async Task PerformIncrementalSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting periodic incremental sync (last version: {Version})",
                _checkpoint.CurrentVersion);

            // Get active hub node connection
            var activeNode = _connectionManager.GetActiveHubNode();
            var channel = _connectionManager.GetActiveChannel();

            if (activeNode == null || channel == null)
            {
                _logger.LogWarning("Cannot perform sync - no active hub node connection");

                // Handle isolated mode
                await _connectionManager.HandleIsolatedModeAsync();

                // Update next sync time even if failed
                _checkpoint.NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes);
                return;
            }

            // Create gRPC client
            var client = new SystemRegisterSync.SystemRegisterSyncClient(channel);

            // Build sync request
            var request = new SyncRequest
            {
                PeerId = _checkpoint.PeerId,
                LastKnownVersion = _checkpoint.CurrentVersion,
                FullSync = false,
                SessionId = string.Empty, // TODO: Get from connection manager
                MaxBlueprints = 0, // Unlimited
                RequestTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            _logger.LogDebug("Requesting incremental sync from hub node {NodeId} (session: {SessionId})",
                activeNode.NodeId, request.SessionId);

            // Call IncrementalSync RPC (server streaming)
            using var call = client.IncrementalSync(request, cancellationToken: cancellationToken);

            int syncedCount = 0;
            long maxVersion = _checkpoint.CurrentVersion;

            // Process streamed blueprints
            await foreach (var entry in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                // Convert protobuf entry to cached blueprint
                var cachedBlueprint = new CachedBlueprint
                {
                    BlueprintId = entry.BlueprintId,
                    Version = entry.Version,
                    Document = entry.BlueprintDocument.ToByteArray(),
                    PublishedAt = DateTimeOffset.FromUnixTimeMilliseconds(entry.PublishedAt).UtcDateTime,
                    PublishedBy = entry.PublishedBy,
                    IsActive = entry.IsActive,
                    Metadata = entry.Metadata.Count > 0 ? entry.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value) : null,
                    Checksum = string.IsNullOrEmpty(entry.Checksum) ? null : entry.Checksum
                };

                // Add to cache
                await _cache.AddOrUpdateBlueprintAsync(cachedBlueprint, cancellationToken);

                syncedCount++;
                maxVersion = Math.Max(maxVersion, entry.Version);

                if (syncedCount % 100 == 0)
                {
                    _logger.LogDebug("Synced {Count} blueprints (max version: {Version})", syncedCount, maxVersion);
                }
            }

            // Update checkpoint
            _checkpoint.CurrentVersion = maxVersion;
            _checkpoint.LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _checkpoint.TotalBlueprints = _cache.GetBlueprintCount();
            _checkpoint.HubNodeId = activeNode.NodeId;
            _checkpoint.NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes);

            _logger.LogInformation("Periodic sync completed successfully. Synced: {Count}, Version: {Version}, Total blueprints: {Total}",
                syncedCount, maxVersion, _checkpoint.TotalBlueprints);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogWarning("Hub node unavailable during sync - entering isolated mode");

            // Handle isolated mode when hub node unreachable
            await _connectionManager.HandleIsolatedModeAsync();

            // Update next sync time
            _checkpoint.NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform periodic incremental sync");

            // Update next sync time even on failure
            _checkpoint.NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes);

            // Don't throw - allow service to continue running and retry on next interval
        }
    }

    /// <summary>
    /// Gets the current sync checkpoint
    /// </summary>
    public Core.SyncCheckpoint GetCheckpoint()
    {
        return _checkpoint;
    }

    /// <summary>
    /// Updates the sync checkpoint (used after manual sync)
    /// </summary>
    public void UpdateCheckpoint(Core.SyncCheckpoint checkpoint)
    {
        _checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        _logger.LogInformation("Sync checkpoint updated: Version {Version}, Blueprints {Count}",
            checkpoint.CurrentVersion, checkpoint.TotalBlueprints);
    }
}
