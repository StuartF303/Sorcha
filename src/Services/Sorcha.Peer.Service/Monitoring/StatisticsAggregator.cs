// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.Peer.Service.Communication;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Distribution;

namespace Sorcha.Peer.Service.Monitoring;

/// <summary>
/// Aggregates statistics from all peer service components
/// </summary>
public class StatisticsAggregator
{
    private readonly ILogger<StatisticsAggregator> _logger;
    private readonly PeerListManager _peerListManager;
    private readonly ConnectionQualityTracker _qualityTracker;
    private readonly TransactionQueueManager _queueManager;
    private readonly GossipProtocolEngine _gossipEngine;
    private readonly CommunicationProtocolManager? _communicationManager;

    public StatisticsAggregator(
        ILogger<StatisticsAggregator> logger,
        PeerListManager peerListManager,
        ConnectionQualityTracker qualityTracker,
        TransactionQueueManager queueManager,
        GossipProtocolEngine gossipEngine,
        CommunicationProtocolManager? communicationManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
        _qualityTracker = qualityTracker ?? throw new ArgumentNullException(nameof(qualityTracker));
        _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
        _gossipEngine = gossipEngine ?? throw new ArgumentNullException(nameof(gossipEngine));
        _communicationManager = communicationManager;
    }

    /// <summary>
    /// Gets comprehensive peer service statistics
    /// </summary>
    public PeerServiceStatistics GetStatistics()
    {
        var allPeers = _peerListManager.GetAllPeers();
        var healthyPeers = _peerListManager.GetHealthyPeers();
        var peerQualities = _qualityTracker.GetAllQualities();
        var circuitBreakers = _communicationManager?.GetCircuitBreakerStats() ??
            new Dictionary<string, CircuitBreakerStats>();

        return new PeerServiceStatistics
        {
            Timestamp = DateTimeOffset.UtcNow,
            PeerStats = new PeerStatistics
            {
                TotalPeers = allPeers.Count,
                HealthyPeers = healthyPeers.Count,
                UnhealthyPeers = allPeers.Count - healthyPeers.Count,
                SeedNodes = allPeers.Count(p => p.IsSeedNode),
                AverageLatencyMs = healthyPeers.Any() ? healthyPeers.Average(p => p.AverageLatencyMs) : 0,
                TotalFailures = allPeers.Sum(p => p.FailureCount)
            },
            QualityStats = new QualityStatistics
            {
                TotalTrackedPeers = peerQualities.Count,
                ExcellentPeers = peerQualities.Count(q => q.Value.QualityScore >= 80),
                GoodPeers = peerQualities.Count(q => q.Value.QualityScore >= 60 && q.Value.QualityScore < 80),
                FairPeers = peerQualities.Count(q => q.Value.QualityScore >= 40 && q.Value.QualityScore < 60),
                PoorPeers = peerQualities.Count(q => q.Value.QualityScore < 40),
                AverageQualityScore = peerQualities.Any() ? peerQualities.Average(q => q.Value.QualityScore) : 0
            },
            QueueStats = new QueueStatistics
            {
                QueueSize = _queueManager.GetQueueSize(),
                IsEmpty = _queueManager.IsEmpty()
            },
            CircuitBreakerStats = new CircuitBreakerSummary
            {
                TotalCircuitBreakers = circuitBreakers.Count,
                OpenCircuits = circuitBreakers.Count(c => c.Value.State == CircuitState.Open),
                HalfOpenCircuits = circuitBreakers.Count(c => c.Value.State == CircuitState.HalfOpen),
                ClosedCircuits = circuitBreakers.Count(c => c.Value.State == CircuitState.Closed)
            }
        };
    }

    /// <summary>
    /// Gets detailed peer information
    /// </summary>
    public IReadOnlyList<DetailedPeerInfo> GetDetailedPeerInfo()
    {
        var allPeers = _peerListManager.GetAllPeers();
        var peerQualities = _qualityTracker.GetAllQualities();

        return allPeers.Select(peer => new DetailedPeerInfo
        {
            PeerId = peer.PeerId,
            Address = peer.Address,
            Port = peer.Port,
            SupportedProtocols = peer.SupportedProtocols,
            FirstSeen = peer.FirstSeen,
            LastSeen = peer.LastSeen,
            FailureCount = peer.FailureCount,
            IsSeedNode = peer.IsSeedNode,
            AverageLatencyMs = peer.AverageLatencyMs,
            Quality = peerQualities.TryGetValue(peer.PeerId, out var quality) ? quality : null
        }).ToList();
    }

    /// <summary>
    /// Gets top performing peers
    /// </summary>
    public IReadOnlyList<string> GetTopPeers(int count = 10)
    {
        return _qualityTracker.GetBestPeers(count);
    }

    /// <summary>
    /// Exports statistics in JSON format
    /// </summary>
    public string ExportAsJson()
    {
        var stats = GetStatistics();
        return System.Text.Json.JsonSerializer.Serialize(stats, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

/// <summary>
/// Comprehensive peer service statistics
/// </summary>
public class PeerServiceStatistics
{
    public DateTimeOffset Timestamp { get; set; }
    public PeerStatistics PeerStats { get; set; } = new();
    public QualityStatistics QualityStats { get; set; } = new();
    public QueueStatistics QueueStats { get; set; } = new();
    public CircuitBreakerSummary CircuitBreakerStats { get; set; } = new();
}

public class PeerStatistics
{
    public int TotalPeers { get; set; }
    public int HealthyPeers { get; set; }
    public int UnhealthyPeers { get; set; }
    public int SeedNodes { get; set; }
    public double AverageLatencyMs { get; set; }
    public int TotalFailures { get; set; }
}

public class QualityStatistics
{
    public int TotalTrackedPeers { get; set; }
    public int ExcellentPeers { get; set; }
    public int GoodPeers { get; set; }
    public int FairPeers { get; set; }
    public int PoorPeers { get; set; }
    public double AverageQualityScore { get; set; }
}

public class QueueStatistics
{
    public int QueueSize { get; set; }
    public bool IsEmpty { get; set; }
}

public class CircuitBreakerSummary
{
    public int TotalCircuitBreakers { get; set; }
    public int OpenCircuits { get; set; }
    public int HalfOpenCircuits { get; set; }
    public int ClosedCircuits { get; set; }
}

public class DetailedPeerInfo
{
    public string PeerId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> SupportedProtocols { get; set; } = new();
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    public int FailureCount { get; set; }
    public bool IsSeedNode { get; set; }
    public int AverageLatencyMs { get; set; }
    public ConnectionQuality? Quality { get; set; }
}
