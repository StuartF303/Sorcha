# Data Model: Validator Engine - Schema & Chain Validation

**Branch**: `020-validator-engine-validation` | **Date**: 2026-02-06

## Modified Entities

### Transaction (Validator Service Model)

**Location**: `src/Services/Sorcha.Validator.Service/Models/Transaction.cs`

**Changes**: Add `PreviousTransactionId` property

| Field | Type | Required | Change | Description |
|-------|------|----------|--------|-------------|
| TransactionId | string | yes | existing | Unique transaction identifier |
| RegisterId | string | yes | existing | Target register |
| BlueprintId | string | yes | existing | Blueprint definition ID |
| ActionId | string | yes | existing | Action within blueprint |
| Payload | JsonElement | yes | existing | Action-specific data (validated against schema) |
| PayloadHash | string | yes | existing | SHA256 hash of payload |
| **PreviousTransactionId** | **string?** | **no** | **NEW** | **Previous transaction ID for chain linkage (null = genesis)** |
| Signatures | List\<Signature\> | yes | existing | Authorization signatures |
| CreatedAt | DateTimeOffset | yes | existing | Creation timestamp |
| ExpiresAt | DateTimeOffset? | no | existing | TTL for mempool eviction |
| Priority | TransactionPriority | no | existing | Mempool ordering |
| Metadata | Dictionary\<string, string\> | no | existing | Extensible key-value metadata |

**Validation rules**:
- `PreviousTransactionId` when present must be non-empty string
- Empty string treated as null (no previous transaction)

### ValidationEngine (Constructor Change)

**Location**: `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs`

**Changes**: Add `IRegisterServiceClient` dependency

| Parameter | Type | Change | Description |
|-----------|------|--------|-------------|
| config | IOptions\<ValidationEngineConfiguration\> | existing | Configuration |
| blueprintCache | IBlueprintCache | existing | Blueprint retrieval |
| hashProvider | IHashProvider | existing | Hash computation |
| cryptoModule | ICryptoModule | existing | Signature verification |
| **registerClient** | **IRegisterServiceClient** | **NEW** | **Register Service for chain validation** |
| logger | ILogger\<ValidationEngine\> | existing | Logging |

## Existing Entities (Referenced, Not Modified)

### Blueprint Action (Blueprint.Models)

| Field | Type | Relevance |
|-------|------|-----------|
| Id | int | Matched via `int.TryParse(transaction.ActionId)` |
| DataSchemas | IEnumerable\<JsonDocument\>? | JSON Schema definitions to validate payload against |

### Docket (Validator Service Model)

| Field | Type | Relevance |
|-------|------|-----------|
| DocketId | string | Unique docket identifier |
| RegisterId | string | Register membership |
| DocketNumber | long | Sequential number (0 = genesis) |
| PreviousHash | string? | Hash of previous docket (null for genesis) |
| DocketHash | string | This docket's hash |

### DocketModel (ServiceClients)

| Field | Type | Relevance |
|-------|------|-----------|
| DocketNumber | long | Sequential numbering for gap detection |
| PreviousHash | string? | Chain linkage verification |
| DocketHash | string | Hash integrity verification |

### ValidationEngineError (Existing)

| Field | Type | Relevance |
|-------|------|-----------|
| Code | string | Error identifier (e.g., VAL_SCHEMA_004) |
| Message | string | Human-readable description |
| Category | ValidationErrorCategory | Schema, Chain, Blueprint, etc. |
| Field | string? | JSON path or field name |
| IsFatal | bool | Whether error is permanently fatal vs retryable |
| Details | Dictionary\<string, object\>? | Additional context |

## New Error Codes

### Schema Validation Errors

| Code | Category | Fatal | Description |
|------|----------|-------|-------------|
| VAL_SCHEMA_004 | Schema | yes | Payload does not conform to action schema |
| VAL_SCHEMA_005 | Blueprint | yes | Action schema is malformed and cannot be parsed |
| VAL_SCHEMA_006 | Schema | yes | Payload fails multiple schema validations |

### Chain Validation Errors

| Code | Category | Fatal | Description |
|------|----------|-------|-------------|
| VAL_CHAIN_001 | Chain | yes | Previous transaction not found in register |
| VAL_CHAIN_002 | Chain | yes | Previous transaction belongs to different register |
| VAL_CHAIN_003 | Chain | yes | Docket chain gap detected (non-sequential numbering) |
| VAL_CHAIN_004 | Chain | yes | Docket hash mismatch (PreviousHash != predecessor's DocketHash) |
| VAL_CHAIN_005 | Chain | no | Potential fork detected (duplicate predecessor reference) |
| VAL_CHAIN_TRANSIENT | Chain | no | Register Service unavailable (retryable) |

## State Transitions

No new state transitions. The existing validation pipeline flow remains:

```
Transaction submitted → Structure → Hash → Schema → Signatures → Chain → Timing → Result
                                            ↑ NEW              ↑ NEW
```

Schema and Chain validation are already part of the pipeline — they just return success unconditionally today.
