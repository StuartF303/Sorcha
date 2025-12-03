# SiccaV3 Validator Service - Comprehensive Analysis

## Executive Summary

The SiccaV3 Validator Service is a .NET 9.0-based microservice responsible for:
- Receiving pending transactions from the peer network
- Validating and grouping transactions into Dockets (blocks)
- Managing the blockchain consensus mechanism
- Communicating with the Register Service to persist validated data
- Implementing genesis block creation for new registers

The architecture follows a clean separation of concerns with dedicated layers for validation, business logic, and HTTP APIs. It uses Dapr for distributed system patterns and SignalR for real-time updates.

---

## 1. Overall Architecture and Structure

### Project Organization

The Validator Service is organized into four main projects:

```
/src/Services/Validator/
├── ValidatorService/          # Main ASP.NET Core service
├── ValidationEngine/          # Core business logic for validation & docket building
├── ValidatorCore/            # Interfaces and contracts
└── ValidatorTests/           # Unit tests
```

### Key Characteristics
- **Framework**: .NET 9.0 with ASP.NET Core
- **Architecture Pattern**: Layered architecture with clear separation of concerns
- **Total Code**: ~788 lines of C# across all validator modules
- **Dependencies**: Dapr (distributed patterns), JWT authentication, Serilog logging

### Technology Stack
- **Messaging**: Dapr Pub/Sub (configurable backend - RabbitMQ, Redis, etc.)
- **State Management**: Dapr State Store
- **Authentication**: JWT Bearer tokens + Dapr secret authentication
- **Logging**: Serilog with Application Insights integration
- **HTTP Client**: Dapr-based HTTP invocation between services

---

## 2. Core Responsibilities and Functionality

### Primary Responsibilities

1. **Transaction Reception**: Receives pending transactions via Dapr pub/sub from PeerService
2. **Memory Pool Management**: Maintains in-memory pool of transactions per register
3. **Docket Generation**: Groups validated transactions into Dockets (blocks)
4. **Genesis Block Creation**: Creates initial genesis dockets for new registers
5. **Consensus Participation**: Coordinates with consensus mechanism to finalize dockets
6. **Register Integration**: Communicates with RegisterService for docket persistence

### Processing Workflow

```
Transaction Flow:
Peer Network 
    → PeerService 
    → [Topic: OnTransaction_Pending]
    → ValidatorService (MemPool)
    → RulesBasedValidator (10s cycle)
    → DocketBuilder
    → [Topic: OnDocket_Confirmed]
    → RegisterService
    → SingularConsensus (consensus finalization)
    → [Topic: OnTransaction_ValidationCompleted]
    → Wallet Services & Clients
```

---

## 3. How It Builds Dockets

### Docket Building Process

**Class**: `DocketBuilder` (ValidationEngine/DocketBuilder.cs)

#### GenerateDocket() Method
```csharp
public Docket GenerateDocket(Docket head)
{
    head.Votes = ComputeSha256Hash(head.Id.ToString());
    head.Hash = ComputeSha256Hash(
        head.Id.ToString() 
        + head.PreviousHash 
        + head.RegisterId 
        + head.TimeStamp 
        + String.Join("", head.TransactionIds)
    );
    return head;
}
```

**Key Properties of a Docket:**
- `Id`: Block height (ulong) - incremental counter starting from 0
- `RegisterId`: Which register this docket belongs to (GUID)
- `Hash`: SHA256 hash of the docket (chain of custody)
- `PreviousHash`: Hash of the previous docket (maintains chain integrity)
- `TransactionIds`: List of transaction hashes included in this docket
- `Votes`: Currently stores hash of the docket ID (placeholder for future voting)
- `TimeStamp`: UTC timestamp of docket creation
- `State`: DocketState enum (Init, Proposed, Accepted, Rejected, Sealed)
- `MetaData`: TransactionMetaData with type = Docket

#### Docket States
- **Init**: Initial state
- **Proposed**: Presented to network for acceptance
- **Accepted**: Network has accepted the docket
- **Rejected**: Network rejected the docket
- **Sealed**: Docket is finalized and immutable

