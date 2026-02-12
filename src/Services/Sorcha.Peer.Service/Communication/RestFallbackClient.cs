// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sorcha.Peer.Service.Communication;

/// <summary>
/// REST-based fallback client for when gRPC is unavailable
/// </summary>
public class RestFallbackClient
{
    private readonly ILogger<RestFallbackClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public RestFallbackClient(
        ILogger<RestFallbackClient> logger,
        HttpClient httpClient,
        string baseUrl)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
    }

    /// <summary>
    /// Sends a ping request
    /// </summary>
    public async Task<bool> PingAsync(string peerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { peerId, timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/peer/ping", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PingResponse>(cancellationToken: cancellationToken);
                return result?.Alive ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST ping failed to {BaseUrl}", _baseUrl);
            return false;
        }
    }

    /// <summary>
    /// Gets peer list via REST
    /// </summary>
    public async Task<List<PeerInfoDto>?> GetPeerListAsync(
        string requestingPeerId,
        int maxPeers = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_baseUrl}/api/peer/list?requestingPeerId={requestingPeerId}&maxPeers={maxPeers}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PeerInfoDto>>(cancellationToken: cancellationToken);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST peer list request failed to {BaseUrl}", _baseUrl);
            return null;
        }
    }

    /// <summary>
    /// Registers a peer via REST
    /// </summary>
    public async Task<bool> RegisterPeerAsync(
        PeerInfoDto peerInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/peer/register",
                peerInfo,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST register peer failed to {BaseUrl}", _baseUrl);
            return false;
        }
    }

    /// <summary>
    /// Notifies about a transaction via REST
    /// </summary>
    public async Task<bool> NotifyTransactionAsync(
        TransactionNotificationDto transaction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/transaction/notify",
                transaction,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NotificationResponse>(cancellationToken: cancellationToken);
                return result?.Accepted ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST transaction notification failed to {BaseUrl}", _baseUrl);
            return false;
        }
    }

    /// <summary>
    /// Sends a generic message via REST
    /// </summary>
    public async Task<bool> SendMessageAsync(
        object message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/peer/message",
                message,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "REST message send failed to {BaseUrl}", _baseUrl);
            return false;
        }
    }
}

// DTOs for REST API
public class PingResponse
{
    public bool Alive { get; set; }
    public long Timestamp { get; set; }
    public string? Version { get; set; }
}

public class PeerInfoDto
{
    public string PeerId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public List<string> SupportedProtocols { get; set; } = new();
}

public class TransactionNotificationDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string OriginPeerId { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public int DataSize { get; set; }
    public string DataHash { get; set; } = string.Empty;
    public int GossipRound { get; set; }
    public int HopCount { get; set; }
    public int TTL { get; set; }
}

public class NotificationResponse
{
    public bool Accepted { get; set; }
    public string? Message { get; set; }
}
