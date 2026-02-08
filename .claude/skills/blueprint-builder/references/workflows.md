# Blueprint Publishing & Execution Workflows

## Publishing a Blueprint

### Step 1: Create Draft Blueprint
```
POST /api/blueprints/
Content-Type: application/json
Authorization: Bearer <token>

{
  "id": "my-blueprint",
  "title": "My Workflow",
  "description": "Description of the workflow",
  ...full blueprint JSON...
}
```

### Step 2: Publish Blueprint
```
POST /api/blueprints/{id}/publish
Authorization: Bearer <token>
```

**Response (success, no cycles):**
```json
{ "blueprintId": "...", "version": 1, "publishedAt": "..." }
```

**Response (success, with cycles):**
```json
{
  "blueprintId": "...",
  "version": 1,
  "publishedAt": "...",
  "warnings": ["Cyclic route detected: ..."]
}
```

**Response (validation error):**
```json
{ "errors": ["Action 'Submit' references unknown participant 'admin'"] }
```

### Step 3: Create Instance
```
POST /api/instances/
Content-Type: application/json
Authorization: Bearer <token>

{
  "blueprintId": "my-blueprint",
  "registerId": "register-001",
  "tenantId": "optional-tenant-id",
  "metadata": { "key": "value" }
}
```

### Step 4: Execute Actions
```
POST /api/instances/{instanceId}/actions/{actionId}/execute
Authorization: Bearer <token>
X-Delegation-Token: <delegation-token>

{
  "blueprintId": "my-blueprint",
  "actionId": "0",
  "instanceId": "instance-id",
  "senderWallet": "wallet-address",
  "registerAddress": "register-address",
  "payloadData": {
    "message": "Hello",
    "counter": 1
  }
}
```

**Response includes `nextActions`** — the routing result telling which action(s) are next.

## Template Publishing Flow

### Fixed Template (No Parameters)
1. Template JSON file has `parameterSchema: null`
2. `POST /api/templates/evaluate` with template ID → returns raw blueprint JSON
3. `POST /api/blueprints/` with returned blueprint
4. `POST /api/blueprints/{id}/publish`
5. `POST /api/instances/` with participant mappings

### Parameterized Template
1. Template JSON has `parameterSchema` with JSON Schema
2. `POST /api/templates/evaluate` with template ID + parameters
3. JSON-e engine evaluates template with parameters → produces blueprint JSON
4. Continue with publish + instance creation

## Template Seeding

Templates are auto-seeded at startup by `TemplateSeedingService`:
- Reads JSON files from `examples/templates/` directory
- Checks if template ID already exists (idempotent)
- Logs seeded/skipped/error counts

Manual re-seed: `POST /api/templates/seed` (requires AdminPolicy)

## Cycle Detection Details

`ValidateBlueprint()` in Program.cs:
1. Runs DFS cycle detection on action routes
2. Cycles produce **warnings** (not errors) — allows publishing
3. Sets `metadata["hasCycles"] = "true"` on the blueprint
4. Other validation errors (missing participants, invalid refs) are still hard errors

`PublishResult` record:
```csharp
public record PublishResult
{
    public bool IsSuccess { get; init; }
    public PublishedBlueprint? PublishedBlueprint { get; init; }
    public string[] Errors { get; init; } = [];
    public string[] Warnings { get; init; } = [];
}
```

## Engine Pipeline

Each action submission goes through 4 steps:

1. **Validate**: Check payload against `dataSchemas` (JSON Schema validation)
2. **Calculate**: Run `calculations` (JSON Logic) to produce computed values
3. **Route**: Evaluate `routes` to determine next action(s)
4. **Disclose**: Apply `disclosures` to control data visibility

## Walkthrough Script Pattern

See `walkthroughs/PingPong/test-ping-pong-workflow.ps1` for a complete example:
- Authenticates via `/api/auth/login`
- Loads template JSON from file
- Creates and publishes blueprint (handles cycle warnings)
- Creates instance with participant wallet mappings
- Executes N round-trips of action submissions
- Verifies payload integrity

## DI Registration for Template Seeding

```csharp
// Dual registration: Singleton + HostedService factory
builder.Services.AddSingleton<TemplateSeedingService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TemplateSeedingService>());
```

This allows both:
- Automatic startup seeding (via IHostedService)
- Manual injection into seed endpoint
