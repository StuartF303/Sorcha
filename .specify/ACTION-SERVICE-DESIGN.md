# Sorcha Action Service - Design Document

**Version:** 1.0
**Date:** 2025-11-15
**Status:** Approved for Implementation
**Related Documents:**
- [Specification](specs/sorcha-action-service.md)
- [Implementation Plan](ACTION-SERVICE-IMPLEMENTATION-PLAN.md)
- [Constitution](constitution.md)

## Executive Summary

The Sorcha Action Service is a critical microservice that enables participant interaction within Blueprint-controlled workflows. It serves as the bridge between Blueprint definitions, participant wallets, and the transaction register, orchestrating the execution of multi-participant data flow workflows.

**Key Capabilities:**
- Action retrieval and filtering for participants
- Schema-validated action submissions
- Privacy-preserving selective data disclosure
- Transaction construction and coordination
- Real-time notifications via SignalR
- File attachment support

**Technology:** .NET 10, ASP.NET Core Minimal APIs, SignalR, Redis, OpenAPI/Scalar

**Timeline:** 14 weeks (114 tasks across 10 phases)

**Team:** 2 backend developers, 1 QA engineer, 1 DevOps engineer

---

## 1. Service Overview

### 1.1 Purpose

The Action Service manages the lifecycle of participant actions within Blueprint workflows:

1. **Discovery** - Help participants find actions they can perform
2. **Retrieval** - Provide action details with historical context
3. **Submission** - Accept and validate action responses
4. **Coordination** - Build and submit transactions to the register
5. **Notification** - Inform participants of action state changes

### 1.2 Position in Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Sorcha Platform                           │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐     ┌──────────────┐     ┌─────────────┐ │
│  │  Blueprint   │────▶│   Action     │────▶│  Register   │ │
│  │  Service     │     │   Service    │     │  Service    │ │
│  │              │     │              │     │             │ │
│  │ • Definitions│     │ • Retrieval  │     │ • Storage   │ │
│  │ • Schemas    │     │ • Submission │     │ • History   │ │
│  └──────────────┘     │ • Validation │     │ • Events    │ │
│                       │ • Encryption │     └─────────────┘ │
│                       │ • Notify     │             │        │
│                       └──────┬───────┘             │        │
│                              │                     │        │
│  ┌──────────────┐           │                     │        │
│  │   Wallet     │◀──────────┘                     │        │
│  │   Service    │                                 │        │
│  │              │                                 │        │
│  │ • Keys       │                                 │        │
│  │ • Encryption │                                 │        │
│  │ • Signing    │                                 │        │
│  └──────────────┘                                 │        │
│                                                    │        │
│  ┌──────────────────────────────────────────────┐│        │
│  │          Peer Service (Distribution)          ││        │
│  └──────────────────────────────────────────────┘│        │
│                       ▲                            │        │
│                       └────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

### 1.3 Core Workflows

#### Workflow 1: Start New Blueprint Instance

```
1. User → GET /api/actions/{wallet}/{register}/blueprints
   ↓
2. Action Service → Get published Blueprints (Blueprint Service)
   ↓
3. Action Service → Filter by starting actions
   ↓
4. Action Service → Return available starting actions
   ↓
5. User → POST /api/actions (with data for starting action)
   ↓
6. Action Service → Validate against schema
   ↓
7. Action Service → Encrypt payloads (Wallet Service)
   ↓
8. Action Service → Build transaction
   ↓
9. Action Service → Submit transaction (Register Service)
   ↓
10. Register Service → Confirm transaction
   ↓
11. Action Service → Broadcast notification (SignalR)
   ↓
12. Next participant receives notification
```

#### Workflow 2: Continue Existing Instance

