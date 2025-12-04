# Feature Specification: API Gateway

**Feature Branch**: `api-gateway`
**Created**: 2025-12-03
**Status**: 95% Complete
**Input**: Derived from `src/Services/Sorcha.ApiGateway` implementation

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unified API Access (Priority: P0)

As a client application, I need a single entry point to access all Sorcha services so that I don't need to manage multiple service endpoints.

**Why this priority**: Core functionality - the gateway is the primary interface for all clients.

**Independent Test**: Can be tested by calling gateway endpoints and verifying they route to backend services.

**Acceptance Scenarios**:

1. **Given** a request to `/api/blueprints`, **When** routed via gateway, **Then** it is forwarded to Blueprint Service.
2. **Given** a request to `/api/wallets`, **When** routed via gateway, **Then** it is forwarded to Wallet Service.
3. **Given** a request to `/api/registers`, **When** routed via gateway, **Then** it is forwarded to Register Service.
4. **Given** an invalid route, **When** requested, **Then** a 404 Not Found is returned.

---

### User Story 2 - Aggregated Health Check (Priority: P0)

As an operations engineer, I need to see the health of all services from a single endpoint so that I can monitor system status.

**Why this priority**: Essential for operations and monitoring.

**Independent Test**: Can be tested by calling `/api/health` and verifying all service statuses are returned.

**Acceptance Scenarios**:

1. **Given** all services are healthy, **When** GET `/api/health`, **Then** status is "healthy" with 200 OK.
2. **Given** one service is unhealthy, **When** GET `/api/health`, **Then** status is "degraded" with 200 OK.
3. **Given** critical services are down, **When** GET `/api/health`, **Then** status is "unhealthy" with 503.
4. **Given** the health endpoint is called, **Then** individual service statuses are returned in the response.

---

### User Story 3 - OpenAPI Documentation (Priority: P1)

As a developer, I need aggregated API documentation so that I can discover and use all available endpoints.

**Why this priority**: Improves developer experience.

**Independent Test**: Can be tested by accessing `/scalar/v1` and verifying documentation loads.

**Acceptance Scenarios**:

1. **Given** the gateway is running, **When** I access `/scalar/v1`, **Then** Scalar UI displays aggregated documentation.
2. **Given** multiple backend services, **When** I access `/openapi/aggregated.json`, **Then** all service endpoints are combined.
3. **Given** a backend service adds new endpoints, **When** documentation is refreshed, **Then** new endpoints appear.

---

### User Story 4 - Client Download (Priority: P2)

As a developer, I need to download the Blazor client application so that I can run it locally or customize it.

**Why this priority**: Enables developer self-service.

**Independent Test**: Can be tested by calling `/api/client/download` and verifying ZIP is returned.

**Acceptance Scenarios**:

1. **Given** the client exists, **When** GET `/api/client/download`, **Then** a ZIP file is returned.
2. **Given** the download endpoint, **When** called, **Then** the filename includes the current date.
3. **Given** I need instructions, **When** GET `/api/client/instructions`, **Then** markdown instructions are returned.

---

### User Story 5 - CORS Support (Priority: P1)

As a frontend application, I need CORS headers so that browser-based clients can access the API.

**Why this priority**: Required for web client integration.

**Independent Test**: Can be tested by making cross-origin request and verifying headers.

**Acceptance Scenarios**:

1. **Given** a request from any origin, **When** CORS preflight, **Then** appropriate headers are returned.
2. **Given** CORS is configured, **When** request is made, **Then** Access-Control-Allow-Origin is present.

---

### Edge Cases

- What happens when a backend service is temporarily unavailable?
- How does the gateway handle request timeout to slow services?
- What happens when OpenAPI aggregation fails for one service?

**Note**: Per constitution VII (DDD terminology), "Participant" is used instead of "user" where applicable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST route requests to appropriate backend services via YARP
- **FR-002**: System MUST aggregate health status from all backend services
- **FR-003**: System MUST aggregate OpenAPI documentation from all services
- **FR-004**: System MUST provide Scalar UI for API exploration
- **FR-005**: System MUST support CORS for browser-based clients
- **FR-006**: System MUST provide system-wide statistics endpoint
- **FR-007**: System MUST provide client download functionality
- **FR-008**: System MUST display a landing page with service status

### Key Entities

- **AggregatedHealthResponse**: Combined health status from all services
- **SystemStatistics**: Aggregated metrics and statistics
- **ClientInfo**: Information about downloadable client

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Gateway routes requests to backend services in under 50ms overhead
- **SC-002**: Health aggregation completes in under 2 seconds
- **SC-003**: OpenAPI aggregation completes in under 5 seconds
- **SC-004**: Gateway handles 1000+ concurrent connections
- **SC-005**: All public APIs documented with OpenAPI
- **SC-006**: 99.9% uptime for gateway
