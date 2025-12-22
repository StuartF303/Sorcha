// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Builds dockets from pending transactions with hybrid triggering (time OR size)
/// </summary>
public interface IDocketBuilder
{
    /// <summary>
    /// Builds a docket from pending transactions in the memory pool
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="forceBuild">Force build even if thresholds not met (for manual triggers)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Built docket, or null if no transactions available or thresholds not met</returns>
    Task<Docket?> BuildDocketAsync(
        string registerId,
        bool forceBuild = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a register is ready for docket building based on hybrid triggers
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="lastBuildTime">Time of last docket build</param>
    /// <returns>True if time threshold OR size threshold is met</returns>
    Task<bool> ShouldBuildDocketAsync(
        string registerId,
        DateTimeOffset lastBuildTime,
        CancellationToken cancellationToken = default);
}
