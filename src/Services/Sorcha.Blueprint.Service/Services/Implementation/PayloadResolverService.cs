// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text;
using System.Text.Json;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Services.Implementation;

/// <summary>
/// Service for payload encryption, decryption, and aggregation
/// </summary>
public class PayloadResolverService : IPayloadResolverService
{
    private readonly ILogger<PayloadResolverService> _logger;
    // TODO Sprint 6: Inject IWalletServiceClient when available
    // TODO Sprint 6: Inject IRegisterServiceClient when available

    public PayloadResolverService(ILogger<PayloadResolverService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, byte[]>> CreateEncryptedPayloadsAsync(
        Dictionary<string, object> disclosureResults,
        Dictionary<string, string> participantWallets,
        string senderWallet,
        CancellationToken cancellationToken = default)
    {
        if (disclosureResults == null)
        {
            throw new ArgumentNullException(nameof(disclosureResults));
        }

        if (participantWallets == null)
        {
            throw new ArgumentNullException(nameof(participantWallets));
        }

        if (string.IsNullOrWhiteSpace(senderWallet))
        {
            throw new ArgumentException("Sender wallet cannot be null or empty", nameof(senderWallet));
        }

        var encryptedPayloads = new Dictionary<string, byte[]>();

        foreach (var (participantId, data) in disclosureResults)
        {
            if (!participantWallets.TryGetValue(participantId, out var wallet))
            {
                _logger.LogWarning("No wallet found for participant {ParticipantId}, skipping encryption", participantId);
                continue;
            }

            try
            {
                // TODO Sprint 6: Replace with actual Wallet Service call
                // var encryptedPayload = await _walletServiceClient.EncryptPayloadAsync(
                //     wallet,
                //     JsonSerializer.SerializeToUtf8Bytes(data),
                //     cancellationToken);

                // STUB: For now, just serialize to JSON (plaintext)
                // This will be replaced with actual encryption in Sprint 6
                var jsonPayload = JsonSerializer.SerializeToUtf8Bytes(data);
                var stubEncryptedPayload = CreateStubEncryptedPayload(jsonPayload, wallet);

                encryptedPayloads[wallet] = stubEncryptedPayload;

                _logger.LogDebug("Created encrypted payload for participant {ParticipantId} (wallet: {Wallet}) - STUB MODE",
                    participantId, wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt payload for participant {ParticipantId}", participantId);
                throw;
            }
        }

        await Task.CompletedTask; // For async signature
        return encryptedPayloads;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> AggregateHistoricalDataAsync(
        string registerAddress,
        IEnumerable<string> transactionIds,
        string wallet,
        IEnumerable<string>? disclosureRules = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registerAddress))
        {
            throw new ArgumentException("Register address cannot be null or empty", nameof(registerAddress));
        }

        if (transactionIds == null)
        {
            throw new ArgumentNullException(nameof(transactionIds));
        }

        if (string.IsNullOrWhiteSpace(wallet))
        {
            throw new ArgumentException("Wallet cannot be null or empty", nameof(wallet));
        }

        var transactionIdList = transactionIds.ToList();
        if (!transactionIdList.Any())
        {
            _logger.LogDebug("No transaction IDs provided for aggregation");
            return new Dictionary<string, object>();
        }

        var aggregatedData = new Dictionary<string, object>();

        foreach (var txId in transactionIdList)
        {
            try
            {
                // TODO Sprint 6: Replace with actual Register Service call
                // var transaction = await _registerServiceClient.GetTransactionAsync(
                //     registerAddress,
                //     txId,
                //     cancellationToken);

                // TODO Sprint 6: Replace with actual Wallet Service call for decryption
                // var decryptedPayload = await _walletServiceClient.DecryptPayloadAsync(
                //     wallet,
                //     transaction.EncryptedPayload,
                //     cancellationToken);

                // STUB: Return placeholder data
                // In reality, we would decrypt the payload and merge it into aggregatedData
                var stubDecryptedData = GetStubTransactionData(txId);

                // Merge the decrypted data into aggregated data
                foreach (var (key, value) in stubDecryptedData)
                {
                    // Later values overwrite earlier ones (most recent wins)
                    aggregatedData[key] = value;
                }

                _logger.LogDebug("Aggregated data from transaction {TxId} - STUB MODE", txId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve/decrypt transaction {TxId}, skipping", txId);
                // Continue with other transactions
            }
        }

        // TODO Sprint 6: Apply disclosure rules if provided
        // If disclosureRules are provided, filter the aggregatedData to only include allowed fields

        await Task.CompletedTask; // For async signature
        return aggregatedData;
    }

    /// <summary>
    /// Creates a stub encrypted payload (Sprint 3 placeholder)
    /// </summary>
    private byte[] CreateStubEncryptedPayload(byte[] payload, string recipientWallet)
    {
        // STUB: Just add a prefix to simulate encryption
        // In Sprint 6, this will be replaced with actual encryption via Wallet Service
        var prefix = Encoding.UTF8.GetBytes($"ENCRYPTED_FOR:{recipientWallet}:");
        var combined = new byte[prefix.Length + payload.Length];
        Array.Copy(prefix, 0, combined, 0, prefix.Length);
        Array.Copy(payload, 0, combined, prefix.Length, payload.Length);
        return combined;
    }

    /// <summary>
    /// Gets stub transaction data (Sprint 3 placeholder)
    /// </summary>
    private Dictionary<string, object> GetStubTransactionData(string txId)
    {
        // STUB: Return placeholder data
        // In Sprint 6, this will be replaced with actual Register Service call and decryption
        return new Dictionary<string, object>
        {
            ["previousTxId"] = txId,
            ["stubDataField"] = $"Historical data from transaction {txId}"
        };
    }
}
