// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Sorcha.Peer.Service.Observability;

/// <summary>
/// OpenTelemetry metrics for Peer Service observability
/// </summary>
/// <remarks>
/// Provides comprehensive metrics for monitoring:
/// - Connection health and status
/// - Heartbeat latency and reliability
/// - Sync performance and throughput
/// - Push notification delivery rates
/// - Failover events
/// </remarks>
public sealed class PeerServiceMetrics : IDisposable
{
    private readonly Meter _meter;

    // Connection metrics
    private readonly ObservableGauge<int> _connectionStatus;
    private int _currentConnectionStatus = 0; // 0=disconnected, 1=connected, 2=isolated

    // Heartbeat metrics
    private readonly Histogram<double> _heartbeatLatency;

    // Sync metrics
    private readonly Histogram<double> _syncDuration;
    private readonly Counter<long> _syncBlueprintCount;

    // Push notification metrics
    private readonly Counter<long> _pushNotificationsDelivered;
    private readonly Counter<long> _pushNotificationsFailed;

    // Failover metrics
    private readonly Counter<long> _failoverCount;

    /// <summary>
    /// Meter name for Peer Service metrics
    /// </summary>
    public const string MeterName = "Sorcha.Peer.Service";

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerServiceMetrics"/> class
    /// </summary>
    public PeerServiceMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        // Connection status gauge (0=disconnected, 1=connected, 2=isolated)
        _connectionStatus = _meter.CreateObservableGauge(
            name: "peer.connection.status",
            observeValue: () => _currentConnectionStatus,
            unit: "status",
            description: "Current connection status (0=disconnected, 1=connected, 2=isolated)");

        // Heartbeat latency histogram (milliseconds)
        _heartbeatLatency = _meter.CreateHistogram<double>(
            name: "peer.heartbeat.latency",
            unit: "ms",
            description: "Heartbeat round-trip time in milliseconds");

        // Sync duration histogram (seconds)
        _syncDuration = _meter.CreateHistogram<double>(
            name: "peer.sync.duration",
            unit: "s",
            description: "System register sync operation duration in seconds");

        // Sync blueprint count counter
        _syncBlueprintCount = _meter.CreateCounter<long>(
            name: "peer.sync.blueprints.count",
            unit: "blueprints",
            description: "Number of blueprints synchronized");

        // Push notification delivery counter
        _pushNotificationsDelivered = _meter.CreateCounter<long>(
            name: "peer.push.notifications.delivered",
            unit: "notifications",
            description: "Number of push notifications successfully delivered");

        // Push notification failure counter
        _pushNotificationsFailed = _meter.CreateCounter<long>(
            name: "peer.push.notifications.failed",
            unit: "notifications",
            description: "Number of failed push notification deliveries");

        // Failover event counter
        _failoverCount = _meter.CreateCounter<long>(
            name: "peer.failover.count",
            unit: "events",
            description: "Number of failover events to different central nodes");
    }

    /// <summary>
    /// Records connection status change
    /// </summary>
    /// <param name="status">Connection status (Disconnected=0, Connected=1, Isolated=2)</param>
    public void RecordConnectionStatus(Core.PeerConnectionStatus status)
    {
        _currentConnectionStatus = status switch
        {
            Core.PeerConnectionStatus.Disconnected => 0,
            Core.PeerConnectionStatus.Connected => 1,
            Core.PeerConnectionStatus.Isolated => 2,
            _ => 0
        };
    }

    /// <summary>
    /// Records heartbeat latency measurement
    /// </summary>
    /// <param name="latencyMs">Latency in milliseconds</param>
    /// <param name="centralNodeId">Central node identifier</param>
    public void RecordHeartbeatLatency(double latencyMs, string centralNodeId)
    {
        var tags = new TagList
        {
            { "central_node_id", centralNodeId }
        };

        _heartbeatLatency.Record(latencyMs, tags);
    }

    /// <summary>
    /// Records sync operation duration
    /// </summary>
    /// <param name="durationSeconds">Duration in seconds</param>
    /// <param name="syncType">Sync type (Full or Incremental)</param>
    /// <param name="blueprintCount">Number of blueprints synced</param>
    public void RecordSyncOperation(double durationSeconds, string syncType, long blueprintCount)
    {
        var tags = new TagList
        {
            { "sync_type", syncType }
        };

        _syncDuration.Record(durationSeconds, tags);
        _syncBlueprintCount.Add(blueprintCount, tags);
    }

    /// <summary>
    /// Records successful push notification delivery
    /// </summary>
    /// <param name="count">Number of successful deliveries</param>
    public void RecordPushNotificationDelivered(long count = 1)
    {
        _pushNotificationsDelivered.Add(count);
    }

    /// <summary>
    /// Records failed push notification delivery
    /// </summary>
    /// <param name="count">Number of failed deliveries</param>
    public void RecordPushNotificationFailed(long count = 1)
    {
        _pushNotificationsFailed.Add(count);
    }

    /// <summary>
    /// Records a failover event
    /// </summary>
    /// <param name="fromNode">Source central node</param>
    /// <param name="toNode">Target central node</param>
    /// <param name="reason">Failover reason</param>
    public void RecordFailover(string fromNode, string toNode, string reason)
    {
        var tags = new TagList
        {
            { "from_node", fromNode },
            { "to_node", toNode },
            { "reason", reason }
        };

        _failoverCount.Add(1, tags);
    }

    /// <summary>
    /// Disposes the meter
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
    }
}
