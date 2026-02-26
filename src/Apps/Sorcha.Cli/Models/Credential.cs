// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Cli.Models;

/// <summary>
/// Verifiable credential summary.
/// </summary>
public class CredentialSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("issuedAt")]
    public DateTimeOffset IssuedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>
/// Full verifiable credential detail.
/// </summary>
public class CredentialDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("issuedAt")]
    public DateTimeOffset IssuedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonPropertyName("claims")]
    public Dictionary<string, string> Claims { get; set; } = new();

    [JsonPropertyName("proof")]
    public string Proof { get; set; } = string.Empty;
}

/// <summary>
/// Request to issue a verifiable credential.
/// </summary>
public class IssueCredentialRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("claims")]
    public Dictionary<string, string> Claims { get; set; } = new();

    [JsonPropertyName("walletAddress")]
    public string WalletAddress { get; set; } = string.Empty;

    [JsonPropertyName("expiresInDays")]
    public int? ExpiresInDays { get; set; }
}

/// <summary>
/// Request to present a verifiable credential.
/// </summary>
public class PresentCredentialRequest
{
    [JsonPropertyName("credentialId")]
    public string CredentialId { get; set; } = string.Empty;

    [JsonPropertyName("verifierAddress")]
    public string VerifierAddress { get; set; } = string.Empty;

    [JsonPropertyName("selectedClaims")]
    public List<string>? SelectedClaims { get; set; }
}

/// <summary>
/// Response from credential presentation.
/// </summary>
public class PresentCredentialResponse
{
    [JsonPropertyName("presentationId")]
    public string PresentationId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("verifiedAt")]
    public DateTimeOffset? VerifiedAt { get; set; }
}

/// <summary>
/// Request to verify a credential.
/// </summary>
public class VerifyCredentialRequest
{
    [JsonPropertyName("credentialId")]
    public string CredentialId { get; set; } = string.Empty;
}

/// <summary>
/// Response from credential verification.
/// </summary>
public class VerifyCredentialResponse
{
    [JsonPropertyName("credentialId")]
    public string CredentialId { get; set; } = string.Empty;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("verifiedAt")]
    public DateTimeOffset VerifiedAt { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Credential status response.
/// </summary>
public class CredentialStatusResponse
{
    [JsonPropertyName("credentialId")]
    public string CredentialId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("isRevoked")]
    public bool IsRevoked { get; set; }

    [JsonPropertyName("revokedAt")]
    public DateTimeOffset? RevokedAt { get; set; }

    [JsonPropertyName("revokedReason")]
    public string? RevokedReason { get; set; }
}
