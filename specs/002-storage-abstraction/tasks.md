# Tasks: Multi-Tier Storage Abstraction Layer

**Input**: Design documents from `/specs/002-storage-abstraction/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Test tasks are included as this is a core infrastructure feature requiring comprehensive testing per Constitution IV.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md project structure:
- `src/Common/Sorcha.Storage.*` - Storage abstraction libraries
- `src/Core/Sorcha.Register.Core/Storage/` - Register verified cache
- `src/Services/Sorcha.*.Service/` - Service integrations
- `tests/Sorcha.Storage.*.Tests/` - Test projects

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and package structure

- [x] T001 Create Sorcha.Storage.Abstractions project in src/Common/Sorcha.Storage.Abstractions/Sorcha.Storage.Abstractions.csproj
- [x] T002 Create Sorcha.Storage.InMemory project in src/Common/Sorcha.Storage.InMemory/Sorcha.Storage.InMemory.csproj
- [x] T003 [P] Create Sorcha.Storage.Redis project in src/Common/Sorcha.Storage.Redis/Sorcha.Storage.Redis.csproj
- [x] T004 [P] Create Sorcha.Storage.EFCore project in src/Common/Sorcha.Storage.EFCore/Sorcha.Storage.EFCore.csproj
- [x] T005 [P] Create Sorcha.Storage.MongoDB project in src/Common/Sorcha.Storage.MongoDB/Sorcha.Storage.MongoDB.csproj
- [x] T006 [P] Create Sorcha.Storage.Abstractions.Tests project in tests/Sorcha.Storage.Abstractions.Tests/Sorcha.Storage.Abstractions.Tests.csproj
- [x] T007 [P] Create Sorcha.Storage.InMemory.Tests project in tests/Sorcha.Storage.InMemory.Tests/Sorcha.Storage.InMemory.Tests.csproj
- [x] T008 Add all new projects to Sorcha.sln solution file
- [x] T009 Configure package references: StackExchange.Redis, MongoDB.Driver, Npgsql.EntityFrameworkCore.PostgreSQL, Polly

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core interfaces and models that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Core Interfaces (from contracts/)

- [ ] T010 Copy ICacheStore interface from specs/002-storage-abstraction/contracts/ICacheStore.cs to src/Common/Sorcha.Storage.Abstractions/Interfaces/ICacheStore.cs
- [ ] T011 [P] Copy IRepository interface from specs/002-storage-abstraction/contracts/IRepository.cs to src/Common/Sorcha.Storage.Abstractions/Interfaces/IRepository.cs
- [ ] T012 [P] Copy IDocumentStore interface from specs/002-storage-abstraction/contracts/IDocumentStore.cs to src/Common/Sorcha.Storage.Abstractions/Interfaces/IDocumentStore.cs
- [ ] T013 [P] Copy IWormStore interface from specs/002-storage-abstraction/contracts/IWormStore.cs to src/Common/Sorcha.Storage.Abstractions/Interfaces/IWormStore.cs

### Configuration Models (from data-model.md)

- [ ] T014 [P] Create StorageConfiguration model in src/Common/Sorcha.Storage.Abstractions/Models/StorageConfiguration.cs
- [ ] T015 [P] Create HotTierConfiguration model in src/Common/Sorcha.Storage.Abstractions/Models/HotTierConfiguration.cs
- [ ] T016 [P] Create WarmTierConfiguration model in src/Common/Sorcha.Storage.Abstractions/Models/WarmTierConfiguration.cs
- [ ] T017 [P] Create ColdTierConfiguration model in src/Common/Sorcha.Storage.Abstractions/Models/ColdTierConfiguration.cs
- [ ] T018 [P] Create ObservabilityConfiguration model in src/Common/Sorcha.Storage.Abstractions/Models/ObservabilityConfiguration.cs
- [ ] T019 [P] Create StorageHealthStatus model in src/Common/Sorcha.Storage.Abstractions/Models/StorageHealthStatus.cs

### In-Memory Implementations (for dev/test)

- [ ] T020 Implement InMemoryCacheStore in src/Common/Sorcha.Storage.InMemory/InMemoryCacheStore.cs
- [ ] T021 [P] Implement InMemoryRepository in src/Common/Sorcha.Storage.InMemory/InMemoryRepository.cs
- [ ] T022 [P] Implement InMemoryDocumentStore in src/Common/Sorcha.Storage.InMemory/InMemoryDocumentStore.cs
- [ ] T023 [P] Implement InMemoryWormStore in src/Common/Sorcha.Storage.InMemory/InMemoryWormStore.cs

### In-Memory Tests

- [ ] T024 Write unit tests for InMemoryCacheStore in tests/Sorcha.Storage.InMemory.Tests/InMemoryCacheStoreTests.cs
- [ ] T025 [P] Write unit tests for InMemoryRepository in tests/Sorcha.Storage.InMemory.Tests/InMemoryRepositoryTests.cs
- [ ] T026 [P] Write unit tests for InMemoryDocumentStore in tests/Sorcha.Storage.InMemory.Tests/InMemoryDocumentStoreTests.cs
- [ ] T027 [P] Write unit tests for InMemoryWormStore in tests/Sorcha.Storage.InMemory.Tests/InMemoryWormStoreTests.cs

### Service Collection Extensions

- [ ] T028 Create ServiceCollectionExtensions for DI registration in src/Common/Sorcha.Storage.Abstractions/Extensions/ServiceCollectionExtensions.cs
- [ ] T029 Create InMemoryServiceExtensions for in-memory provider registration in src/Common/Sorcha.Storage.InMemory/Extensions/InMemoryServiceExtensions.cs

**Checkpoint**: Foundation ready - core interfaces, models, and in-memory implementations complete. User story implementation can now begin.

---

## Phase 3: User Story 1 - Service Developer Configures Storage (Priority: P1) 🎯 MVP

**Goal**: Enable services to configure storage providers through configuration files without code changes

**Independent Test**: Configure a service's appsettings.json with different provider settings and verify the service starts and persists data correctly

### Tests for User Story 1

- [ ] T030 [P] [US1] Write integration tests for storage configuration loading in tests/Sorcha.Storage.Abstractions.Tests/Configuration/StorageConfigurationTests.cs
- [ ] T031 [P] [US1] Write integration tests for provider factory in tests/Sorcha.Storage.Abstractions.Tests/Factory/StorageProviderFactoryTests.cs

### Implementation for User Story 1

- [ ] T032 [US1] Create IStorageProviderFactory interface in src/Common/Sorcha.Storage.Abstractions/Interfaces/IStorageProviderFactory.cs
- [ ] T033 [US1] Implement StorageProviderFactory in src/Common/Sorcha.Storage.Abstractions/Factory/StorageProviderFactory.cs
- [ ] T034 [P] [US1] Implement RedisCacheStore in src/Common/Sorcha.Storage.Redis/RedisCacheStore.cs
- [ ] T035 [P] [US1] Create RedisServiceExtensions in src/Common/Sorcha.Storage.Redis/Extensions/RedisServiceExtensions.cs
- [ ] T036 [P] [US1] Implement EFCoreRepository in src/Common/Sorcha.Storage.EFCore/EFCoreRepository.cs
- [ ] T037 [P] [US1] Create EFCoreServiceExtensions in src/Common/Sorcha.Storage.EFCore/Extensions/EFCoreServiceExtensions.cs
- [ ] T038 [P] [US1] Implement MongoDocumentStore in src/Common/Sorcha.Storage.MongoDB/MongoDocumentStore.cs
- [ ] T039 [P] [US1] Implement MongoWormStore in src/Common/Sorcha.Storage.MongoDB/MongoWormStore.cs
- [ ] T040 [P] [US1] Create MongoServiceExtensions in src/Common/Sorcha.Storage.MongoDB/Extensions/MongoServiceExtensions.cs
- [ ] T041 [US1] Implement circuit breaker policies with Polly in src/Common/Sorcha.Storage.Redis/Resilience/CacheResiliencePolicy.cs
- [ ] T042 [US1] Implement graceful degradation fallback in src/Common/Sorcha.Storage.Abstractions/Resilience/StorageFallbackPolicy.cs
- [ ] T043 [US1] Add configuration validation with fail-fast on invalid config in src/Common/Sorcha.Storage.Abstractions/Validation/StorageConfigurationValidator.cs

### Provider Integration Tests

- [ ] T044 [P] [US1] Create Sorcha.Storage.Redis.Tests project in tests/Sorcha.Storage.Redis.Tests/Sorcha.Storage.Redis.Tests.csproj
- [ ] T045 [P] [US1] Create Sorcha.Storage.EFCore.Tests project in tests/Sorcha.Storage.EFCore.Tests/Sorcha.Storage.EFCore.Tests.csproj
- [ ] T046 [P] [US1] Create Sorcha.Storage.MongoDB.Tests project in tests/Sorcha.Storage.MongoDB.Tests/Sorcha.Storage.MongoDB.Tests.csproj
- [ ] T047 [P] [US1] Write Redis integration tests with Testcontainers in tests/Sorcha.Storage.Redis.Tests/RedisCacheStoreIntegrationTests.cs
- [ ] T048 [P] [US1] Write EF Core integration tests with Testcontainers in tests/Sorcha.Storage.EFCore.Tests/EFCoreRepositoryIntegrationTests.cs
- [ ] T049 [P] [US1] Write MongoDB integration tests with Testcontainers in tests/Sorcha.Storage.MongoDB.Tests/MongoStoreIntegrationTests.cs

**Checkpoint**: User Story 1 complete. Services can now configure any supported storage provider through configuration alone.

---

## Phase 4: User Story 2 - Register Service Loads Verified Data (Priority: P1) 🎯 MVP

**Goal**: Register Service loads ledger data from cold storage, cryptographically verifies every docket/transaction, and serves only verified data

**Independent Test**: Seed cold storage with valid and invalid dockets, start service, verify only valid data is queryable while invalid is flagged

### Tests for User Story 2

- [ ] T050 [P] [US2] Write verification tests for valid docket verification in tests/Sorcha.Register.Core.Tests/Storage/DocketVerificationTests.cs
- [ ] T051 [P] [US2] Write verification tests for corrupted docket detection in tests/Sorcha.Register.Core.Tests/Storage/CorruptionDetectionTests.cs
- [ ] T052 [P] [US2] Write cache initialization tests in tests/Sorcha.Register.Core.Tests/Storage/CacheInitializationTests.cs

### Implementation for User Story 2

- [ ] T053 [US2] Copy IVerifiedRegisterCache interface from specs/002-storage-abstraction/contracts/IVerifiedRegisterCache.cs to src/Core/Sorcha.Register.Core/Storage/IVerifiedRegisterCache.cs
- [ ] T054 [P] [US2] Create VerifiedDocket model in src/Core/Sorcha.Register.Core/Storage/Models/VerifiedDocket.cs
- [ ] T055 [P] [US2] Create VerifiedTransaction model in src/Core/Sorcha.Register.Core/Storage/Models/VerifiedTransaction.cs
- [ ] T056 [P] [US2] Create RegisterOperationalState model in src/Core/Sorcha.Register.Core/Storage/Models/RegisterOperationalState.cs
- [ ] T057 [P] [US2] Create CorruptionRange model in src/Core/Sorcha.Register.Core/Storage/Models/CorruptionRange.cs
- [ ] T058 [P] [US2] Create CacheInitializationResult model in src/Core/Sorcha.Register.Core/Storage/Models/CacheInitializationResult.cs
- [ ] T059 [P] [US2] Create RegisterCacheConfiguration model in src/Core/Sorcha.Register.Core/Storage/Models/RegisterCacheConfiguration.cs
- [ ] T060 [US2] Implement DocketVerifier for hash chain validation in src/Core/Sorcha.Register.Core/Storage/Verification/DocketVerifier.cs
- [ ] T061 [US2] Implement TransactionVerifier for signature validation in src/Core/Sorcha.Register.Core/Storage/Verification/TransactionVerifier.cs
- [ ] T062 [US2] Implement VerifiedRegisterCache in src/Core/Sorcha.Register.Core/Storage/VerifiedRegisterCache.cs
- [ ] T063 [US2] Implement InitializeAsync with blocking/progressive strategy in src/Core/Sorcha.Register.Core/Storage/VerifiedRegisterCache.cs
- [ ] T064 [US2] Implement state management (RegisterState transitions) in src/Core/Sorcha.Register.Core/Storage/StateManager/RegisterStateManager.cs
- [ ] T065 [US2] Integrate IVerifiedRegisterCache with Register Service DI in src/Services/Sorcha.Register.Service/Extensions/RegisterStorageExtensions.cs

**Checkpoint**: User Story 2 complete. Register Service loads and verifies all ledger data on startup, serving only cryptographically verified data.

---

## Phase 5: User Story 3 - Register Service Recovers Corrupted Data (Priority: P2)

**Goal**: When corruption is detected, request replacement dockets from peer network, verify received data, and integrate into local store

**Independent Test**: Corrupt local storage, start service, mock peer responses, verify valid peer data replaces corrupted local data

### Tests for User Story 3

- [ ] T066 [P] [US3] Write recovery request tests in tests/Sorcha.Register.Core.Tests/Storage/Recovery/RecoveryRequestTests.cs
- [ ] T067 [P] [US3] Write peer data verification tests in tests/Sorcha.Register.Core.Tests/Storage/Recovery/PeerDataVerificationTests.cs
- [ ] T068 [P] [US3] Write recovery integration tests in tests/Sorcha.Register.Core.Tests/Storage/Recovery/RecoveryIntegrationTests.cs

### Implementation for User Story 3

- [ ] T069 [US3] Create IPeerRecoveryService interface in src/Core/Sorcha.Register.Core/Storage/Recovery/IPeerRecoveryService.cs
- [ ] T070 [US3] Create RecoveryResult model in src/Core/Sorcha.Register.Core/Storage/Models/RecoveryResult.cs
- [ ] T071 [US3] Implement CorruptionRangeTracker in src/Core/Sorcha.Register.Core/Storage/Recovery/CorruptionRangeTracker.cs
- [ ] T072 [US3] Implement PeerRecoveryService in src/Core/Sorcha.Register.Core/Storage/Recovery/PeerRecoveryService.cs
- [ ] T073 [US3] Implement ProcessRecoveredDataAsync in VerifiedRegisterCache in src/Core/Sorcha.Register.Core/Storage/VerifiedRegisterCache.cs
- [ ] T074 [US3] Implement MarkRangeRecoveredAsync in VerifiedRegisterCache in src/Core/Sorcha.Register.Core/Storage/VerifiedRegisterCache.cs
- [ ] T075 [US3] Add recovery retry logic with exponential backoff in src/Core/Sorcha.Register.Core/Storage/Recovery/RecoveryRetryPolicy.cs
- [ ] T076 [US3] Update RegisterStateManager for recovery state transitions in src/Core/Sorcha.Register.Core/Storage/StateManager/RegisterStateManager.cs

**Checkpoint**: User Story 3 complete. Register Service can detect corruption and recover from peer network.

---

## Phase 6: User Story 4 - Tenant Service Uses Warm Storage (Priority: P2)

**Goal**: Tenant Service persists organization configurations, user identities, and audit logs with ACID transaction support

**Independent Test**: Create, update, query organizations and users through Tenant Service API, verify data persists across service restarts

### Tests for User Story 4

- [ ] T077 [P] [US4] Write repository integration tests in tests/Sorcha.Tenant.Service.Tests/Storage/TenantRepositoryTests.cs
- [ ] T078 [P] [US4] Write multi-tenant isolation tests in tests/Sorcha.Tenant.Service.Tests/Storage/TenantIsolationTests.cs

### Implementation for User Story 4

- [ ] T079 [US4] Create ITenantRepository interface in src/Services/Sorcha.Tenant.Service/Storage/ITenantRepository.cs
- [ ] T080 [US4] Update TenantDbContext for storage abstraction integration in src/Services/Sorcha.Tenant.Service/Data/TenantDbContext.cs
- [ ] T081 [US4] Implement TenantRepository using IRepository in src/Services/Sorcha.Tenant.Service/Storage/TenantRepository.cs
- [ ] T082 [US4] Add storage configuration to Tenant Service appsettings.json in src/Services/Sorcha.Tenant.Service/appsettings.json
- [ ] T083 [US4] Register storage services in Tenant Service DI in src/Services/Sorcha.Tenant.Service/Program.cs

**Checkpoint**: User Story 4 complete. Tenant Service uses warm storage with full ACID transaction support.

---

## Phase 7: User Story 5 - Blueprint Service Uses Document Storage (Priority: P2)

**Goal**: Blueprint Service stores complex JSON blueprint definitions with flexible schema evolution

**Independent Test**: Save, version, and query blueprints through Blueprint Service API

### Tests for User Story 5

- [ ] T084 [P] [US5] Write document store integration tests in tests/Sorcha.Blueprint.Service.Tests/Storage/BlueprintDocumentStoreTests.cs
- [ ] T085 [P] [US5] Write schema evolution tests in tests/Sorcha.Blueprint.Service.Tests/Storage/SchemaEvolutionTests.cs

### Implementation for User Story 5

- [ ] T086 [US5] Create IBlueprintDocumentStore interface in src/Services/Sorcha.Blueprint.Service/Storage/IBlueprintDocumentStore.cs
- [ ] T087 [US5] Implement BlueprintDocumentStore using IDocumentStore in src/Services/Sorcha.Blueprint.Service/Storage/BlueprintDocumentStore.cs
- [ ] T088 [US5] Add storage configuration to Blueprint Service appsettings.json in src/Services/Sorcha.Blueprint.Service/appsettings.json
- [ ] T089 [US5] Register storage services in Blueprint Service DI in src/Services/Sorcha.Blueprint.Service/Program.cs
- [ ] T090 [US5] Update Blueprint Service to use document storage for blueprint persistence in src/Services/Sorcha.Blueprint.Service/Services/BlueprintService.cs

**Checkpoint**: User Story 5 complete. Blueprint Service stores complex JSON blueprints in document storage.

---

## Phase 8: User Story 6 - Wallet Service Caches Hot Data (Priority: P3)

**Goal**: Wallet Service caches frequently accessed wallet metadata for fast retrieval with warm storage fallback

**Independent Test**: Measure response times for wallet queries with and without cache hits

### Tests for User Story 6

- [ ] T091 [P] [US6] Write cache hit/miss tests in tests/Sorcha.Wallet.Service.Tests/Storage/WalletCacheTests.cs
- [ ] T092 [P] [US6] Write graceful degradation tests in tests/Sorcha.Wallet.Service.Tests/Storage/CacheFallbackTests.cs

### Implementation for User Story 6

- [ ] T093 [US6] Create IWalletCacheService interface in src/Services/Sorcha.Wallet.Service/Storage/IWalletCacheService.cs
- [ ] T094 [US6] Implement WalletCacheService using ICacheStore in src/Services/Sorcha.Wallet.Service/Storage/WalletCacheService.cs
- [ ] T095 [US6] Add cache-aside pattern for wallet queries in src/Services/Sorcha.Wallet.Service/Services/WalletService.cs
- [ ] T096 [US6] Add storage configuration to Wallet Service appsettings.json in src/Services/Sorcha.Wallet.Service/appsettings.json
- [ ] T097 [US6] Register storage services in Wallet Service DI in src/Services/Sorcha.Wallet.Service/Program.cs
- [ ] T098 [US6] Implement cache invalidation on wallet updates in src/Services/Sorcha.Wallet.Service/Services/WalletService.cs

**Checkpoint**: User Story 6 complete. Wallet Service uses hot cache with graceful degradation to warm storage.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Observability, documentation, and improvements affecting multiple user stories

### Observability (FR-060-064)

- [ ] T099 [P] Add OpenTelemetry metrics instrumentation in src/Common/Sorcha.Storage.Abstractions/Observability/StorageMetrics.cs
- [ ] T100 [P] Add OpenTelemetry tracing spans in src/Common/Sorcha.Storage.Abstractions/Observability/StorageActivitySource.cs
- [ ] T101 [P] Implement per-tier observability configuration in src/Common/Sorcha.Storage.Abstractions/Observability/ObservabilityConfigurer.cs
- [ ] T102 Add Redis instrumentation via OpenTelemetry.Instrumentation.StackExchangeRedis in src/Common/Sorcha.Storage.Redis/Observability/RedisInstrumentation.cs
- [ ] T103 [P] Add EF Core instrumentation in src/Common/Sorcha.Storage.EFCore/Observability/EFCoreInstrumentation.cs
- [ ] T104 [P] Add MongoDB custom instrumentation in src/Common/Sorcha.Storage.MongoDB/Observability/MongoInstrumentation.cs

### Health Checks

- [ ] T105 Implement ICacheStore health check in src/Common/Sorcha.Storage.Redis/Health/RedisCacheHealthCheck.cs
- [ ] T106 [P] Implement IRepository health check in src/Common/Sorcha.Storage.EFCore/Health/EFCoreHealthCheck.cs
- [ ] T107 [P] Implement IDocumentStore/IWormStore health check in src/Common/Sorcha.Storage.MongoDB/Health/MongoHealthCheck.cs
- [ ] T108 Create StorageHealthCheckExtensions for ASP.NET Core health checks in src/Common/Sorcha.Storage.Abstractions/Health/StorageHealthCheckExtensions.cs

### Documentation

- [ ] T109 [P] Update docs/architecture.md with storage abstraction architecture
- [ ] T110 [P] Create docs/storage-abstraction.md with developer guide
- [ ] T111 Update docs/API-DOCUMENTATION.md with storage configuration APIs
- [ ] T112 Run quickstart.md validation - verify all examples work

### Final Validation

- [ ] T113 Run full test suite and verify >85% coverage
- [ ] T114 Update MASTER-TASKS.md with completed storage abstraction tasks
- [ ] T115 Update development-status.md with storage abstraction completion

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phases 3-8)**: All depend on Foundational phase completion
  - US1 (P1) and US2 (P1) are both MVP critical and can proceed in parallel
  - US3 (P2) depends on US2 (needs verified cache for recovery)
  - US4 (P2), US5 (P2), US6 (P3) can proceed in parallel after Phase 2
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

```
Phase 2 (Foundational)
         │
         ├─────────────────┬─────────────────┬──────────────────┬─────────────────┐
         │                 │                 │                  │                 │
         ▼                 ▼                 ▼                  ▼                 ▼
   US1 (P1)           US2 (P1)          US4 (P2)           US5 (P2)          US6 (P3)
   Configure          Verified          Tenant             Blueprint         Wallet
   Storage            Cache             Storage            Storage           Cache
         │                 │
         │                 ▼
         │            US3 (P2)
         │            Recovery
         │                 │
         ▼                 ▼
         └────────► Phase 9 (Polish)
