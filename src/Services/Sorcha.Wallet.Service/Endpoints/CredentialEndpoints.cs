// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Sorcha.Blueprint.Models.Credentials;
using Sorcha.Cryptography.SdJwt;
using Sorcha.Wallet.Core.Domain.Entities;
using Sorcha.Wallet.Core.Repositories.Interfaces;
using Sorcha.Wallet.Core.Services.Interfaces;
using Sorcha.Wallet.Service.Credentials;

namespace Sorcha.Wallet.Service.Endpoints;

/// <summary>
/// REST endpoints for managing verifiable credentials in a wallet.
/// </summary>
public static class CredentialEndpoints
{
    /// <summary>
    /// Maps credential management endpoints under /api/v1/wallets/{walletAddress}/credentials.
    /// </summary>
    public static IEndpointRouteBuilder MapCredentialEndpoints(this IEndpointRouteBuilder app)
    {
        var credentialGroup = app.MapGroup("/api/v1/wallets/{walletAddress}/credentials")
            .WithTags("Credentials")
            .RequireAuthorization("CanManageWallets");

        credentialGroup.MapGet("/", ListCredentials)
            .WithName("ListCredentials")
            .WithSummary("List all credentials for a wallet")
            .WithDescription("Returns all active verifiable credentials stored in the specified wallet.");

        credentialGroup.MapGet("/{credentialId}", GetCredential)
            .WithName("GetCredential")
            .WithSummary("Get a credential by ID")
            .WithDescription("Returns a specific credential by its DID URI identifier.");

        credentialGroup.MapPost("/match", MatchCredentials)
            .WithName("MatchCredentials")
            .WithSummary("Match credentials against requirements")
            .WithDescription("Finds stored credentials that satisfy the given credential requirements.");

        credentialGroup.MapDelete("/{credentialId}", DeleteCredential)
            .WithName("DeleteCredential")
            .WithSummary("Delete a credential from wallet")
            .WithDescription("Permanently removes a credential from the wallet store.");

        credentialGroup.MapGet("/{credentialId}/export", ExportCredential)
            .WithName("ExportCredential")
            .WithSummary("Export a credential as SD-JWT VC")
            .WithDescription("Returns the raw SD-JWT VC token for use in presentations.");

        credentialGroup.MapPost("/", StoreCredential)
            .WithName("StoreCredential")
            .WithSummary("Store a credential in a wallet")
            .WithDescription("Stores a pre-issued verifiable credential in the specified wallet.");

        credentialGroup.MapPatch("/{credentialId}/status", UpdateCredentialStatus)
            .WithName("UpdateCredentialStatus")
            .WithSummary("Update a credential's status")
            .WithDescription("Updates the status of a credential (e.g., Active → Revoked).");

        credentialGroup.MapPost("/issue", IssueCredential)
            .WithName("IssueCredential")
            .WithSummary("Issue a new credential using the wallet's signing key")
            .WithDescription("Creates and signs a new SD-JWT VC credential using the wallet's private key, stores it, and returns the issued credential.");

        return app;
    }

    private static async Task<IResult> ListCredentials(
        string walletAddress,
        ICredentialStore store,
        CancellationToken cancellationToken = default)
    {
        var credentials = await store.GetByWalletAsync(walletAddress, cancellationToken);

        var response = credentials.Select(c => new
        {
            c.Id,
            c.Type,
            c.IssuerDid,
            c.SubjectDid,
            c.IssuedAt,
            c.ExpiresAt,
            c.Status
        });

        return Results.Ok(response);
    }

    private static async Task<IResult> GetCredential(
        string walletAddress,
        string credentialId,
        ICredentialStore store,
        CancellationToken cancellationToken = default)
    {
        var credential = await store.GetByIdAsync(credentialId, cancellationToken);

        if (credential == null || credential.WalletAddress != walletAddress)
            return Results.NotFound();

        return Results.Ok(credential);
    }

