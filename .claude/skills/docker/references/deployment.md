# Deployment Reference

## Contents
- Environment Configuration
- Port Assignments
- Production Checklist
- Azure Container Apps
- Volume and Secret Management

## Environment Configuration

### Development Environments

| Environment | Purpose | Protocol | Config Source |
|-------------|---------|----------|---------------|
| Aspire | Local debugging | HTTPS | `launchSettings.json` |
| Docker | Integration testing | HTTP | `docker-compose.yml` |
| Production | Live deployment | HTTPS | Azure/K8s config |

### Docker Compose Override Files

```bash
# Standard development
docker-compose up -d

# Debug JWT authentication
docker-compose -f docker-compose.yml -f docker-compose.debug.yml up -d tenant-service

# Infrastructure only (for Aspire development)
docker-compose -f docker-compose.infrastructure.yml up -d
```

### Debug Override Example

```yaml
# docker-compose.debug.yml
services:
  tenant-service:
    environment:
      Logging__LogLevel__Microsoft.AspNetCore.Authentication: Debug
      Logging__LogLevel__Microsoft.AspNetCore.Authorization: Debug
```

## Port Assignments

### Service Ports

| Service | Docker External | Docker Internal | Aspire HTTPS |
|---------|-----------------|-----------------|--------------|
| Blueprint | 5000 | 8080 | 7000 |
| Wallet | internal only | 8080 | 7001 |
| Register | 5290 | 8080 | 7290 |
| Tenant | 5110 | 8080 | 7110 |
| API Gateway | 80/443 | 8080/8443 | 7082 |

### Infrastructure Ports

| Service | Port | Purpose |
|---------|------|---------|
| PostgreSQL | 5432 | Database |
| MongoDB | 27017 | Document store |
| Redis | 6379 (16379 Docker) | Cache |
| Aspire Dashboard | 18888 | Observability |
| OTLP gRPC | 4317 | Telemetry |

## Production Checklist

Copy this checklist for production deployments:

### Security
- [ ] Replace development JWT signing key with secure key from vault
- [ ] Remove `ASPNETCORE_ENVIRONMENT: Development`
- [ ] Enable HTTPS with valid certificates
- [ ] Configure proper CORS origins
- [ ] Set `restart: always` (not `unless-stopped`)

### Configuration
- [ ] Use secrets manager for `JWT_SIGNING_KEY`
- [ ] Configure production database connection strings
- [ ] Set proper resource limits (memory, CPU)
- [ ] Enable container image scanning

### Monitoring
- [ ] Configure Application Insights or production OTEL endpoint
- [ ] Set up alerting on health check failures
- [ ] Configure log aggregation

### Validation
```bash
# Verify health endpoints
curl https://your-domain/health

# Check container logs for errors
docker logs sorcha-tenant-service 2>&1 | grep -i error
```

## Azure Container Apps

### Service Discovery

In Azure Container Apps, services discover each other via internal DNS:

```yaml
environment:
  Services__Blueprint__Url: http://blueprint-api  # Container App name
  Services__Wallet__Url: http://wallet-service
```

### Bicep Deployment

```bicep
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'blueprint-api'
  properties: {
    configuration: {
      ingress: {
        external: false  // Internal only
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'blueprint-api'
          image: '${containerRegistry}.azurecr.io/blueprint-api:latest'
          env: [
            { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
          ]
        }
      ]
    }
  }
}
```

## Volume and Secret Management

### Docker Volumes for Persistence

```yaml
volumes:
  postgres-data:        # Database files
  mongodb-data:         # Document store
  redis-data:           # Cache persistence
  dataprotection-keys:  # Shared encryption keys
  wallet-encryption-keys:  # Wallet-specific keys
```

### WARNING: Data Protection Keys Must Be Shared

**The Problem:**
Without shared Data Protection keys, services can't decrypt each other's tokens.

```yaml
# BAD - Each service has isolated keys
wallet-service:
  volumes:
    - wallet-keys:/home/app/.aspnet/DataProtection-Keys
```

**The Fix:**
```yaml
# GOOD - Shared volume for Data Protection
wallet-service:
  volumes:
    - dataprotection-keys:/home/app/.aspnet/DataProtection-Keys

register-service:
  volumes:
    - dataprotection-keys:/home/app/.aspnet/DataProtection-Keys
```

### Production Secrets

```yaml
# Development (acceptable for local dev)
environment:
  JWT_SIGNING_KEY: ${JWT_SIGNING_KEY:-dev-key}

# Production (use Azure Key Vault or similar)
# Never commit actual secrets to docker-compose.yml
```

## Database Initialization

### PostgreSQL Init Script

```sql
-- docker/postgres-init.sql
SELECT 'CREATE DATABASE sorcha_wallet'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'sorcha_wallet')\gexec

SELECT 'CREATE DATABASE sorcha_tenant'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'sorcha_tenant')\gexec

GRANT ALL PRIVILEGES ON DATABASE sorcha_wallet TO sorcha;
GRANT ALL PRIVILEGES ON DATABASE sorcha_tenant TO sorcha;
```

Mount with read-only access:
```yaml
postgres:
  volumes:
    - ./docker/postgres-init.sql:/docker-entrypoint-initdb.d/01-init.sql:ro
```