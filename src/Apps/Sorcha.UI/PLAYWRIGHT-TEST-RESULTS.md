# Playwright & Browser Testing Results - Sorcha.UI

**Created:** 2026-01-06
**Status:** Partially Successful ⚠️
**Method:** PowerShell + Screenshot

## Test Summary

### ✅ Successfully Verified

**1. Server Startup**
- ✅ Server listening on HTTPS (7083) and HTTP (5173)
- ✅ HTTP redirects to HTTPS correctly
- ✅ No server-side exceptions in logs
- ✅ HTML served without prerendering errors

**2. Layout Rendering**
- ✅ Left sidebar navigation renders
- ✅ Navigation links visible:
  - Home
  - Counter
  - Weather
  - About
- ✅ Main content area renders
- ✅ Blazor WebAssembly loads successfully

**3. Login Page Components**
- ✅ Page accessible at `/login`
- ✅ "Sign In" heading displays
- ✅ Profile dropdown renders
- ✅ Username input field renders
- ✅ Password input field renders
- ✅ Blue "Sign In" button renders
- ✅ Form layout appears correct

### ❌ Issues Discovered

**1. Client-Side Error**

**Symptom:**
- Error banner at bottom: "An unhandled error has occurred. Reload"
- Profile dropdown appears empty (no options visible)

**Likely Causes:**
1. **IJSRuntime Initialization Timing**
   - `ConfigurationService.GetProfilesAsync()` calls `IJSRuntime.InvokeAsync`
   - May be executing before Blazor WASM fully initialized

2. **LocalStorage Access**
   - First call to `localStorage.getItem('sorcha:profiles')` might fail
   - Encryption key generation might fail on first load

3. **Profile Initialization**
   - `InitializeDefaultProfilesAsync()` may not complete before rendering

**Error Location:** Likely in `Login.razor.OnInitializedAsync()`:

```csharp
protected override async Task OnInitializedAsync()
{
    _profiles = (await ConfigService.GetProfilesAsync()).ToList();  // ← Error likely here
    if (_profiles.Count == 0)
    {
        await ConfigService.InitializeDefaultProfilesAsync();
        _profiles = (await ConfigService.GetProfilesAsync()).ToList();
    }
    _selectedProfile = await ConfigService.GetActiveProfileNameAsync();
}
```

### 🔧 Fixes Implemented

**1. Disabled Server-Side Prerendering**

Added to `Login.razor` and `AuthTest.razor`:
```razor
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))
```

**Before:** Server tried to inject `CustomAuthenticationStateProvider` during prerendering
**After:** Components only render client-side in WASM

**Result:** ✅ No more server-side `InvalidOperationException`

**2. Build Configuration**

- ✅ All projects building successfully
- ✅ Blazor WASM output generated
- ✅ JavaScript files bundled correctly
- ✅ encryption.js included in bundle

## Screenshot Analysis

**File:** `C:\Projects\Sorcha\ui-screenshot.png`

**Observations:**

1. **Browser:** Microsoft Edge
2. **URL:** `https://localhost:7083/login`
3. **Certificate:** Accepted (self-signed)
4. **Layout:**
   - Bootstrap-based responsive layout
   - Left sidebar with navigation
   - Main content area centered
   - Login card appears to be ~400px wide (as designed)

5. **Form Elements:**
   - All form controls present
   - Bootstrap styling applied
   - Blue button uses primary color
   - Fields have proper spacing

6. **JavaScript Error:**
   - Red error banner at bottom
   - "Reload" link visible
   - Indicates client-side exception

## HTML Structure Verification

**Via curl:**

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <base href="/">
    <link href="_framework/dotnet.js" rel="preload" />
    <link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="app.css" />
    <script type="importmap">{ /* WASM modules */ }</script>
    <title>Home</title>
</head>
<body>
    <!-- Blazor components render here -->
    <script src="_framework/blazor.web.js"></script>
    <script src="js/encryption.js"></script>
</body>
</html>
```

**✅ Verified:**
- Blazor WASM loaded
- encryption.js included
- Bootstrap CSS loaded
- Import maps configured
- Component hydration enabled

## Playwright Browser Issues

**Problem:** Playwright (in Docker) cannot connect to `localhost` or accept self-signed certificates

**Attempts:**
1. ❌ `http://localhost:5173` → ERR_CONNECTION_REFUSED
2. ❌ `https://localhost:7083` → ERR_CONNECTION_REFUSED
3. ❌ `http://host.docker.internal:5173` → ERR_CERT_AUTHORITY_INVALID
4. ❌ `https://host.docker.internal:7083` → ERR_CERT_AUTHORITY_INVALID
5. ❌ `ignoreHTTPSErrors` option not available in MCP browser context

