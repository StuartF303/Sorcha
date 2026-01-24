# SignalR Patterns Reference

## Contents
- Hub Implementation Patterns
- Group-Based Routing
- Sending from Services
- Strongly-Typed Clients
- Error Handling
- Anti-Patterns

---

## Hub Implementation Patterns

### Basic Hub with Lifecycle Logging

From `src/Services/Sorcha.Blueprint.Service/Hubs/ActionsHub.cs`:

```csharp
public class ActionsHub : Hub
{
    private readonly ILogger<ActionsHub> _logger;

    public ActionsHub(ILogger<ActionsHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Client connected. ConnectionId: {ConnectionId}, User: {User}",
            Context.ConnectionId,
            Context.UserIdentifier ?? "anonymous");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            _logger.LogWarning(exception, "Client disconnected with error. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        else
            _logger.LogInformation("Client disconnected. ConnectionId: {ConnectionId}", Context.ConnectionId);
        
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Strongly-Typed Hub

From `src/Services/Sorcha.Register.Service/Hubs/RegisterHub.cs`:

```csharp
// GOOD - Compile-time safety for client method calls
public class RegisterHub : Hub<IRegisterHubClient>
{
    public async Task SubscribeToRegister(string registerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"register:{registerId}");
    }
}

public interface IRegisterHubClient
{
    Task RegisterCreated(string registerId, string name);
    Task RegisterDeleted(string registerId);
    Task TransactionConfirmed(string registerId, string transactionId);
    Task DocketSealed(string registerId, ulong docketId, string hash);
    Task RegisterHeightUpdated(string registerId, uint newHeight);
}
```

---

## Group-Based Routing

### Group Naming Convention

Sorcha uses namespaced group names for clarity:

| Scope | Format | Example |
|-------|--------|---------|
| Wallet | `wallet:{address}` | `wallet:0x1234abcd` |
| Register | `register:{id}` | `register:reg-001` |
| Tenant | `tenant:{id}` | `tenant:tenant-001` |
| Instance | `instance:{id}` | `instance:inst-001` |

### Subscription Methods

```csharp
// GOOD - Validate input before group operations
public async Task SubscribeToWallet(string walletAddress)
{
    if (string.IsNullOrWhiteSpace(walletAddress))
        throw new HubException("Wallet address cannot be empty");

    var groupName = $"wallet:{walletAddress}";
    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    
    _logger.LogInformation("Subscribed {ConnectionId} to {Group}", 
        Context.ConnectionId, groupName);
}

