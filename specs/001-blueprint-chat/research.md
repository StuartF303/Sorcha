# Research: AI-Assisted Blueprint Design Chat

**Feature**: 001-blueprint-chat
**Date**: 2026-02-01

## Research Areas

### 1. AI Provider Integration

**Decision**: Use Anthropic Claude API with tool use (function calling)

**Rationale**:
- Claude's tool use API is well-suited for structured operations like blueprint manipulation
- Streaming support enables real-time response display (FR-002)
- Strong reasoning capabilities for understanding natural language workflow descriptions
- Existing MCP server in Sorcha already uses similar patterns

**Alternatives Considered**:
- **Azure OpenAI**: Good enterprise option but less flexible tool use patterns
- **Local LLM (Ollama)**: Lower cost but reduced quality for complex reasoning
- **OpenAI GPT-4**: Similar capabilities but Claude preferred for tool use ergonomics

**Implementation Notes**:
- Use `Anthropic.SDK` NuGet package for .NET integration
- Configure API key via environment variable `ANTHROPIC_API_KEY`
- Implement `IAIProviderService` abstraction to allow provider swapping
- Set `max_tokens` appropriately for streaming (4096 default)
- Use `claude-3-5-sonnet` model for balance of speed and capability

### 2. SignalR Streaming Pattern

**Decision**: Use SignalR hub with streaming methods for AI responses

**Rationale**:
- Sorcha.UI already uses SignalR (ActionsHubConnection, RegisterHubConnection)
- Native .NET support with strong typing
- Automatic reconnection handling built-in
- Supports both server-to-client streaming and client invocations

**Pattern**:
```
Client → Hub.SendMessage(sessionId, message)
Hub → Client.ReceiveChunk(chunk)        [streaming, multiple calls]
Hub → Client.ToolExecuted(tool, result) [when AI calls a tool]
Hub → Client.BlueprintUpdated(blueprint) [after tool modifies blueprint]
Hub → Client.MessageComplete()          [signals end of response]
```

**Alternatives Considered**:
- **Server-Sent Events (SSE)**: Simpler but one-way only
- **WebSocket raw**: More work, SignalR provides abstraction
- **Long polling**: Higher latency, not suitable for streaming

### 3. Session State Management

**Decision**: Use Redis for session state with 24-hour TTL

**Rationale**:
- Redis already configured in Sorcha infrastructure
- Supports atomic operations for concurrent access
- TTL aligns with SC-008 (24hr recovery requirement)
- Enables horizontal scaling of Blueprint.Service

**Schema**:
```
Key: chat:session:{sessionId}
Value: JSON {
  userId: string,
  blueprintDraft: Blueprint,
  messages: ChatMessage[],
  createdAt: DateTimeOffset,
  lastActivityAt: DateTimeOffset
}
TTL: 86400 seconds (24 hours)
```

**Alternatives Considered**:
- **In-memory (IMemoryCache)**: Lost on restart, doesn't scale
- **SQL Database**: Overkill for ephemeral session data
- **Browser LocalStorage**: Can't recover on different device

### 4. Blueprint Tool Definitions

**Decision**: Define 8 core tools mapped to Fluent API operations

**Rationale**:
- Matches the operations AI needs per functional requirements
- Each tool has clear input/output contract
- Tools are atomic and composable

**Tools**:
| Tool | Purpose | Fluent API Mapping |
|------|---------|-------------------|
| `create_blueprint` | Initialize new blueprint | `BlueprintBuilder.Create()` |
| `add_participant` | Add workflow actor | `.AddParticipant()` |
| `remove_participant` | Remove actor | (rebuild without participant) |
| `add_action` | Add workflow step | `.AddAction()` |
| `update_action` | Modify existing action | (rebuild with changes) |
| `set_disclosure` | Configure data visibility | `.Disclose()` |
| `add_routing` | Configure conditional flow | `.RouteConditionally()` |
| `validate_blueprint` | Check for errors | `BlueprintValidator.Validate()` |

**Alternatives Considered**:
- **Single "modify_blueprint" tool**: Less granular, harder for AI to use correctly
- **JSON patch operations**: Too low-level, error-prone
- **Natural language only**: No structured output, can't guarantee correctness

### 5. Retry and Error Handling

**Decision**: Polly-based retry with exponential backoff

**Rationale**:
- Industry standard for .NET resilience
- Configurable retry policies
- Integrates with HttpClientFactory

**Configuration**:
```csharp
// 3 retries: 2s, 4s, 8s (exponential backoff)
services.AddHttpClient<IAIProviderService>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))));
```

**Error Categories**:
- **Transient (retry)**: Network timeout, 503, 429 (rate limit)
- **Permanent (fail)**: 401 (auth), 400 (bad request), context too long
- **User-facing message**: Generic "AI service unavailable, please try again"

### 6. Concurrency Model

**Decision**: One active AI request per session, queue subsequent requests

**Rationale**:
- Prevents race conditions on blueprint state
- Simplifies UI (show "thinking" state)
- Aligns with natural conversation flow

**Implementation**:
- Use `SemaphoreSlim(1, 1)` per session
- If request arrives while processing, queue it
- Maximum queue depth: 1 (reject if already queued)

**Alternatives Considered**:
- **Allow concurrent**: Complex state merge, confusing UX
- **Reject immediately**: Poor UX if user types fast
- **Cancel previous**: May lose important context

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Anthropic.SDK | 1.0.0+ | Claude API client |
| Microsoft.AspNetCore.SignalR | (built-in) | Real-time communication |
| Polly | 8.0.0 | Retry policies |
| StackExchange.Redis | (existing) | Session storage |

## Security Considerations

- **API Key Storage**: Use Azure Key Vault or environment variables, never in code
- **Rate Limiting**: Apply authentication rate limit policy to chat endpoint
- **Input Sanitization**: AI prompts are user-controlled, validate before logging
- **Token Limits**: Monitor token usage per session to prevent abuse
- **Audit Trail**: Log all tool executions for compliance (existing ToolAuditService pattern)

## Performance Considerations

- **Connection Pooling**: Reuse HttpClient for AI provider
- **Message Batching**: Batch multiple tool results into single SignalR message
- **Blueprint Serialization**: Use System.Text.Json source generators for speed
- **Redis Pipelining**: Batch session reads/writes when possible
