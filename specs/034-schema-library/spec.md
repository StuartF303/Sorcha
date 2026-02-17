# Feature Specification: Schema Library

**Feature Branch**: `034-schema-library`
**Created**: 2026-02-17
**Status**: Draft
**Input**: User description: "Schema Library - A centralised schema registry with server-side cache and index of formal data standards (schema.org, NIEM, HL7 FHIR, ISO 20022, IFC, W3C VC, UBL, JSON Schema Store, etc.). Organizations have admin-controlled filtered views so designers only see relevant schemas. Server-side cache maintains the complete indexed list from all sources. When building a blueprint, designers select formal schemas before creating custom ones. Includes mechanisms for schema lookup, schema insertion into blueprint actions, and form building from selected schemas."

## Context

Sorcha already has a schema infrastructure with `ISchemaStore`, `ISchemaRepository`, `IExternalSchemaProvider` (SchemaStore.org only), `SchemaEndpoints`, and organization-scoped custom schemas stored in MongoDB. The current system supports three categories: System (4 embedded schemas), External (imported from SchemaStore.org), and Custom (org-scoped).

**What's missing:**
- Only one external source (SchemaStore.org) — no access to NIEM, HL7 FHIR, ISO 20022, IFC, W3C VC, UBL, schema.org, or other formal standards
- No server-side unified index across all sources with rich metadata
- No organisation-level sector filtering (finance orgs see construction schemas and vice versa)
- No schema selection workflow for blueprint designers
- No descriptive browsing experience (sector, use case, field count, related schemas)

**Constraint:** All backend changes live within Blueprint Service (tightly coupled to blueprints).

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse and Search Formal Data Schemas (Priority: P1)

A blueprint designer opens the schema library and searches for a data standard relevant to their workflow. They see schemas organised by sector and source, with descriptive summaries showing what each schema covers, how many fields it has, and what industry standard it comes from. They can filter by sector (e.g., "Healthcare", "Finance", "Construction") and source (e.g., "HL7 FHIR", "ISO 20022"). Only schemas relevant to their organisation's configured sectors are visible.

**Why this priority**: This is the foundational capability — without searchable, descriptive schema browsing, designers cannot discover or use formal standards.

**Independent Test**: Can be tested by logging in as a designer, opening the schema library, and confirming that schemas appear with rich descriptions, correct filtering, and fast search results.

**Acceptance Scenarios**:

1. **Given** a designer in a finance organisation, **When** they open the schema library, **Then** they see schemas from ISO 20022, UBL, FIX Protocol, and schema.org (finance-relevant) but not IFC, NIEM Criminal Justice, or HL7 FHIR
2. **Given** a designer searches "invoice", **When** results load, **Then** they see UBL Invoice, schema.org Invoice, and any matching custom schemas — each with a title, description, field count, source, and sector tag
3. **Given** a designer searches for a schema, **When** results exceed 25 items, **Then** results are paginated with cursor-based navigation and total count displayed
4. **Given** a designer clicks on a schema result, **When** the detail view opens, **Then** they see the full JSON Schema, a human-readable field list with types, required fields highlighted, and example values where available

---

### User Story 2 - Organisation Admin Configures Schema Visibility (Priority: P1)

An organisation administrator opens schema library settings and selects which sectors and sources are relevant to their organisation. For example, a construction company enables "Construction & Planning", "Government", and "General" sectors while disabling "Healthcare", "Finance", and "Benefits". This immediately filters what their blueprint designers see when browsing or searching.

**Why this priority**: Without org-level filtering, designers in specialised organisations are overwhelmed by thousands of irrelevant schemas. This is essential for usability.

**Independent Test**: Can be tested by an admin toggling sector filters on and off, then verifying as a designer that the schema library reflects those changes immediately.

**Acceptance Scenarios**:

1. **Given** an organisation with no configured filters, **When** a designer opens the schema library, **Then** all schemas across all sectors are visible (default: everything enabled)
2. **Given** an admin enables only "Finance" and "General" sectors, **When** a designer in that org searches the library, **Then** only schemas tagged with those sectors appear
3. **Given** an admin disables a sector that contains schemas already used in published blueprints, **When** designers browse, **Then** those schemas are hidden from new searches but remain functional in existing blueprints
4. **Given** an admin re-enables a previously disabled sector, **When** designers search, **Then** schemas from that sector reappear immediately without any re-import

---

### User Story 3 - Server-Side Schema Index and Cache (Priority: P1)

The system maintains a centralised, server-side index of all schemas from all configured external sources. This index is built on first startup and refreshed periodically. Each schema entry includes rich metadata: title, description, sector tags, source provider, field count, schema version, and keywords. The index supports full-text search and filtering without fetching from external sources on every request.

