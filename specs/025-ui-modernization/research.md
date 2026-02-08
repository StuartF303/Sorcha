# Research: Sorcha UI Modernization

**Branch**: `025-ui-modernization` | **Date**: 2026-02-07

## R1: Navigation Restructure Approach

**Decision**: Break the single `Administration.razor` tabbed page into individual pages, each with its own route, and flatten the navigation sidebar.

**Rationale**: The current `Administration.razor` uses MudTabs with 5 tabs (System Health, Organizations, Blueprint Service, Peer Service, Audit Log). This requires 2 clicks to reach any admin function (click Administration → click tab). Breaking into individual pages reduces this to 1 click and makes the navigation sidebar an accurate sitemap.

**Current Navigation Structure**:
```
MY ACTIVITY: Pending Actions, My Workflows, My Transactions, My Wallet
DESIGNER: Blueprints (group), Templates, Schema Library
MANAGEMENT: Wallets (group), Registers, Administration
UTILITY: Settings, Help & Support
```

**Proposed Navigation Structure**:
```
MY ACTIVITY: Pending Actions, My Workflows, My Transactions, My Wallet
DESIGNER: Blueprints (group), Templates, Schema Library
MANAGEMENT: Wallets (group), Registers
ADMIN (group): System Health, Peer Network, Organizations, Validator, Service Principals
UTILITY: Settings, Help & Support
```

**Alternatives Considered**:
- Keep tabbed page but add direct links → confusing (two ways to reach same content)
- Use MudNavGroup for admin → chosen approach, uses collapsible group like Wallets/Blueprints already do

## R2: Existing Truncation Patterns

**Decision**: Create a single reusable `TruncatedId` Blazor component to replace all ad-hoc truncation implementations.

**Rationale**: The codebase has 5+ different truncation patterns:
- `WalletList.razor`: First 8 + last 8 characters
- `RegisterCard.razor`: First 8 characters only
- `TransactionViewModel`: First 8 + last 4 characters (sender), first 8 only (txId)
- `MyActions.razor`: First 8 with bounds check
- `MyTransactions.razor`: First 8 with bounds check

User requirement specifies: "first few characters and at least the last 6 characters." Standardizing on a component eliminates inconsistency.

**Component Design**:
- Input: `Value` (string), optional `Prefix` (auto-detected "0x"), optional `MaxLength` (default 12)
- Truncation: Show first 6 chars + "..." + last 6 chars (total ~15 chars displayed)
- Tooltip: Full value on hover via MudTooltip
- Copy: Click copies to clipboard via `navigator.clipboard.writeText()`, shows MudSnackbar confirmation
- Short values (<= MaxLength): Display in full, no truncation

**Alternatives Considered**:
- CSS-only truncation (text-overflow: ellipsis) → doesn't show last chars
- JavaScript-only approach → not Blazor-idiomatic
- Filter/extension method → no hover/copy interactivity

## R3: Backend API Availability for UI Features

**Decision**: All required backend APIs exist. No new service endpoints need to be created.

**Findings by feature area**:

| Feature | API Status | Endpoint |
|---------|-----------|----------|
| Organization CRUD | EXISTS | `POST/GET/PUT/DELETE /api/organizations/` |
| Organization stats | EXISTS | `GET /api/organizations/stats` |
| Dashboard stats | EXISTS | `GET /api/dashboard` on Gateway |
| Workflow instances | EXISTS | `GET/POST /api/instances/` |
| Next actions | EXISTS | `GET /api/instances/{id}/next-actions` |
| Action execution | EXISTS | `POST /api/instances/{id}/actions/{aid}/execute` |
| Blueprint CRUD | EXISTS | `POST/GET/PUT/DELETE /api/blueprints/` |
| Blueprint publishing | EXISTS | `POST /api/blueprints/{id}/publish` |
| Blueprint versions | EXISTS | `GET /api/blueprints/{id}/versions` |
| Template CRUD | EXISTS | `POST/GET/DELETE /api/templates/` |
| Template evaluation | EXISTS | `POST /api/templates/evaluate` |
| Wallet CRUD | EXISTS | `POST/GET/PATCH/DELETE /api/v1/wallets/` |
| Wallet signing | EXISTS | `POST /api/v1/wallets/{address}/sign` |
| Transaction queries | EXISTS | `GET /api/query/wallets/{address}/transactions` |
| Docket listing | EXISTS | `GET /api/registers/{id}/dockets/` |
| OData queries | EXISTS | `GET /odata/Transactions` with full OData v4 |
| Validator mempool | EXISTS | `GET /api/v1/transactions/mempool/{registerId}` |
| Validator admin | EXISTS | `GET /api/admin/mempool` |
| Service principals | NOT FOUND | No dedicated endpoint; use Tenant auth endpoints |

