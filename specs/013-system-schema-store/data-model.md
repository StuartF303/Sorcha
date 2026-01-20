# Data Model: System Schema Store

**Feature**: 013-system-schema-store
**Date**: 2026-01-20

## Entity Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        SchemaEntry                               │
├─────────────────────────────────────────────────────────────────┤
│ identifier (PK)                                                  │
│ title                                                            │
│ description                                                      │
│ version                                                          │
│ category ─────────────────► SchemaCategory (enum)               │
│ source ───────────────────► SchemaSource                        │
│ status ───────────────────► SchemaStatus (enum)                 │
│ organizationId (nullable) ─► Organization (external)            │
│ isGloballyPublished                                              │
│ content (JSON)                                                   │
│ dateAdded                                                        │
│ dateModified                                                     │
│ dateDeprecated (nullable)                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## Core Entities

### SchemaEntry

The primary entity representing a JSON schema with metadata.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `identifier` | string | Yes | Unique identifier (PK). Format: lowercase, alphanumeric with hyphens |
| `title` | string | Yes | Human-readable display name |
| `description` | string | No | Detailed description of the schema's purpose |
| `version` | string | Yes | Semantic version (major.minor.patch) |
| `category` | SchemaCategory | Yes | Classification: System, External, Custom |
| `source` | SchemaSource | Yes | Origin information |
| `status` | SchemaStatus | Yes | Lifecycle state: Active, Deprecated |
| `organizationId` | string? | No | Owning organization for Custom schemas |
| `isGloballyPublished` | bool | Yes | Whether Custom schema is visible globally |
| `content` | JsonDocument | Yes | The actual JSON schema content |
| `dateAdded` | DateTimeOffset | Yes | When schema was added to store |
| `dateModified` | DateTimeOffset | Yes | Last modification timestamp |
| `dateDeprecated` | DateTimeOffset? | No | When schema was deprecated |

**Validation Rules**:
- `identifier`: 3-100 characters, lowercase alphanumeric with hyphens, unique per scope
- `title`: 1-200 characters
- `version`: Valid semantic version format
- `content`: Must be valid JSON Schema (draft 2020-12)
- System schemas: `organizationId` must be null
- Custom schemas: `organizationId` required

**Indexes**:
- Primary: `identifier` (unique for global schemas)
- Compound: `{organizationId, identifier}` (unique for custom schemas)
- `category` (for filtering)
- `status` (for filtering active schemas)

---

### SchemaCategory (Enumeration)

| Value | Description | Scope | Mutable |
|-------|-------------|-------|---------|
| `System` | Core Sorcha schemas (Installation, Organisation, etc.) | Global | No |
| `External` | Imported from external sources (SchemaStore.org) | Global | No (can deprecate) |
| `Custom` | User-defined schemas | Organization | Yes |

---

### SchemaStatus (Enumeration)

| Value | Description | Behavior |
|-------|-------------|----------|
| `Active` | Schema is available for use | Default state, fully functional |
| `Deprecated` | Schema should not be used for new work | Accessible but flagged in UI |

**State Transitions**:
```
Active → Deprecated  (admin action)
Deprecated → Active  (admin action, re-activation)
```

---

### SchemaSource (Value Object)

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | SourceType | Yes | Internal or External |
| `uri` | string? | No | Source URL for external schemas |
| `provider` | string? | No | Provider name (e.g., "SchemaStore.org") |
| `fetchedAt` | DateTimeOffset? | No | When external schema was retrieved |

**SourceType Enumeration**:
| Value | Description |
|-------|-------------|
| `Internal` | Sorcha-defined (system schemas) |
| `External` | Fetched from external source |
| `Custom` | Created by organization |

---

## System Schemas (Pre-defined)

### Installation Schema

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://sorcha.io/schemas/system/installation",
  "title": "Installation",
  "description": "Represents a Sorcha system deployment",
  "type": "object",
  "required": ["name", "publicKey"],
  "properties": {
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 200,
      "description": "Installation display name"
    },
    "description": {
      "type": "string",
      "maxLength": 2000,
      "description": "Optional description"
    },
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+(-[a-zA-Z0-9]+)?$",
      "description": "Semantic version"
    },
    "publicKey": {
      "type": "string",
      "description": "Base64-encoded public key"
    }
  }
}
```

### Organisation Schema

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://sorcha.io/schemas/system/organisation",
  "title": "Organisation",
  "description": "Represents a participating organization",
  "type": "object",
  "required": ["identifier", "name"],
  "properties": {
    "identifier": {
      "type": "string",
      "pattern": "^[a-z0-9-]+$",
      "minLength": 3,
      "maxLength": 100
    },
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 200
    },
    "description": {
      "type": "string",
      "maxLength": 2000
    },
    "status": {
      "type": "string",
      "enum": ["active", "suspended", "archived"]
    },
    "contactDetails": {
      "type": "object",
      "properties": {
        "email": { "type": "string", "format": "email" },
        "phone": { "type": "string" },
        "address": { "type": "string" }
      }
    }
  }
}
```

