// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.UI.Core.Models.Credentials;

namespace Sorcha.UI.Core.Services.Credentials;

/// <summary>
/// Service interface for interacting with the Wallet Service credential endpoints.
/// </summary>
public interface ICredentialApiService
{
    /// <summary>
    /// Gets all credentials for a wallet address.
    /// </summary>
    Task<List<CredentialCardViewModel>> GetCredentialsAsync(
        string walletAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed credential information by ID.
    /// </summary>
    Task<CredentialDetailViewModel?> GetCredentialDetailAsync(
        string walletAddress, string credentialId, CancellationToken ct = default);

    /// <summary>
    /// Updates a credential's status (e.g., "Active" → "Revoked").
    /// </summary>
    Task<bool> UpdateCredentialStatusAsync(
        string walletAddress, string credentialId, string newStatus, CancellationToken ct = default);

    /// <summary>
    /// Deletes a credential from the wallet.
    /// </summary>
    Task<bool> DeleteCredentialAsync(
        string walletAddress, string credentialId, CancellationToken ct = default);

    /// <summary>
    /// Gets pending presentation requests targeting a wallet address.
    /// </summary>
    Task<List<PresentationRequestViewModel>> GetPresentationRequestsAsync(
        string walletAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific presentation request with matching credentials.
    /// </summary>
    Task<PresentationRequestViewModel?> GetPresentationRequestDetailAsync(
        string requestId, CancellationToken ct = default);

    /// <summary>
    /// Submits a presentation (approve) for a request.
    /// </summary>
    Task<PresentationSubmitResult> SubmitPresentationAsync(
        string requestId, string credentialId, List<string> disclosedClaims,
        string vpToken, CancellationToken ct = default);

    /// <summary>
    /// Denies a presentation request.
    /// </summary>
    Task<bool> DenyPresentationAsync(string requestId, CancellationToken ct = default);
}
