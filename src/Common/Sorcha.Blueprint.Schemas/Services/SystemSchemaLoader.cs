// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Schemas.Models;

namespace Sorcha.Blueprint.Schemas.Services;

/// <summary>
/// Loads system schemas from embedded resources.
/// </summary>
public sealed class SystemSchemaLoader
{
    private readonly ILogger<SystemSchemaLoader> _logger;
    private readonly Lazy<IReadOnlyList<SchemaEntry>> _systemSchemas;

    private static readonly string[] SystemSchemaNames =
    [
        "installation",
        "organisation",
        "participant",
        "register"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemSchemaLoader"/> class.
    /// </summary>
    public SystemSchemaLoader(ILogger<SystemSchemaLoader> logger)
    {
        _logger = logger;
        _systemSchemas = new Lazy<IReadOnlyList<SchemaEntry>>(LoadAllSystemSchemas);
    }

    /// <summary>
    /// Gets all system schemas.
    /// </summary>
    public IReadOnlyList<SchemaEntry> GetSystemSchemas() => _systemSchemas.Value;

    /// <summary>
    /// Gets a specific system schema by identifier.
    /// </summary>
    public SchemaEntry? GetSystemSchema(string identifier)
    {
        return _systemSchemas.Value.FirstOrDefault(
            s => s.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if an identifier is a system schema.
    /// </summary>
    public static bool IsSystemSchema(string identifier)
    {
        return SystemSchemaNames.Contains(identifier.ToLowerInvariant());
    }

    private IReadOnlyList<SchemaEntry> LoadAllSystemSchemas()
    {
        var schemas = new List<SchemaEntry>();
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var name in SystemSchemaNames)
        {
            var schema = LoadSchemaFromResource(assembly, name);
            if (schema is not null)
            {
                schemas.Add(schema);
            }
        }

        _logger.LogInformation("Loaded {Count} system schemas", schemas.Count);
        return schemas.AsReadOnly();
    }

    private SchemaEntry? LoadSchemaFromResource(Assembly assembly, string schemaName)
    {
        var resourceName = $"Sorcha.Blueprint.Schemas.SystemSchemas.{schemaName}.schema.json";

        try
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                _logger.LogError("System schema resource not found: {ResourceName}", resourceName);
                return null;
            }

            using var reader = new StreamReader(stream);
            var jsonContent = reader.ReadToEnd();
            var content = JsonDocument.Parse(jsonContent);

            // Extract metadata from the JSON Schema
            var root = content.RootElement;
            var title = root.TryGetProperty("title", out var titleProp)
                ? titleProp.GetString() ?? schemaName
                : schemaName;
            var description = root.TryGetProperty("description", out var descProp)
                ? descProp.GetString()
                : null;

            return new SchemaEntry
            {
                Identifier = schemaName,
                Title = title,
                Description = description,
                Version = "1.0.0",
                Category = SchemaCategory.System,
                Source = SchemaSource.Internal(),
                Status = SchemaStatus.Active,
                OrganizationId = null,
                IsGloballyPublished = true,
                Content = content,
                DateAdded = DateTimeOffset.UtcNow,
                DateModified = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load system schema: {SchemaName}", schemaName);
            return null;
        }
    }
}
