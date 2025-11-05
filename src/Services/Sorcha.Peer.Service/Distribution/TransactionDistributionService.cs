// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Peer.Service.Core;

namespace Sorcha.Peer.Service.Distribution;

/// <summary>
/// Service for distributing transactions using gossip protocol
/// </summary>
public class TransactionDistributionService
{
    private readonly ILogger<TransactionDistributionService> _logger;
    private readonly PeerServiceConfiguration _configuration;
    private readonly TransactionQueueManager _queueManager;
    private readonly GossipProtocolEngine _gossipEngine;

    public TransactionDistributionService(
        ILogger<TransactionDistributionService> logger,
        IOptions<PeerServiceConfiguration> configuration,
        TransactionQueueManager queueManager,
        GossipProtocolEngine gossipEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
        _gossipEngine = gossipEngine ?? throw new ArgumentNullException(nameof(gossipEngine));
    }

    /// <summary>
    /// Distributes a transaction using gossip protocol
    /// </summary>
    public async Task<bool> DistributeTransactionAsync(
        TransactionNotification transaction,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Distributing transaction {TxId} via gossip", transaction.TransactionId);

            // Check if we should gossip this transaction
            if (!_gossipEngine.ShouldGossip(transaction))
            {
                _logger.LogDebug("Skipping gossip for transaction {TxId} (already seen or expired)",
                    transaction.TransactionId);
                return false;
            }

            // Mark as seen to prevent loops
            _gossipEngine.RecordSeen(transaction.TransactionId);

            // Select gossip targets
            var targets = _gossipEngine.SelectGossipTargets(transaction.TransactionId, transaction.GossipRound);
            if (targets.Count == 0)
            {
                _logger.LogWarning("No gossip targets available for transaction {TxId}", transaction.TransactionId);
                return false;
            }

            // Prepare transaction for next round
            var nextRoundTx = _gossipEngine.PrepareForNextRound(transaction);

            // Send to all targets concurrently
            var sendTasks = targets.Select(peer =>
                SendToPeerAsync(peer, nextRoundTx, cancellationToken));

            var results = await Task.WhenAll(sendTasks);
            var successCount = results.Count(r => r);

            _logger.LogInformation("Distributed transaction {TxId} to {Success}/{Total} peers",
                transaction.TransactionId, successCount, targets.Count);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error distributing transaction {TxId}", transaction.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Sends a transaction to a specific peer
    /// </summary>
    private async Task<bool> SendToPeerAsync(
        Core.PeerNode peer,
        TransactionNotification transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            var address = $"http://{peer.Address}:{peer.Port}";
            using var channel = GrpcChannel.ForAddress(address);
            var client = new TransactionDistribution.TransactionDistributionClient(channel);

            var request = new TransactionNotification
            {
                TransactionId = transaction.TransactionId,
                OriginPeerId = transaction.OriginPeerId,
                Timestamp = transaction.Timestamp.ToUnixTimeSeconds(),
                DataSize = transaction.DataSize,
                DataHash = transaction.DataHash,
                GossipRound = transaction.GossipRound,
                HopCount = transaction.HopCount,
                Ttl = transaction.TTL
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_configuration.Communication.ConnectionTimeout));

            var response = await client.NotifyTransactionAsync(request, cancellationToken: cts.Token);

            _logger.LogDebug("Sent transaction {TxId} to peer {PeerId}: {Success}",
                transaction.TransactionId, peer.PeerId, response.Accepted);

            return response.Accepted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send transaction {TxId} to peer {PeerId}",
                transaction.TransactionId, peer.PeerId);
            return false;
        }
    }

    /// <summary>
    /// Queues a transaction for later distribution (offline mode)
    /// </summary>
    public async Task<bool> QueueTransactionAsync(
        TransactionNotification transaction,
        CancellationToken cancellationToken = default)
    {
        return await _queueManager.EnqueueAsync(transaction, cancellationToken);
    }

    /// <summary>
    /// Processes queued transactions
    /// </summary>
    public async Task<int> ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        var processedCount = 0;
        var maxBatch = 10;

        for (int i = 0; i < maxBatch && !cancellationToken.IsCancellationRequested; i++)
        {
            if (!_queueManager.TryDequeue(out var queuedTx) || queuedTx == null)
            {
                break;
            }

            var success = await DistributeTransactionAsync(queuedTx.Transaction, cancellationToken);

            if (success)
            {
                await _queueManager.MarkAsProcessedAsync(queuedTx.Id, cancellationToken);
                processedCount++;
            }
            else
            {
                await _queueManager.MarkAsFailedAsync(queuedTx, cancellationToken);
            }
        }

        if (processedCount > 0)
        {
            _logger.LogInformation("Processed {Count} queued transactions", processedCount);
        }

        return processedCount;
    }

    /// <summary>
    /// Gets queue statistics
    /// </summary>
    public int GetQueueSize() => _queueManager.GetQueueSize();
}
