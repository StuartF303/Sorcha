# Implementation Plan: AI-Assisted Blueprint Design Chat

**Branch**: `001-blueprint-chat` | **Date**: 2026-02-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-blueprint-chat/spec.md`

## Summary

This feature adds an AI-powered conversational interface to Sorcha.UI that enables users to design workflow blueprints through natural language. The system uses SignalR for real-time streaming of AI responses and live blueprint preview updates. Key components include a ChatHub in Blueprint.Service, client-side ChatHubConnection in Sorcha.UI.Core, AI provider integration with tool-calling capabilities, and session management with draft persistence.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: SignalR, Anthropic Claude API (or compatible), Sorcha.Blueprint.Fluent, MudBlazor
**Storage**: Redis (session/draft persistence), existing Blueprint storage (final save)
**Testing**: xUnit, FluentAssertions, Moq, Playwright (E2E)
**Target Platform**: Blazor WASM (client), ASP.NET Core (server)
**Project Type**: Web application (extends existing Sorcha.UI and Blueprint.Service)
**Performance Goals**: 2s AI response start (SC-003), 1s preview update (SC-004), 500ms validation (SC-005), 50 concurrent sessions (SC-006)
**Constraints**: 100 messages per session limit (FR-019), 3 retries with exponential backoff (FR-016), 24hr session recovery (SC-008)
**Scale/Scope**: 50 concurrent users, extends 2 existing projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | Extends existing Blueprint.Service, no new service required |
| II. Security First | PASS | Uses existing JWT auth (FR-015), AI API keys server-side only |
| III. API Documentation | PASS | SignalR hub methods will have XML docs, REST endpoints use Scalar |
| IV. Testing Requirements | PASS | Unit tests for services, integration for hub, E2E for UI flow |
| V. Code Quality | PASS | Follows existing patterns, async/await, DI |
| VI. Blueprint Creation Standards | PASS | AI uses Fluent API for programmatic generation (valid use case) |
| VII. Domain-Driven Design | PASS | Uses Blueprint, Action, Participant, Disclosure terminology |
| VIII. Observability | PASS | OpenTelemetry tracing for AI calls, structured logging |

**Gate Result**: PASS - No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/001-blueprint-chat/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: AI provider patterns, SignalR streaming
├── data-model.md        # Phase 1: Chat entities, session schema
├── quickstart.md        # Phase 1: Developer setup guide
├── contracts/           # Phase 1: Hub method signatures, AI tool definitions
│   ├── chat-hub.md      # SignalR hub contract
│   └── ai-tools.md      # AI tool definitions for blueprint operations
└── tasks.md             # Phase 2: Implementation tasks
```

### Source Code (repository root)

```text
src/
├── Apps/
│   └── Sorcha.UI/
│       ├── Sorcha.UI.Core/
│       │   ├── Services/
│       │   │   └── ChatHubConnection.cs          # NEW: SignalR client for chat
│       │   └── Models/
│       │       └── Chat/                          # NEW: Chat message models
│       │           ├── ChatMessage.cs
│       │           ├── ChatSession.cs
│       │           └── ToolExecutionResult.cs
│       └── Sorcha.UI.Web.Client/
│           ├── Pages/
│           │   └── BlueprintChat.razor            # NEW: Chat designer page
│           └── Components/
│               └── Chat/                          # NEW: Chat UI components
│                   ├── ChatPanel.razor
│                   ├── ChatMessage.razor
│                   └── BlueprintPreview.razor
│
├── Services/
│   └── Sorcha.Blueprint.Service/
│       ├── Hubs/
│       │   └── ChatHub.cs                         # NEW: SignalR hub for chat
│       ├── Services/
│       │   ├── Interfaces/
│       │   │   ├── IChatOrchestrationService.cs   # NEW: Chat flow orchestration
│       │   │   ├── IAIProviderService.cs          # NEW: AI API abstraction
│       │   │   └── IBlueprintToolExecutor.cs      # NEW: Tool execution
│       │   └── ChatOrchestrationService.cs        # NEW: Implementation
│       │   └── AnthropicProviderService.cs        # NEW: Claude integration
│       │   └── BlueprintToolExecutor.cs           # NEW: Fluent API wrapper
│       └── Models/
│           └── Chat/                              # NEW: Server-side models
│               ├── ChatSessionState.cs
│               └── AIToolDefinition.cs

tests/
├── Sorcha.Blueprint.Service.Tests/
│   └── Services/
│       ├── ChatOrchestrationServiceTests.cs       # NEW
│       └── BlueprintToolExecutorTests.cs          # NEW
├── Sorcha.UI.Core.Tests/
│   └── Services/
│       └── ChatHubConnectionTests.cs              # NEW
└── Sorcha.UI.E2E.Tests/
    └── BlueprintChatTests.cs                      # NEW: E2E chat flow tests
```

**Structure Decision**: Extends existing Sorcha.UI and Blueprint.Service projects. New files follow established patterns (Services/, Models/, Hubs/). No new projects required.

## Complexity Tracking

No constitution violations requiring justification. The feature extends existing infrastructure without introducing new architectural patterns or additional services.

## Post-Design Constitution Re-Check

*Verified after Phase 1 design completion.*

| Principle | Status | Verification |
|-----------|--------|--------------|
| I. Microservices-First | PASS | ChatHub added to existing Blueprint.Service, no new service |
| II. Security First | PASS | API keys in environment/Key Vault, JWT auth enforced on hub |
| III. API Documentation | PASS | Hub contract documented in contracts/chat-hub.md |
| IV. Testing Requirements | PASS | Test files specified for unit, integration, E2E |
| V. Code Quality | PASS | Interfaces defined, async patterns, DI throughout |
| VI. Blueprint Creation Standards | PASS | Fluent API usage documented as valid for AI-driven generation |
| VII. Domain-Driven Design | PASS | All tool names use Blueprint/Action/Participant/Disclosure |
| VIII. Observability | PASS | Research includes OpenTelemetry tracing for AI calls |

**Post-Design Gate Result**: PASS

## Generated Artifacts

| Artifact | Path | Status |
|----------|------|--------|
| Implementation Plan | `specs/001-blueprint-chat/plan.md` | Complete |
| Research | `specs/001-blueprint-chat/research.md` | Complete |
| Data Model | `specs/001-blueprint-chat/data-model.md` | Complete |
| Chat Hub Contract | `specs/001-blueprint-chat/contracts/chat-hub.md` | Complete |
| AI Tools Contract | `specs/001-blueprint-chat/contracts/ai-tools.md` | Complete |
| Quickstart Guide | `specs/001-blueprint-chat/quickstart.md` | Complete |
| Tasks | `specs/001-blueprint-chat/tasks.md` | Complete |