```
1. User → GET /api/actions/{wallet}/{register}
   ↓
2. Action Service → Get pending transactions for wallet (Register Service)
   ↓
3. Action Service → Filter by Blueprint transactions
   ↓
4. Action Service → Return pending actions
   ↓
5. User → GET /api/actions/{wallet}/{register}/{tx}
   ↓
6. Action Service → Get Blueprint definition
   ↓
7. Action Service → Get transaction history
   ↓
8. Action Service → Aggregate previous data
   ↓
9. Action Service → Decrypt payloads (Wallet Service)
   ↓
10. Action Service → Return action with context
   ↓
11. User → POST /api/actions (with response data)
   ↓
12. [Same as steps 6-12 of Workflow 1]
```

#### Workflow 3: Reject Action

```
1. User → POST /api/actions/reject (with reason)
   ↓
2. Action Service → Get previous transaction
   ↓
3. Action Service → Identify previous participant
   ↓
4. Action Service → Build rejection transaction
   ↓
5. Action Service → Submit to Register Service
   ↓
6. Register Service → Confirm rejection
   ↓
7. Action Service → Notify previous participant
```

---

## 2. Detailed Design

### 2.1 Component Architecture

```
Sorcha.Action.Service
│
├── Program.cs (Entry point, service configuration)
│
├── Endpoints/ (Minimal API endpoints)
│   ├── ActionsEndpoints.cs
│   ├── FilesEndpoints.cs
│   └── NotificationsEndpoints.cs
│
├── Services/ (Business logic)
│   ├── Interfaces/
│   │   ├── IActionService.cs
│   │   ├── IActionResolver.cs
│   │   ├── IPayloadResolver.cs
│   │   ├── ITransactionRequestBuilder.cs
│   │   └── ICalculationService.cs
│   │
│   └── Implementations/
│       ├── ActionService.cs
│       ├── ActionResolver.cs
│       ├── PayloadResolver.cs
│       ├── TransactionRequestBuilder.cs
│       └── CalculationService.cs
│
├── Hubs/ (SignalR)
│   └── ActionsHub.cs
│
├── Models/ (DTOs)
│   ├── Requests/
│   │   ├── ActionSubmission.cs
│   │   ├── ActionRejection.cs
│   │   └── ActionFilter.cs
│   │
│   ├── Responses/
│   │   ├── ActionResponse.cs
│   │   ├── ActionSummary.cs
│   │   ├── TransactionResult.cs
│   │   └── PagedResult.cs
│   │
│   └── Internal/
│       ├── FileAttachment.cs
│       └── TransactionConfirmed.cs
│
├── Validators/ (FluentValidation)
│   ├── ActionSubmissionValidator.cs
│   ├── ActionRejectionValidator.cs
│   └── FileAttachmentValidator.cs
│
├── Exceptions/ (Custom exceptions)
│   ├── ActionNotFoundException.cs
│   ├── BlueprintNotFoundException.cs
│   ├── UnauthorizedActionException.cs
│   └── ValidationException.cs
│
├── Extensions/ (Extension methods)
│   ├── ServiceCollectionExtensions.cs
│   └── WebApplicationExtensions.cs
│
└── Configuration/
    └── ActionServiceOptions.cs
```

### 2.2 Service Interfaces

#### IActionService

Primary service for action operations.

```csharp
public interface IActionService
{
    // Retrieval
    Task<IEnumerable<ActionResponse>> GetStartingActionsAsync(
        string walletAddress,
        string registerId,
        CancellationToken ct = default);

    Task<PagedResult<ActionSummary>> GetPendingActionsAsync(
        string walletAddress,
        string registerId,
        ActionFilter filter,
        CancellationToken ct = default);

    Task<ActionResponse?> GetActionByIdAsync(
        string walletAddress,
        string registerId,
        string transactionId,
        bool aggregateData = true,
        CancellationToken ct = default);

    // Submission
    Task<TransactionResult> SubmitActionAsync(
        ActionSubmission submission,
        CancellationToken ct = default);

    Task<TransactionResult> RejectActionAsync(
        ActionRejection rejection,
        CancellationToken ct = default);
}
```

**Key Responsibilities:**
- Orchestrate action retrieval workflows
- Coordinate action submission workflows
- Integrate with dependent services
- Apply business rules
- Handle errors and retries

