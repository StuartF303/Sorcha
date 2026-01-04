# User and Wallet Creation Walkthrough

**Purpose:** Demonstrate creating users in an organization and setting up their default wallets, with future extension to multi-user blueprint sharing scenarios.

**Date Created:** 2026-01-04
**Status:** 🚧 Planning Phase
**Prerequisites:** Docker Desktop, .NET 10 SDK, PowerShell 7+, Running Sorcha services

---

## Overview

This walkthrough teaches you how to:
1. ✅ **Phase 1:** Create a user in an existing organization and set up their default wallet
2. 🚧 **Phase 2:** Create multiple users and deploy a blueprint that shares data between them

**What you'll learn:**
- User management via Tenant Service API
- Wallet creation with HD wallet (BIP32/BIP39/BIP44) support
- JWT authentication flow for users
- Multi-tenancy (organization isolation)
- Cryptographic key algorithms (ED25519, NIST P-256, RSA-4096)
- Mnemonic phrase management and security

---

## Files in This Walkthrough

### Documentation
- **[README.md](./README.md)** - This file - walkthrough overview
- **[PLAN.md](./PLAN.md)** - Detailed implementation plan and specifications
- **PHASE1-RESULTS.md** - Phase 1 execution results (created after testing)
- **PHASE2-RESULTS.md** - Phase 2 results (future)

### Scripts (Phase 1)
- **scripts/phase1-create-user-wallet.ps1** - Main script for user + wallet creation
- **scripts/test-user-login.ps1** - Test user authentication and JWT token
- **scripts/test-wallet-creation.ps1** - Test wallet creation with various algorithms
- **scripts/helpers.ps1** - Shared helper functions

### Data Files
- **data/test-users.json** - Sample user configurations
- **data/wallet-configs.json** - Wallet algorithm specifications

### Blueprints (Phase 2 - Future)
- **blueprints/multi-user-data-share.json** - Multi-user workflow example

---

## Quick Start

### Prerequisites Check

Before running this walkthrough, ensure:

```powershell
# 1. Docker Desktop is running
docker --version
# Expected: Docker version 20.x or higher

# 2. Sorcha services are running
docker-compose ps
# Expected: All services in "Up" state

# 3. Platform is bootstrapped (org + admin user exists)
curl http://localhost/api/health
# Expected: HTTP 200 OK with service health status

# 4. You have admin credentials
# Default: stuart.mackintosh@sorcha.dev / SorchaDev2025!
```

If you haven't bootstrapped yet, see [BlueprintStorageBasic walkthrough](../BlueprintStorageBasic/).

---

## Phase 1: Single User with Wallet

### Step 1: Start Sorcha Services

```bash
# From repository root
docker-compose up -d

# Wait for services to be healthy (30-60 seconds)
docker-compose ps
```

### Step 2: Run the User Creation Script

```powershell
# From repository root
powershell -ExecutionPolicy Bypass -File walkthroughs/UserWalletCreation/scripts/phase1-create-user-wallet.ps1 `
  -UserEmail "alice@example.com" `
  -UserDisplayName "Alice Johnson" `
  -UserPassword "SecurePass123!" `
  -UserRoles @("Member", "Designer") `
  -WalletName "Alice's Primary Wallet" `
  -WalletAlgorithm "ED25519" `
  -OrgSubdomain "demo" `
  -AdminEmail "stuart.mackintosh@sorcha.dev" `
  -AdminPassword "SorchaDev2025!" `
  -Verbose
```

