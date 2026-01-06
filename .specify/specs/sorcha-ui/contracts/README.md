# API Contracts: Sorcha.UI

**Purpose**: Client-side service interface definitions for Sorcha.UI

**Note**: Sorcha.UI is a **client application** that **consumes** backend REST APIs. These contracts define the **client-side service interfaces**, not REST API endpoints.

---

## Service Interfaces

| Interface | Purpose | Implementation | Location |
|-----------|---------|----------------|----------|
| **IAuthenticationService** | OAuth2 Password Grant authentication | `AuthenticationService.cs` | Sorcha.UI.Core/Services/Authentication/ |
| **ITokenCache** | Encrypted JWT token storage | `BrowserTokenCache.cs` (Web), `SecureStorageTokenCache.cs` (MAUI) | Sorcha.UI.Core/Services/Authentication/ |
| **IEncryptionProvider** | AES-256-GCM encryption for LocalStorage | `BrowserEncryptionProvider.cs` (Web), `MauiEncryptionProvider.cs` (MAUI) | Sorcha.UI.Core/Services/Encryption/ |
| **IConfigurationService** | Profile and UI configuration management | `ConfigurationService.cs` | Sorcha.UI.Core/Services/Configuration/ |

---

## Backend API Dependencies

Sorcha.UI consumes these backend REST APIs (via API Gateway):

| API | Service | Purpose | Client |
|-----|---------|---------|--------|
| `POST /api/service-auth/token` | Tenant Service | OAuth2 Password Grant login | `AuthenticationService.LoginAsync()` |
| `POST /api/service-auth/refresh` | Tenant Service | OAuth2 token refresh | `AuthenticationService.RefreshTokenAsync()` |
| `GET /api/blueprints` | Blueprint Service | List blueprints | Designer module |
| `POST /api/blueprints` | Blueprint Service | Create blueprint | Designer module |
| `GET /api/blueprints/{id}` | Blueprint Service | Get blueprint details | Designer module |
| `PUT /api/blueprints/{id}` | Blueprint Service | Update blueprint | Designer module |
| `DELETE /api/blueprints/{id}` | Blueprint Service | Delete blueprint | Designer module |
| `GET /api/registers` | Register Service | List registers | Explorer module |
| `GET /api/registers/{id}/transactions` | Register Service | List transactions | Explorer module |
| `GET /api/transactions/{id}` | Register Service | Get transaction details | Explorer module |
| `GET /api/health` | All Services | Service health check | Admin module |

**HTTP Client**: All API calls use `AuthenticatedHttpMessageHandler` to inject JWT Bearer tokens automatically.

**Service Client Library**: Backend API calls are made via `Sorcha.ServiceClients` library (shared across Sorcha platform).

---

## Client-Side Service Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor WASM Application                   │
└─────────────────────────────────────────────────────────────┘
        │
        ├─► IAuthenticationService
        │   ├─► ITokenCache (BrowserTokenCache)
        │   │   └─► IEncryptionProvider (BrowserEncryptionProvider)
        │   │       └─► JavaScript Interop (encryption.js)
        │   │           └─► Web Crypto API
        │   │
        │   └─► HttpClient + AuthenticatedHttpMessageHandler
        │       └─► API Gateway (YARP)
        │           └─► Tenant Service (gRPC internally)
        │
        ├─► IConfigurationService
        │   └─► IJSRuntime (LocalStorage access)
        │
        └─► Sorcha.ServiceClients (REST/HTTP)
            ├─► Blueprint Service API
            ├─► Register Service API
            └─► Tenant Service API
```

---

## Authentication Flow (Interface Collaboration)

```
1. User submits login form (Login.razor)
   ↓
2. IAuthenticationService.LoginAsync(request, profileName)
   ↓
3. POST /api/service-auth/token (Tenant Service)
   ← TokenResponse { access_token, refresh_token, expires_in }
   ↓
4. ITokenCache.StoreTokenAsync(profileName, tokenEntry)
   ↓
5. IEncryptionProvider.EncryptAsync(JSON.serialize(tokenEntry))
   ↓
6. JavaScript Interop → localStorage.setItem("sorcha:tokens:dev", encrypted)
   ↓
