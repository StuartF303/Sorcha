// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Resolves control blueprint versions for register governance.
/// Control blueprints define consensus parameters, validator management,
/// and other register configuration. They can be updated via control transactions.
/// </summary>
public interface IControlBlueprintVersionResolver
{
    /// <summary>
    /// Gets the currently active control blueprint version for a register.
    /// This is the latest committed version that should be used for validation.
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The active control blueprint version, or null if not found</returns>
    Task<ResolvedControlBlueprintVersion?> GetActiveVersionAsync(
        string registerId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the control blueprint version from a specific control transaction
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="controlTransactionId">Transaction ID of the control update</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The control blueprint version at that transaction, or null if not found</returns>
    Task<ResolvedControlBlueprintVersion?> GetByTransactionAsync(
        string registerId,
        string controlTransactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the version history of control blueprint changes
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all control blueprint versions in order (oldest first)</returns>
    Task<IReadOnlyList<ControlBlueprintVersionInfo>> GetVersionHistoryAsync(
        string registerId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the control blueprint version that was active at a specific point in time
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="asOf">Point in time to resolve version for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The control blueprint version active at that time, or null if not found</returns>
    Task<ResolvedControlBlueprintVersion?> GetVersionAsOfAsync(
        string registerId,
        DateTimeOffset asOf,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a control blueprint version change is pending (in unconfirmed dockets)
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if there are pending control updates</returns>
    Task<bool> HasPendingUpdatesAsync(
        string registerId,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cache for a register, forcing a refresh on next access
    /// </summary>
    /// <param name="registerId">Register ID to invalidate</param>
    void InvalidateCache(string registerId);

    /// <summary>
    /// Event raised when a control blueprint version changes
    /// </summary>
    event EventHandler<ControlBlueprintVersionChangedEventArgs>? VersionChanged;
}

/// <summary>
/// A resolved control blueprint version with full configuration
/// </summary>
public record ResolvedControlBlueprintVersion
{
    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Version number (1-based, chronological). Version 1 is from genesis.</summary>
    public required int VersionNumber { get; init; }

    /// <summary>Transaction ID that established this version (genesis or control update)</summary>
    public required string TransactionId { get; init; }

    /// <summary>When this version became active</summary>
    public required DateTimeOffset ActiveFrom { get; init; }

    /// <summary>The genesis configuration derived from this version</summary>
    public required GenesisConfiguration Configuration { get; init; }

    /// <summary>Whether this is the currently active version</summary>
    public bool IsActive { get; init; }

    /// <summary>Previous version's transaction ID (null for genesis version)</summary>
    public string? PreviousVersionTransactionId { get; init; }

    /// <summary>Summary of what changed in this version (null for genesis)</summary>
    public string? ChangeDescription { get; init; }
}

/// <summary>
/// Summary information about a control blueprint version
/// </summary>
public record ControlBlueprintVersionInfo
{
    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Version number (1-based, chronological)</summary>
    public required int VersionNumber { get; init; }

    /// <summary>Transaction ID that established this version</summary>
    public required string TransactionId { get; init; }

    /// <summary>When this version became active</summary>
    public required DateTimeOffset ActiveFrom { get; init; }

    /// <summary>Whether this is the currently active version</summary>
    public bool IsActive { get; init; }

    /// <summary>Type of change (Genesis, ConfigUpdate, ValidatorChange, etc.)</summary>
    public required string ChangeType { get; init; }

    /// <summary>Brief description of changes</summary>
    public string? ChangeDescription { get; init; }
}

/// <summary>
/// Event args for control blueprint version changes
/// </summary>
public class ControlBlueprintVersionChangedEventArgs : EventArgs
{
    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Previous version number</summary>
    public int PreviousVersionNumber { get; init; }

    /// <summary>New version number</summary>
    public required int NewVersionNumber { get; init; }

    /// <summary>Transaction ID that caused the change</summary>
    public required string TransactionId { get; init; }

    /// <summary>Type of change</summary>
    public required string ChangeType { get; init; }

    /// <summary>When the change occurred</summary>
    public required DateTimeOffset ChangedAt { get; init; }
}
