// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Manages genesis docket creation for new registers
/// </summary>
public interface IGenesisManager
{
    /// <summary>
    /// Creates a genesis docket (first docket in a register's chain)
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="transactions">Genesis transactions (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Genesis docket with DocketNumber = 0 and PreviousHash = null</returns>
    Task<Docket> CreateGenesisDocketAsync(
        string registerId,
        List<Transaction> transactions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a register needs a genesis docket
    /// </summary>
    Task<bool> NeedsGenesisDocketAsync(string registerId, CancellationToken cancellationToken = default);
}
