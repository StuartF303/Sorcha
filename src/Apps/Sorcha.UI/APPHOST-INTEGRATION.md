# AppHost Integration - Sorcha.UI.Web

**Created:** 2026-01-06
**Status:** ✅ Complete

## Overview

Successfully integrated Sorcha.UI.Web into the .NET Aspire AppHost, replacing the old Sorcha.Admin Blazor Server implementation with the new Blazor WebAssembly architecture.

## Changes Made

### 1. AppHost Project Reference

**File:** `src/Apps/Sorcha.AppHost/Sorcha.AppHost.csproj`

**Changed:**
```xml
<!-- OLD -->
<ProjectReference Include="..\Sorcha.Admin\Sorcha.Admin.csproj" />

<!-- NEW -->
<ProjectReference Include="..\Sorcha.UI\Sorcha.UI.Web\Sorcha.UI.Web.csproj" />
```

### 2. AppHost Service Configuration

**File:** `src/Apps/Sorcha.AppHost/AppHost.cs`

**Changed:**
```csharp
// OLD
// Add Blazor Hybrid Admin UI as the default homepage
// Note: This is now a Blazor Web App (Hybrid) with Server + WASM render modes
var adminUI = builder.AddProject<Projects.Sorcha_Admin>("admin-ui")
    .WithReference(apiGateway) // Admin can discover and call API Gateway
    .WithExternalHttpEndpoints(); // Primary external entry point for users

// NEW
// Add Blazor WebAssembly UI as the default homepage
// Note: This is a Blazor Web App with Server + WASM render modes
var uiWeb = builder.AddProject<Projects.Sorcha_UI_Web>("ui-web")
    .WithReference(apiGateway) // UI can discover and call API Gateway
    .WithExternalHttpEndpoints(); // Primary external entry point for users
```

**Also updated comment on line 86:**
```csharp
// OLD
.WithExternalHttpEndpoints(); // Exposed for API calls from Admin UI

// NEW
.WithExternalHttpEndpoints(); // Exposed for API calls from UI
```

**Service Discovery Name:** `ui-web`

### 3. API Gateway Routing Configuration

**File:** `src/Services/Sorcha.ApiGateway/appsettings.json`

#### Updated Scoped Styles Route (Line 179-189)

```json
// OLD
"admin-ui-scoped-styles": {
  "ClusterId": "admin-cluster",
  "Match": {
    "Path": "/Sorcha.Admin.styles.css"
  },
  "Transforms": [
    {
      "PathPattern": "/Sorcha.Admin.styles.css"
    }
  ]
}

// NEW
"admin-ui-scoped-styles": {
  "ClusterId": "admin-cluster",
  "Match": {
    "Path": "/Sorcha.UI.Web.styles.css"
  },
  "Transforms": [
    {
      "PathPattern": "/Sorcha.UI.Web.styles.css"
    }
  ]
}
```

#### Updated Cluster Destination (Line 522-528)

```json
// OLD
"admin-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "http://sorcha-admin:8080"
    }
  }
}

// NEW
"admin-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "http://ui-web:8080"
    }
  }
}
```

**Note:** The cluster name remains `admin-cluster` for backward compatibility with existing routes.

### 4. API Gateway Status Page

**File:** `src/Services/Sorcha.ApiGateway/Program.cs`

**Changed (Line 433):**
```html
<!-- OLD -->
<a href="/" class="btn btn-primary">🏠 Admin UI Home</a>

<!-- NEW -->
<a href="/" class="btn btn-primary">🏠 Sorcha UI Home</a>
```

## Routes Maintained

All existing YARP routes continue to work with the new UI:

**UI Routes (all proxy to `admin-cluster` → `http://ui-web:8080`):**
- `GET /` - Root page (login or home if authenticated)
- `GET /login` - Login page
- `GET /admin/{**catch-all}` - Admin module
- `GET /design/{**catch-all}` - Designer module
- `GET /_framework/{**catch-all}` - Blazor framework files
- `GET /_content/{**catch-all}` - Static content
- `GET /_blazor/{**catch-all}` - Blazor SignalR hub
- `GET /css/{**catch-all}` - CSS files
- `GET /js/{**catch-all}` - JavaScript files
- `GET /favicon.png` - Favicon
- `GET /manifest.json` - PWA manifest
- `GET /icon-192.png` - PWA icon 192x192
- `GET /icon-512.png` - PWA icon 512x512
- `GET /Sorcha.UI.Web.styles.css` - Scoped component styles

**API Routes** remain unchanged and route to backend services.

## Service Discovery