**Implementation Notes:**
- Use dependency injection for service clients
- Apply caching for Blueprint and transaction data
- Implement circuit breakers for external calls
- Log all operations with correlation IDs

#### IActionResolver

Resolves Blueprints and action definitions.

```csharp
public interface IActionResolver
{
    Task<Blueprint> GetBlueprintAsync(
        string blueprintId,
        string registerId,
        CancellationToken ct = default);

    Task<Models.Action> GetActionDefinitionAsync(
        string blueprintId,
        string actionId,
        string registerId,
        CancellationToken ct = default);

    Task<Dictionary<string, string>> ResolveParticipantWalletsAsync(
        Blueprint blueprint,
        string instanceId,
        string registerId,
        CancellationToken ct = default);

    Task<IEnumerable<Blueprint>> GetPublishedBlueprintsAsync(
        string registerId,
        CancellationToken ct = default);
}
```

**Key Responsibilities:**
- Fetch Blueprint definitions from Blueprint Service
- Cache Blueprints for performance (10 min TTL)
- Extract action definitions from Blueprints
- Resolve participant IDs to wallet addresses
- Validate Blueprint structure

**Implementation Notes:**
- Use `IMemoryCache` or Redis for Blueprint caching
- Cache key: `blueprint:{registerId}:{blueprintId}`
- Invalidate cache on Blueprint updates (future)
- Handle Blueprint not found gracefully

#### IPayloadResolver

Handles payload encryption, decryption, and data aggregation.

```csharp
public interface IPayloadResolver
{
    Task<IEnumerable<Payload>> CreatePayloadsAsync(
        ActionSubmission submission,
        Blueprint blueprint,
        Models.Action action,
        Dictionary<string, string> participantWallets,
        CancellationToken ct = default);

    Task<Dictionary<string, object>> AggregateHistoricalDataAsync(
        string transactionId,
        string walletAddress,
        Blueprint blueprint,
        Models.Action action,
        string registerId,
        CancellationToken ct = default);

    Task<byte[]> EncryptPayloadAsync(
        object data,
        IEnumerable<string> recipientWallets,
        CancellationToken ct = default);

    Task<object> DecryptPayloadAsync(
        byte[] encryptedData,
        string walletAddress,
        CancellationToken ct = default);

    Dictionary<string, object> FilterByDisclosure(
        Dictionary<string, object> data,
        Disclosure disclosure);
}
```

**Key Responsibilities:**
- Create encrypted payloads based on Disclosure rules
- Apply selective disclosure using JSON Pointers
- Aggregate data from previous transactions
- Decrypt payloads for authorized wallets
- Separate tracking data from participant data

**Implementation Notes:**
- Use JSON Pointer (RFC 6901) for field selection
- Support both `/field/subfield` and `#/field/subfield` formats
- Call WalletService for encryption/decryption operations
- Handle missing fields gracefully
- Validate disclosure rules before applying

#### ITransactionRequestBuilder

Builds transaction requests for submission.

```csharp
public interface ITransactionRequestBuilder
{
    Task<TransactionRequest> BuildNewInstanceTransactionAsync(
        ActionSubmission submission,
        Blueprint blueprint,
        CancellationToken ct = default);

    Task<TransactionRequest> BuildContinuationTransactionAsync(
        ActionSubmission submission,
        string previousTransactionHash,
        Blueprint blueprint,
        CancellationToken ct = default);

    Task<TransactionRequest> BuildRejectionTransactionAsync(
        ActionRejection rejection,
        string previousTransactionHash,
        Blueprint blueprint,
        CancellationToken ct = default);

    Task<IEnumerable<TransactionRequest>> BuildFileTransactionsAsync(
        IEnumerable<FileAttachment> files,
        string walletAddress,
        string registerId,
        CancellationToken ct = default);
}
```

**Key Responsibilities:**
- Build transaction metadata (Blueprint ID, Action ID, Instance ID)
- Determine transaction type (Action, Rejection, File)
- Link to previous transaction (continuation)
- Generate instance IDs for new workflows
- Build file transactions separately
- Apply digital signatures

