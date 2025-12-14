// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Observability;
using Sorcha.Register.Service.Repositories;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Service for managing system register replication from central node to peer nodes
/// </summary>
/// <remarks>
/// Provides two replication strategies:
/// 1. FullSyncAsync() - Initial sync of all blueprints (used on first connection)
/// 2. IncrementalSyncAsync() - Sync only new blueprints since last version (periodic sync)
///
/// Updates SystemRegisterCache with synchronized blueprints and manages SyncCheckpoint
/// for tracking replication progress.
/// </remarks>
public class SystemRegisterReplicationService
{
    private readonly ILogger<SystemRegisterReplicationService> _logger;
    private readonly ISystemRegisterRepository _systemRegisterRepository;
    private readonly SystemRegisterCache _cache;
    private readonly CentralNodeDiscoveryService _centralNodeDiscoveryService;
    private readonly PeerServiceMetrics _metrics;
    private readonly PeerServiceActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRegisterReplicationService"/> class
    /// </summary>
    public SystemRegisterReplicationService(
        ILogger<SystemRegisterReplicationService> logger,
        ISystemRegisterRepository systemRegisterRepository,
        SystemRegisterCache cache,
        CentralNodeDiscoveryService centralNodeDiscoveryService,
        PeerServiceMetrics metrics,
        PeerServiceActivitySource activitySource)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _systemRegisterRepository = systemRegisterRepository ?? throw new ArgumentNullException(nameof(systemRegisterRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _centralNodeDiscoveryService = centralNodeDiscoveryService ?? throw new ArgumentNullException(nameof(centralNodeDiscoveryService));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
    }

    /// <summary>
    /// Performs full synchronization of all blueprints from system register
    /// </summary>
    /// <param name="peerId">Peer identifier requesting sync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sync checkpoint after successful sync</returns>
    public async Task<SyncCheckpoint> FullSyncAsync(string peerId, CancellationToken cancellationToken = default)
    {
        var centralNodeId = _centralNodeDiscoveryService.GetHostname();
        using var activity = _activitySource.StartFullSyncActivity(peerId, centralNodeId);
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting full sync for peer {PeerId} from central node {CentralNodeId}",
                peerId, centralNodeId);

            // Get all blueprints from repository
            var blueprints = await _systemRegisterRepository.GetAllBlueprintsAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} blueprints from system register for full sync (peer: {PeerId})",
                blueprints.Count, peerId);

            // Convert to cached blueprints
            var cachedBlueprints = blueprints.Select(b => new CachedBlueprint
            {
                BlueprintId = b.BlueprintId,
                Version = b.Version,
                Document = ConvertBsonDocumentToBytes(b.Document),
                PublishedAt = b.PublishedAt,
                PublishedBy = b.PublishedBy,
                IsActive = b.IsActive,
                Metadata = b.Metadata,
                Checksum = b.Checksum
            }).ToList();

            // Update cache
            await _cache.UpdateCacheAsync(cachedBlueprints, cancellationToken);

            // Get latest version
            var latestVersion = await _systemRegisterRepository.GetLatestVersionAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            // Record metrics
            _metrics.RecordSyncOperation(duration.TotalSeconds, "Full", blueprints.Count);
            _activitySource.RecordSuccess(activity, duration, blueprints.Count);

            _logger.LogInformation(
                "Full sync completed for peer {PeerId}. Blueprints: {Count}, Version: 0 -> {Version}, Duration: {Duration}ms",
                peerId, blueprints.Count, latestVersion, duration.TotalMilliseconds);

            // Create sync checkpoint
            return new SyncCheckpoint
            {
                PeerId = peerId,
                CurrentVersion = latestVersion,
                LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                TotalBlueprints = blueprints.Count,
                CentralNodeId = _centralNodeDiscoveryService.GetHostname(),
                NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes)
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _activitySource.RecordFailure(activity, ex, duration);

            _logger.LogError(ex,
                "Failed to perform full sync for peer {PeerId} after {Duration}ms",
                peerId, duration.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Performs incremental synchronization of blueprints since last known version
    /// </summary>
    /// <param name="peerId">Peer identifier requesting sync</param>
    /// <param name="lastKnownVersion">Last synchronized version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sync checkpoint after successful sync</returns>
    public async Task<SyncCheckpoint> IncrementalSyncAsync(string peerId, long lastKnownVersion, CancellationToken cancellationToken = default)
    {
        var centralNodeId = _centralNodeDiscoveryService.GetHostname();
        using var activity = _activitySource.StartIncrementalSyncActivity(peerId, centralNodeId, lastKnownVersion);
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting incremental sync for peer {PeerId} from version {LastVersion} (central node: {CentralNodeId})",
                peerId, lastKnownVersion, centralNodeId);

            // Get blueprints since last version
            var blueprints = await _systemRegisterRepository.GetBlueprintsSinceVersionAsync(lastKnownVersion, cancellationToken);

            _logger.LogInformation("Retrieved {Count} new blueprints since version {Version} for incremental sync",
                blueprints.Count, lastKnownVersion);

            if (blueprints.Count == 0)
            {
                _logger.LogDebug("No new blueprints to sync for peer {PeerId}", peerId);

                // Still return updated checkpoint
                return new SyncCheckpoint
                {
                    PeerId = peerId,
                    CurrentVersion = lastKnownVersion,
                    LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    TotalBlueprints = _cache.GetBlueprintCount(),
                    CentralNodeId = _centralNodeDiscoveryService.GetHostname(),
                    NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes)
                };
            }

            // Convert and update cache with new blueprints
            foreach (var blueprint in blueprints)
            {
                var cachedBlueprint = new CachedBlueprint
                {
                    BlueprintId = blueprint.BlueprintId,
                    Version = blueprint.Version,
                    Document = ConvertBsonDocumentToBytes(blueprint.Document),
                    PublishedAt = blueprint.PublishedAt,
                    PublishedBy = blueprint.PublishedBy,
                    IsActive = blueprint.IsActive,
                    Metadata = blueprint.Metadata,
                    Checksum = blueprint.Checksum
                };

                await _cache.AddOrUpdateBlueprintAsync(cachedBlueprint, cancellationToken);
            }

            // Get latest version
            var latestVersion = await _systemRegisterRepository.GetLatestVersionAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            // Record metrics
            _metrics.RecordSyncOperation(duration.TotalSeconds, "Incremental", blueprints.Count);
            _activitySource.RecordSuccess(activity, duration, blueprints.Count);

            _logger.LogInformation(
                "Incremental sync completed for peer {PeerId}. New blueprints: {Count}, Version: {OldVersion} -> {NewVersion}, Duration: {Duration}ms",
                peerId, blueprints.Count, lastKnownVersion, latestVersion, duration.TotalMilliseconds);

            // Create sync checkpoint
            return new SyncCheckpoint
            {
                PeerId = peerId,
                CurrentVersion = latestVersion,
                LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                TotalBlueprints = _cache.GetBlueprintCount(),
                CentralNodeId = _centralNodeDiscoveryService.GetHostname(),
                NextSyncDue = DateTime.UtcNow.AddMinutes(PeerServiceConstants.PeriodicSyncIntervalMinutes)
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _activitySource.RecordFailure(activity, ex, duration);

            _logger.LogError(ex,
                "Failed to perform incremental sync for peer {PeerId} from version {LastVersion} after {Duration}ms",
                peerId, lastKnownVersion, duration.TotalMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Converts a BsonDocument to byte array for storage/transmission
    /// </summary>
    private static byte[] ConvertBsonDocumentToBytes(MongoDB.Bson.BsonDocument document)
    {
        using var memoryStream = new System.IO.MemoryStream();
        using (var writer = new BsonBinaryWriter(memoryStream))
        {
            BsonSerializer.Serialize(writer, document);
        }
        return memoryStream.ToArray();
    }
}
