# Research: Multi-Tier Storage Abstraction Layer

**Feature**: 002-storage-abstraction
**Date**: 2025-12-07
**Status**: Complete

## Overview

This document captures research decisions for implementing the multi-tier storage abstraction layer. All NEEDS CLARIFICATION items from the technical context have been resolved.

---

## 1. Redis Hot Tier Implementation

### Decision
Use **StackExchange.Redis** with .NET Aspire integration for hot tier caching.

### Rationale
- Industry standard Redis client for .NET with excellent performance
- Native .NET Aspire support via `Aspire.StackExchange.Redis`
- Built-in connection pooling and multiplexing
- Supports all required operations: GET/SET with TTL, INCR, pattern-based DEL

### Alternatives Considered
| Alternative | Why Rejected |
|-------------|--------------|
| Microsoft.Extensions.Caching.StackExchangeRedis | Lower-level, less feature-rich |
| ServiceStack.Redis | Commercial licensing complexity |
| StackExchange.Redis.Extensions | Unnecessary abstraction layer |

### Implementation Notes
```csharp
// Key naming convention
"{service}:{entity}:{id}" // e.g., "wallet:metadata:ws1abc123"

// TTL defaults
- Hot cache entries: 15 minutes (configurable)
- JWKS cache: 1 hour
- Rate limit counters: 1 minute sliding window

// Serialization
- Use System.Text.Json for all serialization
- Register custom converters for Guid, DateTime
```

---

## 2. EF Core Multi-Tenancy Pattern

### Decision
Use **schema-based isolation** with dynamic schema selection per request.

### Rationale
- Already implemented in Tenant Service (`TenantDbContext`)
- Strong isolation between tenants
- PostgreSQL supports schema-based multi-tenancy natively
- Allows per-tenant migrations if needed

### Alternatives Considered
| Alternative | Why Rejected |
|-------------|--------------|
| Database-per-tenant | Operational complexity, connection pool exhaustion |
| Discriminator column | Weaker isolation, query performance concerns |
| Row-level security | Complex to implement, PostgreSQL-specific |

### Implementation Notes
```csharp
// Schema selection pattern (already in TenantDbContext)
public TenantDbContext(DbContextOptions<TenantDbContext> options, string schema = "public")
{
    _currentSchema = schema;
}

// Connection string per-tenant (for future scaling)
// Currently: single database, multiple schemas
// Future: dedicated databases for large tenants
```

---

## 3. MongoDB WORM Pattern

### Decision
Use **MongoDB with immutability enforcement at application layer** plus database-level schema validation.

### Rationale
- MongoDB doesn't have native WORM support, but can enforce via:
  1. Application-layer interface that only exposes Append/Get (no Update/Delete)
  2. MongoDB schema validator requiring `sealedAt` and `hash` fields
  3. Unique index on `hash` to prevent duplicate/tampered documents
- MongoDB Change Streams can audit any attempted modifications

### Alternatives Considered
| Alternative | Why Rejected |
|-------------|--------------|
| PostgreSQL with triggers | Less native for document storage, complex triggers |
| Azure Cosmos DB immutable store | Cloud-specific, not portable |
| Dedicated WORM storage (AWS S3 Glacier) | Latency too high for active queries |
| Custom append-only log files | Lacks query capabilities |

### Implementation Notes
```javascript
// MongoDB collection setup
db.createCollection("dockets", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["_id", "registerId", "height", "sealedAt", "hash", "previousHash"],
      properties: {
        sealedAt: { bsonType: "date" },
        hash: { bsonType: "string", minLength: 64, maxLength: 64 }
      }
    }
  }
});

// Indexes
db.dockets.createIndex({ "hash": 1 }, { unique: true });
db.dockets.createIndex({ "registerId": 1, "height": 1 }, { unique: true });
```

```csharp
// Application-layer enforcement
public class MongoWormStore<TDocument, TId> : IWormStore<TDocument, TId>
{
    // Only these methods exposed - no Update, no Delete
    public Task<TDocument> AppendAsync(TDocument document, ...);
    public Task AppendBatchAsync(IEnumerable<TDocument> documents, ...);
    public Task<TDocument?> GetAsync(TId id, ...);
    public Task<IEnumerable<TDocument>> QueryAsync(...);
}
```

---

## 4. OpenTelemetry Storage Instrumentation

### Decision
Use **OpenTelemetry.Instrumentation** packages with custom enrichment for storage-specific metrics.

### Rationale
- OpenTelemetry is the project standard (Constitution VIII)
- Native instrumentation available for:
  - StackExchange.Redis (`OpenTelemetry.Instrumentation.StackExchangeRedis`)
  - EF Core (`OpenTelemetry.Instrumentation.EntityFrameworkCore`) - Note: requires .NET 10 preview support
  - HTTP clients for MongoDB
- Custom metrics needed for domain-specific measurements (cache hit rate, verification time)

### Alternatives Considered
| Alternative | Why Rejected |
|-------------|--------------|
| Application Insights SDK | Azure-specific, not portable |
| Prometheus client directly | Less integrated than OpenTelemetry |
| Custom logging only | Loses distributed tracing benefits |

### Implementation Notes
```csharp
// Metrics to emit
- sorcha.storage.cache.hit_rate (gauge)
- sorcha.storage.cache.operation_duration (histogram)
- sorcha.storage.warm.query_duration (histogram)
- sorcha.storage.cold.append_duration (histogram)
- sorcha.storage.register.verification_duration (histogram)
- sorcha.storage.register.corruption_detected (counter)

// Trace spans
- "storage.cache.get", "storage.cache.set"
- "storage.warm.query", "storage.warm.save"
- "storage.cold.append", "storage.cold.verify"
- "register.cache.initialize", "register.cache.verify_docket"

// Per-tier configuration
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddRedisInstrumentation(); // Hot tier
        builder.AddEntityFrameworkCoreInstrumentation(); // Warm tier
        builder.AddSource("Sorcha.Storage.MongoDB"); // Cold tier - custom
        builder.AddSource("Sorcha.Register.VerifiedCache"); // Register-specific
    });
```

