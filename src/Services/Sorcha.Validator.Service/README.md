# Sorcha Validator Service

**Version**: 1.0.0
**Status**: Stub/Design Phase (15% Complete - Core Components Only)
**Framework**: .NET 10.0
**Architecture**: Secure Validation Microservice

---

## Overview

The **Validator Service** is the blockchain consensus and security component of the Sorcha platform, responsible for building, validating, and sealing Dockets (blocks) with cryptographic integrity. Designed for secure enclave execution (Intel SGX, AMD SEV, Azure Confidential Computing), it serves as the trust anchor ensuring that only valid, cryptographically sound transactions are committed to Sorcha Registers.

This service acts as the consensus arbitrator for:
- **Docket building** from validated transaction pools
- **Cryptographic validation** of transaction signatures and chain integrity
- **Consensus coordination** across distributed peer networks
- **Genesis block creation** for new register initialization
- **Security-critical operations** executed in isolated secure environments

### Key Features

- **Secure Enclave Design**: Architecture supports Intel SGX, AMD SEV, and HSM execution environments
- **Docket Management**: Build, propose, accept, and seal dockets with SHA256 chain integrity
- **Chain Validation**: Verify blockchain integrity with cryptographic hash verification
- **MemPool Management**: Thread-safe transaction queueing with per-register isolation (planned)
- **Consensus Orchestration**: Distributed validation coordination across peer networks (planned)
- **Genesis Handling**: Special validation rules for register initialization blocks (planned)
- **Transaction Validation**: Verify signatures, schemas, and business rules (planned)
- **Security Isolation**: Cryptographic operations separated from general-purpose services
- **Audit Logging**: Comprehensive security event tracking

---

## Architecture

### Components

```
Validator Service (Secure Environment)
├── API Layer (Planned)
│   ├── Validation Endpoints (/api/validation)
│   ├── Admin Endpoints (/api/admin)
│   └── Metrics Endpoints (/api/metrics)
├── Service Layer (Planned)
│   ├── ValidatorOrchestrator (main service)
│   ├── TransactionValidator (signature verification)
│   ├── ConsensusEngine (distributed validation)
│   └── MemPoolManager (transaction queueing)
├── Core Components (Current)
│   ├── DocketManager (docket building, sealing)
│   └── ChainValidator (blockchain integrity)
├── Sorcha.Validator.Core (Enclave-Safe Library)
│   ├── Pure validation logic (no I/O, stateless)
│   └── Designed for SGX/SEV enclave execution
└── External Integrations
    ├── Wallet Service (signature verification)
    ├── Register Service (blockchain storage)
    ├── Peer Service (consensus coordination)
    └── Blueprint Service (schema validation)
```

### Security Architecture

**Isolation Layers:**
```
┌─────────────────────────────────────────────────────┐
│         Untrusted Environment (Network Layer)        │
│  ┌──────────────┬──────────────┬──────────────┐    │
│  │ API Gateway  │ Peer Service │ Register Svc │    │
│  └──────────────┴──────────────┴──────────────┘    │
└─────────────────────────────────────────────────────┘
                        ↓ Encrypted Channel
┌─────────────────────────────────────────────────────┐
│      Validator Service (Trusted Compute Base)        │
│  ┌──────────────────────────────────────────────┐  │
│  │ Sorcha.Validator.Core (Enclave-Safe)         │  │
│  │ - DocketManager (SHA256 hashing)             │  │
│  │ - ChainValidator (integrity verification)    │  │
│  │ - No I/O, no network calls                   │  │
│  │ - Stateless, deterministic                   │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
                        ↓ Secure Channel
┌─────────────────────────────────────────────────────┐
│         Hardware Security Module (Optional)          │
│  ┌──────────────────────────────────────────────┐  │
│  │ Key Material (Private Keys, Certificates)    │  │
│  │ - Intel SGX Enclave                          │  │
│  │ - AMD SEV Secure Memory                      │  │
│  │ - Azure Confidential Computing               │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

**Security Benefits:**
- **Cryptographic Operations**: SHA256 hashing runs in secure environment
- **Chain Validation**: Blockchain integrity checks isolated from general compute
- **Zero-Trust Design**: Validator doesn't trust any external input without verification
- **Enclave-Ready**: Can be deployed to secure enclaves for production environments

### Data Flow

```
Transaction Submitted (Blueprint Service)
      ↓
Peer Service → [Gossip Distribution]
      ↓
Validator Service → [MemPool (Pending)]
      ↓
