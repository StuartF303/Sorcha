# API Contracts: Blueprint Template Library & Ping-Pong Blueprint

**Date**: 2026-02-08
**Branch**: `027-blueprint-template-library`

## Existing Endpoints (no changes needed)

All required endpoints already exist in the Blueprint Service. This feature wires them together.

### Template CRUD

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| GET | `/api/templates/` | List all published templates | Exists |
| GET | `/api/templates/{id}` | Get template by ID | Exists |
| POST | `/api/templates/` | Create/update template | Exists |
| DELETE | `/api/templates/{id}` | Delete template | Exists |

### Template Evaluation

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| POST | `/api/templates/evaluate` | Evaluate template → Blueprint | Exists |
| POST | `/api/templates/{id}/validate` | Validate parameters | Exists |
| GET | `/api/templates/{id}/examples/{name}` | Evaluate example | Exists |

### Blueprint Lifecycle

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| POST | `/api/blueprints/` | Create blueprint | Exists |
| GET | `/api/blueprints/{id}` | Get blueprint | Exists |
| POST | `/api/blueprints/{id}/publish` | Publish blueprint | Exists (modify cycle check) |

### Instance Management

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| POST | `/api/instances/` | Create instance | Exists |
| GET | `/api/instances/{id}` | Get instance | Exists |
| POST | `/api/instances/{id}/actions/{actionId}/execute` | Execute action | Exists |

## Modified Endpoint

### POST `/api/blueprints/{id}/publish` (modified behavior)

**Change**: Cycle detection produces warnings instead of errors.

**Current response on cycle**: `400 Bad Request` with `{ "errors": ["Circular dependency detected: Action 0 → Action 1 → Action 0"] }`

**New response on cycle**: `200 OK` with `{ "blueprintId": "...", "warnings": ["Cyclic route detected: Action 0 → Action 1 → Action 0. This blueprint will loop indefinitely unless routing conditions provide a termination path."] }`

**Blueprint metadata updated**: `metadata["hasCycles"] = "true"`

## New Endpoint (optional)

### POST `/api/templates/seed` (admin only)

**Purpose**: Manually trigger re-seeding of built-in templates.

**Request**: Empty body
**Response**: `200 OK` with `{ "seeded": 4, "skipped": 0, "errors": [] }`

**Authorization**: Admin policy
**Note**: This is a convenience endpoint. Primary seeding happens automatically at startup.

## Template-to-Instance Flow (sequence)

```
User → GET /api/templates/                          # Browse templates
User → GET /api/templates/ping-pong-001             # View template detail
User → POST /api/templates/evaluate                 # Generate blueprint JSON
       Body: { "templateId": "ping-pong-001", "parameters": {} }
       Response: { "blueprint": {...}, "success": true }
User → POST /api/blueprints/                        # Create blueprint
       Body: { ...blueprint JSON... }
       Response: 201 { "id": "bp-xxx" }
User → POST /api/blueprints/bp-xxx/publish          # Publish (with cycle warning)
       Response: 200 { "warnings": ["Cyclic route detected..."] }
User → POST /api/instances/                          # Create instance
       Body: { "blueprintId": "bp-xxx", "participantWallets": {"ping": "ws1...", "pong": "ws1..."} }
       Response: 201 { "instanceId": "inst-xxx", "currentActionIds": [0] }
User → POST /api/instances/inst-xxx/actions/0/execute   # Ping submits
       Body: { "data": { "message": "Hello", "counter": 1 } }
       Response: 200 { "nextActions": [{"actionId": "1", "participantId": "pong"}] }
User → POST /api/instances/inst-xxx/actions/1/execute   # Pong responds
       Body: { "data": { "message": "World", "counter": 2 } }
       Response: 200 { "nextActions": [{"actionId": "0", "participantId": "ping"}] }
```
