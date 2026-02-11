// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Validates governance rights for Control transactions by checking the submitter's
/// role in the register's admin roster. Zero Tenant Service dependency — roster is
/// reconstructed from the Control transaction chain.
/// </summary>
public interface IRightsEnforcementService
{
    /// <summary>
    /// Validates that the submitter of a Control transaction has the required governance
    /// rights (Owner or Admin role in the register's roster).
    /// </summary>
    /// <param name="transaction">The transaction to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with Permission-category errors if rights check fails</returns>
    Task<ValidationEngineResult> ValidateGovernanceRightsAsync(
        Transaction transaction,
        CancellationToken ct = default);
}
