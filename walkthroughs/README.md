# Sorcha Walkthroughs

This directory contains step-by-step walkthroughs for common Sorcha scenarios. Each walkthrough includes scripts, documentation, and results to help you understand and work with the platform.

---

## Purpose

Walkthroughs serve as:
- **Learning Resources:** Understand how Sorcha works through practical examples
- **Testing Artifacts:** Scripts to verify functionality and regression test
- **Documentation:** Real-world examples with actual results
- **Onboarding:** Help new developers get started quickly

---

## Structure

Each walkthrough is organized in its own subdirectory:

```
walkthroughs/
├── README.md (this file)
├── BlueprintStorageBasic/
│   ├── README.md                      # Walkthrough overview
│   ├── *.ps1                          # Test/demo scripts
│   ├── *.sh                           # Shell scripts (if needed)
│   └── RESULTS.md or *.md             # Results, troubleshooting, findings
└── [NextWalkthrough]/
    └── ...
```

---

## Available Walkthroughs

### [BlueprintStorageBasic](./BlueprintStorageBasic/)
**Status:** ✅ Complete
**Date:** 2026-01-02
**Purpose:** Bring up Sorcha in Docker and demonstrate basic blueprint design (create, modify, save)

**What you'll learn:**
- Starting Sorcha services with Docker Compose
- Bootstrapping the platform (org + admin user)
- JWT authentication
- Uploading blueprints via REST API
- Working with sample blueprints

**Key files:**
- `upload-blueprint-test.ps1` - Main script for blueprint upload
- `test-jwt.ps1` - Authentication testing
- `DOCKER-BOOTSTRAP-RESULTS.md` - Complete results and troubleshooting

### [AdminIntegration](./AdminIntegration/)
**Status:** ✅ Complete
**Date:** 2026-01-02
**Purpose:** Integrate Sorcha.Admin (Blazor WASM) into Docker deployment behind API Gateway

**What you'll learn:**
- Multi-stage Docker builds for Blazor WebAssembly
- nginx configuration for SPA subpath hosting
- YARP reverse proxy routing with API Gateway
- Blazor base path configuration
- Docker Compose service integration
- Admin UI authentication flow

**Key files:**
- `README.md` - Comprehensive integration guide
- `INTEGRATION-RESULTS.md` - Detailed test results and architecture
- `test-admin-integration.ps1` - Automated testing script
- `.walkthrough-info.md` - Quick reference metadata

**Access Points:**
- Admin UI: http://localhost/admin
- Credentials: stuart.mackintosh@sorcha.dev / SorchaDev2025!

### [McpServerBasics](./McpServerBasics/)
**Status:** ✅ Complete
**Date:** 2026-01-29
**Purpose:** Authenticate with Sorcha and use the MCP Server to interact with the platform via AI assistant tools

**What you'll learn:**
- Starting Sorcha services with Docker Compose
- JWT authentication with Tenant Service
- Running the MCP Server with JWT token
- Role-based tool access (admin, designer, participant)
- Using MCP tools for platform health, blueprints, wallets, and ledger
- Integrating with Claude Desktop

**Key files:**
- `get-token-and-run-mcp.ps1` - PowerShell script (Windows)
- `get-token-and-run-mcp.sh` - Bash script (Linux/Mac)
- `test-mcp-server.ps1` - Quick verification tests
- `RESULTS.md` - Complete test results and findings

**Access:**
- MCP Server: `docker-compose run mcp-server --jwt-token <token>`
- Credentials: admin@sorcha.local / Admin123!
- Available Tools: 36 tools across 3 role categories

### [UserWalletCreation](./UserWalletCreation/)
**Status:** 🚧 Planning Phase
**Date:** 2026-01-04
**Purpose:** Create users in an organization and set up their default wallets, with multi-user blueprint sharing scenarios

**What you'll learn:**
- User management via Tenant Service API
- Wallet creation with HD wallet (BIP32/BIP39/BIP44) support
- JWT authentication flow for users
- Multi-tenancy and organization isolation
- Cryptographic key algorithms (ED25519, NIST P-256, RSA-4096)
- Mnemonic phrase management and security
- Multi-user blueprint workflows (Phase 2)

**Key files:**
- `README.md` - Walkthrough overview and quick start
- `PLAN.md` - Detailed implementation plan
- `scripts/phase1-create-user-wallet.ps1` - Main user + wallet creation script
- `scripts/test-user-login.ps1` - Test authentication
- `data/test-users.json` - Sample user configurations

**Phases:**
- **Phase 1:** Single user with default wallet ✅ Planned
- **Phase 2:** Multi-user blueprint sharing 🚧 Future

