# Sorcha.Register.Service Specification

**Version:** 2.0
**Date:** 2025-11-13
**Status:** Proposed
**Related Constitution:** [constitution.md](../constitution.md)
**Related Specifications:**
- [sorcha-cryptography-rewrite.md](sorcha-cryptography-rewrite.md)
- [sorcha-wallet-service.md](sorcha-wallet-service.md)
- [sorcha-transaction-handler.md](sorcha-transaction-handler.md)

**Related Documentation:**
- [Blockchain Transaction Format - JSON-LD Specification](../../docs/blockchain-transaction-format.md)

## Executive Summary

This specification defines the requirements for creating the **Sorcha.Register.Service** - a distributed ledger and block management service for the Sorcha platform. This service handles register (ledger) creation, transaction storage, docket (block) management, and distributed data synchronization. The implementation follows Sorcha's cloud-native architecture using .NET Aspire for orchestration, microservices patterns, and pluggable storage abstractions.

## Purpose and Scope

### Primary Purpose

The Register Service is the foundational ledger component of the Sorcha platform, responsible for:

1. **Register Management** - Create and manage distributed ledgers with unique identities
2. **Transaction Storage** - Persist validated transactions with encrypted payloads
3. **Docket Management** - Seal transactions into blocks (dockets) maintaining chain integrity
4. **Query Services** - Provide advanced querying capabilities for ledger data
5. **Multi-Tenant Isolation** - Enforce tenant boundaries and access control
6. **Real-time Notifications** - Notify participants of ledger state changes
7. **Distributed Synchronization** - Coordinate with peer nodes for data replication

### Strategic Goals

1. **Distributed Ledger Foundation** - Provide immutable, auditable transaction storage
2. **High Performance** - Support high-throughput transaction processing (1000+ tx/s per register)
3. **Scalability** - Handle 10,000+ registers with millions of transactions each
4. **Query Flexibility** - Support complex queries via OData and LINQ
5. **Cloud-Native Design** - Leverage .NET Aspire for orchestration and scalability
6. **Storage Flexibility** - Support multiple storage backends (MongoDB, PostgreSQL, CosmosDB)
7. **Event-Driven** - Integrate via messaging for loose coupling
8. **Developer-Friendly** - Clean abstractions and comprehensive documentation

## Background and Analysis

### Architectural Evolution from Siccar V3

The Sorcha Register Service is based on the proven architecture from Siccar V3 but modernized for cloud-native deployment:

**Key Improvements:**
- **.NET Aspire** replaces Dapr for service orchestration
- **.NET 10** with modern C# 13 features
- **Minimal APIs** instead of traditional controllers
- **gRPC support** for high-performance service-to-service communication
- **Enhanced observability** with OpenTelemetry
- **Flexible storage** with EF Core and NoSQL options
- **Improved testing** with in-memory providers and Testcontainers

**Retained Strengths:**
- **Storage abstraction** via repository pattern
- **Event-driven architecture** for loose coupling
- **Multi-tenant isolation** for enterprise deployments
- **OData query support** for flexible data access
- **SignalR integration** for real-time updates
- **Chain validation** for data integrity

### Current Status

⚠️ **This service is being specified for implementation**

A placeholder specification exists at `.specify/specs/sorcha-register-service.md` with minimal interface definitions. The Wallet Service is designed to gracefully degrade if the Register Service is unavailable.

## Core Concepts

### Register

A **Register** is a distributed ledger instance that stores transactions and dockets. Each register:
- Has a unique identifier (GUID without hyphens)
- Maintains a current height (block number)
- Tracks status (OFFLINE, ONLINE, CHECKING, RECOVERY)
- Can be advertised to the network or private
- May be a full replica or partial node
- Belongs to a specific tenant for access control

### Transaction

A **Transaction** represents a signed data submission to a register. Each transaction:
- Contains a unique transaction ID (64 char hex hash)
- Links to a previous transaction (blockchain chain)
- Includes sender and recipient wallet addresses
- Carries encrypted payloads with selective disclosure
- Contains blueprint metadata for workflow tracking
- Is cryptographically signed for integrity
- Tracks version for schema evolution

**JSON-LD Representation:**
All transactions MUST be representable in JSON-LD format following the [Blockchain Transaction Format specification](../../docs/blockchain-transaction-format.md). Transactions are addressable via DID URIs:
- DID Format: `did:sorcha:register:{registerId}/tx/{txId}`
- JSON-LD Context: `https://sorcha.io/contexts/blockchain/v1.jsonld`
- Supports semantic web integration and universal resolvability

### Docket

A **Docket** (equivalent to a blockchain block) seals a collection of transactions. Each docket:
- Has a unique ID corresponding to the block height
- Contains a hash of the previous docket (chain link)
- References all transactions included in the block
- Includes a timestamp and state (Proposed, Accepted, Sealed)
- Carries consensus voting information
- Is immutable once sealed
- Increments the register height when confirmed

### Payload

A **Payload** contains encrypted data within a transaction. Each payload:
- Is encrypted using AES-256-GCM
- Specifies authorized wallets for decryption
- Includes integrity hash (SHA-256)
- Contains initialization vector and challenges for key derivation
- Supports selective disclosure based on wallet access
- Tracks size for storage management

