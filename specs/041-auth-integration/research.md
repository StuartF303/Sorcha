# Research: Authentication & Authorization Integration

**Feature**: 041-auth-integration
**Date**: 2026-02-25
**Status**: Complete

## Executive Summary

Research reveals that the Sorcha platform already has **substantial auth infrastructure** in place. The scope of this feature is narrower than initially assumed — it's an integration and gap-filling effort, not a greenfield auth build.

## R1: Existing JWT Infrastructure

### Decision: Extend existing `JwtAuthenticationExtensions` + `ServiceAuthClient` pattern

**Rationale**: The shared auth library (FR-011) effectively already exists.

**Current State**:
- `Sorcha.ServiceDefaults/JwtAuthenticationExtensions.cs` provides `AddJwtAuthentication<TBuilder>()` extension
- HMAC-SHA256 signing with `JwtSettings` configuration class
- Dev/test environments auto-generate and persist signing key at `%LOCALAPPDATA%\Sorcha\dev-jwt-signing-key.txt`
- `JwtSettings` supports: Issuer, Audience, SigningKey, AccessTokenLifetimeMinutes (60), RefreshTokenLifetimeHours (24), ServiceTokenLifetimeHours (8), ClockSkewMinutes (5), all validation flags
- `InstallationName` auto-derives Issuer/Audience for simplified config
- SignalR token-from-querystring already supported for `/hubs`, `/hub`, `/actionshub` paths

**Services Already Configured (5/7)**:
| Service | AddJwtAuthentication | UseAuthentication | UseAuthorization | AuthenticationExtensions.cs |
|---------|---------------------|-------------------|------------------|---------------------------|
| Blueprint | Yes | Yes | Yes | Yes — 7 policies |
| Register | Yes | Yes | Yes | Yes — 7 policies |
| Wallet | Yes | Yes | Yes | Yes — 5 policies |
| Tenant | Yes | Yes | Yes | Yes — 10 policies |
| Peer | Yes | Yes | Yes | Yes — 3 policies |
| Validator | **No** | **No** | **No** | **No** |
| API Gateway | Yes | Yes | Yes | No (no service-specific policies) |

**Alternatives Considered**:
- New shared auth library (Sorcha.Auth.Shared) — rejected; ServiceDefaults already serves this role
- IdentityServer/Duende — rejected; Tenant Service already handles token issuance
- Keycloak external IdP — rejected; platform is self-contained

## R2: Service-to-Service Authentication

### Decision: Extend `ServiceAuthClient` with configurable scopes

**Rationale**: The client credentials flow is implemented but scope is hardcoded.

**Current State**:
- `Sorcha.ServiceClients/Auth/ServiceAuthClient.cs` implements `IServiceAuthClient`
- POSTs to `/api/service-auth/token` with `grant_type=client_credentials`
- Thread-safe token caching with `SemaphoreSlim`, refreshes 5 minutes before expiry
- Config keys: `ServiceAuth:ClientId`, `ServiceAuth:ClientSecret`
- Tenant base address: `ServiceClients:TenantService:Address` or fallback `http://tenant-service`
- **Gap**: Scope is hardcoded to `wallets:sign` — needs to be configurable per-service

**Docker-Compose Service Auth Configuration**:
| Service | ClientId | ClientSecret | Status |
|---------|----------|-------------|--------|
| Blueprint | `service-blueprint` | `blueprint-service-secret` | Configured |
| Register | `register-service` | `register-service-secret` | Configured |
| Tenant | `tenant-service` | `tenant-service-secret` | Configured |
| Validator | `validator-service` | `validator-service-secret` | Configured |
| Wallet | — | — | **Missing** |
| Peer | — | — | **Missing** |
| API Gateway | — | — | **Missing** (may not need) |

**Alternatives Considered**:
- mTLS for service-to-service — rejected for now; JWT sufficient within Aspire service mesh
- API keys — rejected; JWT is already the standard, provides richer claims

## R3: Delegation Token Flow

### Decision: Build delegation client in ServiceClients wrapping existing endpoint

**Rationale**: Tenant Service delegation endpoint exists but no client-side integration.

**Current State**:
- `POST /api/service-auth/token/delegated` — AllowAnonymous (uses service credentials in body)
- Tenant Service has `RequireDelegatedAuthority` policy: `token_type=service` + `delegated_user_id` claim
- No `IDelegationTokenClient` in ServiceClients yet
- Blueprint → Wallet → Register workflow needs delegation for user-scoped operations

**Alternatives Considered**:
- Embed delegation logic directly in each service — rejected; DRY principle violated
- Separate delegation microservice — rejected; overkill, Tenant Service already handles it

## R4: Tenant Service Token Capabilities

### Decision: Existing token endpoints are comprehensive; no new endpoints needed

**Rationale**: All required flows (issuance, refresh, revocation, introspection, delegation) already exist.

