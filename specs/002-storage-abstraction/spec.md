# Feature Specification: Multi-Tier Storage Abstraction Layer

**Feature Branch**: `002-storage-abstraction`
**Created**: 2025-12-07
**Status**: Draft
**Input**: User description: "Multi-tier storage abstraction layer with verified cache for Register Service, supporting hot/warm/cold storage tiers with pluggable providers for PostgreSQL, MongoDB, Redis, and cloud storage"

## Overview

The Sorcha platform requires a unified data persistence architecture that supports multiple storage tiers with different characteristics, pluggable storage providers for deployment flexibility, and a specialized verified cache model for the Register Service that cryptographically validates all data before use.

## Clarifications

### Session 2025-12-07

- Q: What startup strategy should Register Service use for large registers? → A: Configurable threshold - operators choose blocking vs. progressive based on docket count, with sensible defaults. Performance guidance to be provided later.
- Q: What observability requirements apply to the storage abstraction? → A: Configurable per-tier - different observability levels per storage tier (e.g., full tracing for cold, metrics only for hot).
- Q: What state model applies to verified dockets and registers? → A: Dockets are binary (Verified or Corrupted - immutable once validated). Register operational state has richer lifecycle (Healthy, Degraded, Recovering, PeerSyncInProgress) to signal remedial actions to clients. Separation of data immutability from operational status.

### Business Context

- Services currently use in-memory storage implementations
- Production deployment requires durable, scalable storage
- Different data types have different storage requirements (ephemeral cache vs. immutable ledger)
- Cloud-agnostic deployment requires pluggable storage providers
- Register Service ledger data must be cryptographically verified to prevent tampering attacks

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Service Developer Configures Storage (Priority: P1)

A developer deploying a Sorcha service needs to configure storage providers through configuration files without changing application code. They select appropriate providers for their deployment environment (local Docker, Azure, AWS, or on-premises).

**Why this priority**: This is the foundational capability - without configurable storage, services cannot persist data in production environments.

**Independent Test**: Can be fully tested by configuring a service's appsettings.json with different provider settings and verifying the service starts and persists data correctly.

**Acceptance Scenarios**:

1. **Given** a service with storage configuration in appsettings.json, **When** the service starts, **Then** it connects to the configured providers and begins accepting requests within 30 seconds
2. **Given** a service configured for PostgreSQL warm storage, **When** data is saved, **Then** the data persists in PostgreSQL and survives service restart
3. **Given** a service configured for Redis hot storage, **When** cached data expires, **Then** the data is automatically removed after the configured TTL
4. **Given** invalid storage configuration, **When** the service starts, **Then** it fails fast with clear error messages identifying the misconfiguration

---

### User Story 2 - Register Service Loads Verified Data (Priority: P1)

The Register Service needs to load ledger data from cold storage on startup, cryptographically verify every docket and transaction, and make only verified data available for queries. Any corrupted data must be flagged for peer recovery.

**Why this priority**: This is the core security model for the ledger - without verification, tampered data could be served to users.

**Independent Test**: Can be fully tested by seeding cold storage with valid and invalid (corrupted) dockets, starting the service, and verifying that only valid data is queryable while invalid data is flagged.

**Acceptance Scenarios**:

1. **Given** cold storage containing 1000 valid dockets, **When** Register Service starts, **Then** all 1000 dockets are verified and loaded into the cache
2. **Given** cold storage with a docket containing an invalid hash, **When** Register Service starts, **Then** that docket is excluded from the cache and marked for peer recovery
3. **Given** cold storage with a broken chain link (previous hash mismatch), **When** Register Service starts, **Then** the chain break is detected and all subsequent dockets are marked for recovery
4. **Given** a transaction with an invalid cryptographic signature, **When** the transaction is loaded, **Then** it is rejected and logged for audit

---

### User Story 3 - Register Service Recovers Corrupted Data (Priority: P2)

When the Register Service detects corrupted data in local storage, it must request replacement dockets from the peer network, verify the received data, and integrate it into the local store.

**Why this priority**: Recovery from corruption is essential for resilience but depends on the verification system (P1) being in place first.

**Independent Test**: Can be fully tested by corrupting local storage, starting the service, mocking peer network responses, and verifying that valid peer data replaces corrupted local data.

**Acceptance Scenarios**:

1. **Given** corrupted dockets detected at heights 50-55, **When** peer network provides valid replacements, **Then** the cache is updated with verified peer data
2. **Given** a peer provides invalid replacement data, **When** verification fails, **Then** the data is rejected and another peer is queried
3. **Given** no peers can provide valid data for a corrupted range, **When** recovery fails, **Then** the Register is marked as degraded with clear status indication

