# Sorcha Platform - Master Task List

**Version:** 3.8 - UPDATED
**Last Updated:** 2025-12-12
**Status:** Active - Sprint 10 Complete, AUTH-002 Complete (Service Authentication Integration)
**Related:** [MASTER-PLAN.md](MASTER-PLAN.md) | [TASK-AUDIT-REPORT.md](TASK-AUDIT-REPORT.md)

---

## Overview

This document consolidates all tasks across the Sorcha platform into a single, prioritized list organized by implementation phase. Tasks are tracked by priority, status, and estimated effort.

**Total Tasks:** 234 (across all phases, including production readiness, blueprint validation, validator service, orchestration, and CLI)
**Completed:** 135 (58%)
**In Progress:** 0 (0%)
**Not Started:** 99 (42%)

**Note:** Counts updated 2026-01-01:
- ✅ **TENANT-SERVICE-001 COMPLETE**: Bootstrap API endpoint implemented (8h)
  - Created `/api/tenants/bootstrap` endpoint in Tenant Service
  - Atomically creates organization, admin user with BCrypt password hashing, and optional service principal
  - Added BootstrapRequest and BootstrapResponse DTOs
  - Comprehensive validation for all inputs (org name, subdomain, email, password)
  - Returns placeholder tokens with instruction to use `/api/auth/login`
  - Integration tests added (8 tests covering success and validation scenarios)
  - See [BootstrapEndpoints.cs](../src/Services/Sorcha.Tenant.Service/Endpoints/BootstrapEndpoints.cs)
