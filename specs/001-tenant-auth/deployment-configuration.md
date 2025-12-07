# Deployment Configuration Guide

**Feature**: 001-tenant-auth
**Date**: 2025-12-07
**Status**: Specification
**Audience**: DevOps, Platform Administrators, Developers

---

## Overview

Sorcha supports multiple deployment topologies to accommodate different organizational needs:

| Deployment Type | Description | Example Domain |
|-----------------|-------------|----------------|
| **SaaS** | Multi-tenant hosted by Sorcha | `tenant.sorcha.io` |
| **Enterprise** | Self-hosted single organization | `auth.big-corporate.com` |
| **HostedTenant** | Dedicated subdomain on SaaS | `small-corp.tenants.sorcha.io` |

Each deployment operates as an independent Sorcha installation with its own:
- Deployment ID (unique GUID)
- Token issuer URL
- Signing keys
- Database(s)
- Optional federation with other deployments

---

## Deployment Configuration Structure

### Environment Variables

All deployment configuration is loaded from environment variables or `appsettings.json`. Configuration is **immutable at runtime** - changes require service restart.

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `SORCHA_DEPLOYMENT_ID` | Yes | Unique GUID for this deployment | `550e8400-e29b-41d4-a716-446655440000` |
| `SORCHA_DEPLOYMENT_NAME` | Yes | Human-readable name | `Sorcha SaaS Production` |
| `SORCHA_DEPLOYMENT_TYPE` | Yes | `SaaS`, `Enterprise`, or `HostedTenant` | `SaaS` |
| `SORCHA_BASE_DOMAIN` | Yes | Base domain for URL construction | `sorcha.io` |
| `SORCHA_TENANT_SERVICE_URL` | Yes | Full URL to Tenant Service | `https://tenant.sorcha.io` |
| `SORCHA_TOKEN_ISSUER` | Yes | JWT `iss` claim value | `https://tenant.sorcha.io` |
| `SORCHA_ALLOWED_AUDIENCES` | Yes | Comma-separated audience list | `https://api.sorcha.io,https://gateway.sorcha.io` |
| `SORCHA_SIGNING_KEY_SOURCE` | Yes | `Local`, `AzureKeyVault`, or `AwsKms` | `AzureKeyVault` |
| `SORCHA_FEDERATION_ENABLED` | No | Enable cross-deployment auth | `true` |

### Signing Key Configuration

#### Local Development (DPAPI)

```bash
SORCHA_SIGNING_KEY_SOURCE=Local
SORCHA_SIGNING_KEY_PATH=/app/keys/signing-key.json
```

#### Azure Key Vault (Production)

```bash
SORCHA_SIGNING_KEY_SOURCE=AzureKeyVault
SORCHA_AKV_URI=https://sorcha-prod.vault.azure.net/
SORCHA_AKV_KEY_NAME=jwt-signing-key
```

#### AWS KMS

```bash
SORCHA_SIGNING_KEY_SOURCE=AwsKms
SORCHA_AWS_REGION=us-east-1
SORCHA_AWS_KMS_KEY_ID=arn:aws:kms:us-east-1:123456789:key/abcd-1234
```

---

## Deployment Type: SaaS

The master SaaS deployment hosts multiple organizations under a shared infrastructure.

### Configuration Example

```json
{
  "Deployment": {
    "DeploymentId": "00000000-0000-0000-0000-000000000001",
    "DeploymentName": "Sorcha SaaS Production",
    "DeploymentType": "SaaS",
    "BaseDomain": "sorcha.io",
    "TenantServiceUrl": "https://tenant.sorcha.io",
    "TokenIssuer": "https://tenant.sorcha.io",
    "AllowedAudiences": [
      "https://api.sorcha.io",
      "https://gateway.sorcha.io"
    ],
    "SigningKey": {
      "Source": "AzureKeyVault",
      "KeyVaultUri": "https://sorcha-prod.vault.azure.net/",
      "KeyName": "jwt-signing-key"
    },
    "Federation": {
      "Enabled": true,
      "TrustedDeployments": []
    }
  }
}
```

### Environment Variables

```bash
# Core deployment settings
export SORCHA_DEPLOYMENT_ID="00000000-0000-0000-0000-000000000001"
export SORCHA_DEPLOYMENT_NAME="Sorcha SaaS Production"
export SORCHA_DEPLOYMENT_TYPE="SaaS"
export SORCHA_BASE_DOMAIN="sorcha.io"
export SORCHA_TENANT_SERVICE_URL="https://tenant.sorcha.io"
export SORCHA_TOKEN_ISSUER="https://tenant.sorcha.io"
export SORCHA_ALLOWED_AUDIENCES="https://api.sorcha.io,https://gateway.sorcha.io"

# Signing key (Azure Key Vault)
export SORCHA_SIGNING_KEY_SOURCE="AzureKeyVault"
export SORCHA_AKV_URI="https://sorcha-prod.vault.azure.net/"
export SORCHA_AKV_KEY_NAME="jwt-signing-key"

# Federation
export SORCHA_FEDERATION_ENABLED="true"
```

