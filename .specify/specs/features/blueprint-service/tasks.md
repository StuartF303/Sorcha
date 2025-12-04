# Tasks: Blueprint Service

**Feature Branch**: `blueprint-service`
**Created**: 2025-12-03
**Status**: 95% Complete (Unified Blueprint-Action Service)

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 18 |
| In Progress | 2 |
| Pending | 3 |
| **Total** | **23** |

---

## Tasks

### BP-001: Create Blueprint Domain Models
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Define core domain models for Blueprint, Participant, Action, Disclosure, and Control with proper JSON serialization attributes and JSON-LD support.

**Acceptance Criteria**:
- [x] Blueprint.cs with all required properties
- [x] Participant.cs with DID/wallet support
- [x] Action.cs with schemas, disclosures, routing
- [x] Disclosure.cs with JSON Pointer support
- [x] Control.cs for form definitions
- [x] Unit tests for model serialization

---

### BP-002: Implement Fluent Builder API
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001

**Description**: Create type-safe fluent builder API for programmatic blueprint construction.

**Acceptance Criteria**:
- [x] BlueprintBuilder with chained methods
- [x] ParticipantBuilder with identity options
- [x] ActionBuilder with schema and routing support
- [x] JsonLogicBuilder for routing conditions
- [x] FormBuilder for UI definitions
- [x] Comprehensive unit tests (102 tests)

---

### BP-003: Implement JSON Schema Validation
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001

**Description**: Implement JSON Schema validation for action data using NJsonSchema.

**Acceptance Criteria**:
- [x] Schema validation service
- [x] External `$ref` resolution
- [x] Schema caching for performance
- [x] Detailed validation error messages
- [x] Support for Draft 7 features

---

### BP-004: Implement JSON Logic Evaluator
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001

**Description**: Implement JSON Logic evaluation for routing conditions and calculations.

**Acceptance Criteria**:
- [x] JsonLogicEvaluator class
- [x] Support for all standard operators
- [x] Variable resolution from data
- [x] Nested expression evaluation
- [x] Error handling for invalid expressions

---

### BP-005: Create Blueprint API Endpoints
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001, BP-003

**Description**: Implement REST API endpoints for blueprint CRUD operations.

**Acceptance Criteria**:
- [x] POST /api/blueprints - Create
- [x] GET /api/blueprints - List
- [x] GET /api/blueprints/{id} - Get by ID
- [x] PUT /api/blueprints/{id} - Update
- [x] DELETE /api/blueprints/{id} - Delete
- [x] OpenAPI documentation

---

### BP-006: Implement Disclosure Filter
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001

**Description**: Implement data filtering based on disclosure rules using JSON Pointers.

