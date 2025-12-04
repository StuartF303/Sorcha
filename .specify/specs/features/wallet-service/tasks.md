# Tasks: Wallet Service

**Feature Branch**: `wallet-service`
**Created**: 2025-12-03
**Status**: 95% Complete (HD Wallet Features Complete)

## Task Summary

| Status | Count |
|--------|-------|
| Complete | 20 |
| In Progress | 2 |
| Pending | 4 |
| **Total** | **26** |

---

## Tasks

### WS-001: Create Domain Models
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Define core domain entities and value objects for wallet management.

**Acceptance Criteria**:
- [x] Wallet.cs entity with all properties
- [x] WalletAddress.cs for HD addresses
- [x] WalletAccess.cs for delegations
- [x] Mnemonic value object with validation
- [x] DerivationPath parser for BIP44

---

### WS-002: Implement WalletManager
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-001

**Description**: Implement wallet creation and recovery from BIP39 mnemonics.

**Acceptance Criteria**:
- [x] Create wallet with 12/24 word mnemonics
- [x] Recover wallet from mnemonic
- [x] Support ED25519, SECP256K1, RSA-4096
- [x] Validate mnemonic checksum
- [x] Generate deterministic addresses

---

### WS-003: Implement KeyManager
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-002

**Description**: Implement HD key derivation using BIP32/BIP44 paths.

**Acceptance Criteria**:
- [x] Parse BIP44 derivation paths
- [x] Derive child keys from master
- [x] Support hardened derivation
- [x] Address generation per algorithm

---

### WS-004: Implement IEncryptionProvider
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: None

**Description**: Create encryption provider abstraction and implementations.

**Acceptance Criteria**:
- [x] IEncryptionProvider interface
- [x] Azure Key Vault provider
- [x] Local DPAPI provider
- [x] AES-256-GCM encryption
- [x] Key ID tracking

---

### WS-005: Implement IWalletRepository
- **Status**: In Progress
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-001

**Description**: Create repository abstraction and EF Core implementation.

**Acceptance Criteria**:
- [x] IWalletRepository interface
- [x] EF Core DbContext
- [x] PostgreSQL entity mapping
- [ ] Full integration testing
- [ ] Migration scripts

---

### WS-006: Implement TransactionServiceAdapter
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-003

**Description**: Adapt Sorcha.TransactionHandler for wallet signing.

**Acceptance Criteria**:
- [x] Sign transactions with wallet keys
- [x] Verify signatures
- [x] Decrypt payloads
- [x] Integration with Sorcha.Cryptography

---

### WS-007: Implement DelegationManager
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-001

**Description**: Implement access control and delegation logic.

**Acceptance Criteria**:
- [x] Grant delegation with role
- [x] Revoke delegation
- [x] Check access permissions
- [x] Audit logging for changes

---

### WS-008: Create API Endpoints
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-002, WS-006, WS-007

**Description**: Implement REST API endpoints for wallet operations.

**Acceptance Criteria**:
- [x] POST /api/wallets - Create
- [x] POST /api/wallets/recover - Recover
- [x] GET /api/wallets - List
- [x] GET /api/wallets/{address} - Get
- [x] POST /api/wallets/{address}/sign - Sign
- [x] POST /api/wallets/{address}/derive - Derive

---

### WS-009: Implement Delegation Endpoints
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-007

**Description**: Implement delegation management endpoints.

**Acceptance Criteria**:
- [x] POST /api/wallets/{address}/delegates
- [x] GET /api/wallets/{address}/delegates
- [x] DELETE /api/wallets/{address}/delegates/{subject}

---

### WS-010: Multi-Tenant Isolation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Implement tenant isolation for wallet access.

**Acceptance Criteria**:
- [x] Inject ITenantProvider
- [x] Filter queries by tenant
- [x] Validate tenant on operations
- [x] No cross-tenant data leakage

---

### WS-011: Unit Tests - WalletManager
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 6 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-002

**Description**: Unit tests for wallet creation and recovery.

**Acceptance Criteria**:
- [x] Mnemonic generation tests
- [x] Recovery tests
- [x] Algorithm support tests
- [x] Error handling tests

---

### WS-012: Unit Tests - KeyManager
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-003