#### Hashing Strategy
- Uses SHA256 for all hash computations
- Hash includes: ID, PreviousHash, RegisterId, TimeStamp, all TransactionIds
- This ensures any modification invalidates the entire chain from that point forward

### Normal Docket Creation (RulesBasedValidator)

In the validation cycle (ProcessValidation method):

1. **Get Register List**: Queries RegisterService for all active registers
2. **Check Height**: If register height < 1, triggers genesis block creation
3. **Retrieve MemPool**: Gets pending transactions for the register
4. **Build Docket** if transactions exist:
   - Creates Docket object with:
     - Id = Height (next block number)
     - RegisterId = current register ID
     - PreviousHash = always "0000000000000000000000000000000000000000000000000000000000000000" (placeholder)
     - TransactionIds = extracted from MemPool
     - TimeStamp = DateTime.UtcNow
     - State = DocketState.Proposed
5. **Generate Hash**: Calls DocketBuilder.GenerateDocket()
6. **Publish**: Sends to RegisterService via Dapr pub/sub on `OnDocket_Confirmed` topic
7. **Clear MemPool**: Removes processed transactions after publishing

### Genesis Block Creation (Genesys)

**Class**: `Genesys` (ValidationEngine/Genesys.cs)

Genesis block is created when a register's height is 0:

```csharp
public async Task ProcessGenesys(string RegisterId)
{
    var docket = Builder.GenerateDocket(new Docket()
    {
        Id = 0,
        RegisterId = RegisterId,
        PreviousHash = "0000000000000000000000000000000000000000000000000000000000000000",
        TransactionIds = new List<string>(),  // Empty!
        TimeStamp = DateTime.UtcNow,
        State = DocketState.Sealed  // Note: Sealed, not Proposed
    });
    
    await Client.PublishEventAsync(PubSubName, Topics.DocketConfirmedTopicName, docket);
}
```

**Key Differences**:
- Height is 0 (genesis)
- No transactions included
- State is immediately Sealed (not Proposed)
- Triggers register height increment in RegisterService

---

## 4. How It Validates Dockets

### Current Validation Implementation

**Status**: The validator service has **minimal validation logic**. It's primarily a **docket builder and grouper**, not a comprehensive validator.

### Implicit Validations Performed

1. **MemPool Receipt** (`ValidatorController.ReceiveTx`):
   - Receives transactions from Dapr pub/sub
   - Validates they contain RegisterId metadata
   - Stores in thread-safe MemPool by RegisterId

2. **Register Existence Check**:
   - RegisterServiceClient.GetRegisters() retrieves active registers
   - Only processes dockets for registers that exist

3. **Height Consistency**:
   - Checks previous docket exists at Height - 1
   - Ensures linear progression of blocks

### Missing Validation Logic

The validator service **does NOT implement**:
- Transaction signature verification
- Transaction payload validation
- Double-spend detection
- Transaction state machine validation
- Docket size/transaction count limits
- Priority fee calculations

**Design Note**: This suggests validation happens elsewhere (possibly in Blueprint Service or Action Service) or is deferred to consensus stage.

### Transaction Model Structure

```csharp
public class TransactionModel
{
    public string TxId { get; set; }           // Hash ID (64 hex chars)
    public string PrevTxId { get; set; }       // Link to previous tx
    public UInt32 Version { get; set; }
    public string SenderWallet { get; set; }   // Base58 wallet address
    public IEnumerable<string> RecipientsWallets { get; set; }
    public DateTime TimeStamp { get; set; }
    public TransactionMetaData MetaData { get; set; }
    public UInt64 PayloadCount { get; set; }
    public PayloadModel[] Payloads { get; set; }
    public string Signature { get; set; }      // Cryptographic signature
}
```

---

## 5. Genesis Block Handling

### Genesis Block Lifecycle

1. **Trigger**: When RulesBasedValidator detects register with Height < 1
2. **Creation**: Genesys.ProcessGenesys() is called
3. **Properties**:
   - Height = 0
   - Empty transaction list
   - State = Sealed (immediately final)
   - No voting/consensus required