## Architecture

### High-Level Design

```
┌───────────────────────────────────────────────────────────────┐
│                 Sorcha.Register.Service                        │
│                  (Microservice Layer)                          │
├───────────────────────────────────────────────────────────────┤
│  API Layer (Minimal APIs)  │  Real-time Layer (SignalR)       │
│  - RegistersAPI            │  - RegisterHub                   │
│  - TransactionsAPI         │  - Group subscriptions           │
│  - DocketsAPI              │  - Real-time events              │
│  - QueryAPI (OData)        │                                  │
├───────────────────────────────────────────────────────────────┤
│  Business Logic Layer                                         │
│  - RegisterManager         │  - TransactionManager            │
│  - DocketManager           │  - QueryManager                  │
│  - ChainValidator          │  - TenantResolver                │
├───────────────────────────────────────────────────────────────┤
│  Storage Abstraction Layer                                    │
│  IRegisterRepository       │  IEventPublisher/Subscriber      │
│  - MongoDB                 │  - Aspire Messaging              │
│  - PostgreSQL (EF Core)    │  - RabbitMQ                      │
│  - InMemory (testing)      │  - Azure Service Bus             │
│  - CosmosDB (future)       │  - InMemory (testing)            │
├───────────────────────────────────────────────────────────────┤
│  Cross-Cutting Concerns                                       │
│  - OpenTelemetry           │  - Health Checks                 │
│  - Structured Logging      │  - Configuration                 │
│  - Authentication/AuthZ    │  - Caching (Redis)               │
└───────────────────────────────────────────────────────────────┘
           │                          │                  │
           ▼                          ▼                  ▼
    ┌────────────┐         ┌──────────────┐    ┌──────────────┐
    │  Storage   │         │  Validator   │    │    Wallet    │
    │  Backend   │         │   Service    │    │   Service    │
    └────────────┘         └──────────────┘    └──────────────┘
```

### Sorcha 4-Layer Architecture Alignment

```
Sorcha/
├── src/
│   ├── Apps/                           # Application Layer
│   │   └── Sorcha.AppHost             # Aspire orchestration
│   │
│   ├── Services/                       # Service Layer (NEW)
│   │   └── Sorcha.Register.Service/
│   │       ├── Program.cs             # Minimal API entry point
│   │       ├── APIs/                  # API endpoint definitions
│   │       │   ├── RegistersApi.cs
│   │       │   ├── TransactionsApi.cs
│   │       │   ├── DocketsApi.cs
│   │       │   └── QueryApi.cs
│   │       ├── Hubs/
│   │       │   └── RegisterHub.cs     # SignalR real-time hub
│   │       └── Configuration/
│   │           └── ServiceConfiguration.cs
│   │
│   ├── Core/                           # Business Logic Layer
│   │   ├── Sorcha.Register.Core/      # Core library (NEW)
│   │   │   ├── Managers/
│   │   │   │   ├── RegisterManager.cs
│   │   │   │   ├── TransactionManager.cs
│   │   │   │   ├── DocketManager.cs
│   │   │   │   └── QueryManager.cs
│   │   │   ├── Storage/
│   │   │   │   ├── IRegisterRepository.cs
│   │   │   │   ├── ITransactionRepository.cs
│   │   │   │   └── IDocketRepository.cs
│   │   │   ├── Events/
│   │   │   │   ├── IEventPublisher.cs
│   │   │   │   ├── IEventSubscriber.cs
│   │   │   │   └── RegisterEvents.cs
│   │   │   ├── Validators/
│   │   │   │   ├── ChainValidator.cs
│   │   │   │   └── DocketValidator.cs
│   │   │   └── Authorization/
│   │   │       ├── ITenantResolver.cs
│   │   │       └── TenantAccessPolicy.cs
│   │   │
│   │   ├── Sorcha.Register.Storage.MongoDB/  # MongoDB implementation
│   │   ├── Sorcha.Register.Storage.PostgreSQL/  # PostgreSQL implementation
│   │   ├── Sorcha.Register.Storage.InMemory/    # Testing implementation
│   │   └── Sorcha.Register.Events.Aspire/       # Aspire messaging
│   │
│   └── Common/                         # Common Layer
│       ├── Sorcha.Register.Models/    # Domain models (NEW)
│       │   ├── Register.cs
│       │   ├── TransactionModel.cs
│       │   ├── Docket.cs
│       │   ├── PayloadModel.cs
│       │   ├── TransactionMetaData.cs
│       │   └── Enums/
│       │       ├── RegisterStatus.cs
│       │       ├── DocketState.cs
│       │       └── TransactionType.cs
│       │
│       ├── Sorcha.Register.Client/    # Client library (NEW)
│       │   ├── IRegisterServiceClient.cs
│       │   ├── RegisterServiceClient.cs
│       │   └── SignalRHubClient.cs
│       │
│       └── Sorcha.ServiceDefaults      # Shared configurations
│
└── tests/                              # Test Projects
    ├── Sorcha.Register.Core.Tests     # Unit tests
    ├── Sorcha.Register.Service.Tests  # API tests
    ├── Sorcha.Register.Integration.Tests  # Integration tests
    └── Sorcha.Register.Performance.Tests  # Performance tests
```

### Data Models

#### Register

