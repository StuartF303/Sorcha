# Implementation Plan: Register Service

**Feature Branch**: `register-service`
**Created**: 2025-12-03
**Status**: 100% Complete (MVD Phase)

## Summary

The Register Service is the foundational ledger component of the Sorcha platform, providing distributed ledger functionality for transaction storage, docket (block) management, and chain integrity. It supports multiple storage backends and integrates with the broader Sorcha ecosystem via events and real-time notifications.

## Design Decisions

### Decision 1: Storage Abstraction

**Approach**: Repository pattern with IRegisterRepository interface and multiple implementations.

**Rationale**:
- MongoDB for high-performance document storage (primary)
- PostgreSQL for relational deployments via EF Core
- In-memory for testing and development

**Alternatives Considered**:
- Single storage backend - Limits deployment flexibility
- Direct MongoDB driver - Couples implementation to storage

### Decision 2: Chain Validation Location

**Approach**: Chain validation logic moved to Validator Service for security isolation.

**Rationale**:
- Cryptographic operations require secured environment
- Supports future enclave deployment (SGX/SEV)
- Clean separation: Register stores, Validator validates

**Alternatives Considered**:
- Chain validation in Register Service - Security boundary concerns

### Decision 3: OData for Queries

**Approach**: OData V4 query support with translation to native backend queries.

**Rationale**:
- Industry standard for flexible REST queries
- Push query execution to storage layer for performance
- Familiar to enterprise developers

**Alternatives Considered**:
- GraphQL - Additional complexity, learning curve
- Custom query DSL - Non-standard, maintenance burden

### Decision 4: SignalR for Real-Time

**Approach**: SignalR hub with Redis backplane for multi-instance scaling.

**Rationale**:
- Native .NET integration
- WebSocket with fallback support
- Group-based subscriptions per register

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│               Sorcha.Register.Service                    │
│                   (ASP.NET Core 10)                      │
├─────────────────────────────────────────────────────────┤
│  APIs/                                                   │
│  ├── RegistersApi.cs         (Register CRUD)            │
│  ├── TransactionsApi.cs      (Transaction storage)      │
│  ├── DocketsApi.cs           (Docket management)        │
│  └── QueryApi.cs             (OData queries)            │
├─────────────────────────────────────────────────────────┤
│  Hubs/                                                   │
│  └── RegisterHub.cs          (SignalR real-time)        │
├─────────────────────────────────────────────────────────┤
│  Managers/                                               │
│  ├── RegisterManager.cs      (Register operations)      │
│  ├── TransactionManager.cs   (Transaction operations)   │
│  └── QueryManager.cs         (Query execution)          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                 Sorcha.Register.Core                     │
│               (Business Logic Library)                   │
├─────────────────────────────────────────────────────────┤
│  Storage/                                                │
│  ├── IRegisterRepository.cs                             │
│  ├── ITransactionRepository.cs                          │
│  └── IDocketRepository.cs                               │
├─────────────────────────────────────────────────────────┤
│  Events/                                                 │
│  ├── IEventPublisher.cs                                 │
│  ├── RegisterCreatedEvent.cs                            │
│  └── TransactionConfirmedEvent.cs                       │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              Sorcha.Register.Models                      │
│                  (Domain Models)                         │
├─────────────────────────────────────────────────────────┤
│  Register.cs, TransactionModel.cs, Docket.cs            │
│  PayloadModel.cs, TransactionMetaData.cs                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│          Storage Implementations                         │
├─────────────────────────────────────────────────────────┤
│  Sorcha.Register.Storage.MongoDB                         │
│  Sorcha.Register.Storage.PostgreSQL                      │
│  Sorcha.Register.Storage.InMemory                        │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Register.Models | 100% | Domain models complete |
| Register.Core | 100% | Interfaces and managers |
| Storage.MongoDB | 100% | Primary production storage |
| Storage.PostgreSQL | 90% | EF Core implementation |
| Storage.InMemory | 100% | Testing implementation |
| Register.Service | 100% | API layer complete |
| SignalR Hub | 100% | Real-time notifications |
| Event Publishing | 100% | Aspire messaging |
| Unit Tests | 100% | 112 tests passing |

### API Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/registers` | Create register | Done |
| GET | `/api/registers` | List registers | Done |
| GET | `/api/registers/{id}` | Get register | Done |
| PUT | `/api/registers/{id}` | Update register | Done |
| DELETE | `/api/registers/{id}` | Delete register | Done |
| POST | `/api/registers/{id}/transactions` | Store transaction | Done |
| GET | `/api/registers/{id}/transactions` | List transactions | Done |
| GET | `/api/registers/{id}/transactions/{txId}` | Get transaction | Done |
| GET | `/api/registers/{id}/transactions/by-sender/{address}` | By sender | Done |
| GET | `/api/registers/{id}/transactions/by-recipient/{address}` | By recipient | Done |
| POST | `/api/registers/{id}/dockets` | Create docket | Done |
| GET | `/api/registers/{id}/dockets` | List dockets | Done |
| GET | `/api/registers/{id}/dockets/{docketId}` | Get docket | Done |

## Dependencies

### Internal Dependencies

- `Sorcha.ServiceDefaults` - .NET Aspire configuration
- `Sorcha.Tenant.Abstractions` - Multi-tenant isolation

### External Dependencies

- `MongoDB.Driver` - MongoDB client
- `Microsoft.EntityFrameworkCore` - EF Core ORM
- `Microsoft.AspNetCore.SignalR` - Real-time hub
- `Microsoft.AspNetCore.OData` - OData queries

### Service Dependencies

- Validator Service - Chain validation and consensus
- Wallet Service - Address verification
- Tenant Service - Tenant context and isolation

## Migration/Integration Notes

### MongoDB Schema

```javascript
// Registers collection
{
  _id: "abc123def456...",
  name: "Supply Chain Register",
  height: 42,
  status: 1, // Online
  advertise: true,
  isFullReplica: true,
  tenantId: "tenant-123",
  createdAt: ISODate("2025-12-03T10:00:00Z"),
  updatedAt: ISODate("2025-12-03T12:00:00Z")
}

// Indexes
db.registers.createIndex({ tenantId: 1 })
db.registers.createIndex({ name: 1 })
db.registers.createIndex({ status: 1 })

// Transactions collection
{
  _id: "txId-64char-hex...",
  registerId: "abc123def456...",
  prevTxId: "prev-txId-64char...",
  version: 1,
  senderWallet: "0x123...",
  recipientsWallets: ["0x456...", "0x789..."],
  timeStamp: ISODate("2025-12-03T10:30:00Z"),
  metaData: { blueprintId: "bp-123", actionId: 2 },
  payloads: [...]
}

// Indexes
db.transactions.createIndex({ registerId: 1, txId: 1 })
db.transactions.createIndex({ registerId: 1, senderWallet: 1 })
db.transactions.createIndex({ registerId: 1, recipientsWallets: 1 })
db.transactions.createIndex({ registerId: 1, "metaData.blueprintId": 1 })
```

### Breaking Changes

- None for MVD phase
- Future: JSON-LD context URL may change

## Open Questions

1. Should we support cross-register transactions?
2. How to handle blockchain pruning for very large registers?
3. Should we add full-text search on transaction metadata?
