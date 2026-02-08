// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sorcha.Peer.Service.Protos;

namespace Sorcha.ServiceClients.Peer;

/// <summary>
/// gRPC client for Peer Service operations
/// </summary>
public class PeerServiceClient : IPeerServiceClient, IDisposable
{
    private readonly ILogger<PeerServiceClient> _logger;
    private readonly string _serviceAddress;
    private readonly GrpcChannel _channel;
    private readonly PeerDiscovery.PeerDiscoveryClient _discoveryClient;
    private readonly PeerCommunication.PeerCommunicationClient _communicationClient;
    private readonly HttpClient _httpClient;
    private readonly string _localPeerId;
    private bool _disposed;
    private bool _peerServiceUnavailableLogged;

    public PeerServiceClient(
        IConfiguration configuration,
        ILogger<PeerServiceClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _serviceAddress = configuration["ServiceClients:PeerService:Address"]
            ?? configuration["GrpcClients:PeerService:Address"]
            ?? throw new InvalidOperationException("Peer Service address not configured");

        _localPeerId = configuration["Validator:ValidatorId"]
            ?? configuration["ServiceClients:PeerId"]
            ?? Environment.MachineName;

        // Create gRPC channel
        _channel = GrpcChannel.ForAddress(_serviceAddress);
        _discoveryClient = new PeerDiscovery.PeerDiscoveryClient(_channel);
        _communicationClient = new PeerCommunication.PeerCommunicationClient(_channel);

        // Create HttpClient for REST API calls (same address serves both gRPC and HTTP)
        _httpClient = new HttpClient { BaseAddress = new Uri(_serviceAddress.TrimEnd('/') + "/") };

        _logger.LogInformation("PeerServiceClient initialized (Address: {Address}, PeerId: {PeerId})", _serviceAddress, _localPeerId);
    }

