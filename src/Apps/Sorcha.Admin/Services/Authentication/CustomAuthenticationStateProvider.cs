using Microsoft.AspNetCore.Components.Authorization;
using Sorcha.Admin.Services.Configuration;
using System.Security.Claims;

namespace Sorcha.Admin.Services.Authentication;

/// <summary>
/// Custom authentication state provider for Sorcha Admin.
/// Integrates JWT token authentication with Blazor's authorization system.
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthenticationService _authService;
    private readonly IConfigurationService _configService;

    public CustomAuthenticationStateProvider(
        IAuthenticationService authService,
        IConfigurationService configService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <summary>
    /// Gets the current authentication state by checking for a valid token.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Get active profile
            var profile = await _configService.GetActiveProfileAsync();
            if (profile == null)
            {
                return CreateAnonymousState();
            }

            // Get access token
            var token = await _authService.GetAccessTokenAsync(profile.Name);
            if (string.IsNullOrEmpty(token))
            {
                return CreateAnonymousState();
            }

            // Parse JWT to extract claims
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            // Error parsing token or checking authentication - return anonymous
            return CreateAnonymousState();
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
