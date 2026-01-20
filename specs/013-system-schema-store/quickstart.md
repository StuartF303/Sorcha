# Quickstart: System Schema Store

**Feature**: 013-system-schema-store
**Date**: 2026-01-20

## Overview

This guide helps developers quickly implement and use the Schema Store feature.

---

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop running (for MongoDB)
- Solution built: `dotnet build`

---

## 1. Run the Services

```bash
# Start all services with Aspire
dotnet run --project src/Apps/Sorcha.AppHost

# Or with Docker Compose
docker-compose up -d
```

**Access Points**:
- Blueprint Service: `http://localhost:7000`
- API Gateway: `http://localhost:80`
- Scalar API Docs: `http://localhost:7000/scalar/v1`

---

## 2. Access System Schemas

System schemas are available immediately without authentication for read access.

```bash
# List all system schemas
curl http://localhost:7000/api/v1/schemas/system

# Get a specific system schema
curl http://localhost:7000/api/v1/schemas/installation
```

**Available System Schemas**:
| Identifier | Description |
|------------|-------------|
| `installation` | Sorcha system deployment |
| `organisation` | Participating organization |
| `participant` | System user with role |
| `register` | Data container with schema |

---

## 3. Search External Schemas

Search SchemaStore.org for industry-standard schemas:

```bash
# Search for schemas (requires auth)
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:7000/api/v1/schemas/external/search?query=package.json"
```

**Response**:
```json
{
  "results": [
    {
      "name": "package.json",
      "description": "NPM package.json file",
      "url": "https://json.schemastore.org/package.json",
      "source": "SchemaStore.org"
    }
  ],
  "totalCount": 1,
  "sourceStatus": {
    "SchemaStore.org": "Available"
  }
}
```

---

## 4. Import External Schema

Import a schema from an external source (requires admin role):

```bash
curl -X POST http://localhost:7000/api/v1/schemas/external/import \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://json.schemastore.org/package.json",
    "identifier": "npm-package"
  }'
```

---

## 5. Create Custom Schema

Create an organization-specific schema (requires admin role):

```bash
curl -X POST http://localhost:7000/api/v1/schemas \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "identifier": "my-custom-schema",
    "title": "My Custom Schema",
    "description": "Schema for my organization",
    "version": "1.0.0",
    "content": {
      "$schema": "https://json-schema.org/draft/2020-12/schema",
      "type": "object",
      "properties": {
        "name": { "type": "string" }
      }
    }
  }'
```

---

## 6. Client-Side Caching (Blazor WASM)

### Inject the Schema Service

```csharp
@inject ISchemaCache SchemaCache

@code {
    private SchemaContentDto? _schema;

    protected override async Task OnInitializedAsync()
    {
        // Automatically uses cache if available
        _schema = await SchemaCache.GetSchemaAsync("installation");
    }
}
```

### Check Cache Status

```csharp
// Check if working offline
var isOffline = await SchemaCache.IsOfflineAsync();

// Force cache refresh
await SchemaCache.RefreshAsync("installation");

// Clear all cached schemas
await SchemaCache.ClearCacheAsync();
```

---

## 7. Using Schemas in Blueprints

Reference schemas in your blueprints:

```json
{
  "title": "Organization Onboarding",
  "participants": [
    {
      "identifier": "admin",
      "schema": "participant"
    }
  ],
  "actions": [
    {
      "identifier": "create-org",
      "data": {
        "schema": "organisation"
      }
    }
  ]
}
```

---

## 8. API Endpoints Summary

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/schemas` | User | List schemas (filtered) |
| GET | `/schemas/system` | User | List system schemas |
| GET | `/schemas/{id}` | User | Get schema with content |
| POST | `/schemas` | Admin | Create custom schema |
| PUT | `/schemas/{id}` | Admin | Update custom schema |
| DELETE | `/schemas/{id}` | Admin | Delete custom schema |
| POST | `/schemas/{id}/deprecate` | Admin | Deprecate schema |
| POST | `/schemas/{id}/activate` | Admin | Reactivate schema |
| POST | `/schemas/{id}/publish` | Admin | Publish globally |
| GET | `/schemas/external/search` | User | Search external sources |
| POST | `/schemas/external/import` | Admin | Import external schema |

---

## 9. Testing

### Run Unit Tests

```bash
dotnet test tests/Sorcha.Blueprint.Schemas.Tests
```

### Run Integration Tests

```bash
# Requires services running
dotnet test tests/Sorcha.Blueprint.Service.IntegrationTests \
  --filter "FullyQualifiedName~Schema"
```

### Test Schema Validation

```csharp
[Fact]
public async Task ValidateData_AgainstInstallationSchema_ShouldPass()
{
    // Arrange
    var schemaStore = _serviceProvider.GetRequiredService<ISchemaStore>();
    var schema = await schemaStore.GetAsync("installation");

    var data = JsonDocument.Parse("""
        {
            "name": "My Installation",
            "publicKey": "base64key..."
        }
        """);

    // Act
    var result = schema.Content.Evaluate(data.RootElement);

    // Assert
    result.IsValid.Should().BeTrue();
}
```

---

## 10. Troubleshooting

### Schema not found
- Ensure the Blueprint service is running
- Check the schema identifier is correct (lowercase, hyphens only)
- Verify your JWT token has the correct organization claim

### External search returns empty
- Check network connectivity to SchemaStore.org
- Review the search query (minimum 2 characters)
- Check if the catalog cache has expired

### Cache not working
- Verify localStorage is available in browser
- Check browser developer tools for storage quota errors
- Clear browser storage and reload

### Permission denied
- Ensure your user has the `administrator` role for write operations
- Check the JWT token includes the correct claims
- Verify the schema belongs to your organization

---

## Next Steps

1. Review the [Data Model](./data-model.md) for entity details
2. Check the [API Contract](./contracts/schema-api.yaml) for full endpoint specs
3. Read the [Research](./research.md) for technical decisions
