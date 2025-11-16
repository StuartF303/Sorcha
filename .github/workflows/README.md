# GitHub Actions Workflows - Optimized CI/CD Pipeline

This directory contains the GitHub Actions workflows for the Sorcha project. The workflows are designed to be efficient, intelligent, and minimize unnecessary builds and tests.

## Overview

The CI/CD pipeline follows a two-stage approach:

1. **PR Validation** - Dependency-aware testing on pull requests
2. **Main CI/CD** - Full build, test, selective publishing, and deployment on merge

## Workflows

### Primary Workflows

#### 1. PR Validation (`pr-validation.yml`)

**Trigger:** Pull requests to `main` or `master` branches

**Purpose:** Run minimal tests to validate changes before merge

**How it works:**
- Detects which components changed in the PR
- Runs tests only for changed components and their dependents
- Uses architectural hierarchy to determine what needs testing

**Component Detection:**
- **Common layer changes** (Cryptography, TransactionHandler, Blueprint.Models) → Tests component + all dependents
- **Core layer changes** (Blueprint.Fluent, Blueprint.Schemas, Blueprint.Engine) → Tests component + services/apps that depend on it
- **Services layer changes** → Tests only the changed services
- **Apps layer changes** → Tests only the changed applications

**Example scenarios:**

| Changed Component | Tests Run |
|------------------|-----------|
| Sorcha.Cryptography | Cryptography tests + Peer Service tests + Integration tests |
| Sorcha.Blueprint.Models | Models tests + Blueprint.Fluent tests + Blueprint Service tests |
| Sorcha.Blueprint.Fluent | Fluent tests + UI E2E tests |
| Sorcha.ApiGateway | Gateway integration tests |
| Sorcha.Blueprint.Designer.Client | Integration tests + UI tests |

#### 2. Main CI/CD (`main-ci-cd.yml`)

**Trigger:** Push to `main` or `master` branches (after PR merge)

**Purpose:** Full validation, selective publishing, and automated deployment

**Workflow stages:**

1. **Detect Changes**
   - Identifies which components changed since the last successful run
   - Determines which artifacts need to be published

2. **Full Build and Test**
   - Builds the entire solution
   - Runs all unit tests and integration tests
   - Uploads test results and coverage reports

3. **Selective Publishing** (runs only for changed components)
   - **NuGet Packages:**
     - `Sorcha.Cryptography` → Published to NuGet.org
     - `Sorcha.TransactionHandler` → Published to NuGet.org

   - **Container Images:**
     - `blueprint-api` → Pushed to Azure Container Registry
     - `api-gateway` → Pushed to Azure Container Registry
     - `peer-service` → Pushed to Azure Container Registry
     - `blazor-client` → Pushed to Azure Container Registry

4. **Azure Deployment** (only after successful publishing)
   - Deploys base infrastructure (if needed)
   - Updates Container Apps with new images
   - Reports deployment URLs

**Manual Override:**
- You can force publish all artifacts using the `workflow_dispatch` trigger with `force_publish_all: true`

#### 3. CodeQL Security Scanning (`codeql.yml`)

**Trigger:** Push to main/develop, PRs to main, scheduled weekly

**Purpose:** Automated security vulnerability scanning

**How it works:**
- Scans C# and JavaScript code
- Runs security-extended and security-and-quality queries
- Reports findings to GitHub Security tab
- Independent of other workflows

#### 4. Release (`release.yml`)

**Trigger:** Git tags matching `v*.*.*`

**Purpose:** Create versioned releases

**How it works:**
- Triggered by version tags (e.g., `v1.0.0`)
- Builds solution with version from tag
- Creates NuGet packages with version
- Creates GitHub release with artifacts
- Optionally publishes to NuGet.org
- Builds and pushes versioned Docker images to GHCR

**Example:**
```bash
git tag v1.0.0
git push origin v1.0.0
# Triggers release workflow
```

## Architectural Hierarchy

The workflows understand the project's architectural layers:

```
┌─────────────────────────────────────────┐
│  Apps (Highest Level)                   │
│  - Sorcha.AppHost                       │
│  - Sorcha.Blueprint.Designer.Client     │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│  Services                               │
│  - Sorcha.Blueprint.Service             │
│  - Sorcha.ApiGateway                    │
│  - Sorcha.Peer.Service                  │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│  Core                                   │
│  - Sorcha.Blueprint.Fluent              │
│  - Sorcha.Blueprint.Schemas             │
│  - Sorcha.Blueprint.Engine              │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│  Common (Lowest Level)                  │
│  - Sorcha.Cryptography                  │
│  - Sorcha.TransactionHandler            │
│  - Sorcha.Blueprint.Models              │
│  - Sorcha.ServiceDefaults               │
└─────────────────────────────────────────┘
```

**Dependency Rules:**
- Changes to **Common** components require testing all layers above
- Changes to **Core** components require testing Services and Apps
- Changes to **Services** require testing only those services
- Changes to **Apps** require testing only those apps

## Required GitHub Secrets

Configure these secrets in your repository settings:

| Secret | Purpose | Required For |
|--------|---------|--------------|
| `NUGET_API_KEY` | NuGet.org API key for publishing packages | NuGet publishing |
| `AZURE_CREDENTIALS` | Azure service principal credentials | Azure deployment |

### Setting up Azure Credentials

```bash
az ad sp create-for-rbac --name "sorcha-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth
```

