# Chat Hub Contract

**Feature**: 001-blueprint-chat
**Hub Path**: `/hubs/chat`
**Authentication**: Required (JWT Bearer)

## Client-to-Server Methods

### StartSession

Initiates a new chat session or resumes an existing one.

```csharp
Task<string> StartSession(string? existingBlueprintId = null)
```

**Parameters**:
- `existingBlueprintId`: Optional blueprint ID to load for editing

**Returns**: Session ID (GUID string)

**Errors**:
- `401 Unauthorized`: User not authenticated
- `404 NotFound`: Blueprint ID not found (if provided)
- `409 Conflict`: User already has an active session

---

### SendMessage

Sends a user message to the AI assistant.

```csharp
Task SendMessage(string sessionId, string message)
```

**Parameters**:
- `sessionId`: Active session ID
- `message`: User's natural language input (max 10000 chars)

**Returns**: void (responses via server-to-client events)

**Errors**:
- `400 BadRequest`: Message empty or too long
- `404 NotFound`: Session not found or expired
- `409 Conflict`: Another message is being processed
- `429 TooManyRequests`: Rate limit exceeded

---

### CancelGeneration

Cancels the current AI response generation.

```csharp
Task CancelGeneration(string sessionId)
```

**Parameters**:
- `sessionId`: Active session ID

**Returns**: void

**Errors**:
- `404 NotFound`: Session not found
- `400 BadRequest`: No generation in progress

---

### SaveBlueprint

Saves the current draft blueprint to permanent storage.

```csharp
Task<string> SaveBlueprint(string sessionId)
```

**Parameters**:
- `sessionId`: Active session ID

**Returns**: Saved blueprint ID

**Errors**:
- `400 BadRequest`: No blueprint in draft, or blueprint invalid
- `404 NotFound`: Session not found

---

### ExportBlueprint

Exports the current draft as JSON or YAML.

```csharp
Task<string> ExportBlueprint(string sessionId, string format)
```

**Parameters**:
- `sessionId`: Active session ID
- `format`: "json" or "yaml"

**Returns**: Serialized blueprint content

**Errors**:
- `400 BadRequest`: Invalid format or no blueprint
- `404 NotFound`: Session not found

---

### EndSession

Explicitly ends a chat session.

```csharp
Task EndSession(string sessionId)
```

**Parameters**:
- `sessionId`: Session ID to end

**Returns**: void

**Errors**:
- `404 NotFound`: Session not found

---

## Server-to-Client Events

### SessionStarted

Fired when a session is successfully created or resumed.

```csharp
void SessionStarted(string sessionId, Blueprint? existingBlueprint, int messageCount)
```

**Parameters**:
- `sessionId`: The new or resumed session ID
- `existingBlueprint`: Blueprint if resuming or editing, null for new
- `messageCount`: Number of messages in session (for resume)

---

### ReceiveChunk

Streams AI response text as it's generated.

```csharp
void ReceiveChunk(string chunk)
```

**Parameters**:
- `chunk`: Partial text content (typically 1-50 characters)

**Notes**:
- Called multiple times per response
- Concatenate chunks to build full message
- Stop receiving when `MessageComplete` fires

---

### ToolExecuting

Notifies that the AI is executing a tool.

```csharp
void ToolExecuting(string toolName, JsonDocument arguments)
```

**Parameters**:
- `toolName`: Name of tool being executed
- `arguments`: Tool parameters (for display)

---

### ToolExecuted

Reports the result of a tool execution.

```csharp
void ToolExecuted(string toolName, bool success, string? error)
```

**Parameters**:
- `toolName`: Name of executed tool
- `success`: Whether execution succeeded
- `error`: Error message if failed, null if success

---

### BlueprintUpdated

Fired when the blueprint draft changes.

```csharp
void BlueprintUpdated(Blueprint blueprint, ValidationResult validation)
```

**Parameters**:
- `blueprint`: Current blueprint state
- `validation`: Validation result with any errors/warnings

---

### MessageComplete

Signals the end of an AI response.

```csharp
void MessageComplete(string messageId)
```

**Parameters**:
- `messageId`: ID of the completed message

---

### SessionError

Reports an error condition.

```csharp
void SessionError(string errorCode, string message)
```

**Parameters**:
- `errorCode`: Machine-readable error code
- `message`: Human-readable error description

**Error Codes**:
- `AI_UNAVAILABLE`: AI service failed after retries
- `SESSION_EXPIRED`: Session timed out
- `MESSAGE_LIMIT`: 100 message limit reached
- `RATE_LIMITED`: Too many requests

---

### MessageLimitWarning

Warns when approaching the message limit.

```csharp
void MessageLimitWarning(int remaining)
```

**Parameters**:
- `remaining`: Number of messages remaining before limit

**Notes**:
- Fired when remaining <= 10 messages

---

## Connection Lifecycle

```
1. Client connects to /hubs/chat with JWT
2. Client calls StartSession() → receives SessionStarted
3. Client calls SendMessage() repeatedly:
   - Receives ReceiveChunk (streaming)
   - Receives ToolExecuting/ToolExecuted (if tools used)
   - Receives BlueprintUpdated (if blueprint changed)
   - Receives MessageComplete (end of response)
4. Client calls SaveBlueprint() when satisfied
5. Client calls EndSession() or disconnects
```

## Reconnection Behavior

- Client should implement exponential backoff: 0s, 2s, 5s, 10s, 30s
- On reconnect, call `StartSession()` with no parameters to resume
- Server returns existing session if within 24 hours
- Messages and blueprint state are preserved
