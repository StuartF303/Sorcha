// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Orchestrates the complete validation pipeline for a register
/// </summary>
/// <remarks>
/// The ValidatorOrchestrator coordinates all validator components to achieve
/// the full validation workflow:
///
/// <para><b>Pipeline Stages:</b></para>
/// <list type="number">
///   <item>Memory Pool: Collect pending transactions</item>
///   <item>Docket Building: Trigger-based docket creation (time OR size)</item>
///   <item>Consensus: Distributed voting among peer validators</item>
///   <item>Persistence: Write confirmed dockets to Register Service</item>
///   <item>Cleanup: Remove processed transactions from memory pool</item>
/// </list>
///
/// <para><b>User Stories:</b></para>
/// <list type="bullet">
///   <item>US1: In-Memory Transaction Pool Management (P0)</item>
///   <item>US2: Docket Creation with Hybrid Triggers (P0)</item>
///   <item>US3: Distributed Consensus Achievement (P1)</item>
///   <item>US4: Register Service Write on Consensus (P1)</item>
/// </list>
/// </remarks>
public interface IValidatorOrchestrator
{
    /// <summary>
    /// Starts validation for a specific register
    /// </summary>
    /// <param name="registerId">Register ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if validation started successfully</returns>
    /// <remarks>
    /// Starts the validation pipeline for the specified register:
    /// <list type="bullet">
    ///   <item>Initializes memory pool for the register</item>
    ///   <item>Starts docket build trigger monitoring</item>
    ///   <item>Begins processing pending transactions</item>
    /// </list>
    ///
    /// Can be called multiple times for the same register (idempotent).
    /// </remarks>
    Task<bool> StartValidatorAsync(string registerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops validation for a specific register
    /// </summary>
    /// <param name="registerId">Register ID to stop</param>
    /// <param name="persistMemPool">Whether to save memory pool state before stopping</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if validation stopped successfully</returns>
    /// <remarks>
    /// Gracefully stops the validation pipeline:
    /// <list type="bullet">
    ///   <item>Completes any in-flight consensus rounds</item>
    ///   <item>Optionally persists memory pool state</item>
    ///   <item>Releases resources</item>
    /// </list>
    /// </remarks>
    Task<bool> StopValidatorAsync(
        string registerId,
        bool persistMemPool = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a validator for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <returns>Validator status information</returns>
    Task<ValidatorStatus?> GetValidatorStatusAsync(string registerId);

    /// <summary>
    /// Processes the validation pipeline for a register (single iteration)
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pipeline execution result</returns>
    /// <remarks>
    /// Executes one complete validation cycle:
    /// <list type="number">
    ///   <item>Check if docket build should trigger</item>
    ///   <item>Build docket if trigger conditions met</item>
    ///   <item>Achieve consensus on proposed docket</item>
    ///   <item>Write confirmed docket to Register Service</item>
    ///   <item>Remove processed transactions from memory pool</item>
    ///   <item>Broadcast confirmed docket to peer network</item>
    /// </list>
    ///
    /// Returns null if no docket was built (no pending transactions or triggers not met).
    /// </remarks>
    Task<PipelineResult?> ProcessValidationPipelineAsync(
        string registerId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Status information for a validator instance
/// </summary>
public record ValidatorStatus
{
    /// <summary>
    /// Register ID being validated
    /// </summary>
    public required string RegisterId { get; init; }

    /// <summary>
    /// Whether validator is currently active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Number of transactions in memory pool
    /// </summary>
    public int TransactionsInMemPool { get; init; }

    /// <summary>
    /// Number of dockets proposed
    /// </summary>
    public long DocketsProposed { get; init; }

    /// <summary>
    /// Number of dockets confirmed
    /// </summary>
    public long DocketsConfirmed { get; init; }

    /// <summary>
    /// Number of dockets rejected
    /// </summary>
    public long DocketsRejected { get; init; }

    /// <summary>
    /// When validator was started
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Last docket build time
    /// </summary>
    public DateTimeOffset? LastDocketBuildAt { get; init; }
}

/// <summary>
/// Result of a validation pipeline execution
/// </summary>
public record PipelineResult
{
    /// <summary>
    /// The docket that was processed
    /// </summary>
    public required Docket Docket { get; init; }

    /// <summary>
    /// Whether consensus was achieved
    /// </summary>
    public bool ConsensusAchieved { get; init; }

    /// <summary>
    /// Whether docket was written to Register Service
    /// </summary>
    public bool WrittenToRegister { get; init; }

    /// <summary>
    /// Total pipeline execution time
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Any errors that occurred
    /// </summary>
    public string? ErrorMessage { get; init; }
}