**Why this priority**: Without a server-side index, every search hits external APIs (slow, rate-limited, unreliable). The index is the performance backbone of the entire feature.

**Independent Test**: Can be tested by querying the schema index API and confirming fast responses, correct metadata, and results from multiple sources — even when external sources are temporarily unreachable.

**Acceptance Scenarios**:

1. **Given** the Blueprint Service starts for the first time, **When** the schema index is empty, **Then** a background process fetches catalogs from all configured sources non-blockingly — the service accepts requests immediately and schemas appear progressively as each source completes, with a "still loading" indicator for incomplete sources
2. **Given** the index is populated, **When** a designer searches "patient referral", **Then** results return from the index (not live external calls) with response time under 500ms
3. **Given** an external source (e.g., HL7 FHIR) is temporarily unreachable, **When** the periodic refresh runs, **Then** the cached entries from that source remain available and the system logs the failure without degrading the user experience
4. **Given** new schemas are published by an external source, **When** the periodic refresh runs, **Then** new entries appear in the index and deprecated entries are marked accordingly

---

### User Story 4 - Select Schemas for Blueprint Actions (Priority: P2)

When building a blueprint, a designer selects formal schemas to use for each action's data requirements. They can browse the filtered schema library, preview a schema's fields, and insert it as the action's data schema. If no suitable formal schema exists, they can create a custom schema. The system encourages formal schema use by presenting the library search first.

**Why this priority**: This connects the schema library to the blueprint building workflow, making formal standards practical to use. Depends on US1 and US3 being complete.

**Independent Test**: Can be tested by creating a new blueprint action, opening the schema picker, selecting a schema, and confirming it appears as the action's data schema with correct field definitions.

**Acceptance Scenarios**:

1. **Given** a designer is adding an action to a blueprint, **When** they open the schema picker, **Then** they see the filtered schema library with search, sector filters, and a "Create Custom" option
2. **Given** a designer selects an ISO 20022 payment schema, **When** they confirm selection, **Then** the schema is inserted as the action's data schema and a form preview is generated automatically
3. **Given** a designer selects a schema with fields they don't need, **When** they review the selection, **Then** they can choose which fields to include (subset selection) while maintaining schema compliance
4. **Given** a designer cannot find a suitable formal schema, **When** they choose "Create Custom", **Then** they can define fields manually and the custom schema is saved to the organisation's library for reuse

---

### User Story 5 - Multiple External Schema Sources (Priority: P2)

The system integrates with multiple formal data standards repositories beyond SchemaStore.org. Each source has a dedicated provider that fetches, normalises, and indexes schemas into the unified index. Sources include schema.org, NIEM, HL7 FHIR, ISO 20022, buildingSMART IFC, W3C Verifiable Credentials, OASIS UBL, and the existing SchemaStore.org.

**Why this priority**: Expanding beyond SchemaStore.org is what makes the library genuinely useful for domain-specific workflows. Depends on US3 (index infrastructure) being in place.

**Independent Test**: Can be tested by searching for domain-specific schemas (e.g., "FHIR Patient", "UBL Invoice", "IFC Building") and confirming results come from the correct source with proper metadata.

**Acceptance Scenarios**:

1. **Given** the index has been populated, **When** a designer searches "patient", **Then** they see results from HL7 FHIR (Patient, Practitioner) and schema.org (MedicalEntity) with source attribution
2. **Given** a new external provider is configured, **When** the index refresh runs, **Then** schemas from that provider appear in search results with correct sector tags and source metadata
3. **Given** each source has different schema formats, **When** schemas are indexed, **Then** all are normalised to JSON Schema draft 2020-12 with consistent metadata fields

---

### User Story 6 - Schema Descriptions and Metadata Enrichment (Priority: P2)

Each schema in the library displays rich, human-readable information to help designers understand what the schema covers without reading raw JSON. This includes: a plain-language description, sector/industry tags, the source standard it comes from, the number and names of fields, which fields are required, example values, and related schemas. Keywords enable better search results.

**Why this priority**: Descriptive metadata is what makes the library usable by non-technical designers. Without it, formal schemas are opaque JSON documents.

**Independent Test**: Can be tested by viewing a schema detail page and confirming all metadata fields are present, accurate, and helpful.

**Acceptance Scenarios**:

