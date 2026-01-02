# Sorcha Docker Bootstrap Results

**Date:** 2026-01-02
**Status:** ✅ SUCCESS - Basic Blueprint Design Capability Achieved

---

## Summary

Successfully brought up a full Sorcha instance from clean start using Docker Compose and demonstrated basic blueprint design workflow including authentication, blueprint creation, and API interaction.

## What Was Accomplished

### 1. Infrastructure Setup ✅
- **Docker Compose:** All 12 services started successfully
  - Infrastructure: Redis, PostgreSQL, MongoDB, Aspire Dashboard
  - Application Services: Blueprint, Wallet, Register, Tenant, Validator
  - Networking: Peer Hub, Peer Service
  - Gateway: API Gateway (HTTP/HTTPS)
- **Health Checks:** All services reporting healthy
- **Ports:** All services accessible on documented ports

### 2. Platform Bootstrap ✅
- **Organization Created:** "Sorcha Development" (subdomain: demo)
  - Org ID: `b7c92150-2a6d-4b1d-9c45-36d93ea62c3f`
- **Admin User Created:** stuart.mackintosh@sorcha.dev
  - User ID: `3eb135ab-0aee-45e2-bb44-414aab691913`
  - Roles: Administrator, Designer, Developer, User, Consumer, Auditor
- **Profile:** docker-direct (configured for direct Docker service access)

### 3. Authentication Working ✅
- **JWT Token Authentication:** Fully functional
- **OAuth2 Password Grant:** Working with created user credentials
- **Authorization:** Bearer token authentication across all services
- **Token Details:**
  - Token Type: user
  - Expires: 60 minutes (3600 seconds)
  - Refresh Token: 24 hours

### 4. Blueprint Service API ✅
- **Authentication Required:** 401 responses enforced correctly
- **Blueprint Upload:** Successfully uploaded "Simple Invoice Approval" blueprint
  - Blueprint ID: `8f405bad-e20a-4817-bd09-6de5d9bb50a8`
  - Title: Simple Invoice Approval
  - Actions: 2 (Submit Invoice, Approve Payment)
  - Participants: 2 (Vendor, Accounts Payable)
- **API Endpoints Tested:**
  - POST /api/blueprints - ✅ Working
  - GET /api/blueprints - ✅ Working
  - Authentication - ✅ Working

### 5. Sample Blueprints Available ✅
Located 8 sample blueprints in `samples/blueprints/`:
- Finance: simple-invoice-approval, moderate-purchase-order, complex-trade-settlement
- Benefits: simple-unemployment-claim, moderate-disability-assessment
- Healthcare: simple-patient-referral, moderate-medical-records-sharing
- Supply Chain: moderate-international-shipping

---

## Known Limitations (Expected for MVD)

### 1. Database Persistence ⚠️
- **Status:** In-memory storage only (as per MASTER-PLAN.md)
- **Impact:** Blueprints not persisted between service restarts
- **Workaround:** Re-upload blueprints after restart
- **Future:** EF Core repositories planned (P1 priority)

### 2. Production Readiness ⚠️
- **Current:** 30% production ready (per README)
- **Missing:**
  - Database persistence (Wallet EF Core, Register MongoDB, Blueprint EF Core)
  - Azure Key Vault integration
  - Security hardening (rate limiting, input validation)
  - Deployment documentation

### 3. CLI Authentication 📝
- **Interactive Mode Only:** CLI designed for interactive password entry
- **Direct API:** Can authenticate via REST API for automation
- **Tokens:** Stored encrypted in platform-specific secure storage

---

## System Access Points

### Main Services
| Service | URL | Purpose |
|---------|-----|---------|
| **API Gateway** | http://localhost/ | Main entry point |
| **API Documentation** | http://localhost/scalar/ | Interactive API docs |
| **Health Check** | http://localhost/api/health | Aggregated service health |
| **Aspire Dashboard** | http://localhost:18888 | Observability & telemetry |

### Direct Service Access (docker-direct profile)
| Service | URL | Purpose |
|---------|-----|---------|
| **Tenant Service** | http://localhost:5110 | Authentication & organizations |
| **Blueprint Service** | http://localhost:5000 | Blueprint management |
| **Wallet Service** | http://localhost:5001 | Cryptographic wallets |
| **Register Service** | http://localhost:5290 | Distributed ledger |
| **Validator Service** | http://localhost:5100 | Schema validation |

### Infrastructure
| Service | URL | Credentials |
|---------|-----|-------------|
| **PostgreSQL** | localhost:5432 | User: `sorcha`, Password: `sorcha_dev_password` |
| **MongoDB** | localhost:27017 | User: `sorcha`, Password: `sorcha_dev_password` |
| **Redis** | localhost:6379 | No authentication |

---

## Test Scripts Created

### 1. `test-jwt.ps1`
- Tests JWT authentication
- Decodes and displays token payload
- Caches token for reuse

### 2. `simple-blueprint-test.ps1`
- Quick authentication + blueprint list test
- Minimal script for verification

### 3. `upload-blueprint-test.ps1` ⭐
- Full blueprint upload workflow
- Authenticates with Tenant Service
- Loads blueprint from JSON file
- Uploads to Blueprint Service
- Verifies creation (within in-memory limitations)
- **Usage:** `powershell.exe -File walkthroughs/BlueprintStorageBasic/upload-blueprint-test.ps1`

---

## Next Steps for Blueprint Design