### [DistributedRegister](./DistributedRegister/)
**Status:** ✅ Complete
**Date:** 2026-02-09
**Purpose:** Cross-machine register creation, peer discovery, subscription, and transaction replication between two Sorcha nodes

**What you'll learn:**
- Two-machine peer seeding and network setup (SSL certs, SSH keys, seed nodes)
- Cross-machine service authentication via temporary service principals
- Register advertisement and discovery on the peer network
- Full-replica subscription and replication monitoring
- Ping-pong blueprint execution with distributed transaction verification

**Key files:**
- `test-distributed-register.ps1` - Main 14-step walkthrough script
- `README.md` - Complete setup guide (SSH, certs, peer seeding, YARP routes)

**Prerequisites:** Two networked machines running Docker, PowerShell 7+

### [OrganizationPingPong](./OrganizationPingPong/)
**Status:** ✅ Complete
**Date:** 2026-02-09
**Purpose:** Full-stack pipeline: organization bootstrap, participants, real wallets, signed register, blueprint publish, and 20 round-trip ping-pong executions

**What you'll learn:**
- Organization bootstrapping (Tenant Service)
- Multi-participant user creation
- ED25519 wallet creation (Wallet Service via API Gateway)
- Two-phase register creation with attestation signing
- Blueprint template loading and wallet address patching
- Blueprint publishing with cycle warning handling
- Workflow instance creation and 40-action execution pipeline

**Key files:**
- `README.md` - Walkthrough overview, parameters, troubleshooting
- `test-org-ping-pong.ps1` - Main 10-phase script

**Profiles:** `gateway` (default), `direct`, `aspire`

### [ConstructionPermit](./ConstructionPermit/)
**Status:** 🚧 In Progress
**Date:** 2026-02-17
**Purpose:** Multi-org, multi-user construction permit approval with conditional routing, calculations, and verifiable credential issuance

**What you'll learn:**
- Multi-organization workflows (4 orgs, 5 participants)
- Two participants from the same organization (Council: Planning Officer + Building Control)
- Conditional routing based on calculated risk score
- JSON Logic calculations (risk score from building parameters, permit fee from project value)
- Rejection paths routing back to the originator
- Verifiable credential issuance (Building Permit VC) on approval
- Blueprint templates with parameterised participant wallets
- Selective disclosure (each participant sees only relevant data)

**Key files:**
- `README.md` - Full scenario specification with action details
- `construction-permit-template.json` - Blueprint template (TODO)
- `test-construction-permit.ps1` - Main walkthrough script (TODO)
- `data/` - Input data for 3 test scenarios (TODO)

**Test Scenarios:**
- **Scenario A:** Low-risk residential (3 storeys, riskScore 6.1) — skips environmental, 5 actions to permit
- **Scenario B:** High-risk commercial (8 storeys, riskScore 22.8) — full 6-action path with environmental review
- **Scenario C:** Rejection — planning officer rejects for zoning non-compliance

### [PerformanceBenchmark](./PerformanceBenchmark/)
**Status:** ✅ Complete
**Date:** 2026-02-13
**Purpose:** Comprehensive performance testing of Register Service with payload, throughput, latency, and concurrency benchmarks

**What you'll learn:**
- Measuring transaction performance across payload sizes (1KB-1MB)
- Throughput testing (transactions per second)
- Latency benchmarking under various load conditions
- Concurrency testing with parallel transaction streams
- Docket building performance measurement
- Identifying performance bottlenecks and optimization opportunities

**Key files:**
- `test-performance.ps1` - Main benchmark suite with 5 test scenarios
- `monitor-resources.ps1` - Docker container resource monitoring
- `PERFORMANCE-REPORT.md` - Results template and analysis framework
- `results/` - Generated benchmark data (JSON/CSV)

**Test Scenarios:**
- Payload sizes: 1KB, 10KB, 50KB, 100KB, 500KB, 1MB
- Throughput: Sustained TPS measurement (60s duration)
- Latency: P50/P95/P99 under normal and stressed conditions
- Concurrency: 1, 5, 10, 25, 50 parallel workers
- Docket building: 10, 50, 100, 500 transaction batches

**Expected Metrics:**
- Throughput: 100+ TPS target
- Latency P95: <200ms (5KB payloads)
- Concurrency: 25+ workers without degradation

---

## Creating a New Walkthrough

### 1. Create Directory Structure
```bash
mkdir -p walkthroughs/YourWalkthroughName
```

### 2. Required Files

Each walkthrough should include:

