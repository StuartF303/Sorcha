# Feature Specification: System Schema Store

**Feature Branch**: `013-system-schema-store`
**Created**: 2026-01-20
**Status**: Draft
**Input**: User description: "System Schema Store - Define core JSON schemas for system entities (installation, organisation, participant, register) with a server-side categorized schema store in Blueprint service, client-side caching in WASM, and integration with external schema sources like SchemaStore.org"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Access Core System Schemas (Priority: P1)

As a blueprint designer, I need to access pre-defined schemas for core system entities (Installation, Organisation, Participant, Register) so that I can create blueprints that work with standard Sorcha data structures.

**Why this priority**: Core system schemas are the foundation for all system blueprints. Without them, no system management workflows can be created. This enables the entire system blueprint ecosystem.

**Independent Test**: Can be fully tested by requesting a system schema (e.g., "Installation") from the Blueprint service and validating the returned JSON schema structure. Delivers immediate value by enabling schema-aware blueprint creation.

**Acceptance Scenarios**:

1. **Given** the Blueprint service is running, **When** a user requests the "Installation" schema, **Then** the system returns a valid JSON schema defining installation properties (name, description, version, public key)
2. **Given** the Blueprint service is running, **When** a user requests all system schemas, **Then** the system returns a categorized list including Installation, Organisation, Participant, and Register schemas
3. **Given** a system schema exists, **When** a user views its details, **Then** they see the schema's title, description, version, category, and full JSON schema definition

---

### User Story 2 - Client-Side Schema Caching (Priority: P2)

As a WASM client user, I need schemas to be cached locally so that blueprint design works efficiently without repeated server requests and remains functional during brief connectivity interruptions.

**Why this priority**: Performance and offline resilience are critical for a responsive user experience. However, this depends on the core schemas existing first (P1).

**Independent Test**: Can be tested by loading schemas in the WASM client, disconnecting from the server, and verifying cached schemas remain accessible for blueprint operations.

**Acceptance Scenarios**:

1. **Given** a user has previously loaded schemas in the WASM client, **When** they access those schemas again, **Then** the client serves them from local cache without a server request
2. **Given** schemas are cached locally, **When** the server has updated versions, **Then** the client detects the change and refreshes the cache
3. **Given** the server is temporarily unavailable, **When** a user accesses previously cached schemas, **Then** the schemas are available from cache with a visual indicator of offline status

---

### User Story 3 - Browse and Search External Schemas (Priority: P3)

As a blueprint designer, I need to discover and use schemas from external sources (like SchemaStore.org) so that I can incorporate industry-standard data formats into my blueprints.

**Why this priority**: External schema integration extends the platform's capabilities but is not essential for core system operations. Core functionality (P1, P2) must work first.

**Independent Test**: Can be tested by searching for a well-known schema (e.g., "package.json") and verifying the system retrieves and displays it from SchemaStore.org.

**Acceptance Scenarios**:

1. **Given** the user wants to find an external schema, **When** they search by name or keyword, **Then** the system queries configured external sources and returns matching schemas
2. **Given** an external schema is found, **When** the user selects it, **Then** the schema is fetched, cached locally, and available for use in blueprints
3. **Given** an external source is unavailable, **When** the user searches for schemas, **Then** the system displays cached results (if any) and indicates which sources are unreachable

---

### User Story 4 - Manage Schema Store Categories (Priority: P4)

As a system administrator, I need to organize schemas into categories (System, External, Custom) so that users can easily find and distinguish between different schema types.

**Why this priority**: Organization and categorization improve usability but are not blocking for core functionality.

**Independent Test**: Can be tested by viewing the schema store and verifying schemas are grouped by category with clear visual distinction.

**Acceptance Scenarios**:

1. **Given** schemas exist from multiple sources, **When** a user views the schema store, **Then** schemas are organized into categories: System, External, and Custom
2. **Given** a user is browsing schemas, **When** they filter by category, **Then** only schemas in the selected category are displayed
3. **Given** an external schema is imported, **When** it is added to the store, **Then** it is automatically categorized as "External" with its source recorded

---

### Edge Cases

- What happens when an external schema source returns invalid JSON? The system should reject the schema with a clear error message and not cache invalid data.
- How does the system handle schema version conflicts when the same schema exists locally and externally? The system should prefer the explicitly imported version and warn about conflicts.
- What happens when the WASM client's cache storage is full? The system should implement LRU (Least Recently Used) eviction for cached external schemas while preserving system schemas.
- How does the system handle external schemas that reference other schemas ($ref)? The system should recursively fetch and cache referenced schemas.
- What happens when an organization tries to publish a Custom schema with an identifier that conflicts with an existing global schema? The system should reject the publish request with a clear conflict message.

## Requirements *(mandatory)*

### Functional Requirements

**Core System Schemas**
- **FR-001**: System MUST provide a pre-defined JSON schema for "Installation" entity containing: name (string, required), description (string), version (semantic version string), publicKey (string, required)
- **FR-002**: System MUST provide a pre-defined JSON schema for "Organisation" entity containing: identifier (string, required), name (string, required), description (string), status (enumeration), contactDetails (object)
- **FR-003**: System MUST provide a pre-defined JSON schema for "Participant" entity containing: identifier (string, required), displayName (string, required), role (enumeration: administrator, designer, member), organisationRef (reference to Organisation)
- **FR-004**: System MUST provide a pre-defined JSON schema for "Register" entity containing: identifier (string, required), title (string, required), description (string), schema (JSON schema reference), status (enumeration)
- **FR-005**: System schemas MUST be immutable - they cannot be modified or deleted by users

