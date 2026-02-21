# Data Model: Verifiable Credential Lifecycle & Presentations

**Phase 1 Output** | **Date**: 2026-02-21

## Entity Relationship Overview

```
CredentialEntity ──────── BitstringStatusList
  │ (statusListIndex)         │ (stored as Control TX)
  │                           │
  │                     RegisterTransaction
  │
  ├── CredentialDisplayConfig (embedded JSON)
  │
  ├── PresentationRequest ────── PresentationResult
  │     (nonce, callback)         (vp_token, verified)
  │
  └── CredentialIssuanceConfig (blueprint action property)
        ├── UsagePolicy
        └── CredentialDisplayConfig
```

## Entities

### CredentialEntity (MODIFY existing)

**Location**: `src/Common/Sorcha.Wallet.Core/Domain/Entities/CredentialEntity.cs`

Existing fields retained. New fields marked with ➕.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | string | PK, DID URI | Existing |
| Type | string | Required, max 200 | Existing |
| IssuerDid | string | Required | Existing |
| SubjectDid | string | Required | Existing |
| ClaimsJson | string | Required, JSON | Existing |
| IssuedAt | DateTimeOffset | Required | Existing |
| ExpiresAt | DateTimeOffset? | Nullable | Existing |
| RawToken | string | Required, SD-JWT VC | Existing |
| Status | string | Required, one of: Active, Suspended, Revoked, Expired, Consumed | MODIFY: was Active/Revoked/Expired only |
| IssuanceTxId | string? | 64-char hex | Existing |
| IssuanceBlueprintId | string? | Max 100 | Existing |
| WalletAddress | string | Required, FK | Existing |
| CreatedAt | DateTimeOffset | Required | Existing |
| ➕ UsagePolicy | string | Required, default "Reusable" | Reusable/SingleUse/LimitedUse |
| ➕ MaxPresentations | int? | Nullable, >0 when LimitedUse | Only for LimitedUse |
| ➕ PresentationCount | int | Required, default 0 | Incremented on each successful presentation |
| ➕ StatusListUrl | string? | Nullable, URL | Points to issuer's status list endpoint |
| ➕ StatusListIndex | int? | Nullable, >=0 | Index position in status list bitstring |
| ➕ DisplayConfigJson | string? | Nullable, JSON | Serialized CredentialDisplayConfig |

**State transitions**:
```
Active → Suspended (issuer/governance suspends)
Active → Revoked (issuer/governance revokes)
Active → Consumed (SingleUse/LimitedUse exhausted)
Active → Expired (expiresAt passes)
Suspended → Active (issuer/governance reinstates)
Suspended → Revoked (issuer/governance revokes while suspended)
Expired → Active (credential refreshed/reissued — new entity created)
```

**Validation rules**:
- Status must be one of the five valid values
- MaxPresentations required when UsagePolicy is LimitedUse
- MaxPresentations must be null when UsagePolicy is Reusable or SingleUse
- PresentationCount must not exceed MaxPresentations (when LimitedUse)
- StatusListUrl and StatusListIndex must both be set or both be null

### BitstringStatusList (NEW)

**Location**: `src/Common/Sorcha.Blueprint.Models/Credentials/BitstringStatusList.cs`

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | string | PK, unique per issuer+register | Format: `{issuerWallet}-{registerId}-{purpose}-{sequence}` |
| IssuerWallet | string | Required | Wallet address of the issuing entity |
| RegisterId | string | Required | Register where canonical list is stored |
| Purpose | string | Required, "revocation" or "suspension" | W3C statusPurpose |
| EncodedList | string | Required | GZip + Base64 encoded bitstring |
| Size | int | Required, >=131072 | Number of entries (bits) in the list |
| NextAvailableIndex | int | Required, default 0 | Next free position for allocation |
| Version | int | Required, default 1 | Incremented on each update |
| LastUpdated | DateTimeOffset | Required | When bitstring was last modified |
| RegisterTxId | string? | 64-char hex | TX ID of latest Control TX on register |

**Validation rules**:
- Size must be at least 131,072 (W3C minimum)
- NextAvailableIndex must be < Size
- Purpose must be exactly "revocation" or "suspension"
- EncodedList must be valid GZip + Base64

**Lifecycle**: Created when first credential is issued by an issuer on a register. Grows by creating new lists when capacity reached.

### PresentationRequest (NEW)

