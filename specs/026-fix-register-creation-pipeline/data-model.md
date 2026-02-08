# Data Model: Register Creation Pipeline Fix

**Branch**: `026-fix-register-creation-pipeline` | **Date**: 2026-02-08

## Entity Changes

### 1. InitiateRegisterCreationRequest (Modified)

**File**: `src/Common/Sorcha.Register.Models/RegisterCreationModels.cs`

| Field | Type | Status | Notes |
|---|---|---|---|
| Name | string | Existing | 1-38 chars, required |
| Description | string? | Existing | Max 500 chars |
| TenantId | string | Existing | Required |
| Owners | List\<OwnerInfo\> | Existing | Min 1 required |
| AdditionalAdmins | List\<AdditionalAdminInfo\>? | Existing | Optional |
| Metadata | Dictionary\<string, string\>? | Existing | Optional |
| **Advertise** | **bool** | **NEW** | **Default false. Controls peer network visibility.** |

### 2. PendingRegistration (Modified)

**File**: `src/Common/Sorcha.Register.Models/RegisterCreationModels.cs`

| Field | Type | Status | Notes |
|---|---|---|---|
| RegisterId | string | Existing | 32-char hex GUID |
| ControlRecord | RegisterControlRecord | Existing | Unsigned control record |
| AttestationHashes | Dictionary\<string, byte[]\> | Existing | Keyed by "role:subject" |
| CreatedAt | DateTimeOffset | Existing | Creation timestamp |
| ExpiresAt | DateTimeOffset | Existing | 5-minute TTL |
| Nonce | string | Existing | 32-byte random |
| **Advertise** | **bool** | **NEW** | **Carried from initiate to finalize** |

### 3. DocketBuildTriggerService State (Modified)

**File**: `src/Services/Sorcha.Validator.Service/Services/DocketBuildTriggerService.cs`

| Field | Type | Status | Notes |
|---|---|---|---|
| _lastBuildTimes | ConcurrentDictionary\<string, DateTimeOffset\> | Existing | Last build time per register |
| _genesisWritten | ConcurrentDictionary\<string, bool\> | Existing | Genesis completion flag |
| **_genesisRetryCount** | **ConcurrentDictionary\<string, int\>** | **NEW** | **Tracks retry attempts per register. Max 3.** |

### 4. Transaction → TransactionModel Mapping (Fixed)

**Source**: `Sorcha.Validator.Service.Models.Transaction`
**Target**: `Sorcha.Register.Models.TransactionModel`

Complete mapping required in `WriteDocketAndTransactionsAsync()`:

| Source Field | Target Field | Transform |
|---|---|---|
| TransactionId | TxId | Direct |
| RegisterId | RegisterId | Direct |
| CreatedAt | TimeStamp | .UtcDateTime |
| Payload (JsonElement) | Payloads[0].Data | GetRawText() → UTF8 → Base64 |
| PayloadHash | Payloads[0].Hash | Direct |
| N/A | PayloadCount | 1 (count of payloads) |
| Signatures[0].PublicKey | SenderWallet | Base64 encode, or "system" |
| Signatures[0].SignatureValue | Signature | Base64 encode |
| BlueprintId | MetaData.BlueprintId | Direct |
| ActionId | MetaData.ActionId | uint.TryParse() |
| PreviousTransactionId | PrevTxId | Direct or string.Empty |
| Metadata dict | MetaData.TransactionType | Extract "Type" key → enum |

### 5. Genesis Constants (New)

**File**: `src/Common/Sorcha.Register.Models/Constants/GenesisConstants.cs` (new file)

| Constant | Value | Purpose |
|---|---|---|
| BlueprintId | "genesis" | Placeholder BlueprintId for genesis transactions |
| ActionId | "register-creation" | ActionId for genesis transactions |

## State Transitions

### Register Creation States

```
[Not Created] → InitiateAsync → [Pending in Redis, 5-min TTL]
[Pending] → FinalizeAsync → [Created in DB, height=0, genesis tx in validator mempool]
[Created, height=0] → DocketBuildTrigger → [Genesis docket written, height=0]
[Created, advertise=true] → PeerNotification → [Advertised to network]
```

### Genesis Docket Build States

```
[Monitored, no genesis] → ShouldBuild=true → BuildDocket → [Genesis docket proposed]
[Genesis proposed] → Consensus (or single-validator) → [Genesis confirmed]
[Genesis confirmed] → WriteDocketAsync → [Genesis written to Register Service]
[Write success] → _genesisWritten=true → [Complete]
[Write failure] → _genesisRetryCount++ → [Retry on next cycle, max 3]
[3 failures] → Unmonitor + log warning → [Failed, admin attention needed]
```
