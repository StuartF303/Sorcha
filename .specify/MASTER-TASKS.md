# Sorcha Platform - Master Task List

**Version:** 4.5 - UPDATED
**Last Updated:** 2026-02-06
**Status:** Active - Peer Service Test Coverage
**Related:** [MASTER-PLAN.md](MASTER-PLAN.md) | [TASK-AUDIT-REPORT.md](TASK-AUDIT-REPORT.md)

---

## Overview

This document consolidates all tasks across the Sorcha platform into a single, prioritized list organized by implementation phase. Tasks are tracked by priority, status, and estimated effort.

**Total Tasks:** 270 (across all phases, including production readiness, blueprint validation, validator service, orchestration, and CLI)
**Completed:** 145 (54%)
**In Progress:** 0 (0%)
**Not Started:** 125 (46%)

---

## Recent Updates

**2026-02-06:**
- ✅ PEER-TESTS COMPLETE: Peer Service unit test coverage (232 new tests, 11 test files)
  - `CircuitBreakerTests` (25 tests): State transitions, thresholds, fallbacks, HalfOpen recovery
  - `GossipProtocolEngineTests` (33 tests): ShouldGossip, RecordSeen, PrepareForNextRound, BloomFilter, cleanup
  - `PushNotificationHandlerTests` (29 tests): Subscriber management, stream notifications, failure cleanup
  - `HubNodeValidatorTests` (16 tests): Hostname pattern validation (n0-n2.sorcha.dev)
  - `HeartbeatValidatorTests` (10 tests): Timeout detection, failover thresholds, constants
  - `RetryBackoffValidatorTests` (14 tests): Exponential backoff, cap at 60s, attempt validation
  - `SyncValidatorTests` (8 tests): Sync timing, interval constants
  - `SystemRegisterValidatorTests` (6 tests): System register ID validation
  - `PeerNodeTests` (19 tests): Equality, hashing, HashSet behavior, defaults
  - `HubNodeInfoTests` (15 tests): Connection state, failure tracking, gRPC address
  - `TransactionNotificationTests` (11 tests): Default values, property round-trip
  - Peer Service test count: 139 → 371 (167% increase)

**2026-01-31:**
- ✅ VAL-9.49/VAL-9.50 COMPLETE: Performance testing for Validator Service
  - NBomber-based validation throughput testing (validator_throughput, validator_batch, validator_stress)
  - Consensus latency testing (validator_metrics, validator_consensus_metrics, validator_registry)
  - Memory pool statistics endpoint performance testing
  - Test suite selection parameter (--test-suite validator)
  - Integrated into Sorcha.Performance.Tests
  - Sprint 9G now 100% complete (7/7 tasks)
  - **Sprint 9 (Validator Service) 100% COMPLETE** (50/50 tasks)
- ✅ VAL-9.39 COMPLETE: Validator Approval flow (consent mode)
  - IValidatorRegistry methods: GetPendingValidatorsAsync, ApproveValidatorAsync, RejectValidatorAsync
  - ValidatorApprovalRequest/ValidatorApprovalResult types
  - REST endpoints: GET /{registerId}/pending, POST /{validatorId}/approve, POST /{validatorId}/reject
  - ValidatorListChangeType: ValidatorApproved, ValidatorRejected events
  - Registration flow updated to set Pending status in consent mode
  - 22 unit tests for approval flow
  - Sprint 9F now 100% complete (7/7 tasks)
- ✅ SETUP-001 COMPLETE: First-run setup wizard for fresh installations
  - `scripts/setup.ps1` - PowerShell setup wizard with 8-step process
  - `scripts/setup.sh` - Bash equivalent for Linux/macOS
  - `scripts/setup-config.yaml` - Configuration template with defaults
  - `scripts/validate-environment.ps1` - Comprehensive environment validation
  - `docs/FIRST-RUN-SETUP.md` - Complete setup documentation
  - Environment detection, configuration generation, volume creation
  - Infrastructure startup, service validation, health checks
  - Interactive and non-interactive modes supported

**2026-01-30:**
- ✅ VAL-9.41 COMPLETE: Control Docket Processor for governance transactions
  - `IControlDocketProcessor` interface and `ControlDocketProcessor` implementation
  - Extracts control transactions from dockets (7 action types supported)
  - Validates control transactions against control blueprint rules
  - Applies control actions: validator.register, validator.approve, validator.suspend, validator.remove
  - Applies control actions: config.update, blueprint.publish, register.updateMetadata
  - Refreshes GenesisConfigService and ValidatorRegistry on state changes
  - 36 unit tests with comprehensive coverage
- ✅ VAL-9.44 COMPLETE: Validator Service configuration system (memory limits, performance)
  - ValidationEngineConfiguration with batch size, parallel validation, timeout settings
  - MemPoolConfiguration with max size, TTL, priority quotas
  - ConsensusConfiguration with approval threshold, vote timeout, retry settings
  - DocketBuildConfiguration with time/size thresholds, max transactions per docket
- ✅ VAL-9.45 COMPLETE: Validator Service metrics API endpoints
  - `/api/metrics` - Aggregated metrics from all subsystems
  - `/api/metrics/validation` - Validation engine stats
  - `/api/metrics/consensus` - Consensus, distribution, failure stats
  - `/api/metrics/pools` - Verified queue metrics
  - `/api/metrics/caches` - Blueprint cache stats
  - `/api/metrics/config` - Current configuration (redacted)
