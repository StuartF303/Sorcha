// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;
using System.Net;
using System.Net.Sockets;

namespace Sorcha.Peer.Service.Network;

/// <summary>
/// Service for detecting and managing network addresses including NAT traversal
/// </summary>
public class NetworkAddressService
{
    private readonly ILogger<NetworkAddressService> _logger;
    private readonly NetworkAddressConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private string? _cachedExternalAddress;
    private DateTime _lastDetectionTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public NetworkAddressService(
        ILogger<NetworkAddressService> logger,
        IOptions<PeerServiceConfiguration> configuration,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value?.NetworkAddress ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Gets the external IP address, using cache if available
    /// </summary>
    public async Task<string?> GetExternalAddressAsync(CancellationToken cancellationToken = default)
    {
        // Return configured external address if provided
        if (!string.IsNullOrEmpty(_configuration.ExternalAddress))
        {
            _logger.LogDebug("Using configured external address: {Address}", _configuration.ExternalAddress);
            return _configuration.ExternalAddress;
        }

        // Return cached address if still valid
        if (_cachedExternalAddress != null &&
            DateTime.UtcNow - _lastDetectionTime < _cacheExpiration)
        {
            _logger.LogDebug("Using cached external address: {Address}", _cachedExternalAddress);
            return _cachedExternalAddress;
        }

        // Detect external address
        var address = await DetectExternalAddressAsync(cancellationToken);

        if (address != null)
        {
            _cachedExternalAddress = address;
            _lastDetectionTime = DateTime.UtcNow;
            _logger.LogInformation("Detected external address: {Address}", address);
        }

        return address;
    }

    /// <summary>
    /// Gets the local IP address
    /// </summary>
    public string? GetLocalAddress()
    {
        try
        {
            var addressFamily = _configuration.PreferredProtocol == "IPv6"
                ? AddressFamily.InterNetworkV6
                : AddressFamily.InterNetwork;

            var host = Dns.GetHostEntry(Dns.GetHostName());
            var localAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == addressFamily);

            if (localAddress != null)
            {
                _logger.LogDebug("Local address: {Address}", localAddress);
                return localAddress.ToString();
            }

            _logger.LogWarning("Could not determine local address");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting local address");
            return null;
        }
    }

    /// <summary>
    /// Detects the external IP address using HTTP lookup services
    /// </summary>
    private async Task<string?> DetectExternalAddressAsync(CancellationToken cancellationToken)
    {
        // Try HTTP lookup services first
        foreach (var service in _configuration.HttpLookupServices)
        {
            try
            {
                _logger.LogDebug("Trying HTTP lookup service: {Service}", service);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var response = await _httpClient.GetStringAsync(service, cts.Token);
                var address = response.Trim();

                if (IPAddress.TryParse(address, out _))
                {
                    _logger.LogInformation("Successfully detected external address via HTTP: {Address}", address);
                    return address;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect address using HTTP service: {Service}", service);
            }
        }

        // TODO: Sprint 3 - Implement STUN protocol for NAT traversal
        // For now, try STUN as a future enhancement
        _logger.LogDebug("STUN detection not yet implemented (Sprint 3)");

        // Fallback to local address if external detection fails
        _logger.LogWarning("Could not detect external address, falling back to local address");
        return GetLocalAddress();
    }

    /// <summary>
    /// Invalidates the cached external address, forcing re-detection
    /// </summary>
    public void InvalidateCache()
    {
        _logger.LogDebug("Invalidating external address cache");
        _cachedExternalAddress = null;
        _lastDetectionTime = DateTime.MinValue;
    }

    /// <summary>
    /// Checks if the current machine is behind NAT
    /// </summary>
    public async Task<bool> IsBehindNatAsync(CancellationToken cancellationToken = default)
    {
        var localAddress = GetLocalAddress();
        var externalAddress = await GetExternalAddressAsync(cancellationToken);

        if (localAddress == null || externalAddress == null)
        {
            return false; // Cannot determine
        }

        var behindNat = localAddress != externalAddress;
        _logger.LogInformation("NAT detection: Local={Local}, External={External}, BehindNAT={BehindNat}",
            localAddress, externalAddress, behindNat);

        return behindNat;
    }
}
