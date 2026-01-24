# CI/CD Reference

## Contents
- GitHub Actions Workflow Structure
- Docker Build Pipeline
- Change Detection
- Container Registry Integration
- Deployment Patterns

## GitHub Actions Workflow Structure

### Main CI/CD Pipeline (`main-ci-cd.yml`)

Sorcha uses a multi-stage pipeline with selective publishing:

```yaml
name: Main CI/CD - Full Build & Selective Publishing

on:
  push:
    branches: [ main, master ]
  workflow_dispatch:
    inputs:
      force_publish_all:
        description: 'Force publish all artifacts'
        type: boolean
        default: true

jobs:
  detect-changes:
    # Determine which components changed
    
  full-build-and-test:
    # Build entire solution, run all tests
    needs: detect-changes
    
  publish-cryptography:
    # Publish NuGet if changed
    needs: [detect-changes, full-build-and-test]
    if: needs.detect-changes.outputs.cryptography == 'true'
    
  publish-containers:
    # Build and push Docker images
    needs: [detect-changes, full-build-and-test]
    
  deploy-to-azure:
    # Deploy to production
    needs: [publish-containers]
```

## Docker Build Pipeline

### Selective Service Building (`docker-build-push.yml`)

```yaml
jobs:
  detect-changes:
    outputs:
      tenant-service: ${{ steps.filter.outputs.tenant-service }}
      rebuild-all: ${{ steps.filter.outputs.rebuild-all }}
    steps:
      - uses: dorny/paths-filter@v3
        with:
          filters: |
            rebuild-all:
              - 'src/Common/**'  # Shared code triggers full rebuild
              - 'docker-compose.yml'
            tenant-service:
              - 'src/Services/Sorcha.Tenant.Service/**'

  build-push-images:
    strategy:
      matrix:
        service:
          - name: tenant-service
            dockerfile: src/Services/Sorcha.Tenant.Service/Dockerfile
            image: sorchadev/tenant-service
    steps:
      - name: Build and push
        if: needs.detect-changes.outputs.rebuild-all == 'true' || 
            needs.detect-changes.outputs[matrix.service.filter] == 'true'
        uses: docker/build-push-action@v6
        with:
          context: .
          file: ${{ matrix.service.dockerfile }}
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          cache-from: type=registry,ref=${{ matrix.service.image }}:buildcache
          cache-to: type=registry,ref=${{ matrix.service.image }}:buildcache,mode=max
```

## Change Detection

### Path-Based Filtering

```yaml
- uses: dorny/paths-filter@v3
  with:
    filters: |
      # Common changes trigger full rebuild
      rebuild-all:
        - 'src/Common/**'
        - 'src/Core/**'
        
      # Service-specific changes
      wallet-service:
        - 'src/Services/Sorcha.Wallet.Service/**'
        - 'src/Common/Sorcha.Cryptography/**'  # Direct dependency
```

### Manual Change Detection Fallback

```bash
# When dorny/paths-filter isn't available
if git cat-file -e ${{ github.event.before }} 2>/dev/null; then
  CHANGED_FILES=$(git diff --name-only ${{ github.event.before }} ${{ github.sha }})
else
  CHANGED_FILES=$(git diff --name-only HEAD~1 HEAD)
fi

echo "services-changed=$(echo "$CHANGED_FILES" | grep -q "src/Services" && echo "true" || echo "false")"
```

## Container Registry Integration

### Azure Container Registry

```yaml
env:
  CONTAINER_REGISTRY: sorchaacr

steps:
  - uses: azure/login@v2
    with:
      creds: ${{ secrets.AZURE_CREDENTIALS }}
      
  - run: az acr login --name ${{ env.CONTAINER_REGISTRY }}
  
  - name: Build and push
    run: |
      docker build -t ${{ env.CONTAINER_REGISTRY }}.azurecr.io/blueprint-api:${{ github.sha }} \
        -t ${{ env.CONTAINER_REGISTRY }}.azurecr.io/blueprint-api:latest \
        -f src/Services/Sorcha.Blueprint.Service/Dockerfile .
      docker push ${{ env.CONTAINER_REGISTRY }}.azurecr.io/blueprint-api --all-tags
```

### Docker Hub

```yaml
steps:
  - uses: docker/login-action@v3
    with:
      username: ${{ secrets.DOCKERHUB_USERNAME }}
      password: ${{ secrets.DOCKERHUB_TOKEN }}
      
  - uses: docker/metadata-action@v5
    id: meta
    with:
      images: sorchadev/tenant-service
      tags: |
        type=ref,event=branch
        type=sha,prefix={{branch}}-
        type=raw,value=latest,enable={{is_default_branch}}
```

## Deployment Patterns

### Azure Bicep Deployment

```yaml
- uses: azure/arm-deploy@v2
  with:
    scope: subscription
    region: ${{ env.AZURE_LOCATION }}
    template: ./infra/main.bicep
    parameters: >
      resourceGroupName=${{ env.AZURE_RESOURCE_GROUP }}
      containerRegistryName=${{ env.CONTAINER_REGISTRY }}
      environment=prod
```

### WARNING: Missing fetch-depth Breaks Change Detection

**The Problem:**
```yaml
# BAD - Shallow clone breaks git diff
- uses: actions/checkout@v4
```

**The Fix:**
```yaml
# GOOD - Full history for accurate change detection
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

## CI/CD Checklist

Copy this checklist when adding new services:

- [ ] Add service to `detect-changes` path filters
- [ ] Add service to build matrix in `docker-build-push.yml`
- [ ] Configure Dockerfile with multi-stage build
- [ ] Add to Azure Container Registry push step
- [ ] Update deployment Bicep template
- [ ] Add health check endpoint (`/health`)
- [ ] Test build locally: `docker-compose build <service>`