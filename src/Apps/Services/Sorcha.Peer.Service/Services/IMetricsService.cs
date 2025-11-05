// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Peer.Service.Models;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Services;

/// <summary>
/// Service for tracking and reporting metrics
/// </summary>
public interface IMetricsService
{
    void IncrementActivePeers();
    void DecrementActivePeers();
    void IncrementTransactionCount();
    ServiceMetrics GetCurrentMetrics();
}

/// <summary>
/// In-memory metrics tracking service
/// </summary>
public class MetricsService : IMetricsService
{
    private int _activePeers;
    private long _totalTransactions;
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private readonly List<long> _recentTransactionTimestamps = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public void IncrementActivePeers()
    {
        Interlocked.Increment(ref _activePeers);
    }

    public void DecrementActivePeers()
    {
        Interlocked.Decrement(ref _activePeers);
    }

    public void IncrementTransactionCount()
    {
        Interlocked.Increment(ref _totalTransactions);

        _lock.Wait();
        try
        {
            _recentTransactionTimestamps.Add(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // Keep only last 60 seconds of transactions
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeMilliseconds();
            _recentTransactionTimestamps.RemoveAll(t => t < cutoff);
        }
        finally
        {
            _lock.Release();
        }
    }

    public ServiceMetrics GetCurrentMetrics()
    {
        _lock.Wait();
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeMilliseconds();
            var recentCount = _recentTransactionTimestamps.Count(t => t >= cutoff);

            var process = Process.GetCurrentProcess();

            return new ServiceMetrics
            {
                ActivePeers = _activePeers,
                TotalTransactions = _totalTransactions,
                ThroughputPerSecond = recentCount,
                CpuUsagePercent = GetCpuUsage(),
                MemoryUsageBytes = process.WorkingSet64,
                UptimeSeconds = (long)_uptime.Elapsed.TotalSeconds
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private double GetCpuUsage()
    {
        // Simplified CPU usage - in production, use a proper CPU counter
        var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / _uptime.Elapsed.TotalMilliseconds * 100;
    }
}
