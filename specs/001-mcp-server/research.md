# Research: Sorcha MCP Server

**Feature**: 001-mcp-server
**Date**: 2026-01-29

## Research Topics

### 1. MCP C# SDK Availability

**Decision**: Use official `ModelContextProtocol` NuGet package

**Rationale**:
- Official SDK maintained by Microsoft in collaboration with Anthropic
- Supports latest MCP specification (2025-06-18)
- Integrates with Microsoft.Extensions.Hosting for DI
- Attribute-based tool definition (`[McpServerToolType]`, `[McpServerTool]`)
- Built-in support for stdio and HTTP/SSE transports

**Alternatives Considered**:
| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| ModelContextProtocol (official) | Microsoft-maintained, latest spec, DI integration | Prerelease status | ✅ Selected |
| mcpdotnet (community) | Stable | Less maintained, older spec | ❌ Rejected |
| Custom implementation | Full control | Unnecessary effort | ❌ Rejected |

**Installation**:
```bash
dotnet add package ModelContextProtocol --prerelease
dotnet add package Microsoft.Extensions.Hosting
```

**Sources**:
- [Official C# SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft .NET Blog Tutorial](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)
- [MCP C# SDK Documentation](https://modelcontextprotocol.github.io/csharp-sdk/)

---

### 2. Tool Definition Pattern

**Decision**: Use attribute-based tool definition with DI injection

**Rationale**:
- SDK supports `[McpServerToolType]` on classes and `[McpServerTool]` on methods
- Services can be injected as method parameters
- `WithToolsFromAssembly()` auto-discovers all tools

**Pattern**:
```csharp
[McpServerToolType]
public class AdminTools
{
    [McpServerTool, Description("Check health of all Sorcha services")]
    public async Task<HealthCheckResult> CheckHealth(
        IHealthCheckService healthService,
        CancellationToken cancellationToken)
    {
        return await healthService.CheckAllServicesAsync(cancellationToken);
    }
}
```

---

### 3. Authentication Strategy

**Decision**: JWT token passed via MCP initialization, validated per-request

**Rationale**:
- MCP servers receive configuration on startup
- JWT claims determine user identity and roles
- Tools filtered by role claims at runtime

**Flow**:
```
1. AI assistant starts MCP server with: --jwt-token <token>
2. Server validates JWT signature and expiration
3. Claims extracted: userId, roles[], tenantId, organizationId
4. McpSessionService caches claims for tool authorization
5. Each tool checks required role before execution
```

**Role Mapping**:
| JWT Role Claim | MCP Persona | Available Tools |
|---------------|-------------|-----------------|
| `sorcha:admin` | Administrator | FR-010 to FR-019 |
| `sorcha:designer` | Designer | FR-020 to FR-032 |
| `sorcha:participant` | Participant | FR-033 to FR-042 |

---

### 4. Service Client Reuse

**Decision**: Direct reuse of Sorcha.ServiceClients

**Rationale**:
- Existing clients cover all required backend operations
- Maintains consistency with other Sorcha applications
- Already configured for Aspire service discovery

**Available Clients**:
| Client Interface | Operations | Used By Tools |
|-----------------|------------|---------------|
| `IBlueprintServiceClient` | Get, Validate | Designer tools |
| `IRegisterServiceClient` | Transactions, Dockets, Registers | Participant, Admin tools |
| `IWalletServiceClient` | Sign, Encrypt, Wallet info | Participant tools |
| `IParticipantServiceClient` | Participant lookup | Participant tools |
| `IPeerServiceClient` | Network status | Admin tools |
| `IValidatorServiceClient` | Consensus status | Admin tools |

**Registration**:
```csharp
services.AddServiceClients(configuration);  // From Sorcha.ServiceClients
```

---

### 5. Rate Limiting Approach

**Decision**: In-memory sliding window rate limiter

**Rationale**:
- Simple, effective for single-instance deployment
- Can upgrade to Redis-backed for distributed scenarios
- Protects backend services from abuse

**Configuration**:
| Limit Type | Value | Scope |
|------------|-------|-------|
| Per-user | 100 req/min | All tools |
| Per-tenant | 1000 req/min | All tools |
| Admin tools | 50 req/min | Admin category |

**Implementation**: Use `System.Threading.RateLimiting` from .NET 7+

---

### 6. Error Handling Strategy

**Decision**: Structured error responses with MCP-compatible format

**Rationale**:
- MCP expects tool results or errors in specific format
- Users need actionable error messages
- Errors should not leak internal details

**Error Categories**:
| Error Type | MCP Response | User Message |
|------------|--------------|--------------|
| Authentication | `isError: true` | "Authentication required. Please provide a valid JWT token." |
| Authorization | `isError: true` | "Access denied. This tool requires {role} permissions." |
| Validation | `isError: true` | "Invalid input: {field} - {reason}" |
| Backend unavailable | `isError: true` | "Service temporarily unavailable. Please retry." |
| Rate limited | `isError: true` | "Rate limit exceeded. Please wait {seconds} seconds." |

---

### 7. Observability Integration

**Decision**: Use Sorcha.ServiceDefaults for OpenTelemetry

**Rationale**:
- Consistent with other Sorcha services
- Automatic trace propagation to backend services
- Metrics for tool invocation latency/success rates

**Instrumentation Points**:
- Tool invocation start/end (spans)
- Authorization checks (spans)
- Service client calls (automatic via HttpClient)
- Rate limit events (counters)
- Error counts by type (counters)

---

## Open Questions Resolved

| Question | Resolution |
|----------|------------|
| Is MCP SDK available for .NET? | Yes - official `ModelContextProtocol` package |
| How to pass auth to MCP server? | JWT token via command-line argument |
| Which transport mode? | stdio primary, HTTP/SSE future |
| How to filter tools by role? | Runtime authorization check per tool |
| Rate limiting approach? | In-memory sliding window |