```

### Within Each User Story

- Tests (when included) should be written to understand expected behavior
- Models before services
- Services before integrations
- Core implementation before service integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T003-T007)
- All Foundational interface copy tasks marked [P] can run in parallel (T011-T013)
- All configuration models marked [P] can run in parallel (T014-T019)
- All in-memory implementations marked [P] can run in parallel (T021-T023)
- Provider implementations (Redis, EFCore, MongoDB) can run in parallel (T034-T040)
- US1, US4, US5, US6 can all run in parallel after Phase 2
- US2 and US3 should be sequential (recovery depends on verified cache)

---

## Parallel Example: Phase 2 Models

```bash
# Launch all configuration models in parallel:
Task: T014 "Create StorageConfiguration model"
Task: T015 "Create HotTierConfiguration model"
Task: T016 "Create WarmTierConfiguration model"
Task: T017 "Create ColdTierConfiguration model"
Task: T018 "Create ObservabilityConfiguration model"
Task: T019 "Create StorageHealthStatus model"
```

## Parallel Example: User Story 1 Providers

```bash
# Launch all provider implementations in parallel:
Task: T034 "Implement RedisCacheStore"
Task: T036 "Implement EFCoreRepository"
Task: T038 "Implement MongoDocumentStore"
Task: T039 "Implement MongoWormStore"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Configure Storage)
4. Complete Phase 4: User Story 2 (Verified Cache)
5. **STOP and VALIDATE**: Test both stories independently
6. Deploy/demo if ready - this is the core functionality

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 + 2 → Test independently → **Deploy/Demo (MVP!)**
3. Add User Story 3 (Recovery) → Test → Deploy
4. Add User Stories 4, 5, 6 (Service integrations) → Test → Deploy
5. Add Phase 9 (Observability, Docs) → Final release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Provider implementations)
   - Developer B: User Story 2 (Verified cache)
   - Developer C: User Story 4 (Tenant) or User Story 5 (Blueprint)
3. Developer B continues to User Story 3 (Recovery) after US2
4. Stories complete and integrate independently

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 115 |
| **Setup Tasks** | 9 |
| **Foundational Tasks** | 20 |
| **US1 Tasks** | 20 |
| **US2 Tasks** | 16 |
| **US3 Tasks** | 11 |
| **US4 Tasks** | 7 |
| **US5 Tasks** | 7 |
| **US6 Tasks** | 8 |
| **Polish Tasks** | 17 |
| **Parallel Opportunities** | 60+ tasks marked [P] |
| **MVP Scope** | US1 + US2 (36 tasks after setup/foundation) |

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- US1 and US2 are both P1 (MVP critical) - prioritize equally
- US3 depends on US2 - cannot start recovery without verified cache
