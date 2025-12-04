# Blueprint Service Quickstart

**Created**: 2025-12-04

## Prerequisites

- .NET 10 SDK
- Docker (for Redis)
- Running instances of:
  - Register Service
  - Wallet Service
  - Tenant Service (for auth)

## Running Locally

### 1. Start Dependencies via Aspire

```bash
cd src/Apps/Sorcha.AppHost
dotnet run
```

This starts all services including Blueprint Service at `http://localhost:5180`.

### 2. Run Blueprint Service Standalone

```bash
cd src/Services/Sorcha.Blueprint.Service
dotnet run
```

**Endpoints**:
- API: `http://localhost:5180`
- OpenAPI: `http://localhost:5180/openapi/v1.json`
- Scalar UI: `http://localhost:5180/scalar`
- Health: `http://localhost:5180/health`

## Quick Test: Create and Execute a Blueprint

### Step 1: Create a Blueprint

```bash
curl -X POST http://localhost:5180/api/blueprints \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "title": "Simple Approval",
    "description": "Two-party approval workflow",
    "participants": [
      { "id": "applicant", "name": "Applicant" },
      { "id": "approver", "name": "Approver" }
    ],
    "actions": [
      {
        "id": 1,
        "title": "Submit Request",
        "sender": "applicant",
        "isStartingAction": true,
        "dataSchema": {
          "type": "object",
          "required": ["amount", "reason"],
          "properties": {
            "amount": { "type": "number", "minimum": 0 },
            "reason": { "type": "string", "minLength": 10 }
          }
        },
        "disclosures": [
          { "participantId": "approver", "paths": ["/*"] }
        ],
        "routes": [
          { "id": "default", "nextActionIds": [2], "isDefault": true }
        ]
      },
      {
        "id": 2,
        "title": "Approve Request",
        "sender": "approver",
        "dataSchema": {
          "type": "object",
          "required": ["approved"],
          "properties": {
            "approved": { "type": "boolean" },
            "comments": { "type": "string" }
          }
        },
        "routes": [
          { "id": "approved", "nextActionIds": [], "condition": { "==": [{ "var": "approved" }, true] } },
          { "id": "rejected", "nextActionIds": [1], "condition": { "==": [{ "var": "approved" }, false] } }
        ],
        "rejectionConfig": {
          "targetActionId": 1,
          "requireReason": true
        }
      }
    ],
    "registerId": "your-register-id"
  }'
```

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Simple Approval",
  "version": 1,
  ...
}
```

### Step 2: Create an Instance

```bash
curl -X POST http://localhost:5180/api/blueprints/{blueprintId}/instances \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "registerId": "your-register-id",
    "participantWallets": {
      "applicant": "ws1abc123...",
      "approver": "ws1def456..."
    }
  }'
```

**Response**:
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "blueprintId": "550e8400-e29b-41d4-a716-446655440000",
  "state": "Active",
  "currentActionIds": [1],
  ...
}
```

### Step 3: Execute First Action (Applicant)

```bash
curl -X POST http://localhost:5180/api/instances/{instanceId}/actions/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Delegation-Token: $DELEGATION_TOKEN" \
  -d '{
    "data": {
      "amount": 5000,
      "reason": "Equipment purchase for Q1 project"
    }
  }'
```

**Response**:
```json
{
  "transactionId": "abc123...",
  "nextActions": [
    {
      "actionId": 2,
      "actionTitle": "Approve Request",
      "participantId": "approver"
    }
  ],
  "isComplete": false
}
```

### Step 4: Execute Second Action (Approver)

```bash
curl -X POST http://localhost:5180/api/instances/{instanceId}/actions/2 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Delegation-Token: $DELEGATION_TOKEN" \
  -d '{
    "data": {
      "approved": true,
      "comments": "Approved - within budget"
    }
  }'
```

**Response**:
```json
{
  "transactionId": "def456...",
  "nextActions": [],
  "isComplete": true
}
```

### Alternative: Reject the Request

```bash
curl -X POST http://localhost:5180/api/instances/{instanceId}/actions/2/reject \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Delegation-Token: $DELEGATION_TOKEN" \
  -d '{
    "reason": "Amount exceeds quarterly budget",
    "fieldErrors": {
      "amount": "Please reduce to under $3000"
    }
  }'
```

**Response**:
```json
{
  "transactionId": "ghi789...",
  "targetAction": {
    "actionId": 1,
    "actionTitle": "Submit Request",
    "participantId": "applicant"
  }
}
```

## Key Concepts

### Delegation Token

The `X-Delegation-Token` header is required for action execution. It grants the Blueprint Service permission to decrypt payloads on behalf of the participant.

Obtain this token from the Tenant Service during authentication.

### State Reconstruction

When executing an action, the service:
1. Fetches prior transactions from Register
2. Decrypts payloads using delegation token
3. Accumulates state from prior actions
4. Evaluates routing conditions against accumulated state

### JSON Logic Routing

Routes use JSON Logic for conditional evaluation:

```json
{
  "routes": [
    {
      "id": "high_value",
      "nextActionIds": [3],
      "condition": { ">": [{ "var": "amount" }, 10000] }
    },
    {
      "id": "normal",
      "nextActionIds": [2],
      "isDefault": true
    }
  ]
}
```

### Parallel Branches

For multi-party approval, use multiple `nextActionIds`:

```json
{
  "routes": [
    {
      "id": "parallel_review",
      "nextActionIds": [2, 3],
      "condition": { ">": [{ "var": "amount" }, 5000] }
    }
  ]
}
```

## SignalR Real-Time Notifications

Connect to receive action notifications:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/actions", {
    accessTokenFactory: () => token
  })
  .build();

connection.on("ActionAvailable", (instanceId, actionId, actionTitle) => {
  console.log(`New action available: ${actionTitle}`);
});

connection.on("WorkflowCompleted", (instanceId) => {
  console.log(`Workflow ${instanceId} completed`);
});

await connection.start();
await connection.invoke("JoinInstance", instanceId);
```

## Running Tests

```bash
# Unit tests
dotnet test tests/Sorcha.Blueprint.Service.Tests --filter Category=Unit

# Integration tests
dotnet test tests/Sorcha.Blueprint.Service.Tests --filter Category=Integration

# All tests with coverage
dotnet test tests/Sorcha.Blueprint.Service.Tests --collect:"XPlat Code Coverage"
```

## Troubleshooting

### "Delegation token invalid"
- Token may be expired - re-authenticate with Tenant Service
- Token may not have decrypt permissions for the participant

### "Cannot reconstruct state"
- Prior transactions may not be accessible
- Check Register Service connectivity
- Verify participant has access to required payloads

### "Routing evaluation failed"
- JSON Logic expression may reference missing data
- Check accumulated state via GET `/api/instances/{id}/state`
- Verify `requiredPriorActions` is correctly specified

### SignalR not connecting
- Check Redis is running for backplane
- Verify CORS configuration for your client origin
- Check authentication token is valid
