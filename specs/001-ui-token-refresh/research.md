# Research: Sorcha.UI Authentication Token Management and Login UX

**Feature Branch**: `001-ui-token-refresh`
**Date**: 2026-02-03

## Executive Summary

Research confirms the existing authentication infrastructure is well-designed and requires minimal changes. The main gaps are: (1) no return URL handling on authentication failure, (2) no Enter key submission on login form, and (3) the redirect on 401 failure removes the token but doesn't navigate to login.

---

## Current Implementation Analysis

### 1. Token Refresh Mechanism

**Decision**: Use existing proactive refresh in `GetAccessTokenAsync()`

**Rationale**: The current implementation already has a dual refresh strategy:
- **Proactive**: `GetAccessTokenAsync()` checks `IsNearExpiration` (5-minute threshold) and refreshes before making requests
- **Reactive**: `AuthenticatedHttpMessageHandler` catches 401 responses and attempts refresh

**Current Code** (`AuthenticationService.cs:78-95`):
```csharp
public async Task<string?> GetAccessTokenAsync(string? profileName = null)
{
    var entry = await _tokenCache.GetTokenAsync(profileName ?? await GetActiveProfile());
    if (entry == null) return null;

    // Proactive refresh when near expiration
    if (entry.IsNearExpiration && !string.IsNullOrEmpty(entry.RefreshToken))
    {
        await RefreshTokenAsync(profileName);
        entry = await _tokenCache.GetTokenAsync(profileName ?? await GetActiveProfile());
    }

    return entry?.AccessToken;
}
```

**Threshold** (`TokenCacheEntry.cs:41`):
```csharp
public bool IsNearExpiration => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
```

**Alternatives Considered**:
- Background timer-based refresh: Rejected - adds complexity, battery drain on mobile, existing reactive approach sufficient
- Configurable threshold: Deferred - 5 minutes works for typical 15-60 minute token lifetimes

---

### 2. Redirect on Authentication Failure

**Decision**: Add `RedirectToLoginAsync(returnUrl)` method to `INavigationService`

**Rationale**: Current behavior on refresh failure:
1. `AuthenticatedHttpMessageHandler` calls `RefreshTokenAsync()`
2. On failure, token is removed from cache
3. **GAP**: No navigation to login page with return URL

**Current Code** (`AuthenticatedHttpMessageHandler.cs:42-66`):
```csharp
if (response.StatusCode == HttpStatusCode.Unauthorized)
{
    await _refreshLock.WaitAsync();
    try
    {
        var refreshed = await _authService.RefreshTokenAsync(profileName);
        if (refreshed)
        {
            // Retry with new token
            var newToken = await _authService.GetAccessTokenAsync(profileName);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
            return await base.SendAsync(request, cancellationToken);
        }
        // GAP: No redirect to login here - just returns 401 response
    }
    finally
    {
        _refreshLock.Release();
    }
}
```

**Implementation Approach**:
1. Create `INavigationService` with `RedirectToLoginAsync(string? returnUrl)` method
2. Inject into `AuthenticatedHttpMessageHandler`
3. On refresh failure, call redirect with current URL as return parameter

**Alternatives Considered**:
- Event-based approach (publish auth failure event): Rejected - more complex, harder to test
- Direct NavigationManager injection: Rejected - creates tight coupling, harder to mock

---

### 3. Return URL Security Validation

**Decision**: Create `UrlValidator.IsValidReturnUrl(url, baseUri)` utility

**Rationale**: Return URL validation is a security requirement (FR-007) to prevent open redirect attacks.

**Validation Rules**:
1. URL must be relative path (starts with `/`) OR
2. URL must be same-origin (same scheme + host + port as base URI)
3. Reject: external domains, javascript: URLs, data: URLs

**Implementation**:
```csharp
public static class UrlValidator
{
    public static bool IsValidReturnUrl(string? url, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        // Relative paths are always safe
        if (url.StartsWith('/') && !url.StartsWith("//")) return true;

        // Check same-origin for absolute URLs
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Scheme == baseUri.Scheme &&
                   uri.Host == baseUri.Host &&
                   uri.Port == baseUri.Port;
        }

        return false;
    }
}
```

**Alternatives Considered**:
- Whitelist approach: Rejected - too restrictive for dynamic SPA routes
- Regex validation: Rejected - error-prone, harder to maintain

---

### 4. Login Form Enhancement

**Decision**: Add `@onkeydown` handler with Enter key detection on password field

**Rationale**: Current login form uses standard HTML without keyboard submission handling.