4. **Publication**: Published to RegisterService via Dapr
5. **Result**: Register height incremented to 1 in database

### Register Height Management

```csharp
// In RulesBasedValidator.ProcessValidation()
if (procReg.Height < 1)
{
    await GenesysBuilder.ProcessGenesys(procReg.Id);
    Registers.Where(r => r.Id == procReg.Id).First().Height = 1;
    break;  // Only one genesis per cycle
}
```

### Register Model Structure

```csharp
public class Register
{
    public string Id { get; set; }              // GUID (unique register identifier)
    public string Name { get; set; }            // Human-readable name
    public uint Height { get; set; }            // Current block height
    public string Votes { get; set; }           // Voting information (TBD)
    public IEnumerable<Docket> Dockets { get; set; }
    public IEnumerable<TransactionModel> Transactions { get; set; }
    public bool Advertise { get; set; }         // Network broadcast flag
    public bool IsFullReplica { get; set; }     // Full transaction storage
    public RegisterStatusTypes Status { get; set; }  // OFFLINE, ONLINE, CHECKING, RECOVERY
}
```

---

## 6. Consensus Mechanism Implementation

### Current Implementation

**Class**: `SingularConsensus` (ValidationEngine/SingularConsensus.cs)

The consensus mechanism is **currently a stub/placeholder**:

```csharp
public class SingularConsensus : ISiccarConsensus
{
    public bool NetworkMaster => throw new NotImplementedException();
    
    private async void ProcessConsent(object? state)
    {
        await Task.Run(() => {});  // Does nothing!
    }
}
```

### Consensus Cycle

- **Start Delay**: Waits (CycleTime + CycleTime/2) before first run
- **Frequency**: Runs every CycleTime seconds (default 10s)
- **Action**: Currently empty - awaits a Task.Run with no operation

### Intended Design (Based on Code Structure)

The consensus mechanism is designed to:
1. Monitor for Proposed dockets
2. Transition them to Accepted state (or Rejected)
3. Eventually transition to Sealed state
4. Update blockchain state atomically

### Configuration

**File**: appsettings.json
```json
"Validator": {
    "CycleTime": 10  // Processing cycle in seconds
}
```

### Current Limitation

**TODO in code**: Comments indicate planned network synchronization:
```csharp
//todo: when we have network, ask the Network for the height first - this might be a recovery.
```

This suggests consensus is deferred until network-aware implementation.

---

## 7. Wallet and Cryptographic Integration

### Wallet Service Architecture

**Location**: `/src/Services/Wallet/`

#### Wallet Model
```csharp
public class Wallet
{
    public string Address { get; init; }          // Primary wallet address (key)
    public string PrivateKey { get; init; }       // Protected (never exposed)
    public string Name { get; set; }              // Human-readable name
    public string Owner { get; init; }            // Owner identifier
    public string Tenant { get; init; }           // Tenant association
    public ICollection<WalletAccess> Delegates { get; set; }
    public ICollection<WalletAddress> Addresses { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; }
}
```

#### Wallet Protection (IWalletProtector)
```csharp
public interface IWalletProtector
{
    string MasterEncryptionKey { get; set; }
    
    string ProtectWallet(string walletToProtect);
    string UnprotectWallet(string walletToUnprotect);
    Wallet ProtectWallet(Wallet unprotectedWallet);
    Wallet UnProtectWallet(Wallet protectedWallet);
    WalletAddress ProtectWalletAddress(WalletAddress unprotectedAddress);
    WalletAddress UnProtectWalletAddress(WalletAddress protectedAddress);
}
```

### Transaction Signature Integration

**In TransactionModel**:
```csharp
public string Signature { get; set; }  // Cryptographic signature
```

The signature is:
- Generated by the sender's wallet service
- Included in the transaction payload
- Not validated by the Validator Service
- Used for non-repudiation and authenticity

### Cryptographic Workflow