ValidatorOrchestrator → [Cycle Timer: Every 10 seconds]
      ↓
DocketManager → [Build Docket from MemPool]
      ↓
TransactionValidator → [Verify Signatures, Schemas]
      ↓
ChainValidator → [Verify PreviousHash, Chain Integrity]
      ↓
ConsensusEngine → [Coordinate Multi-Peer Validation]
      ↓
DocketManager → [Seal Docket (State: Init → Proposed → Accepted → Sealed)]
      ↓
Event Publisher → [Publish DocketConfirmed Event]
      ↓
Register Service → [Store Docket, Update Height]
      ↓
SignalR Hub → [Notify Clients: DocketSealed]
```

---

## Quick Start

### Prerequisites

- **.NET 10 SDK** or later
- **Git**
- *Optional*: Intel SGX SDK (for enclave deployment)
- *Optional*: Azure Confidential Computing environment

### 1. Clone and Navigate

```bash
git clone https://github.com/yourusername/Sorcha.git
cd Sorcha/src/Services/Sorcha.Validator.Service
```

### 2. Set Up Configuration

The service uses `appsettings.json` for configuration. For local development, defaults are pre-configured.

### 3. Run the Service (Planned)

```bash
dotnet run
```

Service will start at:
- **HTTPS**: `https://localhost:7086`
- **HTTP**: `http://localhost:5086`
- **Scalar API Docs**: `https://localhost:7086/scalar` (planned)
- **Health Checks**: `https://localhost:7086/api/health`

---

## Configuration (Planned)

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Sorcha.Validator": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Validator": {
    "CycleTime": 10,
    "EnableSecureEnclave": false,
    "MemPool": {
      "MaxTransactionsPerRegister": 10000,
      "MaxTransactionSize": 10485760,
      "ExpirationMinutes": 60
    },
    "Consensus": {
      "Type": "Singular",
      "MinimumPeers": 3,
      "ConsensusThreshold": 0.67,
      "TimeoutSeconds": 30
    },
    "Validation": {
      "ValidateSignatures": true,
      "ValidateSchemas": true,
      "ValidateChainIntegrity": true,
      "MaxDocketSize": 1000
    }
  },
  "ServiceUrls": {
    "WalletService": "https://localhost:7084",
    "RegisterService": "https://localhost:7085",
    "PeerService": "https://localhost:5000",
    "BlueprintService": "https://localhost:7081"
  },
  "OpenTelemetry": {
    "ServiceName": "Sorcha.Validator.Service",
    "ZipkinEndpoint": "http://localhost:9411"
  }
}
```

### Environment Variables

For production deployment:

```bash
# Validator configuration
VALIDATOR__CYCLETIME=10
VALIDATOR__ENABLESECUREENCLAVE=true

# Service URLs
SERVICEURLS__WALLETSERVICE="https://wallet.sorcha.io"
SERVICEURLS__REGISTERSERVICE="https://register.sorcha.io"
SERVICEURLS__PEERSERVICE="https://peer.sorcha.io"

# Observability
OPENTELEMETRY__ZIPKINENDPOINT="https://zipkin.yourcompany.com"
```

---

## API Endpoints (Planned)

### Validation Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/validation/dockets/build` | Build new docket from mempool |
| POST | `/api/validation/dockets/validate` | Validate incoming docket |
| POST | `/api/validation/transactions/add` | Add transaction to mempool |
| GET | `/api/validation/transactions/{registerId}` | Get pending transactions |
| POST | `/api/validation/genesis` | Create genesis block for register |

### Admin Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/admin/validation/start/{registerId}` | Start validation for register |
| POST | `/api/admin/validation/stop/{registerId}` | Stop validation for register |
| POST | `/api/admin/validation/pause/{registerId}` | Pause validation temporarily |
| POST | `/api/admin/validation/resume/{registerId}` | Resume validation |
| GET | `/api/admin/validation/status` | Get validation status for all registers |
| PUT | `/api/admin/config` | Update runtime configuration |

### Metrics Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/metrics/dockets` | Docket building/validation metrics |
| GET | `/api/metrics/transactions` | Transaction processing metrics |
| GET | `/api/metrics/consensus` | Consensus performance metrics |
| GET | `/api/metrics/mempool` | MemPool statistics |

---

## Current Implementation

### Implemented Components

**DocketManager** (`Managers/DocketManager.cs`)
- Create dockets from transaction lists
- Calculate SHA256 hashes for blockchain integrity
- Propose dockets for consensus
- Seal dockets after approval
- Update register height atomically
- Publish docket events

