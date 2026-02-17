# Research: Schema Library

**Feature**: 034-schema-library
**Date**: 2026-02-17

## R1: Existing Schema Infrastructure

### Decision: Extend existing interfaces, don't replace them

**Rationale**: The current infrastructure (`ISchemaStore`, `ISchemaRepository`, `IExternalSchemaProvider`, `SchemaEndpoints`, `MongoSchemaRepository`) is well-designed and production-ready. The `IExternalSchemaProvider` interface already supports `GetCatalogAsync()`, `SearchAsync()`, `GetSchemaAsync()`, and `IsAvailableAsync()` — exactly what new providers need.

**Alternatives Considered**:
- Replace with new schema registry service → Rejected (unnecessary duplication, breaks existing consumers)
- Create separate index service outside Blueprint Service → Rejected (user constraint: all backend in Blueprint Service)

### Key Existing Components

| Component | Location | Purpose | Changes Needed |
|-----------|----------|---------|----------------|
| `IExternalSchemaProvider` | `Common/Sorcha.Blueprint.Schemas/Services/` | Provider interface | None — already sufficient |
| `SchemaStoreOrgProvider` | Same directory | SchemaStore.org fetcher | None — continue as-is |
| `ISchemaStore` | Same directory | Primary schema API | Extend with sector filtering params |
| `ISchemaRepository` | `Repositories/` | Persistence layer | Add index repository alongside |
| `SchemaEntry` | `Models/` | Schema model | Add SectorTags, Keywords, FieldCount, FieldNames |
| `SchemaEndpoints` | `Blueprint.Service/Endpoints/` | REST API | Add index, sector, health endpoints |
| `MongoSchemaRepository` | `Repositories/` | MongoDB persistence | No changes — add separate index repo |
| `SystemSchemaLoader` | `Services/` | 4 embedded schemas | No changes |
| `FormSchemaService` | `UI.Core/Services/Forms/` | Form auto-generation | No changes — already handles any JSON Schema |

### Existing Enums & Models

- `SchemaCategory`: System, External, Custom — keep as-is
- `SchemaStatus`: Active, Deprecated — extend with `Unavailable` for index entries
- `SchemaSource`: Type (Internal/External/Custom), Uri, Provider, FetchedAt — sufficient for new providers
- `SchemaMetadata` (Core layer): Has `Category` (string) and `Tags` (List<string>) — aligns with sector concept but is freeform

## R2: External Source Access Patterns

### SchemaStore.org (Existing)
- **API**: `GET https://www.schemastore.org/api/json/catalog.json`
- **Format**: JSON Schema draft-07
- **Conversion**: Low — `definitions` → `$defs`, update `$schema`
- **Rate Limits**: None published, CDN-hosted
- **Auth**: None
- **Schema Count**: ~800+ schemas

### schema.org
- **API**: `GET https://schema.org/version/latest/schemaorg-current-https.jsonld`
- **Format**: JSON-LD vocabulary (NOT JSON Schema)
- **Conversion**: High — must transform RDF types to JSON Schema objects, map `rangeIncludes` to JSON types, handle `subClassOf` as `allOf`
- **Rate Limits**: None, static CDN files
- **Auth**: None
- **Schema Count**: ~800 types, curate to ~50 most useful (Person, Organization, Invoice, Event, Product, etc.)

### HL7 FHIR
- **API**: `GET https://www.hl7.org/fhir/fhir.schema.json.zip` (complete bundle)
- **Format**: JSON Schema draft-06 (single file with all resources as `definitions`)
- **Conversion**: Low-Medium — split monolithic file into per-resource schemas, update draft
- **Rate Limits**: None for static downloads
- **Auth**: None
- **Schema Count**: ~150 resource types (Patient, Practitioner, Observation, etc.)

### ISO 20022
- **API**: No public REST API — XSD downloads only
- **Format**: XSD
- **Conversion**: Static bundle — pre-convert selected message schemas to JSON Schema
- **Rate Limits**: N/A (manual downloads)
- **Auth**: None for catalog, registration may be needed for e-Repository
- **Schema Count**: ~777 message definitions, curate to ~50 most common (pacs.008, pain.001, camt.053, etc.)

### W3C Verifiable Credentials
- **API**: Direct GitHub raw URLs
- **Format**: JSON Schema draft-2020-12 (already target format!)
- **Conversion**: None
- **Rate Limits**: GitHub CDN limits (generous)
- **Auth**: None
- **Schema Count**: ~5 core schemas (VC, VP, CredentialSchema, StatusList, etc.)

