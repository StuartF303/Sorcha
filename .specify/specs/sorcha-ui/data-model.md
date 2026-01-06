# Data Model: Sorcha.UI

**Service**: Sorcha.UI | **Date**: 2026-01-06
**Source**: [sorcha-ui.md](../sorcha-ui.md)

## Overview

Sorcha.UI is a **client-only application** that consumes backend services via REST/HTTP. This document defines the **client-side domain models** used for:
- Authentication state management
- Configuration and profile management
- API request/response models
- UI state models

**Note**: Backend domain models (Blueprint, Register, Transaction, etc.) are defined in their respective service specifications. Sorcha.UI **reuses** these models via shared libraries (`Sorcha.Blueprint.Models`, `Sorcha.Register.Models`).

---

## Entity Catalog

| Entity | Location | Purpose | Persistence |
|--------|----------|---------|-------------|
| **LoginRequest** | Core.Models.Authentication | OAuth2 login credentials | None (transient) |
| **TokenResponse** | Core.Models.Authentication | OAuth2 token response | None (transient) |
| **TokenCacheEntry** | Core.Models.Authentication | Cached JWT tokens | LocalStorage (encrypted) |
| **AuthenticationStateInfo** | Core.Models.Authentication | Serialized auth state for WASM transfer | PersistentComponentState |
| **Profile** | Core.Models.Configuration | Environment profile (dev/staging/prod) | LocalStorage (plaintext) |
| **UiConfiguration** | Core.Models.Configuration | UI preferences (theme, language) | LocalStorage (plaintext) |
| **ApiResponse<T>** | Core.Models.Common | Standardized API response wrapper | None (transient) |
| **PaginatedList<T>** | Core.Models.Common | Paginated data results | None (transient) |

---

## Authentication Models

### LoginRequest

**Purpose**: OAuth2 Password Grant login credentials

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/LoginRequest.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Sorcha.UI.Core.Models.Authentication;

/// <summary>
/// OAuth2 Password Grant login request
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Username or email address
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(256, MinimumLength = 3, ErrorMessage = "Username must be 3-256 characters")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional profile name to authenticate against (e.g., "Development", "Production")
    /// If not specified, uses active profile from configuration
    /// </summary>
    public string? ProfileName { get; init; }
}
```

**Validation Rules**:
- Username: Required, 3-256 characters
- Password: Required, 8-128 characters
- ProfileName: Optional

**Usage**: Login.razor form submission

---

### TokenResponse

**Purpose**: OAuth2 token response from Tenant Service

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/TokenResponse.cs`

```csharp
using System.Text.Json.Serialization;

namespace Sorcha.UI.Core.Models.Authentication;

/// <summary>
/// OAuth2 token response (RFC 6749)
/// </summary>
public sealed record TokenResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Token type (always "Bearer")
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// Expiration time in seconds (e.g., 1800 = 30 minutes)
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Refresh token (optional, for token renewal)
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Issued at timestamp (Unix epoch seconds)
    /// </summary>
    [JsonPropertyName("issued_at")]
    public long? IssuedAt { get; init; }

    /// <summary>
    /// Scope (space-separated list of permissions)
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}
```

**Validation Rules**:
- AccessToken: Required, non-empty
- ExpiresIn: Required, > 0
- RefreshToken: Optional (recommended for long sessions)

**Usage**: Deserialized from Tenant Service `/api/service-auth/token` response

---

### TokenCacheEntry

**Purpose**: Cached JWT tokens with expiration metadata

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/TokenCacheEntry.cs`

```csharp
namespace Sorcha.UI.Core.Models.Authentication;

/// <summary>
/// Cached authentication tokens for a specific profile
/// </summary>
public sealed record TokenCacheEntry
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token (for token renewal)
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Absolute expiration time (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Profile name this token is associated with
    /// </summary>
    public string ProfileName { get; init; } = string.Empty;

    /// <summary>
    /// When the token was originally issued (UTC)
    /// </summary>
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Checks if token is expiring soon (within 5 minutes)
    /// </summary>
    public bool IsNearExpiration => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);

    /// <summary>
    /// Time remaining until expiration
    /// </summary>
    public TimeSpan TimeRemaining => ExpiresAt - DateTime.UtcNow;
}
```

**Validation Rules**:
- AccessToken: Required, non-empty
- ExpiresAt: Required, > DateTime.UtcNow
- ProfileName: Required (for multi-profile scenarios)

**Storage**: LocalStorage, encrypted via BrowserEncryptionProvider
**Key Format**: `sorcha:tokens:{profileName}`

**Usage**: BrowserTokenCache retrieval/storage

---

### AuthenticationStateInfo

**Purpose**: Serializable authentication state for PersistentComponentState transfer

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/AuthenticationStateInfo.cs`

