# Quickstart: Quantum-Safe Cryptography

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for full stack)
- Existing Sorcha environment running

## Test PQC Signing (Unit Level)

```bash
# Run cryptography tests (after implementation)
dotnet test tests/Sorcha.Cryptography.Tests --filter "FullyQualifiedName~PQC"
```

## Create a PQC-Enabled Wallet

```bash
# Via API Gateway
curl -X POST http://localhost/api/v1/wallets \
  -H "Authorization: Bearer <jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Quantum-Safe Wallet",
    "algorithm": "ED25519",
    "pqcAlgorithm": "ML-DSA-65",
    "enableHybrid": true
  }'
```

Response includes both `walletAddress` (ws1...) and `pqcWalletAddress` (ws2...).

## Sign with Hybrid Mode

```bash
curl -X POST http://localhost/api/v1/wallets/{address}/sign \
  -H "Authorization: Bearer <jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "data": "<base64-data>",
    "hybridMode": true
  }'
```

Returns a JSON signature containing both classical and PQC signatures.

## Create a Register with Crypto Policy

When creating a register, the genesis control transaction automatically includes a default crypto policy (hybrid mode, all algorithms accepted). To create a PQC-mandatory register:

1. Create the register normally
2. Submit a crypto policy update via governance:

```bash
curl -X POST http://localhost/api/registers/{registerId}/governance/crypto-policy \
  -H "Authorization: Bearer <jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "proposerDid": "did:sorcha:w:ws11q...",
    "policy": {
      "acceptedSignatureAlgorithms": ["ML-DSA-65", "ED25519"],
      "requiredSignatureAlgorithms": ["ML-DSA-65"],
      "enforcementMode": "Strict"
    }
  }'
```

## Verify PQC Addresses

- Classical addresses start with `ws1` (existing)
- PQC addresses start with `ws2` (new)
- Both are ~60 characters, human-readable

## Walkthrough

After implementation, use the existing MedicalEquipmentRefurb walkthrough with PQC-enabled wallets to verify end-to-end functionality.