public async Task UnsubscribeFromWallet(string walletAddress)
{
    if (string.IsNullOrWhiteSpace(walletAddress))
        throw new HubException("Wallet address cannot be empty");

    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"wallet:{walletAddress}");
}
```

---

## Sending from Services

### Service Abstraction Pattern

From `src/Services/Sorcha.Blueprint.Service/Services/Implementation/NotificationService.cs`:

```csharp
public class NotificationService : INotificationService
{
    private readonly IHubContext<ActionsHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<ActionsHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyActionConfirmedAsync(ActionNotification notification, CancellationToken ct = default)
    {
        try
        {
            var groupName = $"wallet:{notification.WalletAddress}";
            
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("ActionConfirmed", notification, ct);

            _logger.LogInformation("Sent ActionConfirmed to wallet {Wallet}", notification.WalletAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ActionConfirmed to wallet {Wallet}", 
                notification.WalletAddress);
            throw;
        }
    }
}
```

### Typed Hub Context in Endpoints

From `src/Services/Sorcha.Register.Service/Program.cs`:

```csharp
// GOOD - Use typed IHubContext for compile-time safety
registersGroup.MapPost("/", async (
    RegisterManager manager,
    IHubContext<RegisterHub, IRegisterHubClient> hubContext,
    CreateRegisterRequest request) =>
{
    var register = await manager.CreateRegisterAsync(request.Name, request.TenantId);

    // Notify subscribers via strongly-typed client
    await hubContext.Clients
        .Group($"tenant:{register.TenantId}")
        .RegisterCreated(register.Id, register.Name);

    return Results.Created($"/api/registers/{register.Id}", register);
});
```

---

## Strongly-Typed Clients

### WARNING: String-Based SendAsync in Typed Hubs

**The Problem:**

```csharp
// BAD - Using SendAsync bypasses type safety
public class RegisterHub : Hub<IRegisterHubClient>
{
    public async Task NotifyCreation(string registerId, string name)
    {
        // Compiles but loses type safety benefits
        await Clients.All.SendAsync("RegisterCreated", registerId, name);
    }
}
```

**Why This Breaks:**
1. Method name typos cause silent failures
2. Parameter count/type mismatches aren't caught at compile time
3. Defeats the purpose of the typed interface

**The Fix:**

```csharp
// GOOD - Direct method call on typed interface
public class RegisterHub : Hub<IRegisterHubClient>
{
    public async Task NotifyCreation(string registerId, string name)
    {
        await Clients.All.RegisterCreated(registerId, name);
    }
}
```

---

## Error Handling

### Hub Method Validation

```csharp
// GOOD - Throw HubException for client-friendly errors
public async Task SubscribeToWallet(string walletAddress)
{
    if (string.IsNullOrWhiteSpace(walletAddress))
        throw new HubException("Wallet address cannot be empty");
    
    // Proceed with valid input
}
```

### Service-Level Error Handling

```csharp
// GOOD - Log and rethrow for visibility
public async Task NotifyActionAvailableAsync(ActionNotification notification, CancellationToken ct)
{
    try
    {
        await _hubContext.Clients.Group($"wallet:{notification.WalletAddress}")
            .SendAsync("ActionAvailable", notification, ct);
            
        _logger.LogInformation("Sent ActionAvailable to {Wallet}", notification.WalletAddress);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send notification to {Wallet}", notification.WalletAddress);
        throw; // Don't swallow - let caller handle
    }
}
```

---

## Anti-Patterns

### WARNING: Blocking Calls in Hub Methods

**The Problem:**

```csharp
// BAD - Blocking call in hub method
public Task ProcessData(string data)
{
    Thread.Sleep(5000);  // Blocks hub thread
    var result = SomeHeavyComputation(data);  // Synchronous
    return Clients.Caller.SendAsync("Result", result);
}
```

**Why This Breaks:**
1. Blocks SignalR's worker threads
2. Prevents other messages from being processed
3. Can cause connection timeouts

**The Fix:**

```csharp
// GOOD - Async all the way
public async Task ProcessData(string data)
{
    await Task.Delay(5000);  // Non-blocking
    var result = await Task.Run(() => SomeHeavyComputation(data));
    await Clients.Caller.SendAsync("Result", result);
}
```

### WARNING: Missing CancellationToken Support

**The Problem:**

```csharp
// BAD - No cancellation support
public async Task NotifyAsync(ActionNotification notification)
{
    await _hubContext.Clients.Group("all").SendAsync("Notify", notification);
}
```

**The Fix:**

```csharp
// GOOD - Support cancellation for graceful shutdown
public async Task NotifyAsync(ActionNotification notification, CancellationToken ct = default)
{
    await _hubContext.Clients.Group("all").SendAsync("Notify", notification, ct);
}
```

### WARNING: Coupling Business Logic to Hub

**The Problem:**

```csharp
// BAD - Business logic directly in hub
public async Task ExecuteAction(ExecuteActionRequest request)
{
    var result = await _actionService.ExecuteAsync(request);
    await _transactionService.SubmitAsync(result);
    await Clients.Group($"wallet:{request.WalletAddress}")
        .SendAsync("ActionCompleted", result);
}
```

**Why This Breaks:**
1. Hub becomes untestable
2. Business logic scattered across layers
3. Hard to reuse notification logic

**The Fix:**

```csharp
// GOOD - Hub only handles connection, services handle logic
public async Task ExecuteAction(ExecuteActionRequest request)
{
    // Delegate to service that uses INotificationService internally
    await _actionExecutionService.ExecuteAsync(request);
}