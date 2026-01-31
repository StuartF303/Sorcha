# Phase 3: Register Service (MVD)

**Goal:** Build simplified Register Service for transaction storage and retrieval
**Duration:** Weeks 10-12
**Total Tasks:** 15
**Completion:** 100% (Core, API, and comprehensive testing complete)

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)

---

## Phase 1-2: Core Implementation ✅ COMPLETE

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

---

## API Integration Tasks ✅ COMPLETE

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| REG-INT-1 | Refactor API to use core managers | P2 | 12h | ✅ Complete | - |
| REG-CODE-DUP | Resolve DocketManager/ChainValidator duplication | P1 | 4h | ✅ Complete | 2025-12-09 |
| REG-003 | MongoDB transaction repository | P1 | 12h | ✅ Complete | 2026-01-31 |
| REG-005 | Implement POST /api/registers/{id}/transactions | P0 | 8h | ✅ Complete | - |
| REG-006 | Implement GET /api/registers/{id}/transactions/{txId} | P0 | 6h | ✅ Complete | - |
| REG-007 | Implement GET /api/registers/{id}/transactions | P0 | 8h | ✅ Complete | - |
| REG-008 | Implement Query API endpoints | P0 | 12h | ✅ Complete | - |
| REG-009 | .NET Aspire integration | P0 | 8h | ✅ Complete | - |
| REG-010 | Unit tests for core logic | P0 | 16h | ✅ Complete | - |
| REG-011 | Integration tests | P0 | 16h | ✅ Complete | - |
| REG-012 | SignalR hub integration tests | P0 | 8h | ✅ Complete | - |
| REG-013 | OData V4 support | P1 | 8h | ✅ Complete | - |

**API Integration Status:** ✅ **COMPLETE** (12/13 tasks, 1 deferred to post-MVD)
**Achievement:** API fully integrated with comprehensive testing (112 tests, ~2,459 LOC)
**MongoDB Persistence:** ✅ **COMPLETE** (REG-003) - Per-register database architecture enabled
**Recommended Next:** End-to-end integration with Blueprint and Wallet services

### REG-003 Completion Details (2026-01-31)

MongoDB persistence implemented with **per-register database architecture**:

**Architecture:**
- Registry database (`sorcha_register_registry`) stores register metadata
- Each register gets its own database (`sorcha_register_{registerId}`)
- Collections per register: `transactions`, `dockets`
- Automatic index creation on register creation

**Implementation:**
- Enhanced `MongoRegisterStorageConfiguration` with `UseDatabasePerRegister` flag
- Refactored `MongoRegisterRepository` to support both architectures
  - Per-register mode (production): Isolated databases per register
  - Single-database mode (legacy): All data in one database for testing
- Updated Docker Compose and appsettings configurations
- Enabled MongoDB storage in Register Service

**Benefits:**
- Complete data isolation between registers
- Scalable across MongoDB shards
- Database-level access control
- Optimized indexes per register
- Clean deletion (drop database = remove register)

---

## Week 12: Integration & Testing ✅ COMPLETE

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

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)
