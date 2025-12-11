# Docker Build and Push Pipeline

**File**: `docker-build-push.yml`
**Status**: Active
**Last Updated**: 2025-12-11

## Overview

This workflow automatically builds and pushes Docker images to Docker Hub when code changes are committed. It intelligently detects which services have changed and only builds those services, saving time and resources.

## Features

- ✅ **Smart Change Detection**: Only builds services that have changed
- ✅ **Dependency-Aware**: Rebuilds all services when common dependencies change
- ✅ **Multi-Architecture**: Builds for both AMD64 and ARM64
- ✅ **Layer Caching**: Uses Docker layer caching for faster builds
- ✅ **Semantic Versioning**: Supports version tagging (when tagged)
- ✅ **Summary Reports**: Provides build summary in GitHub Actions UI

## Triggers

The pipeline runs automatically on:
- Push to `main`, `master`, or `develop` branches
- Changes to files in `src/**`
- Changes to `docker-compose.yml`
- Changes to the workflow file itself

Manual trigger: **Actions** → **Build and Push Docker Images** → **Run workflow**

## Services Built

| Service | Image Name | Description |
|---------|-----------|-------------|
| Tenant Service | `sorcha/tenant-service` | Multi-tenancy and authentication |
| Wallet Service | `sorcha/wallet-service` | Cryptographic wallet management |
| Register Service | `sorcha/register-service` | Distributed ledger transactions |
| Blueprint Service | `sorcha/blueprint-service` | Workflow definition and execution |
| Peer Service | `sorcha/peer-service` | P2P networking |
| API Gateway | `sorcha/api-gateway` | YARP-based API routing |
| Blazor Client | `sorcha/blueprint-designer-client` | Blueprint designer UI |

## Change Detection Logic

**Rebuild ALL services** if:
- Common libraries change (`src/Common/**`)
- Core libraries change (`src/Core/**`)
- `docker-compose.yml` changes
- Workflow file changes

**Rebuild individual services** if:
- Service code changes
- Service-specific dependencies change

## Docker Image Tags

| Tag Format | Example | Description |
|------------|---------|-------------|
| `latest` | `sorcha/tenant-service:latest` | Latest from default branch |
| `{branch}-{sha}` | `sorcha/tenant-service:main-abc1234` | Branch + commit SHA |
| `{branch}` | `sorcha/tenant-service:main` | Current branch |
| `{version}` | `sorcha/tenant-service:1.2.3` | Semantic version (tagged) |

## Setup Requirements

### 1. Docker Hub Repository

Create repositories on Docker Hub:
- `sorcha/tenant-service`
- `sorcha/wallet-service`
- `sorcha/register-service`
- `sorcha/blueprint-service`
- `sorcha/peer-service`
- `sorcha/api-gateway`
- `sorcha/blueprint-designer-client`

### 2. Docker Hub Access Token

1. Docker Hub → Account Settings → Security
2. **New Access Token**
3. Name: `GitHub Actions CI/CD`
4. Permissions: **Read, Write, Delete**
5. Copy token

### 3. GitHub Secrets

Add to repository: **Settings** → **Secrets and variables** → **Actions**

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `DOCKERHUB_USERNAME` | `sorcha` | Docker Hub username/org |
| `DOCKERHUB_TOKEN` | `dckr_pat_xxxxx` | Access token |

## Usage

### Automatic Builds

```bash
git add .
git commit -m "feat: Update tenant service"
git push origin main
```

Workflow will:
1. Detect changed services
2. Build Docker images
3. Push to Docker Hub

### Manual Builds

1. **Actions** → **Build and Push Docker Images**
2. **Run workflow** → Select branch → **Run**

### Force Rebuild All

```bash
git commit --allow-empty -m "chore: Rebuild all Docker images"
git push
```

## Using Docker Images

### Pull from Docker Hub

```bash
# Latest version
docker pull sorcha/tenant-service:latest

# Specific commit
docker pull sorcha/tenant-service:main-abc1234

# Specific version
docker pull sorcha/tenant-service:1.2.3
```

### Production docker-compose.yml

```yaml
services:
  tenant-service:
    image: sorcha/tenant-service:1.2.3  # Use specific version
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: Production
```

## Troubleshooting

### Build Failures

**Error**: `failed to compute cache key`

**Solution**: Clear cache
```bash
git commit --allow-empty -m "chore: Rebuild"
git push
```

**Error**: `denied: requested access to resource is denied`

**Solution**:
1. Verify secrets are set correctly
2. Verify repositories exist on Docker Hub
3. Verify token has write permissions

### No Services Built

**Cause**: No changes detected

**Solution**: Manually trigger or modify common dependency

## Performance

- **Layer Caching**: Reuses layers between builds
- **Parallel Builds**: Multiple services simultaneously
- **Smart Detection**: Only builds changed services
- **BuildKit**: Faster builds

## Multi-Architecture

Builds for:
- AMD64 (Intel/AMD servers)
- ARM64 (Apple Silicon, AWS Graviton, Raspberry Pi)

To disable ARM64 (faster builds):
```yaml
platforms: linux/amd64
```

## Security

✅ **DO**:
- Use access tokens (not passwords)
- Minimal token permissions
- Rotate tokens periodically
- Use GitHub Secrets

❌ **DON'T**:
- Commit credentials
- Use personal passwords
- Grant excessive permissions
- Share tokens

---

**Maintainer**: Sorcha DevOps Team
