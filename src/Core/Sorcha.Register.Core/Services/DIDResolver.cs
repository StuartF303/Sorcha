// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Register.Models;
using Sorcha.ServiceClients.Register;
using Sorcha.ServiceClients.Wallet;

namespace Sorcha.Register.Core.Services;

/// <summary>
/// Resolves Sorcha DID identifiers by dispatching to the appropriate service
/// </summary>
public class DIDResolver : IDIDResolver
{
    private readonly IWalletServiceClient _walletClient;
    private readonly IRegisterServiceClient _registerClient;
    private readonly ILogger<DIDResolver> _logger;

    public DIDResolver(
        IWalletServiceClient walletClient,
        IRegisterServiceClient registerClient,
        ILogger<DIDResolver> logger)
    {
        _walletClient = walletClient ?? throw new ArgumentNullException(nameof(walletClient));
        _registerClient = registerClient ?? throw new ArgumentNullException(nameof(registerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<DIDResolutionResult> ResolveAsync(
        string did,
        CancellationToken cancellationToken = default)
    {
        if (!SorchaDidIdentifier.TryParse(did, out var parsedDid))
        {
            return DIDResolutionResult.Failure(did, $"Invalid DID format: '{did}'");
        }

        return parsedDid.Type switch
        {
            SorchaDidType.Wallet => await ResolveWalletDidAsync(parsedDid, cancellationToken),
            SorchaDidType.Register => await ResolveRegisterDidAsync(parsedDid, cancellationToken),
            _ => DIDResolutionResult.Failure(did, $"Unsupported DID type: {parsedDid.Type}")
        };
    }

    private async Task<DIDResolutionResult> ResolveWalletDidAsync(
        SorchaDidIdentifier did, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Resolving wallet DID: {Did}", did);

        try
        {
            var wallet = await _walletClient.GetWalletAsync(did.Locator, cancellationToken);
            if (wallet == null)
            {
                return DIDResolutionResult.Failure(did.ToString(),
                    $"Wallet not found: {did.Locator}");
            }

            return DIDResolutionResult.Success(
                did.ToString(),
                wallet.PublicKey,
                wallet.Algorithm);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve wallet DID: {Did}", did);
            return DIDResolutionResult.Failure(did.ToString(),
                $"Failed to resolve wallet DID: {ex.Message}");
        }
    }

    private async Task<DIDResolutionResult> ResolveRegisterDidAsync(
        SorchaDidIdentifier did, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Resolving register DID: {Did}", did);

        try
        {
            var transaction = await _registerClient.GetTransactionAsync(
                did.Locator, did.TransactionId!, cancellationToken);

            if (transaction == null)
            {
                return DIDResolutionResult.Failure(did.ToString(),
                    $"Transaction not found: register={did.Locator}, tx={did.TransactionId}");
            }

            // Extract public key from transaction payload
            var publicKeyResult = ExtractPublicKeyFromTransaction(transaction);
            if (publicKeyResult == null)
            {
                return DIDResolutionResult.Failure(did.ToString(),
                    "Transaction does not contain a resolvable public key");
            }

            return DIDResolutionResult.Success(
                did.ToString(),
                publicKeyResult.Value.publicKey,
                publicKeyResult.Value.algorithm);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve register DID: {Did}", did);
            return DIDResolutionResult.Failure(did.ToString(),
                $"Failed to resolve register DID: {ex.Message}");
        }
    }

    private (string publicKey, string algorithm)? ExtractPublicKeyFromTransaction(TransactionModel transaction)
    {
        // For Control transactions, the payload contains a roster with attestations
        if (transaction.Payloads == null || transaction.Payloads.Length == 0)
            return null;

        try
        {
            var payloadData = transaction.Payloads[0].Data;
            if (string.IsNullOrWhiteSpace(payloadData))
                return null;

            // Smart decode: legacy Base64 (+, /, =) or Base64url
            var payloadBytes = payloadData.Contains('+') || payloadData.Contains('/') || payloadData.Contains('=')
                ? Convert.FromBase64String(payloadData)
                : System.Buffers.Text.Base64Url.DecodeFromChars(payloadData);
            var payload = JsonSerializer.Deserialize<ControlTransactionPayload>(payloadBytes,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload?.Roster?.Attestations.Count > 0)
            {
                // Return the first attestation's public key (the sender/owner)
                var attestation = payload.Roster.Attestations[0];
                return (attestation.PublicKey, attestation.Algorithm.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract public key from transaction {TxId}", transaction.TxId);
        }

        return null;
    }
}
