// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.ComponentModel.DataAnnotations;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Represents a push notification for new blueprint publication
/// </summary>
public class BlueprintNotification
{
    /// <summary>
    /// Identifier of newly published blueprint
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>
    /// System register version of this blueprint
    /// </summary>
    [Required]
    public long Version { get; set; }

    /// <summary>
    /// Unix timestamp (milliseconds) of publication
    /// </summary>
    [Required]
    public long PublishedAt { get; set; }

    /// <summary>
    /// Identity of publisher
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string PublishedBy { get; set; } = string.Empty;

    /// <summary>
    /// Optional small metadata preview (max 4KB)
    /// </summary>
    public byte[]? BlueprintSummary { get; set; }

    /// <summary>
    /// Type of notification
    /// </summary>
    [Required]
    public NotificationType Type { get; set; } = NotificationType.BlueprintPublished;
}
