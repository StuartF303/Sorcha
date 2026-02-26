# API Contract: Service-to-Service Authentication

**Feature**: 041-auth-integration
**Date**: 2026-02-25

## Existing Endpoints (Tenant Service — no changes needed)

### POST /api/service-auth/token

Unified OAuth2 token endpoint supporting multiple grant types.

**Auth**: Anonymous (credentials in body)

**Request** (form-urlencoded or JSON):
```
grant_type=client_credentials
client_id=service-blueprint
client_secret=blueprint-service-secret
scope=wallets:sign registers:write
```

**Response** (200 OK):
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiI...",
  "token_type": "Bearer",
  "expires_in": 28800,
  "scope": "wallets:sign registers:write"
}
```

**Error** (401 Unauthorized):
```json
{
  "error": "invalid_client",
  "error_description": "Invalid client credentials"
}
```

### POST /api/auth/token/introspect

Token introspection for real-time validation.

**Auth**: RequireService (service token required)

**Request**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiI..."
}
```

**Response** (200 OK — active):
```json
{
  "active": true,
  "sub": "user-id-guid",
  "token_type": "user",
  "scope": "wallets:sign",
  "exp": 1740528000,
  "iat": 1740524400,
  "iss": "https://tenant.sorcha.io"
}
```

**Response** (200 OK — inactive/revoked):
```json
{
  "active": false
}
```

## New Client Interface: IServiceAuthClient (modification)

### Current Interface
```csharp
public interface IServiceAuthClient
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}
```

### Updated Interface (add scope configuration)
```csharp
public interface IServiceAuthClient
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}
```

**Configuration change** — ServiceAuthClient reads `ServiceAuth:Scopes` from config instead of hardcoded `wallets:sign`:

```json
{
  "ServiceAuth": {
    "ClientId": "service-blueprint",
    "ClientSecret": "blueprint-service-secret",
    "Scopes": "wallets:sign registers:write blueprints:manage"
  }
}
```

## Service Auth Configuration Per Service

| Service | ClientId | Scopes |
|---------|----------|--------|
| Blueprint | `service-blueprint` | `wallets:sign registers:write blueprints:manage` |
| Wallet | `service-wallet` | `registers:write` |
| Register | `register-service` | `validators:notify` |
| Validator | `validator-service` | `registers:write registers:read` |
| Peer | `service-peer` | `registers:write registers:read` |

## Anonymous Endpoints (no auth required)

Per FR-013, these endpoints remain accessible without authentication across all services:

| Pattern | Purpose |
|---------|---------|
| `/health` | Health check |
| `/alive` | Liveness check |
| `/scalar/*` | Scalar API docs |
| `/openapi/*` | OpenAPI specification |
| `/api/auth/login` | User authentication |
| `/api/auth/token/refresh` | Token refresh |
| `/api/service-auth/token` | Service token acquisition |
| `/api/service-auth/token/delegated` | Delegation token acquisition |
