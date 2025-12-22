# Research: Technology Decisions for Validator Service

**Date**: 2025-12-22
**Feature**: Validator Service - Distributed Transaction Validation and Consensus
**Branch**: `002-validator-service`

## Overview

This document consolidates research findings for five key technology decisions that impact the Validator Service implementation. Each decision is evaluated based on best practices from blockchain systems, distributed consensus literature, and .NET ecosystem patterns.

---

## Decision 1: Memory Pool Persistence Strategy

### Context

The memory pool must survive service restarts (NFR-010) to prevent transaction loss. With 10,000 transactions per register and sub-500ms validation requirements, the persistence mechanism must be fast, durable, and support multi-instance scenarios.

### Options Evaluated

| Option | Pros | Cons | Performance | Complexity |
|--------|------|------|-------------|------------|
| **Redis** | Distributed, fast, pub/sub support | External dependency, memory-only | ~1ms write | Medium |
| **EF Core + SQL** | Durable, ACID guarantees, familiar | Slower, heavyweight | ~10-50ms write | High |
| **File-based (JSON)** | Simple, no dependencies | Not distributed, slower | ~5-15ms write | Low |
| **In-memory only** | Fastest | Ephemeral, no durability | 0ms (RAM) | Lowest |

### Decision: **Redis for MVP, with in-memory fallback**

**Rationale:**
1. **Performance**: Redis provides <1ms persistence with TTL support for transaction expiration
2. **Distribution**: Supports multiple validator instances sharing state (NFR-009)
3. **Pub/Sub**: Can leverage for consensus coordination (see Decision 2)
4. **Aspire Integration**: Sorcha already uses Redis via .NET Aspire (constitution compliant)
5. **Fallback**: In-memory mode for development/testing (no Redis required)

**Implementation**:
```csharp
// Sorcha.Validator.Service/Services/MemPoolManager.cs
public class MemPoolManager : IMemPoolManager
{
    private readonly IConnectionMultiplexer _redis; // Optional
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Transaction>> _inMemoryPools;

    // Use Redis if available, fall back to in-memory
    public async Task<bool> AddTransactionAsync(string registerId, Transaction tx)
    {
        if (_redis != null)
            return await AddToRedisAsync(registerId, tx);
        return AddToInMemoryPool(registerId, tx);
    }
}
```

**Alternatives Considered:**
- **EF Core**: Too slow for 100 TPS requirement; considered for future archival/audit trail
- **File-based**: Not distributed; rejected for multi-instance scenarios

---

## Decision 2: Consensus Coordination Mechanism

### Context

Validators must distribute proposed dockets, collect votes, and determine consensus within 30 seconds (NFR-003). Mechanism must handle 3-10 validators with <5s network latency.

### Options Evaluated

| Option | Pros | Cons | Latency | Complexity |
|--------|------|------|---------|------------|
| **gRPC Streaming** | Low latency, bi-directional | Complex state management | <100ms | High |
| **Peer Service Pub/Sub** | Decoupled, broadcast-friendly | Indirect, dependency on Peer | ~500ms | Medium |
| **Direct gRPC Calls** | Simple, request/response | N calls for N validators | <200ms | Low |
| **Polling** | Simple | High latency, inefficient | >1s | Low |

### Decision: **Direct gRPC calls for vote requests + Peer Service pub/sub for docket broadcast**

**Rationale:**
1. **Hybrid Approach**:
   - **Vote Collection**: Direct gRPC calls (validator → peer validator) for low-latency vote requests
   - **Docket Distribution**: Peer Service pub/sub for efficient broadcast to all validators
2. **Latency**: Direct calls provide <200ms response for votes (meets 30s consensus budget)
3. **Constitution Compliance**: Uses gRPC (required by constitution), leverages existing Peer Service
4. **Simplicity**: Request/response pattern simpler than streaming for vote collection
5. **Scalability**: Pub/sub scales better for docket distribution (1 publish → N subscribers)

**Implementation**:
```csharp
// Vote collection: Direct gRPC
var voteRequests = validators.Select(v =>
    _validatorGrpcClient.RequestVoteAsync(v.Address, proposedDocket));
var votes = await Task.WhenAll(voteRequests);

// Docket broadcast: Peer Service pub/sub
await _peerServiceClient.PublishDocketAsync(registerId, confirmedDocket);
```

