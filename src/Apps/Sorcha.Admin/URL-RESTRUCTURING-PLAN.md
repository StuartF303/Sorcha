# Sorcha Admin UI - URL Restructuring Plan

**Version:** 1.0
**Date:** 2026-01-03
**Status:** Planning

## Overview

Restructure the Sorcha Admin UI URL architecture to support multiple application sections from a single deployment, with proper separation between anonymous homepage, authenticated admin pages, and designer pages.

---

## Current State

### URL Structure
- **Base Href:** `/admin/`
- **Homepage:** `/admin/` (Index.razor)
- **Login:** `/admin/login` (Login.razor)
- **Admin Pages:** `/admin/*`
- **Designer Pages:** `/admin/designer/*`
- **API Routes:** `/api/*` (pass-through to services)

### API Gateway Routing
```json
"admin-route": {
  "ClusterId": "admin-cluster",
  "Match": { "Path": "/admin/{**catch-all}" },
  "Transforms": [{ "PathPattern": "/{**catch-all}" }]
}
```

### Issues
1. All pages are under `/admin/` prefix, but homepage should be at root
2. Homepage link to login uses `/admin/login` (should be `/login`)
3. Login page shows a "Sign In" button that opens a modal dialog, but auto-show may not be working
4. No clear separation between admin section and designer section
5. Base href causes all relative links to go under `/admin/`

---

## Desired State

### URL Structure

| Path | Purpose | Access | Render Mode |
|------|---------|--------|-------------|
| `/` | Installation homepage (anonymous) | Public | Server |
| `/login` | Login page with credentials form | Public | Server |
| `/admin/*` | Admin pages (users, tenants, services) | Authenticated (Admin) | Server |
| `/design/*` | Designer pages (blueprint editor) | Authenticated (Designer) | WASM |
| `/api/*` | Backend API pass-through | Varies | N/A |

### Example URLs
- Homepage: `http://localhost:5110/`
- Login: `http://localhost:5110/login`
- Admin Dashboard: `http://localhost:5110/admin/dashboard`
- Service Management: `http://localhost:5110/admin/services`
- User Management: `http://localhost:5110/admin/users`
- Blueprint Designer: `http://localhost:5110/design/editor`
- Schema Library: `http://localhost:5110/design/schemas`

---

## Changes Required

### 1. API Gateway Configuration

**File:** `src/Services/Sorcha.ApiGateway/appsettings.json`

**Changes:**
1. Update `admin-route` to match root path and all UI paths
2. Keep `/api/*` routes as-is for backend services
3. Ensure proper priority/ordering of routes

