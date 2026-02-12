// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// A ledger record indicating that a specific credential has been revoked by its issuer.
/// </summary>
public class CredentialRevocation
{
    /// <summary>
    /// DID URI of the revoked credential.
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(500)]
    [JsonPropertyName("credentialId")]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// DID URI or wallet address of the revoking authority.
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("revokedBy")]
    public string RevokedBy { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of revocation.
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("revokedAt")]
    public DateTimeOffset RevokedAt { get; set; }

    /// <summary>
    /// Human-readable reason for revocation.
    /// </summary>
    [DataAnnotations.MaxLength(1000)]
    [JsonPropertyName("reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    /// <summary>
    /// Transaction ID on the register that recorded the revocation.
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [JsonPropertyName("ledgerTxId")]
    public string LedgerTxId { get; set; } = string.Empty;
}
