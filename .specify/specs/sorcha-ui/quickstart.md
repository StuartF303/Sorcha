# Quickstart Guide: Sorcha.UI Development

**Service**: Sorcha.UI | **Date**: 2026-01-06
**Purpose**: Get Sorcha.UI running locally for development in <15 minutes

---

## Prerequisites

### Required Software

| Tool | Version | Purpose | Download |
|------|---------|---------|----------|
| **.NET SDK** | 10.0 or later | Build and run Blazor WASM | https://dot.net/download |
| **Visual Studio 2022** | 17.12+ or VS Code | IDE (optional but recommended) | https://visualstudio.com |
| **Node.js** | 20.x LTS | Frontend tooling (optional for E2E tests) | https://nodejs.org |
| **Docker Desktop** | Latest | Run backend services | https://docker.com/products/docker-desktop |

### Verification

```bash
# Verify .NET SDK
dotnet --version
# Expected: 10.0.x or higher

# Verify Docker
docker --version
# Expected: Docker version 24.x.x or higher

# Verify Node.js (optional)
node --version
# Expected: v20.x.x
```

---

## Quick Start (15 Minutes)

### Step 1: Clone Repository (2 min)

```bash
# Clone Sorcha repository
git clone https://github.com/your-org/sorcha.git
cd sorcha

# Verify you're in the correct directory
ls src/Apps/Sorcha.UI
# Expected: Sorcha.UI.sln and project directories
```

### Step 2: Start Backend Services (5 min)

Sorcha.UI requires backend services for authentication and data. Use Docker Compose to start them:

```bash
# Start all backend services
docker-compose up -d

# Verify services are running
docker-compose ps
# Expected: api-gateway, sorcha-tenant, sorcha-blueprint, sorcha-register all "Up"

# Wait for services to be healthy (~2 minutes)
docker-compose logs -f api-gateway
# Look for: "Now listening on: http://[::]:8080"
```

**Backend Services Started**:
- API Gateway: http://localhost:8080
- Tenant Service: Internal (gRPC)
- Blueprint Service: Internal (gRPC)
- Register Service: Internal (gRPC)

### Step 3: Build Sorcha.UI (3 min)

```bash
# Navigate to Sorcha.UI directory
cd src/Apps/Sorcha.UI

# Restore NuGet packages
dotnet restore

# Build solution
dotnet build

# Expected output: "Build succeeded. 0 Warning(s). 0 Error(s)"
```

### Step 4: Run Sorcha.UI (2 min)

```bash
# Run the web host (ASP.NET Core + Blazor WASM)
cd Sorcha.UI.Web
dotnet run

# Expected output:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:7083
#       Now listening on: http://localhost:5173
```

**Application URLs**:
- **HTTPS**: https://localhost:7083
- **HTTP**: http://localhost:5173
- **Blazor Dev Server**: https://localhost:7083/_framework/blazor.web.js

### Step 5: Open Browser (1 min)

1. Open browser: https://localhost:7083
2. You should see the Sorcha.UI home page
3. Click "Sign In" to navigate to login page

### Step 6: Login (2 min)

**Default Test Credentials** (created by Docker Compose):

| Username | Password | Role | Profile |
|----------|----------|------|---------|
| `admin@sorcha.local` | `Admin123!` | Administrator | Docker |
| `designer@sorcha.local` | `Designer123!` | Designer | Docker |
| `viewer@sorcha.local` | `Viewer123!` | Viewer | Docker |

**Login Steps**:
1. Navigate to https://localhost:7083/login
2. Enter username: `admin@sorcha.local`
3. Enter password: `Admin123!`
4. Click "Login"
5. ✅ You should be redirected to `/` with authenticated menu

---

## Development Workflow

### Running with Hot Reload

```bash
# Terminal 1: Run backend services
docker-compose up

# Terminal 2: Run Sorcha.UI with hot reload
cd src/Apps/Sorcha.UI/Sorcha.UI.Web
dotnet watch run
```

**Hot Reload Features**:
- ✅ Razor file changes reload instantly
- ✅ C# code changes rebuild automatically
- ✅ CSS changes apply without restart
- ⚠️ Project file changes require manual restart

### Visual Studio 2022

1. Open `src/Apps/Sorcha.UI/Sorcha.UI.sln`
2. Set `Sorcha.UI.Web` as startup project
3. Press **F5** (or **Ctrl+F5** for no debugging)
4. Browser opens automatically at https://localhost:7083

**Debugging**:
- ✅ Breakpoints work in Blazor components (server-side only)
- ✅ Breakpoints work in services, models
- ⚠️ WASM debugging requires browser DevTools (not Visual Studio)

