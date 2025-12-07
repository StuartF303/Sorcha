# Storage Abstraction - Developer Quickstart

**Feature:** Multi-tier Storage Abstraction Layer
**Spec:** [spec.md](./spec.md)
**Plan:** [plan.md](./plan.md)

---

## Overview

The Storage Abstraction Layer provides a unified interface for multi-tier data persistence across Sorcha services. It implements a three-tier architecture (Hot/Warm/Cold) with cryptographic verification for the Register Service.

## Quick Reference

| Tier | Interface | Provider | Use Case |
|------|-----------|----------|----------|
| Hot | `ICacheStore` | Redis, InMemory | Session data, rate limiting, ephemeral cache |
| Warm | `IRepository<T,TId>` | EF Core (PostgreSQL) | Relational data with ACID transactions |
| Warm | `IDocumentStore<T,TId>` | MongoDB | Flexible schema documents |
| Cold | `IWormStore<T,TId>` | MongoDB | Immutable ledger data (append-only) |
| Register | `IVerifiedRegisterCache` | Custom | Cryptographically verified docket cache |

---

## Getting Started

### 1. Add Package References

```xml
<!-- Core abstractions (all services) -->
<PackageReference Include="Sorcha.Storage.Abstractions" />

<!-- Choose implementations based on needs -->
<PackageReference Include="Sorcha.Storage.InMemory" />  <!-- Testing -->
<PackageReference Include="Sorcha.Storage.Redis" />     <!-- Hot tier -->
<PackageReference Include="Sorcha.Storage.EFCore" />    <!-- Warm relational -->
<PackageReference Include="Sorcha.Storage.MongoDB" />   <!-- Warm docs + Cold WORM -->
```

### 2. Configure Services

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add storage configuration
builder.Services.AddStorageAbstraction(options =>
{
    options.HotTier.Provider = CacheProvider.Redis;
    options.HotTier.ConnectionString = builder.Configuration.GetConnectionString("Redis");
    options.HotTier.DefaultExpiration = TimeSpan.FromMinutes(15);

    options.WarmTier.RelationalProvider = RelationalProvider.PostgreSQL;
    options.WarmTier.RelationalConnectionString = builder.Configuration.GetConnectionString("PostgreSQL");

    options.WarmTier.DocumentProvider = DocumentProvider.MongoDB;
    options.WarmTier.DocumentConnectionString = builder.Configuration.GetConnectionString("MongoDB");

    options.ColdTier.Provider = WormProvider.MongoDB;
    options.ColdTier.ConnectionString = builder.Configuration.GetConnectionString("MongoDB");
    options.ColdTier.Database = "sorcha_ledger";
});
```

### 3. Inject and Use

```csharp
public class MyService
{
    private readonly ICacheStore _cache;
    private readonly IRepository<Wallet, Guid> _walletRepo;
    private readonly IDocumentStore<Blueprint, string> _blueprintStore;

    public MyService(
        ICacheStore cache,
        IRepository<Wallet, Guid> walletRepo,
        IDocumentStore<Blueprint, string> blueprintStore)
    {
        _cache = cache;
        _walletRepo = walletRepo;
        _blueprintStore = blueprintStore;
    }

    public async Task<Wallet?> GetWalletAsync(Guid id)
    {
        // Try cache first (cache-aside pattern)
        var cacheKey = $"wallet:{id}";
        return await _cache.GetOrSetAsync(
            cacheKey,
            async ct => await _walletRepo.GetByIdAsync(id, ct),
            TimeSpan.FromMinutes(10));
    }
}
```

---

## Tier-Specific Patterns

### Hot Tier (ICacheStore)

```csharp
// Simple get/set
await _cache.SetAsync("key", myObject, TimeSpan.FromMinutes(5));
var value = await _cache.GetAsync<MyType>("key");

// Cache-aside pattern (recommended)
var result = await _cache.GetOrSetAsync(
    "expensive-query",
    async ct => await _database.RunExpensiveQuery(ct),
    TimeSpan.FromHours(1));

// Rate limiting
var count = await _cache.IncrementAsync(
    $"rate:{userId}:{DateTime.UtcNow:yyyyMMddHH}",
    delta: 1,
    expiration: TimeSpan.FromHours(1));

if (count > 100)
    throw new RateLimitExceededException();

