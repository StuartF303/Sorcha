# Tasks: Sorcha AppHost

**Feature Branch**: `apphost`
**Created**: 2025-12-03
**Status**: 95% Complete

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 12 |
| In Progress | 0 |
| Pending | 2 |
| **Total** | **14** |

---

## Phase 1: Infrastructure Setup

### APP-001: Create AppHost Project
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Create .NET Aspire AppHost project.

**Acceptance Criteria**:
- [x] Project created with Aspire SDK
- [x] Solution integration
- [x] Basic builder setup

---

### APP-002: Add PostgreSQL Resource
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-001

**Description**: Configure PostgreSQL with tenant database.

**Acceptance Criteria**:
- [x] PostgreSQL container configured
- [x] tenant-db database created
- [x] pgAdmin development tool added

---

### APP-003: Add Redis Resource
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-001

**Description**: Configure Redis for distributed caching.

**Acceptance Criteria**:
- [x] Redis container configured
- [x] Redis Commander development tool added
- [x] Connection string configured

---

## Phase 2: Service Registration

### APP-004: Register Tenant Service
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-002, APP-003

**Description**: Add Tenant Service with dependencies.

**Acceptance Criteria**:
- [x] Project reference added
- [x] PostgreSQL reference configured
- [x] Redis reference configured

---

### APP-005: Register Blueprint Service
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-003

**Description**: Add Blueprint Service with dependencies.

**Acceptance Criteria**:
- [x] Project reference added
- [x] Redis reference configured
- [x] Internal only (no external endpoints)

---

### APP-006: Register Wallet Service
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-003

**Description**: Add Wallet Service with dependencies.

**Acceptance Criteria**:
- [x] Project reference added
- [x] Redis reference configured
- [x] Internal only (no external endpoints)

---

### APP-007: Register Register Service
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-003

**Description**: Add Register Service with dependencies.

**Acceptance Criteria**:
- [x] Project reference added
- [x] Redis reference configured
- [x] Internal only (no external endpoints)

---

### APP-008: Register Peer Service
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-003

**Description**: Add Peer Service with dependencies.

**Acceptance Criteria**:
- [x] Project reference added
- [x] Redis reference configured
- [x] Internal only (no external endpoints)

---

### APP-009: Register API Gateway
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: APP-004 through APP-008

**Description**: Add API Gateway with all service references.

**Acceptance Criteria**:
- [x] Project reference added
- [x] All backend service references configured
- [x] Redis reference configured
- [x] External HTTP endpoints enabled

---

### APP-010: Register Blazor Client
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: APP-001

**Description**: Add Blazor WebAssembly client.

**Acceptance Criteria**:
- [x] Project reference added
- [x] External HTTP endpoints enabled
- [x] Static client configuration

---

## Phase 3: Development Experience

### APP-011: Aspire Dashboard Configuration
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: APP-009

**Description**: Configure Aspire Dashboard for monitoring.

**Acceptance Criteria**:
- [x] Dashboard accessible at localhost:15888
- [x] All services visible
- [x] Logs aggregated
- [x] Traces enabled

---

### APP-012: Health Check Integration
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: APP-009

**Description**: Configure health checks for all services.

**Acceptance Criteria**:
- [x] Service health visible in dashboard
- [x] Database health checks
- [x] Redis health checks

---

## Phase 4: Future Enhancements

### APP-013: Add Validator Service
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 2 hours
- **Assignee**: TBD
- **Dependencies**: Validator Service implementation

**Description**: Add Validator Service when implemented.

**Acceptance Criteria**:
- [ ] Project reference added
- [ ] Redis reference configured
- [ ] Service dependencies configured

---

### APP-014: Production Configuration
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: APP-009

**Description**: Add production deployment configuration.

**Acceptance Criteria**:
- [ ] Azure deployment manifest
- [ ] Kubernetes deployment support
- [ ] Secret management configuration
- [ ] HTTPS certificate configuration

---

## Notes

- AppHost is the central orchestration point for all Sorcha services
- Only API Gateway and Blazor Client have external endpoints
- Development tools (pgAdmin, Redis Commander) are for local development only
- Validator Service will be added when blockchain consensus is implemented
