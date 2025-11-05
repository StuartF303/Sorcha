// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;
using Sorcha.Peer.Service.Protos;

namespace Sorcha.Peer.Service.Discovery;

/// <summary>
/// gRPC service implementation for peer discovery
/// </summary>
public class PeerDiscoveryServiceImpl : PeerDiscovery.PeerDiscoveryBase
{
    private readonly ILogger<PeerDiscoveryServiceImpl> _logger;
    private readonly PeerServiceConfiguration _configuration;
    private readonly PeerListManager _peerListManager;

    public PeerDiscoveryServiceImpl(
        ILogger<PeerDiscoveryServiceImpl> logger,
        IOptions<PeerServiceConfiguration> configuration,
        PeerListManager peerListManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _peerListManager = peerListManager ?? throw new ArgumentNullException(nameof(peerListManager));
    }

    /// <summary>
    /// Handles GetPeerList requests - returns a list of known peers
    /// </summary>
    public override Task<PeerListResponse> GetPeerList(PeerListRequest request, ServerCallContext context)
    {
        _logger.LogDebug("GetPeerList request from {PeerId}", request.RequestingPeerId);

        try
        {
            // Get healthy peers
            var healthyPeers = _peerListManager.GetHealthyPeers();

            // Limit to requested count
            var maxPeers = request.MaxPeers > 0 ? request.MaxPeers : 100;
            var peersToReturn = healthyPeers
                .Take(maxPeers)
                .Select(ConvertToPeerInfo)
                .ToList();

            var response = new PeerListResponse
            {
                TotalPeers = healthyPeers.Count
            };
            response.Peers.AddRange(peersToReturn);

            _logger.LogInformation("Returning {Count} peers to {PeerId}",
                peersToReturn.Count, request.RequestingPeerId);

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetPeerList request");
            throw new RpcException(new Status(StatusCode.Internal, "Error retrieving peer list"));
        }
    }

    /// <summary>
    /// Handles RegisterPeer requests - adds a new peer to the list
    /// </summary>
    public override async Task<RegisterPeerResponse> RegisterPeer(RegisterPeerRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RegisterPeer request from {PeerId} at {Address}:{Port}",
            request.PeerInfo.PeerId, request.PeerInfo.Address, request.PeerInfo.Port);

        try
        {
            // Validate peer info
            if (string.IsNullOrEmpty(request.PeerInfo.PeerId))
            {
                return new RegisterPeerResponse
                {
                    Success = false,
                    Message = "Invalid peer ID"
                };
            }

            if (string.IsNullOrEmpty(request.PeerInfo.Address))
            {
                return new RegisterPeerResponse
                {
                    Success = false,
                    Message = "Invalid peer address"
                };
            }

            // Convert and add peer
            var peer = ConvertToPeerNode(request.PeerInfo);
            var added = await _peerListManager.AddOrUpdatePeerAsync(peer);

            if (added)
            {
                _logger.LogInformation("Successfully registered peer {PeerId}", peer.PeerId);
                return new RegisterPeerResponse
                {
                    Success = true,
                    Message = "Peer registered successfully"
                };
            }
            else
            {
                return new RegisterPeerResponse
                {
                    Success = false,
                    Message = "Peer list is full"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RegisterPeer request");
            return new RegisterPeerResponse
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    /// <summary>
    /// Handles Ping requests - simple health check
    /// </summary>
    public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
    {
        _logger.LogTrace("Ping request from {PeerId}", request.PeerId);

        // Update last seen time for the peer
        if (!string.IsNullOrEmpty(request.PeerId))
        {
            _ = _peerListManager.UpdateLastSeenAsync(request.PeerId);
        }

        return Task.FromResult(new PingResponse
        {
            Alive = true,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// Converts a PeerNode to PeerInfo proto message
    /// </summary>
    private PeerInfo ConvertToPeerInfo(PeerNode peer)
    {
        var peerInfo = new PeerInfo
        {
            PeerId = peer.PeerId,
            Address = peer.Address,
            Port = peer.Port
        };
        peerInfo.SupportedProtocols.AddRange(peer.SupportedProtocols);
        return peerInfo;
    }

    /// <summary>
    /// Converts a PeerInfo proto message to PeerNode
    /// </summary>
    private PeerNode ConvertToPeerNode(PeerInfo peerInfo)
    {
        return new PeerNode
        {
            PeerId = peerInfo.PeerId,
            Address = peerInfo.Address,
            Port = peerInfo.Port,
            SupportedProtocols = peerInfo.SupportedProtocols.ToList(),
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
            FailureCount = 0,
            IsBootstrapNode = false,
            AverageLatencyMs = 0,
            Capabilities = new PeerCapabilities
            {
                SupportsStreaming = peerInfo.SupportedProtocols.Contains("GrpcStream"),
                SupportsTransactionDistribution = true,
                MaxTransactionSize = 10 * 1024 * 1024
            }
        };
    }
}