**New Configuration:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "admin-ui-root": {
        "ClusterId": "admin-cluster",
        "Match": { "Path": "/" },
        "Transforms": [{ "PathPattern": "/" }]
      },
      "admin-ui-login": {
        "ClusterId": "admin-cluster",
        "Match": { "Path": "/login" },
        "Transforms": [{ "PathPattern": "/login" }]
      },
      "admin-ui-admin": {
        "ClusterId": "admin-cluster",
        "Match": { "Path": "/admin/{**catch-all}" },
        "Transforms": [{ "PathPattern": "/admin/{**catch-all}" }]
      },
      "admin-ui-design": {
        "ClusterId": "admin-cluster",
        "Match": { "Path": "/design/{**catch-all}" },
        "Transforms": [{ "PathPattern": "/design/{**catch-all}" }]
      },
      "admin-ui-static": {
        "ClusterId": "admin-cluster",
        "Match": { "Path": "/_framework/{**catch-all}" },
        "Transforms": [{ "PathPattern": "/_framework/{**catch-all}" }]
      },
      "admin-ui-static-content": {
        "ClusterId": "admin-cluster",
        "Match": { "Path": "/_content/{**catch-all}" },
        "Transforms": [{ "PathPattern": "/_content/{**catch-all}" }]
      },
      // ... existing API routes remain unchanged ...
    },
    "Clusters": {
      "admin-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://sorcha-admin:8080"
          }
        }
      }
      // ... other clusters unchanged ...
    }
  }
}
```

**Route Priority Order:**
1. Exact matches first: `/`, `/login`
2. Static assets: `/_framework/*`, `/_content/*`
3. Path prefixes: `/admin/*`, `/design/*`
4. API routes: `/api/*`

---

### 2. Admin UI Base Path

**File:** `src/Apps/Sorcha.Admin/Components/App.razor`

**Current:**
```html
<base href="/admin/" />
```

**New:**
```html
<base href="/" />
```

**Impact:** All relative URLs will now resolve from root, not `/admin/`

---

### 3. Admin UI Routing

**File:** `src/Apps/Sorcha.Admin/Program.cs`

**Changes:**
1. Remove `UsePathBase` (if present) - we're handling full paths now
2. Ensure authentication middleware is configured
3. Ensure static assets are mapped

**No code changes needed** - already configured correctly:
```csharp
app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sorcha.Admin.Client._Imports).Assembly);
```

---

### 4. Page Routes

**Files to Update:**

#### Index.razor (Homepage)
**Current:** `@page "/"`
**New:** `@page "/"`
**Change:** Update login link from `/admin/login` to `/login`

**Line 53:**
```html
<!-- BEFORE -->
<MudButton Href="/admin/login" ... >

<!-- AFTER -->
<MudButton Href="/login" ... >
```

#### Login.razor
**Current:** `@page "/login"`
**New:** `@page "/login"`
**Change:** Consider removing the intermediate "Sign In" button page and showing the LoginDialog directly

**Options:**
1. **Option A (Recommended):** Embed the LoginDialog form directly in the page
2. **Option B:** Keep auto-show dialog, but investigate why it's not working
3. **Option C:** Make the "Sign In" button functional and keep the modal approach

#### Admin Pages
Create new admin section pages under `/admin/*`:
- `/admin/dashboard` - Admin dashboard
- `/admin/users` - User management
- `/admin/services` - Service status
- `/admin/tenants` - Tenant management
- `/admin/audit` - Audit logs

#### Designer Pages
Move designer pages to `/design/*`:
- `/design/editor` - Blueprint designer (currently `/designer`)
- `/design/schemas` - Schema library (currently `/schemas`)
- `/design/templates` - Template library (currently `/templates`)

---

### 5. Navigation Links

**Files to Update:**

| File | Current Link | New Link |
|------|-------------|----------|
| Index.razor | `/admin/login` | `/login` |
| Index.razor | `/designer` | `/design/editor` |
| Index.razor | `/schemas` | `/design/schemas` |
| Index.razor | `/templates` | `/design/templates` |
| Index.razor | `/admin/services` | `/admin/services` ✓ |
| Index.razor | `/admin/users` | `/admin/users` ✓ |
| Index.razor | `/admin/audit` | `/admin/audit` ✓ |
| UserProfileMenu.razor | `login` | `/login` |
| RedirectToLogin.razor | `login` | `/login` |
| MainLayout.razor | TBD | TBD |

---

### 6. Login Page Fix

**Problem:** Login page shows a "Sign In" button that should trigger the LoginDialog, but it may not be working.

**File:** `src/Apps/Sorcha.Admin/Pages/Login.razor`

**Current Behavior:**
1. Page renders with a "Sign In" button
2. `OnAfterRenderAsync` auto-shows the LoginDialog
3. User sees button first, then dialog appears (if it works)

**Recommended Fix - Option A: Inline Login Form**

Remove the intermediate button page and embed the login form directly:

```razor
@page "/login"
@using Sorcha.Admin.Services.Authentication
@using Sorcha.Admin.Services.Configuration
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@attribute [AllowAnonymous]
@inject IAuthenticationService AuthService
@inject CustomAuthenticationStateProvider AuthStateProvider
@inject IConfigurationService ConfigService
@inject NavigationManager Navigation
@inject ISnackbar Snackbar

<PageTitle>Login - Sorcha Admin</PageTitle>

<AuthorizeView>
    <Authorized>
        @{
            // Already authenticated - redirect to home
            Navigation.NavigateTo("/", forceLoad: true);
        }
    </Authorized>
    <NotAuthorized>
        <div class="d-flex flex-column align-center justify-center" style="min-height: 100vh;">
            <MudPaper Elevation="4" Class="pa-8" Style="max-width: 500px; width: 100%;">
                <div class="d-flex flex-column align-center mb-6">
                    <MudIcon Icon="@Icons.Material.Filled.AdminPanelSettings" Size="Size.Large" Color="Color.Primary" Class="mb-4" Style="font-size: 4rem;" />
                    <MudText Typo="Typo.h4" Align="Align.Center" Class="mb-2">Sorcha Admin</MudText>
                    <MudText Typo="Typo.body2" Align="Align.Center" Color="Color.Secondary">
                        Blueprint Designer & Administration Portal
                    </MudText>
                </div>

                <!-- INLINE LOGIN FORM -->
                <EditForm Model="@_loginModel" OnValidSubmit="HandleLogin">
                    <DataAnnotationsValidator />

                    <!-- Profile Selector -->
                    <MudSelect T="string" @bind-Value="_selectedProfile" Label="Profile" Variant="Variant.Outlined" Class="mb-4">
                        @foreach (var profile in _profiles)
                        {
                            <MudSelectItem T="string" Value="@profile.Key">@profile.Key</MudSelectItem>
                        }
                    </MudSelect>

                    <!-- Username -->
                    <MudTextField @bind-Value="_loginModel.Username"
                                  Label="Username or Email"
                                  Variant="Variant.Outlined"
                                  Required="true"
                                  Class="mb-4" />

                    <!-- Password -->
                    <MudTextField @bind-Value="_loginModel.Password"
                                  Label="Password"
                                  Variant="Variant.Outlined"
                                  InputType="InputType.Password"
                                  Required="true"
                                  Class="mb-4" />

                    <!-- Error Message -->
                    @if (!string.IsNullOrEmpty(_errorMessage))
                    {
                        <MudAlert Severity="Severity.Error" Class="mb-4">@_errorMessage</MudAlert>
                    }

                    <!-- Login Button -->
                    <MudButton ButtonType="ButtonType.Submit"
                               Variant="Variant.Filled"
                               Color="Color.Primary"
                               FullWidth="true"
                               Size="Size.Large"
                               StartIcon="@Icons.Material.Filled.Login"
                               Disabled="_isLoading">
                        @if (_isLoading)
                        {
                            <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                            <span class="ml-2">Signing In...</span>
                        }
                        else
                        {
                            <span>Sign In</span>
                        }
                    </MudButton>
                </EditForm>

                <MudDivider Class="my-6" />

                <MudText Typo="Typo.caption" Align="Align.Center" Color="Color.Secondary">
                    Secure authentication powered by Sorcha Tenant Service
                </MudText>
            </MudPaper>
        </div>
    </NotAuthorized>
</AuthorizeView>

@code {
    private LoginModel _loginModel = new();
    private Dictionary<string, Profile> _profiles = new();
    private string _selectedProfile = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadProfiles();
        }
    }

    private async Task LoadProfiles()
    {
        try
        {
            var config = await ConfigService.GetConfigurationAsync();
            _profiles = config.Profiles;
            _selectedProfile = config.ActiveProfile ?? ProfileDefaults.DefaultActiveProfile;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading profiles: {ex.Message}", Severity.Error);
        }
    }

    private async Task HandleLogin()
    {
        _isLoading = true;
        _errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var result = await AuthService.LoginAsync(
                _selectedProfile,
                _loginModel.Username,
                _loginModel.Password
            );

            if (result.Success)
            {
                AuthStateProvider.NotifyAuthenticationStateChanged();
                Snackbar.Add($"Welcome, {_loginModel.Username}!", Severity.Success);
                Navigation.NavigateTo("/", forceLoad: true);
            }
            else
            {
                _errorMessage = result.ErrorMessage ?? "Invalid username or password";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Login failed: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
```

**Benefits:**
- Direct credential entry without modal dialog
- Better UX - users see what they need immediately
- Simpler code - no dialog management
- Works with base href `/`

---

### 7. Update Navigation Components

**MainLayout.razor** - Update any hardcoded links to use new paths:
- Admin links: `/admin/*`
- Designer links: `/design/*`
- Login link: `/login`

**UserProfileMenu.razor** - Update logout redirect:
```csharp
// FROM:
Navigation.NavigateTo("login", forceLoad: true);

// TO:
Navigation.NavigateTo("/login", forceLoad: true);
```

**RedirectToLogin.razor** - Update redirect path:
```csharp
// FROM:
Navigation.NavigateTo("login", forceLoad: true);

// TO:
Navigation.NavigateTo("/login", forceLoad: true);
```

---

## Implementation Plan

### Phase 1: API Gateway & Base Path (Foundation)
1. ✅ Update API Gateway routes in appsettings.json
2. ✅ Change base href from `/admin/` to `/` in App.razor
3. ✅ Test that homepage loads at `/`

### Phase 2: Login Page Fix
4. ✅ Update Login.razor with inline form (Option A)
5. ✅ Update navigation links to `/login`
6. ✅ Test login flow from homepage

### Phase 3: Navigation Updates
7. ✅ Update Index.razor links
8. ✅ Update UserProfileMenu.razor redirect
9. ✅ Update RedirectToLogin.razor redirect
10. ✅ Update MainLayout.razor links (if any)

### Phase 4: Page Organization (Future)
11. ⏳ Create dedicated admin pages under `/admin/*`
12. ⏳ Move designer pages to `/design/*`
13. ⏳ Update all internal navigation

### Phase 5: Testing
14. ✅ Test anonymous access to `/`
15. ✅ Test login at `/login`
16. ✅ Test authenticated access to `/admin/*`
17. ✅ Test designer access to `/design/*`
18. ✅ Test API pass-through `/api/*`
19. ✅ Test static assets `/_framework/*`, `/_content/*`

---

## Testing Checklist

### Anonymous User (Not Logged In)
- [ ] Can access `/` (homepage)
- [ ] Can access `/login` (login page)
- [ ] Cannot access `/admin/*` (redirects to `/login`)
- [ ] Cannot access `/design/*` (redirects to `/login`)

### Authenticated User (Admin Role)
- [ ] Can access `/` (homepage)
- [ ] Can access `/admin/*` (admin pages)
- [ ] Can access `/design/*` (designer pages)
- [ ] Can logout and return to `/login`

### API Pass-Through
- [ ] `/api/blueprint/*` routes to Blueprint Service
- [ ] `/api/wallet/*` routes to Wallet Service
- [ ] `/api/register/*` routes to Register Service
- [ ] `/api/tenant/*` routes to Tenant Service
- [ ] `/api/auth/*` routes to Tenant Service

### Static Assets
- [ ] `/_framework/blazor.web.js` loads
- [ ] `/_content/MudBlazor/MudBlazor.min.css` loads
- [ ] All Blazor framework files load correctly

---

## Rollback Plan

If issues arise during implementation:

1. **Revert API Gateway:**
   ```json
   "admin-route": {
     "ClusterId": "admin-cluster",
     "Match": { "Path": "/admin/{**catch-all}" },
     "Transforms": [{ "PathPattern": "/{**catch-all}" }]
   }
   ```

2. **Revert Base Href:**
   ```html
   <base href="/admin/" />
   ```

3. **Revert Navigation Links:**
   - Change `/login` back to `login` (relative)
   - Change `/admin/login` back to `/admin/login`

4. **Rebuild and Redeploy:**
   ```bash
   docker-compose build sorcha-admin
   docker-compose up -d
   ```

---

## Success Criteria

- ✅ Anonymous users can access the homepage at `/`
- ✅ Login page shows credential form at `/login`
- ✅ Login works and redirects to homepage
- ✅ Admin pages accessible at `/admin/*` for authenticated admins
- ✅ Designer pages accessible at `/design/*` for authenticated designers
- ✅ API routes continue to work at `/api/*`
- ✅ Static assets load correctly
- ✅ No broken links or 404 errors

---

## Notes

- This restructuring maintains the single-deployment model while providing logical URL separation
- The Admin UI serves all pages: homepage, login, admin, and designer
- API Gateway routes all UI paths to the same Admin UI service
- API Gateway routes all `/api/*` paths to backend services
- Base href `/` means all relative links resolve from root
- Absolute links (starting with `/`) are recommended for clarity

