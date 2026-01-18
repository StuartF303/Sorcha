# Wallet Service Encryption Providers

**Status:** 📋 Planning Phase
**Date:** 2026-01-11
**Related Specification:** [sorcha-wallet-service.md](../../.specify/specs/sorcha-wallet-service.md)

---

## Overview

This walkthrough documents the design and implementation of production-ready encryption providers for the Sorcha Wallet Service. Currently, the wallet service uses `LocalEncryptionProvider` which stores encryption keys in memory - suitable for development but not for production.

This walkthrough contains:
1. **Implementation Plan** - Comprehensive design and implementation strategy
2. **Provider Specifications** - Detailed specifications for each encryption provider
3. **Test Results** - Testing outcomes and performance benchmarks (to be added)
4. **Setup Guides** - Instructions for configuring each provider (to be added)

---

## The Problem

**Current State:**
- Wallet private keys are encrypted at rest in PostgreSQL
- Encryption uses `LocalEncryptionProvider` with in-memory keys
- Keys are lost on service restart ❌
- Not suitable for production deployment ❌

**What We Need:**
- Production-ready encryption using Azure Key Vault or OS keystores
- Keys that survive service restarts ✅
- Platform-independent implementation ✅
- Configuration-based provider selection ✅

---

## Proposed Solution

### Encryption Providers

1. **Azure Key Vault Provider** (Production - Cloud)
   - Enterprise-grade key management
   - HSM-backed keys (Premium tier)
   - Automatic backup and audit logging
   - Best for: Azure deployments

2. **Windows DPAPI Provider** (Production - Windows)
   - Windows Data Protection API
   - Machine/user-specific encryption
   - No external dependencies
   - Best for: Windows servers, on-premises

3. **macOS Keychain Provider** (Development - macOS)
   - macOS Keychain Services
   - OS-managed key storage
   - Best for: Developer workstations

4. **Linux Secret Service Provider** (Production - Linux)
   - freedesktop.org Secret Service API
   - GNOME Keyring, KWallet integration
   - Fallback to file-based encryption
   - Best for: Linux servers, Docker

### Key Design Principles

✅ **Use compiled infrastructure code** - Leverage OS APIs and Azure SDK, minimize custom crypto
✅ **Platform independence** - Work across Windows, Linux, macOS
✅ **Configuration-driven** - Runtime provider selection via appsettings.json
✅ **Key rotation support** - Multiple keys with keyId concept
✅ **Backward compatibility** - Existing encrypted wallets continue to work

---

## Documents

### [ENCRYPTION-IMPLEMENTATION-PLAN.md](ENCRYPTION-IMPLEMENTATION-PLAN.md)

Comprehensive implementation plan covering:
- Current state analysis
- Requirements summary
- Proposed architecture
- Detailed provider implementations
- Configuration strategy
- Testing approach
- Security considerations
- Implementation tasks

**Read this first** for complete context and design decisions.

---

## Quick Start (After Implementation)

### Azure Key Vault (Production)

```json
{
  "EncryptionProvider": {
    "Type": "AzureKeyVault",
    "AzureKeyVault": {
      "VaultUri": "https://your-vault.vault.azure.net/",
      "DefaultKeyName": "wallet-encryption-key",
      "UseManagedIdentity": true
    }
  }
}
```

### Windows DPAPI (Production)

```json
{
  "EncryptionProvider": {
    "Type": "WindowsDpapi",
    "WindowsDpapi": {
      "KeyStorePath": "C:\\ProgramData\\Sorcha\\WalletKeys",
      "DefaultKeyId": "wallet-key-2025"
    }
  }
}
```

### macOS Keychain (Development)

```json
{
  "EncryptionProvider": {
    "Type": "MacOsKeychain",
    "MacOsKeychain": {
      "ServiceName": "sorcha-wallet-service",
      "DefaultKeyId": "wallet-key-2025"
    }
  }
}
```

### Linux Secret Service (Production)

