# Data Model: Multi-Tier Storage Abstraction Layer

**Feature**: 002-storage-abstraction
**Date**: 2025-12-07
**Status**: Draft

## Overview

This document defines the data entities for the storage abstraction layer, including configuration models, cache entries, and Register Service verified cache entities.

---

## 1. Configuration Entities

### StorageConfiguration

Root configuration for all storage tiers.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Hot | HotTierConfiguration | No | Hot tier (cache) configuration |
| Warm | WarmTierConfiguration | No | Warm tier (operational) configuration |
| Cold | ColdTierConfiguration | No | Cold tier (WORM) configuration |
| Register | RegisterCacheConfiguration | No | Register Service verified cache configuration |

### HotTierConfiguration

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| Provider | string | Yes | - | Provider name: "Redis", "InMemory" |
| Redis | RedisConfiguration | No | - | Redis-specific settings |
| DefaultTtlSeconds | int | No | 900 | Default TTL for cache entries |
| Observability | ObservabilityConfiguration | No | - | Observability settings |

### RedisConfiguration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| ConnectionString | string | Yes | Redis connection string |
| InstanceName | string | No | Key prefix (default: "sorcha:") |
| ConnectTimeout | int | No | Connection timeout in ms |
| SyncTimeout | int | No | Synchronous operation timeout in ms |

### WarmTierConfiguration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Relational | RelationalConfiguration | No | Relational database settings |
| Documents | DocumentConfiguration | No | Document database settings |
| Observability | ObservabilityConfiguration | No | Observability settings |

### RelationalConfiguration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Provider | string | Yes | Provider name: "PostgreSQL", "InMemory" |
| ConnectionString | string | Yes | Database connection string |

### DocumentConfiguration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Provider | string | Yes | Provider name: "MongoDB", "InMemory" |
| ConnectionString | string | Yes | MongoDB connection string |
| DatabaseName | string | Yes | Database name |

### ColdTierConfiguration

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Provider | string | Yes | Provider name: "MongoDB", "InMemory" |
| ConnectionString | string | Yes | MongoDB connection string |
| DatabaseName | string | Yes | Database name for ledger data |
| Observability | ObservabilityConfiguration | No | Observability settings |

### ObservabilityConfiguration

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| Level | string | No | "Metrics" | Level: "None", "Metrics", "StructuredLogging", "FullTracing" |
| MetricsEnabled | bool | No | true | Emit metrics |
| TracingEnabled | bool | No | false | Emit trace spans |
| LoggingEnabled | bool | No | true | Emit structured logs |

### RegisterCacheConfiguration

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| StartupStrategy | string | No | "Blocking" | Strategy: "Blocking", "Progressive" |
| BlockingThreshold | int | No | 1000 | Docket count threshold for auto-progressive |
| VerificationBatchSize | int | No | 100 | Batch size for progressive verification |
| VerificationParallelism | int | No | 4 | Parallel verification workers |

---

## 2. Cache Entities

### CacheEntry&lt;T&gt;

Generic wrapper for cached values with metadata.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Key | string | Yes | Cache key |
| Value | T | Yes | Cached value |
| CreatedAt | DateTime | Yes | When entry was created (UTC) |
| ExpiresAt | DateTime? | No | When entry expires (UTC), null = no expiration |
| SlidingExpiration | TimeSpan? | No | Sliding expiration window |
| Tags | string[] | No | Tags for bulk invalidation |

### CacheStatistics

Statistics for cache operations.

| Field | Type | Description |
|-------|------|-------------|
| TotalRequests | long | Total cache requests |
| Hits | long | Cache hits |
| Misses | long | Cache misses |
| HitRate | double | Computed: Hits / TotalRequests |
| AverageLatencyMs | double | Average operation latency |
| P99LatencyMs | double | 99th percentile latency |
| CurrentEntryCount | long | Current number of entries |
| EvictionCount | long | Total evictions |

---

## 3. Repository Entities

### PagedResult&lt;T&gt;

Pagination wrapper for large result sets.

