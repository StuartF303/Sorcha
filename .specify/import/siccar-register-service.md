# PreviousCodebase.RegisterService Library Specification

**Version:** 1.0
**Date:** 2025-11-13
**Status:** Proposed
**Related Constitution:** [constitution.md](../constitution.md)
**Related Specifications:**
- [previous-codebase-cryptography-rewrite.md](previous-codebase-cryptography-rewrite.md)
- [previous-codebase-wallet-service.md](previous-codebase-wallet-service.md)
- [previous-codebase-transaction-handler.md](previous-codebase-transaction-handler.md)

## Executive Summary

This specification defines the requirements for creating a standalone, reusable register management library named **PreviousCodebase.RegisterService**. This library will handle distributed ledger register creation, transaction storage, docket management, and data synchronization for the previous-codebase distributed ledger platform. The new implementation will be architected to be portable and importable into new system architectures while maintaining compatibility with existing infrastructure.

## Background

### Current State Analysis

The existing register service is implemented as a microservice with ASP.NET Core Web API with moderate coupling to infrastructure dependencies. The code resides in `/src/Services/Register/` and consists of five main projects.

#### Current Architecture

**Project Structure:**
- `RegisterService` - ASP.NET Core Web API (Controllers, Hubs, Configuration)
- `RegisterCore` - Core models, interfaces, and storage abstractions
- `RegisterCoreMongoDBStorage` - MongoDB repository implementation
- `RegisterTests` - Unit test suite
- `RegisterService.IntegrationTests` - Integration test suite

**Key Components:**

**Controllers:**
- **RegistersController** (`RegisterService/V1/Controllers/RegistersController.cs:36`) - Register CRUD operations
  - `GetAllRegisters()` - OData-enabled register listing with tenant filtering
  - `GetRegisterByIdAsync(registerId)` - Single register retrieval
  - `PostRegister(newRegister)` - Register creation with pub/sub event publishing
  - `DeleteRegister(registerId)` - Register deletion with cleanup
  - Max 25 registers per installation limit
  - JWT authorization with role-based access control
  - Dapr pub/sub integration for events

- **TransactionsController** (`RegisterService/V1/Controllers/TransactionsController.cs:30`) - Transaction management
  - `GetTransactions(registerId)` - OData query support for transactions
  - `GetTransaction(registerId, transactionId)` - Single transaction retrieval
  - `GetTransactionsByDocket(registerId, docketId)` - Docket-filtered transactions
  - `PostRemoteTransaction(validatedTransaction)` - Dapr topic subscriber for validated transactions
  - Subscribes to: `TransactionValidationCompleted`, `TransactionSubmitted` topics
  - Publishes to: `TransactionConfirmed` topic
  - SignalR hub integration for real-time updates

- **DocketController** (`RegisterService/V1/Controllers/DocketController.cs:32`) - Sealed transaction collections
  - `GetDockets(registerId)` - OData query on dockets
  - `GetDocket(registerId, docketId)` - Single docket retrieval
  - `PostDocket(registerId, candidateDocket)` - Manual docket submission
  - `ReceiveDocket(headDocket)` - Dapr topic subscriber for confirmed dockets
  - Subscribes to: `DocketConfirmed` topic
  - Updates register height on docket confirmation

- **AddressController** (`RegisterService/V1/Controllers/AddressController.cs`) - Wallet address registration
  - `PostLocalAddress(newAddress)` - Register wallet addresses
  - Subscribes to: `WalletAddressCreation` topic

**Business Logic Layer:**
- **RegisterResolver** (`RegisterService/V1/Services/RegisterResolver.cs:23`) - Authorization and filtering
  - `ResolveRegistersForUser(userClaims)` - Tenant-based register filtering
  - `ThrowIfUserNotAuthorizedForRegister(userClaims, registerId)` - Authorization enforcement
  - Integration with TenantService for multi-tenant access control

**Data Access Layer:**
- **RegisterRepository** (`RegisterCoreMongoDBStorage/RegisterRepository.cs:16`) - MongoDB persistence
  - Collection: `LocalRegisters` - Register metadata storage
  - Dynamic collections per register for transactions and dockets
  - Operations:
    - Register: CRUD, query, count, local register check
    - Docket: insert, retrieve, list by register
    - Transaction: insert, retrieve, list, query with LINQ expressions
  - Configured via `RegisterRepository:MongoDBServer` and `RegisterRepository:DatabaseName`

