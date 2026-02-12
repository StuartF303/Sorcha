// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Engine.Models;

/// <summary>
/// Result of applying disclosure rules, containing filtered data for a specific participant.
/// </summary>
/// <remarks>
/// Disclosure results implement privacy-preserving selective data disclosure.
/// Each participant receives only the fields they are authorized to see,
/// as defined by the disclosure rules in the action.
/// 
/// Example workflow:
/// 1. Action submitted with complete data: { name, email, ssn, salary }
/// 2. Disclosure rules define:
///    - HR sees: { name, email, ssn, salary }
///    - Manager sees: { name, email }
///    - Auditor sees: { name, salary }
/// 3. Three DisclosureResults are created, one per participant,
///    each containing only their authorized fields
/// 
/// The filtered data can then be:
/// - Encrypted for the participant's public key
/// - Stored in the blockchain transaction
/// - Sent as real-time notifications
/// </remarks>
public class DisclosureResult
{
    /// <summary>
    /// The participant ID (DID) who will receive this disclosed data.
    /// </summary>
    /// <remarks>
    /// This is the participant's decentralized identifier (DID).
    /// Examples:
    /// - did:sorcha:wallet:0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb
    /// - did:key:z6MkhaXgBZDvotDkL5257faiztiGiC2QtKLGpbnnEGta2doK
    /// </remarks>
    public required string ParticipantId { get; init; }

    /// <summary>
    /// The filtered data containing only fields authorized for this participant.
    /// </summary>
    /// <remarks>
    /// This is a subset of the complete action data, containing only
    /// the fields specified in the participant's disclosure rules.
    /// 
    /// Fields are selected using JSON Pointers from the disclosure configuration.
    /// 
    /// Example:
    /// Complete data: { "name": "Alice", "email": "alice@example.com", "ssn": "123-45-6789" }
    /// Disclosure fields: ["/name", "/email"]
    /// Result: { "name": "Alice", "email": "alice@example.com" }
    /// </remarks>
    public required Dictionary<string, object> DisclosedData { get; init; }

    /// <summary>
    /// The disclosure rule that was applied (for audit purposes).
    /// </summary>
    /// <remarks>
    /// Optional reference to the disclosure configuration.
    /// Useful for debugging and audit trails.
    /// </remarks>
    public string? DisclosureId { get; init; }

    /// <summary>
    /// Creates a disclosure result.
    /// </summary>
    public static DisclosureResult Create(
        string participantId,
        Dictionary<string, object> disclosedData,
        string? disclosureId = null) => new()
    {
        ParticipantId = participantId,
        DisclosedData = disclosedData,
        DisclosureId = disclosureId
    };
}
