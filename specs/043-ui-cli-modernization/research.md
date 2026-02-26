# Phase 0 Research: UI & CLI Modernization

**Feature**: 043-ui-cli-modernization | **Date**: 2026-02-26

## Research Tasks & Decisions

### R1: Activity Events Storage Location

**Decision**: New `ActivityEvent` entity on Blueprint Service with PostgreSQL/EF Core storage

**Rationale**: Blueprint Service already owns workflow events (ActionsHub notifications, blueprint operations). Adding events storage here keeps event generation and persistence co-located. The existing ActionsHub pattern provides the template for a new EventsHub.

**Alternatives Considered**:
- **Client-side storage (localStorage)**: Rejected — no admin org-wide view, no cross-device access, no retention guarantees
- **Separate Events microservice**: Rejected — over-engineering for the current scope; events are workflow-centric and belong with Blueprint
- **MongoDB on Register Service**: Rejected — events are not ledger data; Register Service doesn't own user-facing workflow events

**Implementation Notes**:
- Blueprint Service currently uses MongoDB (register-related data). Events are relational (user→event, org→events queries) — use PostgreSQL via EF Core alongside existing MongoDB
- Need new `BlueprintEventsDbContext` for PostgreSQL, separate from the MongoDB collections
- 90-day retention via background cleanup job (IHostedService with daily timer)

---

### R2: User Preferences Persistence

**Decision**: New `UserPreferences` entity on Tenant Service, stored in per-org PostgreSQL schema

**Rationale**: Tenant Service owns user identity (UserIdentity model, TenantDbContext). Preferences are per-user settings that belong with the user profile. The per-org schema pattern (`org_{organizationId}`) is already established.

**Alternatives Considered**:
- **Browser localStorage (Blazored.LocalStorage)**: Rejected — user explicitly chose server-side for cross-device consistency
- **Redis cache**: Rejected — preferences need persistence, not just caching
- **Separate column on UserIdentity**: Rejected — would bloat the identity model; separate entity is cleaner and avoids migrations on the core table

**Implementation Notes**:
- Add `UserPreferences` to `TenantDbContext` in per-org schema
- One-to-one relationship with `UserIdentity` via `UserId` FK
- Create on first access (lazy initialization)
- Existing `WalletPreferenceService` in UI.Core uses Blazored.LocalStorage — will be replaced by server-side `UserPreferencesService` calling Tenant Service API
- Fields: Theme, Language, TimeFormat, DefaultWalletAddress, NotificationsEnabled, TwoFactorEnabled

---

### R3: SignalR EventsHub Pattern

**Decision**: New `EventsHub` on Blueprint Service at `/hubs/events`, following ActionsHub pattern

**Rationale**: ActionsHub provides a proven pattern — JWT auth via query parameter, group-based subscriptions, typed client interfaces. EventsHub follows the same structure but for activity events.

**Alternatives Considered**:
- **Extend ActionsHub with event methods**: Rejected — violates SRP; action notifications and activity events are different concerns
- **Server-Sent Events (SSE)**: Rejected — SignalR is already the established pattern, handles reconnection, and MudBlazor integrates well

**Implementation Notes**:
- Hub URL: `/hubs/events` (consistent with `/hubs/chat` pattern)
- Groups: `user:{userId}` for personal events, `org:{organizationId}` for admin org-wide events
- Client method: `EventReceived(ActivityEventDto)` for real-time push
- Auth: JWT via `?access_token={jwt}` query parameter (same as ActionsHub)

---

### R4: MudBlazor Theme Switching

**Decision**: Use MudBlazor's built-in `MudThemeProvider` with `IsDarkMode` binding, persisted via UserPreferences API

**Rationale**: MudBlazor 8.15.0 already has `PaletteLight` and `PaletteDark` defined in the existing MainLayout theme. The `MudThemeProvider` component accepts `IsDarkMode` to switch instantly. No CSS framework changes needed.

**Alternatives Considered**:
- **CSS custom properties only**: Rejected — MudBlazor manages its own component styles via palettes
- **Multiple theme objects**: Rejected — single theme with light/dark palettes is the MudBlazor standard pattern

**Implementation Notes**:
- Current theme already defines both `PaletteLight` and `PaletteDark`
- Add `@bind-IsDarkMode` to `MudThemeProvider`
- System preference detection via `window.matchMedia('(prefers-color-scheme: dark)')` JS interop
- `ThemeService` in UI.Core manages state and persists to UserPreferences API
- Theme switch is instant (<100ms) — just toggles `IsDarkMode` boolean

---

### R5: Localization Approach

**Decision**: JSON resource files in `wwwroot/i18n/` loaded at runtime, with custom `LocalizationService`

**Rationale**: Blazor WASM runs client-side — `Microsoft.Extensions.Localization` with `.resx` files requires server-side rendering or complex build steps. JSON files in wwwroot are fetched at runtime, simple to maintain, and support hot-swapping languages without rebuild.

**Alternatives Considered**:
- **Microsoft.Extensions.Localization with .resx**: Rejected — requires build-time resource embedding, complex in WASM context
- **Third-party (Blazored.Localization)**: Rejected — unnecessary dependency when a simple JSON loader suffices
- **Compile-time code generation**: Rejected — over-engineering for 4 languages

**Implementation Notes**:
- Files: `wwwroot/i18n/{locale}.json` (en.json, fr.json, de.json, es.json)
- Flat key structure: `"nav.dashboard": "Dashboard"`, `"nav.wallets": "Wallets"`, etc.
- `LocalizationService` loads JSON on startup and language change
- Browser language detection: `navigator.language` via JS interop
- Fallback chain: user preference → browser language → English
- `LocalizedText` component or `@inject` service for direct key lookup

