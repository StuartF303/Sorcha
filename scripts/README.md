# Sorcha Docker Scripts

This directory contains utility scripts for building and publishing Sorcha Docker images.

---

## 📦 Push to DockerHub

Scripts to build and push all Sorcha service images to DockerHub.

### Windows (PowerShell)

**Script:** `push-to-dockerhub.ps1`

#### Basic Usage

```powershell
# Push all services with 'latest' tag
.\scripts\push-to-dockerhub.ps1 -DockerHubUser "yourusername"

# Push with specific version tag
.\scripts\push-to-dockerhub.ps1 -DockerHubUser "yourusername" -Tag "v1.0.0"

# Push only specific services
.\scripts\push-to-dockerhub.ps1 -DockerHubUser "yourusername" -Services blueprint,wallet

# Dry run (see what would happen without pushing)
.\scripts\push-to-dockerhub.ps1 -DockerHubUser "yourusername" -DryRun

# Skip building and use existing local images
.\scripts\push-to-dockerhub.ps1 -DockerHubUser "yourusername" -SkipBuild
```

#### Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-DockerHubUser` | ✅ Yes | - | Your DockerHub username or organization |
| `-Tag` | ❌ No | `latest` | Version tag for images (e.g., `v1.0.0`, `1.2.3`) |
| `-Services` | ❌ No | All | Specific services to push: `blueprint`, `wallet`, `register`, `tenant`, `peer`, `validator`, `gateway` |
| `-SkipBuild` | ❌ No | `false` | Skip building, only tag and push existing images |
| `-DryRun` | ❌ No | `false` | Show what would be done without actually pushing |

---

### Linux/macOS (Bash)

**Script:** `push-to-dockerhub.sh`

#### Basic Usage

```bash
# Make script executable (first time only)
chmod +x scripts/push-to-dockerhub.sh

# Push all services
./scripts/push-to-dockerhub.sh -u yourusername

# Push with version tag
./scripts/push-to-dockerhub.sh -u yourusername -t v1.0.0

# Push specific services
./scripts/push-to-dockerhub.sh -u yourusername -s blueprint,wallet

# Dry run
./scripts/push-to-dockerhub.sh -u yourusername --dry-run
```

---

## 🔐 DockerHub Authentication

Before running the scripts:

```bash
docker login
```

The scripts will check authentication and prompt if needed.

---

## 🎯 Available Services

- `blueprint` - Blueprint workflow management
- `wallet` - Cryptographic wallet service
- `register` - Distributed ledger service
- `tenant` - Multi-tenant management
- `peer` - P2P networking
- `validator` - Transaction validation
- `gateway` - YARP API gateway

---

## 📝 Examples

```powershell
# Production release (Windows)
.\scripts\push-to-dockerhub.ps1 -DockerHubUser "sorchaorg" -Tag "v1.0.0"

# Production release (Linux/Mac)
./scripts/push-to-dockerhub.sh -u sorchaorg -t v1.0.0

# Update specific services only
.\scripts\push-to-dockerhub.ps1 -DockerHubUser "myuser" -Services validator,gateway
```

---

**Last Updated:** 2025-12-23
