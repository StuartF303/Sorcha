# Integration Test Architecture

Visual guide to the Peer Service integration test architecture.

## Test Execution Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    Test Runner (xUnit)                      │
│                                                             │
│  dotnet test / run-integration-tests.ps1 / IDE              │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              xUnit Collection Fixture                       │
│                                                             │
│  [Collection("PeerIntegration")] - Shared across all tests  │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                 PeerTestFixture                             │
│                (IAsyncLifetime)                             │
│                                                             │
│  • InitializeAsync() - Creates 3 peer instances             │
│  • AddPeerInstanceAsync() - Adds more peers dynamically     │
│  • DisposeAsync() - Cleanup all resources                   │
└───────┬──────────────────┬──────────────────┬───────────────┘
        │                  │                  │
        ▼                  ▼                  ▼
┌───────────────┐  ┌───────────────┐  ┌───────────────┐
│ PeerInstance  │  │ PeerInstance  │  │ PeerInstance  │
│   (Peer 1)    │  │   (Peer 2)    │  │   (Peer 3)    │
├───────────────┤  ├───────────────┤  ├───────────────┤
│ • PeerId      │  │ • PeerId      │  │ • PeerId      │
│ • HttpClient  │  │ • HttpClient  │  │ • HttpClient  │
│ • GrpcClient  │  │ • GrpcClient  │  │ • GrpcClient  │
│ • Factory     │  │ • Factory     │  │ • Factory     │
│ • BaseAddress │  │ • BaseAddress │  │ • BaseAddress │
└───────┬───────┘  └───────┬───────┘  └───────┬───────┘
        │                  │                  │
        ▼                  ▼                  ▼
┌────────────────────────────────────────────────────────────┐
│           PeerServiceFactory (per instance)                │
│      WebApplicationFactory<Program>                        │
│                                                            │
│  • Configures testing environment                         │
│  • Replaces Redis with in-memory cache                    │
│  • Assigns random port (5000-6000)                        │
│  • Provides DI container access                           │
└────────────┬───────────────────────────────────────────────┘
             │
             ▼
┌────────────────────────────────────────────────────────────┐
│         Real Peer Service Instance (In-Memory)             │
│                                                            │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │  REST API    │  │  gRPC Service│  │  Metrics Service│ │
│  │              │  │              │  │                 │ │
│  │ • /api/peers │  │ • RegisterPeer│ │ • Uptime       │ │
│  │ • /api/health│  │ • GetPeerInfo │ │ • Transaction  │ │
│  │ • /api/metrics│ │ • StreamTx   │ │ • Throughput   │ │
│  └──────────────┘  └──────────────┘  └─────────────────┘ │
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │         InMemoryPeerRepository                       │ │
│  │  (No external dependencies)                          │ │
│  └──────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
```

---

## Test Class Structure

```
┌──────────────────────────────────────────────────────────────┐
│                     Test Classes                             │
└───────┬──────────────┬──────────────┬──────────────┬─────────┘
        │              │              │              │
        ▼              ▼              ▼              ▼
┌───────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────┐
│ Discovery     │ │ Communication │ │ Throughput │ │ Health  │
│ Tests         │ │ Tests        │ │ Tests      │ │ Tests   │
├───────────────┤ ├────────────┤ ├────────────┤ ├──────────┤
│ 12 tests      │ │ 10 tests   │ │ 8 tests    │ │ 6 tests  │
│ ~5s runtime   │ │ ~10s runtime│ │ ~30s runtime│ │ ~5s     │
└───────────────┘ └────────────┘ └────────────┘ └──────────┘
        │              │              │              │
        └──────────────┴──────────────┴──────────────┘
                           │
                           ▼
                  All use same fixture
                  (shared peer instances)
```

---

## Test Execution Lifecycle

```
1. Test Session Start
   │
   ├─▶ xUnit discovers all test classes
   │
   └─▶ Identifies [Collection("PeerIntegration")] attribute
       │
       └─▶ Creates single PeerTestFixture instance
           │
           ├─▶ InitializeAsync() called once
           │   │
           │   ├─▶ Creates Peer 1 (PeerServiceFactory + clients)
           │   ├─▶ Creates Peer 2 (PeerServiceFactory + clients)
           │   └─▶ Creates Peer 3 (PeerServiceFactory + clients)
           │
           ├─▶ Test 1 executes (uses fixture.Peers[0])
           ├─▶ Test 2 executes (uses fixture.Peers[1])
           ├─▶ Test 3 executes (can use any peer)
           ├─▶ ... (all 36+ tests execute)
           │
           └─▶ DisposeAsync() called once
               │
               ├─▶ Shutdown gRPC channels
               ├─▶ Dispose HTTP clients
               └─▶ Dispose WebApplicationFactory instances

