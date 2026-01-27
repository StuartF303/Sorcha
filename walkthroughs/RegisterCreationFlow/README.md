# Register Creation Flow Walkthrough

## Overview

This walkthrough demonstrates the complete two-phase register creation workflow with cryptographic attestations and genesis transaction processing.

## What This Tests

1. **Phase 1 - Initiate**: POST to `/api/registers/initiate`
   - Accepts `owners` array (userId, walletId, role)
   - Computes SHA-256 hash of canonical JSON attestation data
   - Returns registerId, nonce, and `attestationsToSign` with hex-encoded hashes as `dataToSign`
   - Hash bytes stored in pending state (5-minute expiration) for deterministic verification

2. **Signing Phase** (Client-side):
   - Client converts hex hash to bytes
   - Base64-encodes for Wallet Service API
   - Signs with `isPreHashed=true` (prevents double-hashing)
   - Wallet Service returns signature + derived public key

3. **Phase 2 - Finalize**: POST to `/api/registers/finalize`
   - Validates nonce (replay protection)
   - Verifies each attestation signature against stored hash bytes (no re-serialization)
   - Submits genesis transaction to Validator Service (atomic: genesis BEFORE register persist)
   - Creates register in database only if genesis succeeds

4. **Genesis Transaction Processing**:
   - Validator Service receives genesis transaction
   - Signs control record with system wallet via real Wallet Service call (`isPreHashed=true`)
   - Computes actual PayloadHash (SHA-256 of serialized control record)
   - Sets high priority (genesis transactions processed first)
   - Stores in mempool awaiting docket creation

## Architecture Flow

```
┌─────────────────┐
│   Client/CLI    │
└────────┬────────┘
         │
         │ 1. POST /api/registers/initiate
         ▼
┌─────────────────────────────────────────┐
│     Register Service                    │
│  ┌────────────────────────────────┐    │
│  │ RegisterCreationOrchestrator   │    │
│  │  - Generate registerId         │    │
│  │  - Create attestation data     │    │
│  │  - Compute SHA-256 hash        │    │
│  │  - Store hash bytes + pending  │    │
│  └────────────────────────────────┘    │
└────────┬────────────────────────────────┘
         │
         │ 2. Returns: registerId, nonce, attestationsToSign (hex hashes)
         ▼
┌─────────────────┐
│   Client/CLI    │
│  [Sign hash]    │  ◄── Wallet signs hex hash with isPreHashed=true
└────────┬────────┘
         │
         │ 3. POST /api/registers/finalize (with signatures)
         ▼
┌─────────────────────────────────────────┐
│     Register Service                    │
│  ┌────────────────────────────────┐    │
│  │ RegisterCreationOrchestrator   │    │
│  │  - Verify nonce                │    │
│  │  - Verify sigs (stored hashes) │    │
│  │  - Submit genesis (atomic)     │    │
│  │  - Create register in DB       │    │
│  └────────┬───────────────────────┘    │
└───────────┼────────────────────────────┘
            │
            │ 4. POST /api/validator/genesis
            ▼
┌─────────────────────────────────────────┐
│    Validator Service                    │
│  ┌────────────────────────────────┐    │
│  │ Genesis Endpoint               │    │
│  │  - Set HIGH priority           │    │
│  │  - Add metadata (Type=Genesis) │    │
│  │  - Store in mempool            │    │
│  └────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

## Prerequisites

**Docker-Compose (Recommended - Production-like Environment):**
- Docker and Docker Compose installed
- All Sorcha services running via `docker-compose up -d`
- API Gateway exposed on port 80
- PostgreSQL, MongoDB, Redis containers running

**OR**

**.NET Aspire AppHost (Alternative - Debugging):**
- .NET Aspire AppHost running (all services)
- PostgreSQL container running (Register Service database)
- Redis container running (Validator Service mempool)

## Running the Walkthrough

The test script supports multiple profiles via the `-Profile` parameter:

### Profile: `gateway` (Default - Recommended)

Routes all requests through the API Gateway using YARP, simulating production traffic flow:

```powershell
# Start services with Docker Compose
docker-compose up -d