### Participant Schema

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://sorcha.io/schemas/system/participant",
  "title": "Participant",
  "description": "Represents an individual user in the system",
  "type": "object",
  "required": ["identifier", "displayName", "role"],
  "properties": {
    "identifier": {
      "type": "string",
      "pattern": "^[a-z0-9-]+$",
      "minLength": 3,
      "maxLength": 100
    },
    "displayName": {
      "type": "string",
      "minLength": 1,
      "maxLength": 200
    },
    "role": {
      "type": "string",
      "enum": ["administrator", "designer", "member"]
    },
    "organisationRef": {
      "type": "string",
      "description": "Reference to Organisation identifier"
    }
  }
}
```

### Register Schema

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://sorcha.io/schemas/system/register",
  "title": "Register",
  "description": "Represents a data container with schema",
  "type": "object",
  "required": ["identifier", "title"],
  "properties": {
    "identifier": {
      "type": "string",
      "pattern": "^[a-z0-9-]+$",
      "minLength": 3,
      "maxLength": 100
    },
    "title": {
      "type": "string",
      "minLength": 1,
      "maxLength": 200
    },
    "description": {
      "type": "string",
      "maxLength": 2000
    },
    "schema": {
      "type": "string",
      "description": "Reference to schema identifier"
    },
    "status": {
      "type": "string",
      "enum": ["draft", "active", "closed", "archived"]
    }
  }
}
```

---

## DTOs (API Transfer Objects)

### SchemaEntryDto (Response)

```csharp
public record SchemaEntryDto(
    string Identifier,
    string Title,
    string? Description,
    string Version,
    string Category,
    string Status,
    SchemaSourceDto Source,
    string? OrganizationId,
    bool IsGloballyPublished,
    DateTimeOffset DateAdded,
    DateTimeOffset DateModified,
    DateTimeOffset? DateDeprecated
);
```

### SchemaContentDto (Full schema with content)

```csharp
public record SchemaContentDto(
    string Identifier,
    string Title,
    string? Description,
    string Version,
    string Category,
    string Status,
    JsonDocument Content
);
```

### CreateSchemaRequest

```csharp
public record CreateSchemaRequest(
    string Identifier,
    string Title,
    string? Description,
    string Version,
    JsonDocument Content
);
```

### UpdateSchemaRequest

```csharp
public record UpdateSchemaRequest(
    string? Title,
    string? Description,
    string? Version,
    JsonDocument? Content
);
```

### SchemaListResponse

```csharp
public record SchemaListResponse(
    IReadOnlyList<SchemaEntryDto> Schemas,
    int TotalCount,
    string? NextCursor
);
```

---

## Storage Collections

### MongoDB Collection: `schemas`

```javascript
// Sample document
{
  "_id": "installation",
  "title": "Installation",
  "description": "Represents a Sorcha system deployment",
  "version": "1.0.0",
  "category": "System",
  "source": {
    "type": "Internal",
    "uri": null,
    "provider": null,
    "fetchedAt": null
  },
  "status": "Active",
  "organizationId": null,
  "isGloballyPublished": true,
  "content": { /* JSON Schema */ },
  "dateAdded": ISODate("2026-01-20T00:00:00Z"),
  "dateModified": ISODate("2026-01-20T00:00:00Z"),
  "dateDeprecated": null
}
```

### Index Definitions

```javascript
// Unique identifier for global schemas
db.schemas.createIndex({ "identifier": 1 }, { unique: true, partialFilterExpression: { "organizationId": null } })

// Unique identifier within organization
db.schemas.createIndex({ "organizationId": 1, "identifier": 1 }, { unique: true, partialFilterExpression: { "organizationId": { $ne: null } } })

// Category filter
db.schemas.createIndex({ "category": 1 })

// Status filter
db.schemas.createIndex({ "status": 1 })

// Full-text search on title and description
db.schemas.createIndex({ "title": "text", "description": "text" })
```

---

## Client Cache Structure

### localStorage Keys

| Key Pattern | Content | TTL |
|-------------|---------|-----|
| `sorcha:schemas:{id}` | SchemaContentDto JSON | Permanent (version-based invalidation) |
| `sorcha:schemas:_catalog` | External catalog JSON | 24 hours |
| `sorcha:schemas:_versions` | Map of id → version | Permanent |

### Cache Entry Format

```json
{
  "schema": { /* SchemaContentDto */ },
  "cachedAt": "2026-01-20T12:00:00Z",
  "version": "1.0.0",
  "etag": "abc123"
}
```
