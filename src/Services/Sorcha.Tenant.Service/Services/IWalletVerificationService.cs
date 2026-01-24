// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Tenant.Service.Models.Dtos;

namespace Sorcha.Tenant.Service.Services;

/// <summary>
/// Service interface for wallet link verification operations.
/// </summary>
public interface IWalletVerificationService
{
    /// <summary>
    /// Initiates a wallet link challenge for a participant.
    /// Creates a challenge message that must be signed by the wallet.
    /// </summary>
    /// <param name="participantId">Participant ID.</param>
    /// <param name="request">Initiate link request with wallet address and algorithm.</param>
    /// <param name="actorId">ID of the user initiating the link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The challenge response with message to sign.</returns>
    Task<WalletLinkChallengeResponse> InitiateLinkAsync(
        Guid participantId,
        InitiateWalletLinkRequest request,
        string actorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a wallet link challenge and creates the link if successful.
    /// </summary>
    /// <param name="participantId">Participant ID.</param>
    /// <param name="challengeId">Challenge ID.</param>
    /// <param name="request">Verify request with signature and public key.</param>
    /// <param name="actorId">ID of the user verifying the link.</param>
    /// <param name="ipAddress">Client IP address for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The linked wallet address response.</returns>
    Task<LinkedWalletAddressResponse> VerifyLinkAsync(
        Guid participantId,
        Guid challengeId,
        VerifyWalletLinkRequest request,
        string actorId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists linked wallet addresses for a participant.
    /// </summary>
    /// <param name="participantId">Participant ID.</param>
    /// <param name="includeRevoked">Whether to include revoked links.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of linked wallet addresses.</returns>
    Task<List<LinkedWalletAddressResponse>> ListLinksAsync(
        Guid participantId,
        bool includeRevoked = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a wallet link (soft delete for audit trail).
    /// </summary>
    /// <param name="participantId">Participant ID.</param>
    /// <param name="linkId">Wallet link ID.</param>
    /// <param name="actorId">ID of the user revoking the link.</param>
    /// <param name="ipAddress">Client IP address for audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false if not found.</returns>
    Task<bool> RevokeLinkAsync(
        Guid participantId,
        Guid linkId,
        string actorId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pending challenge by ID.
    /// </summary>
    /// <param name="challengeId">Challenge ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The challenge response or null if not found or expired.</returns>
    Task<WalletLinkChallengeResponse?> GetChallengeAsync(
        Guid challengeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires pending challenges that have passed their expiration time.
    /// Called by background job.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of challenges expired.</returns>
    Task<int> ExpirePendingChallengesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a wallet address is already linked to any participant.
    /// </summary>
    /// <param name="walletAddress">Wallet address to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if address is already linked.</returns>
    Task<bool> IsAddressLinkedAsync(
        string walletAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active wallet links for a participant.
    /// Used to enforce max 10 addresses limit.
    /// </summary>
    /// <param name="participantId">Participant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of active links.</returns>
    Task<int> GetActiveLinksCountAsync(
        Guid participantId,
        CancellationToken cancellationToken = default);
}
