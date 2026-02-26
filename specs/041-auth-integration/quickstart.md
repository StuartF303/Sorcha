# Quickstart: Authentication & Authorization Integration

**Feature**: 041-auth-integration
**Date**: 2026-02-25

## Prerequisites

- Docker Desktop running
- `.env` file with `JWT_SIGNING_KEY` (or use default dev key)
- Tenant Service bootstrapped with admin user

## Integration Test Scenarios

### Scenario 1: Service-to-Service Authentication (US1)

**Goal**: Verify Blueprint Service can acquire a service token and call Wallet Service.

```bash
# 1. Start services
docker-compose up -d tenant-service blueprint-service wallet-service

# 2. Blueprint Service acquires service token on startup (automatic via ServiceAuthClient)
# Verify in logs:
docker-compose logs blueprint-service | grep "Service token acquired"

# 3. Make authenticated service-to-service call
# Blueprint → Wallet (internal, service token in Authorization header)
# Verify: 200 OK response, not 401
```

**Acceptance Criteria**:
- Service token contains `token_type: service` and `sub: service-blueprint`
- Token is cached and reused for subsequent calls
- Expired token triggers automatic refresh

### Scenario 2: User Authentication (US2)

**Goal**: Verify user can login, access protected endpoints, and refresh tokens.

```bash
# 1. Login
curl -X POST http://localhost:80/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sorcha.local","password":"Dev_Pass_2025!"}'
# Returns: { "accessToken": "...", "refreshToken": "..." }

# 2. Access protected endpoint
curl http://localhost:80/api/blueprints \
  -H "Authorization: Bearer <access_token>"
# Returns: 200 OK with blueprint list

# 3. Access without token
curl http://localhost:80/api/blueprints
# Returns: 401 Unauthorized

# 4. Refresh token
curl -X POST http://localhost:80/api/auth/token/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<refresh_token>"}'
# Returns: new access + refresh tokens
```

**Acceptance Criteria**:
- All protected endpoints return 401 without valid token
- Health/docs/auth endpoints remain accessible without auth
- Token refresh works within expiry window

### Scenario 3: Delegation Token Flow (US3)

**Goal**: Verify Blueprint → Wallet → Register delegation chain.

```bash
# 1. User authenticates
ACCESS_TOKEN=$(curl -s -X POST http://localhost:80/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sorcha.local","password":"Dev_Pass_2025!"}' | jq -r .accessToken)

# 2. User submits action to Blueprint (Blueprint acts on user's behalf)
curl -X POST http://localhost:80/api/instances/{id}/actions/{actionId}/submit \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"payloadData": {...}}'

# Blueprint internally:
#   a. Gets delegation token from Tenant: POST /api/service-auth/token/delegated
#   b. Calls Wallet with delegation token: POST /api/wallets/{walletId}/sign
#   c. Wallet validates delegation token + user ownership

# 3. Verify delegation token claims
# - Contains delegated_user_id matching the user
# - Contains scope "wallets:sign"
# - Expires in 5 minutes
# - Not refreshable
```

**Acceptance Criteria**:
- Delegation token carries both service identity and user identity
- Wallet verifies user owns the wallet being signed with
- Scope mismatch returns 403 Forbidden
- Expired delegation returns 401 Unauthorized

### Scenario 4: Authorization Policy Enforcement (US4)

**Goal**: Verify fine-grained policies enforce access control.

```bash
# 1. Admin user - can manage everything
curl http://localhost:80/api/blueprints \
  -H "Authorization: Bearer <admin_token>"
# Returns: 200 OK

# 2. Regular user - can read but not admin
curl -X DELETE http://localhost:80/api/service-principals/<id> \
  -H "Authorization: Bearer <user_token>"
# Returns: 403 Forbidden (RequireAdministrator)

# 3. Service token - cannot do user operations
curl http://localhost:80/api/wallets \
  -H "Authorization: Bearer <service_token>"
# Returns: 403 Forbidden (CanManageWallets requires org_id)

# 4. Service token without delegation - cannot do user-scoped ops
curl -X POST http://localhost:80/api/wallets/{id}/sign \
  -H "Authorization: Bearer <service_token_no_delegation>"
# Returns: 403 Forbidden (RequireDelegatedAuthority)
```

### Scenario 5: Token Introspection & Revocation (US5)

**Goal**: Verify revoked tokens are rejected within 30 seconds.

```bash
# 1. User authenticates
ACCESS_TOKEN=$(curl -s -X POST http://localhost:80/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sorcha.local","password":"Dev_Pass_2025!"}' | jq -r .accessToken)

# 2. Verify token works
curl http://localhost:80/api/blueprints -H "Authorization: Bearer $ACCESS_TOKEN"
# Returns: 200 OK

# 3. Revoke the token
curl -X POST http://localhost:80/api/auth/token/revoke \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"token\":\"$ACCESS_TOKEN\"}"

# 4. Wait <30 seconds and retry
sleep 5
curl http://localhost:80/api/blueprints -H "Authorization: Bearer $ACCESS_TOKEN"
# Returns: 401 Unauthorized

# 5. Introspect the token (service-only endpoint)
curl -X POST http://localhost:80/api/auth/token/introspect \
  -H "Authorization: Bearer <service_token>" \
  -H "Content-Type: application/json" \
  -d "{\"token\":\"$ACCESS_TOKEN\"}"
# Returns: { "active": false }
```

## Anonymous Endpoint Verification

All services must expose these without authentication:

```bash
# Health checks
curl http://localhost:80/health          # 200 OK
curl http://localhost:80/alive           # 200 OK

# API documentation
curl http://localhost:80/scalar          # 200 OK (HTML)
curl http://localhost:80/openapi/v1.json # 200 OK (JSON)

# Auth endpoints (Tenant Service)
curl -X POST http://localhost:80/api/auth/login           # 200/401 (not 403)
curl -X POST http://localhost:80/api/auth/token/refresh   # 200/401 (not 403)
curl -X POST http://localhost:80/api/service-auth/token   # 200/401 (not 403)
```

## Validator Service Verification (New Auth)

```bash
# Before this feature: Validator endpoints are unprotected
# After this feature:
curl http://localhost:80/api/validators/status
# Returns: 401 Unauthorized (no token)

curl http://localhost:80/api/validators/status \
  -H "Authorization: Bearer <valid_token>"
# Returns: 200 OK
```