**Core Models:**
- **Register** (`RegisterCore/Models/Register.cs:14`) - Register entity
  - `Id` (guid without hyphens), `Name` (max 38 chars), `Height` (uint)
  - `Status` (OFFLINE, ONLINE, CHECKING, RECOVERY)
  - `Advertise` (bool) - Public network visibility
  - `IsFullReplica` (bool) - Full transaction storage flag
  - Virtual navigation: `Dockets`, `Transactions`

- **TransactionModel** (`RegisterCore/Models/TransactionModel.cs:13`) - Transaction structure
  - `TxId` (64 char hex) - Transaction hash/ID
  - `PrevTxId` (64 char hex) - Previous transaction link
  - `Version` (uint32), `TimeStamp` (UTC DateTime)
  - `SenderWallet` (Base58 address), `RecipientsWallets` (string array)
  - `MetaData` (TransactionMetaData) - Blueprint tracking
  - `Payloads` (PayloadModel array) - Encrypted data
  - `Signature` (cryptographic signature)
  - `PayloadCount` (uint64)

- **Docket** (`RegisterCore/Models/Docket.cs:14`) - Sealed transaction collection
  - `Id` (ulong) - Block height/number
  - `RegisterId` (foreign key)
  - `PreviousHash`, `Hash` - Chain of custody
  - `TransactionIds` (List<string>) - Sealed transaction IDs
  - `TimeStamp` (UTC DateTime)
  - `State` (Init, Proposed, Accepted, Rejected, Sealed)
  - `MetaData` (TransactionMetaData with type=Docket)

- **TransactionMetaData** (`RegisterCore/Models/TransactionMetaData.cs:13`) - Blueprint tracking
  - `RegisterId` (guid), `TransactionType` (enum)
  - `BlueprintId`, `InstanceId` (workflow tracking)
  - `ActionId`, `NextActionId` (blueprint step tracking)
  - `TrackingData` (SortedList<string,string>) - Custom metadata stored as JSON

- **PayloadModel** (`RegisterCore/Models/PayloadModel.cs:4`) - Encrypted payload
  - `WalletAccess` (string array) - Authorized wallets
  - `PayloadSize` (ulong), `Hash` (SHA-256)
  - `Data` (encrypted base64 string)
  - `PayloadFlags` (encryption metadata)
  - `IV` (Challenge) - Initialization vector
  - `Challenges` (Challenge array) - Per-wallet encryption keys

**Real-time Communication:**
- **RegistersHub** (`RegisterService/Hubs/RegistersHub.cs:10`) - SignalR hub
  - Endpoint: `/registershub`
  - Methods: `SubscribeRegister(registerId)`, `UnSubscribeRegister(registerId)`
  - JWT authentication required
  - Dynamic group-based subscriptions per register
  - Real-time transaction and docket notifications

**Configuration & Startup:**
- **Startup.cs** (`RegisterService/Startup.cs:37`) - Service configuration
  - Dependencies:
    - MongoDB (RegisterRepository with connection pooling)
    - Dapr Client (pub/sub and service invocation)
    - JWT Authentication (Bearer + custom DaprScheme)
    - TenantServiceClient (Dapr service-to-service)
    - SignalR (real-time notifications)
    - OData V4 (advanced querying)
    - Serilog (structured logging)
    - Application Insights (telemetry)
  - OData Model: Registers, Transactions, Dockets with filter/expand/select/orderby
  - Health checks: `/api/registers/healthz`

**Infrastructure:**
- **Kubernetes Deployment** (`deploy/k8s/deployment-microservice-register.yaml`)
  - Dapr sidecar enabled (app-id: `register-service`, port: 80)
  - Health check integration
  - Environment variables for MongoDB connection
  - Resource limits: 128Mi memory, 500m CPU
  - Ingress and Service YAML files for network exposure

