# Authentication Setup Guide

## Overview

The Sorcha platform uses **JWT (JSON Web Token) Bearer authentication** for securing all API endpoints. The **Tenant Service** acts as the authentication authority, issuing tokens that are validated by all other services.

## Architecture

```
┌─────────────────┐
│  Tenant Service │ ──► Issues JWT tokens
└────────┬────────┘
         │
         │ JWT Token
         ▼
┌────────────────────────────────────┐
│  Protected Services                │
│  ├─ Blueprint Service (validates) │
│  ├─ Wallet Service (validates)    │
│  ├─ Register Service (validates)  │
│  └─ Peer Service (validates)     │
└────────────────────────────────────┘
```

## Services Configured (AUTH-002 Complete)

### ✅ Tenant Service
- **Role**: Authentication Authority
- **Functionality**: Issues JWT tokens via `/api/auth/login` and `/api/service-auth/token`
- **Token Types**:
  - User tokens (email/password login)
  - Service tokens (client credentials OAuth2)
  - Delegated tokens (service acting on behalf of user)

### ✅ Blueprint Service
- **Authentication**: JWT Bearer validation
- **Authorization Policies**:
  - `CanManageBlueprints` - Create, update, delete blueprints
  - `CanExecuteBlueprints` - Execute actions and workflows
  - `CanPublishBlueprints` - Publish blueprints
  - `RequireService` - Service-to-service operations

### ✅ Wallet Service
- **Authentication**: JWT Bearer validation
- **Authorization Policies**:
  - `CanManageWallets` - Create wallets, list wallets
  - `CanUseWallet` - Sign, encrypt, decrypt operations
  - `RequireService` - Service-to-service operations

### ✅ Register Service
- **Authentication**: JWT Bearer validation
- **Authorization Policies**:
  - `CanManageRegisters` - Create and configure registers
  - `CanSubmitTransactions` - Submit transactions
  - `CanReadTransactions` - Query transactions
  - `RequireService` - Service-to-service notifications

### ✅ Peer Service
- **Authentication**: JWT Bearer validation
- **Authorization Policies**:
  - `RequireAuthenticated` - Subscribe/unsubscribe/purge register replication
  - `CanManagePeers` - Ban, unban, reset peer failure counts
  - `RequireService` - Service-to-service operations
- **Unauthenticated Endpoints**: Read-only monitoring (peer list, health, stats, cache stats)

## Configuration

### JWT Settings (Required for ALL Services)

Add to `appsettings.json` or `appsettings.Development.json`:

```json
{
  "JwtSettings": {
    "Issuer": "https://tenant.sorcha.io",
    "Audience": "https://api.sorcha.io",
    "SigningKey": "your-secret-key-min-32-characters-REPLACE-THIS-IN-PRODUCTION",
    "AccessTokenLifetimeMinutes": 60,
    "RefreshTokenLifetimeHours": 24,
    "ServiceTokenLifetimeHours": 8,
    "ClockSkewMinutes": 5,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateIssuerSigningKey": true,
    "ValidateLifetime": true
  }
}
```

### Environment Variables (Recommended for Production)

```bash
# JWT Configuration
export JwtSettings__Issuer="https://tenant.your-domain.com"
export JwtSettings__Audience="https://api.your-domain.com"
export JwtSettings__SigningKey="<strong-random-key-from-azure-key-vault>"
```

### Azure Key Vault (Production)

For production deployments, store the signing key in Azure Key Vault:

```bash
# Store signing key
az keyvault secret set \
  --vault-name sorcha-keyvault \
  --name JwtSigningKey \
  --value "<your-strong-random-key>"

# Configure app to use Key Vault
export AZURE_KEY_VAULT_ENDPOINT="https://sorcha-keyvault.vault.azure.net/"
```

## Authentication Flow

### 1. User Authentication (Email/Password)

```http
POST https://tenant.sorcha.io/api/auth/login
Content-Type: application/json

{
  "email": "user@organization.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

### 2. Using the Token

Include the access token in the `Authorization` header for all API requests:

```http
GET https://blueprint.sorcha.io/api/blueprints
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 3. Service-to-Service Authentication (OAuth2 Client Credentials)

```http
POST https://tenant.sorcha.io/api/service-auth/token
Content-Type: application/json

{
  "grantType": "client_credentials",
  "clientId": "blueprint-service",
  "clientSecret": "service-secret",
  "scope": "blueprints:write registers:read"
}
```

## Token Claims

### User Tokens
```json
{
  "sub": "user-id-guid",
  "email": "user@organization.com",
  "name": "User Name",
  "org_id": "organization-id-guid",
  "role": "Administrator",
  "token_type": "user",
  "iss": "https://tenant.sorcha.io",
  "aud": "https://api.sorcha.io",
  "exp": 1735891200,
  "iat": 1735887600
}
```

### Service Tokens
```json
{
  "sub": "service-principal-id",
  "client_id": "blueprint-service",
  "org_id": "organization-id-guid",
  "token_type": "service",
  "scope": "blueprints:write registers:read",
  "iss": "https://tenant.sorcha.io",
  "aud": "https://api.sorcha.io",
  "exp": 1735920000,
  "iat": 1735887600
}
```

## Testing Authentication

### 1. Start Tenant Service

```bash
cd src/Apps/Sorcha.AppHost
dotnet run
```

