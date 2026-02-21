// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// W3C Bitstring Status List Entry — embedded in a VC to reference the issuer's status list.
/// </summary>
public class CredentialStatusClaim
{
    /// <summary>
    /// Status entry identifier: status list URL + "#" + index.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Type constant: "BitstringStatusListEntry".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "BitstringStatusListEntry";

    /// <summary>
    /// Which bitstring this entry references: "revocation" or "suspension".
    /// </summary>
    [JsonPropertyName("statusPurpose")]
    public required string StatusPurpose { get; set; }

    /// <summary>
    /// Zero-based position in the bitstring (serialized as string per W3C spec).
    /// </summary>
    [JsonPropertyName("statusListIndex")]
    public required string StatusListIndex { get; set; }

    /// <summary>
    /// URL of the status list credential endpoint.
    /// </summary>
    [JsonPropertyName("statusListCredential")]
    public required string StatusListCredential { get; set; }
}
