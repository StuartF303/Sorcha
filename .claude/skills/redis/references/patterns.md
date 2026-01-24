# Redis Patterns Reference

## Contents
- Connection Management
- Caching Patterns
- Key Naming Conventions
- Resilience Patterns
- Anti-Patterns

---

## Connection Management

### Singleton ConnectionMultiplexer

StackExchange.Redis uses a single multiplexed connection. **NEVER create multiple instances.**

```csharp
// GOOD - Singleton registration in DI
services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = ConfigurationOptions.Parse(connectionString);
    options.AbortOnConnectFail = false;  // Graceful startup
    options.ConnectRetry = 3;
    options.ConnectTimeout = 5000;
    options.SyncTimeout = 5000;
    return ConnectionMultiplexer.Connect(options);
});
```

```csharp
// BAD - Creating new connections per request
public class BadService
{
    public async Task DoSomethingAsync()
    {
        using var redis = ConnectionMultiplexer.Connect("localhost"); // WRONG!
        var db = redis.GetDatabase();
        // ...
    }
}
```

**Why this breaks:** Each `ConnectionMultiplexer.Connect()` opens new TCP connections. Creating per-request connections exhausts file descriptors and causes connection storms.

### Configuration Options

```csharp
// src/Common/Sorcha.Storage.Redis/RedisCacheStore.cs pattern
var options = new ConfigurationOptions
{
    EndPoints = { "redis:6379" },
    AbortOnConnectFail = false,     // Don't throw on startup
    ConnectTimeout = 5000,          // 5s connection timeout
    SyncTimeout = 1000,             // 1s operation timeout
    AsyncTimeout = 5000,            // 5s async timeout
    ConnectRetry = 3,               // Retry 3 times
    ReconnectRetryPolicy = new LinearRetry(1000)
};
```

---

## Caching Patterns

### Cache-Aside Pattern

The standard pattern used in `RedisCacheStore`:

```csharp
public async Task<T?> GetOrSetAsync<T>(
    string key,
    Func<Task<T>> factory,
    TimeSpan? ttl = null)
{
    // Try cache first
    var cached = await GetAsync<T>(key);
    if (cached is not null)
        return cached;
    
    // Compute and cache
    var value = await factory();
    await SetAsync(key, value, ttl ?? DefaultTtl);
    return value;
}
```

### Distributed Locking (Simple)

For preventing thundering herd:

```csharp
public async Task<T?> GetWithLockAsync<T>(string key, Func<Task<T>> factory)
{
    var cached = await _db.StringGetAsync(key);
    if (!cached.IsNullOrEmpty)
        return JsonSerializer.Deserialize<T>(cached!);
    
    var lockKey = $"{key}:lock";
    var acquired = await _db.StringSetAsync(lockKey, "1", TimeSpan.FromSeconds(30), When.NotExists);
    
    if (acquired)
    {
        try
        {
            var value = await factory();
            await _db.StringSetAsync(key, JsonSerializer.Serialize(value), DefaultTtl);
            return value;
        }
        finally
        {
            await _db.KeyDeleteAsync(lockKey);
        }
    }
    
    // Wait and retry
    await Task.Delay(100);
    return await GetAsync<T>(key);
}
```

### Increment Pattern for Counters

```csharp
// src/Services/Sorcha.Tenant.Service/Services/TokenRevocationService.cs
public async Task<long> IncrementAsync(string key, TimeSpan? ttl = null)
{
    var value = await _db.StringIncrementAsync(GetKey(key));
    
    // Set expiration on first increment only
    if (value == 1 && ttl.HasValue)
    {
        await _db.KeyExpireAsync(GetKey(key), ttl);
    }
    
    return value;
}
```

---

## Key Naming Conventions

Sorcha uses hierarchical key naming:

| Pattern | Purpose | TTL | Example |
|---------|---------|-----|---------|
| `sorcha:{type}:{id}` | General cache | 15 min | `sorcha:user:abc123` |
| `auth:revoked:{jti}` | Revoked tokens | Token expiry | `auth:revoked:xyz789` |
| `auth:user_tokens:{userId}` | Token tracking set | Token expiry | `auth:user_tokens:user1` |
| `auth:org_tokens:{orgId}` | Org token set | Token expiry | `auth:org_tokens:org1` |
| `auth:failed:{identifier}` | Failed attempts | 60 seconds | `auth:failed:192.168.1.1` |

