# Implementation Plan: Schema Library

**Branch**: `034-schema-library` | **Date**: 2026-02-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/034-schema-library/spec.md`

## Summary

Build a centralised schema registry with server-side index/cache of formal data standards (schema.org, NIEM, HL7 FHIR, ISO 20022, IFC, W3C VC, UBL, SchemaStore.org). Extends the existing `IExternalSchemaProvider`/`ISchemaStore`/`SchemaEndpoints` infrastructure with: multiple provider implementations, a MongoDB-backed schema index with full-text search, organisation-level sector filtering, a schema picker component for blueprint building, and an admin health dashboard. All backend changes are within Blueprint Service.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: JsonSchema.Net 7.4.0, MongoDB.Driver, MudBlazor 8.15.0, FluentValidation
**Storage**: MongoDB (schema index collection, org preferences collection), in-memory cache for hot path
**Testing**: xUnit + FluentAssertions + Moq (unit), integration tests for providers
**Target Platform**: Linux containers (Docker), Blazor WASM (UI)
**Project Type**: Web application (microservice backend + Blazor WASM frontend)
**Performance Goals**: Schema search < 500ms, index refresh non-blocking, 500+ schemas indexed
**Constraints**: All backend changes within Blueprint Service, existing `IExternalSchemaProvider` interface, non-blocking cold start
**Scale/Scope**: 8 external providers, 500-5000 indexed schemas, per-org filtering, 3 new UI pages

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | All changes within Blueprint Service (schema domain is tightly coupled to blueprints) |
| II. Security First | PASS | Org-scoped visibility enforced, input validation on all endpoints, no secrets in external provider configs |
| III. API Documentation | PASS | All new endpoints documented with Scalar/OpenAPI, XML comments required |
| IV. Testing Requirements | PASS | Unit tests for providers, index service, sector filtering; integration tests for MongoDB |
| V. Code Quality | PASS | async/await for all I/O, DI throughout, nullable enabled |
| VI. Blueprint Creation Standards | PASS | Schemas inserted as JSON into `dataSchemas` — blueprint remains self-contained JSON |
| VII. Domain-Driven Design | PASS | Uses ubiquitous language: Schema (not "definition"), Sector (not "category" — avoids collision with existing `SchemaCategory`), Provider (not "source") |
| VIII. Observability by Default | PASS | Structured logging for refresh operations, admin health dashboard, metrics for provider status |

## Project Structure

### Documentation (this feature)

```text
specs/034-schema-library/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── schema-library-api.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
# Backend (Blueprint Service — all backend changes here)
src/Services/Sorcha.Blueprint.Service/
├── Endpoints/
│   └── SchemaEndpoints.cs              # EXTEND with index, sector, health endpoints
├── Services/
│   ├── SchemaIndexService.cs           # NEW — index management, search, refresh orchestration
│   ├── SchemaIndexRefreshService.cs    # NEW — IHostedService for periodic refresh
│   └── SectorFilterService.cs         # NEW — org sector preference management
└── Models/
    ├── SchemaIndexEntry.cs             # NEW — index entry model
    ├── SchemaProviderConfig.cs         # NEW — provider configuration
    ├── OrganisationSchemaPreferences.cs # NEW — per-org sector config
    └── SchemaSector.cs                 # NEW — sector definitions

# Common (Schemas library — shared models and interfaces)
src/Common/Sorcha.Blueprint.Schemas/
├── Models/
│   └── SchemaEntry.cs                  # EXTEND with SectorTags, Keywords, FieldCount, FieldNames
├── Services/
│   ├── IExternalSchemaProvider.cs       # EXISTING — no changes needed
│   ├── SchemaStoreOrgProvider.cs        # EXISTING — no changes needed
│   ├── SchemaOrgProvider.cs             # NEW — schema.org JSON-LD → JSON Schema
│   ├── FhirSchemaProvider.cs            # NEW — HL7 FHIR StructureDefinitions
│   ├── Iso20022Provider.cs              # NEW — ISO 20022 (static file-based)
│   ├── W3cVcProvider.cs                 # NEW — W3C Verifiable Credentials
│   ├── UblSchemaProvider.cs             # NEW — OASIS UBL JSON Schemas
│   └── StaticFileSchemaProvider.cs      # NEW — NIEM + IFC from bundled files
└── Repositories/
    ├── ISchemaIndexRepository.cs        # NEW — index persistence interface
    └── MongoSchemaIndexRepository.cs    # NEW — MongoDB index storage

# UI (Blazor WASM)
src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Pages/
│   ├── SchemaLibrary.razor             # NEW — browse/search page
│   └── SchemaDetail.razor              # NEW — detail view page
├── Components/
│   ├── SchemaSearchBar.razor           # NEW — search + filter controls
│   ├── SchemaCard.razor                # NEW — result card component
│   ├── SchemaPickerDialog.razor        # NEW — schema selection for blueprint builder
│   ├── SchemaFieldSubsetSelector.razor # NEW — field subset selection
│   ├── SchemaFormPreview.razor         # NEW — form preview from schema
│   └── SectorFilterChips.razor         # NEW — sector filter chips
├── Services/
│   └── ISchemaLibraryApiService.cs     # NEW — API client for schema index
└── ViewModels/
    └── SchemaLibraryViewModel.cs       # NEW — search state, pagination

