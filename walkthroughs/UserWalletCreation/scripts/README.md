# UserWalletCreation Scripts

This directory contains PowerShell scripts for the UserWalletCreation walkthrough.

---

## Scripts Overview

### Main Workflow

**[phase1-create-user-wallet.ps1](./phase1-create-user-wallet.ps1)**
- **Purpose:** Complete Phase 1 workflow (user + wallet creation)
- **Usage:** Create a single user with their default wallet
- **Example:**
  ```powershell
  .\phase1-create-user-wallet.ps1 `
      -UserEmail "alice@example.com" `
      -UserDisplayName "Alice Johnson" `
      -UserPassword "SecurePass123!" `
      -OrgSubdomain "demo"
  ```

**[create-all-test-users.ps1](./create-all-test-users.ps1)**
- **Purpose:** Batch create all users from test-users.json
- **Usage:** Quickly set up multiple test users
- **Example:**
  ```powershell
  .\create-all-test-users.ps1 -SaveMnemonics
  ```

---

### Testing & Verification

**[test-user-login.ps1](./test-user-login.ps1)**
- **Purpose:** Test user authentication and JWT token validation
- **Usage:** Verify user can log in and decode token claims
- **Example:**
  ```powershell
  .\test-user-login.ps1 `
      -Email "alice@example.com" `
      -Password "SecurePass123!" `
      -ShowFullToken
  ```

**[test-wallet-creation.ps1](./test-wallet-creation.ps1)**
- **Purpose:** Test wallet creation with various algorithms
- **Usage:** Create wallets with different crypto algorithms and compare
- **Example:**
  ```powershell
  .\test-wallet-creation.ps1 `
      -Email "alice@example.com" `
      -Password "SecurePass123!" `
      -TestAll
  ```

---

### Utilities

**[helpers.ps1](./helpers.ps1)**
- **Purpose:** Shared helper functions for all scripts
- **Functions:**
  - `Write-Section`, `Write-Success`, `Write-Info` - Console output formatting
  - `Invoke-ApiRequest` - REST API calls with error handling
  - `Get-JwtPayload`, `Get-JwtHeader` - JWT token decoding
  - `Test-JwtExpiration` - Token validation
  - `Test-ServiceHealth` - Service health checks
  - `Get-UserToken` - User authentication
  - `Get-OrganizationBySubdomain` - Org lookup
- **Usage:** Import as module in other scripts

---

## Quick Start

### 1. Create Your First User

```powershell
# From walkthroughs/UserWalletCreation directory
.\scripts\phase1-create-user-wallet.ps1 `
    -UserEmail "your.email@example.com" `
    -UserDisplayName "Your Name" `
    -UserPassword "YourSecurePassword123!" `
    -OrgSubdomain "demo"
```

### 2. Test the Login

```powershell
.\scripts\test-user-login.ps1 `
    -Email "your.email@example.com" `
    -Password "YourSecurePassword123!"
```

### 3. Experiment with Wallets

```powershell
.\scripts\test-wallet-creation.ps1 `
    -Email "your.email@example.com" `
    -Password "YourSecurePassword123!" `
    -Algorithm "NISTP256" `
    -WordCount 24
```

---

## Script Parameters Reference

### Common Parameters (All Scripts)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-TenantServiceUrl` | string | `http://localhost:5110` | Direct Tenant Service URL |
| `-WalletServiceUrl` | string | `http://localhost:5000` | Direct Wallet Service URL |
| `-Verbose` | switch | false | Show detailed output |

### phase1-create-user-wallet.ps1

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-UserEmail` | string | ✅ | - | New user email address |
| `-UserDisplayName` | string | ✅ | - | New user display name |
| `-UserPassword` | string | ✅ | - | New user password |
| `-UserRoles` | string[] | ❌ | `@("Member")` | User roles array |
| `-WalletName` | string | ❌ | `"Default Wallet"` | Wallet display name |
| `-WalletAlgorithm` | string | ❌ | `"ED25519"` | Crypto algorithm (ED25519, NISTP256, RSA4096) |
| `-MnemonicWordCount` | int | ❌ | `12` | BIP39 word count (12 or 24) |
| `-OrgId` | Guid | ❌ | - | Organization ID (use this OR OrgSubdomain) |
| `-OrgSubdomain` | string | ❌ | - | Organization subdomain (use this OR OrgId) |
| `-SaveMnemonicPath` | string | ❌ | - | File path to save mnemonic (TESTING ONLY!) |
| `-AdminEmail` | string | ❌ | `stuart.mackintosh@sorcha.dev` | Admin email |
| `-AdminPassword` | string | ❌ | `SorchaDev2025!` | Admin password |

### test-user-login.ps1

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-Email` | string | ✅ | - | User email |
| `-Password` | string | ✅ | - | User password |
| `-ShowFullToken` | switch | ❌ | false | Display full JWT token |