**NuGet Dependencies:**
- Microsoft.AspNetCore.OData (9.0.0) - Advanced query support
- Dapr.AspNetCore (1.14.0) - Event-driven architecture
- MongoDB.Driver (via RegisterCoreMongoDBStorage) - Data persistence
- Serilog.AspNetCore (8.0.3) - Structured logging
- Microsoft.AspNetCore.SignalR - Real-time updates
- Microsoft.ApplicationInsights.* - Observability
- Swashbuckle.AspNetCore (6.9.0) - API documentation

#### Problems Identified

1. **Moderate Infrastructure Coupling**
   - ASP.NET Core pipeline required for HTTP endpoints
   - Dapr integration embedded in controllers
   - MongoDB required for persistence (no storage abstraction in Core)
   - SignalR tightly coupled to HTTP context
   - Cannot easily reuse register logic outside web context

2. **Repository Pattern Incomplete**
   - `IRegisterRepository` interface exists in RegisterCore
   - Only MongoDB implementation provided
   - No in-memory or alternative storage implementations
   - Repository interface in Core but implementation in separate project
   - Difficult to test without MongoDB instance

3. **Multi-Tenant Authorization Complexity**
   - Tenant filtering requires external TenantService call
   - Authorization logic mixed in controllers and RegisterResolver
   - No clear separation of authentication vs authorization
   - Dapr service-to-service auth using custom DaprScheme
   - JWT claims parsing scattered across controllers

4. **Event-Driven Architecture Dependencies**
   - Hard dependency on Dapr for pub/sub
   - Topic names defined in external `Topics` class
   - No abstraction for event publishing
   - Difficult to test event-driven flows
   - Cannot use alternative message brokers without Dapr

5. **Limited Query Capabilities**
   - OData support at controller level only
   - No rich domain queries in repository
   - LINQ expressions supported but limited documentation
   - `QueryTransactions` and `QueryTransactionPayload` methods exist but underutilized
   - Pagination handled by OData but not in repository layer

6. **Transaction and Docket Management**
   - Register height updated manually in controller code (temporary implementation)
   - No atomic operations for docket insertion + height update
   - Duplicate transaction tracking between Register and Wallet services
   - No clear lifecycle management for transaction states
   - Pending transaction logic exists but not fully implemented

7. **Storage Scalability Concerns**
   - Dynamic collection per register (good design)
   - No sharding or partitioning strategy documented
   - `IsLocalRegisterAsync` uses in-memory cache (localRegisters list)
   - Full replica vs partial replica flag exists but not enforced
   - No pruning strategy for old transactions

8. **Real-time Notification Limitations**
   - SignalR hub requires WebSocket support
   - No fallback for HTTP polling
   - Group management based on registerId
   - No message queuing if client disconnected
   - Hub context adapter exists but minimal functionality

9. **Security Considerations**
   - Private key handling delegated to WalletService
   - Transaction signatures validated elsewhere
   - No rate limiting on register creation (25 max hardcoded)
   - Register deletion allows full data removal
   - JWT validation configured but HTTPS disabled in development

10. **Testing and Portability**
    - Integration tests require full stack (MongoDB, Dapr)
    - No in-memory repository for unit testing
    - Controller tests difficult without HTTP context
    - SignalR hub tests require connection mocking
    - Dapr tests require sidecar or mocking

### Goals

1. **Separation of Concerns** - Register logic independent of HTTP/REST
2. **Portable Architecture** - Usable in any .NET application context
3. **Pluggable Storage** - Multiple storage backend support with clear abstraction
4. **Event System Abstraction** - Decouple from Dapr with abstract event bus
5. **Enhanced Query Support** - Rich domain queries at repository level
6. **Multi-Tenant Isolation** - Clear tenant boundary enforcement
7. **Comprehensive Testing** - >90% coverage with unit and integration tests
8. **Transaction Lifecycle** - Clear state management and atomic operations
9. **Scalability Support** - Sharding, partitioning, and replication strategies
10. **Well-Documented** - Clear API with usage examples and patterns

## Scope

### In Scope

#### Core Register Management

1. **Register Creation & Configuration**
   - Create new registers with unique IDs (guid format)
   - Configure register properties (name, advertise, full replica)
   - Initialize register storage (transactions and dockets collections)
   - Set initial register status (OFFLINE, ONLINE, CHECKING, RECOVERY)
   - Validate register limits per tenant/installation
   - Generate `RegisterCreated` events

