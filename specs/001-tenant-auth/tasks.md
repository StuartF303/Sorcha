# Tasks: Tenant Service Authentication & Multi-Organization Identity Management

**Input**: Design documents from `/specs/001-tenant-auth/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Comprehensive test coverage targeting >85% per constitutional requirements. Tests are mandatory for this security-critical service.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5, US6)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create Sorcha.Tenant.Service project in src/Services/Sorcha.Tenant.Service/
- [x] T002 Create Sorcha.Tenant.Models project in src/Common/Sorcha.Tenant.Models/
- [x] T003 [P] Create Sorcha.Tenant.Service.Tests project in tests/Sorcha.Tenant.Service.Tests/
- [x] T004 [P] Create Sorcha.Tenant.Service.IntegrationTests project in tests/Sorcha.Tenant.Service.IntegrationTests/
- [x] T005 [P] Create Sorcha.Tenant.Service.PerformanceTests project in tests/Sorcha.Tenant.Service.PerformanceTests/
- [x] T006 Add NuGet packages to Sorcha.Tenant.Service (Microsoft.AspNetCore.Authentication.OpenIdConnect, System.IdentityModel.Tokens.Jwt, Fido2NetLib, EF Core, Npgsql, StackExchange.Redis, Aspire.Hosting, Scalar.AspNetCore)
- [x] T007 Add NuGet packages to test projects (xUnit, FluentAssertions, Moq, Testcontainers, NBomber)
- [x] T008 [P] Create directory structure per plan.md (Endpoints/, Models/, Data/, Services/, Extensions/)
- [x] T009 [P] Configure appsettings.json with ConnectionStrings, Redis, JwtSettings, Fido2 sections
- [x] T010 [P] Configure appsettings.Development.json with localhost database and Redis
- [x] T011 [P] Create README.md in src/Services/Sorcha.Tenant.Service/ with service overview
- [x] T012 Add Tenant Service to Sorcha.AppHost in src/Apps/Sorcha.AppHost/Program.cs
- [x] T013 [P] Configure Serilog with Seq sink and correlation IDs in Program.cs
- [ ] T014 [P] Configure OpenTelemetry with Zipkin exporter in Program.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Shared Domain Models

- [x] T015 [P] Create Organization.cs in src/Services/Sorcha.Tenant.Service/Models/
- [x] T016 [P] Create IdentityProviderConfiguration.cs in src/Services/Sorcha.Tenant.Service/Models/
- [x] T017 [P] Create UserIdentity.cs in src/Services/Sorcha.Tenant.Service/Models/
- [x] T018 [P] Create PublicIdentity.cs in src/Services/Sorcha.Tenant.Service/Models/
- [x] T019 [P] Create OrganizationPermissionConfiguration.cs in src/Services/Sorcha.Tenant.Service/Models/
- [x] T020 [P] Create ServicePrincipal.cs in src/Services/Sorcha.Tenant.Service/Models/
- [x] T021 [P] Create AuditLogEntry.cs in src/Services/Sorcha.Tenant.Service/Models/

### Shared DTOs for Cross-Service Use

- [x] T022 [P] Create TokenClaims.cs in src/Common/Sorcha.Tenant.Models/
- [x] T023 [P] Create OrganizationContext.cs in src/Common/Sorcha.Tenant.Models/
- [x] T024 [P] Create PermissionFlags.cs in src/Common/Sorcha.Tenant.Models/

### Database Infrastructure

- [x] T025 Create TenantDbContext.cs in src/Services/Sorcha.Tenant.Service/Data/ with multi-tenant schema support
- [x] T026 Configure entity relationships and indexes in TenantDbContext.OnModelCreating()
- [ ] T027 Create initial EF Core migration for public schema (Organizations, IdentityProviderConfigurations, PublicIdentities, ServicePrincipals)
- [ ] T028 Create schema creation script for per-tenant tables (UserIdentities, OrganizationPermissionConfigurations, AuditLogEntries)
- [x] T029 [P] Create IOrganizationRepository.cs interface in src/Services/Sorcha.Tenant.Service/Data/Repositories/
- [x] T030 [P] Create OrganizationRepository.cs implementation in src/Services/Sorcha.Tenant.Service/Data/Repositories/
- [x] T031 [P] Create IIdentityRepository.cs interface in src/Services/Sorcha.Tenant.Service/Data/Repositories/
- [x] T032 [P] Create IdentityRepository.cs implementation in src/Services/Sorcha.Tenant.Service/Data/Repositories/

### Core Services (Shared Infrastructure)

- [ ] T033 [P] Create ITenantProvider.cs interface and implementation in src/Services/Sorcha.Tenant.Service/Services/ for schema resolution
- [ ] T034 Create ServiceCollectionExtensions.cs in src/Services/Sorcha.Tenant.Service/Extensions/ with AddTenantServices() method
- [ ] T035 Create AuthenticationExtensions.cs in src/Services/Sorcha.Tenant.Service/Extensions/ with AddTenantAuthentication() method
- [ ] T036 Configure dependency injection in Program.cs (DbContext, repositories, services)
- [x] T037 [P] Create health check endpoints (/health/live, /health/ready) in Program.cs
- [x] T038 [P] Configure .NET 10 OpenAPI with Scalar in Program.cs (routes: /openapi/v1.json, /scalar)

### Redis Infrastructure

- [ ] T039 Create ITokenRevocationService.cs interface in src/Services/Sorcha.Tenant.Service/Services/
- [ ] T040 Create TokenRevocationService.cs implementation in src/Services/Sorcha.Tenant.Service/Services/ with Redis integration
- [ ] T041 [P] Configure Redis connection multiplexer in Program.cs with circuit breaker

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Organization Administrator Configures External IDP (Priority: P1) 🎯

**Goal**: Enable organization administrators to configure external IDP (Azure Entra, AWS Cognito, OIDC) with custom branding, test authentication, and receive JWT tokens.

**Independent Test**: Configure test organization with Azure Entra, authenticate user, verify JWT token issued with correct claims.

### Unit Tests for User Story 1

- [ ] T042 [P] [US1] Create OrganizationServiceTests.cs in tests/Sorcha.Tenant.Service.Tests/Services/ with tests for CreateOrganization, ValidateIdpConfiguration
- [ ] T043 [P] [US1] Create OrganizationEndpointsTests.cs in tests/Sorcha.Tenant.Service.Tests/Endpoints/ with tests for POST /organizations, PUT /organizations/{id}/idp

### Integration Tests for User Story 1

- [ ] T044 [P] [US1] Create OrganizationManagementTests.cs in tests/Sorcha.Tenant.Service.IntegrationTests/ with Testcontainers for end-to-end organization creation and IDP configuration

### Implementation for User Story 1

- [ ] T045 [US1] Create IOrganizationService.cs interface in src/Services/Sorcha.Tenant.Service/Services/ with methods: CreateOrganization, UpdateOrganization, ConfigureIdp, ValidateIdpConfiguration
- [ ] T046 [US1] Implement OrganizationService.cs in src/Services/Sorcha.Tenant.Service/Services/ with schema creation logic for new organizations
- [ ] T047 [US1] Create OrganizationEndpoints.cs in src/Services/Sorcha.Tenant.Service/Endpoints/ with endpoints: POST /api/admin/organizations, GET /api/admin/organizations/{id}, PATCH /api/admin/organizations/{id}
- [ ] T048 [US1] Create IDP configuration endpoints in OrganizationEndpoints.cs: PUT /api/admin/organizations/{id}/idp, GET /api/admin/organizations/{id}/idp, DELETE /api/admin/organizations/{id}/idp
- [ ] T049 [US1] Add OpenAPI documentation with .WithOpenApi() for all Organization endpoints
- [ ] T050 [US1] Add XML documentation comments to OrganizationService methods
- [ ] T051 [US1] Implement IDP configuration validation in OrganizationService (test OIDC discovery, validate client credentials)
- [ ] T052 [US1] Add encryption for IDP client secrets using Sorcha.Cryptography library (AES-256-GCM)
- [ ] T053 [US1] Add branding configuration support (logo URL, colors) to Organization model
- [ ] T054 [US1] Add audit logging for organization creation and IDP configuration changes
- [ ] T055 [US1] Seed initial service principals (Blueprint, Wallet, Register, Peer) in migration

**Checkpoint**: Organization administration complete - administrators can create orgs and configure external IDPs

---

## Phase 4: User Story 2 - User Authenticates with Organization Credentials (Priority: P1) 🎯 MVP

**Goal**: Users can sign in with their organization's IDP (Azure Entra, AWS, OIDC), receive JWT tokens, validate tokens, refresh tokens, and logout.

**Independent Test**: Login with organization credentials, receive token, use token to access protected endpoint, refresh token, logout and verify token revoked.

### Unit Tests for User Story 2

- [ ] T056 [P] [US2] Create ExternalIdpServiceTests.cs in tests/Sorcha.Tenant.Service.Tests/Services/ with tests for InitiateLogin, HandleCallback, ExchangeCodeForToken
- [ ] T057 [P] [US2] Create TokenServiceTests.cs in tests/Sorcha.Tenant.Service.Tests/Services/ with tests for IssueToken, ValidateToken, RefreshToken, RevokeToken
- [ ] T058 [P] [US2] Create AuthenticationEndpointsTests.cs in tests/Sorcha.Tenant.Service.Tests/Endpoints/ with tests for /login, /callback, /logout

### Integration Tests for User Story 2

- [ ] T059 [P] [US2] Create AuthenticationFlowTests.cs in tests/Sorcha.Tenant.Service.IntegrationTests/ with end-to-end OAuth2 flow simulation (mock IDP)
- [ ] T060 [P] [US2] Create TokenManagementTests.cs in tests/Sorcha.Tenant.Service.IntegrationTests/ with token lifecycle tests (issue, validate, refresh, revoke)

### Implementation for User Story 2

- [ ] T061 [US2] Create IExternalIdpService.cs interface in src/Services/Sorcha.Tenant.Service/Services/ with methods: InitiateLogin, HandleCallback, ExchangeCodeForToken
- [ ] T062 [US2] Implement ExternalIdpService.cs in src/Services/Sorcha.Tenant.Service/Services/ with dynamic authentication scheme registration using IAuthenticationSchemeProvider
- [ ] T063 [US2] Create ITokenService.cs interface in src/Services/Sorcha.Tenant.Service/Services/ with methods: IssueToken, ValidateToken, RefreshToken, RevokeToken, GenerateJwks
- [ ] T064 [US2] Implement TokenService.cs in src/Services/Sorcha.Tenant.Service/Services/ with RS256 JWT signing, JWKS generation, token validation
- [ ] T065 [US2] Generate RSA key pair (4096-bit) on startup in TokenService, store private key encrypted (Azure Key Vault for production, DPAPI for development)
- [ ] T066 [US2] Create AuthenticationEndpoints.cs in src/Services/Sorcha.Tenant.Service/Endpoints/ with endpoints: GET /api/auth/login, GET /api/auth/callback, POST /api/auth/logout
- [ ] T067 [US2] Create TokenEndpoints.cs in src/Services/Sorcha.Tenant.Service/Endpoints/ with endpoints: POST /api/auth/token/refresh, POST /api/auth/token/validate, GET /.well-known/jwks.json
- [ ] T068 [US2] Add OpenAPI documentation for all Authentication and Token endpoints
- [ ] T069 [US2] Implement PKCE (Proof Key for Code Exchange) support in OAuth2 flow
- [ ] T070 [US2] Add state and nonce validation to prevent CSRF and replay attacks
- [ ] T071 [US2] Implement token claims builder (sub, iss, aud, exp, iat, jti, org_id, roles, permitted_blockchains, can_create_blockchain, can_publish_blueprint)
- [ ] T072 [US2] Add token refresh logic with 24-hour refresh token lifetime
- [ ] T073 [US2] Integrate TokenRevocationService for logout (add JTI to Redis blacklist with TTL)
- [ ] T074 [US2] Add audit logging for authentication events (login, logout, token issued, token revoked)
- [ ] T075 [US2] Implement rate limiting for failed auth attempts (5 per minute per user) using Redis counters
- [ ] T076 [US2] Add correlation ID middleware for distributed tracing
- [ ] T077 [US2] Add clock skew tolerance (±5 minutes) for token validation

**Checkpoint**: OAuth2/OIDC authentication complete - users can login, receive tokens, and access services

---

## Phase 5: User Story 6 - Service-to-Service Authentication (Priority: P1) 🎯 MVP

**Goal**: Enable services (Blueprint, Wallet, Register) to authenticate with client credentials and support delegated authority (service acting on behalf of user).

**Independent Test**: Blueprint Service requests service token, calls Wallet Service with service token + user context, Wallet validates both and applies correct permissions.

### Unit Tests for User Story 6

- [ ] T078 [P] [US6] Create ServiceAuthenticationTests.cs in tests/Sorcha.Tenant.Service.Tests/Services/ with tests for client credentials flow, delegated authority validation
- [ ] T079 [P] [US6] Create ServiceTokenEndpointsTests.cs in tests/Sorcha.Tenant.Service.Tests/Endpoints/ with tests for POST /token/service

### Integration Tests for User Story 6

- [ ] T080 [P] [US6] Create ServiceToServiceTests.cs in tests/Sorcha.Tenant.Service.IntegrationTests/ with end-to-end service authentication and delegated authority scenarios

### Implementation for User Story 6

- [ ] T081 [US6] Add client credentials flow to TokenService (validate service client_id and client_secret, issue service token with 8-hour lifetime)
- [ ] T082 [US6] Create service token endpoint in TokenEndpoints.cs: POST /api/auth/token/service
- [ ] T083 [US6] Add service principal validation in TokenService (check ServicePrincipal table, validate scopes)
- [ ] T084 [US6] Implement service token claims (sub: service-id, token_type: service, scopes: array of permissions)
- [ ] T085 [US6] Add delegated authority support (validate user context + service token together)
- [ ] T086 [US6] Add OpenAPI documentation for service token endpoint with client_credentials grant type
- [ ] T087 [US6] Add audit logging for service token issuance and validation
- [ ] T088 [US6] Implement TLS 1.3 enforcement for service-to-service communication (configuration)

**Checkpoint**: Service-to-service authentication complete - services can authenticate and act on behalf of users

---

## Phase 6: User Story 3 - Public User Authenticates with PassKey (Priority: P2)

**Goal**: Public users can register PassKey (FIDO2/WebAuthn), authenticate with biometric/security key, and receive JWT token with public user permissions.

**Independent Test**: Register PassKey, authenticate with PassKey, receive token, access public blockchain data.

### Unit Tests for User Story 3

- [ ] T089 [P] [US3] Create PassKeyServiceTests.cs in tests/Sorcha.Tenant.Service.Tests/Services/ with tests for GenerateRegistrationOptions, VerifyRegistrationResponse, GenerateAuthenticationOptions, VerifyAuthenticationResponse
- [ ] T090 [P] [US3] Create PassKeyEndpointsTests.cs in tests/Sorcha.Tenant.Service.Tests/Endpoints/ with tests for /passkey/register-options, /passkey/register, /passkey/authentication-options, /passkey/authenticate

### Integration Tests for User Story 3

- [ ] T091 [P] [US3] Create PassKeyAuthenticationTests.cs in tests/Sorcha.Tenant.Service.IntegrationTests/ with end-to-end PassKey registration and authentication flows

### Implementation for User Story 3

- [ ] T092 [US3] Create IPassKeyService.cs interface in src/Services/Sorcha.Tenant.Service/Services/ with methods: GenerateRegistrationOptions, VerifyRegistrationResponse, GenerateAuthenticationOptions, VerifyAuthenticationResponse
- [ ] T093 [US3] Implement PassKeyService.cs using Fido2NetLib for WebAuthn server implementation
- [ ] T094 [US3] Create PassKey endpoints in AuthenticationEndpoints.cs: POST /api/auth/passkey/register-options, POST /api/auth/passkey/register, POST /api/auth/passkey/authentication-options, POST /api/auth/passkey/authenticate
- [ ] T095 [US3] Configure FIDO2 settings in appsettings.json (RP ID, RP name, origins)
- [ ] T096 [US3] Implement challenge generation and storage in Redis (5-minute TTL)
- [ ] T097 [US3] Add attestation verification in PassKeyService (validate origin, RP ID hash, signature)
- [ ] T098 [US3] Store credential ID, public key (COSE format), counter in PublicIdentity table
- [ ] T099 [US3] Implement signature counter increment and verification (detect cloned authenticators)
- [ ] T100 [US3] Issue JWT token for PassKey-authenticated users (token_type: user, org_id: null, permitted_blockchains: only public)
- [ ] T101 [US3] Add OpenAPI documentation for all PassKey endpoints with WebAuthn request/response schemas
- [ ] T102 [US3] Add audit logging for PassKey registration and authentication events
- [ ] T103 [US3] Add support for multiple PassKeys per user (backup authenticators)

**Checkpoint**: PassKey authentication complete - public users can access platform without organization affiliation

---

## Phase 7: User Story 4 - Organization Administrator Manages User Permissions (Priority: P2)

**Goal**: Administrators configure blockchain access, blockchain creation permissions, and blueprint publishing permissions for their organization members.

**Independent Test**: Set blockchain restrictions, attempt user action, verify permission denied. Update permissions, verify immediately reflected in new tokens.

### Unit Tests for User Story 4

- [ ] T104 [P] [US4] Create PermissionServiceTests.cs in tests/Sorcha.Tenant.Service.Tests/Services/ with tests for EvaluatePermissions, BuildTokenClaims, UpdateOrganizationPermissions
- [ ] T105 [P] [US4] Create PermissionEndpointsTests.cs in tests/Sorcha.Tenant.Service.Tests/Endpoints/ with tests for PUT /organizations/{id}/permissions, GET /organizations/{id}/permissions

### Integration Tests for User Story 4

- [ ] T106 [P] [US4] Create PermissionEnforcementTests.cs in tests/Sorcha.Tenant.Service.IntegrationTests/ with permission update and token refresh scenarios

### Implementation for User Story 4

- [ ] T107 [US4] Create IPermissionService.cs interface in src/Services/Sorcha.Tenant.Service/Services/ with methods: EvaluatePermissions, BuildTokenClaims, UpdateOrganizationPermissions
- [ ] T108 [US4] Implement PermissionService.cs in src/Services/Sorcha.Tenant.Service/Services/ with permission evaluation logic
- [ ] T109 [US4] Create permission management endpoints in OrganizationEndpoints.cs: PUT /api/admin/organizations/{id}/permissions, GET /api/admin/organizations/{id}/permissions
- [ ] T110 [US4] Integrate PermissionService into TokenService for claims generation (read OrganizationPermissionConfiguration and include in JWT)
- [ ] T111 [US4] Add permission change event handling (update user's next token refresh to include new permissions)
- [ ] T112 [US4] Add OpenAPI documentation for permission management endpoints
- [ ] T113 [US4] Add audit logging for permission configuration changes
- [ ] T114 [US4] Implement default permissions (CanCreateBlockchain: false, CanPublishBlueprint: false, ApprovedBlockchains: empty)

**Checkpoint**: Permission management complete - administrators can control user capabilities

---

## Phase 8: User Story 5 - Organization Auditor Reviews Activity (Priority: P3)

**Goal**: Auditors can view authentication logs, audit trails, and organizational settings in read-only mode.

**Independent Test**: Login as auditor, view logs and settings, verify modification operations blocked.

### Unit Tests for User Story 5

- [ ] T115 [P] [US5] Create AuditServiceTests.cs in tests/Sorcha.Tenant.Service.Tests/Services/ with tests for QueryAuditLogs, FilterByEventType, FilterByDateRange
- [ ] T116 [P] [US5] Create AuditEndpointsTests.cs in tests/Sorcha.Tenant.Service.Tests/Endpoints/ with tests for GET /audit/logs with various filters

### Integration Tests for User Story 5

- [ ] T117 [P] [US5] Create AuditAccessTests.cs in tests/Sorcha.Tenant.Service.IntegrationTests/ with auditor role enforcement and read-only validation

### Implementation for User Story 5

- [ ] T118 [US5] Create IAuditService.cs interface in src/Services/Sorcha.Tenant.Service/Services/ with methods: QueryAuditLogs, GetAuditLogById
- [ ] T119 [US5] Implement AuditService.cs in src/Services/Sorcha.Tenant.Service/Services/ with query filtering and pagination
- [ ] T120 [US5] Create AuditEndpoints.cs in src/Services/Sorcha.Tenant.Service/Endpoints/ with endpoint: GET /api/audit/logs (query params: event_type, identity_id, start_date, end_date, success, page, page_size)
- [ ] T121 [US5] Add role-based authorization middleware (check roles claim for Administrator or Auditor)
- [ ] T122 [US5] Implement read-only enforcement for Auditor role (block PUT/POST/DELETE operations)
- [ ] T123 [US5] Add OpenAPI documentation for audit log query endpoint
- [ ] T124 [US5] Implement pagination for audit log queries (default 50 per page, max 100)
- [ ] T125 [US5] Add audit log retention policy (90 days minimum, archive to cold storage after)

**Checkpoint**: Audit capabilities complete - auditors have read-only access to logs and settings

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and production readiness

- [ ] T126 [P] Update MASTER-TASKS.md with Tenant Service implementation tasks and completion status
- [ ] T127 [P] Create comprehensive service README in src/Services/Sorcha.Tenant.Service/README.md with API documentation, deployment guide, configuration reference
- [ ] T128 [P] Update docs/architecture.md with Tenant Service architecture diagram and authentication flows
- [ ] T129 [P] Create Tenant Service specification document in .specify/specs/sorcha-tenant-service.md
- [ ] T130 [P] Update docs/api-reference.md with all Tenant Service endpoints
- [ ] T131 Run performance tests with NBomber (token issuance load test: 1,000 concurrent requests, target <500ms)
- [ ] T132 [P] Create Docker Compose file for local development (PostgreSQL, Redis, Tenant Service)
- [ ] T133 Implement Prometheus metrics export (token issuance count, validation latency, failed auth attempts)
- [ ] T134 [P] Add Grafana dashboard JSON for Tenant Service monitoring
- [ ] T135 Implement retry policies for external IDP calls (exponential backoff, max 3 retries)
- [ ] T136 [P] Add circuit breaker for Redis connections (fallback: log warning, skip revocation check)
- [ ] T137 Implement key rotation strategy (30-day rotation, publish both old and new keys during transition)
- [ ] T138 [P] Create migration guide for existing users (if applicable)
- [ ] T139 Run security audit checklist (OWASP Top 10 validation, secret scanning, dependency vulnerabilities)
- [ ] T140 [P] Create quickstart validation script (run all examples from quickstart.md)
- [ ] T141 Optimize database queries (add indexes, analyze slow queries, implement caching where appropriate)
- [ ] T142 [P] Add error response standardization across all endpoints (consistent error format per OpenAPI spec)
- [ ] T143 Validate all OpenAPI documentation renders correctly in Scalar UI
- [ ] T144 Run full test suite and achieve >85% code coverage
- [ ] T145 [P] Create deployment pipeline configuration (CI/CD, environment-specific configs)
- [ ] T146 Final code review and refactoring

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3 - P1)**: Depends on Foundational - organization management is prerequisite for user authentication
- **User Story 2 (Phase 4 - P1 - MVP)**: Depends on Foundational + User Story 1 (organizations must exist for users to login)
- **User Story 6 (Phase 5 - P1 - MVP)**: Depends on Foundational + User Story 2 (token service must exist for service tokens)
- **User Story 3 (Phase 6 - P2)**: Depends on Foundational + User Story 2 (token service must exist for PassKey tokens)
- **User Story 4 (Phase 7 - P2)**: Depends on User Story 1 + User Story 2 (permissions affect token claims)
- **User Story 5 (Phase 8 - P3)**: Depends on Foundational (audit log queries only)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### Critical Path for MVP (Minimum Viable Product)

**MVP Scope**: User Stories 1, 2, and 6 (organization management, user authentication, service authentication)

1. **Phase 1: Setup** (T001-T014) - ~2 hours
2. **Phase 2: Foundational** (T015-T041) - ~1-2 days
3. **Phase 3: User Story 1** (T042-T055) - ~2 days
4. **Phase 4: User Story 2** (T056-T077) - ~3-4 days
5. **Phase 5: User Story 6** (T078-T088) - ~1-2 days
6. **Phase 9 (Partial)**: Critical polish only (T126-T130, T143-T144) - ~1 day

**Total MVP Estimate**: ~10-13 days for core authentication platform

### User Story Dependencies

- **User Story 1 (P1)**: Independent after Foundational - no dependencies on other stories
- **User Story 2 (P1)**: **Depends on User Story 1** (organizations must exist)
- **User Story 6 (P1)**: **Depends on User Story 2** (token service infrastructure)
- **User Story 3 (P2)**: **Depends on User Story 2** (token service infrastructure)
- **User Story 4 (P2)**: **Depends on User Story 1 + User Story 2** (permissions affect existing authentication)
- **User Story 5 (P3)**: Independent after Foundational - read-only audit access

### Parallel Opportunities

#### Phase 1 (Setup)
- T003, T004, T005 (all test projects) can run in parallel
- T008, T009, T010, T011 (configuration and documentation) can run in parallel
- T013, T014 (observability setup) can run in parallel

#### Phase 2 (Foundational)
- T015-T021 (all domain models) can run in parallel (different files)
- T022-T024 (all shared DTOs) can run in parallel (different files)
- T029, T031 (repository interfaces) can run in parallel
- T030, T032 (repository implementations) can run after interfaces, in parallel with each other
- T033, T037, T038 (infrastructure services) can run in parallel
- T039-T041 (Redis infrastructure) can run in parallel

#### User Story 1 (Phase 3)
- T042, T043, T044 (all tests) can run in parallel
- T047, T048 (endpoint creation) can run in parallel after T045-T046 (service implementation)
- T049, T050, T053, T054 (documentation and minor features) can run in parallel

#### User Story 2 (Phase 4)
- T056, T057, T058, T059, T060 (all tests) can run in parallel
- T061-T062, T063-T065 (service implementations) can be worked on by different developers in parallel
- T066, T067 (endpoint groups) can run in parallel after services
- T068, T076, T077 (documentation and middleware) can run in parallel

#### User Story 6 (Phase 5)
- T078, T079, T080 (all tests) can run in parallel
- T086, T087 (documentation and logging) can run in parallel

#### User Story 3 (Phase 6)
- T089, T090, T091 (all tests) can run in parallel
- T094, T095, T101, T102, T103 (endpoint creation and configuration) can run in parallel after T092-T093

#### User Story 4 (Phase 7)
- T104, T105, T106 (all tests) can run in parallel
- T111, T112, T113, T114 (configuration and logging) can run in parallel

#### User Story 5 (Phase 8)
- T115, T116, T117 (all tests) can run in parallel
- T122, T123, T124, T125 (enforcement and configuration) can run in parallel

#### Phase 9 (Polish)
- T126, T127, T128, T129, T130 (all documentation) can run in parallel
- T132, T134, T138, T140, T142, T145 (infrastructure and deployment) can run in parallel
- T133, T141 (performance optimization) can run independently

---

## Parallel Example: User Story 2 (Core Authentication)

```bash
# Launch all tests for User Story 2 together:
Task T056: "ExternalIdpServiceTests - test OAuth2 flow"
Task T057: "TokenServiceTests - test JWT operations"
Task T058: "AuthenticationEndpointsTests - test HTTP endpoints"
Task T059: "AuthenticationFlowTests - end-to-end integration"
Task T060: "TokenManagementTests - token lifecycle"