2. Test Session End
```

---

## Communication Flow (Test → Service)

```
┌──────────────────────────────────────────────────────────────┐
│                        Test Method                           │
└───────┬──────────────────────────────────────────────────────┘
        │
        ▼
┌──────────────────────────────────────────────────────────────┐
│               Access PeerInstance from Fixture               │
│         var peer = _fixture.Peers[0];                        │
└───────┬──────────────────────────────────────────────────────┘
        │
        ├─────────────────┬─────────────────────────────┐
        │                 │                             │
        ▼                 ▼                             ▼
┌─────────────┐   ┌──────────────┐          ┌─────────────────┐
│ REST API    │   │ gRPC Client  │          │ Get Service     │
│             │   │              │          │ from DI         │
│ HttpClient  │   │ PeerService  │          │                 │
│ .GetAsync() │   │ .RegisterPeer│          │ peer.GetService │
│             │   │ .StreamTx()  │          │ <TService>()    │
└─────┬───────┘   └──────┬───────┘          └────────┬────────┘
      │                  │                           │
      │                  │                           │
      ▼                  ▼                           ▼
┌─────────────────────────────────────────────────────────────┐
│              WebApplicationFactory                          │
│         (In-Memory Test Server - Kestrel)                   │
└───────┬─────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│                 Peer Service Program                        │
│                                                             │
│  • Routing (Minimal APIs)                                   │
│  • Middleware (Authentication, Logging)                     │
│  • Services (PeerRepository, MetricsService)                │
│  • gRPC Service (PeerGrpcService)                           │
└───────┬─────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│             Business Logic Execution                        │
│                                                             │
│  IPeerRepository → InMemoryPeerRepository                   │
│  IMetricsService → MetricsService                           │
└───────┬─────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│                    Response                                 │
│                                                             │
│  • HTTP Response (REST)                                     │
│  • gRPC Message (Protocol Buffers)                          │
│  • Status Codes / RPC Status                                │
└───────┬─────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│                  Test Assertions                            │
│                                                             │
│  response.StatusCode.Should().Be(HttpStatusCode.OK);        │
│  result.Success.Should().BeTrue();                          │
│  metrics.TotalTransactions.Should().BeGreaterThan(0);       │
└─────────────────────────────────────────────────────────────┘
```

---

## Throughput Test Flow

```
┌──────────────────────────────────────────────────────────────┐
│     High_Volume_Transaction_Stream_Should_Maintain_Performance│
└───────┬──────────────────────────────────────────────────────┘
        │
        ├─▶ Start Stopwatch
        │
        ├─▶ Open bidirectional gRPC stream
        │   │
        │   ├─▶ RequestStream (client → server)
        │   └─▶ ResponseStream (server → client)
        │
        ├─▶ Send Loop (async task)
        │   │
        │   └─▶ for (int i = 0; i < 1000; i++)
        │       {
        │           await RequestStream.WriteAsync(transaction);
        │       }
        │       await RequestStream.CompleteAsync();
        │
        ├─▶ Receive Loop (main thread)
        │   │
        │   └─▶ await foreach (var response in ResponseStream)
        │       {
        │           responses.Add(response);
        │       }
        │
        ├─▶ Stop Stopwatch
        │
        ├─▶ Calculate Metrics
        │   │
        │   ├─▶ throughput = count / elapsed.TotalSeconds
        │   ├─▶ latency = elapsed.TotalMs / count
        │   └─▶ Console.WriteLine($"Throughput: {throughput} tx/sec")
        │
        └─▶ Assert Performance
            │
            ├─▶ responses.Should().HaveCount(1000)
            └─▶ throughput.Should().BeGreaterThan(100)
```

---

## Parallel Test Execution

```
┌────────────────────────────────────────────────────────────┐
│             xUnit Test Runner (Parallel Mode)              │
└─────┬──────────────┬──────────────┬──────────────┬─────────┘
      │              │              │              │
      ▼              ▼              ▼              ▼
┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
│ Thread 1 │   │ Thread 2 │   │ Thread 3 │   │ Thread 4 │
│          │   │          │   │          │   │          │
│ Test A   │   │ Test B   │   │ Test C   │   │ Test D   │
└────┬─────┘   └────┬─────┘   └────┬─────┘   └────┬─────┘
     │              │              │              │
     └──────────────┴──────────────┴──────────────┘
                    │
                    ▼
          ┌──────────────────────┐
          │  Shared PeerFixture  │
          │                      │
          │  Thread-safe access  │
          │  to peer instances   │
          └──────────────────────┘
                    │
     ┌──────────────┼──────────────┬──────────────┐
     │              │              │              │
     ▼              ▼              ▼              ▼
┌─────────┐   ┌─────────┐   ┌─────────┐   ┌─────────┐
│ Peer 1  │   │ Peer 2  │   │ Peer 3  │   │ Peer N  │
│         │   │         │   │         │   │         │
│ Port    │   │ Port    │   │ Port    │   │ Port    │
│ 5234    │   │ 5891    │   │ 5467    │   │ 5123    │
└─────────┘   └─────────┘   └─────────┘   └─────────┘

Benefits:
• Tests run in parallel (faster execution)
• Each peer isolated on different port
• No test interference
• Shared fixture reduces memory usage
```

---

## Dependency Injection Container

```
┌────────────────────────────────────────────────────────────┐
│          WebApplicationFactory Services (DI)               │
└────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────────┐
│                    Service Container                       │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  Singleton Services:                                       │
│  ├─▶ IPeerRepository → InMemoryPeerRepository             │
│  ├─▶ IMetricsService → MetricsService                     │
│  ├─▶ ILogger<T> → TestLogger<T>                           │
│  └─▶ IOutputCacheStore → MemoryOutputCacheStore           │
│                                                            │
│  gRPC Services:                                            │
│  └─▶ PeerGrpcService (registered via AddGrpc())           │
│                                                            │
│  Test-Specific Overrides:                                 │
│  ├─▶ No Redis (replaced with in-memory)                   │
│  └─▶ No Aspire dependencies (removed)                     │
│                                                            │
└────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────────┐
│              Access from Tests:                            │
│                                                            │
│  var repository = peer.GetService<IPeerRepository>();     │
│  var metrics = peer.GetService<IMetricsService>();        │
└────────────────────────────────────────────────────────────┘
```

---

## Test Data Flow (Discovery Test Example)

```
Test: RegisterPeer_Via_REST_Should_Return_Success

1. Test Setup
   │
   └─▶ Create PeerNode object
       {
         PeerId: "test-peer-123",
         Endpoint: "http://localhost:5001",
         Metadata: { "region": "us-west" }
       }

2. HTTP Request
   │
   └─▶ POST /api/peers
       Content-Type: application/json
       Body: { ...PeerNode... }

3. Service Processing
   │
   ├─▶ Routing: MapPost("/api/peers", ...)
   │
   ├─▶ Model Binding: PeerNode peer
   │
   ├─▶ Repository Call:
   │   repo.RegisterPeerAsync(peer.PeerId, peer.Endpoint, peer.Metadata)
   │   │
   │   └─▶ InMemoryPeerRepository
   │       ├─▶ Acquire lock (thread-safe)
   │       ├─▶ Create PeerNode with timestamp
   │       ├─▶ Store in Dictionary<string, PeerNode>
   │       └─▶ Release lock
   │
   ├─▶ Metrics Update:
   │   metricsService.IncrementActivePeers()
   │
   └─▶ Response:
       Status: 201 Created
       Location: /api/peers/test-peer-123
       Body: { ...PeerNode with timestamp... }

4. Test Assertions
   │
   ├─▶ response.StatusCode.Should().Be(HttpStatusCode.Created)
   │
   ├─▶ response.Headers.Location.Should().Contain("test-peer-123")
   │
   └─▶ var registered = await response.Content.ReadFromJsonAsync<PeerNode>();
       registered.PeerId.Should().Be("test-peer-123")
       registered.Status.Should().Be("active")
```

---

## gRPC Streaming Flow (Communication Test Example)

```
Test: Bidirectional_Stream_Should_Work_Correctly

