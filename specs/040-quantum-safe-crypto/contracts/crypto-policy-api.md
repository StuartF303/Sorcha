# API Contracts: Crypto Policy & PQC Operations

## Register Crypto Policy Endpoints

### GET /api/registers/{registerId}/crypto-policy

Get the current crypto policy for a register.

**Response 200**:
```json
{
  "version": 1,
  "acceptedSignatureAlgorithms": ["ED25519", "ML-DSA-65", "SLH-DSA-128"],
  "requiredSignatureAlgorithms": [],
  "acceptedEncryptionSchemes": ["XCHACHA20-POLY1305", "ML-KEM-768"],
  "acceptedHashFunctions": ["SHA-256", "BLAKE2B-256"],
  "enforcementMode": "Permissive",
  "effectiveFrom": "2026-02-25T00:00:00Z",
  "deprecatedAlgorithms": []
}
```

**Response 404**: Register not found.

### POST /api/registers/{registerId}/governance/crypto-policy

Submit a crypto policy update as a governance control transaction.

**Request**:
```json
{
  "proposerDid": "did:sorcha:w:ws11q...",
  "policy": {
    "acceptedSignatureAlgorithms": ["ED25519", "ML-DSA-65"],
    "requiredSignatureAlgorithms": ["ML-DSA-65"],
    "acceptedEncryptionSchemes": ["ML-KEM-768", "XCHACHA20-POLY1305"],
    "acceptedHashFunctions": ["SHA-256"],
    "enforcementMode": "Strict",
    "deprecatedAlgorithms": ["RSA4096"]
  },
  "approvalSignatures": [
    {
      "signerDid": "did:sorcha:w:ws11q...",
      "signature": "<base64>"
    }
  ]
}
```

**Response 200**: Policy update submitted as control transaction.
**Response 400**: Invalid policy (e.g., required not subset of accepted).
**Response 403**: Proposer not authorized (not in register roster).

### GET /api/registers/{registerId}/crypto-policy/history

Get all historical crypto policy versions for a register.

**Response 200**:
```json
{
  "policies": [
    { "version": 1, "effectiveFrom": "2026-02-25T00:00:00Z", "controlTxId": "abc..." },
    { "version": 2, "effectiveFrom": "2026-06-01T00:00:00Z", "controlTxId": "def..." }
  ]
}
```

## Wallet PQC Endpoints

### POST /api/v1/wallets (Extended)

Create a wallet with optional PQC key generation.

**Request** (extended with optional PQC fields):
```json
{
  "name": "My Quantum-Safe Wallet",
  "algorithm": "ED25519",
  "pqcAlgorithm": "ML-DSA-65",
  "enableHybrid": true
}
```

**Response 201** (extended):
```json
{
  "walletAddress": "ws11q...",
  "pqcWalletAddress": "ws21q...",
  "algorithm": "ED25519",
  "pqcAlgorithm": "ML-DSA-65",
  "mnemonic": "word1 word2 ... word12"
}
```

### POST /api/v1/wallets/{address}/sign (Extended)

Sign data with optional hybrid mode.

**Request** (extended):
```json
{
  "data": "<base64-data-to-sign>",
  "derivationPath": "m/44'/0'/0'/0/0",
  "isPreHashed": false,
  "hybridMode": true
}
```

**Response 200** (hybrid):
```json
{
  "signature": "{\"classical\":\"<base64>\",\"classicalAlgorithm\":\"ED25519\",\"pqc\":\"<base64>\",\"pqcAlgorithm\":\"ML-DSA-65\",\"witnessPublicKey\":\"<base64>\"}",
  "publicKey": "<base64-classical>",
  "algorithm": "ED25519"
}
```

### POST /api/v1/wallets/{address}/encapsulate

Encapsulate a symmetric key using ML-KEM-768.

**Request**:
```json
{
  "recipientPqcPublicKey": "<base64-ml-kem-768-public-key>"
}
```

**Response 200** (only ciphertext is returned; the shared secret is stored server-side per the KEM security model and never exposed over the wire):
```json
{
  "ciphertext": "<base64-encapsulated-key>",
  "algorithm": "ML-KEM-768"
}
```

### POST /api/v1/wallets/{address}/decapsulate

Decapsulate a symmetric key using ML-KEM-768 private key.

**Request**:
```json
{
  "ciphertext": "<base64-encapsulated-key>",
  "derivationPath": "m/44'/1'/0'/0/0"
}
```

**Response 200**:
```json
{
  "sharedSecret": "<base64-32-byte-shared-secret>",
  "algorithm": "ML-KEM-768"
}
```

## Validator BLS Threshold Endpoints

### POST /api/v1/validators/threshold/setup

Initialize BLS threshold signing for a register's validator set.

**Request**:
```json
{
  "registerId": "abc123...",
  "threshold": 3,
  "totalValidators": 5
}
```

**Response 200**:
```json
{
  "sharedPublicKey": "<base64-bls-public-key>",
  "validatorShares": [
    { "validatorId": "v1", "shareIndex": 0, "encryptedShare": "<base64>" }
  ]
}
```

### POST /api/v1/validators/threshold/sign

Submit a partial BLS signature for docket signing.

**Request**:
```json
{
  "registerId": "abc123...",
  "docketHash": "<hex-docket-hash>",
  "partialSignature": "<base64-bls-partial>"
}
```

**Response 200**:
```json
{
  "accepted": true,
  "signaturesCollected": 2,
  "thresholdReached": false
}
```

## ZK Proof Endpoints

### POST /api/registers/{registerId}/proofs/inclusion

Request a zero-knowledge proof of transaction inclusion.

**Request**:
```json
{
  "txId": "<64-char-hex>",
  "docketId": "<docket-hash>"
}
```

**Response 200**:
```json
{
  "proof": "<base64-zk-proof>",
  "merkleRoot": "<hex-merkle-root>",
  "verificationKey": "<base64>"
}
```

### POST /api/registers/{registerId}/proofs/verify-inclusion

Verify a zero-knowledge inclusion proof.

**Request**:
```json
{
  "proof": "<base64-zk-proof>",
  "merkleRoot": "<hex-merkle-root>",
  "verificationKey": "<base64>"
}
```

**Response 200**:
```json
{
  "valid": true
}
```
