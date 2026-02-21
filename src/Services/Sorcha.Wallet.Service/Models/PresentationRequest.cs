// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Security.Cryptography;

namespace Sorcha.Wallet.Service.Models;

/// <summary>
/// Represents an OID4VP presentation request from a verifier.
/// </summary>
public class PresentationRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string VerifierIdentity { get; set; }
    public required string CredentialType { get; set; }
    public string[]? AcceptedIssuers { get; set; }
    public ClaimConstraint[]? RequiredClaims { get; set; }
    public string Nonce { get; set; } = GenerateNonce();
    public required string CallbackUrl { get; set; }
    public string? TargetWalletAddress { get; set; }
    public string Status { get; set; } = PresentationStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? VpToken { get; set; }
    public string? VerificationResult { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    private static string GenerateNonce()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexStringLower(bytes);
    }
}

/// <summary>
/// Constraint on a specific claim in a credential.
/// </summary>
public class ClaimConstraint
{
    public required string ClaimName { get; set; }
    public string? ExpectedValue { get; set; }
}

/// <summary>
/// Valid presentation request statuses.
/// </summary>
public static class PresentationStatus
{
    public const string Pending = "Pending";
    public const string Submitted = "Submitted";
    public const string Verified = "Verified";
    public const string Denied = "Denied";
    public const string Expired = "Expired";

    public static readonly HashSet<string> ValidStatuses =
        [Pending, Submitted, Verified, Denied, Expired];
}

/// <summary>
/// Result of verifying a presentation.
/// </summary>
public class VerificationResult
{
    public required bool IsValid { get; set; }
    public Dictionary<string, object>? VerifiedClaims { get; set; }
    public string? CredentialType { get; set; }
    public string? IssuerDid { get; set; }
    public string? StatusListCheck { get; set; }
    public List<VerificationError>? Errors { get; set; }
}

/// <summary>
/// A single verification failure.
/// </summary>
public class VerificationError
{
    public required string RequirementType { get; set; }
    public required string FailureReason { get; set; }
    public required string Message { get; set; }
}