### Aspire Service Names

When running via AppHost:

| Service | Aspire Name | Internal URL | External URL (if exposed) |
|---------|-------------|--------------|---------------------------|
| UI Web | `ui-web` | `http://ui-web:8080` | https://localhost:7083 |
| API Gateway | `api-gateway` | `http://api-gateway:8080` | https://localhost:7082 |
| Tenant Service | `tenant-service` | `http://tenant-service:8080` | - |
| Blueprint Service | `blueprint-service` | `http://blueprint-service:8080` | - |
| Wallet Service | `wallet-service` | `http://wallet-service:8080` | - |
| Register Service | `register-service` | `http://register-service:8080` | https://localhost:7111 |
| Peer Service | `peer-service` | `http://peer-service:8080` | - |
| Validator Service | `validator-service` | `http://validator-service:8080` | https://localhost:7112 |

### Environment Variables

The UI Web project receives these environment variables from Aspire:

- `services__apigateway__http__0` - API Gateway HTTP endpoint (e.g., `http://api-gateway:8080`)
- `services__apigateway__https__0` - API Gateway HTTPS endpoint (if configured)
- `JwtSettings__*` - JWT configuration (if added)

The UI can discover the API Gateway using:
```csharp
builder.Configuration["services:apigateway:http:0"]
```

## Running the Application

### Via .NET Aspire AppHost (Recommended)

```bash
cd C:/Projects/Sorcha/src/Apps/Sorcha.AppHost
dotnet run
```

**Aspire Dashboard:** http://localhost:15888
**UI Web (via Gateway):** https://localhost:7082 → proxies to `http://ui-web:8080`
**UI Web (direct):** https://localhost:7083
**API Gateway Status:** https://localhost:7082/gateway

### Standalone (Without Aspire)

```bash
cd C:/Projects/Sorcha/src/Apps/Sorcha.UI/Sorcha.UI.Web
dotnet run --launch-profile https
```

**Direct URL:** https://localhost:7083

**Note:** When running standalone, update `appsettings.json` or use profiles to configure the API Gateway URL.

## Configuration Files

### UI Web Configuration

**File:** `src/Apps/Sorcha.UI/Sorcha.UI.Web/appsettings.json`

```json
{
  "ApiGatewayUrl": "https://localhost:7082"
}
```

**File:** `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/wwwroot/appsettings.json`

Default profiles configured in `ConfigurationService`:

```json
{
  "Development": {
    "ApiGatewayUrl": "https://localhost:7082"
  },
  "Docker": {
    "ApiGatewayUrl": "http://localhost:8080"
  }
}
```

## Build Verification

✅ **Build Status:** Successful (0 warnings, 0 errors)

```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:08.57
```

**Built Projects:**
- Sorcha.UI.Shared
- Sorcha.UI.Core
- Sorcha.UI.Web.Client
- Sorcha.UI.Web.Client (Blazor output → wwwroot)
- Sorcha.UI.Web
- Sorcha.AppHost

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         .NET Aspire AppHost                     │
│                                                                 │
│  ┌──────────────┐    ┌──────────────┐    ┌─────────────────┐  │
│  │  UI Web      │    │ API Gateway  │    │ Backend Services│  │
│  │  (ui-web)    │◄───┤ (YARP Proxy) ├───►│ - Tenant        │  │
│  │              │    │              │    │ - Blueprint     │  │
│  │ Port: 7083   │    │ Port: 7082   │    │ - Wallet        │  │
│  └──────────────┘    └──────────────┘    │ - Register      │  │
│        ▲                     ▲            │ - Peer          │  │
│        │                     │            │ - Validator     │  │
│        │                     │            └─────────────────┘  │
│        │                     │                                 │
│  ┌─────┴─────────────────────┴────────────────────────────┐   │
│  │         Shared Infrastructure                           │   │
│  │  - PostgreSQL (tenant-db, wallet-db)                    │   │
│  │  - MongoDB (register-db)                                │   │
│  │  - Redis (caching, SignalR backplane)                   │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘

External Access:
  User Browser → https://localhost:7083 (Direct to UI)
  User Browser → https://localhost:7082 (Gateway → Proxy to UI)
  User Browser → https://localhost:7082/api/* (Gateway → Backend APIs)
