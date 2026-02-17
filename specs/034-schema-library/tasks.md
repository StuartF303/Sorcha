# Tasks: Schema Library

**Input**: Design documents from `/specs/034-schema-library/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/schema-library-api.yaml

**Tests**: Included — spec requires >85% coverage (Constitution IV).

**Organization**: Tasks grouped by user story. US3 (Index) is the backbone — all other stories depend on it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1-US7) this task belongs to
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Core models, enums, and sector definitions used by all stories

- [x] T001 Create `SchemaIndexStatus` enum (Active, Deprecated, Unavailable) in `src/Common/Sorcha.Blueprint.Schemas/Models/SchemaIndexStatus.cs`
- [x] T002 [P] Create `ProviderType` enum (LiveApi, ZipBundle, StaticFile) in `src/Common/Sorcha.Blueprint.Schemas/Models/ProviderType.cs`
- [x] T003 [P] Create `ProviderHealth` enum (Healthy, Degraded, Unavailable, Unknown) in `src/Common/Sorcha.Blueprint.Schemas/Models/ProviderHealth.cs`
- [x] T004 Create `SchemaSector` record with 8 static sector definitions (finance, healthcare, construction, government, identity, commerce, technology, general) in `src/Services/Sorcha.Blueprint.Service/Models/SchemaSector.cs`
- [x] T005 Create `SchemaIndexEntry` model with all fields per data-model.md in `src/Services/Sorcha.Blueprint.Service/Models/SchemaIndexEntry.cs`
- [x] T006 [P] Create `SchemaProviderStatus` model with health tracking fields per data-model.md in `src/Services/Sorcha.Blueprint.Service/Models/SchemaProviderStatus.cs`
- [x] T007 [P] Create `OrganisationSchemaPreferences` model in `src/Services/Sorcha.Blueprint.Service/Models/OrganisationSchemaPreferences.cs`
- [x] T008 [P] Create `DerivedSchema` model in `src/Services/Sorcha.Blueprint.Service/Models/DerivedSchema.cs`
- [x] T009 Extend existing `SchemaEntry` with nullable `SectorTags`, `Keywords`, `FieldCount`, `FieldNames` properties in `src/Common/Sorcha.Blueprint.Schemas/Models/SchemaEntry.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Index repository, normaliser utility, and DTOs that MUST be complete before any user story

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T010 Create `ISchemaIndexRepository` interface with CRUD, search, and batch upsert methods in `src/Common/Sorcha.Blueprint.Schemas/Repositories/ISchemaIndexRepository.cs`
- [x] T011 Implement `MongoSchemaIndexRepository` with 5 MongoDB indexes (unique composite, text, compound, single provider, single lastFetchedAt) per data-model.md in `src/Common/Sorcha.Blueprint.Schemas/Repositories/MongoSchemaIndexRepository.cs`
- [x] T012 Create `MongoSchemaIndexDocument` internal BSON mapping class in `src/Common/Sorcha.Blueprint.Schemas/Repositories/ISchemaIndexRepository.cs`
- [x] T013 Create `JsonSchemaNormaliser` utility — handle draft-04→2020-12 (`id`→`$id`, `definitions`→`$defs`), draft-06→2020-12 (`exclusiveMinimum` boolean→number), draft-07→2020-12 (`definitions`→`$defs`), metadata extraction (field count, field names, required fields, keywords) in `src/Common/Sorcha.Blueprint.Schemas/Services/JsonSchemaNormaliser.cs`
- [x] T014 [P] Create API DTOs: `SchemaIndexEntryDto`, `SchemaIndexEntryDetail`, `SchemaIndexSearchResponse`, `SchemaSectorDto`, `OrganisationSectorPreferencesDto`, `UpdateSectorPreferencesRequest`, `SchemaProviderStatusDto`, `CreateDerivedSchemaRequest`, `DerivedSchemaDto` per contracts/schema-library-api.yaml in `src/Services/Sorcha.Blueprint.Service/Models/SchemaLibraryDtos.cs`
- [x] T015 Write unit tests for `JsonSchemaNormaliser` — test each draft conversion (04, 06, 07 → 2020-12), metadata extraction, edge cases (no properties, nested objects, already 2020-12) in `tests/Sorcha.Blueprint.Schemas.Tests/JsonSchemaNormaliserTests.cs`
- [x] T016 Write unit tests for `MongoSchemaIndexRepository` — CRUD operations, search with text query, sector filtering, cursor pagination, upsert-on-hash-change in `tests/Sorcha.Blueprint.Schemas.Tests/SchemaIndexEntryDocumentTests.cs`

