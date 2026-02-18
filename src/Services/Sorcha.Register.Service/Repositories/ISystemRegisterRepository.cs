// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sorcha.Register.Service.Repositories;

/// <summary>
/// Repository interface for system register blueprint storage and retrieval
/// </summary>
/// <remarks>
/// The system register is a well-known register (ID: 00000000-0000-0000-0000-000000000000) that stores
/// published blueprints available to all peer nodes in the Sorcha network. This repository manages
/// the MongoDB collection storing blueprint documents with version-based incremental synchronization support.
/// </remarks>
public interface ISystemRegisterRepository
{
    /// <summary>
    /// Gets all active blueprints from the system register
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all active blueprint entries</returns>
    Task<List<SystemRegisterEntry>> GetAllBlueprintsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific blueprint by its unique identifier
    /// </summary>
    /// <param name="blueprintId">Blueprint identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blueprint entry or null if not found</returns>
    Task<SystemRegisterEntry?> GetBlueprintByIdAsync(string blueprintId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets blueprints that have been added/updated since a specific version (incremental sync)
    /// </summary>
    /// <param name="sinceVersion">Starting version (exclusive) - returns blueprints with version > sinceVersion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of blueprints newer than the specified version, ordered by version ascending</returns>
    Task<List<SystemRegisterEntry>> GetBlueprintsSinceVersionAsync(long sinceVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a new blueprint to the system register
    /// </summary>
    /// <param name="blueprintId">Unique blueprint identifier</param>
    /// <param name="blueprintDocument">Blueprint JSON document as BsonDocument</param>
    /// <param name="publishedBy">Identity of publisher (user ID or "system")</param>
    /// <param name="metadata">Optional metadata key-value pairs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Newly created system register entry with auto-generated version</returns>
    Task<SystemRegisterEntry> PublishBlueprintAsync(
        string blueprintId,
        BsonDocument blueprintDocument,
        string publishedBy,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version number in the system register
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest version number or 0 if empty</returns>
    Task<long> GetLatestVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the system register has been initialized (contains at least one blueprint)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if system register initialized, false otherwise</returns>
    Task<bool> IsSystemRegisterInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of active blueprints in the system register
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of active blueprints</returns>
    Task<int> GetBlueprintCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a blueprint as inactive (deprecated/withdrawn)
    /// </summary>
    /// <param name="blueprintId">Blueprint identifier to deprecate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blueprint was found and marked inactive</returns>
    Task<bool> DeprecateBlueprintAsync(string blueprintId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a blueprint entry in the system register MongoDB collection
/// </summary>
public class SystemRegisterEntry
{
    /// <summary>
    /// Unique blueprint identifier (MongoDB _id field)
    /// </summary>
    [BsonId]
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>
    /// System register identifier (well-known constant: 00000000-0000-0000-0000-000000000000)
    /// </summary>
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid RegisterId { get; set; } = Guid.Empty;

    /// <summary>
    /// Blueprint JSON document
    /// </summary>
    public BsonDocument Document { get; set; } = new();

    /// <summary>
    /// Timestamp when blueprint was published (UTC)
    /// </summary>
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identity of publisher (user ID or "system")
    /// </summary>
    public string PublishedBy { get; set; } = string.Empty;

    /// <summary>
    /// Incrementing version number for sync (auto-increment)
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Whether blueprint is active/available
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Link to register transaction that published this blueprint (optional)
    /// </summary>
    public string? PublicationTransactionId { get; set; }

    /// <summary>
    /// SHA-256 checksum of Document for integrity verification (optional)
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Optional metadata key-value pairs
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Validates that RegisterId is the well-known system register ID
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if RegisterId is not Guid.Empty</exception>
    public void ValidateSystemRegister()
    {
        if (RegisterId != Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Invalid system register ID: {RegisterId}. Must be {Guid.Empty}");
        }
    }
}