# After services are implemented, launch all endpoint groups:
Task T066: "AuthenticationEndpoints.cs - login, callback, logout"
Task T067: "TokenEndpoints.cs - refresh, validate, JWKS"

# Launch all documentation and configuration in parallel:
Task T068: "OpenAPI documentation for all endpoints"
Task T076: "Correlation ID middleware"
Task T077: "Clock skew tolerance"
```

---

## Implementation Strategy

### MVP First (User Stories 1, 2, 6 Only)

1. Complete Phase 1: Setup (T001-T014)
2. Complete Phase 2: Foundational (T015-T041) - **CRITICAL**
3. Complete Phase 3: User Story 1 (T042-T055) - Organization management
4. Complete Phase 4: User Story 2 (T056-T077) - User authentication
5. Complete Phase 5: User Story 6 (T078-T088) - Service authentication
6. **STOP and VALIDATE**: Test all three stories independently
7. Complete critical polish (T126-T130, T143-T144)
8. Deploy/demo MVP

### Incremental Delivery (Full Platform)

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Milestone: Organization Management
3. Add User Story 2 → Test independently → Milestone: User Authentication (MVP!)
4. Add User Story 6 → Test independently → Milestone: Service Authentication (MVP!)
5. Add User Story 3 → Test independently → Milestone: Public Access
6. Add User Story 4 → Test independently → Milestone: Permission Management
7. Add User Story 5 → Test independently → Milestone: Audit Capabilities
8. Add Polish → Complete → Milestone: Production Ready

### Parallel Team Strategy

With 3 developers after Foundational phase completes:

1. **Team completes Setup + Foundational together** (essential)
2. **Developer A**: User Story 1 (Organization Management)
3. **Developer B**: Starts User Story 2 (after US1 T045-T046 complete, organizations exist)
4. **Developer C**: Starts User Story 6 (after US2 token service exists)
5. **Then**: User Stories 3, 4, 5 can be distributed independently

---

## Notes

- **[P] tasks**: Different files, no shared state, safe for parallel execution
- **[Story] label**: Maps task to specific user story for traceability and independent testing
- **Tests First**: Write tests, ensure they FAIL, then implement to make them PASS (TDD)
- **Commit Frequently**: Commit after each logical task or small group
- **Stop at Checkpoints**: Validate each user story works independently before proceeding
- **Security Critical**: This service handles authentication - extra scrutiny on security reviews
- **Performance Targets**: Token issuance <500ms, validation <50ms - monitor and optimize
- **Constitutional Compliance**: >85% test coverage required, .NET 10 OpenAPI (no Swagger), Serilog + Seq logging

## Task Summary

- **Total Tasks**: 146
- **Setup Phase**: 14 tasks
- **Foundational Phase**: 27 tasks (blocking)
- **User Story 1 (P1)**: 14 tasks
- **User Story 2 (P1 - MVP)**: 22 tasks
- **User Story 6 (P1 - MVP)**: 11 tasks
- **User Story 3 (P2)**: 15 tasks
- **User Story 4 (P2)**: 11 tasks
- **User Story 5 (P3)**: 11 tasks
- **Polish Phase**: 21 tasks
- **Parallel Opportunities**: 89 tasks marked [P] (61% can run in parallel within phases)
- **MVP Scope**: 88 tasks (Phases 1, 2, 3, 4, 5, + critical polish)