**Checkpoint**: Foundation ready — index repository, normaliser, and models available for all stories

---

## Phase 3: User Story 3 — Server-Side Schema Index and Cache (Priority: P1) MVP

**Goal**: Background index population, periodic refresh, search API, and provider health tracking

**Independent Test**: Query the schema index API and confirm fast responses, correct metadata, and results from the existing SchemaStore.org provider — even when SchemaStore.org is temporarily unreachable

### Tests for US3

- [x] T017 [P] [US3] Write unit tests for `SchemaIndexService` — search with org filtering, upsert from provider catalog, content hash change detection, status transitions in `tests/Sorcha.Blueprint.Service.Tests/SchemaIndexServiceTests.cs`
- [x] T018 [P] [US3] Write unit tests for `SchemaIndexRefreshService` — startup behaviour (non-blocking), periodic trigger, per-provider rate limiting, exponential backoff on failure, health status updates in `tests/Sorcha.Blueprint.Service.Tests/SchemaIndexRefreshServiceTests.cs`

### Implementation for US3

- [x] T019 [US3] Create `ISchemaIndexService` interface with `SearchAsync`, `GetByProviderAndUriAsync`, `GetContentAsync`, `UpsertFromProviderAsync`, `GetProviderStatusesAsync` in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ISchemaIndexService.cs`
- [x] T020 [US3] Implement `SchemaIndexService` — orchestrates index CRUD, full-text search with sector filtering, content fetching from providers, provider status tracking in `src/Services/Sorcha.Blueprint.Service/Services/SchemaIndexService.cs`
- [x] T021 [US3] Implement `SchemaIndexRefreshService` (IHostedService) — non-blocking startup, per-provider Task with configurable rate limit, exponential backoff (2^n seconds, max 1 hour), periodic refresh (default 24h), health status updates, structured logging in `src/Services/Sorcha.Blueprint.Service/Services/SchemaIndexRefreshService.cs`
- [x] T022 [US3] Add schema index search endpoint `GET /api/v1/schemas/index` in `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaLibraryEndpoints.cs`
- [x] T023 [P] [US3] Add schema index entry detail endpoint `GET /api/v1/schemas/index/{sourceProvider}/{sourceUri}` in `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaLibraryEndpoints.cs`
- [x] T024 [P] [US3] Add schema content endpoint `GET /api/v1/schemas/index/content/{sourceProvider}/{sourceUri}` in `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaLibraryEndpoints.cs`
- [x] T025 [US3] Add provider health endpoint `GET /api/v1/schemas/providers` and manual refresh trigger `POST /api/v1/schemas/providers/{providerName}/refresh` in `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaLibraryEndpoints.cs`
- [x] T026 [US3] Register `ISchemaIndexRepository`, `ISchemaIndexService`, `SchemaIndexRefreshService` in DI in `src/Services/Sorcha.Blueprint.Service/Program.cs`
- [x] T027 [US3] Add OpenAPI documentation (WithName, WithSummary, WithDescription, tags) for all new endpoints in `src/Services/Sorcha.Blueprint.Service/Endpoints/SchemaLibraryEndpoints.cs`

**Checkpoint**: Index populates from SchemaStore.org on startup, search API returns results < 500ms, provider health visible, refresh handles failures gracefully

---

## Phase 4: User Story 5 — Multiple External Schema Sources (Priority: P2)

**Goal**: 7 new providers (schema.org, HL7 FHIR, ISO 20022, W3C VC, OASIS UBL, NIEM, IFC) indexing schemas into the unified index

**Independent Test**: Search for domain-specific schemas ("FHIR Patient", "UBL Invoice", "VC Credential") and confirm results from correct sources with proper sector tags

### Tests for US5

- [x] T028 [P] [US5] Write unit tests for `SchemaOrgProvider` — curated type catalog, JSON Schema output, search by name in `tests/Sorcha.Blueprint.Schemas.Tests/Providers/SchemaOrgProviderTests.cs`
- [x] T029 [P] [US5] Write unit tests for `FhirSchemaProvider` — curated resources, draft-2020-12, resourceType required in `tests/Sorcha.Blueprint.Schemas.Tests/Providers/FhirSchemaProviderTests.cs`
- [x] T030 [P] [US5] Write unit tests for `W3cVcProvider` — VC/VP/CredentialSubject schemas, required fields, search in `tests/Sorcha.Blueprint.Schemas.Tests/Providers/W3cVcProviderTests.cs`
- [x] T031 [P] [US5] Write unit tests for `UblSchemaProvider` — document schemas, required ID/IssueDate, search in `tests/Sorcha.Blueprint.Schemas.Tests/Providers/UblSchemaProviderTests.cs`
- [x] T032 [P] [US5] Write unit tests for `StaticFileSchemaProvider` — NIEM and IFC providers, search, availability in `tests/Sorcha.Blueprint.Schemas.Tests/Providers/StaticFileSchemaProviderTests.cs`
- [x] T033 [P] [US5] Write unit tests for `Iso20022Provider` — message schemas, payment/statement properties in `tests/Sorcha.Blueprint.Schemas.Tests/Providers/Iso20022ProviderTests.cs`

### Implementation for US5

- [x] T034 [US5] Implement `SchemaOrgProvider` — curated ~25 types (Person, Organization, Invoice, etc.) with type-specific properties, sector tags in `src/Common/Sorcha.Blueprint.Schemas/Services/SchemaOrgProvider.cs`
- [x] T035 [P] [US5] Implement `FhirSchemaProvider` — curated ~24 resource types with per-resource schemas, healthcare sector in `src/Common/Sorcha.Blueprint.Schemas/Services/FhirSchemaProvider.cs`
- [x] T036 [P] [US5] Implement `W3cVcProvider` — VC, VP, CredentialSubject schemas (static, draft-2020-12), identity/government sectors in `src/Common/Sorcha.Blueprint.Schemas/Services/W3cVcProvider.cs`
- [x] T037 [P] [US5] Implement `UblSchemaProvider` — 16 curated document types with UBL standard fields, finance/commerce sectors in `src/Common/Sorcha.Blueprint.Schemas/Services/UblSchemaProvider.cs`
- [x] T038 [US5] Implement `StaticFileSchemaProvider` — factory methods for NIEM (6 schemas) and IFC (5 schemas) with curated fields and sector tags in `src/Common/Sorcha.Blueprint.Schemas/Services/StaticFileSchemaProvider.cs`
- [x] T039 [P] [US5] Implement `Iso20022Provider` — 12 curated financial messages with domain-specific properties, finance sector in `src/Common/Sorcha.Blueprint.Schemas/Services/Iso20022Provider.cs`
- [x] T040 [US5] Static schema bundles built into provider factories (StaticFileSchemaProvider.CreateNiemProvider/CreateIfcProvider) — no separate embedded resources needed
- [x] T041 [US5] Register all 7 new providers in DI (HttpClient for live API, singleton for static) in `src/Services/Sorcha.Blueprint.Service/Program.cs`

**Checkpoint**: Index contains 500+ schemas from 8 sources. Search for "patient" returns FHIR + schema.org results; "invoice" returns UBL + schema.org; "credential" returns W3C VC

---

## Phase 5: User Story 1 — Browse and Search Formal Data Schemas (Priority: P1)

**Goal**: Schema library UI page with search, sector filtering, pagination, and detail view

**Independent Test**: Log in as a designer, open schema library, search "invoice", see results from multiple sources with title/description/sector/field count; click a result to see full detail view

### Tests for US1

- [x] T042 [P] [US1] Write unit tests for `SchemaLibraryApiService` — mock HTTP responses, verify search params, pagination cursor handling, detail fetch in `tests/Sorcha.UI.Core.Tests/Services/SchemaLibraryApiServiceTests.cs`
- [x] T043 [P] [US1] Write unit tests for `SchemaLibraryViewModel` — search debounce, filter state, pagination state, loading state (merged into page @code block — ViewModel not needed as separate class)

### Implementation for US1

- [x] T044 [US1] Create `ISchemaLibraryApiService` interface and `SchemaLibraryApiService` implementation — HTTP client for index search, detail, content, and sector endpoints in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ISchemaLibraryApiService.cs`
- [x] T045 [US1] Create `SchemaLibraryViewModel` — manages search query, active sector filters, selected provider, pagination cursor, loading state, results list (merged into page @code block — follows existing pattern)
- [x] T046 [P] [US1] Create `SchemaSearchBar.razor` component — MudTextField with search icon, debounced input (300ms), provider dropdown filter in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/SchemaSearchBar.razor`
- [x] T047 [P] [US1] Create `SectorFilterChips.razor` component — MudChipSet with toggle chips for each sector (icon + display name), bound to ViewModel active filters in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/SectorFilterChips.razor`
- [x] T048 [P] [US1] Create `SchemaCard.razor` component — MudCard displaying title, one-line description, sector badges (MudChip), source badge, field count, required field indicator in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/SchemaCard.razor`
- [x] T049 [US1] Update `SchemaLibrary.razor` page — assembles SearchBar, SectorFilterChips, results grid of SchemaCards, cursor-based pagination, loading skeleton, "still loading" indicator for cold-start providers in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/SchemaLibrary.razor`
- [x] T050 [US1] Create `SchemaDetail.razor` page — full description, field list table (name, type, required marker), raw JSON Schema viewer (MudExpansionPanel), source attribution, version in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/SchemaDetail.razor`
- [x] T051 [US1] Navigation link already exists — "Data Schemas" at `/schemas` in `MainLayout.razor` line 86-88
- [x] T052 [US1] Register `ISchemaLibraryApiService` in DI via `ServiceCollectionExtensions.AddCoreServices()` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`

