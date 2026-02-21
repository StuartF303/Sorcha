# Contract: Status List Endpoints

**Service**: Blueprint Service
**Base path**: `/api/v1/credentials/status-lists`
**Auth**: Public (unauthenticated) for GET, authenticated for management

## Endpoints

### GET /api/v1/credentials/status-lists/{listId}

**Auth**: None (public)
**Cache**: `Cache-Control: max-age=300` (5 minutes, configurable)

**Response 200**:
```json
{
  "@context": ["https://www.w3.org/ns/credentials/v2"],
  "id": "https://sorcha.example/api/v1/credentials/status-lists/{listId}",
  "type": ["VerifiableCredential", "BitstringStatusListCredential"],
  "issuer": "did:sorcha:w:{issuerAddress}",
  "validFrom": "2026-02-21T00:00:00Z",
  "credentialSubject": {
    "id": "https://sorcha.example/api/v1/credentials/status-lists/{listId}#list",
    "type": "BitstringStatusList",
    "statusPurpose": "revocation",
    "encodedList": "H4sIAAAAAAAA..."
  }
}
```

**Response 404**: Status list not found

### POST /api/v1/credentials/status-lists/{listId}/allocate (internal)

**Auth**: Service-to-service JWT
**Purpose**: Allocate next available index for a new credential

**Request**:
```json
{
  "credentialId": "urn:uuid:abc-123"
}
```

**Response 200**:
```json
{
  "listId": "issuer-register-revocation-1",
  "index": 42,
  "statusListUrl": "https://sorcha.example/api/v1/credentials/status-lists/issuer-register-revocation-1"
}
```

**Response 409**: Status list full (all positions allocated)

### PUT /api/v1/credentials/status-lists/{listId}/bits/{index} (internal)

**Auth**: Service-to-service JWT
**Purpose**: Set or clear a bit at a given index

**Request**:
```json
{
  "value": true,
  "reason": "Revoked by issuer"
}
```

**Response 200**:
```json
{
  "listId": "issuer-register-revocation-1",
  "index": 42,
  "value": true,
  "version": 3,
  "registerTxId": "a1b2c3..."
}
```

**Response 404**: Index out of range or list not found
