# gRPC Workflows Reference

## Contents
- Adding a New gRPC Service
- Proto Compilation
- Dual-Port Configuration
- Testing gRPC Services
- Debugging with Reflection

---

## Adding a New gRPC Service

Copy this checklist and track progress:
- [ ] Step 1: Create proto file in `Protos/` directory
- [ ] Step 2: Add proto to `.csproj` file
- [ ] Step 3: Build to generate C# classes
- [ ] Step 4: Create service implementation in `GrpcServices/`
- [ ] Step 5: Register gRPC in Program.cs
- [ ] Step 6: Map the service endpoint
- [ ] Step 7: Add integration tests

### Step 1: Create Proto File

```protobuf
// src/Services/Sorcha.MyService.Service/Protos/my_service.proto
syntax = "proto3";
package sorcha.myservice.v1;
option csharp_namespace = "Sorcha.MyService.Grpc.V1";

import "google/protobuf/empty.proto";
import "google/protobuf/timestamp.proto";

service MyService {
  rpc GetStatus(google.protobuf.Empty) returns (StatusResponse);
  rpc ProcessItem(ItemRequest) returns (ItemResponse);
}

message StatusResponse {
  bool is_healthy = 1;
  google.protobuf.Timestamp checked_at = 2;
}

message ItemRequest {
  string item_id = 1;
}

message ItemResponse {
  bool success = 1;
  string message = 2;
}
```

### Step 2: Add to Project File

```xml
<!-- Sorcha.MyService.Service.csproj -->
<ItemGroup>
  <Protobuf Include="Protos\my_service.proto" GrpcServices="Server" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Grpc.AspNetCore" Version="2.67.0" />
  <PackageReference Include="Google.Protobuf" Version="3.29.3" />
</ItemGroup>
```

### Step 3: Build and Verify

```bash
dotnet build src/Services/Sorcha.MyService.Service
# Verify generated files in obj/Debug/net10.0/Protos/
```

### Step 4: Implement Service

```csharp
// src/Services/Sorcha.MyService.Service/GrpcServices/MyGrpcService.cs
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using Sorcha.MyService.Grpc.V1;

namespace Sorcha.MyService.Service.GrpcServices;

public class MyGrpcService : Grpc.V1.MyService.MyServiceBase
{
    private readonly ILogger<MyGrpcService> _logger;
    private readonly IMyDomainService _domainService;

    public MyGrpcService(
        ILogger<MyGrpcService> logger,
        IMyDomainService domainService)
    {
        _logger = logger;
        _domainService = domainService;
    }

    public override Task<StatusResponse> GetStatus(
        Empty request, ServerCallContext context)
    {
        return Task.FromResult(new StatusResponse
        {
            IsHealthy = true,
            CheckedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        });
    }

    public override async Task<ItemResponse> ProcessItem(
        ItemRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Processing item {ItemId}", request.ItemId);
        
        var result = await _domainService.ProcessAsync(
            request.ItemId, context.CancellationToken);
        
        return new ItemResponse
        {
            Success = result.Success,
            Message = result.Message
        };
    }
}
```

### Step 5-6: Register in Program.cs

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Development only: enable reflection for grpcurl/grpcui
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

var app = builder.Build();

app.MapGrpcService<MyGrpcService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.Run();
```

---

## Dual-Port Configuration

The Peer Service uses separate ports for REST and gRPC. See the **aspire** skill for service discovery patterns.

```csharp
// src/Services/Sorcha.Peer.Service/Program.cs
var healthCheckPort = builder.Configuration.GetValue("HEALTH_CHECK_PORT", 8080);
var grpcPort = builder.Configuration.GetValue("GRPC_PORT", 5000);

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/1.1 + HTTP/2 for health checks and REST
    options.ListenAnyIP(healthCheckPort, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // HTTP/2 only for gRPC (cleartext in development)
    if (grpcPort != healthCheckPort)
    {
        options.ListenAnyIP(grpcPort, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    }
});

// Required for HTTP/2 without TLS
AppContext.SetSwitch(
    "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
```

---

## Testing gRPC Services

### Unit Testing with Moq

```csharp
// tests/Sorcha.MyService.Tests/GrpcServices/MyGrpcServiceTests.cs
public class MyGrpcServiceTests
{
    private readonly Mock<IMyDomainService> _domainServiceMock;
    private readonly MyGrpcService _sut;

    public MyGrpcServiceTests()
    {
        _domainServiceMock = new Mock<IMyDomainService>();
        _sut = new MyGrpcService(
            Mock.Of<ILogger<MyGrpcService>>(),
            _domainServiceMock.Object);
    }

    [Fact]
    public async Task ProcessItem_ValidId_ReturnsSuccess()
    {
        // Arrange
        _domainServiceMock
            .Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult { Success = true, Message = "Done" });

        var context = TestServerCallContext.Create();

        // Act
        var result = await _sut.ProcessItem(
            new ItemRequest { ItemId = "test-123" }, context);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Done");
    }
}
```

### Integration Testing with WebApplicationFactory

```csharp
public class MyGrpcServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly GrpcChannel _channel;

    public MyGrpcServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        _channel = GrpcChannel.ForAddress(client.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = factory.Server.CreateHandler()
        });
    }

    [Fact]
    public async Task GetStatus_ReturnsHealthy()
    {
        var client = new Grpc.V1.MyService.MyServiceClient(_channel);
        var response = await client.GetStatusAsync(new Empty());
        response.IsHealthy.Should().BeTrue();
    }
}
```

---

## Debugging with Reflection

Enable reflection in development to use `grpcurl` and `grpcui`:

```csharp
// Program.cs
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

// After app.Build()
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}
```

### Using grpcurl

```bash
# List services
grpcurl -plaintext localhost:5000 list

# Describe a service
grpcurl -plaintext localhost:5000 describe sorcha.peer.discovery.PeerDiscovery

# Call a method
grpcurl -plaintext -d '{}' localhost:5000 sorcha.peer.discovery.PeerDiscovery/Ping
```

### Using grpcui

```bash
# Launch web UI
grpcui -plaintext localhost:5000
```

---

## Validation Workflow

When modifying gRPC services, follow this validation loop:

1. Make proto changes
2. Run: `dotnet build`
3. If build fails, fix proto syntax and repeat step 2
4. Make service implementation changes
5. Run: `dotnet test --filter "FullyQualifiedName~GrpcService"`
6. If tests fail, fix implementation and repeat step 5
7. Only proceed when all tests pass

---

## Client Generation for Other Services

When a service needs to call another service's gRPC API:

```xml
<!-- Sorcha.Validator.Service.csproj -->
<ItemGroup>
  <!-- Server for this service's API -->
  <Protobuf Include="Protos\validator.proto" GrpcServices="Server" />
  
  <!-- Client for calling Wallet Service -->
  <Protobuf Include="..\Sorcha.Wallet.Service\Protos\wallet_service.proto" 
            GrpcServices="Client" 
            Link="Protos\wallet_service.proto" />
</ItemGroup>
```

Use the **dotnet** skill for project reference patterns.