---

### User Story 4 - Tenant Service Uses Warm Storage (Priority: P2)

The Tenant Service needs to persist organization configurations, user identities, and audit logs to durable warm storage with full ACID transaction support.

**Why this priority**: Multi-tenant configuration is critical for production but can use standard repository patterns.

**Independent Test**: Can be fully tested by creating, updating, and querying organizations and users through the Tenant Service API and verifying data persists across service restarts.

**Acceptance Scenarios**:

1. **Given** a new organization to create, **When** the create operation completes, **Then** the organization is persisted and immediately queryable
2. **Given** an organization update in progress, **When** the service crashes mid-transaction, **Then** no partial updates are visible (ACID rollback)
3. **Given** multi-tenant isolation requirements, **When** querying data, **Then** only data for the current tenant is returned

---

### User Story 5 - Blueprint Service Uses Document Storage (Priority: P2)

The Blueprint Service needs to store complex JSON blueprint definitions with flexible schema evolution, supporting versioning and efficient queries on nested structures.

**Why this priority**: Blueprint storage enables workflow persistence but can initially use warm-tier document storage.

**Independent Test**: Can be fully tested by saving, versioning, and querying blueprints through the Blueprint Service API.

**Acceptance Scenarios**:

1. **Given** a complex nested blueprint definition, **When** saved to document storage, **Then** the full structure is preserved including all nested properties
2. **Given** a blueprint with a new optional field, **When** loaded alongside older blueprints without that field, **Then** both are handled correctly without migration
3. **Given** a search for blueprints by nested property, **When** the query executes, **Then** matching blueprints are returned efficiently

---

### User Story 6 - Wallet Service Caches Hot Data (Priority: P3)

The Wallet Service needs to cache frequently accessed wallet metadata for fast retrieval while persisting authoritative data to warm storage.

**Why this priority**: Performance optimization through caching improves user experience but is not required for basic functionality.

**Independent Test**: Can be fully tested by measuring response times for wallet queries with and without cache hits.

**Acceptance Scenarios**:

1. **Given** a wallet queried twice within the cache TTL, **When** the second query executes, **Then** it returns in under 10ms from cache
2. **Given** a cached wallet that is updated, **When** the update completes, **Then** the cache is invalidated and the next query fetches fresh data
3. **Given** a cache failure (Redis down), **When** queries execute, **Then** they fall back to warm storage with graceful degradation

---

### Edge Cases

- What happens when cold storage is completely empty on first startup? (Initialize empty verified cache, begin accepting new dockets)
- How does the system handle concurrent writes to the same entity? (Optimistic concurrency with version/ETag checking)
- What happens when Redis cache is unavailable? (Graceful degradation to direct storage access with performance warning)
- How does the Register Service handle very large registers on startup? (Configurable: operators set threshold for blocking vs. progressive verification; defaults to blocking for <1000 dockets, progressive for larger registers)
- What happens when storage providers have incompatible versions? (Fail fast with version mismatch error)

## Requirements *(mandatory)*

### Functional Requirements

#### Core Abstraction Layer

- **FR-001**: System MUST provide a unified storage abstraction with three tiers: Hot (cache), Warm (operational), and Cold (immutable)
- **FR-002**: System MUST allow storage provider selection through configuration without code changes
- **FR-003**: System MUST support graceful degradation when optional storage tiers (hot cache) are unavailable
- **FR-004**: System MUST provide consistent interfaces across all storage tiers for common operations (get, query, count)

#### Hot Tier (Cache)

- **FR-010**: Hot tier MUST support key-value storage with configurable time-to-live (TTL) expiration
- **FR-011**: Hot tier MUST support atomic increment operations for rate limiting use cases
- **FR-012**: Hot tier MUST support pattern-based key deletion for cache invalidation
- **FR-013**: Hot tier MUST support the cache-aside pattern (get-or-set with factory function)

#### Warm Tier (Operational)

- **FR-020**: Warm tier MUST support CRUD operations with ACID transaction guarantees for relational data
- **FR-021**: Warm tier MUST support query operations with predicate-based filtering
- **FR-022**: Warm tier MUST support pagination for large result sets
- **FR-023**: Warm tier MUST support document storage for flexible schema data (blueprints, configurations)
- **FR-024**: Warm tier document storage MUST preserve complex nested JSON structures

#### Cold Tier (Immutable WORM)

- **FR-030**: Cold tier MUST enforce append-only semantics - updates and deletes MUST be prevented
- **FR-031**: Cold tier MUST support sequential range queries for ledger traversal
- **FR-032**: Cold tier MUST support batch append operations for docket sealing
- **FR-033**: Cold tier MUST track and report the current sequence height