```csharp
namespace Sorcha.Register.Models;

/// <summary>
/// Represents a distributed ledger register
/// </summary>
public class Register
{
    /// <summary>
    /// Unique identifier (GUID without hyphens)
    /// </summary>
    [Required]
    [StringLength(32, MinimumLength = 32)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable register name
    /// </summary>
    [Required]
    [StringLength(38, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current block height (number of sealed dockets)
    /// </summary>
    public uint Height { get; set; }

    /// <summary>
    /// Register operational status
    /// </summary>
    public RegisterStatus Status { get; set; } = RegisterStatus.Offline;

    /// <summary>
    /// Whether register is advertised to network peers
    /// </summary>
    public bool Advertise { get; set; }

    /// <summary>
    /// Whether this node maintains full transaction history
    /// </summary>
    public bool IsFullReplica { get; set; } = true;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation
    /// </summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Register creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Consensus votes (implementation TBD)
    /// </summary>
    public string? Votes { get; set; }
}

public enum RegisterStatus
{
    Offline = 0,
    Online = 1,
    Checking = 2,
    Recovery = 3
}
```

#### TransactionModel

```csharp
namespace Sorcha.Register.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a signed transaction in a register with JSON-LD support
/// </summary>
public class TransactionModel
{
    /// <summary>
    /// JSON-LD context for semantic web integration
    /// </summary>
    [JsonPropertyName("@context")]
    public string? Context { get; set; } = "https://sorcha.io/contexts/blockchain/v1.jsonld";

    /// <summary>
    /// JSON-LD type designation
    /// </summary>
    [JsonPropertyName("@type")]
    public string? Type { get; set; } = "Transaction";

    /// <summary>
    /// JSON-LD universal identifier (DID URI)
    /// Format: did:sorcha:register:{registerId}/tx/{txId}
    /// </summary>
    [JsonPropertyName("@id")]
    public string? Id { get; set; }

    /// <summary>
    /// Register identifier this transaction belongs to
    /// </summary>
    [Required]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction identifier (64 character hex hash)
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string TxId { get; set; } = string.Empty;

    /// <summary>
    /// Previous transaction ID for blockchain chain
    /// </summary>
    [StringLength(64, MinimumLength = 64)]
    public string PrevTxId { get; set; } = string.Empty;

    /// <summary>
    /// Block number (docket ID) this transaction is sealed in
    /// </summary>
    public ulong? BlockNumber { get; set; }

    /// <summary>
    /// Transaction format version
    /// </summary>
    public uint Version { get; set; } = 1;

    /// <summary>
    /// Sender wallet address (Base58 encoded)
    /// </summary>
    [Required]
    public string SenderWallet { get; set; } = string.Empty;

    /// <summary>
    /// Recipient wallet addresses
    /// </summary>
    public IEnumerable<string> RecipientsWallets { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Transaction timestamp (UTC)
    /// </summary>
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Blueprint and workflow metadata
    /// </summary>
    public TransactionMetaData? MetaData { get; set; }

    /// <summary>
    /// Number of payloads in transaction
    /// </summary>
    public ulong PayloadCount { get; set; }

    /// <summary>
    /// Encrypted data payloads
    /// </summary>
    public PayloadModel[] Payloads { get; set; } = Array.Empty<PayloadModel>();

    /// <summary>
    /// Cryptographic signature of transaction
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Generates the DID URI for this transaction
    /// </summary>
    public string GenerateDidUri() => $"did:sorcha:register:{RegisterId}/tx/{TxId}";
}
```

#### Docket

```csharp
namespace Sorcha.Register.Models;

/// <summary>
/// Represents a sealed block of transactions (docket)
/// </summary>
public class Docket
{
    /// <summary>
    /// Docket identifier (block height)
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Register identifier this docket belongs to
    /// </summary>
    [Required]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Hash of previous docket for chain integrity
    /// </summary>
    public string PreviousHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of this docket
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// List of transaction IDs sealed in this docket
    /// </summary>
    public List<string> TransactionIds { get; set; } = new();

    /// <summary>
    /// Docket creation timestamp (UTC)
    /// </summary>
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current docket lifecycle state
    /// </summary>
    public DocketState State { get; set; } = DocketState.Init;

    /// <summary>
    /// Docket metadata
    /// </summary>
    public TransactionMetaData? MetaData { get; set; }

    /// <summary>
    /// Consensus votes (implementation TBD)
    /// </summary>
    public string? Votes { get; set; }
}

public enum DocketState
{
    Init = 0,
    Proposed = 1,
    Accepted = 2,
    Rejected = 3,
    Sealed = 4
}
```

#### PayloadModel

```csharp
namespace Sorcha.Register.Models;

/// <summary>
/// Encrypted payload within a transaction
/// </summary>
public class PayloadModel
{
    /// <summary>
    /// Wallet addresses authorized to decrypt this payload
    /// </summary>
    public string[] WalletAccess { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Size of encrypted payload in bytes
    /// </summary>
    public ulong PayloadSize { get; set; }

    /// <summary>
    /// SHA-256 hash of payload for integrity
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted data (Base64 encoded)
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Encryption metadata flags
    /// </summary>
    public string? PayloadFlags { get; set; }

    /// <summary>
    /// Initialization vector for encryption
    /// </summary>
    public Challenge? IV { get; set; }

    /// <summary>
    /// Per-wallet encryption challenges
    /// </summary>
    public Challenge[]? Challenges { get; set; }
}

public class Challenge
{
    public string? Data { get; set; }
    public string? Address { get; set; }
}
```