**Acceptance Criteria**:
- [x] DisclosureFilter service
- [x] JSON Pointer path resolution
- [x] Wildcard support (/* for all fields)
- [x] Nested path support (/a/b/c)
- [x] Unit tests for edge cases

---

### BP-007: Create Blueprint Execution Engine
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 12 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-003, BP-004, BP-006

**Description**: Create the portable execution engine for action execution.

**Acceptance Criteria**:
- [x] ExecutionEngine class
- [x] Action validation workflow
- [x] Routing evaluation
- [x] Calculation execution
- [x] Disclosure application
- [x] Stateless, portable design

---

### BP-008: Implement Instance Management
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-005, BP-007

**Description**: Implement workflow instance lifecycle management.

**Acceptance Criteria**:
- [x] Instance creation endpoint
- [x] Action execution endpoint
- [x] State tracking
- [x] Previous transaction linking

---

### BP-009: Implement Form Generation
- **Status**: Complete
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001

**Description**: Generate form definitions from action schemas and control definitions.

**Acceptance Criteria**:
- [x] Form schema generation
- [x] Control type mapping
- [x] Conditional display support
- [x] Layout generation

---

### BP-010: Add Schema Caching
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-003

**Description**: Implement schema caching for improved validation performance.

**Acceptance Criteria**:
- [x] In-memory cache implementation
- [x] Cache invalidation strategy
- [x] Performance improvement (10x)
- [x] See docs/schema-caching-implementation.md

---

### BP-011: Implement Blueprint Versioning
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-005

**Description**: Add version tracking for blueprints with update semantics.

**Acceptance Criteria**:
- [x] Version field auto-increment
- [x] Version history tracking
- [x] Concurrent update handling

---

### BP-012: Add JSON-LD Context Support
- **Status**: Complete
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001

**Description**: Add JSON-LD context support for semantic interoperability.

**Acceptance Criteria**:
- [x] @context field on Blueprint
- [x] Default Sorcha context
- [x] Schema.org vocabulary mapping
- [x] DID support for participants

---

### BP-013: Integration Tests
- **Status**: In Progress
- **Priority**: P1
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-005, BP-008

**Description**: Create comprehensive integration tests for Blueprint API.

**Acceptance Criteria**:
- [x] CRUD operation tests
- [ ] Action execution flow tests
- [ ] Error handling tests
- [ ] Concurrent access tests
- [ ] Performance benchmarks

---

### BP-014: Database Persistence (PostgreSQL)
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 12 hours
- **Assignee**: TBD
- **Dependencies**: BP-005

**Description**: Implement PostgreSQL repository for blueprint persistence.

**Acceptance Criteria**:
- [ ] EF Core DbContext
- [ ] Blueprint entity mapping
- [ ] JSON column handling
- [ ] Migration scripts
- [ ] Repository interface implementation

---

### BP-015: Redis Distributed Caching
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: BP-010

**Description**: Add Redis as distributed cache for schema and blueprint caching.

**Acceptance Criteria**:
- [ ] Redis cache provider
- [ ] Cache serialization
- [ ] TTL configuration
- [ ] Fallback to in-memory

---

### BP-016: JSON-e Template Support
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 8 hours
- **Assignee**: TBD
- **Dependencies**: BP-001

**Description**: Add JSON-e template evaluation for dynamic blueprint generation.

**Acceptance Criteria**:
- [ ] JsonE.NET integration
- [ ] Template storage
- [ ] Template evaluation endpoint
- [ ] Parameter validation

---

### BP-017: Unit Tests - Models
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-001

**Description**: Unit tests for domain models.

**Acceptance Criteria**:
- [x] Serialization round-trip tests
- [x] Validation tests
- [x] Edge case coverage

---

### BP-018: Unit Tests - Fluent Builders
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-002

**Description**: Unit tests for fluent builder API.

**Acceptance Criteria**:
- [x] Builder chain tests
- [x] Validation tests
- [x] 102 tests passing

---

### BP-019: Unit Tests - Engine
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-007

**Description**: Unit tests for execution engine.

**Acceptance Criteria**:
- [x] Execution flow tests
- [x] Routing tests
- [x] Calculation tests
- [x] Error handling tests

---

### BP-020: OpenAPI Documentation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-005

**Description**: Add comprehensive OpenAPI documentation.

**Acceptance Criteria**:
- [x] All endpoints documented
- [x] Request/response examples
- [x] Error responses documented
- [x] Scalar UI integration

---

### BP-021: Service Registration
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-005

**Description**: Register Blueprint Service with .NET Aspire.

**Acceptance Criteria**:
- [x] Service defaults integration
- [x] Health checks
- [x] Service discovery
- [x] Telemetry configuration

---

### BP-022: API Performance Testing
- **Status**: In Progress
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-013

**Description**: Performance testing for API endpoints.

**Acceptance Criteria**:
- [ ] Load testing with NBomber
- [ ] P95/P99 latency measurements
- [ ] Throughput benchmarks
- [ ] Performance baseline established

---

### BP-023: README Documentation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: BP-005

**Description**: Create comprehensive README for Blueprint Service.

**Acceptance Criteria**:
- [x] Service overview
- [x] API documentation
- [x] Configuration guide
- [x] Examples

---

## Notes

- Blueprint Engine is 100% complete and portable
- Database persistence is the primary remaining work for production readiness
- JSON-e template support is deferred to post-MVD
