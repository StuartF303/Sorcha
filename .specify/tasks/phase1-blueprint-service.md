# Phase 1: Blueprint-Action Service (MVD Core)

**Goal:** Complete the unified Blueprint-Action Service with full execution capabilities
**Duration:** Weeks 1-14 (extended for validation testing)
**Total Tasks:** 82
**Completion:** 95% (78 complete, 0 in progress, 4 not started)

**Validator Service Tasks:** 50
**Validator Completion:** 60% (30 complete, 0 in progress, 20 not started)

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)

---

## Sprint 1: Execution Engine Foundation ✅ COMPLETE

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

---

## Sprint 2: Execution Engine Complete ✅ COMPLETE

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

---

## Sprint 3: Service Layer Foundation ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-3.1 | Implement ActionResolverService | P0 | 8h | ✅ Complete | - |
| BP-3.2 | Implement PayloadResolverService (stubs) | P0 | 10h | ✅ Complete | - |
| BP-3.3 | Implement TransactionBuilderService | P0 | 8h | ✅ Complete | - |
| BP-3.4 | Add Redis caching layer | P1 | 6h | ✅ Complete | - |
| BP-3.5 | Unit tests for service layer | P0 | 12h | ✅ Complete | - |
| BP-3.6 | Integration tests for services | P1 | 8h | ✅ Complete | - |

**Sprint 3 Status:** ✅ **COMPLETE** (6/6 tasks, 52 hours)

---

## Sprint 4: Action API Endpoints ✅ COMPLETE

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

---

## Sprint 5: Execution Helpers & SignalR ✅ SERVER COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-5.1 | POST /api/execution/validate endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.2 | POST /api/execution/calculate endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.3 | POST /api/execution/route endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.4 | POST /api/execution/disclose endpoint | P2 | 4h | ✅ Complete | - |
| BP-5.5 | Implement SignalR ActionsHub | P2 | 8h | ✅ Complete | - |
| BP-5.6 | Redis backplane for SignalR | P2 | 6h | ✅ Complete | - |
| BP-5.7 | SignalR integration tests | P2 | 8h | ✅ Complete | - |
| BP-5.8 | Client-side SignalR integration | P3 | 6h | ✅ Complete | 2026-01-20 |

**Sprint 5 Status:** ✅ **COMPLETE** (8/8 tasks)
**Completed:** 2026-01-20

**BP-5.8 Deliverables:**
- ✅ ActionsHubConnection service for Admin UI (manages SignalR connection lifecycle)
- ✅ Action notification models (ActionNotification, ActionAvailableNotification, etc.)
- ✅ ConnectionState model for UI connection status display
- ✅ MyActions page with real-time updates, connection indicator, and snackbar notifications
- ✅ API Gateway routes for /actionshub SignalR endpoint
**Completed:** 2025-11-17

---

## Sprint 6: Wallet/Register Integration ✅ COMPLETE

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

---

## Sprint 7: Testing & Documentation ✅ COMPLETE

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

---

## Sprint 8: Blueprint Validation Tests ✅ COMPLETE

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

**Sprint 8 Status:** ✅ **COMPLETE** (11/11 tasks, 176 hours)
**Reference:** [BLUEPRINT-VALIDATION-TEST-PLAN.md](../BLUEPRINT-VALIDATION-TEST-PLAN.md)

**Test Coverage Added:** 134 new validation tests

---

## Sprint 9: Validator Service (Decentralized Consensus) 📋 NOT STARTED

**Goal:** Rebuild Sorcha.Validator.Service as a dual-role decentralized consensus participant (initiator + confirmer)

**Updated:** 2026-01-26 - Expanded for decentralized architecture with multi-validator consensus

### Sprint 9A: Core Infrastructure

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.1 | Design dual-role Validator Service architecture | P0 | 12h | ✅ Complete | 2026-01-26 |
| VAL-9.2 | Define Validator Service interfaces and contracts | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.3 | Implement Transaction Pool Poller (Redis) | P0 | 12h | ✅ Complete | 2026-01-26 |
| VAL-9.4 | Implement Blueprint Cache (Redis) | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.5 | Implement Genesis Config Cache | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.6 | Implement Verified Transaction Queue (in-memory) | P0 | 12h | ✅ Complete | 2026-01-26 |