### VS Code

1. Install C# Dev Kit extension
2. Open folder: `src/Apps/Sorcha.UI`
3. Press **F5** to run with debugging
4. Select `.NET Core Launch (web)` configuration

---

## Project Structure Tour

```
src/Apps/Sorcha.UI/
│
├── Sorcha.UI.Web/                   # ← START HERE (ASP.NET Core host)
│   ├── Program.cs                   # Server entry point, cookie auth config
│   ├── Components/
│   │   ├── App.razor               # Root component, PersistentComponentState
│   │   └── Routes.razor            # Routing configuration
│   └── Properties/
│       └── launchSettings.json     # Development server ports
│
├── Sorcha.UI.Web.Client/           # Blazor WASM entry point
│   ├── Program.cs                  # WASM bootstrap, JWT auth config
│   ├── Pages/
│   │   ├── Home.razor             # Landing page (anonymous)
│   │   └── Login.razor            # Login form (server-rendered)
│   └── wwwroot/
│       ├── index.html             # WASM bootstrap HTML
│       └── js/
│           └── encryption.js      # Web Crypto API wrapper
│
├── Sorcha.UI.Shared/               # Shared Razor components
│   ├── Layout/
│   │   ├── MainLayout.razor       # App shell (nav, header, footer)
│   │   └── NavMenu.razor          # Navigation sidebar
│   └── Components/
│       └── UserProfileMenu.razor  # User dropdown menu
│
├── Sorcha.UI.Admin/                # Admin module (lazy-loaded)
│   └── Pages/
│       └── Index.razor            # /admin dashboard
│
├── Sorcha.UI.Designer/             # Designer module (lazy-loaded)
│   └── Pages/
│       └── Index.razor            # /designer dashboard
│
├── Sorcha.UI.Explorer/             # Explorer module (lazy-loaded)
│   └── Pages/
│       └── Index.razor            # /explorer dashboard
│
└── Sorcha.UI.Core/                 # Common library
    ├── Models/                    # Domain models (authentication, config)
    ├── Services/                  # Business services
    │   ├── Authentication/
    │   │   ├── AuthenticationService.cs
    │   │   ├── BrowserTokenCache.cs
    │   │   └── CustomAuthenticationStateProvider.cs
    │   └── Configuration/
    │       └── ConfigurationService.cs
    └── Extensions/
        └── ServiceCollectionExtensions.cs
```

---

## Common Development Tasks

### 1. Add a New Page

**Example**: Add "Settings" page to Admin module

```bash
# Create new Razor file
cd src/Apps/Sorcha.UI/Sorcha.UI.Admin/Pages
touch Settings.razor
```

```razor
@page "/admin/settings"
@attribute [Authorize(Roles = "Administrator")]
@rendermode @(new InteractiveWebAssemblyRenderMode())

<PageTitle>Settings - Sorcha Admin</PageTitle>

<MudText Typo="Typo.h4">Settings</MudText>

@code {
    protected override async Task OnInitializedAsync()
    {
        // Initialization logic
    }
}
```

**Add to Navigation** (`Sorcha.UI.Shared/Layout/NavMenu.razor`):

```razor
<AuthorizeView Roles="Administrator">
    <MudNavLink Href="/admin/settings" Icon="@Icons.Material.Filled.Settings">
        Settings
    </MudNavLink>
</AuthorizeView>
```

### 2. Call Backend API

**Example**: Fetch blueprints from Blueprint Service

```csharp
@inject HttpClient Http

<MudDataGrid T="Blueprint" Items="@_blueprints" Loading="@_loading">
    <!-- Grid columns -->
</MudDataGrid>

@code {
    private List<Blueprint> _blueprints = new();
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _loading = true;
            var response = await Http.GetFromJsonAsync<List<Blueprint>>("/api/blueprints");
            _blueprints = response ?? new();
        }
        catch (HttpRequestException ex)
        {
            // Handle error
            Snackbar.Add($"Failed to load blueprints: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }
}
```

**Note**: `HttpClient` automatically includes JWT Bearer token via `AuthenticatedHttpMessageHandler`.

### 3. Run Tests

```bash
# Run all unit tests
dotnet test

# Run specific test project
dotnet test tests/Sorcha.UI.Core.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Watch mode (auto-rerun on changes)
dotnet watch test --project tests/Sorcha.UI.Core.Tests
```

### 4. Add NuGet Package

```bash
# Example: Add FluentValidation to Core library
cd src/Apps/Sorcha.UI/Sorcha.UI.Core
dotnet add package FluentValidation --version 11.10.0

# Restore packages
dotnet restore
```

---

## Configuration

### Development Profiles

