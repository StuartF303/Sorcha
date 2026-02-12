// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Configuration for seed nodes used for initial network bootstrap.
/// Seed nodes are well-known peers that help new nodes discover the network —
/// they have no special authority. All nodes are equal P2P participants.
/// </summary>
public class SeedNodeConfiguration
{
    /// <summary>
    /// List of seed node endpoints for initial peer discovery
    /// </summary>
    public List<SeedNodeEndpoint> SeedNodes { get; set; } = new();
}

/// <summary>
/// A seed node endpoint used for bootstrap discovery
/// </summary>
public class SeedNodeEndpoint
{
    /// <summary>
    /// Unique identifier for the seed node
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// DNS hostname or IP address of the seed node
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
    /// Whether to use TLS for the connection
    /// </summary>
    public bool EnableTls { get; set; } = false;

    /// <summary>
    /// Computed gRPC channel address
    /// </summary>
    public string GrpcChannelAddress => $"{(EnableTls ? "https" : "http")}://{Hostname}:{Port}";
}