**Sprint 9A Status:** ✅ **COMPLETE** (6/6 tasks, 60 hours)

### Sprint 9B: Validation Engine

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.7 | Implement Validation Engine core | P0 | 24h | ✅ Complete | 2026-01-26 |
| VAL-9.8 | Implement Chain Validation logic (multi-blueprint, versioning) | P0 | 20h | ✅ Complete | 2026-01-26 |
| VAL-9.9 | Implement Blueprint Version Resolution | P0 | 12h | ✅ Complete | 2026-01-26 |
| VAL-9.10 | Implement Exception Response Handler | P0 | 10h | ✅ Complete | 2026-01-26 |
| VAL-9.11 | Validation Engine unit tests | P0 | 16h | ✅ Complete | 2026-01-26 |

**Sprint 9B Status:** ✅ **COMPLETE** (5/5 tasks, 82 hours)

### Sprint 9C: Leader Election & Initiator Role

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.12 | Design Leader Election interface | P0 | 4h | ✅ Complete | 2026-01-26 |
| VAL-9.13 | Implement Rotating Leader Election | P0 | 12h | ✅ Complete | 2026-01-26 |
| VAL-9.14 | Implement Leader Heartbeat mechanism | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.15 | Implement Leader Failure Detection | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.16 | Implement Docket Builder (leader only) | P0 | 16h | ✅ Complete | 2026-01-26 |
| VAL-9.17 | Implement Merkle Tree computation | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.18 | Implement Docket Signing (initiator) | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.19 | Implement Signature Collector | P0 | 16h | ✅ Complete | 2026-01-26 |
| VAL-9.20 | Implement Consensus Failure Handler (abandon/retry) | P0 | 12h | ✅ Complete | 2026-01-26 |
| VAL-9.21 | Implement Pending Docket Store (in-memory) | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.22 | Leader election unit tests | P0 | 12h | ✅ Complete | 2026-01-26 |
| VAL-9.23 | Initiator role unit tests | P0 | 12h | ✅ Complete | 2026-01-26 |

**Sprint 9C Status:** ✅ **COMPLETE** (12/12 tasks, 124 hours)

### Sprint 9D: Confirmer Role

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.24 | Implement Docket Confirmer | P0 | 16h | ✅ Complete | 2026-01-26 |
| VAL-9.25 | Implement Docket Validation (all transactions) | P0 | 12h | ✅ Complete | 2026-01-26 |
| VAL-9.26 | Implement Merkle Root Verification | P0 | 4h | ✅ Complete | 2026-01-26 |
| VAL-9.27 | Implement Initiator Signature Verification | P0 | 4h | ✅ Complete | 2026-01-26 |
| VAL-9.28 | Implement Confirmation Response Handler | P0 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.29 | Implement Bad Actor Detection (logging) | P1 | 8h | ✅ Complete | 2026-01-26 |
| VAL-9.30 | Confirmer role unit tests | P0 | 12h | ✅ Complete | 2026-01-26 |

**Sprint 9D Status:** ✅ **COMPLETE** (7/7 tasks, 64 hours)

### Sprint 9E: Service Integration

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.31 | Peer Service integration - receive transactions (gRPC) | P0 | 12h | 📋 Not Started | - |
| VAL-9.32 | Peer Service integration - broadcast dockets (gRPC) | P0 | 12h | 📋 Not Started | - |
| VAL-9.33 | Peer Service integration - signature exchange (gRPC) | P0 | 12h | 📋 Not Started | - |
| VAL-9.34 | Peer Service integration - docket distribution (gRPC) | P0 | 8h | 📋 Not Started | - |
| VAL-9.35 | Register Service integration - docket submission | P0 | 8h | 📋 Not Started | - |
| VAL-9.36 | Blueprint Service integration - fetch blueprints | P0 | 6h | 📋 Not Started | - |