```csharp
using System.Security.Claims;

namespace Sorcha.UI.Core.Models.Authentication;

/// <summary>
/// Serializable authentication state for server → WASM transfer
/// </summary>
public sealed record AuthenticationStateInfo
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token (optional)
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Token expiration time (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Active profile name
    /// </summary>
    public string ProfileName { get; init; } = string.Empty;

    /// <summary>
    /// User claims (extracted from JWT)
    /// </summary>
    public List<ClaimData> Claims { get; init; } = new();

    /// <summary>
    /// Serializable claim data
    /// </summary>
    public sealed record ClaimData
    {
        public string Type { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string? ValueType { get; init; }
        public string? Issuer { get; init; }
    }

    /// <summary>
    /// Converts to ClaimsPrincipal for AuthenticationState
    /// </summary>
    public ClaimsPrincipal ToClaimsPrincipal()
    {
        var claims = Claims.Select(c => new Claim(
            c.Type,
            c.Value,
            c.ValueType ?? ClaimValueTypes.String,
            c.Issuer ?? "LOCAL"
        ));

        var identity = new ClaimsIdentity(claims, "JWT");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates AuthenticationStateInfo from JWT token
    /// </summary>
    public static AuthenticationStateInfo FromToken(string accessToken, string? refreshToken, DateTime expiresAt, string profileName)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        var claims = jwt.Claims.Select(c => new ClaimData
        {
            Type = c.Type,
            Value = c.Value,
            ValueType = c.ValueType,
            Issuer = c.Issuer
        }).ToList();

        return new AuthenticationStateInfo
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            ProfileName = profileName,
            Claims = claims
        };
    }
}
```

**Validation Rules**:
- AccessToken: Required, valid JWT format
- ExpiresAt: Required, > DateTime.UtcNow
- ProfileName: Required
- Claims: Must include `sub` (user ID), `email`, `role`

**Storage**: PersistentComponentState (server → WASM transfer)
**Size Limit**: <32 KB (enforced by PersistentComponentState)

**Usage**: App.razor persists during server render, WASM Program.cs retrieves on bootstrap

---

## Configuration Models

### Profile

**Purpose**: Environment profile configuration (dev/staging/prod)

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Configuration/Profile.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Sorcha.UI.Core.Models.Configuration;

/// <summary>
/// Environment profile configuration
/// </summary>
public sealed record Profile
{
    /// <summary>
    /// Profile name (e.g., "Development", "Staging", "Production")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// API Gateway base URL
    /// </summary>
    [Required]
    [Url]
    public string ApiGatewayUrl { get; init; } = string.Empty;

    /// <summary>
    /// Tenant Service URL (for authentication)
    /// </summary>
    [Required]
    [Url]
    public string TenantServiceUrl { get; init; } = string.Empty;

    /// <summary>
    /// Blueprint Service URL
    /// </summary>
    [Required]
    [Url]
    public string BlueprintServiceUrl { get; init; } = string.Empty;

    /// <summary>
    /// Register Service URL
    /// </summary>
    [Required]
    [Url]
    public string RegisterServiceUrl { get; init; } = string.Empty;

    /// <summary>
    /// Optional color for UI profile indicator (hex color)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color (#RRGGBB)")]
    public string? Color { get; init; }

    /// <summary>
    /// Whether this is the default active profile
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Optional description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; init; }
}
```

**Validation Rules**:
- Name: Required, unique, 1-100 characters
- All URLs: Required, valid URL format
- Color: Optional, valid hex color (#RRGGBB)
- At most one profile can have IsDefault = true

**Storage**: LocalStorage (plaintext, not encrypted)
**Key Format**: `sorcha:profiles`

**Usage**: ProfileSelector.razor, ConfigurationService

**Default Profiles** (created on first run):
```json
[
  {
    "Name": "Development",
    "DisplayName": "Development (Local)",
    "ApiGatewayUrl": "https://localhost:7082",
    "TenantServiceUrl": "https://localhost:7082/api/service-auth",
    "BlueprintServiceUrl": "https://localhost:7082/api/blueprints",
    "RegisterServiceUrl": "https://localhost:7082/api/registers",
    "Color": "#4CAF50",
    "IsDefault": true,
    "Description": "Local development environment"
  },
  {
    "Name": "Docker",
    "DisplayName": "Docker Compose",
    "ApiGatewayUrl": "http://localhost:8080",
    "TenantServiceUrl": "http://localhost:8080/api/service-auth",
    "BlueprintServiceUrl": "http://localhost:8080/api/blueprints",
    "RegisterServiceUrl": "http://localhost:8080/api/registers",
    "Color": "#2196F3",
    "IsDefault": false,
    "Description": "Docker Compose environment"
  }
]
```

---

### UiConfiguration

**Purpose**: User UI preferences (theme, language, layout)

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Configuration/UiConfiguration.cs`