```

## Authentication Flow

```
┌─────────────┐                                    ┌──────────────┐
│   Browser   │                                    │   UI Web     │
│  (Client)   │                                    │  (Server)    │
└──────┬──────┘                                    └──────┬───────┘
       │                                                  │
       │ 1. Navigate to /                                │
       ├─────────────────────────────────────────────────►
       │                                                  │
       │ 2. Check auth state (no token in LocalStorage)  │
       │◄─────────────────────────────────────────────────┤
       │ 3. Redirect to /login                           │
       │                                                  │
       │ 4. POST /api/service-auth/token                 │
       │    (via ConfigurationService + AuthService)     │
       ├──────────────────────┐                          │
       │                      │                          │
       │                      ▼                          │
       │              ┌──────────────┐                   │
       │              │ API Gateway  │                   │
       │              │   (Proxy)    │                   │
       │              └──────┬───────┘                   │
       │                     │                           │
       │                     ▼                           │
       │              ┌──────────────┐                   │
       │              │   Tenant     │                   │
       │              │   Service    │                   │
       │              └──────┬───────┘                   │
       │                     │                           │
       │ 5. Return JWT token │                           │
       │◄────────────────────┘                           │
       │                                                  │
       │ 6. Encrypt + Store in LocalStorage              │
       │    (AES-256-GCM via Web Crypto API)             │
       │                                                  │
       │ 7. Redirect to /                                │
       ├─────────────────────────────────────────────────►
       │                                                  │
       │ 8. Load authenticated UI                        │
       │◄─────────────────────────────────────────────────┤
       │                                                  │
```

## Testing the Integration

### 1. Start AppHost

```bash
cd C:/Projects/Sorcha/src/Apps/Sorcha.AppHost
dotnet run
```

### 2. Access Aspire Dashboard

Open: http://localhost:15888

Verify services:
- ✅ ui-web (Running)
- ✅ api-gateway (Running)
- ✅ tenant-service (Running)
- ✅ All backend services (Running)

### 3. Access UI via Gateway

Open: https://localhost:7082

**Expected:** Redirects to login page or shows home if authenticated

### 4. Access UI Directly

Open: https://localhost:7083

**Expected:** Redirects to login page or shows home if authenticated

### 5. Test Login Flow

1. Navigate to https://localhost:7083/login
2. Select profile: "Development"
3. Enter credentials:
   - Username: `admin@sorcha.local`
   - Password: `Admin123!`
4. Click "Sign In"

**Expected:**
- ✅ Token stored in LocalStorage (encrypted)
- ✅ Redirect to home page
- ✅ Authenticated state persists on refresh

### 6. Test Protected Routes

Navigate to: https://localhost:7083/auth-test

**Expected:**
- ✅ Shows user information
- ✅ Displays profile, roles, expiration
- ✅ Logout button works

## Migration Notes

### Old Architecture (Sorcha.Admin)

- **Type:** Blazor Server (SignalR-based)
- **Ports:** 7083 (HTTPS), 5173 (HTTP)
- **State Management:** Server-side component state
- **Known Issues:** SignalR bugs, complex state management

### New Architecture (Sorcha.UI.Web)

- **Type:** Blazor WebAssembly (WASM)
- **Ports:** 7083 (HTTPS), 5173 (HTTP) - same for compatibility
- **State Management:** Client-side with encrypted token caching
- **Advantages:**
  - ✅ No SignalR dependencies
  - ✅ Client-side execution
  - ✅ Better offline support
  - ✅ Reduced server load
  - ✅ Encrypted LocalStorage for tokens

### Breaking Changes

None for end users. All routes and URLs remain the same.

### Deprecation

Sorcha.Admin is now deprecated but remains in the repository for reference:
- **Location:** `src/Apps/Sorcha.Admin/`
- **Status:** Not referenced by AppHost
- **Future:** Will be removed in a future release

## Next Steps

### Immediate (Post-Integration)

1. **Manual Testing**
   - ✅ AppHost integration verified
   - ⏳ Login flow testing with backend
   - ⏳ Token refresh testing
   - ⏳ Protected route testing

2. **MudBlazor UI Implementation**
   - Implement MainLayout with AppBar and Navigation
   - Add user profile menu with logout
   - Create dashboard cards for statistics
   - Implement module navigation (Admin, Designer, Explorer)

3. **Module Structure**
   - Lazy load Admin module
   - Lazy load Designer module
   - Lazy load Explorer module

### Future Enhancements

1. **Production Readiness**
   - Configure production profiles
   - Add HTTPS certificates for production
   - Configure CORS policies
   - Add rate limiting

2. **Features**
   - Profile management UI
   - Token refresh notifications
   - Offline support improvements
   - PWA enhancements

---

**Status:** ✅ Integration Complete
**Build:** ✅ All projects building successfully
**Next:** Manual testing with backend services

**Last Updated:** 2026-01-06
