// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Configuration for central node detection and validation
/// </summary>
public class CentralNodeConfiguration
{
    /// <summary>
    /// Explicitly set this node as a central node
    /// </summary>
    public bool IsCentralNode { get; set; } = false;

    /// <summary>
    /// Expected hostname pattern for central nodes (e.g., "*.sorcha.dev")
    /// </summary>
    [MaxLength(255)]
    public string? ExpectedHostnamePattern { get; set; } = "*.sorcha.dev";

    /// <summary>
    /// Whether to validate hostname matches expected pattern when IsCentralNode is true
    /// </summary>
    public bool ValidateHostname { get; set; } = true;

    /// <summary>
    /// List of known central node endpoints with priorities
    /// </summary>
    public List<CentralNodeEndpoint> CentralNodes { get; set; } = new()
    {
        new() { NodeId = "n0.sorcha.dev", Hostname = "n0.sorcha.dev", Port = 5000, Priority = 0 },
        new() { NodeId = "n1.sorcha.dev", Hostname = "n1.sorcha.dev", Port = 5000, Priority = 1 },
        new() { NodeId = "n2.sorcha.dev", Hostname = "n2.sorcha.dev", Port = 5000, Priority = 2 }
    };
}

/// <summary>
/// Represents a central node endpoint configuration
/// </summary>
public class CentralNodeEndpoint
{
    /// <summary>
    /// Unique identifier for the central node
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// DNS hostname of the central node
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    /// gRPC port for peer connections
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Connection priority (0 = highest, try first)
    /// </summary>
    [Range(0, 2)]
    public int Priority { get; set; }
}