**Implementation Notes:**
- Use `Guid.NewGuid()` for instance IDs
- Metadata format: JSON with `blueprintId`, `actionId`, `instanceId`, `tracking`
- File transactions created before action transaction
- Update action submission with file transaction IDs
- Use WalletService for transaction signing

#### ICalculationService

Evaluates JSON Logic calculations and conditions.

```csharp
public interface ICalculationService
{
    Task<Dictionary<string, object>> ApplyCalculationsAsync(
        Dictionary<string, object> data,
        IEnumerable<Calculation> calculations,
        CancellationToken ct = default);

    Task<string?> EvaluateRoutingConditionsAsync(
        Dictionary<string, object> data,
        IEnumerable<Condition> conditions,
        CancellationToken ct = default);

    object EvaluateJsonLogic(
        JsonNode logic,
        Dictionary<string, object> data);
}
```

**Key Responsibilities:**
- Execute JSON Logic expressions
- Apply calculated fields to data
- Evaluate routing conditions
- Determine next action participant
- Validate calculation results

**Implementation Notes:**
- Use JsonLogic.Net library
- Cache compiled expressions (future optimization)
- Handle division by zero and other errors
- Log evaluation results for debugging
- Support all JSON Logic operators

### 2.3 Data Flow

#### Action Submission Flow

```
1. HTTP Request
   ↓
2. ActionsEndpoints.PostAction()
   ↓
3. ActionSubmissionValidator.ValidateAsync()
   ↓
4. IActionService.SubmitActionAsync()
   ├─▶ IActionResolver.GetBlueprintAsync()
   ├─▶ ICalculationService.ApplyCalculationsAsync()
   ├─▶ JSON Schema Validation
   ├─▶ IPayloadResolver.CreatePayloadsAsync()
   │   └─▶ WalletService.EncryptPayload()
   ├─▶ ITransactionRequestBuilder.BuildFileTransactionsAsync()
   │   └─▶ RegisterService.SubmitTransaction()
   ├─▶ ITransactionRequestBuilder.BuildContinuationTransactionAsync()
   └─▶ RegisterService.SubmitTransaction()
   ↓
5. Return 202 Accepted with transaction ID
   ↓
6. RegisterService confirms transaction
   ↓
7. POST /api/actions/notify (internal)
   ↓
8. ActionsHub.BroadcastActionConfirmed()
   ↓
9. SignalR clients receive notification
```

#### Action Retrieval Flow

```
1. HTTP Request
   ↓
2. ActionsEndpoints.GetActionById()
   ↓
3. IActionService.GetActionByIdAsync()
   ├─▶ RegisterService.GetTransaction()
   ├─▶ IActionResolver.GetBlueprintAsync()
   ├─▶ IActionResolver.GetActionDefinitionAsync()
   ├─▶ IPayloadResolver.AggregateHistoricalDataAsync()
   │   ├─▶ RegisterService.GetTransactionHistory()
   │   ├─▶ WalletService.DecryptPayload()
   │   └─▶ IPayloadResolver.FilterByDisclosure()
   └─▶ Build ActionResponse
   ↓
4. Return 200 OK with action details
```

### 2.4 Caching Strategy

#### Blueprint Cache
- **Key:** `blueprint:{registerId}:{blueprintId}`
- **TTL:** 10 minutes
- **Invalidation:** Manual tag-based eviction (future)
- **Storage:** Redis

#### Pending Actions Cache
- **Key:** `actions:pending:{walletAddress}:{registerId}`
- **TTL:** 1 minute
- **Invalidation:** On new transaction confirmed
- **Storage:** Redis

#### Action Details Cache
- **Key:** `action:details:{transactionId}`
- **TTL:** 2 minutes
- **Invalidation:** On transaction updated
- **Storage:** Redis

#### Starting Actions Cache
- **Key:** `actions:starting:{registerId}`
- **TTL:** 5 minutes
- **Invalidation:** On new Blueprint published
- **Storage:** Redis

### 2.5 Error Handling

