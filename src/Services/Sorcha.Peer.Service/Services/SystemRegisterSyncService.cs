// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Sorcha.Peer.Service.Protos;
using Sorcha.Peer.Service.Replication;
using Sorcha.Register.Service.Repositories;

namespace Sorcha.Peer.Service.Services;

/// <summary>
/// gRPC service implementation for system register synchronization
/// </summary>
/// <remarks>
/// Implements the SystemRegisterSync service defined in SystemRegisterSync.proto.
/// Runs on central nodes and provides streaming RPCs for peer nodes to synchronize
/// the system register (published blueprints).
///
/// Synchronization strategies:
/// - FullSync: Streams all active blueprints (initial sync on first connection)
/// - IncrementalSync: Streams only blueprints since last known version (periodic sync every 5 minutes)
/// - SubscribeToPushNotifications: Long-lived stream for real-time blueprint publication events
/// </remarks>
public class SystemRegisterSyncService : SystemRegisterSync.SystemRegisterSyncBase
{
    private readonly ILogger<SystemRegisterSyncService> _logger;
    private readonly ISystemRegisterRepository _systemRegisterRepository;
    private readonly PushNotificationHandler _pushNotificationHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRegisterSyncService"/> class
    /// </summary>
    public SystemRegisterSyncService(
        ILogger<SystemRegisterSyncService> logger,
        ISystemRegisterRepository systemRegisterRepository,
        PushNotificationHandler pushNotificationHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _systemRegisterRepository = systemRegisterRepository ?? throw new ArgumentNullException(nameof(systemRegisterRepository));
        _pushNotificationHandler = pushNotificationHandler ?? throw new ArgumentNullException(nameof(pushNotificationHandler));
    }

    /// <summary>
    /// Streams all active blueprints to peer for full synchronization
    /// </summary>
    public override async Task FullSync(SyncRequest request, IServerStreamWriter<Protos.SystemRegisterEntry> responseStream, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Full sync requested by peer {PeerId} (session: {SessionId})",
                request.PeerId, request.SessionId);

            var startTime = DateTime.UtcNow;

            // Get all active blueprints from repository
            var blueprints = await _systemRegisterRepository.GetAllBlueprintsAsync(context.CancellationToken);

            _logger.LogInformation("Streaming {Count} blueprints to peer {PeerId}", blueprints.Count, request.PeerId);

            // Apply max blueprints limit if specified
            var blueprintsToStream = request.MaxBlueprints > 0
                ? blueprints.Take(request.MaxBlueprints).ToList()
                : blueprints;

