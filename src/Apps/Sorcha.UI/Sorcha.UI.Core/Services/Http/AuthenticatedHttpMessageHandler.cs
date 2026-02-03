// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Net.Http.Headers;
using Sorcha.UI.Core.Services.Authentication;
using Sorcha.UI.Core.Services.Configuration;
using Sorcha.UI.Core.Services.Navigation;

namespace Sorcha.UI.Core.Services.Http;

/// <summary>
/// HTTP message handler that injects JWT Bearer tokens and handles token refresh.
/// Redirects to login page with return URL when token refresh fails.
/// </summary>
public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IConfigurationService _configurationService;
    private readonly INavigationService _navigationService;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthenticatedHttpMessageHandler(
        IAuthenticationService authenticationService,
        IConfigurationService configurationService,
        INavigationService navigationService)
    {
        _authenticationService = authenticationService;
        _configurationService = configurationService;
        _navigationService = navigationService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get active profile and token
        var profileName = await _configurationService.GetActiveProfileNameAsync();
        var token = await _authenticationService.GetAccessTokenAsync(profileName);

        // Inject Bearer token if available
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Send request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized - attempt token refresh
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Refresh token
                var refreshSucceeded = await _authenticationService.RefreshTokenAsync(profileName);

                if (refreshSucceeded)
                {
                    // Retry request with new token
                    var newToken = await _authenticationService.GetAccessTokenAsync(profileName);
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
                else
                {
                    // Token refresh failed - redirect to login with return URL
                    var returnUrl = _navigationService.CurrentUri;
                    await _navigationService.RedirectToLoginAsync(returnUrl);
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshLock?.Dispose();
        }
        base.Dispose(disposing);
    }
}
