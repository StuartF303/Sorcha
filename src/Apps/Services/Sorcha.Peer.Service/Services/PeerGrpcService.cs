// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Core;

namespace Sorcha.Peer.Service.Services;

/// <summary>
/// gRPC service implementation for peer-to-peer communication
/// </summary>
public class PeerGrpcService : PeerService.PeerServiceBase
{
    private readonly ILogger<PeerGrpcService> _logger;
    private readonly IPeerRepository _peerRepository;
    private readonly IMetricsService _metricsService;

    public PeerGrpcService(
        ILogger<PeerGrpcService> logger,
        IPeerRepository peerRepository,
        IMetricsService metricsService)
    {
        _logger = logger;
        _peerRepository = peerRepository;
        _metricsService = metricsService;
    }

    public override async Task<PeerInfoResponse> GetPeerInfo(PeerInfoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting peer info for {PeerId}", request.PeerId);

        var peer = await _peerRepository.GetPeerAsync(request.PeerId);
        if (peer == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Peer {request.PeerId} not found"));
        }

        return new PeerInfoResponse
        {
            PeerId = peer.PeerId,
            Endpoint = peer.Endpoint,
            Status = peer.Status,
            RegisteredAt = peer.RegisteredAt,
            Metadata = { peer.Metadata }
        };
    }

    public override async Task<RegisterPeerResponse> RegisterPeer(RegisterPeerRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Registering peer {PeerId} at {Endpoint}", request.PeerId, request.Endpoint);

        try
        {
            var peerId = await _peerRepository.RegisterPeerAsync(
                request.PeerId,
                request.Endpoint,
                request.Metadata.ToDictionary(x => x.Key, x => x.Value));

            _metricsService.IncrementActivePeers();

            return new RegisterPeerResponse
            {
                Success = true,
                Message = "Peer registered successfully",
                AssignedPeerId = peerId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register peer {PeerId}", request.PeerId);
            return new RegisterPeerResponse
            {
                Success = false,
                Message = $"Failed to register peer: {ex.Message}"
            };
        }
    }

    public override async Task StreamTransactions(
        IAsyncStreamReader<TransactionMessage> requestStream,
        IServerStreamWriter<TransactionResponse> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Starting transaction stream");

        try
        {
            await foreach (var transaction in requestStream.ReadAllAsync(context.CancellationToken))
            {
                _logger.LogDebug("Processing transaction {TransactionId} from {FromPeer} to {ToPeer}",
                    transaction.TransactionId, transaction.FromPeer, transaction.ToPeer);

                _metricsService.IncrementTransactionCount();

                var response = new TransactionResponse
                {
                    TransactionId = transaction.TransactionId,
                    Success = true,
                    Message = "Transaction processed successfully",
                    ProcessedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await responseStream.WriteAsync(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in transaction stream");
            throw new RpcException(new Status(StatusCode.Internal, $"Stream error: {ex.Message}"));
        }
    }

    public override Task<MetricsResponse> GetMetrics(MetricsRequest request, ServerCallContext context)
    {
        _logger.LogDebug("Getting metrics");

        var metrics = _metricsService.GetCurrentMetrics();

        return Task.FromResult(new MetricsResponse
        {
            ActivePeers = metrics.ActivePeers,
            TotalTransactions = metrics.TotalTransactions,
            ThroughputPerSecond = metrics.ThroughputPerSecond,
            CpuUsagePercent = metrics.CpuUsagePercent,
            MemoryUsageBytes = metrics.MemoryUsageBytes,
            UptimeSeconds = metrics.UptimeSeconds
        });
    }
}