2. **Register Lifecycle**
   - Update register metadata (name, status, configuration)
   - Track register height (current block number)
   - Query registers by ID, name, or custom predicates
   - List registers with tenant filtering
   - Archive/delete registers with cleanup
   - Generate `RegisterDeleted` events

3. **Multi-Tenant Support**
   - Tenant-based register isolation
   - Register ownership and access control
   - Tenant-scoped queries and operations
   - Integration with tenant service for authorization
   - Support for installation-wide admin access

#### Transaction Management

1. **Transaction Storage**
   - Insert validated transactions into register collections
   - Retrieve transactions by ID or query criteria
   - List transactions with pagination and filtering
   - Query transactions by blueprint ID, instance ID, or metadata
   - Support for LINQ expression queries
   - OData-compatible query interfaces

2. **Transaction Validation**
   - Verify transaction format and structure
   - Validate transaction signatures (delegated to crypto library)
   - Check transaction chain integrity (prevTxId links)
   - Validate sender and recipient wallet addresses
   - Ensure register exists and is accepting transactions

3. **Transaction Lifecycle**
   - Receive transactions from validation service
   - Store transaction with metadata and payloads
   - Track transaction state (pending, confirmed, sealed)
   - Update wallet service with confirmed transactions
   - Generate `TransactionConfirmed` events
   - Support transaction history queries

4. **Payload Management**
   - Store encrypted transaction payloads
   - Track payload access control (WalletAccess list)
   - Support payload decryption (delegated to wallet service)
   - Query payloads by hash or content
   - Payload size tracking and limits

#### Docket Management

1. **Docket Creation & Sealing**
   - Receive proposed dockets from validator service
   - Validate docket structure and hashes
   - Store dockets with transaction references
   - Update docket state (Init → Proposed → Accepted → Sealed)
   - Maintain chain of custody (previousHash → hash links)
   - Increment register height atomically

2. **Docket Queries**
   - List dockets for a register
   - Retrieve docket by height (ID)
   - Query dockets by state or time range
   - Get transactions for a specific docket
   - Support pagination and filtering

3. **Consensus Integration**
   - Receive confirmed dockets from validator
   - Apply consensus-approved dockets
   - Update register state after docket confirmation
   - Notify participants of docket sealing
   - Generate `DocketConfirmed` events

#### Storage Abstraction

1. **Repository Pattern**
   - `IRegisterRepository` interface defining all operations
   - Register CRUD operations
   - Transaction storage and queries
   - Docket storage and queries
   - Support for atomic transactions
   - Query builders and LINQ support

2. **Storage Implementations**
   - **MongoDB Provider** (current implementation)
     - Collection-per-register design
     - Document storage for registers, transactions, dockets
     - Index optimization for queries
     - Connection pooling and retry logic
   - **In-Memory Provider** (for testing)
     - Dictionary-based storage
     - LINQ query support
     - Thread-safe operations
   - **SQL Provider** (future)
     - Entity Framework Core
     - Relational schema design
     - Table-per-register or sharded tables
   - **Document DB Provider** (future)
     - CosmosDB, DynamoDB support
     - Partition key strategies
     - Global distribution support

3. **Migration and Compatibility**
   - Import from existing MongoDB collections
   - Export to portable formats
   - Schema versioning
   - Backward compatibility with v3 format

#### Event System

1. **Event Definitions**
   - `RegisterCreated` - New register initialized
   - `RegisterDeleted` - Register removed
   - `TransactionReceived` - Transaction stored
   - `TransactionConfirmed` - Transaction validated and sealed
   - `DocketProposed` - New docket candidate
   - `DocketConfirmed` - Docket sealed by consensus
   - `RegisterHeightUpdated` - Block height incremented

2. **Event Bus Abstraction**
   - `IEventPublisher` interface for publishing
   - `IEventSubscriber` interface for consuming
   - Event handler registration and dispatch
   - Implementations: Dapr, RabbitMQ, Azure Service Bus, in-memory
   - Retry policies and dead-letter queues
   - Event versioning support

3. **Real-time Notifications**
   - Abstract notification system (not tied to SignalR)
   - Support for WebSocket, Server-Sent Events, polling
   - Group-based subscriptions (per register)
   - Notification filtering and routing
   - Connection lifecycle management