# Admin UI (management section)
src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Pages/Admin/
│   ├── SchemaProviderHealth.razor      # NEW — provider health dashboard
│   └── SectorConfiguration.razor       # NEW — org sector preferences

# Static schema bundles
src/Common/Sorcha.Blueprint.Schemas/
└── StaticSchemas/                      # NEW — bundled NIEM/IFC schemas
    ├── niem/
    └── ifc/

# Tests
tests/Sorcha.Blueprint.Service.Tests/
├── SchemaIndexServiceTests.cs          # NEW
├── SchemaIndexRefreshServiceTests.cs   # NEW
└── SectorFilterServiceTests.cs         # NEW

tests/Sorcha.Blueprint.Schemas.Tests/
├── Providers/
│   ├── SchemaOrgProviderTests.cs       # NEW
│   ├── FhirSchemaProviderTests.cs      # NEW
│   ├── Iso20022ProviderTests.cs        # NEW
│   ├── W3cVcProviderTests.cs           # NEW
│   ├── UblSchemaProviderTests.cs       # NEW
│   └── StaticFileSchemaProviderTests.cs # NEW
└── MongoSchemaIndexRepositoryTests.cs  # NEW

tests/Sorcha.UI.Core.Tests/
└── SchemaLibrary/
    ├── SchemaLibraryTests.cs           # NEW
    └── SchemaPickerDialogTests.cs      # NEW
```

**Structure Decision**: Extends existing project structure — no new projects created. Backend changes in Blueprint Service + Common Schemas library. UI changes in UI.Core. This follows the existing pattern where schema infrastructure spans Common (models/interfaces) and Service (endpoints/business logic).

## Complexity Tracking

No constitution violations. All changes fit within existing project boundaries.

## Design Decisions

### D1: Provider Strategy

Each external source gets a dedicated `IExternalSchemaProvider` implementation. Sources without public JSON Schema APIs (ISO 20022, NIEM, IFC) use a `StaticFileSchemaProvider` that reads from bundled JSON Schema files checked into the repo under `StaticSchemas/`. These bundles are periodically updated manually.

**Conversion effort by source:**

| Source | Native Format | Target | Effort |
|--------|--------------|--------|--------|
| SchemaStore.org | JSON Schema draft-07 | 2020-12 | Low — keyword migration |
| schema.org | JSON-LD / RDFS | 2020-12 | High — vocabulary → type mapping |
| HL7 FHIR | JSON Schema draft-06 | 2020-12 | Low-Medium — keyword migration |
| ISO 20022 | XSD | 2020-12 | Static bundle (pre-converted) |
| W3C VC | JSON Schema 2020-12 | 2020-12 | None — already target format |
| OASIS UBL | JSON Schema | 2020-12 | Low-Medium — keyword update |
| NIEM | XSD / JSON Schema draft-04 | 2020-12 | Static bundle (pre-converted) |
| IFC | EXPRESS | 2020-12 | Static bundle (pre-converted) |

### D2: Index Storage

Schema index stored in a dedicated MongoDB collection (`schemaIndex`) alongside existing `schemas` collection. Uses MongoDB text index for full-text search across title, description, keywords. Compound indexes on sector tags + source provider for filtered queries.

### D3: Sector Taxonomy

Platform-curated sectors (not user-defined). Initial set:

| Sector ID | Display Name | Sources |
|-----------|-------------|---------|
| `finance` | Finance & Banking | ISO 20022, UBL, SchemaStore |
| `healthcare` | Healthcare | HL7 FHIR, schema.org |
| `construction` | Construction & Planning | IFC, NIEM (facilities subset) |
| `government` | Government & Public Sector | NIEM, W3C VC |
| `identity` | Identity & Credentials | W3C VC, schema.org |
| `commerce` | Commerce & Trade | UBL, schema.org, SchemaStore |
| `technology` | Technology & DevOps | SchemaStore |
| `general` | General Purpose | schema.org, SchemaStore |

### D4: Schema Snapshot on Selection

When a designer selects a schema for a blueprint action, the full JSON Schema content is copied into `dataSchemas`. The blueprint is self-contained. The index entry's source URI is stored in metadata for traceability but is not a runtime dependency.

### D5: Non-Blocking Cold Start

`SchemaIndexRefreshService` (IHostedService) starts on Blueprint Service boot. Each provider runs in its own Task with per-provider rate limiting and exponential backoff. The service accepts requests immediately — searches return whatever is indexed so far, with a loading indicator for incomplete providers.
