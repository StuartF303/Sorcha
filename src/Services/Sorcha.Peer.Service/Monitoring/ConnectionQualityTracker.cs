// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Monitoring;

/// <summary>
/// Tracks connection quality metrics for peers
/// </summary>
public class ConnectionQualityTracker
{
    private readonly ILogger<ConnectionQualityTracker> _logger;
    private readonly ConcurrentDictionary<string, PeerMetrics> _metrics;
    private const int MaxHistorySize = 100;

    public ConnectionQualityTracker(ILogger<ConnectionQualityTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new ConcurrentDictionary<string, PeerMetrics>();
    }

    /// <summary>
    /// Records a successful connection to a peer
    /// </summary>
    public void RecordSuccess(string peerId, long latencyMs)
    {
        if (string.IsNullOrEmpty(peerId))
            return;

        var metrics = _metrics.GetOrAdd(peerId, _ => new PeerMetrics(peerId));
        metrics.RecordSuccess(latencyMs);

        _logger.LogTrace("Recorded success for {PeerId}: {Latency}ms", peerId, latencyMs);
    }

    /// <summary>
    /// Records a failed connection attempt to a peer
    /// </summary>
    public void RecordFailure(string peerId)
    {
        if (string.IsNullOrEmpty(peerId))
            return;

        var metrics = _metrics.GetOrAdd(peerId, _ => new PeerMetrics(peerId));
        metrics.RecordFailure();

        _logger.LogTrace("Recorded failure for {PeerId}", peerId);
    }

    /// <summary>
    /// Gets the quality metrics for a specific peer
    /// </summary>
    public ConnectionQuality? GetQuality(string peerId)
    {
        if (_metrics.TryGetValue(peerId, out var metrics))
        {
            return metrics.GetQuality();
        }

        return null;
    }

    /// <summary>
    /// Gets all peer metrics
    /// </summary>
    public IReadOnlyDictionary<string, ConnectionQuality> GetAllQualities()
    {
        var result = new Dictionary<string, ConnectionQuality>();

        foreach (var kvp in _metrics)
        {
            result[kvp.Key] = kvp.Value.GetQuality();
        }

        return result;
    }

    /// <summary>
    /// Gets the best quality peers
    /// </summary>
    public IReadOnlyList<string> GetBestPeers(int count)
    {
        return _metrics
            .Select(kvp => new { PeerId = kvp.Key, Quality = kvp.Value.GetQuality() })
            .Where(x => x.Quality.QualityScore > 0)
            .OrderByDescending(x => x.Quality.QualityScore)
            .Take(count)
            .Select(x => x.PeerId)
            .ToList();
    }

    /// <summary>
    /// Removes metrics for a peer
    /// </summary>
    public void RemovePeer(string peerId)
    {
        _metrics.TryRemove(peerId, out _);
        _logger.LogDebug("Removed metrics for {PeerId}", peerId);
    }

    /// <summary>
    /// Clears all metrics
    /// </summary>
    public void Clear()
    {
        _metrics.Clear();
        _logger.LogInformation("Cleared all connection quality metrics");
    }
}

/// <summary>
/// Metrics for a single peer
/// </summary>
internal class PeerMetrics
{
    private readonly string _peerId;
    private readonly Queue<long> _latencyHistory;
    private readonly object _lock = new();
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private DateTimeOffset _lastUpdated;

    public PeerMetrics(string peerId)
    {
        _peerId = peerId;
        _latencyHistory = new Queue<long>();
        _lastUpdated = DateTimeOffset.UtcNow;
    }

    public void RecordSuccess(long latencyMs)
    {
        lock (_lock)
        {
            _totalRequests++;
            _successfulRequests++;
            _lastUpdated = DateTimeOffset.UtcNow;

            _latencyHistory.Enqueue(latencyMs);
            if (_latencyHistory.Count > 100) // Keep last 100 samples
            {
                _latencyHistory.Dequeue();
            }
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _totalRequests++;
            _failedRequests++;
            _lastUpdated = DateTimeOffset.UtcNow;
        }
    }

    public ConnectionQuality GetQuality()
    {
        lock (_lock)
        {
            var avgLatency = _latencyHistory.Count > 0
                ? _latencyHistory.Average()
                : 0;

            var successRate = _totalRequests > 0
                ? (double)_successfulRequests / _totalRequests
                : 0;

            // Calculate quality score (0-100)
            // Based on success rate (70%) and latency (30%)
            var latencyScore = CalculateLatencyScore(avgLatency);
            var qualityScore = (successRate * 70) + (latencyScore * 30);

            return new ConnectionQuality
            {
                PeerId = _peerId,
                AverageLatencyMs = avgLatency,
                MinLatencyMs = _latencyHistory.Count > 0 ? _latencyHistory.Min() : 0,
                MaxLatencyMs = _latencyHistory.Count > 0 ? _latencyHistory.Max() : 0,
                SuccessRate = successRate,
                TotalRequests = _totalRequests,
                SuccessfulRequests = _successfulRequests,
                FailedRequests = _failedRequests,
                QualityScore = qualityScore,
                LastUpdated = _lastUpdated
            };
        }
    }

    private double CalculateLatencyScore(double avgLatencyMs)
    {
        // Score based on latency:
        // < 50ms = 1.0 (excellent)
        // 50-100ms = 0.8 (good)
        // 100-200ms = 0.6 (acceptable)
        // 200-500ms = 0.4 (poor)
        // > 500ms = 0.2 (very poor)

        if (avgLatencyMs < 50) return 1.0;
        if (avgLatencyMs < 100) return 0.8;
        if (avgLatencyMs < 200) return 0.6;
        if (avgLatencyMs < 500) return 0.4;
        return 0.2;
    }
}

/// <summary>
/// Connection quality information for a peer
/// </summary>
public class ConnectionQuality
{
    public string PeerId { get; set; } = string.Empty;
    public double AverageLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double SuccessRate { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double QualityScore { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Gets the quality rating as a string
    /// </summary>
    public string QualityRating
    {
        get
        {
            if (QualityScore >= 80) return "Excellent";
            if (QualityScore >= 60) return "Good";
            if (QualityScore >= 40) return "Fair";
            if (QualityScore >= 20) return "Poor";
            return "Very Poor";
        }
    }
}
