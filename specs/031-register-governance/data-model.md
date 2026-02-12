# Data Model: Register Governance

**Branch**: `031-register-governance` | **Date**: 2026-02-11

## Entity Overview

```
┌──────────────────────┐     ┌──────────────────────────┐
│    TransactionType    │     │    SorchaDidIdentifier    │
│  (enum, modified)     │     │  (new value object)       │
│  Control = 0          │     │  Type: Wallet | Register  │
│  Action = 1           │     │  Locator: string          │
│  Docket = 2           │     │  Parse() / ToString()     │
└──────────────────────┘     └──────────────────────────┘

┌──────────────────────────────────┐
│     RegisterControlRecord        │
│     (existing, evolved)          │
├──────────────────────────────────┤
│  RegisterId: string (32 hex)     │
│  Name: string (1-38)             │
│  Description: string? (0-500)    │
│  TenantId: string                │
│  CreatedAt: DateTimeOffset       │
│  Attestations: List<Attest> 1-25 │ ← Cap increased from 10 to 25
│  Metadata: Dict<str,str>?        │
├──────────────────────────────────┤
│  HasOwnerAttestation(): bool     │
│  GetSubjectsWithRole(): IEnum    │
│  GetVotingMembers(): IEnum       │ ← New: Owner + Admin only
│  GetQuorumThreshold(): int       │ ← New: >50% of voting members
└──────────────────────────────────┘

┌──────────────────────────────────┐
│     RegisterAttestation          │
│     (existing, DID updated)      │
├──────────────────────────────────┤
│  Role: RegisterRole              │
│  Subject: string (DID)           │ ← Now `did:sorcha:w:*` or `did:sorcha:r:*:t:*`
│  PublicKey: string (Base64)      │
│  Signature: string (Base64)      │
│  Algorithm: SignatureAlgorithm   │
│  GrantedAt: DateTimeOffset       │
└──────────────────────────────────┘

┌──────────────────────────────────┐
│     GovernanceOperation          │
│     (new metadata entity)        │
├──────────────────────────────────┤
│  OperationType: GovernanceOp     │ ← Add, Remove, Transfer
│  ProposerDid: string             │
│  TargetDid: string               │
│  TargetRole: RegisterRole        │
│  ApprovalSignatures: List<Sig>   │
│  ProposedAt: DateTimeOffset      │
│  ExpiresAt: DateTimeOffset       │ ← ProposedAt + 7 days
│  Status: ProposalStatus          │ ← Pending, Approved, Rejected, Expired
└──────────────────────────────────┘

┌──────────────────────────────────┐
│     ControlTransactionPayload    │
│     (new, wraps roster + op)     │
├──────────────────────────────────┤
│  Version: int                    │ ← Payload schema version
│  Roster: RegisterControlRecord   │ ← Full current roster
│  Operation: GovernanceOperation? │ ← null for genesis
└──────────────────────────────────┘
```

## Enums

### TransactionType (modified)

| Value | Name    | Description                              |
|-------|---------|------------------------------------------|
| 0     | Control | Register governance (genesis + admin ops) |
| 1     | Action  | Blueprint workflow execution              |
| 2     | Docket  | Block sealing                             |

### RegisterRole (existing, unchanged)

| Value | Name     | Voting | Description                    |
|-------|----------|--------|--------------------------------|
| 0     | Owner    | Yes    | Full control, ultimate authority|
| 1     | Admin    | Yes    | Participates in quorum votes   |
| 2     | Auditor  | No     | Read-only, full history access |
| 3     | Designer | No     | Can modify workflows           |

### GovernanceOperationType (new)

| Value | Name     | Description                            |
|-------|----------|----------------------------------------|
| 0     | Add      | Add member to roster                   |
| 1     | Remove   | Remove member from roster              |
| 2     | Transfer | Transfer ownership to existing admin   |

### ProposalStatus (new)

| Value | Name     | Description                            |
|-------|----------|----------------------------------------|
| 0     | Pending  | Awaiting quorum / acceptance           |
| 1     | Approved | Quorum reached, awaiting acceptance    |
| 2     | Rejected | Quorum blocked or target declined      |
| 3     | Expired  | 7-day timeout reached                  |
| 4     | Recorded | Control transaction written            |

## DID Format

### Wallet DID

```
did:sorcha:w:{walletAddress}
```

