# Sorcha Platform - Master Task List

**Version:** 3.9 - UPDATED
**Last Updated:** 2026-01-26
**Status:** Active - Validator Service Requirements Refined (Decentralized Consensus)
**Related:** [MASTER-PLAN.md](MASTER-PLAN.md) | [TASK-AUDIT-REPORT.md](TASK-AUDIT-REPORT.md)

---

## Overview

This document consolidates all tasks across the Sorcha platform into a single, prioritized list organized by implementation phase. Tasks are tracked by priority, status, and estimated effort.

**Total Tasks:** 270 (across all phases, including production readiness, blueprint validation, validator service, orchestration, and CLI)
**Completed:** 136 (50%)
**In Progress:** 0 (0%)
**Not Started:** 134 (50%)

---

## Recent Updates

**2026-01-26:**
- 📋 VALIDATOR-SERVICE-REQUIREMENTS.md UPDATED: Decentralized consensus architecture
  - **Leader election** for docket building (rotating/Raft mechanisms)
  - Dual-role validator (leader/initiator + confirmer)
  - Multi-validator consensus with configurable thresholds
  - **Consensus failure handling** (abandon docket, retry transactions)
  - Genesis blueprint integration for register governance
  - Blueprint versioning via transaction chain
  - Multi-blueprint registers (corrected from single-blueprint)
  - **gRPC communication** via Peer Service
- 📋 GENESIS-BLUEPRINT-SPEC.md CREATED: Genesis block and control blueprint specification
  - Register Control Blueprint schema
  - **Leader election configuration** (mechanism, heartbeat, timeout)
  - Validator registration models (public/consent)
  - Control actions for register governance
  - **Control blueprint versioning** for governance updates
  - Docket structure with multi-signature support
- 📋 Sprint 9 EXPANDED: 14 tasks → 50 tasks (560 hours estimated)
  - Split into 7 sub-sprints (9A-9G) for better tracking
  - Added leader election tasks (9C)
  - Added consensus failure handling, gRPC integration
  - Added control blueprint version resolver

**2026-01-21:**
- ✅ UI-CONSOLIDATION 100% COMPLETE: Admin to Main UI migration (35/35 tasks)
  - All Designer components migrated (ParticipantEditor, ConditionEditor, CalculationEditor)
  - Export/Import dialogs with JSON/YAML support
  - Offline sync service and components
  - Consumer pages: MyActions, MyWorkflows, MyTransactions, MyWallet, Templates
  - Settings page with profile management, Help page with documentation
  - Configuration service tests (59 tests)
  - Fixed Docker profile configuration (relative URLs)
  - **Sorcha.Admin removed from solution** (projects and directories deleted)
  - Documentation updated, deprecation notices added before removal

**2026-01-20:**
- ✅ BP-5.8 COMPLETE: Client-side SignalR integration for Main UI
  - ActionsHubConnection service for real-time action notifications
  - MyActions page with live connection status and snackbar alerts
  - API Gateway routes for /actionshub SignalR endpoint
- 🟡 SETUP-001 PARTIAL: Wallet encryption permissions fix implemented
  - Added `EnsureFallbackDirectoryIsWritable()` with clear error messages
  - Updated setup scripts to fix Docker volume permissions (UID 1654)
  - Created `fix-wallet-encryption-permissions.ps1/.sh` quick-fix scripts
  - Added helpful comments in docker-compose.yml
- ✅ System Schema Store feature complete (T001-T080)

**2026-01-01:**
- ✅ TENANT-SERVICE-001 COMPLETE: Bootstrap API endpoint implemented
- ✅ BOOTSTRAP SCRIPTS CREATED: PowerShell and Bash automation scripts

**2025-12-13:**
- ✅ WS-008/009 COMPLETE: Wallet Service EF Core repository and PostgreSQL migrations

**2025-12-12:**
- ✅ AUTH-003 COMPLETE: PostgreSQL + Redis deployment
- ✅ AUTH-004 COMPLETE: Bootstrap seed scripts
- ✅ AUTH-002 COMPLETE: Service authentication integration

**2025-12-09:**
- ✅ SEC-004 COMPLETE: Security headers added to all services
- ✅ REG-CODE-DUP COMPLETE: Resolved DocketManager/ChainValidator duplication
- ✅ Sprint 10 COMPLETE: All 16 orchestration tasks finished
- ✅ Sprint 8 COMPLETE: All 11 validation tasks finished

---

## Task Status Summary

### By Phase

| Phase | Total Tasks | Complete | In Progress | Not Started | % Complete | Details |
|-------|-------------|----------|-------------|-------------|------------|---------|
| **Phase 1: Blueprint-Action** | 118 | 64 | 0 | 54 | **54%** | [View Tasks](tasks/phase1-blueprint-service.md) |
| **Phase 2: Wallet Service** | 34 | 34 | 0 | 0 | **100%** ✅ | [View Tasks](tasks/phase2-wallet-service.md) |
| **Phase 3: Register Service** | 15 | 14 | 0 | 1 | **93%** ✅ | [View Tasks](tasks/phase3-register-service.md) |
| **Phase 4: Enhancements** | 25 | 0 | 0 | 25 | 0% | [View Tasks](tasks/phase4-enhancements.md) |
| **Production Readiness** | 10 | 0 | 0 | 10 | 0% ⚠️ | [View Tasks](tasks/production-readiness.md) |
| **CLI Admin Tool** | 60 | 0 | 0 | 60 | 0% | [View Tasks](tasks/cli-admin-tool.md) |
| **Deferred** | 10 | 0 | 0 | 10 | 0% | [View Tasks](tasks/deferred-tasks.md) |
| **TOTAL** | **270** | **112** | **0** | **158** | **41%** | |

