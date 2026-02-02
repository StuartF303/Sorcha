# Tasks: AI-Assisted Blueprint Design Chat

**Input**: Design documents from `/specs/001-blueprint-chat/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: Test tasks are included as specified in the feature specification (Testing Requirements: IV in constitution).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create Chat folder structure in `src/Services/Sorcha.Blueprint.Service/` (Hubs/, Services/, Services/Interfaces/, Models/Chat/)
- [x] T002 [P] Create Chat folder structure in `src/Apps/Sorcha.UI/Sorcha.UI.Core/` (Services/, Models/Chat/)
- [x] T003 [P] Create Chat folder structure in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/` (Pages/, Components/Chat/)
- [x] T004 [P] Add Anthropic SDK NuGet package to `src/Services/Sorcha.Blueprint.Service/Sorcha.Blueprint.Service.csproj`
- [x] T005 [P] Add Polly NuGet package to `src/Services/Sorcha.Blueprint.Service/Sorcha.Blueprint.Service.csproj` (for retry with exponential backoff)
- [x] T006 Create test project folders in `tests/Sorcha.Blueprint.Service.Tests/Services/` for chat service tests

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Server-Side Models

- [x] T007 Create `ChatSession` model in `src/Services/Sorcha.Blueprint.Service/Models/Chat/ChatSession.cs` per data-model.md
- [x] T008 [P] Create `ChatMessage` model in `src/Services/Sorcha.Blueprint.Service/Models/Chat/ChatMessage.cs` per data-model.md
- [x] T009 [P] Create `ToolCall` model in `src/Services/Sorcha.Blueprint.Service/Models/Chat/ToolCall.cs` per data-model.md
- [x] T010 [P] Create `ToolResult` model in `src/Services/Sorcha.Blueprint.Service/Models/Chat/ToolResult.cs` per data-model.md
- [x] T011 [P] Create `SessionStatus` and `MessageRole` enums in `src/Services/Sorcha.Blueprint.Service/Models/Chat/ChatEnums.cs`

### Client-Side Models

- [x] T012 [P] Create `ChatMessage` client model in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Chat/ChatMessage.cs`
- [x] T013 [P] Create `ChatSession` client model in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Chat/ChatSession.cs`
- [x] T014 [P] Create `ToolExecutionResult` client model in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Chat/ToolExecutionResult.cs`

### AI Provider Service

- [x] T015 Create `IAIProviderService` interface in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/IAIProviderService.cs` with StreamCompletionAsync returning IAsyncEnumerable<AIStreamEvent>
- [x] T016 Create `AIStreamEvent` hierarchy (TextChunk, ToolUse, StreamEnd) in `src/Services/Sorcha.Blueprint.Service/Models/Chat/AIStreamEvents.cs`
- [x] T017 Implement `AnthropicProviderService` in `src/Services/Sorcha.Blueprint.Service/Services/AnthropicProviderService.cs` with Polly retry (3 retries, exponential backoff: 2s, 4s, 8s)
- [x] T018 Add AI provider configuration section to `src/Services/Sorcha.Blueprint.Service/appsettings.json` (ApiKey placeholder, Model, MaxTokens)
- [ ] T019 Write unit tests for `AnthropicProviderService` in `tests/Sorcha.Blueprint.Service.Tests/Services/AnthropicProviderServiceTests.cs`

### Session Management (Redis)

- [x] T020 Create `IChatSessionStore` interface in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/IChatSessionStore.cs` for Redis operations
- [x] T021 Implement `RedisChatSessionStore` in `src/Services/Sorcha.Blueprint.Service/Services/RedisChatSessionStore.cs` per data-model.md Redis schema
- [ ] T022 Write unit tests for `RedisChatSessionStore` in `tests/Sorcha.Blueprint.Service.Tests/Services/RedisChatSessionStoreTests.cs`

### Tool Executor

- [x] T023 Create `IBlueprintToolExecutor` interface in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/IBlueprintToolExecutor.cs` with ExecuteAsync and GetToolDefinitions
- [x] T024 Create `ToolDefinition` model in `src/Services/Sorcha.Blueprint.Service/Models/Chat/ToolDefinition.cs` for AI tool schemas
- [x] T025 Implement `BlueprintToolExecutor` in `src/Services/Sorcha.Blueprint.Service/Services/BlueprintToolExecutor.cs` with tool registration
- [ ] T026 Write unit tests for `BlueprintToolExecutor` in `tests/Sorcha.Blueprint.Service.Tests/Services/BlueprintToolExecutorTests.cs`

### Orchestration Service

