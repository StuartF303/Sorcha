# Implementation Plan: CLI-4.4 Interactive Mode (REPL)

**Branch**: `master` | **Date**: 2025-12-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/master/spec.md`

## Summary

This plan implements an interactive console mode (REPL) for the Sorcha CLI, enabling administrators to execute multiple commands in a persistent session with context awareness, command history, tab completion, and rich terminal feedback. The implementation uses System.CommandLine for command parsing (reusing existing command infrastructure) and ReadLine for readline features (history, completion). Context is injected via middleware, and tab completion caches API responses with a 5-minute TTL and dirty cache handling.

**Key Technical Approach:**
- **In-process REPL** with System.CommandLine + ReadLine
- **Context middleware** injecting session state as default parameter values
- **Completion cache** with TTL and automatic invalidation on mutating operations
- **Silent error fallback** for tab completion API failures
- **Automatic multi-line mode** triggered by opening brace/bracket detection

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: System.CommandLine 2.0.0-beta4, ReadLine 2.0.1, Spectre.Console 0.49.1
**Storage**: Local file system (`~/.sorcha/history`, `~/.sorcha/config.json`, `~/.sorcha/session.log`)
**Testing**: xUnit, FluentAssertions
**Target Platform**: Windows, Linux, macOS (cross-platform CLI)
**Project Type**: Single console application (Sorcha.Cli)
**Performance Goals**: Tab completion < 100ms, history search < 50ms, startup < 500ms
**Constraints**: Must maintain identical command syntax to non-interactive mode, no breaking changes
**Scale/Scope**: ~1,500-2,000 LOC for REPL core, 500-700 LOC for tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Constitutional Principle | Status | Notes |
|-------------------------|--------|-------|
| ✅ Microservices-First Architecture | PASS | REPL is client-side, no service coupling |
| ✅ Security First | PASS | Tokens redacted in history, file permissions 0600, auto-logout after inactivity |
| ✅ API Documentation | N/A | No new APIs (CLI tool) |
| ✅ Testing Requirements | PASS | Plan includes >80% unit test coverage target |
| ✅ Code Quality | PASS | Using async/await, DI, .NET 10 patterns |
| ✅ Development Standards | PASS | Follows existing CLI patterns and conventions |
| ⚠️ Complexity Tracking | MINOR | Adds ~2,000 LOC but justified for UX improvement |

**No violations requiring justification.**

## Project Structure

### Documentation (this feature)

```text
specs/master/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (technical unknowns resolved)
├── data-model.md        # Phase 1 output (ReplSession, CacheEntry, command flow)
├── quickstart.md        # Phase 1 output (user guide for interactive mode)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Apps/Sorcha.Cli/
├── Commands/
│   ├── BaseCommand.cs              # [EXISTS] Command base class
│   ├── OrganizationCommands.cs     # [EXISTS] Org commands
│   ├── RegisterCommands.cs         # [EXISTS] Register commands
│   ├── WalletCommands.cs           # [EXISTS] Wallet commands
│   ├── PeerCommands.cs             # [EXISTS] Peer commands
│   └── ConsoleCommand.cs           # [NEW] REPL entry command
│
├── Repl/                           # [NEW] Interactive mode components
│   ├── ReplEngine.cs               # Main REPL loop manager
│   ├── ReplSession.cs              # Session state (context, history, cache)
│   ├── HistoryManager.cs           # Command history persistence
│   ├── CompletionProvider.cs       # Tab completion logic
│   ├── CompletionCache.cs          # Cached API data for completion
│   ├── CacheInvalidationMonitor.cs # Detects mutating commands
│   ├── ContextMiddleware.cs        # Injects session context into commands
│   ├── PromptRenderer.cs           # Context-aware prompt display
│   ├── MultiLineInputHandler.cs    # Brace matching for multi-line JSON
│   └── ReplConfiguration.cs        # REPL-specific config
│
├── Models/
│   ├── CliConfiguration.cs         # [EXISTS] CLI config
│   ├── CacheEntry.cs               # [NEW] Cache entry model
│   └── CompletionResult.cs         # [NEW] Completion result model
│
├── Services/
│   ├── IAuthenticationService.cs   # [EXISTS] Auth service
│   ├── ITenantServiceClient.cs     # [EXISTS] Tenant client
│   ├── IRegisterServiceClient.cs   # [EXISTS] Register client
│   └── IWalletServiceClient.cs     # [EXISTS] Wallet client
│
└── Program.cs                      # [MODIFY] Add ConsoleCommand to root

