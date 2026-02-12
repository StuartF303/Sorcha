// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Credentials;

/// <summary>
/// Checks revocation status for a credential.
/// Implementations query the wallet store or ledger for credential status.
/// </summary>
public interface IRevocationChecker
{
    /// <summary>
    /// Checks whether a credential has been revoked.
    /// </summary>
    /// <param name="credentialId">The credential DID URI to check.</param>
    /// <param name="issuerWallet">The issuer wallet address (used to locate credential in store).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The revocation status: "Active", "Revoked", or null if unavailable.</returns>
    Task<string?> CheckRevocationStatusAsync(
        string credentialId,
        string issuerWallet,
        CancellationToken cancellationToken = default);
}
