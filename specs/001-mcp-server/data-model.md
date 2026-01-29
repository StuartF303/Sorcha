# Data Model: Sorcha MCP Server

**Feature**: 001-mcp-server
**Date**: 2026-01-29

## Overview

The MCP server is stateless - it does not persist data directly. This document defines the data structures used for tool inputs, outputs, and session management.

---

## Core Entities

### McpSession

Represents an authenticated MCP session.

| Field | Type | Description |
|-------|------|-------------|
| SessionId | string | Unique session identifier |
| UserId | Guid | User ID from JWT claims |
| TenantId | Guid | Tenant ID from JWT claims |
| OrganizationId | Guid? | Optional organization context |
| Roles | string[] | Role claims (admin, designer, participant) |
| WalletAddress | string? | Primary wallet address if participant |
| TokenExpiry | DateTimeOffset | JWT expiration time |
| CreatedAt | DateTimeOffset | Session start time |

**Validation Rules**:
- UserId is required
- TenantId is required
- At least one role is required
- TokenExpiry must be in the future

---

### ToolInvocation

Audit record for tool calls.

| Field | Type | Description |
|-------|------|-------------|
| InvocationId | Guid | Unique invocation ID |
| SessionId | string | Associated session |
| ToolName | string | Tool that was invoked |
| InputHash | string | SHA256 hash of input (for audit, not storing PII) |
| Success | bool | Whether tool completed successfully |
| DurationMs | long | Execution time in milliseconds |
| ErrorType | string? | Error category if failed |
| Timestamp | DateTimeOffset | When invocation occurred |

---

### RateLimitEntry

Tracks rate limit state per user/tenant.

| Field | Type | Description |
|-------|------|-------------|
| Key | string | Rate limit key (user:{id} or tenant:{id}) |
| WindowStart | DateTimeOffset | Start of current window |
| RequestCount | int | Requests in current window |
| WindowDurationSeconds | int | Window size (60 for per-minute) |

---

## Tool Input/Output Models

### Administrator Tools

#### HealthCheckInput
No input parameters required.

#### HealthCheckOutput
| Field | Type | Description |
|-------|------|-------------|
| OverallStatus | string | "Healthy", "Degraded", or "Unhealthy" |
| Services | ServiceHealth[] | Individual service statuses |
| CheckedAt | DateTimeOffset | When check was performed |

#### ServiceHealth
| Field | Type | Description |
|-------|------|-------------|
| ServiceName | string | e.g., "Blueprint", "Register" |
| Status | string | "Healthy", "Degraded", "Unhealthy" |
| ResponseTimeMs | long? | Response time if reachable |
| ErrorMessage | string? | Error details if unhealthy |
| Endpoint | string | Service endpoint URL |

---

#### LogQueryInput
| Field | Type | Description |
|-------|------|-------------|
| Service | string? | Filter by service name |
| Level | string? | Filter by log level (Debug, Info, Warning, Error) |
| StartTime | DateTimeOffset? | Start of time range |
| EndTime | DateTimeOffset? | End of time range |
| CorrelationId | string? | Filter by correlation ID |
| MaxResults | int | Maximum results (default 100, max 1000) |

#### LogQueryOutput
| Field | Type | Description |
|-------|------|-------------|
| Entries | LogEntry[] | Matching log entries |
| TotalCount | int | Total matches (may exceed returned) |
| Truncated | bool | Whether results were truncated |

#### LogEntry
| Field | Type | Description |
|-------|------|-------------|
| Timestamp | DateTimeOffset | Log timestamp |
| Level | string | Log level |
| Service | string | Source service |
| Message | string | Log message |
| CorrelationId | string? | Correlation ID if present |
| Properties | Dictionary<string, object> | Additional structured data |

---

#### TenantInput (for create/update)
| Field | Type | Description |
|-------|------|-------------|
| Name | string | Tenant name (3-100 chars) |
| Status | string? | Status for update (Active, Suspended) |

#### TenantOutput
| Field | Type | Description |
|-------|------|-------------|
| TenantId | Guid | Tenant identifier |
| Name | string | Tenant name |
| Status | string | Current status |
| CreatedAt | DateTimeOffset | Creation time |
| UserCount | int | Number of users |
| OrganizationCount | int | Number of organizations |

---

### Designer Tools

#### BlueprintListInput
| Field | Type | Description |
|-------|------|-------------|
| Status | string? | Filter by status (Draft, Published, Archived) |
| TitleContains | string? | Search in title |
| CreatedAfter | DateTimeOffset? | Filter by creation date |
| Page | int | Page number (1-based) |
| PageSize | int | Items per page (default 20, max 100) |

#### BlueprintListOutput
| Field | Type | Description |
|-------|------|-------------|
| Blueprints | BlueprintSummary[] | Blueprint summaries |
| Page | int | Current page |
| PageSize | int | Items per page |
| TotalCount | int | Total matching blueprints |
| TotalPages | int | Total pages |

#### BlueprintSummary
| Field | Type | Description |
|-------|------|-------------|
| BlueprintId | string | Unique identifier |
| Title | string | Blueprint title |
| Version | int | Version number |
| Status | string | Current status |
| ParticipantCount | int | Number of participants |
| ActionCount | int | Number of actions |
| CreatedAt | DateTimeOffset | Creation time |
| UpdatedAt | DateTimeOffset | Last update time |

---

#### BlueprintCreateInput
| Field | Type | Description |
|-------|------|-------------|
| Definition | string | Blueprint JSON or YAML |
| Format | string | "json" or "yaml" |

