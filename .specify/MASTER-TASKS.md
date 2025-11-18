# Sorcha Platform - Master Task List

**Version:** 3.2 - AUDITED
**Last Updated:** 2025-11-18
**Status:** Active - Post-Audit
**Related:** [MASTER-PLAN.md](MASTER-PLAN.md) | [TASK-AUDIT-REPORT.md](TASK-AUDIT-REPORT.md)

---

## Overview

This document consolidates all tasks across the Sorcha platform into a single, prioritized list organized by implementation phase. Tasks are tracked by priority, status, and estimated effort.

**Total Tasks:** 158 (across all phases, including production readiness and blueprint validation tasks)
**Completed:** 100 (63%)
**In Progress:** 0 (0%)
**Not Started:** 58 (37%)

**Note:** Counts updated 2025-11-18 after comprehensive task status audit and addition of Blueprint Validation Test Plan (10 new tasks). See [TASK-AUDIT-REPORT.md](TASK-AUDIT-REPORT.md) and [BLUEPRINT-VALIDATION-TEST-PLAN.md](BLUEPRINT-VALIDATION-TEST-PLAN.md) for details.

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
| **Phase 1: Blueprint-Action** | 66 | 54 | 0 | 12 | **82%** ✅ |
| **Phase 2: Wallet Service** | 32 | 32 | 0 | 0 | **100%** ✅ |
| **Phase 3: Register Service** | 15 | 14 | 0 | 1 | **93%** ✅ |
| **Phase 4: Enhancements** | 25 | 0 | 0 | 25 | 0% |
| **Production Readiness** (NEW) | 10 | 0 | 0 | 10 | 0% ⚠️ |
| **Deferred** | 10 | 0 | 0 | 10 | 0% |
| **TOTAL** | **158** | **100** | **0** | **58** | **63%** |

**Note:** Phase 1-3 completion increased significantly after audit corrections. Production Readiness tasks newly identified.

### By Priority

| Priority | Total | Complete | In Progress | Not Started |
|----------|-------|----------|-------------|-------------|
| **P0 - Critical (MVD Blocker)** | 6 | 0 | 0 | 6 ⚠️ |
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
| BP-8.1 | Implement BlueprintStructuralValidationTests | P0 | 16h | 📋 Not Started | - |
| BP-8.2 | Implement BlueprintWorkflowValidationTests | P0 | 24h | 📋 Not Started | - |
| BP-8.3 | Implement graph cycle detection | P0 | 12h | 📋 Not Started | - |
| BP-8.4 | Implement DisclosureValidationTests | P1 | 16h | 📋 Not Started | - |
| BP-8.5 | Extend SchemaValidatorTests (Blueprint/Action schemas) | P1 | 16h | 📋 Not Started | - |
| BP-8.6 | Implement JsonLogicValidationTests | P1 | 24h | 📋 Not Started | - |
| BP-8.7 | Implement MultiParticipantWorkflowTests | P1 | 16h | 📋 Not Started | - |
| BP-8.8 | Implement FormValidationTests | P2 | 8h | 📋 Not Started | - |
| BP-8.9 | Extend BlueprintTemplateServiceTests | P2 | 16h | 📋 Not Started | - |
| BP-8.10 | Extend JSON-LD validation tests | P3 | 8h | 📋 Not Started | - |

**Sprint 8 Status:** 📋 **NOT STARTED** (0/10 tasks, 156 hours ~20 days)
**Recommended Start:** Week 12 (After Sprint 7 completion)
**Reference:** [BLUEPRINT-VALIDATION-TEST-PLAN.md](BLUEPRINT-VALIDATION-TEST-PLAN.md)

**Critical Tests (P0 - MVD Blockers):**
- Structural validation: Participant references, wallet addresses, action/participant counts
- Workflow validation: Action routing, sequence validation
- **Graph cycle detection**: Prevent infinite Blueprint loops (BS-046)

**Core Tests (P1):**
- Disclosure validation: Data visibility rules and recipient validation
- Schema validation: Blueprint/Action embedded schemas, PreviousData
- JSON Logic: Conditions, calculations, participant routing
- Multi-participant workflows: Linear, branching, round-robin patterns

**Enhanced Tests (P2/P3):**
- Form validation: UI control types and schema alignment
- Template validation: Parameter substitution and instantiation
- JSON-LD compliance: Semantic web and Verifiable Credentials

**Deliverables:**
- ~70 new test cases across 10 categories
- Graph cycle detection implementation (critical for workflow integrity)
- Participant reference integrity validation
- Complete Blueprint schema validation coverage
- Multi-participant workflow patterns validated

**Related Tasks:** BS-045, BS-046, BP-3.5, BP-7.1

### Sprint 9: Production Readiness

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-9.1 | Performance optimization | P2 | 8h | 📋 Not Started | - |
| BP-9.2 | Security hardening | P1 | 8h | 📋 Not Started | - |
| BP-9.3 | Monitoring and alerting | P2 | 6h | 📋 Not Started | - |
| BP-9.4 | Production deployment guide | P2 | 4h | 📋 Not Started | - |

**Sprint 9 Status:** 📋 **NOT STARTED** (0/4 tasks, 26 hours)
**Recommended Start:** Week 14

---

## Phase 2: Wallet Service API

**Goal:** Create REST API for Wallet Service and integrate with Blueprint Service
**Duration:** Weeks 7-9
**Total Tasks:** 32
**Completion:** 100% (32 complete - core library, API layer, tests, and integration)

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
| WS-008 | EF Core repository implementation | P2 | 20h | 📋 Not Started | - |
| WS-009 | Database migrations (PostgreSQL) | P2 | 8h | 📋 Not Started | - |
| WS-013 | Azure Key Vault provider | P2 | 16h | 📋 Not Started | - |

**Enhancement Status:** 📋 **DEFERRED** (0/3 tasks, 44 hours)

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
| REG-CODE-DUP | Resolve DocketManager/ChainValidator duplication | P1 | 4h | 📋 Deferred | - |
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
| AUTH-001 | Implement JWT authentication across all services | P0 | 16h | 📋 Not Started | - |
| AUTH-002 | Implement role-based authorization (RBAC) | P0 | 12h | 📋 Not Started | - |
| AUTH-003 | User identity management integration | P1 | 10h | 📋 Not Started | - |

**Rationale:** Services currently have NO authentication/authorization. All APIs are completely open!

### Security Hardening (P0-P1)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| SEC-001 | HTTPS enforcement and certificate management | P0 | 4h | 🚧 Partial | - |
| SEC-002 | API rate limiting and throttling | P1 | 8h | 📋 Not Started | - |
| SEC-003 | Input validation hardening (OWASP compliance) | P1 | 12h | 📋 Not Started | - |
| SEC-004 | Security headers (CSP, HSTS, X-Frame-Options) | P1 | 4h | 📋 Not Started | - |

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
