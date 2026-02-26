# Feature Specification: Authentication & Authorization Integration

**Feature Branch**: `041-auth-integration`
**Created**: 2026-02-25
**Status**: Draft
**Input**: Integrate the existing Tenant Service with Blueprint, Wallet, Register, and Peer services to enable JWT-based authentication, service-to-service authorization, and delegation token flows.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Service-to-Service Authentication (Priority: P1)

As an internal service (Blueprint, Wallet, Register, or Peer), I need to authenticate with the Tenant Service using client credentials so that I can make authorized calls to other services in the platform.

**Why this priority**: This is the foundation for all inter-service communication. Without service authentication, no other auth feature can function. Every workflow execution (Blueprint calling Wallet to sign, Wallet calling Register to store) depends on services proving their identity.

**Independent Test**: Can be fully tested by starting two services, having one request a service token, and using that token to call the other. Delivers value by securing the service mesh.

**Acceptance Scenarios**:

1. **Given** Blueprint Service starts, **When** it requests a service token from Tenant Service using its client credentials, **Then** a valid JWT with `token_type: service` is returned.
2. **Given** a valid service token, **When** Blueprint Service calls Wallet Service, **Then** the request is authorized and succeeds.
3. **Given** an expired service token, **When** Blueprint Service calls Wallet Service, **Then** 401 Unauthorized is returned.
4. **Given** invalid client credentials, **When** any service requests a token, **Then** 401 Unauthorized is returned.
5. **Given** a valid service token, **When** the token is inspected, **Then** it contains the service identity and permitted scopes.

---

### User Story 2 — User Authentication (Priority: P1)

As a user, I need to authenticate and receive access credentials so that I can use protected platform capabilities like creating blueprints, managing wallets, and querying registers.

**Why this priority**: Required for all user-facing operations. Users currently access the system in an unauthenticated state, which is unacceptable for production.

**Independent Test**: Can be tested by logging in with valid credentials, accessing a protected endpoint, and verifying the request succeeds. Also test that unauthenticated requests are rejected.

**Acceptance Scenarios**:

1. **Given** valid user credentials, **When** the user authenticates, **Then** access and refresh tokens are returned.
2. **Given** a valid access token, **When** the user calls a protected endpoint, **Then** the request succeeds.
3. **Given** an expired access token, **When** the user calls a protected endpoint, **Then** 401 Unauthorized is returned.
4. **Given** a valid refresh token within its expiry window, **When** the user requests new tokens, **Then** fresh access and refresh tokens are returned.
5. **Given** a revoked refresh token, **When** used to request new tokens, **Then** the request is rejected.

---

### User Story 3 — Delegation Token Flow (Priority: P1)

As Blueprint Service acting on behalf of a user, I need to obtain delegation tokens so that downstream services (Wallet, Register) can verify both the service identity and the user's authorization for the specific operation.

**Why this priority**: The core Blueprint → Wallet → Register workflow requires the Blueprint Service to act on behalf of users. Without delegation, there is no way for Wallet Service to know which user authorized the signing operation.

**Independent Test**: Can be tested by submitting an action as an authenticated user, verifying Blueprint obtains a delegation token, and confirming Wallet accepts it for the user's wallet operations.

**Acceptance Scenarios**:

1. **Given** a user's access token, **When** Blueprint Service requests a delegation token from Tenant Service, **Then** a delegation token containing `delegated_user_id` claim is returned.
2. **Given** a delegation token, **When** Blueprint calls Wallet Service, **Then** Wallet can identify both the calling service and the delegated user.
3. **Given** a delegation token for scope `wallets:sign`, **When** used for a `wallets:read` operation, **Then** authorization fails with 403 Forbidden (scope mismatch).
4. **Given** an expired delegation token, **When** used to call a downstream service, **Then** 401 Unauthorized is returned.
5. **Given** a delegation token, **When** the delegated user's original token has been revoked, **Then** the delegation token remains valid until its natural 5-minute expiry (in-flight operations are not interrupted; revocation takes effect when the delegation token expires).

---

### User Story 4 — Authorization Policies (Priority: P1)

As a service, I need to enforce fine-grained authorization policies so that users and services can only perform operations they are explicitly permitted to do.

**Why this priority**: Security requirement. Authentication alone is insufficient — the system must enforce what each authenticated actor is allowed to do.

**Independent Test**: Can be tested by issuing tokens with different roles/scopes and verifying that each role grants access only to its permitted endpoints.

