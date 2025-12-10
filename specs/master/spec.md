# CLI-4.4: Interactive Mode (REPL) Feature Specification

**Version:** 1.0
**Date:** 2025-12-10
**Status:** Clarification Phase
**Feature ID:** CLI-4.4
**Sprint:** Sprint 4

---

## Executive Summary

The Sorcha CLI Interactive Mode (REPL - Read-Eval-Print Loop) provides an interactive console interface for platform administration. This mode enables operators to execute multiple commands in a persistent session with context awareness, command history, tab completion, and rich terminal feedback.

**Key Benefits:**
- Single authentication session for multiple operations
- Context-aware command execution (current org, register, profile)
- Enhanced productivity with tab completion and command history
- Rich visual feedback with colors, tables, and progress indicators
- Lower cognitive load compared to repeated CLI invocations

---

## Functional Requirements

### Core Features

1. **Session Management**
   - Launch interactive mode via `sorcha console` or `sorcha` (no arguments)
   - Maintain session state including authentication tokens, current context
   - Graceful exit via `exit`, `quit`, or Ctrl+C
   - Auto-save session history to disk

2. **Command Execution**
   - Accept all existing CLI commands in interactive mode
   - Execute commands synchronously with immediate feedback
   - Support multi-line input for complex JSON payloads (see Multi-Line Input section below)
   - Preserve command syntax identical to non-interactive mode

   **Multi-Line Input:**
   - **Automatic Detection:** When user types opening brace `{` or bracket `[`, REPL enters multi-line mode
   - **Prompt Change:** Subsequent lines show continuation prompt `... >` instead of `sorcha>`
   - **Termination:** Multi-line mode ends when closing brace/bracket is balanced
   - **Editing:** User can edit multi-line input with arrow keys, backspace
   - **Cancellation:** `Ctrl+C` cancels multi-line input and returns to normal prompt
   - **Example:**
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
   - **Brace Matching:** Parser tracks nesting depth to detect proper closing
   - **Validation:** JSON syntax validation occurs after closing brace (show error if malformed, allow correction)

3. **Command History**
   - Navigate command history using Up/Down arrow keys
   - Persist history across sessions (stored in `~/.sorcha/history`)
   - Search history with Ctrl+R (reverse search)
   - Maximum 1000 commands in history

4. **Tab Completion**
   - Auto-complete command names (e.g., `org <TAB>` → list of subcommands)
   - Auto-complete resource IDs from cached API responses (e.g., org IDs, register IDs, wallet addresses)
   - Auto-complete option flags (e.g., `--reg<TAB>` → `--register-id`)
   - Display completion suggestions when multiple matches exist

   **Completion Data Caching:**
   - Cache API responses in session memory with configurable TTL (default: 5 minutes)
   - Cache entries marked with timestamp and expiry
   - On tab completion: check cache first, fetch from API if expired or missing
   - **Dirty Cache Handling:** Cache invalidated when user performs create/update/delete operations (e.g., creating new org invalidates org list cache)
   - Force refresh with `Ctrl+F5` or `refresh` command

5. **Context Management**
   - `use org <id>` - Set current organization context
   - `use register <id>` - Set current register context
   - `use profile <name>` - Switch environment profile
   - `status` - Display current context (auth status, active org/register/profile)
   - Display context in prompt: `sorcha[org-name]>` or `sorcha[profile:org-name]>`

6. **Help System**
   - `help` - Show available commands
   - `help <command>` - Show help for specific command
   - `?` - Alias for help
   - Inline command suggestions for typos

7. **Visual Enhancements**
   - Colored output (success = green, error = red, warning = yellow)
   - Table formatting for list commands
   - Progress indicators for long-running operations
   - Welcome banner on startup
   - Clear screen command (`clear`)

---

## Technical Architecture

### REPL Implementation

**Architecture:** In-process with System.CommandLine + ReadLine.Refit for rich terminal features