- ✅ **BOOTSTRAP SCRIPTS CREATED**: PowerShell and Bash bootstrap automation scripts
  - `scripts/bootstrap-sorcha.ps1` - Windows PowerShell bootstrap script
  - `scripts/bootstrap-sorcha.sh` - Linux/macOS Bash bootstrap script
  - `scripts/README-BOOTSTRAP.md` - Comprehensive bootstrap documentation
  - 8 new enhancement tasks added to Sprint 5: Bootstrap Automation
  - Scripts use placeholder commands pending CLI implementation (CLI-BOOTSTRAP-001 through -006)
  - See [CLI Sprint 5: Bootstrap Automation](#sprint-5-bootstrap-automation-additional---weeks-9-10)

**Note:** Counts updated 2025-12-13:
- ✅ **WS-008/009 COMPLETE**: Wallet Service EF Core repository and PostgreSQL migrations (28h)
  - EfCoreWalletRepository with complete CRUD operations
  - WalletDbContext with 4 entities and comprehensive indexing
  - Migration 20251207234439_InitialWalletSchema applied successfully
  - Smart DI configuration: EF Core if PostgreSQL configured, InMemory fallback
  - Wallet schema verified in PostgreSQL database
  - See Phase 2: Wallet Service - Optional Storage & Encryption section

**Note:** Counts updated 2025-12-12:
- ✅ **AUTH-003 COMPLETE**: PostgreSQL + Redis deployment complete (8h)
  - Docker Compose infrastructure configured for PostgreSQL 17, Redis 8, MongoDB 8
  - Connection strings aligned across all services
  - Database initialization scripts and health checks configured
  - Comprehensive infrastructure setup guide created
  - See [docs/INFRASTRUCTURE-SETUP.md](../docs/INFRASTRUCTURE-SETUP.md)
- ✅ **AUTH-004 COMPLETE**: Bootstrap seed scripts complete (12h)
  - Automatic database seeding on Tenant Service first startup
  - Default organization, admin user, and service principals created automatically
  - Service principal credentials logged for configuration
  - See [scripts/README.md](../scripts/README.md#bootstrap-database-automatic)
- ✅ **AUTH-002 COMPLETE**: Service authentication integration complete (24h)
  - JWT Bearer authentication added to Blueprint, Wallet, and Register services
  - Authorization policies implemented for all protected endpoints
  - Shared configuration template and comprehensive documentation created
  - See [AUTHENTICATION-SETUP.md](../docs/AUTHENTICATION-SETUP.md)

**Note:** Counts updated 2025-12-10:
- ✅ **CLI SPECIFICATION COMPLETE**: Sorcha CLI Admin Tool specification finalized (2,300+ lines)
  - Interactive console mode (REPL) with history and tab completion
  - Flag-based mode for scripts and AI agents
  - Authentication caching with OS-specific encryption
  - Tenant, Register, and Peer service integration
  - Multi-environment profile support
  - See [sorcha-cli-admin-tool.md](.specify/specs/sorcha-cli-admin-tool.md)
- ✅ **AUTH-001 SPECIFICATION COMPLETE**: Tenant Service specification finalized (1,730+ lines)
  - Comprehensive authentication and authorization specification
  - 4 authentication flows: User, Service-to-Service, Delegated Authority, Token Refresh
  - Token lifecycle management with Redis-backed revocation
  - 30+ REST API endpoints fully documented
  - 9 authorization policies (RBAC)
  - Operational requirements: 99.5% SLA, stateless horizontal scaling, degraded operation modes
  - Bootstrap seed scripts documented for development/MVD deployment
  - Implementation 80% complete, PostgreSQL repository and production deployment pending
  - See [sorcha-tenant-service.md](.specify/specs/sorcha-tenant-service.md)

**Note:** Counts updated 2025-12-09:
- ✅ **SEC-004 COMPLETE**: Security headers added to all services (4h)
  - OWASP-recommended headers (X-Frame-Options, CSP, X-Content-Type-Options, etc.)
  - UseApiSecurityHeaders() and UseSecurityHeaders() extension methods in ServiceDefaults
  - Applied to Blueprint, Wallet, Register, Tenant, Peer services and API Gateway
- ✅ **REG-CODE-DUP COMPLETE**: Resolved DocketManager/ChainValidator duplication (4h)
  - Confirmed implementations correctly in Validator.Service (per 2025-11-16 refactoring)
  - Deleted orphaned test files from Register.Core.Tests
- ✅ **Sprint 10 COMPLETE**: All 16 tasks finished (64h total)
  - BP-10.1 to BP-10.16: Blueprint Service orchestration with delegation tokens
  - StateReconstructionService, ActionExecutionService, DelegationTokenMiddleware
  - Instance management with IInstanceStore
  - 123 total tests passing (25 new orchestration tests)
- ✅ **Sprint 8 COMPLETE**: All 11 tasks finished (176h total)
  - BP-8.1 to BP-8.4: P0 structural/workflow validation (55 tests)
  - BP-8.5 to BP-8.11: P1-P3 comprehensive validation (79 new tests added)
  - Total: 134 new validation tests covering all Blueprint categories
- Added Sprint 9: Validator Service (14 new tasks, 182 hours)
- See [VALIDATOR-SERVICE-REQUIREMENTS.md](VALIDATOR-SERVICE-REQUIREMENTS.md) and [BLUEPRINT-VALIDATION-TEST-PLAN.md](BLUEPRINT-VALIDATION-TEST-PLAN.md)

---

## Table of Contents

1. [Task Status Summary](#task-status-summary)
2. [Priority Definitions](#priority-definitions)
3. [Phase 1: Blueprint-Action Service (MVD Core)](#phase-1-blueprint-action-service-mvd-core)
4. [Phase 2: Wallet Service API](#phase-2-wallet-service-api)
5. [Phase 3: Register Service (MVD)](#phase-3-register-service-mvd)
6. [Phase 4: Post-MVD Enhancements](#phase-4-post-mvd-enhancements)
7. [Deferred Tasks](#deferred-tasks)

---

## Task Status Summary

### By Phase

| Phase | Total Tasks | Complete | In Progress | Not Started | % Complete |
|-------|-------------|----------|-------------|-------------|------------|
| **Phase 1: Blueprint-Action** | 82 | 77 | 0 | 5 | **94%** ✅ |
| **Phase 2: Wallet Service** | 34 | 34 | 0 | 0 | **100%** ✅ |
| **Phase 3: Register Service** | 15 | 14 | 0 | 1 | **93%** ✅ |
| **Phase 4: Enhancements** | 25 | 0 | 0 | 25 | 0% |
| **Production Readiness** | 10 | 0 | 0 | 10 | 0% ⚠️ |
| **CLI Admin Tool** (NEW) | 60 | 0 | 0 | 60 | 0% |
| **Deferred** | 10 | 0 | 0 | 10 | 0% |
| **TOTAL** | **234** | **123** | **0** | **111** | **53%** |

**Note:** Phase 1 now includes Sprint 10 (16 orchestration tasks). Sprint 8 validation and Sprint 10 orchestration complete.

### By Priority

| Priority | Total | Complete | In Progress | Not Started |
|----------|-------|----------|-------------|-------------|
| **P0 - Critical (MVD Blocker)** | 6 | 3 | 0 | 3 ⚠️ |
| **P1 - High (Production Ready)** | 21 | 0 | 0 | 21 ⚠️ |
| **P2 - Medium (Enhancements)** | 65 | 58 | 0 | 7 |
| **P3 - Low (Post-MVD)** | 66 | 42 | 0 | 24 |

**⚠️ Critical Note:** Priority classification SIGNIFICANTLY revised after audit. Most completed tasks were incorrectly classified as P0/P1. True P0 tasks are MVD blockers ONLY. See [TASK-AUDIT-REPORT.md](TASK-AUDIT-REPORT.md) Section 3 for details.

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

## Phase 1: Blueprint-Action Service (MVD Core)

**Goal:** Complete the unified Blueprint-Action Service with full execution capabilities
**Duration:** Weeks 1-14 (extended for validation testing)
**Total Tasks:** 66
**Completion:** 82% (54 complete, 0 in progress, 12 not started)

### Sprint 1: Execution Engine Foundation ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-1.1 | Create Sorcha.Blueprint.Engine project | P0 | 2h | ✅ Complete | - |
| BP-1.2 | Define core execution interfaces | P0 | 4h | ✅ Complete | - |
| BP-1.3 | Implement execution models | P0 | 6h | ✅ Complete | - |
| BP-1.4 | Implement SchemaValidator | P0 | 10h | ✅ Complete | - |
| BP-1.5 | SchemaValidator unit tests | P0 | 8h | ✅ Complete | - |
| BP-1.6 | Implement JsonLogicEvaluator | P0 | 10h | ✅ Complete | - |
| BP-1.7 | JsonLogicEvaluator unit tests | P0 | 8h | ✅ Complete | - |

**Sprint 1 Status:** ✅ **COMPLETE** (7/7 tasks, 48 hours)

### Sprint 2: Execution Engine Complete ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-2.1 | Implement DisclosureProcessor | P0 | 8h | ✅ Complete | - |
| BP-2.2 | DisclosureProcessor unit tests | P0 | 6h | ✅ Complete | - |
| BP-2.3 | Implement RoutingEngine | P0 | 8h | ✅ Complete | - |
| BP-2.4 | RoutingEngine unit tests | P0 | 6h | ✅ Complete | - |
| BP-2.5 | Implement ActionProcessor orchestration | P0 | 10h | ✅ Complete | - |
| BP-2.6 | Implement ExecutionEngine facade | P0 | 6h | ✅ Complete | - |
| BP-2.7 | Complete unit test coverage (>90%) | P0 | 8h | ✅ Complete | - |
| BP-2.8 | Integration tests for realistic workflows | P1 | 10h | ✅ Complete | - |

**Sprint 2 Status:** ✅ **COMPLETE** (8/8 tasks, 62 hours)

### Sprint 3: Service Layer Foundation ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-3.1 | Implement ActionResolverService | P0 | 8h | ✅ Complete | - |
| BP-3.2 | Implement PayloadResolverService (stubs) | P0 | 10h | ✅ Complete | - |
| BP-3.3 | Implement TransactionBuilderService | P0 | 8h | ✅ Complete | - |
| BP-3.4 | Add Redis caching layer | P1 | 6h | ✅ Complete | - |
| BP-3.5 | Unit tests for service layer | P0 | 12h | ✅ Complete | - |
| BP-3.6 | Integration tests for services | P1 | 8h | ✅ Complete | - |

**Sprint 3 Status:** ✅ **COMPLETE** (6/6 tasks, 52 hours)
**Completed:** 2025 (exact date from git history)

### Sprint 4: Action API Endpoints ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-4.1 | GET /api/actions/{wallet}/{register}/blueprints | P0 | 4h | ✅ Complete | - |
| BP-4.2 | GET /api/actions/{wallet}/{register} (paginated) | P0 | 6h | ✅ Complete | - |
| BP-4.3 | GET /api/actions/{wallet}/{register}/{tx} | P0 | 4h | ✅ Complete | - |
| BP-4.4 | POST /api/actions (submit action) | P0 | 8h | ✅ Complete | - |
| BP-4.5 | POST /api/actions/reject | P1 | 4h | ✅ Complete | - |
| BP-4.6 | GET /api/files/{wallet}/{register}/{tx}/{fileId} | P1 | 6h | ✅ Complete | - |
| BP-4.7 | API integration tests | P0 | 10h | ✅ Complete | - |
| BP-4.8 | OpenAPI documentation | P1 | 4h | ✅ Complete | - |

**Sprint 4 Status:** ✅ **COMPLETE** (8/8 tasks, 46 hours)
**Completed:** 2025-11-16

### Sprint 5: Execution Helpers & SignalR ✅ SERVER COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-5.1 | POST /api/execution/validate endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.2 | POST /api/execution/calculate endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.3 | POST /api/execution/route endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.4 | POST /api/execution/disclose endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.5 | Implement SignalR ActionsHub | P2 | 8h | ✅ Complete | - |
| BP-5.6 | Redis backplane for SignalR | P2 | 6h | ✅ Complete | - |
| BP-5.7 | SignalR integration tests | P2 | 8h | ✅ Complete | - |
| BP-5.8 | Client-side SignalR integration | P3 | 6h | ❌ Not Started | - |

**Sprint 5 Status:** ✅ **SERVER COMPLETE** (7/8 tasks, 1 client-side task deferred to P3)
**Completed:** 2025-11-17
**Audit Finding:** BP-5.7 found complete with 16 tests in SignalRIntegrationTests.cs; BP-5.8 has no client code

### Sprint 6: Wallet/Register Integration ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-6.1 | Implement Wallet Service client | P0 | 8h | ✅ Complete | - |
| BP-6.2 | Implement Register Service client | P0 | 8h | ✅ Complete | - |
| BP-6.3 | Update PayloadResolverService with real integration | P0 | 6h | ✅ Complete | - |
| BP-6.4 | Update action submission endpoints with Register integration | P0 | 6h | ✅ Complete | - |
| BP-6.5 | Integration tests with Wallet Service | P0 | 10h | ✅ Complete | - |
| BP-6.6 | Integration tests with Register Service | P0 | 10h | ✅ Complete | - |

**Sprint 6 Status:** ✅ **COMPLETE** (6/6 tasks, 48 hours)
**Completed:** 2025-11-17

**Deliverables:**
- ✅ WalletServiceClient - Full HTTP client with encrypt, decrypt, sign, get wallet (256 lines)
- ✅ RegisterServiceClient - Full HTTP client with submit, get transaction(s), query (281 lines)
- ✅ PayloadResolverService - Real integration with Wallet & Register services (195 lines)
- ✅ Action submission endpoints - Submit transactions to Register Service after building
- ✅ Integration tests - 58 test cases across WalletRegisterIntegrationTests, PayloadResolverIntegrationTests
- ✅ End-to-end Blueprint → Wallet → Register flow operational

### Sprint 7: Testing & Documentation ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-7.1 | E2E test suite for complete workflows | P0 | 16h | ✅ Complete | - |
| BP-7.2 | Performance testing (NBomber) | P1 | 8h | ✅ Complete | - |
| BP-7.3 | Load testing (1000 req/s) | P2 | 6h | ✅ Complete | - |
| BP-7.4 | Security testing (OWASP Top 10) | P1 | 8h | ✅ Complete | - |
| BP-7.5 | Complete API documentation | P1 | 6h | ✅ Complete | - |
| BP-7.6 | Integration guide | P2 | 6h | ✅ Complete | - |

**Sprint 7 Status:** ✅ **COMPLETE** (6/6 tasks, 50 hours)
**Completed:** 2025-11-17

**Deliverables:**
- ✅ Comprehensive E2E test suite (BlueprintActionEndToEndTests, WalletIntegrationEndToEndTests, RegisterServiceEndToEndTests)
- ✅ Enhanced performance testing with NBomber (12 scenarios covering all services)
- ✅ Load testing scenarios supporting 1000+ req/s with ramp-up/ramp-down
- ✅ Security testing suite covering OWASP Top 10 vulnerabilities
- ✅ Complete API documentation with examples and error codes
- ✅ Comprehensive integration guide with multiple language examples

### Sprint 8: Blueprint Validation Tests 📋 NEW

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-8.1 | Implement BlueprintStructuralValidationTests | P0 | 16h | ✅ Complete | - |
| BP-8.2 | Implement BlueprintWorkflowValidationTests | P0 | 24h | ✅ Complete | - |
| BP-8.3 | Implement graph cycle detection | P0 | 12h | ✅ Complete | - |
| BP-8.4 | Implement TransactionChainValidationTests | P0 | 20h | ✅ Complete | - |
| BP-8.5 | Implement DisclosureValidationTests | P1 | 16h | ✅ Complete | 2025-11-23 |
| BP-8.6 | Extend SchemaValidatorTests (Blueprint/Action schemas) | P1 | 16h | ✅ Complete | 2025-11-23 |
| BP-8.7 | Implement JsonLogicValidationTests | P1 | 24h | ✅ Complete | 2025-11-23 |
| BP-8.8 | Implement MultiParticipantWorkflowTests | P1 | 16h | ✅ Complete | 2025-11-23 |
| BP-8.9 | Implement FormValidationTests | P2 | 8h | ✅ Complete | 2025-11-23 |
| BP-8.10 | Extend BlueprintTemplateServiceTests | P2 | 16h | ✅ Complete | 2025-11-23 |
| BP-8.11 | Extend JSON-LD validation tests | P3 | 8h | ✅ Complete | 2025-11-23 |

**Sprint 8 Status:** ✅ **COMPLETE** (11/11 tasks complete, 176h completed, 0h remaining)
**Recommended Start:** Week 12 (After Sprint 7 completion)
**Reference:** [BLUEPRINT-VALIDATION-TEST-PLAN.md](BLUEPRINT-VALIDATION-TEST-PLAN.md)

**Critical Tests (P0 - MVD Blockers):**
- Structural validation: Participant references, wallet addresses, action/participant counts
- Workflow validation: Action routing, sequence validation
- **Graph cycle detection**: Prevent infinite Blueprint loops (BS-046)
- **Transaction chain validation**: previousId references, chain continuity, instance isolation

**Core Tests (P1):**
- Disclosure validation: Data visibility rules and recipient validation
- Schema validation: Blueprint/Action embedded schemas, PreviousData
- JSON Logic: Conditions, calculations, participant routing
- Multi-participant workflows: Linear, branching, round-robin patterns

**Enhanced Tests (P2/P3):**
- Form validation: UI control types and schema alignment
- Template validation: Parameter substitution and instantiation
- JSON-LD compliance: Semantic web and Verifiable Credentials

**Completed Deliverables (4/11):**
- ✅ BlueprintStructuralValidationTests: 18 tests (participant/action counts, references, wallet validation)
- ✅ BlueprintWorkflowValidationTests: 16 tests (routing, sequence, cycle detection via DFS)
- ✅ Graph cycle detection: Simple/complex/self-referencing cycle detection
- ✅ TransactionChainValidationTests: 21 tests (previousId, continuity, branching, integrity)

**Total Test Coverage Added:** 55 tests (34 structural/workflow + 21 chain validation)

**Remaining Deliverables (7/11):**
- Disclosure validation tests
- Schema validation extension tests
- JSON Logic validation tests
- Multi-participant workflow tests
- Form/template/JSON-LD validation tests

**Related Tasks:** BS-045, BS-046, BP-3.5, BP-7.1

### Sprint 9: Validator Service 📋 NEW

**Goal:** Rebuild Sorcha.Validator.Service to validate transactions from memory pool against Blueprint rules

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.1 | Design Validator Service architecture | P0 | 8h | 📋 Not Started | - |
| VAL-9.2 | Implement Transaction Pool Poller (Redis) | P0 | 12h | 📋 Not Started | - |
| VAL-9.3 | Implement Validation Engine core | P0 | 24h | 📋 Not Started | - |
| VAL-9.4 | Implement Chain Validation logic | P0 | 16h | 📋 Not Started | - |
| VAL-9.5 | Implement Blueprint Cache (Redis) | P0 | 8h | 📋 Not Started | - |
| VAL-9.6 | Implement Verified Transaction Queue (in-memory) | P0 | 12h | 📋 Not Started | - |
| VAL-9.7 | Implement Exception Response Handler | P0 | 10h | 📋 Not Started | - |
| VAL-9.8 | Implement Docket Builder | P0 | 16h | 📋 Not Started | - |
| VAL-9.9 | Peer Service integration (message source) | P0 | 12h | 📋 Not Started | - |
| VAL-9.10 | Register Service integration (docket submission) | P0 | 8h | 📋 Not Started | - |
| VAL-9.11 | Configuration system (memory limits, performance) | P1 | 8h | 📋 Not Started | - |
| VAL-9.12 | Validator Service unit tests | P0 | 20h | 📋 Not Started | - |
| VAL-9.13 | Validator Service integration tests | P1 | 16h | 📋 Not Started | - |
| VAL-9.14 | Performance testing (validation throughput) | P1 | 12h | 📋 Not Started | - |

**Sprint 9 Status:** 📋 **NOT STARTED** (0/14 tasks, 182 hours ~23 days)
**Recommended Start:** Week 13 (After Sprint 8 completion)
**Reference:** [VALIDATOR-SERVICE-REQUIREMENTS.md](VALIDATOR-SERVICE-REQUIREMENTS.md)

**Key Features:**
- Transaction validation against Blueprint JSON rules (DataSchemas, JSON Logic, Disclosures)
- **Chain-based instance tracking** via previousId (no separate instance ID)
- Exception responses sent to original sender via Peer Service
- Configurable in-memory verified queue with size limits
- Blueprint caching for performance
- Docket building from verified transactions

**Validation Rules:**
1. Schema validation (JSON Schema Draft 2020-12)
2. JSON Logic condition evaluation
3. Disclosure rules and participant authorization
4. **Chain validation** (previousId references, continuity, instance isolation)
5. Previous data validation against chain
6. Workflow integrity (action sequencing, routing)

**Integration Points:**
- Peer Service: Transaction messages (inbound) & exception responses (outbound)
- Blueprint Service: Fetch Blueprint JSON (with caching)
- Register Service: Docket submission
- Redis: Memory pool, Blueprint cache, response queues

**Configuration:**
- Memory limits (verified queue, cache, total)
- Performance tuning (concurrent validations, batch sizes)
- Resilience (retries, circuit breakers, DLQ)

**Related Tasks:** BP-8.4 (Chain validation tests)

### Sprint 10: Blueprint Service Orchestration ✅ COMPLETE

**Goal:** Implement full workflow orchestration with delegation tokens, state reconstruction, and instance management

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-10.1 | Update service clients with delegation token support | P0 | 4h | ✅ Complete | 2025-12-04 |
| BP-10.2 | Implement AccumulatedState model | P0 | 2h | ✅ Complete | 2025-12-04 |
| BP-10.3 | Implement Instance model | P0 | 2h | ✅ Complete | 2025-12-04 |
| BP-10.4 | Implement Branch model | P0 | 1h | ✅ Complete | 2025-12-04 |
| BP-10.5 | Implement NextAction model | P0 | 1h | ✅ Complete | 2025-12-04 |
| BP-10.6 | Implement IStateReconstructionService interface | P0 | 2h | ✅ Complete | 2025-12-04 |
| BP-10.7 | Implement IActionExecutionService interface | P0 | 2h | ✅ Complete | 2025-12-04 |
| BP-10.8 | Implement StateReconstructionService | P0 | 8h | ✅ Complete | 2025-12-04 |
| BP-10.9 | Implement ActionExecutionService | P0 | 12h | ✅ Complete | 2025-12-04 |
| BP-10.10 | Implement DelegationTokenMiddleware | P0 | 4h | ✅ Complete | 2025-12-04 |
| BP-10.11 | Implement IInstanceStore and InMemoryInstanceStore | P0 | 4h | ✅ Complete | 2025-12-04 |
| BP-10.12 | Add orchestration API endpoints | P0 | 6h | ✅ Complete | 2025-12-04 |
| BP-10.13 | Fix unit test compilation and failures | P0 | 4h | ✅ Complete | 2025-12-04 |
| BP-10.14 | Fix integration test DI configuration | P0 | 4h | ✅ Complete | 2025-12-04 |
| BP-10.15 | Write StateReconstructionService tests | P1 | 4h | ✅ Complete | 2025-12-04 |
| BP-10.16 | Write ActionExecutionService tests | P1 | 4h | ✅ Complete | 2025-12-04 |

**Sprint 10 Status:** ✅ **COMPLETE** (16/16 tasks, 64 hours)
**Completed:** 2025-12-04

**Key Deliverables:**
- ✅ StateReconstructionService - Reconstructs accumulated state from prior transactions using delegation tokens
- ✅ ActionExecutionService - 15-step orchestration: instance lookup → state reconstruction → validation → routing → transaction building → signing → submission → notification
- ✅ DelegationTokenMiddleware - Extracts X-Delegation-Token header and injects into request context
- ✅ Instance management - Full CRUD operations via IInstanceStore with in-memory implementation
- ✅ Orchestration models - AccumulatedState, Instance, Branch, NextAction, BranchState
- ✅ Extended service clients - IWalletServiceClient.DecryptWithDelegationAsync, IRegisterServiceClient.GetTransactionsByInstanceIdAsync
- ✅ New API endpoints - POST /api/instances/{id}/actions/{actionId}/execute, POST /api/instances/{id}/actions/{actionId}/reject, GET /api/instances/{id}/state
- ✅ BlueprintServiceWebApplicationFactory - Custom test factory with mock HTTP handlers, in-memory cache, no-op output cache
- ✅ 123 total tests passing (98 pre-existing + 25 new orchestration tests)

**Test Coverage:**
- StateReconstructionServiceTests: 10 tests (constructor validation, reconstruction scenarios, branch handling)
- ActionExecutionServiceTests: 11 tests (constructor validation, execution validation, rejection validation)
- Integration tests: 57 tests passing with proper DI configuration

### Sprint 11: Production Readiness

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-11.1 | Performance optimization | P2 | 8h | 📋 Not Started | - |
| BP-11.2 | Security hardening | P1 | 8h | 📋 Not Started | - |
| BP-11.3 | Monitoring and alerting | P2 | 6h | 📋 Not Started | - |
| BP-11.4 | Production deployment guide | P2 | 4h | 📋 Not Started | - |

**Sprint 11 Status:** 📋 **NOT STARTED** (0/4 tasks, 26 hours)
**Recommended Start:** Week 15

---

## Phase 2: Wallet Service API

**Goal:** Create REST API for Wallet Service and integrate with Blueprint Service
**Duration:** Weeks 7-9
**Total Tasks:** 34
**Completion:** 100% (34 complete - core library, API layer, tests, integration, and EF Core persistence)

### Completed: Core Library Implementation ✅

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| WS-001 | Setup Sorcha.WalletService project | P0 | 6h | ✅ Complete | 4 projects created |
| WS-002 | Implement domain models & enums | P0 | 12h | ✅ Complete | 4 entities, 2 value objects, 8 events |
| WS-003 | Define service interfaces | P0 | 8h | ✅ Complete | 4 service interfaces, 3 infrastructure |
| WS-004 | Implement WalletManager | P0 | 20h | ✅ Complete | Fully functional |
| WS-005 | Implement KeyManagementService | P0 | 24h | ✅ Complete | HD wallet, BIP32/39/44 |
| WS-006 | Implement TransactionService | P0 | 16h | ✅ Complete | Sign, verify, encrypt, decrypt |
| WS-007 | Implement DelegationService | P1 | 12h | ✅ Complete | Access control complete |
| WS-010 | InMemoryWalletRepository | P1 | 12h | ✅ Complete | Thread-safe, test-ready |
| WS-011 | LocalEncryptionProvider (AES-GCM) | P1 | 12h | ✅ Complete | Development use only |
| WS-012 | InMemoryEventPublisher | P1 | 8h | ✅ Complete | Test-ready |

**Core Library Status:** ✅ **COMPLETE** (13/13 tasks, 90% functionality)

### ✅ API Layer & Integration (COMPLETE)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| WS-025 | Setup Sorcha.WalletService.Api project | P0 | 6h | ✅ Complete | - |
| WS-026.1 | POST /api/wallets (create wallet) | P0 | 4h | ✅ Complete | - |
| WS-026.2 | GET /api/wallets/{id} (get wallet) | P0 | 3h | ✅ Complete | - |
| WS-026.3 | POST /api/wallets/{id}/sign (sign transaction) | P0 | 5h | ✅ Complete | - |
| WS-026.4 | POST /api/wallets/{id}/decrypt (decrypt payload) | P0 | 4h | ✅ Complete | - |
| WS-026.5 | POST /api/wallets/{id}/addresses (generate address) | P1 | 4h | ⚠️ 501 By Design | - |
| WS-026.6 | POST /api/wallets/{id}/encrypt (encrypt payload) | P0 | 4h | ✅ Complete | - |
| WS-027 | .NET Aspire integration | P0 | 12h | ✅ Complete | - |
| WS-028 | API integration with ApiGateway | P0 | 6h | ✅ Complete | - |
| WS-029 | OpenAPI documentation | P1 | 4h | ✅ Complete | - |
| WS-030 | Unit tests for API layer | P0 | 10h | ✅ Complete | - |
| WS-031 | Integration tests (E2E) | P0 | 12h | ✅ Complete | - |

**API Layer Status:** ✅ **COMPLETE** (12/12 tasks, 68 hours)
**Completed:** 2025-11-17
**Notes:**
- 2 Controllers: WalletsController (10 endpoints), DelegationController (4 endpoints)
- 25+ integration tests, 20+ unit tests
- YARP reverse proxy configured: /api/wallets/* → Wallet Service
- GenerateAddress returns 501 Not Implemented (by design - mnemonic not stored)

### Integration with Blueprint Service ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| WS-INT-1 | Update Blueprint Service to use Wallet API | P2 | 8h | ✅ Complete | - |
| WS-INT-2 | Replace encryption/decryption stubs | P2 | 6h | ✅ Complete | - |
| WS-INT-3 | End-to-end integration tests | P2 | 12h | ✅ Complete | - |
| WS-INT-4 | Performance testing | P2 | 6h | ✅ Complete | - |

**Integration Status:** ✅ **COMPLETE** (4/4 tasks, 32 hours)
**Completed:** 2025-11-17 (completed under Sprint 6 & 7 task IDs: BP-6.1-6.6, BP-7.2)
**Audit Finding:** WalletServiceClient implemented (256 LOC), PayloadResolverService updated, 27 E2E tests found, NBomber performance tests complete

### Optional: Enhanced Storage & Encryption (Post-MVD)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| WS-008 | EF Core repository implementation | P2 | 20h | ✅ Complete | 2025-12-13 |
| WS-009 | Database migrations (PostgreSQL) | P2 | 8h | ✅ Complete | 2025-12-13 |
| WS-013 | Azure Key Vault provider | P2 | 16h | 📋 Not Started | - |

**Enhancement Status:** ✅ **2/3 COMPLETE** (2 complete, 0 in progress, 1 not started, 28/44 hours)

**WS-008/009 Completion Notes (2025-12-13):**
- ✅ EfCoreWalletRepository.cs with complete CRUD operations, soft delete, optimistic concurrency
- ✅ WalletDbContext.cs with 4 entities (Wallets, WalletAddresses, WalletAccess, WalletTransactions)
- ✅ PostgreSQL-specific features: JSONB columns, gen_random_uuid(), comprehensive indexing
- ✅ Migration 20251207234439_InitialWalletSchema created and applied
- ✅ Smart DI configuration: EF Core if PostgreSQL configured, InMemory otherwise
- ✅ NpgsqlDataSource with EnableDynamicJson for Dictionary<string, string> serialization
- ✅ Automatic migration application on service startup
- ✅ Connection string workaround for Windows/Docker Desktop: host.docker.internal
- ✅ Wallet schema verified in PostgreSQL: all 4 tables + indexes created
- 📋 Azure Key Vault provider (WS-013) remains for production key storage

---

## Phase 3: Register Service (MVD)

**Goal:** Build simplified Register Service for transaction storage and retrieval
**Duration:** Weeks 10-12
**Total Tasks:** 15
**Completion:** 100% (Core, API, and comprehensive testing complete)

### ✅ Phase 1-2: Core Implementation (COMPLETE)

**Status:** Completed

**What Exists (~3,500 LOC):**
- ✅ Domain models: Register, TransactionModel, Docket, PayloadModel, TransactionMetaData
- ✅ RegisterManager - CRUD operations (204 lines)
- ✅ TransactionManager - Storage/retrieval (225 lines)
- ✅ DocketManager - Block creation/sealing (255 lines)
- ✅ QueryManager - Advanced queries (233 lines)
- ✅ ChainValidator - Integrity validation (268 lines)
- ✅ IRegisterRepository abstraction (214 lines, 20+ methods)
- ✅ InMemoryRegisterRepository implementation (265 lines)
- ✅ Event system (IEventPublisher, RegisterEvents)

### ✅ API Integration Tasks (COMPLETE)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| REG-INT-1 | Refactor API to use core managers | P2 | 12h | ✅ Complete | - |
| REG-CODE-DUP | Resolve DocketManager/ChainValidator duplication | P1 | 4h | ✅ Complete | 2025-12-09 |
| REG-003 | MongoDB transaction repository | P1 | 12h | 📋 Deferred | - |
| REG-005 | Implement POST /api/registers/{id}/transactions | P0 | 8h | ✅ Complete | - |
| REG-006 | Implement GET /api/registers/{id}/transactions/{txId} | P0 | 6h | ✅ Complete | - |
| REG-007 | Implement GET /api/registers/{id}/transactions | P0 | 8h | ✅ Complete | - |
| REG-008 | Implement Query API endpoints | P0 | 12h | ✅ Complete | - |
| REG-009 | .NET Aspire integration | P0 | 8h | ✅ Complete | - |
| REG-010 | Unit tests for core logic | P0 | 16h | ✅ Complete | - |
| REG-011 | Integration tests | P0 | 16h | ✅ Complete | - |
| REG-012 | SignalR hub integration tests | P0 | 8h | ✅ Complete | - |
| REG-013 | OData V4 support | P1 | 8h | ✅ Complete | - |

**API Integration Status:** ✅ **COMPLETE** (11/13 tasks, 2 deferred to post-MVD)
**Achievement:** API fully integrated with comprehensive testing (112 tests, ~2,459 LOC)
**Recommended Next:** End-to-end integration with Blueprint and Wallet services

### Week 12: Integration & Testing ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| REG-INT-2 | Integrate with Blueprint Service | P2 | 8h | ✅ Complete | - |
| REG-INT-3 | Update transaction submission flow | P2 | 6h | ✅ Complete | - |
| REG-INT-4 | End-to-end workflow tests | P2 | 16h | ✅ Complete | - |
| REG-INT-5 | Performance testing | P2 | 8h | ✅ Complete | - |

**Integration Status:** ✅ **COMPLETE** (4/4 tasks, 38 hours)
**Completed:** 2025-11-17 (completed under Sprint 6 & 7 task IDs: BP-6.2, BP-6.4, BP-6.6, BP-7.1-7.2)
**Audit Finding:** RegisterServiceClient implemented (281 LOC), transaction submission working, E2E tests exist, performance testing complete
**Note:** Task IDs renumbered to avoid duplication with REG-INT-1 and REG-CODE-DUP above

---

## Phase 4: Post-MVD Enhancements

**Goal:** Improve quality, performance, and add advanced features
**Duration:** Weeks 13-18
**Total Tasks:** 25
**Completion:** 0%

### Blueprint Service Enhancements

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| ENH-BP-1 | Database persistence (EF Core) | P2 | 16h | 📋 Not Started | - |
| ENH-BP-2 | Blueprint versioning improvements | P3 | 8h | 📋 Not Started | - |
| ENH-BP-3 | Graph cycle detection | P2 | 8h | 📋 Not Started | - |
| ENH-BP-4 | Advanced validation rules | P3 | 10h | 📋 Not Started | - |
| ENH-BP-5 | Blueprint templates | P3 | 12h | 📋 Not Started | - |

### Wallet Service Enhancements

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| ENH-WS-1 | EF Core repository | P2 | 20h | 📋 Not Started | - |
| ENH-WS-2 | Azure Key Vault provider | P2 | 16h | 📋 Not Started | - |
| ENH-WS-3 | AWS KMS provider | P3 | 16h | 📋 Not Started | - |
| ENH-WS-4 | Wallet recovery features | P2 | 10h | 📋 Not Started | - |
| ENH-WS-5 | Advanced access control | P3 | 12h | 📋 Not Started | - |

### Register Service Enhancements

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| ENH-REG-1 | Advanced block validation | P3 | 12h | 📋 Not Started | - |
| ENH-REG-2 | Consensus mechanism | P3 | 24h | 📋 Not Started | - |
| ENH-REG-3 | Block synchronization | P3 | 16h | 📋 Not Started | - |
| ENH-REG-4 | Query optimization | P2 | 8h | 📋 Not Started | - |

### Cryptography Enhancements

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CRYPT-1 | Implement key recovery (RecoverKeySetAsync) | P2 | 8h | ❌ Not Implemented | - |
| CRYPT-2 | NIST P-256 ECIES encryption | P2 | 12h | 📋 Not Started | - |
| CRYPT-3 | Additional hash algorithms | P3 | 6h | 📋 Not Started | - |

**Note:** CRYPT-1 has stub method that returns "not yet implemented" error. See TASK-AUDIT-REPORT.md Section 4.

### TransactionHandler Enhancements

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| TX-016 | Migration guide documentation | P2 | 4h | 📋 Not Started | - |
| TX-017 | Code examples and samples | P2 | 6h | 📋 Not Started | - |
| TX-018 | Service integration validation | P2 | 16h | ✅ Complete | - |
| TX-019 | Regression testing | P2 | 12h | ✅ Complete | - |

**Audit Finding:** TX-019 complete with 94 tests across 10 files (backward compatibility, integration, unit tests). TX-018 validated through Sprint 6 & 7 integration work.

### Performance & Monitoring

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| PERF-1 | Caching optimization | P2 | 10h | 📋 Not Started | - |
| PERF-2 | Database query optimization | P2 | 8h | 📋 Not Started | - |
| PERF-3 | Load balancing configuration | P3 | 6h | 📋 Not Started | - |
| MON-1 | Advanced monitoring dashboards | P3 | 10h | 📋 Not Started | - |
| MON-2 | Alerting configuration | P2 | 6h | 📋 Not Started | - |

---

## Production Readiness Tasks

**Goal:** Critical security, authentication, and operational tasks required for production deployment
**Duration:** 2-3 weeks (parallel with MVD demo preparation)
**Total Tasks:** 10
**Completion:** 0% (newly identified during audit)

**⚠️ CRITICAL:** These tasks were NOT tracked in previous versions of this document but are ESSENTIAL for production deployment.

### Authentication & Authorization (P0 - BLOCKERS)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| AUTH-001 | Implement Tenant Service (JWT + RBAC + Delegation) | P0 | 80h | 🚧 80% Complete | - |
| AUTH-002 | Integrate services with Tenant Service authentication | P0 | 24h | ✅ Complete | 2025-12-12 |
| AUTH-003 | Deploy PostgreSQL + Redis for Tenant Service | P0 | 8h | ✅ Complete | 2025-12-12 |
| AUTH-004 | Bootstrap seed scripts (admin + service principals) | P0 | 12h | ✅ Complete | 2025-12-12 |
| AUTH-005 | Production deployment with Azure AD/B2C | P1 | 16h | 📋 Not Started | - |

**Rationale:** Services currently have NO authentication/authorization. All APIs are completely open!

**AUTH-001 Status:** ✅ **Specification 100% complete** ([View Spec](.specify/specs/sorcha-tenant-service.md))
- ✅ User authentication with JWT tokens (60 min lifetime)
- ✅ Service-to-service authentication (OAuth2 client credentials, 8 hour tokens)
- ✅ Delegation tokens for Blueprint→Wallet→Register flows
- ✅ Token refresh flow (24 hour refresh token lifetime)
- ✅ Hybrid token validation (local JWT + optional introspection)
- ✅ Token revocation with Redis-backed store
- ✅ Multi-tenant organization management
- ✅ 9 authorization policies (RBAC)
- ✅ 30+ REST API endpoints documented
- ✅ Stateless horizontal scaling architecture
- ✅ 99.5% SLA target with degraded operation modes
- 🚧 Implementation 80% complete (core features implemented)
- 📋 PostgreSQL repository pending
- 📋 Production deployment pending

**AUTH-002 Status:** ✅ **Complete (2025-12-12)**
- ✅ Blueprint Service: JWT Bearer authentication with authorization policies (CanManageBlueprints, CanExecuteBlueprints, CanPublishBlueprints, RequireService)
- ✅ Wallet Service: JWT Bearer authentication with authorization policies (CanManageWallets, CanUseWallet, RequireService)
- ✅ Register Service: JWT Bearer authentication with authorization policies (CanManageRegisters, CanSubmitTransactions, CanReadTransactions, RequireService)
- ✅ Configuration: Shared JWT settings template (appsettings.jwt.json)
- ✅ Documentation: Complete authentication setup guide (docs/AUTHENTICATION-SETUP.md)
- 📋 Peer Service authentication pending (service not yet implemented)
- 📋 API Gateway JWT validation pending

**AUTH-003 Status:** ✅ **Complete (2025-12-12)** - Infrastructure deployment complete
- ✅ PostgreSQL 17 container configured and tested
- ✅ Redis 8 container configured and tested
- ✅ MongoDB 8 container configured and tested
- ✅ Docker Compose infrastructure-only file created (`docker-compose.infrastructure.yml`)
- ✅ Database initialization script (`scripts/init-databases.sql`)
- ✅ Connection strings aligned between Docker Compose and appsettings
- ✅ Health checks configured for all infrastructure services
- ✅ Comprehensive infrastructure setup guide created (`docs/INFRASTRUCTURE-SETUP.md`)
- ✅ Data persistence with Docker volumes
- ⚠️  **Note:** Windows/Docker Desktop may require `host.docker.internal` for host connectivity

**AUTH-004 Status:** ✅ **Complete (2025-12-12)** - Automatic database seeding implemented
- ✅ DatabaseInitializer enhanced with service principal seeding
- ✅ Default organization created: "Sorcha Local" (subdomain: `sorcha-local`)
- ✅ Default admin user: `admin@sorcha.local` / `Dev_Pass_2025!`
- ✅ Service principals created: Blueprint, Wallet, Register, Peer services
- ✅ Well-known GUIDs for consistent testing
- ✅ Client secrets generated and logged on first startup
- ✅ Configurable via appsettings ("Seed:*" configuration keys)
- ✅ Documentation added to scripts/README.md
- ⚠️  **Action Required:** Copy service principal secrets from Tenant Service logs on first startup

### Security Hardening (P0-P1)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| SEC-001 | HTTPS enforcement and certificate management | P0 | 4h | 🚧 Partial | - |
| SEC-002 | API rate limiting and throttling | P1 | 8h | 📋 Not Started | - |
| SEC-003 | Input validation hardening (OWASP compliance) | P1 | 12h | 📋 Not Started | - |
| SEC-004 | Security headers (CSP, HSTS, X-Frame-Options) | P1 | 4h | ✅ Complete | 2025-12-09 |

**Related:** BP-8.2 Security hardening task (promoted from P1 in Phase 1)

### Operations & Monitoring (P1)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| OPS-001 | Production logging infrastructure (Serilog/ELK) | P1 | 8h | 🚧 Partial | - |
| OPS-002 | Health check endpoints (deep checks) | P1 | 4h | ✅ Complete | - |
| OPS-003 | Deployment documentation and runbooks | P1 | 8h | 📋 Not Started | - |

**Note:** OPS-002 already implemented via .NET Aspire health checks

### Data Management (P1)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| DATA-001 | Database backup and restore strategy | P1 | 6h | 📋 Not Started | - |
| DATA-002 | Database migration scripts and versioning | P1 | 8h | 📋 Not Started | - |

**Related:** ENH-WS-1, REG-003, ENH-BP-1 (database persistence implementations)

---

## CLI Admin Tool Implementation

**Goal:** Build cross-platform administrative CLI with interactive console and automation support
**Duration:** 8 weeks (4 sprints of 2 weeks each)
**Total Tasks:** 52 (12 + 13 + 12 + 15 across 4 sprints)
**Completion:** 0% (specification complete, implementation pending)
**Related Specification:** [sorcha-cli-admin-tool.md](.specify/specs/sorcha-cli-admin-tool.md)

**Key Features:**
- Interactive console mode (REPL) with history and tab completion
- Flag-based mode for scripts and AI agents
- Authentication caching with OS-specific encryption
- Tenant, Register, and Peer service integration
- Multi-environment profile support
- Cross-platform (Windows, macOS, Linux)

### Sprint 1: Foundation & Infrastructure (Weeks 1-2)

**Goal:** Project structure, configuration management, authentication, and token caching

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-1.1 | Create Sorcha.Cli project structure | P0 | 2h | 📋 Not Started | - |
| CLI-1.2 | Configure System.CommandLine framework | P0 | 4h | 📋 Not Started | - |
| CLI-1.3 | Implement configuration management (profiles) | P0 | 8h | 📋 Not Started | - |
| CLI-1.4 | Implement TokenCache with OS-specific encryption | P0 | 12h | 📋 Not Started | - |
| CLI-1.5 | Implement WindowsDpapiEncryption provider | P0 | 4h | 📋 Not Started | - |
| CLI-1.6 | Implement MacOsKeychainEncryption provider | P0 | 4h | 📋 Not Started | - |
| CLI-1.7 | Implement LinuxSecretServiceEncryption provider | P0 | 4h | 📋 Not Started | - |
| CLI-1.8 | Implement AuthenticationService with caching | P0 | 8h | 📋 Not Started | - |
| CLI-1.9 | Create base Command classes and routing | P0 | 6h | 📋 Not Started | - |
| CLI-1.10 | Implement global options (--profile, --output, --quiet) | P1 | 4h | 📋 Not Started | - |
| CLI-1.11 | Implement exit code standards (0-8) | P1 | 2h | 📋 Not Started | - |
| CLI-1.12 | Unit tests for configuration and auth services | P1 | 6h | 📋 Not Started | - |

**Sprint 1 Total:** 12 tasks, 64 hours

**Deliverables:**
- CLI project compiles and installs as global tool (`dotnet tool install -g`)
- Configuration profiles work (create, switch, list)
- Authentication service with token caching functional
- OS-specific encryption providers implemented
- Base command framework ready

---

### Sprint 2: Tenant Service Commands (Weeks 3-4)

**Goal:** Organization, user, and service principal management commands

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-2.1 | Create Refit ITenantServiceClient interface | P0 | 4h | 📋 Not Started | - |
| CLI-2.2 | Configure HTTP client with Polly resilience policies | P0 | 4h | 📋 Not Started | - |
| CLI-2.3 | Implement `sorcha org` commands (list, get, create, update, delete) | P0 | 8h | 📋 Not Started | - |
| CLI-2.4 | Implement `sorcha user` commands (list, get, create, update, delete) | P0 | 8h | 📋 Not Started | - |
| CLI-2.5 | Implement `sorcha principal` commands (list, get, create, delete) | P0 | 6h | 📋 Not Started | - |
| CLI-2.6 | Implement `sorcha principal rotate-secret` command | P1 | 4h | 📋 Not Started | - |
| CLI-2.7 | Implement `sorcha auth login` (user + service) | P0 | 6h | 📋 Not Started | - |
| CLI-2.8 | Implement `sorcha auth logout` and token management | P0 | 4h | 📋 Not Started | - |
| CLI-2.9 | Implement table output formatter (Spectre.Console) | P0 | 6h | 📋 Not Started | - |
| CLI-2.10 | Implement JSON output formatter | P0 | 3h | 📋 Not Started | - |
| CLI-2.11 | Implement CSV output formatter | P1 | 3h | 📋 Not Started | - |
| CLI-2.12 | Unit tests for Tenant Service commands | P1 | 8h | 📋 Not Started | - |
| CLI-2.13 | Integration tests with mock Tenant Service | P1 | 6h | 📋 Not Started | - |

**Sprint 2 Total:** 13 tasks, 70 hours

**Deliverables:**
- All Tenant Service commands functional
- Organization, user, and service principal CRUD
- Authentication (login/logout) working
- Multiple output formats (table, JSON, CSV)
- Unit and integration tests passing

---

### Sprint 3: Register & Transaction Commands (Weeks 5-6)

**Goal:** Register management and transaction viewing/search

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-3.1 | Create Refit IRegisterServiceClient interface | P0 | 4h | 📋 Not Started | - |
| CLI-3.2 | Implement `sorcha register` commands (list, get, create, update, delete) | P0 | 6h | 📋 Not Started | - |
| CLI-3.3 | Implement `sorcha register stats` command | P1 | 4h | 📋 Not Started | - |
| CLI-3.4 | Implement `sorcha tx list` command with pagination | P0 | 6h | 📋 Not Started | - |
| CLI-3.5 | Implement `sorcha tx get` command with payload display | P0 | 4h | 📋 Not Started | - |
| CLI-3.6 | Implement `sorcha tx search` command (query by blueprint, action, etc.) | P1 | 6h | 📋 Not Started | - |
| CLI-3.7 | Implement `sorcha tx verify` command (signatures + chain) | P2 | 6h | 📋 Not Started | - |
| CLI-3.8 | Implement `sorcha tx export` command (JSON/CSV/Excel) | P2 | 6h | 📋 Not Started | - |
| CLI-3.9 | Implement `sorcha tx timeline` command | P2 | 4h | 📋 Not Started | - |
| CLI-3.10 | Add pagination support for list commands | P1 | 4h | 📋 Not Started | - |
| CLI-3.11 | Unit tests for Register and Transaction commands | P1 | 6h | 📋 Not Started | - |
| CLI-3.12 | Integration tests with mock Register Service | P1 | 6h | 📋 Not Started | - |

**Sprint 3 Total:** 12 tasks, 62 hours

**Deliverables:**
- Register CRUD commands functional
- Transaction viewer (list, get, search)
- Transaction verification and export (P2 features)
- Pagination support
- Integration tests passing

---

### Sprint 4: Peer Service, Interactive Mode & Polish (Weeks 7-8)

**Goal:** Peer monitoring, interactive console (REPL), and final polish

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-4.1 | Create Refit IPeerServiceClient interface | P0 | 3h | 📋 Not Started | - |
| CLI-4.2 | Implement `sorcha peer list` command | P0 | 4h | 📋 Not Started | - |
| CLI-4.3 | Implement `sorcha peer get` command with metrics | P0 | 4h | 📋 Not Started | - |
| CLI-4.4 | Implement `sorcha peer topology` command (tree/graph) | P1 | 6h | 📋 Not Started | - |
| CLI-4.5 | Implement `sorcha peer health` command | P1 | 4h | 📋 Not Started | - |
| CLI-4.6 | Implement interactive console mode (ConsoleHost) | P1 | 12h | 📋 Not Started | - |
| CLI-4.7 | Implement command history (CommandHistory class) | P1 | 4h | 📋 Not Started | - |
| CLI-4.8 | Implement tab completion (TabCompleter class) | P1 | 8h | 📋 Not Started | - |
| CLI-4.9 | Implement context awareness (ConsoleContext) | P1 | 4h | 📋 Not Started | - |
| CLI-4.10 | Implement special console commands (help, clear, status, use, exit) | P1 | 4h | 📋 Not Started | - |
| CLI-4.11 | Implement audit logging to ~/.sorcha/audit.log | P1 | 4h | 📋 Not Started | - |
| CLI-4.12 | Add comprehensive error handling and user-friendly messages | P1 | 6h | 📋 Not Started | - |
| CLI-4.13 | Write user documentation (README, command reference) | P1 | 8h | 📋 Not Started | - |
| CLI-4.14 | Package as .NET global tool and publish to NuGet | P0 | 4h | 📋 Not Started | - |
| CLI-4.15 | E2E testing on Windows, macOS, and Linux | P1 | 8h | 📋 Not Started | - |

**Sprint 4 Total:** 15 tasks, 83 hours

**Deliverables:**
- Peer monitoring commands functional
- Interactive console mode (REPL) fully working
- Command history and tab completion
- Context-aware prompts
- Audit logging
- Published to NuGet as global tool
- Cross-platform testing complete
- User documentation complete

---

### CLI Implementation Summary

**Total Effort:** 279 hours (~7 weeks of full-time work)

**Sprint Breakdown:**
- Sprint 1 (Foundation): 12 tasks, 64 hours
- Sprint 2 (Tenant): 13 tasks, 70 hours
- Sprint 3 (Register): 12 tasks, 62 hours
- Sprint 4 (Peer + REPL): 15 tasks, 83 hours

**Priority Distribution:**
- P0 (Critical): 23 tasks (CLI must work for administration)
- P1 (High): 18 tasks (REPL, advanced features)
- P2 (Medium): 3 tasks (Nice-to-have features)

**Testing Coverage:**
- Unit tests: CLI-1.12, CLI-2.12, CLI-3.11 (20 hours)
- Integration tests: CLI-2.13, CLI-3.12 (12 hours)
- E2E tests: CLI-4.15 (8 hours)
- **Total testing effort:** 40 hours (14% of total)

**Dependencies:**
- Sprint 1 must complete before Sprint 2 (auth and config required)
- Sprint 2 must complete before Sprint 3 (HTTP client framework)
- Sprint 4 can partially overlap with Sprint 3 (Peer + REPL independent)

**Success Criteria:**
- ✅ Install as global tool: `dotnet tool install -g sorcha.cli`
- ✅ Authenticate and cache tokens across commands
- ✅ Manage organizations, users, service principals
- ✅ View registers and transactions
- ✅ Monitor peer network
- ✅ Interactive console mode with history and completion
- ✅ Script-friendly with JSON output and exit codes

---

### Sprint 5: Bootstrap Automation (Additional - Weeks 9-10)

**Goal:** Bootstrap commands for automated platform setup

**Background:** Bootstrap scripts have been created (`scripts/bootstrap-sorcha.ps1` and `scripts/bootstrap-sorcha.sh`) that guide users through initial Sorcha installation setup. These scripts currently use placeholder commands and require the following CLI enhancements to be fully functional.

**Related Files:**
- `scripts/bootstrap-sorcha.ps1` (PowerShell bootstrap script)
- `scripts/bootstrap-sorcha.sh` (Bash bootstrap script)
- `scripts/README-BOOTSTRAP.md` (Bootstrap documentation)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-BOOTSTRAP-001 | Implement `sorcha config init` command | P0 | 6h | 📋 Not Started | - |
| CLI-BOOTSTRAP-002 | Implement `sorcha org create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-003 | Implement `sorcha user create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-004 | Implement `sorcha sp create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-005 | Implement `sorcha register create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-006 | Implement `sorcha node configure` command (NEW) | P1 | 6h | 📋 Not Started | - |
| TENANT-SERVICE-001 | Implement bootstrap API endpoint | P1 | 8h | ✅ Complete | 2026-01-01 |
| PEER-SERVICE-001 | Implement node configuration API | P1 | 6h | 📋 Not Started | - |

**Sprint 5 Total:** 8 tasks, 42 hours

**Task Details:**

#### CLI-BOOTSTRAP-001: Implement `sorcha config init`
**Purpose:** Initialize CLI configuration profile with service URLs

**Command:**
```bash
sorcha config init \
  --profile docker \
  --tenant-url http://localhost/api/tenants \
  --register-url http://localhost/api/register \
  --wallet-url http://localhost/api/wallets \
  --peer-url http://localhost/api/peers \
  --auth-url http://localhost/api/service-auth/token
```

**Implementation:**
- Create or update profile in `~/.sorcha/config.json`
- Validate service URLs connectivity (HTTP GET to health endpoints)
- Set default client ID
- Return success/failure with exit code

**Output:**
```json
{
  "profile": "docker",
  "status": "created",
  "serviceUrls": {
    "tenant": "http://localhost/api/tenants",
    "register": "http://localhost/api/register",
    "wallet": "http://localhost/api/wallets",
    "peer": "http://localhost/api/peers"
  },
  "connectivityCheck": "passed"
}
```

#### CLI-BOOTSTRAP-002: Implement `sorcha org create`
**Purpose:** Create organization with subdomain

**Command:**
```bash
sorcha org create \
  --name "System Organization" \
  --subdomain "system" \
  --description "Primary system organization"
```

**Implementation:**
- Call Tenant Service API: `POST /api/tenants/organizations`
- Create organization with provided details
- Return organization ID in JSON output

**Output:**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "name": "System Organization",
  "subdomain": "system",
  "status": "Active"
}
```

#### CLI-BOOTSTRAP-003: Implement `sorcha user create`
**Purpose:** Create user in organization with role

**Command:**
```bash
sorcha user create \
  --org-id <guid> \
  --email admin@sorcha.local \
  --name "System Administrator" \
  --password <secure> \
  --role Administrator
```

**Implementation:**
- Call Tenant Service API: `POST /api/tenants/users`
- Create user with specified role
- Handle password securely (prompt if not provided)
- Return user ID

**Output:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "admin@sorcha.local",
  "name": "System Administrator",
  "role": "Administrator",
  "organizationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```

#### CLI-BOOTSTRAP-004: Implement `sorcha sp create`
**Purpose:** Create service principal with scopes

**Command:**
```bash
sorcha sp create \
  --name "sorcha-bootstrap" \
  --scopes "all" \
  --description "Bootstrap automation principal"
```

**Implementation:**
- Call Tenant Service API: `POST /api/tenants/service-principals`
- Create service principal
- Generate and return client secret (display once with warning)
- Return client ID

**Output:**
```json
{
  "clientId": "sorcha-bootstrap-20260101",
  "clientSecret": "sk_live_a7b3c2d1_4e5f6a7b8c9d0e1f",
  "name": "sorcha-bootstrap",
  "scopes": ["all"],
  "warning": "This secret is only shown once. Store it securely!"
}
```

#### CLI-BOOTSTRAP-005: Implement `sorcha register create`
**Purpose:** Create register in organization

**Command:**
```bash
sorcha register create \
  --name "System Register" \
  --org-id <guid> \
  --description "Primary system register" \
  --publish
```

**Implementation:**
- Call Register Service API: `POST /api/register/registers`
- Create register
- Optionally publish register
- Return register ID

**Output:**
```json
{
  "id": "reg_abc123",
  "name": "System Register",
  "organizationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "status": "Published"
}
```

#### CLI-BOOTSTRAP-006: Implement `sorcha node configure` (NEW)
**Purpose:** Configure P2P node identity and settings

**Command:**
```bash
sorcha node configure \
  --node-id "node-hostname" \
  --description "Primary Sorcha node" \
  --enable-p2p true \
  --public-address <optional>
```

**Implementation:**
- Call Peer Service API: `POST /api/peers/configure` (requires PEER-SERVICE-001)
- Set node identity
- Configure P2P settings
- Return node status

**Output:**
```json
{
  "nodeId": "node-hostname",
  "description": "Primary Sorcha node",
  "p2pEnabled": true,
  "status": "configured"
}
```

#### TENANT-SERVICE-001: Implement Bootstrap API Endpoint
**Purpose:** Atomic bootstrap operation (organization + admin user + service principal)

**Endpoint:** `POST /api/tenants/bootstrap`

**Request:**
```json
{
  "organizationName": "System Organization",
  "organizationSubdomain": "system",
  "adminEmail": "admin@sorcha.local",
  "adminName": "System Administrator",
  "adminPassword": "<secure>",
  "servicePrincipalName": "sorcha-bootstrap"
}
```

**Response:**
```json
{
  "organizationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "servicePrincipal": {
    "clientId": "sorcha-bootstrap-20260101",
    "clientSecret": "sk_live_a7b3c2d1_4e5f6a7b8c9d0e1f"
  },
  "warning": "Client secret is only shown once. Store it securely!"
}
```

**Benefits:**
- Single atomic operation (rollback on failure)
- Consistent state
- Simplified bootstrap flow
- Reduced API calls

**Implementation:**
- Use database transaction to ensure atomicity
- Create organization
- Create admin user in organization
- Create service principal
- Commit or rollback
- Return all IDs and credentials

#### PEER-SERVICE-001: Implement Node Configuration API
**Purpose:** Configure peer node identity and P2P settings

**Endpoint:** `POST /api/peers/configure`

**Request:**
```json
{
  "nodeId": "node-hostname",
  "description": "Primary Sorcha node",
  "enableP2P": true,
  "publicAddress": "optional-external-ip"
}
```

**Response:**
```json
{
  "nodeId": "node-hostname",
  "description": "Primary Sorcha node",
  "p2pEnabled": true,
  "publicAddress": null,
  "status": "configured"
}
```

**Implementation:**
- Store node configuration in database/config
- Update P2P service settings
- Enable/disable P2P networking
- Return current configuration status

**Deliverables:**
- Bootstrap commands fully functional
- Bootstrap scripts work end-to-end
- Atomic bootstrap API endpoint
- Node configuration API
- Updated bootstrap script documentation

**Dependencies:**
- Requires Sprint 2 (Tenant commands framework)
- Requires Sprint 3 (Register commands framework)
- TENANT-SERVICE-001 requires Tenant Service database
- PEER-SERVICE-001 requires Peer Service configuration store
- ✅ Works on Windows, macOS, and Linux

---

## Deferred Tasks

**These tasks are not required for MVD and will be addressed post-launch:**

### Peer Service Transaction Processing

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| PEER-1 | Transaction processing loop | P3 | 12h | 📋 Deferred | Sprint 4 originally planned |
| PEER-2 | Transaction distribution | P3 | 10h | 📋 Deferred | P2P gossip protocol |
| PEER-3 | Streaming communication | P3 | 8h | 📋 Deferred | gRPC streaming |

### Tenant Service Full Implementation

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| TENANT-1 | Multi-tenant data isolation | P3 | 16h | 📋 Deferred | Use simple provider for MVD |
| TENANT-2 | Azure AD integration | P3 | 12h | 📋 Deferred | Full identity federation |
| TENANT-3 | Billing and metering | P3 | 20h | 📋 Deferred | Enterprise feature |

### Advanced Features

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| ADV-1 | Smart contract support | P3 | 40h | 📋 Deferred | Future roadmap |
| ADV-2 | Advanced consensus | P3 | 32h | 📋 Deferred | Beyond simple Register |
| ADV-3 | External SDK development | P3 | 24h | 📋 Deferred | Developer ecosystem |
| ADV-4 | Blueprint marketplace | P3 | 30h | 📋 Deferred | Community feature |

---

## Task Management

### Weekly Review Process

1. **Monday:** Review completed tasks from previous week
2. **Wednesday:** Check in-progress tasks, identify blockers
3. **Friday:** Plan next week's tasks, update priorities

### Status Updates

**Completed Tasks:**
- Update status to ✅ Complete
- Document completion date
- Archive related work items

**In Progress Tasks:**
- Update with current progress (%)
- Flag any blockers
- Estimate completion date

**Blocked Tasks:**
- Identify blocker
- Assign owner to resolve
- Escalate if blocking MVD

### Reporting

**Bi-weekly Progress Report:**
- Tasks completed
- Tasks in progress
- Blockers and risks
- Timeline adjustments

---

## Task Dependencies

### Critical Path (MVD Blocking)

```
BP-3.x (Service Layer) → BP-4.x (Action APIs) → BP-5.5 (SignalR)
    ↓
WS-025 → WS-026.x (Wallet API)
    ↓
WS-INT-x (Integration)
    ↓
REG-001 → REG-005/006/007 (Register API)
    ↓
REG-INT-x (Full Integration)
    ↓
BP-7.1 (E2E Tests)
```

### Parallel Tracks

**Track 1: Blueprint Service** (Weeks 1-6)
- BP-3.x, BP-4.x, BP-5.x can proceed independently

**Track 2: Wallet Service** (Weeks 7-9)
- WS-025 through WS-031 can proceed in parallel with Register Service planning

**Track 3: Register Service** (Weeks 10-12)
- Can start planning while Wallet API is being built

---

## Success Metrics

**Sprint Completion:**
- ✅ Sprint 1: 100% (7/7 tasks)
- ✅ Sprint 2: 100% (8/8 tasks)
- 🎯 Sprint 3: Target 100% by Week 2
- 🎯 Overall MVD: Target 100% by Week 12

**Code Quality:**
- Test coverage >85% for all new code
- Zero critical bugs
- All CI/CD checks passing

**Documentation:**
- OpenAPI specs for all endpoints
- Integration guides updated
- Code examples provided

---

**Related Documents:**
- [MASTER-PLAN.md](MASTER-PLAN.md) - Overall implementation plan
- [Project Constitution](constitution.md) - Standards and principles
- [Project Specification](spec.md) - Requirements and architecture

---

**Last Updated:** 2025-11-16
**Next Review:** Week 3 (after Sprint 3)
**Document Owner:** Sorcha Architecture Team