1. **Given** a schema is displayed in search results, **When** a designer views the listing, **Then** they see: title, one-line description, sector badge, source badge, field count, and a "required fields" indicator
2. **Given** a designer opens a schema's detail view, **When** the detail loads, **Then** they see: full description, all fields with types and constraints, required markers, example values (if provided by the source), and links to related schemas
3. **Given** a schema from HL7 FHIR has nested objects, **When** displayed, **Then** the field list shows a flattened, readable hierarchy (e.g., "patient.name.given" with type "string")

---

### User Story 7 - Form Preview from Selected Schema (Priority: P3)

When a designer selects a schema for a blueprint action, the system generates a preview of the form that participants will see. This uses the existing form auto-generation capabilities (FormSchemaService) to show a live preview of the form layout derived from the schema's field definitions.

**Why this priority**: Seeing the form before committing helps designers evaluate whether a schema is right for their action. Lower priority because form generation already exists — this just adds a preview step.

**Independent Test**: Can be tested by selecting a schema and confirming the preview matches what would be rendered in the actual form.

**Acceptance Scenarios**:

1. **Given** a designer previews a schema in the schema picker, **When** the preview loads, **Then** they see the auto-generated form layout with correct field types (text, number, date, selection, checkbox)
2. **Given** a schema has enum fields, **When** previewed, **Then** enum fields display as dropdowns with the available options listed
3. **Given** a schema has required fields, **When** previewed, **Then** required fields are visually distinguished from optional fields

---

### Edge Cases

- What happens when an external source changes its API or goes permanently offline? — The system marks that source as unavailable, retains cached schemas, and alerts admins
- What happens when two sources provide different schemas with the same name (e.g., "Invoice" from UBL and schema.org)? — Both appear in results with source attribution to distinguish them
- What happens when an org admin filters out a sector used in a draft (unpublished) blueprint? — Draft blueprints retain their schema references; the designer sees a warning that the schema is no longer in the org's library
- What happens when a formal schema is updated at the source? — The index refresh detects version changes and updates the entry; existing blueprints are unaffected because schema content is snapshotted into `dataSchemas` at selection time
- What happens when the schema index is very large (tens of thousands of schemas)? — Pagination, cursor-based queries, and sector filtering keep response times manageable
- What happens when a designer selects a subset of fields from a formal schema? — A derived schema is created referencing the original, maintaining traceability to the standard

## Clarifications

### Session 2026-02-17

- Q: How should the service behave during first-time index population (cold start)? → A: Non-blocking with progressive availability — service starts immediately; schemas appear as each source completes; designers see a "still loading" indicator for incomplete sources
- Q: How are schemas deduplicated across sources? → A: Source + URI uniqueness — each schema is uniquely identified by (source provider, source URI); duplicates across sources are separate entries with source badges
- Q: How should external source rate limiting be handled during refresh? → A: Per-provider configurable rate with backoff — each provider has its own rate limit setting and uses exponential backoff on 429/timeout responses
- Q: How do administrators monitor schema index health? → A: Admin-visible health dashboard in the management section of the UI showing per-provider status (last fetch time, schema count, errors), plus structured logging for refresh operations
- Q: How are schemas versioned in blueprints? → A: Snapshot at selection — full schema JSON is copied into the blueprint's dataSchemas at selection time; blueprint is self-contained and immune to future index changes

## Requirements *(mandatory)*

### Functional Requirements

**Index & Cache:**
- **FR-001**: System MUST maintain a server-side schema index containing entries from all configured external sources
- **FR-002**: System MUST populate the index via a non-blocking background process on first startup (service accepts requests immediately; schemas appear progressively as each source completes) and refresh it periodically (configurable interval, default 24 hours)
- **FR-003**: Each index entry MUST include: title, description, sector tags, source provider name, source URI, field count, field names, required fields, keywords, schema version, and last-fetched timestamp
- **FR-004**: System MUST serve all search and browse queries from the index, not from live external API calls
- **FR-005**: System MUST retain cached entries when an external source is temporarily unavailable during refresh

**External Sources:**
- **FR-006**: System MUST support multiple external schema providers via the existing `IExternalSchemaProvider` interface
- **FR-007**: System MUST include providers for: SchemaStore.org (existing), schema.org, HL7 FHIR, ISO 20022, W3C Verifiable Credentials, OASIS UBL, and a static/file-based provider for standards without public APIs (NIEM, IFC)
- **FR-008**: Each provider MUST normalise fetched schemas to JSON Schema draft 2020-12 format with consistent metadata
- **FR-009**: System MUST tag each indexed schema with one or more sector labels (e.g., "Finance", "Healthcare", "Construction", "Government", "General")
- **FR-009a**: Each external provider MUST have a configurable rate limit and MUST use exponential backoff when receiving 429/timeout responses during refresh
- **FR-009b**: System MUST provide an admin-visible health dashboard in the management section of the UI showing per-provider status (last fetch time, schema count, error details) and MUST emit structured logs for all refresh operations

