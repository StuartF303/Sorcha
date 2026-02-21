// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Sorcha.ServiceClients.Did;

/// <summary>
/// Resolves did:web DIDs by fetching a DID Document over HTTPS.
///   - did:web:{domain}               → GET https://{domain}/.well-known/did.json
///   - did:web:{domain}:{path}:{...}  → GET https://{domain}/{path}/.../did.json
/// </summary>
public class WebDidResolver : IDidResolver
{
    private const string Method = "web";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebDidResolver> _logger;

    public WebDidResolver(HttpClient httpClient, ILogger<WebDidResolver> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool CanResolve(string didMethod) =>
        string.Equals(didMethod, Method, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<DidDocument?> ResolveAsync(string did, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(did))
            return null;

        var url = BuildUrl(did);
        if (url is null)
            return null;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(RequestTimeout);

        try
        {
            using var response = await _httpClient.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "did:web resolution failed for {Did}: HTTP {StatusCode}",
                    did, (int)response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var doc = JsonSerializer.Deserialize<DidDocument>(json, JsonOptions);

            if (doc is null)
            {
                _logger.LogWarning("did:web resolution returned null document for {Did}", did);
                return null;
            }

            // Verify the resolved document ID matches the DID we requested
            if (!string.Equals(doc.Id, did, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "did:web document ID mismatch: expected {Expected}, got {Actual}",
                    did, doc.Id);
                return null;
            }

            return doc;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("did:web resolution timed out for {Did}", did);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "did:web resolution network error for {Did}", did);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "did:web resolution returned invalid JSON for {Did}", did);
            return null;
        }
    }

    /// <summary>
    /// Builds the HTTPS URL for the DID Document.
    /// Colons after the domain are converted to path separators.
    /// Percent-encoded domain segments are decoded.
    /// </summary>
    private Uri? BuildUrl(string did)
    {
        // did:web:{domain}  or  did:web:{domain}:{path1}:{path2}
        var parts = did.Split(':');
        if (parts.Length < 3)
        {
            _logger.LogWarning("Invalid did:web format: {Did}", did);
            return null;
        }

        // Decode percent-encoded domain (e.g., example%2Ecom → example.com)
        var domain = Uri.UnescapeDataString(parts[2]);

        if (string.IsNullOrWhiteSpace(domain))
        {
            _logger.LogWarning("did:web has empty domain: {Did}", did);
            return null;
        }

        // Build path from remaining segments
        string path;
        if (parts.Length > 3)
        {
            var pathSegments = parts[3..].Select(Uri.UnescapeDataString);
            path = $"https://{domain}/{string.Join('/', pathSegments)}/did.json";
        }
        else
        {
            path = $"https://{domain}/.well-known/did.json";
        }

        if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Could not construct valid URL from did:web {Did}", did);
            return null;
        }

        // HTTPS only
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogWarning("did:web requires HTTPS, got {Scheme} for {Did}", uri.Scheme, did);
            return null;
        }

        return uri;
    }
}
