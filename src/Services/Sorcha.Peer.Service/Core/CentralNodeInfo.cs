// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Configuration and runtime state for a central node endpoint
/// </summary>
public class CentralNodeInfo
{
    private static readonly Regex HostnamePattern = new(@"^n[0-2]\.sorcha\.dev$", RegexOptions.Compiled);

    /// <summary>
    /// Unique identifier for the central node (matches hostname)
    /// </summary>
    [Required]
    [MaxLength(64)]
    [RegularExpression(@"^n[0-2]\.sorcha\.dev$", ErrorMessage = "Central node hostname must match pattern: n0.sorcha.dev, n1.sorcha.dev, or n2.sorcha.dev")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// DNS hostname of central node (must be n0/n1/n2.sorcha.dev)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Hostname { get; set; } = string.Empty;

    /// <summary>
    /// gRPC port for peer connections
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Connection priority (0 = highest, try first)
    /// </summary>
    [Required]
    [Range(0, 2)]
    public int Priority { get; set; }

    /// <summary>
    /// Current connection state
    /// </summary>
    [Required]
    public CentralNodeConnectionStatus ConnectionStatus { get; set; } = CentralNodeConnectionStatus.Disconnected;

    /// <summary>
    /// Timestamp of last connection attempt (UTC)
    /// </summary>
    public DateTime? LastConnectionAttempt { get; set; }

    /// <summary>
    /// Timestamp of last successful connection (UTC)
    /// </summary>
    public DateTime? LastSuccessfulConnection { get; set; }

    /// <summary>
    /// Timestamp of last heartbeat sent (UTC)
    /// </summary>
    public DateTime? LastHeartbeatSent { get; set; }

    /// <summary>
    /// Timestamp of last heartbeat acknowledged by central node (UTC)
    /// </summary>
    public DateTime? LastHeartbeatAcknowledged { get; set; }

    /// <summary>
    /// Number of consecutive connection failures
    /// </summary>
    public int ConsecutiveFailures { get; set; } = 0;

    /// <summary>
    /// Whether this is the actively connected central node (only one can be true)
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Computed gRPC channel address
    /// </summary>
    public string GrpcChannelAddress => $"https://{Hostname}:{Port}";

    /// <summary>
    /// Validates central node hostname pattern
    /// </summary>
    /// <param name="hostname">Hostname to validate</param>
    /// <returns>True if hostname matches pattern</returns>
    public static bool IsValidHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return false;

        return HostnamePattern.IsMatch(hostname);
    }

    /// <summary>
    /// Resets connection state (called on successful connection)
    /// </summary>
    public void ResetConnectionState()
    {
        ConsecutiveFailures = 0;
        ConnectionStatus = CentralNodeConnectionStatus.Connected;
        LastSuccessfulConnection = DateTime.UtcNow;
    }

    /// <summary>
    /// Records connection failure
    /// </summary>
    public void RecordFailure()
    {
        ConsecutiveFailures++;
        ConnectionStatus = CentralNodeConnectionStatus.Failed;
        LastConnectionAttempt = DateTime.UtcNow;
    }
}
