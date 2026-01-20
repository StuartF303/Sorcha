// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Schemas.DTOs;

namespace Sorcha.Blueprint.Schemas.Services;

/// <summary>
/// Provider that fetches schemas from SchemaStore.org.
/// Implements catalog caching with configurable TTL.
/// </summary>
public class SchemaStoreOrgProvider : IExternalSchemaProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SchemaStoreOrgProvider> _logger;
    private readonly TimeSpan _cacheTtl;

    private List<SchemaStoreCatalogEntry>? _catalogCache;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private const string CatalogUrl = "https://www.schemastore.org/api/json/catalog.json";
    private const string ProviderDisplayName = "SchemaStore.org";

    /// <summary>
    /// Creates a new SchemaStoreOrgProvider.
    /// </summary>
    /// <param name="httpClient">HttpClient for making requests.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="cacheTtl">How long to cache the catalog. Defaults to 1 hour.</param>
    public SchemaStoreOrgProvider(
        HttpClient httpClient,
        ILogger<SchemaStoreOrgProvider> logger,
        TimeSpan? cacheTtl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheTtl = cacheTtl ?? TimeSpan.FromHours(1);
    }

    /// <inheritdoc />
    public string ProviderName => ProviderDisplayName;

    /// <inheritdoc />
    public async Task<ExternalSchemaSearchResponse> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new ExternalSchemaSearchResponse([], 0, ProviderName, query);
        }

        try
        {
            var catalog = await GetCachedCatalogAsync(cancellationToken);

            var matches = catalog
                .Where(entry => MatchesQuery(entry, query))
                .Select(MapToResult)
                .ToList();

            _logger.LogDebug("Search for '{Query}' returned {Count} results from {Provider}",
                query, matches.Count, ProviderName);

            return new ExternalSchemaSearchResponse(
                Results: matches,
                TotalCount: matches.Count,
                Provider: ProviderName,
                Query: query);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to search SchemaStore.org for '{Query}'", query);

            // Return cached results if available
            if (_catalogCache != null)
            {
                var cachedMatches = _catalogCache
                    .Where(entry => MatchesQuery(entry, query))
                    .Select(MapToResult)
                    .ToList();

                return new ExternalSchemaSearchResponse(
                    Results: cachedMatches,
                    TotalCount: cachedMatches.Count,
                    Provider: ProviderName,
                    Query: query,
                    IsPartialResult: true);
            }

            return new ExternalSchemaSearchResponse([], 0, ProviderName, query, IsPartialResult: true);
        }
    }

    /// <inheritdoc />
    public async Task<ExternalSchemaResult?> GetSchemaAsync(string schemaUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(schemaUrl))
        {
            return null;
        }

        try
        {
            // First, find the catalog entry for this schema URL
            var catalog = await GetCachedCatalogAsync(cancellationToken);
            var entry = catalog.FirstOrDefault(e =>
                string.Equals(e.Url, schemaUrl, StringComparison.OrdinalIgnoreCase));

            // Fetch the actual schema content
            _logger.LogDebug("Fetching schema from {Url}", schemaUrl);
            var response = await _httpClient.GetAsync(schemaUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);

            if (entry != null)
            {
                return new ExternalSchemaResult(
                    Name: entry.Name,
                    Description: entry.Description ?? string.Empty,
                    Url: schemaUrl,
                    Provider: ProviderName,
                    FileMatch: entry.FileMatch,
                    Versions: entry.Versions?.Keys.ToList(),
                    Content: content);
            }

            // If not in catalog, create a basic result
            var schemaTitle = ExtractSchemaTitle(content);
            return new ExternalSchemaResult(
                Name: schemaTitle ?? Path.GetFileName(new Uri(schemaUrl).LocalPath),
                Description: ExtractSchemaDescription(content) ?? string.Empty,
                Url: schemaUrl,
                Provider: ProviderName,
                Content: content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch schema from {Url}", schemaUrl);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse schema from {Url}", schemaUrl);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExternalSchemaResult>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var catalog = await GetCachedCatalogAsync(cancellationToken);
            return catalog.Select(MapToResult);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get catalog from SchemaStore.org");

            if (_catalogCache != null)
            {
                return _catalogCache.Select(MapToResult);
            }

            return [];
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, CatalogUrl);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private async Task<List<SchemaStoreCatalogEntry>> GetCachedCatalogAsync(CancellationToken cancellationToken)
    {
        // Check if cache is still valid
        if (_catalogCache != null && DateTimeOffset.UtcNow < _cacheExpiry)
        {
            return _catalogCache;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_catalogCache != null && DateTimeOffset.UtcNow < _cacheExpiry)
            {
                return _catalogCache;
            }

            _logger.LogDebug("Fetching catalog from SchemaStore.org");
            var response = await _httpClient.GetAsync(CatalogUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var catalog = await response.Content.ReadFromJsonAsync<SchemaStoreCatalog>(cancellationToken: cancellationToken);

            _catalogCache = catalog?.Schemas ?? [];
            _cacheExpiry = DateTimeOffset.UtcNow.Add(_cacheTtl);

            _logger.LogInformation("Loaded {Count} schemas from SchemaStore.org catalog", _catalogCache.Count);

            return _catalogCache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static bool MatchesQuery(SchemaStoreCatalogEntry entry, string query)
    {
        var lowerQuery = query.ToLowerInvariant();

        // Search in name
        if (entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Search in description
        if (!string.IsNullOrEmpty(entry.Description) &&
            entry.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Search in file match patterns
        if (entry.FileMatch != null &&
            entry.FileMatch.Any(fm => fm.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static ExternalSchemaResult MapToResult(SchemaStoreCatalogEntry entry)
    {
        return new ExternalSchemaResult(
            Name: entry.Name,
            Description: entry.Description ?? string.Empty,
            Url: entry.Url,
            Provider: ProviderDisplayName,
            FileMatch: entry.FileMatch,
            Versions: entry.Versions?.Keys.ToList());
    }

    private static string? ExtractSchemaTitle(JsonDocument? doc)
    {
        if (doc?.RootElement.TryGetProperty("title", out var title) == true)
        {
            return title.GetString();
        }
        return null;
    }

    private static string? ExtractSchemaDescription(JsonDocument? doc)
    {
        if (doc?.RootElement.TryGetProperty("description", out var desc) == true)
        {
            return desc.GetString();
        }
        return null;
    }

    // DTOs for SchemaStore.org catalog format
    private sealed record SchemaStoreCatalog(List<SchemaStoreCatalogEntry> Schemas);

    private sealed record SchemaStoreCatalogEntry(
        string Name,
        string? Description,
        string Url,
        IReadOnlyList<string>? FileMatch,
        Dictionary<string, string>? Versions);
}
