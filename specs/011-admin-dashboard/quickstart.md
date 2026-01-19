# Quickstart: Admin Dashboard and Management

**Feature Branch**: `011-admin-dashboard`
**Date**: 2026-01-19

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop running (for backend services)
- VS Code or Rider IDE

## Getting Started

### 1. Start Backend Services

```bash
# Start all Sorcha services with Docker
cd /c/Projects/Sorcha
docker-compose up -d

# Verify services are running
docker-compose ps
```

### 2. Verify API Availability

```bash
# Check Tenant Service health
curl http://localhost/tenant/health

# Check Organization endpoint
curl http://localhost/api/organizations/stats
```

### 3. Run the UI Application

```bash
# Option A: Run with Aspire (recommended for development)
dotnet run --project src/Apps/Sorcha.AppHost

# Option B: Run UI standalone
dotnet run --project src/Apps/Sorcha.UI/Sorcha.UI.Web
```

### 4. Access the Application

- **Main UI**: http://localhost:5252
- **Admin Dashboard**: http://localhost:5252/admin
- **API Gateway**: http://localhost:80

---

## Development Workflow

### Adding a New Component

1. Create the component in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/`
2. Follow the naming pattern: `{Feature}{Action}.razor` (e.g., `OrganizationList.razor`)
3. Add to the Administration.razor tabs

### Testing Changes

```bash
# Run E2E tests
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Admin"

# Run in headed mode for debugging
HEADED=true dotnet test tests/Sorcha.UI.E2E.Tests
```

---

## Key Files

| File | Purpose |
|------|---------|
| `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Administration.razor` | Main admin page with tabs |
| `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Admin/` | Admin component library |
| `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/` | Service abstractions |
| `src/Services/Sorcha.Tenant.Service/Endpoints/OrganizationEndpoints.cs` | Backend API |

---

## Authentication

The admin features require authentication. Use the bootstrap endpoint to create initial admin credentials:

```bash
# Bootstrap system (first-time setup)
curl -X POST http://localhost/api/bootstrap/initialize \
  -H "Content-Type: application/json" \
  -d '{"adminEmail": "admin@sorcha.local", "adminPassword": "Admin123!"}'
```

Login at http://localhost:5252/login with the admin credentials.

---

## Troubleshooting

### Services Not Responding

```bash
# Check Docker logs
docker-compose logs tenant-service

# Restart specific service
docker-compose restart tenant-service
```

### Authentication Issues

```bash
# Verify JWT configuration
curl http://localhost/tenant/.well-known/openid-configuration

# Check token endpoint
curl -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@sorcha.local", "password": "Admin123!"}'
```

### UI Not Loading

```bash
# Clear browser cache and Blazor state
# In Chrome: Developer Tools > Application > Clear Site Data

# Rebuild UI
dotnet build src/Apps/Sorcha.UI/Sorcha.UI.Web
```

---

## API Quick Reference

### Organizations

```bash
# List organizations (requires admin token)
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/organizations

# Create organization
curl -X POST http://localhost/api/organizations \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name": "Acme Corp", "subdomain": "acme"}'

# Validate subdomain
curl http://localhost/api/organizations/validate-subdomain/acme
```

### Users

```bash
# List organization users
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost/api/organizations/{orgId}/users

# Add user
curl -X POST http://localhost/api/organizations/{orgId}/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email": "user@acme.com", "displayName": "John Doe", "externalIdpUserId": "ext-123", "roles": ["Member"]}'
```

### Health Checks

```bash
# Check all services
for svc in blueprint register wallet tenant validator peer; do
  echo "$svc: $(curl -s http://localhost/$svc/health | jq -r .status)"
done
```
