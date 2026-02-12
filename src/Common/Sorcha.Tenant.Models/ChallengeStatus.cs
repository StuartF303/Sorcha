// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Models;

/// <summary>
/// Status of a wallet link verification challenge.
/// </summary>
public enum ChallengeStatus
{
    /// <summary>
    /// Challenge is awaiting signature verification.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Challenge was successfully verified and wallet linked.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Challenge time limit exceeded (5 minutes).
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Signature verification failed.
    /// </summary>
    Failed = 3
}
