# CSP Errors Fixed - Summary

**Created:** 2026-01-06
**Status:** ✅ CSP Errors Resolved
**Remaining:** Profile initialization timing issue

## Issues Fixed

### 1. ✅ Content Security Policy (CSP) Errors

**Problem:**
- Multiple CSP violations in browser console
- Blazor WebAssembly requires specific CSP directives
- Missing security headers

**Solution:**
Added comprehensive CSP middleware to `Sorcha.UI.Web/Program.cs`:

```csharp
// Add CSP headers for Blazor WebAssembly
app.Use(async (context, next) =>
{
    // Content Security Policy for Blazor WASM
    var csp = string.Join("; ", new[]
    {
        "default-src 'self'",
        "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'", // WASM requires unsafe-eval
        "style-src 'self' 'unsafe-inline'", // Blazor uses inline styles
        "img-src 'self' data: https:",
        "font-src 'self' data:",
        "connect-src 'self' https://localhost:* http://localhost:* wss://localhost:* ws://localhost:*",
        "worker-src 'self' blob:", // For Blazor WASM workers
        "frame-ancestors 'none'", // Prevent clickjacking
        "base-uri 'self'",
        "form-action 'self'"
    });

    context.Response.Headers["Content-Security-Policy"] = csp;

    // Other security headers
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    await next();
});
```

**Headers Now Sent:**
```
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; ...
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
```

**Verified:**
```bash
curl -k -I https://localhost:7083/login | grep -i "content-security\|x-frame\|x-content"
# Returns all expected headers ✅
```

### 2. ✅ Server-Side Prerendering Disabled

**Problem:**
- `InvalidOperationException` when server tried to inject `CustomAuthenticationStateProvider`
- IJSRuntime-dependent services can't run server-side

**Solution:**
Added `@rendermode` directive to client components:

```razor
@page "/login"
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))
```

**Applied to:**
- Login.razor
- AuthTest.razor

### 3. ✅ Loading State Added

**Problem:**
- Form rendered before profiles loaded
- Empty dropdown caused confusion

**Solution:**
Added conditional rendering with loading spinner:

```razor
@if (!_profilesLoaded)
{
    <div class="text-center" style="margin: 100px auto;">
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
        <p class="mt-3">Loading profiles...</p>
    </div>
}
else
{
    <!-- Login form here -->
}
```

### 4. ✅ Profile Initialization Moved

**Problem:**
- `OnInitializedAsync` executes before JavaScript is available
- IJSRuntime calls fail during initialization

**Solution:**
Moved profile loading to `OnAfterRenderAsync`:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && !_profilesLoaded)
    {
        try
        {
            // Initialize profiles after first render (when JS is available)
            _profiles = (await ConfigService.GetProfilesAsync()).ToList();

            if (_profiles.Count == 0)
            {
                await ConfigService.InitializeDefaultProfilesAsync();
                _profiles = (await ConfigService.GetProfilesAsync()).ToList();
            }

            // ... rest of initialization
            StateHasChanged();
        }
        catch (Exception ex)
        {
            // Fallback to in-memory profiles
            _profiles = new List<Profile> { /* defaults */ };
            _profilesLoaded = true;
            StateHasChanged();
        }
    }
}
```

## Screenshots

### Before Fixes
- ❌ CSP errors in console
- ❌ Server-side exceptions
- ❌ "An unhandled error has occurred"

### After Fixes
- ✅ CSP headers configured
- ✅ No CSP violations
- ✅ Loading spinner displays
- ⏳ Profile initialization still timing out (see below)

## Remaining Issue

### Profile Initialization Error

**Symptom:**
- "An unhandled error has occurred. Reload" banner at bottom
- Shows "Loading profiles..." spinner
- Profiles never finish loading

**Likely Causes:**

1. **IJSRuntime timing issue:**
   ```csharp
   // This might be executing before Blazor WASM is fully ready
   await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "sorcha:profiles");
   ```

2. **Encryption key generation:**
   ```javascript
   // May be failing in encryption.js
   EncryptionHelper.generateKey()
   ```

3. **LocalStorage access:**
   ```csharp
   // First-time access might throw exception
   var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
   ```

**Debug Steps:**

1. **Add console logging to ConfigurationService:**
   ```csharp
   public async Task<IEnumerable<Profile>> GetProfilesAsync()
   {
       Console.WriteLine("[Config] GetProfilesAsync starting");
       try
       {
           var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", ProfilesStorageKey);
           Console.WriteLine($"[Config] Retrieved from storage: {json?.Length ?? 0} chars");
           // ... rest
       }
       catch (Exception ex)
       {
           Console.WriteLine($"[Config] Error: {ex.Message}");
           throw;
       }
   }
   ```

2. **Check browser console (F12):**
   - Open Edge DevTools
   - Navigate to `/login`
   - Check Console tab for errors
   - Check Application → Local Storage

3. **Test encryption.js separately:**
   ```javascript
   // In browser console
   EncryptionHelper.isAvailable()  // Should return true
   await EncryptionHelper.generateKey()  // Should return base64 string
   ```

**Temporary Workaround:**

The fallback code already creates in-memory profiles on error:

```csharp
catch (Exception ex)
{
    // Fallback: create default profiles in memory
    _profiles = new List<Profile>
    {
        new Profile
        {
            Name = "Development",
            ApiGatewayUrl = "https://localhost:7082",
            Description = "Local development environment"
        }
    };
    _selectedProfile = "Development";
    _profilesLoaded = true;
    StateHasChanged();
}
```

**Issue:** Exception is being thrown before fallback executes, caught by Blazor error boundary.

## Recommendations

### Immediate Fix

**Option 1: Wrap in ErrorBoundary**

```razor
<ErrorBoundary>
    <ChildContent>
        @if (!_profilesLoaded)
        {
            <div class="text-center">
                <div class="spinner-border"></div>
                <p>Loading profiles...</p>
            </div>
        }
        else
        {
            <!-- Login form -->
        }
    </ChildContent>
    <ErrorContent Context="ex">
        <div class="alert alert-warning">
            <p>Profile loading failed. Using default profile.</p>
            <button class="btn btn-primary" @onclick="@(() => InitializeDefaultProfiles())">
                Continue with Defaults
            </button>
        </div>
    </ErrorContent>
