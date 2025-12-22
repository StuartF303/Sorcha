// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Validator's decision on a proposed docket
/// </summary>
public enum VoteDecision
{
    /// <summary>
    /// Reject the docket (validation failed)
    /// </summary>
    Reject = 0,

    /// <summary>
    /// Approve the docket (validation passed)
    /// </summary>
    Approve = 1
}