```
Sender Wallet (WalletService)
    → Generate TransactionModel
    → Sign with private key
    → Publish to PeerService
    → PeerService validates/routes
    → ValidatorService receives (no re-validation)
    → Docket includes signed transactions
    → RegisterService stores immutably
```

### Security Considerations

1. **Private Keys**: Stored in wallet service, never exposed
2. **Master Key**: Used for wallet encryption (configurable storage)
3. **HSM Support**: IWalletProtector designed for Hardware Security Module
4. **Delegation**: WalletAccess entities manage wallet sharing
5. **Tenant Isolation**: Wallets belong to specific tenants

---

## 8. APIs for Administration, Metrics, and Control

### Administrative APIs

#### ValidatorController Endpoints

**Route Prefix**: `/api/Validators` (from Constants.ValidatorAPIURL)

##### 1. Get Validator Status
```
GET /api/Validators
Authorization: Bearer {JWT token}
Roles: installation.admin, installation.reader, register.maintainer

Response: JSON array of Register status
{
  "id": "register-guid",
  "name": "Register Name",
  "height": 42,
  "status": "ONLINE",
  "dockets": [...],
  "transactions": [...]
}
```

**Implementation**: Returns `validator.Status` property (serialized list of registers)

##### 2. Get Transaction by ID
```
GET /api/Validators/{transactionId}
Authorization: Bearer {JWT token}
Roles: installation.admin, installation.reader, register.maintainer

Response: TransactionModel from state store
```

**Implementation**: Retrieves from Dapr state store using @dapr annotation

##### 3. Receive Transaction (Event Handler)
```
POST /api/Validators
Content-Type: application/json
Topic: OnTransaction_Pending (Dapr pub/sub)

Request Body: TransactionModel
Response: 200 OK (async)
```

**Implementation**: 
- Subscribed to PeerService via Dapr pub/sub
- Adds transaction to MemPool
- Grouped by RegisterId

### Health Check Endpoints

**Route**: `/api/Validators/healthz`

- ASP.NET Core health checks endpoint
- Reports service and dependency health
- Checks:
  - Service responsiveness
  - Dapr connectivity
  - RegisterService connectivity

### Metrics and Monitoring

#### Logging
- **Framework**: Serilog with structured logging
- **Sinks**: 
  - Application Insights (telemetry)
  - Console output
  - Seq (log aggregation)
- **Configuration**: appsettings.json
- **Min Level**: Information (debug in development)

#### Application Insights Integration
- Service profiling
- Request tracking
- Exception telemetry
- Performance monitoring

### Status Property

```csharp
public JsonElement Status 
{ 
    get { 
        return JsonSerializer.SerializeToElement(Registers, jopts); 
    } 
}
```

Returns current list of registers with:
- Height
- Vote information
- Associated dockets and transactions

---

## 9. Register/Blockchain Interaction

### RegisterServiceClient Interface

```csharp
public interface IRegisterServiceClient : ISiccarServiceClient
{
    // Transactions
    Task<TransactionModel> GetTransactionById(string registerId, string txId);
    Task<List<TransactionModel>> GetAllTransactions(string registerId, string query);
    Task<List<TransactionModel>> GetBlueprintTransactions(string registerId);
    Task<List<TransactionModel>> GetTransactionsByInstanceId(string registerId, string instanceId);
    Task<string> PostNewTransaction(string register, TransactionModel data);
    
    // Dockets (Blocks)
    Task<Docket> GetDocketByHeight(string register, UInt64 height);
    Task<string> PostNewDocket(Docket data);
    
    // Registers
    Task<List<Register>> GetRegisters();
    Task<Register> GetRegister(string registerId);
    Task<Register> CreateRegister(Register data);
    Task<bool> DeleteRegister(string registerId);
    
    // Events
    Task SubscribeRegister(string registerId);
    Task UnSubscribeRegister(string registerId);
    Task StartEvents();
}
```

### Validator-to-Register Communication