**File**: `Sorcha.UI.Core/Models/Configuration/ProfileDefaults.cs`

Default profiles are created on first run:

| Profile | API Gateway URL | Use Case |
|---------|----------------|----------|
| **Development** | `https://localhost:7082` | Local .NET Aspire dev environment |
| **Docker** | `http://localhost:8080` | Docker Compose backend services |

**Switching Profiles**:
1. Click user menu (top-right)
2. Select "Switch Profile"
3. Choose "Docker"
4. Confirm logout (required)
5. Login again with Docker profile credentials

### App Settings

**File**: `Sorcha.UI.Web/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiGateway": {
    "BaseUrl": "https://localhost:7082"
  },
  "Authentication": {
    "TokenRefreshThresholdMinutes": 5,
    "MaxTokenSizeKB": 32
  }
}
```

---

## Troubleshooting

### Problem: "Cannot connect to API Gateway"

**Symptoms**: Login fails with "Connection refused" error

**Solution**:
```bash
# Check backend services are running
docker-compose ps

# Restart services if needed
docker-compose restart

# Check API Gateway health
curl http://localhost:8080/health
# Expected: {"status":"Healthy"}
```

---

### Problem: "Encryption error - Web Crypto API not available"

**Symptoms**: Browser console shows "crypto.subtle is undefined"

**Cause**: Accessing app over HTTP (non-localhost)

**Solution**:
1. Use HTTPS: https://localhost:7083 (recommended)
2. OR use localhost: http://localhost:5173 (dev only)
3. OR configure HTTPS in `launchSettings.json`

---

### Problem: "Authentication state not displaying after login"

**Symptoms**: Login succeeds, but UI still shows "Login" button

**Cause**: Blazor Server circuit isolation bug (should not occur in WASM)

**Solution** (if using old Sorcha.Admin):
- ✅ Migrate to Sorcha.UI (WASM) - this replaces the buggy Blazor Server implementation
- ⚠️ If still using Sorcha.Admin, follow migration guide

**Verification**:
```bash
# Check browser console for logs
# Expected: "✓ Authentication state recovered from PersistentComponentState"
```

---

### Problem: "Module fails to lazy load"

**Symptoms**: 404 error when navigating to `/admin`, `/designer`, or `/explorer`

**Solution**:
```bash
# Rebuild solution
cd src/Apps/Sorcha.UI
dotnet build

# Clear browser cache (Ctrl+Shift+Delete)
# Hard refresh (Ctrl+F5)
```

---

### Problem: "Build fails with 'duplicate reference' error"

**Cause**: Project references wrong assembly

**Solution**:
1. Check `.csproj` files for duplicate `<ProjectReference>`
2. Run `dotnet restore --force`
3. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

---

## Next Steps

### For Frontend Developers

1. ✅ **Learn MudBlazor**: https://mudblazor.com/getting-started/usage
2. ✅ **Blazor WASM Docs**: https://learn.microsoft.com/aspnet/core/blazor/
3. ✅ **Z.Blazor.Diagrams**: https://github.com/zHaytam/Blazor.Diagrams (for Designer module)

### For Backend Developers

1. ✅ **Sorcha Architecture**: Read `.specify/spec.md`
2. ✅ **Backend Services**: Read service specifications in `.specify/specs/`
3. ✅ **API Gateway**: Review YARP configuration in `src/Apps/Sorcha.ApiGateway/`

### For QA/Testers

1. ✅ **Test Credentials**: See "Login" section above
2. ✅ **E2E Tests**: Run Playwright tests in `tests/Sorcha.UI.Integration.Tests/E2E/`
3. ✅ **Test Data**: Seed data created by `docker-compose up` (see `docker-compose.yml`)

---

## Resources

### Documentation

- **Specification**: `.specify/specs/sorcha-ui.md`
- **Implementation Plan**: `.specify/specs/sorcha-ui/plan.md`
- **Data Model**: `.specify/specs/sorcha-ui/data-model.md`
- **API Contracts**: `.specify/specs/sorcha-ui/contracts/`

### External Links

- **Blazor Documentation**: https://learn.microsoft.com/aspnet/core/blazor/
- **MudBlazor Components**: https://mudblazor.com/components/
- **.NET Aspire**: https://learn.microsoft.com/dotnet/aspire/
- **YARP (API Gateway)**: https://microsoft.github.io/reverse-proxy/

### Getting Help

- **GitHub Issues**: https://github.com/your-org/sorcha/issues
- **Slack**: #sorcha-dev (internal)
- **Documentation Questions**: Tag @architecture-team in PR

---

**Happy Coding!** 🚀

**Document Version**: 1.0 | **Last Updated**: 2026-01-06
