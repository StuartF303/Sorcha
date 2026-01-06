# Sorcha Admin - Blueprint Designer & Administration Portal

A modern Blazor WebAssembly application for designing blueprints and administering the Sorcha platform.

## Features

### Blueprint Designer
- **Visual Blueprint Editor**: Drag-and-drop interface for creating workflow blueprints
- **Schema Management**: Configure JSON schemas for data validation
- **Real-time Validation**: Client-side blueprint validation using the portable execution engine
- **Template Library**: Pre-built blueprint templates for common scenarios

### Authentication & Configuration
- **JWT-Based Authentication**: Secure OAuth2 Password Grant flow integration with Tenant Service
- **Multi-Environment Support**: Switch between dev, local, docker, aspire, staging, and production profiles
- **Profile Management**: Create, edit, and manage custom environment configurations
- **Encrypted Token Storage**: AES-256-GCM encryption for secure token storage in browser LocalStorage
- **Automatic Token Refresh**: Transparent token refresh when approaching expiration (&lt;5 minutes)

### Administration
- **Service Health Monitoring**: Real-time health checks for all Sorcha services
- **User Management**: (Coming soon) Manage users, roles, and permissions
- **Audit Logging**: (Coming soon) View system audit logs and activity

## Getting Started

### Prerequisites

- .NET 10 SDK or later
- A running Sorcha backend environment (Tenant Service, Blueprint Service, etc.)

### Running Locally

```bash
# Navigate to the Sorcha.Admin project directory
cd src/Apps/Sorcha.Admin

# Restore dependencies
dotnet restore

# Run the application
dotnet run
```

The application will be available at `https://localhost:7083` (or the port configured in `launchSettings.json`).

### Building for Production

```bash
# Build optimized production bundle
dotnet publish -c Release

# Output will be in: bin/Release/net10.0/publish/wwwroot
```

## Authentication

### First-Time Login

1. Navigate to the application - you'll be redirected to `/login`
2. Select your environment from the dropdown (default: `dev`)
3. Enter credentials:
   - **Username**: `admin@sorcha.local`
   - **Password**: (provided by your administrator)
4. Click "Login"

Upon successful authentication:
- JWT access token is encrypted and stored in browser LocalStorage
- You're redirected to the Blueprint Designer
- Your session persists across browser refreshes

### Default Profiles

The application comes with 6 pre-configured environment profiles:

| Profile | Tenant Service URL | Use Case |
|---------|-------------------|----------|
| **dev** | `https://localhost:7080` | Local development (HTTPS, self-signed certs) |
| **local** | `http://localhost:5080` | Local development (HTTP, no SSL) |
| **docker** | `http://localhost:8080/tenant` | Docker Compose deployment |
| **aspire** | `https://localhost:7051/api/tenant` | .NET Aspire orchestration |
| **staging** | `https://n0.sorcha.dev` | Staging environment |
| **production** | `https://tenant.sorcha.io` | Production environment |

### OAuth2 Password Grant Flow

The application uses the **OAuth2 Password Grant** flow (RFC 6749 Section 4.3):

1. User enters username and password
2. Application sends POST request to `{profile.AuthTokenUrl}`:
   ```http
   POST /api/service-auth/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=password&username=admin@sorcha.local&password=***&client_id=sorcha-admin
   ```
3. Tenant Service validates credentials and returns JWT token:
   ```json
   {
     "access_token": "eyJhbGc...",
     "token_type": "Bearer",
     "expires_in": 1800,
     "refresh_token": "..."
   }
   ```
4. Token is encrypted and stored in LocalStorage: `sorcha:tokens:{profileName}`
5. All API requests automatically include: `Authorization: Bearer {token}`

### Token Security

**Encryption:**
- Algorithm: AES-256-GCM via Web Crypto API
- Key Derivation: PBKDF2 (100,000 iterations) from browser fingerprint
- Storage: Encrypted tokens stored as Base64 in LocalStorage

**Limitations:**
- Browser encryption is NOT as secure as OS keystores (Windows DPAPI, macOS Keychain)
- Protects against casual inspection but not XSS attacks or malicious browser extensions
- Use short token lifetimes (default: 30 minutes) and aggressive refresh as mitigation

**Token Refresh:**
- Tokens automatically refresh when &lt;5 minutes remaining
- Refresh uses `grant_type=refresh_token` flow
- If refresh fails, user is prompted to re-authenticate

### Switching Environments

**Quick Switch (AppBar):**
1. Click the environment dropdown in the top navigation bar
2. Select a different profile (e.g., `dev` → `staging`)
3. You'll be prompted to login again if tokens don't exist for that profile