**Location**: `src/Services/Sorcha.Wallet.Service/Models/PresentationRequest.cs`

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | string | PK, UUID | Unique request identifier |
| VerifierIdentity | string | Required, max 500 | DID or display name of requesting verifier |
| CredentialType | string | Required, max 200 | Type of credential requested |
| AcceptedIssuers | string[]? | Nullable | DID list of acceptable issuers |
| RequiredClaims | ClaimConstraint[]? | Nullable | Claims that must be present |
| Nonce | string | Required, 32-char random hex | Replay prevention |
| CallbackUrl | string | Required, valid HTTPS URL | Where vp_token is POSTed |
| TargetWalletAddress | string? | Nullable | Specific wallet, or null for any |
| Status | string | Required | Pending/Submitted/Verified/Denied/Expired |
| CreatedAt | DateTimeOffset | Required | Request creation time |
| ExpiresAt | DateTimeOffset | Required | TTL expiry (default: CreatedAt + 5 min) |
| VpToken | string? | Nullable | SD-JWT presentation once submitted |
| VerificationResult | string? | Nullable, JSON | Serialized verification outcome |

**State transitions**:
```
Pending → Submitted (holder submits presentation)
Pending → Denied (holder denies request)
Pending → Expired (TTL passes without response)
Submitted → Verified (presentation passes verification)
Submitted → Denied (presentation fails verification)
```

**Validation rules**:
- Nonce must be exactly 32 hex characters
- CallbackUrl must be valid HTTPS URL
- ExpiresAt must be after CreatedAt
- Status transitions must follow the state machine (no backwards transitions)

### DidDocument (NEW)

**Location**: `src/Common/Sorcha.ServiceClients/Did/DidDocument.cs`

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | string | Required, valid DID | The DID this document describes |
| VerificationMethod | VerificationMethod[] | Required, >=1 | Public keys and their types |
| Authentication | string[] | Nullable | Key IDs used for authentication |
| AssertionMethod | string[] | Nullable | Key IDs used for signing credentials |
| Service | ServiceEndpoint[]? | Nullable | Service endpoints |

**VerificationMethod**:

| Field | Type | Notes |
|-------|------|-------|
| Id | string | Key identifier (e.g., `did:example:123#key-1`) |
| Type | string | `JsonWebKey2020`, `Ed25519VerificationKey2020`, etc. |
| Controller | string | DID of the key controller |
| PublicKeyJwk | JsonElement? | JWK representation of public key |
| PublicKeyMultibase | string? | Multibase-encoded public key |

### CredentialDisplayConfig (NEW)

**Location**: `src/Common/Sorcha.Blueprint.Models/Credentials/CredentialDisplayConfig.cs`

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| BackgroundColor | string | Valid hex color, default "#1976D2" | Card background |
| TextColor | string | Valid hex color, default "#FFFFFF" | Card text |
| Icon | string | MudBlazor icon name, default "Certificate" | Card type icon |
| CardLayout | string | Standard/Compact/Ticket, default "Standard" | Layout template |
| HighlightClaims | Dictionary<string, string> | Claim path → display label | Claims shown on card face |

### CredentialIssuanceConfig (MODIFY existing)

**Location**: `src/Common/Sorcha.Blueprint.Models/Credentials/CredentialIssuanceConfig.cs`

New fields added to existing model:

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| ➕ UsagePolicy | UsagePolicy | Default: Reusable | Reusable/SingleUse/LimitedUse |
| ➕ MaxPresentations | int? | >0, required for LimitedUse | Null for Reusable/SingleUse |
| ➕ DisplayConfig | CredentialDisplayConfig? | Nullable | Issuer visual template |

### UsagePolicy (NEW)

**Location**: `src/Common/Sorcha.Blueprint.Models/Credentials/UsagePolicy.cs`

```
enum UsagePolicy {
  Reusable = 0,    // Unlimited presentations
  SingleUse = 1,   // Exactly one presentation, then consumed
  LimitedUse = 2   // N presentations (MaxPresentations), then consumed
}
```

### CredentialStatusClaim (NEW)

**Location**: `src/Common/Sorcha.Blueprint.Models/Credentials/CredentialStatusClaim.cs`

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | string | Required, URL + fragment | Status list URL + `#` + index |
| Type | string | Required, "BitstringStatusListEntry" | W3C type constant |
| StatusPurpose | string | Required, "revocation" or "suspension" | Which bitstring |
| StatusListIndex | string | Required, non-negative integer string | Position in bitstring |
| StatusListCredential | string | Required, URL | Status list endpoint |

## Index Strategy

- **CredentialEntity**: Index on `(WalletAddress, Status)` for wallet credential listing
- **CredentialEntity**: Index on `(IssuerDid, Type)` for credential matching
- **PresentationRequest**: Index on `(TargetWalletAddress, Status)` for inbox queries
- **PresentationRequest**: Index on `(Nonce)` unique for replay prevention
- **BitstringStatusList**: Index on `(IssuerWallet, RegisterId, Purpose)` for list lookup
