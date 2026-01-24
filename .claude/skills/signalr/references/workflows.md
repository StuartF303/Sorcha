# SignalR Workflows Reference

## Contents
- Setup and Configuration
- Authentication Setup
- Testing SignalR Hubs
- Scaling with Redis Backplane
- Client Connection Patterns

---

## Setup and Configuration

### Adding SignalR to a Service

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add SignalR services
builder.Services.AddSignalR();

// Register notification service abstraction
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Map hub AFTER authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ActionsHub>("/actionshub");
```

### Hub Configuration Checklist

Copy this checklist when adding a new hub:

- [ ] Create hub class inheriting from `Hub` or `Hub<T>`
- [ ] Define client interface if using typed hub
- [ ] Add constructor with ILogger injection
- [ ] Implement `OnConnectedAsync` with logging
- [ ] Implement `OnDisconnectedAsync` with error logging
- [ ] Add subscription/unsubscription methods
- [ ] Create service abstraction for sending
- [ ] Register services in DI
- [ ] Map hub endpoint in Program.cs
- [ ] Write integration tests

---

## Authentication Setup

### JWT via Query Parameter

SignalR clients pass JWT tokens via query parameter since WebSocket upgrade requests cannot include custom headers.

**Server Configuration:**

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Extract token from query string for SignalR
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && 
                    (path.StartsWithSegments("/actionshub") || 
                     path.StartsWithSegments("/hubs/register")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

**Client Connection:**

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl($"https://localhost:7000/actionshub?access_token={jwtToken}")
    .Build();
```

See the **jwt** skill for token generation and validation patterns.

---

## Testing SignalR Hubs

### Integration Test Setup

From `tests/Sorcha.Blueprint.Service.Tests/Integration/SignalRIntegrationTests.cs`:

```csharp
public class SignalRIntegrationTests : IClassFixture<BlueprintServiceWebApplicationFactory>, IAsyncLifetime
{
    private readonly BlueprintServiceWebApplicationFactory _factory;
    private readonly List<HubConnection> _connections = new();

    public SignalRIntegrationTests(BlueprintServiceWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            if (connection.State == HubConnectionState.Connected)
                await connection.StopAsync();
            await connection.DisposeAsync();
        }
        _connections.Clear();
    }

    private HubConnection CreateHubConnection()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{_factory.Server.BaseAddress}actionshub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        _connections.Add(connection);
        return connection;
    }
}
```

### Testing Notification Receipt with Channels

```csharp
[Fact]
public async Task NotifyActionAvailable_ToSubscribedWallet_ReceivesNotification()
{
    // Arrange
    var connection = CreateHubConnection();
    var walletAddress = "0xabcdef1234567890";
    var receivedNotification = Channel.CreateUnbounded<ActionNotification>();

    connection.On<ActionNotification>("ActionAvailable", notification =>
    {
        receivedNotification.Writer.TryWrite(notification);
    });

    await connection.StartAsync();
    await connection.InvokeAsync("SubscribeToWallet", walletAddress);

    var notificationService = _factory.Services.GetRequiredService<INotificationService>();
    var expected = new ActionNotification
    {
        TransactionHash = "0x9876543210fedcba",
        WalletAddress = walletAddress,
        RegisterAddress = "0xregister123",
        Message = "New action available"
    };

    // Act
    await notificationService.NotifyActionAvailableAsync(expected);

    // Assert
    var received = await receivedNotification.Reader.ReadAsync(
        new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

    received.TransactionHash.Should().Be(expected.TransactionHash);
    received.WalletAddress.Should().Be(expected.WalletAddress);
}
```

See the **xunit** and **fluent-assertions** skills for testing patterns.

### Testing Non-Receipt (Unsubscribed)

```csharp
[Fact]
public async Task Notification_ToUnsubscribedWallet_DoesNotReceive()
{
    var connection = CreateHubConnection();
    var receivedNotification = Channel.CreateUnbounded<ActionNotification>();

    connection.On<ActionNotification>("ActionAvailable", n => receivedNotification.Writer.TryWrite(n));
    await connection.StartAsync();
    // NOT subscribing to wallet

    var notificationService = _factory.Services.GetRequiredService<INotificationService>();
    await notificationService.NotifyActionAvailableAsync(new ActionNotification
    {
        TransactionHash = "0xshouldnotreceive",
        WalletAddress = "0xunsubscribed999",
        RegisterAddress = "0xregister000"
    });

    // Assert - Should timeout
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var received = false;
    try
    {
        await receivedNotification.Reader.ReadAsync(cts.Token);
        received = true;
    }
    catch (OperationCanceledException) { /* Expected */ }

    received.Should().BeFalse("unsubscribed wallet should not receive notification");
}
```

### Test Validation Workflow

1. Create hub connection with test factory
2. Register message handlers with `connection.On<T>()`
3. Connect and subscribe to groups
4. Trigger notification via service
5. Assert receipt with timeout
6. If testing non-receipt, expect timeout

---

## Scaling with Redis Backplane

### WARNING: Current Status - Not Yet Implemented

Sorcha's SignalR hubs currently run single-instance. For multi-instance deployments, configure Redis backplane.

### Configuration (When Implemented)

```csharp
// Install: Microsoft.AspNetCore.SignalR.StackExchangeRedis

builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")!, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("sorcha-blueprint");
    });
```

See the **redis** skill for connection configuration.

### Aspire Integration

```csharp
// In AppHost
var redis = builder.AddRedis("redis");

builder.AddProject<Projects.Sorcha_Blueprint_Service>("blueprint")
    .WithReference(redis);
```

See the **aspire** skill for orchestration patterns.

---

## Client Connection Patterns

### Blazor Client Connection

```csharp
@inject NavigationManager Navigation
@implements IAsyncDisposable

@code {
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/actionshub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ActionNotification>("ActionAvailable", notification =>
        {
            // Update UI
            InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}
```

See the **blazor** skill for component patterns.

### Connection State Management

```csharp
// Handle reconnection
_hubConnection.Reconnecting += error =>
{
    _logger.LogWarning("Connection lost, attempting reconnect: {Error}", error?.Message);
    return Task.CompletedTask;
};

_hubConnection.Reconnected += connectionId =>
{
    _logger.LogInformation("Reconnected with ID: {ConnectionId}", connectionId);
    // Re-subscribe to groups after reconnect
    return ResubscribeToGroupsAsync();
};

_hubConnection.Closed += error =>
{
    _logger.LogError("Connection closed: {Error}", error?.Message);
    return Task.CompletedTask;
};
```

### Connection Retry Pattern

```csharp
public async Task EnsureConnectedAsync(CancellationToken ct)
{
    while (_hubConnection.State != HubConnectionState.Connected)
    {
        try
        {
            await _hubConnection.StartAsync(ct);
        }
        catch when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection failed, retrying...");
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}