```json
{
  "EncryptionProvider": {
    "Type": "LinuxSecretService",
    "LinuxSecretService": {
      "ServiceName": "sorcha-wallet-service",
      "DefaultKeyId": "wallet-key-2025",
      "FallbackKeyStorePath": "/var/lib/sorcha/wallet-keys"
    }
  }
}
```

---

## Implementation Status

### Phase 1: Design & Planning ✅
- [x] Analyze current encryption implementation
- [x] Review requirements from wallet service spec
- [x] Review existing OS encryption patterns (CLI tool)
- [x] Design provider architecture
- [x] Create comprehensive implementation plan
- [x] Clarify implementation decisions

### Phase 2: Implementation ✅ (Local Providers Complete)
**Priority: Local providers first (faster development), then cloud**
- [x] Implement structured audit logging framework ✅
- [x] Implement Windows DPAPI provider (Priority 1) ✅
- [x] Implement Linux Secret Service provider (Priority 2) ✅
- [ ] Implement macOS Keychain provider (Priority 3) ⏸️ (Deferred - focus on Windows/Linux)
- [ ] Implement Azure Key Vault provider (Priority 4) 📋
- [x] Update service configuration and DI registration ✅

### Phase 3: Testing 📋
- [ ] Unit tests for each provider
- [ ] Integration tests (Azure, platform-specific)
- [ ] Performance benchmarks
- [ ] Key rotation testing

### Phase 4: Documentation 📋
- [ ] Azure Key Vault setup guide
- [ ] Windows DPAPI setup guide
- [ ] Linux setup guide
- [ ] Update wallet service README

### Phase 5: Deployment 📋
- [ ] Update Docker Compose configuration
- [ ] Update Azure deployment templates
- [ ] Update CI/CD pipelines
- [ ] Security review

---

## Related Files

**Existing Code:**
- `src/Common/Sorcha.Wallet.Core/Encryption/Interfaces/IEncryptionProvider.cs` - Interface definition
- `src/Common/Sorcha.Wallet.Core/Encryption/Providers/LocalEncryptionProvider.cs` - Current implementation
- `src/Services/Sorcha.Wallet.Service/Extensions/WalletServiceExtensions.cs` - Service configuration ✅ (Updated)
- `src/Apps/Sorcha.Cli/Infrastructure/WindowsDpapiEncryption.cs` - Windows DPAPI example
- `src/Apps/Sorcha.Cli/Infrastructure/MacOsKeychainEncryption.cs` - macOS Keychain example
- `src/Apps/Sorcha.Cli/Infrastructure/LinuxEncryption.cs` - Linux encryption example

**Implemented Files (2026-01-11):**
- `src/Common/Sorcha.Wallet.Core/Encryption/Logging/EncryptionAuditLogger.cs` ✅ (New)
- `src/Common/Sorcha.Wallet.Core/Encryption/Configuration/EncryptionProviderOptions.cs` ✅ (New)
- `src/Common/Sorcha.Wallet.Core/Encryption/Providers/WindowsDpapiEncryptionProvider.cs` ✅ (New)
- `src/Common/Sorcha.Wallet.Core/Encryption/Providers/LinuxSecretServiceEncryptionProvider.cs` ✅ (New)
- `src/Services/Sorcha.Wallet.Service/appsettings.json` ✅ (Updated with encryption config examples)
- `walkthroughs/WalletEncryption/docker-compose.encryption-example.yml` ✅ (New)

**Pending Files:**
- `src/Common/Sorcha.Wallet.Core/Encryption/Providers/AzureKeyVaultEncryptionProvider.cs` 📋
- `src/Common/Sorcha.Wallet.Core/Encryption/Providers/MacOsKeychainEncryptionProvider.cs` ⏸️ (Deferred)
- `tests/Sorcha.Wallet.Core.Tests/Encryption/AzureKeyVaultEncryptionProviderTests.cs` 📋
- `tests/Sorcha.Wallet.Core.Tests/Encryption/WindowsDpapiEncryptionProviderTests.cs` 📋
- `tests/Sorcha.Wallet.Core.Tests/Encryption/LinuxSecretServiceEncryptionProviderTests.cs` 📋
- `tests/Sorcha.Wallet.Service.Benchmarks/EncryptionBenchmarks.cs` 📋

