# Tasks: CLI-4.4 Interactive Mode (REPL)

**Input**: Design documents from `specs/master/`
**Prerequisites**: plan.md, spec.md (4 user stories: US-1 through US-4)

**Tests**: Unit tests are included for all REPL components (>80% coverage target per constitution)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

Single project structure:
- Production code: `src/Apps/Sorcha.Cli/`
- Tests: `tests/Sorcha.Cli.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and REPL directory structure

- [ ] T001 Create `src/Apps/Sorcha.Cli/Repl/` directory for REPL components
- [ ] T002 Create `src/Apps/Sorcha.Cli/Models/` directory if not exists for REPL models
- [ ] T003 Create `tests/Sorcha.Cli.Tests/Repl/` directory for REPL component tests
- [ ] T004 [P] Create `.sorcha` user directory structure in user home folder (`~/.sorcha/`)
- [ ] T005 [P] Verify ReadLine 2.0.1 package reference in `src/Apps/Sorcha.Cli/Sorcha.Cli.csproj`
- [ ] T006 [P] Verify Spectre.Console 0.49.1 package reference in `src/Apps/Sorcha.Cli/Sorcha.Cli.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core REPL infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Create `ReplConfiguration` model in `src/Apps/Sorcha.Cli/Repl/ReplConfiguration.cs`
- [ ] T008 Create `ReplSession` model in `src/Apps/Sorcha.Cli/Repl/ReplSession.cs`
- [ ] T009 [P] Create `CacheEntry` model in `src/Apps/Sorcha.Cli/Models/CacheEntry.cs`
- [ ] T010 [P] Create `CompletionResult` model in `src/Apps/Sorcha.Cli/Models/CompletionResult.cs`
- [ ] T011 Create `IReplEngine` interface in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T012 Create `ReplEngine` skeleton implementation with main REPL loop in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T013 Create unit test for `ReplConfiguration` in `tests/Sorcha.Cli.Tests/Repl/ReplConfigurationTests.cs`
- [ ] T014 Create unit test for `ReplSession` in `tests/Sorcha.Cli.Tests/Repl/ReplSessionTests.cs`
- [ ] T015 [P] Create unit test for `ReplEngine` initialization in `tests/Sorcha.Cli.Tests/Repl/ReplEngineTests.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Basic Interactive Session (Priority: P1) 🎯 MVP

**Goal**: Enable launching interactive mode, executing existing CLI commands, and graceful exit

**Independent Test**:
1. Run `sorcha console` or `sorcha` (no args)
2. Verify welcome banner displays
3. Execute `org list` command
4. Type `exit` and verify session exits cleanly
5. Verify history file created at `~/.sorcha/history`

### Tests for User Story 1

- [ ] T016 [P] [US1] Create unit test for ConsoleCommand structure in `tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs`
- [ ] T017 [P] [US1] Create unit test for ReplEngine.RunAsync() basic flow in `tests/Sorcha.Cli.Tests/Repl/ReplEngineTests.cs`
- [ ] T018 [P] [US1] Create unit test for welcome banner display in `tests/Sorcha.Cli.Tests/Repl/PromptRendererTests.cs`

### Implementation for User Story 1

- [ ] T019 [P] [US1] Create `ConsoleCommand` entry point in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T020 [P] [US1] Create `PromptRenderer` for welcome banner and prompt display in `src/Apps/Sorcha.Cli/Repl/PromptRenderer.cs`
- [ ] T021 [US1] Implement ReplEngine.RunAsync() main loop with ReadLine input in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T022 [US1] Implement command parsing using existing System.CommandLine root command in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T023 [US1] Implement command execution via existing command handlers in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T024 [US1] Implement exit handling (exit, quit, Ctrl+C) in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T025 [US1] Add ConsoleCommand to root command in `src/Apps/Sorcha.Cli/Program.cs`
- [ ] T026 [US1] Implement session cleanup on exit in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`

**Checkpoint**: At this point, User Story 1 should be fully functional - can launch interactive mode, execute existing commands, and exit

---

## Phase 4: User Story 2 - Context-Aware Operations (Priority: P2)

**Goal**: Enable setting organization/register context and inject context into commands as defaults