**Description**: Unit tests for HD key derivation.

**Acceptance Criteria**:
- [x] Path parsing tests
- [x] Derivation determinism tests
- [x] Address generation tests

---

### WS-013: Unit Tests - TransactionService
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-006

**Description**: Unit tests for signing and verification.

**Acceptance Criteria**:
- [x] Signing tests
- [x] Verification tests
- [x] Invalid signature tests

---

### WS-014: Unit Tests - DelegationManager
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-007

**Description**: Unit tests for access control.

**Acceptance Criteria**:
- [x] Grant/revoke tests
- [x] Permission check tests
- [x] Audit logging tests

---

### WS-015: Integration Tests - API
- **Status**: In Progress
- **Priority**: P1
- **Estimate**: 8 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Integration tests for API endpoints.

**Acceptance Criteria**:
- [x] CRUD endpoint tests
- [ ] Signing flow tests
- [ ] Delegation flow tests
- [ ] Error handling tests

---

### WS-016: Integration Tests - Database
- **Status**: Pending
- **Priority**: P1
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: WS-005

**Description**: Integration tests with PostgreSQL.

**Acceptance Criteria**:
- [ ] Testcontainers setup
- [ ] CRUD operation tests
- [ ] Concurrent access tests
- [ ] Migration tests

---

### WS-017: Integration Tests - Key Vault
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: WS-004

**Description**: Integration tests with Azure Key Vault.

**Acceptance Criteria**:
- [ ] Key Vault connection tests
- [ ] Encryption round-trip tests
- [ ] Key rotation tests

---

### WS-018: OpenAPI Documentation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Add comprehensive OpenAPI documentation.

**Acceptance Criteria**:
- [x] All endpoints documented
- [x] Request/response schemas
- [x] Error responses documented
- [x] Scalar UI integration

---

### WS-019: Health Checks
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Implement health checks for service dependencies.

**Acceptance Criteria**:
- [x] Database health check
- [x] Key Vault health check
- [x] Composite health endpoint

---

### WS-020: Service Registration
- **Status**: Complete
- **Priority**: P0
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Register service with .NET Aspire.

**Acceptance Criteria**:
- [x] Service defaults integration
- [x] Service discovery
- [x] Telemetry configuration

---

### WS-021: Audit Logging
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Implement security audit logging.

**Acceptance Criteria**:
- [x] Log wallet creation
- [x] Log signing operations
- [x] Log delegation changes
- [x] Structured logging format

---

### WS-022: Performance Testing
- **Status**: Pending
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: TBD
- **Dependencies**: WS-015

**Description**: Performance testing for signing operations.

**Acceptance Criteria**:
- [ ] Signing latency benchmarks
- [ ] Throughput testing
- [ ] Memory profiling

---

### WS-023: README Documentation
- **Status**: Complete
- **Priority**: P1
- **Estimate**: 2 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Create comprehensive README.

**Acceptance Criteria**:
- [x] Service overview
- [x] API documentation
- [x] Configuration guide
- [x] Security considerations

---

### WS-024: AWS KMS Provider
- **Status**: Pending
- **Priority**: P3
- **Estimate**: 6 hours
- **Assignee**: TBD
- **Dependencies**: WS-004

**Description**: Implement AWS KMS encryption provider.

**Acceptance Criteria**:
- [ ] AWS KMS client integration
- [ ] Encryption/decryption
- [ ] Key rotation support

---

### WS-025: Transaction History
- **Status**: Complete
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-006

**Description**: Track wallet transaction history.

**Acceptance Criteria**:
- [x] WalletTransaction entity
- [x] Record signed transactions
- [x] Query transaction history

---

### WS-026: Event Publishing
- **Status**: Complete
- **Priority**: P2
- **Estimate**: 4 hours
- **Assignee**: AI Assistant
- **Dependencies**: WS-008

**Description**: Publish domain events for wallet operations.

**Acceptance Criteria**:
- [x] WalletCreated event
- [x] TransactionSigned event
- [x] DelegateChanged event
- [x] .NET Aspire messaging integration

---

## Notes

- 111 unit tests currently passing
- Azure Key Vault integration tested in development environment
- PostgreSQL persistence is primary remaining work
- AWS KMS support deferred to post-MVD
