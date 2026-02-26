# Data Model: Authentication & Authorization Integration

**Feature**: 041-auth-integration
**Date**: 2026-02-25

## Entities

### Service Token (JWT)

A JWT representing a service's identity for service-to-service communication.

| Field | Type | Description |
|-------|------|-------------|
| `sub` | string | Service principal client ID (e.g., `service-blueprint`) |
| `token_type` | string | Always `"service"` |
| `iss` | string | Tenant Service issuer URL |
| `aud` | string[] | Audience URIs |
| `scope` | string | Space-delimited scopes (e.g., `wallets:sign registers:write`) |
| `service_name` | string | Human-readable service name |
| `iat` | long | Issued at (Unix timestamp) |
| `exp` | long | Expiry (Unix timestamp) — default 8 hours |
| `jti` | string | Unique token ID for revocation tracking |

**Source**: Issued by Tenant Service at `POST /api/service-auth/token` (grant_type=client_credentials)

### User Access Token (JWT)

A JWT representing an authenticated user.

| Field | Type | Description |
|-------|------|-------------|
| `sub` | string | User ID (GUID) |
| `token_type` | string | `"user"` |
| `name` | string | Display name |
| `email` | string | Email address |
| `org_id` | string | Organization ID (GUID) |
| `role` | string[] | Roles (e.g., `Administrator`, `Member`) |
| `can_create_blockchain` | string | `"true"` if permitted |
| `can_publish_blueprint` | string | `"true"` if permitted |
| `iss` | string | Tenant Service issuer URL |
| `aud` | string[] | Audience URIs |
| `iat` | long | Issued at |
| `exp` | long | Expiry — default 60 minutes |
| `jti` | string | Unique token ID |

**Source**: Issued by Tenant Service at `POST /api/auth/login` or `POST /api/service-auth/token` (grant_type=password)

### Refresh Token

An opaque token for obtaining new access tokens without re-authentication.

| Field | Type | Description |
|-------|------|-------------|
| `token` | string | Opaque token value |
| `user_id` | string | Associated user ID |
| `expires_at` | DateTime | Expiry — default 24 hours |
| `is_revoked` | bool | Revocation status |
| `created_at` | DateTime | Issuance timestamp |

**Source**: Returned alongside access token from login/refresh endpoints. Stored in Tenant Service database.

### Delegation Token (JWT)

A JWT issued when a service acts on behalf of a user.

| Field | Type | Description |
|-------|------|-------------|
| `sub` | string | Service principal client ID |
| `token_type` | string | `"service"` |
| `delegated_user_id` | string | User ID being represented |
| `delegated_org_id` | string | User's organization ID |
| `scope` | string | Scoped operations (e.g., `wallets:sign`) |
| `iss` | string | Tenant Service issuer URL |
| `aud` | string[] | Audience URIs |
| `iat` | long | Issued at |
| `exp` | long | Expiry — **5 minutes maximum, not refreshable** |
| `jti` | string | Unique token ID |

**Source**: Issued by Tenant Service at `POST /api/service-auth/token/delegated`

### Authorization Policy

A named rule defining what claims are required for endpoint access.

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | Policy identifier (e.g., `RequireService`, `CanManageWallets`) |
| `required_claims` | Claim[] | Claims that must be present |
| `required_roles` | string[] | Roles that must be present (OR logic) |
| `assertion` | func | Custom assertion logic (e.g., org_id OR service token) |

**Standard Policies** (should exist in all services):

| Policy | Claims Required | Description |
|--------|----------------|-------------|
| `RequireAuthenticated` | Any valid JWT | Base authentication check |
| `RequireService` | `token_type=service` | Service-to-service only |
| `RequireOrganizationMember` | `org_id` present | Org-scoped user operations |
| `RequireAdministrator` | `role=Administrator` | Admin operations |
| `RequireDelegatedAuthority` | `token_type=service` + `delegated_user_id` | Service acting on behalf of user |

### Service Principal

The identity configuration assigned to each service for client credentials authentication.

| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Primary key |
| `client_id` | string | Unique client identifier (e.g., `service-blueprint`) |
| `client_secret_hash` | string | BCrypt hash of client secret |
| `display_name` | string | Human-readable name |
| `scopes` | string[] | Permitted scopes |
| `status` | enum | `Active`, `Suspended`, `Revoked` |
| `created_at` | DateTime | Creation timestamp |

**Source**: Managed via Tenant Service at `/api/service-principals/` (admin-only). Already implemented.

## Relationships

```
User ──1:N──▶ User Access Token
User Access Token ──1:1──▶ Refresh Token
Service Principal ──1:N──▶ Service Token
Service Token + User Access Token ──produces──▶ Delegation Token
Authorization Policy ──applied-to──▶ Endpoint
```

## State Transitions

### Token Lifecycle

```
[Not Issued] → [Active] → [Expired]
                  │
                  └──▶ [Revoked] → Token introspection returns active: false
```

### Service Token Cache Lifecycle

```
[Empty] → GetTokenAsync() → [Cached/Active]
           │                      │
           │                 (5 min before expiry)
           │                      │
           └──────────────────▶ [Refreshing] → [Cached/Active]
```
