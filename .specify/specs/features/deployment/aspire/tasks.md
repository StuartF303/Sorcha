# Tasks: .NET Aspire Deployment

**Feature Branch**: `deployment-aspire`
**Created**: 2025-12-03
**Status**: 95% Complete

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 11 |
| In Progress | 0 |
| Pending | 2 |
| **Total** | **13** |

---

## Phase 1: Foundation

### ASP-001: Create AppHost Project
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Create .NET Aspire AppHost project.

**Acceptance Criteria**:
- [x] Project created with Aspire SDK
- [x] Solution reference added
- [x] Basic Program.cs setup

---

### ASP-002: Configure PostgreSQL
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: ASP-001

**Description**: Add PostgreSQL resource with tenant database.

**Acceptance Criteria**:
- [x] PostgreSQL resource configured
- [x] Database "sorcha_tenant" created
- [x] pgAdmin development tool added

---

### ASP-003: Configure Redis
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: ASP-001

**Description**: Add Redis resource for caching.

**Acceptance Criteria**:
- [x] Redis resource configured
- [x] Redis Commander tool added
- [x] Connection string injection

---

## Phase 2: Service Registration

### ASP-004: Register All Services
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: ASP-002, ASP-003

**Description**: Register all platform services.

**Acceptance Criteria**:
- [x] Tenant Service registered
- [x] Blueprint Service registered
- [x] Wallet Service registered
- [x] Register Service registered
- [x] Peer Service registered
- [x] API Gateway registered
- [x] Blazor Client registered

---

### ASP-005: Configure Service References
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: ASP-004

**Description**: Set up service dependencies.

**Acceptance Criteria**:
- [x] Tenant Service → PostgreSQL, Redis
- [x] All services → Redis
- [x] API Gateway → All backend services

---

### ASP-006: External Endpoints
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 1 hour
- **Assignee**: AI Assistant
- **Dependencies**: ASP-004

**Description**: Configure external HTTP endpoints.

**Acceptance Criteria**:
- [x] API Gateway external
- [x] Blazor Client external
- [x] Internal services not external

---

## Phase 3: Observability

### ASP-007: Aspire Dashboard
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: ASP-004

**Description**: Configure Aspire Dashboard.

**Acceptance Criteria**:
- [x] Dashboard accessible at port 15888
- [x] All services visible
- [x] Health status displayed

---

### ASP-008: Log Aggregation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: ASP-007

**Description**: Configure centralized logging.

**Acceptance Criteria**:
- [x] Service logs aggregated
- [x] Log filtering by service
- [x] Log level support

---

### ASP-009: Distributed Tracing
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 3 hours
- **Assignee**: AI Assistant
- **Dependencies**: ASP-007

**Description**: Enable distributed tracing.

**Acceptance Criteria**:
- [x] OpenTelemetry configured
- [x] Trace correlation works
- [x] Spans visible in dashboard

---

### ASP-010: Health Checks
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: ASP-004

**Description**: Configure service health checks.

**Acceptance Criteria**:
- [x] Health endpoints registered
- [x] Database health checks
- [x] Redis health checks
- [x] Dashboard shows health

---

## Phase 4: Service Defaults

### ASP-011: ServiceDefaults Library
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: ASP-001

**Description**: Create shared service configuration.

**Acceptance Criteria**:
- [x] OpenTelemetry setup
- [x] Health check setup
- [x] Service discovery client
- [x] Resilience policies

---

## Phase 5: Future Enhancements

### ASP-012: Database Migrations
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: ASP-002

**Description**: Automatic EF Core migrations on startup.

**Acceptance Criteria**:
- [ ] Migration on service start
- [ ] Idempotent migrations
- [ ] Error handling
- [ ] Rollback support

---

### ASP-013: Production Configuration
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: ASP-006

**Description**: Production environment configuration.

**Acceptance Criteria**:
- [ ] Environment-specific settings
- [ ] Secret management
- [ ] HTTPS configuration
- [ ] Resource limits

---

## Notes

- .NET Aspire is the primary development environment
- All services use ServiceDefaults for consistent configuration
- Dashboard provides comprehensive observability
- Production deployment will use Kubernetes manifests