**Acceptance Scenarios**:

1. **Given** a user token with an `org_id` claim, **When** the user accesses a wallet belonging to their organization, **Then** the `CanManageWallets` policy grants access.
2. **Given** a delegation token with `delegated_user_id` and `token_type: service`, **When** the service accesses the delegated user's wallet, **Then** the `RequireDelegatedAuthority` policy grants access.
3. **Given** a token without `org_id` and without delegation claims, **When** accessing any wallet endpoint, **Then** 403 Forbidden is returned.
4. **Given** a service token without delegation, **When** attempting a user-scoped operation, **Then** 403 Forbidden is returned.
5. **Given** a user with `register:write` scope, **When** submitting a transaction, **Then** the operation succeeds.
6. **Given** a user without `register:write` scope, **When** attempting to submit a transaction, **Then** 403 Forbidden is returned.

---

### User Story 5 — Token Introspection & Revocation (Priority: P2)

As a service, I need to introspect tokens and check revocation status so that I can make real-time authorization decisions and immediately reject compromised credentials.

**Why this priority**: Important for security but not blocking the core authentication flow. Services can initially rely on token expiry; introspection adds real-time revocation capabilities.

**Independent Test**: Can be tested by issuing a token, introspecting it to verify claims, revoking it, and confirming introspection returns inactive status.

**Acceptance Scenarios**:

1. **Given** a valid token, **When** a service calls the introspection endpoint, **Then** the token's claims (subject, roles, scopes, expiry) are returned.
2. **Given** a revoked token, **When** introspected, **Then** `active: false` is returned.
3. **Given** an invalid or malformed token, **When** introspected, **Then** `active: false` is returned.
4. **Given** a token is revoked, **When** it is used within 30 seconds of revocation, **Then** the request is rejected.

---

### Edge Cases

- What happens when the Tenant Service is temporarily unavailable? Services should cache their service tokens and continue operating until token expiry. User requests should fail gracefully with a 503.
- What happens when a service token expires during a multi-step workflow (e.g., mid-way through Blueprint → Wallet → Register)? The calling service should transparently refresh and retry once.
- What happens when a user's session is terminated while a long-running workflow is in progress? In-flight delegation tokens should remain valid until their short expiry (5 minutes).
- What happens when clock skew between services exceeds the tolerance? Token validation should allow a configurable clock skew window (default 5 minutes).
- What happens if a service presents a token signed by an unknown key? The request should be rejected with 401 and the event logged as a security incident.
- What happens when multiple services attempt to refresh the same token simultaneously? Token refresh must be idempotent — multiple refresh calls with the same refresh token should be safe.
- What happens when an anonymous peer submits a transaction that conflicts with one from an authenticated peer? The reputation system should weight the authenticated peer's submission higher. The specific conflict resolution rules are part of the reputation mechanism design.
- What happens when an authenticated peer's JWT expires during an active replication session? The peer should re-authenticate and resume. In-flight transactions already accepted into the mempool are not affected.

## Clarifications

### Session 2026-02-25

