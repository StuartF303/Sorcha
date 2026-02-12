# Data Model: Verifiable Credentials & eIDAS-Aligned Attestation System

**Branch**: `031-verifiable-credentials` | **Date**: 2026-02-12

## Entity Relationship Overview

```
┌──────────────┐         ┌─────────────────────┐
│  Blueprint   │────────▶│      Action          │
│              │  1:N    │                      │
└──────────────┘         │  CredentialReqs[]    │──┐
                         │  IssuanceConfig?     │  │
                         └─────────────────────┘  │
                                                   │
                         ┌─────────────────────┐  │
                         │CredentialRequirement │◀─┘
                         │                      │  0:N per Action
                         │  Type                │
                         │  AcceptedIssuers[]   │
                         │  RequiredClaims[]    │
                         │  RevocationPolicy    │
                         └─────────────────────┘

┌──────────────┐         ┌─────────────────────┐
│ Participant  │────────▶│VerifiableCredential  │
│              │  1:N    │  (stored in wallet)  │
│ DidUri       │         │                      │
│ WalletAddr   │         │  CredentialId        │
└──────────────┘         │  Issuer (DID)        │
                         │  Subject (DID)       │
                         │  Type                │
                         │  Claims{}            │
                         │  IssuedAt            │
                         │  ExpiresAt           │
                         │  RawToken (SD-JWT)   │
                         │  Status              │
                         └────────┬────────────┘
                                  │
                         ┌────────▼────────────┐
                         │CredentialRevocation  │
                         │                      │
                         │  CredentialId        │
                         │  RevokedBy (DID)     │
                         │  RevokedAt           │
                         │  Reason              │
                         │  LedgerTxId          │
                         └─────────────────────┘

┌──────────────────────┐
│CredentialIssuanceConf│  0:1 per Action
│                      │
│  CredentialType      │
│  ClaimMappings[]     │
│  RecipientPartId     │
│  ExpiryDuration      │
│  RegisterId?         │  (optional: record on register)
│  Disclosable[]       │
└──────────────────────┘

┌──────────────────────┐
│CredentialPresentation│  submitted at action time
│                      │
│  CredentialId        │
│  DisclosedClaims{}   │
│  KeyBindingProof?    │
│  RawPresentation     │
└──────────────────────┘

┌──────────────────────┐
│  CredentialRegister  │  uses existing Register infrastructure
│                      │
│  RegisterId          │
│  IssuerDid           │
│  CredentialType      │
│  Transactions[]      │  (issuance + revocation events)
└──────────────────────┘
```

## Entity Definitions

### CredentialRequirement

Specifies what credential(s) a participant must present to execute an action.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Type | string | Yes | Credential type identifier (e.g., "LicenseCredential", "IdentityAttestation") |
| AcceptedIssuers | string[] | No | List of accepted issuer DIDs or wallet addresses. Empty = any issuer accepted |
| RequiredClaims | ClaimConstraint[] | No | Claims that must be disclosed and their value constraints |
| RevocationCheckPolicy | enum | Yes | `FailClosed` (default) or `FailOpen` |
| Description | string | No | Human-readable description of what credential is needed (for UI) |

**Validation Rules:**
- Type must be non-empty, max 200 characters
- AcceptedIssuers, if provided, must be valid DID URIs or wallet addresses
- At least Type is required; claims and issuers are optional filters
- RevocationCheckPolicy defaults to FailClosed

### ClaimConstraint

Defines a required claim and optional value constraint within a CredentialRequirement.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| ClaimName | string | Yes | The claim key (e.g., "licenseType", "skillLevel") |
| ExpectedValue | object | No | Exact value match. Null = any value accepted (just must be present) |

### CredentialIssuanceConfig

Defines how a blueprint action mints a credential upon execution.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| CredentialType | string | Yes | Type of credential to issue (e.g., "LicenseCredential") |
| ClaimMappings | ClaimMapping[] | Yes | Maps action data fields to credential claims |
| RecipientParticipantId | string | Yes | Participant ID who receives the credential |
| ExpiryDuration | duration | No | How long the credential is valid (e.g., "P365D" = 1 year). Null = no expiry |
| RegisterId | string | No | If set, records the credential on this register for public queryability |
| Disclosable | string[] | No | Claim names that support selective disclosure. Null = all claims disclosable |

### ClaimMapping

Maps an action data field to a credential claim.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| ClaimName | string | Yes | The claim key in the issued credential |
| SourceField | string | Yes | JSON Pointer to the action data field (e.g., "/licenseType") |

### VerifiableCredential (stored entity)