### URL Patterns

| Service | URL Pattern | Example |
|---------|-------------|---------|
| Tenant Service | `https://tenant.{BaseDomain}` | `https://tenant.sorcha.io` |
| API Gateway | `https://api.{BaseDomain}` | `https://api.sorcha.io` |
| Org Subdomains | `https://{subdomain}.{BaseDomain}` | `https://acme.sorcha.io` |
| JWKS Endpoint | `https://tenant.{BaseDomain}/.well-known/jwks.json` | `https://tenant.sorcha.io/.well-known/jwks.json` |

---

## Deployment Type: Enterprise

Enterprise deployments are self-hosted by organizations on their own infrastructure.

### Configuration Example

```json
{
  "Deployment": {
    "DeploymentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "DeploymentName": "Big Corp Production",
    "DeploymentType": "Enterprise",
    "BaseDomain": "big-corporate.com",
    "TenantServiceUrl": "https://auth.big-corporate.com",
    "TokenIssuer": "https://auth.big-corporate.com",
    "AllowedAudiences": [
      "https://sorcha.big-corporate.com",
      "https://api.big-corporate.com"
    ],
    "SigningKey": {
      "Source": "AwsKms",
      "Region": "eu-west-1",
      "KeyId": "arn:aws:kms:eu-west-1:987654321:key/xyz-9876"
    },
    "Federation": {
      "Enabled": true,
      "TrustedDeployments": [
        {
          "DeploymentId": "00000000-0000-0000-0000-000000000001",
          "DeploymentName": "Sorcha SaaS Production",
          "TokenIssuer": "https://tenant.sorcha.io",
          "JwksUrl": "https://tenant.sorcha.io/.well-known/jwks.json"
        }
      ]
    }
  }
}
```

### Environment Variables

```bash
# Core deployment settings
export SORCHA_DEPLOYMENT_ID="a1b2c3d4-e5f6-7890-abcd-ef1234567890"
export SORCHA_DEPLOYMENT_NAME="Big Corp Production"
export SORCHA_DEPLOYMENT_TYPE="Enterprise"
export SORCHA_BASE_DOMAIN="big-corporate.com"
export SORCHA_TENANT_SERVICE_URL="https://auth.big-corporate.com"
export SORCHA_TOKEN_ISSUER="https://auth.big-corporate.com"
export SORCHA_ALLOWED_AUDIENCES="https://sorcha.big-corporate.com,https://api.big-corporate.com"

# Signing key (AWS KMS)
export SORCHA_SIGNING_KEY_SOURCE="AwsKms"
export SORCHA_AWS_REGION="eu-west-1"
export SORCHA_AWS_KMS_KEY_ID="arn:aws:kms:eu-west-1:987654321:key/xyz-9876"

# Federation with SaaS
export SORCHA_FEDERATION_ENABLED="true"
```

### Key Considerations

1. **DNS Configuration**: Enterprise deployments require DNS records pointing to their infrastructure
2. **TLS Certificates**: Must provision and manage their own certificates
3. **Database Isolation**: Separate database instance (not shared with SaaS)
4. **Key Management**: Can use their preferred KMS (AWS, Azure, HashiCorp Vault)
5. **Federation**: Optional trust relationship with SaaS for cross-org collaboration

---

## Deployment Type: HostedTenant

Hosted tenants get a dedicated subdomain on the SaaS infrastructure while maintaining their own configuration.

### Configuration Example

```json
{
  "Deployment": {
    "DeploymentId": "f0e1d2c3-b4a5-9687-fedc-ba0987654321",
    "DeploymentName": "Small Corp (Hosted)",
    "DeploymentType": "HostedTenant",
    "BaseDomain": "small-corp.tenants.sorcha.io",
    "TenantServiceUrl": "https://auth.small-corp.tenants.sorcha.io",
    "TokenIssuer": "https://auth.small-corp.tenants.sorcha.io",
    "AllowedAudiences": [
      "https://api.small-corp.tenants.sorcha.io",
      "https://api.sorcha.io"
    ],
    "SigningKey": {
      "Source": "AzureKeyVault",
      "KeyVaultUri": "https://sorcha-prod.vault.azure.net/",
      "KeyName": "jwt-signing-key-small-corp"
    },
    "ParentDeployment": {
      "DeploymentId": "00000000-0000-0000-0000-000000000001",
      "TokenIssuer": "https://tenant.sorcha.io"
    },
    "Federation": {
      "Enabled": true,
      "TrustedDeployments": [
        {
          "DeploymentId": "00000000-0000-0000-0000-000000000001",
          "DeploymentName": "Sorcha SaaS Production",
          "TokenIssuer": "https://tenant.sorcha.io",
          "JwksUrl": "https://tenant.sorcha.io/.well-known/jwks.json"
        }
      ]
    }
  }
}
```

