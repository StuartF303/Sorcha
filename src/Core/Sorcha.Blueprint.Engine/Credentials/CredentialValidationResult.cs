// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models.Credentials;

namespace Sorcha.Blueprint.Engine.Credentials;

/// <summary>
/// Result of verifying credential presentations against action requirements.
/// </summary>
public class CredentialValidationResult
{
    /// <summary>
    /// Whether all credential requirements are satisfied.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Specific failure details for each unmet requirement.
    /// </summary>
    public List<CredentialValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Successfully verified credentials with their disclosed claims.
    /// </summary>
    public List<VerifiedCredentialDetail> VerifiedCredentials { get; set; } = new();

    /// <summary>
    /// Non-fatal warnings (e.g., revocation check unavailable with fail-open policy).
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Detail record for a successfully verified credential.
/// </summary>
public class VerifiedCredentialDetail
{
    /// <summary>
    /// DID URI of the verified credential.
    /// </summary>
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// Credential type (e.g., "LicenseCredential").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// DID URI or wallet address of the issuer.
    /// </summary>
    public string IssuerDid { get; set; } = string.Empty;

    /// <summary>
    /// Only the claims that were disclosed and verified.
    /// </summary>
    public Dictionary<string, object> VerifiedClaims { get; set; } = new();

    /// <summary>
    /// Whether the cryptographic signature validated successfully.
    /// </summary>
    public bool SignatureValid { get; set; }

    /// <summary>
    /// Revocation status: "Active", "Revoked", or "Unknown".
    /// </summary>
    public string RevocationStatus { get; set; } = "Active";
}