- ✅ SEC-002 COMPLETE: API rate limiting and throttling for all services
  - Added RateLimiter extension methods to ServiceDefaults
  - 5 policy types: API (100/min), Authentication (10/min), Strict (5/min), Heavy (10 concurrent), Relaxed (1000/min)
  - Applied to all 7 services: API Gateway, Blueprint, Register, Validator, Wallet, Tenant, Peer
  - IP-based partitioning with X-Forwarded-For proxy support
  - Rate limit headers (Retry-After, X-RateLimit-Policy) on 429 responses
- ✅ SEC-003 COMPLETE: Input validation hardening (OWASP compliance)
  - Created InputValidationMiddleware with attack pattern detection
  - SQL injection protection with comprehensive regex patterns
  - XSS attack detection (script tags, event handlers, javascript: URLs)
  - Path traversal prevention (../, encoded variants)
  - Command injection detection (shell metacharacters, dangerous commands)
  - LDAP injection protection
  - Configurable via InputValidationOptions (max body size, query length, header length)
  - Applied to all 7 services: API Gateway, Blueprint, Register, Validator, Wallet, Tenant, Peer
  - Health/alive/scalar/openapi paths excluded from validation

**2026-01-29:**
- ✅ SEC-001 COMPLETE: HTTPS enforcement with HSTS for all services
  - Added UseHttpsEnforcement() extension method to ServiceDefaults
  - HSTS header (max-age=1yr, includeSubDomains, preload) in production
  - Applied to all 7 services: API Gateway, Blueprint, Register, Validator, Wallet, Tenant, Peer
- ✅ VAL-9.46/47/48 COMPLETE: Validator Service integration tests (133 tests passing)
  - ValidationEngineIntegrationTests: Transaction validation, batch processing, payload hash verification
  - ConsensusEngineIntegrationTests: Multi-validator consensus, docket publishing, failure handling
  - GenesisConfigServiceIntegrationTests: Genesis transaction, control record validation
  - DocketBuilderIntegrationTests: Docket construction, merkle tree, transaction ordering
  - MemPoolIntegrationTests: Transaction pool, expiration, priority, concurrency
  - ServiceClientIntegrationTests: Register, Blueprint, Peer, Wallet client integration
  - BlueprintCacheIntegrationTests: Caching, version resolution, invalidation
  - LeaderElectionIntegrationTests: Leader status, rotating election, failure handling
- ✅ AUTH-001 UPDATED: Tenant Service PostgreSQL repositories confirmed complete (95%)
  - All 3 repositories fully implemented: Organization, Identity, Participant
  - 67 integration tests passing
  - EF Core DbContext with multi-schema support

**2026-01-28:**
- ✅ UI-REGISTER-MANAGEMENT 100% COMPLETE: Register list, details, creation, query (70/70 tasks)
  - Enhanced CreateRegisterWizard with 4-step flow including wallet selection
  - Added RegisterSearchBar component for client-side filtering
  - Created TransactionQueryForm and Query page for cross-register wallet search
  - Added clipboard.js interop with snackbar confirmation
  - Full data-testid attributes for E2E testing
- ✅ CLI-REGISTER-COMMANDS 100% COMPLETE: Two-phase creation, dockets, queries
  - `sorcha register create/list/dockets/query` commands
  - System.CommandLine 2.0.2 with proper option naming conventions
  - Refit HTTP clients for API integration

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
| **Phase 1: Blueprint-Action** | 118 | 68 | 0 | 50 | **58%** | [View Tasks](tasks/phase1-blueprint-service.md) |
| **Phase 2: Wallet Service** | 34 | 34 | 0 | 0 | **100%** ✅ | [View Tasks](tasks/phase2-wallet-service.md) |
| **Phase 3: Register Service** | 15 | 14 | 0 | 1 | **93%** ✅ | [View Tasks](tasks/phase3-register-service.md) |
| **Phase 4: Enhancements** | 25 | 0 | 0 | 25 | 0% | [View Tasks](tasks/phase4-enhancements.md) |
| **Production Readiness** | 10 | 6 | 0 | 4 | **60%** | [View Tasks](tasks/production-readiness.md) |
| **CLI Admin Tool** | 60 | 0 | 0 | 60 | 0% | [View Tasks](tasks/cli-admin-tool.md) |
| **Deferred** | 10 | 0 | 0 | 10 | 0% | [View Tasks](tasks/deferred-tasks.md) |
| **TOTAL** | **270** | **120** | **0** | **150** | **44%** | |

### By Priority

| Priority | Total | Complete | In Progress | Not Started |
|----------|-------|----------|-------------|-------------|
| **P0 - Critical (MVD Blocker)** | 40 | 7 | 0 | 33 ⚠️ |
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
VAL-9.x (Validator Service - Decentralized Consensus) ✅ COMPLETE
    ├── VAL-9A: Core Infrastructure ✅
    ├── VAL-9B: Validation Engine ✅
    ├── VAL-9C: Initiator Role (Docket Building) ✅
    ├── VAL-9D: Confirmer Role ✅
    ├── VAL-9E: Service Integration (Peer, Register, Blueprint) ✅
    ├── VAL-9F: Validator Registration & Genesis ✅
    └── VAL-9G: Configuration & Testing ✅
    ↓
BP-11.x (Production Readiness) ⚠️ CURRENT BLOCKER
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
- ✅ Sprint 9: 100% (50/50 tasks) - Validator Service Complete!
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

**Last Updated:** 2026-02-06
**Next Review:** Weekly
**Document Owner:** Sorcha Architecture Team