**Alternatives Considered:**
- **gRPC Streaming**: Over-engineered for request/response vote pattern; reserved for future real-time block streaming
- **Polling**: Too slow; rejected due to latency requirements

---

## Decision 3: Fork Resolution Implementation

### Context

When validators create competing dockets (fork scenario), the longest-chain strategy applies. Question: how deep should chain reorganization (reorg) go?

### Options Evaluated

| Option | Pros | Cons | Security | Complexity |
|--------|------|------|----------|------------|
| **Unlimited Depth** | Maximum security, full reorg | Slow, resource-intensive | High | High |
| **Limited (10 dockets)** | Balanced, predictable | Finality delay | Medium-High | Medium |
| **Recent (3 dockets)** | Fast, simple | Lower security, shallow | Medium | Low |

### Decision: **Limited depth (10 dockets) for MVP**

**Rationale:**
1. **Finality Balance**: 10 dockets ≈ 100 seconds (at 10s/docket) provides reasonable finality window
2. **Security**: Deep enough to handle network partitions (assumption: <5s RTT, so ~6 dockets max during partition)
3. **Performance**: Bounded reorg cost (max 10 dockets to re-validate)
4. **Industry Practice**: Bitcoin considers 6 confirmations "final"; 10 dockets provides ~1.6x safety margin
5. **Upgrade Path**: Can increase depth later without breaking changes

**Implementation**:
```csharp
// Sorcha.Validator.Core/Validators/ChainValidator.cs
public const int MAX_REORG_DEPTH = 10;

public ForkResolutionResult ResolveFork(Chain localChain, Chain competingChain)
{
    var commonAncestor = FindCommonAncestor(localChain, competingChain);
    var reorgDepth = localChain.Height - commonAncestor.Height;

    if (reorgDepth > MAX_REORG_DEPTH)
        return ForkResolutionResult.RejectDeep Fork(reorgDepth);

    if (competingChain.Height > localChain.Height)
        return ForkResolutionResult.SwitchToLongerChain(competingChain);

    return ForkResolutionResult.KeepLocalChain();
}
```

**Alternatives Considered:**
- **Unlimited**: Rejected due to potential DoS (malicious long fork)
- **3 dockets**: Too shallow for network partition scenarios; considered for future "fast finality" mode

---

## Decision 4: Memory Pool Priority Scheme

### Context

Transactions have priority levels for queue management (FIFO with priority override). Question: how are priorities assigned?

### Options Evaluated

| Option | Pros | Cons | Fairness | Complexity |
|--------|------|------|----------|------------|
| **Explicit Field** | Clear, flexible | Requires client support | Depends on rules | Low |
| **Blueprint Action** | Context-aware | Couples to blueprint | Medium | Medium |
| **Gas Fee** | Economic, gaming-resistant | Requires fee mechanism | High (if fees required) | High |
| **Timestamp** | Simple | Not priority, just FIFO | Low | Lowest |

### Decision: **Explicit priority field with enum values (Low, Normal, High)**

**Rationale:**
1. **Simplicity**: Transaction model already supports metadata; add `Priority` enum field
2. **MVP-Friendly**: No fee mechanism required; can add gas fees later without breaking changes
3. **Blueprint Agnostic**: Validators don't need to interpret blueprint semantics for prioritization
4. **Client Control**: Blueprint designers decide transaction priorities (sensible default: Normal)
5. **Gaming Resistance**: Limit high-priority transactions per register (e.g., max 10% of memory pool)

**Implementation**:
```csharp
// Sorcha.Validator.Service/Models/Transaction.cs
public class Transaction
{
    public TransactionPriority Priority { get; set; } = TransactionPriority.Normal;
}

public enum TransactionPriority
{
    Low = 0,
    Normal = 1,  // Default
    High = 2     // Limited to 10% of memory pool
}

// MemPoolManager enforces high-priority quota
public bool CanAddHighPriority(string registerId)
{
    var pool = GetPool(registerId);
    var highPriorityCount = pool.Count(t => t.Priority == TransactionPriority.High);
    var totalCount = pool.Count();
    return (highPriorityCount / (double)totalCount) < 0.10; // 10% max
}
```

