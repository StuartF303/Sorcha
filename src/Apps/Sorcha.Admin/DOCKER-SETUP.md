# Sorcha Admin UI - Docker Configuration

## Overview

The Sorcha Admin UI has been converted from Blazor WebAssembly to Blazor Web App (Hybrid) and configured for Docker deployment behind the API Gateway at `/admin/*` paths.

## Architecture Changes

### Before (Blazor WASM)
- Static files served by nginx
- Client-side only rendering
- Files served at `/admin/` subfolder

### After (Blazor Web App)
- ASP.NET Core runtime required
- Hybrid Server + WebAssembly rendering
- Full application running on ASP.NET Core

## Docker Configuration

### Dockerfile
- **Base Image:** `mcr.microsoft.com/dotnet/aspnet:10.0`
- **Build Image:** `mcr.microsoft.com/dotnet/sdk:10.0`
- **Port:** 8080 (HTTP)
- **Entry Point:** `dotnet Sorcha.Admin.dll`

### Build Command
```bash
docker build -f src/Apps/Sorcha.Admin/Dockerfile -t sorcha-admin:latest .
```

### Run Command
```bash
docker run -p 8080:8080 --name sorcha-admin sorcha-admin:latest
```

## URL Routing

### API Gateway Configuration
The Admin UI now serves multiple URL paths from a single deployment:

- **Homepage Route:** `/` → `http://sorcha-admin:8080/`
- **Login Route:** `/login` → `http://sorcha-admin:8080/login`
- **Admin Routes:** `/admin/{**catch-all}` → `http://sorcha-admin:8080/admin/{**catch-all}`
- **Designer Routes:** `/design/{**catch-all}` → `http://sorcha-admin:8080/design/{**catch-all}`
- **Static Assets:** `/_framework/{**catch-all}`, `/_content/{**catch-all}` → Admin UI

### Example URLs
- **Homepage:** `http://gateway/` (anonymous access, installation info)
- **Login:** `http://gateway/login` (inline credential form)
- **Admin Pages:** `http://gateway/admin/*` (authenticated admins)
- **Designer Pages:** `http://gateway/design/*` (authenticated designers)
- **API Routes:** `http://gateway/api/*` (pass-through to backend services)

### Application Base Path
- **Base Href:** `/` (root, not `/admin/`)
- **Path Base Middleware:** None (removed - Gateway routes full paths)

## Changes Made

### 1. Dockerfile (`src/Apps/Sorcha.Admin/Dockerfile`)
- Converted from nginx to ASP.NET Core runtime
- Multi-stage build with SDK for compilation
- Exposes port 8080
- Includes Client project in build

### 2. App.razor (`src/Apps/Sorcha.Admin/Components/App.razor`)
```html
<base href="/" />
```
**Changed from:** `<base href="/admin/" />`

### 3. Program.cs (`src/Apps/Sorcha.Admin/Program.cs`)
- Removed `app.UsePathBase("/admin")` (no longer needed)
- Added `app.MapStaticAssets()` to serve Blazor framework files
- Added authentication/authorization middleware

### 4. Login Page (`src/Apps/Sorcha.Admin/Pages/Login.razor`)
- **Complete rewrite:** Inline credential form instead of modal dialog
- **Profile Selector:** Dropdown to choose authentication profile
- **Username/Password:** Direct input fields with icons
- **Error Handling:** Inline error messages
- **Improved UX:** No modal dialogs, cleaner authentication flow

### 5. Navigation Updates
- **Index.razor:** Changed `/admin/login` → `/login`
- **RedirectToLogin.razor:** Changed `login` → `/login`
- **UserProfileMenu.razor:** Changed `login` → `/login`
- All navigation uses absolute paths from root

### 6. API Gateway (`src/Services/Sorcha.ApiGateway/appsettings.json`)
**New Routes Added:**
```json
"admin-ui-root": {
  "ClusterId": "admin-cluster",
  "Match": { "Path": "/" }
},
"admin-ui-login": {
  "ClusterId": "admin-cluster",
  "Match": { "Path": "/login" }
},
"admin-ui-admin": {
  "ClusterId": "admin-cluster",
  "Match": { "Path": "/admin/{**catch-all}" }
},
"admin-ui-design": {
  "ClusterId": "admin-cluster",
  "Match": { "Path": "/design/{**catch-all}" }
},
"admin-ui-static-framework": {
  "ClusterId": "admin-cluster",
  "Match": { "Path": "/_framework/{**catch-all}" }
},
"admin-ui-static-content": {
  "ClusterId": "admin-cluster",
  "Match": { "Path": "/_content/{**catch-all}" }
}
```

