# Contract: Presentation Endpoints

**Service**: Wallet Service
**Base path**: `/api/v1/presentations`
**Auth**: Varies (see per-endpoint)

## Endpoints

### POST /api/v1/presentations/request

**Auth**: Authenticated (verifier JWT)
**Purpose**: Create a new presentation request

**Request**:
```json
{
  "credentialType": "ChemicalHandlingLicense",
  "acceptedIssuers": ["did:sorcha:w:hse-authority-address"],
  "requiredClaims": [
    { "claimName": "class", "expectedValue": "CategoryB" }
  ],
  "callbackUrl": "https://verifier.example/callback",
  "targetWalletAddress": "holder-wallet-address",
  "ttlSeconds": 300
}
```

**Response 201**:
```json
{
  "requestId": "uuid-request-id",
  "nonce": "a1b2c3d4e5f6...32hexchars",
  "requestUrl": "https://sorcha.example/api/v1/presentations/uuid-request-id",
  "qrCodeUrl": "openid4vp://authorize?request_uri=https://sorcha.example/api/v1/presentations/uuid-request-id&nonce=a1b2c3d4e5f6...32hexchars",
  "expiresAt": "2026-02-21T10:05:00Z"
}
```

**Response 400**: Invalid request (missing required fields, invalid callback URL)

### GET /api/v1/presentations/{requestId}

**Auth**: Authenticated (holder or verifier JWT)
**Purpose**: Fetch presentation request details (wallet UI uses this)

**Response 200**:
```json
{
  "requestId": "uuid-request-id",
  "verifierIdentity": "Chemical Supplier Corp",
  "credentialType": "ChemicalHandlingLicense",
  "acceptedIssuers": ["did:sorcha:w:hse-authority-address"],
  "requiredClaims": [
    { "claimName": "class", "expectedValue": "CategoryB" }
  ],
  "nonce": "a1b2c3d4e5f6...32hexchars",
  "status": "Pending",
  "expiresAt": "2026-02-21T10:05:00Z",
  "matchingCredentials": [
    {
      "credentialId": "urn:uuid:cred-123",
      "type": "ChemicalHandlingLicense",
      "issuerDid": "did:sorcha:w:hse-authority-address",
      "disclosableClaims": ["class", "permitNumber", "holderName"],
      "requestedClaims": ["class"]
    }
  ]
}
```

**Response 404**: Request not found
**Response 410**: Request expired

### POST /api/v1/presentations/{requestId}/submit

**Auth**: Authenticated (holder JWT)
**Purpose**: Submit a presentation (wallet approves disclosure)

**Request**:
```json
{
  "credentialId": "urn:uuid:cred-123",
  "disclosedClaims": ["class", "permitNumber"],
  "vpToken": "eyJhbGciOiJFZERTQSJ9...~WyJzYWx0IiwiY2xhc3MiLCJDYXRlZ29yeUIiXQ~"
}
```

**Response 200**:
```json
{
  "requestId": "uuid-request-id",
  "status": "Verified",
  "verificationResult": {
    "isValid": true,
    "verifiedClaims": {
      "class": "CategoryB",
      "permitNumber": "HSE-2026-001"
    },
    "credentialType": "ChemicalHandlingLicense",
    "issuerDid": "did:sorcha:w:hse-authority-address",
    "statusListCheck": "Active"
  }
}
```

**Response 200 (failed verification)**:
```json
{
  "requestId": "uuid-request-id",
  "status": "Denied",
  "verificationResult": {
    "isValid": false,
    "errors": [
      { "requirementType": "ChemicalHandlingLicense", "failureReason": "Revoked", "message": "Credential has been revoked by issuer" }
    ]
  }
}
```

**Response 400**: Invalid submission (missing vpToken, wrong request ID)
**Response 404**: Request not found
**Response 410**: Request expired

### POST /api/v1/presentations/{requestId}/deny

**Auth**: Authenticated (holder JWT)
**Purpose**: Deny a presentation request

**Response 200**:
```json
{
  "requestId": "uuid-request-id",
  "status": "Denied"
}
```

### GET /api/v1/presentations/{requestId}/result

**Auth**: Authenticated (verifier JWT)
**Purpose**: Poll for verification result (verifier terminal uses this)

**Response 200**: Same as submit response (Verified or Denied)
**Response 202**: Request still Pending (not yet submitted)
**Response 404**: Request not found
**Response 410**: Request expired