**Profile Management (Settings):**
1. Click the user icon → "Settings"
2. Navigate to the "Configuration" tab
3. View all profiles, create new ones, edit existing ones

### Creating Custom Profiles

1. Navigate to **Settings → Configuration**
2. Click "New Profile"
3. Configure the profile:
   - **Profile Name**: Alphanumeric, dashes, underscores only (e.g., `my-custom-env`)
   - **Service URLs**: Complete URLs for each Sorcha service
   - **Auth Token URL**: OAuth2 token endpoint (e.g., `https://myserver.com/api/service-auth/token`)
   - **Client ID**: OAuth2 client identifier (default: `sorcha-admin`)
   - **Timeout**: Request timeout in seconds (1-300)
   - **Verify SSL**: Enable/disable SSL certificate verification (disable only for dev with self-signed certs)
4. Click "Create"

### Logout

1. Click the user icon in the top-right corner
2. Select "Logout"
3. Tokens are cleared from LocalStorage
4. You're redirected to `/login`

## Configuration Management

### Configuration Storage

Configuration is stored in browser LocalStorage at key `sorcha:config`:

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
      "timeoutSeconds": 30
    }
  }
}
```

### Profile Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Unique profile identifier (alphanumeric, dash, underscore) |
| `tenantServiceUrl` | string | Yes | Base URL for Tenant Service API |
| `registerServiceUrl` | string | Yes | Base URL for Register Service API |
| `peerServiceUrl` | string | Yes | Base URL for Peer Service API |
| `walletServiceUrl` | string | Yes | Base URL for Wallet Service API |
| `blueprintServiceUrl` | string | Yes | Base URL for Blueprint Service API |
| `authTokenUrl` | string | Yes | OAuth2 token endpoint (full URL) |
| `defaultClientId` | string | No | OAuth2 client ID (default: `sorcha-admin`) |
| `verifySsl` | boolean | No | Verify SSL certificates (default: `true`) |
| `timeoutSeconds` | int | No | Request timeout in seconds (default: `30`, range: 1-300) |

### Sharing Configuration with Sorcha CLI

The Sorcha.Admin configuration structure is **identical** to the Sorcha CLI configuration. You can:

1. **Export from CLI to Admin**:
   - Copy `%USERPROFILE%\.sorcha\config.json` (Windows) or `~/.sorcha/config.json` (Linux/macOS)
   - Open browser DevTools → Application → Local Storage → `sorcha:config`
   - Paste the JSON into the value field

2. **Export from Admin to CLI**:
   - Open browser DevTools → Application → Local Storage → `sorcha:config`
   - Copy the JSON value
   - Save to `~/.sorcha/config.json` or `%USERPROFILE%\.sorcha\config.json`

## Architecture

### Technology Stack

- **Frontend Framework**: Blazor WebAssembly (.NET 10)
- **UI Components**: MudBlazor 8.15.0
- **State Management**: Blazor built-in (`CascadingAuthenticationState`)
- **HTTP Client**: `IHttpClientFactory` with `DelegatingHandler` for JWT injection
- **Storage**: Blazored.LocalStorage 4.5.0
- **Encryption**: Web Crypto API (SubtleCrypto)
- **Authentication**: ASP.NET Core Components.Authorization 10.0.0

### Project Structure

```
Sorcha.Admin/
├── Components/
│   ├── Authentication/
│   │   ├── LoginDialog.razor              # Login modal with profile selector
│   │   ├── ProfileSelector.razor          # Quick environment switcher (AppBar)
│   │   ├── UserProfileMenu.razor          # User menu with logout
│   │   └── RedirectToLogin.razor          # Auth guard for unauthorized access
│   ├── Configuration/
│   │   └── ProfileEditorDialog.razor      # Full profile CRUD editor
│   └── [Blueprint Designer Components]
├── Models/
│   ├── Authentication/
│   │   ├── LoginRequest.cs                # OAuth2 password grant request
│   │   ├── TokenResponse.cs               # OAuth2 token response (RFC 6749)
│   │   ├── TokenCacheEntry.cs             # Cached token with expiration
│   │   └── AuthenticationStateInfo.cs     # Blazor auth state
│   └── Configuration/
│       ├── Profile.cs                     # Environment profile model
│       ├── AdminConfiguration.cs          # Root config (profiles + active)
│       └── ProfileDefaults.cs             # Default profile factory
├── Services/
│   ├── Authentication/
│   │   ├── IAuthenticationService.cs      # Auth service abstraction
│   │   ├── AuthenticationService.cs       # OAuth2 implementation
│   │   ├── BrowserTokenCache.cs           # Encrypted LocalStorage cache
│   │   └── CustomAuthenticationStateProvider.cs  # Blazor auth state provider
│   ├── Configuration/
│   │   ├── IConfigurationService.cs       # Config service abstraction
│   │   └── ConfigurationService.cs        # LocalStorage config management
│   ├── Encryption/
│   │   ├── IEncryptionProvider.cs         # Encryption abstraction
│   │   └── BrowserEncryptionProvider.cs   # Web Crypto API encryption
│   └── Http/
│       └── AuthenticatedHttpMessageHandler.cs  # JWT injection handler
├── Pages/
│   ├── Login.razor                        # Standalone login page
│   ├── Home.razor                         # Blueprint Designer
│   └── Settings.razor                     # Settings (with Configuration tab)
├── Layout/
│   └── MainLayout.razor                   # AppBar with ProfileSelector & UserMenu
├── wwwroot/
│   └── js/
│       └── encryption.js                  # SubtleCrypto encryption implementation
└── Program.cs                             # DI registration and app setup
```

### Dependency Injection Setup

Key services registered in `Program.cs`:

```csharp
// Authentication & Authorization
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s =>
    s.GetRequiredService<CustomAuthenticationStateProvider>());