**Independent Test**:
1. Launch `sorcha console`
2. Execute `use org acme-corp`
3. Verify prompt changes to `sorcha[acme-corp]>`
4. Execute `org get` (without --org-id) and verify it uses context
5. Execute `status` and verify it shows current org
6. Execute `use org` (no args) and verify context cleared

### Tests for User Story 2

- [ ] T027 [P] [US2] Create unit test for ContextMiddleware injection logic in `tests/Sorcha.Cli.Tests/Repl/ContextMiddlewareTests.cs`
- [ ] T028 [P] [US2] Create unit test for UseOrgCommand in `tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs`
- [ ] T029 [P] [US2] Create unit test for UseRegisterCommand in `tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs`
- [ ] T030 [P] [US2] Create unit test for StatusCommand in `tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs`
- [ ] T031 [P] [US2] Create unit test for context-aware prompt rendering in `tests/Sorcha.Cli.Tests/Repl/PromptRendererTests.cs`

### Implementation for User Story 2

- [ ] T032 [P] [US2] Create `ContextMiddleware` for injecting session context into parsed commands in `src/Apps/Sorcha.Cli/Repl/ContextMiddleware.cs`
- [ ] T033 [P] [US2] Implement `UseOrgCommand` subcommand in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T034 [P] [US2] Implement `UseRegisterCommand` subcommand in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T035 [P] [US2] Implement `UseProfileCommand` subcommand in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T036 [P] [US2] Implement `StatusCommand` subcommand in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T037 [US2] Integrate ContextMiddleware into ReplEngine command execution flow in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T038 [US2] Update PromptRenderer to display current context (org/register/profile) in `src/Apps/Sorcha.Cli/Repl/PromptRenderer.cs`
- [ ] T039 [US2] Update ReplSession to track context changes in `src/Apps/Sorcha.Cli/Repl/ReplSession.cs`

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - basic session + context management

---

## Phase 5: User Story 3 - Command History Navigation (Priority: P3)

**Goal**: Enable navigating command history with arrow keys and Ctrl+R search, with persistence

**Independent Test**:
1. Launch `sorcha console`
2. Execute several commands (e.g., `org list`, `wallet list`, `status`)
3. Press Up arrow and verify previous command appears
4. Press Down arrow and verify forward navigation
5. Press Ctrl+R and type `wallet` to search
6. Exit and relaunch - verify history persisted
7. Verify history file at `~/.sorcha/history` contains commands

### Tests for User Story 3

- [ ] T040 [P] [US3] Create unit test for HistoryManager.AddCommand() in `tests/Sorcha.Cli.Tests/Repl/HistoryManagerTests.cs`
- [ ] T041 [P] [US3] Create unit test for HistoryManager.GetHistory() in `tests/Sorcha.Cli.Tests/Repl/HistoryManagerTests.cs`
- [ ] T042 [P] [US3] Create unit test for HistoryManager.SearchHistory() in `tests/Sorcha.Cli.Tests/Repl/HistoryManagerTests.cs`
- [ ] T043 [P] [US3] Create unit test for HistoryManager.SaveAsync() persistence in `tests/Sorcha.Cli.Tests/Repl/HistoryManagerTests.cs`
- [ ] T044 [P] [US3] Create unit test for HistoryManager.LoadAsync() loading in `tests/Sorcha.Cli.Tests/Repl/HistoryManagerTests.cs`
- [ ] T045 [P] [US3] Create unit test for history size limit (1000 commands) in `tests/Sorcha.Cli.Tests/Repl/HistoryManagerTests.cs`

### Implementation for User Story 3