- [x] T027 Create `IChatOrchestrationService` interface in `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/IChatOrchestrationService.cs` per quickstart.md
- [x] T028 Implement `ChatOrchestrationService` in `src/Services/Sorcha.Blueprint.Service/Services/ChatOrchestrationService.cs` coordinating AI, tools, and session
- [ ] T029 Write unit tests for `ChatOrchestrationService` in `tests/Sorcha.Blueprint.Service.Tests/Services/ChatOrchestrationServiceTests.cs`

### SignalR Hub

- [x] T030 Create `ChatHub` in `src/Services/Sorcha.Blueprint.Service/Hubs/ChatHub.cs` implementing client-to-server methods per chat-hub.md contract
- [x] T031 Configure ChatHub in `src/Services/Sorcha.Blueprint.Service/Program.cs` (MapHub, CORS, authentication)
- [ ] T032 Write integration tests for `ChatHub` in `tests/Sorcha.Blueprint.Service.Tests/Hubs/ChatHubTests.cs`

### SignalR Client

- [x] T033 Create `IChatHubConnection` interface in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/IChatHubConnection.cs`
- [x] T034 Implement `ChatHubConnection` in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ChatHubConnection.cs` with reconnection logic (exponential backoff: 0s, 2s, 5s, 10s, 30s)
- [x] T035 Register ChatHubConnection in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`
- [ ] T036 Write unit tests for `ChatHubConnection` in `tests/Sorcha.UI.Core.Tests/Services/ChatHubConnectionTests.cs`

### DI Registration

- [x] T037 Create `ChatServicesExtensions` in `src/Services/Sorcha.Blueprint.Service/Extensions/ChatServicesExtensions.cs` to register all chat services
- [x] T038 Call ChatServicesExtensions in `src/Services/Sorcha.Blueprint.Service/Program.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Design Blueprint Through Conversation (Priority: P1) 🎯 MVP

**Goal**: Enable users to create blueprints through natural language conversation with real-time preview updates

**Independent Test**: Open chat interface, describe "a two-party approval workflow where Alice submits and Bob approves", verify blueprint shows correct participants and actions

### AI Tools for US1

- [x] T039 [US1] Implement `create_blueprint` tool in `BlueprintToolExecutor` per ai-tools.md
- [x] T040 [P] [US1] Implement `add_participant` tool in `BlueprintToolExecutor` per ai-tools.md
- [x] T041 [P] [US1] Implement `remove_participant` tool in `BlueprintToolExecutor` per ai-tools.md
- [x] T042 [P] [US1] Implement `add_action` tool in `BlueprintToolExecutor` per ai-tools.md
- [x] T043 [P] [US1] Implement `update_action` tool in `BlueprintToolExecutor` per ai-tools.md
- [ ] T044 [US1] Write unit tests for blueprint creation tools in `tests/Sorcha.Blueprint.Service.Tests/Services/BlueprintToolExecutorTests.cs`

### UI Components for US1

- [x] T045 [US1] Create `BlueprintChat.razor` page in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/BlueprintChat.razor` with chat panel and preview layout
- [x] T046 [P] [US1] Create `ChatPanel.razor` component in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Chat/ChatPanel.razor` for message display and input
- [x] T047 [P] [US1] Create `ChatMessageItem.razor` component in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Chat/ChatMessageItem.razor` for individual message rendering (streaming support)
- [x] T048 [P] [US1] Create `BlueprintPreview.razor` component in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Chat/BlueprintPreview.razor` for live blueprint visualization
- [x] T049 [US1] Wire up ChatHubConnection events to UI components in `BlueprintChat.razor`
- [x] T050 [US1] Add route `/designer/chat` to navigation in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Layout/MainLayout.razor`

### System Prompt

- [x] T051 [US1] Create system prompt for AI with blueprint context and tool documentation in `src/Services/Sorcha.Blueprint.Service/Services/ChatOrchestrationService.cs`

### E2E Tests for US1

- [ ] T052 [US1] Create `BlueprintChatTests.cs` in `tests/Sorcha.UI.E2E.Tests/BlueprintChatTests.cs`
- [ ] T053 [US1] Write E2E test: Create simple two-participant blueprint through conversation
- [ ] T054 [US1] Write E2E test: AI response streaming displays incrementally
- [ ] T055 [US1] Write E2E test: Blueprint preview updates after tool execution

**Checkpoint**: User Story 1 complete - users can design basic blueprints through conversation

---

## Phase 4: User Story 2 - Real-Time Blueprint Validation (Priority: P1) 🎯 MVP

**Goal**: Continuously validate blueprints and display errors, allow AI to explain and fix issues

**Independent Test**: Create invalid blueprint (missing participant), verify validation panel shows error, ask AI to fix it

### Validation Tool

- [x] T056 [US2] Implement `validate_blueprint` tool in `BlueprintToolExecutor` per ai-tools.md with error codes (MIN_PARTICIPANTS, MIN_ACTIONS, MISSING_SENDER, etc.)
- [ ] T057 [US2] Write unit tests for validate_blueprint tool

### Validation UI

- [x] T058 [US2] Add `ValidationPanel.razor` component in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Components/Chat/ValidationPanel.razor` showing errors/warnings
- [x] T059 [US2] Update `BlueprintPreview.razor` to display validation status with color indicators
- [x] T060 [US2] Update `BlueprintChat.razor` to trigger validation after each tool execution (auto-validation in ChatOrchestrationService)