// Configuration
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Authentication Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<BrowserTokenCache>();

// Encryption
builder.Services.AddScoped<IEncryptionProvider, BrowserEncryptionProvider>();

// HTTP with JWT Injection
builder.Services.AddScoped<AuthenticatedHttpMessageHandler>();
builder.Services.AddHttpClient("SorchaAPI")
    .AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();
```

### Authentication Flow

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │ 1. Navigate to app
       ▼
┌─────────────────────────┐
│  AuthenticationState    │ ──► Not authenticated?
│  Provider               │     Redirect to /login
└─────────────────────────┘
       │ 2. User enters credentials
       ▼
┌─────────────────────────┐
│ AuthenticationService   │
│ - LoginAsync()          │ ──► POST /api/service-auth/token
└─────────────────────────┘     (OAuth2 Password Grant)
       │ 3. Token received
       ▼
┌─────────────────────────┐
│  BrowserTokenCache      │
│  - Encrypt token        │ ──► LocalStorage
│  - Store encrypted      │     sorcha:tokens:{profile}
└─────────────────────────┘
       │ 4. AuthState updated
       ▼
┌─────────────────────────┐
│  Components render      │ ──► Show authenticated UI
│  (Blueprint Designer)   │
└─────────────────────────┘
       │ 5. API call needed
       ▼
┌─────────────────────────┐
│ HttpMessageHandler      │
│ - Get token from cache  │ ──► Add Authorization header
│ - Auto-refresh if near │     Bearer {token}
│   expiration            │
└─────────────────────────┘
       │ 6. API request
       ▼
┌─────────────────────────┐
│  Backend Service API    │ ──► Validates JWT
│  (Blueprint/Wallet/etc) │     Returns data
└─────────────────────────┘
```

## Security Considerations

### Browser-Based Encryption Limitations

⚠️ **Important**: Browser LocalStorage encryption is **NOT as secure** as OS-level keystores (Windows DPAPI, macOS Keychain).

**What it protects against:**
- Casual inspection of LocalStorage in DevTools
- Accidental exposure of tokens in screenshots/logs
- Simple attacks without JavaScript execution

**What it does NOT protect against:**
- XSS (Cross-Site Scripting) attacks - malicious scripts can call decryption methods
- Malicious browser extensions with access to the same origin
- Physical access to an unlocked browser session
- Determined attackers with browser debugging access

**Mitigations:**
- Short token lifetimes (default: 30 minutes)
- Automatic token refresh (reduces re-authentication friction)
- Use HTTPS for all production deployments
- Implement Content Security Policy (CSP) headers
- Consider additional security for high-value environments:
  - IP whitelisting
  - Multi-factor authentication (MFA) - coming soon
  - Hardware security keys (future enhancement)

### SSL Certificate Verification

**Production environments** (`staging`, `production`):
- **Always enable** SSL certificate verification (`verifySsl: true`)
- Use certificates from trusted Certificate Authorities (CA)

**Development environments** (`dev`, `local`):
- SSL verification can be disabled for self-signed certificates
- **Never** deploy dev profiles to production environments
- Consider using mkcert for locally-trusted development certificates

## Troubleshooting

### Login Issues