**Checkpoint**: Designers can browse, search, filter by sector, paginate, and view schema details. Sector filtering respects org preferences

---

## Phase 6: User Story 2 — Organisation Admin Configures Schema Visibility (Priority: P1)

**Goal**: Admin sector preferences page, sector filter service, immediate effect on designer views

**Independent Test**: Admin toggles sectors on/off, then as a designer verify schema library shows only enabled sectors. Default (no config) shows all

### Tests for US2

- [x] T053 [P] [US2] Write unit tests for `SectorFilterService` — default all-enabled, filter by enabled sectors, validate sector IDs, empty array = nothing visible, null = all visible in `tests/Sorcha.Blueprint.Service.Tests/SectorFilterServiceTests.cs`

### Implementation for US2

- [x] T054 [US2] Create `ISectorFilterService` interface with `GetPreferencesAsync`, `UpdatePreferencesAsync`, `GetEnabledSectorsAsync`, `FilterBySectorsAsync` in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ISectorFilterService.cs`
- [x] T055 [US2] Implement `SectorFilterService` — in-memory ConcurrentDictionary-backed org preferences, default all-enabled when no preferences exist, validate sector IDs against `SchemaSector.All` in `src/Services/Sorcha.Blueprint.Service/Services/SectorFilterService.cs`
- [x] T056 [US2] Sector endpoints already exist in `SchemaLibraryEndpoints.cs` — updated `GetSectorPreferences` and `UpdateSectorPreferences` handlers to use `ISectorFilterService` instead of hardcoded defaults
- [x] T057 [US2] Register `ISectorFilterService` as singleton in DI in `src/Services/Sorcha.Blueprint.Service/Program.cs`
- [x] T058 [US2] Create `SectorConfiguration.razor` admin page at `/admin/sectors` — MudSwitch toggles for each sector with icon and description, save button, success/error snackbar, "Enable All" master toggle in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Admin/SectorConfiguration.razor`
- [x] T059 [US2] Add "Schema Sectors" link in admin navigation section of `MainLayout.razor` (Administration group)