Add the JSON output to the `AZURE_CREDENTIALS` secret.

## Environment Configuration

The workflows use these environment variables:

```yaml
DOTNET_VERSION_10: '10.0.x'
AZURE_RESOURCE_GROUP: sorcha
AZURE_LOCATION: uksouth
CONTAINER_REGISTRY: sorchaacr
```

Update these in the workflow files if your configuration differs.

## GitHub Environments

The workflows use GitHub Environments for deployment protection:

- **`nuget-production`** - Protects NuGet package publishing
- **`azure-production`** - Protects Azure deployments

Configure environment protection rules in repository settings → Environments.

## Workflow Outputs

### PR Validation
- Test results for changed components
- Summary of what was tested and why
- Test artifacts uploaded for 30 days

### Main CI/CD
- Full test results and coverage reports
- Published NuGet package artifacts (90 days retention)
- Deployed container image tags
- Azure deployment URLs
- Deployment summary with all published artifacts

## Monitoring

### Workflow Success Criteria

**PR Validation:**
- All changed components must build successfully
- Tests for changed components and critical dependents must pass
- Some dependent tests are allowed to fail (marked with `|| true`)

**Main CI/CD:**
- Full build must succeed
- All critical tests must pass
- Publishing only happens if tests pass
- Deployment only happens if all publishing succeeds

### Viewing Results

1. **GitHub Actions Tab** - View workflow runs and status
2. **PR Comments** - Coverage reports added automatically
3. **Workflow Summary** - Detailed breakdown of what ran
4. **Artifacts** - Download test results and packages

## Troubleshooting

### PR validation running too many tests
- Check the change detection logic in the workflow
- Ensure file paths are correctly mapped to components
- Component dependencies might need adjustment

### Publishing is skipped
- Check if changes were detected for that component
- Verify the `detect-changes` job output
- Use manual dispatch with `force_publish_all: true` to override

### Azure deployment fails
- Verify `AZURE_CREDENTIALS` secret is valid
- Check that base infrastructure exists
- Ensure ACR login succeeds
- Verify Dockerfile paths are correct

### Tests failing in CI but passing locally
- Check .NET version differences
- Verify all dependencies are restored
- Check for environment-specific configuration
- Review test output artifacts

## Best Practices

### For Developers

1. **Small, focused PRs** - Easier to validate and faster CI
2. **Update tests with code** - Ensures validation catches issues
3. **Check workflow output** - Review what tests ran and why
4. **Use draft PRs** - For work-in-progress to avoid unnecessary runs

### For Maintainers

1. **Monitor workflow costs** - GitHub Actions has usage limits
2. **Update dependencies** - Keep workflow actions up to date
3. **Review failed runs** - Investigate and fix flaky tests
4. **Adjust change detection** - As architecture evolves

## Pipeline Changes (2025-11-13)

**Old Architecture:**
- Single monolithic build on every PR and push
- All tests run regardless of changes
- All artifacts published on every merge
- No dependency awareness

**New Architecture:**
- Dependency-aware PR validation
- Selective testing based on changes
- Selective artifact publishing
- Automated deployment only after successful publishing

**Benefits:**
- ✅ Faster PR feedback (only run necessary tests)
- ✅ Reduced CI/CD costs (less compute time)
- ✅ Safer deployments (tested before publish)
- ✅ More efficient (no unnecessary builds)

## Future Improvements

Potential enhancements to consider:

- [ ] Add test result reporting to PR comments
- [ ] Implement caching for faster builds
- [ ] Add performance benchmarking
- [ ] Create staging environment deployment
- [ ] Add rollback capabilities
- [ ] Implement blue-green deployments
- [ ] Add smoke tests after deployment
- [ ] Create release notes automation

## Workflow Requirements

All workflow modifications must follow the rules defined in [WORKFLOW_REQUIREMENTS.md](../WORKFLOW_REQUIREMENTS.md).

**Key requirements:**
- Maintain architectural hierarchy awareness
- Follow change detection patterns
- Preserve dependency testing rules
- Use consistent naming conventions
- Always test workflow changes before merging

**Before modifying workflows:**
1. Read [WORKFLOW_REQUIREMENTS.md](../WORKFLOW_REQUIREMENTS.md)
2. Understand impact on change detection
3. Test changes in a feature branch
4. Update documentation if behavior changes

## Removed Workflows

The following workflows were removed as part of the 2025-01-13 optimization:

- ~~`build-test.yml`~~ - Replaced by pr-validation.yml and main-ci-cd.yml
- ~~`designer-ci.yml`~~ - Functionality merged into pr-validation.yml
- ~~`azure-deploy.yml`~~ - Replaced by main-ci-cd.yml deployment stage
- ~~`cryptography-nuget.yml`~~ - Replaced by main-ci-cd.yml selective publishing
- ~~`transaction-nuget.yml`~~ - Replaced by main-ci-cd.yml selective publishing

These workflows were redundant and caused:
- Duplicate builds
- Wasted CI/CD minutes
- Longer PR validation times
- Unnecessary artifact publishing

## Support

For issues with workflows:
1. Check workflow logs in GitHub Actions
2. Review this documentation and [WORKFLOW_REQUIREMENTS.md](../WORKFLOW_REQUIREMENTS.md)
3. Check component dependencies
4. Verify secrets and environment configuration
5. Open an issue with workflow run details
