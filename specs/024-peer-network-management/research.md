# Research: Peer Network Management & Observability

**Feature**: 024-peer-network-management
**Date**: 2026-02-07

## Gap Analysis

### 1. Peer Banning — No Current Support

**Current State**: `PeerNode` has no ban mechanism. Peers are automatically removed when `FailureCount >= 5` (non-seed nodes only). No persistent ban list.

**Decision**: Add `IsBanned`, `BannedAt`, and `BanReason` properties to `PeerNode`. Persist to PostgreSQL via `PeerDbContext`. Ban checks integrated into `PeerListManager.GetHealthyPeers()` and `GetAllPeers()` (banned peers excluded from healthy list, included in full list with status).

**Rationale**: Simpler than a separate ban table. Ban state is part of the peer entity itself. EF Core migration adds columns. All existing queries that use `GetHealthyPeers()` automatically exclude banned peers.

**Alternatives Considered**:
- Separate `BannedPeers` table: More complex, requires JOIN queries, harder to maintain consistency. Rejected.
- In-memory-only ban list: Lost on restart, violates spec clarification requirement. Rejected.

### 2. Failure Count Reset — Partial Support

**Current State**: `UpdateLastSeenAsync()` resets `FailureCount` to 0 automatically when a peer is seen. No manual reset mechanism.

**Decision**: Add `ResetFailureCountAsync(string peerId)` to `PeerListManager`. Exposed via `POST /api/peers/{peerId}/reset`.

**Rationale**: Operators need to manually recover peers that accumulated failures during a transient outage without waiting for the peer to reconnect.

### 3. Connection Quality Exposure — Data Exists, No Endpoint

**Current State**: `ConnectionQualityTracker` tracks quality scores (0-100) with latency, success rate, and quality rating (Excellent/Good/Fair/Poor). Data is in-memory only. No REST endpoint exposes it.

**Decision**: Add `GET /api/peers/quality` endpoint that calls `ConnectionQualityTracker.GetAllQualities()` and returns the data. Also add per-peer quality to the `GET /api/peers/{peerId}` response.

**Rationale**: No new service logic needed — just endpoint wiring. Quality data is already computed.

### 4. Register Subscription Management — Internal Only

**Current State**: `RegisterSyncBackgroundService` has `SubscribeToRegisterAsync()` and `UnsubscribeFromRegisterAsync()` methods, but they're only called internally. No REST endpoints expose them.

**Decision**: Add `POST /api/registers/{registerId}/subscribe` and `DELETE /api/registers/{registerId}/subscribe` endpoints that delegate to `RegisterSyncBackgroundService`.

**Rationale**: Methods already exist. Wiring to endpoints is straightforward. Unsubscribe retains cached data per spec clarification; add optional `?purge=true` query parameter.

### 5. Available Registers Discovery — Partially Implemented

**Current State**: `RegisterAdvertisementService` tracks remote peer advertisements via `ProcessRemoteAdvertisementsAsync()`, but only stores them per-peer on `PeerNode.AdvertisedRegisters`. No aggregated "what registers exist across the network" view.

**Decision**: Add `GetNetworkAdvertisedRegisters()` method to `RegisterAdvertisementService` that aggregates `AdvertisedRegisters` across all known peers, counting how many peers hold each register and tracking the max version. Exposed via `GET /api/registers/available`.

**Rationale**: Simple aggregation over existing in-memory data. No new storage needed.

### 6. UI Enhancement — Current Component is Basic

**Current State**: `PeerServiceAdmin.razor` shows service health, basic metrics (CPU, memory, throughput), and a simple peer table (ID, endpoint, status, connected since). Uses 5-second auto-refresh timer. Calls `/api/peer/status` and `/api/peers`.

**Decision**: Enhance the component with 4 tabbed panels:
1. **Network Overview**: Enhanced peer table with latency, quality score, advertised registers count, ban status. Summary cards at top.
2. **Register Subscriptions**: Table showing all subscriptions with progress bars. Subscribe/unsubscribe actions.
3. **Available Registers**: Registers discovered from peer advertisements. Subscribe button per register.
4. **Peer Quality**: Quality breakdown chart, per-peer quality table with ban/reset actions.

**Rationale**: Tabs keep the component organized. MudBlazor `MudTabs` pattern used elsewhere in admin pages.

### 7. CLI Extensions — 6 New Subcommands

**Current State**: CLI has `peer list`, `peer get`, `peer stats`, `peer health`. Uses Refit `IPeerServiceClient` with 4 methods.

**Decision**: Add 6 subcommands:
- `peer subscriptions` — list register subscriptions
- `peer subscribe --register-id <id> --mode <forward-only|full-replica>` — subscribe
- `peer unsubscribe --register-id <id> [--purge]` — unsubscribe
- `peer quality` — show quality scores for all peers
- `peer ban --peer-id <id> [--reason <text>]` — ban a peer
- `peer reset --peer-id <id>` — reset failure count

Extend `IPeerServiceClient` Refit interface with matching methods.

**Rationale**: Follows existing command pattern. Each subcommand maps 1:1 to a REST endpoint.

### 8. API Gateway Routing — Already Covered

**Current State**: YARP config has `peers-direct-route` matching `/api/peers/{**catch-all}` and `peer-route` matching `/api/peer/{**catch-all}`.

**Decision**: New endpoints under `/api/peers/...` and `/api/registers/...` are already covered by existing catch-all routes. No YARP config changes needed.

**Rationale**: The `{**catch-all}` pattern handles all sub-paths. Verified that `/api/registers/...` maps through `peer-route` since it goes to the peer service. However, need to add a route for `/api/registers/{**catch-all}` since it doesn't match either existing pattern.

**Action**: Add `registers-route` to YARP config: `/api/registers/{**catch-all}` → peer-cluster `/api/registers/{**catch-all}`.

### 9. Authentication for Management Endpoints

**Current State**: Existing GET endpoints are unauthenticated (except `/api/peers/connected` which differentiates anonymous vs authenticated). No POST/DELETE endpoints exist.

**Decision**: All new POST/DELETE management endpoints require `[Authorize]`. New GET endpoints for quality and available registers are public (read-only monitoring data). Subscribe/unsubscribe/ban/unban/reset all require authentication.

**Rationale**: Consistent with other Sorcha services where read endpoints are open but write endpoints require auth.