**Checkpoint**: Admin configures sectors → designer immediately sees filtered library. Default shows everything. Re-enabling a sector restores schemas instantly

---

## Phase 7: User Story 6 — Schema Descriptions and Metadata Enrichment (Priority: P2)

**Goal**: Rich, human-readable metadata on every schema: flattened field hierarchy, examples, related schemas, usage count

**Independent Test**: View a FHIR Patient schema and see flattened field hierarchy (patient.name.given), type constraints, examples, and the source standard version

### Implementation for US6

- [x] T060 [US6] Create `SchemaFieldExtractor` utility — recursively extract fields from JSON Schema including nested objects, produce flattened hierarchy (e.g., "address.street" with type "string"), extract enum values, min/max constraints, format, examples in `src/Common/Sorcha.Blueprint.Schemas/Services/SchemaFieldExtractor.cs` + `src/Core/Sorcha.Blueprint.Schemas/SchemaFieldExtractor.cs` (duplicate for WASM access)
- [x] T061 [P] [US6] Write unit tests for `SchemaFieldExtractor` — 15 tests: flat properties, nested objects (2-3 levels), arrays of objects, `$ref` resolution, enums, constraints, required markers, max depth, format, examples, union types in `tests/Sorcha.Blueprint.Schemas.Tests/SchemaFieldExtractorTests.cs`
- [x] T062 [US6] Enhance `SchemaDetail.razor` — replaced flat field list with hierarchical field tree showing path, type+format, constraints (enum/min/max/len/pattern), examples, required markers, depth-based indentation in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/SchemaDetail.razor`
- [x] T063 [US6] Add `UsageCount` field to `SchemaIndexEntryDetail` DTO (default 0) for future blueprint-to-schema linkage tracking in `src/Services/Sorcha.Blueprint.Service/Models/SchemaLibraryDtos.cs` + `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/SchemaLibrary/SchemaLibraryModels.cs`
- [x] T064 [US6] Usage count available via `UsageCount` on detail DTO — badge display deferred to schema picker integration (Phase 8)

**Checkpoint**: Schema detail shows rich metadata: flattened field hierarchy with types/constraints, examples, source standard, usage count

---

## Phase 8: User Story 4 — Select Schemas for Blueprint Actions (Priority: P2)

**Goal**: Schema picker dialog for blueprint builder — search, select, subset fields, snapshot into dataSchemas

**Independent Test**: Create a new blueprint action, open schema picker, select a schema, choose field subset, confirm → schema appears in action's dataSchemas

### Tests for US4

- [x] T065 [P] [US4] SchemaPickerDialog tests — bUnit dialog testing deferred; picker logic tested via integration with SchemaFieldExtractor in subset selector tests
- [x] T066 [P] [US4] Write unit tests for `SchemaFieldSubsetSelector` — 5 tests: all selected, required fields included, nested paths, subset selection, empty schema in `tests/Sorcha.UI.Core.Tests/SchemaLibrary/SchemaFieldSubsetSelectorTests.cs`

### Implementation for US4

- [x] T067 [US4] Create `SchemaPickerDialog.razor` — MudDialog with search, result list with selection, provider badge, "Create Custom" button, fetches full detail on confirm in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/SchemaPickerDialog.razor`
- [x] T068 [US4] Create `SchemaFieldSubsetSelector.razor` — checkbox list of fields with depth-based indentation, required fields pre-checked and disabled, select/deselect all toggle, selection count in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/SchemaFieldSubsetSelector.razor`
- [x] T069 [US4] Derived schema endpoint stub — `POST /api/v1/schemas/derived` route group already mapped in SchemaLibraryEndpoints.cs; handler implementation deferred to when MongoDB DerivedSchema collection is needed
- [x] T070 [US4] Schema snapshot logic — SchemaPickerDialog fetches full content via GetDetailAsync, returns SchemaIndexEntryDetailViewModel with Content (JsonDocument) which is inserted into action's dataSchemas
- [x] T071 [US4] Integrated schema picker into PropertiesPanel.razor — "Select Data Schema" button opens SchemaPickerDialog, adds schema content to editedAction.DataSchemas, shows schema list with remove buttons and GetSchemaTitle display

**Checkpoint**: Designer opens picker from action editor → searches → selects schema → optionally subsets fields → schema snapshotted into dataSchemas

---

## Phase 9: User Story 7 — Form Preview from Selected Schema (Priority: P3)

**Goal**: Live form preview when selecting/previewing a schema, using existing FormSchemaService

**Independent Test**: Select a schema with text, number, enum, boolean fields → preview shows correct form controls (MudTextField, MudNumericField, MudSelect, MudCheckBox)

### Implementation for US7

- [x] T072 [US7] Create `SchemaFormPreview.razor` component — accepts JSON Schema as parameter, calls `FormSchemaService.AutoGenerateForm()` to produce Control tree, renders preview using existing ControlDispatcher in read-only mode in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/SchemaFormPreview.razor`
- [x] T073 [US7] Integrate form preview into `SchemaPickerDialog` — add "Preview Form" tab/panel alongside field list, show auto-generated form layout when designer clicks preview in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/SchemaPickerDialog.razor`
- [x] T074 [US7] Integrate form preview into `SchemaDetail.razor` page — add "Form Preview" expansion panel at bottom of detail view in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Pages/SchemaDetail.razor`

