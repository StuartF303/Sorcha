# Blueprint Storage Basic Walkthrough

**Purpose:** Demonstrates bringing up Sorcha in Docker from clean start and achieving basic blueprint design capability (create, modify, save blueprints).

**Date Created:** 2026-01-02
**Status:** ✅ Complete
**Prerequisites:** Docker Desktop, .NET 10 SDK, PowerShell

---

## Overview

This walkthrough demonstrates:
1. Starting all Sorcha services with Docker Compose
2. Bootstrapping the platform (creating org + admin user)
3. Authenticating with JWT tokens
4. Creating and uploading blueprints via API
5. Verifying blueprint storage (in-memory for MVD)

---

## Files in This Walkthrough

### Documentation
- **DOCKER-BOOTSTRAP-RESULTS.md** - Complete results, access points, troubleshooting guide

### Test Scripts
- **test-jwt.ps1** - Test JWT authentication, decode token payload
- **simple-blueprint-test.ps1** - Quick authentication + blueprint list test
- **upload-blueprint-test.ps1** - ⭐ Full blueprint upload workflow (main script)
- **test-blueprint-api.ps1** - Comprehensive CRUD operations test

---

## Quick Start

### 1. Start Docker Services
```bash
# From repository root
docker-compose up -d

# Verify all services healthy
curl http://localhost/api/health
```

### 2. Bootstrap Platform (First Time Only)
```powershell
# From repository root
powershell -ExecutionPolicy Bypass -File scripts/bootstrap-sorcha.ps1 `
  -Profile docker-direct `
  -NonInteractive `
  -OrgName "Sorcha Development" `
  -Subdomain "demo" `
  -AdminEmail "stuart.mackintosh@sorcha.dev" `
  -AdminName "Stuart Mackintosh" `
  -AdminPassword "SorchaDev2025!"
```

### 3. Test Authentication
```powershell
# From repository root
powershell -File walkthroughs/BlueprintStorageBasic/test-jwt.ps1
```

### 4. Upload a Blueprint
```powershell
# From repository root
powershell -File walkthroughs/BlueprintStorageBasic/upload-blueprint-test.ps1
```

---

## Key Results

**✅ All services running:**
- 12 Docker containers (Blueprint, Wallet, Register, Tenant, Validator, Peer, API Gateway, + infrastructure)
- All services reporting healthy

**✅ Authentication working:**
- JWT Bearer tokens
- OAuth2 password grant
- 60-minute token lifetime

**✅ Blueprint upload working:**
- Successfully uploaded "Simple Invoice Approval" blueprint
- REST API accepting authenticated requests
- Sample blueprints available in `samples/blueprints/`

**⚠️ Known limitation:**
- In-memory storage (blueprints not persisted between restarts)
- Expected for MVD phase (97% complete, 30% production ready)
- Database persistence planned as P1 priority

---

## Access Points

| Service | URL | Purpose |
|---------|-----|---------|
| API Gateway | http://localhost/ | Main entry point |
| API Docs | http://localhost/scalar/ | Interactive API documentation |
| Health Check | http://localhost/api/health | Aggregated service health |
| Aspire Dashboard | http://localhost:18888 | Observability & telemetry |
| Tenant Service | http://localhost:5110 | Authentication (direct) |
| Blueprint Service | http://localhost:5000 | Blueprint management (direct) |

---

## Credentials

**Bootstrap Admin User:**
- Email: `stuart.mackintosh@sorcha.dev`
- Password: `SorchaDev2025!`
- Organization: Sorcha Development (subdomain: demo)

---

## Next Steps

1. **Blueprint Design:** Create custom blueprints as JSON/YAML files
2. **Blueprint Execution:** Implement workflow execution (Wallet → Register integration)
3. **Production Readiness:** Add database persistence (EF Core + MongoDB)

---

## Related Documentation

- Main results: [DOCKER-BOOTSTRAP-RESULTS.md](./DOCKER-BOOTSTRAP-RESULTS.md)
- Sample blueprints: `../../samples/blueprints/`
- Bootstrap script: `../../scripts/bootstrap-sorcha.ps1`
- Docker setup: [../../docs/DOCKER-QUICK-START.md](../../docs/DOCKER-QUICK-START.md)

---

## Troubleshooting

### Services won't start
```bash
docker-compose logs service-name
docker-compose restart service-name
```

### Authentication failing
- Verify credentials: stuart.mackintosh@sorcha.dev / SorchaDev2025!
- Check token hasn't expired (60 min)
- Verify Tenant Service running: `curl http://localhost:5110/health`

### Blueprints not persisting
- **Expected behavior** - using in-memory storage
- Re-upload blueprints after service restart
- Production will use PostgreSQL persistence

---

**Walkthrough Complete!** You can now create, modify, and save blueprints using the Sorcha platform running in Docker.