### Key Differences from SaaS

| Aspect | SaaS | HostedTenant |
|--------|------|--------------|
| Domain | `tenant.sorcha.io` | `auth.small-corp.tenants.sorcha.io` |
| Signing Key | Shared SaaS key | Dedicated key per tenant |
| Token Issuer | Single issuer | Tenant-specific issuer |
| Database | Shared with schema isolation | Can be dedicated or shared |
| Billing | Standard SaaS pricing | Premium tier |

---

## Cross-Deployment Federation

Federation enables users from one Sorcha deployment to authenticate with services on another deployment.

### Establishing Trust

1. **Administrator Action**: Deployment admin adds remote deployment to trusted list
2. **JWKS Exchange**: Local deployment caches remote JWKS public keys
3. **Token Validation**: Services validate tokens from trusted deployments

### Federated Token Flow

```
┌─────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│   User at   │     │  Big Corp Tenant    │     │  Sorcha SaaS        │
│  Big Corp   │     │  (Enterprise)       │     │  (Federated Target) │
└─────────────┘     └─────────────────────┘     └─────────────────────┘
       │                      │                           │
       │  1. Authenticate     │                           │
       │─────────────────────>│                           │
       │                      │                           │
       │  2. JWT issued       │                           │
       │  iss: auth.big-      │                           │
       │  corporate.com       │                           │
       │<─────────────────────│                           │
       │                      │                           │
       │  3. Access SaaS resource with Big Corp token     │
       │─────────────────────────────────────────────────>│
       │                      │                           │
       │                      │  4. Validate token:       │
       │                      │  - Check iss in trusted   │
       │                      │    deployments            │
       │                      │  - Fetch/cache JWKS       │
       │                      │  - Verify signature       │
       │                      │  - Check federated=true   │
       │                      │                           │
       │  5. Resource returned (with federated context)   │
       │<─────────────────────────────────────────────────│
```

### Token Claims for Federated Tokens

When a service receives a token from a federated deployment:

```json
{
  "sub": "user-uuid-at-big-corp",
  "iss": "https://auth.big-corporate.com",
  "aud": ["https://api.sorcha.io"],
  "deployment_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "deployment_name": "Big Corp Production",
  "federated": true,
  "org_id": "org-uuid-at-big-corp",
  "token_type": "user",
  "roles": ["member"],
  "exp": 1733234567,
  "iat": 1733230967
}
```

The `federated: true` flag indicates this token was issued by a remote deployment and may have different authorization rules applied.

### JWKS Caching Strategy

| Setting | Value | Description |
|---------|-------|-------------|
| Cache TTL | 1 hour | How long to cache JWKS before refresh |
| Grace Period | 15 minutes | Continue accepting tokens during refresh |
| Retry Interval | 5 minutes | Retry failed JWKS fetches |
| Circuit Breaker | 3 failures | Temporarily disable after repeated failures |

---

## Service Configuration

### All Services (JWT Validation)

Every Sorcha service needs to validate JWTs. Configuration is shared via `Sorcha.ServiceDefaults`:

```csharp
// In Program.cs
builder.AddServiceDefaults();
builder.AddJwtAuthentication(); // Reads deployment config automatically
```

### appsettings.json Structure

```json
{
  "Deployment": {
    "DeploymentId": "...",
    "DeploymentType": "...",
    "TokenIssuer": "...",
    "AllowedAudiences": ["..."]
  },
  "JwtValidation": {
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00",
    "RequireSignedTokens": true
  },
  "Federation": {
    "Enabled": true,
    "JwksCacheTtlMinutes": 60,
    "JwksCacheGracePeriodMinutes": 15
  }
}
```

---

## Database Configuration

### Per-Deployment Database Strategy

| Deployment Type | Database Strategy | Connection String Pattern |
|-----------------|-------------------|---------------------------|
| SaaS | Shared database, schema-per-org | `Host=db.sorcha.io;Database=sorcha_tenant` |
| Enterprise | Dedicated database | `Host=db.big-corporate.com;Database=sorcha` |
| HostedTenant | Shared or dedicated (configurable) | Varies |