**README.md** - Overview with:
- Purpose and goals
- Prerequisites
- Quick start instructions
- Key results
- Access points/credentials (if applicable)
- Next steps
- Troubleshooting

**Scripts** - Executable test/demo scripts:
- PowerShell (`.ps1`) for Windows
- Bash (`.sh`) for Linux/Mac
- Clear naming: `test-*.ps1`, `demo-*.ps1`, `setup-*.ps1`

**Results** - Documentation of outcomes:
- `*-RESULTS.md` with findings, limitations, next steps
- Screenshots (if helpful)
- Sample output

### 3. Naming Conventions

**Directory names:**
- PascalCase (e.g., `BlueprintStorageBasic`, `WalletIntegration`)
- Descriptive of the scenario
- No spaces or special characters

**Script names:**
- Lowercase with hyphens (e.g., `test-jwt.ps1`, `upload-blueprint-test.ps1`)
- Prefix with purpose: `test-`, `demo-`, `setup-`, `verify-`

**Documentation:**
- README.md (overview)
- *-RESULTS.md (findings/outcomes)
- Additional docs as needed

### 4. Script Best Practices

**PowerShell scripts:**
```powershell
# Include error handling
$ErrorActionPreference = "Stop"

# Clear output with colors
Write-Host "Step 1: Doing something..." -ForegroundColor Yellow
Write-Host "  ✓ Success!" -ForegroundColor Green

# Document prerequisites in comments
# Requires: Docker Desktop, .NET 10 SDK

# Make scripts runnable from repo root
# Use relative paths: samples/blueprints/...
```

**Bash scripts:**
```bash
#!/bin/bash
set -e  # Exit on error

# Clear step indicators
echo "==> Step 1: Doing something..."
echo "✓ Success!"

# Use relative paths from repo root
```

### 5. Documentation Template

```markdown
# [Walkthrough Name]

**Purpose:** [One sentence describing the goal]
**Date Created:** [YYYY-MM-DD]
**Status:** ✅ Complete | 🚧 In Progress | ⚠️ Deprecated
**Prerequisites:** [List required tools/setup]

---

## Overview
[2-3 sentences explaining what this demonstrates]

## Files in This Walkthrough
- **file1.ps1** - Description
- **file2.md** - Description

## Quick Start
[Step-by-step instructions]

## Key Results
[What was accomplished]

## Known Limitations
[Any caveats or issues]

## Next Steps
[What to do after this walkthrough]

## Troubleshooting
[Common issues and solutions]
```

---

## Guidelines for AI Assistants

When creating walkthroughs as an AI assistant:

1. **Always create a dedicated subdirectory** - Never put scripts/results in repo root
2. **Follow the structure above** - README.md + scripts + results
3. **Use clear naming** - PascalCase for dirs, lowercase-hyphen for scripts
4. **Include error handling** - Scripts should fail gracefully with helpful messages
5. **Document prerequisites** - List required tools, versions, services
6. **Provide working examples** - Scripts should be copy-paste ready
7. **Include actual results** - Show real output, errors encountered, solutions
8. **Update this README** - Add your walkthrough to the "Available Walkthroughs" section
9. **Link to related docs** - Connect to existing documentation
10. **Think about reusability** - Scripts may be used for regression testing

---

## Standards & Conventions

### File Organization
- All walkthrough files in subdirectories (not repo root)
- Relative paths from repository root
- Self-contained (can be run independently)

### Documentation
- Markdown format for all documentation
- Clear headings and sections
- Code blocks with language tags
- Tables for structured data

### Scripts
- Include purpose/description at top
- Error handling (exit on error)
- Clear output with progress indicators
- Credentials in variables (not hardcoded in multiple places)

### Results
- Document both successes and failures
- Include troubleshooting for common issues
- Note any workarounds or limitations
- Provide next steps

---

## Maintenance

### When to Update
- Breaking changes to APIs or services
- New features that affect the walkthrough
- Discovered issues or better approaches
- Dependency version changes

### Deprecation
If a walkthrough becomes outdated:
1. Update status to ⚠️ Deprecated
2. Add note explaining why
3. Link to replacement walkthrough (if available)
4. Don't delete (keep for historical reference)

---

## Related Documentation

- [CLAUDE.md](../CLAUDE.md) - AI assistant guide
- [README.md](../README.md) - Project overview
- [docs/](../docs/) - Technical documentation
- [.specify/](../.specify/) - Specifications and planning

---

**Questions?** Check the main [README.md](../README.md) or [CLAUDE.md](../CLAUDE.md) for more guidance.