// Pattern-based invalidation
await _cache.RemoveByPatternAsync("user:123:*");
```

### Warm Tier - Relational (IRepository)

```csharp
// CRUD operations
var wallet = await _walletRepo.AddAsync(new Wallet { Name = "My Wallet" });
var found = await _walletRepo.GetByIdAsync(wallet.Id);
wallet.Name = "Updated Name";
await _walletRepo.UpdateAsync(wallet);
await _walletRepo.SaveChangesAsync(); // Unit of work commit

// Query with predicate
var activeWallets = await _walletRepo.QueryAsync(
    w => w.IsActive && w.TenantId == tenantId);

// Pagination
var page = await _walletRepo.GetPagedAsync(
    page: 1,
    pageSize: 20,
    predicate: w => w.TenantId == tenantId);
```

### Warm Tier - Documents (IDocumentStore)

```csharp
// Flexible schema storage
var blueprint = new Blueprint
{
    Id = Guid.NewGuid().ToString(),
    Name = "My Blueprint",
    Definition = new { /* complex nested structure */ }
};

await _blueprintStore.InsertAsync(blueprint);

// Query with expressions
var myBlueprints = await _blueprintStore.QueryAsync(
    b => b.OwnerId == userId,
    limit: 50);

// Upsert pattern
await _blueprintStore.UpsertAsync(blueprint.Id, blueprint);
```

### Cold Tier - WORM (IWormStore)

```csharp
// Append-only - NO update or delete methods exist
var docket = new DocketDocument
{
    Height = 42,
    Hash = ComputeHash(transactions),
    PreviousHash = previousDocket.Hash,
    Transactions = transactions
};

// Append is the only write operation
await _wormStore.AppendAsync(docket);

// Batch append for efficiency
await _wormStore.AppendBatchAsync(dockets);

// Read operations
var docket = await _wormStore.GetAsync(42UL);
var range = await _wormStore.GetRangeAsync(startId: 1UL, endId: 100UL);

// Integrity verification
var result = await _wormStore.VerifyIntegrityAsync(startId: 1UL, endId: 1000UL);
if (!result.IsValid)
{
    foreach (var violation in result.Violations)
    {
        _logger.LogError("Integrity violation: {Type} at {Id}",
            violation.ViolationType, violation.DocumentId);
    }
}
```

### Register Verified Cache (IVerifiedRegisterCache)

```csharp
// Initialize cache on startup (verifies from cold storage)
var initResult = await _cache.InitializeAsync(registerId);
if (initResult.HasCorruption)
{
    _logger.LogWarning("Corruption detected in {Count} ranges",
        initResult.CorruptedRanges.Length);
    // Automatic peer recovery will be triggered
}

// All reads go through verified cache - NEVER direct to cold storage
var docket = await _cache.GetDocketAsync(registerId, height: 42);
var tx = await _cache.GetTransactionAsync(registerId, txId);

// Query verified transactions only
var userTx = await _cache.QueryTransactionsAsync(
    registerId,
    t => t.SenderWallet == walletAddress,
    limit: 100);

// Add new data (verification happens automatically)
var result = await _cache.AddVerifiedDocketAsync(docket, transactions);
if (!result.IsValid)
{
    _logger.LogError("Verification failed: {Error}", result.ErrorMessage);
}

// Check operational state
var state = await _cache.GetOperationalStateAsync(registerId);
switch (state.State)
{
    case RegisterState.Healthy:
        // Normal operation
        break;
    case RegisterState.Degraded:
        // Some data unavailable, partial service
        break;
    case RegisterState.Recovering:
        // Active peer sync in progress
        break;
}

