# Contract: Unified Transaction Submission Endpoint

**Endpoint**: `POST /api/v1/transactions/validate` (existing, no change)

## Request Model (Existing — ValidateTransactionRequest)

Already supports all transaction types. No new fields needed.

```
POST /api/v1/transactions/validate
Content-Type: application/json

{
  "transactionId": "a1b2c3...64hex",
  "registerId": "abc123def456",
  "blueprintId": "genesis",                    // or blueprint GUID, or "register-governance-v1"
  "actionId": "register-creation",             // or "blueprint-publish", or action step
  "payload": { ... },                          // JsonElement — control record, action data, etc.
  "payloadHash": "e3b0c44298fc...64hex",       // SHA-256 of canonical JSON payload
  "signatures": [
    {
      "publicKey": "base64...",
      "signatureValue": "base64...",
      "algorithm": "ED25519"
    }
  ],
  "createdAt": "2026-02-18T10:00:00Z",
  "expiresAt": null,                           // null for control/genesis
  "previousTransactionId": null,               // null for first transaction
  "priority": "High",                          // High for genesis/control, Normal for actions
  "metadata": {
    "Type": "Genesis",                         // or "Control" for blueprint publish/governance
    "RegisterName": "My Register",
    "TenantId": "tenant-123",
    "SystemWalletAddress": "addr..."
  }
}
```

## Response Model (Existing — no change)

### Success (200)
```json
{
  "isValid": true,
  "added": true,
  "transactionId": "a1b2c3...64hex",
  "registerId": "abc123def456",
  "addedAt": "2026-02-18T10:00:01Z"
}
```

### Validation Failure (400)
```json
{
  "isValid": false,
  "errors": [
    { "code": "INVALID_SIGNATURE", "message": "Signature verification failed", "field": "signatures[0]" }
  ]
}
```

### Pool Full / Duplicate (409)
```json
{
  "isValid": true,
  "added": false,
  "message": "Failed to submit transaction to unverified pool (pool full or duplicate)"
}
```

## Validation Pipeline Behaviour by Type

| Stage | Genesis | Blueprint Publish | Action | Governance |
|-------|---------|-------------------|--------|------------|
| Structure | Validate | Validate | Validate | Validate |
| Payload hash | Verify | Verify | Verify | Verify |
| Schema | SKIP | SKIP | Validate | SKIP |
| Signature | **Verify** (NEW) | **Verify** (NEW) | Verify | Verify |
| Blueprint conformance | SKIP | SKIP | Validate | SKIP |
| Governance rights | Allow (first TX) | N/A | N/A | Validate |
| Chain | Validate | Validate | Validate | Validate |
| Timing | Validate | Validate | Validate | Validate |

## Transaction Type Detection

The pipeline determines transaction type from:
1. `BlueprintId == "genesis"` → Genesis
2. `Metadata["Type"] == "Genesis"` → Genesis
3. `Metadata["Type"] == "Control"` → Control (blueprint publish, governance)
4. `BlueprintId == "register-governance-v1"` → Governance
5. Otherwise → Action

## Service Client Model (Unified)

```csharp
// Rename ActionTransactionSubmission → TransactionSubmission
public record TransactionSubmission
{
    public required string TransactionId { get; init; }
    public required string RegisterId { get; init; }
    public required string BlueprintId { get; init; }
    public required string ActionId { get; init; }
    public required JsonElement Payload { get; init; }
    public required string PayloadHash { get; init; }
    public required List<SignatureInfo> Signatures { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? PreviousTransactionId { get; init; }
    public TransactionPriority Priority { get; init; } = TransactionPriority.Normal;
    public Dictionary<string, string>? Metadata { get; init; }
}
```

Note: `ActionTransactionSubmission` already has all these fields. The rename is cosmetic — both types can coexist during migration.
