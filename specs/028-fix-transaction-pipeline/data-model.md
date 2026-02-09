# Data Model: Fix Transaction Submission Pipeline

**Date**: 2026-02-09
**Feature**: 028-fix-transaction-pipeline

## Overview

This feature modifies the transaction flow between three services. No new entities are created — existing models are reused with new mapping paths.

## Transaction Data Flow

```
BuiltTransaction (Blueprint Service)
  ├── TransactionData: byte[] (JSON payload bytes)
  ├── TxId: string (SHA-256 hex, 64 chars)
  ├── SenderWallet: string (wallet address)
  ├── Signature: byte[] (raw signature bytes)
  └── Metadata: Dictionary<string, object>
        ├── blueprintId: string
        ├── instanceId: string
        └── actionId: string (note: stored as string)

        ↓ NEW: Map to ValidateTransactionRequest

ValidateTransactionRequest (Validator Service input)
  ├── TransactionId: string ← TxId
  ├── RegisterId: string ← RegisterId
  ├── BlueprintId: string ← Metadata["blueprintId"]
  ├── ActionId: string ← Metadata["actionId"]
  ├── Payload: JsonElement ← Deserialize(TransactionData)
  ├── PayloadHash: string ← TxId (SHA-256 of TransactionData)
  ├── Signatures: List<SignatureRequest>
  │     └── { PublicKey (base64), SignatureValue (base64), Algorithm }
  ├── CreatedAt: DateTimeOffset ← DateTimeOffset.UtcNow
  ├── ExpiresAt: DateTimeOffset? ← null (no expiry for action txs)
  ├── Priority: TransactionPriority ← Normal
  └── Metadata: Dictionary<string, string>?
        ├── "instanceId" ← Metadata["instanceId"]
        └── "Type" ← "Action"

        ↓ EXISTING: Validator validates, adds to mempool, builds docket

Transaction (Validator mempool, Redis)
  ├── TransactionId, RegisterId, BlueprintId, ActionId
  ├── Payload, PayloadHash, Signatures
  ├── CreatedAt, Priority, AddedToPoolAt, RetryCount
  └── PreviousTransactionId, Metadata

        ↓ EXISTING: DocketSerializer.ToRegisterModel() in docket write-back

TransactionModel (Register Service, MongoDB)
  ├── TxId, RegisterId, DocketNumber (set during write-back)
  ├── SenderWallet, Signature (base64), TimeStamp
  ├── MetaData: { BlueprintId, InstanceId, ActionId }
  ├── Payloads: PayloadModel[], PayloadCount
  └── PrevTxId, Version
```

## Key Mappings

### BuiltTransaction → ValidateTransactionRequest (NEW)

| Source (BuiltTransaction) | Target (ValidateTransactionRequest) | Transformation |
|---------------------------|--------------------------------------|----------------|
| `TxId` | `TransactionId` | Direct copy |
| `RegisterId` | `RegisterId` | Direct copy |
| `Metadata["blueprintId"]` | `BlueprintId` | Cast to string |
| `Metadata["actionId"]` | `ActionId` | Cast to string |
| `TransactionData` | `Payload` | `JsonSerializer.Deserialize<JsonElement>(TransactionData)` |
| `TxId` | `PayloadHash` | Direct copy (TxId IS the SHA-256 hash) |
| `Signature` + wallet public key | `Signatures[0]` | Base64-encode both, set Algorithm from wallet |
| `DateTime.UtcNow` | `CreatedAt` | New DateTimeOffset |
| (constant) | `Priority` | `TransactionPriority.Normal` |
| `Metadata["instanceId"]` + "Action" | `Metadata` | `{ "instanceId": "...", "Type": "Action" }` |

### Transaction → TransactionModel (EXISTING, in DocketSerializer.ToRegisterModel)

Already implemented. Maps Validator's `Transaction` to Register's `TransactionModel` during docket write-back. The `DocketNumber` is set by the Register Service's docket write endpoint.

## State Transitions

### Transaction Lifecycle

```
[Created by Blueprint Service]
        │
        ▼
   ┌─────────┐      Validator rejects
   │ Pending  │ ───────────────────────► [Discarded / Error returned]
   │ (mempool)│
   └────┬─────┘
        │ Docket built + consensus
        ▼
   ┌──────────┐
   │ Confirmed│  Written to Register DB with DocketNumber
   │ (sealed) │  "transaction:confirmed" event published
   └──────────┘
```

### Register Monitoring Lifecycle

```
[Genesis docket created]
        │
        ▼
   ┌───────────┐
   │ Monitored │ ← RegisterForMonitoring() called on genesis
   │           │ ← Also called when action tx enters mempool
   └─────┬─────┘
         │ DocketBuildTriggerService polls
         │ When mempool has transactions → build docket
         ▼
   [Docket cycle repeats]
```

## Modified Interfaces

### IValidatorServiceClient (add method)

```
SubmitTransactionAsync(ActionTransactionSubmission request, CancellationToken)
  → Task<TransactionSubmissionResult>
```

### ActionTransactionSubmission (new record)

```
TransactionId: string (required)
RegisterId: string (required)
BlueprintId: string (required)
ActionId: string (required)
Payload: JsonElement (required)
PayloadHash: string (required)
Signatures: List<SignatureInfo> (required)
CreatedAt: DateTimeOffset (required)
Metadata: Dictionary<string, string>? (optional)
```

### TransactionSubmissionResult (new record)

```
Success: bool
TransactionId: string
RegisterId: string
AddedAt: DateTimeOffset?
ErrorMessage: string?
ErrorCode: string? (e.g., "VALIDATION_FAILED", "MEMPOOL_FULL", "DUPLICATE")
```

## Existing Models (Unchanged)

- `TransactionModel` (Register.Models) — no changes
- `Transaction` (Validator.Service.Models) — no changes
- `Docket` (Validator.Service.Models) — no changes
- `DocketModel` (ServiceClients) — no changes
- `ValidateTransactionRequest` (Validator endpoint) — no changes (reused as-is)
