// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// Specifies what credential(s) a participant must present to execute a blueprint action.
/// </summary>
public class CredentialRequirement
{
    /// <summary>
    /// Credential type identifier (e.g., "LicenseCredential", "IdentityAttestation").
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// List of accepted issuer DIDs or wallet addresses. Empty means any issuer is accepted.
    /// </summary>
    [JsonPropertyName("acceptedIssuers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? AcceptedIssuers { get; set; }

    /// <summary>
    /// Claims that must be disclosed and their value constraints.
    /// </summary>
    [JsonPropertyName("requiredClaims")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ClaimConstraint>? RequiredClaims { get; set; }

    /// <summary>
    /// Policy for handling revocation check failures. Defaults to FailClosed.
    /// </summary>
    [JsonPropertyName("revocationCheckPolicy")]
    public RevocationCheckPolicy RevocationCheckPolicy { get; set; } = RevocationCheckPolicy.FailClosed;

    /// <summary>
    /// Human-readable description of what credential is needed (displayed in UI).
    /// </summary>
    [DataAnnotations.MaxLength(500)]
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}