**Components:**
- **REPL Loop Manager** - Main loop handling input/output, session lifecycle
- **Command Parser** - Leverages existing System.CommandLine infrastructure
- **Session State Manager** - Maintains context (auth tokens, current org/register/profile)
- **History Manager** - Persists and retrieves command history
- **Completion Cache Manager** - Manages cached API data for tab completion with TTL and invalidation
- **Completion Provider** - Generates tab completion suggestions from command metadata + cached data
- **Cache Invalidation Monitor** - Watches for mutating commands (create/update/delete) and invalidates affected cache entries
- **Prompt Renderer** - Displays context-aware prompt with colors

**Dependencies:**
- `System.CommandLine` (already in use) - Command parsing and execution
- `ReadLine.Refit` or `Spectre.Console` - Readline features (history, completion, colors)
- Existing command infrastructure (no duplication)

### Session State

**Stored State:**
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
    public List<string> CommandHistory { get; set; }
    public Dictionary<string, CacheEntry> CompletionCache { get; set; }
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);
}

public class CacheEntry
{
    public DateTime FetchedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public object Data { get; set; } // List of org IDs, register IDs, etc.

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
```

### Command Flow

1. User enters command in REPL
2. REPL loop reads input with ReadLine features (history, completion)
3. Command parsed using existing System.CommandLine root command
4. **Context Middleware** intercepts parsed command and injects session context (current org/register) as default parameter values for options that support context-awareness (e.g., `--org-id` defaults to session's CurrentOrganizationId if not explicitly provided)
5. Command executed via existing command handlers (unchanged, no direct session state access)
6. Output rendered with colors/formatting
7. Session state updated if context changed (e.g., `use org` command)
8. Prompt redisplayed with updated context

**Context Injection Mechanism:**
- Middleware checks if command has `--org-id`, `--register-id`, or `--profile` options
- If option not provided in user input AND session has corresponding context value, inject as default
- Explicit user values always override session context
- Command handlers remain stateless and testable

### Cache Invalidation Strategy

**Automatic Invalidation:**
- **Create operations** (e.g., `org create`, `register create`, `wallet create`) → Invalidate corresponding list cache
- **Update operations** (e.g., `org update`, `user update`) → Invalidate both list and specific item cache
- **Delete operations** (e.g., `org delete`, `wallet delete`) → Invalidate list cache
- **Detected pattern:** Monitor command names containing `create`, `update`, `delete`, `remove` keywords

**Manual Refresh:**
- `refresh` command → Clear all completion cache
- `refresh orgs` → Clear only organization cache
- `Ctrl+F5` during tab completion → Force fresh API fetch (bypass cache for that completion only)

**Cache Keys:**
- `organizations.list` → List of all org IDs + names
- `registers.list` → List of all register IDs + names
- `wallets.list` → List of wallet addresses
- `users.{orgId}.list` → Users for specific org (context-dependent)

**TTL Configuration:**
- Default: 5 minutes
- Configurable via `~/.sorcha/config.json`: `"completionCacheTtl": 300` (seconds)
- Environment-specific: Dev (1 min), Staging (3 min), Production (5 min)

### Tab Completion Error Handling

**API Fetch Failures:**
When tab completion attempts to fetch resource IDs from the API and encounters an error (network failure, 401 Unauthorized, 500 Internal Server Error, timeout, etc.):

1. **Graceful Degradation:** Silently fall back to basic completion (command names, subcommands, option flags only)
2. **No User Interruption:** Do NOT display error messages or dialogs during tab completion
3. **Logging:** Log warning to session log file (`~/.sorcha/session.log`) with error details
4. **Stale Cache Tolerance:** If stale cached data exists (expired but still in memory), use it for completion
5. **Retry Strategy:** On next tab press, retry API call (failures don't permanently disable resource completion)

**Error Scenarios:**
- **Network timeout** → Use command/flag completion, log warning
- **401 Unauthorized** → Use command/flag completion, log "session expired" warning, suggest re-authentication
- **500 Server Error** → Use command/flag completion, log server error
- **Stale cache available** → Prefer stale cache over no resource completion (mark with `*` indicator if shown)

**User Feedback (Minimal):**
- Only display subtle indicator if verbose mode enabled: `(using cached data)` in completion list
- Critical auth errors: Show one-time session warning banner on next command execution (not during tab completion)

---

## User Stories

### US-1: Basic Interactive Session
**As an** administrator
**I want to** launch an interactive console session
**So that** I can execute multiple commands without re-authenticating

**Acceptance Criteria:**
- Launch via `sorcha console` or `sorcha`
- Welcome banner displays CLI version and help text
- Prompt displays `sorcha>` initially
- Can execute any existing CLI command
- Exit via `exit`, `quit`, or Ctrl+C
- Session history persisted to `~/.sorcha/history`

### US-2: Context-Aware Operations
**As an** administrator
**I want to** set a current organization context
**So that** I don't have to specify `--org-id` on every command

**Acceptance Criteria:**
- `use org <id>` sets current organization
- Prompt changes to `sorcha[org-name]>`
- Subsequent commands default to current org (if applicable)
- `status` shows current org, register, profile
- Context cleared on `use org` with no arguments

### US-3: Command History Navigation
**As an** administrator
**I want to** navigate previous commands with arrow keys
**So that** I can quickly re-run or modify recent operations

**Acceptance Criteria:**
- Up arrow retrieves previous command
- Down arrow moves forward in history
- History persists across sessions
- Ctrl+R enables reverse search
- Maximum 1000 commands stored

### US-4: Tab Completion
**As an** administrator
**I want to** auto-complete commands and IDs
**So that** I can work faster and avoid typos

**Acceptance Criteria:**
- Tab completes command names (e.g., `org <TAB>`)
- Tab completes subcommands
- Tab completes option flags
- Tab completes resource IDs from cache (organizations, registers)
- Multiple matches show suggestion list

---

## Non-Functional Requirements

### Performance
- Command execution latency: Same as non-interactive mode (no additional overhead)
- Tab completion response time: < 100ms
- History search: < 50ms for 1000 entries
- Session startup time: < 500ms

### Usability
- Identical command syntax to non-interactive mode (zero learning curve)
- Visual feedback for long-running operations (> 2 seconds)
- Error messages displayed inline with suggestions
- No breaking changes to existing CLI behavior

### Reliability
- Graceful handling of Ctrl+C (prompt for confirmation if operation in progress)
- Session state auto-saved every 30 seconds
- Recovery from network errors without exiting session
- History corruption protection (validate format on load)

### Security
- Session tokens not displayed in history (redacted)
- History file permissions: 0600 (user-only read/write)
- Auto-logout after 60 minutes of inactivity (configurable)
- Clear sensitive data from memory on exit

---

## Out of Scope

- Scripting language features (variables, loops, conditionals) - use shell scripts instead
- Remote REPL access - use SSH + CLI
- Multi-user collaborative sessions
- Plugin/extension system for custom commands
- GUI or web-based console

---

## Clarifications

### Session 2025-12-10

- Q: What architecture should the REPL use for command processing and session management? → A: Option B - In-process with System.CommandLine + ReadLine.Refit for rich terminal features
- Q: How should the current context (org/register) be made available to commands that support context-aware defaults? → A: Option B - Command middleware injecting context as default parameter values
- Q: For tab completion of resource IDs (org IDs, register IDs, wallet addresses), where should the completion data come from? → A: Option B - Cache API responses in session with configurable TTL (5 min default), with dirty cache handling
- Q: When tab completion fetches data from the API and encounters an error (network failure, unauthorized, etc.), what should happen? → A: Option B - Silently fall back to command/flag completion only, log warning
- Q: How should multi-line input work for complex JSON payloads (e.g., transaction submit with large payload)? → A: Option C - Detect opening brace/bracket, enter multi-line mode until closing

---

## Open Questions

(To be addressed during planning phase)

## Acceptance Criteria

- All existing CLI commands work in interactive mode
- Command history persists across sessions
- Tab completion works for commands, subcommands, and common resource IDs
- Context management (`use org`, `use register`) functions correctly
- Prompt reflects current context
- Session exits gracefully
- Performance metrics met (< 100ms tab completion, < 500ms startup)
- Security requirements met (history permissions, token redaction)
