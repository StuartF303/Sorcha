# Data Model: Schema Library

**Feature**: 034-schema-library
**Date**: 2026-02-17

## Entities

### SchemaIndexEntry (NEW)

Server-side index entry for a schema from any source. Lightweight metadata — full schema content fetched on demand from the provider or from the existing `schemas` collection.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | string | Generated (ObjectId) | MongoDB document ID |
| `SourceProvider` | string | Required | Provider name (e.g., "SchemaStore.org", "HL7 FHIR") |
| `SourceUri` | string | Required | Original schema URL or identifier at source |
| `Title` | string | Required, max 200 | Human-readable schema title |
| `Description` | string | Max 2000 | Plain-language description |
| `SectorTags` | string[] | At least 1 | Platform-curated sector labels |
| `Keywords` | string[] | Optional | Search keywords extracted from schema content |
| `FieldCount` | int | >= 0 | Number of top-level properties |
| `FieldNames` | string[] | Optional | Names of top-level properties |
| `RequiredFields` | string[] | Optional | Names of required properties |
| `SchemaVersion` | string | Semantic versioning | Version from source (e.g., "R5", "2.3", "1.0.0") |
| `JsonSchemaDraft` | string | Default "2020-12" | Target JSON Schema draft after normalisation |
| `ContentHash` | string | SHA-256 hex | Hash of normalised schema content for change detection |
| `Status` | SchemaIndexStatus | enum | Active, Deprecated, Unavailable |
| `LastFetchedAt` | DateTimeOffset | Required | When this entry was last refreshed from source |
| `DateAdded` | DateTimeOffset | Auto-set | When first indexed |
| `DateModified` | DateTimeOffset | Auto-set | When last modified |

**Uniqueness**: Composite unique key on `(SourceProvider, SourceUri)`

**Indexes**:
1. Unique: `{SourceProvider, SourceUri}`
2. Text: `{Title, Description, Keywords}`
3. Compound: `{SectorTags, Status}`
4. Single: `{SourceProvider}`
5. Single: `{LastFetchedAt}`

**State Transitions**:
```
                 ┌──────────────┐
  First Index    │              │   Source publishes
  ──────────────▶│    Active    │◀── new version
                 │              │   (re-fetch updates entry)
                 └──────┬───────┘
                        │
         Source marks    │   Source goes
         deprecated     │   offline
              │         │         │
              ▼         │         ▼
      ┌───────────┐     │  ┌──────────────┐
      │Deprecated │     │  │ Unavailable  │
      └───────────┘     │  └──────┬───────┘
              │         │         │
              └─────────┴─────────┘
                    ▲
                    │  Source comes back
                    │  online → re-fetch
```

---

### SchemaProviderStatus (NEW)

Runtime status tracking for each configured provider. Stored in MongoDB for persistence across restarts.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `ProviderName` | string | Required, unique | Provider identifier |
| `IsEnabled` | bool | Default true | Whether provider is active |
| `BaseUri` | string | Optional | Provider's base URL |
| `ProviderType` | ProviderType | enum | LiveApi, ZipBundle, StaticFile |
| `RateLimitPerSecond` | double | > 0, default 2.0 | Max requests per second |
| `RefreshIntervalHours` | int | > 0, default 24 | Hours between refreshes |
| `LastSuccessfulFetch` | DateTimeOffset? | Nullable | Last successful refresh timestamp |
| `LastError` | string? | Nullable | Last error message (if failed) |
| `LastErrorAt` | DateTimeOffset? | Nullable | When last error occurred |
| `SchemaCount` | int | >= 0 | Number of schemas indexed from this provider |
| `HealthStatus` | ProviderHealth | enum | Healthy, Degraded, Unavailable, Unknown |
| `BackoffUntil` | DateTimeOffset? | Nullable | Exponential backoff expiry |
| `ConsecutiveFailures` | int | >= 0, default 0 | Failure counter for backoff calculation |

**Enums**:
- `ProviderType`: `LiveApi`, `ZipBundle`, `StaticFile`
- `ProviderHealth`: `Healthy`, `Degraded`, `Unavailable`, `Unknown`

---

### OrganisationSchemaPreferences (NEW)

Per-organisation configuration for schema visibility filtering.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | string | Generated | MongoDB document ID |
| `OrganizationId` | string | Required, unique | Organisation identifier |
| `EnabledSectors` | string[] | Optional (null = all enabled) | Sector IDs visible to designers |
| `LastModifiedAt` | DateTimeOffset | Auto-set | When preferences were last changed |
| `ModifiedByUserId` | string | Required on update | User who made the change |