**Sprint 9E Status:** 📋 **NOT STARTED** (0/6 tasks, 58 hours)

### Sprint 9F: Validator Registration & Genesis

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.37 | Implement Validator Registry (active validators) | P0 | 12h | 📋 Not Started | - |
| VAL-9.38 | Implement Validator Registration flow (public mode) | P0 | 12h | 📋 Not Started | - |
| VAL-9.39 | Implement Validator Approval flow (consent mode) | P1 | 12h | 📋 Not Started | - |
| VAL-9.40 | Implement Genesis Config Reader | P0 | 8h | 📋 Not Started | - |
| VAL-9.41 | Implement Control Docket Processor | P1 | 16h | 📋 Not Started | - |
| VAL-9.42 | Implement Control Blueprint Version Resolver | P0 | 8h | 📋 Not Started | - |
| VAL-9.43 | Validator registration API endpoints | P0 | 8h | 📋 Not Started | - |

**Sprint 9F Status:** 📋 **NOT STARTED** (0/7 tasks, 76 hours)

### Sprint 9G: Configuration & Testing

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| VAL-9.44 | Configuration system (memory limits, performance) | P1 | 8h | 📋 Not Started | - |
| VAL-9.45 | API endpoints (health, metrics, pool stats) | P1 | 8h | 📋 Not Started | - |
| VAL-9.46 | Validator Service integration tests | P0 | 24h | 📋 Not Started | - |
| VAL-9.47 | Multi-validator consensus integration tests | P0 | 20h | 📋 Not Started | - |
| VAL-9.48 | Leader election integration tests | P0 | 12h | 📋 Not Started | - |
| VAL-9.49 | Performance testing (validation throughput) | P1 | 12h | 📋 Not Started | - |
| VAL-9.50 | Performance testing (consensus latency) | P1 | 12h | 📋 Not Started | - |

**Sprint 9G Status:** 📋 **NOT STARTED** (0/7 tasks, 96 hours)

---

**Sprint 9 Total:** 🚧 **IN PROGRESS** (30/50 tasks, 560 hours)

**References:**
- [VALIDATOR-SERVICE-REQUIREMENTS.md](../VALIDATOR-SERVICE-REQUIREMENTS.md) - Updated with decentralized consensus
- [GENESIS-BLUEPRINT-SPEC.md](../GENESIS-BLUEPRINT-SPEC.md) - Genesis block and control blueprint specification

**Key Deliverables:**
- **Leader election** with rotating mechanism (Raft-style future option)
- **Dual-role validator** (leader/initiator + confirmer)
- **Multi-validator consensus** with configurable thresholds
- **Consensus failure handling** (abandon and retry)
- **Genesis blueprint integration** for register governance
- **Control blueprint versioning** for governance updates
- **Blueprint versioning** via transaction chain
- **Validator registration** (public + consent modes)
- **gRPC communication** via Peer Service
- Bad actor detection logging (future: throttling/removal)

---

## Sprint 10: Blueprint Service Orchestration ✅ COMPLETE

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
- ✅ StateReconstructionService - Reconstructs accumulated state from prior transactions
- ✅ ActionExecutionService - 15-step orchestration workflow
- ✅ DelegationTokenMiddleware - X-Delegation-Token header extraction
- ✅ Instance management - Full CRUD via IInstanceStore
- ✅ 123 total tests passing (98 pre-existing + 25 new orchestration tests)

---

## Sprint 11: Production Readiness 📋 NOT STARTED

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| BP-11.1 | Performance optimization | P2 | 8h | 📋 Not Started | - |
| BP-11.2 | Security hardening | P1 | 8h | 📋 Not Started | - |
| BP-11.3 | Monitoring and alerting | P2 | 6h | 📋 Not Started | - |
| BP-11.4 | Production deployment guide | P2 | 4h | 📋 Not Started | - |

**Sprint 11 Status:** 📋 **NOT STARTED** (0/4 tasks, 26 hours)

---

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)
