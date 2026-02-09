// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using Sorcha.Register.Models.Enums;

namespace Sorcha.Register.Models;

/// <summary>
/// Represents a sealed docket of transactions
/// </summary>
public class Docket
{
    /// <summary>
    /// Docket identifier (docket height)
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Register identifier this docket belongs to
    /// </summary>
    [Required]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Hash of previous docket for chain integrity
    /// </summary>
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of this docket
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// List of transaction IDs sealed in this docket
    /// </summary>
    public List<string> TransactionIds { get; set; } = new();

    /// <summary>
    /// Docket creation timestamp (UTC)
    /// </summary>
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current docket lifecycle state
    /// </summary>
    public DocketState State { get; set; } = DocketState.Init;

    /// <summary>
    /// Docket metadata
    /// </summary>
    public TransactionMetaData? MetaData { get; set; }

    /// <summary>
    /// Consensus votes (implementation TBD)
    /// </summary>
    public string? Votes { get; set; }
}