**Solution:** Used PowerShell + Microsoft Edge + screenshot capture

## Remaining Work

### Immediate Fixes Needed

**1. Fix Client-Side Error**

**Option A: Add Error Handling**
```csharp
protected override async Task OnInitializedAsync()
{
    try
    {
        _profiles = (await ConfigService.GetProfilesAsync()).ToList();
        if (_profiles.Count == 0)
        {
            await ConfigService.InitializeDefaultProfilesAsync();
            _profiles = (await ConfigService.GetProfilesAsync()).ToList();
        }
        _selectedProfile = await ConfigService.GetActiveProfileNameAsync();
    }
    catch (Exception ex)
    {
        _errorMessage = $"Failed to load profiles: {ex.Message}";
        Console.WriteLine($"Profile initialization error: {ex}");

        // Fallback: Create default profiles manually
        _profiles = new List<Profile>
        {
            new Profile { Name = "Development", ApiGatewayUrl = "https://localhost:7082", Description = "Local development" }
        };
        _selectedProfile = "Development";
    }
}
```

**Option B: Lazy Initialization**
```csharp
protected override async Task OnInitializedAsync()
{
    // Delay profile loading until first render
    await Task.Yield();

    _profiles = (await ConfigService.GetProfilesAsync()).ToList();
    // ... rest of code
}
```

**Option C: OnAfterRender**
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _profiles = (await ConfigService.GetProfilesAsync()).ToList();
        if (_profiles.Count == 0)
        {
            await ConfigService.InitializeDefaultProfilesAsync();
            _profiles = (await ConfigService.GetProfilesAsync()).ToList();
        }
        _selectedProfile = await ConfigService.GetActiveProfileNameAsync();
        StateHasChanged();
    }
}
```

**2. Verify Encryption.js Initialization**

Add logging to `encryption.js`:
```javascript
console.log('[Encryption] Module loaded');
window.EncryptionHelper = {
    isAvailable: function() {
        const available = typeof crypto !== 'undefined' && typeof crypto.subtle !== 'undefined';
        console.log('[Encryption] isAvailable:', available);
        return available;
    },
    // ... rest of code
};
```

**3. Test Profile Dropdown**

After fixing error, verify:
- ✅ Profile dropdown shows "Development" and "Docker"
- ✅ Selecting profile updates state
- ✅ Form submission works (with mock backend)

### Integration Testing

**When Backend Available:**

1. **Full Authentication Flow:**
   ```
   1. Navigate to /
   2. Redirect to /login
   3. Select "Development" profile
   4. Enter: admin@sorcha.local / Admin123!
   5. Click "Sign In"
   6. Verify: Token stored (encrypted)
   7. Verify: Redirect to /
   8. Navigate to /auth-test
   9. Verify: User info displayed
   10. Click "Logout"
   11. Verify: Redirect to /login
   ```

2. **Token Persistence:**
   - Login successfully
   - Refresh page (F5)
   - Verify: Still authenticated
   - Check LocalStorage: `sorcha:tokens:Development`

3. **Profile Switching:**
   - Login with Development
   - Logout
   - Login with Docker
   - Verify: Different tokens cached

## Server Logs (Clean)

```
Using launch settings from C:\Projects\Sorcha\src\Apps\Sorcha.UI\Sorcha.UI.Web\Properties\launchSettings.json...
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7083
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5173
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\Projects\Sorcha\src\Apps\Sorcha.UI\Sorcha.UI.Web
```

**✅ No exceptions, no errors**

## Curl Tests

### 1. Root Page
```bash
curl -k https://localhost:7083
```
**Result:** ✅ HTML served (Blazor layout)

### 2. Login Page
```bash
curl -k https://localhost:7083/login
```
**Result:** ✅ HTML served (no server-side exception)

### 3. HTTP Redirect
```bash
curl -I http://localhost:5173
```
**Result:** ✅ `307 Temporary Redirect → https://localhost:7083/`

## Architecture Verified

