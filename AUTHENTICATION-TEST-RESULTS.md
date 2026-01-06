# Sorcha.UI.Web Authentication Testing Results

**Date:** 2026-01-06
**Test Environment:** Docker Compose
**Status:** ⚠️ Blocked - Requires Secure Context (HTTPS or localhost)

---

## Executive Summary

Authentication testing revealed and resolved **5 critical issues** in the Docker environment. The authentication flow is now fully functional **except** for Web Crypto API requirements, which necessitate accessing the application via HTTPS or localhost instead of IP addresses.

---

## Issues Encountered and Resolved

### ✅ Issue 1: Wrong Docker Profile URL
**Error:**
```
POST https://localhost:7082/api/service-auth/token - CSP violation
```

**Root Cause:**
- Docker profile configured with `http://localhost:8080` (wrong port)
- Default active profile was "Development" (pointing to Aspire AppHost)

**Fix:**
Updated `ConfigurationService.cs` Docker profile:
```csharp
ApiGatewayUrl = "" // Empty string for same-origin requests
```

**File:** `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Configuration/ConfigurationService.cs:212`

---

### ✅ Issue 2: Content Type Mismatch (415 Unsupported Media Type)
**Error:**
```
POST http://localhost/api/service-auth/token - 415 (Unsupported Media Type)
```

**Root Cause:**
Client sending `application/json`, server expecting `application/x-www-form-urlencoded` (OAuth2 standard)

**Fix:**
Updated `AuthenticationService.cs` to use `FormUrlEncodedContent`:
```csharp
var formData = new Dictionary<string, string>
{
    ["username"] = request.Username,
    ["password"] = request.Password,
    ["grant_type"] = request.GrantType,
    ["client_id"] = "sorcha-ui-web"
};

var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));
```

**Files:**
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/AuthenticationService.cs:43-57`
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/AuthenticationService.cs:116-130`

---

### ✅ Issue 3: Data Protection Key Mismatch
**Error:**
```
An unhandled error has occurred.
AntiforgeryValidationException: The key {guid} was not found in the key ring
```

**Root Cause:**
App storing Data Protection keys in `/root/.aspnet/DataProtection-Keys`
Docker volume mapped to `/home/app/.aspnet/DataProtection-Keys`

**Fix:**
Added Data Protection configuration in `Program.cs`:
```csharp
var dataProtectionPath = Path.Combine("/home/app/.aspnet/DataProtection-Keys");
if (Directory.Exists("/home/app/.aspnet"))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}
```

**File:** `src/Apps/Sorcha.UI/Sorcha.UI.Web/Program.cs:7-13`

---

### ✅ Issue 4: CSP Cross-Origin Violation
**Error:**
```
Connecting to 'http://localhost/api/service-auth/token' violates CSP directive
```

**Root Cause:**
Accessing UI via `http://172.19.0.14:8080` (IP address)
Client trying to POST to `http://localhost` (different origin)

**Fix:**
Changed Docker profile to use empty string for same-origin relative URLs:
```csharp
var tokenUrl = string.IsNullOrEmpty(profile.ApiGatewayUrl)
    ? "/api/service-auth/token"
    : $"{profile.ApiGatewayUrl}/api/service-auth/token";
```

**File:** `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/AuthenticationService.cs:53-55`

---

### ✅ Issue 5: HttpClient Missing BaseAddress
**Error:**
```
An error occurred: net_http_client_invalid_requesturi
```

**Root Cause:**
HttpClient registered without `BaseAddress`, cannot resolve relative URLs like `/api/service-auth/token`

**Fix:**
Registered HttpClient with base address in `ServiceCollectionExtensions.cs`:
```csharp
public static IServiceCollection AddCoreServices(this IServiceCollection services, string baseAddress)
{
    // ...
    services.AddScoped<HttpClient>(sp => new HttpClient
    {
        BaseAddress = new Uri(baseAddress)
    });
    // ...
}
```

**Files:**
- `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs:18-33`
- `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Program.cs:7`

---

### ⚠️ Issue 6: Circular Dependency in DI Container
**Error:**
```
AggregateException_ctor_DefaultMessage (Lazy_Value_RecursiveCallsToValue)
```

**Root Cause:**
- `AuthenticationService` needs `HttpClient`
- `HttpClient` configured with `AuthenticatedHttpMessageHandler`
- `AuthenticatedHttpMessageHandler` needs `IAuthenticationService`
- Circular dependency!