### test-wallet-creation.ps1

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-Email` | string | ✅ | - | User email for authentication |
| `-Password` | string | ✅ | - | User password |
| `-TestAll` | switch | ❌ | false | Test all algorithms (6 wallets) |
| `-Algorithm` | string | ❌ | `"ED25519"` | Algorithm to test (if not -TestAll) |
| `-WordCount` | int | ❌ | `12` | Mnemonic word count (if not -TestAll) |

### create-all-test-users.ps1

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `-DataFile` | string | ❌ | `../data/test-users.json` | Path to test users JSON |
| `-SaveMnemonics` | switch | ❌ | false | Save mnemonics to files (TESTING ONLY!) |
| `-OutputDir` | string | ❌ | `../output` | Directory for saving mnemonics |
| `-AdminEmail` | string | ❌ | `stuart.mackintosh@sorcha.dev` | Admin email |
| `-AdminPassword` | string | ❌ | `SorchaDev2025!` | Admin password |

---

## Error Handling

All scripts include comprehensive error handling:

- **API Errors:** HTTP status codes and error details extracted from response
- **Service Unavailable:** Clear message when services aren't running
- **Authentication Failures:** Specific guidance for login issues
- **Parameter Validation:** PowerShell validates parameters before execution
- **Troubleshooting Tips:** Context-specific guidance in error messages

### Common Errors

**"Organization with subdomain 'X' not found"**
- Solution: Verify organization exists, or use correct subdomain
- Check: `curl http://localhost:5110/api/organizations`

**"HTTP 401 Unauthorized"**
- Solution: Token expired or invalid credentials
- Check: Re-authenticate, verify email/password

**"HTTP 409 Conflict"**
- Solution: User with email already exists
- Check: Use different email or delete existing user

**"Service not responding"**
- Solution: Start Sorcha services
- Check: `docker-compose ps` and `docker-compose up -d`

---

## Output Examples

### Successful User Creation

```
╔════════════════════════════════════════════════════════════════════════╗
║  Sorcha User and Wallet Creation - Phase 1                             ║
╚════════════════════════════════════════════════════════════════════════╝

==> Step 1: Admin Authentication
  ✓ Admin authenticated
    Token expires in: 3600 seconds

==> Step 2: Resolve Organization
  ✓ Organization found: Sorcha Development (demo)
    Organization ID: 1a234567-89ab-cdef-0123-456789abcdef

==> Step 3: Create User in Organization
  ✓ User created successfully
    User ID: 7a234567-89ab-cdef-0123-456789abcdef
    Email: alice@example.com
    Display Name: Alice Johnson
    Roles: Member, Designer

==> Step 4: User Authentication
  ✓ User authenticated successfully
    Token expires in: 3600 seconds

==> Step 5: Create Default Wallet
  ✓ Wallet created successfully!
    Address: SORCHA1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0
    Name: Alice's Primary Wallet
    Algorithm: ED25519

╔════════════════════════════════════════════════════════════════════════╗
║  ⚠️  CRITICAL: SAVE YOUR MNEMONIC PHRASE ⚠️                            ║
╚════════════════════════════════════════════════════════════════════════╝

Your 12-word mnemonic phrase:
  1. abandon
  2. ability
  [... 10 more words ...]

⚠️  WARNING: [Security warning message]

╔════════════════════════════════════════════════════════════════════════╗
║  ✅ PHASE 1 COMPLETE - USER AND WALLET CREATED SUCCESSFULLY ✅          ║
╚════════════════════════════════════════════════════════════════════════╝
```

---

## Best Practices

### Security

1. **Never hardcode passwords** - Use parameters or prompt
2. **Never save mnemonics** - Except for testing with `-SaveMnemonicPath`
3. **Delete test mnemonics** - Immediately after testing
4. **Use strong passwords** - Minimum 12 characters, mixed case, numbers, symbols
5. **Secure admin credentials** - Don't commit admin passwords to source control

### Testing

1. **Start simple** - Create one user before batch operations
2. **Verify each step** - Use test scripts to confirm functionality
3. **Check service logs** - `docker-compose logs [service-name]` for debugging
4. **Clean up** - Delete test users/wallets when done

### Development

1. **Use `-Verbose`** - Get detailed output for debugging
2. **Check service health** - Before running scripts
3. **Review error messages** - They contain specific troubleshooting guidance
4. **Read the docs** - [README.md](../README.md) has full walkthrough

---

## Troubleshooting

### Scripts Not Found

**Problem:** `The term '.\phase1-create-user-wallet.ps1' is not recognized`

**Solution:**
```powershell
# Ensure you're in the correct directory
cd c:\projects\Sorcha\walkthroughs\UserWalletCreation
.\scripts\phase1-create-user-wallet.ps1 ...

# Or use full path
c:\projects\Sorcha\walkthroughs\UserWalletCreation\scripts\phase1-create-user-wallet.ps1 ...
```

### Execution Policy

**Problem:** `File cannot be loaded because running scripts is disabled`

**Solution:**
```powershell
# Run PowerShell as Administrator, then:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or run individual script bypassing policy:
powershell -ExecutionPolicy Bypass -File .\scripts\phase1-create-user-wallet.ps1 ...
```

### Parameter Validation Failed

**Problem:** `Cannot validate argument on parameter 'WalletAlgorithm'`

**Solution:** Use exact algorithm names (case-sensitive):
- `ED25519` (not `ed25519` or `EdDSA`)
- `NISTP256` (not `P256` or `nist-p256`)
- `RSA4096` (not `rsa` or `RSA-4096`)

---

## Contributing

Found a bug or have a suggestion?

1. Document the issue with script name, parameters, and error message
2. Check [PHASE1-RESULTS.md](../PHASE1-RESULTS.md) for known issues
3. Create GitHub issue with walkthrough label
4. Include full error output and environment details

---

## Related Documentation

- **Walkthrough Overview:** [../README.md](../README.md)
- **Implementation Plan:** [../PLAN.md](../PLAN.md)
- **Test Results:** [../PHASE1-RESULTS.md](../PHASE1-RESULTS.md)
- **Sample Data:** [../data/](../data/)

---

**Ready to start?** Run `.\scripts\phase1-create-user-wallet.ps1 --help` for parameter details!