**Schema Store Service**
- **FR-006**: Blueprint service MUST expose endpoints to list, retrieve, and search schemas
- **FR-007**: System MUST categorize schemas as: "System" (core Sorcha schemas), "External" (from external sources), or "Custom" (user-defined)
- **FR-008**: System MUST store schema metadata including: identifier, title, description, version, category, source, status (Active/Deprecated), dateAdded, dateModified, dateDeprecated (if applicable)
- **FR-009**: System MUST support schema versioning using semantic versioning (major.minor.patch)
- **FR-010**: System MUST validate all schemas against JSON Schema specification before storing
- **FR-010a**: System MUST require authentication for all schema endpoints
- **FR-010b**: System MUST restrict schema creation and modification to users with administrator role; all authenticated users may read schemas
- **FR-010c**: System MUST allow administrators to mark schemas as Deprecated; deprecated schemas remain accessible but are visually flagged in listings and search results
- **FR-010d**: System MUST display a deprecation warning when a deprecated schema is selected for use in a blueprint
- **FR-010e**: System and External schemas MUST be globally accessible to all organizations
- **FR-010f**: Custom schemas MUST be scoped to the creating organization by default; only users within that organization can view and use them
- **FR-010g**: Organization administrators MUST be able to publish Custom schemas globally, making them visible to all organizations (read-only for other organizations)
- **FR-010h**: System MUST track the owning organization for Custom schemas and indicate publishing status (organization-only or globally published)

**Client-Side Caching**
- **FR-011**: WASM client MUST cache retrieved schemas in browser storage
- **FR-012**: Client MUST use ETag or version-based cache validation to detect server-side changes
- **FR-013**: Client MUST provide offline access to previously cached schemas
- **FR-014**: Client MUST indicate when displaying cached data in offline mode

**External Schema Integration**
- **FR-015**: System MUST support fetching schemas from SchemaStore.org catalog
- **FR-016**: System MUST allow configuration of additional external schema sources
- **FR-017**: System MUST cache external schemas locally after first retrieval
- **FR-018**: System MUST handle external source unavailability gracefully, falling back to cached versions
- **FR-019**: System MUST respect rate limits and caching headers from external sources

### Key Entities

- **Schema**: Represents a JSON schema definition with metadata (identifier, title, description, version, category, source, status [Active/Deprecated], the schema content itself)
- **SchemaCategory**: Classification of schemas - System (immutable, global), External (from SchemaStore.org etc., global), Custom (organization-created, scoped to organization unless published globally)
- **SchemaSource**: Origin of a schema - Internal (Sorcha-defined), or External with source URL and attribution
- **Installation**: Core entity representing a Sorcha system deployment with identity and cryptographic keys
- **Organisation**: Core entity representing a group or company participating in the system
- **Participant**: Core entity representing an individual user with a defined role within an organisation
- **Register**: Core entity representing a data container with a defined schema and access rules

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can retrieve any system schema within 500 milliseconds on first request
- **SC-002**: Cached schemas load in the WASM client within 100 milliseconds (no server round-trip)
- **SC-003**: Blueprint designers can discover and use all four core system schemas without documentation reference
- **SC-004**: External schema searches return results within 3 seconds under normal network conditions
- **SC-005**: Client remains functional for schema-related operations during server outages of up to 24 hours (using cached data)
- **SC-006**: 95% of blueprint designers can locate a specific schema category within 10 seconds
- **SC-007**: System correctly validates and rejects invalid JSON schemas 100% of the time

## Assumptions

- JSON Schema draft 2020-12 will be used as the schema specification standard
- SchemaStore.org API is publicly accessible and provides a stable interface
- Browser storage (IndexedDB or localStorage) provides sufficient capacity for typical schema caching needs
- The Blueprint service already has infrastructure for HTTP endpoints and can be extended
- System schemas will be versioned alongside the Sorcha platform releases
- External schemas will be cached with a default TTL of 24 hours, configurable by administrators

## Dependencies

- Existing Blueprint service infrastructure
- Browser storage APIs (IndexedDB preferred, localStorage fallback)
- Network access to SchemaStore.org and other configured external sources
- JSON Schema validation library (already in use: JsonSchema.Net)

## Clarifications

### Session 2026-01-20

- Q: Who can add/modify external and custom schemas? → A: Authenticated read/write with role-based permissions - administrators can add/modify schemas, all authenticated users can read
- Q: Should schemas have lifecycle states? → A: Active and Deprecated states - deprecated schemas remain accessible but are flagged to discourage new usage
- Q: Are schemas global or organization-scoped? → A: Hybrid with sharing - System and External schemas are global; Custom schemas are organization-scoped but can optionally be published globally

## Out of Scope

- Schema authoring/editing tools (users import existing schemas)
- Schema transformation or conversion between formats
- Real-time collaboration on schema definitions
- Schema diff/comparison tooling
- Custom schema creation workflows (addressed in future feature)