#### Docket Submission Flow
1. **Validator publishes**: `OnDocket_Confirmed` topic
2. **Register receives**: DocketController.ReceiveDocket()
3. **Register stores**: InsertDocketAsync()
4. **Register updates height**: Height++
5. **Register publishes**: `OnTransaction_ValidationCompleted` topic

#### Code Example
```csharp
// In RulesBasedValidator.ProcessValidation()
var docket = Builder.GenerateDocket(new Docket() { ... });

// Publish to RegisterService
await Client.PublishEventAsync<Docket>(
    PubSubName, 
    Topics.DocketConfirmedTopicName, 
    docket
);

// Clear MemPool and notify wallets
foreach (TransactionModel transaction in localRegPool)
{
    await Client.PublishEventAsync<TransactionModel>(
        PubSubName, 
        Topics.TransactionValidationCompletedTopicName, 
        transaction
    );
}
```

### Transaction Event Chain

```
OnTransaction_Pending
    → ValidatorService (MemPool)
    → OnDocket_Confirmed
    → RegisterService (storage)
    → OnTransaction_ValidationCompleted
    → WalletService/Clients (final state)
```

---

## 10. Key Domain Models

### Docket Model
```csharp
public class Docket
{
    [Key] public ulong Id { get; set; }                    // Block height
    [ForeignKey("Register")] public string RegisterId { get; set; }
    [Required] public string PreviousHash { get; set; }    // Chain link
    [Required] public string Hash { get; set; }            // Block hash
    public string Votes { get; set; }                      // Voting data
    public TransactionMetaData MetaData { get; set; }      // Type = Docket
    public List<string> TransactionIds { get; set; }       // Tx hashes in block
    public IEnumerable<TransactionModel> Transactions { get; set; }  // Navigation
    [Required] public DateTime TimeStamp { get; set; }     // UTC creation time
    [Required] public DocketState State { get; set; }      // Current state
}

public enum DocketState { Init, Proposed, Accepted, Rejected, Sealed }
```

### TransactionModel
```csharp
public class TransactionModel
{
    [Key] [BsonId] public string Id => TxId;  // 64-char hex hash
    public string TxId { get; set; }
    public string PrevTxId { get; set; }      // Previous transaction link
    public UInt32 Version { get; set; }
    [Required] public string SenderWallet { get; set; }    // Base58 address
    [Required] public IEnumerable<string> RecipientsWallets { get; set; }
    public DateTime TimeStamp { get; set; }
    [Required] public TransactionMetaData MetaData { get; set; }
    public UInt64 PayloadCount { get; set; }
    [Required] public PayloadModel[] Payloads { get; set; }
    [Required] public string Signature { get; set; }       // Crypto signature
}
```

### TransactionMetaData
```csharp
public class TransactionMetaData
{
    [Key] public int Id { get; set; }                      // DB record ID
    [Required] public string RegisterId { get; set; }      // Register GUID
    public TransactionTypes TransactionType { get; set; }  // Action, Blueprint, File, etc.
    public string BlueprintId { get; set; }                // Blueprint reference
    public string InstanceId { get; set; }                 // Blueprint instance
    public int ActionId { get; set; }                      // Action step
    public int NextActionId { get; set; }                  // Next action
    public SortedList<string, string> TrackingData { get; set; }  // Progress tracking
}

public enum TransactionTypes
{
    Docket = 0,
    Rejection = 4,
    Blueprint = 10,
    Action = 11,
    File = 12,
    Production = 13,
    Challenge = 14,
    Participant = 15
}
```

### PayloadModel
```csharp
public class PayloadModel
{
    public string[] WalletAccess { get; set; }      // Wallet addresses with access
    public ulong PayloadSize { get; set; }
    public string Hash { get; set; }                // Payload hash
    public string Data { get; set; }                // Encrypted payload data
    public string PayloadFlags { get; set; }        // Feature flags
    public Challenge IV { get; set; }               // Initialization vector
    public Challenge[] Challenges { get; set; }     // Crypto challenges
}

public class Challenge
{
    public string hex { get; set; }
    public ulong size { get; set; }
}
```

