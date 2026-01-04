# User and Wallet Creation Walkthrough - Implementation Plan

**Purpose:** Create a walkthrough demonstrating user creation in an organization and wallet setup, then extend to multi-user blueprint sharing scenarios.

**Date Created:** 2026-01-04
**Status:** 🚧 Planning Phase
**Priority:** P2 (Documentation/Testing)

---

## Overview

This walkthrough demonstrates the complete lifecycle of:

### Phase 1: Single User with Wallet
1. Starting Sorcha services (Docker)
2. Creating an organization (if not exists)
3. Creating a user in the organization
4. User authentication (obtaining JWT token)
5. Creating a default wallet for the user
6. Verifying wallet ownership and capabilities

### Phase 2: Multi-User Blueprint Sharing (Extension)
1. Creating multiple users in the same organization
2. Creating wallets for each user
3. Deploying a blueprint that shares data between users
4. Executing blueprint actions that involve multiple wallets
5. Verifying cross-user data sharing and transaction flow

---

## Target Audience

- **Developers** - Understanding user/wallet creation flow
- **QA/Testers** - End-to-end testing scenarios
- **Documentation** - Real-world usage examples
- **Onboarding** - New team members learning the platform

---

## Prerequisites

- Docker Desktop running
- .NET 10 SDK installed
- PowerShell 7+ (Windows) or Bash (Linux/Mac)
- Sorcha services running via `docker-compose up -d`
- Existing bootstrap (organization + admin user) from BlueprintStorageBasic walkthrough

---

## Architecture Components Involved

### Services
1. **Tenant Service** (`localhost:5110`)
   - Organization management
   - User creation and authentication
   - Role-based access control

2. **Wallet Service** (`localhost:5000` or via API Gateway)
   - Wallet creation with HD wallet support
   - Cryptographic key management
   - Transaction signing

3. **Blueprint Service** (`localhost:5002` or via API Gateway)
   - Blueprint storage and retrieval
   - Workflow definition management

4. **API Gateway** (`localhost:80/443`)
   - Unified API access point
   - Request routing

### Libraries
- `Sorcha.ServiceClients` - Consolidated HTTP clients for inter-service communication
- `Sorcha.Cryptography` - ED25519, NIST P-256, RSA-4096 support
- `Sorcha.Blueprint.Models` - Workflow definition models

---

## Phase 1: Implementation Details

### Directory Structure

```
walkthroughs/UserWalletCreation/
├── README.md                           # Walkthrough overview
├── PLAN.md                             # This file - detailed plan
├── PHASE1-RESULTS.md                   # Phase 1 execution results
├── PHASE2-RESULTS.md                   # Phase 2 execution results (future)
├── scripts/
│   ├── phase1-create-user-wallet.ps1  # Main Phase 1 script
│   ├── phase1-create-user-wallet.sh   # Linux/Mac version
│   ├── test-user-login.ps1            # Test authentication
│   ├── test-wallet-creation.ps1       # Test wallet operations
│   └── helpers.ps1                     # Shared helper functions
├── data/
│   ├── test-users.json                # Sample user configurations
│   └── wallet-configs.json            # Sample wallet configurations
└── blueprints/
    └── multi-user-data-share.json     # Phase 2 blueprint (future)
```

---

## Phase 1: Script Specifications

### 1. Main Script: `phase1-create-user-wallet.ps1`

**Purpose:** Complete workflow for creating a user and their default wallet

**Parameters:**
- `-ApiBaseUrl` (string, default: "http://localhost") - API Gateway URL
- `-TenantServiceUrl` (string, default: "http://localhost:5110") - Direct Tenant Service URL
- `-WalletServiceUrl` (string, default: "http://localhost:5000") - Direct Wallet Service URL
- `-OrgId` (Guid, optional) - Existing organization ID (if known)
- `-OrgSubdomain` (string, optional) - Organization subdomain to lookup
- `-UserEmail` (string, required) - New user email address
- `-UserDisplayName` (string, required) - New user display name
- `-UserPassword` (string, required) - New user password (local auth)
- `-UserRoles` (string[], default: @("Member")) - User roles (Member, Designer, Developer, etc.)
- `-WalletName` (string, default: "Default Wallet") - Wallet display name
- `-WalletAlgorithm` (string, default: "ED25519") - Crypto algorithm (ED25519, NISTP256, RSA4096)
- `-MnemonicWordCount` (int, default: 12) - BIP39 word count (12 or 24)
- `-SaveMnemonicPath` (string, optional) - File path to save mnemonic (for testing only!)
- `-AdminEmail` (string, default: "stuart.mackintosh@sorcha.dev") - Admin credentials for user creation
- `-AdminPassword` (string, default: "SorchaDev2025!") - Admin password
- `-Verbose` (switch) - Detailed output

