// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using MongoDB.Bson;
using MongoDB.Driver;

namespace Sorcha.Register.Service.Repositories;

/// <summary>
/// MongoDB implementation of the system register repository
/// </summary>
/// <remarks>
/// Collection name: sorcha_system_register_blueprints
/// Indexes:
/// - Version (ascending) - for incremental sync queries
/// - PublishedAt (descending) - for recent blueprints
/// - IsActive (ascending) - for filtering active blueprints
/// Version number auto-increment strategy: Find max version + 1
/// </remarks>
public class MongoSystemRegisterRepository : ISystemRegisterRepository
{
    private readonly IMongoCollection<SystemRegisterEntry> _collection;
    private readonly ILogger<MongoSystemRegisterRepository> _logger;
    private const string CollectionName = "sorcha_system_register_blueprints";

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoSystemRegisterRepository"/> class
    /// </summary>
    /// <param name="database">MongoDB database instance</param>
    /// <param name="logger">Logger instance</param>
    public MongoSystemRegisterRepository(
        IMongoDatabase database,
        ILogger<MongoSystemRegisterRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ = database ?? throw new ArgumentNullException(nameof(database));

        _collection = database.GetCollection<SystemRegisterEntry>(CollectionName);

        // Create indexes on startup
        CreateIndexes();
    }

    /// <summary>
    /// Creates required indexes for the system register collection
    /// </summary>
    private void CreateIndexes()
    {
        try
        {
            // Index on Version (ascending) for incremental sync
            var versionIndex = Builders<SystemRegisterEntry>.IndexKeys.Ascending(x => x.Version);
            _collection.Indexes.CreateOne(new CreateIndexModel<SystemRegisterEntry>(versionIndex));

            // Index on PublishedAt (descending) for recent queries
            var publishedAtIndex = Builders<SystemRegisterEntry>.IndexKeys.Descending(x => x.PublishedAt);
            _collection.Indexes.CreateOne(new CreateIndexModel<SystemRegisterEntry>(publishedAtIndex));

            // Index on IsActive for filtering
            var isActiveIndex = Builders<SystemRegisterEntry>.IndexKeys.Ascending(x => x.IsActive);
            _collection.Indexes.CreateOne(new CreateIndexModel<SystemRegisterEntry>(isActiveIndex));

            _logger.LogInformation("System register collection indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create system register collection indexes");
        }
    }

    /// <inheritdoc/>
    public async Task<List<SystemRegisterEntry>> GetAllBlueprintsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SystemRegisterEntry>.Filter.Eq(x => x.IsActive, true);
            var blueprints = await _collection
                .Find(filter)
                .SortBy(x => x.Version)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} active blueprints from system register", blueprints.Count);
            return blueprints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all blueprints from system register");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SystemRegisterEntry?> GetBlueprintByIdAsync(string blueprintId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SystemRegisterEntry>.Filter.Eq(x => x.BlueprintId, blueprintId);
            var blueprint = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (blueprint != null)
            {
                _logger.LogDebug("Retrieved blueprint {BlueprintId} from system register", blueprintId);
            }
            else
            {
                _logger.LogDebug("Blueprint {BlueprintId} not found in system register", blueprintId);
            }

            return blueprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get blueprint {BlueprintId} from system register", blueprintId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<SystemRegisterEntry>> GetBlueprintsSinceVersionAsync(long sinceVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SystemRegisterEntry>.Filter.Gt(x => x.Version, sinceVersion);
            var blueprints = await _collection
                .Find(filter)
                .SortBy(x => x.Version)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} blueprints since version {Version} from system register",
                blueprints.Count, sinceVersion);

            return blueprints;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get blueprints since version {Version} from system register", sinceVersion);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SystemRegisterEntry> PublishBlueprintAsync(
        string blueprintId,
        BsonDocument blueprintDocument,
        string publishedBy,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if blueprint already exists
            var existing = await GetBlueprintByIdAsync(blueprintId, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException($"Blueprint {blueprintId} already exists in system register");
            }

            // Get next version number (max + 1)
            var latestVersion = await GetLatestVersionAsync(cancellationToken);
            var nextVersion = latestVersion + 1;

            // Create new entry
            var entry = new SystemRegisterEntry
            {
                BlueprintId = blueprintId,
                RegisterId = Guid.Empty, // System register well-known ID
                Document = blueprintDocument,
                PublishedAt = DateTime.UtcNow,
                PublishedBy = publishedBy,
                Version = nextVersion,
                IsActive = true,
                Metadata = metadata
            };

            // Validate system register ID
            entry.ValidateSystemRegister();

            // Insert into MongoDB
            await _collection.InsertOneAsync(entry, cancellationToken: cancellationToken);

            _logger.LogInformation("Published blueprint {BlueprintId} to system register with version {Version}",
                blueprintId, nextVersion);

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish blueprint {BlueprintId} to system register", blueprintId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var latestEntry = await _collection
                .Find(Builders<SystemRegisterEntry>.Filter.Empty)
                .SortByDescending(x => x.Version)
                .Limit(1)
                .FirstOrDefaultAsync(cancellationToken);

            var version = latestEntry?.Version ?? 0;
            _logger.LogDebug("Latest system register version: {Version}", version);

            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get latest version from system register");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsSystemRegisterInitializedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(
                Builders<SystemRegisterEntry>.Filter.Empty,
                cancellationToken: cancellationToken);

            var isInitialized = count > 0;
            _logger.LogDebug("System register initialized: {IsInitialized} (count: {Count})", isInitialized, count);

            return isInitialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if system register is initialized");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetBlueprintCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(
                Builders<SystemRegisterEntry>.Filter.Eq(x => x.IsActive, true),
                cancellationToken: cancellationToken);

            _logger.LogDebug("System register contains {Count} active blueprints", count);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get blueprint count from system register");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeprecateBlueprintAsync(string blueprintId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SystemRegisterEntry>.Filter.Eq(x => x.BlueprintId, blueprintId);
            var update = Builders<SystemRegisterEntry>.Update.Set(x => x.IsActive, false);

            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

            if (result.ModifiedCount > 0)
            {
                _logger.LogInformation("Deprecated blueprint {BlueprintId} in system register", blueprintId);
                return true;
            }
            else
            {
                _logger.LogWarning("Blueprint {BlueprintId} not found or already deprecated", blueprintId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deprecate blueprint {BlueprintId}", blueprintId);
            throw;
        }
    }
}