    private static async Task<IResult> MatchCredentials(
        string walletAddress,
        [FromBody] IEnumerable<CredentialRequirement> requirements,
        ICredentialStore store,
        CredentialMatcher matcher,
        CancellationToken cancellationToken = default)
    {
        var credentials = await store.GetByWalletAsync(walletAddress, cancellationToken);
        var matches = matcher.Match(requirements, credentials);

        var response = matches.Select(kvp => new
        {
            RequirementType = kvp.Key,
            Matched = kvp.Value != null,
            CredentialId = kvp.Value?.Id,
            IssuerDid = kvp.Value?.IssuerDid,
            ExpiresAt = kvp.Value?.ExpiresAt
        });

        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteCredential(
        string walletAddress,
        string credentialId,
        ICredentialStore store,
        CancellationToken cancellationToken = default)
    {
        var credential = await store.GetByIdAsync(credentialId, cancellationToken);

        if (credential == null || credential.WalletAddress != walletAddress)
            return Results.NotFound();

        await store.DeleteAsync(credentialId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ExportCredential(
        string walletAddress,
        string credentialId,
        ICredentialStore store,
        CancellationToken cancellationToken = default)
    {
        var credential = await store.GetByIdAsync(credentialId, cancellationToken);

        if (credential == null || credential.WalletAddress != walletAddress)
            return Results.NotFound();

        return Results.Ok(new
        {
            credential.Id,
            credential.Type,
            credential.RawToken
        });
    }

    private static async Task<IResult> StoreCredential(
        string walletAddress,
        [FromBody] StoreCredentialRequest request,
        ICredentialStore store,
        CancellationToken cancellationToken = default)
    {
        var entity = new CredentialEntity
        {
            Id = request.CredentialId,
            Type = request.Type,
            IssuerDid = request.IssuerDid,
            SubjectDid = request.SubjectDid,
            ClaimsJson = request.ClaimsJson,
            IssuedAt = request.IssuedAt,
            ExpiresAt = request.ExpiresAt,
            RawToken = request.RawToken,
            Status = "Active",
            IssuanceTxId = request.IssuanceTxId,
            IssuanceBlueprintId = request.IssuanceBlueprintId,
            WalletAddress = walletAddress,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await store.StoreAsync(entity, cancellationToken);

        return Results.Created($"/api/v1/wallets/{walletAddress}/credentials/{entity.Id}", new
        {
            entity.Id,
            entity.Type,
            entity.IssuerDid,
            entity.SubjectDid,
            entity.IssuedAt,
            entity.ExpiresAt,
            entity.Status
        });
    }

    private static async Task<IResult> UpdateCredentialStatus(
        string walletAddress,
        string credentialId,
        [FromBody] UpdateStatusRequest request,
        ICredentialStore store,
        CancellationToken cancellationToken = default)
    {
        var credential = await store.GetByIdAsync(credentialId, cancellationToken);

        if (credential == null || credential.WalletAddress != walletAddress)
            return Results.NotFound();

        var updated = await store.UpdateStatusAsync(credentialId, request.Status, cancellationToken);

        return updated
            ? Results.Ok(new { credentialId, status = request.Status })
            : Results.Problem("Failed to update credential status");
    }

    private static async Task<IResult> IssueCredential(
        string walletAddress,
        [FromBody] IssueCredentialRequest request,
        IWalletRepository walletRepository,
        IKeyManagementService keyManagement,
        ISdJwtService sdJwtService,
        ICredentialStore store,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        // 1. Get the issuer wallet
        var wallet = await walletRepository.GetByAddressAsync(walletAddress, cancellationToken: cancellationToken);
        if (wallet == null)
            return Results.NotFound(new { error = $"Wallet '{walletAddress}' not found" });

        if (wallet.Status != Sorcha.Wallet.Core.Domain.WalletStatus.Active)
            return Results.BadRequest(new { error = $"Wallet is {wallet.Status} and cannot issue credentials" });

        var logger = loggerFactory.CreateLogger("Sorcha.Wallet.Service.Endpoints.CredentialEndpoints");

        // 2. Decrypt the wallet's private key
        var privateKey = await keyManagement.DecryptPrivateKeyAsync(
            wallet.EncryptedPrivateKey, wallet.EncryptionKeyId);

        // 3. Calculate expiry
        var issuedAt = DateTimeOffset.UtcNow;
        DateTimeOffset? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(request.ExpiryDuration))
        {
            try
            {
                expiresAt = issuedAt + XmlConvert.ToTimeSpan(request.ExpiryDuration);
            }
            catch (FormatException)
            {
                expiresAt = issuedAt + TimeSpan.FromDays(365);
            }
        }

        // 4. Create SD-JWT VC token
        var claims = new Dictionary<string, object>(request.Claims)
        {
            ["type"] = request.CredentialType,
            ["vct"] = request.CredentialType
        };

        var token = await sdJwtService.CreateTokenAsync(
            claims,
            request.DisclosableClaims,
            walletAddress,
            request.RecipientWallet,
            privateKey,
            wallet.Algorithm,
            expiresAt,
            cancellationToken);

        // 5. Generate credential ID
        var credentialId = $"urn:uuid:{Guid.NewGuid()}";

        // 6. Build and store credential in issuer's wallet
        var claimsJson = JsonSerializer.Serialize(claims);
        var issuerEntity = new CredentialEntity
        {
            Id = credentialId,
            Type = request.CredentialType,
            IssuerDid = walletAddress,
            SubjectDid = request.RecipientWallet,
            ClaimsJson = claimsJson,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            RawToken = token.RawToken,
            Status = "Active",
            IssuanceBlueprintId = request.IssuanceBlueprintId,
            WalletAddress = walletAddress,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await store.StoreAsync(issuerEntity, cancellationToken);

        // 7. Store copy in recipient's wallet (if different and exists)
        if (!string.Equals(walletAddress, request.RecipientWallet, StringComparison.OrdinalIgnoreCase))
        {
            var recipientWallet = await walletRepository.GetByAddressAsync(
                request.RecipientWallet, cancellationToken: cancellationToken);
            if (recipientWallet != null)
            {
                var recipientEntity = new CredentialEntity
                {
                    Id = credentialId,
                    Type = request.CredentialType,
                    IssuerDid = walletAddress,
                    SubjectDid = request.RecipientWallet,
                    ClaimsJson = claimsJson,
                    IssuedAt = issuedAt,
                    ExpiresAt = expiresAt,
                    RawToken = token.RawToken,
                    Status = "Active",
                    IssuanceBlueprintId = request.IssuanceBlueprintId,
                    WalletAddress = request.RecipientWallet,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await store.StoreAsync(recipientEntity, cancellationToken);

                logger.LogInformation(
                    "Credential {CredentialId} stored in recipient wallet {RecipientWallet}",
                    credentialId, request.RecipientWallet);
            }
            else
            {
                logger.LogWarning(
                    "Recipient wallet {RecipientWallet} not found — credential stored only in issuer wallet",
                    request.RecipientWallet);
            }
        }

        logger.LogInformation(
            "Issued credential {CredentialId} of type {Type} from {Issuer} to {Recipient}",
            credentialId, request.CredentialType, walletAddress, request.RecipientWallet);

        return Results.Ok(new IssuedCredentialResponse
        {
            CredentialId = credentialId,
            Type = request.CredentialType,
            IssuerDid = walletAddress,
            SubjectDid = request.RecipientWallet,
            Claims = claims,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            RawToken = token.RawToken
        });
    }
}

/// <summary>
/// Request to store a pre-issued credential in a wallet.
/// </summary>
public class StoreCredentialRequest
{
    public required string CredentialId { get; init; }
    public required string Type { get; init; }
    public required string IssuerDid { get; init; }
    public required string SubjectDid { get; init; }
    public required string ClaimsJson { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public required string RawToken { get; init; }
    public string? IssuanceTxId { get; init; }
    public string? IssuanceBlueprintId { get; init; }
}

/// <summary>
/// Request to issue a new credential using the wallet's signing key.
/// </summary>
public class IssueCredentialRequest
{
    public required string CredentialType { get; init; }
    public required Dictionary<string, object> Claims { get; init; }
    public required string RecipientWallet { get; init; }
    public string? ExpiryDuration { get; init; }
    public List<string>? DisclosableClaims { get; init; }
    public string? IssuanceBlueprintId { get; init; }
}

/// <summary>
/// Request to update a credential's status.
/// </summary>
public class UpdateStatusRequest
{
    public required string Status { get; init; }
}

/// <summary>
/// Response from credential issuance.
/// </summary>
public class IssuedCredentialResponse
{
    public required string CredentialId { get; init; }
    public required string Type { get; init; }
    public required string IssuerDid { get; init; }
    public required string SubjectDid { get; init; }
    public required Dictionary<string, object> Claims { get; init; }
    public required DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public required string RawToken { get; init; }
}