**Expected Output:**
```
==> Step 1: Admin Authentication
  ✓ Admin authenticated

==> Step 2: Resolve Organization
  ✓ Organization found: Sorcha Development (demo)
    Organization ID: 1a234567-89ab-cdef-0123-456789abcdef

==> Step 3: Create User
  ✓ User created: 7a234567-89ab-cdef-0123-456789abcdef
    Email: alice@example.com
    Display Name: Alice Johnson
    Roles: Member, Designer

==> Step 4: User Login
  ✓ User authenticated
    Token expires in: 3600 seconds

==> Step 5: Create Wallet
  ✓ Wallet created!
    Address: SORCHA1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0
    Name: Alice's Primary Wallet
    Algorithm: ED25519
    Public Key: 0x1a2b3c4d5e6f7890abcdef...

⚠️  CRITICAL: SAVE YOUR MNEMONIC PHRASE ⚠️

Mnemonic Words:
  abandon
  ability
  able
  about
  above
  absent
  absorb
  abstract
  absurd
  abuse
  access
  accident

⚠️ NEVER share your mnemonic phrase! It cannot be recovered if lost. ⚠️

==> Step 6: Verify Wallet Ownership
  ✓ User has 1 wallet(s)
    - Alice's Primary Wallet: SORCHA1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0

==> ✅ PHASE 1 COMPLETE ✅

User Details:
  Email: alice@example.com
  User ID: 7a234567-89ab-cdef-0123-456789abcdef
  Organization ID: 1a234567-89ab-cdef-0123-456789abcdef

Wallet Details:
  Address: SORCHA1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0
  Algorithm: ED25519

Next Steps:
  1. Save the mnemonic phrase in a secure location
  2. Test wallet signing: .\scripts\test-wallet-signing.ps1 -WalletAddress 'SORCHA1a2b3c4d...'
  3. Create additional users for multi-user scenarios (Phase 2)
```

### Step 3: Test User Authentication

```powershell
# Test that the new user can log in
powershell -File walkthroughs/UserWalletCreation/scripts/test-user-login.ps1 `
  -Email "alice@example.com" `
  -Password "SecurePass123!"
```

### Step 4: Test Wallet Operations

```powershell
# Test wallet creation with different algorithms
powershell -File walkthroughs/UserWalletCreation/scripts/test-wallet-creation.ps1 `
  -Email "alice@example.com" `
  -Password "SecurePass123!" `
  -TestAll
```

---

## Phase 2: Multi-User Blueprint Sharing (Future)

**Status:** 🚧 Planned

This phase will demonstrate:
1. Creating multiple users (Alice, Bob, Charlie)
2. Creating wallets for each user
3. Deploying a blueprint that requires multi-wallet interaction
4. Executing blueprint actions with data sharing between users
5. Verifying transaction chain with multiple signatures

**Example Scenario:** Invoice Approval Workflow
- Alice (Vendor) submits invoice → signs with her wallet
- Bob (Approver) reviews and approves → signs with his wallet
- Charlie (Finance) processes payment → signs with his wallet

See [PLAN.md](./PLAN.md) for detailed Phase 2 specifications.

---

## Service APIs Used

### Tenant Service (`localhost:5110`)

**User Creation:**
```http
POST /api/organizations/{orgId}/users
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "email": "alice@example.com",
  "displayName": "Alice Johnson",
  "password": "SecurePass123!",
  "roles": ["Member", "Designer"]
}
```

**User Login:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecurePass123!"
}
```

### Wallet Service (`localhost:5000`)

**Create Wallet:**
```http
POST /api/v1/wallets
Authorization: Bearer {userToken}
Content-Type: application/json

{
  "name": "Alice's Primary Wallet",
  "algorithm": "ED25519",
  "wordCount": 12
}
```

**List Wallets:**
```http
GET /api/v1/wallets
Authorization: Bearer {userToken}
```

---

## Wallet Algorithms Comparison

| Algorithm | Key Size | Signature Size | Performance | Security Level | Recommended For |
|-----------|----------|----------------|-------------|----------------|-----------------|
| **ED25519** | 256 bits | 64 bytes | Fast | High | ✅ Default choice (recommended) |
| **NISTP256** | 256 bits | 64 bytes | Medium | High | NIST compliance requirements |
| **RSA4096** | 4096 bits | 512 bytes | Slower | Very High | Maximum security scenarios |

**Recommendation:** Use **ED25519** unless you have specific requirements for NIST compliance or maximum key size.

---

## Mnemonic Security Best Practices

### ✅ DO
- **Save immediately** - Write down the mnemonic phrase on paper
- **Store securely** - Use a safe, fireproof location
- **Use passphrase** - Add optional passphrase for extra security
- **Test recovery** - Verify you can recover the wallet before depositing assets
- **Backup redundancy** - Keep multiple secure copies in different locations

### ❌ DON'T
- **Screenshot** - Never take screenshots of the mnemonic
- **Cloud storage** - Never save to cloud drives (iCloud, Google Drive, Dropbox)
- **Email/SMS** - Never send via email or text message
- **Share** - Never share with anyone (Sorcha support will never ask for it)
- **Digital storage** - Avoid storing in password managers (use paper backup)

