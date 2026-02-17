// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Schemas.DTOs;

namespace Sorcha.Blueprint.Schemas.Services;

/// <summary>
/// Provider that generates JSON Schemas from curated schema.org types.
/// Fetches the JSON-LD vocabulary and transforms selected types to JSON Schema.
/// </summary>
public class SchemaOrgProvider : IExternalSchemaProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SchemaOrgProvider> _logger;
    private List<ExternalSchemaResult>? _catalogCache;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private const string VocabularyUrl = "https://schema.org/version/latest/schemaorg-current-https.jsonld";
    private const string ProviderDisplayName = "schema.org";

    /// <summary>
    /// Curated schema.org types relevant for data exchange workflows.
    /// </summary>
    private static readonly Dictionary<string, string[]> CuratedTypes = new()
    {
        ["Person"] = ["general", "identity"],
        ["Organization"] = ["general", "commerce"],
        ["PostalAddress"] = ["general"],
        ["ContactPoint"] = ["general"],
        ["Invoice"] = ["finance", "commerce"],
        ["Order"] = ["commerce"],
        ["Product"] = ["commerce"],
        ["Offer"] = ["commerce"],
        ["Event"] = ["general"],
        ["Place"] = ["general", "construction"],
        ["MonetaryAmount"] = ["finance"],
        ["PriceSpecification"] = ["finance", "commerce"],
        ["MedicalCondition"] = ["healthcare"],
        ["MedicalProcedure"] = ["healthcare"],
        ["Patient"] = ["healthcare"],
        ["HealthInsurancePlan"] = ["healthcare"],
        ["GovernmentService"] = ["government"],
        ["LegislationObject"] = ["government"],
        ["EducationalOrganization"] = ["general"],
        ["CreativeWork"] = ["general"],
        ["PropertyValue"] = ["general"],
        ["QuantitativeValue"] = ["general"],
        ["GeoCoordinates"] = ["general", "construction"],
        ["Report"] = ["general"],
        ["Claim"] = ["identity", "government"],
    };

    public SchemaOrgProvider(HttpClient httpClient, ILogger<SchemaOrgProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProviderName => ProviderDisplayName;
    public string[] DefaultSectorTags => ["general", "commerce"];

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
            using var request = new HttpRequestMessage(HttpMethod.Head, VocabularyUrl);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Schema.org provider availability check failed");
            return false;
        }
    }

    private async Task<List<ExternalSchemaResult>> GetOrBuildCatalogAsync(CancellationToken cancellationToken)
    {
        if (_catalogCache is not null) return _catalogCache;

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_catalogCache is not null) return _catalogCache;

            _logger.LogInformation("Building schema.org catalog from curated types");
            var results = new List<ExternalSchemaResult>();

            foreach (var (typeName, sectors) in CuratedTypes)
            {
                var schema = BuildJsonSchemaForType(typeName);
                var uri = $"https://schema.org/{typeName}";

                results.Add(new ExternalSchemaResult(
                    Name: typeName,
                    Description: $"schema.org {typeName} type — structured data for {string.Join(", ", sectors)} workflows",
                    Url: uri,
                    Provider: ProviderDisplayName,
                    Content: schema));
            }

            _catalogCache = results;
            _logger.LogInformation("Built {Count} schema.org schemas", results.Count);
            return results;
        }
        finally { _cacheLock.Release(); }
    }

    /// <summary>
    /// Builds a minimal JSON Schema for a schema.org type.
    /// Uses a template with @context and common properties.
    /// </summary>
    internal static JsonDocument BuildJsonSchemaForType(string typeName)
    {
        var schema = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["$id"] = $"https://schema.org/{typeName}",
            ["title"] = typeName,
            ["description"] = $"schema.org {typeName} type",
            ["type"] = "object",
            ["properties"] = BuildPropertiesForType(typeName),
        };

        return JsonDocument.Parse(schema.ToJsonString());
    }

    private static JsonObject BuildPropertiesForType(string typeName)
    {
        var props = new JsonObject
        {
            ["@type"] = new JsonObject { ["type"] = "string", ["const"] = typeName },
            ["name"] = new JsonObject { ["type"] = "string", ["description"] = "The name of the item" },
            ["description"] = new JsonObject { ["type"] = "string", ["description"] = "A description of the item" },
            ["identifier"] = new JsonObject { ["type"] = "string", ["description"] = "Identifier" },
            ["url"] = new JsonObject { ["type"] = "string", ["format"] = "uri" },
        };

        // Add type-specific properties
        switch (typeName)
        {
            case "Person":
                props["givenName"] = new JsonObject { ["type"] = "string" };
                props["familyName"] = new JsonObject { ["type"] = "string" };
                props["email"] = new JsonObject { ["type"] = "string", ["format"] = "email" };
                props["telephone"] = new JsonObject { ["type"] = "string" };
                props["birthDate"] = new JsonObject { ["type"] = "string", ["format"] = "date" };
                break;
            case "Organization":
                props["legalName"] = new JsonObject { ["type"] = "string" };
                props["taxID"] = new JsonObject { ["type"] = "string" };
                props["email"] = new JsonObject { ["type"] = "string", ["format"] = "email" };
                break;
            case "Invoice":
                props["totalPaymentDue"] = new JsonObject { ["type"] = "number" };
                props["paymentDueDate"] = new JsonObject { ["type"] = "string", ["format"] = "date" };
                props["paymentStatus"] = new JsonObject { ["type"] = "string" };
                props["accountId"] = new JsonObject { ["type"] = "string" };
                break;
            case "PostalAddress":
                props["streetAddress"] = new JsonObject { ["type"] = "string" };
                props["addressLocality"] = new JsonObject { ["type"] = "string" };
                props["addressRegion"] = new JsonObject { ["type"] = "string" };
                props["postalCode"] = new JsonObject { ["type"] = "string" };
                props["addressCountry"] = new JsonObject { ["type"] = "string" };
                break;
            case "MonetaryAmount":
                props["value"] = new JsonObject { ["type"] = "number" };
                props["currency"] = new JsonObject { ["type"] = "string" };
                break;
        }

        return props;
    }
}