### AI Validation Integration

- [x] T061 [US2] Update system prompt to instruct AI to call validate_blueprint after changes and explain errors
- [x] T062 [US2] Add logic in `ChatOrchestrationService` to auto-validate after blueprint-modifying tools

### E2E Tests for US2

- [ ] T063 [US2] Write E2E test: Validation errors display when blueprint is invalid
- [ ] T064 [US2] Write E2E test: AI explains validation error when asked
- [ ] T065 [US2] Write E2E test: AI fixes validation error through conversation

**Checkpoint**: User Stories 1 AND 2 complete - MVP functionality delivered

---

## Phase 5: User Story 3 - Define Data Schemas Through Conversation (Priority: P2)

**Goal**: Enable users to define data collection requirements that translate to proper JSON schemas

**Independent Test**: Ask AI to "collect name, email, and loan amount between 1000 and 50000", verify schema has correct types and constraints

### Schema Support in Tools

- [x] T066 [US3] Extend `add_action` tool to fully support dataFields parameter with types (string, number, integer, boolean, date, file)
- [x] T067 [P] [US3] Add schema constraint support (minLength, maxLength, minimum, maximum, pattern, format)
- [ ] T068 [US3] Write unit tests for data schema generation in tools

### Schema UI

- [x] T069 [US3] Update `BlueprintPreview.razor` to display action data schemas with field details
- [x] T070 [US3] Add schema field type icons and constraint indicators

### AI Schema Generation

- [x] T071 [US3] Update system prompt with data schema examples and best practices

### E2E Tests for US3

- [ ] T072 [US3] Write E2E test: AI creates schema with string fields and email format
- [ ] T073 [US3] Write E2E test: AI creates schema with numeric constraints
- [ ] T074 [US3] Write E2E test: AI modifies schema to make field optional

**Checkpoint**: User Story 3 complete - data schema definition through conversation

---

## Phase 6: User Story 4 - Configure Disclosure Rules (Priority: P2)

**Goal**: Enable users to control data visibility between participants through conversation

**Independent Test**: Ask AI to "only show salary to the manager", verify disclosure rules restrict that field

### Disclosure Tool

- [x] T075 [US4] Implement `set_disclosure` tool in `BlueprintToolExecutor` per ai-tools.md with field path support
- [ ] T076 [US4] Write unit tests for set_disclosure tool

### Disclosure UI

- [x] T077 [US4] Update `BlueprintPreview.razor` to display disclosure rules per action
- [x] T078 [US4] Add visual indicators showing which participant sees which fields

### AI Disclosure Configuration

- [x] T079 [US4] Update system prompt with disclosure examples and privacy best practices

### E2E Tests for US4

- [ ] T080 [US4] Write E2E test: AI configures disclosure to hide specific fields from participant
- [ ] T081 [US4] Write E2E test: AI adds field to existing disclosure list

**Checkpoint**: User Story 4 complete - disclosure rules through conversation

---

## Phase 7: User Story 5 - Edit Existing Blueprints (Priority: P3)

**Goal**: Load existing blueprints into the chat for conversational modification

**Independent Test**: Load saved blueprint, ask AI to "add a legal review step", verify modification applied

### Load Blueprint Functionality

- [x] T082 [US5] Add `existingBlueprintId` parameter handling in `ChatHub.StartSession` per chat-hub.md
- [x] T083 [US5] Implement blueprint loading in `ChatOrchestrationService.CreateSessionAsync`
- [x] T084 [US5] Update `ChatHubConnection` to support starting session with blueprint ID

### Edit UI

- [x] T085 [US5] Add blueprint selector/search to `BlueprintChat.razor` for loading existing blueprints
- [x] T086 [US5] Update `SessionStarted` handler to display loaded blueprint in preview

### AI Edit Context

- [x] T087 [US5] Update system prompt to describe loaded blueprint structure to AI
- [x] T088 [US5] Ensure AI can understand and modify existing blueprint elements

