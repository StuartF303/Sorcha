# Data Model: Fix Register Creation - Fully Functional Cryptographic Register Flow

**Branch**: `015-fix-register-crypto` | **Date**: 2026-01-27

## Modified Entities

### PendingRegistration (in-memory, ConcurrentDictionary)

**File:** `src/Common/Sorcha.Register.Models/RegisterCreationModels.cs`

| Field | Type | Status | Description |
|-------|------|--------|-------------|
| RegisterId | string | Existing | GUID without hyphens (32-char hex) |
| ControlRecord | RegisterControlRecord | Existing | Template control record |
| ControlRecordHash | string | Existing (unused) | Currently `string.Empty` |
| CreatedAt | DateTimeOffset | Existing | Creation timestamp |
| ExpiresAt | DateTimeOffset | Existing | CreatedAt + 5 minutes |
| Nonce | string | Existing | 32 random bytes, Base64-encoded |
| **AttestationHashes** | **Dictionary\<string, byte[]\>** | **NEW** | Map of `"{role}:{subject}"` -> SHA-256 hash bytes. Stored at initiate, consumed at finalize for signature verification. |

**State transitions:** Created (at initiate) -> Consumed (at finalize, atomic TryRemove) | Expired (after 5 min TTL, cleaned by background task)

### AttestationToSign (response DTO)

**File:** `src/Common/Sorcha.Register.Models/RegisterCreationModels.cs`

| Field | Type | Status | Description |
|-------|------|--------|-------------|
| UserId | string | Existing | Owner user ID |
| WalletId | string | Existing | Owner wallet ID |
| Role | RegisterRole | Existing | Owner/Admin |
| AttestationData | AttestationSigningData | Existing | The attestation payload |
| DataToSign | string | **CHANGED** | Was: canonical JSON string. Now: hex-encoded SHA-256 hash of canonical JSON. XML doc updated. |

### SignTransactionRequest (wallet endpoint DTO)

**File:** `src/Services/Sorcha.Wallet.Service/Models/SignTransactionRequest.cs`

| Field | Type | Status | Description |
|-------|------|--------|-------------|
| TransactionData | string | Existing | Base64-encoded data to sign |
| DerivationPath | string? | Existing | BIP44 or Sorcha system path |
| **IsPreHashed** | **bool** | **NEW** | Default `false`. When `true`, wallet signs bytes directly without SHA-256 hashing. |

### WalletSignResult (new return type)

**File:** `src/Common/Sorcha.ServiceClients/Wallet/IWalletServiceClient.cs`

| Field | Type | Description |
|-------|------|-------------|
| Signature | byte[] | Raw signature bytes |
| PublicKey | byte[] | Derived public key bytes |
| SignedBy | string | Wallet address that signed |
| Algorithm | string | Algorithm used (ED25519, NISTP256, RSA4096) |

**Usage:** Returned by `IWalletServiceClient.SignTransactionAsync` (replaces `Task<string>` return type).

## New Entities

### IServiceAuthClient / ServiceAuthClient

**File:** `src/Common/Sorcha.ServiceClients/Auth/IServiceAuthClient.cs`, `ServiceAuthClient.cs`

Not a data entity -- this is a service client for JWT token acquisition. Internal state:

| Field | Type | Description |
|-------|------|-------------|
| _cachedToken | string? | Cached JWT access token |
| _tokenExpiry | DateTimeOffset | When the cached token expires |
| _refreshLock | SemaphoreSlim | Thread-safe token refresh |

**Configuration (per-service):**

| Config Key | Type | Description |
|------------|------|-------------|
| ServiceAuth:ClientId | string | Service principal client_id (e.g., `service-validator`) |
| ServiceAuth:ClientSecret | string | Service principal client_secret |
| ServiceAuth:TenantServiceUrl | string | URL of Tenant Service (or use Aspire service discovery) |

## Relationships

```
PendingRegistration ---stores---> AttestationHashes (1:N, keyed by role:subject)
                    ---references--> RegisterControlRecord (1:1, template)

AttestationToSign ---contains--> DataToSign (hex SHA-256 hash)
                  ---references--> AttestationSigningData (1:1)

FinalizeRequest ---contains--> SignedAttestations (1:N)
                ---matches----> PendingRegistration.AttestationHashes (by role:subject)

WalletSignResult ---returned-by--> WalletServiceClient.SignTransactionAsync
                 ---consumed-by--> ValidationEndpoints (system wallet signing)
                 ---consumed-by--> GenesisManager (docket signing)

ServiceAuthClient ---authenticates--> WalletServiceClient (Bearer token)
                  ---calls----------> Tenant Service /api/service-auth/token
```

## Validation Rules

| Entity | Rule | Source |
|--------|------|--------|
| PendingRegistration | ExpiresAt > UtcNow (5-min TTL) | Existing |
| PendingRegistration | Nonce must match on finalize | Existing |
| PendingRegistration | AttestationHashes must have entry for each SignedAttestation | New (FR-004) |
| SignTransactionRequest | IsPreHashed: when true, TransactionData must be exactly 32 bytes (SHA-256) | New (FR-003) |
| WalletSignResult | Signature and PublicKey must be non-empty byte arrays | New (FR-006) |
| ServiceAuthClient | Token must not be expired; refresh 5 min before expiry | New (FR-014) |
