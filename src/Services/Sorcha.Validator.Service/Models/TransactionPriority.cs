// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Priority level for transactions in the memory pool
/// </summary>
public enum TransactionPriority
{
    /// <summary>
    /// Low priority - processed after normal
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - default processing
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - expedited processing (limited to 10% of memory pool)
    /// </summary>
    High = 2
}