```csharp
namespace Sorcha.UI.Core.Models.Configuration;

/// <summary>
/// User interface configuration and preferences
/// </summary>
public sealed record UiConfiguration
{
    /// <summary>
    /// Active profile name
    /// </summary>
    public string ActiveProfile { get; init; } = "Development";

    /// <summary>
    /// UI theme (Light/Dark/System)
    /// </summary>
    public ThemeMode Theme { get; init; } = ThemeMode.System;

    /// <summary>
    /// Language/locale (e.g., "en-US", "en-GB")
    /// </summary>
    public string Language { get; init; } = "en-US";

    /// <summary>
    /// Whether navigation sidebar is expanded
    /// </summary>
    public bool SidebarExpanded { get; init; } = true;

    /// <summary>
    /// Recent activity log max entries
    /// </summary>
    public int MaxRecentActivity { get; init; } = 50;

    /// <summary>
    /// Last accessed module (for remembering user navigation)
    /// </summary>
    public string? LastModule { get; init; }
}

/// <summary>
/// UI theme modes
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Light theme
    /// </summary>
    Light,

    /// <summary>
    /// Dark theme
    /// </summary>
    Dark,

    /// <summary>
    /// Follow system theme preference
    /// </summary>
    System
}
```

**Storage**: LocalStorage (plaintext)
**Key Format**: `sorcha:ui-config`

**Usage**: MainLayout.razor, ConfigurationService

---

## Common Models

### ApiResponse<T>

**Purpose**: Standardized wrapper for API responses

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Common/ApiResponse.cs`

```csharp
namespace Sorcha.UI.Core.Models.Common;

/// <summary>
/// Standardized API response wrapper
/// </summary>
/// <typeparam name="T">Response data type</typeparam>
public sealed record ApiResponse<T>
{
    /// <summary>
    /// Whether the request succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response data (null if error)
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error message (null if success)
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a success response
    /// </summary>
    public static ApiResponse<T> Ok(T data, int statusCode = 200) =>
        new() { Success = true, Data = data, StatusCode = statusCode };

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> Fail(string error, int statusCode = 400) =>
        new() { Success = false, Error = error, StatusCode = statusCode };
}
```

**Usage**: Wraps all backend API call responses for consistent error handling

---

### PaginatedList<T>

**Purpose**: Paginated data results from backend APIs

**File**: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Common/PaginatedList.cs`

```csharp
namespace Sorcha.UI.Core.Models.Common;

/// <summary>
/// Paginated list response
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public sealed record PaginatedList<T>
{
    /// <summary>
    /// Current page items
    /// </summary>
    public List<T> Items { get; init; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Page size (items per page)
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total item count across all pages
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Total page count
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
```

**Usage**: UserTable.razor, BlueprintList.razor, TransactionTable.razor (pagination controls)

---

## Entity Relationships

```
AuthenticationStateInfo
├── Contains: TokenCacheEntry data (access token, refresh token, expiration)
├── Contains: List<ClaimData> (JWT claims)
└── References: Profile (via ProfileName)

TokenCacheEntry
├── Stored per: Profile (key: sorcha:tokens:{profileName})
└── Used by: AuthenticationService, BrowserTokenCache

Profile
├── Stored in: List<Profile> in LocalStorage (key: sorcha:profiles)
├── Referenced by: AuthenticationStateInfo.ProfileName
└── Used by: ConfigurationService, ProfileSelector

UiConfiguration
├── References: Profile (via ActiveProfile name)
├── Stored in: LocalStorage (key: sorcha:ui-config)
└── Used by: MainLayout, ConfigurationService

LoginRequest → TokenResponse → TokenCacheEntry → AuthenticationStateInfo
(Transient)     (Transient)     (Persisted)      (Transferred to WASM)
```

---

## State Transitions

### Authentication State Lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│                    Authentication Flow                       │
└─────────────────────────────────────────────────────────────┘

1. Anonymous (Initial State)
   - No TokenCacheEntry in LocalStorage
   - AuthenticationStateInfo = empty
   - UI shows "Login" button

2. Login Submitted
   - User fills LoginRequest form
   - POST to Tenant Service /api/service-auth/token
   - Receive TokenResponse

3. Token Cached
   - Convert TokenResponse → TokenCacheEntry
   - Encrypt and store in LocalStorage (key: sorcha:tokens:{profileName})
   - Create AuthenticationStateInfo from TokenResponse