---

## 5. Graceful Degradation Pattern

### Decision
Use **Polly** for circuit breaker and retry policies with fallback to direct storage access.

### Rationale
- Polly is the .NET standard for resilience patterns
- Integrates with `Microsoft.Extensions.Http.Resilience` for HTTP-based storage
- Circuit breaker prevents cascade failures when cache is down
- Fallback pattern allows warm storage to serve requests directly

### Alternatives Considered
| Alternative | Why Rejected |
|-------------|--------------|
| Custom retry logic | Reinventing the wheel, error-prone |
| Steeltoe Circuit Breaker | Less active development than Polly |
| No resilience (fail fast) | Poor user experience during transient failures |

### Implementation Notes
```csharp
// Circuit breaker configuration
var circuitBreakerPolicy = Policy
    .Handle<RedisConnectionException>()
    .Or<TimeoutException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (ex, duration) => logger.LogWarning("Cache circuit opened for {Duration}", duration),
        onReset: () => logger.LogInformation("Cache circuit closed"));

// Fallback pattern
var fallbackPolicy = Policy<T?>
    .Handle<RedisConnectionException>()
    .FallbackAsync(
        fallbackAction: async (ctx, ct) => await _warmStore.GetByIdAsync(id, ct),
        onFallbackAsync: async (result, ctx) =>
            logger.LogWarning("Cache fallback to warm storage for {Operation}", ctx.OperationKey));

// Combined policy
var resiliencePolicy = Policy.WrapAsync(fallbackPolicy, circuitBreakerPolicy, retryPolicy);
```

---

## 6. Verified Cache Cryptographic Verification

### Decision
Use existing **Sorcha.Cryptography** module for signature verification with SHA-256 for hash chain validation.

### Rationale
- Sorcha.Cryptography already implements ED25519, NIST P-256, RSA-4096 signature verification
- SHA-256 is used for docket hash computation (existing `Docket.Hash` field)
- No external dependencies needed - reuse existing infrastructure

### Implementation Notes
```csharp
// Verification flow
public async Task<VerificationResult> VerifyDocketAsync(Docket docket, IEnumerable<TransactionModel> transactions)
{
    // 1. Verify docket hash
    var computedHash = _hashProvider.ComputeHash(SerializeDocket(docket), HashType.SHA256);
    if (Convert.ToHexString(computedHash) != docket.Hash)
        return VerificationResult.Invalid(CorruptionType.InvalidDocketHash);

    // 2. Verify chain link
    var previousDocket = await GetDocketAsync(docket.RegisterId, docket.Id - 1);
    if (previousDocket != null && previousDocket.Hash != docket.PreviousHash)
        return VerificationResult.Invalid(CorruptionType.BrokenChainLink);

    // 3. Verify each transaction signature
    foreach (var tx in transactions)
    {
        var isValid = await _cryptoModule.VerifyAsync(
            tx.Signature,
            SerializeForSigning(tx),
            tx.SenderWallet);
        if (!isValid)
            return VerificationResult.Invalid(CorruptionType.InvalidTransactionSignature, tx.TxId);
    }

    return VerificationResult.Valid();
}
```

---

## 7. Configuration Structure

### Decision
Use **hierarchical configuration** in appsettings.json with provider-specific sections.

### Rationale
- Follows .NET configuration patterns
- Supports environment variable overrides for secrets
- Clear separation between tiers

### Implementation Notes
```json
{
  "Storage": {
    "Hot": {
      "Provider": "Redis",
      "Redis": {
        "ConnectionString": "${REDIS_CONNECTION_STRING}",
        "InstanceName": "sorcha:",
        "DefaultTtlSeconds": 900
      },
      "Observability": {
        "Level": "Metrics"
      }
    },
    "Warm": {
      "Relational": {
        "Provider": "PostgreSQL",
        "ConnectionString": "${POSTGRES_CONNECTION_STRING}"
      },
      "Documents": {
        "Provider": "MongoDB",
        "ConnectionString": "${MONGODB_CONNECTION_STRING}",
        "DatabaseName": "sorcha"
      },
      "Observability": {
        "Level": "StructuredLogging"
      }
    },
    "Cold": {
      "Provider": "MongoDB",
      "ConnectionString": "${MONGODB_CONNECTION_STRING}",
      "DatabaseName": "sorcha_ledger",
      "Observability": {
        "Level": "FullTracing"
      }
    },
    "Register": {
      "StartupStrategy": "Progressive",
      "BlockingThreshold": 1000,
      "VerificationBatchSize": 100
    }
  }
}
```

---

## Summary

All technical decisions have been made and documented. No NEEDS CLARIFICATION items remain.

| Topic | Decision | Confidence |
|-------|----------|------------|
| Redis Client | StackExchange.Redis | High |
| Multi-tenancy | Schema-based isolation | High |
| WORM Enforcement | Application-layer + MongoDB validation | Medium-High |
| Observability | OpenTelemetry with custom metrics | High |
| Resilience | Polly circuit breaker + fallback | High |
| Cryptography | Existing Sorcha.Cryptography | High |
| Configuration | Hierarchical appsettings.json | High |

Ready to proceed to Phase 1: Design & Contracts.