**ChainValidator** (`Validators/ChainValidator.cs`)
- Verify blockchain chain integrity
- Detect broken hash links
- Validate docket sequences
- Report validation failures

### Pending Components

- **ValidatorOrchestrator**: Main service coordination (planned)
- **TransactionValidator**: Signature and schema validation (planned)
- **ConsensusEngine**: Multi-peer consensus coordination (planned)
- **MemPoolManager**: Transaction queue management (planned)
- **GenesisManager**: Genesis block creation (planned)
- **API Layer**: REST endpoints (planned)

---

## Development

### Project Structure

```
Sorcha.Validator.Service/
├── Program.cs (planned)            # Service entry point
├── Managers/
│   └── DocketManager.cs            # ✅ Docket building and sealing
├── Validators/
│   └── ChainValidator.cs           # ✅ Chain integrity validation
├── Services/ (planned)
│   ├── ValidatorOrchestrator.cs    # Main service coordination
│   ├── TransactionValidator.cs     # Transaction validation
│   ├── ConsensusEngine.cs          # Consensus orchestration
│   └── MemPoolManager.cs           # Transaction queueing
└── appsettings.json (planned)      # Configuration

Core Libraries:
├── Sorcha.Validator.Core/          # Enclave-safe core library
│   ├── Managers/
│   │   └── DocketManager.cs
│   └── Validators/
│       └── ChainValidator.cs
└── Sorcha.Register.Models/         # Shared domain models
    ├── Docket.cs
    ├── TransactionModel.cs
    └── Register.cs
```

### Running Tests (Planned)

```bash
# Run all Validator Service tests
dotnet test tests/Sorcha.Validator.Service.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Watch mode (auto-rerun on changes)
dotnet watch test --project tests/Sorcha.Validator.Service.Tests
```

### Code Coverage

**Current Coverage**: N/A (no tests yet)
**Tests**: Pending implementation
**Lines of Code**: ~500 LOC (core components only)

---

## Docket Lifecycle

### Docket States

```
┌──────────┐
│   Init   │ ← Docket created with transactions
└────┬─────┘
     ↓
┌──────────┐
│ Proposed │ ← Docket proposed for consensus
└────┬─────┘
     ↓
┌──────────┐
│ Accepted │ ← Consensus approval received
└────┬─────┘
     ↓
┌──────────┐
│  Sealed  │ ← Docket sealed and stored in Register
└──────────┘

Alternative paths:
┌──────────┐
│ Rejected │ ← Consensus rejection (retry or discard)
└──────────┘
```

### Docket Building Process

```csharp
// 1. Create docket from transaction pool
var docket = await docketManager.CreateDocketAsync(
    registerId: "abc123",
    transactionIds: new List<string> { "tx1", "tx2", "tx3" },
    cancellationToken);

// Docket properties:
// - Id: Next block height (register.Height + 1)
// - PreviousHash: SHA256 of previous docket
// - Hash: SHA256 of current docket content
// - State: Init
// - TransactionIds: List of transaction hashes

// 2. Propose for consensus
var proposed = await docketManager.ProposeDocketAsync(docket);
// State: Init → Proposed

// 3. Consensus coordination (external ConsensusEngine)
// - Broadcast docket to peers
// - Collect votes
// - Determine acceptance (threshold: 67% of peers)

// 4. Seal docket after approval
var sealed = await docketManager.SealDocketAsync(docket);
// State: Proposed → Sealed
// - Store in Register Service
// - Update register height atomically
// - Publish DocketConfirmed event
```

### SHA256 Hash Calculation

Docket hash is calculated deterministically:

```csharp
var hashInput = new
{
    docket.Id,                    // Block height
    docket.RegisterId,            // Register identifier
    docket.PreviousHash,          // Previous block hash
    TransactionIds = docket.TransactionIds.OrderBy(t => t),  // Sorted tx list
    TimeStamp = docket.TimeStamp.ToString("O")  // ISO 8601 timestamp
};

var json = JsonSerializer.Serialize(hashInput);
var bytes = Encoding.UTF8.GetBytes(json);
var hashBytes = SHA256.HashData(bytes);
var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
```

This ensures:
- **Deterministic**: Same inputs always produce same hash
- **Tamper-Proof**: Any change to docket content changes hash
- **Chain Integrity**: Previous hash links blocks together

---

## Integration with Other Services

### Wallet Service Integration