#### TransactionMetaData

```csharp
namespace Sorcha.Register.Models;

/// <summary>
/// Metadata for blueprint workflow tracking
/// </summary>
public class TransactionMetaData
{
    /// <summary>
    /// Register this transaction belongs to
    /// </summary>
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Type of transaction
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// Blueprint definition identifier
    /// </summary>
    public string? BlueprintId { get; set; }

    /// <summary>
    /// Blueprint instance identifier
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Current action/step in blueprint
    /// </summary>
    public uint? ActionId { get; set; }

    /// <summary>
    /// Next action/step in blueprint
    /// </summary>
    public uint? NextActionId { get; set; }

    /// <summary>
    /// Custom tracking data (JSON serialized)
    /// </summary>
    public SortedList<string, string>? TrackingData { get; set; }
}

public enum TransactionType
{
    Genesis = 0,
    Action = 1,
    Docket = 2,
    System = 3
}
```

### Storage Abstraction

```csharp
namespace Sorcha.Register.Core.Storage;

/// <summary>
/// Repository interface for register operations
/// </summary>
public interface IRegisterRepository
{
    // Register Operations
    Task<bool> IsLocalRegisterAsync(string registerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Register>> GetRegistersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Register>> QueryRegistersAsync(
        Func<Register, bool> predicate,
        CancellationToken cancellationToken = default);
    Task<Register?> GetRegisterAsync(string registerId, CancellationToken cancellationToken = default);
    Task<Register> InsertRegisterAsync(Register newRegister, CancellationToken cancellationToken = default);
    Task<Register> UpdateRegisterAsync(Register register, CancellationToken cancellationToken = default);
    Task DeleteRegisterAsync(string registerId, CancellationToken cancellationToken = default);
    Task<int> CountRegistersAsync(CancellationToken cancellationToken = default);

    // Docket Operations
    Task<IEnumerable<Docket>> GetDocketsAsync(
        string registerId,
        CancellationToken cancellationToken = default);
    Task<Docket?> GetDocketAsync(
        string registerId,
        ulong docketId,
        CancellationToken cancellationToken = default);
    Task<Docket> InsertDocketAsync(Docket docket, CancellationToken cancellationToken = default);
    Task UpdateRegisterHeightAsync(
        string registerId,
        uint newHeight,
        CancellationToken cancellationToken = default);

    // Transaction Operations
    Task<IQueryable<TransactionModel>> GetTransactionsAsync(
        string registerId,
        CancellationToken cancellationToken = default);
    Task<TransactionModel?> GetTransactionAsync(
        string registerId,
        string transactionId,
        CancellationToken cancellationToken = default);
    Task<TransactionModel> InsertTransactionAsync(
        TransactionModel transaction,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionModel>> QueryTransactionsAsync(
        string registerId,
        Expression<Func<TransactionModel, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionModel>> GetTransactionsByDocketAsync(
        string registerId,
        ulong docketId,
        CancellationToken cancellationToken = default);

    // Advanced Queries
    Task<IEnumerable<TransactionModel>> GetAllTransactionsByRecipientAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionModel>> GetAllTransactionsBySenderAddressAsync(
        string registerId,
        string address,
        CancellationToken cancellationToken = default);
}
```

### Event System

