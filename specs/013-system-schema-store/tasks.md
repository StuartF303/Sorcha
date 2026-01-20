# Tasks: System Schema Store

**Input**: Design documents from `/specs/013-system-schema-store/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/schema-api.yaml, research.md

**Tests**: Included per constitution requirement (>85% coverage target)

**Organization**: Tasks grouped by user story for independent implementation and testing

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create Sorcha.Blueprint.Schemas project and configure dependencies

- [X] T001 Create Sorcha.Blueprint.Schemas project at `src/Common/Sorcha.Blueprint.Schemas/Sorcha.Blueprint.Schemas.csproj`
- [X] T002 Add project reference to solution file `Sorcha.sln`
- [X] T003 [P] Configure project dependencies (JsonSchema.Net, MongoDB.Driver) in `src/Common/Sorcha.Blueprint.Schemas/Sorcha.Blueprint.Schemas.csproj`
- [X] T004 [P] Create test project at `tests/Sorcha.Blueprint.Schemas.Tests/Sorcha.Blueprint.Schemas.Tests.csproj`
- [X] T005 Add test project reference to solution file

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core models and interfaces that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 [P] Create SchemaCategory enum at `src/Common/Sorcha.Blueprint.Schemas/Models/SchemaCategory.cs`
- [X] T007 [P] Create SchemaStatus enum at `src/Common/Sorcha.Blueprint.Schemas/Models/SchemaStatus.cs`
- [X] T008 [P] Create SourceType enum at `src/Common/Sorcha.Blueprint.Schemas/Models/SourceType.cs`
- [X] T009 [P] Create SchemaSource value object at `src/Common/Sorcha.Blueprint.Schemas/Models/SchemaSource.cs`
- [X] T010 Create SchemaEntry entity at `src/Common/Sorcha.Blueprint.Schemas/Models/SchemaEntry.cs`
- [X] T011 [P] Create SchemaEntryDto record at `src/Common/Sorcha.Blueprint.Schemas/DTOs/SchemaEntryDto.cs`
- [X] T012 [P] Create SchemaContentDto record at `src/Common/Sorcha.Blueprint.Schemas/DTOs/SchemaContentDto.cs`
- [X] T013 [P] Create SchemaSourceDto record at `src/Common/Sorcha.Blueprint.Schemas/DTOs/SchemaSourceDto.cs`
- [X] T014 [P] Create SchemaListResponse record at `src/Common/Sorcha.Blueprint.Schemas/DTOs/SchemaListResponse.cs`
- [X] T015 [P] Create CreateSchemaRequest record at `src/Common/Sorcha.Blueprint.Schemas/DTOs/CreateSchemaRequest.cs`
- [X] T016 [P] Create UpdateSchemaRequest record at `src/Common/Sorcha.Blueprint.Schemas/DTOs/UpdateSchemaRequest.cs`
- [X] T017 Create ISchemaStore interface at `src/Common/Sorcha.Blueprint.Schemas/Services/ISchemaStore.cs`
- [X] T018 Create SchemaEntryMapper for entity-DTO conversions at `src/Common/Sorcha.Blueprint.Schemas/Mappers/SchemaEntryMapper.cs`
- [X] T019 Add Sorcha.Blueprint.Schemas reference to Blueprint.Service at `src/Services/Sorcha.Blueprint.Service/Sorcha.Blueprint.Service.csproj`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Access Core System Schemas (Priority: P1) MVP

**Goal**: Blueprint designers can access the four pre-defined system schemas (Installation, Organisation, Participant, Register)

**Independent Test**: Request GET /api/v1/schemas/system and verify all four schemas returned with valid JSON schema content

### Tests for User Story 1

- [X] T020 [P] [US1] Unit test for SystemSchemaLoader at `tests/Sorcha.Blueprint.Schemas.Tests/SystemSchemaLoaderTests.cs`
- [X] T021 [P] [US1] Unit test for SchemaStore GetSystemSchemas at `tests/Sorcha.Blueprint.Schemas.Tests/SchemaStoreTests.cs`
- [X] T022 [P] [US1] Integration test for GET /schemas/system at `tests/Sorcha.Blueprint.Service.IntegrationTests/SchemaEndpointsTests.cs`

### Implementation for User Story 1

- [X] T023 [P] [US1] Create installation.schema.json at `src/Common/Sorcha.Blueprint.Schemas/SystemSchemas/installation.schema.json`
- [X] T024 [P] [US1] Create organisation.schema.json at `src/Common/Sorcha.Blueprint.Schemas/SystemSchemas/organisation.schema.json`
- [X] T025 [P] [US1] Create participant.schema.json at `src/Common/Sorcha.Blueprint.Schemas/SystemSchemas/participant.schema.json`
- [X] T026 [P] [US1] Create register.schema.json at `src/Common/Sorcha.Blueprint.Schemas/SystemSchemas/register.schema.json`
- [X] T027 [US1] Configure embedded resources in csproj for SystemSchemas/*.json at `src/Common/Sorcha.Blueprint.Schemas/Sorcha.Blueprint.Schemas.csproj`
- [X] T028 [US1] Create SystemSchemaLoader to load embedded JSON at `src/Common/Sorcha.Blueprint.Schemas/Services/SystemSchemaLoader.cs`
- [X] T029 [US1] Implement SchemaStore with system schema support at `src/Common/Sorcha.Blueprint.Schemas/Services/SchemaStore.cs`
- [X] T030 [US1] Create SchemaEndpoints with GET /schemas/system at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T031 [US1] Add GET /schemas/{identifier} endpoint to SchemaEndpoints at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T032 [US1] Register SchemaStore and endpoints in DI at `src/Services/Sorcha.Blueprint.Service/Program.cs`
- [X] T033 [US1] Add ETag header support for cache validation in GET /schemas/{identifier}

**Checkpoint**: User Story 1 complete - system schemas accessible via API

---

## Phase 4: User Story 2 - Client-Side Schema Caching (Priority: P2)

**Goal**: WASM client caches schemas locally for performance and offline access

**Independent Test**: Load schemas in browser, disconnect server, verify cached schemas still accessible with offline indicator

**Depends on**: US1 (needs schemas to cache)

### Tests for User Story 2

- [X] T034 [P] [US2] Unit test for SchemaCache - Uses existing LocalStorageSchemaCacheService tests
- [X] T035 [P] [US2] Integration test for offline caching behavior (manual test plan in quickstart.md)

### Implementation for User Story 2

**Note**: Integrated with existing `SchemaLibraryService` architecture in `src/Core/Sorcha.Blueprint.Schemas/`

- [X] T036 [P] [US2] Create ISchemaCache interface - Uses existing `ISchemaCacheService` at `src/Core/Sorcha.Blueprint.Schemas/ISchemaCacheService.cs`
- [X] T037 [P] [US2] Create SchemaCacheEntry model - Uses existing `SchemaCacheEntry` at `src/Core/Sorcha.Blueprint.Schemas/SchemaCacheEntry.cs`
- [X] T038 [US2] Implement SchemaCache with localStorage - Uses existing `LocalStorageSchemaCacheService` at `src/Core/Sorcha.Blueprint.Schemas/LocalStorageSchemaCacheService.cs`
- [X] T039 [US2] Add version-based cache invalidation logic - Implemented ETag caching in `BlueprintServiceRepository`
- [X] T040 [US2] Add offline detection - Implemented fallback to cache on HttpRequestException in `BlueprintServiceRepository`
- [X] T041 [US2] Register SchemaCache in DI at `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Program.cs`
- [X] T042 [US2] Add ISchemaApiService interface - Implemented via `ISchemaRepository` at `src/Core/Sorcha.Blueprint.Schemas/ISchemaRepository.cs`
- [X] T043 [US2] Implement SchemaApiService - Created `BlueprintServiceRepository` at `src/Core/Sorcha.Blueprint.Schemas/BlueprintServiceRepository.cs`

**Checkpoint**: User Story 2 complete - schemas cached locally with offline support

---

## Phase 5: User Story 3 - Browse and Search External Schemas (Priority: P3)

**Goal**: Blueprint designers can search SchemaStore.org and import external schemas

**Independent Test**: Search for "package.json", verify results from SchemaStore.org, import schema

**Depends on**: US1 (core infrastructure)

### Tests for User Story 3

- [X] T044 [P] [US3] Unit test for ExternalSchemaProvider at `tests/Sorcha.Blueprint.Schemas.Tests/ExternalSchemaProviderTests.cs`
- [X] T045 [P] [US3] Integration test for GET /schemas/external/search at `tests/Sorcha.Blueprint.Service.IntegrationTests/SchemaEndpointsTests.cs`
- [X] T046 [P] [US3] Integration test for POST /schemas/external/import at `tests/Sorcha.Blueprint.Service.IntegrationTests/SchemaEndpointsTests.cs`

### Implementation for User Story 3

- [X] T047 [P] [US3] Create IExternalSchemaProvider interface at `src/Common/Sorcha.Blueprint.Schemas/Services/IExternalSchemaProvider.cs`
- [X] T048 [P] [US3] Create ExternalSchemaResult DTO at `src/Common/Sorcha.Blueprint.Schemas/DTOs/ExternalSchemaResult.cs`
- [X] T049 [P] [US3] Create ExternalSchemaSearchResponse DTO at `src/Common/Sorcha.Blueprint.Schemas/DTOs/ExternalSchemaSearchResponse.cs`
- [X] T050 [P] [US3] Create ImportExternalSchemaRequest DTO at `src/Common/Sorcha.Blueprint.Schemas/DTOs/ImportExternalSchemaRequest.cs`
- [X] T051 [US3] Implement SchemaStoreOrgProvider for SchemaStore.org API at `src/Common/Sorcha.Blueprint.Schemas/Services/SchemaStoreOrgProvider.cs`
- [X] T052 [US3] Add catalog caching with TTL to SchemaStoreOrgProvider
- [X] T053 [US3] Add GET /schemas/external/search endpoint at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T054 [US3] Add POST /schemas/external/import endpoint at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T055 [US3] Implement schema validation before import using JsonSchema.Net
- [X] T056 [US3] Add graceful fallback for external source unavailability
- [X] T057 [US3] Register ExternalSchemaProvider in DI at `src/Services/Sorcha.Blueprint.Service/Program.cs`

**Checkpoint**: User Story 3 complete - external schema search and import working

---

## Phase 6: User Story 4 - Manage Schema Store Categories (Priority: P4)

**Goal**: Administrators can manage custom schemas with category organization and lifecycle states

**Independent Test**: Create custom schema, filter by category, deprecate schema, verify filtering works

**Depends on**: US1 (core infrastructure)

### Tests for User Story 4

- [X] T058 [P] [US4] Unit test for custom schema CRUD at `tests/Sorcha.Blueprint.Schemas.Tests/SchemaStoreTests.cs`
- [X] T059 [P] [US4] Unit test for category filtering at `tests/Sorcha.Blueprint.Schemas.Tests/SchemaStoreTests.cs`
- [X] T060 [P] [US4] Integration test for POST /schemas at `tests/Sorcha.Blueprint.Service.IntegrationTests/SchemaEndpointsTests.cs`
- [X] T061 [P] [US4] Integration test for deprecate/activate endpoints at `tests/Sorcha.Blueprint.Service.IntegrationTests/SchemaEndpointsTests.cs`

### Implementation for User Story 4

- [X] T062 [US4] Add MongoDB repository for schema persistence at `src/Common/Sorcha.Blueprint.Schemas/Repositories/MongoSchemaRepository.cs`
- [X] T063 [US4] Configure MongoDB indexes for schemas collection
- [X] T064 [US4] Implement GET /schemas with category/status/search filtering at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T065 [US4] Implement POST /schemas for custom schema creation at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T066 [US4] Implement PUT /schemas/{identifier} for updates at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T067 [US4] Implement DELETE /schemas/{identifier} at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T068 [US4] Implement POST /schemas/{identifier}/deprecate at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T069 [US4] Implement POST /schemas/{identifier}/activate at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T070 [US4] Implement POST /schemas/{identifier}/publish for global publishing at `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaEndpoints.cs`
- [X] T071 [US4] Add organization scoping for custom schemas (extract from JWT claims)
- [X] T072 [US4] Add authorization policy for administrator-only write operations
- [X] T073 [US4] Add conflict detection for global schema publish

**Checkpoint**: User Story 4 complete - full schema management with categories and lifecycle

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Observability, documentation, and quality improvements

- [X] T074 [P] Add OpenTelemetry tracing for schema operations at `src/Common/Sorcha.Blueprint.Schemas/Observability/SchemaActivitySource.cs`
- [X] T075 [P] Add health check for schema store at `src/Services/Sorcha.Blueprint.Service/HealthChecks/SchemaStoreHealthCheck.cs`
- [X] T076 [P] Add XML documentation to all public APIs
- [X] T077 [P] Update Scalar API docs configuration at `src/Services/Sorcha.Blueprint.Service/Program.cs`
- [X] T078 Validate quickstart.md scenarios work end-to-end
- [X] T079 Code cleanup and consistency review
- [X] T080 Run full test suite and verify >85% coverage

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational - can start after Phase 2
- **User Story 2 (Phase 4)**: Depends on US1 (needs schemas to cache)
- **User Story 3 (Phase 5)**: Depends on Foundational - can run parallel with US1/US2
- **User Story 4 (Phase 6)**: Depends on Foundational - can run parallel with US1/US2/US3
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 1: Setup
    ↓
Phase 2: Foundational (BLOCKS ALL)
    ↓
    ├── US1 (P1): Core System Schemas ← MVP
    │       ↓
    │   US2 (P2): Client Caching (depends on US1)
    │
    ├── US3 (P3): External Schemas (parallel with US1/US2)
    │
    └── US4 (P4): Category Management (parallel with US1/US2/US3)
            ↓
        Phase 7: Polish
```