**Alternatives Considered:**
- **Gas Fees**: Deferred to post-MVP; explicit priority sufficient for MVD
- **Blueprint-derived**: Rejected due to tight coupling and complexity

---

## Decision 5: Hybrid Docket Trigger Implementation

### Context

Dockets build when either time threshold (default 10s) OR size threshold (default 50 transactions) is reached. Question: how to implement efficiently?

### Options Evaluated

| Option | Pros | Cons | Accuracy | Resource Usage |
|--------|------|------|----------|----------------|
| **Timer + Size Check** | Accurate, event-driven | Timer overhead | High (±10ms) | Low |
| **Background Polling** | Simple | Inefficient, delayed | Medium (±500ms) | Medium |
| **Event-Driven (Producer/Consumer)** | Efficient, reactive | Complex | High (±5ms) | Low |

### Decision: **Event-driven architecture with PeriodicTimer + ConcurrentQueue**

**Rationale:**
1. **Accuracy**: `PeriodicTimer` (.NET 6+) provides accurate interval timing with low overhead
2. **Responsiveness**: Size threshold detected immediately when transaction added
3. **Efficiency**: No polling; events trigger docket builds
4. **Testability**: Can mock timer and queue for deterministic tests
5. **.NET Idiom**: Uses modern `PeriodicTimer` and `Channel<T>` patterns

**Implementation**:
```csharp
// Sorcha.Validator.Service/Services/DocketBuilder.cs
public class DocketBuilder : BackgroundService
{
    private readonly PeriodicTimer _timer;
    private readonly MemPoolConfiguration _config;
    private readonly Channel<string> _buildRequests; // Register IDs

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(_config.TimeThreshold));

        while (await _timer.WaitForNextTickAsync(ct))
        {
            // Time threshold reached - build dockets for all registers
            foreach (var registerId in GetActiveRegisters())
                await RequestBuildAsync(registerId);
        }
    }

    // Called when size threshold reached
    public async Task OnSizeThresholdReached(string registerId)
    {
        await _buildRequests.Writer.WriteAsync(registerId);
    }
}

// Memory pool size check on transaction add
public async Task AddTransactionAsync(Transaction tx)
{
    await _memPool.AddAsync(tx);

    if (_memPool.GetCount(tx.RegisterId) >= _config.SizeThreshold)
        await _docketBuilder.OnSizeThresholdReached(tx.RegisterId);
}
```

**Alternatives Considered:**
- **Background Polling**: Rejected due to accuracy concerns (±500ms too coarse for 100 TPS)
- **Timer-only**: Rejected; doesn't handle size threshold

---

## Summary of Decisions

| Decision | Choice | Key Benefit |
|----------|--------|-------------|
| **Memory Pool Persistence** | Redis + in-memory fallback | <1ms persistence, distributed, Aspire-integrated |
| **Consensus Coordination** | Direct gRPC (votes) + Peer pub/sub (dockets) | <200ms latency, simple, constitution-compliant |
| **Fork Resolution Depth** | 10 dockets | Balanced security/performance, industry-aligned |
| **Transaction Priority** | Explicit enum field (Low/Normal/High) | Simple, flexible, no gas fees required |
| **Hybrid Docket Trigger** | Event-driven (PeriodicTimer + Channel) | Accurate, efficient, testable |

---

## Implementation Checklist

- [ ] Add Redis connection in `appsettings.json` with fallback to in-memory
- [ ] Implement `MemPoolManager` with Redis and in-memory backends
- [ ] Create gRPC service definition for vote requests in `validator.proto`
- [ ] Implement `ConsensusEngine` with direct gRPC vote collection
- [ ] Implement `ChainValidator` with `MAX_REORG_DEPTH = 10`
- [ ] Add `TransactionPriority` enum to Transaction model
- [ ] Implement priority quota enforcement in `MemPoolManager`
- [ ] Create `DocketBuilder` as `BackgroundService` with `PeriodicTimer`
- [ ] Wire up size threshold detection in memory pool add method
- [ ] Add configuration models for all thresholds (time, size, reorg depth)

---

**Research Complete**: All technology decisions resolved. Proceed to Phase 1 (Design Artifacts).
