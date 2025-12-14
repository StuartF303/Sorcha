using Sorcha.Admin.Services.Authentication;
using Sorcha.Admin.Services.Configuration;
using System.Net.Http.Headers;

namespace Sorcha.Admin.Services.Http;

/// <summary>
/// HTTP message handler that automatically adds JWT Bearer tokens to outgoing requests.
/// Integrates with the authentication system to fetch tokens for the active profile.
/// </summary>
public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authService;
    private readonly IConfigurationService _configService;

    public AuthenticatedHttpMessageHandler(
        IAuthenticationService authService,
        IConfigurationService configService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <summary>
    /// Intercepts HTTP requests and adds the Authorization header with JWT token.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get active profile
            var profile = await _configService.GetActiveProfileAsync();

            if (profile != null)
            {
                // Get access token for this profile (auto-refreshes if needed)
                var token = await _authService.GetAccessTokenAsync(profile.Name);

                if (!string.IsNullOrEmpty(token))
                {
                    // Add Authorization header with Bearer token
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
        catch
        {
            // Failed to get token - continue without authentication
            // The server will return 401 if authentication is required
        }

        // Continue with the request
        return await base.SendAsync(request, cancellationToken);
    }
}