**Problem**: "Invalid username or password"
- **Solution**: Verify credentials with your administrator
- Check Tenant Service logs for authentication errors
- Ensure the Tenant Service is running and accessible

**Problem**: "Failed to connect to authentication server"
- **Solution**: Verify `authTokenUrl` in profile configuration
- Check network connectivity to the Tenant Service
- Review browser console for CORS errors
- Verify Tenant Service is running: `curl https://localhost:7080/health`

**Problem**: "SSL certificate error" (dev environment)
- **Solution**: Set `verifySsl: false` for dev profile
- Or install self-signed certificate as trusted in your OS

### Token Refresh Issues

**Problem**: Frequent re-authentication prompts
- **Solution**: Check token `expires_in` value (should be ≥1800 seconds)
- Verify refresh token is being returned by Tenant Service
- Check browser console for refresh errors

**Problem**: "Token expired" errors during normal use
- **Solution**: Token refresh may be failing silently
- Check browser console for network errors
- Verify `authTokenUrl` is correct in profile configuration

### Profile Management

**Problem**: Can't delete active profile
- **Solution**: This is intentional - switch to a different profile first, then delete

**Problem**: Profile changes not taking effect
- **Solution**: Logout and login again with the new profile
- Clear browser cache if profile changes don't persist
- Check browser DevTools → Application → Local Storage for `sorcha:config`

### Browser Compatibility

**Supported Browsers:**
- Chrome/Edge 90+
- Firefox 88+
- Safari 14.1+

**Required Features:**
- Web Crypto API (SubtleCrypto)
- LocalStorage
- ES2020+ JavaScript features

## Development

### Prerequisites

- .NET 10 SDK
- Node.js (for Blazor tooling)
- Visual Studio 2022 or VS Code with C# extension

### Building

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run with hot reload
dotnet watch run
```

### Testing

```bash
# Run unit tests (if available)
dotnet test

# Run Blazor WASM in development mode
dotnet run --environment Development
```

### Adding New Authentication Methods

The authentication system is designed for extensibility. To add new auth methods:

1. **Add method to `IAuthenticationService`**:
   ```csharp
   Task<TokenResponse> LoginWithDeviceFlowAsync(string profileName);
   Task<TokenResponse> LoginWithOAuthAsync(string provider, string profileName);
   ```

2. **Implement in `AuthenticationService`**:
   - Device Flow: RFC 8628
   - OAuth2/OIDC: Authorization Code flow with PKCE

3. **Add UI components**:
   - `DeviceFlowDialog.razor` - Display device code and poll for completion
   - `OAuthLoginButton.razor` - Redirect to OAuth2 provider

4. **Update Profile model** (if needed):
   ```csharp
   public string? OAuthProvider { get; set; }  // "Google", "Microsoft", etc.
   public string? OAuthClientId { get; set; }
   public string? OAuthRedirectUri { get; set; }
   ```

## Future Enhancements

### Planned Features

- **Multi-Factor Authentication (MFA)**: TOTP, SMS, or email verification
- **OAuth2/OIDC Integration**: Login with Google, Microsoft, GitHub
- **Device Code Flow**: Authentication for headless/CLI scenarios
- **Certificate-based Authentication**: Client certificates via Web Crypto API
- **Session Management**: View active sessions, revoke tokens
- **Audit Logging**: Track all authentication and authorization events
- **Role-Based UI**: Hide/show features based on user roles
- **Offline Support**: Service Worker for offline blueprint editing

### Known Limitations

- No multi-factor authentication (MFA) support yet
- Token refresh relies on refresh token availability
- No session timeout UI warning (auto-refresh mitigates this)
- Profile export/import UI not yet implemented (manual via DevTools)

**For critical known issues and detailed troubleshooting, see [KNOWN-ISSUES.md](KNOWN-ISSUES.md)**

## Support

### Documentation

- **Sorcha Platform**: [Main README](../../../README.md)
- **API Documentation**: [docs/API-DOCUMENTATION.md](../../../docs/API-DOCUMENTATION.md)
- **Development Status**: [docs/development-status.md](../../../docs/development-status.md)
- **Architecture**: [docs/architecture.md](../../../docs/architecture.md)

### Getting Help

- **Issues**: Create a GitHub issue with the `sorcha-admin` label
- **Questions**: Check the main Sorcha documentation
- **Security Concerns**: Report via private security disclosure

## License

Copyright © 2024 Sorcha Project

This project is licensed under the MIT License - see the [LICENSE](../../../LICENSE) file for details.

---

**Built with Blazor WebAssembly and MudBlazor** | **Secured with JWT and Web Crypto API** | **Part of the Sorcha Platform**
