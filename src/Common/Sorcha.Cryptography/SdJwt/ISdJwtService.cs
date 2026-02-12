// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sorcha.Cryptography.SdJwt;

/// <summary>
/// Service for creating, verifying, and presenting SD-JWT VC tokens
/// per RFC 9901 (SD-JWT) and the SD-JWT VC profile.
/// </summary>
public interface ISdJwtService
{
    /// <summary>
    /// Creates a new SD-JWT VC token with selective disclosure support.
    /// </summary>
    /// <param name="claims">All credential claims to include.</param>
    /// <param name="disclosableClaims">Claim names that support selective disclosure. Null = all disclosable.</param>
    /// <param name="issuer">Issuer identifier (DID URI or wallet address).</param>
    /// <param name="subject">Subject identifier (DID URI or wallet address).</param>
    /// <param name="signingKey">Private key bytes for signing (algorithm determined by key type).</param>
    /// <param name="algorithm">Signing algorithm (e.g., "EdDSA", "ES256", "RS256").</param>
    /// <param name="expiresAt">Optional expiration timestamp.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created SD-JWT token with all disclosures.</returns>
    Task<SdJwtToken> CreateTokenAsync(
        Dictionary<string, object> claims,
        IEnumerable<string>? disclosableClaims,
        string issuer,
        string subject,
        byte[] signingKey,
        string algorithm,
        DateTimeOffset? expiresAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an SD-JWT token's signature, structure, and extracts all disclosed claims.
    /// </summary>
    /// <param name="rawToken">The serialized SD-JWT token.</param>
    /// <param name="issuerPublicKey">Issuer's public key for signature verification.</param>
    /// <param name="algorithm">Expected signing algorithm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result with extracted claims.</returns>
    Task<SdJwtVerificationResult> VerifyTokenAsync(
        string rawToken,
        byte[] issuerPublicKey,
        string algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a presentation from an SD-JWT token, disclosing only selected claims.
    /// </summary>
    /// <param name="rawToken">The complete SD-JWT token.</param>
    /// <param name="claimsToDisclose">Claim names to reveal in the presentation.</param>
    /// <param name="holderKey">Holder's private key for key binding proof (optional).</param>
    /// <param name="audience">Intended verifier (for key binding JWT).</param>
    /// <param name="nonce">Nonce for key binding JWT replay prevention.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SD-JWT presentation with selected disclosures.</returns>
    Task<SdJwtPresentation> CreatePresentationAsync(
        string rawToken,
        IEnumerable<string> claimsToDisclose,
        byte[]? holderKey = null,
        string? audience = null,
        string? nonce = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an SD-JWT presentation, extracting only the disclosed claims.
    /// </summary>
    /// <param name="rawPresentation">The serialized SD-JWT presentation.</param>
    /// <param name="issuerPublicKey">Issuer's public key for signature verification.</param>
    /// <param name="algorithm">Expected signing algorithm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification result with only disclosed claims.</returns>
    Task<SdJwtVerificationResult> VerifyPresentationAsync(
        string rawPresentation,
        byte[] issuerPublicKey,
        string algorithm,
        CancellationToken cancellationToken = default);
}