```csharp
// GOOD - Consistent prefix helper
private string GetKey(string key) => $"{_keyPrefix}{key}";

// GOOD - Structured keys
var userCacheKey = $"sorcha:user:{userId}";
var blueprintKey = $"sorcha:blueprint:{blueprintId}";

// BAD - Inconsistent naming
var key1 = "user_" + userId;        // Inconsistent separator
var key2 = $"User:{userId}";        // Inconsistent casing
```

---

## Resilience Patterns

### Circuit Breaker with Polly

```csharp
// src/Common/Sorcha.Storage.Redis/RedisCacheStore.cs
private static ResiliencePipeline BuildResiliencePipeline(CircuitBreakerConfiguration? config)
{
    config ??= new CircuitBreakerConfiguration();
    
    return new ResiliencePipelineBuilder()
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,                          // Break at 50% failures
            MinimumThroughput = config.FailureThreshold, // 5 failures minimum
            SamplingDuration = config.SamplingDuration,  // 1 minute window
            BreakDuration = config.BreakDuration         // 30 second break
        })
        .AddTimeout(TimeSpan.FromSeconds(5))
        .Build();
}
```

### Graceful Degradation

```csharp
// GOOD - Fail open for non-critical operations
public async Task<bool> IsTokenRevokedAsync(string jti)
{
    try
    {
        return await _db.KeyExistsAsync($"auth:revoked:{jti}");
    }
    catch (RedisConnectionException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable, allowing token");
        return false;  // Availability over strict security
    }
}

// GOOD - Silent cache failures
public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
{
    try
    {
        await _pipeline.ExecuteAsync(async ct =>
        {
            await _db.StringSetAsync(GetKey(key), JsonSerializer.Serialize(value), ttl);
        }, default);
    }
    catch (BrokenCircuitException)
    {
        // Cache is not critical path - silently continue
    }
}
```

---

## Anti-Patterns

### WARNING: Blocking Calls in Async Context

**The Problem:**

```csharp
// BAD - Blocks thread pool
public User GetUser(string id)
{
    var value = _db.StringGet($"user:{id}");  // Synchronous!
    return JsonSerializer.Deserialize<User>(value!);
}
```

**Why This Breaks:**
1. Blocks thread pool threads, reducing throughput
2. Can cause deadlocks in ASP.NET Core
3. Wastes resources while waiting for network I/O

**The Fix:**

```csharp
// GOOD - Async all the way
public async Task<User?> GetUserAsync(string id)
{
    var value = await _db.StringGetAsync($"user:{id}");
    return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<User>(value!);
}
```

### WARNING: Keys Pattern in Production

**The Problem:**

```csharp
// BAD - Scans ALL keys
var server = redis.GetServer("localhost", 6379);
var keys = server.Keys(pattern: "user:*").ToList();
```

**Why This Breaks:**
1. `KEYS` command is O(N) where N is total keys in database
2. Blocks Redis for entire duration
3. Can cause multi-second pauses with millions of keys

**The Fix:**

```csharp
// GOOD - Use SCAN with cursor
public async IAsyncEnumerable<RedisKey> ScanKeysAsync(string pattern)
{
    var server = _redis.GetServers().First();
    await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 1000))
    {
        yield return key;
    }
}
```

### WARNING: Large Values Without Compression

**The Problem:**

```csharp
// BAD - Storing large JSON blobs
var hugeObject = new { Data = new byte[10_000_000] };
await _db.StringSetAsync("large", JsonSerializer.Serialize(hugeObject));
```

**Why This Breaks:**
1. Redis is single-threaded - large values block other operations
2. Network bandwidth consumption
3. Memory fragmentation

**The Fix:**

```csharp
// GOOD - Compress large values
public async Task SetCompressedAsync<T>(string key, T value)
{
    var json = JsonSerializer.Serialize(value);
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
    {
        await gzip.WriteAsync(Encoding.UTF8.GetBytes(json));
    }
    await _db.StringSetAsync(key, output.ToArray());
}
```

Or better: store large blobs in blob storage and cache references only.