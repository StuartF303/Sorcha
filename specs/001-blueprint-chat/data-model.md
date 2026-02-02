# Data Model: AI-Assisted Blueprint Design Chat

**Feature**: 001-blueprint-chat
**Date**: 2026-02-01

## Entities

### ChatSession

Represents an active conversation for designing a blueprint.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | string (GUID) | Required, Unique | Session identifier |
| UserId | string (GUID) | Required | Authenticated user ID |
| OrganizationId | string (GUID) | Required | User's organization |
| BlueprintDraft | Blueprint? | Optional | Current in-progress blueprint |
| ExistingBlueprintId | string? | Optional | If editing an existing blueprint |
| Messages | List&lt;ChatMessage&gt; | Max 100 items | Conversation history |
| Status | SessionStatus | Required | Active, Completed, Expired |
| CreatedAt | DateTimeOffset | Required | Session start time |
| LastActivityAt | DateTimeOffset | Required | Last message timestamp |

**Validation Rules**:
- Messages list capped at 100 (FR-019)
- Session expires after 24 hours of inactivity (SC-008)
- Only one active session per user (simplifies state management)

**State Transitions**:
```
[Created] → Active → Completed (user saves blueprint)
                   → Expired (24hr inactivity)
                   → Active (user resumes)
```

### ChatMessage

An individual message in the conversation.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | string (GUID) | Required, Unique | Message identifier |
| SessionId | string (GUID) | Required, FK | Parent session |
| Role | MessageRole | Required | User or Assistant |
| Content | string | Required, Max 10000 chars | Message text |
| ToolCalls | List&lt;ToolCall&gt;? | Optional | AI tool invocations |
| ToolResults | List&lt;ToolResult&gt;? | Optional | Tool execution outcomes |
| Timestamp | DateTimeOffset | Required | When message was created |
| IsStreaming | bool | Default false | True while AI is generating |

**Validation Rules**:
- Content cannot be empty
- ToolCalls only valid for Assistant role
- ToolResults must reference valid ToolCalls

### ToolCall

A request from the AI to execute a blueprint operation.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | string | Required | Tool call identifier (from AI) |
| ToolName | string | Required, Enum | One of defined tools |
| Arguments | JsonDocument | Required | Tool parameters |

**Valid Tool Names**:
- `create_blueprint`
- `add_participant`
- `remove_participant`
- `add_action`
- `update_action`
- `set_disclosure`
- `add_routing`
- `validate_blueprint`

### ToolResult

The outcome of executing a tool.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| ToolCallId | string | Required | References ToolCall.Id |
| Success | bool | Required | Whether execution succeeded |
| Result | JsonDocument? | Optional | Tool output data |
| Error | string? | Optional | Error message if failed |
| BlueprintChanged | bool | Required | Whether blueprint was modified |

## Enums

### SessionStatus
- `Active` - Session in use
- `Completed` - Blueprint saved successfully
- `Expired` - Timed out after 24 hours

### MessageRole
- `User` - Message from the human user
- `Assistant` - Message from the AI

## Relationships

```
ChatSession (1) ─────── (0..1) Blueprint
     │
     └──── (0..100) ChatMessage
                │
                ├──── (0..*) ToolCall
                │
                └──── (0..*) ToolResult
```

## Storage Schema (Redis)

### Session Storage
```
Key Pattern: chat:session:{sessionId}
Type: Hash
Fields:
  - userId: string
  - organizationId: string
  - status: string (enum value)
  - blueprintDraft: string (JSON)
  - existingBlueprintId: string (nullable)
  - createdAt: string (ISO 8601)
  - lastActivityAt: string (ISO 8601)
TTL: 86400 seconds (24 hours from last activity)
```

### Message Storage
```
Key Pattern: chat:messages:{sessionId}
Type: List (LPUSH/LRANGE)
Value: JSON array of ChatMessage objects
Max Length: 100 (LTRIM after each add)
```

### Active Session Index
```
Key Pattern: chat:user:{userId}:active
Type: String
Value: sessionId
TTL: Same as session (linked expiry)
```

## Indexes and Queries

| Query | Key Pattern | Operation |
|-------|-------------|-----------|
| Get session by ID | `chat:session:{sessionId}` | HGETALL |
| Get user's active session | `chat:user:{userId}:active` | GET |
| Get session messages | `chat:messages:{sessionId}` | LRANGE 0 -1 |
| Add message | `chat:messages:{sessionId}` | LPUSH + LTRIM |
| Update session activity | `chat:session:{sessionId}` | HSET + EXPIRE |

## Validation Summary

| Entity | Rule | Error Message |
|--------|------|---------------|
| ChatSession | Messages.Count <= 100 | "Session message limit reached (100)" |
| ChatSession | LastActivityAt within 24h | "Session expired" |
| ChatMessage | Content.Length <= 10000 | "Message too long" |
| ChatMessage | Content not empty | "Message cannot be empty" |
| ToolCall | ToolName in valid set | "Unknown tool: {name}" |
| ToolResult | ToolCallId exists | "Invalid tool call reference" |