### Schema Naming

Organizations within a deployment get isolated schemas:

```
sorcha_tenant (database)
├── public (shared entities: Organizations, FederatedDeployments)
├── org_acme (Acme Corp data)
├── org_contoso (Contoso data)
└── org_fabrikam (Fabrikam data)
```

---

## Kubernetes Deployment

### ConfigMap Example

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: sorcha-deployment-config
data:
  SORCHA_DEPLOYMENT_ID: "00000000-0000-0000-0000-000000000001"
  SORCHA_DEPLOYMENT_NAME: "Sorcha SaaS Production"
  SORCHA_DEPLOYMENT_TYPE: "SaaS"
  SORCHA_BASE_DOMAIN: "sorcha.io"
  SORCHA_TENANT_SERVICE_URL: "https://tenant.sorcha.io"
  SORCHA_TOKEN_ISSUER: "https://tenant.sorcha.io"
  SORCHA_ALLOWED_AUDIENCES: "https://api.sorcha.io,https://gateway.sorcha.io"
  SORCHA_FEDERATION_ENABLED: "true"
```

### Secret Example (Azure Key Vault reference)

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sorcha-signing-key-config
type: Opaque
stringData:
  SORCHA_SIGNING_KEY_SOURCE: "AzureKeyVault"
  SORCHA_AKV_URI: "https://sorcha-prod.vault.azure.net/"
  SORCHA_AKV_KEY_NAME: "jwt-signing-key"
```

---

## Local Development

For local development, use simplified configuration:

```bash
# .env file for local development
SORCHA_DEPLOYMENT_ID=dev-00000000-0000-0000-0000-000000000001
SORCHA_DEPLOYMENT_NAME=Local Development
SORCHA_DEPLOYMENT_TYPE=SaaS
SORCHA_BASE_DOMAIN=localhost
SORCHA_TENANT_SERVICE_URL=https://localhost:7080
SORCHA_TOKEN_ISSUER=https://localhost:7080
SORCHA_ALLOWED_AUDIENCES=https://localhost:7081,https://localhost:7082
SORCHA_SIGNING_KEY_SOURCE=Local
SORCHA_SIGNING_KEY_PATH=./keys/dev-signing-key.json
SORCHA_FEDERATION_ENABLED=false
```

### Generate Local Signing Key

```bash
# Generate RSA key pair for local development
openssl genrsa -out ./keys/dev-signing-key.pem 2048
openssl rsa -in ./keys/dev-signing-key.pem -pubout -out ./keys/dev-signing-key.pub.pem

# Convert to JSON format for JWKS
dotnet run --project tools/KeyConverter -- ./keys/dev-signing-key.pem ./keys/dev-signing-key.json
```

---

## Validation Checklist

Before deploying, verify:

- [ ] `SORCHA_DEPLOYMENT_ID` is a valid, unique GUID
- [ ] `SORCHA_TOKEN_ISSUER` URL is publicly accessible (for JWKS)
- [ ] Signing key is properly configured and accessible
- [ ] `SORCHA_ALLOWED_AUDIENCES` includes all consuming services
- [ ] Federation trusted deployments have valid JWKS URLs
- [ ] Database connection string is correct for deployment type
- [ ] TLS certificates are valid for all service domains

---

## Troubleshooting

### Token Validation Fails with "Invalid Issuer"

**Cause**: Token issuer doesn't match local deployment or trusted federations.

**Solution**:
1. Check `SORCHA_TOKEN_ISSUER` matches what's in the token
2. If federated, verify remote deployment is in trusted list
3. Check for trailing slashes in issuer URLs (must match exactly)

### JWKS Fetch Fails for Federated Deployment

**Cause**: Cannot retrieve public keys from remote deployment.

**Solution**:
1. Verify `JwksUrl` is correct and accessible
2. Check network/firewall allows outbound HTTPS
3. Verify remote deployment is running and healthy
4. Check JWKS cache hasn't expired during outage

### "deployment_id" Claim Missing from Token

**Cause**: Token was issued before deployment-aware claims were implemented.

**Solution**:
1. Have user re-authenticate to get new token
2. Check Tenant Service version includes deployment claims
3. Verify `SORCHA_DEPLOYMENT_ID` is set on Tenant Service

---

## Related Documents

- [Specification](spec.md) - Full authentication requirements
- [Data Model](data-model.md) - Database schema and entities
- [Quickstart](quickstart.md) - Local development setup
- [Research](research.md) - Technology decisions and rationale
