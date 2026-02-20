// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sorcha.ServiceClients.Register.Models;

/// <summary>
/// A published participant record on a register (latest version)
/// </summary>
public class PublishedParticipantRecord
{
    public required string ParticipantId { get; init; }
    public required string OrganizationName { get; init; }
    public required string ParticipantName { get; init; }
    public required string Status { get; init; }
    public required int Version { get; init; }
    public required string LatestTxId { get; init; }
    public required List<ParticipantAddressInfo> Addresses { get; init; }
    public JsonElement? Metadata { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public List<ParticipantVersionSummary>? History { get; init; }
}

/// <summary>
/// Address entry in a published participant record
/// </summary>
public class ParticipantAddressInfo
{
    public required string WalletAddress { get; init; }
    public required string PublicKey { get; init; }
    public required string Algorithm { get; init; }
    public bool Primary { get; init; }
}

/// <summary>
/// Paginated list of published participants
/// </summary>
public class ParticipantPage
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public List<PublishedParticipantRecord> Participants { get; init; } = [];
}

/// <summary>
/// Summary of a participant version (for history)
/// </summary>
public class ParticipantVersionSummary
{
    public required int Version { get; init; }
    public required string TxId { get; init; }
    public required string Status { get; init; }
    public required string ParticipantName { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Public key resolution result
/// </summary>
public class PublicKeyResolution
{
    public required string ParticipantId { get; init; }
    public required string ParticipantName { get; init; }
    public required string WalletAddress { get; init; }
    public required string PublicKey { get; init; }
    public required string Algorithm { get; init; }
    public required string Status { get; init; }
}