**Organisation Filtering:**
- **FR-010**: Organisation administrators MUST be able to configure which sectors are visible to their designers
- **FR-011**: System MUST store sector visibility preferences per organisation
- **FR-012**: Schema search and browse results MUST be filtered by the requesting user's organisation sector preferences
- **FR-013**: Disabling a sector MUST NOT affect schemas already referenced by existing blueprints
- **FR-014**: When no sector preferences are configured, all sectors MUST be visible (default: everything enabled)

**Search & Browse:**
- **FR-015**: System MUST support full-text search across schema title, description, field names, and keywords
- **FR-016**: System MUST support filtering by sector, source provider, category (System/External/Custom), and status (Active/Deprecated)
- **FR-017**: Search results MUST display: title, one-line description, sector badge(s), source badge, field count, and required-field indicator
- **FR-018**: Schema detail view MUST display: full description, all fields with types and constraints, required markers, example values, related schemas, and the raw JSON Schema

**Blueprint Integration:**
- **FR-019**: System MUST provide a schema picker component that integrates with the blueprint builder workflow
- **FR-020**: The schema picker MUST present the filtered schema library with search, allowing designers to select formal schemas for blueprint actions
- **FR-021**: When a designer selects a schema, system MUST snapshot the full schema JSON into the action's `dataSchemas` field — the blueprint is self-contained and immune to future index changes
- **FR-022**: System MUST support selecting a subset of fields from a formal schema for use in an action
- **FR-023**: System MUST generate a form preview from a selected schema using existing form auto-generation capabilities
- **FR-024**: System MUST present formal schema search before the "Create Custom" option to encourage standards adoption

**Metadata & Descriptions:**
- **FR-025**: Each schema listing MUST include a human-readable description of what the schema covers
- **FR-026**: Schema detail view MUST present fields in a readable format including nested object hierarchies
- **FR-027**: System MUST display the originating standard name and version for external schemas
- **FR-028**: System MUST track and display schema usage count (how many blueprints reference it) per organisation

### Key Entities

- **SchemaIndexEntry**: An entry in the server-side schema index — identifier (unique by source provider + source URI), title, description, sector tags, source provider, source URI, field count, field names, required fields, keywords, schema version, normalised content hash, last-fetched timestamp, status (Active/Deprecated/Unavailable)
- **SchemaProvider**: A configured external source — provider name, base URI, provider type, refresh interval, rate limit (requests per second), enabled/disabled, last successful fetch, health status, backoff state
- **OrganisationSchemaPreferences**: Per-org configuration — organisation ID, enabled sectors list, last modified timestamp, modified by user ID
- **SchemaSector**: A classification label — sector ID (e.g., "finance", "healthcare", "construction"), display name, description, icon identifier
- **DerivedSchema**: A subset of a formal schema — parent schema reference, included fields, organisation ID, created timestamp

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Designers can search and find relevant formal schemas from at least 5 different standards sources within 2 seconds
- **SC-002**: Organisation administrators can configure sector visibility in under 1 minute, and changes take effect immediately for designers
- **SC-003**: The schema index contains at least 500 schemas from 5+ sources after initial population
- **SC-004**: Schema search returns results within 500ms for any query when the index is populated
- **SC-005**: 80% of blueprint actions in new blueprints reference formal schemas rather than fully custom schemas (measured after 3 months of adoption)
- **SC-006**: Designers can select a formal schema and insert it into a blueprint action in under 30 seconds
- **SC-007**: The system remains fully functional (search, browse, select) when one or more external sources are offline
- **SC-008**: Organisation sector filtering reduces visible schemas by at least 60% for specialised organisations (e.g., a finance-only org sees less than 40% of total schemas)

## Assumptions

- All backend changes (API endpoints, providers, index, caching) will be implemented within the Blueprint Service since schemas and blueprints are tightly coupled
- The existing `IExternalSchemaProvider` interface is sufficient for all new providers (each source just needs a new implementation)
- Standards without public JSON Schema APIs (NIEM, IFC) will use a static/file-based provider with periodically updated schema bundles
- Schema.org vocabulary definitions can be converted to JSON Schema format via a transformation layer
- The existing MongoDB infrastructure supports the schema index storage requirements
- The existing `FormSchemaService` handles auto-generation from any valid JSON Schema without modifications
- Blueprint builder integration (US4) establishes the API mechanisms and components, but the full builder UX will be built separately in a future feature
- Sector tags are curated by the platform (not user-defined) to maintain consistency across organisations