```csharp
namespace Sorcha.Register.Core.Events;

/// <summary>
/// Event publisher abstraction for register events
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(string topic, TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class;
}

/// <summary>
/// Event subscriber abstraction for consuming events
/// </summary>
public interface IEventSubscriber
{
    Task SubscribeAsync<TEvent>(string topic, Func<TEvent, Task> handler, CancellationToken cancellationToken = default)
        where TEvent : class;
}

// Event Definitions

public class RegisterCreatedEvent
{
    public string RegisterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RegisterDeletedEvent
{
    public string RegisterId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}

public class TransactionConfirmedEvent
{
    public string TransactionId { get; set; } = string.Empty;
    public string RegisterId { get; set; } = string.Empty;
    public List<string> ToWallets { get; set; } = new();
    public string SenderWallet { get; set; } = string.Empty;
    public string PreviousTransactionId { get; set; } = string.Empty;
    public TransactionMetaData? MetaData { get; set; }
    public DateTime ConfirmedAt { get; set; }
}

public class DocketConfirmedEvent
{
    public string RegisterId { get; set; } = string.Empty;
    public ulong DocketId { get; set; }
    public List<string> TransactionIds { get; set; } = new();
    public string Hash { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
}

public class RegisterHeightUpdatedEvent
{
    public string RegisterId { get; set; } = string.Empty;
    public uint OldHeight { get; set; }
    public uint NewHeight { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## Integration Points

### Service Dependencies

#### 1. Validator Service
**Purpose:** Transaction validation and consensus

**Integration:**
- Subscribes to `TransactionValidationCompleted` events
- Subscribes to `TransactionSubmitted` events
- Subscribes to `DocketConfirmed` events
- Publishes `TransactionConfirmed` when stored
- Receives validated transaction payloads
- Accepts consensus-approved dockets

#### 2. Wallet Service
**Purpose:** Transaction signing and address management

**Integration:**
- Publishes `TransactionConfirmed` events for wallet updates
- Subscribes to `WalletAddressCreated` events
- Queries wallet service for signature verification
- Supports wallet-based payload decryption
- Tracks sender and recipient addresses

#### 3. Peer Service
**Purpose:** Network synchronization and distributed consensus

**Integration (Future):**
- Synchronize register state across network
- Replicate transactions and dockets to peers
- Participate in distributed consensus
- Handle network topology changes
- Support register discovery and advertising

#### 4. Tenant Service
**Purpose:** Multi-tenant isolation and authorization

**Integration:**
- Query tenant service for register access control
- Validate user permissions for register operations
- Filter registers by tenant membership
- Support tenant-scoped queries
- Integration via .NET Aspire service discovery

### Aspire Messaging Topics

**Subscribed Events:**
- `transaction:validation-completed` - Receive validated transactions
- `transaction:submitted` - Receive submitted transactions (alternate path)
- `docket:confirmed` - Receive consensus-approved dockets
- `wallet:address-created` - Track wallet address registrations

**Published Events:**
- `register:created` - Notify register creation
- `register:deleted` - Notify register deletion
- `register:height-updated` - Notify block height increments
- `transaction:confirmed` - Notify transaction storage and confirmation
- `transaction:received` - Notify transaction receipt (for monitoring)
- `docket:proposed` - Notify docket proposal (for validation)
- `docket:sealed` - Notify docket sealing (for replication)

### Technology Stack

**Runtime:**
- .NET 10 (10.0.100)
- C# 13
- ASP.NET Core 10

**Frameworks:**
- .NET Aspire 9.5+ for orchestration
- Minimal APIs for REST endpoints
- SignalR for real-time notifications
- gRPC for service-to-service calls (optional)

**Storage:**
- **Primary:** MongoDB 7.0+ for document storage
- **Alternative:** PostgreSQL 16+ with EF Core for relational storage
- **Testing:** In-memory provider for unit tests
- **Future:** CosmosDB, DynamoDB support

**Caching:**
- Redis for distributed caching
- In-memory cache for local operations

**Observability:**
- OpenTelemetry for distributed tracing
- Serilog for structured logging
- Prometheus metrics (via Aspire)
- Health checks and readiness probes

**API:**
- OData V4 for advanced queries
- OpenAPI/Swagger for documentation
- Scalar for API explorer UI

**Testing:**
- xUnit for test framework
- Moq for mocking
- FluentAssertions for assertions
- Testcontainers for integration tests
- NBomber for performance tests

## Functional Requirements

### FR-REG-001: Register Management

**As a** system administrator
**I want to** create and manage distributed ledger registers
**So that** I can organize transactions into separate ledgers

**Acceptance Criteria:**
- Create registers with unique IDs and names
- Update register metadata (name, status, settings)
- Delete registers with all associated data
- List registers with tenant filtering
- Query registers by ID, name, or custom predicates
- Support up to 10,000 registers per installation
- Track register creation and update timestamps
- Enforce register name length limits (1-38 characters)

### FR-REG-002: Transaction Storage

**As a** blockchain participant
**I want to** store validated transactions in registers
**So that** they are immutable and auditable

**Acceptance Criteria:**
- Insert validated transactions into specified registers
- Store encrypted payloads with wallet-based access control
- Maintain transaction chain integrity (prevTxId links)
- Support transaction versioning for schema evolution
- Track sender and recipient wallet addresses
- Store blueprint metadata for workflow tracking
- Generate transaction IDs as cryptographic hashes
- Publish transaction confirmed events

### FR-REG-003: Docket Management

**As a** consensus validator
**I want to** seal transactions into dockets (blocks)
**So that** the ledger maintains chain integrity

**Acceptance Criteria:**
- Create dockets with sequential IDs (block heights)
- Include list of transaction IDs in each docket
- Maintain chain integrity with previousHash links
- Update register height atomically when docket is sealed
- Support docket states: Init, Proposed, Accepted, Rejected, Sealed
- Validate docket hash integrity
- Track docket timestamps
- Publish docket confirmed events

### FR-REG-004: Query Services

**As a** application developer
**I want to** query register data flexibly
**So that** I can retrieve specific transactions and dockets

**Acceptance Criteria:**
- Support OData queries ($filter, $select, $orderby, $top, $skip)
- Query transactions by sender/recipient address
- Query transactions by blueprint ID or instance ID
- Query transactions by docket ID
- Query dockets by register and height range
- Support LINQ expression queries at repository level
- Return queryable interfaces for efficient pagination
- Support full-text search on metadata (future)

### FR-REG-004A: JSON-LD and DID Resolution

**As a** semantic web application
**I want to** resolve DID URIs to transaction data in JSON-LD format
**So that** I can integrate with W3C standards and enable universal addressability

**Acceptance Criteria:**
- Support DID URI format: `did:sorcha:register:{registerId}/tx/{txId}`
- Resolve DID URIs via GET endpoint: `/api/registers/{registerId}/transactions/{txId}`
- Return transactions in JSON-LD format with `@context`, `@type`, and `@id` fields
- Support content negotiation for `application/ld+json` Accept header
- Generate DID URIs automatically when transactions are stored
- Include DID URI in all API responses containing transactions
- Validate DID URI format in API requests
- Support both compact and expanded JSON-LD forms
- Serve blockchain JSON-LD context at: `https://sorcha.io/contexts/blockchain/v1.jsonld`
- Cache JSON-LD contexts for performance

