# Siccar.WalletService Library Specification

**Version:** 1.0
**Date:** 2025-11-13
**Status:** Proposed
**Related Constitution:** [constitution.md](../constitution.md)
**Related Specifications:**
- [siccar-cryptography-rewrite.md](siccar-cryptography-rewrite.md)
- [siccar-transaction-handler.md](siccar-transaction-handler.md)

## Executive Summary

This specification defines the requirements for creating a standalone, reusable wallet management library named **Siccar.WalletService**. This library will handle cryptographic wallet creation, key management, transaction signing, delegation/access control, and secure key storage for the SICCAR distributed ledger platform. The new implementation will be architected to be portable and importable into new system architectures.

## Background

### Current State Analysis

The existing wallet service is implemented as a monolithic ASP.NET Core microservice with tight coupling to infrastructure and external services. The code resides in `/src/Services/Wallet` and consists of:

#### Current Architecture

**Project Structure:**
- `WalletService` - ASP.NET Core Web API (Controllers, Startup, Configuration)
- `WalletServiceCore` - Domain models, interfaces, and repositories
- `WalletSQLRepository` - Entity Framework Core data access layer
- `WalletService.IntegrationTests` - Integration test suite
- `WalletTests` - Unit test suite

**Key Components:**
- **WalletsController** - 15+ REST endpoints for wallet CRUD, transaction signing, delegation
- **PendingTransactionsController** - Transaction state management and Dapr subscriptions
- **WalletFactory** - Creates/recovers wallets from mnemonics using ED25519 keys
- **WalletRepository** - Data persistence with encryption via ASP.NET Data Protection
- **WalletProtector** - Encrypts/decrypts private keys at rest using Azure Key Vault
- **WalletContext** - EF Core DbContext supporting MySQL and Cosmos SQL

#### Problems Identified

1. **Tight Coupling to Infrastructure**
   - Hardcoded dependency on ASP.NET Core pipeline
   - Entity Framework Core required for all operations
   - Dapr integration embedded in controllers
   - Cannot be used outside HTTP/REST context

2. **Monolithic Service Design**
   - Business logic mixed with HTTP concerns
   - Controllers directly call cryptography libraries
   - Difficult to reuse wallet logic in other contexts
   - No clear separation between API and domain logic

3. **Complex External Dependencies**
   - Register Service client for transaction history retrieval
   - JWT authentication tightly coupled to request pipeline
   - Azure Key Vault required for data protection
   - MySQL/Cosmos DB required for storage
   - Dapr pub/sub for event publishing

4. **Security and Key Management Issues**
   - Private keys stored encrypted in database
   - Encryption keys in Azure Key Vault (cloud dependency)
   - Data protection keys on local filesystem as fallback
   - No hardware security module (HSM) support
   - No support for HD wallet derivation beyond basic paths

5. **Limited Wallet Features**
   - Only ED25519 algorithm supported
   - Single derivation path "m/" hardcoded
   - No multi-signature support
   - No wallet import/export (only mnemonic recovery)
   - No watch-only wallet support

6. **Transaction Management Complexity**
   - Transaction state tracked in wallet database
   - Duplicate transaction data between Register and Wallet services
   - Pending transaction logic spread across controllers
   - No clear transaction lifecycle management

7. **Testing and Portability**
   - Integration tests require full infrastructure stack
   - In-memory repository implementation is incomplete
   - Cannot easily test wallet operations in isolation
   - Difficult to use in non-Kubernetes environments

### Goals

1. **Separation of Concerns** - Wallet logic separate from HTTP/REST API
2. **Portable Architecture** - Usable in any .NET application context
3. **Pluggable Storage** - Abstract storage layer with multiple implementations
4. **Enhanced Security** - HSM support, better key management, audit logging
5. **Comprehensive Testing** - >90% coverage with unit and integration tests
6. **Well-Documented** - Clear API with usage examples
7. **Multi-Algorithm Support** - ED25519, SECP256K1, RSA
8. **HD Wallet Support** - Full BIP32/BIP39/BIP44 implementation
9. **Event-Driven Design** - Decouple from Dapr with abstract event bus

## Scope

### In Scope

#### Core Wallet Management

1. **Wallet Creation & Recovery**
   - Generate new wallets with BIP39 mnemonics (12/24 words)
   - Recover wallets from existing mnemonics
   - Support multiple cryptographic algorithms (ED25519, SECP256K1, RSA)
   - Hierarchical Deterministic (HD) wallet support (BIP32/BIP44)
   - Wallet metadata management (name, description, tags)
   - Wallet archiving and soft deletion