**Workflow:**

```powershell
# Step 1: Authenticate as admin
Write-Host "==> Step 1: Admin Authentication" -ForegroundColor Cyan
$adminToken = Get-AdminToken -Email $AdminEmail -Password $AdminPassword -TenantServiceUrl $TenantServiceUrl

# Step 2: Resolve organization ID
Write-Host "==> Step 2: Resolve Organization" -ForegroundColor Cyan
if ($OrgId) {
    $orgId = $OrgId
} elseif ($OrgSubdomain) {
    $org = Get-OrganizationBySubdomain -Subdomain $OrgSubdomain -Token $adminToken
    $orgId = $org.Id
} else {
    Write-Error "Must provide either OrgId or OrgSubdomain"
}

# Step 3: Create user in organization
Write-Host "==> Step 3: Create User" -ForegroundColor Cyan
$createUserRequest = @{
    email = $UserEmail
    displayName = $UserDisplayName
    password = $UserPassword
    roles = $UserRoles
}
$user = Invoke-RestMethod -Uri "$TenantServiceUrl/api/organizations/$orgId/users" `
    -Method POST `
    -Headers @{ Authorization = "Bearer $adminToken" } `
    -Body ($createUserRequest | ConvertTo-Json) `
    -ContentType "application/json"

Write-Host "  ✓ User created: $($user.id)" -ForegroundColor Green
Write-Host "    Email: $($user.email)" -ForegroundColor Gray
Write-Host "    Display Name: $($user.displayName)" -ForegroundColor Gray
Write-Host "    Roles: $($user.roles -join ', ')" -ForegroundColor Gray

# Step 4: User login (get user token)
Write-Host "==> Step 4: User Login" -ForegroundColor Cyan
$loginRequest = @{
    email = $UserEmail
    password = $UserPassword
}
$tokenResponse = Invoke-RestMethod -Uri "$TenantServiceUrl/api/auth/login" `
    -Method POST `
    -Body ($loginRequest | ConvertTo-Json) `
    -ContentType "application/json"

$userToken = $tokenResponse.accessToken
Write-Host "  ✓ User authenticated" -ForegroundColor Green
Write-Host "    Token expires in: $($tokenResponse.expiresIn) seconds" -ForegroundColor Gray

# Step 5: Create wallet for user
Write-Host "==> Step 5: Create Wallet" -ForegroundColor Cyan
$createWalletRequest = @{
    name = $WalletName
    algorithm = $WalletAlgorithm
    wordCount = $MnemonicWordCount
}
$walletResponse = Invoke-RestMethod -Uri "$WalletServiceUrl/api/v1/wallets" `
    -Method POST `
    -Headers @{ Authorization = "Bearer $userToken" } `
    -Body ($createWalletRequest | ConvertTo-Json) `
    -ContentType "application/json"

Write-Host "  ✓ Wallet created!" -ForegroundColor Green
Write-Host "    Address: $($walletResponse.wallet.address)" -ForegroundColor Yellow
Write-Host "    Name: $($walletResponse.wallet.name)" -ForegroundColor Gray
Write-Host "    Algorithm: $($walletResponse.wallet.algorithm)" -ForegroundColor Gray
Write-Host "    Public Key: $($walletResponse.wallet.publicKey.Substring(0,32))..." -ForegroundColor Gray

# Step 6: Display mnemonic warning
Write-Host ""
Write-Host "⚠️  CRITICAL: SAVE YOUR MNEMONIC PHRASE ⚠️" -ForegroundColor Red -BackgroundColor Yellow
Write-Host ""
Write-Host "Mnemonic Words:" -ForegroundColor Yellow
$walletResponse.mnemonicWords | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
Write-Host ""
Write-Host $walletResponse.warning -ForegroundColor Red
Write-Host ""