### OASIS UBL
- **API**: Static ZIP downloads from OASIS
- **Format**: JSON Schema (draft unspecified) + XSD
- **Conversion**: Low-Medium — check draft, update if needed
- **Rate Limits**: None
- **Auth**: None
- **Schema Count**: 91 document types (Invoice, Order, DespatchAdvice, etc.)

### NIEM
- **API**: GitHub releases + Movement tool
- **Format**: XSD (core) + JSON Schema draft-04 (Movement output)
- **Conversion**: Static bundle — use Movement-generated schemas, update from draft-04
- **Rate Limits**: None
- **Auth**: None
- **Schema Count**: Thousands of elements, curate subsets by domain (Justice, Immigration, etc.)

### buildingSMART IFC
- **API**: bSDD REST/GraphQL + ifcJSON GitHub repo
- **Format**: EXPRESS (core), community JSON Schemas
- **Conversion**: Static bundle — use ifcJSON community schemas
- **Rate Limits**: None for test endpoint
- **Auth**: OAuth2 for secured bSDD endpoint, none for GitHub
- **Schema Count**: ~800 IFC classes, curate to ~100 most common

## R3: Provider Implementation Strategy

### Decision: Three provider patterns

1. **Live API providers** (fetch on refresh): SchemaStore.org, schema.org, HL7 FHIR, W3C VC
2. **ZIP/bundle providers** (download + extract): OASIS UBL
3. **Static file providers** (bundled in repo): ISO 20022, NIEM, IFC

**Rationale**: Sources without stable public APIs shouldn't be fetched at runtime — static bundles are more reliable and don't require network access for standards that change infrequently (ISO 20022 updates every few months, NIEM yearly, IFC every few years).

### JSON Schema Draft Normalisation

All providers must normalise to draft-2020-12. Common transformations:
- `$schema` → `"https://json-schema.org/draft/2020-12/schema"`
- `definitions` → `$defs` (draft-07 → 2020-12)
- `id` → `$id` (draft-04 → 2020-12)
- `exclusiveMinimum: true` (boolean) → `exclusiveMinimum: <value>` (number) (draft-04/06 → 2020-12)
- Split monolithic schemas into individual resources (FHIR)

Create a shared `JsonSchemaNormaliser` utility class for these transformations.

## R4: Index Design

### Decision: Separate MongoDB collection with text index

**Rationale**: The existing `schemas` collection stores full schema content for custom/imported schemas. The new `schemaIndex` collection stores lightweight metadata entries for all indexed schemas. This separation keeps the existing system unchanged while adding fast search.

**MongoDB Indexes for `schemaIndex`:**
1. Unique compound: `{sourceProvider, sourceUri}` — deduplication key
2. Text: `{title, description, keywords}` — full-text search
3. Compound: `{sectorTags, status}` — sector-filtered queries
4. Single: `{sourceProvider}` — per-provider queries
5. Single: `{lastFetchedAt}` — refresh age queries

### Org Preferences Collection

New `organisationSchemaPreferences` collection:
- Index on `{organizationId}` (unique)
- Stores `enabledSectors` array per org
- Default (no document): all sectors enabled

## R5: Sector Filtering Design

### Decision: Platform-curated sector taxonomy, stored as enum-like constants

**Rationale**: User-defined sectors would create inconsistency across organisations. Platform-curated sectors ensure schemas are consistently tagged and filtering works predictably.

**Implementation**: `SchemaSector` record with static instances (similar to `SchemaSource.Internal()`). Sectors stored as string arrays on index entries and org preferences for MongoDB query flexibility.

## R6: Schema Picker Integration

### Decision: MudDialog-based picker with embedded schema library search

**Rationale**: The blueprint builder already uses MudDialog for other selection workflows (LoadBlueprintDialog). The schema picker follows the same pattern — a dialog with search, filters, and selection that returns the chosen schema.

**Flow**:
1. Designer clicks "Add Data Schema" on an action
2. SchemaPickerDialog opens with search bar + sector filters
3. Designer searches/browses, clicks a schema card
4. Schema detail + field subset selector shown
5. Designer confirms → full schema JSON snapshotted into `dataSchemas`
6. Optional: FormSchemaService generates preview

## R7: Admin Health Dashboard

### Decision: Dedicated page in management section showing per-provider status

**Rationale**: Admins need visibility into which providers are healthy, when last refresh succeeded, and how many schemas each contributes. This is essential for diagnosing issues when schemas don't appear.

**Components**:
- Provider status cards (name, status indicator, last fetch time, schema count, error details)
- Manual refresh trigger per provider
- Org sector configuration panel
