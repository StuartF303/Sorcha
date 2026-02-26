# Implementation Plan: Authentication & Authorization Integration

**Branch**: `041-auth-integration` | **Date**: 2026-02-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/041-auth-integration/spec.md`

## Summary

Integrate the existing Tenant Service JWT infrastructure with Blueprint, Wallet, Register, Peer, and Validator services to enable defense-in-depth authentication, service-to-service authorization, delegation token flows, and fine-grained endpoint policies.

**Key insight from research**: The platform already has ~80% of the auth infrastructure. Five of seven services have JWT auth middleware and authorization policies configured. The Tenant Service has complete token issuance, refresh, revocation, introspection, and delegation endpoints. This feature is primarily a **gap-filling and integration-wiring** effort, not a greenfield build.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0 (LTS)
**Primary Dependencies**: Microsoft.AspNetCore.Authentication.JwtBearer, System.IdentityModel.Tokens.Jwt, Grpc.AspNetCore (Peer auth interceptor)
**Storage**: Redis (token revocation cache), PostgreSQL (Tenant Service — existing)
**Testing**: xUnit + FluentAssertions + Moq (unit), Docker Compose (integration)
**Target Platform**: Linux containers via Docker / .NET Aspire orchestration
**Project Type**: Microservices (7 services + shared libraries)
**Performance Goals**: Token validation <5ms overhead per request; revocation propagation <30 seconds
**Constraints**: Zero breaking changes to existing API contracts; defense-in-depth (gateway + per-service validation)
**Scale/Scope**: 7 services, ~15 modified files, ~5 new files, ~300 lines of new code + ~500 lines of tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | No new services created. Each service independently validates tokens. No upward dependencies. |
| II. Security First | PASS | Zero-trust model: defense-in-depth JWT validation at gateway AND each service. HMAC-SHA256 signing. No secrets in source. |
| III. API Documentation | PASS | All new endpoints will have XML documentation + OpenAPI via Scalar. Existing endpoints preserved. |
| IV. Testing Requirements | PASS | Target >85% coverage for new code. xUnit tests with integration tests for cross-service auth flows. |
| V. Code Quality | PASS | Async/await for token operations. DI for all auth services. Nullable enabled. .NET 10 / C# 13. |
| VI. Blueprint Creation Standards | N/A | No blueprint changes. |
| VII. Domain-Driven Design | PASS | Using established terms: Service Principal, Delegation Token, Authorization Policy. |
| VIII. Observability by Default | PASS | All auth failures logged with structured logging. Health checks remain at `/health`, `/alive`. |

**Gate Result**: PASS — no violations. Proceed to implementation.

## Project Structure

### Documentation (this feature)

```text
specs/041-auth-integration/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 research findings
├── data-model.md        # Key entity models
├── quickstart.md        # Integration test scenarios
├── contracts/           # API contract definitions
│   ├── service-auth.md  # Service-to-service auth contracts
│   └── delegation.md    # Delegation token flow contracts
├── checklists/
│   └── requirements.md  # Specification quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Common/
│   ├── Sorcha.ServiceDefaults/
│   │   └── JwtAuthenticationExtensions.cs    # EXISTING — extend with shared policy helpers
│   └── Sorcha.ServiceClients/
│       └── Auth/
│           ├── IServiceAuthClient.cs          # EXISTING — add scope configuration
│           ├── ServiceAuthClient.cs           # EXISTING — configurable scopes
│           ├── IDelegationTokenClient.cs      # NEW — delegation token client interface
│           ├── DelegationTokenClient.cs       # NEW — delegation token acquisition
│           └── TokenClaimConstants.cs         # NEW — shared claim name constants
├── Services/
│   ├── Sorcha.Validator.Service/
│   │   ├── Extensions/
│   │   │   └── AuthenticationExtensions.cs   # NEW — Validator auth + policies
│   │   └── Program.cs                        # MODIFY — add auth middleware
│   ├── Sorcha.Blueprint.Service/
│   │   └── Extensions/
│   │       └── AuthenticationExtensions.cs   # MODIFY — add RequireDelegatedAuthority
│   ├── Sorcha.Wallet.Service/
│   │   └── Extensions/
│   │       └── AuthenticationExtensions.cs   # MODIFY — add RequireDelegatedAuthority
│   ├── Sorcha.Register.Service/
│   │   └── Extensions/
│   │       └── AuthenticationExtensions.cs   # MODIFY — add RequireDelegatedAuthority
│   └── Sorcha.Peer.Service/
│       ├── Extensions/
│       │   └── AuthenticationExtensions.cs   # MODIFY — add peer reputation policies
│       └── GrpcServices/
│           └── PeerAuthInterceptor.cs        # NEW — gRPC JWT metadata interceptor

