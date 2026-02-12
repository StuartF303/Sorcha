// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Sorcha.ServiceClients.Wallet;
using Sorcha.ServiceClients.Register;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Services.Implementation;

/// <summary>
/// Service for payload encryption, decryption, and aggregation
/// </summary>
public class PayloadResolverService : IPayloadResolverService
{
    private readonly ILogger<PayloadResolverService> _logger;
    private readonly IWalletServiceClient _walletServiceClient;
    private readonly IRegisterServiceClient _registerServiceClient;

    public PayloadResolverService(
        ILogger<PayloadResolverService> logger,
        IWalletServiceClient walletServiceClient,
        IRegisterServiceClient registerServiceClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _walletServiceClient = walletServiceClient ?? throw new ArgumentNullException(nameof(walletServiceClient));
        _registerServiceClient = registerServiceClient ?? throw new ArgumentNullException(nameof(registerServiceClient));
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
                // Serialize the data to JSON bytes
                var jsonPayload = JsonSerializer.SerializeToUtf8Bytes(data);

                // Encrypt the payload using the Wallet Service
                var encryptedPayload = await _walletServiceClient.EncryptPayloadAsync(
                    wallet,
                    jsonPayload,
                    cancellationToken);

                encryptedPayloads[wallet] = encryptedPayload;

                _logger.LogDebug("Created encrypted payload for participant {ParticipantId} (wallet: {Wallet})",
                    participantId, wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt payload for participant {ParticipantId}", participantId);
                throw;
            }
        }

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
                // Get the transaction from the Register Service
                var transaction = await _registerServiceClient.GetTransactionAsync(
                    registerAddress,
                    txId,
                    cancellationToken);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TxId} not found in register {RegisterAddress}, skipping", txId, registerAddress);
                    continue;
                }

                // Find the payload for this wallet in the transaction's payloads
                var payloadForWallet = transaction.Payloads?.FirstOrDefault(p =>
                    p.WalletAccess?.Contains(wallet) == true);

                if (payloadForWallet == null)
                {
                    _logger.LogWarning("No payload found for wallet {Wallet} in transaction {TxId}, skipping", wallet, txId);
                    continue;
                }

                // Decrypt the payload using the Wallet Service
                // PayloadModel.Data is Base64 encoded string, convert to bytes
                var payloadBytes = !string.IsNullOrEmpty(payloadForWallet.Data)
                    ? Convert.FromBase64String(payloadForWallet.Data)
                    : Array.Empty<byte>();

                var decryptedPayload = await _walletServiceClient.DecryptPayloadAsync(
                    wallet,
                    payloadBytes,
                    cancellationToken);

                // Deserialize the decrypted payload
                var decryptedData = JsonSerializer.Deserialize<Dictionary<string, object>>(decryptedPayload);

                if (decryptedData != null)
                {
                    // Merge the decrypted data into aggregated data
                    foreach (var (key, value) in decryptedData)
                    {
                        // Later values overwrite earlier ones (most recent wins)
                        aggregatedData[key] = value;
                    }

                    _logger.LogDebug("Aggregated data from transaction {TxId}", txId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve/decrypt transaction {TxId}, skipping", txId);
                // Continue with other transactions
            }
        }

        // Apply disclosure rules if provided
        if (disclosureRules != null && disclosureRules.Any())
        {
            var allowedFields = new HashSet<string>(disclosureRules);
            var filteredData = new Dictionary<string, object>();

            foreach (var (key, value) in aggregatedData)
            {
                if (allowedFields.Contains(key))
                {
                    filteredData[key] = value;
                }
            }

            _logger.LogDebug("Applied disclosure rules: {OriginalCount} fields filtered to {FilteredCount} fields",
                aggregatedData.Count, filteredData.Count);

            return filteredData;
        }

        return aggregatedData;
    }

}
