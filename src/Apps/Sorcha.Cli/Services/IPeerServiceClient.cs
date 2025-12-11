using Refit;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Refit client interface for Peer Service REST API.
/// </summary>
public interface IPeerServiceClient
{
    /// <summary>
    /// Lists all known peers.
    /// </summary>
    [Get("/api/peers")]
    Task<List<PeerInfo>> ListPeersAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Gets a peer by ID.
    /// </summary>
    [Get("/api/peers/{peerId}")]
    Task<PeerInfo> GetPeerAsync(string peerId, [Header("Authorization")] string authorization);

    /// <summary>
    /// Gets peer network statistics.
    /// </summary>
    [Get("/api/peers/stats")]
    Task<PeerServiceStatistics> GetStatisticsAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Gets peer health status.
    /// </summary>
    [Get("/api/peers/health")]
    Task<PeerHealthResponse> GetHealthAsync([Header("Authorization")] string authorization);
}