The Validator Service integrates with the Wallet Service for:
- **Signature Verification**: Validate transaction cryptographic signatures
- **Key Management**: Access public keys for signature validation
- **Challenge Verification**: Verify payload encryption challenges

**Communication**: HTTP REST API
**Endpoints Used**: `/api/v1/wallets/{address}/verify`, `/api/v1/wallets/{address}/key`

### Register Service Integration

The Validator Service integrates with the Register Service for:
- **Docket Storage**: Store sealed dockets in blockchain
- **Chain Queries**: Retrieve previous dockets for hash verification
- **Height Management**: Update register block height atomically

**Communication**: Event-driven messaging + HTTP REST API
**Events Published**: `DocketConfirmed`, `RegisterHeightUpdated`

### Peer Service Integration

The Validator Service integrates with the Peer Service for:
- **Consensus Coordination**: Broadcast dockets to peers for validation
- **Vote Collection**: Gather consensus votes from peer network
- **Network State**: Monitor peer availability for consensus threshold

**Communication**: gRPC + Event-driven messaging
**Events Subscribed**: `PeerConnected`, `PeerDisconnected`

### Blueprint Service Integration

The Validator Service integrates with the Blueprint Service for:
- **Schema Validation**: Retrieve blueprint JSON schemas for transaction validation
- **Business Rules**: Validate transactions against blueprint rules
- **Action Verification**: Ensure transactions match blueprint action definitions

**Communication**: HTTP REST API
**Endpoints Used**: `/api/blueprints/{id}`, `/api/blueprints/{id}/versions/{version}`

---

## Security Considerations

### Secure Enclave Support

The Validator Service is designed for deployment in secure enclaves:

**Intel SGX (Software Guard Extensions):**
```bash
# Build with SGX support
dotnet build -c Release -r linux-x64 /p:SgxEnabled=true

# Run in enclave
./run-in-sgx.sh
```

**AMD SEV (Secure Encrypted Virtualization):**
```bash
# Deploy to Azure Confidential Computing VM
az vm create \
  --resource-group validator-rg \
  --name validator-vm \
  --image UbuntuServer \
  --size Standard_DC2s_v2 \
  --admin-username azureuser
```

**Azure Confidential Computing:**
- Use DC-series VMs with Intel SGX support
- Enable Confidential Compute attestation
- Store keys in Azure Key Vault with HSM backing

### Authentication (Production)

- **Current**: Development mode (no authentication required)
- **Production**: Mutual TLS (mTLS) for validator-to-validator communication
- **Service Authentication**: JWT bearer tokens for external service calls

### Authorization

- **Consensus Voting**: Only registered validators can participate in consensus
- **Admin Operations**: Role-based access control for admin endpoints
- **Audit Logging**: All validation and consensus operations logged

### Data Protection

- **SHA256 Hashing**: All dockets use cryptographic hashing for integrity
- **Signature Verification**: All transactions must be cryptographically signed
- **No Sensitive Logging**: Never log private keys, mnemonics, or sensitive payloads

### Secrets Management

- **Private Keys**: Store in Azure Key Vault with HSM backing
- **TLS Certificates**: Rotate every 90 days
- **Enclave Secrets**: Use sealed storage for SGX environments

---

## Consensus Mechanisms (Planned)

### Singular Consensus (Default)

**Description**: Single validator node (no distributed consensus)

**Use Case**: Development, testing, single-organization deployments

**Algorithm**:
1. Validator builds docket
2. Validator immediately accepts docket
3. Docket sealed without peer voting

**Configuration**:
```json
{
  "Consensus": {
    "Type": "Singular",
    "AutoAccept": true
  }
}
```

### Byzantine Fault Tolerance (BFT) Consensus (Future)

**Description**: Distributed consensus across multiple validator nodes

**Use Case**: Production multi-organization deployments requiring Byzantine fault tolerance

**Algorithm**:
1. Validator builds docket and proposes to network
2. Each validator independently validates docket
3. Validators broadcast votes (Accept/Reject)
4. If ≥67% vote Accept, docket is sealed
5. If <67% vote Accept, docket is rejected (transactions return to mempool)

**Configuration**:
```json
{
  "Consensus": {
    "Type": "BFT",
    "MinimumPeers": 3,
    "ConsensusThreshold": 0.67,
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  }
}
```

**Byzantine Fault Tolerance Properties:**
- Tolerates up to (n-1)/3 faulty or malicious validators
- Requires ≥67% (2/3 + 1) agreement for consensus
- Example: 4 validators can tolerate 1 faulty, 7 can tolerate 2 faulty