**Service Principals Note**: The Tenant Service has JWT-based auth and token management (`/api/auth/token/*`) but no explicit "service principal" CRUD. For the UI, we can list service tokens via the token introspection endpoint and show their status. If a full CRUD is needed, a new endpoint would be required in a future Tenant Service update.

## R4: Blueprint Cloud Persistence Migration

**Decision**: Replace `IBlueprintStorageService` (LocalStorage-based) with a new implementation that calls the Blueprint Service API.

**Rationale**: Current `BlueprintStorageService` uses `Blazored.LocalStorage` with key `sorcha:blueprints`. The Blueprint Service already has full CRUD at `/api/blueprints/`. The interface `IBlueprintStorageService` with `SaveAsync()`, `LoadAsync()`, `DeleteAsync()` maps cleanly to the API.

**Migration Strategy**:
1. Create `ApiBlueprintStorageService` implementing `IBlueprintStorageService`
2. This implementation calls Blueprint Service API via HttpClient
3. Replace DI registration (swap LocalStorage implementation for API implementation)
4. The Designer component calls the same interface — no Designer changes needed
5. The `LoadBlueprintDialog` component needs updating to fetch from API instead of LocalStorage

**Alternatives Considered**:
- Hybrid (LocalStorage as cache + API sync) → over-engineering for now
- Keep LocalStorage for drafts, API for published → adds complexity

## R5: Test Infrastructure Pattern

**Decision**: Use the existing NUnit + Playwright Docker E2E test pattern for all new pages.

**Rationale**: The project has a well-established E2E test infrastructure:
- `DockerTestBase` with console error capture, network monitoring, screenshot on failure
- `AuthenticatedDockerTestBase` with one-time auth per fixture
- `TestConstants` with all routes and credentials
- `MudBlazorHelpers` for component-specific assertions
- Page Object pattern (`LoginPage`, `DashboardPage`, `NavigationComponent`)

Each new page should get:
1. A Page Object class in `PageObjects/`
2. A test class in `Docker/` extending `AuthenticatedDockerTestBase`
3. Categories: `[Category("Docker")]`, `[Category("<feature>")]`
4. Tests for: page load, data display, user interactions, error states

## R6: OData Query Builder Approach

**Decision**: Build a visual query builder component using MudBlazor form controls that generates OData `$filter` expressions.

**Rationale**: The Register Service already exposes full OData v4 support (`$filter`, `$orderby`, `$select`, `$expand`, `$count`, `$top`, `$skip`). The UI needs to make this accessible without requiring users to type OData syntax.

**Component Design**:
- Row-based query builder: each row = one filter condition
- Dropdowns for: field (from schema), operator (eq, ne, gt, lt, contains, startswith), value input
- Logical combinator: and/or between rows
- Add/remove rows
- "Preview" shows the raw OData query string
- "Execute" runs the query and displays results in a MudTable

**Alternatives Considered**:
- Free-text OData input → requires user knowledge
- GraphQL → Register Service doesn't support it
- Custom query API → OData already exists and is powerful

## R7: Validator Admin Panel Data Sources

**Decision**: Use Validator Service admin endpoints for mempool status and configure read-only display.

**Rationale**: The Validator Service exposes:
- `GET /api/v1/transactions/mempool/{registerId}` — mempool stats per register
- `GET /api/admin/mempool` — admin-level mempool inspection
- `POST /api/admin/config` — admin configuration (display only, not editable from UI initially)

The UI will poll these endpoints and display:
- Total pending transactions across all registers
- Per-register mempool depth
- Oldest pending transaction age
- Recent validation events (from mempool stats)

**Note**: Consensus state visibility is limited by what the Validator Service exposes. The current API shows mempool statistics but not detailed consensus state. The UI will show what's available and can be extended when new endpoints are added.