- Q: Should downstream services also validate JWT signatures, or trust the gateway? → A: Defense-in-depth — both gateway and each service independently validate JWT signatures. Services do not trust the network layer.
- Q: Which endpoint categories remain accessible without authentication? → A: Minimal anonymous — only health checks (`/health`, `/alive`), OpenAPI/Scalar docs (`/scalar`, `/openapi`), and authentication endpoints (`/auth/token`, `/auth/refresh`) are anonymous. All other endpoints require authentication.
- Q: How should Peer Service gRPC connections authenticate? → A: Peers MUST support both authenticated and anonymous inbound connections. Authenticated peers use JWT (service-account equivalent) via gRPC metadata, establishing strong reputation. Anonymous peers are permitted but receive lower reputation scores — reputation-based mechanisms determine whether inbound transactions are accepted. mTLS is optional for on-the-wire encryption (configurable per-peer in the peer configuration UI), since data is already encrypted at the transaction layer.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: All protected endpoints across all services (Blueprint, Wallet, Register, Peer, Validator) MUST return 401 Unauthorized when called without a valid authentication token.
- **FR-002**: Services MUST be able to authenticate with each other using client credentials to obtain service tokens.
- **FR-003**: The system MUST support delegation tokens that allow one service to act on behalf of an authenticated user with specific, limited scopes.
- **FR-004**: Authorization MUST be enforced at the endpoint level with policies that check roles, scopes, and token type (user vs. service vs. delegation).
- **FR-005**: Service tokens MUST be cached locally and refreshed automatically before expiry to avoid unnecessary round-trips.
- **FR-006**: Revoked tokens MUST be rejected within 30 seconds of revocation.
- **FR-007**: Token validation MUST support configurable clock skew tolerance (default 5 minutes) to handle distributed clock differences.
- **FR-008**: The API Gateway MUST validate tokens before forwarding requests to downstream services, acting as the first line of authentication. Each downstream service MUST also independently validate JWT signatures (defense-in-depth) — services do not trust the network layer.
- **FR-009**: All authentication and authorization failures MUST be logged with sufficient detail for security audit (without logging sensitive token content).
- **FR-010**: The system MUST NOT introduce breaking changes to existing API contracts — endpoints that are currently accessible should continue to function identically once authenticated.
- **FR-011**: A shared authentication library MUST be provided so that all services configure authentication and authorization consistently.
- **FR-012**: Delegation tokens MUST have a short expiry (5 minutes maximum) and MUST NOT be refreshable.
- **FR-013**: Health check endpoints (`/health`, `/alive`), OpenAPI/Scalar documentation endpoints, and authentication endpoints (`/auth/token`, `/auth/refresh`) MUST remain accessible without authentication. All other endpoints MUST require a valid token.
- **FR-014**: The Peer Service MUST accept both authenticated and anonymous inbound gRPC connections. Authenticated peers present JWT tokens (service-account equivalent) via gRPC call metadata, establishing strong reputation. Anonymous peers are permitted but assigned lower reputation scores.
- **FR-015**: Reputation-based mechanisms MUST be used to determine whether inbound transactions from peers are accepted, with authenticated peers receiving higher trust scores than anonymous peers.
- **FR-016**: mTLS for peer-to-peer gRPC channels MUST be a configurable option (enabled/disabled per peer via the peer configuration UI), since transaction-layer encryption already protects payload confidentiality.

### Key Entities

- **Service Token**: A JWT representing a service's identity, containing the service name, permitted scopes, and expiry. Used for service-to-service communication.
- **User Access Token**: A JWT representing an authenticated user, containing user ID, organization membership, roles, and scopes. Short-lived (15-60 minutes).
- **Refresh Token**: A long-lived opaque token used to obtain new access tokens without re-authentication. Revocable.
- **Delegation Token**: A JWT issued when a service needs to act on behalf of a user. Contains both the service identity and the delegated user identity, scoped to specific operations. Short-lived (5 minutes), not refreshable.
- **Authorization Policy**: A named rule that defines what claims (roles, scopes, token type) are required to access a specific endpoint or resource.
- **Service Principal**: The identity configuration (client ID + secret) assigned to each service for client credentials authentication.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of protected endpoints return 401 when called without a valid token, verified by automated integration tests.
- **SC-002**: Service-to-service authentication succeeds on first attempt in normal operating conditions, with automatic retry on transient failures.
- **SC-003**: The delegation flow (user → Blueprint → Wallet → Register) completes successfully for authorized users with correct scopes.
- **SC-004**: Token refresh completes within the expiry window, maintaining uninterrupted user sessions.
- **SC-005**: Revoked tokens are rejected within 30 seconds of revocation across all services.
- **SC-006**: All authentication scenarios (happy path and error cases) are covered by automated integration tests with >90% pass rate.
- **SC-007**: Zero breaking changes to existing API contracts — all current API consumers continue to function after authentication is added.
- **SC-008**: Platform production readiness increases from 10% to 50%+ upon completion of this feature.

### Assumptions

- The Tenant Service is fully implemented and tested (67 tests, 91% pass rate) with JWT issuance, token introspection, and revocation already operational.
- Redis is available and configured for token revocation cache.
- .NET Aspire service discovery is operational for inter-service communication.
- The existing API Gateway (YARP) supports adding authentication middleware without architectural changes.
- All services are deployed within a trusted network boundary; the API Gateway is the only public-facing entry point.

### Dependencies

| Dependency         | Status       | Notes                              |
| ------------------ | ------------ | ---------------------------------- |
| Tenant Service     | Complete     | 67 tests, JWT issuance working     |
| Redis              | Available    | For token revocation cache         |
| .NET Aspire        | Available    | Service discovery                  |
| API Gateway (YARP) | Available    | Token forwarding to be configured  |
