# Sorcha.Admin - Feature Requirements & Architecture Documentation

**Version:** 1.0
**Last Updated:** 2026-01-06
**Purpose:** Comprehensive documentation of all features, components, and architecture for migration reference

---

## Table of Contents

1. [Overview](#overview)
2. [Core Features](#core-features)
3. [Architecture & Technology Stack](#architecture--technology-stack)
4. [Component Inventory](#component-inventory)
5. [Service Layer](#service-layer)
6. [Data Models](#data-models)
7. [Authentication & Authorization](#authentication--authorization)
8. [Configuration Management](#configuration-management)
9. [Encryption & Security](#encryption--security)
10. [HTTP Client & API Integration](#http-client--api-integration)
11. [UI/UX Patterns](#uiux-patterns)
12. [Dependencies](#dependencies)
13. [Migration Considerations](#migration-considerations)

---

## Overview

**Sorcha.Admin** is a modern web-based administration and blueprint designer application for the Sorcha distributed ledger platform. It provides:

- Visual blueprint design and editing
- Multi-environment configuration management
- JWT-based authentication with encrypted token storage
- Real-time service health monitoring
- Seamless environment switching

**Current Implementation:** Blazor Server with InteractiveServerRenderMode
**Recommended Migration Target:** Blazor WebAssembly (see [KNOWN-ISSUES.md](KNOWN-ISSUES.md))

---

## Core Features

### 1. Blueprint Designer

**Status:** Core UI implemented, designer logic in progress

**Capabilities:**
- Visual drag-and-drop blueprint editor interface
- JSON schema configuration and validation
- Real-time blueprint validation using portable execution engine
- Template library for common blueprint patterns
- Blueprint import/export (JSON/YAML formats)
- Integration with Blueprint Service API

**User Workflows:**
- Create new blueprint from scratch or template
- Edit existing blueprints with live validation
- Preview blueprint execution flow
- Save blueprints to Blueprint Service
- Load blueprints from Blueprint Service

### 2. Authentication System

**Status:** Fully implemented (OAuth2 Password Grant flow)

**Capabilities:**
- JWT-based authentication via Tenant Service
- OAuth2 Password Grant flow (RFC 6749 Section 4.3)
- Automatic token refresh when <5 minutes to expiration
- Encrypted token storage in browser LocalStorage
- Per-profile token caching (allows multi-environment sessions)
- Logout with token revocation

**Supported Auth Flows:**
- Password Grant (current implementation)
- Designed for extensibility: Device Flow, OAuth2/OIDC (future)

### 3. Multi-Environment Configuration

**Status:** Fully implemented

**Capabilities:**
- 6 pre-configured environment profiles (dev, local, docker, aspire, staging, production)
- Custom profile creation and management
- Environment quick-switching via AppBar dropdown
- Profile-specific settings:
  - Service URLs for all 5 Sorcha services
  - OAuth2 token endpoint
  - SSL certificate verification toggle
  - Request timeout configuration
  - Custom settings dictionary
- Configuration persistence in LocalStorage
- Configuration export/import (manual via DevTools)

**Default Profiles:**

| Profile | Use Case | Tenant Service URL |
|---------|----------|-------------------|
| dev | Local HTTPS development | https://localhost:7080 |
| local | Local HTTP development | http://localhost:5080 |
| docker | Docker Compose | http://api-gateway:8080/api/tenant |
| aspire | .NET Aspire orchestration | https://localhost:7051/api/tenant |
| staging | Staging environment | https://n0.sorcha.dev |
| production | Production environment | https://tenant.sorcha.io |

### 4. Service Health Monitoring

**Status:** Basic implementation

**Capabilities:**
- Real-time health checks for all 5 Sorcha services
- Service status visualization (online/offline indicators)
- Health check dashboard widget
- Environment name display
- API endpoint visibility

**Services Monitored:**
- Tenant Service
- Blueprint Service
- Wallet Service
- Register Service
- Peer Service

### 5. Dashboard & Welcome Screen

**Status:** Implemented

**Capabilities:**
- Welcome message and platform overview
- Feature highlights with icons
- System status card (health monitoring)
- Recent activity log
- Quick action buttons (Sign In, View Blueprints, etc.)
- Dashboard statistics widgets (placeholders for future data)

### 6. User Profile Management

**Status:** Basic implementation

**Capabilities:**
- User profile menu in AppBar
- Display authenticated user name and email
- Profile selector (environment switcher)
- Logout functionality
- User avatar/icon display

---

## Architecture & Technology Stack

### Frontend Framework

**Blazor Server** (.NET 10)
- Render Mode: `InteractiveServer`
- Prerendering: Disabled for pages requiring JavaScript/LocalStorage
- SignalR circuit for server-client communication

**Recommendation:** Migrate to **Blazor WebAssembly** to resolve circuit isolation issues (see KNOWN-ISSUES.md)

### UI Framework

**MudBlazor 8.15.0**
- Material Design components
- Required providers:
  - `MudThemeProvider` (theme management)
  - `MudPopoverProvider` (popovers and tooltips)
  - `MudDialogProvider` (modal dialogs)
  - `MudSnackbarProvider` (toast notifications)

### State Management

**Blazor Built-in Patterns:**
- `CascadingAuthenticationState` for authentication state
- `AuthenticationStateProvider` for claims-based identity
- Component-level state with `StateHasChanged()`
- Service-injected state for configuration and tokens

### Storage

**Blazored.LocalStorage 4.5.0**
- Browser LocalStorage for configuration persistence
- Encrypted token storage
- Key structure:
  - `sorcha:config` - Configuration (profiles, active profile)
  - `sorcha:tokens:{profileName}` - Encrypted JWT tokens per profile

### Encryption

**Web Crypto API (SubtleCrypto)**
- AES-256-GCM encryption
- PBKDF2 key derivation (100,000 iterations)
- Browser fingerprint-based key generation
- Fallback to plaintext with "PLAINTEXT:" marker when crypto unavailable (HTTP on non-localhost)

### HTTP Client

**IHttpClientFactory with DelegatingHandler**
- Named client: "SorchaAPI"
- `AuthenticatedHttpMessageHandler` for automatic JWT injection
- Authorization header: `Bearer {token}`
- Automatic token refresh on API calls

### Authentication

**ASP.NET Core Components.Authorization 10.0.0**
- `AuthorizeView` components for conditional rendering
- `[Authorize]` attributes for page protection
- `[AllowAnonymous]` for public pages (Login)
- Claims-based authorization

### Diagram Editor (Blueprint Designer)

**Z.Blazor.Diagrams 3.0.2**
- Visual workflow diagram editor
- Node/edge based blueprint modeling
- Drag-and-drop interface
- Real-time diagram updates

---

## Component Inventory

### Pages

| Component | Path | Purpose | Auth Required |
|-----------|------|---------|---------------|
| **Login.razor** | `/Pages/Login.razor` | Standalone login page with profile selector | No ([AllowAnonymous]) |
| **Index.razor** | `/Pages/Index.razor` | Dashboard/welcome page with system status | No (public landing) |
| **Home.razor** | `/Pages/Home.razor` | Blueprint Designer main interface | Yes (planned) |
| **Settings.razor** | `/Pages/Settings.razor` | Settings with Configuration tab | Yes (planned) |

### Layout

| Component | Path | Purpose |
|-----------|------|---------|
| **MainLayout.razor** | `/Layout/MainLayout.razor` | Main application layout with AppBar, drawer, and content area |
| **NavMenu.razor** | `/Layout/NavMenu.razor` | Side navigation drawer with links |

### Authentication Components

| Component | Path | Purpose |
|-----------|------|---------|
| **LoginDialog.razor** | `/Components/Authentication/LoginDialog.razor` | Login modal with profile selector (inline form) |
| **ProfileSelector.razor** | `/Components/Authentication/ProfileSelector.razor` | AppBar dropdown for environment switching |
| **UserProfileMenu.razor** | `/Components/Authentication/UserProfileMenu.razor` | User menu with logout (AppBar) |
| **RedirectToLogin.razor** | `/Components/Authentication/RedirectToLogin.razor` | Auth guard redirect component |

### Configuration Components

| Component | Path | Purpose |
|-----------|------|---------|
| **ProfileEditorDialog.razor** | `/Components/Configuration/ProfileEditorDialog.razor` | Full profile CRUD editor (create/edit profiles) |

### Dashboard Components

| Component | Path | Purpose |
|-----------|------|---------|
| **SystemStatusCard.razor** | `/Components/SystemStatusCard.razor` | Health check dashboard widget |
| **RecentActivityLog.razor** | `/Components/RecentActivityLog.razor` | Activity event log widget |
| **DashboardStatistics.razor** | `/Components/DashboardStatistics.razor` | Statistics display widget |

### Core Components

| Component | Path | Purpose |
|-----------|------|---------|
| **App.razor** | `/Components/App.razor` | Root component (HTML document) |
| **Routes.razor** | `/Components/Routes.razor` | Routing configuration with CascadingAuthenticationState |

---

## Service Layer

### Authentication Services

#### IAuthenticationService

**Path:** `/Services/Authentication/IAuthenticationService.cs`

**Responsibilities:**
- OAuth2 Password Grant authentication
- Token retrieval and caching
- Token refresh logic
- Logout and token revocation

**Methods:**
```csharp
Task<TokenResponse> LoginAsync(LoginRequest request, string profileName);
Task<string?> GetAccessTokenAsync(string profileName);
Task<string?> GetRefreshTokenAsync(string profileName);
Task<bool> RefreshTokenAsync(string profileName);
Task LogoutAsync(string profileName);
bool IsAuthenticated(string profileName);
```

#### AuthenticationService

**Path:** `/Services/Authentication/AuthenticationService.cs`

**Implementation Details:**
- Uses `HttpClient` to POST to OAuth2 token endpoint
- Request format: `application/x-www-form-urlencoded`
- Parameters: `grant_type`, `username`, `password`, `client_id`
- Delegates token storage to `BrowserTokenCache`
- Handles token refresh with `grant_type=refresh_token`

#### BrowserTokenCache

**Path:** `/Services/Authentication/BrowserTokenCache.cs`

**Responsibilities:**
- Encrypted token storage in LocalStorage
- Token retrieval and decryption
- Token expiration checking
- Per-profile token isolation

**Storage Format:**
```
LocalStorage Key: sorcha:tokens:{profileName}
Value: Base64-encoded encrypted JSON containing:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "...",
  "expiresAt": "2026-01-06T12:30:00Z"
}
```

#### CustomAuthenticationStateProvider

**Path:** `/Services/Authentication/CustomAuthenticationStateProvider.cs`

**Responsibilities:**
- Blazor authentication state provider
- Retrieves JWT token from cache
- Parses JWT claims (sub, email, name, roles, org_id, etc.)
- Creates ClaimsPrincipal for authorization
- Provides NotifyAuthenticationStateChanged() for state updates

**Claims Extracted:**
- `sub` → ClaimTypes.NameIdentifier
- `email` → ClaimTypes.Email
- `name` → ClaimTypes.Name
- `role` → ClaimTypes.Role (array support)
- `org_id` → "org_id"
- `org_name` → "org_name"

### Configuration Services

#### IConfigurationService

**Path:** `/Services/Configuration/IConfigurationService.cs`

**Responsibilities:**
- Profile management (CRUD)
- Active profile selection
- Configuration persistence to LocalStorage
- Default profile initialization

**Methods:**
```csharp
Task<AdminConfiguration> GetConfigurationAsync();
Task SaveConfigurationAsync(AdminConfiguration config);
Task<Profile?> GetActiveProfileAsync();
Task SetActiveProfileAsync(string profileName);
Task<Profile?> GetProfileAsync(string profileName);
Task AddOrUpdateProfileAsync(Profile profile);
Task DeleteProfileAsync(string profileName);
```

#### ConfigurationService

**Path:** `/Services/Configuration/ConfigurationService.cs`

**Implementation Details:**
- Uses `Blazored.LocalStorage` for persistence
- Key: `sorcha:config`
- Initializes with 6 default profiles on first use
- Profile validation (name, URLs, timeout range)
- Active profile tracking

### Encryption Services

#### IEncryptionProvider

**Path:** `/Services/Encryption/IEncryptionProvider.cs`

**Responsibilities:**
- Symmetric encryption/decryption abstraction
- Key derivation
- Secure random generation

**Methods:**
```csharp
Task<byte[]> EncryptAsync(string plaintext);
Task<string> DecryptAsync(byte[] ciphertext);
Task<bool> IsAvailableAsync();
```

#### BrowserEncryptionProvider

**Path:** `/Services/Encryption/BrowserEncryptionProvider.cs`

**Implementation Details:**
- JavaScript interop wrapper for `/wwwroot/js/encryption.js`
- Calls `window.sorchaEncryption.encrypt()` and `.decrypt()`
- Detects Web Crypto API availability
- Handles fallback mode (plaintext with "PLAINTEXT:" marker)
- UTF-8 encoding/decoding for string data
- Comprehensive debug logging via console

### HTTP Services

#### AuthenticatedHttpMessageHandler

**Path:** `/Services/Http/AuthenticatedHttpMessageHandler.cs`

**Responsibilities:**
- DelegatingHandler for HTTP client pipeline
- Automatic JWT injection into Authorization header
- Token refresh on 401 responses
- Retry logic after token refresh

**Behavior:**
1. Before request: Retrieve access token from cache
2. Check token expiration (<5 minutes → auto-refresh)
3. Add `Authorization: Bearer {token}` header
4. On 401 response: Attempt token refresh, retry request
5. On refresh failure: Remove invalid token, return 401

---

## Data Models

### Authentication Models

#### LoginRequest

**Path:** `/Models/Authentication/LoginRequest.cs`

```csharp
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientId { get; set; } = "sorcha-admin";
}
```

#### TokenResponse

**Path:** `/Models/Authentication/TokenResponse.cs`

```csharp
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } // Seconds
}
```

#### TokenCacheEntry

**Path:** `/Models/Authentication/TokenCacheEntry.cs`

```csharp
public class TokenCacheEntry
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsNearExpiration => DateTime.UtcNow.AddMinutes(5) >= ExpiresAt;
}
```

#### AuthenticationStateInfo

**Path:** `/Models/Authentication/AuthenticationStateInfo.cs`

```csharp
public class AuthenticationStateInfo
{
    public bool IsAuthenticated { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, string> Claims { get; set; } = new();
}
```

### Configuration Models

#### Profile

**Path:** `/Models/Configuration/Profile.cs`

```csharp
public class Profile
{
    public string Name { get; set; } = string.Empty;
    public string TenantServiceUrl { get; set; } = string.Empty;
    public string RegisterServiceUrl { get; set; } = string.Empty;
    public string PeerServiceUrl { get; set; } = string.Empty;
    public string WalletServiceUrl { get; set; } = string.Empty;
    public string BlueprintServiceUrl { get; set; } = string.Empty;
    public string AuthTokenUrl { get; set; } = string.Empty;
    public string DefaultClientId { get; set; } = "sorcha-admin";
    public bool VerifySsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public Dictionary<string, string> CustomSettings { get; set; } = new();
}
```

**Validation Rules:**
- Name: Alphanumeric, dash, underscore only
- URLs: Must be valid HTTP/HTTPS URLs
- TimeoutSeconds: 1-300 range

#### AdminConfiguration

**Path:** `/Models/Configuration/AdminConfiguration.cs`

```csharp
public class AdminConfiguration
{
    public string? ActiveProfile { get; set; }
    public Dictionary<string, Profile> Profiles { get; set; } = new();
}
```

#### ProfileDefaults

**Path:** `/Models/Configuration/ProfileDefaults.cs`

**Purpose:** Factory class for creating default profiles

**Profiles Created:**
- dev (localhost:7080 HTTPS)
- local (localhost:5080 HTTP)
- docker (api-gateway:8080 HTTP)
- aspire (localhost:7051 HTTPS)
- staging (n0.sorcha.dev HTTPS)
- production (tenant.sorcha.io HTTPS)

---

## Authentication & Authorization

### OAuth2 Password Grant Flow

**Standard:** RFC 6749 Section 4.3

**Request:**
```http
POST {profile.AuthTokenUrl}
Content-Type: application/x-www-form-urlencoded

grant_type=password
&username={username}
&password={password}
&client_id={clientId}
```

**Response:**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 1800,
  "refresh_token": "..."
}
```

### Token Refresh Flow

**Request:**
```http
POST {profile.AuthTokenUrl}
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token
&refresh_token={refreshToken}
&client_id={clientId}
```

**Response:** Same as login response

### JWT Claims Structure

**Expected Claims:**
```json
{
  "sub": "00000000-0000-0000-0001-000000000001",
  "email": "admin@sorcha.local",
  "name": "System Administrator",
  "role": ["Administrator", "Designer", "Developer"],
  "org_id": "00000000-0000-0000-0000-000000000001",
  "org_name": "Sorcha Local",
  "nbf": 1736090000,
  "exp": 1736091800,
  "iat": 1736090000,
  "iss": "http://localhost",
  "aud": "http://localhost"
}
```

### Authorization Patterns

**Page Protection:**
```razor
@page "/blueprints"
@attribute [Authorize]
```

**Conditional Rendering:**
```razor
<AuthorizeView>
    <Authorized>
        <UserProfileMenu />
    </Authorized>
    <NotAuthorized>
        <MudButton Href="/login">Sign In</MudButton>
    </NotAuthorized>
</AuthorizeView>
```

**Role-Based Access (planned):**
```razor
<AuthorizeView Roles="Administrator,Designer">
    <Authorized>
        <!-- Admin/Designer only content -->
    </Authorized>
</AuthorizeView>
```

---

## Configuration Management

### LocalStorage Structure

**Key:** `sorcha:config`

**Value:**
```json
{
  "activeProfile": "dev",
  "profiles": {
    "dev": {
      "name": "dev",
      "tenantServiceUrl": "https://localhost:7080",
      "registerServiceUrl": "https://localhost:7081",
      "peerServiceUrl": "https://localhost:7082",
      "walletServiceUrl": "https://localhost:7083",
      "blueprintServiceUrl": "https://localhost:7084",
      "authTokenUrl": "https://localhost:7080/api/service-auth/token",
      "defaultClientId": "sorcha-admin",
      "verifySsl": false,
      "timeoutSeconds": 30,
      "customSettings": {}
    }
  }
}
```

### Profile Lifecycle

**Initialization:**
1. `ConfigurationService` checks LocalStorage for `sorcha:config`
2. If not found, creates default configuration with 6 profiles
3. Sets `activeProfile` to `ProfileDefaults.DefaultActiveProfile` ("dev")

**Profile Switch:**
1. User selects profile from `ProfileSelector` dropdown
2. `SetActiveProfileAsync(profileName)` updates `activeProfile`
3. Configuration saved to LocalStorage
4. If no token exists for profile → redirect to `/login`
5. If token exists → continue with cached token

**Profile CRUD:**
1. Create: Validate name uniqueness, URL formats, timeout range
2. Read: `GetProfileAsync(profileName)` from dictionary
3. Update: Replace existing profile in dictionary
4. Delete: Remove from dictionary (cannot delete active profile)

### Environment Variables Support (Planned)

Future enhancement to support configuration from environment variables for containerized deployments:

```csharp
TenantServiceUrl = Environment.GetEnvironmentVariable("ApiGateway__BaseUrl")
    ?? "http://localhost/api/tenant"
```

---

## Encryption & Security

### Web Crypto API Encryption

**JavaScript Implementation:** `/wwwroot/js/encryption.js`

**Algorithm:** AES-256-GCM
- **Key Derivation:** PBKDF2-SHA-256, 100,000 iterations
- **Salt:** Browser fingerprint-derived (navigator properties)
- **IV:** Random 12-byte initialization vector per encryption
- **Tag:** 128-bit authentication tag

**Encryption Process:**
1. Generate browser fingerprint from navigator properties
2. Derive 256-bit key using PBKDF2 with fingerprint as salt
3. Generate random 12-byte IV
4. Encrypt plaintext with AES-GCM
5. Concatenate: `IV (12 bytes) + Ciphertext + Tag`
6. Encode as Base64

**Decryption Process:**
1. Decode Base64 to bytes
2. Extract IV (first 12 bytes)
3. Extract ciphertext + tag (remaining bytes)
4. Derive same key using browser fingerprint
5. Decrypt with AES-GCM using IV, ciphertext, and key
6. Verify authentication tag

**Fallback Mode (HTTP on non-localhost):**
- Web Crypto API unavailable → `crypto.subtle` is `undefined`
- Fallback to plaintext storage with "PLAINTEXT:" marker
- Warning logged to console
- C# code detects marker and skips decryption

### Security Considerations

**What Encryption Protects:**
- Casual inspection of LocalStorage in DevTools
- Accidental exposure in screenshots/logs
- Simple attacks without JavaScript execution

**What Encryption Does NOT Protect:**
- XSS attacks (malicious scripts can call decryption)
- Malicious browser extensions (same-origin access)
- Physical access to unlocked browser session
- Determined attackers with debugging tools

**Mitigations:**
- Short token lifetimes (30 minutes default)
- Automatic token refresh (reduces friction)
- HTTPS for production (enables Web Crypto API)
- Content Security Policy headers (planned)
- MFA for high-security environments (planned)

### SSL/TLS Configuration

**Production:**
- **Always** enable SSL certificate verification (`verifySsl: true`)
- Use CA-signed certificates
- Enforce HTTPS

**Development:**
- Self-signed certificates allowed (`verifySsl: false`)
- Use `mkcert` for locally-trusted dev certificates
- **Never** deploy dev profiles to production

---

## HTTP Client & API Integration

### HttpClient Configuration

**Registration in Program.cs:**
```csharp
builder.Services.AddScoped<AuthenticatedHttpMessageHandler>();

builder.Services.AddHttpClient("SorchaAPI")
    .AddHttpMessageHandler<AuthenticatedHttpMessageHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            // SSL validation based on profile.VerifySsl
            return !profile.VerifySsl || errors == SslPolicyErrors.None;
        }
    });
```

### API Endpoints Integration

**Tenant Service:**
- `POST /api/service-auth/token` - OAuth2 token endpoint
- `POST /api/service-auth/refresh` - Token refresh
- `POST /api/service-auth/logout` - Logout/revoke
- Future: User management, role assignment

**Blueprint Service:**
- `GET /api/blueprints` - List blueprints
- `GET /api/blueprints/{id}` - Get blueprint
- `POST /api/blueprints` - Create blueprint
- `PUT /api/blueprints/{id}` - Update blueprint
- `DELETE /api/blueprints/{id}` - Delete blueprint
- `POST /api/blueprints/{id}/validate` - Validate blueprint

**Wallet Service:**
- `GET /api/wallets` - List wallets
- `GET /api/wallets/{id}` - Get wallet details
- `POST /api/wallets` - Create wallet
- `GET /api/wallets/{id}/balance` - Get wallet balance

**Register Service:**
- `GET /api/registers` - List registers
- `GET /api/registers/{id}/transactions` - Get register transactions
- `POST /api/registers/{id}/transactions` - Submit transaction

**Peer Service:**
- `GET /api/peers` - List connected peers
- `GET /api/peers/{id}` - Get peer details
- `POST /api/peers/connect` - Connect to peer

### Error Handling

**HTTP Status Code Handling:**
- `200-299`: Success, parse response
- `401`: Unauthorized → Attempt token refresh → Retry or redirect to `/login`
- `403`: Forbidden → Display error, user lacks permission
- `404`: Not Found → Display "resource not found"
- `500-599`: Server Error → Display error message, log to console

**Network Error Handling:**
- Connection refused → "Cannot connect to {service}"
- Timeout → "Request timed out after {timeout} seconds"
- DNS failure → "Cannot resolve {hostname}"

---

## UI/UX Patterns

### MudBlazor Component Usage

**Common Patterns:**

**Forms:**
```razor
<EditForm Model="@_model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <MudTextField @bind-Value="_model.Username" Label="Username" Required="true" />
    <MudButton ButtonType="ButtonType.Submit" Color="Color.Primary">Submit</MudButton>
</EditForm>
```

**Dialogs:**
```razor
@inject IDialogService DialogService

private async Task OpenDialogAsync()
{
    var dialog = DialogService.Show<LoginDialog>("Login");
    var result = await dialog.Result;
    if (!result.Cancelled)
    {
        // Handle result
    }
}
```

**Snackbar Notifications:**
```razor
@inject ISnackbar Snackbar

private void ShowNotification()
{
    Snackbar.Add("Operation successful", Severity.Success);
}
```

**Tables:**
```razor
<MudTable Items="@_items" Hover="true" Striped="true">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Status</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Status</MudTd>
    </RowTemplate>
</MudTable>
```

### Responsive Design

**Breakpoints (MudBlazor):**
- `xs`: <600px (mobile)
- `sm`: 600-960px (tablet)
- `md`: 960-1280px (small desktop)
- `lg`: 1280-1920px (desktop)
- `xl`: >1920px (large desktop)

**Responsive Patterns:**
```razor
<MudContainer MaxWidth="MaxWidth.Large">
    <MudGrid>
        <MudItem xs="12" md="6" lg="4">
            <!-- Responsive grid item -->
        </MudItem>
    </MudGrid>
</MudContainer>
```

### Navigation Patterns

**AppBar Navigation:**
- Logo/title (left)
- Navigation links (center)
- Profile selector and user menu (right)

**Drawer Navigation:**
- Mini drawer with icons
- Expands on hover (`OpenMiniOnHover="true"`)
- Persistent state (`@bind-Open="_drawerOpen"`)

**Page Navigation:**
```razor
@inject NavigationManager Navigation

Navigation.NavigateTo("/blueprints");
Navigation.NavigateTo("/login", forceLoad: true); // Full page reload
```

### Loading States

**Button Loading:**
```razor
<MudButton Disabled="@_isLoading">
    @if (_isLoading)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
        <span>Loading...</span>
    }
    else
    {
        <span>Submit</span>
    }
</MudButton>
```

**Page Loading:**
```razor
@if (_isLoading)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    <!-- Page content -->
}
```

### Error Display

**Inline Errors:**
```razor
@if (!string.IsNullOrEmpty(_errorMessage))
{
    <MudAlert Severity="Severity.Error" Dense="true">
        @_errorMessage
    </MudAlert>
}
```

**Global Error Boundary:**
```razor
<!-- In App.razor -->
<div id="blazor-error-ui">
    An unhandled error has occurred.
    <a href="/" class="reload">Reload</a>
    <a class="dismiss">🗙</a>
</div>
```

---

## Dependencies

### NuGet Packages

```xml
<PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.0" />
<PackageReference Include="MudBlazor" Version="8.15.0" />
<PackageReference Include="Z.Blazor.Diagrams" Version="3.0.2" />
```

### JavaScript Libraries

**Encryption:**
- `/wwwroot/js/encryption.js` - Custom SubtleCrypto wrapper

**MudBlazor:**
- `_content/MudBlazor/MudBlazor.min.js`
- `_content/MudBlazor/MudBlazor.min.css`

**Z.Blazor.Diagrams:**
- `_content/Z.Blazor.Diagrams/script.min.js`
- `_content/Z.Blazor.Diagrams/default.styles.css`

### Fonts

**Google Fonts:**
- Roboto (300, 400, 500, 700 weights)

### Icons

**Material Design Icons:**
- Via MudBlazor (`@Icons.Material.Filled.*`)

---

## Migration Considerations

### Blazor Server → Blazor WebAssembly

**Reasons for Migration (see KNOWN-ISSUES.md):**
- **Circuit Isolation Issues:** Authentication state not persisting across circuit recreation
- **State Management Complexity:** Blazor Server circuits don't naturally share LocalStorage state
- **Better User Experience:** WASM runs in browser, no WebSocket dependencies, works offline
- **Simplified Architecture:** No server-side circuit memory management

**Migration Checklist:**

**1. Project Structure:**
- [ ] Create new Blazor WASM project (`Sorcha.Admin.Client`)
- [ ] Create ASP.NET Core host project (`Sorcha.Admin.Server`) for static file serving
- [ ] Move Razor components to `.Client` project
- [ ] Move models to shared library

**2. Service Layer:**
- [ ] ✅ Keep `IAuthenticationService`, `AuthenticationService` (no changes)
- [ ] ✅ Keep `BrowserTokenCache` (no changes)
- [ ] ✅ Keep `IConfigurationService`, `ConfigurationService` (no changes)
- [ ] ✅ Keep `BrowserEncryptionProvider` (no changes)
- [ ] ⚠️ Update `CustomAuthenticationStateProvider` → **Remove all circuit-related workarounds**
- [ ] ✅ Keep `AuthenticatedHttpMessageHandler` (no changes)

**3. Component Updates:**
- [ ] ⚠️ Remove `@rendermode` directives (WASM doesn't use render modes)
- [ ] ⚠️ Remove prerendering workarounds (OnAfterRenderAsync for JSInterop)
- [ ] ✅ Keep all MudBlazor provider instances (no changes)
- [ ] ✅ Keep `CascadingAuthenticationState` in Routes.razor

**4. Authentication Flow:**
- [ ] ✅ Keep OAuth2 Password Grant flow (no changes)
- [ ] ✅ Keep token caching in LocalStorage (no changes)
- [ ] ✅ Keep encryption with Web Crypto API (no changes)
- [ ] **❌ REMOVE** all `NotifyAuthenticationStateChanged()` workarounds in components
- [ ] **✅ TEST** authentication state persistence across page navigations (should work naturally)

**5. Program.cs Updates:**
- [ ] Change `builder.Services.AddServerSideBlazor()` → `builder.Services.AddBlazorWebAssemblyServices()`
- [ ] Keep all DI registrations (authentication, configuration, encryption, HTTP)
- [ ] Update HTTP client base address to WASM host

**6. Testing:**
- [ ] Test login flow
- [ ] Test authentication state display in UI (top bar, navigation)
- [ ] Test profile switching
- [ ] Test token refresh
- [ ] Test logout
- [ ] Test all API integrations (Blueprint, Wallet, Register, Peer services)

**7. Deployment:**
- [ ] Update Dockerfile for WASM hosting
- [ ] Update docker-compose.yml
- [ ] Test HTTPS configuration
- [ ] Verify Web Crypto API availability over HTTPS

### Estimated Migration Effort

**Total Effort:** 2-3 days

**Breakdown:**
- Project setup and structure: 4 hours
- Component migration: 6 hours
- Service layer updates: 2 hours
- Testing and debugging: 8 hours
- Documentation updates: 2 hours

---

## Future Enhancements

### Planned Features (Post-Migration)

**Authentication:**
- Multi-Factor Authentication (MFA) - TOTP, SMS, email
- OAuth2/OIDC Integration - Google, Microsoft, GitHub
- Device Code Flow - Headless/CLI authentication
- Certificate-based authentication - Client certs via Web Crypto API
- Session management - View active sessions, revoke tokens

**Authorization:**
- Role-based UI - Hide/show features based on user roles
- Fine-grained permissions - Attribute-based access control (ABAC)
- Audit logging - Track all auth/authz events

**Blueprint Designer:**
- Advanced visual editor - Custom node types, edge routing
- Version control - Blueprint versioning and diff
- Collaboration - Multi-user editing (SignalR)
- Simulation mode - Test blueprint execution before deployment

**Administration:**
- User management - Create, edit, delete users
- Role management - Create custom roles, assign permissions
- Audit log viewer - Search, filter, export audit events
- System health dashboard - Real-time metrics, alerting

**Developer Tools:**
- API Explorer - Swagger-style API testing interface
- Log viewer - Real-time log streaming from services
- Database browser - View/edit data in Tenant/Register/Wallet databases
- Performance profiling - Identify bottlenecks

**Progressive Web App (PWA):**
- Offline support - Service Worker for offline blueprint editing
- App installation - Install as desktop/mobile app
- Push notifications - Real-time alerts

---

## Conclusion

This document captures the complete feature set, architecture, and implementation details of Sorcha.Admin as of **2026-01-06**.

**Next Steps:**
1. Review [KNOWN-ISSUES.md](KNOWN-ISSUES.md) for critical blockers
2. Decide on migration to Blazor WebAssembly
3. Use this document as reference for migration planning
4. Update this document as new features are added

**For questions or clarifications, see:**
- [README.md](README.md) - User-facing documentation
- [KNOWN-ISSUES.md](KNOWN-ISSUES.md) - Current technical issues
- Sorcha Platform Documentation - [Main README](../../../README.md)

---

**Document Version:** 1.0
**Last Updated:** 2026-01-06
**Status:** Complete - Ready for migration planning
