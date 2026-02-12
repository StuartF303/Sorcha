// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// Maps an action data field to a credential claim during credential issuance.
/// </summary>
public class ClaimMapping
{
    /// <summary>
    /// The claim key in the issued credential (e.g., "licenseType").
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("claimName")]
    public string ClaimName { get; set; } = string.Empty;

    /// <summary>
    /// JSON Pointer to the action data field (e.g., "/licenseType", "/applicant/name").
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(500)]
    [JsonPropertyName("sourceField")]
    public string SourceField { get; set; } = string.Empty;
}
