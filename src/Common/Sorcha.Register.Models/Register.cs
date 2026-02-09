// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using Sorcha.Register.Models.Enums;

namespace Sorcha.Register.Models;

/// <summary>
/// Represents a distributed ledger register
/// </summary>
public class Register
{
    /// <summary>
    /// Unique identifier (GUID without hyphens)
    /// </summary>
    [Required]
    [StringLength(32, MinimumLength = 32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable register name
    /// </summary>
    [Required]
    [StringLength(38, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current docket height (number of sealed dockets)
    /// </summary>
    public uint Height { get; set; }

    /// <summary>
    /// Register operational status
    /// </summary>
    public RegisterStatus Status { get; set; } = RegisterStatus.Offline;

    /// <summary>
    /// Whether register is advertised to network peers
    /// </summary>
    public bool Advertise { get; set; }

    /// <summary>
    /// Whether this node maintains full transaction history
    /// </summary>
    public bool IsFullReplica { get; set; } = true;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation
    /// </summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Register creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Consensus votes (implementation TBD)
    /// </summary>
    public string? Votes { get; set; }
}
