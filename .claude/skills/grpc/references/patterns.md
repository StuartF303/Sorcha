# gRPC Patterns Reference

## Contents
- Service Implementation Patterns
- Client Patterns
- Error Handling
- Streaming Patterns
- Proto Design Patterns
- Anti-Patterns

---

## Service Implementation Patterns

### Base Class Inheritance

Every gRPC service inherits from the generated base class:

```csharp
// src/Services/Sorcha.Validator.Service/GrpcServices/ValidatorGrpcService.cs
public class ValidatorGrpcService : ValidatorService.ValidatorServiceBase
{
    private readonly IValidatorService _validatorService;
    private readonly ILogger<ValidatorGrpcService> _logger;

    public ValidatorGrpcService(
        IValidatorService validatorService,
        ILogger<ValidatorGrpcService> logger)
    {
        _validatorService = validatorService;
        _logger = logger;
    }

    public override async Task<VoteResponse> RequestVote(
        VoteRequest request, ServerCallContext context)
    {
        var domainRequest = MapToDomain(request);
        var result = await _validatorService.ProcessVoteAsync(
            domainRequest, context.CancellationToken);
        return MapToProto(result);
    }
}
```

### Domain Model Mapping

**DO:** Map proto messages to domain models at the gRPC boundary:

```csharp
// GOOD - Clean separation
private static VoteRequest MapToDomain(Grpc.V1.VoteRequest proto)
{
    return new VoteRequest
    {
        Term = proto.Term,
        CandidateId = proto.CandidateId,
        LastLogIndex = proto.LastLogIndex
    };
}

private static Grpc.V1.VoteResponse MapToProto(VoteResult domain)
{
    return new Grpc.V1.VoteResponse
    {
        VoteGranted = domain.Granted,
        CurrentTerm = domain.Term
    };
}
```

**DON'T:** Let proto types leak into business logic:

```csharp
// BAD - Proto types in domain layer
public class ValidatorService
{
    public Task<Grpc.V1.VoteResponse> ProcessVote(Grpc.V1.VoteRequest request)
    // Proto types shouldn't be in domain layer
}
```

---

## Client Patterns

### Thread-Safe Caching with Retry

From `WalletIntegrationService.cs`:

```csharp
public class WalletIntegrationService : IWalletIntegrationService, IDisposable
{
    private readonly WalletService.WalletServiceClient _walletClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private WalletDetails? _cachedDetails;

    public WalletIntegrationService(GrpcChannel walletServiceChannel)
    {
        _walletClient = new WalletService.WalletServiceClient(walletServiceChannel);
        _retryPolicy = CreateRetryPolicy();
    }

    private static AsyncRetryPolicy CreateRetryPolicy()
    {
        return Policy
            .Handle<RpcException>(ex => IsTransient(ex.StatusCode))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
    }

    private static bool IsTransient(StatusCode code) =>
        code is StatusCode.Unavailable or StatusCode.DeadlineExceeded;
}
```

### Channel Registration Pattern

Register `GrpcChannel` as singleton, inject into services:

```csharp
// Program.cs
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<WalletConfiguration>();
    return GrpcChannel.ForAddress(config.Endpoint, new GrpcChannelOptions
    {
        HttpHandler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        }
    });
});

// Service receives channel via DI
builder.Services.AddScoped<IWalletIntegrationService, WalletIntegrationService>();
```

---

## Error Handling

### RpcException with Status Codes

```csharp
// src/Services/Sorcha.Peer.Service/Discovery/PeerDiscoveryServiceImpl.cs
public override async Task<RegisterPeerResponse> RegisterPeer(
    RegisterPeerRequest request, ServerCallContext context)
{
    if (string.IsNullOrEmpty(request.PeerId))
    {
        throw new RpcException(new Status(
            StatusCode.InvalidArgument, 
            "PeerId is required"));
    }

    try
    {
        var registered = await _peerRegistry.RegisterAsync(request.PeerId);
        return new RegisterPeerResponse { Success = registered };
    }
    catch (DuplicatePeerException)
    {
        throw new RpcException(new Status(
            StatusCode.AlreadyExists,
            $"Peer {request.PeerId} already registered"));
    }
}
```