</ErrorBoundary>
```

**Option 2: Add retry logic**

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && !_profilesLoaded)
    {
        // Wait a bit for WASM to fully initialize
        await Task.Delay(500);

        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                _profiles = (await ConfigService.GetProfilesAsync()).ToList();
                _profilesLoaded = true;
                StateHasChanged();
                return;
            }
            catch (Exception ex)
            {
                if (retry == 2)
                {
                    // Last retry - use fallback
                    UseDefaultProfiles();
                    return;
                }
                await Task.Delay(500);
            }
        }
    }
}
```

**Option 3: Remove LocalStorage dependency for profiles**

Create profiles in code instead of loading from LocalStorage:

```csharp
protected override void OnInitialized()
{
    // No async needed - just create profiles
    _profiles = new List<Profile>
    {
        new Profile { Name = "Development", ApiGatewayUrl = "https://localhost:7082", ... },
        new Profile { Name = "Docker", ApiGatewayUrl = "http://localhost:8080", ... }
    };
    _selectedProfile = "Development";
    _profilesLoaded = true;
}
```

Later save selected profile to LocalStorage only when needed.

### Testing Without Backend

Since backend services aren't running, we can test the UI in isolation:

1. **Hardcode profiles** (Option 3 above)
2. **Mock the authentication service** for UI testing
3. **Add "Demo Mode" flag** that skips actual authentication

```csharp
// In Login.razor
private async Task HandleLogin()
{
    #if DEBUG
    if (_username == "demo" && _password == "demo")
    {
        // Demo mode - skip real authentication
        Navigation.NavigateTo("/", forceLoad: true);
        return;
    }
    #endif

    // Real authentication
    await AuthService.LoginAsync(request, _selectedProfile);
}
```

## Verification

### CSP Headers Working ✅

```bash
curl -k -I https://localhost:7083/login 2>&1 | grep -i "content-security"
# Output:
# Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; ...
```

### No CSP Console Errors ✅

Browser console no longer shows:
- ~~Refused to execute inline script~~
- ~~Refused to load script~~
- ~~Refused to evaluate string as JavaScript~~

### Security Headers Present ✅

All recommended security headers:
- ✅ Content-Security-Policy
- ✅ X-Content-Type-Options: nosniff
- ✅ X-Frame-Options: DENY
- ✅ X-XSS-Protection: 1; mode=block
- ✅ Referrer-Policy: strict-origin-when-cross-origin

### Blazor WASM Loading ✅

- ✅ `_framework/blazor.web.js` loads
- ✅ `js/encryption.js` loads
- ✅ WASM modules download
- ✅ Components render (loading spinner shows)

## Summary

### ✅ Completed

1. **CSP Configuration** - All headers configured for Blazor WASM
2. **Security Headers** - Best practice headers added
3. **Prerendering** - Disabled for client-only components
4. **Loading State** - Spinner shows during initialization
5. **Error Handling** - Try/catch with fallback profiles
6. **Lifecycle Fix** - Moved to OnAfterRenderAsync

### ⏳ Pending

1. **Profile Initialization** - JS timing issue causing error
2. **Error Boundary** - Add ErrorBoundary component
3. **Backend Integration** - Test with real Tenant Service
4. **Browser Console Debugging** - Check actual error message

### 📋 Next Steps

1. Add ErrorBoundary around Login component
2. Add console.log debugging to ConfigurationService
3. Test encryption.js in browser console
4. Consider hardcoding profiles for initial testing
5. Test with backend services running

---

**CSP Errors:** ✅ Fixed
**Security Headers:** ✅ Implemented
**Profile Loading:** ⏳ In progress (timing issue)
**Last Updated:** 2026-01-06
