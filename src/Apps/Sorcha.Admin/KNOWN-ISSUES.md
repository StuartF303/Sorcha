# Known Issues - Sorcha.Admin

**Last Updated:** 2026-01-06

## ❌ CRITICAL: Authentication State Not Displaying After Login (Blazor Server)

**Status:** ❌ BLOCKING - Unresolved
**Severity:** **CRITICAL - Production Blocker**
**Component:** Authentication, UI State Management, Blazor Server Circuits
**Blazor Mode:** Server (InteractiveServer)

### Description

After successful login, the JWT token is correctly stored in LocalStorage, but the UI still displays "Login" link instead of the authenticated user profile menu. The authentication state stored during login is not propagated to the new Blazor circuit created after navigation from login page to home page.

### Symptoms

1. ✅ Login succeeds, token stored in LocalStorage (confirmed via DevTools)
2. ✅ `AuthenticationService.LoginAsync()` completes successfully
3. ✅ `BrowserTokenCache` stores encrypted token
4. ❌ Navigation to home page creates NEW Blazor WebSocket circuit
5. ❌ UI shows "Login" link instead of user profile menu
6. ❌ `CustomAuthenticationStateProvider.GetAuthenticationStateAsync()` is **NEVER called** in new circuit
7. ❌ Component lifecycle methods (`OnInitializedAsync`, `OnAfterRenderAsync`) are **NEVER called** despite extensive logging
8. ❌ AuthorizeView components don't receive authenticated state

###Root Cause

**Blazor Server Circuit Isolation:**
- Each navigation creates a new Blazor Server circuit with separate DI scope
- `AuthenticationStateProvider` in new circuit never automatically retrieves token from LocalStorage
- Authentication state doesn't transfer between circuits without explicit serialization
- Interactive render modes (`@rendermode InteractiveServer`) not properly cascading to components OR components not executing in interactive context

### Evidence from Console Logs

**Login succeeds (in login page circuit):**
```
[INFO] [AuthenticationService] ✓ Login completed successfully for 'admin@sorcha.local'
[INFO] [BrowserTokenCache] ✓ Token successfully stored for profile 'docker'
```

**New circuit created after navigation:**
```
[INFO] [2026-01-05T19:41:04.047Z] WebSocket connected to ws://192.168.51.103/_blazor?id=NEW_ID
```

**But NO component lifecycle logs appear (indicating components not running in interactive mode):**
```
❌ MISSING: [Index] OnInitializedAsync called
❌ MISSING: [Index] OnAfterRenderAsync called
❌ MISSING: [MainLayout] OnAfterRenderAsync called
❌ MISSING: [CustomAuthStateProvider] GetAuthenticationStateAsync called
```

### Attempted Fixes (20+ iterations, ALL FAILED)

**A. Render Mode Configurations:**
1. ✗ `@rendermode InteractiveServer` on Index.razor
2. ✗ `@rendermode InteractiveServer` on Login.razor
3. ✗ `@rendermode InteractiveServer` on MainLayout.razor
4. ✗ `@rendermode InteractiveServer` on Routes component (in App.razor)
5. ✗ `@rendermode="@(new InteractiveServerRenderMode(prerender: false))"` on Routes
6. ✗ Various combinations of above (pages + layout, routes + pages, routes + login, etc.)

**B. Authentication State Triggers:**
7. ✗ `AuthStateProvider.NotifyAuthenticationStateChanged()` in Login.razor after login
8. ✗ `AuthStateProvider.NotifyAuthenticationStateChanged()` in Index.OnInitializedAsync
9. ✗ `AuthStateProvider.NotifyAuthenticationStateChanged()` in Index.OnAfterRenderAsync
10. ✗ `AuthStateProvider.NotifyAuthenticationStateChanged()` in MainLayout.OnAfterRenderAsync
11. ✗ Explicit `await GetAuthenticationStateAsync()` call in MainLayout.OnInitializedAsync
12. ✗ `StateHasChanged()` after NotifyAuthenticationStateChanged()
13. ✗ `Task.Delay(50-100ms)` to allow state propagation

**C. Navigation Strategies:**
14. ✗ `Navigation.NavigateTo("/", forceLoad: true)` - creates new circuit, state lost
15. ✗ `Navigation.NavigateTo("/", forceLoad: false)` - same circuit, but state still not showing
16. ✗ 100ms delay before navigation
17. ✗ No navigation (stay on login page after auth) - still doesn't work

**D. Component Hierarchy & Cascading:**
18. ✗ `<CascadingAuthenticationState>` in Routes.razor (already present)
19. ✗ `<CascadingAuthenticationState>` in MainLayout.razor
20. ✗ Removed duplicate CascadingAuthenticationState instances
21. ✗ MudBlazor providers in different locations (Login vs MainLayout vs Routes)

**E. Debug Logging (to identify root cause):**
22. ✗ Comprehensive logging in `CustomAuthenticationStateProvider.GetAuthenticationStateAsync()` - **logs NEVER appear**
23. ✗ Logging in `MainLayout.OnAfterRenderAsync()` - **logs NEVER appear**
24. ✗ Logging in `Index.OnInitializedAsync()` - **logs NEVER appear**
25. ✗ Logging in `Index.OnAfterRenderAsync()` - **logs NEVER appear**

**Conclusion:** Components are NOT running in interactive mode despite all configuration attempts.

### Impact

- ✅ **Blocks all authenticated features testing**
- ✅ **Blocks production deployment**
- ✅ **Confusing user experience** (appear logged out after successful login)
- ✅ **Cannot verify authorization features** (AuthorizeView, [Authorize] attributes)
- ✅ **Undermines authentication architecture**