#### Query and Analytics

1. **Advanced Querying**
   - OData V4 query support ($filter, $select, $expand, $orderby, $top, $skip)
   - LINQ expression queries at repository level
   - Full-text search on transaction metadata
   - Time-range queries for transactions and dockets
   - Blueprint instance tracking queries
   - Wallet-based transaction queries

2. **Aggregations and Statistics**
   - Transaction count per register
   - Docket height and chain integrity checks
   - Payload size aggregations
   - Transaction rate metrics
   - Storage utilization tracking

3. **Chain Validation**
   - Verify transaction chain integrity (prevTxId links)
   - Verify docket chain integrity (previousHash links)
   - Detect missing or orphaned transactions
   - Validate register height consistency
   - Generate integrity reports

#### Security and Access Control

1. **Authentication Integration**
   - JWT token validation
   - Service-to-service authentication (Dapr, mutual TLS)
   - API key support for client SDKs
   - Role-based access control (RBAC)

2. **Authorization**
   - Tenant-based isolation
   - Role-based permissions (RegisterCreator, RegisterMaintainer, RegisterReader)
   - Installation-level admin access
   - Delegate access control (via wallet service integration)

3. **Audit Logging**
   - Log all register mutations
   - Track transaction submissions
   - Record docket confirmations
   - Log access control changes
   - Structured logging with correlation IDs

4. **Data Protection**
   - Encrypted payloads (AES-256-GCM)
   - Wallet-based access control on payloads
   - At-rest encryption for storage backends
   - In-transit encryption (TLS)

#### API and Client Libraries

1. **Core Service Interfaces**
   - `IRegisterService` - High-level register operations
   - `ITransactionService` - Transaction management
   - `IDocketService` - Docket operations
   - `IQueryService` - Advanced querying
   - Dependency injection support

2. **HTTP Client Library**
   - REST API client for backward compatibility
   - OData query builder
   - SignalR hub client
   - Async/await pattern
   - Retry policies and circuit breakers

3. **gRPC Support** (future)
   - High-performance service-to-service calls
   - Streaming support for large queries
   - Protocol buffer definitions
   - Bi-directional streaming for real-time updates

### Out of Scope

1. **Consensus Algorithms** - Handled by ValidatorService
2. **Cryptographic Operations** - Delegated to crypto library and WalletService
3. **Wallet Management** - Handled by WalletService
4. **Blueprint Execution** - Handled by workflow orchestration service
5. **Network/P2P Layer** - Handled by PeerService
6. **Transaction Building** - Handled by clients via TransactionHandler
7. **Key Management** - Delegated to WalletService
8. **User/Tenant Management** - Handled by TenantService
9. **API Gateway/Routing** - Infrastructure concern
10. **Blockchain Pruning** - Future enhancement

## Architecture

### High-Level Design

```
┌─────────────────────────────────────────────────────────────┐
|                    PreviousCodebase.RegisterService                    |
│                      (Core Library)                          │
├─────────────────────────────────────────────────────────────┤
│  Register Management  │  Transaction Management             │
│  - Create/Update/Delete│  - Store Transactions              │
│  - Query/List          │  - Query Transactions              │
│  - Status Management   │  - Payload Management              │
│                        │                                     │
│  Docket Management     │  Multi-Tenant Support              │
│  - Seal Transactions   │  - Tenant Isolation                │
│  - Chain Validation    │  - Access Control                  │
│  - Height Tracking     │  - Authorization                   │
├─────────────────────────────────────────────────────────────┤
│              Storage Abstraction Layer                       │
│  IRegisterRepository  │  IEventPublisher/Subscriber         │
│  - MongoDB            │  - Dapr Pub/Sub                     │
│  - InMemory           │  - RabbitMQ                         │
│  - SQL (future)       │  - Azure Service Bus                │
│  - DocumentDB (future)│  - In-Memory (testing)              │
└─────────────────────────────────────────────────────────────┘
           │                        │                 │
           ▼                        ▼                 ▼
    ┌──────────┐         ┌──────────────┐   ┌──────────────┐
    │ MongoDB  │         │ ValidatorSvc │   │ WalletService│
    └──────────┘         └──────────────┘   └──────────────┘
```

### Project Structure

