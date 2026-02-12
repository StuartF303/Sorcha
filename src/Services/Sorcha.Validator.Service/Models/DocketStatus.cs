// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Models;

/// <summary>
/// Status of a docket in the consensus process
/// </summary>
public enum DocketStatus
{
    /// <summary>
    /// Docket created, awaiting consensus
    /// </summary>
    Proposed = 0,

    /// <summary>
    /// Consensus achieved, persisted to Register Service
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Consensus failed
    /// </summary>
    Rejected = 2
}
