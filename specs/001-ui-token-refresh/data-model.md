# Data Model: Sorcha.UI Authentication Token Management and Login UX

**Feature Branch**: `001-ui-token-refresh`
**Date**: 2026-02-03

## Overview

This feature primarily enhances existing authentication infrastructure. No new persistent entities are required. The data model focuses on the transient state and validation logic needed for return URL handling.

---

## Existing Entities (No Changes)

### TokenCacheEntry

**Location**: `Sorcha.UI.Core/Models/Authentication/TokenCacheEntry.cs`

Already contains all required properties for token lifecycle management:

| Property | Type | Description |
|----------|------|-------------|
| AccessToken | string | JWT access token |
| RefreshToken | string? | OAuth2 refresh token |
| ExpiresAt | DateTime | Token expiration (UTC) |
| ProfileName | string | Configuration profile identifier |
| IssuedAt | DateTime | When token was cached |
| IsExpired | bool (computed) | `DateTime.UtcNow >= ExpiresAt` |
| IsNearExpiration | bool (computed) | `DateTime.UtcNow >= ExpiresAt.AddMinutes(-5)` |
| TimeRemaining | TimeSpan (computed) | `ExpiresAt - DateTime.UtcNow` |

**No changes required** - existing model supports all token refresh scenarios.

---

## New Abstractions

### INavigationService Interface

**Location**: `Sorcha.UI.Core/Services/Navigation/INavigationService.cs`

Purpose: Abstraction for authentication-aware navigation with return URL support.

```csharp
public interface INavigationService
{
    /// <summary>
    /// Gets the current navigation URI.
    /// </summary>
    string CurrentUri { get; }

    /// <summary>
    /// Redirects to login page with optional return URL.
    /// Does nothing if already on login page (prevents loops).
    /// </summary>
    /// <param name="returnUrl">URL to return to after login. Validated for security.</param>
    Task RedirectToLoginAsync(string? returnUrl = null);

    /// <summary>
    /// Navigates to the specified URL if valid, otherwise navigates to default destination.
    /// </summary>
    /// <param name="url">Target URL (validated for security)</param>
    /// <param name="defaultDestination">Fallback if URL is invalid</param>
    void NavigateToValidatedUrl(string? url, string defaultDestination = "dashboard");
}
```

### NavigationService Implementation

**Location**: `Sorcha.UI.Core/Services/Navigation/NavigationService.cs`

Dependencies:
- `NavigationManager` (Blazor)
- `ILogger<NavigationService>` (optional, for debugging)

State:
- None (stateless service)

---

## Utility Classes

### UrlValidator

**Location**: `Sorcha.UI.Core/Utilities/UrlValidator.cs`

Purpose: Security validation for return URLs to prevent open redirect attacks.

```csharp
public static class UrlValidator
{
    /// <summary>
    /// Validates that a return URL is safe (relative path or same-origin).
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <param name="baseUri">Application base URI for same-origin check</param>
    /// <returns>True if URL is safe to redirect to</returns>
    public static bool IsValidReturnUrl(string? url, Uri baseUri);

    /// <summary>
    /// Validates that a return URL is safe using string base URI.
    /// </summary>
    public static bool IsValidReturnUrl(string? url, string baseUri);
}
```

**Validation Rules**:

| Input | Valid | Reason |
|-------|-------|--------|
| `/dashboard` | ✅ | Relative path |
| `/app/registers/123` | ✅ | Relative path with segments |
| `dashboard` | ❌ | Not a path (could be interpreted as host) |
| `//evil.com/path` | ❌ | Protocol-relative URL (security risk) |
| `https://evil.com/` | ❌ | Different origin |
| `https://same.host/path` | ✅ | Same origin (when baseUri matches) |
| `javascript:alert(1)` | ❌ | Script injection |
| `data:text/html,...` | ❌ | Data URL injection |
| `null` / `""` / `"  "` | ❌ | Empty/whitespace |

---

## Query Parameters

### Login Page Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| returnUrl | string | No | URL-encoded path to redirect after successful login |

**Example URLs**:
- `/auth/login` - Login without return URL (redirects to dashboard)
- `/auth/login?returnUrl=%2Fdashboard` - Return to /dashboard
- `/auth/login?returnUrl=%2Fapp%2Fregisters%2F123` - Return to /app/registers/123

---

## State Transitions

### Authentication State Machine

```
┌─────────────────┐
│  Unauthenticated │
└────────┬────────┘
         │ Login success
         ▼
┌─────────────────┐
│  Authenticated   │◄──────────────────┐
└────────┬────────┘                    │
         │                              │
         ├─── Token near expiration ───►│ Refresh success
         │    (5 min threshold)         │
         │                              │
         ├─── Token expired ───────────►│ Refresh success
         │    (401 response)            │
         │                              │
         │    Refresh fails             │
         ▼                              │
┌─────────────────┐                    │
│  Redirect to    │                    │
│  Login (+ URL)  │────────────────────┘
└─────────────────┘   Login success
```

### Return URL Flow

```
1. User on /app/registers/123
2. Token expires, refresh fails
3. → Redirect to /auth/login?returnUrl=%2Fapp%2Fregisters%2F123
4. User logs in successfully
5. → Validate returnUrl: IsValidReturnUrl("/app/registers/123", baseUri) = true
6. → Navigate to /app/registers/123
```

---

## No Database Changes

This feature is entirely client-side. No changes to:
- PostgreSQL schemas
- MongoDB collections
- Redis keys
- Entity Framework migrations

---

## Integration Points

| Component | Integration | Direction |
|-----------|-------------|-----------|
| AuthenticatedHttpMessageHandler | Calls INavigationService.RedirectToLoginAsync | Outbound |
| Login.razor | Reads returnUrl query parameter | Inbound |
| Login.razor | Calls INavigationService.NavigateToValidatedUrl | Outbound |
| NavigationService | Uses NavigationManager | Internal |
| NavigationService | Uses UrlValidator | Internal |
