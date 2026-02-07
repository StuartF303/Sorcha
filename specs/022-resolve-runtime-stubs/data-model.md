# Data Model: 022-resolve-runtime-stubs

**Date**: 2026-02-07
**Branch**: `022-resolve-runtime-stubs`

## New Entities

### ConsensusFailureRecord

Persisted diagnostic record for failed consensus rounds.

| Field | Type | Description |
|-------|------|-------------|
| `DocketId` | string | The docket that failed consensus |
| `RegisterId` | string | Register the docket belongs to |
| `FailureReason` | string | Human-readable reason for failure |
| `FailureType` | enum | `InsufficientVotes`, `MajorityRejection`, `Timeout`, `NetworkError` |
| `ProposerValidatorId` | string | Validator that proposed the docket |
| `ParticipatingValidators` | List<string> | Validators that participated |
| `VoteResults` | Dictionary<string, string> | ValidatorId → vote decision |
| `FailedAt` | DateTimeOffset | When the failure occurred |
| `RetryCount` | int | Number of retry attempts |

**Storage**: Redis Hash with TTL (30 days)
**Key pattern**: `validator:consensus:failures:{docketId}`

---

### ValidatorRegistrationTransaction

On-chain record of a validator joining the network.

| Field | Type | Description |
|-------|------|-------------|
| `ValidatorId` | string | Unique validator identifier |
| `GrpcEndpoint` | string | Validator's gRPC address |
| `PublicKey` | byte[] | Validator's signing public key |
| `RegistrationType` | enum | `Registration`, `Approval`, `Deregistration` |
| `RegisteredAt` | DateTimeOffset | Registration timestamp |
| `ApprovedBy` | List<string> | Validators that approved the registration |

**Storage**: On-chain transaction in system register

---

## Modified Entities

### PendingRegistration (migrated to Redis)

No field changes. Storage changed from `ConcurrentDictionary` to Redis Hash.

**Key pattern**: `register:pending:{registerId}`
**Serialization**: JSON
**TTL**: Based on `ExpiresAt` field

---

### WalletAccess (new repository method)

No field changes. Adding `GetAccessByIdAsync(Guid accessId)` query method to repository.

---

### KeyChain Export Format

| Field | Type | Description |
|-------|------|-------------|
| `Version` | int | Export format version (1) |
| `EncryptedData` | byte[] | AES-256-GCM encrypted keychain JSON |
| `Salt` | byte[] | PBKDF2 salt for key derivation |
| `IV` | byte[] | AES initialization vector |
| `Checksum` | string | SHA-256 of plaintext for integrity verification |

---

## State Transitions

### Validator Registration Lifecycle

```
Unregistered → Pending (registration tx submitted)
Pending → Active (approved by majority)
Active → Deregistered (deregistration tx submitted)
```

### Consensus Failure Handling

```
Proposed → Voting → Failed (insufficient votes / majority rejection / timeout)
Failed → Retried (up to max retries)
Failed → Abandoned (max retries exceeded, failure record persisted)
```
