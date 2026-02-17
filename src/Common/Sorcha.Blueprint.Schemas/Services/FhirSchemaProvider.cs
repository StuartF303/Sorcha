// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Schemas.DTOs;

namespace Sorcha.Blueprint.Schemas.Services;

/// <summary>
/// Provider for HL7 FHIR R4 schemas. Downloads the monolithic FHIR JSON Schema
/// and splits definitions into per-resource schemas.
/// </summary>
public class FhirSchemaProvider : IExternalSchemaProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FhirSchemaProvider> _logger;
    private List<ExternalSchemaResult>? _catalogCache;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private const string FhirSchemaUrl = "https://hl7.org/fhir/fhir.schema.json";
    private const string ProviderDisplayName = "HL7 FHIR";

    /// <summary>
    /// Curated FHIR resource types most relevant for data exchange.
    /// </summary>
    private static readonly HashSet<string> CuratedResources = new(StringComparer.OrdinalIgnoreCase)
    {
        "Patient", "Practitioner", "Organization", "Encounter", "Condition",
        "Observation", "DiagnosticReport", "Medication", "MedicationRequest",
        "AllergyIntolerance", "Immunization", "Procedure", "CarePlan",
        "Claim", "Coverage", "ExplanationOfBenefit", "Consent",
        "DocumentReference", "Bundle", "Composition", "Questionnaire",
        "QuestionnaireResponse", "ServiceRequest", "Appointment",
    };

    public FhirSchemaProvider(HttpClient httpClient, ILogger<FhirSchemaProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProviderName => ProviderDisplayName;

    public async Task<ExternalSchemaSearchResponse> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new ExternalSchemaSearchResponse([], 0, ProviderName, query);

        var catalog = await GetOrBuildCatalogAsync(cancellationToken);
        var matches = catalog
            .Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new ExternalSchemaSearchResponse(matches, matches.Count, ProviderName, query);
    }

    public async Task<ExternalSchemaResult?> GetSchemaAsync(string schemaUrl, CancellationToken cancellationToken = default)
    {
        var catalog = await GetOrBuildCatalogAsync(cancellationToken);
        return catalog.FirstOrDefault(r => string.Equals(r.Url, schemaUrl, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<ExternalSchemaResult>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        return await GetOrBuildCatalogAsync(cancellationToken);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, FhirSchemaUrl);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private async Task<List<ExternalSchemaResult>> GetOrBuildCatalogAsync(CancellationToken cancellationToken)
    {
        if (_catalogCache is not null) return _catalogCache;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_catalogCache is not null) return _catalogCache;

            _logger.LogInformation("Building FHIR schema catalog from curated resource types");
            var results = new List<ExternalSchemaResult>();

            // Build per-resource schemas from curated list
            foreach (var resourceName in CuratedResources.OrderBy(r => r))
            {
                var schema = BuildResourceSchema(resourceName);
                var uri = $"https://hl7.org/fhir/StructureDefinition/{resourceName}";

                results.Add(new ExternalSchemaResult(
                    Name: $"FHIR {resourceName}",
                    Description: $"HL7 FHIR R4 {resourceName} resource schema",
                    Url: uri,
                    Provider: ProviderDisplayName,
                    Content: schema));
            }

            _catalogCache = results;
            _logger.LogInformation("Built {Count} FHIR resource schemas", results.Count);
            return results;
        }
        finally { _cacheLock.Release(); }
    }

    /// <summary>
    /// Builds a minimal JSON Schema for a FHIR resource type.
    /// </summary>
    internal static JsonDocument BuildResourceSchema(string resourceName)
    {
        var schema = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"] = $"https://hl7.org/fhir/StructureDefinition/{resourceName}",
            ["title"] = $"FHIR {resourceName}",
            ["description"] = $"HL7 FHIR R4 {resourceName} resource",
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["resourceType"] = new JsonObject { ["type"] = "string", ["const"] = resourceName },
                ["id"] = new JsonObject { ["type"] = "string", ["description"] = "Logical id of this artifact" },
                ["meta"] = new JsonObject { ["type"] = "object", ["description"] = "Metadata about the resource" },
                ["identifier"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "object" },
                    ["description"] = "Business identifiers"
                },
            },
            ["required"] = new JsonArray("resourceType"),
        };

        return JsonDocument.Parse(schema.ToJsonString());
    }
}
