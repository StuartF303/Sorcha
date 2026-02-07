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

    /// <summary>
    /// Gets connection quality scores for all peers.
    /// </summary>
    [Get("/api/peers/quality")]
    Task<List<ConnectionQualityInfo>> GetQualityScoresAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Gets register subscriptions.
    /// </summary>
    [Get("/api/registers/subscriptions")]
    Task<List<SubscriptionInfo>> GetSubscriptionsAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Gets available registers in the network.
    /// </summary>
    [Get("/api/registers/available")]
    Task<List<AvailableRegisterInfo>> GetAvailableRegistersAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Subscribes to a register.
    /// </summary>
    [Post("/api/registers/{registerId}/subscribe")]
    Task<CliSubscribeResponse> SubscribeToRegisterAsync(string registerId, [Body] CliSubscribeRequest request, [Header("Authorization")] string authorization);

    /// <summary>
    /// Unsubscribes from a register.
    /// </summary>
    [Delete("/api/registers/{registerId}/subscribe")]
    Task<CliUnsubscribeResponse> UnsubscribeFromRegisterAsync(string registerId, [Query] bool purge, [Header("Authorization")] string authorization);

    /// <summary>
    /// Purges cached data for a register.
    /// </summary>
    [Delete("/api/registers/{registerId}/cache")]
    Task<CliPurgeResponse> PurgeCacheAsync(string registerId, [Header("Authorization")] string authorization);

    /// <summary>
    /// Bans a peer.
    /// </summary>
    [Post("/api/peers/{peerId}/ban")]
    Task<CliBanResponse> BanPeerAsync(string peerId, [Body] CliBanRequest request, [Header("Authorization")] string authorization);

    /// <summary>
    /// Unbans a peer.
    /// </summary>
    [Delete("/api/peers/{peerId}/ban")]
    Task<CliBanResponse> UnbanPeerAsync(string peerId, [Header("Authorization")] string authorization);

    /// <summary>
    /// Resets a peer's failure count.
    /// </summary>
    [Post("/api/peers/{peerId}/reset")]
    Task<CliResetResponse> ResetPeerAsync(string peerId, [Header("Authorization")] string authorization);
}
