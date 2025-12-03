# Feature Specification: Sorcha AppHost

**Feature Branch**: `apphost`
**Created**: 2025-12-03
**Status**: Complete (95%)
**Input**: Derived from AppHost.cs and .NET Aspire documentation

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Start Development Environment (Priority: P0)

As a developer, I need to start all Sorcha services with a single command so that I can develop and test locally.

**Why this priority**: Core development experience - developers need easy local environment setup.

**Independent Test**: Can be tested by running `dotnet run` and verifying all services start.

**Acceptance Scenarios**:

1. **Given** the AppHost project, **When** I run `dotnet run`, **Then** all services start with proper dependencies.
2. **Given** running services, **When** I access the Aspire Dashboard, **Then** I can see all service health status.
3. **Given** service dependencies, **When** services start, **Then** they wait for dependencies to be ready.
4. **Given** a service crash, **When** restarted, **Then** it reconnects to infrastructure resources.

---

### User Story 2 - Service Discovery (Priority: P0)

As a service, I need to discover other services automatically so that inter-service communication works without hardcoded URLs.

**Why this priority**: Essential for microservices communication.

**Independent Test**: Can be tested by verifying services can call each other by name.

**Acceptance Scenarios**:

1. **Given** registered services, **When** a service calls another by name, **Then** the call is routed correctly.
2. **Given** API Gateway, **When** it references backend services, **Then** YARP routes are configured automatically.
3. **Given** service health checks, **When** a service is unhealthy, **Then** traffic is not routed to it.
4. **Given** multiple service replicas, **When** calling a service, **Then** load balancing occurs.

---

### User Story 3 - Infrastructure Resources (Priority: P0)

As a platform operator, I need infrastructure resources (PostgreSQL, Redis) provisioned automatically so that services have their dependencies.

**Why this priority**: Services depend on infrastructure for persistence and caching.

**Independent Test**: Can be tested by verifying database and cache connections work.

**Acceptance Scenarios**:

1. **Given** AppHost configuration, **When** started, **Then** PostgreSQL container is running with tenant-db.
2. **Given** AppHost configuration, **When** started, **Then** Redis container is running.
3. **Given** services with Redis reference, **When** started, **Then** they can connect to Redis.
4. **Given** Tenant Service, **When** started, **Then** it can connect to PostgreSQL tenant-db.

---

### User Story 4 - Development Tools (Priority: P1)

As a developer, I need access to development tools so that I can debug and monitor infrastructure.

**Why this priority**: Improves developer experience and debugging capabilities.

**Independent Test**: Can be tested by accessing pgAdmin and Redis Commander.

**Acceptance Scenarios**:

1. **Given** PostgreSQL, **When** AppHost runs, **Then** pgAdmin is available for database management.
2. **Given** Redis, **When** AppHost runs, **Then** Redis Commander is available for cache inspection.
3. **Given** Aspire Dashboard, **When** accessed, **Then** I can view logs, traces, and metrics.
4. **Given** running services, **When** I view traces, **Then** distributed traces span multiple services.

---

### User Story 5 - External Endpoints (Priority: P0)

As a client application, I need to access the platform through well-defined external endpoints.

**Why this priority**: External access is required for the Blueprint Designer and API consumers.

**Independent Test**: Can be tested by verifying only gateway and designer are externally accessible.

**Acceptance Scenarios**:

1. **Given** API Gateway, **When** deployed, **Then** it has external HTTP endpoints exposed.
2. **Given** Blazor Client, **When** deployed, **Then** it has external HTTP endpoints exposed.
3. **Given** backend services, **When** deployed, **Then** they are NOT directly accessible externally.
4. **Given** internal services, **When** accessed, **Then** traffic must go through API Gateway.

---

### Edge Cases

- What happens when infrastructure containers fail to start?
- How does the system handle resource contention during parallel startup?
- What happens when database migrations fail during startup?
- How are secrets managed in development vs production?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST orchestrate all platform services using .NET Aspire
- **FR-002**: System MUST provision PostgreSQL for tenant data
- **FR-003**: System MUST provision Redis for distributed caching
- **FR-004**: System MUST configure service discovery automatically
- **FR-005**: System MUST expose only API Gateway and Blazor Client externally
- **FR-006**: System MUST provide development tools (pgAdmin, Redis Commander)
- **FR-007**: System MUST configure service references and dependencies
- **FR-008**: System SHOULD support environment-specific configuration
- **FR-009**: System SHOULD provide health checks for all resources
- **FR-010**: System COULD support container-based deployment

### Key Entities

- **DistributedApplication**: The Aspire orchestration container
- **Service Project**: Individual microservice deployment unit
- **Resource**: Infrastructure component (database, cache)
- **External Endpoint**: Publicly accessible URL

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All services start within 60 seconds
- **SC-002**: Service discovery works for all registered services
- **SC-003**: Database connections established within 10 seconds
- **SC-004**: Redis connections established within 5 seconds
- **SC-005**: Aspire Dashboard accessible and showing all services
- **SC-006**: Development tools (pgAdmin, Redis Commander) accessible
- **SC-007**: External endpoints only expose gateway and client
- **SC-008**: Zero manual configuration required for local development