- [ ] T046 [P] [US3] Create `IHistoryManager` interface in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T047 [US3] Implement HistoryManager with in-memory command list in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T048 [US3] Implement HistoryManager.AddCommand() with max 1000 limit in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T049 [US3] Implement HistoryManager.SaveAsync() to write `~/.sorcha/history` file in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T050 [US3] Implement HistoryManager.LoadAsync() to read from `~/.sorcha/history` file in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T051 [US3] Implement HistoryManager.SearchHistory() for Ctrl+R reverse search in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T052 [US3] Set file permissions to 0600 (user-only) on history file in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T053 [US3] Integrate HistoryManager into ReplEngine with ReadLine integration in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T054 [US3] Configure ReadLine to use HistoryManager for Up/Down arrow navigation in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T055 [US3] Implement token redaction in history (sensitive data protection) in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`
- [ ] T056 [US3] Implement history corruption protection on load in `src/Apps/Sorcha.Cli/Repl/HistoryManager.cs`

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently - session + context + history

---

## Phase 6: User Story 4 - Tab Completion (Priority: P4)

**Goal**: Enable tab completion for commands, subcommands, flags, and resource IDs with caching

**Independent Test**:
1. Launch `sorcha console`
2. Type `org ` and press Tab - verify subcommands shown (list, get, create, etc.)
3. Type `org get --org-` and press Tab - verify `--org-id` completed
4. Type `org get --org-id ` and press Tab - verify org IDs from API cache shown
5. Create a new org and verify cache invalidated
6. Type `refresh` command and verify cache cleared

### Tests for User Story 4

- [ ] T057 [P] [US4] Create unit test for CompletionProvider command name completion in `tests/Sorcha.Cli.Tests/Repl/CompletionProviderTests.cs`
- [ ] T058 [P] [US4] Create unit test for CompletionProvider subcommand completion in `tests/Sorcha.Cli.Tests/Repl/CompletionProviderTests.cs`
- [ ] T059 [P] [US4] Create unit test for CompletionProvider option flag completion in `tests/Sorcha.Cli.Tests/Repl/CompletionProviderTests.cs`
- [ ] T060 [P] [US4] Create unit test for CompletionProvider resource ID completion from cache in `tests/Sorcha.Cli.Tests/Repl/CompletionProviderTests.cs`
- [ ] T061 [P] [US4] Create unit test for CompletionCache TTL expiry (5 min) in `tests/Sorcha.Cli.Tests/Repl/CompletionCacheTests.cs`
- [ ] T062 [P] [US4] Create unit test for CompletionCache.Get() with expired entry in `tests/Sorcha.Cli.Tests/Repl/CompletionCacheTests.cs`
- [ ] T063 [P] [US4] Create unit test for CacheInvalidationMonitor detecting create commands in `tests/Sorcha.Cli.Tests/Repl/CacheInvalidationMonitorTests.cs`
- [ ] T064 [P] [US4] Create unit test for CacheInvalidationMonitor detecting update commands in `tests/Sorcha.Cli.Tests/Repl/CacheInvalidationMonitorTests.cs`
- [ ] T065 [P] [US4] Create unit test for CacheInvalidationMonitor detecting delete commands in `tests/Sorcha.Cli.Tests/Repl/CacheInvalidationMonitorTests.cs`
- [ ] T066 [P] [US4] Create unit test for graceful degradation on API failure in `tests/Sorcha.Cli.Tests/Repl/CompletionProviderTests.cs`

### Implementation for User Story 4

- [ ] T067 [P] [US4] Create `ICompletionProvider` interface in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`
- [ ] T068 [P] [US4] Create `CompletionCache` class in `src/Apps/Sorcha.Cli/Repl/CompletionCache.cs`
- [ ] T069 [US4] Implement CompletionCache.Get() with TTL check in `src/Apps/Sorcha.Cli/Repl/CompletionCache.cs`
- [ ] T070 [US4] Implement CompletionCache.Set() with expiry timestamp in `src/Apps/Sorcha.Cli/Repl/CompletionCache.cs`
- [ ] T071 [US4] Implement CompletionCache.Invalidate() for specific cache keys in `src/Apps/Sorcha.Cli/Repl/CompletionCache.cs`
- [ ] T072 [US4] Implement CompletionProvider.GetCompletionsAsync() for command names in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`
- [ ] T073 [US4] Implement CompletionProvider subcommand completion logic in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`
- [ ] T074 [US4] Implement CompletionProvider option flag completion logic in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`
- [ ] T075 [US4] Implement CompletionProvider resource ID completion with API fetch in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`
- [ ] T076 [US4] Implement graceful degradation on API timeout (<100ms limit) in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`
- [ ] T077 [US4] Implement stale cache tolerance (use expired cache on error) in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`
- [ ] T078 [US4] Create `CacheInvalidationMonitor` to detect mutating commands in `src/Apps/Sorcha.Cli/Repl/CacheInvalidationMonitor.cs`
- [ ] T079 [US4] Implement keyword detection (create/update/delete/remove) in `src/Apps/Sorcha.Cli/Repl/CacheInvalidationMonitor.cs`
- [ ] T080 [US4] Implement cache invalidation logic for organizations.list in `src/Apps/Sorcha.Cli/Repl/CacheInvalidationMonitor.cs`
- [ ] T081 [US4] Implement cache invalidation logic for registers.list in `src/Apps/Sorcha.Cli/Repl/CacheInvalidationMonitor.cs`
- [ ] T082 [US4] Implement cache invalidation logic for wallets.list in `src/Apps/Sorcha.Cli/Repl/CacheInvalidationMonitor.cs`
- [ ] T083 [US4] Integrate CompletionProvider with ReadLine tab completion callback in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T084 [US4] Integrate CacheInvalidationMonitor into post-command execution flow in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T085 [US4] Implement RefreshCommand to manually clear cache in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T086 [US4] Add session logging for completion errors to `~/.sorcha/session.log` in `src/Apps/Sorcha.Cli/Repl/CompletionProvider.cs`