| Field | Type | Description |
|-------|------|-------------|
| Items | IEnumerable&lt;T&gt; | Page items |
| TotalCount | int | Total item count across all pages |
| Page | int | Current page number (1-based) |
| PageSize | int | Items per page |
| TotalPages | int | Computed: ceil(TotalCount / PageSize) |
| HasNextPage | bool | Computed: Page < TotalPages |
| HasPreviousPage | bool | Computed: Page > 1 |

### QueryOptions

Options for repository queries.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| Skip | int | No | 0 | Records to skip |
| Take | int | No | 100 | Records to take (max 1000) |
| OrderBy | string | No | null | Property to order by |
| OrderDescending | bool | No | false | Order direction |
| IncludeDeleted | bool | No | false | Include soft-deleted records |

---

## 4. Register Service Verified Cache Entities

### VerifiedDocket

A cryptographically verified docket in the cache.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| RegisterId | string | Yes | Parent register ID |
| Height | ulong | Yes | Docket height (block number) |
| Hash | string | Yes | SHA-256 hash of docket contents |
| PreviousHash | string | Yes | Hash of previous docket |
| TransactionIds | string[] | Yes | Transaction IDs in this docket |
| Timestamp | DateTime | Yes | When docket was sealed (UTC) |
| VerificationStatus | DocketVerificationStatus | Yes | Binary: Verified or Corrupted |
| VerifiedAt | DateTime | Yes | When verification completed (UTC) |
| CorruptionDetails | string? | No | Details if corrupted |

### DocketVerificationStatus (Enum)

Binary verification status - immutable once set.

| Value | Description |
|-------|-------------|
| Verified | Docket passed all verification checks |
| Corrupted | Docket failed verification |

### RegisterOperationalState

Operational status of a register with lifecycle management.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| RegisterId | string | Yes | Register ID |
| State | RegisterState | Yes | Current operational state |
| VerifiedHeight | ulong | Yes | Highest verified docket height |
| TotalHeight | ulong | Yes | Total docket count in cold storage |
| CorruptedRanges | CorruptionRange[] | No | Ranges requiring recovery |
| LastStateChange | DateTime | Yes | When state last changed (UTC) |
| Message | string? | No | Human-readable status message |

### RegisterState (Enum)

Operational states for a register.

| Value | Description |
|-------|-------------|
| Initializing | Cache is being populated from cold storage |
| Healthy | Fully verified, serving all requests |
| Degraded | Some corruption detected, partial service |
| Recovering | Actively syncing from peer network |
| PeerSyncInProgress | Fetching specific ranges from peers |
| Offline | Not serving requests |

### CorruptionRange

Represents a range of dockets that failed verification.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| RegisterId | string | Yes | Parent register ID |
| StartHeight | ulong | Yes | First corrupted docket height |
| EndHeight | ulong | Yes | Last corrupted docket height |
| Type | CorruptionType | Yes | Type of corruption detected |
| Details | string | Yes | Description of the corruption |
| DetectedAt | DateTime | Yes | When corruption was detected (UTC) |
| RecoveryAttempts | int | No | Number of recovery attempts |
| LastRecoveryAttempt | DateTime? | No | Last recovery attempt time |
| RecoveryStatus | RecoveryStatus | No | Current recovery status |

### CorruptionType (Enum)

Types of corruption detected during verification.

| Value | Description |
|-------|-------------|
| InvalidDocketHash | Docket hash doesn't match computed hash |
| BrokenChainLink | Previous hash doesn't match actual previous docket |
| InvalidTransactionSignature | Transaction signature verification failed |
| MissingData | Docket or transaction missing from storage |
| MalformedData | Data format/schema invalid |

### RecoveryStatus (Enum)

Status of corruption recovery.

| Value | Description |
|-------|-------------|
| Pending | Recovery not yet attempted |
| InProgress | Currently fetching from peers |
| Succeeded | Recovery completed successfully |
| Failed | Recovery failed after max attempts |
| Abandoned | No peers could provide valid data |

