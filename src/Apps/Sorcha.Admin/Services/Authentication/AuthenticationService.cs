using Microsoft.JSInterop;
using Sorcha.Admin.Models.Authentication;
using Sorcha.Admin.Models.Configuration;
using Sorcha.Admin.Services.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Authentication service implementation for Sorcha Admin.
/// Handles OAuth2 Password Grant flow with the Tenant Service.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfigurationService _configService;
    private readonly ITokenCache _tokenCache;
    private readonly IJSRuntime _jsRuntime;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        IConfigurationService configService,
        ITokenCache tokenCache,
        IJSRuntime jsRuntime)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    private async Task LogAsync(string level, string message, object? data = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync($"console.{level}", $"[AuthenticationService] {message}", data ?? "");
        }
        catch
        {
            // Ignore logging errors
        }
    }

    /// <summary>
    /// Authenticates a user with username and password.
    /// Implements OAuth2 Password Grant flow (RFC 6749 Section 4.3).
    /// </summary>
    public async Task<TokenResponse> LoginAsync(LoginRequest request, string profileName)
    {
        await LogAsync("info", $"LoginAsync called for profile: '{profileName}', username: '{request?.Username}'");

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrEmpty(profileName))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));

        // Get profile configuration
        await LogAsync("debug", $"Getting profile configuration for '{profileName}'...");
        var profile = await _configService.GetProfileAsync(profileName);
        if (profile == null)
        {
            await LogAsync("error", $"Profile '{profileName}' not found");
            throw new InvalidOperationException($"Profile '{profileName}' not found.");
        }

        await LogAsync("debug", $"Profile found: AuthTokenUrl = '{profile.AuthTokenUrl}'");

        // Create HTTP client
        var httpClient = _httpClientFactory.CreateClient();

        // Build OAuth2 token request (application/x-www-form-urlencoded)
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = request.Username,
            ["password"] = request.Password,
            ["client_id"] = request.ClientId ?? profile.DefaultClientId ?? "sorcha-admin"
        };

        if (!string.IsNullOrEmpty(request.Scope))
            formData["scope"] = request.Scope;

        var content = new FormUrlEncodedContent(formData);

        try
        {
            // POST to token endpoint
            await LogAsync("debug", $"Posting to token endpoint: {profile.AuthTokenUrl}");
            var response = await httpClient.PostAsync(profile.AuthTokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                await LogAsync("error", $"Authentication failed: HTTP {response.StatusCode}", errorContent);
                throw new UnauthorizedAccessException(
                    $"Authentication failed: {response.StatusCode}. {errorContent}");
            }

            await LogAsync("debug", "Authentication successful, parsing token response...");

            // Parse token response
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                await LogAsync("error", "Invalid token response from server");
                throw new InvalidOperationException("Invalid token response from server.");
            }

            await LogAsync("debug", $"Token received: length = {tokenResponse.AccessToken.Length}, expires_in = {tokenResponse.ExpiresIn}");

            // Store token in cache
            var cacheEntry = new TokenCacheEntry
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                Profile = profileName,
                Subject = request.Username
            };

            await LogAsync("info", $"Storing token in cache for profile '{profileName}'...");
            await _tokenCache.SetAsync(profileName, cacheEntry);
            await LogAsync("info", $"✓ Login completed successfully for '{request.Username}'");

            return tokenResponse;
        }
        catch (HttpRequestException ex)
        {
            await LogAsync("error", $"HTTP request failed: {ex.Message}", ex.StackTrace);
            throw new InvalidOperationException(
                $"Failed to connect to authentication server at {profile.AuthTokenUrl}", ex);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            await LogAsync("error", $"Unexpected error during login: {ex.Message}", new {
                ExceptionType = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });
            throw;
        }
    }

    /// <summary>
    /// Gets the access token for the specified profile.
    /// Automatically refreshes if expiring soon.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));

        // Get cached token
        var entry = await _tokenCache.GetAsync(profileName);
        if (entry == null)
            return null;

        // Check if token is expiring soon (within 5 minutes)
        if (entry.IsExpiringSoon(5))
        {
            // Attempt to refresh
            try
            {
                var refreshed = await RefreshTokenAsync(profileName);
                if (refreshed != null)
                    return refreshed.AccessToken;
            }
            catch
            {
                // Refresh failed - return existing token if still valid
                if (!entry.IsExpired)
                    return entry.AccessToken;

                return null;
            }
        }

        return entry.AccessToken;
    }

    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// </summary>
    public async Task<TokenResponse?> RefreshTokenAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));

        // Get cached token
        var entry = await _tokenCache.GetAsync(profileName);
        if (entry == null || string.IsNullOrEmpty(entry.RefreshToken))
            return null;

        // Get profile configuration
        var profile = await _configService.GetProfileAsync(profileName);
        if (profile == null)
            throw new InvalidOperationException($"Profile '{profileName}' not found.");

        // Create HTTP client
        var httpClient = _httpClientFactory.CreateClient();

        // Build OAuth2 refresh token request
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = entry.RefreshToken,
            ["client_id"] = profile.DefaultClientId ?? "sorcha-admin"
        };

        var content = new FormUrlEncodedContent(formData);

        try
        {
            // POST to token endpoint
            var response = await httpClient.PostAsync(profile.AuthTokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                // Refresh failed - clear cached token
                await _tokenCache.ClearAsync(profileName);
                return null;
            }

            // Parse token response
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                return null;

            // Update cache with new token
            var newEntry = new TokenCacheEntry
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken ?? entry.RefreshToken, // Keep old refresh token if not returned
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                Profile = profileName,
                Subject = entry.Subject
            };

            await _tokenCache.SetAsync(profileName, newEntry);

            return tokenResponse;
        }
        catch (HttpRequestException)
        {
            // Network error during refresh - return null
            return null;
        }
    }

    /// <summary>
    /// Checks if the user is authenticated for the specified profile.
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
            return false;

        var token = await GetAccessTokenAsync(profileName);
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Logs out from the specified profile.
    /// </summary>
    public async Task LogoutAsync(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
            throw new ArgumentException("Profile name cannot be null or empty.", nameof(profileName));

        await _tokenCache.ClearAsync(profileName);
    }

    /// <summary>
    /// Logs out from all profiles.
    /// </summary>
    public async Task LogoutAllAsync()
    {
        await _tokenCache.ClearAllAsync();
    }

    // Future extensibility methods (not implemented)

    public Task<DeviceCodeResponse> StartDeviceFlowAsync(string profileName)
    {
        throw new NotImplementedException("Device Code flow not yet implemented.");
    }

    public Task<TokenResponse> PollDeviceFlowAsync(DeviceCodeResponse deviceCode, string profileName)
    {
        throw new NotImplementedException("Device Code flow not yet implemented.");
    }
}