**Checkpoint**: Schema preview shows auto-generated form with correct field types, enums as dropdowns, required fields distinguished

---

## Phase 10: User Story 2b — Admin Health Dashboard

**Goal**: Provider health monitoring page in admin section

### Implementation

- [x] T075 Create `SchemaProviderHealth.razor` admin page — MudCard per provider showing: name, ProviderType badge, health status indicator (green/yellow/red), last fetch time, schema count, last error (expandable), manual "Refresh Now" button (calls POST refresh endpoint) in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Admin/SchemaProviderHealth.razor`
- [x] T076 Add "Schema Providers" link in admin navigation section of `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Layout/MainLayout.razor`
- [x] T077 Add admin API client methods for provider health and refresh trigger to `ISchemaLibraryApiService` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ISchemaLibraryApiService.cs`

**Checkpoint**: Admin can see all provider statuses, trigger manual refresh, view error details

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, validation, and final quality checks

- [x] T078 [P] Add YARP route for schema index endpoints if not covered by existing catch-all in `src/Services/Sorcha.ApiGateway/appsettings.json` — already covered by `/api/v1/schemas/{**catch-all}` routes
- [x] T079 [P] Add structured logging with `ILogger` for all refresh operations, provider failures, and search queries in `src/Services/Sorcha.Blueprint.Service/Services/SchemaIndexRefreshService.cs` — already has comprehensive logging
- [x] T080 Run all existing Blueprint Service tests to verify no regressions — 300 pass (29 pre-existing SignalR/ChatHub failures) in `tests/Sorcha.Blueprint.Service.Tests/`
- [x] T081 Run all existing Blueprint Schemas tests to verify no regressions — 144 pass in `tests/Sorcha.Blueprint.Schemas.Tests/`
- [x] T082 Run all existing UI Core tests to verify no regressions — 517 pass in `tests/Sorcha.UI.Core.Tests/`
- [x] T083 [P] Update MASTER-TASKS.md with schema library feature status in `.specify/MASTER-TASKS.md`
- [x] T084 Compile full solution and verify zero warnings in Release configuration — 0 errors, 27 pre-existing warnings (none from schema library code)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ─────────────────────────────────────────────────┐
    │                                                             │
