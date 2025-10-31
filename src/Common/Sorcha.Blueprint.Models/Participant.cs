// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// A participant in a Blueprint workflow
/// </summary>
public class Participant : IEquatable<Participant>
{
    /// <summary>
    /// Unique identifier for the participant
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.Key]
    [DataAnnotations.MaxLength(64)]
    [Json.Schema.Generation.Required]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Friendly name for the participant
    /// </summary>
    [DataAnnotations.Required(ErrorMessage = "Participant name is required.")]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(100)]
    [Json.Schema.Generation.Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization the participant belongs to
    /// </summary>
    [DataAnnotations.Required(ErrorMessage = "Organization is required.")]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("organisation")]
    public string Organisation { get; set; } = string.Empty;

    /// <summary>
    /// Wallet address of the participant
    /// </summary>
    [DataAnnotations.MaxLength(100)]
    [Json.Schema.Generation.Required]
    [JsonPropertyName("walletAddress")]
    public string WalletAddress { get; set; } = string.Empty;

    /// <summary>
    /// Decentralized Identifier (DID) URI for the participant
    /// </summary>
    [DataAnnotations.MaxLength(200)]
    [JsonPropertyName("didUri")]
    public string? DidUri { get; set; }

    /// <summary>
    /// Whether to use a stealth address for privacy
    /// </summary>
    [JsonPropertyName("useStealthAddress")]
    public bool UseStealthAddress { get; set; } = false;

    public override bool Equals(object? obj) => Equals(obj as Participant);

    public bool Equals(Participant? other)
    {
        return other != null && Id == other.Id && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name);
    }
}