### CacheInitializationResult

Result of loading and verifying cold storage data on startup.

| Field | Type | Description |
|-------|------|-------------|
| RegisterId | string | Register ID |
| VerifiedDocketCount | int | Number of successfully verified dockets |
| TotalDocketCount | int | Total dockets in cold storage |
| CorruptedRanges | CorruptionRange[] | Ranges that failed verification |
| LoadDuration | TimeSpan | Time taken to load from cold storage |
| VerificationDuration | TimeSpan | Time taken to verify all dockets |
| ResultingState | RegisterState | Operational state after initialization |
| HasCorruption | bool | Computed: CorruptedRanges.Any() |
| IsFullyVerified | bool | Computed: !HasCorruption |
| VerificationRate | double | Computed: VerifiedDocketCount / VerificationDuration.TotalSeconds |

---

## 5. Health & Status Entities

### StorageHealthStatus

Overall health status for storage layer.

| Field | Type | Description |
|-------|------|-------------|
| IsHealthy | bool | Overall health status |
| HotTier | TierHealthStatus | Hot tier health |
| WarmTier | TierHealthStatus | Warm tier health |
| ColdTier | TierHealthStatus | Cold tier health |
| Timestamp | DateTime | When status was checked |

### TierHealthStatus

Health status for a single storage tier.

| Field | Type | Description |
|-------|------|-------------|
| IsHealthy | bool | Tier health status |
| Provider | string | Provider name |
| ResponseTimeMs | double | Last probe response time |
| ErrorMessage | string? | Error if unhealthy |
| CircuitState | string | Circuit breaker state: Closed, Open, HalfOpen |

---

## Entity Relationships

```
StorageConfiguration
├── HotTierConfiguration
│   ├── RedisConfiguration
│   └── ObservabilityConfiguration
├── WarmTierConfiguration
│   ├── RelationalConfiguration
│   ├── DocumentConfiguration
│   └── ObservabilityConfiguration
├── ColdTierConfiguration
│   └── ObservabilityConfiguration
└── RegisterCacheConfiguration

RegisterOperationalState
├── RegisterState (enum)
└── CorruptionRange[]
    ├── CorruptionType (enum)
    └── RecoveryStatus (enum)

CacheInitializationResult
├── CorruptionRange[]
└── RegisterState (enum)

VerifiedDocket
└── DocketVerificationStatus (enum)
```

---

## Validation Rules

### StorageConfiguration
- At least one tier must be configured
- ConnectionStrings must not be empty when provider is not "InMemory"

### RegisterCacheConfiguration
- BlockingThreshold must be > 0
- VerificationBatchSize must be between 10 and 1000
- VerificationParallelism must be between 1 and 16

### CorruptionRange
- EndHeight must be >= StartHeight
- StartHeight must be >= 0

### CacheEntry
- Key must not be empty
- ExpiresAt must be in the future if set
- SlidingExpiration must be positive if set

---

## State Transitions

### RegisterState Transitions

```
[Initializing] ──────────────────────────────────────┐
      │                                              │
      │ (all verified)                               │ (corruption found)
      ▼                                              ▼
  [Healthy] ◄────────────────────────────────── [Degraded]
      │                                              │
      │ (corruption detected)                        │ (recovery started)
      ▼                                              ▼
  [Degraded] ───────────────────────────────► [Recovering]
      │                                              │
      │ (recovery started)                           │ (sync in progress)
      ▼                                              ▼
  [Recovering] ◄──────────────────────────── [PeerSyncInProgress]
      │                                              │
      │ (recovery complete)                          │ (recovery complete)
      ▼                                              ▼
  [Healthy] ◄────────────────────────────────────────┘
      │
      │ (shutdown/error)
      ▼
  [Offline]
```

### RecoveryStatus Transitions

```
[Pending] ──► [InProgress] ──► [Succeeded]
                   │
                   ├──► [Failed] ──► [InProgress] (retry)
                   │
                   └──► [Abandoned]
```