tests/Sorcha.Cli.Tests/
├── Commands/
│   └── ConsoleCommandTests.cs      # [NEW] Console command tests
│
└── Repl/                           # [NEW] REPL component tests
    ├── ReplEngineTests.cs
    ├── ReplSessionTests.cs
    ├── HistoryManagerTests.cs
    ├── CompletionProviderTests.cs
    ├── CompletionCacheTests.cs
    ├── CacheInvalidationMonitorTests.cs
    ├── ContextMiddlewareTests.cs
    ├── PromptRendererTests.cs
    └── MultiLineInputHandlerTests.cs
```

**Structure Decision**:
Single project structure maintained (Option 1). New `Repl/` namespace added under existing `Sorcha.Cli` project to encapsulate REPL-specific components while leveraging existing `Commands/`, `Models/`, and `Services/` infrastructure. This approach minimizes complexity and reuses System.CommandLine command definitions without duplication.

## Complexity Tracking

> **No constitutional violations requiring justification.**

While this feature adds ~2,000 LOC, the complexity is justified by:
- Significant UX improvement (no re-authentication, context awareness)
- Uses existing command infrastructure (no duplication)
- Maintains identical syntax to non-interactive mode (zero learning curve)
- Optional feature (non-interactive mode still available)

---

## Phase 0: Research and Resolve Technical Unknowns

**Output**: `specs/master/research.md`

### Research Questions

1. **ReadLine Library Capabilities**
   - Q: Does ReadLine 2.0.1 support custom tab completion providers?
   - Q: How does ReadLine handle Ctrl+R reverse search?
   - Q: Can ReadLine track multi-line input state for brace matching?
   - **Action**: Review ReadLine API documentation and examples

2. **System.CommandLine Context Injection**
   - Q: How can middleware intercept parsed command options before handler execution?
   - Q: What is the best way to inject default values for options not provided by user?
   - Q: Will context injection interfere with existing command handlers?
   - **Action**: Experiment with System.CommandLine middleware patterns

3. **Cache Invalidation Detection**
   - Q: How can we reliably detect command names containing "create/update/delete" keywords?
   - Q: Should cache invalidation be synchronous or async after command execution?
   - Q: What if a command fails—should cache still be invalidated?
   - **Action**: Design pattern for hooking into command execution lifecycle

4. **Multi-Line Input Implementation**
   - Q: Does ReadLine support custom prompt strings for continuation lines?
   - Q: How to handle nested brace matching (e.g., `{ "meta": { ... } }`)?
   - Q: Can user cancel multi-line input mid-entry?
   - **Action**: Prototype brace matching algorithm with ReadLine

5. **Error Handling for Tab Completion**
   - Q: Can tab completion be non-blocking (won't freeze terminal on slow API)?
   - Q: How to suppress error output during tab completion?
   - Q: Should stale cache be preferred over no completion at all?
   - **Action**: Design graceful degradation strategy

6. **Session State Persistence**
   - Q: Where should session state be stored (`~/.sorcha/` directory)?
   - Q: Should history file use plaintext or encrypted format?
   - Q: What is the max reasonable history size (1000 commands = ~100KB)?
   - **Action**: Define file format and permissions

7. **Authentication Token Lifecycle**
   - Q: How long should auth tokens persist in session?
   - Q: Should REPL auto-refresh expiring tokens?
   - Q: What happens when token expires mid-session?
   - **Action**: Design token refresh strategy

**Deliverable**: Document answers in `research.md` with code examples where applicable.

---

## Phase 1: Design, Data Models, and API Contracts

**Output**: `specs/master/data-model.md`, `specs/master/quickstart.md`, `specs/master/contracts/`

### 1.1 Data Models

**File**: `specs/master/data-model.md`

Document the following data structures:

#### ReplSession
```csharp
public class ReplSession
{
    public string? CurrentProfile { get; set; }
    public string? CurrentOrganizationId { get; set; }
    public string? CurrentOrganizationName { get; set; }
    public string? CurrentRegisterId { get; set; }
    public string? CurrentRegisterName { get; set; }
    public AuthenticationToken? Token { get; set; }
    public DateTime SessionStartTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public List<string> CommandHistory { get; set; } = new();
    public Dictionary<string, CacheEntry> CompletionCache { get; set; } = new();
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);
    public int AutoLogoutMinutes { get; set; } = 60;
}
```

#### CacheEntry
```csharp
public class CacheEntry
{
    public DateTime FetchedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public object Data { get; set; } // List of strings (IDs, names, addresses)
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    public bool IsStale() => IsExpired();
}
```

#### CompletionResult
```csharp
public class CompletionResult
{
    public string[] Suggestions { get; set; } = Array.Empty<string>();
    public bool FromCache { get; set; }
    public bool IsPartial { get; set; } // True if API call failed, showing only command/flag completion
}
```

#### ReplConfiguration
```csharp
public class ReplConfiguration
{
    public string HistoryFilePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sorcha", "history");
    public string SessionLogPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".sorcha", "session.log");
    public int MaxHistorySize { get; set; } = 1000;
    public TimeSpan CompletionCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
    public int AutoLogoutMinutes { get; set; } = 60;
    public bool EnableColors { get; set; } = true;
}
```

### 1.2 Component Interfaces

Document interfaces for key components:

#### IReplEngine
```csharp
public interface IReplEngine
{
    Task RunAsync(CancellationToken cancellationToken = default);
    void Exit();
}
```

#### IHistoryManager
```csharp
public interface IHistoryManager
{
    void AddCommand(string command);
    string[] GetHistory();
    string[] SearchHistory(string query);
    Task SaveAsync();
    Task LoadAsync();
}
```

#### ICompletionProvider
```csharp
public interface ICompletionProvider
{
    Task<CompletionResult> GetCompletionsAsync(string input, int cursorPosition);
    void InvalidateCache(string cacheKey);
    void RefreshCache();
}
```

#### IContextMiddleware
```csharp
public interface IContextMiddleware
{
    void InjectContext(ParseResult parseResult, ReplSession session);
}
```

### 1.3 Command Flow Diagram

Document the command execution flow:

```
User Input → ReadLine (history/completion) → Parse (System.CommandLine)
    ↓
