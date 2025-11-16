// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Register.Models.Enums;

/// <summary>
/// Represents the operational status of a register
/// </summary>
public enum RegisterStatus
{
    /// <summary>
    /// Register is offline and not processing transactions
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Register is online and operational
    /// </summary>
    Online = 1,

    /// <summary>
    /// Register is being checked for integrity
    /// </summary>
    Checking = 2,

    /// <summary>
    /// Register is in recovery mode
    /// </summary>
    Recovery = 3
}