#### Error Categories

1. **Validation Errors (400 Bad Request)**
   - Invalid request format
   - Schema validation failure
   - Missing required fields
   - File too large
   - Invalid file type

2. **Authentication Errors (401 Unauthorized)**
   - Missing JWT token
   - Invalid JWT token
   - Expired token

3. **Authorization Errors (403 Forbidden)**
   - Wallet not authorized for action
   - Not a participant in Blueprint
   - Transaction not for this wallet

4. **Not Found Errors (404 Not Found)**
   - Blueprint not found
   - Action not found
   - Transaction not found
   - Wallet not found

5. **Server Errors (500 Internal Server Error)**
   - Dependency service unavailable
   - Database errors
   - Encryption errors
   - Unexpected exceptions

#### Error Response Format

Use ASP.NET Core ProblemDetails:

```json
{
  "type": "https://sorcha.dev/errors/validation",
  "title": "Validation Failed",
  "status": 400,
  "detail": "The submitted data does not match the required schema",
  "instance": "/api/actions",
  "traceId": "00-1234567890abcdef-fedcba0987654321-00",
  "errors": {
    "quantity": ["Must be greater than 0"],
    "price": ["Must be a valid decimal number"]
  }
}
```

#### Retry Strategy

For external service calls:
- **Transient errors:** Retry 3 times with exponential backoff (2s, 4s, 8s)
- **Circuit breaker:** Open after 5 consecutive failures, half-open after 30s
- **Timeout:** 10 seconds per request
- **Bulkhead:** Max 100 concurrent requests per service

### 2.6 Security Design

#### Authentication

JWT Bearer token required for all endpoints except `/health` and `/alive`.

**Token Claims:**
- `sub`: Wallet address
- `tenant`: Tenant ID (for multi-tenancy)
- `iss`: Token issuer
- `aud`: Must include "action-service"
- `exp`: Expiration timestamp

**Validation:**
- Verify signature using issuer's public key
- Check expiration
- Verify audience includes "action-service"
- Verify wallet address matches request

#### Authorization

**Endpoint-Level:**
- All endpoints require authenticated wallet
- Validate wallet address in request matches JWT claim

**Action-Level:**
- Verify wallet is a participant in the Blueprint
- Check wallet has permission for specific action
- Validate transaction ownership for updates

**Delegation (Future):**
- Support read-only delegates
- Support read-write delegates
- Time-based delegation expiration

#### Rate Limiting

**Per Wallet:**
- 100 requests per minute
- 1000 requests per hour

**Global:**
- 10,000 requests per minute per instance

**Response:**
- 429 Too Many Requests
- `Retry-After` header with seconds

#### Input Validation

**Request Validation:**
- Max request size: 4MB
- Max file count: 10 per action
- Max file size: 10MB per file
- Allowed file types: PDF, JPEG, PNG, XLSX

**Data Validation:**
- JSON Schema validation against Blueprint schema
- FluentValidation for request DTOs
- Sanitize file names
- Validate MIME types

**Injection Prevention:**
- Use parameterized queries (if SQL used)
- Escape special characters in logs
- Validate JSON Logic expressions
- Prevent XXE attacks in XML files

#### Audit Logging

Log security-relevant events:
- Failed authentication attempts
- Authorization failures
- Validation failures
- Suspicious activity (rapid requests, large payloads)
- Data access (who accessed what action)

**Log Format:**
```json
{
  "timestamp": "2025-11-15T12:00:00Z",
  "eventType": "ActionAccessed",
  "walletAddress": "wallet_abc123",
  "transactionId": "tx_67890",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "result": "success",
  "correlationId": "00-1234...321-00"
}
```

---

## 3. API Design