**Uniqueness**: Unique on `OrganizationId`

**Behaviour**:
- `null` or missing document → all sectors enabled (default)
- Empty array `[]` → no sectors enabled (nothing visible)
- Populated array → only listed sectors visible

---

### SchemaSector (CONSTANT)

Platform-curated sector definitions. Defined as static constants, not stored in database.

| Field | Type | Description |
|-------|------|-------------|
| `Id` | string | Lowercase identifier (e.g., "finance", "healthcare") |
| `DisplayName` | string | Human-readable name (e.g., "Finance & Banking") |
| `Description` | string | What this sector covers |
| `Icon` | string | MudBlazor icon identifier |

**Initial Values**:

| Id | Display Name | Icon |
|----|-------------|------|
| `finance` | Finance & Banking | `Icons.Material.Filled.AccountBalance` |
| `healthcare` | Healthcare | `Icons.Material.Filled.LocalHospital` |
| `construction` | Construction & Planning | `Icons.Material.Filled.Construction` |
| `government` | Government & Public Sector | `Icons.Material.Filled.AccountBalanceWallet` |
| `identity` | Identity & Credentials | `Icons.Material.Filled.Badge` |
| `commerce` | Commerce & Trade | `Icons.Material.Filled.ShoppingCart` |
| `technology` | Technology & DevOps | `Icons.Material.Filled.Code` |
| `general` | General Purpose | `Icons.Material.Filled.Category` |

---

### DerivedSchema (NEW)

A subset of a formal schema created when a designer selects specific fields.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | string | Generated | MongoDB document ID |
| `ParentSourceProvider` | string | Required | Source provider of parent schema |
| `ParentSourceUri` | string | Required | Source URI of parent schema |
| `ParentTitle` | string | Required | Title of parent schema |
| `IncludedFields` | string[] | At least 1 | Field names selected for inclusion |
| `ExcludedFields` | string[] | Optional | Field names explicitly excluded |
| `DerivedContent` | JsonDocument | Required | The subset JSON Schema |
| `OrganizationId` | string | Required | Owning organisation |
| `CreatedAt` | DateTimeOffset | Auto-set | When derived |
| `CreatedByUserId` | string | Required | User who created the derivation |

---

### SchemaEntry EXTENSIONS (EXISTING MODEL)

Extensions to the existing `SchemaEntry` model to support index metadata.

| New Field | Type | Description |
|-----------|------|-------------|
| `SectorTags` | string[]? | Sector labels for filtering (null for System schemas) |
| `Keywords` | string[]? | Search keywords |
| `FieldCount` | int? | Number of properties |
| `FieldNames` | string[]? | Property names |

These fields are nullable to maintain backwards compatibility with existing System and Custom schemas that don't have sector tags.

---

### SchemaIndexStatus (NEW ENUM)

| Value | Description |
|-------|-------------|
| `Active` | Available and up-to-date |
| `Deprecated` | Marked as deprecated by source, still queryable |
| `Unavailable` | Source unreachable, entry retained from last successful fetch |

---

## Relationships

```
SchemaProviderStatus 1 ──────── * SchemaIndexEntry
   (provider)                      (indexed schemas)

OrganisationSchemaPreferences 1 ── * SchemaSector (reference by ID)
   (org config)                      (enabled sectors)

SchemaIndexEntry * ──────── * SchemaSector (reference by ID)
   (sector tags)               (sector labels)

DerivedSchema * ──────── 1 SchemaIndexEntry
   (subset)                  (parent schema - by provider+uri)
```

## Validation Rules

1. **SchemaIndexEntry.Title**: Required, 1-200 characters
2. **SchemaIndexEntry.SectorTags**: At least one sector tag required
3. **SchemaIndexEntry.SourceProvider + SourceUri**: Unique composite key — duplicates rejected
4. **SchemaIndexEntry.ContentHash**: Recalculated on every refresh — if unchanged, entry not updated (reduces writes)
5. **OrganisationSchemaPreferences.EnabledSectors**: Each value must match a valid `SchemaSector.Id`
6. **DerivedSchema.IncludedFields**: Each field must exist in the parent schema's properties
7. **DerivedSchema.DerivedContent**: Must be valid JSON Schema draft 2020-12