---

## Observability

### Logging (Serilog + Seq)

Structured logging with Serilog:

```csharp
Log.Information("Docket {DocketId} created for register {RegisterId} with {TxCount} transactions",
    docketId, registerId, transactionCount);
Log.Warning("Chain validation failed for docket {DocketId}: {Reason}", docketId, reason);
Log.Error("Consensus failed for docket {DocketId}: threshold not met ({Votes}/{Required})",
    docketId, votes, required);
```

**Log Sinks**:
- Console (development)
- Seq (production) - `http://localhost:5341`

### Tracing (OpenTelemetry + Zipkin)

Distributed tracing with OpenTelemetry:

```bash
# View traces in Zipkin
open http://localhost:9411
```

**Traced Operations**:
- Docket building
- Transaction validation
- Chain validation
- Consensus coordination
- Signature verification

### Metrics (Prometheus)

Metrics exposed at `/metrics` (planned):
- Docket throughput (dockets/sec)
- Transaction validation latency
- Consensus success rate
- MemPool size by register
- Chain validation failures
- Signature verification time

---

## Troubleshooting (Planned)

### Common Issues

**Issue**: Docket sealing fails with chain validation error
**Solution**: Verify previous docket hash is correct. Use ChainValidator to diagnose.

```csharp
var validator = new ChainValidator(repository);
var results = await validator.ValidateRegisterChainAsync(registerId);
// Inspect results for broken chain links
```

**Issue**: Consensus never reaches threshold
**Solution**: Check peer connectivity and ensure minimum peers are online.

```bash
# Check peer count
curl https://localhost:7086/api/admin/validation/status

# Check consensus configuration
cat appsettings.json | grep "MinimumPeers"
```

**Issue**: MemPool filling up with pending transactions
**Solution**: Increase validation cycle frequency or investigate transaction validation failures.

```json
{
  "Validator": {
    "CycleTime": 5,  # Reduce from 10 to 5 seconds
    "MemPool": {
      "MaxTransactionsPerRegister": 20000  # Increase limit
    }
  }
}
```

### Debug Mode

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Sorcha.Validator": "Trace",
      "Sorcha.Validator.Core": "Trace"
    }
  }
}
```

---

## Contributing

### Development Workflow

1. **Create a feature branch**: `git checkout -b feature/your-feature`
2. **Make changes**: Follow C# coding conventions
3. **Write tests**: Maintain >85% coverage
4. **Run tests**: `dotnet test`
5. **Format code**: `dotnet format`
6. **Commit**: `git commit -m "feat: your feature description"`
7. **Push**: `git push origin feature/your-feature`
8. **Create PR**: Reference issue number

### Code Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use async/await for I/O operations
- Add XML documentation for public APIs
- **Security Critical**: All crypto operations must be reviewed by security team
- **Enclave-Safe**: Core validation logic must avoid I/O, network, and non-deterministic operations

---

## Resources

- **Design Document**: [docs/validator-service-design.md](../../docs/validator-service-design.md)
- **Implementation Plan**: [docs/validator-service-implementation-plan.md](../../docs/validator-service-implementation-plan.md)
- **Quick Reference**: [docs/VALIDATOR-SERVICE-QUICK-REFERENCE.md](../../docs/VALIDATOR-SERVICE-QUICK-REFERENCE.md)
- **Architecture**: [docs/architecture.md](../../docs/architecture.md)
- **Development Status**: [docs/development-status.md](../../docs/development-status.md)

---

## Technology Stack

**Runtime:**
- .NET 10.0 (10.0.100)
- C# 13
- ASP.NET Core 10

**Security:**
- Intel SGX SDK (optional, for enclave deployment)
- AMD SEV support (via Azure Confidential Computing)
- Azure Key Vault for secrets management

**Cryptography:**
- SHA256 for blockchain hashing
- ED25519/NIST P-256/RSA-4096 for signatures (via Wallet Service)

**Observability:**
- OpenTelemetry for distributed tracing
- Serilog for structured logging
- Prometheus metrics

**Testing:**
- xUnit for test framework
- FluentAssertions for assertions

---

## License

Apache License 2.0 - See [LICENSE](../../LICENSE) for details.

---

**Last Updated**: 2025-11-23
**Maintained By**: Sorcha Platform Team
**Status**: 🚧 Stub/Design Phase (15% Complete - Core Components Only)
