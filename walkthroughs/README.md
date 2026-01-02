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
