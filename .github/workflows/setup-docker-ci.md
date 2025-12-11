# Docker CI/CD Setup Guide

This guide will help you set up the Docker build and push pipeline for GitHub Actions.

## Prerequisites

- ✅ Docker Hub account
- ✅ GitHub repository with admin access
- ✅ GitHub CLI (`gh`) installed (optional but recommended)

## Step 1: Create Docker Hub Access Token

### Via Web UI

1. Go to https://hub.docker.com
2. Log in to your account
3. Click your profile → **Account Settings**
4. Select **Security** tab
5. Click **New Access Token**
6. Configure token:
   - **Description**: `GitHub Actions CI/CD`
   - **Access permissions**: **Read, Write, Delete**
7. Click **Generate**
8. **Copy the token immediately** (you won't see it again!)

### Via CLI (Docker Desktop)

```bash
# Login to Docker Hub
docker login

# Create token via Docker Hub web UI (CLI doesn't support token creation)
```

## Step 2: Create Docker Hub Repositories

You need to create repositories for each service. You have two options:

### Option A: Organization Namespace (Recommended)

Create an organization on Docker Hub (e.g., `sorcha`) and create repositories:

```
sorcha/tenant-service
sorcha/wallet-service
sorcha/register-service
sorcha/blueprint-service
sorcha/peer-service
sorcha/api-gateway
sorcha/blueprint-designer-client
```

### Option B: Personal Namespace

Use your Docker Hub username and create repositories:

```
yourusername/tenant-service
yourusername/wallet-service
# ... etc
```

### Creating Repositories

For each repository:

1. Go to Docker Hub → **Repositories**
2. Click **Create Repository**
3. Repository name: `tenant-service` (or other service name)
4. Visibility: **Public** or **Private**
5. Click **Create**

Repeat for all 7 services.

## Step 3: Configure GitHub Secrets

### Option A: Using GitHub CLI (Recommended)

```bash
# Navigate to your repository
cd /path/to/Sorcha

# Set Docker Hub username
gh secret set DOCKERHUB_USERNAME --body "sorcha"

# Set Docker Hub token (will prompt for input)
gh secret set DOCKERHUB_TOKEN

# Verify secrets were created
gh secret list
```

### Option B: Using GitHub Web UI

1. Go to your GitHub repository
2. **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**

**Secret 1:**
- Name: `DOCKERHUB_USERNAME`
- Value: `sorcha` (or your Docker Hub username/org)
- Click **Add secret**

**Secret 2:**
- Name: `DOCKERHUB_TOKEN`
- Value: Paste the access token from Step 1
- Click **Add secret**

## Step 4: Update Workflow Image Names (if needed)

If you're using a different Docker Hub namespace, update the workflow:

**File**: `.github/workflows/docker-build-push.yml`

Find this section (around line 58):

```yaml
matrix:
  service:
    - name: tenant-service
      filter: tenant-service
      dockerfile: src/Services/Sorcha.Tenant.Service/Dockerfile
      image: sorcha/tenant-service  # ← Change 'sorcha' to your namespace
```

Update all `image` values to use your namespace.

## Step 5: Test the Workflow

### Option A: Trigger Manually

1. Go to **Actions** tab in GitHub
2. Select **Build and Push Docker Images**
3. Click **Run workflow**
4. Select branch: `main`
5. Click **Run workflow**

### Option B: Push a Commit

```bash
# Make a trivial change
git commit --allow-empty -m "chore: Test Docker CI/CD pipeline"
git push origin main
```

## Step 6: Monitor the Build

1. Go to **Actions** tab
2. Click on the running workflow
3. Monitor progress:
   - **detect-changes**: Shows which services will be built
   - **build-push-images**: Shows individual service builds
   - **build-summary**: Shows final summary

### Expected Output

For a successful run, you should see:

```
✓ detect-changes
✓ build-push-images (tenant-service)
✓ build-push-images (wallet-service)
✓ build-push-images (register-service)
✓ build-push-images (blueprint-service)
✓ build-push-images (peer-service)
✓ build-push-images (api-gateway)
✓ build-push-images (blazor-client)
✓ build-summary
```

## Step 7: Verify Images on Docker Hub

1. Go to Docker Hub → **Repositories**
2. Check each repository has new tags:
   - `latest`
   - `main` (or your branch name)
   - `main-{sha}` (branch + commit SHA)

Example:
```
sorcha/tenant-service:latest
sorcha/tenant-service:main
sorcha/tenant-service:main-abc1234
```

## Troubleshooting

### Issue: "Repository does not exist"

**Error**:
```
denied: requested access to the resource is denied
```

**Solution**:
- Verify repositories exist on Docker Hub
- Check repository names match workflow configuration
- Ensure visibility is set correctly (public/private)

### Issue: "Authentication failed"

**Error**:
```
Error: Cannot perform an interactive login from a non TTY device
```

**Solution**:
- Verify `DOCKERHUB_TOKEN` secret is set correctly
- Regenerate access token on Docker Hub
- Ensure token has **Read, Write, Delete** permissions

### Issue: "Build takes too long"

**First build** is slow (10-15 minutes per service) because:
- Building .NET 10 SDK base image
- Restoring NuGet packages
- No layer cache exists yet

**Subsequent builds** are faster (2-5 minutes) because:
- Layer caching is used
- Only changed layers are rebuilt
- Base images are cached

### Issue: "All services build on every commit"

**Cause**: Common dependencies changed

**Expected behavior**:
- Changes to `src/Common/**` → Rebuild all services
- Changes to specific service → Rebuild only that service

**Solution**:
- This is by design
- Common library changes affect all services
- If you only changed one service, only that service rebuilds

### Issue: Multi-architecture build fails

**Error**:
```
ERROR: Multi-platform build is not supported for the docker driver
```

**Solution**: Docker Buildx is not set up correctly. The workflow includes:

```yaml
- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v3
```

If this fails, check GitHub Actions runner has Docker Buildx support.

**Workaround**: Disable multi-arch builds (edit workflow):

```yaml
platforms: linux/amd64  # Remove linux/arm64
```

## Advanced Configuration

### Customize Branch Triggers

Edit `.github/workflows/docker-build-push.yml`:

```yaml
on:
  push:
    branches:
      - main
      - develop
      - release/**  # Add release branches
```

### Add Version Tags

When you create a version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow will build images with version tags:
```
sorcha/tenant-service:1.0.0
sorcha/tenant-service:1.0
```

### Disable Multi-Architecture

For faster builds (AMD64 only):

```yaml
platforms: linux/amd64
```

### Change Base Image

To use Alpine instead of Ubuntu Chiseled:

**Edit**: `src/Services/Sorcha.Tenant.Service/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
```

## Next Steps

Once the pipeline is working:

1. ✅ Update `docker-compose.yml` to use published images
2. ✅ Set up deployment to staging environment
3. ✅ Configure image vulnerability scanning
4. ✅ Add deployment notifications (Slack, Discord, etc.)

## Security Best Practices

✅ **DO**:
- Rotate access tokens every 90 days
- Use minimal token permissions (Read, Write)
- Enable 2FA on Docker Hub account
- Use organization namespaces for team projects
- Tag images with commit SHA for traceability

❌ **DON'T**:
- Commit Docker Hub credentials to repository
- Use personal password for automation
- Share access tokens publicly
- Use `latest` tag in production deployments

## Support

For issues:
- Check [DOCKER_BUILD_PUSH.md](DOCKER_BUILD_PUSH.md) for detailed documentation
- Review GitHub Actions logs
- Check Docker Hub repository settings
- Verify secrets are configured correctly

---

**Last Updated**: 2025-12-11
**Maintainer**: Sorcha DevOps Team
