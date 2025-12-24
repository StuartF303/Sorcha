// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Configuration for hub node detection and validation
/// </summary>
public class HubNodeConfiguration
{
    /// <summary>
    /// Explicitly set this node as a hub node
    /// </summary>
    public bool IsHubNode { get; set; } = false;

    /// <summary>
    /// Expected hostname pattern for hub nodes (e.g., "*.sorcha.dev")
    /// </summary>
    [MaxLength(255)]
    public string? ExpectedHostnamePattern { get; set; } = "*.sorcha.dev";

    /// <summary>
    /// Whether to validate hostname matches expected pattern when IsHubNode is true
    /// </summary>
    public bool ValidateHostname { get; set; } = true;

    /// <summary>
    /// List of known hub node endpoints with priorities
    /// </summary>
    public List<HubNodeEndpoint> HubNodes { get; set; } = new();
}

/// <summary>
/// Represents a hub node endpoint configuration
/// </summary>
public class HubNodeEndpoint
{
    /// <summary>
    /// Unique identifier for the hub node
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// DNS hostname of the hub node
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

    /// <summary>
    /// Whether to use TLS for the connection (default: false for local development)
    /// </summary>
    public bool EnableTls { get; set; } = false;
}