### 3.1 Endpoint Summary

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| GET | `/api/actions/{wallet}/{register}/blueprints` | Get starting actions | JWT |
| GET | `/api/actions/{wallet}/{register}` | Get pending actions | JWT |
| GET | `/api/actions/{wallet}/{register}/{tx}` | Get action details | JWT |
| POST | `/api/actions` | Submit action | JWT |
| POST | `/api/actions/reject` | Reject action | JWT |
| GET | `/api/files/{wallet}/{register}/{tx}/{fileId}` | Download file | JWT |
| POST | `/api/actions/notify` | Internal notification | Internal |
| GET | `/health` | Health check | None |
| GET | `/alive` | Liveness check | None |
| GET | `/openapi/v1.json` | OpenAPI spec | None |
| GET | `/scalar/v1` | API documentation | None |

### 3.2 SignalR Hub

**Endpoint:** `wss://action-service/actionshub`

**Client Methods:**
- `ActionAvailable(notification)` - New action available
- `ActionConfirmed(notification)` - Action submission confirmed
- `ActionRejected(notification)` - Action rejected

**Server Methods:**
- `SubscribeToWallet(walletAddress)` - Subscribe to wallet notifications
- `UnsubscribeFromWallet(walletAddress)` - Unsubscribe

### 3.3 OpenAPI Documentation

All endpoints documented using .NET 10 built-in OpenAPI support.

**Metadata:**
- Summary (short description)
- Description (detailed explanation)
- Request schema with examples
- Response schemas with examples
- Status codes with meanings
- Authentication requirements

**Example:**
```csharp
app.MapPost("/api/actions", async (
    [FromBody] ActionSubmission submission,
    IActionService actionService,
    CancellationToken ct) =>
{
    var result = await actionService.SubmitActionAsync(submission, ct);
    return Results.Accepted($"/api/actions/status/{result.TransactionId}", result);
})
.WithName("SubmitAction")
.WithSummary("Submit a completed action")
.WithDescription("Validates submission data against Blueprint schema, encrypts payloads, and creates transaction")
.Accepts<ActionSubmission>("application/json")
.Produces<TransactionResult>(202)
.Produces<ProblemDetails>(400)
.Produces<ProblemDetails>(401)
.Produces<ProblemDetails>(403)
.Produces<ProblemDetails>(404)
.RequireAuthorization();
```

---

## 4. Testing Strategy

### 4.1 Unit Testing

**Coverage Target:** >80%

**Test Structure:**
```
Sorcha.Action.Service.Tests/
├── Services/
│   ├── ActionServiceTests.cs
│   ├── ActionResolverTests.cs
│   ├── PayloadResolverTests.cs
│   ├── TransactionRequestBuilderTests.cs
│   └── CalculationServiceTests.cs
├── Validators/
│   ├── ActionSubmissionValidatorTests.cs
│   └── ActionRejectionValidatorTests.cs
└── Endpoints/
    ├── ActionsEndpointsTests.cs
    └── FilesEndpointsTests.cs
```

**Mocking:**
- Mock external service clients (WalletService, RegisterService, etc.)
- Mock IMemoryCache for caching tests
- Mock ILogger for logging verification
- Use Moq for all mocks

**Test Naming:**
```csharp
[Fact]
public async Task SubmitActionAsync_ValidSubmission_ReturnsTransactionResult()
{
    // Arrange
    var submission = new ActionSubmission { /* ... */ };
    // ... setup mocks

    // Act
    var result = await _actionService.SubmitActionAsync(submission);

    // Assert
    result.Should().NotBeNull();
    result.TransactionId.Should().NotBeNullOrEmpty();
}
```

### 4.2 Integration Testing

**Test Structure:**
```
Sorcha.Action.Service.Integration.Tests/
├── ActionsEndpointsTests.cs
├── FilesEndpointsTests.cs
├── NotificationsTests.cs
├── SignalRHubTests.cs
└── Fixtures/
    ├── ActionServiceWebApplicationFactory.cs
    └── TestDataBuilder.cs
```

**Test Configuration:**
- Use WebApplicationFactory for in-memory hosting
- Use Testcontainers for Redis
- Mock external services with WireMock
- Use test database (in-memory or containerized)