### Immediate (Now Working)
1. ✅ Create blueprints as JSON/YAML files
2. ✅ Upload via Blueprint Service API with authentication
3. ✅ Use sample blueprints as templates
4. ⚠️ Note: Blueprints not persisted (in-memory storage)

### Blueprint Execution Workflow (Next Phase)
1. **Load Blueprint:** Use `Sorcha.Demo` app or API
2. **Create Wallets:** Use Wallet Service to create participant wallets
3. **Execute Actions:** Submit actions through Blueprint Service
4. **Sign Transactions:** Wallet Service signs action payloads
5. **Store on Ledger:** Register Service records transactions
6. **Real-time Updates:** SignalR notifications for action events

### Production Readiness (P1 Tasks from MASTER-PLAN.md)
1. **Database Persistence:**
   - Wallet Service: EF Core → PostgreSQL
   - Register Service: MongoDB repository
   - Blueprint Service: EF Core → PostgreSQL
2. **Azure Key Vault Integration:** Secure key management for Wallet Service
3. **Security Hardening:** Rate limiting, input validation, HTTPS enforcement
4. **Performance Testing:** Load testing with NBomber
5. **E2E Integration Tests:** Blueprint → Wallet → Register flow

---

## Configuration Files

### CLI Profiles
Located: `C:\Users\stuart\.sorcha\config.json`

**Current Active Profile:** `docker-direct`
```json
{
  "tenantServiceUrl": "http://localhost:5110",
  "walletServiceUrl": "http://localhost:5001",
  "registerServiceUrl": "http://localhost:5290",
  "authTokenUrl": "http://localhost:5110/api/service-auth/token"
}
```

**Alternative Profiles:**
- `dev` - For .NET Aspire development (HTTPS on 7xxx ports)
- `docker` - For API Gateway routing (http://localhost:8080/...)
- `local` - For individual service runs

### Credentials
**Bootstrap Admin User:**
- Email: stuart.mackintosh@sorcha.dev
- Password: SorchaDev2025!
- Organization: Sorcha Development (subdomain: demo)
- Org ID: b7c92150-2a6d-4b1d-9c45-36d93ea62c3f
- User ID: 3eb135ab-0aee-45e2-bb44-414aab691913

---

## Common Commands

### Docker Management
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Restart specific service
docker-compose restart blueprint-service

# Check service status
docker-compose ps
```

### Blueprint Workflow
```powershell
# Upload a blueprint (run from repository root)
powershell.exe -File walkthroughs/BlueprintStorageBasic/upload-blueprint-test.ps1

# Test authentication
powershell.exe -File walkthroughs/BlueprintStorageBasic/test-jwt.ps1

# Quick service check
powershell.exe -File walkthroughs/BlueprintStorageBasic/simple-blueprint-test.ps1
```

### Manual API Testing
```bash
# Get auth token
curl -X POST http://localhost:5110/api/service-auth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=stuart.mackintosh@sorcha.dev&password=SorchaDev2025!&client_id=sorcha-cli"

# List blueprints (with token)
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  http://localhost:5000/api/blueprints

# Check health
curl http://localhost/api/health
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      API Gateway (YARP)                     │
│                   http://localhost:80/443                   │
└────────────────────────────┬────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
    ┌────▼────┐      ┌───────▼────────┐    ┌────▼────┐
    │ Blueprint│      │   Tenant       │    │ Wallet  │
    │ Service  │      │   Service      │    │ Service │
    │ :5000    │      │   :5110        │    │ :5001   │
    └─────┬────┘      └───────┬────────┘    └────┬────┘
          │                   │                   │
          │                   │                   │
    ┌─────▼────┐      ┌───────▼────────┐    ┌────▼────┐
    │ Register │      │   PostgreSQL   │    │  Redis  │
    │ Service  │      │   :5432        │    │  :6379  │
    │ :5290    │      └────────────────┘    └─────────┘
    └─────┬────┘
          │
    ┌─────▼────────┐
    │   MongoDB    │
    │   :27017     │
    └──────────────┘
```

---

## Troubleshooting

### Issue: Service won't start
```bash
# Check logs
docker-compose logs service-name

# Restart service
docker-compose restart service-name

# Rebuild if needed
docker-compose up -d --build service-name
```

### Issue: Authentication failing
- Verify credentials: stuart.mackintosh@sorcha.dev / SorchaDev2025!
- Check Tenant Service is running: `curl http://localhost:5110/health`
- Verify token hasn't expired (60 min lifetime)

### Issue: Blueprint upload returns 401
- Obtain fresh token (tokens expire after 60 minutes)
- Verify Authorization header format: `Bearer YOUR_TOKEN`
- Check Tenant Service logs: `docker logs sorcha-tenant-service`

### Issue: Blueprints not persisting
- **Expected behavior** - using in-memory storage
- Blueprints lost on service restart
- Re-upload after restart
- Production will use EF Core + PostgreSQL

---

## Conclusion

✅ **Goal Achieved:** Successfully brought up Sorcha in Docker and demonstrated blueprint design capability

**What Works:**
- Full microservices architecture running in Docker
- Authentication and authorization (JWT Bearer tokens)
- Blueprint Service API (create, read operations)
- Sample blueprints ready to use
- All services healthy and communicating

**Current Limitations:**
- In-memory storage (blueprints not persisted)
- 30% production ready (database persistence pending)
- CLI interactive mode only for auth

**Ready For:**
- Blueprint JSON/YAML design and upload
- API development and testing
- Blueprint execution workflow (next phase)
- Integration testing

**Next Phase:** Blueprint execution workflow with Wallet + Register services
