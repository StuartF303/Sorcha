# Implementation Plan: Wallet Service

**Feature Branch**: `wallet-service`
**Created**: 2025-12-03
**Status**: 95% Complete (HD Wallet Features Complete)
**Production Ready**: 45% (Critical Infrastructure Pending)

## Summary

The Wallet Service provides cryptographic wallet management for the Sorcha platform, including HD wallet creation, key derivation, transaction signing, and access control delegation. It integrates with Azure Key Vault for secure key storage.

## Design Decisions

### Decision 1: Encryption Provider Strategy

**Approach**: Support multiple encryption providers with Azure Key Vault as the production default.

**Rationale**:
- Azure Key Vault provides FIPS 140-2 Level 2 certified HSM protection
- Local DPAPI enables development without cloud dependencies
- Pluggable architecture allows future AWS KMS integration

**Alternatives Considered**:
- Hardware wallets - Requires physical device management
- Pure software encryption - Less secure for production

### Decision 2: BIP32/BIP39/BIP44 Implementation

**Approach**: Use NBitcoin library for HD wallet standards.

**Rationale**:
- Mature, well-tested implementation
- Full BIP standard compliance
- Multi-algorithm support

**Alternatives Considered**:
- Custom implementation - High risk, significant effort
- BouncyCastle only - No BIP mnemonic support

### Decision 3: Repository Pattern

**Approach**: Abstract repository with EF Core implementation as default.

**Rationale**:
- Database-agnostic design enables PostgreSQL, SQL Server, SQLite
- In-memory provider simplifies testing
- Future document DB support (CosmosDB, MongoDB)

### Decision 4: Access Control Model

**Approach**: Subject-based RBAC with three roles: Owner, Delegate-ReadWrite, Delegate-ReadOnly.

**Rationale**:
- Simple model covers most enterprise use cases
- Subject claims from JWT enable service-to-service delegation
- Reason tracking provides audit trail

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                Sorcha.Wallet.Service                     │
│                   (ASP.NET Core 10)                      │
├─────────────────────────────────────────────────────────┤
│  Endpoints/                                              │
│  ├── WalletEndpoints.cs      (CRUD, signing)            │
│  ├── DelegationEndpoints.cs  (Access control)           │
│  └── AddressEndpoints.cs     (HD derivation)            │
├─────────────────────────────────────────────────────────┤
│  Services/                                               │
│  ├── IWalletService.cs                                  │
│  ├── WalletService.cs        (Facade)                   │
│  ├── WalletManager.cs        (Creation/recovery)        │
│  ├── KeyManager.cs           (HD derivation)            │
│  ├── TransactionServiceAdapter.cs (Signing)             │
│  └── DelegationManager.cs    (Access control)           │
├─────────────────────────────────────────────────────────┤
│  Repositories/                                           │
│  ├── IWalletRepository.cs                               │
│  ├── EfCoreWalletRepository.cs                          │
│  └── InMemoryWalletRepository.cs                        │
├─────────────────────────────────────────────────────────┤
│  Encryption/                                             │
│  ├── IEncryptionProvider.cs                             │
│  ├── AzureKeyVaultProvider.cs                           │
│  ├── AwsKmsProvider.cs       (Planned)                  │
│  └── LocalDpapiProvider.cs                              │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                  Sorcha.Wallet.Core                      │
│              (Domain Models & Interfaces)                │
├─────────────────────────────────────────────────────────┤
│  Entities/                                               │
│  ├── Wallet.cs                                          │
│  ├── WalletAddress.cs                                   │
│  ├── WalletAccess.cs                                    │
│  └── WalletTransaction.cs                               │
├─────────────────────────────────────────────────────────┤
│  ValueObjects/                                           │
│  ├── Mnemonic.cs                                        │
│  ├── PrivateKey.cs                                      │
│  ├── PublicKey.cs                                       │
│  └── DerivationPath.cs                                  │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Wallet.Core | 100% | Domain models and interfaces |
| WalletManager | 100% | Creation and recovery |
| KeyManager | 100% | HD derivation with BIP44 |
| TransactionService | 100% | Signing, verification, encryption |
| DelegationService | 100% | Access control |
| HD Address Management | 100% | Client-side derivation, gap limit |
| InMemoryWalletRepository | 100% | Thread-safe development |
| EfCoreWalletRepository | 0% | BLOCKER - Not implemented |
| AzureKeyVaultProvider | 0% | BLOCKER - Not implemented |
| LocalEncryptionProvider | 100% | AES-256-GCM development |
| API Endpoints (21) | 100% | All implemented |
| Unit Tests | 85% | 111 tests passing |
| Integration Tests | 100% | 29 tests passing |
| Performance Tests | 100% | 6 benchmarks passing |

### Production Blockers

| Blocker | Impact | Effort |
|---------|--------|--------|
| Authentication/Authorization | No security | 2-3 days |
| EF Core Repository | Data lost on restart | 3-5 days |
| Azure Key Vault Provider | No production key mgmt | 3-5 days |

### API Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/wallets` | Create new wallet | Done |
| POST | `/api/wallets/recover` | Recover from mnemonic | Done |
| GET | `/api/wallets` | List user's wallets | Done |
| GET | `/api/wallets/{address}` | Get wallet details | Done |
| PUT | `/api/wallets/{address}` | Update metadata | Done |
| DELETE | `/api/wallets/{address}` | Soft delete wallet | Done |
| POST | `/api/wallets/{address}/sign` | Sign transaction | Done |
| POST | `/api/wallets/{address}/verify` | Verify signature | Done |
| POST | `/api/wallets/{address}/derive` | Derive HD address | Done |
| GET | `/api/wallets/{address}/addresses` | List derived addresses | Done |
| POST | `/api/wallets/{address}/delegates` | Add delegation | Done |
| GET | `/api/wallets/{address}/delegates` | List delegations | Done |
| DELETE | `/api/wallets/{address}/delegates/{subject}` | Revoke delegation | Done |

## Dependencies

### Internal Dependencies

- `Sorcha.Cryptography` - Key generation, signing, hashing
- `Sorcha.TransactionHandler` - Transaction building
- `Sorcha.ServiceDefaults` - .NET Aspire configuration
- `Sorcha.Tenant.Abstractions` - Multi-tenant isolation

### External Dependencies

- `NBitcoin` - BIP32/BIP39/BIP44 implementation
- `Azure.Security.KeyVault.Keys` - Azure Key Vault client
- `Microsoft.EntityFrameworkCore` - ORM
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL provider

### Service Dependencies

- Tenant Service - Tenant context and isolation
- Register Service - Transaction history retrieval (future)

## Migration/Integration Notes

### Database Schema

```sql
-- Wallets table
CREATE TABLE wallets (
    address VARCHAR(100) PRIMARY KEY,
    encrypted_private_key TEXT NOT NULL,
    encryption_key_id VARCHAR(100) NOT NULL,
    algorithm VARCHAR(20) NOT NULL,
    owner VARCHAR(100) NOT NULL,
    tenant VARCHAR(100) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    tags JSONB,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    deleted_at TIMESTAMPTZ,
    version INT NOT NULL,
    row_version BYTEA
);

-- Indexes
CREATE INDEX idx_wallets_owner ON wallets(owner);
CREATE INDEX idx_wallets_tenant ON wallets(tenant);
CREATE INDEX idx_wallets_status ON wallets(status);
```

### Breaking Changes

- None for MVD phase
- Future: Wallet schema v2 may require migration

## Open Questions

1. Should we support hardware wallet signing via PKCS#11?
2. How to handle wallet import from other platforms (WIF format)?
3. Should we implement stealth addresses for enhanced privacy?