**Checkpoint**: At this point, all 4 user stories should work independently and together - full REPL functionality

---

## Phase 7: Advanced Features & Polish

**Purpose**: Enhancements and cross-cutting concerns

- [ ] T087 [P] Create `MultiLineInputHandler` for brace matching in `src/Apps/Sorcha.Cli/Repl/MultiLineInputHandler.cs`
- [ ] T088 [P] Create unit test for MultiLineInputHandler brace detection in `tests/Sorcha.Cli.Tests/Repl/MultiLineInputHandlerTests.cs`
- [ ] T089 [P] Create unit test for MultiLineInputHandler nested braces in `tests/Sorcha.Cli.Tests/Repl/MultiLineInputHandlerTests.cs`
- [ ] T090 Implement automatic multi-line mode on opening brace/bracket in `src/Apps/Sorcha.Cli/Repl/MultiLineInputHandler.cs`
- [ ] T091 Implement prompt change to `... >` for continuation lines in `src/Apps/Sorcha.Cli/Repl/PromptRenderer.cs`
- [ ] T092 Implement brace nesting depth tracking in `src/Apps/Sorcha.Cli/Repl/MultiLineInputHandler.cs`
- [ ] T093 Implement Ctrl+C cancellation during multi-line input in `src/Apps/Sorcha.Cli/Repl/MultiLineInputHandler.cs`
- [ ] T094 Integrate MultiLineInputHandler into ReplEngine input loop in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T095 [P] Implement auto-logout after 60 minutes inactivity in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T096 [P] Implement session state auto-save every 30 seconds in `src/Apps/Sorcha.Cli/Repl/ReplEngine.cs`
- [ ] T097 [P] Implement ClearCommand for clearing screen in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T098 [P] Implement HelpCommand override for REPL-specific help in `src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs`
- [ ] T099 [P] Add colored output for success/error/warning in `src/Apps/Sorcha.Cli/Repl/PromptRenderer.cs`
- [ ] T100 [P] Update README.md with interactive mode usage examples in `README.md`
- [ ] T101 [P] Create quickstart.md user guide in `specs/master/quickstart.md`
- [ ] T102 [P] Add integration test for end-to-end REPL session in `tests/Sorcha.Cli.Tests/Repl/ReplEngineIntegrationTests.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (US-1 → US-2 → US-3 → US-4)
- **Advanced Features & Polish (Phase 7)**: Depends on desired user stories being complete (minimum US-1)

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Integrates with US-1 but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Independent of US-1/US-2
- **User Story 4 (P4)**: Can start after Foundational (Phase 2) - Independent of US-1/US-2/US-3

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models before services
- Services before integration
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- All tests for a user story marked [P] can run in parallel
- Models/classes within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Create unit test for ConsoleCommand structure in tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs"
Task: "Create unit test for ReplEngine.RunAsync() basic flow in tests/Sorcha.Cli.Tests/Repl/ReplEngineTests.cs"
Task: "Create unit test for welcome banner display in tests/Sorcha.Cli.Tests/Repl/PromptRendererTests.cs"

# Launch parallel implementations for User Story 1:
Task: "Create ConsoleCommand entry point in src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs"
Task: "Create PromptRenderer for welcome banner in src/Apps/Sorcha.Cli/Repl/PromptRenderer.cs"
```