### E2E Tests for US5

- [ ] T089 [US5] Write E2E test: Load existing blueprint into chat session
- [ ] T090 [US5] Write E2E test: AI modifies loaded blueprint and changes persist

**Checkpoint**: User Story 5 complete - edit existing blueprints through conversation

---

## Phase 8: User Story 6 - Export and Save Blueprints (Priority: P3)

**Goal**: Save completed blueprints to storage or export as JSON/YAML

**Independent Test**: Complete blueprint design, click save, verify blueprint appears in blueprint list

### Save Functionality

- [x] T091 [US6] Implement `ChatHub.SaveBlueprint` method per chat-hub.md using existing Blueprint storage
- [x] T092 [US6] Add save confirmation handling in `ChatHubConnection`
- [x] T093 [US6] Add "Save Blueprint" button to `BlueprintChat.razor` (enabled only when valid)

### Export Functionality

- [x] T094 [US6] Implement `ChatHub.ExportBlueprint` method per chat-hub.md (JSON/YAML formats)
- [x] T095 [US6] Add export UI (dropdown for format selection, download trigger) to `BlueprintChat.razor`

### AI Save Integration

- [x] T096 [US6] Update system prompt to guide users to save when blueprint is complete

### E2E Tests for US6

- [ ] T097 [US6] Write E2E test: Save blueprint and verify in storage
- [ ] T098 [US6] Write E2E test: Export blueprint as JSON
- [ ] T099 [US6] Write E2E test: Export blueprint as YAML

**Checkpoint**: User Story 6 complete - full save and export functionality

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

### Routing Tool (Advanced)

- [x] T100 [P] Implement `add_routing` tool in `BlueprintToolExecutor` per ai-tools.md for conditional routing
- [ ] T101 Write unit tests for add_routing tool

### Session Management

- [x] T102 Implement message limit warning (MessageLimitWarning event when remaining <= 10) per FR-019
- [x] T103 [P] Implement session recovery for interrupted sessions (24hr window per SC-008)
- [x] T104 Add cancel generation button and implement `ChatHub.CancelGeneration`

### Error Handling

- [x] T105 Implement AI unavailability error display per FR-016 (after 3 retries)
- [x] T106 [P] Add SessionError event handling in UI with user-friendly messages

### Performance

- [ ] T107 Verify streaming latency < 2s (SC-003) and add performance logging
- [ ] T108 [P] Verify preview update latency < 1s (SC-004)
- [ ] T109 [P] Verify validation latency < 500ms (SC-005)

### Documentation

- [ ] T110 [P] Update `docs/` with blueprint chat feature documentation
- [ ] T111 [P] Add API documentation comments to ChatHub methods
- [ ] T112 Run quickstart.md validation to ensure developer setup works

### OpenTelemetry

- [ ] T113 Add OpenTelemetry tracing for AI provider calls per research.md
- [ ] T114 [P] Add structured logging for chat operations

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - US1 (P1) and US2 (P1) should be completed first (MVP)
  - US3 (P2) and US4 (P2) can proceed after MVP
  - US5 (P3) and US6 (P3) are final enhancements
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Integrates with US1 validation display
- **User Story 3 (P2)**: Can start after US1 - Extends add_action tool
- **User Story 4 (P2)**: Can start after US1 - Adds new disclosure tool
- **User Story 5 (P3)**: Can start after US1 - Adds load functionality
- **User Story 6 (P3)**: Can start after US1 - Adds save/export functionality

### Within Each User Story

- Models before services
- Services before endpoints/hub
- Backend before frontend components
- Core implementation before E2E tests
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational model tasks (T007-T014) can run in parallel
- AI tools (T039-T043) can be implemented in parallel
- UI components (T046-T048) can be implemented in parallel
- E2E tests can be written in parallel within a story

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 - Basic conversation design
4. Complete Phase 4: User Story 2 - Validation feedback
5. **STOP and VALIDATE**: Test MVP functionality end-to-end
6. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 + 2 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 3 (schemas) → Test independently → Deploy/Demo
4. Add User Story 4 (disclosures) → Test independently → Deploy/Demo
5. Add User Story 5 (edit) → Test independently → Deploy/Demo
6. Add User Story 6 (save/export) → Test independently → Deploy/Demo
7. Polish phase → Final release

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Performance targets: 2s streaming start (SC-003), 1s preview (SC-004), 500ms validation (SC-005)
- Message limit: 100 per session with warning at 10 remaining (FR-019)
- Retry policy: 3 retries with exponential backoff (2s, 4s, 8s) for AI failures (FR-016)
