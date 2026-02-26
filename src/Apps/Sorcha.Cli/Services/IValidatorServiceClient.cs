// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Refit;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Refit client interface for the Validator Service API.
/// </summary>
public interface IValidatorServiceClient
{
    /// <summary>
    /// Gets the current validator status.
    /// </summary>
    [Get("/api/validator/status")]
    Task<ValidatorStatus> GetStatusAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Starts the validator service.
    /// </summary>
    [Post("/api/validator/start")]
    Task<ValidatorActionResponse> StartAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Stops the validator service.
    /// </summary>
    [Post("/api/validator/stop")]
    Task<ValidatorActionResponse> StopAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Triggers processing of pending transactions for a register.
    /// </summary>
    [Post("/api/validator/registers/{registerId}/process")]
    Task<ValidatorProcessResult> ProcessRegisterAsync(string registerId, [Header("Authorization")] string authorization);

    /// <summary>
    /// Runs an integrity check on a register's chain.
    /// </summary>
    [Post("/api/validator/registers/{registerId}/integrity-check")]
    Task<IntegrityCheckResult> IntegrityCheckAsync(string registerId, [Header("Authorization")] string authorization);
}
