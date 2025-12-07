# Quickstart: Tenant Service Local Development

**Feature**: 001-tenant-auth
**Date**: 2025-11-22
**Audience**: Developers setting up local development environment

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop (for PostgreSQL and Redis)
- Visual Studio 2025, VS Code, or Rider
- Git

## Quick Start (5 Minutes)

### 1. Clone and Navigate

```bash
cd C:\Projects\Sorcha
git checkout 001-tenant-auth
```

### 2. Start Dependencies

```bash
# Start PostgreSQL and Redis using Docker Compose
docker-compose up -d postgres redis
```

### 3. Configure appsettings

Create `src/Services/Sorcha.Tenant.Service/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "TenantDatabase": "Host=localhost;Port=5432;Database=sorcha_tenant;Username=sorcha_user;Password=dev_password;Include Error Detail=true"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "JwtSettings": {
    "Issuer": "https://localhost:7080",
    "Audience": ["https://localhost:7081", "https://localhost:7082"],
    "AccessTokenLifetimeMinutes": 60,
    "RefreshTokenLifetimeHours": 24
  },
  "Fido2": {
    "ServerDomain": "localhost",
    "ServerName": "Sorcha (Development)",
    "Origins": ["https://localhost:7080"]
  }
}
```

### 4. Run Migrations

```bash
cd src/Services/Sorcha.Tenant.Service
dotnet ef database update
```

### 5. Run the Service

```bash
dotnet run
```

Service will start at `https://localhost:7080`

### 6. Test with Swagger/Scalar

Open browser to `https://localhost:7080/scalar` for interactive API documentation.

## Detailed Setup Guide

### Database Setup

**Docker Compose** (`docker-compose.yml` in repo root):

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: sorcha_user
      POSTGRES_PASSWORD: dev_password
      POSTGRES_DB: sorcha_tenant
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
```

**Manual PostgreSQL Setup**:

```sql
CREATE DATABASE sorcha_tenant;
CREATE USER sorcha_user WITH PASSWORD 'dev_password';
GRANT ALL PRIVILEGES ON DATABASE sorcha_tenant TO sorcha_user;
```

### Create Test Organization

Use Scalar UI at `/scalar` or curl:

```bash
# Create organization (auto-generates schema)
curl -X POST https://localhost:7080/api/admin/organizations \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Organization",
    "subdomain": "testorg",
    "branding": {
      "logoUrl": "https://example.com/logo.png",
      "primaryColor": "#0078D4",
      "secondaryColor": "#50E6FF"
    }
  }'

# Response:
# {
#   "id": "org-uuid-here",
#   "name": "Test Organization",
#   "subdomain": "testorg",
#   "status": "Active",
#   "createdAt": "2025-11-22T10:00:00Z"
# }
```

### Configure Mock External IDP (for testing)

For local testing without real Azure Entra/AWS Cognito, use a mock OIDC server:

**Option 1: IdentityServer Test Server** (recommended for local dev)

```bash
dotnet new -i Duende.IdentityServer.Templates
dotnet new isempty -n MockIdp
cd MockIdp
dotnet run
```

Configure test organization to use mock IDP:

```bash
curl -X PUT https://localhost:7080/api/admin/organizations/{orgId}/idp \
  -H "Content-Type: application/json" \
  -d '{
    "providerType": "GenericOidc",
    "issuerUrl": "https://localhost:5001",
    "clientId": "test-client",
    "clientSecret": "test-secret",
    "scopes": ["openid", "profile", "email"],
    "metadataUrl": "https://localhost:5001/.well-known/openid-configuration"
  }'
```

**Option 2: Azure Entra (requires Azure subscription)**

1. Create Azure Entra app registration
2. Set redirect URI: `https://localhost:7080/api/auth/callback`
3. Copy Application (client) ID and create client secret
4. Use `/configure-idp` endpoint with real Azure values

### Test Authentication Flow

**OAuth2 Login Flow**:

```bash
# 1. Initiate login (browser redirect)
open https://localhost:7080/api/auth/login?org=testorg

# 2. User authenticates with external IDP (mock or real)

# 3. Callback receives authorization code and exchanges for token

# 4. JWT token returned to application
```

**PassKey Registration** (requires browser with WebAuthn support):

```bash
# 1. Request registration options
curl -X POST https://localhost:7080/api/auth/passkey/register-options \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser@example.com",
    "displayName": "Test User"
  }'

# 2. Browser calls navigator.credentials.create() with options

# 3. Complete registration with attestation
curl -X POST https://localhost:7080/api/auth/passkey/register \
  -H "Content-Type: application/json" \
  -d '{
    "id": "credential-id-base64",
    "rawId": "raw-credential-id-base64",
    "response": {
      "attestationObject": "attestation-base64",
      "clientDataJSON": "client-data-base64"
    },
    "type": "public-key"
  }'

# 4. JWT token returned
```

**Service Token Request** (client credentials):

```bash
curl -X POST https://localhost:7080/api/auth/token/service \
  -H "Content-Type: application/json" \
  -d '{
    "grant_type": "client_credentials",
    "client_id": "service-blueprint",
    "client_secret": "service-secret-here",
    "scope": "wallet:sign register:commit"
  }'

# Response:
# {
#   "access_token": "eyJhbGci...",
#   "token_type": "Bearer",
#   "expires_in": 28800
# }
```