---

## Parallel Example: User Story 2

```bash
# Launch all tests for User Story 2 together:
Task: "Create unit test for ContextMiddleware injection logic in tests/Sorcha.Cli.Tests/Repl/ContextMiddlewareTests.cs"
Task: "Create unit test for UseOrgCommand in tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs"
Task: "Create unit test for UseRegisterCommand in tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs"
Task: "Create unit test for StatusCommand in tests/Sorcha.Cli.Tests/Commands/ConsoleCommandTests.cs"
Task: "Create unit test for context-aware prompt in tests/Sorcha.Cli.Tests/Repl/PromptRendererTests.cs"

# Launch parallel implementations for User Story 2:
Task: "Create ContextMiddleware in src/Apps/Sorcha.Cli/Repl/ContextMiddleware.cs"
Task: "Implement UseOrgCommand in src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs"
Task: "Implement UseRegisterCommand in src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs"
Task: "Implement UseProfileCommand in src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs"
Task: "Implement StatusCommand in src/Apps/Sorcha.Cli/Commands/ConsoleCommand.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: Foundational (T007-T015) - CRITICAL - blocks all stories
3. Complete Phase 3: User Story 1 (T016-T026)
4. **STOP and VALIDATE**: Test User Story 1 independently:
   - Launch `sorcha console`
   - Execute existing commands
   - Exit cleanly
   - Verify history file created
5. Deploy/demo if ready - **basic interactive mode is functional!**

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready (T001-T015)
2. Add User Story 1 (T016-T026) → Test independently → Deploy/Demo (MVP! ✅)
3. Add User Story 2 (T027-T039) → Test independently → Deploy/Demo (MVP + Context awareness ✅)
4. Add User Story 3 (T040-T056) → Test independently → Deploy/Demo (MVP + Context + History ✅)
5. Add User Story 4 (T057-T086) → Test independently → Deploy/Demo (Full REPL! ✅)
6. Add Advanced Features (T087-T102) → Polish and enhance (Multi-line, docs, etc.)
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (T001-T015)
2. Once Foundational is done:
   - **Developer A**: User Story 1 (T016-T026) - Basic Interactive Session
   - **Developer B**: User Story 2 (T027-T039) - Context Management
   - **Developer C**: User Story 3 (T040-T056) - Command History
   - **Developer D**: User Story 4 (T057-T086) - Tab Completion
3. Stories complete and integrate independently
4. Team converges on Phase 7 for advanced features and polish

---

## Task Summary

**Total Tasks**: 102
- **Setup (Phase 1)**: 6 tasks
- **Foundational (Phase 2)**: 9 tasks (blocks all stories)
- **User Story 1 (Phase 3)**: 11 tasks (8 implementation + 3 tests)
- **User Story 2 (Phase 4)**: 13 tasks (8 implementation + 5 tests)
- **User Story 3 (Phase 5)**: 17 tasks (11 implementation + 6 tests)
- **User Story 4 (Phase 6)**: 30 tasks (20 implementation + 10 tests)
- **Advanced Features & Polish (Phase 7)**: 16 tasks

**Parallel Opportunities**: 45 tasks marked [P] can run in parallel within their phase

**Independent Test Criteria**:
- **US-1**: Launch, execute commands, exit, verify history file
- **US-2**: Set context, verify prompt change, execute context-aware command, view status
- **US-3**: Navigate history with arrows, search with Ctrl+R, verify persistence
- **US-4**: Tab complete commands/flags/IDs, verify cache refresh on create/delete

**Suggested MVP Scope**: User Story 1 only (T001-T026 = 26 tasks)

**Format Validation**: ✅ All tasks follow checklist format with ID, [P] marker (if parallel), [Story] label (if user story phase), and file paths

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- All file paths are absolute from repository root
