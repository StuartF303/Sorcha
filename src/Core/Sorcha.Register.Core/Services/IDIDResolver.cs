// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Register.Core.Services;

/// <summary>
/// Result of resolving a Sorcha DID
/// </summary>
public class DIDResolutionResult
{
    /// <summary>
    /// Whether the resolution was successful
    /// </summary>
    public bool IsResolved { get; init; }

    /// <summary>
    /// Base64-encoded public key
    /// </summary>
    public string? PublicKey { get; init; }

    /// <summary>
    /// Signature algorithm (e.g., ED25519, NISTP256, RSA4096)
    /// </summary>
    public string? Algorithm { get; init; }

    /// <summary>
    /// The original DID that was resolved
    /// </summary>
    public string Did { get; init; } = string.Empty;

    /// <summary>
    /// Error message if resolution failed
    /// </summary>
    public string? Error { get; init; }

    public static DIDResolutionResult Success(string did, string publicKey, string algorithm) =>
        new() { IsResolved = true, Did = did, PublicKey = publicKey, Algorithm = algorithm };

    public static DIDResolutionResult Failure(string did, string error) =>
        new() { IsResolved = false, Did = did, Error = error };
}

/// <summary>
/// Resolves Sorcha DID identifiers to their associated public keys
/// </summary>
public interface IDIDResolver
{
    /// <summary>
    /// Resolves a Sorcha DID to its public key and algorithm
    /// </summary>
    /// <param name="did">DID string (did:sorcha:w:* or did:sorcha:r:*:t:*)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolution result with public key, or error</returns>
    Task<DIDResolutionResult> ResolveAsync(
        string did,
        CancellationToken cancellationToken = default);
}
