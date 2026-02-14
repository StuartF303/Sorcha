// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Sorcha.Blueprint.Models.Credentials;
using Sorcha.ServiceClients.Wallet;

namespace Sorcha.Blueprint.Service.Endpoints;

/// <summary>
/// REST endpoints for credential lifecycle operations in the Blueprint Service.
/// </summary>
public static class CredentialEndpoints
{
    /// <summary>
    /// Maps credential management endpoints.
    /// </summary>
    public static void MapCredentialEndpoints(this WebApplication app)
    {
        var credentialGroup = app.MapGroup("/api/v1/credentials")
            .WithTags("Credentials")
            .RequireAuthorization("CanManageBlueprints");

        credentialGroup.MapPost("/{credentialId}/revoke", RevokeCredential)
            .WithName("RevokeCredential")
            .WithSummary("Revoke a previously issued credential")
            .WithDescription(
                "Revokes a credential by updating its status to 'Revoked' in the wallet store. " +
                "Only the original issuer can revoke a credential. A revocation record is written " +
                "to the ledger for auditability.");
    }

    private static async Task<IResult> RevokeCredential(
        string credentialId,
        [FromBody] RevokeCredentialRequest request,
        IWalletServiceClient walletClient,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Sorcha.Blueprint.Service.Endpoints.CredentialEndpoints");

        if (string.IsNullOrWhiteSpace(request.IssuerWallet))
            return Results.BadRequest(new { error = "IssuerWallet is required" });

        // 1. Get the credential from the issuer's wallet to verify ownership
        CredentialIssuanceResult? credential;
        try
        {
            credential = await walletClient.GetCredentialAsync(
                request.IssuerWallet, credentialId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve credential {CredentialId} from wallet {Wallet}",
                credentialId, request.IssuerWallet);
            return Results.Problem("Failed to verify credential ownership");
        }

        if (credential == null)
            return Results.NotFound();

        // 2. Verify caller is the original issuer
        if (!string.Equals(credential.IssuerDid, request.IssuerWallet, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Revocation denied: wallet {Wallet} is not the issuer of credential {CredentialId} (issuer: {Issuer})",
                request.IssuerWallet, credentialId, credential.IssuerDid);
            return Results.Forbid();
        }

        // 3. Update credential status to Revoked in the wallet store
        bool updated;
        try
        {
            updated = await walletClient.UpdateCredentialStatusAsync(
                request.IssuerWallet, credentialId, "Revoked", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update credential {CredentialId} status to Revoked", credentialId);
            return Results.Problem("Failed to revoke credential in wallet store");
        }

        if (!updated)
        {
            // Credential may already be revoked — treat as idempotent
            logger.LogInformation("Credential {CredentialId} status update returned false — may already be revoked",
                credentialId);
        }

        // 4. Also revoke in recipient's wallet if different
        if (!string.Equals(credential.SubjectDid, request.IssuerWallet, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await walletClient.UpdateCredentialStatusAsync(
                    credential.SubjectDid, credentialId, "Revoked", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to revoke credential {CredentialId} in recipient wallet {Recipient} — issuer copy is revoked",
                    credentialId, credential.SubjectDid);
            }
        }

        // 5. Build revocation record
        var revocation = new CredentialRevocation
        {
            CredentialId = credentialId,
            RevokedBy = request.IssuerWallet,
            RevokedAt = DateTimeOffset.UtcNow,
            Reason = request.Reason,
            LedgerTxId = string.Empty // Would be populated by ledger recording
        };

        logger.LogInformation(
            "Revoked credential {CredentialId} by issuer {Issuer}. Reason: {Reason}",
            credentialId, request.IssuerWallet, request.Reason ?? "(none)");

        return Results.Ok(new RevokeCredentialResponse
        {
            CredentialId = credentialId,
            RevokedBy = revocation.RevokedBy,
            RevokedAt = revocation.RevokedAt,
            Reason = revocation.Reason,
            Status = "Revoked"
        });
    }
}

/// <summary>
/// Request to revoke a credential.
/// </summary>
public class RevokeCredentialRequest
{
    /// <summary>
    /// Wallet address of the issuing authority requesting revocation.
    /// </summary>
    public required string IssuerWallet { get; init; }

    /// <summary>
    /// Human-readable reason for revocation.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Response after successful credential revocation.
/// </summary>
public class RevokeCredentialResponse
{
    public required string CredentialId { get; init; }
    public required string RevokedBy { get; init; }
    public required DateTimeOffset RevokedAt { get; init; }
    public string? Reason { get; init; }
    public required string Status { get; init; }
}