### Status Code Mapping

| Domain Error | gRPC Status Code |
|--------------|------------------|
| Not found | `StatusCode.NotFound` |
| Validation failed | `StatusCode.InvalidArgument` |
| Already exists | `StatusCode.AlreadyExists` |
| Unauthorized | `StatusCode.PermissionDenied` |
| Timeout | `StatusCode.DeadlineExceeded` |
| Unavailable | `StatusCode.Unavailable` |

---

## Streaming Patterns

### Server Streaming

From `SystemRegisterSync.proto`:

```protobuf
service SystemRegisterSync {
  rpc FullSync(SyncRequest) returns (stream SystemRegisterEntry);
}
```

```csharp
public override async Task FullSync(
    SyncRequest request,
    IServerStreamWriter<SystemRegisterEntry> responseStream,
    ServerCallContext context)
{
    await foreach (var entry in _syncService.GetEntriesAsync(context.CancellationToken))
    {
        await responseStream.WriteAsync(entry);
    }
}
```

### Bidirectional Streaming

From `Heartbeat.proto`:

```protobuf
service Heartbeat {
  rpc MonitorHeartbeat(stream HeartbeatMessage) returns (stream HeartbeatAcknowledgement);
}
```

```csharp
public override async Task MonitorHeartbeat(
    IAsyncStreamReader<HeartbeatMessage> requestStream,
    IServerStreamWriter<HeartbeatAcknowledgement> responseStream,
    ServerCallContext context)
{
    await foreach (var heartbeat in requestStream.ReadAllAsync(context.CancellationToken))
    {
        var ack = new HeartbeatAcknowledgement { ReceivedAt = Timestamp.FromDateTime(DateTime.UtcNow) };
        await responseStream.WriteAsync(ack);
    }
}
```

---

## Proto Design Patterns

### Package Naming Convention

```protobuf
syntax = "proto3";
package sorcha.servicename.v1;  // Pattern: sorcha.<service>.v<version>
option csharp_namespace = "Sorcha.ServiceName.Grpc.V1";
```

### Algorithm Enums

```protobuf
// wallet_service.proto
enum WalletAlgorithm {
  WALLET_ALGORITHM_UNSPECIFIED = 0;  // Always have unspecified as 0
  ED25519 = 1;
  NISTP256 = 2;
  RSA4096 = 3;
}
```

---

## Anti-Patterns

### WARNING: Creating New Channels Per Request

**The Problem:**

```csharp
// BAD - New channel per call
public async Task<Result> CallServiceAsync()
{
    using var channel = GrpcChannel.ForAddress("https://service:5001");
    var client = new MyService.MyServiceClient(channel);
    return await client.DoWorkAsync(new Request());
}
```

**Why This Breaks:**
1. TCP connection overhead on every call
2. No HTTP/2 multiplexing benefits
3. Connection pool exhaustion under load

**The Fix:**

```csharp
// GOOD - Singleton channel, reused
public class MyClient
{
    private readonly MyService.MyServiceClient _client;
    
    public MyClient(GrpcChannel channel)
    {
        _client = new MyService.MyServiceClient(channel);
    }
}
```

### WARNING: Ignoring CancellationToken

**The Problem:**

```csharp
// BAD - Ignores cancellation
public override async Task<Response> Process(Request request, ServerCallContext context)
{
    await Task.Delay(5000);  // Won't cancel if client disconnects
    return new Response();
}
```

**The Fix:**

```csharp
// GOOD - Respects cancellation
public override async Task<Response> Process(Request request, ServerCallContext context)
{
    await Task.Delay(5000, context.CancellationToken);
    return new Response();
}
```

### WARNING: Swallowing RpcException

**The Problem:**

```csharp
// BAD - Silent failure
try
{
    await _client.DoWorkAsync(request);
}
catch (RpcException)
{
    // Swallowed - no logging, no rethrow
}
```

**The Fix:**

```csharp
// GOOD - Log and handle appropriately
try
{
    await _client.DoWorkAsync(request);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
{
    _logger.LogWarning(ex, "Service unavailable, will retry");
    throw;  // Let retry policy handle
}
```