2. **Key Management**
   - Master key generation using cryptographically secure RNG
   - Derived key creation with BIP44 paths (m/44'/coin'/account'/change/index)
   - Private key encryption at rest (multiple encryption providers)
   - Public key derivation and address generation
   - Key rotation and versioning support
   - Export keys in WIF (Wallet Import Format)

3. **Address Management**
   - Generate receive addresses from HD paths
   - Track used vs. unused addresses
   - Address labeling and categorization
   - Watch-only address support
   - Multi-signature address generation (future)

4. **Access Control & Delegation**
   - Owner, delegate read-write, delegate read-only roles
   - Subject-based access control (user/service principals)
   - Reason tracking for access grants
   - Time-based access expiration
   - Revocation and audit trail
   - Multi-tenant isolation

5. **Transaction Operations**
   - Sign transactions using wallet private keys
   - Validate transaction signatures
   - Decrypt transaction payloads for wallet recipients
   - Track wallet transaction history
   - UTXO management (unspent transaction outputs)
   - Transaction state tracking (pending, confirmed, spent)

6. **Security Features**
   - Encrypted private key storage (AES-256-GCM)
   - Multiple encryption providers (Azure Key Vault, AWS KMS, local)
   - Hardware Security Module (HSM) integration
   - Key derivation using PBKDF2 or Argon2
   - Secure mnemonic generation and validation
   - Audit logging for all sensitive operations
   - Rate limiting for key operations

#### Storage Abstraction

1. **Repository Pattern**
   - Abstract `IWalletRepository` interface
   - Support for CRUD operations
   - Query support (by owner, by tenant, by address)
   - Transaction support for atomic operations
   - Pagination and filtering

2. **Storage Implementations**
   - Entity Framework Core provider (SQL databases)
   - In-memory provider (testing, caching)
   - Document database provider (MongoDB, CosmosDB)
   - File-based provider (encrypted JSON files)
   - Distributed cache provider (Redis)

3. **Migration Support**
   - Import from existing wallet database
   - Export to portable formats
   - Schema versioning and migrations
   - Backward compatibility with v3 wallet format

#### Event System

1. **Event Definitions**
   - `WalletCreated` - New wallet generated
   - `WalletRecovered` - Wallet restored from mnemonic
   - `WalletDeleted` - Wallet marked for deletion
   - `AddressGenerated` - New address derived
   - `TransactionSigned` - Transaction signed by wallet
   - `DelegateAdded/Updated/Removed` - Access control changes
   - `KeyRotated` - Encryption key changed

2. **Event Bus Abstraction**
   - `IEventPublisher` interface for publishing events
   - Event handlers with retry and dead-letter support
   - Implementations: Dapr, RabbitMQ, Azure Service Bus, in-memory
   - Event versioning and schema evolution

#### API & Client Library

1. **Core Service Interface**
   - `IWalletService` - Main wallet operations
   - `IKeyManagementService` - Key generation and derivation
   - `ITransactionService` - Signing and validation
   - `IDelegationService` - Access control management
   - Dependency injection support

2. **Client Libraries**
   - HTTP REST client (for existing API compatibility)
   - gRPC client (for high-performance scenarios)
   - SignalR client (for real-time updates)
   - Direct in-process client

### Out of Scope

1. **HTTP API Layer** - Separate project (Siccar.WalletService.Api)
2. **User Interface** - UI components for wallet management
3. **Blockchain Consensus** - Handled by Register Service
4. **Transaction Pool Management** - Handled by Register Service
5. **Multi-signature Coordination** - Future enhancement
6. **Hardware Wallet Integration** - Future enhancement (Ledger, Trezor)
7. **Decentralized Identity (DID)** - Future integration
8. **Cross-chain Support** - Future enhancement

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Siccar.WalletService.Api                     │
│              (ASP.NET Core REST API - Separate)                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                    Siccar.WalletService                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              IWalletService (Facade)                     │  │
│  └───┬──────────────────┬──────────────────┬────────────────┘  │
│      │                  │                  │                    │
│  ┌───▼──────┐    ┌──────▼──────┐    ┌─────▼────────────┐      │
│  │  Wallet  │    │     Key     │    │   Transaction    │      │
│  │ Manager  │    │  Manager    │    │     Service      │      │
│  └───┬──────┘    └──────┬──────┘    └─────┬────────────┘      │
│      │                  │                  │                    │
│  ┌───▼──────────────────▼──────────────────▼────────────┐      │
│  │           IWalletRepository (Abstract)               │      │
│  └───┬──────────────────┬──────────────────┬────────────┘      │
│      │                  │                  │                    │
│  ┌───▼────┐      ┌──────▼─────┐      ┌────▼──────┐            │
│  │   EF   │      │  Document  │      │ In-Memory │            │
│  │  Core  │      │     DB     │      │  Storage  │            │
│  └────────┘      └────────────┘      └───────────┘            │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │           IEncryptionProvider (Abstract)                 │  │
│  └───┬──────────────────┬──────────────────┬────────────────┘  │
│      │                  │                  │                    │
│  ┌───▼────┐      ┌──────▼─────┐      ┌────▼──────┐            │
│  │ Azure  │      │    AWS     │      │   Local   │            │
│  │  KV    │      │    KMS     │      │  DPAPI    │            │
│  └────────┘      └────────────┘      └───────────┘            │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │           IEventPublisher (Abstract)                     │  │
│  └───┬──────────────────┬──────────────────┬────────────────┘  │
│      │                  │                  │                    │
│  ┌───▼────┐      ┌──────▼─────┐      ┌────▼──────┐            │
│  │  Dapr  │      │ RabbitMQ   │      │ In-Memory │            │
│  │ PubSub │      │            │      │   Events  │            │
│  └────────┘      └────────────┘      └───────────┘            │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
           ┌──────────────────────┐
           │ Siccar.Cryptography  │
           │ (Key Generation,     │
           │  Signing, Hashing)   │
           └──────────────────────┘
```

### Layer Responsibilities

#### 1. Service Layer (`Services/`)

**IWalletService** - Facade for all wallet operations
- Coordinates between managers
- Handles cross-cutting concerns (logging, validation)
- Manages transaction boundaries
- Enforces business rules

**WalletManager** - Wallet lifecycle management
- Create, recover, update, delete wallets
- Manage wallet metadata
- Handle wallet archiving

**KeyManager** - Cryptographic key operations
- Generate master keys from mnemonics
- Derive keys using BIP44 paths
- Encrypt/decrypt private keys
- Key rotation and versioning

**TransactionServiceAdapter** - Transaction operations
- Sign transactions with wallet keys
- Verify transaction signatures
- Decrypt payloads
- Track transaction state

**DelegationManager** - Access control
- Grant, revoke, update delegations
- Validate access permissions
- Audit delegation changes

#### 2. Domain Layer (`Domain/`)

**Entities:**
- `Wallet` - Core wallet entity with encrypted keys
- `WalletAddress` - Derived addresses with paths
- `WalletAccess` - Delegation/access control records
- `WalletTransaction` - Transaction tracking
- `WalletMetadata` - Extended wallet information
- `AuditLog` - Security audit trail

**Value Objects:**
- `Mnemonic` - BIP39 mnemonic phrase
- `PrivateKey` - Encrypted private key
- `PublicKey` - Public key and address
- `DerivationPath` - BIP44 path parser
- `AccessRight` - Permission enumeration

**Domain Events:**
- `WalletCreatedEvent`
- `AddressGeneratedEvent`
- `TransactionSignedEvent`
- `DelegateChangedEvent`

#### 3. Repository Layer (`Repositories/`)

**IWalletRepository** - Abstract storage interface
```csharp
Task<Wallet?> GetByAddressAsync(string address, string? userSubject = null);
Task<IEnumerable<Wallet>> GetByOwnerAsync(string ownerSubject);
Task<IEnumerable<Wallet>> GetByTenantAsync(string tenantId);
Task<Wallet> CreateAsync(Wallet wallet);
Task<Wallet> UpdateAsync(Wallet wallet);
Task DeleteAsync(string address);
Task<bool> ExistsAsync(string address);
```

**ITransactionRepository** - Transaction tracking
```csharp
Task AddTransactionAsync(string walletAddress, WalletTransaction transaction);
Task<IEnumerable<WalletTransaction>> GetTransactionsAsync(string walletAddress);
Task UpdateTransactionStateAsync(string transactionId, TransactionState state);
```

**Implementations:**
- `EFCoreWalletRepository` - Entity Framework Core
- `InMemoryWalletRepository` - In-memory (testing)
- `MongoWalletRepository` - MongoDB document database
- `FileWalletRepository` - Encrypted JSON files

#### 4. Encryption Layer (`Encryption/`)

**IEncryptionProvider** - Abstract encryption interface
```csharp
Task<byte[]> EncryptAsync(byte[] plaintext, string keyId);
Task<byte[]> DecryptAsync(byte[] ciphertext, string keyId);
Task<string> CreateKeyAsync(string keyName);
Task RotateKeyAsync(string keyId);
```

**Implementations:**
- `AzureKeyVaultEncryption` - Azure Key Vault
- `AwsKmsEncryption` - AWS Key Management Service
- `DataProtectionEncryption` - ASP.NET Core Data Protection
- `AesGcmEncryption` - Local AES-256-GCM

#### 5. Event Layer (`Events/`)

**IEventPublisher** - Abstract event bus
```csharp
Task PublishAsync<T>(T @event, string? topic = null) where T : class;
Task PublishBatchAsync<T>(IEnumerable<T> events, string? topic = null) where T : class;
```

**IEventSubscriber** - Event consumption
```csharp
Task SubscribeAsync<T>(string topic, Func<T, Task> handler) where T : class;
Task UnsubscribeAsync(string topic);
```

**Implementations:**
- `DaprEventBus` - Dapr pub/sub
- `RabbitMqEventBus` - RabbitMQ
- `AzureServiceBusEventBus` - Azure Service Bus
- `InMemoryEventBus` - In-memory for testing

### Data Models

#### Wallet Entity

```csharp
public class Wallet
{
    // Primary Key
    public string Address { get; set; }  // Primary wallet address (public key)

    // Cryptographic Data (Encrypted)
    public string EncryptedPrivateKey { get; set; }  // AES-256-GCM encrypted
    public string EncryptionKeyId { get; set; }  // Reference to encryption key
    public string Algorithm { get; set; }  // ED25519, SECP256K1, RSA

    // Ownership & Multi-tenancy
    public string Owner { get; set; }  // User subject (sub claim)
    public string Tenant { get; set; }  // Tenant identifier

    // Metadata
    public string Name { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string>? Tags { get; set; }

    // State
    public WalletStatus Status { get; set; }  // Active, Archived, Deleted
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Relationships
    public ICollection<WalletAddress> Addresses { get; set; }
    public ICollection<WalletAccess> Delegates { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; }

    // Version & Concurrency
    public int Version { get; set; }
    public byte[] RowVersion { get; set; }  // For optimistic concurrency
}

public enum WalletStatus
{
    Active,
    Archived,
    Deleted,
    Locked
}
```

#### WalletAddress Entity

```csharp
public class WalletAddress
{
    // Composite Key
    public string WalletId { get; set; }  // Foreign key to Wallet
    public string Address { get; set; }  // Derived public address

    // Derivation Info
    public string DerivationPath { get; set; }  // BIP44 path (m/44'/0'/0'/0/0)
    public int Index { get; set; }  // Address index

    // Metadata
    public string? Label { get; set; }
    public AddressType Type { get; set; }  // Receive, Change, Custom

    // Usage Tracking
    public bool IsUsed { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? FirstUsedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation
    public Wallet Wallet { get; set; }
}

public enum AddressType
{
    Receive,
    Change,
    Custom,
    WatchOnly
}
```

#### WalletAccess Entity

```csharp
public class WalletAccess
{
    // Primary Key
    public int Id { get; set; }

    // Foreign Keys
    public string WalletId { get; set; }

    // Access Control
    public string Subject { get; set; }  // User/service principal
    public string Tenant { get; set; }
    public AccessType AccessType { get; set; }

    // Metadata
    public string Reason { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string AssignedBy { get; set; }

    // State
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }

    // Navigation
    public Wallet Wallet { get; set; }
}

public enum AccessType
{
    None = 0,
    Owner = 1,
    DelegateReadWrite = 2,
    DelegateReadOnly = 3
}
```

#### WalletTransaction Entity

```csharp
public class WalletTransaction
{
    // Primary Key
    public string Id { get; set; }  // {TransactionId}:{WalletId}

    // Foreign Keys
    public string TransactionId { get; set; }
    public string WalletId { get; set; }

    // Transaction Data
    public string? PreviousId { get; set; }
    public string Sender { get; set; }
    public string ReceivedAddress { get; set; }

    // State
    public bool IsSendingWallet { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsSpent { get; set; }

    // Metadata
    public TransactionMetaData MetaData { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? SpentAt { get; set; }

    // Navigation
    public Wallet Wallet { get; set; }
}
```

### Security Architecture

#### 1. Key Encryption Hierarchy

```
User Passphrase (optional)
    │
    ▼
Master Encryption Key (MEK) ─────► Stored in Azure KV/AWS KMS/HSM
    │
    ▼
Data Encryption Key (DEK) ────────► Rotated periodically
    │
    ▼
Wallet Private Keys (Encrypted) ──► Stored in database
```

#### 2. Encryption at Rest

- **Private Keys**: AES-256-GCM with authenticated encryption
- **Mnemonics**: Never stored (user responsibility to backup)
- **DEK**: Encrypted by MEK (envelope encryption)
- **MEK**: Stored in hardware-backed key vault

#### 3. Encryption in Transit

- TLS 1.3 for all network communication
- Encrypted event payloads for sensitive data
- No private keys transmitted (only signatures)

#### 4. Access Control

- Multi-tenant isolation at database level
- Row-level security based on tenant
- Subject-based access control (SBAC)
- Delegation with time-based expiration
- Audit logging for all privileged operations

#### 5. Audit Logging

```csharp
public class AuditLog
{
    public long Id { get; set; }
    public string WalletAddress { get; set; }
    public string Action { get; set; }  // Created, Accessed, Modified, Deleted
    public string Subject { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public AuditSeverity Severity { get; set; }
}

public enum AuditSeverity
{
    Info,
    Warning,
    Critical
}
```

### Service Interactions

#### External Service Dependencies

**1. Siccar.Cryptography (Library)**
- Key generation (ED25519, SECP256K1)
- Signature creation and verification
- Hash functions (SHA-256, SHA-512)
- Mnemonic generation (BIP39)
- **Interaction:** Direct library calls (synchronous)

**2. Siccar.TransactionHandler (Library)**
- Transaction building and parsing
- Payload encryption/decryption
- Transaction validation
- **Interaction:** Direct library calls (synchronous)

**3. Register Service (External Microservice)**
- **Used For:** Retrieving transaction history when recovering wallets
- **Endpoints Used:**
  - `GET /transactions/recipient/{address}` - Get received transactions
  - `GET /transactions/sender/{address}` - Get sent transactions
  - `GET /transactions/{id}` - Get single transaction
- **Interaction:** HTTP/Dapr service invocation
- **Failure Mode:** Wallet can be created without transaction history (graceful degradation)

**4. Event Bus (Infrastructure)**
- **Published Events:**
  - `OnWallet_AddressCreated` - When new address derived
  - `OnTransaction_Pending` - When transaction signed
- **Subscribed Events:**
  - `OnTransaction_Confirmed` - Update transaction state to confirmed
- **Interaction:** Async pub/sub via Dapr/RabbitMQ/Azure Service Bus
- **Failure Mode:** Events buffered and retried

**5. Encryption Provider (Infrastructure)**
- **Azure Key Vault**: MEK storage and cryptographic operations
- **AWS KMS**: Alternative MEK storage
- **Local DPAPI**: Fallback encryption provider
- **Interaction:** Async encryption/decryption operations
- **Failure Mode:** Service degraded if encryption unavailable

**6. Authentication Service (External)**
- **JWT Token Validation:** Subject and tenant claims extraction
- **Interaction:** Delegated to API layer (out of scope for library)

#### Service Communication Patterns

**Synchronous:**
- Cryptography library calls
- TransactionHandler calls
- Direct repository operations

**Asynchronous:**
- Event publishing (fire-and-forget)
- Register Service calls (with timeout)
- Encryption operations (with retry)

**Event-Driven:**
- Transaction confirmation updates
- Wallet state change notifications
- Delegation change notifications

### Migration Path from Current Implementation

#### Phase 1: Core Library Extraction
1. Extract domain models to new `Siccar.WalletService` project
2. Define abstract interfaces (IWalletRepository, IEncryptionProvider, IEventPublisher)
3. Implement core services (WalletManager, KeyManager)
4. Create unit tests with in-memory implementations

#### Phase 2: Storage Migration
1. Implement EFCore repository using existing database schema
2. Create migration tool to validate data compatibility
3. Add encryption provider implementations
4. Test with existing production data (read-only)

#### Phase 3: API Decoupling
1. Create new `Siccar.WalletService.Api` project
2. Migrate controllers to use new service layer
3. Maintain existing REST endpoints for backward compatibility
4. Run both implementations in parallel (shadow mode)

#### Phase 4: Event System Migration
1. Implement event bus abstraction
2. Create Dapr event bus implementation
3. Migrate from direct Dapr calls to abstraction
4. Add event replay capability for migration

#### Phase 5: Cutover
1. Feature flag to switch between old and new implementations
2. Gradual traffic migration (canary deployment)
3. Monitor metrics and errors
4. Rollback capability if needed

#### Phase 6: Cleanup
1. Remove old WalletService implementation
2. Archive migration tools
3. Update documentation
4. Deprecation notices for old API patterns

## API Design

### Service Interface

```csharp
public interface IWalletService
{
    // Wallet Lifecycle
    Task<WalletCreationResult> CreateWalletAsync(CreateWalletRequest request, CancellationToken ct = default);
    Task<WalletRecoveryResult> RecoverWalletAsync(RecoverWalletRequest request, CancellationToken ct = default);
    Task<Wallet> GetWalletAsync(string address, string? userSubject = null, CancellationToken ct = default);
    Task<IEnumerable<Wallet>> GetWalletsByOwnerAsync(string ownerSubject, CancellationToken ct = default);
    Task<IEnumerable<Wallet>> GetWalletsByTenantAsync(string tenantId, CancellationToken ct = default);
    Task<Wallet> UpdateWalletAsync(string address, UpdateWalletRequest request, CancellationToken ct = default);
    Task DeleteWalletAsync(string address, CancellationToken ct = default);

    // Address Management
    Task<WalletAddress> GenerateAddressAsync(string walletAddress, DerivationPath path, CancellationToken ct = default);
    Task<IEnumerable<WalletAddress>> GetAddressesAsync(string walletAddress, CancellationToken ct = default);

    // Transaction Operations
    Task<SignedTransaction> SignTransactionAsync(string walletAddress, Transaction transaction, CancellationToken ct = default);
    Task<bool> VerifyTransactionAsync(Transaction transaction, CancellationToken ct = default);
    Task<byte[][]> DecryptPayloadsAsync(string walletAddress, TransactionModel transaction, CancellationToken ct = default);
    Task<IEnumerable<WalletTransaction>> GetTransactionsAsync(string walletAddress, TransactionFilter? filter = null, CancellationToken ct = default);

    // Delegation Management
    Task<WalletAccess> AddDelegateAsync(string walletAddress, AddDelegateRequest request, CancellationToken ct = default);
    Task<WalletAccess> UpdateDelegateAsync(string walletAddress, UpdateDelegateRequest request, CancellationToken ct = default);
    Task RemoveDelegateAsync(string walletAddress, string subject, CancellationToken ct = default);
    Task<IEnumerable<WalletAccess>> GetDelegatesAsync(string walletAddress, CancellationToken ct = default);

    // Access Control
    Task<bool> CanAccessWalletAsync(string walletAddress, string subject, AccessType requiredAccess, CancellationToken ct = default);
}
```

### Request/Response Models

```csharp
public record CreateWalletRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Mnemonic { get; init; }  // Optional for recovery
    public CryptoAlgorithm Algorithm { get; init; } = CryptoAlgorithm.ED25519;
    public string Tenant { get; init; }
    public string Owner { get; init; }
    public Dictionary<string, string>? Tags { get; init; }
}

public record WalletCreationResult
{
    public required Wallet Wallet { get; init; }
    public required string Mnemonic { get; init; }  // User must backup!
    public WalletAddress PrimaryAddress { get; init; }
}

public record RecoverWalletRequest
{
    public required string Name { get; init; }
    public required string Mnemonic { get; init; }
    public string? RegisterId { get; init; }  // For transaction history
    public CryptoAlgorithm Algorithm { get; init; } = CryptoAlgorithm.ED25519;
    public string Tenant { get; init; }
    public string Owner { get; init; }
}

public record UpdateWalletRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, string>? Tags { get; init; }
    public WalletStatus? Status { get; init; }
}

public record AddDelegateRequest
{
    public required string Subject { get; init; }
    public required AccessType AccessType { get; init; }
    public required string Reason { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record TransactionFilter
{
    public bool? IsConfirmed { get; init; }
    public bool? IsSpent { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 100;
}
```

## Testing Strategy

### Unit Tests (Target: >90% Coverage)

**Core Services:**
- `WalletManagerTests` - Wallet creation, recovery, updates
- `KeyManagerTests` - Key generation, derivation, encryption
- `TransactionServiceTests` - Signing, verification, decryption
- `DelegationManagerTests` - Access control logic

**Domain Models:**
- `WalletTests` - Entity behavior and validation
- `DerivationPathTests` - BIP44 path parsing
- `MnemonicTests` - BIP39 validation

**Repositories:**
- `InMemoryRepositoryTests` - In-memory implementation
- `EFCoreRepositoryTests` - EF Core with in-memory database

**Encryption:**
- `AesGcmEncryptionTests` - Local encryption
- `EncryptionProviderTests` - Provider abstraction

### Integration Tests

**Database Tests:**
- MySQL integration with real schema
- Cosmos DB integration
- MongoDB integration
- Concurrent access and locking

**Encryption Tests:**
- Azure Key Vault integration (requires Azure resources)
- AWS KMS integration (requires AWS resources)
- Key rotation scenarios

**Event Bus Tests:**
- Dapr pub/sub integration
- RabbitMQ integration
- Event ordering and replay

**End-to-End Tests:**
- Wallet creation → address generation → transaction signing → delegation
- Wallet recovery → transaction history sync
- Multi-tenant isolation verification

### Performance Tests

**Benchmarks:**
- Wallet creation throughput (target: >100/sec)
- Transaction signing latency (target: <50ms)
- Decryption performance (target: <100ms)
- Repository query performance (target: <10ms for indexed queries)

**Load Tests:**
- 10,000 wallets per tenant
- 1,000 concurrent operations
- Event bus throughput (target: >1000 events/sec)

### Security Tests

**Penetration Testing:**
- SQL injection attempts
- Access control bypass attempts
- Encryption key extraction attempts

**Compliance:**
- OWASP Top 10 verification
- GDPR data protection validation
- Key management best practices (NIST SP 800-57)

## Documentation Requirements

### Technical Documentation

1. **Architecture Decision Records (ADRs)**
   - Why abstract storage layer
   - Why multi-algorithm support
   - Why event-driven design
   - Encryption provider selection

2. **API Reference**
   - XML documentation for all public APIs
   - Code examples for common scenarios
   - Error handling patterns

3. **Integration Guide**
   - How to integrate library into new projects
   - Configuration options
   - Dependency injection setup
   - Event bus configuration

4. **Migration Guide**
   - Migrating from old WalletService
   - Database migration scripts
   - Breaking changes and workarounds
   - Rollback procedures

### User Documentation

1. **Quick Start Guide**
   - Installing the package
   - Creating first wallet
   - Signing a transaction

2. **Wallet Management Guide**
   - Wallet creation and recovery
   - Mnemonic backup best practices
   - Address management
   - Delegation setup

3. **Security Guide**
   - Key protection recommendations
   - Encryption provider selection
   - Audit logging setup
   - Compliance considerations

4. **Troubleshooting Guide**
   - Common errors and solutions
   - Performance optimization
   - Debugging tips

## Risks and Mitigations

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| **Data loss during migration** | Critical | Medium | Multi-phase migration with rollback, extensive testing with production data copies |
| **Performance regression** | High | Medium | Comprehensive benchmarks, load testing, gradual rollout with monitoring |
| **Breaking API changes** | High | High | Maintain backward compatibility layer, deprecation warnings, versioned APIs |
| **Security vulnerabilities** | Critical | Low | Security review, penetration testing, HSM support, audit logging |
| **Encryption provider failure** | High | Low | Multi-provider support, fallback mechanisms, key escrow for recovery |
| **Event bus message loss** | Medium | Medium | Durable queues, retry logic, dead-letter queues, event replay capability |
| **Multi-tenancy data leakage** | Critical | Low | Row-level security, integration tests, security audits, penetration testing |
| **HD wallet incompatibility** | Medium | Medium | BIP32/BIP39/BIP44 compliance testing, test vectors from reference implementations |

## Success Criteria

### Functional Requirements

✅ Create wallets with BIP39 mnemonics (12/24 words)
✅ Recover wallets from mnemonics
✅ Support ED25519, SECP256K1, RSA algorithms
✅ Generate HD wallet addresses (BIP44)
✅ Sign transactions with wallet keys
✅ Decrypt transaction payloads
✅ Manage delegations (add, update, remove)
✅ Multi-tenant isolation
✅ Event publishing for state changes

### Non-Functional Requirements

✅ >90% unit test coverage
✅ <50ms transaction signing latency (p95)
✅ <100ms payload decryption latency (p95)
✅ Support 10,000+ wallets per tenant
✅ Zero downtime migration
✅ Backward compatibility with existing API
✅ Complete XML API documentation
✅ Security audit passed

### Migration Requirements

✅ 100% data migrated successfully
✅ All existing integrations continue working
✅ No user-facing downtime
✅ Rollback capability within 1 hour
✅ Performance equal or better than current

## Timeline

### Phase 1: Foundation (Weeks 1-3)
- Project setup and dependencies
- Domain models and interfaces
- Core service implementations
- Unit tests (in-memory)

### Phase 2: Storage Layer (Weeks 4-6)
- Repository implementations
- Database migrations
- Encryption providers
- Integration tests

### Phase 3: Event System (Weeks 7-8)
- Event bus abstraction
- Event publisher implementations
- Event handler registration
- Event integration tests

### Phase 4: Migration Tooling (Weeks 9-10)
- Data migration scripts
- Validation tools
- Rollback procedures
- Shadow mode implementation

### Phase 5: API Layer (Weeks 11-12)
- New API project
- Controller migration
- Backward compatibility layer
- API integration tests

### Phase 6: Testing & Security (Weeks 13-14)
- Performance testing
- Security audit
- Penetration testing
- Load testing

### Phase 7: Documentation (Week 15)
- API reference
- Integration guide
- Migration guide
- Security guide

### Phase 8: Deployment (Weeks 16-18)
- Staging deployment
- Canary deployment
- Gradual rollout
- Monitoring and validation

### Phase 9: Cutover (Week 19)
- Full production deployment
- Old service deprecation
- Post-deployment validation

### Phase 10: Cleanup (Week 20)
- Remove old code
- Archive migration tools
- Final documentation
- Retrospective

**Total Duration:** 20 weeks (5 months)

## Dependencies

### Internal Dependencies

- **Siccar.Cryptography** v2.0 - Key generation, signing, hashing
- **Siccar.TransactionHandler** v1.0 - Transaction building and payload management
- **Siccar.Common** - Shared models and utilities

### External Dependencies

- **.NET 8.0** - Runtime framework
- **Entity Framework Core 8.0** - ORM for SQL databases
- **Azure.Identity** - Azure authentication
- **Azure.Security.KeyVault.Keys** - Azure Key Vault integration
- **AWSSDK.KeyManagementService** - AWS KMS integration
- **Dapr.Client** - Dapr SDK
- **RabbitMQ.Client** - RabbitMQ integration
- **MongoDB.Driver** - MongoDB integration
- **NBitcoin** - BIP32/BIP39/BIP44 implementation
- **BenchmarkDotNet** - Performance testing
- **Moq** - Mocking framework
- **FluentAssertions** - Test assertions
- **xUnit** - Test framework

### Infrastructure Dependencies

- **MySQL 8.0+** or **PostgreSQL 14+** - Primary database
- **Azure Key Vault** or **AWS KMS** - Key management
- **Dapr** or **RabbitMQ** or **Azure Service Bus** - Event bus
- **Kubernetes** - Container orchestration (optional)
- **Azure Blob Storage** (optional) - Data protection key persistence

## Appendix

### A. Glossary

- **BIP32** - Bitcoin Improvement Proposal 32: Hierarchical Deterministic Wallets
- **BIP39** - Bitcoin Improvement Proposal 39: Mnemonic code for generating deterministic keys
- **BIP44** - Bitcoin Improvement Proposal 44: Multi-Account Hierarchy for Deterministic Wallets
- **ED25519** - Edwards-curve Digital Signature Algorithm using Curve25519
- **SECP256K1** - Elliptic curve used in Bitcoin's public key cryptography
- **HD Wallet** - Hierarchical Deterministic wallet that can generate many key pairs from a single seed
- **MEK** - Master Encryption Key: Top-level key used to encrypt DEKs
- **DEK** - Data Encryption Key: Key used to encrypt actual data
- **DPAPI** - Data Protection API: Windows encryption service
- **HSM** - Hardware Security Module: Physical device for key management
- **UTXO** - Unspent Transaction Output: Blockchain accounting model

### B. References

- [BIP32 Specification](https://github.com/bitcoin/bips/blob/master/bip-0032.mediawiki)
- [BIP39 Specification](https://github.com/bitcoin/bips/blob/master/bip-0039.mediawiki)
- [BIP44 Specification](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki)
- [NIST SP 800-57: Key Management](https://csrc.nist.gov/publications/detail/sp/800-57-part-1/rev-5/final)
- [Azure Key Vault Best Practices](https://docs.microsoft.com/en-us/azure/key-vault/general/best-practices)
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Dapr Documentation](https://docs.dapr.io/)

### C. Related Specifications

- [siccar-cryptography-rewrite.md](siccar-cryptography-rewrite.md) - Cryptography library specification
- [siccar-transaction-handler.md](siccar-transaction-handler.md) - Transaction handler specification
- [constitution.md](../constitution.md) - Project constitution and principles

---

**Document Status:** Draft for Review
**Next Review Date:** 2025-11-20
**Approval Required From:** Architecture Team, Security Team, Platform Team
