# Tasks: Validator Service

**Feature Branch**: `validator-service`
**Created**: 2025-12-03
**Status**: 0% Complete (Post-MVD Priority)

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 0 |
| In Progress | 0 |
| Pending | 32 |
| **Total** | **32** |

---

## Phase 1: Foundation

### VAL-001: Create Project Structure
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: None

**Description**: Create Validator Service and Core library projects.

**Acceptance Criteria**:
- [ ] Sorcha.Validator.Service project
- [ ] Sorcha.Validator.Core library
- [ ] Test projects
- [ ] Service defaults reference

---

### VAL-002: Define Interfaces
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-001

**Description**: Define all service interfaces.

**Acceptance Criteria**:
- [ ] IValidatorOrchestrator
- [ ] IDocketBuilder
- [ ] ITransactionValidator
- [ ] IConsensusEngine
- [ ] IMemPoolManager
- [ ] IGenesisManager

---

### VAL-003: Define Models
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-001

**Description**: Define all data models.

**Acceptance Criteria**:
- [ ] Docket model
- [ ] ConsensusVote model
- [ ] GenesisConfig model
- [ ] ValidationResult models
- [ ] MemPoolStats model

---

### VAL-004: Define Configuration
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: TBD
- **Dependencies**: VAL-001

**Description**: Define configuration classes.

**Acceptance Criteria**:
- [ ] ValidatorServiceConfiguration
- [ ] ConsensusConfiguration
- [ ] SecurityConfiguration
- [ ] appsettings.json schema

---

## Phase 2: Core Library

### VAL-005: DocketValidator
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-003

**Description**: Implement docket validation logic.

**Acceptance Criteria**:
- [ ] ComputeDocketHash (SHA256)
- [ ] ValidateDocket
- [ ] Chain integrity checks
- [ ] Deterministic operation

---

### VAL-006: TransactionValidator (Core)
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-003

**Description**: Implement transaction validation logic.

**Acceptance Criteria**:
- [ ] ValidateTransaction
- [ ] ValidateAgainstSchemas
- [ ] Stateless operation
- [ ] No I/O dependencies

---

### VAL-007: ConsensusValidator
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: VAL-003

**Description**: Implement consensus vote validation.

**Acceptance Criteria**:
- [ ] ValidateConsensusVote
- [ ] Quorum calculation
- [ ] Vote aggregation

---

### VAL-008: ChainValidator
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: VAL-005

**Description**: Implement chain integrity validation.

**Acceptance Criteria**:
- [ ] ValidateChainIntegrity
- [ ] Fork detection
- [ ] Genesis validation

---

## Phase 3: Service Layer

### VAL-009: MemPoolManager
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-003

**Description**: Implement per-register MemPools.

**Acceptance Criteria**:
- [ ] AddTransactionAsync
- [ ] GetPendingTransactionsAsync
- [ ] RemoveTransactionsAsync
- [ ] CleanupExpiredTransactionsAsync
- [ ] Thread-safe operations

---

### VAL-010: DocketBuilder (Service)
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 10 hours
- **Assignee**: TBD
- **Dependencies**: VAL-005, VAL-009

**Description**: Implement docket building orchestration.

**Acceptance Criteria**:
- [ ] BuildDocketAsync
- [ ] CreateGenesisBlockAsync
- [ ] External service integration
- [ ] Error handling

---

### VAL-011: TransactionValidator (Service)
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-006

**Description**: Implement transaction validation with external calls.

**Acceptance Criteria**:
- [ ] ValidateAsync
- [ ] ValidateBatchAsync
- [ ] Wallet Service integration
- [ ] Blueprint Service integration

---

### VAL-012: ConsensusEngine
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 12 hours
- **Assignee**: TBD
- **Dependencies**: VAL-007

**Description**: Implement distributed consensus.

**Acceptance Criteria**:
- [ ] AchieveConsensusAsync
- [ ] ValidateConsensusVoteAsync
- [ ] Peer Service integration
- [ ] Timeout handling

---

### VAL-013: GenesisManager
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: VAL-010

**Description**: Implement genesis block creation.

**Acceptance Criteria**:
- [ ] CreateGenesisBlockAsync
- [ ] IsGenesisValidAsync
- [ ] Register Service integration

---

### VAL-014: ValidatorOrchestrator
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 12 hours
- **Assignee**: TBD
- **Dependencies**: VAL-010, VAL-011, VAL-012

**Description**: Implement main orchestrator.

**Acceptance Criteria**:
- [ ] StartValidationAsync
- [ ] StopValidationAsync
- [ ] PauseValidationAsync
- [ ] ResumeValidationAsync
- [ ] GetStatusAsync
- [ ] BackgroundService loop

---

## Phase 4: API Layer

### VAL-015: Service Clients
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-002

**Description**: Implement external service clients.

**Acceptance Criteria**:
- [ ] WalletServiceClient
- [ ] PeerServiceClient
- [ ] RegisterServiceClient
- [ ] BlueprintServiceClient
- [ ] Retry policies

---

### VAL-016: ValidationEndpoints
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-014

**Description**: Implement validation API endpoints.

