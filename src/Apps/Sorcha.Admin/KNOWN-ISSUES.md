# Known Issues - Sorcha.Admin

**Last Updated:** 2026-01-05

## 1. Encryption.js Error Over HTTP

**Status:** Non-Critical
**Severity:** Medium (blocks encryption features, not core functionality)

### Description
Browser console shows error when accessing the application over HTTP:
```
TypeError: Cannot read properties of undefined (reading 'importKey')
    at Object.importKey (encryption.js:24:41)
```

### Root Cause
The Web Crypto API (`crypto.subtle`) is only available in secure contexts:
- HTTPS connections
- localhost (HTTP is allowed for development)
- File URLs

When accessing the application via HTTP on a non-localhost address (e.g., `http://192.168.51.103/`), the `crypto.subtle` object is `undefined`.

### Impact
- Client-side encryption features will not function
- Basic UI navigation and rendering are unaffected
- Authentication and configuration management still work (using LocalStorage without encryption)

### Workaround
Access the application via:
- HTTPS (recommended for production)
- http://localhost (for local development)

### Potential Fixes

**Option 1: Configure HTTPS (Recommended)**
1. Update docker-compose.yml to use HTTPS certificates
2. Configure API Gateway to serve sorcha-admin over HTTPS
3. Update all internal URLs to use HTTPS

**Option 2: Make Encryption Optional**
1. Modify encryption.js to detect if `crypto.subtle` is available
2. Fall back to plaintext storage with user warning
3. Add UI indicator when encryption is unavailable

**Option 3: Polyfill for Development**
- Use a crypto polyfill library for HTTP development environments
- Note: Less secure, only for development

### Code Location
- `Sorcha.Admin.Client/wwwroot/js/encryption.js` (lines around 24)
- `BrowserEncryptionProvider.cs` - C# wrapper calling encryption.js

---

## 2. System Status Shows Offline by Default

**Status:** Configuration Issue
**Severity:** Low (cosmetic, doesn't affect functionality)

### Description
The System Status card on the dashboard shows:
- "System Offline" alert
- "0 / 1 services healthy"
- Default profile pointing to `http://localhost/api/tenant`

### Root Cause
Default configuration profile in ConfigurationService uses localhost URLs which don't resolve to actual services in Docker environment.

### Impact
- Visual indicator shows system as offline
- Doesn't prevent navigation or authentication
- Actual functionality depends on user configuring proper profile

### Potential Fix
Update default profile in ConfigurationService to use Docker service discovery URLs:
```csharp
new AdminProfile
{
    Name = "Docker (Default)",
    TenantServiceUrl = "http://api-gateway:8080/api/tenant",
    BlueprintServiceUrl = "http://api-gateway:8080/api/blueprints",
    // ... other services
}
```

Alternatively, initialize from environment variables:
```csharp
TenantServiceUrl = Environment.GetEnvironmentVariable("ApiGateway__BaseUrl")
    ?? "http://localhost/api/tenant"
```

### Code Location
- `Sorcha.Admin/Services/Configuration/ConfigurationService.cs` - Default profile initialization
- `Sorcha.Admin/Components/SystemStatusCard.razor` - Health check logic

---

## Fixed Issues (Reference)

### ✅ Main Content Not Rendering (FIXED 2026-01-05)
**Problem:** Entire page content area was empty, only navigation rendered
**Cause:** SystemStatusCard component blocking on synchronous LocalStorage access during prerendering
**Fix:** Modified `GetEnvironmentName()` and `GetApiEndpoint()` to check task completion before accessing Result
**Files:** `src/Apps/Sorcha.Admin/Components/SystemStatusCard.razor` (lines 179-214)

### ✅ API Gateway Connection Refused (FIXED 2026-01-05)
**Problem:** Container logs showed "Connection refused (localhost:8061)"
**Cause:** sorcha-admin using localhost instead of Docker service name
**Fix:** Added environment variables to docker-compose.yml for API Gateway URL
**Files:** `docker-compose.yml` (lines 338-343)

### ✅ Antiforgery Token Decryption Errors (FIXED 2026-01-05)
**Problem:** "The antiforgery token could not be decrypted" on every restart
**Cause:** Data protection keys not persisted across container restarts
**Fix:** Added persistent volume mount for data protection keys
**Files:** `docker-compose.yml` (line 345)

---

## Testing Notes

### Verified Working (2026-01-05)
- ✅ Page renders with full content (welcome message, feature list, dashboard widgets)
- ✅ Navigation sidebar and top bar functional
- ✅ Blazor Server circuit establishes correctly
- ✅ Sign In button navigates to login page
- ✅ Recent Activity log displays events
- ✅ System Status card renders (shows offline state, but component works)

### Tested With
- Playwright browser automation
- Docker container: sorcha-admin
- Browser: Chromium
- Access URL: http://192.168.51.103/

---

## Recommendations

### Priority 1 (Security)
- [ ] Configure HTTPS for production deployment
- [ ] Enable certificate validation in API Gateway
- [ ] Update all URLs to use HTTPS

### Priority 2 (User Experience)
- [ ] Fix encryption.js to work over HTTP with fallback
- [ ] Configure default profile with correct service URLs
- [ ] Add health check that works in ASP.NET runtime (not wget)

### Priority 3 (Monitoring)
- [ ] Add structured logging for encryption errors
- [ ] Add telemetry for health check failures
- [ ] Monitor container startup time and health

---

**For questions or updates to this document, see:**
- Git commit history for recent fixes
- Docker logs: `docker logs sorcha-admin`
- Browser console for client-side errors
