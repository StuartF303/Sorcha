# Implementation Plan: Validator Service

**Feature Branch**: `validator-service`
**Created**: 2025-12-03
**Status**: 0% Complete (Specification Phase)

## Summary

The Validator Service is the blockchain consensus and validation component of the Sorcha platform. It implements distributed ledger consensus, building and validating Dockets (blocks) that contain Transactions from Blueprint Action executions. The service is designed with an enclave-safe core library for secure execution.

## Design Decisions

### Decision 1: 4-Layer Architecture

**Approach**: API -> Service Orchestration -> External Clients -> Core Library.

**Rationale**:
- Clear separation of concerns
- Enclave-safe core library can run in SGX/SEV/HSM
- Testable components with mocked dependencies
- Consistent with Sorcha service patterns

### Decision 2: Enclave-Safe Core Library

**Approach**: Sorcha.Validator.Core is a pure .NET library with no I/O.

**Rationale**:
- Can be compiled for Intel SGX or AMD SEV
- All data passed as parameters
- Stateless, deterministic operations
- Maximum security for validation logic

### Decision 3: Simple Quorum Consensus

**Approach**: Quorum-based consensus (default 2/3 majority).

**Rationale**:
- Simple to implement and understand
- Configurable quorum percentage
- Sufficient for enterprise deployments
- Can be upgraded to BFT in future

### Decision 4: Per-Register MemPools

**Approach**: Separate MemPool for each register with size limits.

**Rationale**:
- Isolation between registers
- Configurable limits per register
- FIFO ordering for fairness
- Automatic expiration cleanup

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Sorcha.Validator.Service                    │
│                   (ASP.NET Core 10)                      │
├─────────────────────────────────────────────────────────┤
│  Layer 1: API Layer (Minimal APIs)                      │
│  ├── ValidationEndpoints.cs   (Docket build/validate)   │
│  ├── AdminEndpoints.cs        (Start/stop/pause)        │
│  └── MetricsEndpoints.cs      (Operational metrics)     │
├─────────────────────────────────────────────────────────┤
│  Layer 2: Service Orchestration                         │
│  ├── ValidatorOrchestrator.cs (Main coordinator)        │
│  ├── DocketBuilder.cs         (Build dockets)           │
│  ├── TransactionValidator.cs  (Validate transactions)   │
│  ├── ConsensusEngine.cs       (Distributed consensus)   │
│  ├── MemPoolManager.cs        (Transaction pools)       │
│  └── GenesisManager.cs        (Genesis blocks)          │
├─────────────────────────────────────────────────────────┤
│  Layer 3: External Service Clients                      │
│  ├── WalletServiceClient.cs   (Signature verification)  │
│  ├── PeerServiceClient.cs     (Consensus coordination)  │
│  ├── RegisterServiceClient.cs (Chain queries)           │
│  └── BlueprintServiceClient.cs (Schema retrieval)       │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│               Sorcha.Validator.Core                      │
│         (Enclave-Safe Library, Pure .NET)                │
├─────────────────────────────────────────────────────────┤
│  Validators/                                             │
│  ├── DocketValidator.cs       (Hash computation)        │
│  ├── TransactionValidator.cs  (Schema validation)       │
│  ├── ConsensusValidator.cs    (Vote verification)       │
│  └── ChainValidator.cs        (Integrity checks)        │
├─────────────────────────────────────────────────────────┤
│  Characteristics:                                        │
│  ✓ No I/O (all data passed as parameters)               │
│  ✓ No network (no HttpClient, no database)              │
│  ✓ Stateless (no mutable state)                         │
│  ✓ Deterministic (same input = same output)             │
│  ✓ Thread-safe (can run in parallel)                    │
│  ✓ Enclave-compatible (Intel SGX, AMD SEV, HSM)         │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Project Structure | 0% | Not created |
| Interfaces | 0% | Not defined |
| ValidatorOrchestrator | 0% | Not implemented |
| DocketBuilder | 0% | Not implemented |
| TransactionValidator | 0% | Not implemented |
| ConsensusEngine | 0% | Not implemented |
| MemPoolManager | 0% | Not implemented |
| GenesisManager | 0% | Not implemented |
| Sorcha.Validator.Core | 0% | Not created |
| API Endpoints | 0% | Not implemented |
| Unit Tests | 0% | Not created |

### API Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/validation/dockets/build` | Build new docket | Pending |
| POST | `/api/validation/dockets/validate` | Validate docket | Pending |
| POST | `/api/validation/transactions/add` | Add to MemPool | Pending |
| POST | `/api/validation/genesis` | Create genesis | Pending |
| POST | `/api/admin/validation/start/{registerId}` | Start validation | Pending |
| POST | `/api/admin/validation/stop/{registerId}` | Stop validation | Pending |
| POST | `/api/admin/validation/pause/{registerId}` | Pause validation | Pending |
| POST | `/api/admin/validation/resume/{registerId}` | Resume validation | Pending |
| GET | `/api/admin/validation/status` | Get all statuses | Pending |
| GET | `/api/metrics/dockets` | Docket metrics | Pending |
| GET | `/api/metrics/transactions` | Transaction metrics | Pending |
| GET | `/api/metrics/consensus` | Consensus metrics | Pending |
| GET | `/api/metrics/mempool` | MemPool metrics | Pending |

## Dependencies

### Internal Dependencies

- `Sorcha.ServiceDefaults` - .NET Aspire configuration
- `Sorcha.Cryptography` - Hashing (SHA256)
- `Sorcha.TransactionHandler` - Transaction models

### External Dependencies

- `System.Text.Json` - JSON serialization

### Service Dependencies

- Wallet Service - Signature verification
- Peer Service - Docket broadcasting, consensus
- Register Service - Chain queries, storage
- Blueprint Service - Schema retrieval

## Migration/Integration Notes

### Consensus Configuration

```json
{
  "Validator": {
    "Consensus": {
      "Type": "SimpleQuorum",
      "QuorumPercentage": 0.67,
      "MinimumValidators": 1,
      "VoteTimeout": "00:00:30"
    },
    "MemPool": {
      "MaxSizePerRegister": 10000,
      "TransactionExpirationTime": "24:00:00"
    },
    "DocketBuilding": {
      "MaxTransactionsPerDocket": 100,
      "MaxDocketSizeBytes": 1048576,
      "BuildInterval": "00:00:10"
    }
  }
}
```

### Breaking Changes

- None (new service)

## Open Questions

1. Should we support multiple consensus algorithms (PBFT, Raft)?
2. How to handle validator node addition/removal dynamically?
3. Should enclave attestation be required for production?
4. How to handle cross-register transactions?