# Verify services are running
docker-compose ps

# Run the walkthrough via API Gateway (default)
pwsh walkthroughs/RegisterCreationFlow/test-register-creation.ps1

# Or explicitly specify gateway profile
pwsh walkthroughs/RegisterCreationFlow/test-register-creation.ps1 -Profile gateway
```

**Architecture:**
```
Client (localhost) → API Gateway (port 80)
  → YARP routes /api/registers/* to Register Service (internal)
  → YARP routes /api/validator/* to Validator Service (internal)
  → Services communicate via Docker network
```

**When to use:**
- ✅ Testing production-like routing behavior (RECOMMENDED)
- ✅ Verifying API Gateway configuration
- ✅ Integration testing across services
- ✅ Demonstrating end-to-end flow to stakeholders

### Profile: `direct` (Debugging)

Directly accesses services on exposed ports, bypassing the API Gateway:

```powershell
# Start services with Docker Compose
docker-compose up -d

# Run the walkthrough with direct service access
pwsh walkthroughs/RegisterCreationFlow/test-register-creation.ps1 -Profile direct
```

**Architecture:**
```
Client (localhost) → Register Service (port 5290) - DIRECT
                  → Validator Service (port 5100) - DIRECT
```

**When to use:**
- 🔧 Debugging service-specific issues
- 🔧 Testing service endpoints in isolation
- 🔧 Verifying service health without gateway
- 🔧 Development troubleshooting

### Profile: `docker` (Advanced - Docker Internal Network)

For testing container-to-container communication without localhost exposure:

```powershell
# Run the Docker-internal test script
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-docker.ps1
```

**When to use:**
- Advanced debugging of Docker networking
- Testing DNS resolution between containers
- Simulating internal service communication

## Usage Examples

```powershell
# Default: Via API Gateway (recommended)
pwsh walkthroughs/RegisterCreationFlow/test-register-creation.ps1

# Explicit gateway profile
pwsh walkthroughs/RegisterCreationFlow/test-register-creation.ps1 -Profile gateway

# Direct access for debugging
pwsh walkthroughs/RegisterCreationFlow/test-register-creation.ps1 -Profile direct

# Docker internal network (advanced)
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-docker.ps1
```

## Expected Results

1. ✅ Initiate returns registerId, control record, and nonce
2. ✅ Control record contains owner attestation template
3. ✅ Finalize accepts signed control record
4. ✅ Register created in database
5. ✅ Genesis transaction submitted to Validator
6. ✅ Genesis transaction appears in mempool with HIGH priority

## Key Components

### RegisterControlRecord Structure

```json
{
  "registerId": "abc123...",
  "name": "My Test Register",
  "description": "Testing register creation",
  "tenantId": "tenant-001",
  "createdAt": "2025-01-04T...",
  "metadata": { ... },
  "attestations": [
    {
      "role": "Owner",
      "subject": "did:sorcha:user-001",
      "publicKey": "[base64]",
      "signature": "[base64]",
      "algorithm": "ED25519",
      "grantedAt": "2025-01-04T..."
    }
  ]
}
```

### Genesis Transaction Metadata

- `blueprintId`: "genesis" (special marker)
- `actionId`: "register-creation"
- `priority`: HIGH
- `expiresAt`: null (never expires)
- `metadata.Type`: "Genesis"
- `metadata.RegisterName`: register name
- `metadata.TenantId`: tenant ID

## Security Features

1. **Nonce-based Replay Protection**: Each initiate generates unique nonce
2. **Signature Verification**: All attestations verified against stored hash bytes using Sorcha.Cryptography
3. **Expiration**: Pending registrations expire after 5 minutes
4. **Hash-based Signing**: SHA-256 hash of canonical JSON stored at initiate time, eliminating re-serialization issues
5. **Pre-hashed Signing**: `isPreHashed=true` prevents double-hashing in wallet signing pipeline
6. **Atomic Creation**: Genesis transaction submitted before register persist (no orphaned registers)

## Files in This Walkthrough

- `README.md` - This file
- `test-register-creation.ps1` - PowerShell test script
- `test-register-creation.sh` - Bash test script (Linux/macOS)
- `RESULTS.md` - Test execution results and findings

## Troubleshooting

**Issue**: 404 Not Found on /api/registers/initiate
- **Solution**: Ensure Register Service is running on correct port (check AppHost dashboard)

**Issue**: 401 Unauthorized on finalize
- **Solution**: Check nonce matches between initiate and finalize

**Issue**: Signature verification failure
- **Solution**: Use valid signatures (in demo, we use placeholders - this will fail real verification)

## Real Wallet Signing Integration (test-register-creation-with-real-signing.ps1)

**Status**: Fully Functional (6 of 6 steps working)

This enhanced walkthrough performs real end-to-end cryptographic signing via the Wallet Service:

```powershell
# Test with ED25519 (default)
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1

# Test with NIST P-256
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1 -Algorithm NISTP256

# Test with RSA-4096
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1 -Algorithm RSA4096

# Direct service access for debugging
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1 -Profile direct

# Show full JSON structures (control record, genesis transaction, signed docket)
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1 -ShowJson
```

### Workflow Steps

1. **Admin Authentication** (Tenant Service)
   - Authenticates via `/api/service-auth/token`
   - Obtains bearer token for subsequent requests

2. **Wallet Creation** (Wallet Service)
   - Creates HD wallet with specified algorithm (ED25519/NISTP256/RSA4096)
   - Generates mnemonic (BIP39), derives keys (BIP32/BIP44)
   - Returns wallet address and public key

3. **Register Initiation** (Register Service)
   - POST to `/api/registers/initiate` with `owners` array
   - Computes SHA-256 hash of canonical JSON attestation data
   - Returns hex-encoded hash as `dataToSign` (not canonical JSON)
   - Stores hash bytes in `PendingRegistration.AttestationHashes`

4. **Attestation Signing** (Wallet Service)
   - Converts hex hash to bytes, Base64-encodes for wallet API
   - Signs with `isPreHashed=true` (wallet skips internal SHA-256)
   - Uses derivation path `sorcha:register-attestation`
   - Returns Base64-encoded signature + derived public key

5. **Register Finalization** (Register Service)
   - POST to `/api/registers/finalize` with `signedAttestations` array
   - Verifies each signature against stored hash bytes (no re-serialization)
   - Submits genesis to Validator (atomic: genesis before persist)
   - Creates register in MongoDB only if genesis succeeds

6. **Genesis Verification** (Validator Service)
   - Genesis transaction in mempool with HIGH priority
   - Signed by system wallet with real cryptographic signature
   - Real PayloadHash (SHA-256 of control record)

### Key Design Decisions

- **Hash-based `dataToSign`**: Returns hex SHA-256 hash instead of canonical JSON, eliminating JSON re-serialization determinism issues
- **`isPreHashed` flag**: Prevents double-hashing by telling TransactionService to skip its internal SHA-256 step
- **Stored attestation hashes**: `PendingRegistration.AttestationHashes` dictionary (keyed by `"{role}:{subject}"`) eliminates need to re-serialize at verification time
- **Atomic register+genesis**: FinalizeAsync submits genesis BEFORE persisting register, preventing orphaned registers

## Next Steps

After this walkthrough:
1. Test with multiple attestations (owner + admins)
2. Test with different signature algorithms (NISTP256, RSA4096)
3. Verify docket creation from genesis transaction
4. Implement Redis-backed pending registration storage

## References

- [Register Service Spec](../../.specify/specs/sorcha-register-service.md)
- [RegisterCreationOrchestrator.cs](../../src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs)
- [Genesis Transaction Endpoint](../../src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs)
