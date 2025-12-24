// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using MongoDB.Bson;
using Sorcha.Register.Service.Repositories;

namespace Sorcha.Register.Service.Services;

/// <summary>
/// Service for managing the system register initialization and blueprint publication
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Initialize system register on hub node startup
/// - Seed default blueprints (register-creation-v1)
/// - Validate system register integrity
/// - Provide idempotent initialization (skip if already initialized)
/// </remarks>
public class SystemRegisterService
{
    private readonly ISystemRegisterRepository _repository;
    private readonly ILogger<SystemRegisterService> _logger;
    private const string DefaultBlueprintId = "register-creation-v1";

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRegisterService"/> class
    /// </summary>
    /// <param name="repository">System register repository</param>
    /// <param name="logger">Logger instance</param>
    public SystemRegisterService(
        ISystemRegisterRepository repository,
        ILogger<SystemRegisterService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the system register (idempotent - safe to call multiple times)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if initialization performed, false if already initialized</returns>
    public async Task<bool> InitializeSystemRegisterAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking system register initialization status");

            // Check if already initialized
            var isInitialized = await _repository.IsSystemRegisterInitializedAsync(cancellationToken);

            if (isInitialized)
            {
                _logger.LogInformation("System register already initialized - skipping initialization");

                // Validate integrity
                await ValidateSystemRegisterIntegrityAsync(cancellationToken);

                return false;
            }

            _logger.LogInformation("System register not initialized - beginning initialization");

            // Seed default blueprints
            await SeedDefaultBlueprintsAsync(cancellationToken);

            _logger.LogInformation("System register initialization complete");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system register");
            throw;
        }
    }

    /// <summary>
    /// Seeds default blueprints into the system register
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task SeedDefaultBlueprintsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default blueprints into system register");

        // Create register-creation-v1 blueprint
        var registerCreationBlueprint = CreateRegisterCreationBlueprintDocument();

        await _repository.PublishBlueprintAsync(
            blueprintId: DefaultBlueprintId,
            blueprintDocument: registerCreationBlueprint,
            publishedBy: "system",
            metadata: new Dictionary<string, string>
            {
                { "category", "register" },
                { "type", "creation" },
                { "isDefault", "true" }
            },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Seeded default blueprint: {BlueprintId}", DefaultBlueprintId);
    }

    /// <summary>
    /// Creates the register-creation-v1 blueprint document
    /// </summary>
    /// <returns>BSON document representing the register creation blueprint</returns>
    private static BsonDocument CreateRegisterCreationBlueprintDocument()
    {
        // This is a simplified version - in production this would be a complete JSON-LD blueprint
        var blueprint = new BsonDocument
        {
            { "@context", "https://sorcha.dev/blueprints/v1" },
            { "id", "register-creation-v1" },
            { "name", "Register Creation Workflow" },
            { "version", "1.0.0" },
            { "description", "Default workflow for creating a new register in the Sorcha platform" },
            { "actions", new BsonArray
                {
                    new BsonDocument
                    {
                        { "id", "validate-request" },
                        { "name", "Validate Register Creation Request" },
                        { "type", "validation" }
                    },
                    new BsonDocument
                    {
                        { "id", "create-register" },
                        { "name", "Create New Register" },
                        { "type", "register-creation" }
                    },
                    new BsonDocument
                    {
                        { "id", "publish-transaction" },
                        { "name", "Publish Register Creation Transaction" },
                        { "type", "transaction" }
                    }
                }
            }
        };

        return blueprint;
    }

    /// <summary>
    /// Validates system register integrity on startup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ValidateSystemRegisterIntegrityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating system register integrity");

            // Get all blueprints
            var blueprints = await _repository.GetAllBlueprintsAsync(cancellationToken);

            _logger.LogInformation("System register contains {Count} active blueprints", blueprints.Count);

            // Validate each blueprint has correct RegisterId
            var invalidBlueprints = blueprints
                .Where(b => b.RegisterId != Guid.Empty)
                .Select(b => b.BlueprintId)
                .ToList();

            if (invalidBlueprints.Any())
            {
                var invalidIds = string.Join(", ", invalidBlueprints);
                _logger.LogError("System register integrity check failed - invalid register IDs found: {InvalidIds}", invalidIds);
                throw new InvalidOperationException($"System register integrity check failed - blueprints with invalid register IDs: {invalidIds}");
            }

            // Validate version sequence (should be monotonically increasing)
            var orderedBlueprints = blueprints.OrderBy(b => b.Version).ToList();
            for (int i = 1; i < orderedBlueprints.Count; i++)
            {
                if (orderedBlueprints[i].Version <= orderedBlueprints[i - 1].Version)
                {
                    _logger.LogError("System register integrity check failed - version sequence violation at index {Index}", i);
                    throw new InvalidOperationException($"System register integrity check failed - version sequence is not monotonic");
                }
            }

            _logger.LogInformation("System register integrity validated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System register integrity validation failed");
            throw;
        }
    }

    /// <summary>
    /// Publishes a new blueprint to the system register
    /// </summary>
    /// <param name="blueprintId">Unique blueprint identifier</param>
    /// <param name="blueprintDocument">Blueprint JSON document</param>
    /// <param name="publishedBy">Publisher identity</param>
    /// <param name="metadata">Optional metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Published blueprint entry</returns>
    public async Task<SystemRegisterEntry> PublishBlueprintAsync(
        string blueprintId,
        BsonDocument blueprintDocument,
        string publishedBy,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Publishing blueprint {BlueprintId} to system register", blueprintId);

            var entry = await _repository.PublishBlueprintAsync(
                blueprintId,
                blueprintDocument,
                publishedBy,
                metadata,
                cancellationToken);

            _logger.LogInformation("Blueprint {BlueprintId} published with version {Version}",
                blueprintId, entry.Version);

            // TODO: Trigger push notification to connected peers

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish blueprint {BlueprintId}", blueprintId);
            throw;
        }
    }

    /// <summary>
    /// Gets all blueprints from the system register
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active blueprints</returns>
    public async Task<List<SystemRegisterEntry>> GetAllBlueprintsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllBlueprintsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a specific blueprint by ID
    /// </summary>
    /// <param name="blueprintId">Blueprint identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blueprint entry or null</returns>
    public async Task<SystemRegisterEntry?> GetBlueprintAsync(string blueprintId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetBlueprintByIdAsync(blueprintId, cancellationToken);
    }

    /// <summary>
    /// Gets the current system register version (latest blueprint version)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current version number</returns>
    public async Task<long> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetLatestVersionAsync(cancellationToken);
    }
}