### Validate JWT Tokens

```bash
# Validate token
curl -X POST https://localhost:7080/api/auth/token/validate \
  -H "Content-Type: application/json" \
  -d '{
    "token": "eyJhbGciOiJSUzI1NiIs..."
  }'

# Response:
# {
#   "valid": true,
#   "claims": {
#     "sub": "user-uuid",
#     "org_id": "org-uuid",
#     "roles": ["member"],
#     "permitted_blockchains": ["blockchain-id-1"]
#   }
# }
```

## Testing

### Run Unit Tests

```bash
cd tests/Sorcha.Tenant.Service.Tests
dotnet test
```

### Run Integration Tests (with Testcontainers)

```bash
cd tests/Sorcha.Tenant.Service.IntegrationTests
dotnet test

# Testcontainers will automatically start PostgreSQL and Redis containers
```

### Run Performance Tests

```bash
cd tests/Sorcha.Tenant.Service.PerformanceTests
dotnet run

# NBomber will simulate load and generate performance report
```

## Troubleshooting

### Database Connection Issues

**Error**: "Connection refused" or "password authentication failed"

**Solution**:
```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Check connection string in appsettings.Development.json
# Verify username, password, and database name match docker-compose.yml
```

### Redis Connection Issues

**Error**: "It was not possible to connect to the redis server(s)"

**Solution**:
```bash
# Check Redis is running
docker ps | grep redis

# Test Redis connection
redis-cli ping
# Should return: PONG
```

### Migration Failures

**Error**: "Migrations pending" or "Schema does not exist"

**Solution**:
```bash
# Drop and recreate database
docker-compose down -v
docker-compose up -d postgres redis

# Re-run migrations
cd src/Services/Sorcha.Tenant.Service
dotnet ef database update
```

### IDP Configuration Errors

**Error**: "Unable to retrieve OIDC configuration"

**Solution**:
- Verify `metadataUrl` is accessible: `curl https://idp-url/.well-known/openid-configuration`
- Check firewall/network settings
- For Azure Entra: verify tenant ID is correct
- For local mock IDP: ensure it's running on expected port

### Token Validation Failures

**Error**: "Invalid signature" or "Token has expired"

**Solution**:
- Check system clock synchronization (token validation uses timestamps)
- Verify JWKS endpoint is accessible: `curl https://localhost:7080/.well-known/jwks.json`
- Ensure token is not expired (check `exp` claim)
- Check Redis is running (revocation list check)

## Environment Variables

For production-like testing, use environment variables instead of appsettings:

```bash
# Database and cache
export ConnectionStrings__TenantDatabase="Host=localhost;Port=5432;Database=sorcha_tenant;Username=sorcha_user;Password=dev_password"
export Redis__ConnectionString="localhost:6379"

# Deployment configuration (see deployment-configuration.md for details)
export SORCHA_DEPLOYMENT_ID="dev-00000000-0000-0000-0000-000000000001"
export SORCHA_DEPLOYMENT_NAME="Local Development"
export SORCHA_DEPLOYMENT_TYPE="SaaS"
export SORCHA_BASE_DOMAIN="localhost"
export SORCHA_TENANT_SERVICE_URL="https://localhost:7080"
export SORCHA_TOKEN_ISSUER="https://localhost:7080"
export SORCHA_ALLOWED_AUDIENCES="https://localhost:7081,https://localhost:7082"
export SORCHA_SIGNING_KEY_SOURCE="Local"
export SORCHA_FEDERATION_ENABLED="false"

# JWT settings
export JwtSettings__AccessTokenLifetimeMinutes="60"

dotnet run
```

## VS Code Launch Configuration

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Tenant Service (Development)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Services/Sorcha.Tenant.Service/bin/Debug/net10.0/Sorcha.Tenant.Service.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Services/Sorcha.Tenant.Service",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7080;http://localhost:7081"
      }
    }
  ]
}
```

## Next Steps

1. **Implement Endpoints**: Follow [plan.md](plan.md) for implementation order
2. **Add Tests**: Write tests alongside implementation (TDD)
3. **Integrate with Blueprint Service**: Test service-to-service authentication
4. **Deploy to .NET Aspire**: Register Tenant Service in `Sorcha.AppHost`

## Useful Commands

```bash
# View database schema
psql -h localhost -U sorcha_user -d sorcha_tenant -c "\dt public.*"

# View organization schemas
psql -h localhost -U sorcha_user -d sorcha_tenant -c "\dn"

# Check Redis keys
redis-cli keys "*"

# View audit logs
psql -h localhost -U sorcha_user -d sorcha_tenant -c "SELECT * FROM org_testorg.\"AuditLogEntries\" ORDER BY \"Timestamp\" DESC LIMIT 10"

# Generate EF migration
dotnet ef migrations add MigrationName --context TenantDbContext

# View JWKS public keys
curl https://localhost:7080/.well-known/jwks.json | jq
```

## Resources

- [Specification](spec.md)
- [Implementation Plan](plan.md)
- [Data Model](data-model.md)
- [Deployment Configuration](deployment-configuration.md)
- [API Contracts](contracts/)
- [Research Decisions](research.md)
