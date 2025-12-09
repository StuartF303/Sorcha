# Sorcha CLI - API Workflow Exerciser

A command-line tool to exercise the Sorcha API endpoints using test credentials against running Docker services.

## Features

- **Split-screen UI** - Shows workflow progress on the left and activity log on the right
- **Multiple workflows** - Health check, admin operations, and user operations
- **API logging** - All HTTP requests/responses are logged with timing
- **Test credentials** - Uses well-known test organization and users

## Prerequisites

- .NET 10 SDK
- Docker services running (see docker-compose.yml)

## Building

```bash
dotnet build src/Apps/Sorcha.Cli/Sorcha.Cli.csproj
```

## Running

### Start Docker services first

```bash
docker-compose up -d
```

### Run the CLI

```bash
# Run all workflows in interactive mode (default)
dotnet run --project src/Apps/Sorcha.Cli

# Run specific workflow
dotnet run --project src/Apps/Sorcha.Cli -- --workflow health
dotnet run --project src/Apps/Sorcha.Cli -- --workflow admin
dotnet run --project src/Apps/Sorcha.Cli -- --workflow user

# Run in non-interactive mode with verbose output
dotnet run --project src/Apps/Sorcha.Cli -- --interactive false --verbose

# Show help
dotnet run --project src/Apps/Sorcha.Cli -- --help
```

## Workflows

### Health Check (`--workflow health`)

Verifies all services are running and healthy:
- Blueprint Service (http://localhost:5000)
- Wallet Service (http://localhost:5001)
- Tenant Service (http://localhost:5110)
- Register Service (http://localhost:5290)
- Peer Service (http://localhost:5002)
- API Gateway (http://localhost:8080)

### Admin Workflow (`--workflow admin`)

Tests administrative operations:
1. Get test organization
2. Validate subdomain
3. List organization users
4. Get individual user details (Admin, Member, Auditor)
5. Verify current authenticated user
6. Create new test organization
7. Add new user to organization

### User Workflow (`--workflow user`)

Tests user operations:
1. List existing wallets
2. Create new ED25519 wallet
3. Get wallet details
4. Sign test data
5. List blueprints
6. Create simple blueprint
7. Get blueprint details
8. Publish blueprint
9. List registers
10. Create new register
11. Submit transaction
12. Query transactions
13. Cleanup test data

## Test Credentials

The CLI uses well-known test credentials:

| Entity | ID | Details |
|--------|-----|---------|
| Organization | `00000000-0000-0000-0000-000000000001` | Test Organization |
| Admin User | `00000000-0000-0000-0001-000000000001` | admin@test-org.sorcha.io |
| Member User | `00000000-0000-0000-0001-000000000002` | member@test-org.sorcha.io |
| Auditor User | `00000000-0000-0000-0001-000000000003` | auditor@test-org.sorcha.io |

## UI Layout

The CLI now features a **3-panel split-screen layout** with auto-scrolling and pause-after-step functionality:

```
┌─────────────────────┬─────────────────────┬──────────────────────────────────┐
│  Workflow Progress  │  Activity Log       │  Payload Detail                  │
│                     │                     │                                  │
│  Progress: 3/6      │  10:23:45.12        │  Response: 200 OK (5ms)          │
│  steps (50%)        │  >> GET /health     │  10:23:45.15                     │
│                     │                     │  GET http://localhost:5000/...   │
│  ✓ 1. Check BP Svc  │  10:23:45.15        │  Content-Type: application/json  │
│      (⏸ PAUSED)     │  << 200 5ms         │  ──────────────────────────────  │
│  ✓ 2. Check Wallet  │                     │  {                               │
│  ● 3. Check Tenant  │  10:23:45.20        │    "status": "Healthy",          │
│  ○ 4. Check Reg Svc │  i  Blueprint OK    │    "service": "Blueprint",       │
│  ○ 5. Check Peer    │                     │    "version": "1.0.0",           │
│  ○ 6. Check Gateway │  10:23:45.25        │    "dependencies": {             │
│                     │  >> GET /health     │      "redis": "Connected",       │
│                     │                     │      "database": "Healthy"       │
│                     │  10:23:45.28        │    }                             │
│                     │  << 200 3ms         │  }                               │
│                     │                     │                                  │
│                     │  10:23:45.30        │  ... (scroll for more)           │
│                     │  ✓ Wallet OK        │                                  │
└─────────────────────┴─────────────────────┴──────────────────────────────────┘
Press Ctrl+C to cancel | Space to pause | Panels auto-scroll
```

### New Features

**✅ 3-Panel Layout:**
- **Left Panel**: Workflow progress with step status indicators
- **Middle Panel**: Activity log showing request/response timeline
- **Right Panel**: Detailed payload viewer with JSON syntax highlighting

**✅ Pause After Each Step:**
- The CLI automatically pauses after each test completes
- Press any key to continue to the next step
- Gives you time to review the detailed payload data
- Pause indicator shows: `(⏸ PAUSED - Press any key to continue)`

**✅ Scrollable Panels:**
- All three panels support auto-scrolling when content exceeds window height
- Most recent activity and payloads are always visible
- Long JSON payloads are formatted and truncated with scroll indicators

**✅ JSON Syntax Highlighting:**
- Property names in cyan
- String values in green
- Numbers in yellow
- Booleans and null in magenta
- Auto-formatted with proper indentation

## Architecture

```
Sorcha.Cli/
├── Configuration/
│   └── TestCredentials.cs      # Well-known test IDs and URLs
├── Services/
│   ├── ApiClientBase.cs        # Base HTTP client functionality
│   ├── LoggingHttpHandler.cs   # HTTP logging middleware
│   ├── TenantApiClient.cs      # Tenant Service API client
│   ├── WalletApiClient.cs      # Wallet Service API client
│   ├── BlueprintApiClient.cs   # Blueprint Service API client
│   └── RegisterApiClient.cs    # Register Service API client
├── UI/
│   ├── ActivityLog.cs          # Thread-safe activity logging
│   ├── WorkflowProgress.cs     # Step progress tracking with pause support
│   ├── PayloadDetail.cs        # Detailed payload viewer with JSON highlighting
│   └── SplitScreenRenderer.cs  # 3-panel split-screen UI renderer
├── Workflows/
│   ├── IWorkflow.cs            # Workflow interface
│   ├── HealthCheckWorkflow.cs  # Service health checks
│   ├── AdminWorkflow.cs        # Admin operations
│   └── UserWorkflow.cs         # User operations
└── Program.cs                  # CLI entry point
```
