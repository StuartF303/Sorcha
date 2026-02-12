// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// Submitted by a participant at action time to satisfy credential requirements.
/// Contains the credential presentation with selective disclosure.
/// </summary>
public class CredentialPresentation
{
    /// <summary>
    /// DID URI of the credential being presented (e.g., "did:sorcha:credential:abc123").
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(500)]
    [JsonPropertyName("credentialId")]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// Only the claims revealed via selective disclosure.
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("disclosedClaims")]
    public Dictionary<string, object> DisclosedClaims { get; set; } = new();

    /// <summary>
    /// The SD-JWT presentation token (JWT~disclosure1~disclosure2~KB-JWT).
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [JsonPropertyName("rawPresentation")]
    public string RawPresentation { get; set; } = string.Empty;

    /// <summary>
    /// Key binding JWT proving the presenter holds the credential key.
    /// </summary>
    [JsonPropertyName("keyBindingProof")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KeyBindingProof { get; set; }
}
