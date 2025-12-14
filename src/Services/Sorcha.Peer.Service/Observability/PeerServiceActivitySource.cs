// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Diagnostics;

namespace Sorcha.Peer.Service.Observability;

/// <summary>
/// OpenTelemetry activity source for distributed tracing in Peer Service
/// </summary>
/// <remarks>
/// Provides comprehensive tracing for:
/// - Connection lifecycle (connect, disconnect, failover)
/// - System register synchronization (full sync, incremental sync)
/// - Heartbeat operations
/// - Push notification delivery
/// </remarks>
public sealed class PeerServiceActivitySource : IDisposable
{
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Activity source name for Peer Service
    /// </summary>
    public const string ActivitySourceName = "Sorcha.Peer.Service";

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerServiceActivitySource"/> class
    /// </summary>
    public PeerServiceActivitySource()
    {
        _activitySource = new ActivitySource(ActivitySourceName, "1.0.0");
    }

    /// <summary>
    /// Starts a connection activity for tracing connection lifecycle
    /// </summary>
    /// <param name="centralNodeId">Central node identifier</param>
    /// <param name="priority">Connection priority</param>
    /// <returns>Activity instance or null if not sampled</returns>
    public Activity? StartConnectionActivity(string centralNodeId, int priority)
    {
        var activity = _activitySource.StartActivity(
            name: "peer.connection.connect",
            kind: ActivityKind.Client);

        activity?.SetTag("central_node_id", centralNodeId);
        activity?.SetTag("priority", priority);
        activity?.SetTag("peer.service", "connection");

        return activity;
    }

    /// <summary>
    /// Starts a failover activity for tracing failover operations
    /// </summary>
    /// <param name="fromNode">Source central node</param>
    /// <param name="toNode">Target central node</param>
    /// <param name="reason">Failover reason</param>
    /// <returns>Activity instance or null if not sampled</returns>
    public Activity? StartFailoverActivity(string fromNode, string toNode, string reason)
    {
        var activity = _activitySource.StartActivity(
            name: "peer.connection.failover",
            kind: ActivityKind.Client);

        activity?.SetTag("from_node", fromNode);
        activity?.SetTag("to_node", toNode);
        activity?.SetTag("reason", reason);
        activity?.SetTag("peer.service", "failover");

        return activity;
    }

    /// <summary>
    /// Starts a full sync activity for tracing full synchronization
    /// </summary>
    /// <param name="peerId">Peer identifier</param>
    /// <param name="centralNodeId">Central node identifier</param>
    /// <returns>Activity instance or null if not sampled</returns>
    public Activity? StartFullSyncActivity(string peerId, string centralNodeId)
    {
        var activity = _activitySource.StartActivity(
            name: "peer.sync.full",
            kind: ActivityKind.Client);

        activity?.SetTag("peer_id", peerId);
        activity?.SetTag("central_node_id", centralNodeId);
        activity?.SetTag("sync_type", "full");
        activity?.SetTag("peer.service", "sync");

        return activity;
    }

    /// <summary>
    /// Starts an incremental sync activity for tracing incremental synchronization
    /// </summary>
    /// <param name="peerId">Peer identifier</param>
    /// <param name="centralNodeId">Central node identifier</param>
    /// <param name="lastKnownVersion">Last synchronized version</param>
    /// <returns>Activity instance or null if not sampled</returns>
    public Activity? StartIncrementalSyncActivity(string peerId, string centralNodeId, long lastKnownVersion)
    {
        var activity = _activitySource.StartActivity(
            name: "peer.sync.incremental",
            kind: ActivityKind.Client);

        activity?.SetTag("peer_id", peerId);
        activity?.SetTag("central_node_id", centralNodeId);
        activity?.SetTag("sync_type", "incremental");
        activity?.SetTag("last_known_version", lastKnownVersion);
        activity?.SetTag("peer.service", "sync");

        return activity;
    }

    /// <summary>
    /// Starts a heartbeat activity for tracing heartbeat send/acknowledge
    /// </summary>
    /// <param name="centralNodeId">Central node identifier</param>
    /// <param name="sequenceNumber">Heartbeat sequence number</param>
    /// <returns>Activity instance or null if not sampled</returns>
    public Activity? StartHeartbeatActivity(string centralNodeId, long sequenceNumber)
    {
        var activity = _activitySource.StartActivity(
            name: "peer.heartbeat.send",
            kind: ActivityKind.Client);

        activity?.SetTag("central_node_id", centralNodeId);
        activity?.SetTag("sequence_number", sequenceNumber);
        activity?.SetTag("peer.service", "heartbeat");

        return activity;
    }

    /// <summary>
    /// Starts a push notification receive activity
    /// </summary>
    /// <param name="blueprintId">Blueprint identifier</param>
    /// <param name="version">System register version</param>
    /// <returns>Activity instance or null if not sampled</returns>
    public Activity? StartPushNotificationActivity(string blueprintId, long version)
    {
        var activity = _activitySource.StartActivity(
            name: "peer.notification.receive",
            kind: ActivityKind.Server);

        activity?.SetTag("blueprint_id", blueprintId);
        activity?.SetTag("version", version);
        activity?.SetTag("peer.service", "notification");

        return activity;
    }

    /// <summary>
    /// Records a successful operation outcome on the activity
    /// </summary>
    /// <param name="activity">Activity to update</param>
    /// <param name="duration">Operation duration</param>
    /// <param name="blueprintCount">Number of blueprints processed (optional)</param>
    public void RecordSuccess(Activity? activity, TimeSpan duration, long? blueprintCount = null)
    {
        if (activity == null) return;

        activity.SetTag("success", true);
        activity.SetTag("duration_ms", duration.TotalMilliseconds);

        if (blueprintCount.HasValue)
        {
            activity.SetTag("blueprint_count", blueprintCount.Value);
        }

        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a failed operation outcome on the activity
    /// </summary>
    /// <param name="activity">Activity to update</param>
    /// <param name="exception">Exception that caused the failure</param>
    /// <param name="duration">Operation duration</param>
    public void RecordFailure(Activity? activity, Exception exception, TimeSpan duration)
    {
        if (activity == null) return;

        activity.SetTag("success", false);
        activity.SetTag("duration_ms", duration.TotalMilliseconds);
        activity.SetTag("error.type", exception.GetType().FullName);
        activity.SetTag("error.message", exception.Message);
        activity.SetTag("exception.stacktrace", exception.StackTrace);

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }

    /// <summary>
    /// Disposes the activity source
    /// </summary>
    public void Dispose()
    {
        _activitySource?.Dispose();
    }
}
