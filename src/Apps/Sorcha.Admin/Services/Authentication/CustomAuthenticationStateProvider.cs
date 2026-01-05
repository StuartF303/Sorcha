using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Sorcha.Admin.Services.Configuration;
using System.Security.Claims;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Custom authentication state provider for Sorcha Admin.
/// Integrates JWT token authentication with Blazor's authorization system.
/// Handles both server-side prerendering and client-side interactive rendering.
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthenticationService _authService;
    private readonly IConfigurationService _configService;
    private readonly IJSRuntime _jsRuntime;

    public CustomAuthenticationStateProvider(
        IAuthenticationService authService,
        IConfigurationService configService,
        IJSRuntime jsRuntime)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <summary>
    /// Gets the current authentication state by checking for a valid token.
    /// During server-side prerendering, returns anonymous state since JSInterop is not available.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await LogAsync("info", "[CustomAuthStateProvider] GetAuthenticationStateAsync called");

        try
        {
            // Get active profile
            await LogAsync("debug", "[CustomAuthStateProvider] Retrieving active profile...");
            var profile = await _configService.GetActiveProfileAsync();
            if (profile == null)
            {
                await LogAsync("warn", "[CustomAuthStateProvider] No active profile found - returning anonymous");
                return CreateAnonymousState();
            }
            await LogAsync("info", $"[CustomAuthStateProvider] Active profile: {profile.Name}");

            // Get access token
            await LogAsync("debug", $"[CustomAuthStateProvider] Retrieving access token for profile: {profile.Name}");
            var token = await _authService.GetAccessTokenAsync(profile.Name);
            if (string.IsNullOrEmpty(token))
            {
                await LogAsync("warn", "[CustomAuthStateProvider] No access token found - returning anonymous");
                return CreateAnonymousState();
            }
            await LogAsync("info", $"[CustomAuthStateProvider] Access token retrieved (length: {token.Length})");

            // Parse JWT to extract claims
            await LogAsync("debug", "[CustomAuthStateProvider] Parsing JWT claims...");
            var claims = ParseClaimsFromJwt(token);
            var claimsList = claims.ToList();
            await LogAsync("info", $"[CustomAuthStateProvider] Parsed {claimsList.Count} claims");

            foreach (var claim in claimsList)
            {
                await LogAsync("debug", $"[CustomAuthStateProvider] Claim: {claim.Type} = {claim.Value}");
            }

            var identity = new ClaimsIdentity(claimsList, "jwt");
            var user = new ClaimsPrincipal(identity);

            await LogAsync("info", $"[CustomAuthStateProvider] Authentication successful - User: {user.Identity?.Name ?? "Unknown"}, IsAuthenticated: {user.Identity?.IsAuthenticated}");
            return new AuthenticationState(user);
        }
        catch (InvalidOperationException ex)
        {
            // JSInterop not available during prerendering - return anonymous
            // Auth state will be loaded after interactive render on client
            await LogAsync("warn", $"[CustomAuthStateProvider] JSInterop not available (prerendering): {ex.Message}");
            return CreateAnonymousState();
        }
        catch (JSException ex)
        {
            // JavaScript error during auth check - return anonymous
            await LogAsync("error", $"[CustomAuthStateProvider] JavaScript error: {ex.Message}");
            return CreateAnonymousState();
        }
        catch (Exception ex)
        {
            // Other error parsing token or checking authentication - return anonymous
            await LogAsync("error", $"[CustomAuthStateProvider] Unexpected error: {ex.GetType().Name} - {ex.Message}");
            return CreateAnonymousState();
        }
    }

    /// <summary>
    /// Logs a message to the browser console via JSInterop.
    /// </summary>
    private async Task LogAsync(string level, string message)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync($"console.{level}", message);
        }
        catch
        {
            // Ignore logging errors (JSInterop may not be available)
        }
    }

    /// <summary>
    /// Notifies Blazor that the authentication state has changed (login/logout).
    /// Call this after login or logout operations.
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Parses claims from a JWT token.
    /// This is a simple implementation that decodes the payload without signature verification
    /// (signature verification happens on the server side).
    /// </summary>
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();

        try
        {
            // JWT format: header.payload.signature
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return claims;

            // Decode payload (Base64Url encoded)
            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                // Standard JWT claims mapping
                foreach (var kvp in keyValuePairs)
                {
                    var claimType = kvp.Key switch
                    {
                        "sub" => ClaimTypes.NameIdentifier,
                        "name" => ClaimTypes.Name,
                        "email" => ClaimTypes.Email,
                        "role" => ClaimTypes.Role,
                        _ => kvp.Key
                    };

                    // Handle array values (like roles)
                    if (kvp.Value is System.Text.Json.JsonElement element)
                    {
                        if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in element.EnumerateArray())
                            {
                                // Get the actual string value, not the JSON representation
                                var value = item.ValueKind == System.Text.Json.JsonValueKind.String
                                    ? item.GetString() ?? ""
                                    : item.ToString();
                                claims.Add(new Claim(claimType, value));
                            }
                        }
                        else
                        {
                            // Get string value for non-array elements
                            var value = element.ValueKind == System.Text.Json.JsonValueKind.String
                                ? element.GetString() ?? ""
                                : element.ToString();
                            claims.Add(new Claim(claimType, value));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(claimType, kvp.Value?.ToString() ?? ""));
                    }
                }
            }
        }
        catch
        {
            // Token parsing failed - return empty claims
        }

        return claims;
    }

    /// <summary>
    /// Decodes Base64Url encoded string (JWT payload format).
    /// </summary>
    private byte[] ParseBase64WithoutPadding(string base64)
    {
        // Base64Url to Base64 conversion
        base64 = base64.Replace('-', '+').Replace('_', '/');

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Creates an anonymous (unauthenticated) state.
    /// </summary>
    private AuthenticationState CreateAnonymousState()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(anonymous);
    }
}