4. Server → WASM Transfer
   - Server: Serialize AuthenticationStateInfo to PersistentComponentState
   - WASM: Retrieve AuthenticationStateInfo on bootstrap
   - WASM: Create ClaimsPrincipal from Claims

5. Authenticated (Active State)
   - TokenCacheEntry in LocalStorage (encrypted)
   - AuthenticationStateInfo in memory
   - ClaimsPrincipal with roles
   - UI shows UserProfileMenu

6. Token Refresh (Periodic)
   - Check TokenCacheEntry.IsNearExpiration (< 5 min remaining)
   - POST to /api/service-auth/refresh with RefreshToken
   - Update TokenCacheEntry in LocalStorage

7. Token Expired/Invalid
   - API returns 401 Unauthorized
   - Attempt refresh once
   - If refresh fails → Logout (return to State 1)

8. Logout
   - Clear TokenCacheEntry from LocalStorage
   - Clear AuthenticationStateInfo from memory
   - Clear PersistentComponentState
   - Redirect to /login
```

### Profile Switching State Transition

```
Active Profile: Development
↓
User clicks "Switch to Production"
↓
Confirmation Dialog: "You will be logged out of Development..."
↓
User confirms
↓
Logout from Development (clear tokens)
↓
Set ActiveProfile = "Production" in UiConfiguration
↓
Redirect to /login
↓
User logs in to Production
↓
Active Profile: Production
```

---

## Validation Summary

| Model | Required Fields | Validation Rules | Storage |
|-------|----------------|------------------|---------|
| **LoginRequest** | Username, Password | Username: 3-256 chars, Password: 8-128 chars | None (transient) |
| **TokenResponse** | AccessToken, ExpiresIn | AccessToken non-empty, ExpiresIn > 0 | None (transient) |
| **TokenCacheEntry** | AccessToken, ExpiresAt, ProfileName | AccessToken non-empty, ExpiresAt future | LocalStorage (encrypted) |
| **AuthenticationStateInfo** | AccessToken, ExpiresAt, ProfileName, Claims | Valid JWT, size <32KB | PersistentComponentState |
| **Profile** | Name, DisplayName, all URLs | Unique name, valid URLs | LocalStorage (plaintext) |
| **UiConfiguration** | ActiveProfile | Must reference existing Profile | LocalStorage (plaintext) |
| **ApiResponse<T>** | Success, StatusCode | StatusCode 100-599 | None (transient) |
| **PaginatedList<T>** | Items, PageNumber, PageSize, TotalCount | PageNumber ≥ 1, PageSize > 0 | None (transient) |

---

## Persistence Strategy

### LocalStorage Schema

```
sorcha:tokens:{profileName}         → Encrypted TokenCacheEntry JSON
sorcha:profiles                      → Plaintext List<Profile> JSON
sorcha:ui-config                     → Plaintext UiConfiguration JSON
sorcha:encryption-key                → PBKDF2-derived encryption key (Web Crypto API)
```

### Encryption Details

**Algorithm**: AES-256-GCM
**Key Derivation**: PBKDF2-SHA-256 (100,000 iterations) from browser fingerprint
**IV**: Random 12-byte IV per encryption
**Fallback**: Plaintext storage with "PLAINTEXT:" prefix when Web Crypto unavailable (HTTP localhost)

**Encrypted Fields**:
- ✅ `sorcha:tokens:{profileName}` (contains JWT tokens)
- ❌ `sorcha:profiles` (plaintext, no sensitive data)
- ❌ `sorcha:ui-config` (plaintext, no sensitive data)

---

## Domain Model Dependencies

Sorcha.UI **consumes** these backend domain models (defined in shared libraries):

| Model | Source Library | Used By Module |
|-------|----------------|----------------|
| **Blueprint** | Sorcha.Blueprint.Models | Designer |
| **Action** | Sorcha.Blueprint.Models | Designer |
| **Participant** | Sorcha.Blueprint.Models | Designer |
| **Register** | Sorcha.Register.Models (TBD) | Explorer |
| **Transaction** | Sorcha.Register.Models (TBD) | Explorer |
| **User** | Sorcha.Tenant.Models (TBD) | Admin |
| **Organization** | Sorcha.Tenant.Models (TBD) | Admin |

**Note**: Backend models are **NOT** duplicated in Sorcha.UI. The UI project references shared model libraries to maintain consistency.

---

## Next Steps

1. ✅ Data model defined
2. ⏭️ Generate API contracts (Phase 1.2)
3. ⏭️ Create quickstart guide (Phase 1.3)
4. ⏭️ Generate task breakdown (Phase 2, separate `/speckit.tasks` command)

---

**Document Version**: 1.0 | **Last Updated**: 2026-01-06
