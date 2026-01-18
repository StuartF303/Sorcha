# Production Readiness Tasks

**Goal:** Critical security, authentication, and operational tasks required for production deployment
**Duration:** 2-3 weeks (parallel with MVD demo preparation)
**Total Tasks:** 10
**Completion:** 0% (newly identified during audit)

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)

---

**⚠️ CRITICAL:** These tasks were NOT tracked in previous versions of this document but are ESSENTIAL for production deployment.

---

## Authentication & Authorization (P0 - BLOCKERS)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| AUTH-001 | Implement Tenant Service (JWT + RBAC + Delegation) | P0 | 80h | 🚧 80% Complete | - |
| AUTH-002 | Integrate services with Tenant Service authentication | P0 | 24h | ✅ Complete | 2025-12-12 |
| AUTH-003 | Deploy PostgreSQL + Redis for Tenant Service | P0 | 8h | ✅ Complete | 2025-12-12 |
| AUTH-004 | Bootstrap seed scripts (admin + service principals) | P0 | 12h | ✅ Complete | 2025-12-12 |
| AUTH-005 | Production deployment with Azure AD/B2C | P1 | 16h | 📋 Not Started | - |

**Rationale:** Services currently have NO authentication/authorization. All APIs are completely open!

### AUTH-001 Status Details

✅ **Specification 100% complete** ([View Spec](../specs/sorcha-tenant-service.md))
- ✅ User authentication with JWT tokens (60 min lifetime)
- ✅ Service-to-service authentication (OAuth2 client credentials, 8 hour tokens)
- ✅ Delegation tokens for Blueprint→Wallet→Register flows
- ✅ Token refresh flow (24 hour refresh token lifetime)
- ✅ Hybrid token validation (local JWT + optional introspection)
- ✅ Token revocation with Redis-backed store
- ✅ Multi-tenant organization management
- ✅ 9 authorization policies (RBAC)
- ✅ 30+ REST API endpoints documented
- ✅ Stateless horizontal scaling architecture
- ✅ 99.5% SLA target with degraded operation modes
- 🚧 Implementation 80% complete (core features implemented)
- 📋 PostgreSQL repository pending
- 📋 Production deployment pending

### AUTH-002 Status Details

✅ **Complete (2025-12-12)**
- ✅ Blueprint Service: JWT Bearer authentication with authorization policies (CanManageBlueprints, CanExecuteBlueprints, CanPublishBlueprints, RequireService)
- ✅ Wallet Service: JWT Bearer authentication with authorization policies (CanManageWallets, CanUseWallet, RequireService)
- ✅ Register Service: JWT Bearer authentication with authorization policies (CanManageRegisters, CanSubmitTransactions, CanReadTransactions, RequireService)
- ✅ Configuration: Shared JWT settings template (appsettings.jwt.json)
- ✅ Documentation: Complete authentication setup guide (docs/AUTHENTICATION-SETUP.md)
- 📋 Peer Service authentication pending (service not yet implemented)
- 📋 API Gateway JWT validation pending

### AUTH-003 Status Details

✅ **Complete (2025-12-12)** - Infrastructure deployment complete
- ✅ PostgreSQL 17 container configured and tested
- ✅ Redis 8 container configured and tested
- ✅ MongoDB 8 container configured and tested
- ✅ Docker Compose infrastructure-only file created (`docker-compose.infrastructure.yml`)
- ✅ Database initialization script (`scripts/init-databases.sql`)
- ✅ Connection strings aligned between Docker Compose and appsettings
- ✅ Health checks configured for all infrastructure services
- ✅ Comprehensive infrastructure setup guide created (`docs/INFRASTRUCTURE-SETUP.md`)
- ✅ Data persistence with Docker volumes
- ⚠️ **Note:** Windows/Docker Desktop may require `host.docker.internal` for host connectivity

### AUTH-004 Status Details

✅ **Complete (2025-12-12)** - Automatic database seeding implemented
- ✅ DatabaseInitializer enhanced with service principal seeding
- ✅ Default organization created: "Sorcha Local" (subdomain: `sorcha-local`)
- ✅ Default admin user: `admin@sorcha.local` / `Dev_Pass_2025!`
- ✅ Service principals created: Blueprint, Wallet, Register, Peer services
- ✅ Well-known GUIDs for consistent testing
- ✅ Client secrets generated and logged on first startup
- ✅ Configurable via appsettings ("Seed:*" configuration keys)
- ✅ Documentation added to scripts/README.md
- ⚠️ **Action Required:** Copy service principal secrets from Tenant Service logs on first startup

---

## Security Hardening (P0-P1)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| SEC-001 | HTTPS enforcement and certificate management | P0 | 4h | 🚧 Partial | - |
| SEC-002 | API rate limiting and throttling | P1 | 8h | 📋 Not Started | - |
| SEC-003 | Input validation hardening (OWASP compliance) | P1 | 12h | 📋 Not Started | - |
| SEC-004 | Security headers (CSP, HSTS, X-Frame-Options) | P1 | 4h | ✅ Complete | 2025-12-09 |

**Related:** BP-8.2 Security hardening task (promoted from P1 in Phase 1)

---

## Operations & Monitoring (P1)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| OPS-001 | Production logging infrastructure (Serilog/ELK) | P1 | 8h | 🚧 Partial | - |
| OPS-002 | Health check endpoints (deep checks) | P1 | 4h | ✅ Complete | - |
| OPS-003 | Deployment documentation and runbooks | P1 | 8h | 📋 Not Started | - |

**Note:** OPS-002 already implemented via .NET Aspire health checks

---

## Data Management (P1)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| DATA-001 | Database backup and restore strategy | P1 | 6h | 📋 Not Started | - |
| DATA-002 | Database migration scripts and versioning | P1 | 8h | 📋 Not Started | - |

**Related:** ENH-WS-1, REG-003, ENH-BP-1 (database persistence implementations)

---

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)
