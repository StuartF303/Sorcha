// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Register.Models.Enums;

/// <summary>
/// Represents the lifecycle state of a docket (block)
/// </summary>
public enum DocketState
{
    /// <summary>
    /// Docket is initialized but not proposed
    /// </summary>
    Init = 0,

    /// <summary>
    /// Docket has been proposed for consensus
    /// </summary>
    Proposed = 1,

    /// <summary>
    /// Docket has been accepted by consensus
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Docket has been rejected by consensus
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Docket has been sealed and is immutable
    /// </summary>
    Sealed = 4
}
