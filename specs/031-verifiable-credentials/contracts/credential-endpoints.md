# API Contracts: Verifiable Credentials

**Branch**: `031-verifiable-credentials` | **Date**: 2026-02-12

All endpoints are exposed via the API Gateway (YARP) under `/api/`.

## Wallet Service — Credential Storage Endpoints

Base path: `/api/v1/wallets/{walletAddress}/credentials`

### List Credentials

```
GET /api/v1/wallets/{walletAddress}/credentials
```

**Query Parameters:**
| Param | Type | Description |
|-------|------|-------------|
| type | string | Filter by credential type |
| issuer | string | Filter by issuer DID/wallet address |
| status | string | Filter by status: `active`, `revoked`, `expired` |
| page | int | Page number (default: 1) |
| pageSize | int | Page size (default: 20, max: 100) |

**Response 200:**
```json
{
  "items": [
    {
      "id": "did:sorcha:credential:abc123",
      "type": "LicenseCredential",
      "issuerDid": "did:sorcha:register:reg1/tx/tx1",
      "claims": { "licenseType": "electrical", "level": "master" },
      "issuedAt": "2026-02-12T10:00:00Z",
      "expiresAt": "2027-02-12T10:00:00Z",
      "status": "active"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

**Auth:** JWT Bearer — must be the wallet owner or admin.

---

### Get Credential by ID

```
GET /api/v1/wallets/{walletAddress}/credentials/{credentialId}
```

**Response 200:**
```json
{
  "id": "did:sorcha:credential:abc123",
  "type": "LicenseCredential",
  "issuerDid": "did:sorcha:register:reg1/tx/tx1",
  "subjectDid": "did:sorcha:wallet:0xabc...",
  "claims": { "licenseType": "electrical", "level": "master", "name": "Jane Smith" },
  "issuedAt": "2026-02-12T10:00:00Z",
  "expiresAt": "2027-02-12T10:00:00Z",
  "status": "active",
  "rawToken": "<SD-JWT token>",
  "issuanceTxId": "tx-abc123",
  "issuanceBlueprintId": "bp-license-001"
}
```

**Auth:** JWT Bearer — must be the wallet owner.

---

### Export Credential (Portable Token)

```
GET /api/v1/wallets/{walletAddress}/credentials/{credentialId}/export
```

**Response 200:**
```json
{
  "format": "sd-jwt-vc",
  "token": "<complete SD-JWT VC token for external use>"
}
```

**Auth:** JWT Bearer — must be the wallet owner.

---

### Match Credentials Against Requirements

```
POST /api/v1/wallets/{walletAddress}/credentials/match
```

**Request Body:**
```json
{
  "requirements": [
    {
      "type": "LicenseCredential",
      "acceptedIssuers": ["did:sorcha:register:reg1/tx/tx1"],
      "requiredClaims": [
        { "claimName": "licenseType", "expectedValue": "electrical" }
      ]
    }
  ]
}
```

**Response 200:**
```json
{
  "matches": [
    {
      "requirementIndex": 0,
      "matched": true,
      "credentials": [
        {
          "credentialId": "did:sorcha:credential:abc123",
          "type": "LicenseCredential",
          "issuerDid": "did:sorcha:register:reg1/tx/tx1",
          "relevantClaims": { "licenseType": "electrical" },
          "expiresAt": "2027-02-12T10:00:00Z",
          "status": "active"
        }
      ]
    }
  ],
  "allSatisfied": true
}
```

**Response 200 (unmet):**
```json
{
  "matches": [
    {
      "requirementIndex": 0,
      "matched": false,
      "credentials": [],
      "unmetReason": "No credentials of type 'LicenseCredential' from accepted issuers found"
    }
  ],
  "allSatisfied": false
}
```

**Auth:** JWT Bearer — must be the wallet owner.

---

### Delete Credential from Wallet

```
DELETE /api/v1/wallets/{walletAddress}/credentials/{credentialId}
```

**Response 204:** No content.

**Note:** This removes the credential from the wallet store only. It does not revoke the credential — revocation is an issuer action on the ledger.

**Auth:** JWT Bearer — must be the wallet owner.

---

## Blueprint Service — Credential Verification (Internal)

These operations happen within the blueprint engine execution pipeline, not as standalone endpoints.

### Verify Credential Presentation (Engine Internal)

Called by ActionProcessor Step 0 during action execution.

**Input (from ExecutionContext):**
```json
{
  "credentialPresentations": [
    {
      "credentialId": "did:sorcha:credential:abc123",
      "disclosedClaims": { "licenseType": "electrical" },
      "rawPresentation": "<SD-JWT presentation token>",
      "keyBindingProof": "<KB-JWT>"
    }
  ]
}
```

**Output (CredentialValidationResult):**
```json
{
  "isValid": true,
  "errors": [],
  "verifiedCredentials": [
    {
      "credentialId": "did:sorcha:credential:abc123",
      "type": "LicenseCredential",
      "issuerDid": "did:sorcha:register:reg1/tx/tx1",
      "verifiedClaims": { "licenseType": "electrical" },
      "signatureValid": true,
      "revocationStatus": "active"
    }
  ]
}
```

**Failure Output:**
```json
{
  "isValid": false,
  "errors": [
    {
      "requirementType": "LicenseCredential",
      "failureReason": "Expired",
      "message": "Credential did:sorcha:credential:abc123 expired on 2026-01-01T00:00:00Z"
    }
  ],
  "verifiedCredentials": []
}
```

---

## Blueprint Service — Credential Issuance (Internal)

Triggered when a credential-issuing action is executed.

### Issue Credential (Engine Internal)

Called by ActionProcessor after Step 4 (Disclosure) when action has CredentialIssuanceConfig.

**Input (from CredentialIssuanceConfig + Action Data):**
```json
{
  "credentialType": "LicenseCredential",
  "claimMappings": [
    { "claimName": "licenseType", "sourceField": "/licenseType" },
    { "claimName": "level", "sourceField": "/skillLevel" },
    { "claimName": "name", "sourceField": "/applicantName" }
  ],
  "recipientParticipantId": "applicant",
  "expiryDuration": "P365D",
  "registerId": "reg-licenses-001",
  "disclosable": ["name", "licenseType", "level"]
}
```

**Output (IssuedCredential):**
```json
{
  "credentialId": "did:sorcha:credential:new-uuid",
  "type": "LicenseCredential",
  "issuerDid": "did:sorcha:wallet:0xauthority...",
  "subjectDid": "did:sorcha:wallet:0xapplicant...",
  "claims": { "licenseType": "electrical", "level": "master", "name": "Jane Smith" },
  "issuedAt": "2026-02-12T14:30:00Z",
  "expiresAt": "2027-02-12T14:30:00Z",
  "rawToken": "<SD-JWT VC token>",
  "issuanceTxId": "tx-issuance-001"
}
```

---

## Register Service — Credential Register Queries

Uses existing register query infrastructure. A "credential register" is a standard Sorcha register whose transactions contain credential issuance/revocation events.

### Query Credentials on a Register

```
GET /api/registers/{registerId}/transactions?$filter=Metadata/credentialType eq 'LicenseCredential'
```

Uses existing OData v4 query support. Credential metadata is stored in the transaction Metadata field.

---

## Blueprint Service — Revocation Endpoint

### Revoke Credential

```
POST /api/v1/credentials/{credentialId}/revoke
```

**Request Body:**
```json
{
  "reason": "License holder violated safety regulations",
  "registerId": "reg-licenses-001"
}
```

**Response 200:**
```json
{
  "credentialId": "did:sorcha:credential:abc123",
  "revokedAt": "2026-02-12T16:00:00Z",
  "revokedBy": "did:sorcha:wallet:0xauthority...",
  "reason": "License holder violated safety regulations",
  "ledgerTxId": "tx-revocation-001"
}
```

**Response 403:** Caller is not the original issuer of this credential.

**Auth:** JWT Bearer — must be the original issuer (wallet address matches credential issuer).

---

## Blueprint JSON Contract — New Action Properties

### CredentialRequirements on Action

```json
{
  "actions": [
    {
      "id": 1,
      "title": "Submit Work Order",
      "sender": "contractor",
      "credentialRequirements": [
        {
          "type": "LicenseCredential",
          "acceptedIssuers": ["did:sorcha:wallet:0xlicensing-authority"],
          "requiredClaims": [
            { "claimName": "licenseType", "expectedValue": "electrical" }
          ],
          "revocationCheckPolicy": "failClosed",
          "description": "Valid electrical license required"
        }
      ],
      "dataSchemas": [],
      "routes": []
    }
  ]
}
```

### CredentialIssuanceConfig on Action

```json
{
  "actions": [
    {
      "id": 3,
      "title": "Approve License",
      "sender": "authority",
      "credentialIssuanceConfig": {
        "credentialType": "LicenseCredential",
        "claimMappings": [
          { "claimName": "licenseType", "sourceField": "/licenseType" },
          { "claimName": "level", "sourceField": "/skillLevel" }
        ],
        "recipientParticipantId": "applicant",
        "expiryDuration": "P365D",
        "registerId": "reg-licenses-001",
        "disclosable": ["licenseType", "level"]
      },
      "routes": []
    }
  ]
}
```