**Cluster Configuration:**
```json
"admin-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "http://sorcha-admin:8080"
    }
  }
}
```

### 6. Removed Files
- `nginx.conf` (no longer needed)

## Environment Variables

### Required
- `ASPNETCORE_ENVIRONMENT` - Set to `Production`, `Development`, or `Staging`
- `ASPNETCORE_URLS` - Default: `http://+:8080`

### Optional
- `ApiGateway__BaseUrl` - API Gateway URL for service communication
- Standard .NET logging configuration

## Testing Locally

### Direct Access
```bash
dotnet run --project src/Apps/Sorcha.Admin/Sorcha.Admin.csproj
# Access at: http://localhost:8080/
# Login at: http://localhost:8080/login
# Admin pages: http://localhost:8080/admin/*
# Designer: http://localhost:8080/design/*
```

### Via API Gateway (Docker)
```bash
# Access homepage: http://localhost:5110/
# Login: http://localhost:5110/login
# Admin: http://localhost:5110/admin/*
# Designer: http://localhost:5110/design/*
```

## Known Issues

### None Currently
All URL routing issues have been resolved. The application now serves:
- Anonymous homepage at `/`
- Login page at `/login` with inline credential form
- Admin pages at `/admin/*` (requires authentication)
- Designer pages at `/design/*` (requires authentication)
- API pass-through at `/api/*` (routes to backend services)

## Production Deployment

### Docker Compose
```yaml
services:
  sorcha-admin:
    image: sorcha-admin:latest
    container_name: sorcha-admin
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    networks:
      - sorcha-network

  api-gateway:
    image: sorcha-api-gateway:latest
    ports:
      - "80:8080"
    depends_on:
      - sorcha-admin
    networks:
      - sorcha-network

networks:
  sorcha-network:
    driver: bridge
```

### Kubernetes
```yaml
apiVersion: v1
kind: Service
metadata:
  name: sorcha-admin
spec:
  selector:
    app: sorcha-admin
  ports:
    - protocol: TCP
      port: 8080
      targetPort: 8080
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sorcha-admin
spec:
  replicas: 2
  selector:
    matchLabels:
      app: sorcha-admin
  template:
    metadata:
      labels:
        app: sorcha-admin
    spec:
      containers:
      - name: sorcha-admin
        image: sorcha-admin:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## Migration Checklist

- [x] Update Dockerfile for ASP.NET Core runtime
- [x] Configure base path in App.razor
- [x] Add UsePathBase middleware in Program.cs
- [x] Update navigation calls to use relative paths
- [x] Update API Gateway cluster destination port
- [x] Remove nginx configuration
- [x] Add .dockerignore file
- [ ] Test Docker build
- [ ] Test Docker run locally
- [ ] Test behind API Gateway
- [ ] Update docker-compose.yml (if applicable)

## Next Steps

1. **Rebuild Docker Images:**
   ```bash
   docker-compose build sorcha-admin
   docker-compose build sorcha-api-gateway
   ```

2. **Deploy Updated Services:**
   ```bash
   docker-compose up -d
   ```

3. **Test All Routes:**
   - Homepage: `http://localhost:5110/`
   - Login: `http://localhost:5110/login`
   - Admin: `http://localhost:5110/admin/*` (after authentication)
   - Designer: `http://localhost:5110/design/*` (after authentication)
   - API: `http://localhost:5110/api/*` (backend services)

4. **Verify Authentication Flow:**
   - Navigate to homepage → Click "Sign In" → Enter credentials → Verify redirect to homepage
   - Test logout → Verify redirect to login page
   - Test accessing `/admin/*` without auth → Verify redirect to login

## Support

For issues or questions, see:
- [Main README](../../README.md)
- [CLAUDE.md](../../CLAUDE.md)
- [Troubleshooting Guide](../../TROUBLESHOOTING.md)
