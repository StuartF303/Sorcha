# Feature Specification: .NET Aspire Deployment

**Feature Branch**: `deployment-aspire`
**Created**: 2025-12-03
**Status**: Production Ready (95%)
**Input**: Derived from AppHost configuration and .NET Aspire documentation

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Local Development (Priority: P0)

As a developer, I need to run the complete Sorcha platform locally so that I can develop and test features.

**Why this priority**: Primary development workflow.

**Independent Test**: Can be tested by running `dotnet run` from AppHost.

**Acceptance Scenarios**:

1. **Given** the AppHost project, **When** I run `dotnet run`, **Then** all services start.
2. **Given** running services, **When** I access Aspire Dashboard, **Then** I see all service health.
3. **Given** infrastructure resources, **When** started, **Then** PostgreSQL and Redis are available.
4. **Given** development tools, **When** accessed, **Then** pgAdmin and Redis Commander work.

---

### User Story 2 - Service Discovery (Priority: P0)

As a service, I need to discover other services by name so that inter-service calls work without hardcoded URLs.

**Why this priority**: Foundation for microservices communication.

**Independent Test**: Can be tested by making service-to-service calls.

**Acceptance Scenarios**:

1. **Given** registered services, **When** API Gateway calls Blueprint Service, **Then** service discovery resolves the URL.
2. **Given** service references, **When** a service starts, **Then** it waits for dependencies.
3. **Given** a service restart, **When** it reconnects, **Then** service discovery still works.
4. **Given** health checks, **When** a service is unhealthy, **Then** it's removed from discovery.

---

### User Story 3 - Configuration Management (Priority: P0)

As a platform operator, I need centralized configuration so that services have consistent settings.

**Why this priority**: Essential for multi-service configuration.

**Independent Test**: Can be tested by verifying connection strings are injected.

**Acceptance Scenarios**:

1. **Given** PostgreSQL resource, **When** Tenant Service starts, **Then** connection string is injected.
2. **Given** Redis resource, **When** services start, **Then** Redis connection is available.
3. **Given** service endpoints, **When** gateway configures routes, **Then** URLs are correct.
4. **Given** environment variables, **When** services read config, **Then** values are correct.

---

### User Story 4 - Observability (Priority: P1)

As a platform operator, I need observability into all services so that I can monitor and debug issues.

**Why this priority**: Critical for operations and troubleshooting.

**Independent Test**: Can be tested by viewing logs and traces in dashboard.

**Acceptance Scenarios**:

1. **Given** running services, **When** I view dashboard, **Then** I see aggregated logs.
2. **Given** an HTTP request, **When** it spans services, **Then** I see distributed traces.
3. **Given** service metrics, **When** I view dashboard, **Then** I see resource usage.
4. **Given** an error, **When** I view logs, **Then** I can trace the root cause.

---

### User Story 5 - Container Orchestration (Priority: P1)

As a platform operator, I need container management so that services can be deployed as containers.

**Why this priority**: Foundation for Kubernetes and cloud deployment.

**Independent Test**: Can be tested by running with Docker.

**Acceptance Scenarios**:

1. **Given** AppHost, **When** I run with Docker mode, **Then** services run as containers.
2. **Given** containers, **When** they need resources, **Then** volumes are mounted correctly.
3. **Given** container networking, **When** services communicate, **Then** DNS resolution works.
4. **Given** container restart, **When** service crashes, **Then** it's automatically restarted.

---

### Edge Cases

- What happens when Docker is not installed?
- How are port conflicts handled?
- What happens when database migration fails?
- How is data persisted between restarts?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST orchestrate all services using .NET Aspire
- **FR-002**: System MUST provide service discovery for all registered services
- **FR-003**: System MUST inject connection strings for infrastructure resources
- **FR-004**: System MUST provide Aspire Dashboard for monitoring
- **FR-005**: System MUST support distributed tracing across services
- **FR-006**: System MUST aggregate logs from all services
- **FR-007**: System SHOULD support container-based deployment
- **FR-008**: System SHOULD provide development tools (pgAdmin, Redis Commander)
- **FR-009**: System COULD support custom health check endpoints

### Key Entities

- **DistributedApplication**: Aspire orchestration host
- **Project Resource**: Service project reference
- **Container Resource**: Docker container configuration
- **Service Discovery**: Name-based service resolution

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All services start within 60 seconds
- **SC-002**: Service discovery resolution under 100ms
- **SC-003**: Dashboard accessible within 5 seconds of startup
- **SC-004**: Trace correlation works across 5+ services
- **SC-005**: Log aggregation captures all service logs
- **SC-006**: Container restart recovery under 30 seconds
