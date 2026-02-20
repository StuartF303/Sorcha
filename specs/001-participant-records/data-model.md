# Data Model: Published Participant Records on Register

**Branch**: `001-participant-records` | **Date**: 2026-02-20

## Entities

### 1. TransactionType Enum Extension

**File**: `src/Common/Sorcha.Register.Models/Enums/TransactionType.cs`

| Value | Name | Purpose |
|-------|------|---------|
| 0 | Control | Register governance (genesis, blueprint-publish) |
| 1 | Action | Blueprint workflow action execution |
| 2 | Docket | Block sealing record |
| **3** | **Participant** | **Published participant identity record** |

### 2. ParticipantRecord (Payload Model)

**New file**: `src/Common/Sorcha.Register.Models/ParticipantRecord.cs`

Represents the content payload of a Participant transaction. Stored as JSON within the transaction's payload field.

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| participantId | UUID (string) | Yes | Format: GUID | Immutable identity anchor, generated on first publication |
| organizationName | string | Yes | 1-200 chars | Informational, can change across versions |
| participantName | string | Yes | 1-200 chars | Informational, can change across versions |
| status | string | Yes | Enum: active, deprecated, revoked | Latest version's status is canonical |
| version | int | Yes | >= 1 | Incremented on each update, highest wins |
| addresses | array | Yes | 1-10 items | At least one address required |
| metadata | object | No | Opaque JSON | Description, links, capabilities, etc. |

### 3. ParticipantAddress (Nested Model)

**Within**: `ParticipantRecord.Addresses` array

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| walletAddress | string | Yes | 1-256 chars | Unique per register (across active participants) |
| publicKey | string | Yes | Base64-encoded | Used for field-level encryption and signature verification |
| algorithm | string | Yes | Enum: ED25519, P-256, RSA-4096 | Must match publicKey format |
| primary | bool | No | Default: false | First address is default if none marked |

### 4. ParticipantRecordStatus Enum

**New file**: `src/Common/Sorcha.Register.Models/Enums/ParticipantRecordStatus.cs`

| Value | Name | Queryable | Notes |
|-------|------|-----------|-------|
| active | Active | Default results | Normal operating state |
| deprecated | Deprecated | Excluded by default, included with flag | Transitioning, still usable |
| revoked | Revoked | Excluded from all default queries | Permanently retired |

### 5. TransactionSubmission Update

**File**: `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs`

Make `BlueprintId` and `ActionId` nullable to support non-blueprint transactions:

| Field | Current | New | Notes |
|-------|---------|-----|-------|
| BlueprintId | required string | string? | Null for Participant TXs |
| ActionId | required string | string? | "participant-publish" for Participant TXs |

## Relationships

```
Register (1) ──── (*) Transaction
                      │
                      ├── TransactionType = Participant
                      │   └── Payload = ParticipantRecord
                      │       └── Addresses[] = ParticipantAddress[]
                      │
                      ├── TransactionType = Control
                      │   └── (genesis, blueprint-publish)
                      │
                      └── TransactionType = Action
                          └── (workflow step)

Chain Linkage (PrevTxId):
  Control TX (genesis) ← Control TX (blueprint-publish) ← Participant TX v1 ← Participant TX v2
                                                        ← Participant TX v1 (different participant)
                                                        ← Action TX (workflow)
```

## State Transitions

```
                    ┌─────────┐
    First publish → │  active  │
                    └────┬────┘
                         │ version update (status=deprecated)
                    ┌────▼────────┐
                    │ deprecated  │
                    └────┬────────┘
                         │ version update (status=revoked)
                    ┌────▼─────┐
                    │ revoked  │ (terminal for default queries, but new version can re-activate)
                    └──────────┘

Note: Any status can transition to any other status via a new version.
Re-activation (revoked → active) is permitted by publishing a new version.
```

## Index Requirements

### MongoDB Indexes (per-register database)

**Existing** (no changes):
- `TxId` (unique)
- `SenderWallet`
- `TimeStamp` (descending)
- `DocketNumber`
- `MetaData.BlueprintId` + `MetaData.InstanceId` (compound)
- `PrevTxId`

**New**:
- `MetaData.TransactionType` — enables efficient filtering for participant-only queries

### Application-Level Index (Register Service)

**Participant Address Index** (in-memory + Redis cache):

| Key | Value | TTL |
|-----|-------|-----|
| `participant:addr:{registerId}:{walletAddress}` | `{ participantId, latestTxId, version, status }` | 1 hour |
| `participant:id:{registerId}:{participantId}` | `{ latestTxId, version, status, addresses[] }` | 1 hour |
| `participant:list:{registerId}` | `[ participantId1, participantId2, ... ]` | 1 hour |

**Rebuild strategy**: On cache miss, scan Participant-type transactions on the register, group by participantId, take highest version.

## Uniqueness Constraints

| Scope | Constraint | Enforced By |
|-------|-----------|-------------|
| Per register | TxId must be unique | MongoDB unique index |
| Per register | Wallet address can only belong to one active participant | Validator (check address index before accepting) |
| Per participant chain | Version must be higher than previous | Validator (compare with latest version) |
| Per participant chain | PrevTxId must match previous version's TxId | Validator (chain integrity check) |