A credential stored in the participant's wallet.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Id | string | Yes | Unique credential identifier (DID URI: `did:sorcha:credential:{uuid}`) |
| Type | string | Yes | Credential type (e.g., "LicenseCredential") |
| IssuerDid | string | Yes | DID URI or wallet address of the issuer |
| SubjectDid | string | Yes | DID URI or wallet address of the holder |
| Claims | Dictionary<string, object> | Yes | All credential claims (key-value pairs) |
| IssuedAt | DateTimeOffset | Yes | When the credential was issued |
| ExpiresAt | DateTimeOffset | No | When the credential expires. Null = no expiry |
| RawToken | string | Yes | The complete SD-JWT VC token (for export and presentation) |
| Status | enum | Yes | `Active`, `Revoked`, `Expired` |
| IssuanceTxId | string | Yes | Transaction ID that recorded the issuance on the ledger |
| IssuanceBlueprintId | string | Yes | Blueprint ID that issued this credential |
| WalletAddress | string | Yes | Wallet address of the holder (for lookup) |
| CreatedAt | DateTimeOffset | Yes | When stored in the wallet |

**State Transitions:**
```
[Issued] ──▶ Active ──▶ Revoked (by issuer authority)
                   ──▶ Expired (automatic, based on ExpiresAt)
```
- Active → Revoked: Triggered by revocation action. Irreversible.
- Active → Expired: Automatic when current time > ExpiresAt. Irreversible.
- Revoked/Expired: Terminal states. No transition out.

**Uniqueness:** Id is globally unique (DID URI). One credential per Id per wallet.

### CredentialPresentation

Submitted by a participant at action time to satisfy credential requirements.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| CredentialId | string | Yes | DID URI of the credential being presented |
| DisclosedClaims | Dictionary<string, object> | Yes | Only the claims revealed via selective disclosure |
| RawPresentation | string | Yes | The SD-JWT presentation token (JWT~disclosure1~disclosure2~KB-JWT) |
| KeyBindingProof | string | No | Key binding JWT proving the presenter holds the credential key |

### CredentialRevocation

A ledger record for a revoked credential.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| CredentialId | string | Yes | DID URI of the revoked credential |
| RevokedBy | string | Yes | DID URI or wallet address of the revoking authority |
| RevokedAt | DateTimeOffset | Yes | Timestamp of revocation |
| Reason | string | No | Human-readable reason for revocation |
| LedgerTxId | string | Yes | Transaction ID on the register that recorded the revocation |

## Modified Existing Entities

### Action (Sorcha.Blueprint.Models.Action)

New properties added:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| CredentialRequirements | IEnumerable<CredentialRequirement> | No | Credentials required to execute this action |
| CredentialIssuanceConfig | CredentialIssuanceConfig | No | Configuration for minting a credential on action completion |

### ExecutionContext (Sorcha.Blueprint.Engine.Models.ExecutionContext)

New property added:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| CredentialPresentations | IEnumerable<CredentialPresentation> | No | Credentials submitted by the participant for this action |

### ActionExecutionResult (Sorcha.Blueprint.Engine.Models.ActionExecutionResult)

New properties added:

| Field | Type | Description |
|-------|------|-------------|
| CredentialValidation | CredentialValidationResult | Result of credential verification |
| IssuedCredential | VerifiableCredential | Credential minted by this action (if applicable) |

### CredentialValidationResult (new)

| Field | Type | Description |
|-------|------|-------------|
| IsValid | bool | Whether all credential requirements are satisfied |
| Errors | List<CredentialValidationError> | Specific failure details |
| VerifiedCredentials | List<VerifiedCredentialDetail> | Successfully verified credentials |

### CredentialValidationError (new)

| Field | Type | Description |
|-------|------|-------------|
| RequirementType | string | Which credential type requirement failed |
| FailureReason | enum | `Missing`, `Expired`, `Revoked`, `InvalidSignature`, `IssuerNotAccepted`, `ClaimMismatch`, `RevocationCheckUnavailable` |
| Message | string | Human-readable error message |

### VerifiedCredentialDetail (new)

Detail record for a successfully verified credential, included in CredentialValidationResult.

| Field | Type | Description |
|-------|------|-------------|
| CredentialId | string | DID URI of the verified credential |
| Type | string | Credential type (e.g., "LicenseCredential") |
| IssuerDid | string | DID URI or wallet address of the issuer |
| VerifiedClaims | Dictionary<string, object> | Only the claims that were disclosed and verified |
| SignatureValid | bool | Whether the cryptographic signature validated successfully |
| RevocationStatus | string | `Active`, `Revoked`, or `Unknown` (when revocation check unavailable) |
