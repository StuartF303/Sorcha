# Blueprint Storage Basic Walkthrough

**Purpose:** Basic blueprint CRUD operations — create, list, get, delete blueprints via API.

**Category:** Foundation
**Status:** Active
**Prerequisites:** Docker Desktop, PowerShell 7+

---

## Quick Start

```bash
# 1. Start Docker services
docker-compose up -d

# 2. Generate secrets (first time only)
pwsh walkthroughs/initialize-secrets.ps1

# 3. Run setup (bootstrap org + admin)
pwsh walkthroughs/BlueprintStorageBasic/setup.ps1

# 4. Run the walkthrough
pwsh walkthroughs/BlueprintStorageBasic/run.ps1
```

## What It Tests

1. Admin authentication via bootstrap + login fallback
2. Blueprint creation with participants and actions
3. Blueprint retrieval by ID
4. Blueprint listing (count verification)
5. Blueprint deletion

## Files

| File | Purpose |
|------|---------|
| `config.json` | Walkthrough metadata |
| `setup.ps1` | Bootstrap org, save state |
| `run.ps1` | Execute CRUD tests |
| `state.json` | Runtime state (gitignored) |

## Options

```powershell
# Use direct service ports (not API Gateway)
pwsh walkthroughs/BlueprintStorageBasic/setup.ps1 -Profile direct

# Show request/response JSON
pwsh walkthroughs/BlueprintStorageBasic/run.ps1 -ShowJson
```
