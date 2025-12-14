// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using Sorcha.Peer.Service.Protos;
using System.Collections.Concurrent;

namespace Sorcha.Peer.Service.Replication;

/// <summary>
/// Handles push notification distribution to connected peer nodes when blueprints are published
/// </summary>
/// <remarks>
/// Maintains a list of active subscribers (connected peer nodes) and distributes
/// push notifications when blueprints are published to the system register.
///
/// Notification strategy:
/// - Best-effort delivery (log failures, continue to other subscribers)
/// - Target: 80% of peers within 30 seconds (SC-016)
/// - Automatic subscriber cleanup on stream failure
/// - Thread-safe subscriber management using ConcurrentDictionary
/// </remarks>
public class PushNotificationHandler
{
    private readonly ILogger<PushNotificationHandler> _logger;
    private readonly ConcurrentDictionary<string, SubscriberInfo> _subscribers = new();
    private long _sequenceNumber = 0;

    /// <summary>
    /// Information about a subscribed peer
    /// </summary>
    private class SubscriberInfo
    {
        public required string PeerId { get; init; }
        public required string SessionId { get; init; }
        public required IServerStreamWriter<BlueprintNotification> Stream { get; init; }
        public DateTime SubscribedAt { get; init; } = DateTime.UtcNow;
        public long NotificationsSent { get; set; } = 0;
        public long NotificationsFailed { get; set; } = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PushNotificationHandler"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public PushNotificationHandler(ILogger<PushNotificationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new subscriber for push notifications
    /// </summary>
    /// <param name="peerId">Peer identifier</param>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="stream">gRPC server stream writer</param>
    /// <returns>True if registered successfully, false if already registered</returns>
    public bool RegisterSubscriber(string peerId, string sessionId, IServerStreamWriter<BlueprintNotification> stream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(stream);

        var subscriberKey = $"{peerId}:{sessionId}";
        var subscriberInfo = new SubscriberInfo
        {
            PeerId = peerId,
            SessionId = sessionId,
            Stream = stream
        };

        if (_subscribers.TryAdd(subscriberKey, subscriberInfo))
        {
            _logger.LogInformation("Registered push notification subscriber: {PeerId} (session: {SessionId}), Total subscribers: {Count}",
                peerId, sessionId, _subscribers.Count);
            return true;
        }

        _logger.LogWarning("Subscriber already registered: {PeerId} (session: {SessionId})",
            peerId, sessionId);
        return false;
    }

    /// <summary>
    /// Unregisters a subscriber from push notifications
    /// </summary>
    /// <param name="peerId">Peer identifier</param>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>True if unregistered successfully, false if not found</returns>
    public bool UnregisterSubscriber(string peerId, string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(peerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var subscriberKey = $"{peerId}:{sessionId}";

        if (_subscribers.TryRemove(subscriberKey, out _))
        {
            _logger.LogInformation("Unregistered push notification subscriber: {PeerId} (session: {SessionId}), Remaining subscribers: {Count}",
                peerId, sessionId, _subscribers.Count);
            return true;
        }

        _logger.LogDebug("Subscriber not found for unregistration: {PeerId} (session: {SessionId})",
            peerId, sessionId);
        return false;
    }

    /// <summary>
    /// Notifies all subscribers that a blueprint has been published
    /// </summary>
    /// <param name="blueprintId">Blueprint identifier</param>
    /// <param name="version">System register version</param>
    /// <param name="publishedAt">Publication timestamp</param>
    /// <param name="publishedBy">Publisher identity</param>
    /// <param name="checksum">Blueprint checksum (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public async Task NotifyBlueprintPublishedAsync(
        string blueprintId,
        long version,
        DateTime publishedAt,
        string publishedBy,
        string? checksum = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blueprintId);
        ArgumentException.ThrowIfNullOrWhiteSpace(publishedBy);

        var sequenceNumber = Interlocked.Increment(ref _sequenceNumber);

        var notification = new BlueprintNotification
        {
            BlueprintId = blueprintId,
            Version = version,
            PublishedAt = new DateTimeOffset(publishedAt).ToUnixTimeMilliseconds(),
            PublishedBy = publishedBy,
            Type = NotificationType.BlueprintPublished,
            Checksum = checksum ?? string.Empty,
            SequenceNumber = sequenceNumber
        };

        if (_subscribers.IsEmpty)
        {
            _logger.LogDebug("No subscribers to notify for blueprint {BlueprintId} (version {Version})",
                blueprintId, version);
            return;
        }

        _logger.LogInformation("Notifying {Count} subscribers of blueprint publication: {BlueprintId} (version {Version}, sequence {Sequence})",
            _subscribers.Count, blueprintId, version, sequenceNumber);

        var startTime = DateTime.UtcNow;
        int successCount = 0;
        int failureCount = 0;
        var failedSubscribers = new List<string>();

        // Send notifications to all subscribers in parallel (best-effort)
        var notificationTasks = _subscribers.Select(async kvp =>
        {
            var subscriberKey = kvp.Key;
            var subscriber = kvp.Value;

            try
            {
                // Write notification to stream with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                await subscriber.Stream.WriteAsync(notification, linkedCts.Token);

                subscriber.NotificationsSent++;
                Interlocked.Increment(ref successCount);

                _logger.LogDebug("Sent notification to subscriber {PeerId} (session {SessionId}) for blueprint {BlueprintId}",
                    subscriber.PeerId, subscriber.SessionId, blueprintId);
            }
            catch (Exception ex)
            {
                subscriber.NotificationsFailed++;
                Interlocked.Increment(ref failureCount);
                failedSubscribers.Add(subscriberKey);

                _logger.LogWarning(ex,
                    "Failed to send notification to subscriber {PeerId} (session {SessionId}) for blueprint {BlueprintId}",
                    subscriber.PeerId, subscriber.SessionId, blueprintId);
            }
        });

        await Task.WhenAll(notificationTasks);

        // Clean up failed subscribers
        foreach (var subscriberKey in failedSubscribers)
        {
            if (_subscribers.TryRemove(subscriberKey, out var failedSubscriber))
            {
                _logger.LogInformation("Removed failed subscriber: {PeerId} (session {SessionId})",
                    failedSubscriber.PeerId, failedSubscriber.SessionId);
            }
        }

        var duration = DateTime.UtcNow - startTime;
        var successRate = _subscribers.Count > 0 ? (double)successCount / (_subscribers.Count + failedSubscribers.Count) * 100 : 0;

        _logger.LogInformation(
            "Blueprint notification completed: {BlueprintId} (version {Version}). Success: {Success}/{Total} ({Rate:F1}%), Duration: {Duration}ms",
            blueprintId, version, successCount, successCount + failureCount, successRate, duration.TotalMilliseconds);

        // Log warning if success rate below target (80%)
        if (successRate < 80 && successCount + failureCount > 0)
        {
            _logger.LogWarning(
                "Push notification success rate ({Rate:F1}%) below target (80%) for blueprint {BlueprintId}",
                successRate, blueprintId);
        }
    }

    /// <summary>
    /// Gets the current number of active subscribers
    /// </summary>
    /// <returns>Number of subscribers</returns>
    public int GetSubscriberCount()
    {
        return _subscribers.Count;
    }

    /// <summary>
    /// Gets statistics for all subscribers
    /// </summary>
    /// <returns>Dictionary of subscriber statistics</returns>
    public Dictionary<string, (long Sent, long Failed)> GetSubscriberStatistics()
    {
        return _subscribers.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value.NotificationsSent, kvp.Value.NotificationsFailed));
    }
}