```
src/
├── PreviousCodebase.RegisterService/              # Core library
│   ├── Services/
│   │   ├── RegisterService.cs          # Register operations
│   │   ├── TransactionService.cs       # Transaction operations
│   │   ├── DocketService.cs            # Docket operations
│   │   └── QueryService.cs             # Advanced queries
│   ├── Storage/
│   │   ├── IRegisterRepository.cs      # Storage abstraction
│   │   ├── ITransactionRepository.cs   # (Optional split)
│   │   └── IDocketRepository.cs        # (Optional split)
│   ├── Events/
│   │   ├── IEventPublisher.cs          # Event publishing
│   │   ├── IEventSubscriber.cs         # Event consumption
│   │   └── RegisterEvents.cs           # Event definitions
│   ├── Models/
│   │   ├── Register.cs                 # Register entity
│   │   ├── TransactionModel.cs         # Transaction entity
│   │   ├── Docket.cs                   # Docket entity
│   │   ├── TransactionMetaData.cs      # Metadata
│   │   └── PayloadModel.cs             # Payload structure
│   ├── Authorization/
│   │   ├── IAuthorizationService.cs    # Authorization abstraction
│   │   └── TenantAccessPolicy.cs       # Tenant policies
│   └── Exceptions/
│       ├── RegisterException.cs        # Base exception
│       └── RegisterNotFoundException.cs
│
├── PreviousCodebase.RegisterService.Storage.MongoDB/   # MongoDB implementation
│   ├── RegisterRepository.cs
│   └── MongoDBConfiguration.cs
│
├── PreviousCodebase.RegisterService.Storage.InMemory/  # In-memory implementation
│   └── InMemoryRegisterRepository.cs
│
├── PreviousCodebase.RegisterService.Events.Dapr/       # Dapr event bus
│   ├── DaprEventPublisher.cs
│   └── DaprEventSubscriber.cs
│
├── PreviousCodebase.RegisterService.Events.InMemory/   # In-memory events
│   └── InMemoryEventBus.cs
│
├── PreviousCodebase.RegisterService.API/               # ASP.NET Core API
│   ├── Controllers/
│   │   ├── RegistersController.cs
│   │   ├── TransactionsController.cs
│   │   └── DocketsController.cs
│   ├── Hubs/
│   │   └── RegistersHub.cs             # SignalR hub
│   ├── Configuration/
│   │   └── ODataConfiguration.cs
│   └── Startup.cs
│
├── PreviousCodebase.RegisterService.Client/            # Client library
│   ├── RegisterServiceClient.cs        # HTTP client
│   └── SignalRHubClient.cs             # Real-time client
│
└── PreviousCodebase.RegisterService.Tests/             # Test projects
    ├── Unit/
    ├── Integration/
    └── Performance/
```

### Data Models

**Register**
```csharp
public class Register
{
    public string Id { get; set; }              // Guid without hyphens
    public string Name { get; set; }            // Max 38 characters
    public uint Height { get; set; }            // Current block height
    public string Votes { get; set; }           // Consensus votes (TBD)
    public bool Advertise { get; set; }         // Network visibility
    public bool IsFullReplica { get; set; }     // Full vs partial storage
    public RegisterStatusTypes Status { get; set; }
    public string TenantId { get; set; }        // Multi-tenant isolation
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**TransactionModel**
```csharp
public class TransactionModel
{
    public string TxId { get; set; }            // 64 char hex hash
    public string PrevTxId { get; set; }        // Previous transaction
    public uint Version { get; set; }           // Transaction version
    public string SenderWallet { get; set; }    // Base58 address
    public IEnumerable<string> RecipientsWallets { get; set; }
    public DateTime TimeStamp { get; set; }     // UTC
    public TransactionMetaData MetaData { get; set; }
    public ulong PayloadCount { get; set; }
    public PayloadModel[] Payloads { get; set; }
    public string Signature { get; set; }       // Cryptographic signature
}
```

**Docket**
```csharp
public class Docket
{
    public ulong Id { get; set; }               // Block height
    public string RegisterId { get; set; }      // Foreign key
    public string PreviousHash { get; set; }    // Chain link
    public string Hash { get; set; }            // Block hash
    public List<string> TransactionIds { get; set; }
    public DateTime TimeStamp { get; set; }     // UTC
    public DocketState State { get; set; }
    public TransactionMetaData MetaData { get; set; }
    public string Votes { get; set; }           // Consensus votes
}
```

### Storage Interface

```csharp
public interface IRegisterRepository
{
    // Register operations
    Task<bool> IsLocalRegisterAsync(string registerId);
    Task<IEnumerable<Register>> GetRegistersAsync();
    Task<IEnumerable<Register>> QueryRegisters(Func<Register, bool> predicate);
    Task<Register> GetRegisterAsync(string registerId);
    Task<Register> InsertRegisterAsync(Register newRegister);
    Task<Register> UpdateRegisterAsync(Register register);
    Task DeleteRegisterAsync(string registerId);
    Task<int> CountRegisters();

