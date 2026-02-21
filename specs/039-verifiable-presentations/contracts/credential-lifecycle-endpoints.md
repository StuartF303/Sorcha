# Contract: Credential Lifecycle Endpoints

**Services**: Wallet Service (storage) + Blueprint Service (lifecycle operations)

## Wallet Service Extensions

**Base path**: `/api/v1/wallets/{walletAddress}/credentials`
**Auth**: Authenticated (wallet owner JWT)

### PATCH /{credentialId}/status (MODIFY existing)

Extended to support new status values.

**Request**:
```json
{
  "status": "Suspended"
}
```

**Valid status values**: Active, Suspended, Revoked, Consumed
(Expired is set automatically, not via this endpoint)

**Response 200**:
```json
{
  "credentialId": "urn:uuid:cred-123",
  "previousStatus": "Active",
  "newStatus": "Suspended",
  "updatedAt": "2026-02-21T10:00:00Z"
}
```

**Response 400**: Invalid status transition (e.g., Revoked → Active)
**Response 403**: Not authorized (not the issuer or governance role)
**Response 404**: Credential not found

## Blueprint Service Lifecycle Endpoints

**Base path**: `/api/v1/credentials`
**Auth**: Authenticated (issuer or governance role JWT)

### POST /{credentialId}/suspend (NEW)

**Purpose**: Temporarily suspend a credential (reversible)

**Request**:
```json
{
  "issuerWallet": "issuer-wallet-address",
  "reason": "Pending investigation"
}
```

**Response 200**:
```json
{
  "credentialId": "urn:uuid:cred-123",
  "status": "Suspended",
  "suspendedBy": "issuer-wallet-address",
  "suspendedAt": "2026-02-21T10:00:00Z",
  "reason": "Pending investigation",
  "statusListUpdated": true
}
```

**Response 400**: Credential not in Active state
**Response 403**: Not the original issuer or register governance role
**Response 404**: Credential not found

### POST /{credentialId}/reinstate (NEW)

**Purpose**: Reinstate a suspended credential

**Request**:
```json
{
  "issuerWallet": "issuer-wallet-address",
  "reason": "Investigation cleared"
}
```

**Response 200**:
```json
{
  "credentialId": "urn:uuid:cred-123",
  "status": "Active",
  "reinstatedBy": "issuer-wallet-address",
  "reinstatedAt": "2026-02-21T10:05:00Z",
  "reason": "Investigation cleared",
  "statusListUpdated": true
}
```

**Response 400**: Credential not in Suspended state
**Response 403**: Not the original issuer or register governance role
**Response 404**: Credential not found

### POST /{credentialId}/revoke (MODIFY existing)

Extended to update status list. Existing endpoint behavior preserved.

**Request**:
```json
{
  "issuerWallet": "issuer-wallet-address",
  "reason": "License expired and not renewed"
}
```

**Response 200**:
```json
{
  "credentialId": "urn:uuid:cred-123",
  "status": "Revoked",
  "revokedBy": "issuer-wallet-address",
  "revokedAt": "2026-02-21T10:00:00Z",
  "reason": "License expired and not renewed",
  "statusListUpdated": true,
  "ledgerTxId": "a1b2c3d4..."
}
```

### POST /{credentialId}/refresh (NEW)

**Purpose**: Reissue an expired credential with a fresh expiry

**Request**:
```json
{
  "issuerWallet": "issuer-wallet-address",
  "newExpiryDuration": "P365D"
}
```

**Response 200**:
```json
{
  "originalCredentialId": "urn:uuid:cred-old",
  "originalStatus": "Consumed",
  "newCredential": {
    "credentialId": "urn:uuid:cred-new",
    "type": "ChemicalHandlingLicense",
    "status": "Active",
    "issuedAt": "2026-02-21T10:00:00Z",
    "expiresAt": "2027-02-21T10:00:00Z"
  }
}
```

**Response 400**: Credential not in Expired state, or type doesn't support refresh
**Response 403**: Not the original issuer
**Response 404**: Credential not found