---

## Key Decisions

### Why Not Use ASP.NET Core Data Protection?

ASP.NET Core Data Protection is designed for protecting cookies and tokens (short-lived data). We need:
- Long-term encryption for wallet private keys (years)
- Explicit key management and rotation
- Enterprise key management (Azure Key Vault)
- Platform-independent abstraction

### Why Multiple Providers?

Different deployment scenarios require different solutions:
- **Azure deployments** → Azure Key Vault (enterprise, managed)
- **Windows servers** → DPAPI (no cloud dependency)
- **Linux servers** → Secret Service (standard Linux keyring)
- **Development** → macOS Keychain or Local provider

### Why Not TPM 2.0?

TPM 2.0 support is planned as a P2 enhancement. Initial focus is on:
1. Azure Key Vault (highest priority, cloud)
2. Windows DPAPI (Windows servers)
3. OS keystores (macOS, Linux)

TPM integration adds complexity and is best suited for specialized hardware deployments.

---

## Security Notes

**What This Protects Against:**
- ✅ Database compromise (private keys encrypted)
- ✅ Memory dumps (keys not in plaintext memory)
- ✅ Service restart (keys persisted)
- ✅ Unauthorized decryption (requires key store access)

**What This Does NOT Protect Against:**
- ❌ Compromised service account (can access keys)
- ❌ Physical server access (attacker with root/admin)
- ❌ Side-channel attacks (timing, cache)

**Recommendations:**
- Use Azure Key Vault with Managed Identity (no credentials in code)
- Use HSM-backed keys for high-value wallets
- Implement rate limiting on encryption operations
- Monitor for unusual encryption activity
- Regularly rotate encryption keys (90-day cycle)

---

## Questions & Answers

**Q: Can we migrate from LocalEncryptionProvider to Azure Key Vault?**
A: Yes. The keyId is stored with each encrypted wallet. Migration strategy:
1. Deploy new provider (Azure Key Vault)
2. Update configuration to use Azure Key Vault
3. New wallets use Azure Key Vault automatically
4. Existing wallets continue using old key until re-encryption
5. Background job re-encrypts existing wallets (optional)

**Q: What happens if Azure Key Vault is unavailable?**
A: Service will fail to encrypt/decrypt wallets. Recommended:
- Implement retry logic with exponential backoff
- Monitor Key Vault availability
- Consider read-through caching for frequently used keys
- Have maintenance mode for Key Vault outages

**Q: How do we handle key rotation?**
A: The `keyId` concept supports rotation:
1. Create new key: `CreateKeyAsync("wallet-key-2026")`
2. Update default: Change `GetDefaultKeyId()` to return new key ID
3. New operations use new key automatically
4. Old data still decryptable with old key (never delete old keys)
5. Background job can re-encrypt data with new key (optional)

**Q: What's the performance impact of Azure Key Vault?**
A: Network latency ~10-50ms per operation. Strategies:
- Cache decrypted keys in memory (with TTL)
- Use envelope encryption (DEK + MEK) to reduce Key Vault calls
- Batch operations where possible
- Monitor p95/p99 latency

---

## Next Steps

1. **Review ENCRYPTION-IMPLEMENTATION-PLAN.md** - Complete design document
2. **Prioritize providers** - Recommend starting with Azure Key Vault
3. **Set up Azure resources** - Create Key Vault for testing
4. **Implement providers** - Start with highest priority
5. **Write tests** - Unit tests → integration tests → benchmarks
6. **Update documentation** - Setup guides and README updates

---

**Created:** 2026-01-11
**Status:** Planning Phase
**Next Milestone:** Begin Azure Key Vault provider implementation