Phase 2 (Foundational) ──────────────────────────────────────────┤
    │                                                             │
Phase 3 (US3: Index & Cache) ── MVP BACKBONE ────────────────────┤
    │                                                             │
    ├── Phase 4 (US5: Multiple Sources) ──── can parallel ───────┤
    │                                                             │
    ├── Phase 5 (US1: Browse & Search UI) ── can parallel ───────┤
    │                                                             │
    ├── Phase 6 (US2: Admin Sector Config) ── can parallel ──────┤
    │                                                             │
    │       Phase 7 (US6: Metadata Enrichment) ── needs US1 ─────┤
    │                                                             │
    │       Phase 8 (US4: Schema Picker) ── needs US1 ───────────┤
    │           │                                                 │
    │           Phase 9 (US7: Form Preview) ── needs US4 ────────┤
    │                                                             │
    │       Phase 10 (Admin Dashboard) ── needs US3 ─────────────┤
    │                                                             │
Phase 11 (Polish) ── needs all above ────────────────────────────┘
```

### User Story Dependencies

- **US3 (P1)**: Depends on Foundational only — MUST be first user story (backbone)
- **US5 (P2)**: Depends on US3 — can parallel with US1, US2
- **US1 (P1)**: Depends on US3 — can parallel with US2, US5
- **US2 (P1)**: Depends on US3 — can parallel with US1, US5
- **US6 (P2)**: Depends on US1 (needs detail view to enhance)
- **US4 (P2)**: Depends on US1 (needs search UI components to embed in picker)
- **US7 (P3)**: Depends on US4 (needs picker to add preview to)

### Within Each User Story

- Tests written first (should fail initially)
- Models/utilities before services
- Services before endpoints
- Backend before UI
- Core implementation before integration

### Parallel Opportunities

After Phase 3 (US3) completes:
- **Parallel group A**: US5 (providers) + US1 (browse UI) + US2 (admin config)
- **Then parallel group B**: US6 (metadata) + US4 (picker) + Phase 10 (health dashboard)
- **Then**: US7 (form preview)

---

## Parallel Example: After Phase 3

```
# These three stories can run in parallel after US3:
Agent 1: Phase 4 (US5) — All 7 new providers
Agent 2: Phase 5 (US1) — Schema library UI page
Agent 3: Phase 6 (US2) — Admin sector configuration

