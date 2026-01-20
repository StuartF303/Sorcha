# Research: System Schema Store

**Feature**: 013-system-schema-store
**Date**: 2026-01-20

## Research Summary

All technical unknowns have been resolved through codebase analysis and external API investigation.

---

## 1. SchemaStore.org API Integration

**Decision**: Use the SchemaStore.org catalog API at `https://www.schemastore.org/api/json/catalog.json`

**Rationale**: This is the official public API providing access to 700+ JSON schemas. It's stable, well-documented, and requires no authentication.

**API Structure**:
```json
{
  "$schema": "https://json.schemastore.org/schema-catalog.json",
  "version": 1,
  "schemas": [
    {
      "name": "package.json",
      "description": "NPM package.json file",
      "fileMatch": ["package.json"],
      "url": "https://json.schemastore.org/package.json",
      "versions": { "..." }
    }
  ]
}
```

**Key Fields**:
| Field | Type | Usage |
|-------|------|-------|
| `name` | string | Schema identifier for display/search |
| `description` | string | Brief explanation for UI |
| `fileMatch` | string[] | File patterns (optional) |
| `url` | string | Direct link to fetch schema |
| `versions` | object | Version → URL mapping (optional) |

**Search Strategy**: Client-side filtering of the catalog (no server-side search API exists). Cache the full catalog locally and filter by name/description.

**Alternatives Considered**:
- JSON Schema Store Registry: Less comprehensive, no better API
- Building custom schema aggregator: Over-engineering for MVP

---

## 2. MongoDB Storage Pattern

**Decision**: Use `MongoDocumentStore<SchemaEntry, string>` from `Sorcha.Storage.MongoDB`

**Rationale**: Existing pattern already used throughout the codebase. Provides async CRUD operations, query support, and consistent error handling.

**Existing Pattern** (from `MongoDocumentStore.cs`):
```csharp
public class MongoDocumentStore<TDocument, TId> : IDocumentStore<TDocument, TId>
{
    Task<TDocument?> GetAsync(TId id, CancellationToken ct);
    Task<IEnumerable<TDocument>> QueryAsync(Expression<Func<TDocument, bool>> filter, ...);
    Task<TDocument> InsertAsync(TDocument document, CancellationToken ct);
    Task<TDocument> UpsertAsync(TId id, TDocument document, CancellationToken ct);
    Task<bool> DeleteAsync(TId id, CancellationToken ct);
}
```

**Collection Design**:
- Collection name: `schemas`
- Index on: `identifier` (unique), `category`, `organizationId`, `status`
- Compound index for organization scoping: `{organizationId, identifier}`

**Alternatives Considered**:
- Custom repository: Unnecessary abstraction layer
- Redis-only storage: Not suitable for complex queries needed for search/filter

---

## 3. Client-Side Caching Pattern

**Decision**: Use localStorage via JSInterop following the `BrowserTokenCache` pattern

**Rationale**: Existing pattern in codebase provides encrypted storage, consistent key naming, and proper WASM interop handling.

**Existing Pattern** (from `BrowserTokenCache.cs`):
```csharp
private const string StorageKeyPrefix = "sorcha:tokens:";
private readonly IJSRuntime _jsRuntime;

await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
```

**Schema Cache Design**:
- Key format: `sorcha:schemas:{identifier}`
- Catalog cache: `sorcha:schemas:_catalog` (external catalog)
- Metadata: Store version/ETag for invalidation
- No encryption needed (schemas are not sensitive)

**Cache Invalidation Strategy**:
1. Version-based: Compare schema version on server response
2. ETag-based: Use HTTP ETag headers for external schemas
3. TTL-based: Default 24 hours for external schemas, system schemas permanent

**Alternatives Considered**:
- IndexedDB: More complex, overkill for JSON schema storage (~100KB typical)
- SessionStorage: Lost on tab close, poor for offline support

---

## 4. Existing Codebase Integration Points

**Decision**: Extend existing projects rather than create new ones

### Blueprint.Schemas (NEW project to create)
Location: `src/Common/Sorcha.Blueprint.Schemas/`
Purpose: Schema models, store interface, external provider

### Blueprint.Service (extend existing)
Location: `src/Services/Sorcha.Blueprint.Service/`
Add: `Endpoints/SchemaEndpoints.cs` for Minimal API

### UI.Core (extend existing)
Location: `src/Apps/Sorcha.UI/Sorcha.UI.Core/`
Existing: `SchemaProvider.cs` model already exists
Add: `Services/ISchemaCache.cs`, `Services/SchemaCache.cs`

**Rationale**: Follows microservices-first principle. Schema management is closely related to blueprint functionality.

---

## 5. System Schema Definition Format

**Decision**: Store system schemas as embedded JSON resources in the assembly

**Rationale**:
- Immutable by design (FR-005)
- Versioned with application releases
- No database migration needed
- Fast startup (no I/O)

**Structure**:
```
Sorcha.Blueprint.Schemas/
└── SystemSchemas/
    ├── installation.schema.json
    ├── organisation.schema.json
    ├── participant.schema.json
    └── register.schema.json
```

**Loading Strategy**:
```csharp
var assembly = Assembly.GetExecutingAssembly();
var stream = assembly.GetManifestResourceStream("Sorcha.Blueprint.Schemas.SystemSchemas.installation.schema.json");
```

**Alternatives Considered**:
- Database seeding: Requires migration, mutable
- File system: Deployment dependency, path issues

---

## 6. Authorization Pattern

**Decision**: Use existing JWT/Claims-based authorization with policy-based access

**Rationale**: Consistent with existing Sorcha authentication patterns. Tenant service already provides organization context in JWT claims.

**Implementation**:
```csharp
// Read access - any authenticated user
[Authorize]
public static async Task<Results<Ok<SchemaEntry>, NotFound>> GetSchema(...)

// Write access - administrator role required
[Authorize(Policy = "RequireAdministrator")]
public static async Task<Results<Created<SchemaEntry>, BadRequest>> CreateSchema(...)
```

**Organization Scoping**: Extract `organizationId` from JWT claims for custom schema filtering.

---

## 7. Performance Considerations

**Decision**: Implement two-tier caching (Redis server-side, localStorage client-side)

**Server-Side (Redis)**:
- Cache external schema catalog (5 minute TTL)
- Cache individual external schemas (24 hour TTL)
- System schemas: memory cache only (immutable)

**Client-Side (localStorage)**:
- All accessed schemas cached locally
- Version check on first load per session
- Offline fallback for cached schemas

**Performance Targets** (from spec):
| Operation | Target | Approach |
|-----------|--------|----------|
| System schema retrieval | <500ms | Memory cache + embedded resources |
| Cached schema access | <100ms | localStorage, no network |
| External search | <3s | Catalog cache + client filter |

---

## Remaining Questions

None - all technical decisions resolved.

---

## References

- SchemaStore.org API: https://www.schemastore.org/api/json/catalog.json
- MongoDocumentStore: `src/Common/Sorcha.Storage.MongoDB/MongoDocumentStore.cs`
- BrowserTokenCache: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/BrowserTokenCache.cs`
- SchemaProvider model: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Designer/SchemaProvider.cs`
