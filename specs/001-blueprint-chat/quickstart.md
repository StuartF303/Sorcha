# Developer Quickstart: AI-Assisted Blueprint Design Chat

**Feature**: 001-blueprint-chat
**Date**: 2026-02-01

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for Redis)
- Anthropic API key (or compatible AI provider)
- Sorcha solution cloned and buildable

## Environment Setup

### 1. Configure AI Provider

Add to your environment or `appsettings.Development.json`:

```bash
# Environment variable (recommended)
export ANTHROPIC_API_KEY="sk-ant-..."

# Or in appsettings.Development.json
{
  "AIProvider": {
    "Provider": "Anthropic",
    "ApiKey": "sk-ant-...",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096
  }
}
```

### 2. Start Infrastructure

```bash
# Start Redis and other dependencies
docker-compose up -d redis postgres

# Or start everything
docker-compose up -d
```

### 3. Run the Services

```bash
# Option 1: Run with Aspire (recommended for development)
dotnet run --project src/Apps/Sorcha.AppHost

# Option 2: Run Blueprint Service directly
dotnet run --project src/Services/Sorcha.Blueprint.Service
```

### 4. Access the Chat Interface

1. Navigate to `http://localhost/app/designer/chat` (via API Gateway)
2. Or directly: `https://localhost:7000/designer/chat` (Blueprint Service)
3. Log in with your Sorcha credentials
4. Start designing!

## Quick Test

### Via UI

1. Open the chat interface
2. Type: "Create a simple approval workflow where Alice submits a request and Bob approves it"
3. Watch the blueprint build in real-time
4. Say: "Add a rejection option for Bob"
5. Say: "Save the blueprint"

### Via SignalR Test Client

```csharp
// Quick test with SignalR client
var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7000/hubs/chat", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(jwtToken);
    })
    .Build();

connection.On<string>("ReceiveChunk", chunk => Console.Write(chunk));
connection.On<Blueprint, ValidationResult>("BlueprintUpdated", (bp, v) =>
    Console.WriteLine($"\nBlueprint: {bp.Title}, Valid: {v.IsValid}"));

await connection.StartAsync();
var sessionId = await connection.InvokeAsync<string>("StartSession");
await connection.InvokeAsync("SendMessage", sessionId,
    "Create a document approval workflow with two participants");
```

## Project Structure

```
src/Services/Sorcha.Blueprint.Service/
├── Hubs/
│   └── ChatHub.cs                    # SignalR hub
├── Services/
│   ├── Interfaces/
│   │   ├── IChatOrchestrationService.cs
│   │   ├── IAIProviderService.cs
│   │   └── IBlueprintToolExecutor.cs
│   ├── ChatOrchestrationService.cs   # Main orchestration
│   ├── AnthropicProviderService.cs   # Claude integration
│   └── BlueprintToolExecutor.cs      # Fluent API wrapper
└── Models/Chat/
    ├── ChatSession.cs
    ├── ChatMessage.cs
    └── ToolDefinitions.cs

src/Apps/Sorcha.UI/
├── Sorcha.UI.Core/
│   ├── Services/
│   │   └── ChatHubConnection.cs      # SignalR client
│   └── Models/Chat/
│       └── *.cs
└── Sorcha.UI.Web.Client/
    ├── Pages/
    │   └── BlueprintChat.razor       # Main page
    └── Components/Chat/
        ├── ChatPanel.razor
        ├── ChatMessage.razor
        └── BlueprintPreview.razor
```

## Key Interfaces

### IChatOrchestrationService

```csharp
public interface IChatOrchestrationService
{
    Task<ChatSession> CreateSessionAsync(ClaimsPrincipal user, string? blueprintId = null);
    Task<ChatSession?> GetSessionAsync(string sessionId);
    Task ProcessMessageAsync(
        string sessionId,
        string message,
        Func<string, Task> onChunk,
        Func<string, ToolResult, Task> onToolResult,
        Func<Blueprint, ValidationResult, Task> onBlueprintUpdate,
        CancellationToken cancellationToken = default);
    Task<Blueprint?> SaveBlueprintAsync(string sessionId);
    Task EndSessionAsync(string sessionId);
}
```

### IAIProviderService

```csharp
public interface IAIProviderService
{
    IAsyncEnumerable<AIStreamEvent> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

public abstract record AIStreamEvent;
public record TextChunk(string Text) : AIStreamEvent;
public record ToolUse(string Id, string Name, JsonDocument Arguments) : AIStreamEvent;
public record StreamEnd : AIStreamEvent;
```

### IBlueprintToolExecutor

```csharp
public interface IBlueprintToolExecutor
{
    Task<ToolResult> ExecuteAsync(
        string toolName,
        JsonDocument arguments,
        BlueprintBuilder builder,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ToolDefinition> GetToolDefinitions();
}
```

## Testing

### Run Unit Tests

```bash
dotnet test tests/Sorcha.Blueprint.Service.Tests --filter "Category=Chat"
```

### Run E2E Tests

```bash
# Ensure Docker services are running
docker-compose up -d

# Run Playwright tests
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=BlueprintChat"
```

## Debugging Tips

### Enable Detailed Logging

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Sorcha.Blueprint.Service.Services": "Debug",
      "Sorcha.Blueprint.Service.Hubs": "Debug"
    }
  }
}
```

### Inspect Redis Sessions

```bash
# Connect to Redis CLI
docker exec -it sorcha-redis redis-cli

# List all chat sessions
KEYS chat:session:*

# Get session details
HGETALL chat:session:{sessionId}

# Get session messages
LRANGE chat:messages:{sessionId} 0 -1
```

### Monitor SignalR Traffic

Use browser DevTools → Network → WS tab to inspect SignalR messages.

## Common Issues

### "AI service unavailable"

- Check `ANTHROPIC_API_KEY` is set
- Verify API key is valid: `curl -H "x-api-key: $ANTHROPIC_API_KEY" https://api.anthropic.com/v1/models`
- Check Blueprint.Service logs for detailed error

### "Session expired"

- Sessions expire after 24 hours of inactivity
- Start a new session with `StartSession()`

### "Message limit reached"

- Sessions are limited to 100 messages
- Save blueprint and start new session if needed

### SignalR connection fails

- Ensure JWT token is valid and not expired
- Check CORS configuration allows your origin
- Verify WebSocket is not blocked by proxy

## Next Steps

1. Review [Chat Hub Contract](./contracts/chat-hub.md)
2. Review [AI Tool Definitions](./contracts/ai-tools.md)
3. Review [Data Model](./data-model.md)
4. Check [Implementation Tasks](./tasks.md) (after running `/speckit.tasks`)
