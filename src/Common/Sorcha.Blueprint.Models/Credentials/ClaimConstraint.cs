// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// Defines a required claim and optional value constraint within a credential requirement.
/// </summary>
public class ClaimConstraint
{
    /// <summary>
    /// The claim key that must be present in the credential (e.g., "licenseType", "skillLevel").
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("claimName")]
    public string ClaimName { get; set; } = string.Empty;

    /// <summary>
    /// Expected value for the claim. Null means any value is accepted (claim just must be present).
    /// </summary>
    [JsonPropertyName("expectedValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ExpectedValue { get; set; }
}