**Token Endpoints Available**:
| Endpoint | Purpose | Auth |
|----------|---------|------|
| `POST /api/auth/login` | User login → access + refresh tokens | Anonymous |
| `POST /api/auth/token/refresh` | Refresh access token | Anonymous |
| `POST /api/auth/token/revoke` | Revoke a token | Authenticated |
| `POST /api/auth/token/introspect` | Token introspection | RequireService |
| `POST /api/auth/me` | Current user from claims | Authenticated |
| `POST /api/auth/logout` | Revoke current Bearer token | Authenticated |
| `POST /api/service-auth/token` | Unified OAuth2 token endpoint (password, client_credentials, refresh_token) | Anonymous |
| `POST /api/service-auth/token/delegated` | Delegation token issuance | Anonymous |
| `POST /api/service-auth/rotate-secret` | Rotate client secret | Anonymous (requires current secret) |

**Service Principal Management** (Admin-only):
- CRUD operations on `/api/service-principals/`
- Scopes, suspension, reactivation, revocation

## R5: Authorization Policies

### Decision: Standardize policy naming across services; add missing policies

**Rationale**: Services have different policy sets; some critical policies are missing.

**Existing Policy Inventory**:

| Policy | Blueprint | Wallet | Register | Tenant | Peer |
|--------|-----------|--------|----------|--------|------|
| RequireAuthenticated | Yes | Yes | Yes | Yes | Yes |
| RequireService | Yes | Yes | Yes | Yes | Yes |
| RequireOrganizationMember | Yes | Yes | Yes | Yes | — |
| RequireAdministrator | Yes (named "Administrator") | — | — | Yes | — |
| RequireDelegatedAuthority | — | — | — | Yes | — |
| CanManage* | CanManageBlueprints | CanManageWallets | CanManageRegisters | — | CanManagePeers |
| Domain-specific | CanExecuteBlueprints, CanPublishBlueprints | CanUseWallet | CanSubmitTransactions, CanReadTransactions, CanWriteDockets | CanCreateBlockchain, CanPublishBlueprint, RequirePublicUser, RequireAuditor | — |

**Gaps**:
- No `RequireDelegatedAuthority` policy in downstream services (Wallet, Register)
- Wallet: No `RequireOrganizationMember` — has `CanManageWallets` assertion that checks org_id
- Validator: No policies at all
- Blueprint "Administrator" policy should be renamed to `RequireAdministrator` for consistency

## R6: API Gateway Token Forwarding

### Decision: YARP already forwards Authorization headers; verify configuration

**Rationale**: YARP reverse proxy forwards all headers by default including Authorization.

**Current State**:
- API Gateway has `AddJwtAuthentication()` + `UseAuthentication()` + `UseAuthorization()`
- YARP routes defined for all service clusters
- No custom `RequestTransforms` that would strip Authorization headers
- Defense-in-depth: Gateway validates JWT, then downstream service validates again independently

## R7: Peer Service gRPC Authentication

### Decision: JWT via gRPC metadata for authenticated peers; anonymous with lower reputation

**Rationale**: Per spec clarification — both auth models needed for open peer network.

**Current State**:
- Peer Service has `AuthenticationExtensions.cs` with `RequireAuthenticated`, `CanManagePeers`, `RequireService` policies
- gRPC services exist in `GrpcServices/` directory
- No gRPC interceptor for JWT extraction from metadata yet
- Reputation system exists but needs integration with auth status

**Implementation Approach**:
- gRPC server interceptor extracts JWT from `authorization` metadata key
- Authenticated peers: full claims validation, strong reputation score
- Anonymous peers: no claims, lower reputation score
- mTLS: configurable per-peer (already noted as optional in spec)

## Identified Gaps (Action Items)

| # | Gap | Impact | Priority |
|---|-----|--------|----------|
| G1 | Validator Service has no auth | Critical — unprotected service | P1 |
| G2 | ServiceAuthClient scope is hardcoded | Medium — limits service token usefulness | P1 |
| G3 | No IDelegationTokenClient | High — delegation flow unwired | P1 |
| G4 | Missing docker-compose env vars (Wallet, Peer) | Medium — services can't acquire tokens | P1 |
| G5 | No RequireDelegatedAuthority in Wallet/Register | High — delegation verification missing | P1 |
| G6 | gRPC auth interceptor for Peer Service | Medium — peer auth model incomplete | P2 |
| G7 | No integration tests for cross-service auth | High — no verification of auth flows | P1 |
| G8 | Inconsistent policy naming (Blueprint "Administrator" vs "RequireAdministrator") | Low — cosmetic | P3 |
| G9 | No token revocation cache check in services | Medium — FR-006 30-second revocation | P2 |
| G10 | No shared TokenClaims constants | Low — each service redeclares claim names | P2 |
