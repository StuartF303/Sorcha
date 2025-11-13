# GitHub Actions Workflows

This directory contains the CI/CD workflows for the Sorcha platform.

## Active Workflows

### 1. Build and Test (`build-test.yml`)

**Triggers:**
- Push to any branch (main, master, develop, feature/*)
- Pull requests to main/master
- Manual trigger via workflow_dispatch

**Purpose:**
- Build the entire solution
- Run all unit tests (Cryptography, TransactionHandler, Blueprint, etc.)
- Generate test results and code coverage reports
- Run E2E tests on pull requests

**Jobs:**
1. `build-and-test`: Builds solution and runs all test suites
2. `e2e-tests`: Runs Playwright E2E tests (PR only)

**This workflow does NOT:**
- Build Docker images
- Deploy to any environment
- Publish packages

---

### 2. Azure Deployment (`azure-deploy.yml`)

**Triggers:**
- Automatically after successful "Build and Test" workflow completion on main/master
- Manual trigger with option to skip test verification

**Purpose:**
- Deploy base infrastructure (Resource Group, ACR, Redis, Container Apps Environment)
- Build and push Docker images to Azure Container Registry
- Deploy Container Apps to Azure

**Jobs:**
1. `check-build-success`: Verifies the build-test workflow succeeded
2. `deploy-azure`: Deploys infrastructure and applications using Bicep templates

**Services Deployed:**
- Blueprint API
- API Gateway
- Peer Service
- Blazor Client (UI)

**Configuration:**
```yaml
env:
  AZURE_RESOURCE_GROUP: sorcha
  AZURE_LOCATION: uksouth
  CONTAINER_REGISTRY: sorchaacr
```

---

### 3. NuGet Publishing (`nuget-publish.yml` & `cryptography-nuget.yml`)

**Purpose:**
- Build, test, pack, and publish NuGet packages

---

## Workflow Architecture

```
Push/PR → Build and Test
            ├─ Build Solution
            ├─ Run All Tests
            └─ Upload Artifacts
                    ↓
        [main/master only]
                    ↓
            Build and Test ✓
                    ↓
            Azure Deployment
            ├─ Check Build Success
            ├─ Deploy Infrastructure (Bicep)
            ├─ Build & Push Docker Images
            └─ Deploy Container Apps
```

## Required Secrets

```
AZURE_CREDENTIALS    - Azure service principal JSON
NUGET_API_KEY       - nuget.org API key
```

## Pipeline Changes (2025-11-13)

**Old:** `ci-cd.yml` → Single monolithic pipeline
**New:** Separated into focused workflows

**Benefits:**
- ✅ Faster CI feedback
- ✅ Deployment only after successful tests
- ✅ No unnecessary Docker builds

See `../PIPELINE-MIGRATION.md` for details.