**Blazor WASM Hybrid Mode:**
- ✅ Server-side minimal rendering
- ✅ Client-side WASM components
- ✅ No server-side authentication state provider
- ✅ Client-side services injected only in WASM

**Authentication Architecture:**
- ✅ Services registered in `Sorcha.UI.Web.Client/Program.cs`
- ✅ Not registered in `Sorcha.UI.Web/Program.cs` (correct!)
- ✅ IJSRuntime available only client-side
- ✅ LocalStorage accessed only from browser

## Recommendations

### 1. Debugging Client Error

**Add to Login.razor:**
```csharp
@code {
    protected override async Task OnInitializedAsync()
    {
        Console.WriteLine("[Login] OnInitializedAsync starting");

        try
        {
            Console.WriteLine("[Login] Loading profiles...");
            _profiles = (await ConfigService.GetProfilesAsync()).ToList();
            Console.WriteLine($"[Login] Loaded {_profiles.Count} profiles");

            if (_profiles.Count == 0)
            {
                Console.WriteLine("[Login] No profiles found, initializing defaults...");
                await ConfigService.InitializeDefaultProfilesAsync();
                _profiles = (await ConfigService.GetProfilesAsync()).ToList();
                Console.WriteLine($"[Login] After init: {_profiles.Count} profiles");
            }

            _selectedProfile = await ConfigService.GetActiveProfileNameAsync();
            Console.WriteLine($"[Login] Active profile: {_selectedProfile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Login] ERROR: {ex.Message}");
            Console.WriteLine($"[Login] Stack: {ex.StackTrace}");
            _errorMessage = $"Initialization failed: {ex.Message}";
        }
    }
}
```

**Check Browser Console:**
- Open Edge DevTools (F12)
- Navigate to `/login`
- Check Console tab for `[Login]` messages
- Identify where initialization fails

### 2. Test Without Authentication

Create a simple test page that doesn't use auth services:

**SimpleTest.razor:**
```razor
@page "/simple-test"
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))
@inject IJSRuntime JS

<h3>Simple Test</h3>

<p>Blazor WASM is working: @_wasmWorking</p>
<p>IJSRuntime available: @_jsRuntimeWorking</p>
<p>LocalStorage available: @_localStorageWorking</p>

<button @onclick="TestLocalStorage">Test LocalStorage</button>
<p>@_testResult</p>

@code {
    private bool _wasmWorking = true;
    private bool _jsRuntimeWorking;
    private bool _localStorageWorking;
    private string _testResult = "";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsRuntimeWorking = true;
                await JS.InvokeVoidAsync("console.log", "IJSRuntime works!");

                await JS.InvokeVoidAsync("localStorage.setItem", "test", "value");
                var value = await JS.InvokeAsync<string>("localStorage.getItem", "test");
                _localStorageWorking = value == "value";

                StateHasChanged();
            }
            catch (Exception ex)
            {
                _testResult = $"Error: {ex.Message}";
                StateHasChanged();
            }
        }
    }

    private async Task TestLocalStorage()
    {
        try
        {
            await JS.InvokeVoidAsync("localStorage.setItem", "sorcha-test", DateTime.Now.ToString());
            var value = await JS.InvokeAsync<string>("localStorage.getItem", "sorcha-test");
            _testResult = $"✅ LocalStorage works! Value: {value}";
        }
        catch (Exception ex)
        {
            _testResult = $"❌ Error: {ex.Message}";
        }
    }
}
```

Navigate to: `https://localhost:7083/simple-test`

Expected:
- ✅ Blazor WASM is working: True
- ✅ IJSRuntime available: True
- ✅ LocalStorage available: True

### 3. Production Readiness Checklist

Before production deployment:

- [ ] Fix client-side initialization error
- [ ] Add error boundaries around async operations
- [ ] Test profile dropdown population
- [ ] Test all 7 authentication scenarios (from AUTHENTICATION-TEST-GUIDE.md)
- [ ] Verify token encryption/decryption
- [ ] Test token refresh on 401
- [ ] Test logout flow
- [ ] Test profile switching
- [ ] Replace placeholder UI with MudBlazor components
- [ ] Add proper loading indicators
- [ ] Add validation messages
- [ ] Test with real backend (Tenant Service + API Gateway)

---

**Status:** Layout and structure verified ✅
**Authentication:** Pending error fix ⏳
**Next Step:** Debug client-side profile initialization error

**Last Updated:** 2026-01-06