### Register Model
```csharp
public class Register
{
    [Key] public string Id { get; set; }                   // GUID (no hyphens)
    [Required] public string Name { get; set; }            // Display name
    public uint Height { get; set; }                       // Block height
    public string Votes { get; set; }                      // Voting info
    public IEnumerable<Docket> Dockets { get; set; }       // Blocks
    public IEnumerable<TransactionModel> Transactions { get; set; }
    public bool Advertise { get; set; }                    // Public flag
    public bool IsFullReplica { get; set; }                // Full storage
    public RegisterStatusTypes Status { get; set; }        // State
}

public enum RegisterStatusTypes
{
    OFFLINE = -1,
    ONLINE = 0,
    CHECKING = 1,
    RECOVERY = 2
}
```

### MemPool Structure

```csharp
private Dictionary<string, List<TransactionModel>> pool;

// Thread-safe operations with lock(_locker)
public void AddTxToPool(TransactionModel tx)
{
    if (pool.ContainsKey(tx.MetaData.RegisterId))
        pool[tx.MetaData.RegisterId].Add(tx);
    else
        pool.Add(tx.MetaData.RegisterId, new List<TransactionModel> { tx });
}

public List<TransactionModel> GetPool(string RegisterId = "")
{
    // Returns and CLEARS the register's pool
    var regView = pool[RegisterId];
    pool.Remove(RegisterId);
    return regView;
}
```

---

## 11. Security Considerations

### Authentication & Authorization

#### JWT Bearer Tokens
- **Issuer**: TenantService (configurable)
- **Audience**: "siccar.dev" (configurable)
- **Validation**:
  - Signature verification using issuer's public key
  - Tenant claim requirement
  - Role-based access control

#### Role-Based Access Control (RBAC)
```csharp
[Authorize(Roles = $"{Constants.InstallationAdminRole}," +
    "{Constants.InstallationReaderRole}," +
    "{Constants.RegisterMaintainerRole}")]
```

**Available Roles**:
- `installation.admin` - System administration
- `installation.reader` - Read-only system access
- `register.maintainer` - Register management
- `register.creator` - Register creation
- `register.reader` - Register read access
- `wallet.user`, `wallet.owner`, `wallet.delegate` - Wallet operations
- `blueprint.admin`, `blueprint.authoriser` - Blueprint management

#### Dapr Authentication
- **Separate scheme**: `DaprAuthenticationScheme`
- **Secret-based**: DaprSecret configuration
- **Service-to-service**: Validates internal calls from RegisterService

### Dapr Pub/Sub Security

- **Topic Restrictions**: Service receives only configured topics
- **Policy Authorization**: `AuthenticationDefaults.DaprAuthorizationPolicy`
- **Metadata Validation**: Topics include service identity

### Sensitive Data Protection

#### Never Exposed
1. **Wallet Private Keys**: Encrypted in WalletService only
2. **Transaction Details**: Encrypted payloads with access controls
3. **Docket Votes**: Currently placeholder (future voting)

#### Access Controls
1. **Wallet Delegation**: WalletAccess entities manage sharing
2. **Payload Access**: Wallet addresses in WalletAccess[]
3. **Tenant Isolation**: All data scoped to tenant

### Input Validation

#### ValidatorController
```csharp
if (transaction.Value is null)
    return this.NotFound();

if (!ModelState.IsValid)
    return BadRequest(ModelState);
```

#### Docket Validation
- Register existence check
- Height consistency verification
- No direct transaction validation (delegated)

### Dapr Security Features

1. **State Store Encryption**: Dapr encrypts state by default
2. **Secrets Management**: Dapr rotates secrets automatically
3. **Service Invocation mTLS**: Optional mutual TLS
4. **Network Policies**: Dapr enforces service boundaries

### Cryptographic Standards

- **Hashing**: SHA256 for all blockchain hashes
- **Signatures**: Transaction signatures (algorithm TBD in WalletService)
- **Wallet Encryption**: Configurable (local file or HSM)

### Potential Vulnerabilities

