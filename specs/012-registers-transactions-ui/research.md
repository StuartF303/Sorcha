# Research: Registers and Transactions UI

**Date**: 2026-01-20
**Feature**: 012-registers-transactions-ui

## Executive Summary

Research confirms that all required backend infrastructure is already in place. The Register Service provides complete REST APIs for registers and transactions, plus a SignalR hub for real-time notifications. No new backend development is required - this is a pure UI feature.

## Research Findings

### 1. Register Service API Availability

**Decision**: Use existing Register Service APIs via API Gateway

**Rationale**: The Register Service already exposes all required endpoints with proper authentication, pagination, and filtering. The API Gateway routes `/api/register/*` to the Register Service.

**Alternatives considered**:
- Direct service calls (rejected: violates gateway pattern)
- New BFF layer (rejected: unnecessary complexity, APIs already well-suited)

**Key Endpoints Verified**:
| Endpoint | Verified | Notes |
|----------|----------|-------|
| `GET /api/registers` | ✅ | Supports tenantId filter |
| `GET /api/registers/{id}` | ✅ | Returns full Register model |
| `GET /api/registers/{id}/transactions` | ✅ | Paginated, sorted newest first |
| `GET /api/registers/{id}/transactions/{txId}` | ✅ | Full transaction details |
| `POST /api/registers/initiate` | ✅ | Genesis phase 1 |
| `POST /api/registers/finalize` | ✅ | Genesis phase 2 |

### 2. Real-Time Updates via SignalR

**Decision**: Use existing RegisterHub at `/hubs/register`

**Rationale**: The hub already provides all necessary client methods for real-time transaction notifications and register status updates.

**Alternatives considered**:
- Polling (rejected: inefficient, poor UX)
- Server-Sent Events (rejected: SignalR already in place, better reconnection)
- New dedicated UI hub (rejected: RegisterHub already has all needed methods)

**Client Methods Available**:
```csharp
interface IRegisterHubClient
{
    Task RegisterCreated(string registerId, string name);
    Task RegisterDeleted(string registerId);
    Task TransactionConfirmed(string registerId, string transactionId);
    Task DocketSealed(string registerId, ulong docketId, string hash);
    Task RegisterHeightUpdated(string registerId, uint newHeight);
}
```

**Server Methods Available**:
```csharp
Task SubscribeToRegister(string registerId);
Task UnsubscribeFromRegister(string registerId);
Task SubscribeToTenant(string tenantId);
Task UnsubscribeFromTenant(string tenantId);
```

### 3. UI Component Pattern

**Decision**: Follow existing Sorcha.UI component structure

**Rationale**: Consistency with existing codebase (Admin, Designer, Wallets components) ensures maintainability and developer familiarity.

**Alternatives considered**:
- Standalone micro-frontend (rejected: over-engineering for this scope)
- Different component library (rejected: MudBlazor already in use)

**Existing Patterns to Follow**:
- Components in `Sorcha.UI.Core/Components/{Feature}/`
- Pages in `Sorcha.UI.Web.Client/Pages/{Feature}/`
- Services in `Sorcha.UI.Core/Services/`
- Uses MudBlazor for all UI components

### 4. Navigation Structure

**Decision**: New sidebar menu item "Registers" (already configured)

**Rationale**: The MainLayout.razor already has a Registers nav link at line 77-79.

**Existing Navigation Entry**:
```razor
<MudNavLink Href="registers" Icon="@Icons.Material.Filled.Storage">
    Registers
</MudNavLink>
```

### 5. Authentication & Authorization

**Decision**: Use existing JWT authentication with role claims

**Rationale**: Existing auth infrastructure supports the required role-based access (Administrator vs Participant).

**Role Detection Pattern** (from existing code):
```csharp
var roleClaim = authState.User.Claims
    .FirstOrDefault(c => c.Type == ClaimTypes.Role);
var isAdmin = roleClaim?.Value == "Administrator";
```

### 6. Virtual Scrolling for Large Lists

**Decision**: Use MudBlazor's MudVirtualize component

**Rationale**: Built-in virtualization in MudBlazor handles large lists efficiently without additional dependencies.

**Alternatives considered**:
- Custom virtual scrolling (rejected: MudVirtualize is well-tested)
- Simple pagination only (rejected: doesn't meet real-time scrolling requirement)

**Example Pattern**:
```razor
<MudVirtualize Items="@transactions" Context="tx" OverscanCount="10">
    <TransactionRow Transaction="@tx" />
</MudVirtualize>
```

## Data Models

### Register (from existing Register.cs)
```csharp
public class Register
{
    public string Id { get; set; }           // 32-char GUID
    public string Name { get; set; }         // 1-38 chars
    public uint Height { get; set; }         // Block count
    public RegisterStatus Status { get; set; } // Online/Offline/Checking/Recovery
    public bool Advertise { get; set; }      // Public visibility
    public string TenantId { get; set; }     // Organization ID
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### TransactionModel (from existing TransactionModel.cs)
```csharp
public class TransactionModel
{
    public string Id { get; set; }           // DID URI
    public string RegisterId { get; set; }
    public string TxId { get; set; }         // 64-char hex hash
    public string PrevTxId { get; set; }
    public ulong? DocketNumber { get; set; }
    public string SenderWallet { get; set; } // Base58
    public IEnumerable<string> RecipientsWallets { get; set; }
    public DateTime TimeStamp { get; set; }
    public TransactionMetaData? MetaData { get; set; }
    public ulong PayloadCount { get; set; }
    public string Signature { get; set; }
}
```

## Conclusion

All technical decisions are resolved. No NEEDS CLARIFICATION items remain. The implementation can proceed using existing infrastructure without any backend modifications.

**Key Takeaways**:
1. ✅ APIs exist and are well-documented
2. ✅ SignalR hub has all required methods
3. ✅ Navigation already configured
4. ✅ Auth/role system supports requirements
5. ✅ UI patterns established in codebase