// Get corruption ranges for monitoring
var corrupted = await _cache.GetCorruptedRangesAsync(registerId);
foreach (var range in corrupted)
{
    _logger.LogWarning("Corrupted range {Start}-{End}: {Type}",
        range.StartHeight, range.EndHeight, range.Type);
}
```

---

## Configuration

### appsettings.json

```json
{
  "Storage": {
    "HotTier": {
      "Provider": "Redis",
      "ConnectionString": "localhost:6379",
      "DefaultExpiration": "00:15:00",
      "CircuitBreaker": {
        "FailureThreshold": 5,
        "SamplingDuration": "00:01:00",
        "BreakDuration": "00:00:30"
      }
    },
    "WarmTier": {
      "Relational": {
        "Provider": "PostgreSQL",
        "ConnectionString": "Host=localhost;Database=sorcha;Username=app;Password=secret"
      },
      "Document": {
        "Provider": "MongoDB",
        "ConnectionString": "mongodb://localhost:27017",
        "Database": "sorcha_docs"
      }
    },
    "ColdTier": {
      "Provider": "MongoDB",
      "ConnectionString": "mongodb://localhost:27017",
      "Database": "sorcha_ledger",
      "Collection": "dockets"
    },
    "RegisterCache": {
      "StartupStrategy": "ProgressiveWithThreshold",
      "BlockingThreshold": 1000,
      "VerificationBatchSize": 100,
      "MaxRecoveryAttempts": 3
    },
    "Observability": {
      "HotTier": "Metrics",
      "WarmTier": "StructuredLogging",
      "ColdTier": "FullTracing"
    }
  }
}
```

### Environment Variable Overrides

```bash
# Hot tier
Storage__HotTier__ConnectionString=redis-cluster:6379

# Warm tier relational
Storage__WarmTier__Relational__ConnectionString=Host=db;Database=sorcha

# Cold tier
Storage__ColdTier__ConnectionString=mongodb://mongo-cluster:27017
```

---

## Testing

### In-Memory Implementations

```csharp
// Use in-memory for unit tests
services.AddStorageAbstraction(options =>
{
    options.UseInMemoryProviders();
});

// Or inject directly
var cache = new InMemoryCacheStore();
var repo = new InMemoryRepository<Wallet, Guid>();
var worm = new InMemoryWormStore<DocketDocument, ulong>();
```

### Integration Tests with Testcontainers

```csharp
public class StorageIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder().Build();
    private readonly MongoDbContainer _mongo = new MongoDbBuilder().Build();

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        await _mongo.StartAsync();
    }

    [Fact]
    public async Task CacheStore_ShouldPersistAndRetrieve()
    {
        var cache = new RedisCacheStore(_redis.GetConnectionString());

        await cache.SetAsync("test", new { Value = 42 });
        var result = await cache.GetAsync<dynamic>("test");

        Assert.Equal(42, result.Value);
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync();
        await _mongo.DisposeAsync();
    }
}
```

---

## Observability

### Metrics (OpenTelemetry)

```csharp
// Metrics are automatically emitted:
// - sorcha_cache_hits_total
// - sorcha_cache_misses_total
// - sorcha_cache_latency_ms
// - sorcha_repository_operations_total
// - sorcha_worm_appends_total
// - sorcha_verification_duration_ms
// - sorcha_corruption_ranges_count
```

### Structured Logging

```csharp
// Logs include structured context:
// - OperationType
// - TierName
// - Duration
// - Success/Failure
// - TenantId (for multi-tenant operations)
```

### Tracing

```csharp
// Distributed traces span across tiers:
// [HTTP Request]
//   └── [Cache Lookup] (cache miss)
//       └── [Repository Query]
//           └── [Cache Set]
```

---

## Common Patterns

### Multi-tenant Isolation

```csharp
// Tenant context is automatically applied
public class TenantAwareRepository<T, TId> : IRepository<T, TId>
{
    private readonly ITenantContext _tenantContext;

    // All queries automatically filtered by tenant
}
```

### Cache Invalidation

```csharp
// Invalidate on write
await _walletRepo.UpdateAsync(wallet);
await _cache.RemoveAsync($"wallet:{wallet.Id}");

// Or use pattern invalidation
await _cache.RemoveByPatternAsync($"wallet:{wallet.Id}:*");
```

### Graceful Degradation

```csharp
// Cache failures fall through to database
try
{
    return await _cache.GetAsync<Wallet>(key);
}
catch (CacheException)
{
    _logger.LogWarning("Cache unavailable, falling to database");
    return await _walletRepo.GetByIdAsync(id);
}
```

---

## Troubleshooting

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Cache always misses | Redis connection failed | Check circuit breaker state, verify connection string |
| Slow verification | Large docket count | Increase batch size or use progressive strategy |
| Corruption detected | Storage corruption or tampering | Check cold storage integrity, trigger peer recovery |
| RegisterState.Offline | No peers available for recovery | Check peer network connectivity |

---

## Next Steps

1. Review [data-model.md](./data-model.md) for entity definitions
2. Review [contracts/](./contracts/) for interface specifications
3. Review [research.md](./research.md) for technical decisions
4. Check [spec.md](./spec.md) for full requirements
