# Docker Reference

## Contents
- Service Configuration Patterns
- Multi-Stage Build Pattern
- Health Check Configuration
- Volume Management
- Network Configuration
- Anti-Patterns

## Service Configuration Patterns

### YAML Anchors for Shared Configuration

Sorcha uses YAML anchors to share OpenTelemetry and JWT settings:

```yaml
# Define anchors at top of docker-compose.yml
x-otel-env: &otel-env
  OTEL_EXPORTER_OTLP_ENDPOINT: http://aspire-dashboard:18889
  OTEL_SERVICE_NAME: ${OTEL_SERVICE_NAME:-sorcha-service}

x-jwt-env: &jwt-env
  JwtSettings__InstallationName: ${INSTALLATION_NAME:-localhost}
  JwtSettings__SigningKey: ${JWT_SIGNING_KEY:-base64-encoded-key}

# Apply to services
services:
  blueprint-service:
    environment:
      <<: [*otel-env, *jwt-env]  # Merge both anchor configs
      OTEL_SERVICE_NAME: blueprint-service  # Override specific value
```

### Internal vs External Services

```yaml
# GOOD - Internal service (no ports published)
wallet-service:
  # No ports: section - accessed only via Docker network
  environment:
    ASPNETCORE_URLS: http://+:8080

# GOOD - External service (ports published for direct access)
register-service:
  ports:
    - "5290:8080"  # Map host 5290 to container 8080
```

## Multi-Stage Build Pattern

### Standard .NET Service Dockerfile

```dockerfile
# Build stage - full SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Sorcha.Blueprint.Service/Sorcha.Blueprint.Service.csproj", "src/Services/Sorcha.Blueprint.Service/"]
COPY ["src/Common/Sorcha.ServiceDefaults/Sorcha.ServiceDefaults.csproj", "src/Common/Sorcha.ServiceDefaults/"]
RUN dotnet restore "src/Services/Sorcha.Blueprint.Service/Sorcha.Blueprint.Service.csproj"
COPY src/ ./src/
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Permissions stage - prepare non-root directories
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS permissions
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && \
    chown -R 1654:1654 /home/app/.aspnet

# Final stage - Chiseled (distroless)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=permissions --chown=$APP_UID:$APP_UID /home/app/.aspnet /home/app/.aspnet
COPY --from=publish /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Sorcha.Blueprint.Service.dll"]
```

### WARNING: Chiseled Images Have No Shell

**The Problem:**
Chiseled images exclude shell tools for security. Health checks using shell commands fail.

**Why This Breaks:**
```yaml
# BAD - Shell-based health check fails on chiseled images
healthcheck:
  test: ["CMD-SHELL", "curl -f http://localhost:8080/health"]
```

**The Fix:**
Use HTTP-based health probes or orchestrator-level monitoring:
```yaml
# GOOD - Rely on depends_on with infrastructure health checks
depends_on:
  redis:
    condition: service_healthy  # Redis has shell, can health check
```

## Health Check Configuration

### Infrastructure Services (Have Shell)

```yaml
postgres:
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U sorcha"]
    interval: 5s
    timeout: 3s
    retries: 60
    start_period: 30s  # Allow DB to initialize

redis:
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 10s
    timeout: 3s
    retries: 5

mongodb:
  healthcheck:
    test: ["CMD", "mongosh", "--eval", "db.adminCommand('ping')"]
    interval: 10s
    timeout: 3s
    retries: 5
```

### Application Services (Chiseled - No Shell)

```yaml
blueprint-service:
  # No healthcheck section - handled externally via /health endpoint
  depends_on:
    redis:
      condition: service_healthy
    aspire-dashboard:
      condition: service_started
```

## Volume Management

### Named Volumes for Persistence

```yaml
volumes:
  redis-data:           # Redis persistence
  postgres-data:        # PostgreSQL data
  mongodb-data:         # MongoDB data
  dataprotection-keys:  # .NET Data Protection keys (shared)
  wallet-encryption-keys:  # Wallet service encryption keys
```

### Volume Mounts

```yaml
services:
  postgres:
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./docker/postgres-init.sql:/docker-entrypoint-initdb.d/01-init.sql:ro

  wallet-service:
    volumes:
      - dataprotection-keys:/home/app/.aspnet/DataProtection-Keys
      - wallet-encryption-keys:/var/lib/sorcha/wallet-keys
```

### WARNING: Init Scripts Must Be Read-Only

```yaml
# GOOD - Read-only init script
- ./docker/postgres-init.sql:/docker-entrypoint-initdb.d/01-init.sql:ro

# BAD - Writable init script (security risk)
- ./docker/postgres-init.sql:/docker-entrypoint-initdb.d/01-init.sql
```

## Network Configuration

### Bridge Network Pattern

```yaml
networks:
  sorcha-network:
    driver: bridge

services:
  api-gateway:
    environment:
      Services__Blueprint__Url: http://blueprint-service:8080  # Docker DNS
      Services__Wallet__Url: http://wallet-service:8080
    networks:
      - sorcha-network
```

### WARNING: Never Use localhost in Container-to-Container Communication

**The Problem:**
```yaml
# BAD - localhost refers to the container itself
Services__Blueprint__Url: http://localhost:5000
```

**Why This Breaks:**
Each container has its own network namespace. `localhost` inside `api-gateway` container refers to `api-gateway`, not `blueprint-service`.

**The Fix:**
```yaml
# GOOD - Use Docker service name (DNS)
Services__Blueprint__Url: http://blueprint-service:8080
```

## Anti-Patterns

### WARNING: Hardcoded Secrets in docker-compose.yml

**The Problem:**
```yaml
# BAD - Secrets visible in version control
environment:
  POSTGRES_PASSWORD: my-super-secret-password
  JWT_SIGNING_KEY: actual-signing-key-here
```

**The Fix:**
```yaml
# GOOD - Use environment variables or .env file
environment:
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-dev_only_password}
  JWT_SIGNING_KEY: ${JWT_SIGNING_KEY}
```

### WARNING: Missing restart Policy

**The Problem:**
```yaml
# BAD - Container stays down after crash
my-service:
  image: my-image
```

**The Fix:**
```yaml
# GOOD - Auto-restart on failure
my-service:
  image: my-image
  restart: unless-stopped
```

## Useful Commands

```bash
# Rebuild and restart single service
docker-compose build blueprint-service && docker-compose up -d --force-recreate blueprint-service

# View real-time logs
docker-compose logs -f wallet-service

# Check container resource usage
docker stats

# Clean up unused resources
docker system prune -a --volumes

# Execute command in running container (non-chiseled only)
docker exec -it sorcha-postgres psql -U sorcha
```