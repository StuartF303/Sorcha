// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Wallet.Core.Domain.Entities;

namespace Sorcha.Wallet.Service.Credentials;

/// <summary>
/// Store for managing verifiable credentials in a wallet.
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Gets all credentials for a wallet address.
    /// </summary>
    Task<IReadOnlyList<CredentialEntity>> GetByWalletAsync(string walletAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets a credential by its ID.
    /// </summary>
    Task<CredentialEntity?> GetByIdAsync(string credentialId, CancellationToken ct = default);

    /// <summary>
    /// Stores a new credential.
    /// </summary>
    Task StoreAsync(CredentialEntity credential, CancellationToken ct = default);

    /// <summary>
    /// Deletes a credential from the wallet store.
    /// </summary>
    Task<bool> DeleteAsync(string credentialId, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a credential (e.g., "Active" → "Revoked").
    /// </summary>
    Task<bool> UpdateStatusAsync(string credentialId, string status, CancellationToken ct = default);

    /// <summary>
    /// Finds credentials matching the specified type and optional filters.
    /// </summary>
    Task<IReadOnlyList<CredentialEntity>> MatchAsync(
        string walletAddress,
        string? type = null,
        IEnumerable<string>? acceptedIssuers = null,
        CancellationToken ct = default);
}
