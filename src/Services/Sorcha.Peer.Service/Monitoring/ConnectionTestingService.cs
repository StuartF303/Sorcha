// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Discovery;
using Sorcha.Peer.Service.Protos;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Monitoring;

/// <summary>
/// Service for testing connection quality to peers
/// </summary>
public class ConnectionTestingService
{
    private readonly ILogger<ConnectionTestingService> _logger;
    private readonly PeerServiceConfiguration _configuration;
    private readonly ConnectionQualityTracker _qualityTracker;

    public ConnectionTestingService(
        ILogger<ConnectionTestingService> logger,
        IOptions<PeerServiceConfiguration> configuration,
        ConnectionQualityTracker qualityTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _qualityTracker = qualityTracker ?? throw new ArgumentNullException(nameof(qualityTracker));
    }

    /// <summary>
    /// Tests connection to a peer and records the results
    /// </summary>
    public async Task<ConnectionTestResult> TestConnectionAsync(
        PeerNode peer,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResult
        {
            PeerId = peer.PeerId,
            Address = peer.Address,
            Port = peer.Port,
            StartTime = DateTimeOffset.UtcNow
        };

        try
        {
            _logger.LogDebug("Testing connection to {PeerId} at {Address}:{Port}",
                peer.PeerId, peer.Address, peer.Port);

            // Test ping
            var pingSuccess = await TestPingAsync(peer, cancellationToken);
            result.PingSuccess = pingSuccess;

            if (pingSuccess)
            {
                result.LatencyMs = sw.ElapsedMilliseconds;
                _qualityTracker.RecordSuccess(peer.PeerId, result.LatencyMs);
                result.Success = true;
                _logger.LogDebug("Connection test successful: {PeerId}, latency: {Latency}ms",
                    peer.PeerId, result.LatencyMs);
            }
            else
            {
                _qualityTracker.RecordFailure(peer.PeerId);
                result.Success = false;
                result.ErrorMessage = "Ping failed";
                _logger.LogWarning("Connection test failed for {PeerId}: Ping failed", peer.PeerId);
            }
        }
        catch (RpcException ex)
        {
            sw.Stop();
            result.Success = false;
            result.LatencyMs = sw.ElapsedMilliseconds;
            result.ErrorMessage = $"gRPC error: {ex.StatusCode} - {ex.Message}";
            _qualityTracker.RecordFailure(peer.PeerId);
            _logger.LogWarning("Connection test failed for {PeerId}: {Error}",
                peer.PeerId, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.LatencyMs = sw.ElapsedMilliseconds;
            result.ErrorMessage = ex.Message;
            _qualityTracker.RecordFailure(peer.PeerId);
            _logger.LogError(ex, "Error testing connection to {PeerId}", peer.PeerId);
        }

        result.EndTime = DateTimeOffset.UtcNow;
        return result;
    }

    /// <summary>
    /// Tests connections to multiple peers concurrently
    /// </summary>
    public async Task<IReadOnlyList<ConnectionTestResult>> TestMultipleAsync(
        IEnumerable<PeerNode> peers,
        int maxConcurrent = 10,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
        var tasks = peers.Select(async peer =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await TestConnectionAsync(peer, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        var successCount = results.Count(r => r.Success);

        _logger.LogInformation("Tested {Total} peers: {Success} successful, {Failed} failed",
            results.Length, successCount, results.Length - successCount);

        return results;
    }

    /// <summary>
    /// Tests ping to a peer
    /// </summary>
    private async Task<bool> TestPingAsync(PeerNode peer, CancellationToken cancellationToken)
    {
        try
        {
            var address = $"http://{peer.Address}:{peer.Port}";
            using var channel = GrpcChannel.ForAddress(address);
            var client = new PeerDiscovery.PeerDiscoveryClient(channel);

            var request = new PingRequest
            {
                PeerId = _configuration.NodeId ?? "test"
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_configuration.Communication.ConnectionTimeout));

            var response = await client.PingAsync(request, cancellationToken: cts.Token);
            return response.Status == PeerStatus.Online;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the best quality peers based on recent connection tests
    /// </summary>
    public IReadOnlyList<string> GetBestPeers(int count)
    {
        return _qualityTracker.GetBestPeers(count);
    }

    /// <summary>
    /// Gets connection quality for a specific peer
    /// </summary>
    public ConnectionQuality? GetPeerQuality(string peerId)
    {
        return _qualityTracker.GetQuality(peerId);
    }

    /// <summary>
    /// Gets connection quality for all peers
    /// </summary>
    public IReadOnlyDictionary<string, ConnectionQuality> GetAllPeerQualities()
    {
        return _qualityTracker.GetAllQualities();
    }
}

/// <summary>
/// Result of a connection test
/// </summary>
public class ConnectionTestResult
{
    public string PeerId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool Success { get; set; }
    public bool PingSuccess { get; set; }
    public long LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
}
