// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Serialization;
using Sorcha.Register.Models.Enums;

namespace Sorcha.Register.Models;

/// <summary>
/// Payload model for a Participant transaction. Stored as JSON within the transaction's payload field.
/// Represents a discoverable identity assertion published to a register.
/// </summary>
public class ParticipantRecord
{
    /// <summary>
    /// Immutable identity anchor — system-generated UUID on first publication,
    /// carried unchanged in all subsequent versions
    /// </summary>
    [JsonPropertyName("participantId")]
    public required string ParticipantId { get; init; }

    /// <summary>
    /// Organization publishing this participant (informational, can change across versions)
    /// </summary>
    [JsonPropertyName("organizationName")]
    public required string OrganizationName { get; init; }

    /// <summary>
    /// Published display name for this participant (informational, can change across versions)
    /// </summary>
    [JsonPropertyName("participantName")]
    public required string ParticipantName { get; init; }

    /// <summary>
    /// Lifecycle status of this participant version
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ParticipantRecordStatus Status { get; init; }

    /// <summary>
    /// Version number — incremented on each update, highest wins
    /// </summary>
    [JsonPropertyName("version")]
    public required int Version { get; init; }

    /// <summary>
    /// Cryptographic addresses for this participant (1–10 entries required)
    /// </summary>
    [JsonPropertyName("addresses")]
    public required List<ParticipantAddress> Addresses { get; init; }

    /// <summary>
    /// Optional extensible metadata (description, links, capabilities)
    /// </summary>
    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; init; }
}

/// <summary>
/// A cryptographic address entry within a participant record
/// </summary>
public class ParticipantAddress
{
    /// <summary>
    /// Wallet address string — unique per register across active participants
    /// </summary>
    [JsonPropertyName("walletAddress")]
    public required string WalletAddress { get; init; }

    /// <summary>
    /// Base64-encoded public key for this address
    /// </summary>
    [JsonPropertyName("publicKey")]
    public required string PublicKey { get; init; }

    /// <summary>
    /// Cryptographic algorithm identifier (ED25519, P-256, RSA-4096)
    /// </summary>
    [JsonPropertyName("algorithm")]
    public required string Algorithm { get; init; }

    /// <summary>
    /// Whether this is the primary/default address for the participant.
    /// If no address is marked primary, the first in the array is the default.
    /// </summary>
    [JsonPropertyName("primary")]
    public bool Primary { get; init; }
}