    // Docket operations
    Task<IEnumerable<Docket>> GetDocketsAsync(string registerId);
    Task<Docket> GetDocketAsync(string registerId, ulong docketId);
    Task<Docket> InsertDocketAsync(Docket docket);

    // Transaction operations
    Task<IQueryable<TransactionModel>> GetTransactionsAsync(string registerId);
    Task<TransactionModel> GetTransactionAsync(string registerId, string transactionId);
    Task<TransactionModel> InsertTransactionAsync(TransactionModel transaction);
    Task<IEnumerable<TransactionModel>> QueryTransactions(
        string registerId,
        Expression<Func<TransactionModel, bool>> predicate);
    Task<IEnumerable<TransactionModel>> QueryTransactionPayload(
        string registerId,
        Expression<Func<TransactionModel, bool>> predicate);
}
```

### Event System

```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(string topic, TEvent eventData)
        where TEvent : class;
}

public interface IEventSubscriber
{
    Task SubscribeAsync<TEvent>(string topic,
        Func<TEvent, Task> handler)
        where TEvent : class;
}

// Event Definitions
public class RegisterCreated
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransactionConfirmed
{
    public string TransactionId { get; set; }
    public List<string> ToWallets { get; set; }
    public string Sender { get; set; }
    public string PreviousTransactionId { get; set; }
    public TransactionMetaData MetaData { get; set; }
}

