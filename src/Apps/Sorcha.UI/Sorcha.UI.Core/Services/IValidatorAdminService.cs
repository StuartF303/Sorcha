// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.UI.Core.Models.Admin;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Service for fetching validator/mempool status for admin display.
/// </summary>
public interface IValidatorAdminService
{
    /// <summary>
    /// Gets overall mempool status across all registers.
    /// </summary>
    Task<ValidatorStatusViewModel> GetMempoolStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets mempool statistics for a specific register.
    /// </summary>
    Task<RegisterMempoolStat> GetRegisterMempoolAsync(string registerId, CancellationToken cancellationToken = default);
}
