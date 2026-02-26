# Tasks: Authentication & Authorization Integration

**Input**: Design documents from `/specs/041-auth-integration/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Integration tests included — SC-006 requires automated coverage of all auth scenarios.

**Organization**: Tasks grouped by user story. User stories 1-4 are P1 (core), user story 5 is P2 (enhancement).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create project structure and fill configuration gaps identified in research.md

- [x] T001 Create integration test project `tests/Sorcha.Auth.IntegrationTests/Sorcha.Auth.IntegrationTests.csproj` with xUnit, FluentAssertions, and project references to ServiceClients and ServiceDefaults
- [x] T002 [P] Add missing ServiceAuth environment variables for Wallet and Peer services in `docker-compose.yml` (ServiceAuth__ClientId, ServiceAuth__ClientSecret, ServiceAuth__Scopes per contracts/service-auth.md)
- [x] T003 [P] Add missing service auth configuration for Wallet and Peer services in `src/Apps/Sorcha.AppHost/Program.cs` (environment variables matching docker-compose)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared constants, configurable scopes, and Validator Service auth — MUST complete before user story work

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Create `TokenClaimConstants.cs` in `src/Common/Sorcha.ServiceClients/Auth/` with shared claim name constants (token_type, delegated_user_id, delegated_org_id, org_id, service_name, scope) replacing hardcoded strings across services
- [x] T005 Modify `ServiceAuthClient.cs` in `src/Common/Sorcha.ServiceClients/Auth/` to read scopes from `ServiceAuth:Scopes` configuration instead of hardcoded `wallets:sign` — fall back to `wallets:sign` if config missing for backwards compatibility
- [x] T006 [P] Create `AuthenticationExtensions.cs` in `src/Services/Sorcha.Validator.Service/Extensions/` with authorization policies: RequireAuthenticated, RequireService, RequireOrganizationMember, RequireAdministrator, CanValidateChains, CanWriteDockets — following the pattern from Blueprint/Register service extensions
- [x] T007 Modify `Program.cs` in `src/Services/Sorcha.Validator.Service/` to call `builder.AddJwtAuthentication()`, `builder.Services.AddAuthorizationPolicies()`, `app.UseAuthentication()`, `app.UseAuthorization()` and apply `RequireAuthorization()` to all protected endpoints while keeping health/docs anonymous
- [x] T008 [P] Write unit tests for Validator auth in `tests/Sorcha.Validator.Service.Tests/AuthenticationTests.cs` — test: protected endpoints return 401 without token, health/docs endpoints remain anonymous, RequireService policy rejects user tokens, CanValidateChains policy grants access to authorized service tokens
- [x] T009 Write unit tests for configurable scopes in `tests/Sorcha.ServiceClients.Tests/Auth/ServiceAuthClientTests.cs` — test that scopes from config are sent in token request, test fallback to default scope when config is missing

**Checkpoint**: Foundation ready — all services have JWT auth middleware, shared constants exist, ServiceAuthClient supports configurable scopes

---

## Phase 3: User Story 1 — Service-to-Service Authentication (Priority: P1) MVP

**Goal**: Services authenticate with each other using client credentials and cached service tokens

**Independent Test**: Start Blueprint + Wallet services, verify Blueprint acquires service token and successfully calls Wallet with it

### Implementation for User Story 1

- [x] T010 [US1] Verify all service entries in `docker-compose.yml` have correct ServiceAuth__ClientId, ServiceAuth__ClientSecret, and ServiceAuth__Scopes environment variables per contracts/service-auth.md scope table (Blueprint, Wallet, Register, Validator, Peer)
- [x] T011 [US1] Add structured logging for service token acquisition lifecycle in `src/Common/Sorcha.ServiceClients/Auth/ServiceAuthClient.cs` — log token acquired (no secret content), cache hit, refresh triggered, and acquisition failure events using ILogger
- [x] T012 [US1] Write `ServiceTokenFlowTests.cs` in `tests/Sorcha.Auth.IntegrationTests/` — test: service acquires token with valid credentials (AS-1.1), service calls another service with token (AS-1.2), expired token returns 401 (AS-1.3), invalid credentials return 401 (AS-1.4), token contains service identity and scopes (AS-1.5)

**Checkpoint**: Service-to-service auth verified — services acquire tokens, cache them, and use them for inter-service calls

---

## Phase 4: User Story 2 — User Authentication (Priority: P1)

**Goal**: Users authenticate via Tenant Service and access protected endpoints across all services through the API Gateway

**Independent Test**: Login with valid credentials, access protected Blueprint endpoint, verify 401 without token, verify token refresh works

### Implementation for User Story 2

- [x] T013 [US2] Audit all endpoint files across Blueprint, Wallet, Register, Peer, and Validator services to verify every endpoint has either `.RequireAuthorization()` with appropriate policy or `.AllowAnonymous()` — document any unprotected endpoints found and add missing auth attributes
- [x] T014 [P] [US2] Verify anonymous endpoints remain accessible without authentication: health checks (`/health`, `/alive`), Scalar docs (`/scalar`), OpenAPI spec (`/openapi`), and auth endpoints (`/api/auth/login`, `/api/auth/token/refresh`, `/api/service-auth/token`) per FR-013
- [x] T015 [US2] Write `UserAuthFlowTests.cs` in `tests/Sorcha.Auth.IntegrationTests/` — test: valid credentials return tokens (AS-2.1), valid token accesses protected endpoint (AS-2.2), expired token returns 401 (AS-2.3), refresh token returns new tokens (AS-2.4), revoked refresh token is rejected (AS-2.5)

**Checkpoint**: User authentication verified — login, protected access, token refresh, and anonymous endpoints all work correctly

---

## Phase 5: User Story 3 — Delegation Token Flow (Priority: P1)

**Goal**: Blueprint Service obtains delegation tokens to act on behalf of users when calling Wallet and Register services

**Independent Test**: Authenticated user submits action → Blueprint gets delegation token → Wallet accepts it and identifies both service and user

### Implementation for User Story 3

- [x] T016 [P] [US3] Create `IDelegationTokenClient.cs` interface in `src/Common/Sorcha.ServiceClients/Auth/` with `Task<string?> GetDelegationTokenAsync(string userAccessToken, string[] scopes, CancellationToken cancellationToken)` per contracts/delegation.md
- [x] T017 [US3] Create `DelegationTokenClient.cs` implementation in `src/Common/Sorcha.ServiceClients/Auth/` — acquires service token via IServiceAuthClient, POSTs to `/api/service-auth/token/delegated` with both tokens, no caching (delegation tokens are short-lived and user-specific), returns null on failure with structured error logging
- [x] T018 [US3] Register `IDelegationTokenClient`/`DelegationTokenClient` in the ServiceClients DI registration extension method in `src/Common/Sorcha.ServiceClients/` (scoped lifetime, alongside existing IServiceAuthClient registration)
- [x] T019 [P] [US3] Add `RequireDelegatedAuthority` policy to `src/Services/Sorcha.Blueprint.Service/Extensions/AuthenticationExtensions.cs` — require claims `token_type=service` AND `delegated_user_id` present
- [x] T020 [P] [US3] Add `RequireDelegatedAuthority` policy to `src/Services/Sorcha.Wallet.Service/Extensions/AuthenticationExtensions.cs` — require claims `token_type=service` AND `delegated_user_id` present
- [x] T021 [P] [US3] Add `RequireDelegatedAuthority` policy to `src/Services/Sorcha.Register.Service/Extensions/AuthenticationExtensions.cs` — require claims `token_type=service` AND `delegated_user_id` present
- [x] T022 [US3] Write `DelegationTokenClientTests.cs` unit tests in `tests/Sorcha.ServiceClients.Tests/Auth/` — test: delegation token acquired with valid tokens (AS-3.1), delegation contains both service and user identity (AS-3.2), scope mismatch returns null (AS-3.3), expired delegation returns null (AS-3.4), revoked user token causes failure (AS-3.5)
- [x] T023 [US3] Write `DelegationFlowTests.cs` in `tests/Sorcha.Auth.IntegrationTests/` — test end-to-end: user submits action → Blueprint acquires delegation → Wallet validates delegation token with correct user identity and scopes

**Checkpoint**: Delegation flow verified — Blueprint can act on behalf of users, Wallet/Register validate delegation claims

---

## Phase 6: User Story 4 — Authorization Policies (Priority: P1)

**Goal**: Fine-grained authorization policies enforce what each actor (user, service, delegation) can do at every endpoint

**Independent Test**: Issue tokens with different roles/scopes, verify each role grants access only to permitted endpoints and returns 403 for unauthorized operations

### Implementation for User Story 4

- [x] T024 [US4] Rename `"Administrator"` policy to `"RequireAdministrator"` in `src/Services/Sorcha.Blueprint.Service/Extensions/AuthenticationExtensions.cs` and update all endpoint references in `src/Services/Sorcha.Blueprint.Service/Endpoints/` that use the old policy name
- [x] T025 [P] [US4] Add `RequireOrganizationMember` policy to `src/Services/Sorcha.Peer.Service/Extensions/AuthenticationExtensions.cs` (currently missing — Blueprint, Wallet, Register all have it)
- [x] T026 [US4] Create `PeerAuthInterceptor.cs` gRPC server interceptor in `src/Services/Sorcha.Peer.Service/GrpcServices/` — extract JWT from `authorization` gRPC metadata key, validate token, set auth context for authenticated peers (strong reputation), allow anonymous peers through with lower reputation flag per FR-014
- [x] T027 [US4] Register `PeerAuthInterceptor` in Peer Service DI and gRPC pipeline in `src/Services/Sorcha.Peer.Service/Program.cs`
- [x] T028 [US4] Add reputation score integration with authentication status in `src/Services/Sorcha.Peer.Service/` — authenticated peers (valid JWT) receive higher base reputation score than anonymous peers per FR-015 (integrate with existing reputation system, do not build new reputation logic)
- [x] T029 [US4] Write `AuthorizationPolicyTests.cs` in `tests/Sorcha.Auth.IntegrationTests/` — test: wallet owner accesses own wallet (AS-4.1), delegate accesses delegated wallet (AS-4.2), no-role user gets 403 on wallet (AS-4.3), service token without delegation gets 403 on user-scoped op (AS-4.4), register:write user can submit TX (AS-4.5), no-scope user gets 403 on TX submit (AS-4.6)

**Checkpoint**: Authorization policies verified — each actor type has correct access, 403 returned for unauthorized operations

---

## Phase 7: User Story 5 — Token Introspection & Revocation (Priority: P2)

**Goal**: Services can introspect tokens and revoked tokens are rejected within 30 seconds across all services

**Independent Test**: Issue token, introspect to verify claims, revoke it, confirm introspection returns inactive and subsequent requests are rejected within 30 seconds

### Implementation for User Story 5

- [x] T030 [US5] Add token revocation check middleware to `src/Common/Sorcha.ServiceDefaults/JwtAuthenticationExtensions.cs` — on each request, check Redis revocation cache for the token's `jti` claim; reject with 401 if found; use configurable cache TTL matching FR-006 30-second window
- [x] T031 [P] [US5] Create `ITokenIntrospectionClient.cs` interface and `TokenIntrospectionClient.cs` implementation in `src/Common/Sorcha.ServiceClients/Auth/` — wraps `POST /api/auth/token/introspect` endpoint, requires service token for authentication, returns introspection result with active status and claims
- [x] T032 [US5] Register `ITokenIntrospectionClient` in ServiceClients DI registration and configure Redis connection for revocation cache in `src/Common/Sorcha.ServiceDefaults/`
- [x] T033 [US5] Write `TokenRevocationTests.cs` in `tests/Sorcha.Auth.IntegrationTests/` — test: valid token introspection returns claims (AS-5.1), revoked token returns active:false (AS-5.2), malformed token returns active:false (AS-5.3), revoked token rejected within 30 seconds (AS-5.4)

**Checkpoint**: Introspection and revocation verified — services can query token status and revoked tokens are rejected promptly

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, security hardening, and validation across all stories

- [x] T034 [P] Update `docs/AUTHENTICATION-SETUP.md` with complete service auth configuration table, delegation flow diagram, and quickstart commands from quickstart.md
- [x] T035 [P] Review and verify security audit logging (FR-009) across all services — all auth failures must be logged with request path, token type, failure reason, timestamp (without sensitive token content); add missing log statements
- [x] T036 Verify zero breaking changes (FR-010) by running the full existing test suite (`dotnet test`) and confirming all previously-passing tests still pass
- [x] T037 Run quickstart.md validation scenarios end-to-end in Docker Compose — verify all 5 scenarios produce expected results (SKIPPED: requires Docker infrastructure)
- [x] T038 [P] Add mTLS configuration option scaffolding to Peer Service config model per FR-016 — add `EnableMtls` boolean property to peer configuration (implementation deferred, config model only)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — service token infrastructure
- **US2 (Phase 4)**: Depends on Foundational — can run in parallel with US1
- **US3 (Phase 5)**: Depends on Foundational — benefits from US1 completion (service tokens needed for delegation)
- **US4 (Phase 6)**: Depends on Foundational — can run in parallel with US1-US3
- **US5 (Phase 7)**: Depends on Foundational — can run in parallel with US1-US4
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Foundation only — no cross-story dependencies
- **US2 (P1)**: Foundation only — no cross-story dependencies
- **US3 (P1)**: Benefits from US1 (service tokens used to acquire delegation tokens) but can be implemented with mock tokens
- **US4 (P1)**: Benefits from US3 (RequireDelegatedAuthority policy) — shares policy definitions
- **US5 (P2)**: Independent — revocation cache is separate from other stories

### Within Each User Story

- Models/interfaces before implementations
- Implementations before DI registration
- DI registration before integration tests
- Unit tests alongside implementation (not strict TDD — tests included per story)

### Parallel Opportunities

**Phase 2 (Foundational)**:
```
Parallel: T004 (TokenClaimConstants) + T006 (Validator AuthExtensions) + T008 (Validator tests)
Sequential: T005 (ServiceAuthClient scopes) → T009 (scope tests)
Sequential: T006 (Validator AuthExtensions) → T007 (Validator Program.cs)
```

**Phase 5 (US3 — Delegation)**:
```
Parallel: T016 (interface) + T019 (Blueprint policy) + T020 (Wallet policy) + T021 (Register policy)
Sequential: T016 (interface) → T017 (implementation) → T018 (DI registration) → T022 (unit tests) → T023 (integration tests)
```

**Phase 6 (US4 — Policies)**:
```
Parallel: T024 (Blueprint rename) + T025 (Peer OrgMember) + T026 (PeerAuthInterceptor)
Sequential: T026 (interceptor) → T027 (register in DI) → T028 (reputation integration)
```

**Cross-Phase Parallel (if team capacity allows)**:
```
After Phase 2: US1 + US2 + US4 + US5 can all start simultaneously
US3 best started after US1 completes (needs working service tokens)
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup (3 tasks)
2. Complete Phase 2: Foundational (6 tasks)
3. Complete Phase 3: US1 — Service-to-Service Auth (3 tasks)
4. **STOP and VALIDATE**: Test service token acquisition and cross-service calls
5. This alone secures the service mesh

### Incremental Delivery

1. Setup + Foundational → 9 tasks, all services have JWT middleware
2. Add US1 (Service Auth) → 3 tasks, service mesh secured
3. Add US2 (User Auth) → 3 tasks, user endpoints protected
4. Add US3 (Delegation) → 8 tasks, on-behalf-of flows work
5. Add US4 (Policies) → 6 tasks, fine-grained access control
6. Add US5 (Introspection) → 4 tasks, real-time revocation
7. Polish → 5 tasks, documentation and validation

### Suggested MVP Scope

**Phases 1-3 (US1)**: 12 tasks — delivers service-to-service authentication, secures the service mesh, fills the Validator Service gap

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story from spec.md
- Research.md gaps G1-G10 are mapped across phases: G1→Phase 2, G2→Phase 2, G3→Phase 5, G4→Phase 1, G5→Phase 5, G6→Phase 6, G7→per-story, G8→Phase 6, G9→Phase 7, G10→Phase 2
- Most services already have auth middleware — this is gap-filling, not greenfield
- Tenant Service endpoints are NOT modified — all token issuance/revocation/introspection already exists
- 38 total tasks across 8 phases