┌──────────────────────────────────────────────────────────────┐
│                      Test Thread                             │
└───────┬──────────────────────────────────────────────────────┘
        │
        ├─▶ 1. Open bidirectional stream
        │   var call = grpcClient.StreamTransactions()
        │
        ├─▶ 2. Start reader task (async)
        │   │
        │   Task.Run(async () => {
        │     await foreach (var response in call.ResponseStream) {
        │       responses.Add(response);
        │     }
        │   });
        │
        ├─▶ 3. Writer loop (main thread)
        │   │
        │   for (int i = 0; i < 20; i++) {
        │     var tx = new TransactionMessage {
        │       TransactionId = $"tx-{i}",
        │       Payload = [...],
        │       ...
        │     };
        │     await call.RequestStream.WriteAsync(tx);
        │     await Task.Delay(10); // Small delay
        │   }
        │
        ├─▶ 4. Complete sending
        │   await call.RequestStream.CompleteAsync();
        │
        ├─▶ 5. Wait for all responses
        │   await readerTask;
        │
        └─▶ 6. Verify
            responses.Should().HaveCount(20)
            responses.Should().OnlyContain(r => r.Success)

┌──────────────────────────────────────────────────────────────┐
│                    Server Processing                         │
│              (PeerGrpcService.StreamTransactions)            │
└───────┬──────────────────────────────────────────────────────┘
        │
        └─▶ await foreach (var tx in requestStream) {
              // Log transaction
              logger.LogDebug("Processing {TxId}", tx.TransactionId);

              // Update metrics
              metricsService.IncrementTransactionCount();

              // Create response
              var response = new TransactionResponse {
                TransactionId = tx.TransactionId,
                Success = true,
                ProcessedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
              };

              // Send back to client
              await responseStream.WriteAsync(response);
            }

┌──────────────────────────────────────────────────────────────┐
│                       Result                                 │
│                                                              │
│  20 transactions sent → 20 responses received                │
│  All processed successfully                                  │
│  Metrics updated (TotalTransactions += 20)                   │
└──────────────────────────────────────────────────────────────┘
```

---

## Memory Management

```
┌────────────────────────────────────────────────────────────┐
│                 Test Session Lifecycle                     │
└────────────────────────────────────────────────────────────┘

Session Start (Memory: ~50 MB baseline)
│
├─▶ Create PeerTestFixture
│   └─▶ Memory: +5 MB
│
├─▶ Create 3 PeerServiceFactory instances
│   └─▶ Memory: +30 MB (10 MB each)
│
├─▶ Create HttpClients + GrpcChannels
│   └─▶ Memory: +5 MB
│
├─▶ Run Discovery Tests (12 tests)
│   └─▶ Memory: +10 MB (temporary allocations)
│
├─▶ Run Communication Tests (10 tests)
│   └─▶ Memory: +20 MB (gRPC streams, payloads)
│
├─▶ Run Throughput Tests (8 tests)
│   └─▶ Memory: +50 MB (large payload buffers)
│   │
│   └─▶ GC.Collect() between iterations
│       Memory returns to ~100 MB
│
├─▶ Run Health Tests (6 tests)
│   └─▶ Memory: +5 MB
│
└─▶ DisposeAsync() called
    │
    ├─▶ Shutdown gRPC channels
    ├─▶ Dispose HttpClients
    ├─▶ Dispose WebApplicationFactory
    └─▶ Memory: Returns to ~50 MB baseline

Peak Memory: ~150 MB
Final Memory: ~50 MB
Memory Growth: <100 MB over full suite
```

---

## Performance Optimization Strategies

```
1. Fixture Reuse
   ─────────────
   ❌ Create new peer per test
      • 36 tests × 3 peers = 108 peer instances
      • ~10s startup overhead per test
      • Total time: ~6 minutes

   ✅ Share 3 peers across all tests
      • 1 fixture × 3 peers = 3 peer instances
      • ~1s startup overhead total
      • Total time: ~50 seconds

2. Parallel Execution
   ──────────────────
   ✅ xUnit runs tests in parallel by default
      • Utilizes multiple CPU cores
      • Tests are isolated (thread-safe fixture)
      • 3-5x faster than sequential

3. In-Memory Dependencies
   ──────────────────────
   ✅ Replace Redis with in-memory cache
      • No network overhead
      • No external service startup
      • Tests start instantly

4. Random Port Assignment
   ──────────────────────
   ✅ Each peer gets random port (5000-6000)
      • No port conflicts
      • Can run multiple test sessions
      • Safe for parallel execution
```

---

This architecture enables:
- ✅ Fast test execution (~50 seconds)
- ✅ Isolated, independent tests
- ✅ Real service behavior validation
- ✅ Parallel execution capability
- ✅ No external dependencies
- ✅ Easy to debug and maintain