**Example:**
```csharp
public class ActionsEndpointsTests : IClassFixture<ActionServiceWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ActionsEndpointsTests(ActionServiceWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPendingActions_WithValidWallet_ReturnsActions()
    {
        // Arrange
        var wallet = "wallet_test123";
        var register = "register_1";
        var token = GenerateTestToken(wallet);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            $"/api/actions/{wallet}/{register}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ActionSummary>>();
        result.Should().NotBeNull();
    }
}
```

### 4.3 End-to-End Testing

**Scenarios:**
1. Complete workflow: Start → Submit → Notify → Continue → Complete
2. Rejection workflow: Submit → Reject → Notify → Resubmit
3. File attachment workflow: Submit with files → Retrieve action → Download files
4. Multi-participant workflow: Multiple actions across participants
5. Error scenarios: Invalid data, unauthorized access, service failures

**Tools:**
- xUnit for test framework
- FluentAssertions for readable assertions
- Testcontainers for dependencies
- NBomber for load testing (optional)

### 4.4 Performance Testing

**Targets:**
- GET endpoints: < 200ms (p95)
- POST endpoints: < 500ms (p95)
- Throughput: 1000 req/s per instance
- SignalR: 10,000 concurrent connections

**Load Tests:**
- Gradual ramp-up: 0 → 1000 req/s over 5 minutes
- Sustained load: 1000 req/s for 30 minutes
- Spike test: 0 → 5000 req/s → 0 over 1 minute
- Stress test: Increase until failure

**Monitoring:**
- Response times (min, max, avg, p50, p95, p99)
- Throughput (req/s)
- Error rate (%)
- CPU usage (%)
- Memory usage (MB)
- Database connections

---

## 5. Deployment

### 5.1 Container Configuration

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Services/Sorcha.Action.Service/Sorcha.Action.Service.csproj", "src/Services/Sorcha.Action.Service/"]
RUN dotnet restore "src/Services/Sorcha.Action.Service/Sorcha.Action.Service.csproj"
COPY . .
WORKDIR "/src/src/Services/Sorcha.Action.Service"
RUN dotnet build "Sorcha.Action.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sorcha.Action.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sorcha.Action.Service.dll"]
```

**Image:**
- Registry: `ghcr.io/sorcha/action-service`
- Tags: `latest`, `v1.0.0`, `{git-sha}`
- Size target: < 200MB
- Security: Non-root user, minimal base image

### 5.2 Aspire Configuration

**AppHost Integration:**
```csharp
var actionService = builder.AddProject<Projects.Sorcha_Action_Service>("action-service")
    .WithHttpHealthCheck("/health")
    .WithReference(walletService)
    .WithReference(registerService)
    .WithReference(blueprintService)
    .WithReference(redis)
    .WaitFor(walletService)
    .WaitFor(registerService)
    .WaitFor(blueprintService)
    .WaitFor(redis);
```

### 5.3 Scaling Strategy

**Horizontal Scaling:**
- Minimum instances: 2 (HA)
- Maximum instances: 10 (burst)
- Scale metric: CPU > 70% or Requests > 800/s
- Scale-down delay: 5 minutes

**SignalR Scale-Out:**
- Redis backplane for message distribution
- Sticky sessions for WebSocket connections
- Graceful connection draining on scale-down

**Resource Limits:**
- CPU: 0.5 cores (request), 2 cores (limit)
- Memory: 512MB (request), 2GB (limit)
- Storage: 1GB (logs, temp files)

---

## 6. Monitoring & Observability

### 6.1 Metrics

**Application Metrics:**
- `actions_retrieved_total` - Counter of action retrievals
- `actions_submitted_total` - Counter of action submissions
- `actions_rejected_total` - Counter of action rejections
- `action_submission_duration_ms` - Histogram of submission latency
- `signalr_connections_total` - Gauge of active SignalR connections
- `cache_hits_total` / `cache_misses_total` - Cache effectiveness

**Infrastructure Metrics:**
- CPU usage (%)
- Memory usage (MB)
- Request rate (req/s)
- Error rate (%)
- Response time (ms)

### 6.2 Logging

**Log Levels:**
- **Debug:** Detailed flow for troubleshooting
- **Info:** Action submissions, retrievals, confirmations
- **Warning:** Validation failures, retries, cache misses
- **Error:** Service failures, exceptions, security violations
- **Critical:** Service unavailable, data corruption

**Structured Logging:**
```csharp
_logger.LogInformation(
    "Action submitted: {TransactionId} by {WalletAddress} for {BlueprintId}/{ActionId}",
    transactionId, walletAddress, blueprintId, actionId);