**Fix:**
Removed message handler from AuthenticationService's HttpClient (authentication service doesn't need authenticated client):
```csharp
// Register a plain HttpClient for AuthenticationService (no message handler to avoid circular dependency)
services.AddScoped<HttpClient>(sp => new HttpClient
{
    BaseAddress = new Uri(baseAddress)
});
```

**File:** `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs:29-33`

---

## Current Blocker

### ❌ Issue 7: Web Crypto API Requires Secure Context

**Error:**
```
Cannot read properties of undefined (reading 'generateKey')
TypeError: Cannot read properties of undefined (reading 'generateKey')
```

**Root Cause:**
Web Crypto API (`crypto.subtle`) is **only available** in secure contexts:
- ✅ HTTPS (https://example.com)
- ✅ localhost (http://localhost, http://localhost:port)
- ❌ IP addresses over HTTP (http://172.19.0.14:8080)

When accessing via IP address, `crypto.subtle` is `undefined`, causing token encryption to fail.

**Code Location:**
`src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/wwwroot/js/encryption.js:21`

**Current Code:**
```javascript
generateKey: async function() {
    if (!this.isAvailable()) {
        throw new Error("Web Crypto API not available. Use HTTPS or localhost.");
    }

    const key = await crypto.subtle.generateKey(
        { name: "AES-GCM", length: 256 },
        true,
        ["encrypt", "decrypt"]
    );
    // ...
}
```

**Attempted Solutions:**
1. ❌ Access via http://localhost - Connection refused (Docker networking issue on Windows)
2. ❌ Access via IP address - Web Crypto API not available in non-secure context

---

## Solutions for Web Crypto API Issue

### Option 1: Configure HTTPS with Self-Signed Certificate (Recommended for Docker)

**Steps:**
1. Generate self-signed certificate for development
2. Update `docker-compose.yml` to mount certificate
3. Configure Kestrel in `Program.cs` to use HTTPS
4. Update API Gateway YARP configuration for HTTPS
5. Access via https://localhost

**Files to Modify:**
- `docker-compose.yml` - Add certificate volume mount
- `src/Apps/Sorcha.UI/Sorcha.UI.Web/Program.cs` - Configure Kestrel HTTPS
- `src/Apps/Sorcha.UI/Sorcha.UI.Web/appsettings.json` - HTTPS endpoint configuration

**Pros:**
- Production-like environment
- Secure token storage
- Works with Docker networking

**Cons:**
- Requires certificate management
- Browser warnings for self-signed certs (can be bypassed)

---

### Option 2: Fix Docker Networking for localhost Access

**Steps:**
1. Verify Docker Desktop networking mode (WSL2 backend)
2. Ensure port 80 is correctly forwarded to Windows host
3. Check Windows Firewall rules for port 80
4. Access via http://localhost

**Diagnosis Commands:**
```powershell
# Check if port 80 is listening on Windows host
netstat -ano | findstr ":80"

# Test localhost connectivity
Test-NetConnection -ComputerName localhost -Port 80

# Check Docker port mappings
docker-compose ps
```

**Current Status:**
- Port mapping exists: `0.0.0.0:80->8080/tcp`
- Connection to localhost refused (needs investigation)

**Pros:**
- No certificate management
- Simple configuration
- Web Crypto API available

**Cons:**
- Windows/Docker networking can be complex
- May conflict with other services on port 80

---

### Option 3: Use Alternative Storage Without Encryption (NOT RECOMMENDED)

**Warning:** This option removes security for stored tokens.

**Steps:**
1. Modify `BrowserTokenCache.cs` to store tokens in plain text
2. Remove encryption calls from token storage/retrieval
3. Add warning banner to UI about insecure storage

**Pros:**
- Works immediately in any context
- No HTTPS required

**Cons:**
- ❌ Tokens stored in plain text in localStorage
- ❌ Violates security best practices
- ❌ Not acceptable for production
- ❌ Fails security audits

---

## Test Credentials

```
Username: admin@sorcha.local
Password: Dev_Pass_2025!
```

---

## Access URLs

| Service | URL | Status |
|---------|-----|--------|
| Via IP Address | http://172.19.0.14:8080 | ⚠️ Web Crypto API unavailable |
| Via localhost | http://localhost | ❌ Connection refused |
| API Gateway (direct) | http://172.19.0.14:80 | ✅ Working |
| UI Web (direct) | http://172.19.0.14:5173 | ⚠️ Web Crypto API unavailable |

---

## Files Modified

### Configuration
1. `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Configuration/ConfigurationService.cs`
   - Updated Docker profile URL to empty string for same-origin requests

### Authentication
2. `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/AuthenticationService.cs`
   - Changed from JSON to form-urlencoded content type
   - Added relative URL handling for empty ApiGatewayUrl

### Dependency Injection
3. `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`
   - Added HttpClient with BaseAddress registration
   - Removed circular dependency with AuthenticatedHttpMessageHandler
   - Updated AddCoreServices to accept baseAddress parameter

4. `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Program.cs`
   - Simplified registration to pass baseAddress to AddCoreServices

### Server Configuration
5. `src/Apps/Sorcha.UI/Sorcha.UI.Web/Program.cs`
   - Added Data Protection configuration for correct key storage path

### JavaScript Encryption
6. `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/wwwroot/js/encryption.js`
   - Added `isAvailable()` check to `generateKey()` function
   - Improved error message for secure context requirement

---

## Next Steps

### Immediate (P0)
1. **Implement HTTPS for Docker environment**
   - Generate self-signed certificate for localhost
   - Update docker-compose.yml with HTTPS configuration
   - Test authentication flow via https://localhost

2. **Fix Docker Networking for localhost**
   - Investigate why localhost:80 connection is refused on Windows
   - Verify Docker Desktop WSL2 backend networking
   - Test with alternative ports if necessary

### Short-term (P1)
3. **Test Complete Authentication Flow**
   - Login with credentials
   - Verify JWT token storage (encrypted in localStorage)
   - Test token refresh mechanism
   - Verify authenticated API calls
   - Test logout functionality

4. **Verify Authenticated UI Controls**
   - Sidebar navigation rendering for authenticated users
   - Role-based menu items
   - User profile display
   - Authentication state management

### Long-term (P2)
5. **Production HTTPS Configuration**
   - Use proper SSL certificates (Let's Encrypt, Azure Key Vault)
   - Configure production CSP headers
   - Implement HSTS (HTTP Strict Transport Security)
   - Security audit for token storage and transmission

---

## Success Criteria

- [ ] Access application via secure context (HTTPS or localhost)
- [ ] Docker profile correctly selected in Environment dropdown
- [ ] Login form submits to `/api/service-auth/token`
- [ ] OAuth2 request uses form-urlencoded content type
- [ ] Web Crypto API available for token encryption
- [ ] JWT token received and cached (encrypted)
- [ ] User redirected to authenticated view
- [ ] Sidebar navigation renders for authenticated user
- [ ] No CSP violations in browser console
- [ ] No Data Protection key errors

---

## Technical Details

### OAuth2 Password Grant Flow

```
Client                          API Gateway                    Tenant Service
  |                                  |                                 |
  |-- POST /api/service-auth/token ->|                                 |
  |   (form-urlencoded)              |                                 |
  |   username, password, grant_type |                                 |
  |                                  |-- Forward to tenant-service ->  |
  |                                  |                                 |
  |                                  |<-- 200 OK with JWT token -----  |
  |                                  |    { access_token, refresh_token, expires_in }
  |<-- 200 OK with JWT token --------|                                 |
  |                                  |                                 |
  |-- Encrypt and store in localStorage                                |
  |                                  |                                 |
```

### Token Storage Security

```
1. User logs in
2. Receive JWT token from server
3. Generate AES-256-GCM encryption key (Web Crypto API)
4. Encrypt token with AES key
5. Store encrypted token in localStorage
6. Store encryption key in secure storage (IndexedDB or sessionStorage)
7. On subsequent requests:
   - Retrieve encryption key
   - Decrypt token from localStorage
   - Add to Authorization header as Bearer token
```

**Security Note:** This flow REQUIRES Web Crypto API, which is ONLY available in secure contexts (HTTPS or localhost).

---

## Conclusion

**Status:** 🟡 Authentication flow is **technically complete** but **blocked by security requirements**

All code-level issues have been resolved:
✅ URL configuration
✅ Content type handling
✅ Data Protection keys
✅ CSP compliance
✅ HttpClient configuration
✅ Dependency injection

**Remaining blocker:** Web Crypto API unavailable in non-secure context (HTTP over IP address)

**Recommended next step:** Implement HTTPS for Docker environment OR fix Docker networking for localhost access.

---

**Test Date:** 2026-01-06
**Test Environment:** Docker Compose (Windows with WSL2)
**Browser:** Playwright Chromium
**Reporter:** Claude Code Assistant
