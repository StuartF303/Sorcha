# Register Creation Flow Walkthrough

## Overview

This walkthrough demonstrates the complete two-phase register creation workflow with cryptographic attestations and genesis transaction processing.

## What This Tests

1. **Phase 1 - Initiate**: POST to `/api/registers/initiate`
   - Generates unsigned control record with attestation templates
   - Returns registerId, nonce, and data to sign
   - Control record stored in pending state (5-minute expiration)

2. **Signing Phase** (Client-side):
   - Client signs the control record hash with wallet private key
   - In this demo, we use placeholder signatures for testing

3. **Phase 2 - Finalize**: POST to `/api/registers/finalize`
   - Validates nonce (replay protection)
   - Verifies all attestation signatures
   - Creates register in database
   - Submits genesis transaction to Validator Service mempool

4. **Genesis Transaction Processing**:
   - Validator Service receives genesis transaction
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
│  │  - Create control record       │    │
│  │  - Compute canonical JSON hash │    │
│  │  - Store in pending state      │    │
│  └────────────────────────────────┘    │
└────────┬────────────────────────────────┘
         │
         │ 2. Returns: registerId, nonce, dataToSign
         ▼
┌─────────────────┐
│   Client/CLI    │
│  [Sign data]    │  ◄── Wallet signs hash with private key
└────────┬────────┘
         │
         │ 3. POST /api/registers/finalize (with signatures)
         ▼
┌─────────────────────────────────────────┐
│     Register Service                    │
│  ┌────────────────────────────────┐    │
│  │ RegisterCreationOrchestrator   │    │
│  │  - Verify nonce                │    │
│  │  - Verify signatures           │    │
│  │  - Create register in DB       │    │
│  │  - Build genesis transaction   │    │
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
2. **Signature Verification**: All attestations verified using Sorcha.Cryptography
3. **Expiration**: Pending registrations expire after 5 minutes
4. **Canonical JSON**: RFC 8785 compliant hashing for signature consistency

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

**Status**: 4 of 5 steps working (80% functional)

This enhanced walkthrough integrates real cryptographic signing via the Wallet Service:

```powershell
# Test with ED25519 (default)
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1

# Test with NIST P-256
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1 -Algorithm NISTP256

# Test with RSA-4096
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1 -Algorithm RSA4096

# Direct service access for debugging
pwsh walkthroughs/RegisterCreationFlow/test-register-creation-with-real-signing.ps1 -Profile direct
```

### Workflow Steps

1. ✅ **Admin Authentication** (Tenant Service)
   - Authenticates via `/api/service-auth/token`
   - Obtains bearer token for subsequent requests
   - Status: **WORKING**

2. ✅ **Wallet Creation** (Wallet Service)
   - Creates HD wallet with specified algorithm (ED25519/NISTP256/RSA4096)
   - Generates mnemonic (BIP39), derives keys (BIP32/BIP44)
   - Returns wallet address and public key
   - Status: **WORKING**

3. ✅ **Register Initiation** (Register Service)
   - POST to `/api/registers/initiate`
   - Generates canonical JSON hash of control record
   - Returns registerId, nonce, and hash to sign
   - Status: **WORKING**

4. ✅ **Data Signing** (Wallet Service)
   - Signs SHA-256 hash with wallet private key
   - POST to `/api/v1/wallets/{address}/sign`
   - Returns Base64-encoded signature
   - Status: **WORKING**

5. ❌ **Signature Verification** (Register Service)
   - POST to `/api/registers/finalize` with signed control record
   - Verifies signature using `Sorcha.Cryptography.ICryptoModule.VerifyAsync()`
   - Status: **FAILING** (HTTP 401 Unauthorized)

### Known Issues

#### Issue: Signature Verification Failure

**Symptom**: Finalize endpoint returns HTTP 401 with log message:
```
Signature verification failed for attestation: subject=did:sorcha:admin, role=Owner
```

**Investigation Findings**:
- ✅ Signature byte length correct for ED25519 (64 bytes)
- ✅ Public key byte length correct (32 bytes)
- ✅ Hash byte length correct (32 bytes for SHA-256)
- ✅ Base64 encoding/decoding working correctly
- ✅ Wallet signing operation succeeds
- ❌ Cryptographic verification fails

**Possible Root Causes** (under investigation):
1. **Canonical JSON Mismatch**: The control record JSON serialization in finalize may differ from initiate
   - Initiate computes hash: `JsonSerializer.Serialize(controlRecord, _canonicalJsonOptions)` (line 117)
   - Finalize receives already-serialized control record from client
   - May have different field ordering, whitespace, or null handling

2. **Data Signed vs Data Verified Mismatch**:
   - Wallet signs: SHA-256 hash of canonical JSON (32 bytes)
   - Register verifies: Expects same 32-byte hash
   - If control record is reconstructed differently, hashes won't match

3. **Algorithm Parameter Mismatch**:
   - Wallet uses: `WalletNetworks.ED25519` → `Ed25519.Sign()`
   - Register uses: `MapAlgorithm(SignatureAlgorithm.ED25519)` → `ICryptoModule.VerifyAsync()`
   - Potential parameter mismatch in verification call

**Code References**:
- Signing: `WalletManager.cs` lines 550-593
- Verification: `RegisterCreationOrchestrator.cs` lines 317-370
- Hash computation: `RegisterCreationOrchestrator.cs` lines 117-121

**Next Steps**:
1. Add detailed logging of canonical JSON in both initiate and finalize
2. Compare hex dumps of data signed vs data verified
3. Verify Sorcha.Cryptography test coverage for ED25519 verification
4. Consider adding debug endpoint to return signed data for comparison

### Technical Fixes Applied

**Fix #1: Wallet Response Structure** (Commit: 6d80caf)
- Wallet API returns nested `{ wallet: {...}, mnemonicWords: [...] }` structure
- Updated scripts to access `$response.wallet.address` instead of `$response.address`

**Fix #2: Pending Registration State Loss** (Commit: 6d80caf)
- Created `IPendingRegistrationStore` singleton service
- Implemented `PendingRegistrationStore` with `ConcurrentDictionary`
- Prevents state loss between initiate and finalize requests
- DI: Orchestrator (scoped) injects store (singleton)

**Fix #3: Control Record Request Structure** (Commit: 6d80caf)
- Finalize endpoint expects full control record with attestations
- Updated request to include: registerId, name, description, tenantId, metadata, attestations
- Attestations include: role, subject, publicKey (Base64), signature (Base64), algorithm

## Next Steps

After this walkthrough:
1. ~~Integrate real wallet signing (Wallet Service)~~ ✅ **COMPLETED**
2. **Resolve signature verification issue** (current blocker)
3. Test with multiple attestations (owner + admins)
4. Test with different signature algorithms (NISTP256, RSA4096)
5. Verify docket creation from genesis transaction

## References

- [Register Service Spec](../../.specify/specs/sorcha-register-service.md)
- [RegisterCreationOrchestrator.cs](../../src/Services/Sorcha.Register.Service/Services/RegisterCreationOrchestrator.cs)
- [Genesis Transaction Endpoint](../../src/Services/Sorcha.Validator.Service/Endpoints/ValidationEndpoints.cs)
