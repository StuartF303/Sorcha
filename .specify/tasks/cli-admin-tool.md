# CLI Admin Tool Implementation

**Goal:** Build cross-platform administrative CLI with interactive console and automation support
**Duration:** 8 weeks (4 sprints of 2 weeks each)
**Total Tasks:** 60 (12 + 13 + 12 + 15 + 8 across 5 sprints)
**Completion:** 0% (specification complete, implementation pending)
**Related Specification:** [sorcha-cli-admin-tool.md](../specs/sorcha-cli-admin-tool.md)

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)

---

## Key Features

- Interactive console mode (REPL) with history and tab completion
- Flag-based mode for scripts and AI agents
- Authentication caching with OS-specific encryption
- Tenant, Register, and Peer service integration
- Multi-environment profile support
- Cross-platform (Windows, macOS, Linux)

---

## Sprint 1: Foundation & Infrastructure (Weeks 1-2)

**Goal:** Project structure, configuration management, authentication, and token caching

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-1.1 | Create Sorcha.Cli project structure | P0 | 2h | 📋 Not Started | - |
| CLI-1.2 | Configure System.CommandLine framework | P0 | 4h | 📋 Not Started | - |
| CLI-1.3 | Implement configuration management (profiles) | P0 | 8h | 📋 Not Started | - |
| CLI-1.4 | Implement TokenCache with OS-specific encryption | P0 | 12h | 📋 Not Started | - |
| CLI-1.5 | Implement WindowsDpapiEncryption provider | P0 | 4h | 📋 Not Started | - |
| CLI-1.6 | Implement MacOsKeychainEncryption provider | P0 | 4h | 📋 Not Started | - |
| CLI-1.7 | Implement LinuxSecretServiceEncryption provider | P0 | 4h | 📋 Not Started | - |
| CLI-1.8 | Implement AuthenticationService with caching | P0 | 8h | 📋 Not Started | - |
| CLI-1.9 | Create base Command classes and routing | P0 | 6h | 📋 Not Started | - |
| CLI-1.10 | Implement global options (--profile, --output, --quiet) | P1 | 4h | 📋 Not Started | - |
| CLI-1.11 | Implement exit code standards (0-8) | P1 | 2h | 📋 Not Started | - |
| CLI-1.12 | Unit tests for configuration and auth services | P1 | 6h | 📋 Not Started | - |

**Sprint 1 Total:** 12 tasks, 64 hours

**Deliverables:**
- CLI project compiles and installs as global tool (`dotnet tool install -g`)
- Configuration profiles work (create, switch, list)
- Authentication service with token caching functional
- OS-specific encryption providers implemented
- Base command framework ready

---

## Sprint 2: Tenant Service Commands (Weeks 3-4)

**Goal:** Organization, user, and service principal management commands

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-2.1 | Create Refit ITenantServiceClient interface | P0 | 4h | 📋 Not Started | - |
| CLI-2.2 | Configure HTTP client with Polly resilience policies | P0 | 4h | 📋 Not Started | - |
| CLI-2.3 | Implement `sorcha org` commands (list, get, create, update, delete) | P0 | 8h | 📋 Not Started | - |
| CLI-2.4 | Implement `sorcha user` commands (list, get, create, update, delete) | P0 | 8h | 📋 Not Started | - |
| CLI-2.5 | Implement `sorcha principal` commands (list, get, create, delete) | P0 | 6h | 📋 Not Started | - |
| CLI-2.6 | Implement `sorcha principal rotate-secret` command | P1 | 4h | 📋 Not Started | - |
| CLI-2.7 | Implement `sorcha auth login` (user + service) | P0 | 6h | 📋 Not Started | - |
| CLI-2.8 | Implement `sorcha auth logout` and token management | P0 | 4h | 📋 Not Started | - |
| CLI-2.9 | Implement table output formatter (Spectre.Console) | P0 | 6h | 📋 Not Started | - |
| CLI-2.10 | Implement JSON output formatter | P0 | 3h | 📋 Not Started | - |
| CLI-2.11 | Implement CSV output formatter | P1 | 3h | 📋 Not Started | - |
| CLI-2.12 | Unit tests for Tenant Service commands | P1 | 8h | 📋 Not Started | - |
| CLI-2.13 | Integration tests with mock Tenant Service | P1 | 6h | 📋 Not Started | - |

**Sprint 2 Total:** 13 tasks, 70 hours

**Deliverables:**
- All Tenant Service commands functional
- Organization, user, and service principal CRUD
- Authentication (login/logout) working
- Multiple output formats (table, JSON, CSV)
- Unit and integration tests passing

---

## Sprint 3: Register & Transaction Commands (Weeks 5-6)