1. **No Transaction Signature Verification**: Validator trusts PeerService
2. **Placeholder Voting**: Consensus not implemented yet
3. **Fixed PreviousHash**: All dockets use same previous hash placeholder
4. **No Rate Limiting**: No transaction rate limits per wallet
5. **MemPool Flooding**: No max transaction limits per register

---

## 12. Enclave and Secure Execution Patterns

### Current Implementation

**Status**: Enclaves are **NOT currently implemented** in the Validator Service.

### Intended Architecture (Based on Code Design)

#### HSM Support
The `IWalletProtector` interface is designed for Hardware Security Module integration:

```csharp
public interface IWalletProtector
{
    string MasterEncryptionKey { get; set; }  // Can't be read from HSM
    
    // Encrypt/decrypt operations that could delegate to HSM
    string ProtectWallet(string walletToProtect);
    string UnprotectWallet(string walletToUnprotect);
}
```

This suggests planned support for:
- Hardware Key Storage (HSM)
- Secure cryptographic operations
- Key management separation

#### Multi-Tenancy Isolation

Docket and transaction processing is isolated per:
- **RegisterId**: Each register processed separately
- **Tenant**: Wallet owner restrictions
- **MemPool**: Separate transaction pools per register

#### Service Isolation via Dapr

Each service operates in isolation with:
- **Service Invocation**: Direct service-to-service calls through Dapr
- **State Store Access**: Dapr grants scoped access to state
- **Event Isolation**: Topics enforce service boundaries

---

## Implementation Workflow Summary

### Validation Cycle (10-second intervals, configurable)

```
[Timer Fires Every 10s]
    ↓
RulesBasedValidator.ProcessValidation()
    ↓
[For Each Register]
    ↓
Check Height
    → if Height < 1: Genesys.ProcessGenesys() → Sealed Genesis Block
    → else: Continue
    ↓
Retrieve MemPool for Register
    ↓
If Transactions Exist:
    → DocketBuilder.GenerateDocket()
    → Compute SHA256 hashes
    → Publish to Topics.DocketConfirmedTopicName
    → Clear MemPool
    → Publish TransactionValidationCompleted for each TX
    ↓
Update Register Height in Memory
    ↓
[Consensus Cycle - Currently Empty]
    ↓
RegisterService receives docket via pub/sub
    → InsertDocketAsync()
    → Increment Height in DB
    → Publish Transaction finalization events
```

### Thread Safety

**MemPool Implementation**:
```csharp
private readonly object _locker = new();

public void AddTxToPool(TransactionModel tx)
{
    lock (_locker)  // Thread-safe insertion
    {
        // Add to dictionary
    }
}

public List<TransactionModel> GetPool(string RegisterId = "")
{
    lock (_locker)  // Thread-safe retrieval & removal
    {
        var regView = pool[RegisterId];
        pool.Remove(RegisterId);
        return regView;
    }
}
```

This ensures concurrent access from multiple threads (HTTP receiver + validation timer) is safe.

---

## Dependencies and Integration Points

### External Services
1. **RegisterService**: Docket storage, register retrieval
2. **PeerService**: Transaction sourcing
3. **WalletService**: Signature validation (not yet used)
4. **TenantService**: JWT issuer for authentication
5. **Blueprint/Action Services**: Transaction metadata context

### Dapr Components
1. **Pub/Sub**: Transaction and docket event distribution
2. **State Store**: Transaction caching (optional)
3. **Service Invocation**: Direct RegisterService calls
4. **Secrets**: DaprSecret for inter-service auth

### NuGet Dependencies
- Dapr.Client, Dapr.AspNetCore (v1.14.0)
- Microsoft.AspNetCore.Authentication.JwtBearer
- Serilog for structured logging
- FluentValidation for model validation

---

## Configuration and Deployment

### Configuration Files

**appsettings.json**:
```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "Serilog": { /* structured logging config */ },
  "Validator": { "CycleTime": 10 },  // Validation cycle in seconds
  "DaprSecret": "",                   // Dapr authentication secret
  "TenantIssuer": "http://localhost", // JWT issuer URL
  "ApplicationInsights": { "ConnectionString": "" }
}
```

