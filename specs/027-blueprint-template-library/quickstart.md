# Quickstart: Blueprint Template Library & Ping-Pong Blueprint

**Date**: 2026-02-08
**Branch**: `027-blueprint-template-library`

## Prerequisites

- Docker running with all Sorcha services (`docker-compose up -d`)
- Admin authentication token

## Quick Test: Run the Ping-Pong Workflow

### 1. Verify templates are seeded

```bash
curl http://localhost/api/templates/ | jq '.[] | {id, title, category}'
```

Expected: Ping-Pong template appears with `category: "demo"`.

### 2. Create a blueprint from the template

```bash
curl -X POST http://localhost/api/templates/evaluate \
  -H "Content-Type: application/json" \
  -d '{"templateId": "ping-pong-001", "parameters": {}}'
```

### 3. Publish and create instance

```bash
# Create blueprint
curl -X POST http://localhost/api/blueprints/ \
  -H "Content-Type: application/json" \
  -d @blueprint.json

# Publish (expect cycle warning, not error)
curl -X POST http://localhost/api/blueprints/{id}/publish

# Create instance with two participants
curl -X POST http://localhost/api/instances/ \
  -H "Content-Type: application/json" \
  -d '{"blueprintId": "{id}", "participantWallets": {"ping": "wallet-a", "pong": "wallet-b"}}'
```

### 4. Execute Ping-Pong cycle

```bash
# Ping submits (counter=1)
curl -X POST http://localhost/api/instances/{instanceId}/actions/0/execute \
  -H "Content-Type: application/json" \
  -d '{"data": {"message": "Hello", "counter": 1}}'

# Pong responds (counter=2)
curl -X POST http://localhost/api/instances/{instanceId}/actions/1/execute \
  -H "Content-Type: application/json" \
  -d '{"data": {"message": "World", "counter": 2}}'

# Continue alternating...
```

## UI Access

Navigate to `http://localhost/app/templates` to browse the template library, select Ping-Pong, and create an instance via the UI.

## Key Files

| File | Purpose |
|------|---------|
| `examples/templates/ping-pong-template.json` | Ping-Pong blueprint template definition |
| `src/Services/Sorcha.Blueprint.Service/Templates/TemplateSeedingService.cs` | Startup seeding hosted service |
| `src/Services/Sorcha.Blueprint.Service/Program.cs` | Modified cycle detection (warn, not reject) |
| `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Templates.razor` | Template library UI page |
| `walkthroughs/PingPong/test-ping-pong-workflow.ps1` | End-to-end walkthrough script |