**Acceptance Criteria**:
- [ ] POST /api/validation/dockets/build
- [ ] POST /api/validation/dockets/validate
- [ ] POST /api/validation/transactions/add
- [ ] POST /api/validation/genesis
- [ ] OpenAPI documentation

---

### VAL-017: AdminEndpoints
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-014

**Description**: Implement admin API endpoints.

**Acceptance Criteria**:
- [ ] POST /api/admin/validation/start
- [ ] POST /api/admin/validation/stop
- [ ] POST /api/admin/validation/pause
- [ ] POST /api/admin/validation/resume
- [ ] GET /api/admin/validation/status

---

### VAL-018: MetricsEndpoints
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-014

**Description**: Implement metrics endpoints.

**Acceptance Criteria**:
- [ ] GET /api/metrics/dockets
- [ ] GET /api/metrics/transactions
- [ ] GET /api/metrics/consensus
- [ ] GET /api/metrics/mempool

---

### VAL-019: Rate Limiting
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-016

**Description**: Implement rate limiting.

**Acceptance Criteria**:
- [ ] Transaction add limits
- [ ] Docket build limits
- [ ] Validation limits
- [ ] Per-IP partitioning

---

### VAL-020: Authentication
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-016

**Description**: Implement JWT authentication.

**Acceptance Criteria**:
- [ ] JWT validation
- [ ] Role-based authorization
- [ ] Service-to-service auth

---

## Phase 5: Testing

### VAL-021: Unit Tests - Core Validators
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-005, VAL-006, VAL-007

**Description**: Unit tests for core library.

**Acceptance Criteria**:
- [ ] DocketValidator tests
- [ ] TransactionValidator tests
- [ ] ConsensusValidator tests
- [ ] ChainValidator tests
- [ ] 90%+ coverage

---

### VAL-022: Unit Tests - MemPoolManager
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-009

**Description**: Unit tests for MemPool.

**Acceptance Criteria**:
- [ ] Add/remove tests
- [ ] Concurrent access tests
- [ ] Expiration tests
- [ ] Size limit tests

---

### VAL-023: Unit Tests - Service Layer
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-014

**Description**: Unit tests for service layer.

**Acceptance Criteria**:
- [ ] DocketBuilder tests
- [ ] TransactionValidator tests
- [ ] ConsensusEngine tests
- [ ] Orchestrator tests

---

### VAL-024: Integration Tests - API
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 10 hours
- **Assignee**: TBD
- **Dependencies**: VAL-016

**Description**: Integration tests for API.

**Acceptance Criteria**:
- [ ] Endpoint tests
- [ ] Error handling tests
- [ ] Authorization tests
- [ ] Rate limiting tests

---

### VAL-025: Integration Tests - Consensus
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 12 hours
- **Assignee**: TBD
- **Dependencies**: VAL-012

**Description**: Integration tests for consensus.

**Acceptance Criteria**:
- [ ] Multi-validator tests
- [ ] Quorum tests
- [ ] Timeout tests
- [ ] Vote verification tests

---

### VAL-026: Performance Tests
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-024

**Description**: Performance benchmarks.

**Acceptance Criteria**:
- [ ] Docket build benchmarks
- [ ] Validation benchmarks
- [ ] MemPool throughput
- [ ] Consensus timing

---

## Phase 6: Integration

### VAL-027: Service Registration
- **Status**: Pending
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: TBD
- **Dependencies**: VAL-016

**Description**: Register with .NET Aspire.

**Acceptance Criteria**:
- [ ] AppHost registration
- [ ] Service discovery
- [ ] Health checks
- [ ] Telemetry

---

### VAL-028: Redis Integration
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-009

**Description**: Integrate Redis for caching.

**Acceptance Criteria**:
- [ ] Cache provider
- [ ] Pub/sub messaging
- [ ] Connection configuration

---

### VAL-029: OpenTelemetry
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: VAL-018

**Description**: Configure telemetry.

**Acceptance Criteria**:
- [ ] Metrics collection
- [ ] Distributed tracing
- [ ] Structured logging

---

### VAL-030: Documentation
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: VAL-016

**Description**: Complete documentation.

**Acceptance Criteria**:
- [ ] README
- [ ] API documentation
- [ ] Configuration guide
- [ ] Deployment guide

---

### VAL-031: Enclave Preparation
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 16 hours
- **Assignee**: TBD
- **Dependencies**: VAL-021

**Description**: Prepare for enclave deployment.

**Acceptance Criteria**:
- [ ] Core library validation (no I/O)
- [ ] SGX build configuration
- [ ] Azure Confidential Computing test

---

### VAL-032: Security Audit
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: VAL-024

**Description**: Security review.

**Acceptance Criteria**:
- [ ] Input validation review
- [ ] Rate limiting validation
- [ ] Authentication review
- [ ] Penetration testing

---

## Notes

- Validator Service is post-MVD priority
- Core library must be enclave-safe (no I/O)
- Consensus implementation uses simple quorum for MVP
- More sophisticated consensus (PBFT) is future consideration
- Enclave deployment is future enhancement
