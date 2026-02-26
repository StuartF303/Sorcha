# Implementation Plan: UI & CLI Modernization

**Branch**: `043-ui-cli-modernization` | **Date**: 2026-02-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/043-ui-cli-modernization/spec.md`

## Summary

Modernize the Sorcha UI (Blazor WASM) with 7 major improvements (activity log, sidebar consolidation, status footer, wallet management, dashboard wizard, realtime validator, settings/i18n) and extend the CLI to achieve 100% backend API coverage. Backend changes required: events API on Blueprint Service, user preferences + 2FA on Tenant Service.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: MudBlazor 8.15.0, SignalR, System.CommandLine 2.0, Blazored.LocalStorage, Microsoft.Extensions.Localization
**Storage**: PostgreSQL (EF Core — Tenant Service), MongoDB (Register Service)
**Testing**: xUnit + FluentAssertions + Moq, Playwright (E2E)
**Target Platform**: Blazor WASM (browser) + CLI (cross-platform .NET)
**Project Type**: Web application (frontend + backend microservices)
**Performance Goals**: Real-time events <2s latency, theme switch <100ms, CLI command completion <5s
**Constraints**: Blazor WASM download size, SignalR WebSocket connectivity, 90-day event retention
**Scale/Scope**: ~20 modified files (UI), ~15 new files (CLI commands), 3 backend service changes, 4 language packs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Events API on Blueprint Service (owns workflow events), preferences on Tenant Service (owns user identity). No cross-service coupling. |
| II. Security First | PASS | 2FA uses TOTP (RFC 6238). Events API requires JWT auth. User preferences scoped per-user. |
| III. API Documentation | PASS | All new endpoints get XML docs + Scalar OpenAPI. |
| IV. Testing Requirements | PASS | Unit tests for services, integration tests for APIs, E2E for UI flows. >85% target. |
| V. Code Quality | PASS | Async/await, DI, nullable enabled, C# 13 features. |
| VI. Blueprint Standards | N/A | No blueprint changes. |
| VII. Domain-Driven Design | PASS | Uses Sorcha terminology: Blueprint, Action, Participant, Disclosure. |
| VIII. Observability | PASS | Structured logging on all new endpoints, health checks maintained. |

**GATE RESULT: PASS** — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/043-ui-cli-modernization/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: technology decisions
├── data-model.md        # Phase 1: entity definitions
├── quickstart.md        # Phase 1: getting started guide
├── contracts/           # Phase 1: API contracts
│   ├── events-api.md
│   ├── user-preferences-api.md
│   └── totp-api.md
└── checklists/
    └── requirements.md  # Quality validation
```

### Source Code (repository root)

```text
# Backend changes
src/Services/Sorcha.Blueprint.Service/
├── Endpoints/EventEndpoints.cs          # NEW: Events REST API
├── Hubs/EventsHub.cs                    # NEW: Events SignalR hub
├── Models/ActivityEvent.cs              # NEW: Event entity
├── Services/
│   ├── Interfaces/IEventService.cs      # NEW
│   └── Implementation/EventService.cs   # NEW
└── Program.cs                           # MODIFY: register events

src/Services/Sorcha.Tenant.Service/
├── Endpoints/UserPreferenceEndpoints.cs # NEW: Preferences API
├── Endpoints/TotpEndpoints.cs           # NEW: 2FA API
├── Models/UserPreferences.cs            # NEW: Preferences entity
├── Models/TotpConfiguration.cs          # NEW: 2FA entity
├── Services/
│   ├── Interfaces/ITotpService.cs       # NEW
│   └── Implementation/TotpService.cs    # NEW
└── Program.cs                           # MODIFY: register endpoints

# Frontend changes
src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/
├── Components/Layout/
│   ├── MainLayout.razor                 # MODIFY: sidebar, footer, theme
│   ├── ActivityLogPanel.razor           # NEW: overlay panel
│   └── StatusFooter.razor               # NEW: footer bar
├── Components/Shared/
│   ├── WalletQrDialog.razor             # NEW: QR code dialog
│   └── LocalizedText.razor              # NEW: localization component
├── Pages/
│   ├── Home.razor                       # MODIFY: wizard conditional
│   ├── Settings.razor                   # MODIFY: expand with tabs
│   ├── Admin/Validator.razor            # MODIFY: realtime stats
│   └── Wallets/
│       ├── WalletList.razor             # MODIFY: list/card toggle, default
│       └── CreateWallet.razor           # MODIFY: PQC algorithms
└── wwwroot/
    └── i18n/                            # NEW: translation files
        ├── en.json
        ├── fr.json
        ├── de.json
        └── es.json

src/Apps/Sorcha.UI/Sorcha.UI.Core/
├── Services/
│   ├── ActivityLogService.cs            # NEW: events client
│   ├── UserPreferencesService.cs        # NEW: preferences client
│   ├── ThemeService.cs                  # NEW: theme management
│   ├── LocalizationService.cs           # NEW: i18n service
│   └── TotpService.cs                   # NEW: 2FA client
└── Models/
    ├── ActivityEventDto.cs              # NEW
    └── UserPreferencesDto.cs            # NEW

# CLI changes
src/Apps/Sorcha.Cli/Commands/
├── BlueprintCommand.cs                  # NEW: blueprint CRUD + publish
├── ParticipantCommand.cs                # NEW: participant identity
├── CredentialCommand.cs                 # NEW: VC operations
├── ValidatorCommand.cs                  # NEW: validator admin
└── AdminCommand.cs                      # NEW: system admin ops

# Tests
tests/Sorcha.Blueprint.Service.Tests/
├── Endpoints/EventEndpointTests.cs      # NEW
└── Services/EventServiceTests.cs        # NEW

tests/Sorcha.Tenant.Service.Tests/
├── Endpoints/UserPreferenceTests.cs     # NEW
└── Endpoints/TotpEndpointTests.cs       # NEW

tests/Sorcha.Cli.Tests/
├── Commands/BlueprintCommandTests.cs    # NEW
├── Commands/ParticipantCommandTests.cs  # NEW
└── Commands/CredentialCommandTests.cs   # NEW
```

**Structure Decision**: Extends existing microservice structure. No new projects needed — changes go into existing service projects, UI projects, and CLI project. New files follow established folder conventions.

## Complexity Tracking

No constitution violations. No complexity justifications needed.