            // Stream each blueprint
            int streamedCount = 0;
            foreach (var blueprint in blueprintsToStream)
            {
                // Check for cancellation
                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Full sync cancelled for peer {PeerId} after streaming {Count} blueprints",
                        request.PeerId, streamedCount);
                    break;
                }

                // Convert to protobuf message
                var entry = new Protos.SystemRegisterEntry
                {
                    BlueprintId = blueprint.BlueprintId,
                    RegisterId = blueprint.RegisterId.ToString(),
                    BlueprintDocument = Google.Protobuf.ByteString.CopyFrom(ConvertBsonDocumentToBytes(blueprint.Document)),
                    PublishedAt = new DateTimeOffset(blueprint.PublishedAt).ToUnixTimeMilliseconds(),
                    PublishedBy = blueprint.PublishedBy,
                    Version = blueprint.Version,
                    IsActive = blueprint.IsActive,
                    PublicationTransactionId = blueprint.PublicationTransactionId ?? string.Empty,
                    Checksum = blueprint.Checksum ?? string.Empty
                };

                // Add metadata
                if (blueprint.Metadata != null)
                {
                    entry.Metadata.Add(blueprint.Metadata);
                }

                // Stream to peer
                await responseStream.WriteAsync(entry, context.CancellationToken);
                streamedCount++;

                if (streamedCount % 100 == 0)
                {
                    _logger.LogDebug("Streamed {Count}/{Total} blueprints to peer {PeerId}",
                        streamedCount, blueprintsToStream.Count, request.PeerId);
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Full sync completed for peer {PeerId}. Streamed {Count} blueprints in {Duration}ms",
                request.PeerId, streamedCount, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full sync for peer {PeerId}", request.PeerId);
            throw new RpcException(new Status(StatusCode.Internal, $"Full sync failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Streams blueprints published since peer's last known version (incremental sync)
    /// </summary>
    public override async Task IncrementalSync(SyncRequest request, IServerStreamWriter<Protos.SystemRegisterEntry> responseStream, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Incremental sync requested by peer {PeerId} from version {Version} (session: {SessionId})",
                request.PeerId, request.LastKnownVersion, request.SessionId);

            var startTime = DateTime.UtcNow;

            // Get blueprints since last known version
            var blueprints = await _systemRegisterRepository.GetBlueprintsSinceVersionAsync(
                request.LastKnownVersion,
                context.CancellationToken);

            if (blueprints.Count == 0)
            {
                _logger.LogInformation("No new blueprints for peer {PeerId} since version {Version}",
                    request.PeerId, request.LastKnownVersion);
                return;
            }

            _logger.LogInformation("Streaming {Count} new blueprints to peer {PeerId}", blueprints.Count, request.PeerId);

            // Apply max blueprints limit if specified
            var blueprintsToStream = request.MaxBlueprints > 0
                ? blueprints.Take(request.MaxBlueprints).ToList()
                : blueprints;

            // Stream each blueprint
            int streamedCount = 0;
            foreach (var blueprint in blueprintsToStream)
            {
                // Check for cancellation
                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Incremental sync cancelled for peer {PeerId} after streaming {Count} blueprints",
                        request.PeerId, streamedCount);
                    break;
                }

                // Convert to protobuf message
                var entry = new Protos.SystemRegisterEntry
                {
                    BlueprintId = blueprint.BlueprintId,
                    RegisterId = blueprint.RegisterId.ToString(),
                    BlueprintDocument = Google.Protobuf.ByteString.CopyFrom(ConvertBsonDocumentToBytes(blueprint.Document)),
                    PublishedAt = new DateTimeOffset(blueprint.PublishedAt).ToUnixTimeMilliseconds(),
                    PublishedBy = blueprint.PublishedBy,
                    Version = blueprint.Version,
                    IsActive = blueprint.IsActive,
                    PublicationTransactionId = blueprint.PublicationTransactionId ?? string.Empty,
                    Checksum = blueprint.Checksum ?? string.Empty
                };

                // Add metadata
                if (blueprint.Metadata != null)
                {
                    entry.Metadata.Add(blueprint.Metadata);
                }

                // Stream to peer
                await responseStream.WriteAsync(entry, context.CancellationToken);
                streamedCount++;
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Incremental sync completed for peer {PeerId}. Streamed {Count} blueprints in {Duration}ms",
                request.PeerId, streamedCount, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during incremental sync for peer {PeerId}", request.PeerId);
            throw new RpcException(new Status(StatusCode.Internal, $"Incremental sync failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Subscribes peer to push notifications for blueprint publications
    /// </summary>
    /// <remarks>
    /// This is a long-lived server streaming RPC. When blueprints are published,
    /// notifications are pushed to all subscribed peers via the PushNotificationHandler.
    /// Peer nodes maintain this subscription stream and trigger incremental sync when notifications arrive.
    /// </remarks>
    public override async Task SubscribeToPushNotifications(SubscriptionRequest request, IServerStreamWriter<BlueprintNotification> responseStream, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Push notification subscription requested by peer {PeerId} (session: {SessionId})",
                request.PeerId, request.SessionId);

            // Register subscriber with push notification handler
            var registered = _pushNotificationHandler.RegisterSubscriber(
                request.PeerId,
                request.SessionId,
                responseStream);

            if (!registered)
            {
                _logger.LogWarning("Failed to register subscriber {PeerId} (session: {SessionId}) - already registered",
                    request.PeerId, request.SessionId);
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Subscriber already registered"));
            }

            try
            {
                // Keep stream alive until cancellation
                // Notifications are pushed by PushNotificationHandler when blueprints are published
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    // Heartbeat to keep stream alive (every 30 seconds)
                    await Task.Delay(TimeSpan.FromSeconds(30), context.CancellationToken);

                    _logger.LogDebug("Push notification subscription active for peer {PeerId}", request.PeerId);
                }

                _logger.LogInformation("Push notification subscription closed for peer {PeerId}", request.PeerId);
            }
            finally
            {
                // Unregister subscriber on stream completion
                _pushNotificationHandler.UnregisterSubscriber(request.PeerId, request.SessionId);
                _logger.LogInformation("Unregistered push notification subscriber: {PeerId} (session: {SessionId})",
                    request.PeerId, request.SessionId);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Push notification subscription cancelled for peer {PeerId}", request.PeerId);
            // Ensure unregistration
            _pushNotificationHandler.UnregisterSubscriber(request.PeerId, request.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in push notification subscription for peer {PeerId}", request.PeerId);
            // Ensure unregistration on error
            _pushNotificationHandler.UnregisterSubscriber(request.PeerId, request.SessionId);
            throw new RpcException(new Status(StatusCode.Internal, $"Subscription failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Returns sync checkpoint for a peer
    /// </summary>
    public override async Task<Protos.SyncCheckpoint> GetSyncCheckpoint(CheckpointRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Sync checkpoint requested by peer {PeerId}", request.PeerId);

            // Get latest version from repository
            var latestVersion = await _systemRegisterRepository.GetLatestVersionAsync(context.CancellationToken);
            var blueprintCount = await _systemRegisterRepository.GetBlueprintCountAsync(context.CancellationToken);

            return new Protos.SyncCheckpoint
            {
                PeerId = request.PeerId,
                CurrentVersion = latestVersion,
                LastSyncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                TotalBlueprints = blueprintCount,
                CentralNodeId = Environment.MachineName, // TODO: Get from CentralNodeDiscoveryService
                NextSyncDue = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds(),
                Status = CheckpointStatus.UpToDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync checkpoint for peer {PeerId}", request.PeerId);
            throw new RpcException(new Status(StatusCode.Internal, $"Get checkpoint failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Converts a BsonDocument to byte array for transmission
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