### Parallel Opportunities

**Within Phase 2 (Foundational)**:
- T006, T007, T008, T009 (all enums/value objects) - parallel
- T011-T016 (all DTOs) - parallel

**Within Phase 3 (US1)**:
- T020, T021, T022 (tests) - parallel
- T023, T024, T025, T026 (schema JSON files) - parallel

**Within Phase 5 (US3)**:
- T044, T045, T046 (tests) - parallel
- T047, T048, T049, T050 (DTOs/interfaces) - parallel

**Across Phases (after Foundational)**:
- US1 and US3 can run in parallel
- US1 and US4 can run in parallel
- US3 and US4 can run in parallel

---

## Parallel Example: User Story 1 Implementation

```bash
# After Foundational phase completes, launch tests in parallel:
Task T020: "Unit test for SystemSchemaLoader"
Task T021: "Unit test for SchemaStore GetSystemSchemas"
Task T022: "Integration test for GET /schemas/system"

# Then launch all schema JSON files in parallel:
Task T023: "Create installation.schema.json"
Task T024: "Create organisation.schema.json"
Task T025: "Create participant.schema.json"
Task T026: "Create register.schema.json"

# Then sequential implementation (dependencies between tasks):
Task T027 → T028 → T029 → T030 → T031 → T032 → T033
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T005)
2. Complete Phase 2: Foundational (T006-T019)
3. Complete Phase 3: User Story 1 (T020-T033)
4. **STOP and VALIDATE**: Test system schemas accessible via API
5. Deploy/demo if ready - core value delivered

**MVP Scope**: 33 tasks, delivers core system schemas

### Incremental Delivery

1. **Setup + Foundational** → Project structure ready
2. **Add US1** → System schemas accessible (MVP!)
3. **Add US2** → Client caching with offline support
4. **Add US3** → External schema discovery
5. **Add US4** → Full schema management
6. **Polish** → Production ready

### Parallel Team Strategy

With 2+ developers after Foundational:
- Developer A: US1 → US2 (caching depends on US1)
- Developer B: US3 (external) or US4 (management)

---

## Summary

| Phase | User Story | Tasks | Parallel Tasks |
|-------|------------|-------|----------------|
| 1 | Setup | 5 | 2 |
| 2 | Foundational | 14 | 11 |
| 3 | US1 - Core Schemas (P1) | 14 | 7 |
| 4 | US2 - Client Caching (P2) | 10 | 3 |
| 5 | US3 - External Schemas (P3) | 14 | 8 |
| 6 | US4 - Category Management (P4) | 16 | 4 |
| 7 | Polish | 7 | 5 |
| **Total** | | **80** | **40** |

**Independent Test Criteria per Story**:
- US1: GET /api/v1/schemas/system returns 4 valid JSON schemas
- US2: Schemas accessible from cache after server disconnect
- US3: Search "package.json" returns SchemaStore.org results
- US4: Create/filter/deprecate custom schemas works

**Suggested MVP**: Complete through Phase 3 (US1) = 33 tasks
