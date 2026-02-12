// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sorcha.Demo.Configuration;

namespace Sorcha.Demo.Services.Auth;

/// <summary>
/// Handles service principal authentication for the Demo application
/// </summary>
public class DemoAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DemoAuthService> _logger;
    private readonly SorchaApiConfiguration _config;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public DemoAuthService(
        HttpClient httpClient,
        ILogger<DemoAuthService> logger,
        SorchaApiConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Gets the current access token, refreshing if necessary
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Check if we have valid credentials configured
        if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret))
        {
            _logger.LogDebug("No service principal credentials configured");
            return null;
        }

        // Check if token is still valid (with 1 minute buffer)
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow.AddMinutes(1) < _tokenExpiry)
        {
            return _accessToken;
        }

        // Get new token
        return await RefreshTokenAsync(ct);
    }

    /// <summary>
    /// Forces a token refresh
    /// </summary>
    public async Task<string?> RefreshTokenAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret))
        {
            return null;
        }

        try
        {
            var tokenUrl = _config.GetAuthTokenUrl();
            _logger.LogDebug("Requesting token from {TokenUrl}", tokenUrl);

            // Use form-urlencoded as required by the token endpoint
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _config.ClientId,
                ["client_secret"] = _config.ClientSecret,
                ["grant_type"] = "client_credentials"
            });

            var response = await _httpClient.PostAsync(tokenUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Token request failed: {StatusCode} - {Error}",
                    response.StatusCode, error);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogError("Token response was empty or invalid");
                return null;
            }

            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // Buffer

            _logger.LogInformation("Successfully obtained access token (expires in {ExpiresIn}s)",
                tokenResponse.ExpiresIn);

            return _accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain access token");
            return null;
        }
    }

    /// <summary>
    /// Checks if authentication is configured
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrEmpty(_config.ClientId) && !string.IsNullOrEmpty(_config.ClientSecret);

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
