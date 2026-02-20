# Quickstart: Published Participant Records

**Branch**: `001-participant-records` | **Date**: 2026-02-20

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for MongoDB, Redis, PostgreSQL)
- Existing Sorcha services running (`docker-compose up -d` or Aspire)

## What This Feature Adds

A new transaction type `Participant` (value 3) that allows organizations to publish participant identity records directly to a register. Published records contain the participant's name, organization, wallet addresses (with public keys for multiple algorithms), and optional metadata. Records are discoverable by any node via address lookup.

## Key Files to Modify

### Shared Models (touches all services)
1. `src/Common/Sorcha.Register.Models/Enums/TransactionType.cs` — Add `Participant = 3`
2. `src/Common/Sorcha.Register.Models/ParticipantRecord.cs` — New payload model
3. `src/Common/Sorcha.Register.Models/Enums/ParticipantRecordStatus.cs` — New status enum
4. `src/Common/Sorcha.ServiceClients/Validator/IValidatorServiceClient.cs` — Make BlueprintId/ActionId nullable

### Validator Service
5. `src/Services/Sorcha.Validator.Service/Services/ValidationEngine.cs` — Participant schema validation, governance skip, fork detection awareness
6. `src/Services/Sorcha.Validator.Service/Schemas/participant-record-v1.json` — Built-in JSON Schema

### Register Service
7. `src/Services/Sorcha.Register.Service/Program.cs` — New participant query endpoints
8. `src/Services/Sorcha.Register.Service/Services/ParticipantIndexService.cs` — New address index

### Tenant Service
9. `src/Services/Sorcha.Tenant.Service/Services/IParticipantPublishingService.cs` — New publishing service
10. `src/Services/Sorcha.Tenant.Service/Services/ParticipantPublishingService.cs` — Implementation
11. `src/Services/Sorcha.Tenant.Service/Endpoints/ParticipantEndpoints.cs` — New publish endpoints

### Service Clients
12. `src/Common/Sorcha.ServiceClients/Register/IRegisterServiceClient.cs` — Participant query methods
13. `src/Common/Sorcha.ServiceClients/Register/RegisterServiceClient.cs` — Implementation

### Tests
14. `tests/Sorcha.Validator.Service.Tests/Services/ValidationEngineTests.cs` — Participant validation tests
15. `tests/Sorcha.Register.Service.Tests/` — Participant query endpoint tests
16. `tests/Sorcha.Tenant.Service.Tests/` — Publishing service tests
17. `tests/Sorcha.ServiceClients.Tests/` — Service client tests

## Transaction Flow

```
1. Org admin → Tenant Service (POST /api/organizations/{orgId}/participants/publish)
   ↓ Authorization check + build ParticipantRecord payload
2. Tenant Service → Wallet Service (sign with user's wallet)
   ↓ Returns signature
3. Tenant Service → Validator Service (POST /api/v1/transactions/validate)
   ↓ TransactionSubmission { Type=Participant, Payload=ParticipantRecord }
4. Validator validates:
   ✓ Signature verification
   ✓ Participant record schema validation (built-in schema)
   ✓ Chain integrity (PrevTxId → latest Control TX)
   ✗ NO governance roster check
   ✗ NO blueprint conformance check
5. Validator → Mempool → Docket Builder → Register Service
   ↓ Transaction confirmed
6. Register Service indexes addresses for lookup
7. Any node can query: GET /api/registers/{id}/participants/by-address/{addr}
```

## Build and Test

```bash
# Build affected projects
dotnet build src/Common/Sorcha.Register.Models/
dotnet build src/Common/Sorcha.ServiceClients/
dotnet build src/Services/Sorcha.Validator.Service/
dotnet build src/Services/Sorcha.Register.Service/
dotnet build src/Services/Sorcha.Tenant.Service/

# Run tests
dotnet test tests/Sorcha.Validator.Service.Tests/
dotnet test tests/Sorcha.Register.Service.Tests/
dotnet test tests/Sorcha.Tenant.Service.Tests/
dotnet test tests/Sorcha.ServiceClients.Tests/

# Docker rebuild (after code changes)
docker-compose build validator register tenant
docker-compose up -d --force-recreate validator register tenant
```

## Example: Publish a Participant

```bash
# 1. Login to get JWT
TOKEN=$(curl -s http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sorcha.local","password":"Dev_Pass_2025!"}' \
  | jq -r '.access_token')

# 2. Publish participant to a register
curl -X POST http://localhost/api/organizations/{orgId}/participants/publish \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "registerId": "register-001",
    "participantName": "Planning Review Desk",
    "organizationName": "Council Building Department",
    "addresses": [{
      "walletAddress": "sra1q...",
      "publicKey": "base64...",
      "algorithm": "ED25519",
      "primary": true
    }],
    "signerWalletAddress": "sra1x..."
  }'

# 3. Look up by address
curl http://localhost/api/registers/register-001/participants/by-address/sra1q... \
  -H "Authorization: Bearer $TOKEN"
```
