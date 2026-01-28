# Data Model: CLI Register Commands Update

**Branch**: `016-cli-register-update` | **Date**: 2026-01-28

## Shared Models (from Sorcha.Register.Models)

These models are referenced directly from the shared library. No CLI-local copies.

### Register

| Field | Type | Description |
|-------|------|-------------|
| Id | string (32-char hex) | Unique register identifier |
| Name | string (1-38 chars) | Human-readable name |
| Height | uint | Block height (docket count) |
| Status | RegisterStatus enum | Online, Offline, Archived, etc. |
| Advertise | bool | Discoverable on peer network |
| IsFullReplica | bool | Full replication enabled |
| TenantId | string | Owning tenant/organization |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last modification timestamp |
| Votes | string? | Consensus votes |

### TransactionModel

| Field | Type | Description |
|-------|------|-------------|
| @context | string | JSON-LD context URI |
| @type | string | JSON-LD type ("Transaction") |
| @id | string | JSON-LD identifier (DID URI) |
| RegisterId | string | Owning register |
| TxId | string (64-char hex) | Transaction hash |
| PrevTxId | string (64-char hex) | Previous transaction hash |
| BlockNumber | ulong? | Docket ID |
| Version | uint | Transaction version |
| SenderWallet | string | Base58 sender address |
| RecipientsWallets | string[] | Base58 recipient addresses |
| TimeStamp | DateTime | Transaction timestamp |
| MetaData | TransactionMetaData? | Blueprint workflow metadata |
| PayloadCount | ulong | Number of payloads |
| Payloads | PayloadModel[] | Encrypted payload data |
| Signature | string | Cryptographic signature |

### Docket

| Field | Type | Description |
|-------|------|-------------|
| Id | ulong | Block height |
| RegisterId | string | Owning register |
| PreviousHash | string | Hash of previous docket |
| Hash | string | Hash of this docket |
| TransactionIds | List\<string\> | Sealed transaction IDs |
| TimeStamp | DateTime | Docket creation timestamp |
| State | DocketState enum | Init, Committed, Finalized, etc. |
| MetaData | TransactionMetaData? | Docket metadata |
| Votes | string? | Consensus votes |

### Register Creation Models

| Model | Purpose |
|-------|---------|
| InitiateRegisterCreationRequest | Phase 1 input: name, description, tenantId, owners |
| InitiateRegisterCreationResponse | Phase 1 output: registerId, attestationsToSign, expiresAt, nonce |
| AttestationToSign | Per-owner attestation data + hex hash to sign |
| AttestationSigningData | Canonical JSON: role, subject, registerId, registerName, grantedAt |
| FinalizeRegisterCreationRequest | Phase 2 input: registerId, nonce, signedAttestations |
| SignedAttestation | Signed attestation: attestationData, publicKey, signature, algorithm |
| FinalizeRegisterCreationResponse | Phase 2 output: registerId, status, genesisTransactionId, genesisDocketId |

### Enums

| Enum | Values |
|------|--------|
| RegisterStatus | Online, Offline, Archived, ... |
| DocketState | Init, Committed, Finalized, ... |
| TransactionType | data, genesis, ... |
| RegisterRole | Owner, Admin, ... |
| SignatureAlgorithm | Ed25519, EcdsaP256, ... |

## CLI-Local Models (remain in Sorcha.Cli.Models)

These models are CLI-specific and do NOT have shared equivalents.

### Wallet Models (unchanged)

- `Wallet` - Address, Name, PublicKey, Algorithm, Status, Owner, Tenant
- `CreateWalletRequest` / `CreateWalletResponse`
- `RecoverWalletRequest`
- `SignTransactionRequest` - **Updated**: add `IsPreHashed` (bool) and `DerivationPath` (string?) fields
- `SignTransactionResponse`
- `UpdateWalletRequest`

### Auth/Config Models (unchanged)

- `LoginRequest`, `TokenResponse`, `TokenCacheEntry`
- `Profile`, `CliConfiguration`

### Other Service Models (unchanged)

- `Organization`, `User`, `ServicePrincipal`
- `Peer`, `Bootstrap`

## Models to Delete from CLI

| File | Models Removed | Replaced By |
|------|----------------|-------------|
| `Models/Register.cs` | Register, CreateRegisterRequest, Transaction, SubmitTransactionRequest, SubmitTransactionResponse | Sorcha.Register.Models.Register, TransactionModel, InitiateRegisterCreationRequest, FinalizeRegisterCreationRequest, etc. |

## State Transitions

### Register Creation (Two-Phase)

```
[No Register]
  → POST /initiate → [PendingRegistration] (5-min TTL)
  → POST /finalize → [Register: Status=Online, Height=1]
  → (timeout) → [PendingRegistration expired, cleaned up]
```

### Register Status Updates

```
Online ↔ Offline ↔ Archived
```