tests/
├── Sorcha.ServiceClients.Tests/
│   └── Auth/
│       ├── ServiceAuthClientTests.cs         # MODIFY — test configurable scopes
│       └── DelegationTokenClientTests.cs     # NEW — delegation client tests
├── Sorcha.Validator.Service.Tests/
│   └── AuthenticationTests.cs                # NEW — Validator auth tests
└── Sorcha.Auth.IntegrationTests/             # NEW — cross-service auth integration tests
    ├── ServiceTokenFlowTests.cs              # NEW — US1 integration tests
    ├── UserAuthFlowTests.cs                  # NEW — US2 integration tests
    ├── DelegationFlowTests.cs                # NEW — US3 integration tests
    └── AuthorizationPolicyTests.cs           # NEW — US4 integration tests
```

**Structure Decision**: Existing microservices architecture. No new projects beyond the integration test project. Modifications concentrated in `Extensions/AuthenticationExtensions.cs` per service and `Sorcha.ServiceClients/Auth/` for shared client code.

### Configuration Changes

```text
docker-compose.yml                            # MODIFY — add missing ServiceAuth env vars
src/Apps/Sorcha.AppHost/Program.cs             # MODIFY — add missing service auth env vars for Aspire
```

## Architecture

### Token Flow Diagram

```
User Login → Tenant Service → Access Token (JWT) + Refresh Token
                                    │
                                    ▼
                            API Gateway (YARP)
                            ├── Validates JWT (defense-in-depth layer 1)
                            └── Forwards Authorization header
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
              Blueprint Svc   Register Svc    Wallet Svc
              ├── Validates JWT (layer 2)
              ├── Checks policy (role/scope/type)
              └── If delegation needed:
                  └── Requests delegation token from Tenant
                      └── Calls downstream with delegation JWT
```

### Service-to-Service Token Acquisition

```
Service Start → ServiceAuthClient.GetTokenAsync()
                ├── POST /api/service-auth/token (client_credentials)
                ├── Cache token in memory (SemaphoreSlim thread-safe)
                └── Auto-refresh 5 min before expiry
```

### Delegation Flow (Blueprint → Wallet → Register)

```
User submits action → Blueprint Service
  ├── User's JWT present (forwarded from gateway)
  ├── Blueprint requests delegation token from Tenant Service
  │   POST /api/service-auth/token/delegated
  │   Body: { service_token, user_access_token, scopes: ["wallets:sign"] }
  │   Response: Delegation JWT (5 min TTL, not refreshable)
  ├── Blueprint calls Wallet Service with delegation JWT
  │   Authorization: Bearer <delegation-jwt>
  │   Wallet validates: token_type=service + delegated_user_id present
  └── Wallet calls Register with delegation JWT (if needed)
      Register validates same claims
```

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Shared auth library location | `Sorcha.ServiceDefaults` (existing) | Already serves this role; no new project needed |
| Token signing algorithm | HMAC-SHA256 (existing) | Already implemented and configured across all services |
| Service token scope | Configurable via `ServiceAuth:Scopes` config | Current hardcoded `wallets:sign` limits usefulness |
| Delegation client | `IDelegationTokenClient` in ServiceClients | Follows established pattern of IServiceAuthClient |
| Peer gRPC auth | Server interceptor + metadata JWT | Standard gRPC pattern; aligns with clarification on reputation-based model |
| Revocation propagation | Redis pub/sub (existing Tenant Service pattern) | Already implemented in Tenant Service; extend to other services |
| Integration test approach | Docker Compose + xUnit | Consistent with existing test patterns |

## Complexity Tracking

> No constitution violations to justify.

| Aspect | Complexity | Justification |
|--------|-----------|---------------|
| New integration test project | Low | Required for cross-service auth verification (FR-009, SC-006) |
| gRPC auth interceptor | Low | Single file, standard pattern, needed for FR-014 |
| Delegation client | Low | Follows existing ServiceAuthClient pattern |