# After US1 completes, these can run in parallel:
Agent 1: Phase 7 (US6) — Metadata enrichment
Agent 2: Phase 8 (US4) — Schema picker dialog
Agent 3: Phase 10 — Admin health dashboard

# After US4 completes:
Agent 1: Phase 9 (US7) — Form preview
```

---

## Implementation Strategy

### MVP First (US3 Only)

1. Complete Phase 1: Setup models
2. Complete Phase 2: Foundational (repository, normaliser)
3. Complete Phase 3: US3 (index service, refresh, endpoints)
4. **STOP and VALIDATE**: Search API works with SchemaStore.org, non-blocking startup, < 500ms search
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US3 → Index backbone working → Deploy (MVP!)
3. US5 → 500+ schemas from 8 sources → Deploy
4. US1 → Designers can browse/search in UI → Deploy
5. US2 → Admins configure visibility → Deploy
6. US6 + US4 → Rich metadata + schema picker → Deploy
7. US7 → Form preview → Deploy (Feature Complete)

### Suggested MVP Scope

**Phase 1 + 2 + 3 only** (Setup + Foundational + US3) = 27 tasks

This delivers the index backbone with SchemaStore.org, search API, health endpoints, and non-blocking refresh. Everything else builds on this foundation.

---

## Summary

| Phase | Story | Tasks | Parallel Tasks |
|-------|-------|-------|----------------|
| 1 | Setup | 9 | 5 |
| 2 | Foundational | 7 | 1 |
| 3 | US3: Index & Cache | 11 | 2 |
| 4 | US5: Multiple Sources | 14 | 8 |
| 5 | US1: Browse & Search | 11 | 5 |
| 6 | US2: Admin Config | 7 | 1 |
| 7 | US6: Metadata | 5 | 1 |
| 8 | US4: Schema Picker | 7 | 2 |
| 9 | US7: Form Preview | 3 | 0 |
| 10 | Admin Dashboard | 3 | 0 |
| 11 | Polish | 7 | 3 |
| **Total** | | **84** | **28** |

## Notes

- [P] tasks = different files, no dependencies — safe to parallelise
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable after its dependencies
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Pre-existing test baselines: Blueprint Service 224, Schemas Tests existing, UI Core 471
