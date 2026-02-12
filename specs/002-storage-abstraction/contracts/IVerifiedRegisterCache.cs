// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
// CONTRACT: This file defines the interface specification for implementation

using System.Linq.Expressions;

namespace Sorcha.Register.Core.Storage;

/// <summary>
/// Verified cache for register data. Data is cryptographically verified
/// before being added to the cache. The cache is the authoritative source
/// for all read operations.
/// </summary>
/// <remarks>
/// CRITICAL SECURITY MODEL:
/// - Cold storage is NOT trusted - all data must be verified before use
/// - Only verified data enters the cache
/// - ALL read queries MUST go through this cache, never directly to cold storage
/// - Corrupted data triggers peer recovery, not error responses
/// </remarks>
public interface IVerifiedRegisterCache
{
    /// <summary>
    /// Initializes the cache by loading and verifying data from cold storage.
    /// Invalid data is skipped and marked for peer recovery.
    /// </summary>
    /// <param name="registerId">Register to initialize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Initialization result with any corruption detected</returns>
    Task<CacheInitializationResult> InitializeAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a verified docket from cache. Never reads directly from cold storage.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="height">Docket height</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified docket or null if not in cache</returns>
    Task<VerifiedDocket?> GetDocketAsync(
        string registerId,
        ulong height,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets verified dockets in a range.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="startHeight">Start height (inclusive)</param>
    /// <param name="endHeight">End height (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified dockets in range</returns>
    Task<IEnumerable<VerifiedDocket>> GetDocketRangeAsync(
        string registerId,
        ulong startHeight,
        ulong endHeight,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a verified transaction from cache.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="txId">Transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified transaction or null if not in cache</returns>
    Task<VerifiedTransaction?> GetTransactionAsync(
        string registerId,
        string txId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries transactions from the verified cache.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="limit">Maximum results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching verified transactions</returns>
    Task<IEnumerable<VerifiedTransaction>> QueryTransactionsAsync(
        string registerId,
        Expression<Func<VerifiedTransaction, bool>> predicate,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new docket to the cache after verification.
    /// Also persists to cold storage atomically.
    /// </summary>
    /// <param name="docket">Docket to add (must pass verification)</param>
    /// <param name="transactions">Transactions in the docket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    Task<VerificationResult> AddVerifiedDocketAsync(
        Docket docket,
        IEnumerable<TransactionModel> transactions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current verified chain height.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Highest verified docket height (0 if empty)</returns>
    Task<ulong> GetVerifiedHeightAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current operational state of a register.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current operational state</returns>
    Task<RegisterOperationalState> GetOperationalStateAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ranges that need recovery from peers.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Corrupted ranges requiring recovery</returns>
    Task<IEnumerable<CorruptionRange>> GetCorruptedRangesAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes recovered data from peer network.
    /// Verifies the data before integrating into cache.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="dockets">Recovered dockets from peers</param>
    /// <param name="transactions">Recovered transactions from peers</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recovery result</returns>
    Task<RecoveryResult> ProcessRecoveredDataAsync(
        string registerId,
        IEnumerable<Docket> dockets,
        IEnumerable<TransactionModel> transactions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a range as recovered after successful peer sync.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="startHeight">Start of recovered range</param>
    /// <param name="endHeight">End of recovered range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkRangeRecoveredAsync(
        string registerId,
        ulong startHeight,
        ulong endHeight,
        CancellationToken cancellationToken = default);
}

#region Supporting Types

/// <summary>
/// A cryptographically verified docket in the cache.
/// </summary>
public record VerifiedDocket(
    string RegisterId,
    ulong Height,
    string Hash,
    string PreviousHash,
    string[] TransactionIds,
    DateTime Timestamp,
    DocketVerificationStatus VerificationStatus,
    DateTime VerifiedAt,
    string? CorruptionDetails = null);

/// <summary>
/// A cryptographically verified transaction in the cache.
/// </summary>
public record VerifiedTransaction(
    string RegisterId,
    string TxId,
    ulong DocketHeight,
    string SenderWallet,
    string[] Recipients,
    DateTime Timestamp,
    DateTime VerifiedAt);

/// <summary>
/// Binary verification status - immutable once set.
/// </summary>
public enum DocketVerificationStatus
{
    /// <summary>Docket passed all verification checks</summary>
    Verified,
    /// <summary>Docket failed verification</summary>
    Corrupted
}

/// <summary>
/// Operational status of a register.
/// </summary>
public record RegisterOperationalState(
    string RegisterId,
    RegisterState State,
    ulong VerifiedHeight,
    ulong TotalHeight,
    CorruptionRange[] CorruptedRanges,
    DateTime LastStateChange,
    string? Message = null);

/// <summary>
/// Operational states for a register.
/// </summary>
public enum RegisterState
{
    /// <summary>Cache is being populated from cold storage</summary>
    Initializing,
    /// <summary>Fully verified, serving all requests</summary>
    Healthy,
    /// <summary>Some corruption detected, partial service</summary>
    Degraded,
    /// <summary>Actively syncing from peer network</summary>
    Recovering,
    /// <summary>Fetching specific ranges from peers</summary>
    PeerSyncInProgress,
    /// <summary>Not serving requests</summary>
    Offline
}

/// <summary>
/// Represents a range of corrupted dockets.
/// </summary>
public record CorruptionRange(
    string RegisterId,
    ulong StartHeight,
    ulong EndHeight,
    CorruptionType Type,
    string Details,
    DateTime DetectedAt,
    int RecoveryAttempts = 0,
    DateTime? LastRecoveryAttempt = null,
    RecoveryStatus RecoveryStatus = RecoveryStatus.Pending);

/// <summary>
/// Types of corruption detected.
/// </summary>
public enum CorruptionType
{
    /// <summary>Docket hash doesn't match computed hash</summary>
    InvalidDocketHash,
    /// <summary>Previous hash linkage broken</summary>
    BrokenChainLink,
    /// <summary>Transaction signature invalid</summary>
    InvalidTransactionSignature,
    /// <summary>Data missing from storage</summary>
    MissingData,
    /// <summary>Data format/schema invalid</summary>
    MalformedData
}

/// <summary>
/// Status of corruption recovery.
/// </summary>
public enum RecoveryStatus
{
    /// <summary>Recovery not yet attempted</summary>
    Pending,
    /// <summary>Currently fetching from peers</summary>
    InProgress,
    /// <summary>Recovery completed successfully</summary>
    Succeeded,
    /// <summary>Recovery failed after max attempts</summary>
    Failed,
    /// <summary>No peers could provide valid data</summary>
    Abandoned
}

/// <summary>
/// Result of cache initialization.
/// </summary>
public record CacheInitializationResult(
    string RegisterId,
    int VerifiedDocketCount,
    int TotalDocketCount,
    CorruptionRange[] CorruptedRanges,
    TimeSpan LoadDuration,
    TimeSpan VerificationDuration,
    RegisterState ResultingState)
{
    public bool HasCorruption => CorruptedRanges.Length > 0;
    public bool IsFullyVerified => !HasCorruption;
    public double VerificationRate => VerificationDuration.TotalSeconds > 0
        ? VerifiedDocketCount / VerificationDuration.TotalSeconds
        : 0;
}

/// <summary>
/// Result of verification when adding new data.
/// </summary>
public record VerificationResult(
    bool IsValid,
    string? ErrorMessage = null,
    CorruptionType? CorruptionType = null,
    IReadOnlyList<string>? InvalidTransactionIds = null);

/// <summary>
/// Result of processing recovered data.
/// </summary>
public record RecoveryResult(
    bool IsSuccess,
    int DocketsRecovered,
    int TransactionsRecovered,
    string? ErrorMessage = null,
    IReadOnlyList<CorruptionRange>? RemainingCorruption = null);

#endregion

#region Placeholder Types (defined elsewhere)

// These types are defined in Sorcha.Register.Models
// Included here as placeholders for contract completeness

/// <summary>Placeholder - actual type in Sorcha.Register.Models</summary>
public class Docket { }

/// <summary>Placeholder - actual type in Sorcha.Register.Models</summary>
public class TransactionModel { }

#endregion
