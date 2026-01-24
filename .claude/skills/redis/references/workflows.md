# Redis Workflows Reference

## Contents
- Service Setup
- Docker Configuration
- Testing with Redis
- Debugging and Monitoring
- Common Operations

---

## Service Setup

### Adding Redis to a New Service

Copy this checklist and track progress:
- [ ] Step 1: Add Aspire reference in AppHost
- [ ] Step 2: Configure connection in service
- [ ] Step 3: Register services in DI
- [ ] Step 4: Add health check
- [ ] Step 5: Verify connection at startup

#### Step 1: AppHost Configuration

```csharp
// src/Apps/Sorcha.AppHost/AppHost.cs
var redis = builder.AddRedis("redis")
    .WithRedisCommander();  // Dev UI on port 8081

var myService = builder.AddProject<Projects.MyService>()
    .WithReference(redis);
```

#### Step 2: Service Configuration

```csharp
// Program.cs
builder.AddRedisDistributedCache("redis");   // IDistributedCache
builder.AddRedisOutputCache("redis");        // Output caching

// Or direct connection
builder.AddRedisClient("redis");             // IConnectionMultiplexer
```

#### Step 3: DI Registration

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddRedisServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("redis");
    
    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var options = ConfigurationOptions.Parse(connectionString!);
        options.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(options);
    });
    
    services.AddScoped<ICacheStore, RedisCacheStore>();
    
    return services;
}
```

#### Step 4: Health Check

```csharp
builder.Services.AddHealthChecks()
    .AddRedis(connectionString!, name: "redis", tags: new[] { "ready" });
```

---

## Docker Configuration

### docker-compose.yml Setup

```yaml
redis:
  image: redis:8-alpine
  container_name: sorcha-redis
  restart: unless-stopped
  ports:
    - "16379:6379"          # External access for debugging
  volumes:
    - redis-data:/data      # Persistence
  networks:
    - sorcha-network
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 10s
    timeout: 3s
    retries: 5
  command: redis-server --appendonly yes  # AOF persistence
```

### Service Environment Variables

```yaml
my-service:
  environment:
    - ConnectionStrings__Redis=redis:6379
    # Or for Aspire naming
    - ConnectionStrings__redis=redis:6379
```

### Verify Redis is Running

```bash
# Check container status
docker-compose ps redis

# Test connection
docker exec sorcha-redis redis-cli ping

# Monitor operations
docker exec sorcha-redis redis-cli monitor
```

---

## Testing with Redis

### Integration Tests with Testcontainers

```csharp
public class RedisIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:8-alpine")
        .Build();
    
    private IConnectionMultiplexer _connection = null!;
    
    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(
            _redis.GetConnectionString());
    }
    
    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _redis.DisposeAsync();
    }
    
    [Fact]
    public async Task CacheStore_SetAndGet_ReturnsValue()
    {
        var store = new RedisCacheStore(_connection, Options.Create(new HotTierConfiguration()));
        
        await store.SetAsync("test", new { Name = "Test" });
        var result = await store.GetAsync<dynamic>("test");
        
        result.Should().NotBeNull();
        ((string)result!.Name).Should().Be("Test");
    }
}
```

### Unit Tests with Mocking

```csharp
public class TokenRevocationServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    
    public TokenRevocationServiceTests()
    {
        _dbMock = new Mock<IDatabase>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_dbMock.Object);
    }
    
    [Fact]
    public async Task RevokeTokenAsync_ValidToken_SetsKeyWithTtl()
    {
        var service = new TokenRevocationService(_redisMock.Object);
        var jti = "test-jti";
        var expiry = DateTimeOffset.UtcNow.AddHours(1);
        
        await service.RevokeTokenAsync(jti, expiry);
        
        _dbMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(jti)),
            It.IsAny<RedisValue>(),
            It.Is<TimeSpan?>(t => t!.Value.TotalMinutes > 50),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
}
```

### Test Validation Loop

1. Write test
2. Run: `dotnet test --filter "FullyQualifiedName~Redis"`
3. If test fails, check Redis connection and fix
4. Only proceed when all tests pass

---

## Debugging and Monitoring

### Redis Commander (Development)

Access via: `http://localhost:8081` (when using Aspire with `.WithRedisCommander()`)

### CLI Debugging

```bash
# Connect to Redis CLI
docker exec -it sorcha-redis redis-cli

# View all keys (development only!)
KEYS *

# Get specific key
GET sorcha:user:123

# Check TTL
TTL auth:revoked:abc123

# Monitor real-time operations
MONITOR

# Memory stats
INFO memory

# Check connected clients
CLIENT LIST
```

### Checking Cache Statistics

```csharp
// Get stats from RedisCacheStore
var stats = await cacheStore.GetStatisticsAsync();

Console.WriteLine($"Hit Rate: {stats.HitRate:P2}");
Console.WriteLine($"P99 Latency: {stats.P99LatencyMs:F2}ms");
Console.WriteLine($"Total Requests: {stats.TotalRequests}");
Console.WriteLine($"Evictions: {stats.EvictionCount}");
```

### Connection Events

```csharp
redis.ConnectionFailed += (sender, args) =>
    _logger.LogError("Redis connection failed: {Endpoint} - {FailureType}",
        args.EndPoint, args.FailureType);

redis.ConnectionRestored += (sender, args) =>
    _logger.LogInformation("Redis connection restored: {Endpoint}",
        args.EndPoint);

redis.ErrorMessage += (sender, args) =>
    _logger.LogWarning("Redis error: {Message}", args.Message);
```

---

## Common Operations

### Bulk Delete by Pattern

```csharp
public async Task<long> InvalidateUserCacheAsync(string userId)
{
    var server = _redis.GetServers().First();
    var pattern = $"sorcha:*:{userId}:*";
    var keys = server.Keys(pattern: pattern).ToArray();
    
    if (keys.Length == 0)
        return 0;
    
    return await _db.KeyDeleteAsync(keys);
}
```

### Atomic Get-Set-If-Not-Exists

```csharp
public async Task<bool> TryAcquireLockAsync(string resource, TimeSpan expiry)
{
    var key = $"lock:{resource}";
    return await _db.StringSetAsync(key, Environment.MachineName, expiry, When.NotExists);
}

public async Task ReleaseLockAsync(string resource)
{
    var key = $"lock:{resource}";
    await _db.KeyDeleteAsync(key);
}
```

### Pub/Sub for Real-Time Events

```csharp
// Publisher
var subscriber = redis.GetSubscriber();
await subscriber.PublishAsync("events:blueprint", JsonSerializer.Serialize(new
{
    Type = "BlueprintUpdated",
    Id = blueprintId,
    Timestamp = DateTimeOffset.UtcNow
}));

// Subscriber
subscriber.Subscribe("events:blueprint", (channel, message) =>
{
    var evt = JsonSerializer.Deserialize<BlueprintEvent>(message!);
    _logger.LogInformation("Blueprint event: {Type} for {Id}", evt.Type, evt.Id);
});
```

### Pipeline for Batch Operations

```csharp
public async Task SetMultipleAsync(Dictionary<string, object> items)
{
    var batch = _db.CreateBatch();
    var tasks = new List<Task>();
    
    foreach (var (key, value) in items)
    {
        tasks.Add(batch.StringSetAsync(
            GetKey(key),
            JsonSerializer.Serialize(value),
            DefaultTtl));
    }
    
    batch.Execute();
    await Task.WhenAll(tasks);
}