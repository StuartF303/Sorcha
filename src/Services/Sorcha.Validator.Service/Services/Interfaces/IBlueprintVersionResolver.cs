// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Resolves blueprint versions for transaction validation.
/// Follows the transaction chain (previousId references) to determine
/// which blueprint version should be used for validating a given action.
/// </summary>
public interface IBlueprintVersionResolver
{
    /// <summary>
    /// Resolves the blueprint version that should be used to validate an action transaction.
    /// Follows the previousId chain from the action back to its originating blueprint publication.
    /// </summary>
    /// <param name="registerId">Register containing the transaction</param>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="previousTransactionId">The previousId from the action transaction</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The blueprint version to use, or null if not found</returns>
    Task<ResolvedBlueprintVersion?> ResolveForActionAsync(
        string registerId,
        string blueprintId,
        string previousTransactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the blueprint associated with a specific publication transaction
    /// </summary>
    /// <param name="registerId">Register containing the transaction</param>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="publicationTransactionId">Transaction ID of the blueprint publication</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The blueprint from that specific publication, or null if not found</returns>
    Task<ResolvedBlueprintVersion?> GetByPublicationTransactionAsync(
        string registerId,
        string blueprintId,
        string publicationTransactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all versions of a blueprint in chronological order
    /// </summary>
    /// <param name="registerId">Register containing the blueprint</param>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all versions in publication order (oldest first)</returns>
    Task<IReadOnlyList<BlueprintVersionInfo>> GetVersionHistoryAsync(
        string registerId,
        string blueprintId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the latest published version of a blueprint
    /// </summary>
    /// <param name="registerId">Register containing the blueprint</param>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The latest blueprint version, or null if not found</returns>
    Task<ResolvedBlueprintVersion?> GetLatestVersionAsync(
        string registerId,
        string blueprintId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the blueprint version that was active at a specific point in time
    /// </summary>
    /// <param name="registerId">Register containing the blueprint</param>
    /// <param name="blueprintId">Blueprint ID</param>
    /// <param name="asOf">Point in time to resolve version for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The blueprint version active at that time, or null if not found</returns>
    Task<ResolvedBlueprintVersion?> GetVersionAsOfAsync(
        string registerId,
        string blueprintId,
        DateTimeOffset asOf,
        CancellationToken ct = default);
}

/// <summary>
/// A resolved blueprint version with full context
/// </summary>
public record ResolvedBlueprintVersion
{
    /// <summary>Blueprint ID</summary>
    public required string BlueprintId { get; init; }

    /// <summary>Version number (1-based, chronological)</summary>
    public required int VersionNumber { get; init; }

    /// <summary>Transaction ID that published this version</summary>
    public required string PublicationTransactionId { get; init; }

    /// <summary>The blueprint data</summary>
    public required BlueprintModel Blueprint { get; init; }

    /// <summary>When this version was published</summary>
    public required DateTimeOffset PublishedAt { get; init; }

    /// <summary>Whether this is the latest version</summary>
    public bool IsLatest { get; init; }

    /// <summary>Transaction ID of the previous blueprint version (null for first version)</summary>
    public string? PreviousVersionTransactionId { get; init; }
}

/// <summary>
/// Summary information about a blueprint version
/// </summary>
public record BlueprintVersionInfo
{
    /// <summary>Blueprint ID</summary>
    public required string BlueprintId { get; init; }

    /// <summary>Version number (1-based, chronological)</summary>
    public required int VersionNumber { get; init; }

    /// <summary>Transaction ID that published this version</summary>
    public required string PublicationTransactionId { get; init; }

    /// <summary>When this version was published</summary>
    public required DateTimeOffset PublishedAt { get; init; }

    /// <summary>Blueprint title at this version</summary>
    public string? Title { get; init; }

    /// <summary>Whether this is the latest version</summary>
    public bool IsLatest { get; init; }
}