    public async Task<List<ValidatorInfo>> QueryValidatorsAsync(
        string registerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Querying validators for register {RegisterId}", registerId);

            var request = new PeerListRequest
            {
                RequestingPeerId = _localPeerId,
                MaxPeers = 100
            };

            var response = await _discoveryClient.GetPeerListAsync(
                request,
                cancellationToken: cancellationToken);

            // Reset unavailable flag on successful connection
            _peerServiceUnavailableLogged = false;

            var validators = response.Peers
                .Where(p => p.Capabilities?.SupportsTransactionDistribution == true)
                .Select(p => new ValidatorInfo
                {
                    ValidatorId = p.PeerId,
                    GrpcEndpoint = $"{p.Address}:{p.Port}",
                    IsActive = true,
                    ReputationScore = 1.0
                })
                .ToList();

            _logger.LogDebug("Found {Count} validators for register {RegisterId}", validators.Count, registerId);
            return validators;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            if (!_peerServiceUnavailableLogged)
            {
                _logger.LogWarning("Peer Service unavailable - returning empty validator list");
                _peerServiceUnavailableLogged = true;
            }
            else
            {
                _logger.LogDebug("Peer Service still unavailable - returning empty validator list");
            }
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query validators for register {RegisterId}", registerId);
            return [];
        }
    }

    public async Task PublishProposedDocketAsync(
        string registerId,
        string docketId,
        byte[] docketData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Publishing proposed docket {DocketId} for register {RegisterId} ({DataLength} bytes)",
                docketId, registerId, docketData.Length);

            var message = new PeerMessage
            {
                SenderPeerId = _localPeerId,
                RecipientPeerId = "*", // Broadcast to all
                MessageType = MessageType.TransactionNotification,
                Payload = Google.Protobuf.ByteString.CopyFrom(docketData),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var response = await _communicationClient.SendMessageAsync(
                message,
                cancellationToken: cancellationToken);

            if (response.Received)
            {
                _logger.LogInformation(
                    "Successfully published proposed docket {DocketId} for register {RegisterId}",
                    docketId, registerId);
            }
            else
            {
                _logger.LogWarning(
                    "Proposed docket {DocketId} publish not acknowledged for register {RegisterId}",
                    docketId, registerId);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            if (!_peerServiceUnavailableLogged)
            {
                _logger.LogWarning(
                    "Peer Service unavailable - cannot publish proposed docket {DocketId}",
                    docketId);
                _peerServiceUnavailableLogged = true;
            }
            else
            {
                _logger.LogDebug(
                    "Peer Service still unavailable - cannot publish proposed docket {DocketId}",
                    docketId);
            }
        }
        catch (RpcException ex)
        {
            _logger.LogDebug(
                "Peer Service gRPC error publishing proposed docket {DocketId}: {Status}",
                docketId, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to publish proposed docket {DocketId} for register {RegisterId}",
                docketId, registerId);
        }
    }

    public async Task BroadcastConfirmedDocketAsync(
        string registerId,
        string docketId,
        byte[] docketData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Broadcasting confirmed docket {DocketId} for register {RegisterId} ({DataLength} bytes)",
                docketId, registerId, docketData.Length);

            var message = new PeerMessage
            {
                SenderPeerId = _localPeerId,
                RecipientPeerId = "*", // Broadcast to all
                MessageType = MessageType.TransactionResponse, // Using TransactionResponse for confirmed dockets
                Payload = Google.Protobuf.ByteString.CopyFrom(docketData),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var response = await _communicationClient.SendMessageAsync(
                message,
                cancellationToken: cancellationToken);

            if (response.Received)
            {
                _logger.LogInformation(
                    "Successfully broadcast confirmed docket {DocketId} for register {RegisterId}",
                    docketId, registerId);
            }
            else
            {
                _logger.LogWarning(
                    "Confirmed docket {DocketId} broadcast not acknowledged for register {RegisterId}",
                    docketId, registerId);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogDebug(
                "Peer Service unavailable - cannot broadcast confirmed docket {DocketId}",
                docketId);
        }
        catch (RpcException ex)
        {
            _logger.LogDebug(
                "Peer Service gRPC error broadcasting confirmed docket {DocketId}: {Status}",
                docketId, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to broadcast confirmed docket {DocketId} for register {RegisterId}",
                docketId, registerId);
        }
    }

    public async Task ReportValidatorBehaviorAsync(
        string validatorId,
        string behavior,
        string details,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Reporting behavior '{Behavior}' for validator {ValidatorId}: {Details}",
                behavior, validatorId, details);

            // Use PeerStatusUpdate message type to report behavior
            var behaviorReport = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
            {
                Type = "ValidatorBehaviorReport",
                ValidatorId = validatorId,
                Behavior = behavior,
                Details = details,
                ReportedBy = _localPeerId,
                Timestamp = DateTimeOffset.UtcNow
            });

            var message = new PeerMessage
            {
                SenderPeerId = _localPeerId,
                RecipientPeerId = "*", // Broadcast to all for decentralized reputation
                MessageType = MessageType.PeerStatusUpdate,
                Payload = Google.Protobuf.ByteString.CopyFrom(behaviorReport),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var response = await _communicationClient.SendMessageAsync(
                message,
                cancellationToken: cancellationToken);

            if (response.Received)
            {
                _logger.LogInformation(
                    "Reported behavior '{Behavior}' for validator {ValidatorId}",
                    behavior, validatorId);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogDebug(
                "Peer Service unavailable - cannot report validator {ValidatorId} behavior",
                validatorId);
        }
        catch (RpcException ex)
        {
            _logger.LogDebug(
                "Peer Service gRPC error reporting validator {ValidatorId} behavior: {Status}",
                validatorId, ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to report behavior for validator {ValidatorId}",
                validatorId);
        }
    }

    public async Task AdvertiseRegisterAsync(
        string registerId,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Advertising register {RegisterId} (isPublic={IsPublic}) to Peer Service",
                registerId, isPublic);

            var response = await _httpClient.PostAsJsonAsync(
                $"api/registers/{registerId}/advertise",
                new { isPublic },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully advertised register {RegisterId} (isPublic={IsPublic})",
                    registerId, isPublic);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to advertise register {RegisterId}: {StatusCode}",
                    registerId, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(
                ex,
                "Peer Service unavailable - cannot advertise register {RegisterId}",
                registerId);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Failed to advertise register {RegisterId} to Peer Service",
                registerId);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _channel.Dispose();
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
