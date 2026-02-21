# Walkthrough Migration Guide

This document describes the changes made during the walkthrough overhaul (February 2026).

---

## What Changed

### Before (Old Pattern)
- Each walkthrough was a single monolithic script (150–800+ lines)
- Helper functions copy-pasted across scripts (~150 lines of boilerplate each)
- Hardcoded passwords throughout (`Admin123!`, `PerfTest2026!`, etc.)
- Hardcoded URLs per profile (duplicated in every script)
- No consistent structure — each script was different
- No shared error handling or HTTP call patterns

### After (New Pattern)
- **Shared module** (`modules/SorchaWalkthrough/SorchaWalkthrough.psm1`) eliminates all duplication
- **Externalized secrets** (`.secrets/passwords.json`) — no hardcoded passwords
- **Consistent `setup.ps1` + `run.ps1` pattern** for every walkthrough
- **`state.json`** bridges setup to run (tokens, IDs, URLs)
- **`config.json`** metadata for each walkthrough
- **`run-all.ps1`** master runner for CI/CD

---

## Script Structure Migration

### Old: Single script
```powershell
# Old: everything in one file
param($Profile = 'gateway', $AdminEmail = "admin@foo.local", $AdminPassword = "HardCoded123!")

# 150 lines of helper functions...
function Write-Step { ... }
function Invoke-Api { ... }
function Bootstrap-Org { ... }

# Setup inline
$token = Bootstrap-Org -Email $AdminEmail -Password $AdminPassword ...
$wallet = Create-Wallet ...

# Tests inline
Invoke-Api -Url "$RegisterUrl/..." ...
```

### New: setup.ps1 + run.ps1
```powershell
# setup.ps1 — bootstrap org, create wallet, save state
Import-Module ./walkthroughs/modules/SorchaWalkthrough/SorchaWalkthrough.psm1 -Force
$secrets = Get-SorchaSecrets -WalkthroughName "my-walkthrough"
$env = Initialize-SorchaEnvironment -Profile $Profile -SkipHealthCheck:$SkipHealthCheck
$admin = Connect-SorchaAdmin -TenantUrl $env.TenantUrl -OrgName "My Org" ...
$wallet = New-SorchaWallet -WalletUrl $env.WalletUrl -Name "My Wallet" -Headers $admin.Headers
# Save to state.json
```

```powershell
# run.ps1 — load state, execute walkthrough
Import-Module ./walkthroughs/modules/SorchaWalkthrough/SorchaWalkthrough.psm1 -Force
$state = Get-Content state.json | ConvertFrom-Json
# Execute walkthrough steps using shared functions
```

---

## Key Shared Module Functions

| Old Pattern | New Function | Notes |
|-------------|-------------|-------|
| Inline `Write-Host` with colors | `Write-WtStep`, `Write-WtSuccess`, `Write-WtFail`, `Write-WtInfo`, `Write-WtWarn`, `Write-WtBanner` | Consistent formatting |
| Inline `Invoke-RestMethod` | `Invoke-SorchaApi` | Handles JSON + form-urlencoded, error extraction |
| Inline JWT decode | `Decode-SorchaJwt` | Base64url padding fix |
| Inline hex-to-base64 | `ConvertFrom-HexToBase64` | For attestation signing |
| Inline bootstrap + login | `Connect-SorchaAdmin` | Bootstrap with 409 fallback → login |
| Inline org creation | `Get-OrCreateOrganization` | Idempotent, checks existing first |
| Inline user creation | `Get-OrCreateUser` | Idempotent, checks existing first |
| Inline wallet creation | `New-SorchaWallet` | ED25519, optional public key fetch |
| Inline challenge-sign-verify | `Register-SorchaParticipant` | Self-register + wallet linking |
| Inline participant publish | `Publish-SorchaParticipant` | On-register identity, 409 handling |
| Inline 3-phase register | `New-SorchaRegister` | Initiate → sign attestations → finalize |
| Inline blueprint upload+publish | `Publish-SorchaBlueprint` | Template patch, upload, publish |
| Inline action execution | `Invoke-SorchaAction` | Execute or reject with X-Delegation-Token |
| Hardcoded URL switch/case | `Initialize-SorchaEnvironment` | Profile-based URLs + Docker health check |
| Hardcoded passwords | `Get-SorchaSecrets` | Reads from `.secrets/passwords.json` |

---

## Secrets Migration

### Old: Hardcoded
```powershell
$AdminEmail = "admin@perf.local"
$AdminPassword = "PerfTest2026!"
```

### New: Generated + externalized
```powershell
# First time: generate secrets
pwsh walkthroughs/initialize-secrets.ps1

# In scripts: read secrets
$secrets = Get-SorchaSecrets -WalkthroughName "perf"
# Returns: adminEmail, adminName, adminPassword (+ extra users if defined)
```

Password policy: 8+ chars, upper + lower + digit + special character.

Override via environment variable: `SORCHA_WT_SECRETS_<WALKTHROUGH>` (JSON string).

---

## Deleted Scripts

The following obsolete scripts were removed (functionality merged into setup.ps1/run.ps1):

| Walkthrough | Deleted | Replacement |
|-------------|---------|-------------|
| BlueprintStorageBasic | `simple-blueprint-test.ps1`, `test-jwt.ps1`, `test-blueprint-api.ps1`, `upload-blueprint-test.ps1` | `setup.ps1` + `run.ps1` |
| PingPong | `test-ping-pong-workflow.ps1` | `setup.ps1` + `run.ps1` |
| RegisterCreationFlow | 4 scripts archived to `.archive/` | `setup.ps1` + `run.ps1` |
| WalletVerification | `test-register-with-wallet-signing.ps1`, `test-wallet-functions.ps1` | `setup.ps1` + `run.ps1` |
| RegisterMongoDB | `test-docker-compose.ps1`, `verify-startup.ps1` | `test-mongodb-integration.ps1` |
| OrganizationPingPong | `test-org-ping-pong.ps1` | `setup.ps1` + `run.ps1` |
| ConstructionPermit | `test-construction-permit.ps1` | `setup.ps1` + `run.ps1` |
| MedicalEquipmentRefurb | `test-medical-equipment-refurb.ps1` | `setup.ps1` + `run.ps1` |
| DistributedRegister | `test-distributed-register.ps1` | `setup.ps1` + `run.ps1` |
| PerformanceBenchmark | `bootstrap-perf-org.ps1` | `setup.ps1` |

---

## Running Everything

```powershell
# Generate secrets (once)
pwsh walkthroughs/initialize-secrets.ps1

# Run all walkthroughs
pwsh walkthroughs/run-all.ps1

# Run with options
pwsh walkthroughs/run-all.ps1 -SkipAdvanced      # Skip DistributedRegister + PerformanceBenchmark
pwsh walkthroughs/run-all.ps1 -OnlySetup          # Run only setup.ps1 (no run.ps1)
pwsh walkthroughs/run-all.ps1 -Profile direct      # Use direct service URLs
```

---

## Design-Only Walkthroughs

These walkthroughs contain design documentation only (no working scripts):

| Walkthrough | Purpose |
|-------------|---------|
| `UserWalletCreation/` | Design study for user + wallet creation flows |
| `WalletEncryption/` | Design for production encryption providers |

They have not been migrated to the shared module pattern.