### By Priority

| Priority | Total | Complete | In Progress | Not Started |
|----------|-------|----------|-------------|-------------|
| **P0 - Critical (MVD Blocker)** | 40 | 3 | 0 | 37 ⚠️ |
| **P1 - High (Production Ready)** | 32 | 0 | 0 | 32 ⚠️ |
| **P2 - Medium (Enhancements)** | 65 | 58 | 0 | 7 |
| **P3 - Low (Post-MVD)** | 66 | 42 | 0 | 24 |

**Note:** Sprint 9 (Validator Service) expanded significantly for decentralized consensus architecture.

---

## Priority Definitions

**P0 - Critical (MVD Blocker):**
Tasks that must be completed for the MVD to function. Without these, the end-to-end workflow will not work.

**P1 - High (MVD Core):**
Important tasks that significantly enhance the MVD but have workarounds if delayed.

**P2 - Medium (MVD Nice-to-Have):**
Tasks that improve quality, performance, or developer experience but aren't essential for MVD launch.

**P3 - Low (Post-MVD):**
Enhancement tasks that can be deferred until after MVD is complete.

---

## Detailed Task Lists by Phase

| Phase | Description | Link |
|-------|-------------|------|
| [Phase 1: Blueprint-Action Service](tasks/phase1-blueprint-service.md) | Core execution engine, Sprints 1-11 | 118 tasks |
| [Phase 2: Wallet Service](tasks/phase2-wallet-service.md) | REST API, EF Core, integration | 34 tasks |
| [Phase 3: Register Service](tasks/phase3-register-service.md) | Transaction storage, OData | 15 tasks |
| [Phase 4: Post-MVD Enhancements](tasks/phase4-enhancements.md) | Quality, performance, advanced features | 25 tasks |
| [Production Readiness](tasks/production-readiness.md) | Security, auth, operations | 10 tasks |
| [CLI Admin Tool](tasks/cli-admin-tool.md) | Cross-platform CLI, 5 sprints | 60 tasks |
| [Deferred Tasks](tasks/deferred-tasks.md) | Post-launch features | 10 tasks |

**Key Specifications:**
| Document | Description |
|----------|-------------|
| [VALIDATOR-SERVICE-REQUIREMENTS.md](VALIDATOR-SERVICE-REQUIREMENTS.md) | Decentralized consensus validator |
| [GENESIS-BLUEPRINT-SPEC.md](GENESIS-BLUEPRINT-SPEC.md) | Genesis block and control blueprint |

---

## Critical Path (MVD Blocking)

```
BP-3.x (Service Layer) → BP-4.x (Action APIs) → BP-5.5 (SignalR) ✅
    ↓
WS-025 → WS-026.x (Wallet API) ✅
    ↓
WS-INT-x (Integration) ✅
    ↓
REG-001 → REG-005/006/007 (Register API) ✅
    ↓
REG-INT-x (Full Integration) ✅
    ↓
BP-7.1 (E2E Tests) ✅
    ↓
VAL-9.x (Validator Service - Decentralized Consensus) ⚠️ CURRENT BLOCKER
    ├── VAL-9A: Core Infrastructure
    ├── VAL-9B: Validation Engine
    ├── VAL-9C: Initiator Role (Docket Building)
    ├── VAL-9D: Confirmer Role
    ├── VAL-9E: Service Integration (Peer, Register, Blueprint)
    ├── VAL-9F: Validator Registration & Genesis
    └── VAL-9G: Configuration & Testing
    ↓
BP-11.x (Production Readiness)
```

---

## Task Management

### Weekly Review Process

1. **Monday:** Review completed tasks from previous week
2. **Wednesday:** Check in-progress tasks, identify blockers
3. **Friday:** Plan next week's tasks, update priorities

### Success Metrics

**Sprint Completion:**
- ✅ Sprint 1: 100% (7/7 tasks)
- ✅ Sprint 2: 100% (8/8 tasks)
- ✅ Sprint 3-7: 100% Complete
- ✅ Sprint 8: 100% (11/11 tasks)
- ✅ Sprint 10: 100% (16/16 tasks)

**Code Quality:**
- Test coverage >85% for all new code
- Zero critical bugs
- All CI/CD checks passing

---

**Related Documents:**
- [MASTER-PLAN.md](MASTER-PLAN.md) - Overall implementation plan
- [Project Constitution](constitution.md) - Standards and principles
- [Project Specification](spec.md) - Requirements and architecture

---

**Last Updated:** 2025-12-12
**Next Review:** Weekly
**Document Owner:** Sorcha Architecture Team