### ⚠️ WARNING
**The mnemonic phrase is the ONLY way to recover your wallet.** If you lose it:
- Your wallet cannot be recovered
- Your private keys are lost forever
- Any assets in the wallet are permanently inaccessible

**Sorcha does NOT store your mnemonic phrase.** Only you have access to it.

---

## Troubleshooting

### User creation fails with "Conflict"
**Problem:** User with email already exists in organization

**Solution:**
```powershell
# Use a different email address, or delete the existing user:
# (Requires admin authentication)
DELETE /api/organizations/{orgId}/users/{userId}
```

### Wallet creation fails with "Unauthorized"
**Problem:** JWT token expired or invalid

**Solution:**
```powershell
# Re-authenticate to get a fresh token
POST /api/auth/login
# Tokens expire after 60 minutes (default)
```

### "Algorithm not supported" error
**Problem:** Invalid wallet algorithm specified

**Solution:**
```powershell
# Use one of the supported algorithms:
# - "ED25519" (recommended)
# - "NISTP256"
# - "RSA4096"
```

### Services not responding
**Problem:** Docker containers not running or unhealthy

**Solution:**
```bash
# Check service status
docker-compose ps

# Restart specific service
docker-compose restart wallet-service

# View logs
docker-compose logs -f wallet-service

# Restart all services
docker-compose restart
```

### Organization not found
**Problem:** Organization ID or subdomain incorrect

**Solution:**
```powershell
# List all organizations (requires admin token)
GET /api/organizations

# Verify your subdomain matches
# Default from bootstrap: "demo"
```

---

## Access Points

| Service | URL | Purpose |
|---------|-----|---------|
| API Gateway | http://localhost/ | Main entry point |
| Tenant Service (direct) | http://localhost:5110 | User/org management |
| Wallet Service (direct) | http://localhost:5000 | Wallet operations |
| API Documentation | http://localhost/scalar/ | Interactive API docs |
| Health Check | http://localhost/api/health | Service status |

---

## Credentials

### Bootstrap Admin User
- **Email:** stuart.mackintosh@sorcha.dev
- **Password:** SorchaDev2025!
- **Organization:** Sorcha Development (subdomain: demo)
- **Roles:** Administrator, SystemAdmin

### Test Users (Created by Walkthrough)
See [data/test-users.json](./data/test-users.json) for sample user configurations.

---

## Next Steps

After completing this walkthrough:

1. **Create Multiple Users** - Run the script multiple times with different user details
2. **Test Wallet Recovery** - Use the mnemonic to recover a wallet on a different installation
3. **Explore HD Wallet Features** - Derive multiple addresses from a single wallet (BIP44)
4. **Sign Transactions** - Use wallet to sign blueprint execution transactions
5. **Phase 2 Preparation** - Design multi-user blueprint scenario

---

## Related Documentation

### Sorcha Documentation
- [BlueprintStorageBasic Walkthrough](../BlueprintStorageBasic/) - Getting started with Docker setup
- [AdminIntegration Walkthrough](../AdminIntegration/) - Admin UI integration
- [Wallet Service Specification](../../.specify/specs/sorcha-wallet-service.md)
- [Tenant Service Specification](../../.specify/specs/sorcha-tenant-service.md)
- [Cryptography Specification](../../.specify/specs/sorcha-cryptography-rewrite.md)

### External Standards
- [BIP32: HD Wallets](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki)
- [BIP39: Mnemonic Phrases](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)
- [BIP44: Multi-Account Hierarchy](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki)

### API Documentation
- Tenant Service API: http://localhost:5110/scalar
- Wallet Service API: http://localhost:5000/scalar

---

## Contributing

Found an issue or have suggestions for improvement?

1. Test the scripts with different scenarios
2. Document any edge cases or errors encountered
3. Propose enhancements in [PLAN.md](./PLAN.md)
4. Update [PHASE1-RESULTS.md](./PHASE1-RESULTS.md) with your findings

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 0.1.0 | 2026-01-04 | Initial planning and documentation |

---

**Ready to create users and wallets?** Follow the [Quick Start](#quick-start) guide above!

For detailed implementation plan, see [PLAN.md](./PLAN.md).