The Tenant Service will be available at: `https://localhost:7080` (check Aspire dashboard)

### 2. Create a Test User

```bash
# Register a test organization and user
curl -X POST https://localhost:7080/api/organizations \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Organization",
    "subdomain": "test-org"
  }'

# Add a user to the organization
curl -X POST https://localhost:7080/api/organizations/{org-id}/users \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@test-org.com",
    "displayName": "Admin User",
    "externalIdpUserId": "test-123",
    "roles": ["Administrator"]
  }'
```

### 3. Login and Get Token

```bash
curl -X POST https://localhost:7080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@test-org.com",
    "password": "password123"
  }'
```

Save the `accessToken` from the response.

### 4. Test Protected Endpoints

```bash
# Test Blueprint Service
curl https://localhost:7081/api/blueprints \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Test Wallet Service
curl https://localhost:7082/api/v1/wallets \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"

# Test Register Service
curl https://localhost:7083/api/registers \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## Authorization Policies

### Blueprint Service

| Policy | Description | Required Claims |
|--------|-------------|-----------------|
| `CanManageBlueprints` | Create, update, delete blueprints | `org_id` OR `token_type=service` |
| `CanExecuteBlueprints` | Execute actions | Authenticated user |
| `CanPublishBlueprints` | Publish blueprints | `can_publish_blueprint=true` OR `role=Administrator` |
| `RequireService` | Service operations | `token_type=service` |

### Wallet Service

| Policy | Description | Required Claims |
|--------|-------------|-----------------|
| `CanManageWallets` | Create, list wallets | `org_id` OR `token_type=service` |
| `CanUseWallet` | Sign, encrypt, decrypt | Authenticated user |
| `RequireService` | Service operations | `token_type=service` |

### Register Service

| Policy | Description | Required Claims |
|--------|-------------|-----------------|
| `CanManageRegisters` | Create registers | `org_id` OR `token_type=service` |
| `CanSubmitTransactions` | Submit transactions | Authenticated user |
| `CanReadTransactions` | Query transactions | Authenticated user |
| `RequireService` | Notifications | `token_type=service` |

### Peer Service

| Policy | Description | Required Claims |
|--------|-------------|-----------------|
| `RequireAuthenticated` | Subscribe/unsubscribe/purge registers | Authenticated user |
| `CanManagePeers` | Ban, unban, reset peers | `org_id` OR `token_type=service` |
| `RequireService` | Service operations | `token_type=service` |

## Security Best Practices

### Development
- ✅ Use a development signing key (min 32 characters)
- ✅ Store keys in `appsettings.Development.json` (gitignored)
- ✅ Use HTTPS for local development
- ✅ Test with both user and service tokens

### Production
- ✅ **NEVER** commit signing keys to source control
- ✅ Use Azure Key Vault or AWS Secrets Manager
- ✅ Rotate signing keys regularly (every 90 days recommended)
- ✅ Use strong random keys (256+ bits)
- ✅ Enable HTTPS everywhere
- ✅ Set appropriate token lifetimes
- ✅ Monitor failed authentication attempts
- ✅ Implement token revocation for compromised tokens

## Troubleshooting

### 401 Unauthorized Errors

**Symptom**: API returns 401 Unauthorized

**Common Causes:**
1. **Missing or invalid token** - Check Authorization header format
2. **Expired token** - Request a new token
3. **Wrong signing key** - Ensure all services use the same SigningKey
4. **Wrong issuer/audience** - Check JwtSettings match across services

**Solution:**
```bash
# Check token expiration
echo "YOUR_TOKEN" | base64 -d | jq .exp

# Verify signing key matches
grep SigningKey appsettings.*.json
```

### 403 Forbidden Errors

**Symptom**: Token validates but operation denied

**Common Causes:**
1. **Missing required claims** - Check token has needed claims (org_id, role, etc.)
2. **Insufficient permissions** - User lacks required role
3. **Wrong token type** - Using user token for service operation or vice versa

**Solution:**
```bash
# Decode and inspect token claims
echo "YOUR_TOKEN" | jwt decode -

# Check authorization policy requirements
```

### Token Not Validating

**Symptom**: Services cannot validate tokens from Tenant Service

**Checklist:**
- [ ] All services have same `JwtSettings:SigningKey`
- [ ] All services have same `JwtSettings:Issuer`
- [ ] All services have same `JwtSettings:Audience`
- [ ] JWT Bearer package installed on all services
- [ ] `app.UseAuthentication()` called before `app.UseAuthorization()`

## Next Steps

After authentication is configured:

1. **API Gateway Integration** - Configure YARP gateway for centralized auth
2. **Token Refresh** - Implement automatic token refresh on client side
3. **Multi-tenancy** - Enforce org_id isolation in data queries
4. **Audit Logging** - Log all authentication and authorization events
5. **Rate Limiting** - Implement rate limiting per user/organization

## References

- **JWT Specification**: https://jwt.io/
- **ASP.NET Core Authentication**: https://learn.microsoft.com/aspnet/core/security/authentication/
- **Azure Key Vault**: https://learn.microsoft.com/azure/key-vault/
- **OAuth 2.0 Client Credentials**: https://oauth.net/2/grant-types/client-credentials/

---

**Status**: ✅ AUTH-002 Complete (All services integrated)
**Last Updated**: 2026-02-08
**Version**: 1.1