#### Register Service Verified Cache

- **FR-040**: Register Service MUST verify cryptographic signatures of all transactions before adding to cache
- **FR-041**: Register Service MUST verify hash chain integrity (previous hash linkage) for all dockets
- **FR-042**: Register Service MUST exclude corrupted data from the verified cache
- **FR-043**: Register Service MUST track corrupted ranges for peer recovery
- **FR-044**: Register Service MUST serve ALL read queries from the verified cache, never directly from cold storage
- **FR-045**: Register Service MUST verify new dockets before adding to both cache and cold storage atomically
- **FR-046**: Register Service MUST report cache initialization status including verification results and any corruption detected
- **FR-047**: Register Service MUST support configurable startup verification strategy (blocking or progressive) with sensible defaults based on register size
- **FR-048**: Register Service MUST maintain and expose operational state (Healthy, Degraded, Recovering, PeerSyncInProgress) to signal remedial actions to clients
- **FR-049**: Register Service MUST treat docket verification as binary and immutable - a docket is either Verified (queryable) or Corrupted (excluded), with no intermediate states

#### Provider Support

- **FR-050**: System MUST support Redis as a hot tier provider
- **FR-051**: System MUST support PostgreSQL as a warm tier relational provider
- **FR-052**: System MUST support MongoDB as a warm tier document provider and cold tier WORM provider
- **FR-053**: System MUST support in-memory providers for development and testing
- **FR-054**: System MUST allow future extension to cloud-native providers (Azure Cosmos DB, AWS DynamoDB) without breaking changes

#### Observability

- **FR-060**: System MUST support configurable observability levels per storage tier
- **FR-061**: Hot tier MUST emit metrics for cache hit/miss rates, operation latency, and error counts
- **FR-062**: Warm tier MUST emit structured logs for CRUD operations and metrics for query performance
- **FR-063**: Cold tier MUST support full distributed tracing for append and verification operations
- **FR-064**: All tiers MUST integrate with standard observability infrastructure (OpenTelemetry compatible)

### Key Entities

- **StorageConfiguration**: Defines which providers to use for each tier and their connection settings
- **CacheEntry**: Key-value pair with optional TTL for hot tier storage
- **VerifiedDocket**: A cryptographically verified docket in the Register Service cache; binary state (Verified or Corrupted) - immutable once validated
- **RegisterOperationalState**: Operational status of a register with lifecycle states: Healthy (fully verified, serving requests), Degraded (some corruption detected, partial service), Recovering (peer sync in progress), PeerSyncInProgress (actively fetching from peers). Signals remedial actions to clients.
- **CorruptionRange**: Represents a range of docket heights that failed verification and require peer recovery
- **CacheInitializationResult**: Result of loading and verifying cold storage data on startup, including counts, corruption details, and resulting operational state

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Services can be configured for any supported storage provider through configuration changes alone (no code modifications required)
- **SC-002**: Register Service detects 100% of tampered dockets (invalid hash or signature) and excludes them from the verified cache
- **SC-003**: Hot tier cache operations complete in under 10ms for 99th percentile requests
- **SC-004**: Warm tier operations complete in under 100ms for 99th percentile requests
- **SC-005**: Register Service startup completes verification of 10,000 dockets in under 60 seconds
- **SC-006**: System maintains data integrity after simulated storage provider failures (no data loss in warm/cold tiers)
- **SC-007**: Cache tier failures do not cause service unavailability (graceful degradation within 5 seconds)
- **SC-008**: All existing service functionality continues to work after storage abstraction integration (zero regression)

## Assumptions

- Storage providers (Redis, PostgreSQL, MongoDB) are available in the deployment environment
- Network connectivity to storage providers is reliable with standard latency
- Cryptographic libraries for signature verification are available (existing Sorcha.Cryptography)
- Peer network protocol for requesting replacement dockets exists or will be developed separately
- Services are deployed in environments with sufficient memory for in-memory verified caches

## Dependencies

- **Sorcha.Cryptography**: Required for signature verification in Register Service verified cache
- **Peer Service**: Required for corruption recovery (can be stubbed for initial implementation)
- **.NET Aspire**: Service orchestration and service discovery for storage connections

## Out of Scope

- Automatic failover between storage providers (manual configuration change required)
- Cross-region replication (handled by underlying provider configuration)
- Data migration tooling between providers
- Encryption at rest (delegated to storage provider configuration)
- Backup and restore procedures (operational concern, not application feature)