```

### 6.3 Distributed Tracing

**OpenTelemetry:**
- Trace all HTTP requests
- Trace all service calls (WalletService, RegisterService, etc.)
- Trace SignalR connections
- Include correlation IDs in all logs

**Trace Attributes:**
- `wallet.address`
- `blueprint.id`
- `action.id`
- `transaction.id`
- `service.name`
- `http.method`
- `http.status_code`

### 6.4 Health Checks

**Health Endpoint (`/health`):**
- Self health (service running)
- Redis connectivity
- WalletService availability
- RegisterService availability
- BlueprintService availability

**Liveness Endpoint (`/alive`):**
- Basic service responsiveness
- No dependency checks

---

## 7. Open Questions & Decisions

### Decision Log

| ID | Decision | Date | Rationale |
|----|----------|------|-----------|
| D001 | Use Minimal APIs instead of Controllers | 2025-11-15 | Modern, lightweight, aligns with Sorcha standard |
| D002 | Use Redis for caching | 2025-11-15 | Already used by Blueprint Service, supports distributed cache |
| D003 | Use SignalR for notifications | 2025-11-15 | Real-time requirements, built-in .NET support |
| D004 | Use FluentValidation | 2025-11-15 | Separation of validation logic, reusable validators |
| D005 | 4MB max request size | 2025-11-15 | Balance between file support and memory usage |
| D006 | 10MB max file size | 2025-11-15 | Reasonable for documents, prevents abuse |
| D007 | JSON Logic for calculations | 2025-11-15 | Matches Blueprint spec, flexible expressions |

### Open Questions

| ID | Question | Status | Target Resolution |
|----|----------|--------|-------------------|
| Q001 | Should sustainability calculations be internal or external? | Open | Week 5 |
| Q002 | How to implement multi-tenancy? | Open | Week 2 |
| Q003 | Where to store file binaries long-term? | Open | Week 8 |
| Q004 | How long to retain offline notifications? | Open | Week 9 |
| Q005 | Should we maintain separate action history? | Open | Week 3 |

---

## 8. Success Criteria

### Phase Success Criteria

Each phase complete when:
- ✅ All tasks completed
- ✅ All tests passing (>80% coverage)
- ✅ Code reviewed
- ✅ OpenAPI docs updated
- ✅ No critical bugs

### Project Success Criteria

Project successful when:
1. ✅ All 114 tasks completed
2. ✅ All REST endpoints functional
3. ✅ SignalR hub operational
4. ✅ >80% test coverage
5. ✅ All integration tests passing
6. ✅ Performance targets met
7. ✅ Security review passed
8. ✅ Production deployment successful
9. ✅ Zero critical bugs in first 2 weeks
10. ✅ Documentation complete

---

## 9. References

1. [Action Service Specification](specs/sorcha-action-service.md)
2. [Implementation Plan](ACTION-SERVICE-IMPLEMENTATION-PLAN.md)
3. [Sorcha Architecture](../docs/architecture.md)
4. [Sorcha Constitution](constitution.md)
5. [Blueprint Schema](../docs/blueprint-schema.md)
6. [Wallet Service Spec](specs/sorcha-wallet-service.md)
7. [Register Service Spec](specs/sorcha-register-service.md)
8. [Transaction Handler Spec](specs/sorcha-transaction-handler.md)

---

**Document Control**
- **Created:** 2025-11-15
- **Author:** Sorcha Architecture Team
- **Status:** Approved for Implementation
- **Review Frequency:** Weekly during implementation
- **Next Review:** 2025-11-22