**Current Form** (`Login.razor:42-96`):
```html
<div class="form-group mb-3">
    <label class="form-label">Password</label>
    <input type="password" class="form-control" @bind="_password" />
</div>
<button type="button" class="btn btn-primary" @onclick="HandleLogin" disabled="@_isLoading">
    Sign In
</button>
```

**Enhanced Implementation**:
```html
<input type="password"
       class="form-control"
       @bind="_password"
       @onkeydown="HandleKeyDown" />
```

```csharp
private async Task HandleKeyDown(KeyboardEventArgs e)
{
    if (e.Key == "Enter" && !_isLoading)
    {
        await HandleLogin();
    }
}
```

**Alternatives Considered**:
- HTML form with `type="submit"`: Requires restructuring, may conflict with Blazor binding
- MudBlazor `MudTextField` with `OnKeyDown`: Would require migrating entire form to MudBlazor (scope creep)

---

### 5. Return URL Consumption on Login

**Decision**: Read `returnUrl` from query string, redirect after successful login

**Rationale**: Login page needs to:
1. Parse `returnUrl` query parameter on initialization
2. Validate URL using `UrlValidator`
3. Redirect to validated URL after successful login (or default to dashboard)

**Implementation Flow**:
```csharp
[Parameter]
[SupplyParameterFromQuery(Name = "returnUrl")]
public string? ReturnUrl { get; set; }

private async Task HandleLogin()
{
    // ... existing login logic ...

    if (loginSuccess)
    {
        var destination = UrlValidator.IsValidReturnUrl(ReturnUrl, NavigationManager.BaseUri)
            ? ReturnUrl
            : "dashboard";
        NavigationManager.NavigateTo(destination, forceLoad: false);
    }
}
```

---

### 6. Preventing Redirect Loops

**Decision**: Check if already on login page before redirecting

**Rationale**: Edge case where token expires while on login page should not cause infinite redirect.

**Implementation**:
```csharp
public async Task RedirectToLoginAsync(string? returnUrl = null)
{
    var currentPath = new Uri(_navigationManager.Uri).AbsolutePath;

    // Don't redirect if already on login page
    if (currentPath.EndsWith("/login", StringComparison.OrdinalIgnoreCase))
        return;

    var loginUrl = UrlValidator.IsValidReturnUrl(returnUrl, _navigationManager.BaseUri)
        ? $"auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}"
        : "auth/login";

    _navigationManager.NavigateTo(loginUrl, forceLoad: false);
}
```

---

## Technology Best Practices

### Blazor WASM Authentication Patterns

1. **Use `AuthenticationStateProvider`** for auth state - already implemented
2. **Cascading authentication state** via `CascadingAuthenticationState` - already in place
3. **`IAuthorizationService`** for complex policies - not needed for this feature
4. **Token storage in localStorage** with encryption - already implemented

### Open Redirect Prevention

1. **OWASP recommendation**: Validate against whitelist or same-origin
2. **ASP.NET Core pattern**: Use `LocalRedirect()` which only accepts local URLs
3. **Our approach**: `UrlValidator.IsValidReturnUrl()` checks relative or same-origin

### Keyboard Accessibility

1. **WCAG 2.1 Success Criterion 2.1.1**: All functionality available from keyboard
2. **Form submission**: Enter key should submit when focus is on any input field
3. **Our approach**: Handle Enter key on password field (last input before submit)

---

## Dependencies

| Dependency | Version | Purpose | Status |
|------------|---------|---------|--------|
| Microsoft.AspNetCore.Components.Authorization | 10.0.0 | Auth state provider | Existing |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.0 | Blazor WASM runtime | Existing |
| MudBlazor | 8.x | UI components (not used in login currently) | Existing |

No new dependencies required.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Open redirect vulnerability | Low | High | UrlValidator with strict validation |
| Infinite redirect loop | Low | Medium | Check current path before redirect |
| Token refresh race condition | Low | Low | Existing semaphore mechanism |
| Breaking existing login flow | Low | High | Comprehensive E2E tests |

---

## Summary of Decisions

| Area | Decision | Rationale |
|------|----------|-----------|
| Token refresh | Use existing proactive refresh | Already implemented, 5-min threshold appropriate |
| Redirect mechanism | New `INavigationService` | Clean abstraction, testable |
| URL validation | New `UrlValidator` utility | Prevents open redirect, reusable |
| Enter key handling | `@onkeydown` on password field | Minimal change, standard Blazor pattern |
| Return URL consumption | Query parameter with validation | Standard web pattern, secure |