---

### R6: TOTP 2FA Implementation

**Decision**: Standard TOTP (RFC 6238) with server-side secret storage on Tenant Service, using `OtpNet` NuGet package

**Rationale**: TOTP is the industry standard supported by all authenticator apps (Google Authenticator, Authy, Microsoft Authenticator). OtpNet is a well-maintained .NET library for TOTP generation/verification.

**Alternatives Considered**:
- **WebAuthn/FIDO2**: Rejected — more complex, requires hardware key support; TOTP covers the 2FA requirement
- **SMS-based OTP**: Rejected — requires SMS gateway, less secure, phone number dependency
- **Custom implementation**: Rejected — OtpNet handles RFC 6238 correctly

**Implementation Notes**:
- `TotpConfiguration` entity: UserId, EncryptedSecret, BackupCodes (hashed), IsEnabled, CreatedAt
- Setup flow: Generate secret → QR code URI → User scans → Verify first code → Enable
- Login enforcement: If 2FA enabled, login returns `requiresTwoFactor: true`, client prompts for code
- Backup codes: 8 single-use codes, BCrypt hashed, marked as used when consumed
- QR code generation: Client-side using JS library (no backend QR generation needed)

---

### R7: CLI Command Gap Analysis

**Decision**: Add 5 new command groups: Blueprint, Participant, Credential, Validator, Admin

**Rationale**: Current CLI has 13 command groups covering Tenant (auth, user, org, service-principal), Wallet, Register, Transaction, Docket, Query, Peer, Config, Bootstrap. Missing: Blueprint operations, Participant identity, Credential/VC, Validator admin, and system admin (health, schemas).

**Existing CLI Commands** (13):
1. BootstrapCommand
2. OrganizationCommand
3. UserCommand
4. ServicePrincipalCommand
5. AuthCommand
6. RegisterCommand
7. TransactionCommand
8. WalletCommand
9. DocketCommand
10. QueryCommand
11. PeerCommand
12. ConfigCommand
13. version

**New Commands Needed** (5):
1. **BlueprintCommand**: list, get, create, publish, delete, versions, instances
2. **ParticipantCommand**: register, list, get, update, search, wallet-link, wallet-link-verify
3. **CredentialCommand**: list, get, issue, present, verify, revoke, status
4. **ValidatorCommand**: status, start, stop, process, integrity-check
5. **AdminCommand**: health, schema-sectors (list, get), schema-providers (list, get), alerts

**Implementation Notes**:
- Follow existing `BaseCommand` pattern with `ExecuteAsync(CommandContext)`
- Use existing `IOutputFormatter` infrastructure for table/json/csv output
- CLI uses direct HTTP calls via `HttpClient` (not Refit for existing commands)
- Each command group: parent Command class + subcommand classes

---

### R8: Sidebar Consolidation

**Decision**: Merge MANAGEMENT and ADMIN NavGroups into single "Administration" group, add hamburger toggle with icon-only collapsed mode

**Rationale**: Current sidebar has separate MANAGEMENT (Wallets, Registers, Participants) and ADMIN (Health, Peers, Organizations, Validator, Service Principals, Schema Sectors, Schema Providers) sections. Consolidation reduces cognitive load and provides a single place for all administrative actions.

**Implementation Notes**:
- MudBlazor `MudDrawer` already supports `MiniVariant` mode (icon-only when collapsed)
- Set `@bind-Open="_drawerOpen"` and `Variant="DrawerVariant.Mini"` for icon-only collapse
- Single "Administration" `MudNavGroup` with alphabetical items
- Role filtering: `AuthenticationStateProvider` to check roles, hide items per role
- System Admin sees all; Org Admin sees: Participants, Registers, Users; Regular user sees none

---

### R9: QR Code Generation

**Decision**: Client-side QR code generation using `qrcode.js` via JS interop

**Rationale**: QR codes are generated from wallet addresses (public data). Client-side generation avoids backend round-trips and is fast.

**Alternatives Considered**:
- **Server-side QR generation (QRCoder NuGet)**: Rejected — adds backend dependency for a purely presentational concern
- **Blazor WASM QR library (QRCoder.Blazor)**: Could work but adds WASM binary size
- **JS interop with qrcode.js**: Lightweight, well-tested, minimal WASM impact

**Implementation Notes**:
- Include `qrcode.min.js` in wwwroot
- JS interop: `await JSRuntime.InvokeVoidAsync("generateQrCode", elementId, address)`
- Display in `MudDialog` (WalletQrDialog component)
- Share button: `navigator.clipboard.writeText(address)` with fallback

---

### R10: Web Push Notifications

**Decision**: Standard Web Push API with service worker, opt-in via settings

**Rationale**: Web Push API is the browser-standard for push notifications. Works across all modern browsers.

**Implementation Notes**:
- Service worker registration in `wwwroot/service-worker.js`
- VAPID keys for push subscription (generated server-side, stored in config)
- Push subscription stored in Tenant Service (UserPreferences)
- Notification trigger: EventsHub sends event → if user offline, queue for push
- Deferred to later iteration if complexity warrants — core settings toggle is P3

## Technology Summary

| Technology | Version | Purpose |
|-----------|---------|---------|
| MudBlazor | 8.15.0 | UI components, theme, layout |
| SignalR | .NET 10 built-in | Real-time events |
| EF Core | 10.0 | PostgreSQL for events + preferences |
| OtpNet | 4.x | TOTP 2FA |
| qrcode.js | latest | Client-side QR generation |
| System.CommandLine | 2.0 | CLI framework |
| JSON i18n | custom | Localization resource files |

## Open Questions

None — all NEEDS CLARIFICATION items resolved in spec clarification phase.