public class DocketConfirmed
{
    public string RegisterId { get; set; }
    public ulong DocketId { get; set; }
    public List<string> TransactionIds { get; set; }
    public DateTime TimeStamp { get; set; }
}
```

## Integration Points

### Service Dependencies

1. **TenantService**
   - Authorization and access control
   - Tenant-to-register mapping
   - Multi-tenant isolation enforcement
   - Dapr service-to-service invocation

2. **ValidatorService**
   - Transaction validation completion events
   - Docket confirmation events
   - Consensus voting results
   - Transaction submission notifications

3. **WalletService**
   - Transaction confirmation notifications
   - Wallet address registration
   - Payload decryption requests
   - Signature verification

4. **PeerService** (future)
   - Register synchronization
   - Distributed consensus
   - Network topology management

### Pub/Sub Topics

**Subscribed Topics:**
- `OnTransaction_ValidationCompleted` - Receive validated transactions
- `OnTransaction_Submitted` - Receive submitted transactions
- `OnDocket_Confirmed` - Receive confirmed dockets
- `OnWallet_AddressCreated` - Track wallet addresses

**Published Topics:**
- `OnTransaction_Confirmed` - Notify transaction confirmation
- `OnRegister_Created` - Notify register creation
- `OnRegister_Deleted` - Notify register deletion

### Infrastructure Dependencies

1. **MongoDB**
   - Connection string via configuration
   - Database name: `RegisterService`
   - Collections: `LocalRegisters` + per-register collections
   - Connection pooling and retry logic

2. **Dapr**
   - Pub/Sub component: `pubsub`
   - State store: `statestore` (optional)
   - Service invocation for inter-service calls
   - Health checks and sidecars

3. **SignalR** (API layer)
   - Real-time notifications
   - WebSocket transport
   - Hub endpoint: `/registershub`

4. **Authentication**
   - JWT Bearer tokens
   - Authority: `TenantIssuer` config
   - Audience: `PreviousCodebaseAudience` config
   - Custom Dapr authentication scheme

## Implementation Tasks

See individual task files in `.specify/tasks/` directory:

**Setup Tasks:**
- REG-001: Setup RegisterService core library project
- REG-002: Implement core models and enums
- REG-003: Define storage abstractions

**Core Implementation:**
- REG-004: Implement RegisterService business logic
- REG-005: Implement TransactionService business logic
- REG-006: Implement DocketService business logic
- REG-007: Implement QueryService for advanced queries

**Storage Layer:**
- REG-008: Implement MongoDB repository
- REG-009: Implement in-memory repository
- REG-010: Implement storage configuration

**Event System:**
- REG-011: Implement event abstractions
- REG-012: Implement Dapr event publisher/subscriber
- REG-013: Implement in-memory event bus

**Authorization:**
- REG-014: Implement authorization service
- REG-015: Implement tenant access policies

**API Layer:**
- REG-016: Implement ASP.NET Core controllers
- REG-017: Implement SignalR hub
- REG-018: Configure OData support
- REG-019: Implement API middleware

**Client Library:**
- REG-020: Implement HTTP client library
- REG-021: Implement SignalR client

**Testing:**
- REG-022: Unit tests for register operations
- REG-023: Unit tests for transaction operations
- REG-024: Unit tests for docket operations
- REG-025: Integration tests with MongoDB
- REG-026: Integration tests with Dapr
- REG-027: Integration tests end-to-end
- REG-028: Performance benchmarks

**Documentation:**
- REG-029: API documentation and examples
- REG-030: Migration guide from v3
- REG-031: Deployment guide

## Success Criteria

1. **Functionality**
   - All register CRUD operations working
   - Transaction storage and retrieval operational
   - Docket sealing and chain validation working
   - Multi-tenant isolation enforced
   - Event publishing and subscription functional

2. **Quality**
   - >90% unit test coverage
   - All integration tests passing
   - Performance benchmarks meet SLAs
   - Security audit passed
   - Code review approved

3. **Documentation**
   - API documentation complete
   - Usage examples provided
   - Migration guide available
   - Architecture diagrams updated

4. **Compatibility**
   - Backward compatible with existing API
   - Imports from v3 MongoDB schema
   - Dapr integration maintained
   - Existing clients continue to work

## Non-Functional Requirements

### Performance
- Transaction insert: <100ms (p99)
- Transaction query: <50ms (p99)
- Register list: <200ms (p99)
- Support 1000+ transactions/second per register
- Support 10,000+ concurrent SignalR connections

### Scalability
- Support 10,000+ registers per installation
- Support 1,000,000+ transactions per register
- Horizontal scaling via MongoDB sharding
- Read replicas for query scaling

### Reliability
- 99.9% uptime SLA
- Zero data loss guarantee
- Automatic retry with exponential backoff
- Circuit breaker for external dependencies
- Health checks and readiness probes

### Security
- Encrypted payloads (AES-256-GCM)
- TLS 1.3 for all communications
- JWT token validation
- Role-based access control
- Audit logging for all mutations
- No sensitive data in logs

### Observability
- Structured logging (Serilog)
- Distributed tracing (OpenTelemetry)
- Metrics (Prometheus/Application Insights)
- Health checks and liveness probes
- Alert policies defined

## Risk Management

### Technical Risks
1. **MongoDB Performance at Scale**
   - Mitigation: Implement sharding strategy, test with production-like data
2. **SignalR Connection Limits**
   - Mitigation: Implement backplane (Redis), connection pooling
3. **Event Ordering Guarantees**
   - Mitigation: Use Dapr message sequencing, idempotent handlers

### Migration Risks
1. **Breaking Changes to Existing Clients**
   - Mitigation: Maintain backward compatibility layer, versioned API
2. **Data Migration Complexity**
   - Mitigation: Incremental migration, validation tooling
3. **Performance Regression**
   - Mitigation: Benchmark before/after, staged rollout

## References

- [MongoDB Best Practices](https://docs.mongodb.com/manual/administration/production-notes/)
- [Dapr Pub/Sub](https://docs.dapr.io/developing-applications/building-blocks/pubsub/)
- [OData V4 Specification](https://www.odata.org/documentation/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [.NET Dependency Injection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

## Version History

- **1.0** (2025-11-13) - Initial specification based on current RegisterService analysis