### Environment Variables
- `DAPR_GRPC_PORT`: Dapr communication port
- `DAPR_HTTP_PORT`: Dapr HTTP port
- `Validator__CycleTime`: Override cycle time

### Docker Deployment
- **Base Image**: ASP.NET 9.0
- **Port**: 5294 (configurable in launchSettings.json)
- **Health Check**: /api/Validators/healthz

---

## Design Patterns and Best Practices

### Applied Patterns

1. **Hosted Service Pattern**: RulesBasedValidator implements IHostedService
2. **Dependency Injection**: Constructor-based DI throughout
3. **Pub/Sub Pattern**: Event-driven architecture via Dapr
4. **Adapter Pattern**: IDaprClientAdaptor abstracts Dapr SDK
5. **Factory Pattern**: DocketBuilder and Genesys are transient services
6. **Repository Pattern**: RegisterServiceClient abstracts Register persistence

### Code Organization

- **Separation of Concerns**: Interfaces (ValidatorCore) separate from implementations (ValidationEngine)
- **Single Responsibility**: DocketBuilder only builds, RulesBasedValidator orchestrates
- **Dependency Inversion**: Depends on abstractions (interfaces) not concretions
- **Configuration Management**: IConfiguration injected for settings

---

## Known Limitations and TODOs

### Documented TODOs in Code

1. **Height Recovery**:
   ```csharp
   //todo: when we have network, ask the Network for the height first - this might be a recovery.
   ```
   - Suggests future network sync on recovery

2. **Docket Consensus Finalization** (in DocketController.PostDocket):
   ```csharp
   // OK this is just for the moment as we WOULD NOT WRITE THIS STRAIGHT TO STORAGE!!!
   ```
   - Indicates temporary direct-to-storage approach before proper consensus

3. **Transaction Wallet Notification** (in DocketController.ReceiveDocket):
   ```csharp
   // now inform any local participants about updates
   // we are currently calling it from Receive Transaction
   // InformLocalWallets(head.RegisterId, head.TransactionIds);
   ```

4. **Voting/Consensus Mechanism**:
   - Comment: "Not sure why this is here!!" on Votes field
   - Consensus implementation is a stub

### Feature Gaps

1. **No Transaction Validation**: Validator doesn't validate transaction content
2. **No Network Consensus**: Single-node consensus stub only
3. **No Fork Resolution**: No handling of chain forks
4. **No Pruning**: No transaction/docket pruning mechanism
5. **No RPC API**: Limited external API compared to blockchain nodes
6. **No Block Proposal**: Validator always creates blocks, no proposal/voting cycle

---

## Recommended Design Patterns for Sorcha Equivalent

Based on this analysis, when designing the Sorcha Validator Service, consider:

1. **Implement Proper Consensus**: Don't leave it as a stub - implement BFT or PoS
2. **Add Comprehensive Validation**: Validate transactions before docket inclusion
3. **Network Awareness**: Implement peer-to-peer docket distribution
4. **Fork Handling**: Design for chain fork resolution
5. **Scalability**: Consider transaction batching and state sharding
6. **Rate Limiting**: Implement queue limits and transaction fees
7. **Monitoring**: Add detailed metrics for docket creation and validation
8. **Recovery**: Implement full recovery protocol from stored state

---

## Conclusion

The SiccaV3 Validator Service is a foundational microservice designed to group and organize transactions into immutable blocks (Dockets). It provides a clean, extensible architecture with:

- **Clear separation** between transaction reception, docket building, and consensus
- **Event-driven design** using Dapr pub/sub for loose coupling
- **Security-first approach** with JWT authentication and role-based access
- **Multi-register support** with per-register processing pipelines
- **Extensible validation** designed to plug in custom rules

The main current limitations are the lack of a true consensus mechanism and minimal transaction validation, which appear to be intentionally deferred for future phases. The architecture is well-positioned for these enhancements.

