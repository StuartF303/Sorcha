# Quickstart: Schema Library

**Feature**: 034-schema-library
**Branch**: `034-schema-library`

## Implementation Order

The feature builds in 10 phases. Each phase is independently testable.

### Phase 1: Data Model & Index Infrastructure
**Goal**: Schema index MongoDB collection, repository, and core models

1. Create `SchemaIndexEntry` model with all fields
2. Create `SchemaIndexStatus` enum (Active, Deprecated, Unavailable)
3. Create `ISchemaIndexRepository` interface
4. Implement `MongoSchemaIndexRepository` with indexes
5. Create `SchemaProviderStatus` model
6. Create `SchemaSector` static definitions (8 sectors)
7. Unit tests for models and repository

**Validates**: Data model is correct, MongoDB indexes work, CRUD operations pass

### Phase 2: JSON Schema Normalisation Utility
**Goal**: Shared utility to convert schemas from various drafts to 2020-12

1. Create `JsonSchemaNormaliser` class
2. Handle draft-04 → 2020-12 (`id` → `$id`, `definitions` → `$defs`)
3. Handle draft-06 → 2020-12 (exclusiveMin/Max boolean → number)
4. Handle draft-07 → 2020-12 (`definitions` → `$defs`)
5. Extract metadata: field count, field names, required fields, keywords
6. Comprehensive unit tests for each draft conversion

**Validates**: Any JSON Schema draft can be normalised to 2020-12

### Phase 3: External Schema Providers (Live API)
**Goal**: Implement providers that fetch from APIs at runtime

1. `SchemaOrgProvider` — fetch JSON-LD vocabulary, transform to JSON Schema types
2. `FhirSchemaProvider` — download FHIR bundle, split into per-resource schemas
3. `W3cVcProvider` — fetch VC/VP schemas from GitHub
4. Unit tests with mocked HTTP responses for each provider

**Validates**: Each provider returns normalised schemas with correct metadata

### Phase 4: External Schema Providers (Static/Bundle)
**Goal**: Implement providers for sources without live APIs

1. `StaticFileSchemaProvider` — reads from `StaticSchemas/` directory
2. `UblSchemaProvider` — downloads UBL ZIP, extracts JSON schemas
3. `Iso20022Provider` — reads from static bundle (pre-converted)
4. Bundle curated NIEM and IFC schemas in `StaticSchemas/`
5. Unit tests for each provider

**Validates**: Static and bundle providers return schemas correctly

### Phase 5: Schema Index Service & Refresh
**Goal**: Background index population and periodic refresh

1. Create `SchemaIndexService` — orchestrates index CRUD, search, and refresh
2. Create `SchemaIndexRefreshService` (IHostedService) — periodic refresh with configurable interval
3. Per-provider rate limiting with exponential backoff
4. Non-blocking startup — service accepts requests while providers are loading
5. Provider health tracking (status, last fetch, error details)
6. Register all providers in DI (multiple `IExternalSchemaProvider` registrations)
7. Unit tests for index service, integration tests for refresh lifecycle

**Validates**: Index populates on startup, refreshes periodically, handles provider failures

### Phase 6: Organisation Sector Filtering
**Goal**: Per-org sector visibility preferences

1. Create `OrganisationSchemaPreferences` model
2. Create `SectorFilterService` — manages preferences, filters search results
3. Store preferences in MongoDB collection
4. Integrate filtering into `SchemaIndexService.SearchAsync()`
5. Unit tests for filtering logic

**Validates**: Org admins can configure sectors, designers see filtered results

### Phase 7: API Endpoints
**Goal**: REST API for index search, sectors, provider health

1. Add schema index search endpoint (`GET /api/v1/schemas/index`)
2. Add index entry detail endpoint (`GET /api/v1/schemas/index/{provider}/{uri}`)
3. Add schema content endpoint (`GET /api/v1/schemas/index/{provider}/{uri}/content`)
4. Add sector list endpoint (`GET /api/v1/schemas/sectors`)
5. Add sector preferences endpoints (`GET/PUT /api/v1/schemas/sectors/preferences`)
6. Add provider health endpoint (`GET /api/v1/schemas/providers`)
7. Add manual refresh trigger (`POST /api/v1/schemas/providers/{name}/refresh`)
8. Add derived schema endpoint (`POST /api/v1/schemas/derived`)
9. OpenAPI documentation for all endpoints
10. Integration tests for endpoint responses

**Validates**: All API contracts work, auth enforced, responses match OpenAPI spec

### Phase 8: UI — Schema Library Page
**Goal**: Browse and search schema library in UI

1. Create `ISchemaLibraryApiService` — HTTP client for index API
2. Create `SchemaLibraryViewModel` — search state, filters, pagination
3. Create `SchemaSearchBar.razor` — search input + filter controls
4. Create `SectorFilterChips.razor` — sector toggle chips
5. Create `SchemaCard.razor` — result card with title, description, sector badge, source badge, field count
6. Create `SchemaLibrary.razor` page — assembles search bar, filters, results grid
7. Create `SchemaDetail.razor` page — full detail view with field list, raw JSON
8. Add navigation link to schema library
9. Unit tests for components

**Validates**: Designers can browse, search, and view schema details

### Phase 9: UI — Schema Picker & Blueprint Integration
**Goal**: Schema selection dialog for blueprint builder

1. Create `SchemaPickerDialog.razor` — MudDialog with embedded library search
2. Create `SchemaFieldSubsetSelector.razor` — field selection checkboxes
3. Create `SchemaFormPreview.razor` — form preview using FormSchemaService
4. Integrate picker into blueprint builder action editor
5. Schema snapshot on selection → insert into `dataSchemas`
6. Unit tests for picker and integration

**Validates**: Designers can select schemas for blueprint actions, preview forms

### Phase 10: UI — Admin Health Dashboard & Sector Config
**Goal**: Admin management pages

1. Create `SchemaProviderHealth.razor` — provider status cards with health indicators
2. Create `SectorConfiguration.razor` — org sector preference toggles
3. Manual refresh trigger button per provider
4. Add navigation links in admin section
5. Unit tests for admin components

**Validates**: Admins can monitor providers, configure sectors, trigger refreshes

## Key Risks

| Risk | Mitigation |
|------|------------|
| schema.org JSON-LD → JSON Schema conversion is complex | Start with curated subset (~50 types), iterate |
| FHIR schema is monolithic (~150 resources in one file) | Split on `definitions` keys, process in chunks |
| External APIs may be slow or unreliable | Non-blocking cold start, retain cache, exponential backoff |
| Large index (5000+ schemas) may slow search | MongoDB text index, cursor pagination, sector filtering |
| Static bundles become outdated | Document bundle update process, log bundle age |

## Test Coverage Targets

| Area | Target | Framework |
|------|--------|-----------|
| Provider unit tests | >90% | xUnit + Moq (mocked HTTP) |
| Index service tests | >85% | xUnit + FluentAssertions |
| Sector filtering tests | >90% | xUnit |
| API endpoint tests | >85% | Integration tests |
| UI component tests | >80% | bUnit / manual |
| Schema normaliser | >95% | xUnit (all draft conversions) |
