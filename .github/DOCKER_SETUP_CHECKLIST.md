# Docker CI/CD Setup Checklist

Use this checklist to track your setup progress.

## Prerequisites
- [ ] Docker Hub account exists
- [ ] `sorchadev` organization created on Docker Hub
- [ ] Admin access to GitHub repository

## Step 1: Docker Hub Repositories
Create these 7 repositories under `sorchadev`:

- [ ] `sorchadev/tenant-service`
- [ ] `sorchadev/wallet-service`
- [ ] `sorchadev/register-service`
- [ ] `sorchadev/blueprint-service`
- [ ] `sorchadev/peer-service`
- [ ] `sorchadev/api-gateway`
- [ ] `sorchadev/blueprint-designer-client`

**Verification**: Visit https://hub.docker.com/u/sorchadev and confirm all 7 repos exist

## Step 2: Docker Hub Access Token
- [ ] Created access token with "Read, Write, Delete" permissions
- [ ] Token description: `GitHub Actions CI/CD`
- [ ] Token saved securely (starts with `dckr_pat_`)

**Verification**: Access token copied to clipboard or secure location

## Step 3: GitHub Secrets
- [ ] `DOCKERHUB_USERNAME` = `sorchadev`
- [ ] `DOCKERHUB_TOKEN` = your access token

**Verification**:
```powershell
gh secret list
# Should show both secrets
```

Or visit: GitHub repo → Settings → Secrets and variables → Actions

## Step 4: Workflow File
- [ ] Updated `.github/workflows/docker-build-push.yml` with `sorchadev` namespace
- [ ] Committed workflow file
- [ ] Pushed to `master` branch

**Verification**:
```powershell
git log --oneline -1
# Should show your commit message
```

## Step 5: First Build
- [ ] GitHub Actions workflow triggered automatically
- [ ] Workflow ran without errors
- [ ] All 7 services built successfully

**Verification**: Visit GitHub repo → Actions tab → Check latest workflow run shows green checkmarks

## Step 6: Docker Hub Images
Check each repository has tags:
- [ ] `tenant-service` has `latest`, `master`, `master-{sha}` tags
- [ ] `wallet-service` has tags
- [ ] `register-service` has tags
- [ ] `blueprint-service` has tags
- [ ] `peer-service` has tags
- [ ] `api-gateway` has tags
- [ ] `blueprint-designer-client` has tags

**Verification**: Visit https://hub.docker.com/r/sorchadev/tenant-service/tags

## Step 7: Test Pull
- [ ] Successfully pulled at least one image

**Verification**:
```powershell
docker pull sorchadev/tenant-service:latest
# Should download successfully
```

---

## Final Verification

Run this script to verify everything:

```powershell
# Check GitHub secrets (requires gh CLI)
Write-Host "Checking GitHub secrets..." -ForegroundColor Cyan
gh secret list | Select-String "DOCKERHUB"

# Check if workflow file exists
Write-Host "`nChecking workflow file..." -ForegroundColor Cyan
Test-Path .github\workflows\docker-build-push.yml

# Check Docker Hub connection
Write-Host "`nTesting Docker Hub connection..." -ForegroundColor Cyan
docker pull sorchadev/tenant-service:latest

Write-Host "`nSetup verification complete!" -ForegroundColor Green
```

---

## If Something Failed

### Repositories not found
- Verify repositories exist at https://hub.docker.com/u/sorchadev
- Check repository names match exactly (lowercase, hyphenated)

### Authentication failed
- Regenerate Docker Hub access token
- Update `DOCKERHUB_TOKEN` secret in GitHub
- Verify token has "Read, Write, Delete" permissions

### Build failed
- Check GitHub Actions logs for specific error
- Common issues:
  - Dockerfile path incorrect
  - Dependencies not restored
  - Out of disk space (GitHub Actions runner)

### Images not appearing
- Check workflow completed successfully (green checkmark)
- Verify push step succeeded in workflow logs
- Check Docker Hub repository visibility (public vs private)

---

**Setup Date**: _______________
**Completed By**: _______________
**Notes**: _______________________________________________