### FR-REG-005: Multi-Tenant Isolation

**As a** platform operator
**I want to** isolate registers by tenant
**So that** tenants cannot access each other's data

**Acceptance Criteria:**
- Associate each register with a tenant ID
- Filter all queries by tenant context
- Enforce tenant boundaries in authorization
- Support installation-wide admin access
- Track tenant membership via Tenant Service
- Reject operations on unauthorized registers
- Audit tenant access attempts

### FR-REG-006: Real-time Notifications

**As a** application user
**I want to** receive real-time updates when register state changes
**So that** I can react to new transactions and dockets

**Acceptance Criteria:**
- Provide SignalR hub for real-time subscriptions
- Support group-based subscriptions per register
- Notify on new transactions
- Notify on new dockets
- Notify on register height updates
- Authenticate SignalR connections via JWT
- Handle connection lifecycle (connect, disconnect, reconnect)
- Support thousands of concurrent connections

### FR-REG-007: Chain Validation

**As a** system monitor
**I want to** validate chain integrity
**So that** I can detect data corruption or tampering

**Acceptance Criteria:**
- Validate transaction chain (prevTxId links)
- Validate docket chain (previousHash links)
- Detect missing or orphaned transactions
- Verify register height consistency
- Generate integrity reports
- Support repair operations for detected issues
- Log validation failures for investigation

### FR-REG-008: Storage Abstraction

**As a** platform architect
**I want to** support multiple storage backends
**So that** deployments can choose optimal storage for their needs

**Acceptance Criteria:**
- Define IRegisterRepository abstraction
- Implement MongoDB repository with connection pooling
- Implement PostgreSQL repository with EF Core
- Implement in-memory repository for testing
- Support configuration-driven storage selection
- Maintain data portability across backends
- Document migration procedures

### FR-REG-009: Event Integration

**As a** system integrator
**I want to** publish and subscribe to register events
**So that** services can react to ledger changes

**Acceptance Criteria:**
- Publish register created/deleted events
- Publish transaction confirmed events
- Publish docket confirmed events
- Subscribe to validation completed events
- Subscribe to wallet address created events
- Support multiple event providers (Aspire, RabbitMQ, Azure Service Bus)
- Implement idempotent event handlers
- Support event replay for recovery

### FR-REG-010: Address Indexing

**As a** wallet service
**I want to** query transactions by wallet address
**So that** I can retrieve transaction history for wallets

**Acceptance Criteria:**
- Index transactions by sender address
- Index transactions by recipient addresses
- Support efficient address-based queries
- Return all transactions for a given address
- Sort results by timestamp
- Support pagination for large result sets
- Cache frequently accessed addresses

## Non-Functional Requirements

### NFR-REG-001: Performance

**Requirement:** High-throughput transaction processing

**Targets:**
- Transaction insert: < 100ms (p99)
- Transaction query by ID: < 50ms (p99)
- Register list (100 items): < 200ms (p99)
- Address query (1000 transactions): < 500ms (p99)
- Docket insert with height update: < 150ms (p99)
- Support 1,000+ transactions/second per register
- Support 100+ concurrent API requests

**Validation:**
- Performance benchmarks with NBomber
- Load testing with production-like data volumes
- Monitoring via Application Insights

### NFR-REG-002: Scalability

**Requirement:** Support enterprise-scale deployments

**Targets:**
- 10,000+ registers per installation
- 1,000,000+ transactions per register
- 10,000+ concurrent SignalR connections
- Horizontal scaling via MongoDB sharding
- Read replicas for query scaling
- Stateless API for multi-instance deployment

**Validation:**
- Capacity testing with scaled data
- Sharding strategy documentation
- Multi-instance deployment tests

### NFR-REG-003: Reliability

**Requirement:** High availability and data integrity

**Targets:**
- 99.9% uptime SLA
- Zero data loss guarantee
- Automatic retry with exponential backoff
- Circuit breaker for external dependencies
- Health checks every 10 seconds
- Graceful degradation when dependencies unavailable

**Validation:**
- Chaos engineering tests
- Failover scenario testing
- Health check monitoring

### NFR-REG-004: Security

**Requirement:** Secure data storage and access

**Targets:**
- Encrypted payloads (AES-256-GCM)
- TLS 1.3 for all communications
- JWT token validation (RS256, ES256)
- Role-based access control (RBAC)
- Audit logging for all mutations
- No sensitive data in logs or errors
- Protection against injection attacks

**Validation:**
- Security audit and penetration testing
- Compliance with OWASP Top 10
- Regular dependency vulnerability scans

### NFR-REG-005: Observability

**Requirement:** Comprehensive monitoring and diagnostics

**Targets:**
- Structured logging with Serilog
- Distributed tracing with OpenTelemetry
- Metrics collection (Prometheus format)
- Health and readiness endpoints
- Correlation IDs for request tracking
- Alert policies for failures

