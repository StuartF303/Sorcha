# API Contract: Wallet Sign Endpoint (Updated)

**Service:** Wallet Service
**Base Path:** `/api/v1/wallets`
**Auth:** `CanManageWallets` (JWT Bearer)

---

## POST /api/v1/wallets/{address}/sign

Signs data using the wallet at the given address. Supports both raw data (hashed internally) and pre-hashed data (signed directly).

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| address | string | Wallet address to sign with |

### Request

```json
{
  "transactionData": "string (Base64-encoded bytes, required)",
  "derivationPath": "string (optional, e.g., 'm/44'/0'/0'/0/5' or 'sorcha:register-attestation')",
  "isPreHashed": false
}
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| transactionData | string | required | Base64-encoded bytes to sign |
| derivationPath | string? | null | BIP44 path or Sorcha system path for key derivation |
| isPreHashed | bool | false | When `true`, the `transactionData` contains a pre-computed hash (e.g., SHA-256). The wallet signs these bytes directly without applying SHA-256 first. When `false` (default), the wallet applies SHA-256 to the data before signing (existing behavior). |

### Response (200 OK)

```json
{
  "signature": "string (Base64-encoded signature bytes)",
  "signedBy": "string (wallet address)",
  "signedAt": "2026-01-27T00:00:00Z",
  "publicKey": "string (Base64-encoded derived public key bytes)"
}
```

### Errors

| Status | Condition |
|--------|-----------|
| 400 | Missing transactionData, invalid Base64 |
| 401 | Not authenticated |
| 403 | Not authorized (missing CanManageWallets) |
| 404 | Wallet not found at given address |
| 500 | Signing operation failed |

### Behavior by `isPreHashed`

**`isPreHashed: false` (default):**
1. Decode `transactionData` from Base64 to raw bytes
2. Compute `SHA256(rawBytes)` -> hash
3. Sign hash with derived private key via CryptoModule
4. Return signature + public key

**`isPreHashed: true`:**
1. Decode `transactionData` from Base64 to raw bytes (these ARE the hash)
2. Sign raw bytes directly with derived private key via CryptoModule (no SHA-256 applied)
3. Return signature + public key

---

## Service Client Contract: WalletSignResult

Return type for `IWalletServiceClient.SignTransactionAsync`:

```csharp
public record WalletSignResult
{
    public required byte[] Signature { get; init; }
    public required byte[] PublicKey { get; init; }
    public required string SignedBy { get; init; }
    public required string Algorithm { get; init; }
}
```

### Interface Change

```csharp
// Before:
Task<string> SignTransactionAsync(string walletAddress, byte[] transactionData,
    string? derivationPath = null, CancellationToken ct = default);

// After:
Task<WalletSignResult> SignTransactionAsync(string walletAddress, byte[] transactionData,
    string? derivationPath = null, bool isPreHashed = false, CancellationToken ct = default);
```

---

## Service Auth Contract: ServiceAuthClient

**Endpoint called:** Tenant Service `POST /api/service-auth/token`

### Token Request (form-urlencoded)

```
grant_type=client_credentials
client_id=service-validator
client_secret=<configured-secret>
scope=wallet:sign
```

### Token Response

```json
{
  "access_token": "eyJ...",
  "refresh_token": "",
  "token_type": "Bearer",
  "expires_in": 28800,
  "scope": "wallet:sign"
}
```

### Token Caching

- Cache token in memory
- Refresh when within 5 minutes of `expires_in`
- Thread-safe refresh via `SemaphoreSlim`
- On failure: log error, return null (callers should handle gracefully)
