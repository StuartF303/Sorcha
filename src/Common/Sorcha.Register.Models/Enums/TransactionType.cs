// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Register.Models.Enums;

/// <summary>
/// Represents the type of transaction
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Control transaction (register governance — genesis + admin operations)
    /// </summary>
    Control = 0,

    /// <summary>
    /// Action transaction (blueprint workflow action)
    /// </summary>
    Action = 1,

    /// <summary>
    /// Docket transaction (block sealing)
    /// </summary>
    Docket = 2
}