**Goal:** Register management and transaction viewing/search

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-3.1 | Create Refit IRegisterServiceClient interface | P0 | 4h | 📋 Not Started | - |
| CLI-3.2 | Implement `sorcha register` commands (list, get, create, update, delete) | P0 | 6h | 📋 Not Started | - |
| CLI-3.3 | Implement `sorcha register stats` command | P1 | 4h | 📋 Not Started | - |
| CLI-3.4 | Implement `sorcha tx list` command with pagination | P0 | 6h | 📋 Not Started | - |
| CLI-3.5 | Implement `sorcha tx get` command with payload display | P0 | 4h | 📋 Not Started | - |
| CLI-3.6 | Implement `sorcha tx search` command (query by blueprint, action, etc.) | P1 | 6h | 📋 Not Started | - |
| CLI-3.7 | Implement `sorcha tx verify` command (signatures + chain) | P2 | 6h | 📋 Not Started | - |
| CLI-3.8 | Implement `sorcha tx export` command (JSON/CSV/Excel) | P2 | 6h | 📋 Not Started | - |
| CLI-3.9 | Implement `sorcha tx timeline` command | P2 | 4h | 📋 Not Started | - |
| CLI-3.10 | Add pagination support for list commands | P1 | 4h | 📋 Not Started | - |
| CLI-3.11 | Unit tests for Register and Transaction commands | P1 | 6h | 📋 Not Started | - |
| CLI-3.12 | Integration tests with mock Register Service | P1 | 6h | 📋 Not Started | - |

**Sprint 3 Total:** 12 tasks, 62 hours

**Deliverables:**
- Register CRUD commands functional
- Transaction viewer (list, get, search)
- Transaction verification and export (P2 features)
- Pagination support
- Integration tests passing

---

## Sprint 4: Peer Service, Interactive Mode & Polish (Weeks 7-8)

**Goal:** Peer monitoring, interactive console (REPL), and final polish

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-4.1 | Create Refit IPeerServiceClient interface | P0 | 3h | 📋 Not Started | - |
| CLI-4.2 | Implement `sorcha peer list` command | P0 | 4h | 📋 Not Started | - |
| CLI-4.3 | Implement `sorcha peer get` command with metrics | P0 | 4h | 📋 Not Started | - |
| CLI-4.4 | Implement `sorcha peer topology` command (tree/graph) | P1 | 6h | 📋 Not Started | - |
| CLI-4.5 | Implement `sorcha peer health` command | P1 | 4h | 📋 Not Started | - |
| CLI-4.6 | Implement interactive console mode (ConsoleHost) | P1 | 12h | 📋 Not Started | - |
| CLI-4.7 | Implement command history (CommandHistory class) | P1 | 4h | 📋 Not Started | - |
| CLI-4.8 | Implement tab completion (TabCompleter class) | P1 | 8h | 📋 Not Started | - |
| CLI-4.9 | Implement context awareness (ConsoleContext) | P1 | 4h | 📋 Not Started | - |
| CLI-4.10 | Implement special console commands (help, clear, status, use, exit) | P1 | 4h | 📋 Not Started | - |
| CLI-4.11 | Implement audit logging to ~/.sorcha/audit.log | P1 | 4h | 📋 Not Started | - |
| CLI-4.12 | Add comprehensive error handling and user-friendly messages | P1 | 6h | 📋 Not Started | - |
| CLI-4.13 | Write user documentation (README, command reference) | P1 | 8h | 📋 Not Started | - |
| CLI-4.14 | Package as .NET global tool and publish to NuGet | P0 | 4h | 📋 Not Started | - |
| CLI-4.15 | E2E testing on Windows, macOS, and Linux | P1 | 8h | 📋 Not Started | - |

**Sprint 4 Total:** 15 tasks, 83 hours

**Deliverables:**
- Peer monitoring commands functional
- Interactive console mode (REPL) fully working
- Command history and tab completion
- Context-aware prompts
- Audit logging
- Published to NuGet as global tool
- Cross-platform testing complete
- User documentation complete

---

## Sprint 5: Bootstrap Automation (Weeks 9-10)

**Goal:** Bootstrap commands for automated platform setup

**Background:** Bootstrap scripts have been created (`scripts/bootstrap-sorcha.ps1` and `scripts/bootstrap-sorcha.sh`) that guide users through initial Sorcha installation setup. These scripts currently use placeholder commands and require the following CLI enhancements to be fully functional.

**Related Files:**
- `scripts/bootstrap-sorcha.ps1` (PowerShell bootstrap script)
- `scripts/bootstrap-sorcha.sh` (Bash bootstrap script)
- `scripts/README-BOOTSTRAP.md` (Bootstrap documentation)