Context Middleware (inject session defaults)
    ↓
Command Handler (existing, unchanged)
    ↓
Output (Spectre.Console formatting)
    ↓
Update Session State (if context changed)
    ↓
Cache Invalidation Monitor (if mutating command)
    ↓
Display Prompt (with updated context)
```

### 1.4 Quickstart Guide

**File**: `specs/master/quickstart.md`

Create user-facing documentation:

```markdown
# Sorcha CLI Interactive Mode - Quick Start

## Launch Interactive Mode

```bash
sorcha console
# OR
sorcha  # (no arguments)
```

## Welcome Screen

```
╔════════════════════════════════════════════╗
║   Sorcha CLI v1.0.0 - Interactive Mode   ║
║   Type 'help' for commands, 'exit' to quit ║
╔════════════════════════════════════════════╝

sorcha>
```

## Basic Commands

### Set Context
```
sorcha> use org acme-corp
✓ Switched to organization: Acme Corporation (acme-corp)
sorcha[acme-corp]>

sorcha[acme-corp]> use register invoices
✓ Switched to register: invoices (reg-inv-001)
sorcha[acme-corp/invoices]>
```

### View Current Context
```
sorcha[acme-corp]> status
╔══════════════════════════════════════════════╗
║           Session Status                     ║
╠══════════════════════════════════════════════╣
║ Profile:        dev                          ║
║ Organization:   Acme Corporation (acme-corp) ║
║ Register:       invoices (reg-inv-001)       ║
║ Authenticated:  Yes (token expires in 45m)   ║
║ Session Start:  2025-12-10 10:30:15          ║
╚══════════════════════════════════════════════╝
```