**Validation:**
- Observability dashboard setup
- Alert response time tests
- Log aggregation verification

### NFR-REG-006: Testability

**Requirement:** Comprehensive test coverage

**Targets:**
- > 90% unit test coverage
- Integration tests for all APIs
- Performance benchmarks
- In-memory repository for fast tests
- Testcontainers for integration tests
- Automated CI/CD test execution

**Validation:**
- Code coverage reports
- Test execution time < 5 minutes
- All tests pass before merge

## Implementation Plan

### Phase 1: Foundation (Sprint 1-2)
**Goal:** Core domain models and storage abstraction

**Tasks:**
- REG-001: Setup project structure
- REG-002: Implement domain models and enums
- REG-003: Define storage repository interfaces
- REG-004: Implement in-memory repository for testing
- REG-005: Unit tests for models and repository abstraction

**Deliverables:**
- Sorcha.Register.Models project
- Sorcha.Register.Core project with interfaces
- Sorcha.Register.Storage.InMemory project
- 80%+ test coverage

### Phase 2: Core Business Logic (Sprint 3-4)
**Goal:** Register and transaction management

**Tasks:**
- REG-006: Implement RegisterManager
- REG-007: Implement TransactionManager
- REG-008: Implement DocketManager
- REG-009: Implement QueryManager
- REG-010: Implement ChainValidator
- REG-011: Unit tests for all managers

**Deliverables:**
- Complete business logic layer
- Chain validation logic
- 90%+ test coverage

### Phase 3: Storage Implementations (Sprint 5-6)
**Goal:** Production storage backends

**Tasks:**
- REG-012: Implement MongoDB repository
- REG-013: Configure MongoDB indexes
- REG-014: Implement PostgreSQL repository with EF Core
- REG-015: Database migration scripts
- REG-016: Integration tests with Testcontainers

**Deliverables:**
- Sorcha.Register.Storage.MongoDB project
- Sorcha.Register.Storage.PostgreSQL project
- Migration and seeding scripts

### Phase 4: Event System (Sprint 7)
**Goal:** Event-driven integration

**Tasks:**
- REG-017: Define event interfaces and models
- REG-018: Implement Aspire messaging event bus
- REG-019: Implement RabbitMQ event bus (optional)
- REG-020: Event subscriber implementations
- REG-021: Integration tests for events

**Deliverables:**
- Sorcha.Register.Events.Aspire project
- Event handler registration
- Idempotent event processing

### Phase 5: API Layer (Sprint 8-9)
**Goal:** REST API and real-time endpoints

**Tasks:**
- REG-022: Setup Sorcha.Register.Service project
- REG-023: Implement Minimal API endpoints
- REG-024: Configure OData for queries
- REG-025: Implement SignalR hub
- REG-026: API authentication and authorization
- REG-027: OpenAPI documentation
- REG-028: API integration tests

**Deliverables:**
- Complete REST API
- SignalR real-time hub
- OpenAPI specification

### Phase 6: Client Library (Sprint 10)
**Goal:** Client SDK for consuming services

**Tasks:**
- REG-029: Implement IRegisterServiceClient interface
- REG-030: Implement HTTP client with retry policies
- REG-031: Implement SignalR hub client
- REG-032: Client usage examples
- REG-033: Client integration tests

**Deliverables:**
- Sorcha.Register.Client project
- NuGet package
- Usage documentation

### Phase 7: Performance & Observability (Sprint 11)
**Goal:** Production-ready observability and performance

**Tasks:**
- REG-034: Configure OpenTelemetry
- REG-035: Setup structured logging
- REG-036: Implement health checks
- REG-037: Performance benchmarks with NBomber
- REG-038: Load testing and optimization

**Deliverables:**
- Observability dashboard
- Performance benchmark suite
- Optimization recommendations

### Phase 8: Multi-Tenant Authorization (Sprint 12)
**Goal:** Tenant isolation and access control

**Tasks:**
- REG-039: Implement tenant resolver
- REG-040: Integrate with Tenant Service
- REG-041: Role-based access control
- REG-042: Tenant filtering in queries
- REG-043: Authorization tests

**Deliverables:**
- Complete authorization layer
- Tenant isolation enforcement
- Access control documentation

### Phase 9: Integration & E2E Testing (Sprint 13)
**Goal:** End-to-end validation

**Tasks:**
- REG-044: Aspire orchestration configuration
- REG-045: Service-to-service integration tests
- REG-046: End-to-end workflow tests
- REG-047: Performance regression tests
- REG-048: Security audit

**Deliverables:**
- Complete integration test suite
- E2E test scenarios
- Security assessment report

### Phase 10: Documentation & Deployment (Sprint 14)
**Goal:** Production deployment readiness

**Tasks:**
- REG-049: API documentation and examples
- REG-050: Architecture documentation
- REG-051: Deployment guide (Aspire, Kubernetes)
- REG-052: Migration guide from Siccar
- REG-053: Operations runbook
- REG-054: Final review and approval

**Deliverables:**
- Complete documentation
- Deployment scripts
- Migration tools
- Production readiness checklist

## Success Criteria