| ID | Task | Priority | Effort | Status | Assignee |
|----|------|----------|--------|--------|----------|
| CLI-BOOTSTRAP-001 | Implement `sorcha config init` command | P0 | 6h | 📋 Not Started | - |
| CLI-BOOTSTRAP-002 | Implement `sorcha org create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-003 | Implement `sorcha user create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-004 | Implement `sorcha sp create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-005 | Implement `sorcha register create` command | P0 | 4h | 📋 Not Started | - |
| CLI-BOOTSTRAP-006 | Implement `sorcha node configure` command (NEW) | P1 | 6h | 📋 Not Started | - |
| TENANT-SERVICE-001 | Implement bootstrap API endpoint | P1 | 8h | ✅ Complete | 2026-01-01 |
| PEER-SERVICE-001 | Implement node configuration API | P1 | 6h | 📋 Not Started | - |

**Sprint 5 Total:** 8 tasks, 42 hours

### Task Details

#### CLI-BOOTSTRAP-001: Implement `sorcha config init`
**Purpose:** Initialize CLI configuration profile with service URLs

**Command:**
```bash
sorcha config init \
  --profile docker \
  --tenant-url http://localhost/api/tenants \
  --register-url http://localhost/api/register \
  --wallet-url http://localhost/api/wallets \
  --peer-url http://localhost/api/peers \
  --auth-url http://localhost/api/service-auth/token
```

#### CLI-BOOTSTRAP-002: Implement `sorcha org create`
**Purpose:** Create organization with subdomain

**Command:**
```bash
sorcha org create \
  --name "System Organization" \
  --subdomain "system" \
  --description "Primary system organization"
```

#### CLI-BOOTSTRAP-003: Implement `sorcha user create`
**Purpose:** Create user in organization with role

**Command:**
```bash
sorcha user create \
  --org-id <guid> \
  --email admin@sorcha.local \
  --name "System Administrator" \
  --password <secure> \
  --role Administrator
```

#### CLI-BOOTSTRAP-004: Implement `sorcha sp create`
**Purpose:** Create service principal with scopes

**Command:**
```bash
sorcha sp create \
  --name "sorcha-bootstrap" \
  --scopes "all" \
  --description "Bootstrap automation principal"
```

#### CLI-BOOTSTRAP-005: Implement `sorcha register create`
**Purpose:** Create register in organization

**Command:**
```bash
sorcha register create \
  --name "System Register" \
  --org-id <guid> \
  --description "Primary system register" \
  --publish
```

#### CLI-BOOTSTRAP-006: Implement `sorcha node configure`
**Purpose:** Configure P2P node identity and settings

**Command:**
```bash
sorcha node configure \
  --node-id "node-hostname" \
  --description "Primary Sorcha node" \
  --enable-p2p true \
  --public-address <optional>
```

**Deliverables:**
- Bootstrap commands fully functional
- Bootstrap scripts work end-to-end
- Atomic bootstrap API endpoint
- Node configuration API
- Updated bootstrap script documentation

---

## CLI Implementation Summary

**Total Effort:** 321 hours (~8 weeks of full-time work)

**Sprint Breakdown:**
- Sprint 1 (Foundation): 12 tasks, 64 hours
- Sprint 2 (Tenant): 13 tasks, 70 hours
- Sprint 3 (Register): 12 tasks, 62 hours
- Sprint 4 (Peer + REPL): 15 tasks, 83 hours
- Sprint 5 (Bootstrap): 8 tasks, 42 hours

**Priority Distribution:**
- P0 (Critical): 28 tasks (CLI must work for administration)
- P1 (High): 22 tasks (REPL, advanced features)
- P2 (Medium): 3 tasks (Nice-to-have features)

**Testing Coverage:**
- Unit tests: CLI-1.12, CLI-2.12, CLI-3.11 (20 hours)
- Integration tests: CLI-2.13, CLI-3.12 (12 hours)
- E2E tests: CLI-4.15 (8 hours)
- **Total testing effort:** 40 hours (12% of total)

**Dependencies:**
- Sprint 1 must complete before Sprint 2 (auth and config required)
- Sprint 2 must complete before Sprint 3 (HTTP client framework)
- Sprint 4 can partially overlap with Sprint 3 (Peer + REPL independent)

**Success Criteria:**
- ✅ Install as global tool: `dotnet tool install -g sorcha.cli`
- ✅ Authenticate and cache tokens across commands
- ✅ Manage organizations, users, service principals
- ✅ View registers and transactions
- ✅ Monitor peer network
- ✅ Interactive console mode with history and completion
- ✅ Script-friendly with JSON output and exit codes

---

**Back to:** [MASTER-TASKS.md](../MASTER-TASKS.md)