### Tab Completion
Press `Tab` to auto-complete commands, subcommands, and resource IDs:

```
sorcha> org <TAB>
list  get  create  update  delete

sorcha> org get --org-id <TAB>
acme-corp  techstart  innovate-inc

sorcha> wallet list --<TAB>
--profile  --output  --quiet  --verbose
```

### Command History
- **Up/Down arrows**: Navigate command history
- **Ctrl+R**: Reverse search history

```
(reverse-i-search)`wallet': wallet list
```

### Multi-Line Input
Automatically detected when you type `{` or `[`:

```
sorcha> tx submit --register-id reg-123 --payload {
... >   "type": "invoice",
... >   "amount": 1500.00,
... >   "metadata": {
... >     "invoice_id": "INV-2025-001"
... >   }
... > }
✓ Transaction submitted: tx-abc123
```

### Help System
```
sorcha> help
sorcha> help org
sorcha> ?
```

### Exit
```
sorcha> exit
# OR
sorcha> quit
# OR press Ctrl+C
```

## Tips

1. **Context-Aware Commands**: Once you set an org context with `use org`, you don't need to specify `--org-id` on every command.

2. **History Persistence**: Your command history is saved to `~/.sorcha/history` and persists across sessions.

3. **Cache Refresh**: If tab completion shows stale data, use `refresh` or press `Ctrl+F5` during completion.

4. **Session Log**: Errors and warnings are logged to `~/.sorcha/session.log`.

5. **Auto-Logout**: Sessions automatically expire after 60 minutes of inactivity (configurable).
```

### 1.5 Contracts

**Directory**: `specs/master/contracts/`

No external API contracts (REPL is client-side). Document internal contracts:

**File**: `specs/master/contracts/completion-cache-keys.md`

```markdown
# Completion Cache Keys

Standard cache keys used by CompletionProvider:

| Cache Key | Data Type | TTL | Invalidated By |
|-----------|-----------|-----|----------------|
| `organizations.list` | `List<string>` (IDs) | 5 min | `org create`, `org delete` |
| `registers.list` | `List<string>` (IDs) | 5 min | `register create`, `register delete` |
| `wallets.list` | `List<string>` (addresses) | 5 min | `wallet create`, `wallet delete` |
| `users.{orgId}.list` | `List<string>` (usernames) | 5 min | `user create`, `user delete` |
```

**File**: `specs/master/contracts/context-injection-rules.md`

```markdown
# Context Injection Rules

Middleware injects session context into command options using these rules:

| Option Name | Source | Condition |
|-------------|--------|-----------|
| `--org-id` | `ReplSession.CurrentOrganizationId` | If option not provided by user AND session has org context |
| `--register-id` | `ReplSession.CurrentRegisterId` | If option not provided by user AND session has register context |
| `--profile` | `ReplSession.CurrentProfile` | If option not provided by user AND session has profile context |

**Priority**: Explicit user values ALWAYS override session context.
```

---

## Phase 2: Implementation Tasks

**Output**: `specs/master/tasks.md` (generated by `/speckit.tasks` command, NOT by this plan)

Phase 2 execution is delegated to the `/speckit.tasks` command, which will generate a dependency-ordered task list based on this plan and the feature specification.

**Expected Task Categories**:
1. Core REPL Engine (ReplEngine, ReplSession, ReplConfiguration)
2. History Management (HistoryManager with file persistence)
3. Tab Completion (CompletionProvider, CompletionCache, CacheInvalidationMonitor)
4. Context Management (ContextMiddleware, prompt rendering)
5. Multi-Line Input (MultiLineInputHandler with brace matching)
6. Commands (ConsoleCommand entry point, `use` subcommands, `status`, `refresh`)
7. Unit Tests (for all components, target >80% coverage)
8. Integration Tests (end-to-end REPL session scenarios)
9. Documentation (README updates, quickstart guide)

---

## Implementation Notes

### Key Design Decisions

1. **Reuse System.CommandLine Infrastructure**: No command duplication. REPL parses input using the same `RootCommand` tree as non-interactive mode, ensuring identical syntax and behavior.

2. **Middleware-Based Context Injection**: Session state (current org/register) is injected as default values for options AFTER parsing but BEFORE handler execution. This keeps handlers stateless and testable.

3. **Graceful Degradation for Tab Completion**: API failures during tab completion are silent. User sees command/flag completion only, and errors are logged. Stale cache data is tolerated.

4. **Automatic Cache Invalidation**: Monitor watches for command names containing "create", "update", "delete", "remove" and invalidates corresponding cache entries after successful execution.

5. **Multi-Line Input Triggering**: Automatic detection of opening brace/bracket triggers multi-line mode. Closing brace/bracket (when balanced) exits multi-line mode. User can cancel with Ctrl+C.

6. **Session Persistence**: History and session log persist to disk. Auth tokens are in-memory only (not persisted). History file has 0600 permissions (user-only read/write).

### Testing Strategy

- **Unit Tests**: Mock dependencies (IAuthenticationService, ITenantServiceClient, etc.) to test REPL components in isolation
- **Integration Tests**: End-to-end scenarios (launch REPL, set context, execute commands, verify state)
- **Manual Testing**: Cross-platform verification (Windows, Linux, macOS) with real API interactions

### Risk Mitigation

| Risk | Mitigation |
|------|------------|
| ReadLine incompatibility on macOS | Test early on macOS; fallback to basic Console.ReadLine if needed |
| Tab completion latency > 100ms | Implement timeout (100ms) for API calls; use cache as fallback |
| Context injection breaks existing commands | Thorough integration tests; middleware only injects when option not provided |
| Multi-line input confusing users | Clear prompt change (`... >`); document in quickstart guide |

### Dependencies

- **Existing CLI infrastructure** (Commands, Services, Models) - MUST be stable
- **ReadLine library** - Research Phase 0 will validate capabilities
- **System.CommandLine** - Already in use; middleware pattern to be validated in Phase 0

---

## Acceptance Criteria

From feature specification (`spec.md`):

- [x] All existing CLI commands work in interactive mode
- [ ] Command history persists across sessions
- [ ] Tab completion works for commands, subcommands, and common resource IDs
- [ ] Context management (`use org`, `use register`) functions correctly
- [ ] Prompt reflects current context
- [ ] Session exits gracefully
- [ ] Performance metrics met (< 100ms tab completion, < 500ms startup)
- [ ] Security requirements met (history permissions, token redaction)

---

## Next Steps

1. **Execute Phase 0 Research** (`/speckit.plan` continues with research workflow)
2. **Execute Phase 1 Design** (generate data models, quickstart, contracts)
3. **Run `/speckit.tasks`** to generate implementation task list from this plan
4. **Begin Implementation** following generated tasks

---

**Plan Status**: Ready for Phase 0 Research
**Plan Author**: AI Assistant (Claude Sonnet 4.5)
**Plan Date**: 2025-12-10
