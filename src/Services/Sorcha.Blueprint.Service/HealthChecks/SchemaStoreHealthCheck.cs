// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Service.HealthChecks;

/// <summary>
/// Health check for the Schema Store service.
/// Verifies that system schemas are loaded and accessible.
/// </summary>
public sealed class SchemaStoreHealthCheck : IHealthCheck
{
    private readonly ISchemaStore _schemaStore;
    private readonly ILogger<SchemaStoreHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaStoreHealthCheck"/> class.
    /// </summary>
    public SchemaStoreHealthCheck(
        ISchemaStore schemaStore,
        ILogger<SchemaStoreHealthCheck> logger)
    {
        _schemaStore = schemaStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify system schemas are loaded
            var systemSchemas = await _schemaStore.GetSystemSchemasAsync(cancellationToken);

            if (systemSchemas.Count == 0)
            {
                _logger.LogWarning("Schema store health check failed: No system schemas loaded");
                return HealthCheckResult.Degraded(
                    "No system schemas loaded",
                    data: new Dictionary<string, object>
                    {
                        ["system_schema_count"] = 0
                    });
            }

            // Verify expected system schemas are present
            var expectedSchemas = new[] { "installation", "organisation", "participant", "register" };
            var loadedIdentifiers = systemSchemas.Select(s => s.Identifier).ToHashSet();
            var missingSchemas = expectedSchemas.Where(e => !loadedIdentifiers.Contains(e)).ToList();

            if (missingSchemas.Count > 0)
            {
                _logger.LogWarning(
                    "Schema store health check degraded: Missing system schemas: {MissingSchemas}",
                    string.Join(", ", missingSchemas));

                return HealthCheckResult.Degraded(
                    $"Missing system schemas: {string.Join(", ", missingSchemas)}",
                    data: new Dictionary<string, object>
                    {
                        ["system_schema_count"] = systemSchemas.Count,
                        ["missing_schemas"] = missingSchemas
                    });
            }

            return HealthCheckResult.Healthy(
                "Schema store is healthy",
                data: new Dictionary<string, object>
                {
                    ["system_schema_count"] = systemSchemas.Count,
                    ["loaded_schemas"] = loadedIdentifiers.ToList()
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema store health check failed with exception");
            return HealthCheckResult.Unhealthy(
                "Schema store health check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message
                });
        }
    }
}
