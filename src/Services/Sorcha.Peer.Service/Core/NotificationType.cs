// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Type of blueprint notification
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// New blueprint published
    /// </summary>
    BlueprintPublished = 0,

    /// <summary>
    /// Existing blueprint updated (new version)
    /// </summary>
    BlueprintUpdated = 1,

    /// <summary>
    /// Blueprint deprecated/withdrawn
    /// </summary>
    BlueprintDeprecated = 2
}