# Step 7: Optionally save mnemonic to file (TEST ONLY!)
if ($SaveMnemonicPath) {
    $mnemonicData = @{
        walletAddress = $walletResponse.wallet.address
        mnemonicWords = $walletResponse.mnemonicWords
        createdAt = Get-Date -Format "o"
        warning = "NEVER commit this file to source control!"
    }
    $mnemonicData | ConvertTo-Json | Set-Content -Path $SaveMnemonicPath
    Write-Host "  ⚠️  Mnemonic saved to: $SaveMnemonicPath (DELETE AFTER TESTING!)" -ForegroundColor Yellow
}

# Step 8: Verify wallet ownership
Write-Host "==> Step 6: Verify Wallet Ownership" -ForegroundColor Cyan
$wallets = Invoke-RestMethod -Uri "$WalletServiceUrl/api/v1/wallets" `
    -Method GET `
    -Headers @{ Authorization = "Bearer $userToken" }

Write-Host "  ✓ User has $($wallets.Count) wallet(s)" -ForegroundColor Green
$wallets | ForEach-Object {
    Write-Host "    - $($_.name): $($_.address)" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "==> ✅ PHASE 1 COMPLETE ✅" -ForegroundColor Green -BackgroundColor Black
Write-Host ""
Write-Host "User Details:" -ForegroundColor Cyan
Write-Host "  Email: $UserEmail" -ForegroundColor White
Write-Host "  User ID: $($user.id)" -ForegroundColor White
Write-Host "  Organization ID: $orgId" -ForegroundColor White
Write-Host ""
Write-Host "Wallet Details:" -ForegroundColor Cyan
Write-Host "  Address: $($walletResponse.wallet.address)" -ForegroundColor White
Write-Host "  Algorithm: $WalletAlgorithm" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Save the mnemonic phrase in a secure location" -ForegroundColor White
Write-Host "  2. Test wallet signing: .\scripts\test-wallet-signing.ps1 -WalletAddress '$($walletResponse.wallet.address)'" -ForegroundColor White
Write-Host "  3. Create additional users for multi-user scenarios (Phase 2)" -ForegroundColor White
Write-Host ""
```

**Output:**
- Console output with color-coded status messages
- User details (ID, email, roles)
- Wallet details (address, public key, algorithm)
- Mnemonic phrase (with strong warning)
- Optional mnemonic file (for testing only)

---

### 2. Helper Script: `test-user-login.ps1`

**Purpose:** Test user authentication and token validation

**Parameters:**
- `-Email` (string, required)
- `-Password` (string, required)
- `-TenantServiceUrl` (string, default: "http://localhost:5110")

**Functionality:**
- Authenticate user
- Decode JWT token payload
- Display claims (user_id, org_id, roles, exp, iat)
- Verify token not expired

**Example Output:**
```
==> Testing User Login
  ✓ Authentication successful

Token Claims:
  sub: 7a234567-89ab-cdef-0123-456789abcdef
  email: john.doe@example.com
  name: John Doe
  org_id: 1a234567-89ab-cdef-0123-456789abcdef
  org_name: Sorcha Development
  roles: Member, Designer
  iat: 2026-01-04T10:30:00Z
  exp: 2026-01-04T11:30:00Z (expires in 59 minutes)
  token_type: access

  ✓ Token is valid
```

---

### 3. Helper Script: `test-wallet-creation.ps1`

**Purpose:** Test wallet creation with various configurations

**Parameters:**
- `-UserToken` (string, required) - JWT token
- `-WalletServiceUrl` (string, default: "http://localhost:5000")
- `-TestAll` (switch) - Test all algorithms (ED25519, NISTP256, RSA4096)

**Functionality:**
- Create wallets with different algorithms
- Test 12 vs 24-word mnemonics
- List all user wallets
- Compare performance/capabilities

---

### 4. Data Files

**`data/test-users.json`**
```json
{
  "users": [
    {
      "email": "alice@example.com",
      "displayName": "Alice Johnson",
      "password": "SecurePass123!",
      "roles": ["Member", "Designer"],
      "walletName": "Alice's Primary Wallet",
      "walletAlgorithm": "ED25519"
    },
    {
      "email": "bob@example.com",
      "displayName": "Bob Smith",
      "password": "SecurePass123!",
      "roles": ["Member", "Developer"],
      "walletName": "Bob's Dev Wallet",
      "walletAlgorithm": "NISTP256"
    },
    {
      "email": "charlie@example.com",
      "displayName": "Charlie Brown",
      "password": "SecurePass123!",
      "roles": ["Member"],
      "walletName": "Charlie's Wallet",
      "walletAlgorithm": "ED25519"
    }
  ]
}
```

**`data/wallet-configs.json`**
```json
{
  "algorithms": [
    {
      "name": "ED25519",
      "description": "Edwards-curve Digital Signature Algorithm (recommended)",
      "keySize": 256,
      "signatureSize": 64,
      "performance": "Fast",
      "securityLevel": "High"
    },
    {
      "name": "NISTP256",
      "description": "NIST P-256 Elliptic Curve (ECDSA)",
      "keySize": 256,
      "signatureSize": 64,
      "performance": "Medium",
      "securityLevel": "High"
    },
    {
      "name": "RSA4096",
      "description": "RSA with 4096-bit keys",
      "keySize": 4096,
      "signatureSize": 512,
      "performance": "Slower",
      "securityLevel": "Very High"
    }
  ],
  "mnemonicOptions": [
    {
      "wordCount": 12,
      "entropy": 128,
      "checksum": 4,
      "security": "Standard"
    },
    {
      "wordCount": 24,
      "entropy": 256,
      "checksum": 8,
      "security": "Maximum"
    }
  ]
}
```

---

## Phase 2: Multi-User Blueprint Sharing (Future Extension)

### Objectives
1. Create 3+ users in the same organization
2. Create wallets for each user
3. Deploy a blueprint that requires multi-wallet interaction
4. Execute blueprint actions with data sharing
5. Verify transaction signatures from multiple wallets

### Blueprint Example: "Invoice Approval Workflow"

**Scenario:**
- **Alice** (Vendor) submits an invoice
- **Bob** (Approver) reviews and approves
- **Charlie** (Finance) processes payment

**Blueprint Actions:**
1. `SubmitInvoice` - Alice signs transaction with invoice data
2. `ReviewInvoice` - Bob signs approval transaction
3. `ProcessPayment` - Charlie signs payment transaction

**Data Flow:**
- Invoice transaction references Alice's wallet address
- Approval transaction references Bob's wallet + invoice transaction ID
- Payment transaction references all previous transactions

**Implementation:** (Phase 2 script)
```powershell
# Create all users and wallets (using Phase 1 script in loop)
# Deploy multi-user blueprint
# Execute blueprint actions with wallet signing
# Verify transaction chain on Register Service
```

---

## Testing Strategy

### Manual Testing
1. Run Phase 1 script with sample user
2. Verify user exists in Tenant Service
3. Verify wallet exists in Wallet Service
4. Test user login independently
5. Test wallet signing operations

### Automated Testing
1. Integration tests for user creation API
2. Integration tests for wallet creation API
3. End-to-end test: user → wallet → blueprint execution
4. Performance testing: 100+ users with wallets

### Edge Cases
- [ ] User with existing email (conflict)
- [ ] Invalid wallet algorithm
- [ ] Expired JWT token during wallet creation
- [ ] Wallet creation without authentication
- [ ] User with no roles (default to Member)
- [ ] Organization doesn't exist (error handling)
- [ ] Mnemonic recovery (test restore from saved mnemonic)

---

## Documentation Requirements

### README.md
- Quick start guide
- Prerequisites
- Step-by-step walkthrough
- Troubleshooting common issues
- Links to API documentation

### PHASE1-RESULTS.md
- Execution results with screenshots
- Sample output
- Performance metrics (time to create user, wallet)
- Known limitations
- Next steps

### PHASE2-RESULTS.md (Future)
- Multi-user scenario results
- Blueprint execution flow
- Transaction verification
- Cross-wallet data sharing demonstration

---

## Success Criteria

### Phase 1
- ✅ Script successfully creates user in organization
- ✅ User can authenticate and receive JWT token
- ✅ User can create default wallet
- ✅ Mnemonic phrase displayed with strong security warning
- ✅ Wallet ownership verified (user can list their wallets)
- ✅ Script handles errors gracefully
- ✅ Documentation is clear and comprehensive

### Phase 2 (Future)
- ✅ Multiple users created with unique wallets
- ✅ Blueprint deployed that requires multi-wallet signing
- ✅ Blueprint actions executed by different users
- ✅ Transaction chain verified on Register Service
- ✅ Cross-user data sharing demonstrated

---

## Dependencies

### Services Required
- ✅ Tenant Service (user management)
- ✅ Wallet Service (wallet creation)
- 🚧 Blueprint Service (Phase 2)
- 🚧 Register Service (Phase 2 - transaction storage)

### External Dependencies
- Docker Desktop
- PowerShell 7+ or Bash
- .NET 10 SDK (for running services)
- curl or Invoke-RestMethod (HTTP client)

### Configuration
- Sorcha services running via Docker Compose
- API Gateway routing configured
- Existing organization (from bootstrap)

---

## Timeline Estimate

### Phase 1: Single User with Wallet
- **Planning:** ✅ Complete (this document)
- **Script Development:** 2-3 hours
  - Main script: 1.5 hours
  - Helper scripts: 1 hour
  - Data files: 0.5 hours
- **Testing:** 1-2 hours
  - Manual testing: 1 hour
  - Edge case testing: 1 hour
- **Documentation:** 1-2 hours
  - README: 1 hour
  - RESULTS: 1 hour

**Total Phase 1:** 4-7 hours

### Phase 2: Multi-User Blueprint Sharing
- **Planning:** 1 hour
- **Blueprint Design:** 2 hours
- **Script Development:** 3-4 hours
- **Testing:** 2-3 hours
- **Documentation:** 2 hours

**Total Phase 2:** 10-12 hours

**Overall Total:** 14-19 hours

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Service not running | High | Add health check before script execution |
| Authentication failure | High | Detailed error messages, token validation |
| Mnemonic loss | Critical | Strong warnings, optional test-mode save |
| API changes | Medium | Version API endpoints, update docs |
| Docker networking issues | Medium | Support both direct URLs and API Gateway |
| Password complexity requirements | Low | Document password requirements clearly |

---

## Future Enhancements

### Phase 3: Advanced Scenarios
- HD wallet address derivation (BIP44 paths)
- Multi-signature wallets
- Wallet recovery from mnemonic
- Wallet backup/export
- Encrypted wallet export

### Phase 4: Integration with Other Services
- Register Service transaction submission
- Peer Service data synchronization
- Blueprint execution with wallet signing
- Real-time notifications (SignalR)

### Phase 5: UI Integration
- Blazor Admin UI user management
- Wallet creation wizard
- Mnemonic backup flow
- QR code display for wallet addresses

---

## References

### Internal Documentation
- [CLAUDE.md](../../CLAUDE.md) - AI assistant guide
- [constitution.md](../../.specify/constitution.md) - Architecture principles
- [sorcha-wallet-service.md](../../.specify/specs/sorcha-wallet-service.md) - Wallet Service spec
- [sorcha-tenant-service.md](../../.specify/specs/sorcha-tenant-service.md) - Tenant Service spec
- [BlueprintStorageBasic README](../BlueprintStorageBasic/README.md) - Previous walkthrough

### API Documentation
- Tenant Service API: http://localhost:5110/scalar
- Wallet Service API: http://localhost:5000/scalar
- API Gateway Docs: http://localhost/scalar

### External Standards
- [BIP32](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki) - Hierarchical Deterministic Wallets
- [BIP39](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki) - Mnemonic Phrases
- [BIP44](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki) - Multi-Account Hierarchy

---

## Approval and Sign-off

**Plan Created By:** AI Assistant (Claude)
**Date:** 2026-01-04
**Status:** 🚧 Awaiting User Approval

**Next Steps After Approval:**
1. Create directory structure
2. Implement Phase 1 scripts
3. Test with running Sorcha instance
4. Document results in PHASE1-RESULTS.md
5. Update walkthroughs/README.md with new entry

---

**Questions for User:**
1. Should we use the existing bootstrap admin credentials, or create a dedicated test admin?
2. Do you want both PowerShell and Bash versions of scripts, or PowerShell-first?
3. Should Phase 1 include wallet recovery testing, or save that for a separate walkthrough?
4. Do you have a preferred blueprint scenario for Phase 2, or use the Invoice Approval example?
5. Should we integrate this with existing walkthroughs (e.g., extend BlueprintStorageBasic)?
