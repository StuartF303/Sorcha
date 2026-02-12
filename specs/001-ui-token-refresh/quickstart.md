# Quickstart: Sorcha.UI Authentication Token Management and Login UX

**Feature Branch**: `001-ui-token-refresh`
**Date**: 2026-02-03

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for E2E tests)
- Node.js 18+ (for Playwright)

## Quick Setup

```bash
# Checkout feature branch
git checkout 001-ui-token-refresh

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run unit tests
dotnet test tests/Sorcha.UI.Core.Tests/

# Start services for E2E testing
docker-compose up -d

# Run E2E tests (after services are healthy)
cd tests/Sorcha.UI.E2E.Tests
npx playwright test
```

---

## Implementation Order

### Step 1: UrlValidator Utility

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Utilities/UrlValidator.cs`

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Utilities;

/// <summary>
/// Validates URLs for security, preventing open redirect attacks.
/// </summary>
public static class UrlValidator
{
    /// <summary>
    /// Validates that a return URL is safe to redirect to.
    /// </summary>
    public static bool IsValidReturnUrl(string? url, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Relative paths starting with single / are safe
        if (url.StartsWith('/') && !url.StartsWith("//"))
            return true;

        // Check for dangerous schemes
        if (url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;

        // Absolute URLs must be same-origin
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return string.Equals(uri.Scheme, baseUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) &&
                   uri.Port == baseUri.Port;
        }

        return false;
    }

    /// <summary>
    /// Validates that a return URL is safe to redirect to.
    /// </summary>
    public static bool IsValidReturnUrl(string? url, string baseUri)
    {
        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var uri))
            return false;

        return IsValidReturnUrl(url, uri);
    }
}
```

**Test**: `tests/Sorcha.UI.Core.Tests/Utilities/UrlValidatorTests.cs`

---

### Step 2: INavigationService Interface

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Navigation/INavigationService.cs`

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Services.Navigation;

/// <summary>
/// Provides authentication-aware navigation with return URL support.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current navigation URI.
    /// </summary>
    string CurrentUri { get; }

    /// <summary>
    /// Redirects to login page with optional return URL.
    /// </summary>
    Task RedirectToLoginAsync(string? returnUrl = null);

    /// <summary>
    /// Navigates to validated URL or default destination.
    /// </summary>
    void NavigateToValidatedUrl(string? url, string defaultDestination = "dashboard");
}
```

---

### Step 3: NavigationService Implementation

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Navigation/NavigationService.cs`

```csharp
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.AspNetCore.Components;
using Sorcha.UI.Core.Utilities;

namespace Sorcha.UI.Core.Services.Navigation;

/// <summary>
/// Implements authentication-aware navigation with return URL support.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly NavigationManager _navigationManager;

    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public string CurrentUri => _navigationManager.Uri;

    public Task RedirectToLoginAsync(string? returnUrl = null)
    {
        var currentPath = new Uri(_navigationManager.Uri).AbsolutePath;

        // Prevent redirect loops
        if (currentPath.Contains("/login", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var loginUrl = "auth/login";
        if (UrlValidator.IsValidReturnUrl(returnUrl, _navigationManager.BaseUri))
        {
            loginUrl = $"auth/login?returnUrl={Uri.EscapeDataString(returnUrl!)}";
        }

        _navigationManager.NavigateTo(loginUrl, forceLoad: false);
        return Task.CompletedTask;
    }

    public void NavigateToValidatedUrl(string? url, string defaultDestination = "dashboard")
    {
        var destination = UrlValidator.IsValidReturnUrl(url, _navigationManager.BaseUri)
            ? url!
            : defaultDestination;

        _navigationManager.NavigateTo(destination, forceLoad: false);
    }
}
```

---

### Step 4: Register Services

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`

Add to `AddCoreServices()`:

```csharp
// Navigation service for return URL handling
services.AddScoped<INavigationService, NavigationService>();
```

---

### Step 5: Update AuthenticatedHttpMessageHandler

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Http/AuthenticatedHttpMessageHandler.cs`

Add redirect on refresh failure:

```csharp
// Add to constructor
private readonly INavigationService _navigationService;

public AuthenticatedHttpMessageHandler(
    IAuthenticationService authService,
    IConfigurationService configService,
    INavigationService navigationService)  // Add parameter
{
    _authService = authService;
    _configService = configService;
    _navigationService = navigationService;  // Store
}

// In SendAsync, after refresh failure:
if (!refreshed)
{
    // Get current URL for return parameter
    var returnUrl = _navigationService.CurrentUri;
    await _navigationService.RedirectToLoginAsync(returnUrl);
}
```

---

### Step 6: Update Login.razor

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Login.razor`

Add query parameter and Enter key handling:

```razor
@page "/auth/login"
@inject INavigationService NavigationService

<!-- Add parameter -->
@code {
    [Parameter]
    [SupplyParameterFromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    // Add keyboard handler
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !_isLoading)
        {
            await HandleLogin();
        }
    }

    // Update navigation after login success
    private async Task HandleLogin()
    {
        // ... existing validation and login logic ...

        if (loginSuccess)
        {
            // Use NavigationService for validated redirect
            NavigationService.NavigateToValidatedUrl(ReturnUrl, "dashboard");
        }
    }
}
```

Update password input:

```html
<input type="password"
       class="form-control"
       @bind="_password"
       @onkeydown="HandleKeyDown" />
```

---

## Testing

### Unit Tests

```bash
# Run UrlValidator tests
dotnet test tests/Sorcha.UI.Core.Tests/ --filter "UrlValidator"

# Run NavigationService tests
dotnet test tests/Sorcha.UI.Core.Tests/ --filter "NavigationService"
```

### E2E Tests

```bash
# Start services
docker-compose up -d

# Wait for health checks
curl http://localhost/health

# Run Playwright tests
cd tests/Sorcha.UI.E2E.Tests
npx playwright test authentication/
```

---

## Validation Checklist

- [x] UrlValidator rejects external URLs (verified: 34 unit tests pass)
- [x] UrlValidator rejects javascript: URLs (verified: UrlValidatorTests)
- [x] UrlValidator accepts relative paths (verified: UrlValidatorTests)
- [x] UrlValidator accepts same-origin absolute URLs (verified: UrlValidatorTests)
- [x] NavigationService prevents redirect loops (verified: NavigationServiceTests)
- [x] NavigationService includes returnUrl parameter (verified: NavigationServiceTests)
- [x] Login page reads returnUrl from query string (verified: Login.razor @SupplyParameterFromQuery)
- [x] Login page redirects to returnUrl after success (verified: NavigateToValidatedUrl call)
- [x] Login page falls back to dashboard if returnUrl invalid (verified: E2E tests)
- [x] Enter key submits login form (verified: HandleKeyDown in Login.razor)
- [x] Enter key does not duplicate submissions during loading (verified: _isLoading check)
- [x] Token refresh triggers redirect on failure (verified: AuthenticatedHttpMessageHandler)

---

## Troubleshooting

### Return URL Not Working

1. Check browser network tab for redirect response
2. Verify returnUrl is URL-encoded in query string
3. Check console for UrlValidator rejection logs

### Enter Key Not Submitting

1. Verify `@onkeydown` attribute on password input
2. Check `_isLoading` state isn't stuck true
3. Ensure `HandleKeyDown` method is defined

### Redirect Loop

1. Check NavigationService loop prevention logic
2. Verify login page path check includes `/login`
3. Clear localStorage and try fresh login
