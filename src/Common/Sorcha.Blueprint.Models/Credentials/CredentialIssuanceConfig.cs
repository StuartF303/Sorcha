// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// Defines how a blueprint action mints a verifiable credential upon execution.
/// </summary>
public class CredentialIssuanceConfig
{
    /// <summary>
    /// Type of credential to issue (e.g., "LicenseCredential").
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("credentialType")]
    public string CredentialType { get; set; } = string.Empty;

    /// <summary>
    /// Maps action data fields to credential claims.
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [JsonPropertyName("claimMappings")]
    public IEnumerable<ClaimMapping> ClaimMappings { get; set; } = [];

    /// <summary>
    /// Participant ID who receives the credential.
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(100)]
    [JsonPropertyName("recipientParticipantId")]
    public string RecipientParticipantId { get; set; } = string.Empty;

    /// <summary>
    /// How long the credential is valid (ISO 8601 duration, e.g., "P365D" = 1 year).
    /// Null means no expiry.
    /// </summary>
    [JsonPropertyName("expiryDuration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpiryDuration { get; set; }

    /// <summary>
    /// If set, records the credential on this register for public queryability
    /// (e.g., a "Register of Licenses").
    /// </summary>
    [DataAnnotations.MaxLength(100)]
    [JsonPropertyName("registerId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegisterId { get; set; }

    /// <summary>
    /// Claim names that support selective disclosure. Null means all claims are disclosable.
    /// </summary>
    [JsonPropertyName("disclosable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Disclosable { get; set; }
}