### Functionality
- ✅ All register CRUD operations functional
- ✅ Transaction storage and querying operational
- ✅ Docket sealing and chain validation working
- ✅ Multi-tenant isolation enforced
- ✅ Real-time notifications functional
- ✅ Event publishing and subscription working
- ✅ All integration points tested

### Quality
- ✅ > 90% unit test coverage
- ✅ All integration tests passing
- ✅ Performance targets met (1000+ tx/s)
- ✅ Security audit passed
- ✅ Code review approved
- ✅ No critical vulnerabilities

### Documentation
- ✅ API documentation complete with examples
- ✅ Architecture diagrams created
- ✅ Deployment guide available
- ✅ Migration guide from Siccar
- ✅ Operations runbook complete

### Compatibility
- ✅ Integrates with Wallet Service
- ✅ Integrates with Validator Service (when available)
- ✅ Works with Aspire orchestration
- ✅ Supports multiple storage backends
- ✅ NuGet package published

## Risk Management

### Technical Risks

**Risk:** MongoDB performance at scale (10M+ transactions per register)
- **Likelihood:** Medium
- **Impact:** High
- **Mitigation:**
  - Implement sharding strategy early
  - Test with production-scale data
  - Design indexes for common queries
  - Consider time-series collections for transaction storage
  - Document query optimization patterns

**Risk:** SignalR connection limits and scalability
- **Likelihood:** Medium
- **Impact:** Medium
- **Mitigation:**
  - Implement Redis backplane for multi-instance scaling
  - Connection pooling and management
  - Graceful degradation if WebSocket unavailable
  - Load testing with 10,000+ concurrent connections

**Risk:** Event ordering guarantees across distributed services
- **Likelihood:** High
- **Impact:** High
- **Mitigation:**
  - Use Aspire message sequencing features
  - Implement idempotent event handlers
  - Include sequence numbers in events
  - Document eventual consistency expectations

**Risk:** Storage migration complexity (Siccar to Sorcha)
- **Likelihood:** High
- **Impact:** High
- **Mitigation:**
  - Create automated migration tools
  - Support incremental migration
  - Validate data integrity post-migration
  - Provide rollback procedures

**Risk:** gRPC compatibility across environments
- **Likelihood:** Low
- **Impact:** Medium
- **Mitigation:**
  - Provide REST fallback for all operations
  - Test in various network environments
  - Document firewall and proxy requirements

### Operational Risks

**Risk:** Breaking changes impact existing integrations
- **Likelihood:** Medium
- **Impact:** High
- **Mitigation:**
  - Maintain API versioning
  - Provide backward compatibility layer
  - Staged rollout with canary deployments
  - Comprehensive integration testing

**Risk:** Performance regression during migration
- **Likelihood:** Medium
- **Impact:** Medium
- **Mitigation:**
  - Baseline performance before migration
  - Continuous performance benchmarking
  - Phased rollout with monitoring
  - Rollback plan if targets not met

## Out of Scope

The following items are explicitly out of scope for this specification:

1. **Consensus Algorithms** - Handled by Validator Service
2. **Cryptographic Operations** - Delegated to Sorcha.Cryptography library
3. **Wallet Management** - Handled by Wallet Service
4. **Blueprint Execution** - Handled by Blueprint Engine
5. **Network/P2P Layer** - Handled by Peer Service
6. **Transaction Building** - Handled by clients via TransactionHandler
7. **Key Management** - Delegated to Wallet Service
8. **User/Tenant Management** - Handled by Tenant Service (future)
9. **API Gateway/Routing** - Infrastructure concern (handled by Sorcha.ApiGateway)
10. **Blockchain Pruning** - Future enhancement
11. **Smart Contracts** - Future enhancement
12. **Cross-Register Transactions** - Future enhancement

## References

### Internal Documentation
- [Sorcha Constitution](../constitution.md)
- [Sorcha Architecture](../../docs/architecture.md)
- [Wallet Service Specification](sorcha-wallet-service.md)
- [Transaction Handler Specification](sorcha-transaction-handler.md)
- [Cryptography Specification](sorcha-cryptography-rewrite.md)

### External Resources
- [MongoDB Best Practices](https://docs.mongodb.com/manual/administration/production-notes/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OData V4 Specification](https://www.odata.org/documentation/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [gRPC for .NET](https://grpc.io/docs/languages/csharp/)

### Import Source
- **Original Specification:** [Siccar Register Service](../archive/import-siccar-register-20251113/siccar-register-service.md)
- **Source Repository:** https://github.com/StuartF303/SICCARV3.git
- **Import Date:** 2025-11-13

## Version History

- **2.0** (2025-11-13) - Sorcha platform upgrade
  - Migrated from Siccar V3 specification
  - Updated for .NET Aspire orchestration
  - Aligned with Sorcha 4-layer architecture
  - Modernized for .NET 10 and C# 13
  - Added comprehensive integration points
  - Enhanced event system design
  - Expanded testing strategy

- **1.0** (2025-11-13) - Original Siccar specification
  - Initial analysis and requirements
  - Dapr-based architecture
  - MongoDB storage focus

---

**Document Status:** Proposed - Ready for Review
**Priority:** High (Core Platform Service)
**Assigned To:** To Be Determined
**Related Tasks:** See [.specify/tasks/REG-*.md]