- **Resolution**: `IWalletServiceClient.GetWalletAsync(walletAddress)` → `WalletInfo.PublicKey`
- **Scope**: Local instance only (requires Wallet Service access)
- **Example**: `did:sorcha:w:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa`

### Register DID

```
did:sorcha:r:{registerId}:t:{transactionId}
```

- **Resolution**: `IRegisterServiceClient.GetTransactionAsync(registerId, txId)` → payload public key
- **Scope**: Any peer with register replica
- **Example**: `did:sorcha:r:a1b2c3d4e5f6:t:0123456789abcdef...`

### Parsing Rules

| Component    | Validation                                  |
|-------------|---------------------------------------------|
| Prefix      | Must start with `did:sorcha:`               |
| Type        | `w` (wallet) or `r` (register)             |
| Wallet addr | Non-empty, valid Base58                     |
| Register ID | 32-character hex string                     |
| TX ID       | 64-character hex string                     |

## State Transitions

### Governance Proposal Lifecycle

```
                    ┌──── Quorum blocked ────┐
                    ▼                        │
  Proposed ──► Pending ──► Approved ──► Recorded
                 │                        ▲
                 │           Target       │
                 │           accepts ─────┘
                 │
                 ├──► Rejected (target declines)
                 │
                 └──► Expired (7-day timeout)
```

### Admin Roster Transitions

```
Genesis:    [] → [Owner(A)]
Add:        [Owner(A)] → [Owner(A), Admin(B)]
Add:        [Owner(A), Admin(B)] → [Owner(A), Admin(B), Auditor(C)]
Remove:     [Owner(A), Admin(B), Auditor(C)] → [Owner(A), Auditor(C)]
Transfer:   [Owner(A), Auditor(C)] → [Owner(C), Admin(A)]
            (C promoted from Auditor to Owner, A demoted to Admin)
```

Note on Transfer: Target must be an existing Admin (not Auditor/Designer). The example above assumes C was first promoted to Admin, then transferred ownership.

## Quorum Calculation

```
VotingMembers = Roster.Where(r => r.Role == Owner || r.Role == Admin)

For Add:
  m = VotingMembers.Count
  quorum = floor(m / 2) + 1    // >50%

For Remove:
  m = VotingMembers.Count - 1  // Exclude target
  quorum = floor(m / 2) + 1    // >50% of adjusted pool

For Transfer:
  No quorum — Owner signs + target accepts

Owner Override:
  Any Add/Remove operation where proposer is Owner bypasses quorum
```

## Relationships

| Entity               | Relates To             | Cardinality | Description                               |
|---------------------|------------------------|-------------|-------------------------------------------|
| Register            | ControlTransaction     | 1:N         | Register has many Control TXs over time   |
| ControlTransaction  | ControlPayload         | 1:1         | Each Control TX has exactly one payload   |
| ControlPayload      | RegisterControlRecord  | 1:1         | Payload wraps full roster snapshot        |
| ControlPayload      | GovernanceOperation    | 1:0..1      | Genesis has no operation; others have one  |
| RegisterControlRecord | RegisterAttestation  | 1:1..25     | Roster has 1-25 attestation entries       |
| RegisterAttestation | SorchaDidIdentifier    | 1:1         | Each attestation has one DID subject      |

## Validation Rules

| Entity                | Rule                                                    |
|----------------------|----------------------------------------------------------|
| RegisterControlRecord | Exactly 1 Owner attestation                             |
| RegisterControlRecord | Max 25 total attestations                               |
| RegisterControlRecord | No duplicate DIDs in roster                             |
| RegisterAttestation   | DID must be well-formed (`did:sorcha:w:*` or `r:*:t:*`)|
| RegisterAttestation   | PublicKey must be valid Base64                           |
| RegisterAttestation   | Signature must verify against PublicKey                  |
| GovernanceOperation   | Transfer proposer must be Owner                         |
| GovernanceOperation   | Transfer target must be existing Admin                  |
| GovernanceOperation   | Remove target must exist in current roster              |
| GovernanceOperation   | Add target must NOT exist in current roster             |
| GovernanceOperation   | ExpiresAt = ProposedAt + 7 days                        |
| ControlTransaction    | Full roster included (not delta)                        |
| ControlTransaction    | Quorum met or Owner override for Add/Remove             |