### Recommended Solutions

Based on 20+ fix attempts and extensive debugging, this appears to be a fundamental Blazor Server architecture limitation with LocalStorage-based authentication across circuit recreation.

**🔴 Option 1: Migrate to Blazor WebAssembly (RECOMMENDED for Sorcha.Admin)**

**Pros:**
- ✅ No circuit isolation - single SPA runs in browser
- ✅ State persists naturally in browser memory between navigations
- ✅ AuthenticationStateProvider works as expected
- ✅ LocalStorage naturally accessible in same context
- ✅ Better user experience (no WebSocket dependencies, works offline)
- ✅ Scales better (no server-side circuit memory)

**Cons:**
- ⚠️ Requires app migration from Server to WASM
- ⚠️ Initial download size larger (but cached)
- ⚠️ No server-side prerendering benefits

**Effort:** Medium (2-3 days for full migration and testing)

---

**🟡 Option 2: Persistent Authentication State Serialization**

Implement `PersistentAuthenticationStateProvider` with `<AuthenticationStateSerialization />`:
- Serializes auth state from prerender → interactive
- Stores in hidden form field or JavaScript
- Requires custom implementation to load from LocalStorage

**Pros:**
- ✅ Stays with Blazor Server
- ✅ Official Blazor pattern for auth state persistence

**Cons:**
- ⚠️ Complex implementation
- ⚠️ Not well-documented for LocalStorage scenario
- ⚠️ May still have circuit isolation issues

**Reference:** https://learn.microsoft.com/aspnet/core/blazor/security/server/additional-scenarios#pass-tokens-to-a-blazor-server-app

**Effort:** Medium-High (requires significant refactoring)

---

**🟡 Option 3: Session-Based Authentication (Server-Side)**

Move from client-side JWT LocalStorage to server-side session cookies:
- Store auth state in server memory/Redis
- Use cookie-based authentication
- Natural fit for Blazor Server circuits

**Pros:**
- ✅ Works naturally with Blazor Server architecture
- ✅ More secure (tokens never in browser)
- ✅ Simpler circuit state management

**Cons:**
- ⚠️ Requires backend changes to support cookie auth
- ⚠️ Loses OAuth2 JWT benefits
- ⚠️ Session state management complexity
- ⚠️ Doesn't align with distributed microservices architecture

**Effort:** High (requires backend auth changes)

---

**🔵 Option 4: Hybrid Rendering (WASM for Auth)**

Use static SSR for public pages, WASM for authenticated pages:
- Login page: Static SSR or Server
- Authenticated pages (Blueprint Designer): WASM
- Leverages .NET 8+ per-page render modes

**Pros:**
- ✅ Best of both worlds
- ✅ Solves auth state persistence
- ✅ Fast initial load

**Cons:**
- ⚠️ Complex architecture
- ⚠️ Requires .NET 8+ features
- ⚠️ May have transition issues between modes

**Effort:** High (architectural changes)

---

### Decision Required

**RECOMMENDATION: Migrate to Blazor WebAssembly (Option 1)**

**Rationale:**
1. Cleanest solution - aligns with SPA architecture
2. Eliminates all circuit-related state issues
3. Better user experience for admin/designer tools
4. Easier to test and debug
5. Similar effort to Option 2 but with better long-term benefits
6. No backend changes required

**Next Steps:**
1. ✅ Create FEATURE-REQUIREMENTS.md capturing all current Sorcha.Admin features
2. ✅ Document known issues (this file)
3. ⏭️ Create new Blazor WASM project structure
4. ⏭️ Migrate authentication services to WASM
5. ⏭️ Migrate UI components to WASM
6. ⏭️ Test authentication flow in WASM
7. ⏭️ Migrate Blueprint Designer components

### Affected Files

**Authentication:**
- `src/Apps/Sorcha.Admin/Services/Authentication/CustomAuthenticationStateProvider.cs`
- `src/Apps/Sorcha.Admin/Services/Authentication/AuthenticationService.cs`
- `src/Apps/Sorcha.Admin/Services/Authentication/BrowserTokenCache.cs`

**Pages & Layout:**
- `src/Apps/Sorcha.Admin/Pages/Login.razor`
- `src/Apps/Sorcha.Admin/Pages/Index.razor`
- `src/Apps/Sorcha.Admin/Layout/MainLayout.razor`
- `src/Apps/Sorcha.Admin/Components/Routes.razor`
- `src/Apps/Sorcha.Admin/Components/App.razor`

**Components:**
- `src/Apps/Sorcha.Admin/Components/Authentication/UserProfileMenu.razor`
- `src/Apps/Sorcha.Admin/Components/Authentication/ProfileSelector.razor`

**Configuration:**
- `src/Apps/Sorcha.Admin/Program.cs`

---

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

### ✅ JavaScript Interop During Prerendering (FIXED 2026-01-05)
**Problem:** Container logs showing "JavaScript interop calls cannot be issued at this time" 500 errors
**Cause:** `Index.razor` calling `JSRuntime.InvokeVoidAsync()` in `OnInitializedAsync()` during server-side prerendering
**Fix:** Moved all JavaScript interop calls from `OnInitializedAsync()` to `OnAfterRenderAsync(bool firstRender)`
**Files:** `src/Apps/Sorcha.Admin/Pages/Index.razor` (lines 462-505)
**Impact:** Container now starts cleanly without 500 errors, page renders correctly

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