7. CustomAuthenticationStateProvider.NotifyAuthenticationStateChanged()
   ↓
8. UI updates (AuthorizeView shows authenticated content)
```

---

## Contract Validation Rules

### IAuthenticationService

**LoginAsync**:
- ✅ `request.Username`: Required, 3-256 characters
- ✅ `request.Password`: Required, 8-128 characters
- ✅ `profileName`: Must exist in IConfigurationService.GetProfilesAsync()
- ✅ Returns: TokenResponse with non-empty AccessToken, ExpiresIn > 0
- ❌ Throws: `HttpRequestException` on 401/403, `InvalidOperationException` if profile not found

**RefreshTokenAsync**:
- ✅ Requires: Cached RefreshToken for profile (from ITokenCache)
- ✅ Returns: `true` if refresh succeeded, `false` if refresh failed (re-login required)
- ⚠️ Side Effect: Updates TokenCacheEntry in ITokenCache on success

**LogoutAsync**:
- ✅ Effect: Calls `ITokenCache.RemoveTokenAsync(profileName)`
- ✅ Effect: Calls `CustomAuthenticationStateProvider.NotifyAuthenticationStateChanged()`

### ITokenCache

**StoreTokenAsync**:
- ✅ `entry.AccessToken`: Required, non-empty
- ✅ `entry.ExpiresAt`: Required, > DateTime.UtcNow
- ✅ Side Effect: Encrypts token via IEncryptionProvider before storing
- ❌ Throws: `InvalidOperationException` if encryption fails

**GetTokenAsync**:
- ✅ Returns: `null` if token not found or expired
- ✅ Returns: `null` if decryption fails (corrupted data)
- ⚠️ Side Effect: Removes expired token from storage

### IEncryptionProvider

**EncryptAsync**:
- ✅ `plaintext`: Required, non-empty
- ✅ Returns: Base64-encoded ciphertext + IV (format: `{iv}:{ciphertext}`)
- ❌ Throws: `NotSupportedException` if Web Crypto API unavailable (HTTP non-localhost)

**DecryptAsync**:
- ✅ `ciphertext`: Required, valid format (`{iv}:{ciphertext}`)
- ❌ Throws: `CryptographicException` if decryption fails (wrong key, corrupted data)

### IConfigurationService

**SetActiveProfileAsync**:
- ✅ `profileName`: Must exist in profiles list
- ❌ Throws: `InvalidOperationException` if profile not found
- ⚠️ Warning: Requires logout if switching from different active profile

**SaveProfileAsync**:
- ✅ `profile.Name`: Required, unique (except for updates)
- ✅ `profile.ApiGatewayUrl`: Required, valid URL
- ✅ All service URLs: Required, valid URLs
- ❌ Throws: `ArgumentException` if validation fails

---

## Testing Strategy

### Unit Tests (xUnit + Moq)

- **IAuthenticationService**: Mock ITokenCache, IEncryptionProvider, HttpClient
- **ITokenCache**: Mock IEncryptionProvider, IJSRuntime (LocalStorage)
- **IEncryptionProvider**: Mock IJSRuntime (Web Crypto API via JS Interop)
- **IConfigurationService**: Mock IJSRuntime (LocalStorage)

### Integration Tests (bUnit + Testcontainers)

- **End-to-end login flow**: Login.razor → IAuthenticationService → backend API (mocked)
- **Token refresh**: AuthenticatedHttpMessageHandler triggers refresh on 401
- **Profile switching**: ProfileSelector.razor → IConfigurationService → logout → login

---

## Implementation Priority

**Phase 1** (MVP - Week 1):
1. ✅ IAuthenticationService (OAuth2 Password Grant)
2. ✅ ITokenCache (LocalStorage encryption)
3. ✅ IEncryptionProvider (Web Crypto API)
4. ✅ IConfigurationService (Profile management)

**Phase 2** (Post-MVP):
5. ⏭️ MAUI implementations (SecureStorageTokenCache, MauiEncryptionProvider)
6. ⏭️ Offline support (service workers, IndexedDB)

---

**Document Version**: 1.0 | **Last Updated**: 2026-01-06