#### BlueprintCreateOutput
| Field | Type | Description |
|-------|------|-------------|
| BlueprintId | string | Created blueprint ID |
| Version | int | Version number (1 for new) |
| ValidationResult | ValidationResult | Validation details |

---

#### BlueprintValidateInput
| Field | Type | Description |
|-------|------|-------------|
| BlueprintId | string? | Existing blueprint ID |
| Definition | string? | Blueprint definition (if not using ID) |
| Format | string | "json" or "yaml" |

#### ValidationResult
| Field | Type | Description |
|-------|------|-------------|
| IsValid | bool | Overall validity |
| Errors | ValidationIssue[] | Validation errors |
| Warnings | ValidationIssue[] | Non-blocking warnings |

#### ValidationIssue
| Field | Type | Description |
|-------|------|-------------|
| Path | string | JSON path to issue location |
| Code | string | Error/warning code |
| Message | string | Human-readable message |
| Severity | string | "Error" or "Warning" |

---

#### DisclosureAnalysisInput
| Field | Type | Description |
|-------|------|-------------|
| BlueprintId | string | Blueprint to analyze |

#### DisclosureAnalysisOutput
| Field | Type | Description |
|-------|------|-------------|
| BlueprintId | string | Analyzed blueprint |
| Actions | ActionDisclosure[] | Per-action disclosure breakdown |

#### ActionDisclosure
| Field | Type | Description |
|-------|------|-------------|
| ActionId | int | Action sequence number |
| ActionTitle | string | Action title |
| Disclosures | ParticipantDisclosure[] | What each participant sees |

#### ParticipantDisclosure
| Field | Type | Description |
|-------|------|-------------|
| ParticipantId | string | Participant identifier |
| ParticipantName | string | Participant name |
| DisclosedFields | string[] | JSON pointers to visible fields |
| HiddenFields | string[] | Fields not disclosed |

---

### Participant Tools

#### InboxListInput
| Field | Type | Description |
|-------|------|-------------|
| Status | string? | Filter by status (Pending, Completed) |
| WorkflowId | string? | Filter by workflow |
| Page | int | Page number |
| PageSize | int | Items per page |

#### InboxListOutput
| Field | Type | Description |
|-------|------|-------------|
| Actions | PendingAction[] | Pending actions |
| Page | int | Current page |
| TotalCount | int | Total pending actions |

#### PendingAction
| Field | Type | Description |
|-------|------|-------------|
| ActionId | string | Unique action identifier |
| WorkflowId | string | Parent workflow ID |
| WorkflowTitle | string | Workflow title |
| ActionTitle | string | Action title |
| SenderName | string | Who sent this action |
| ReceivedAt | DateTimeOffset | When action was received |
| DueDate | DateTimeOffset? | Optional due date |

---

#### ActionSubmitInput
| Field | Type | Description |
|-------|------|-------------|
| ActionId | string | Action to respond to |
| Data | Dictionary<string, object> | Response data |
| DryRun | bool | If true, validate only |

#### ActionSubmitOutput
| Field | Type | Description |
|-------|------|-------------|
| Success | bool | Whether submission succeeded |
| TransactionId | string? | Created transaction ID |
| ValidationResult | ValidationResult | Validation details |
| NextAction | NextActionInfo? | Information about next step |

#### NextActionInfo
| Field | Type | Description |
|-------|------|-------------|
| ActionTitle | string | Next action title |
| AssignedTo | string | Participant receiving next action |
| WorkflowStatus | string | Updated workflow status |

---

#### WalletInfoOutput
| Field | Type | Description |
|-------|------|-------------|
| WalletAddress | string | Primary wallet address |
| Algorithm | string | Cryptographic algorithm |
| Status | string | Wallet status |
| LinkedIdentities | LinkedIdentity[] | Linked DIDs/credentials |
| CreatedAt | DateTimeOffset | Wallet creation time |

#### LinkedIdentity
| Field | Type | Description |
|-------|------|-------------|
| Type | string | "DID" or "VerifiableCredential" |
| Identifier | string | Identity identifier |
| LinkedAt | DateTimeOffset | When linked |

---

#### WalletSignInput
| Field | Type | Description |
|-------|------|-------------|
| Message | string | Message to sign (base64 or UTF-8) |
| Encoding | string | "base64" or "utf8" |

#### WalletSignOutput
| Field | Type | Description |
|-------|------|-------------|
| Signature | string | Base64-encoded signature |
| PublicKey | string | Base64-encoded public key |
| Algorithm | string | Algorithm used |
| SignedBy | string | Wallet address that signed |

---

## MCP Resource URIs

| URI Pattern | Returns | Permission |
|-------------|---------|------------|
| `sorcha://blueprints` | BlueprintSummary[] | designer, admin |
| `sorcha://blueprints/{id}` | Full blueprint JSON | designer, admin |
| `sorcha://inbox` | PendingAction[] | participant |
| `sorcha://workflows/{id}` | WorkflowStatus | participant (own workflows) |
| `sorcha://registers/{id}` | Register data (filtered) | participant (disclosed data) |
| `sorcha://schemas/{name}` | JSON Schema | designer |

---

## State Transitions

### Workflow Status
```
Created → InProgress → Completed
                    → Cancelled
                    → Failed
```

### Blueprint Status
```
Draft → Published → Archived
     → Archived (direct archive)
```

### Action Status
```
Pending → Completed
       → Rejected
       → Expired
```
