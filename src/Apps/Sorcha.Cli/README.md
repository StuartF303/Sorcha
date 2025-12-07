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

```
┌─────────────────────────────────────┬─────────────────────────────────────┐
│  Workflow Progress                  │  Activity Log                       │
│                                     │                                     │
│  Progress: 3/6 steps (50%)          │  10:23:45.12  >> GET /health       │
│                                     │  10:23:45.15  << 200 5ms           │
│  ✓ 1. Check Blueprint Service       │  10:23:45.20  i  Blueprint healthy │
│  ✓ 2. Check Wallet Service          │  10:23:45.25  >> GET /health       │
│  ● 3. Check Tenant Service          │  10:23:45.28  << 200 3ms           │
│  ○ 4. Check Register Service        │  10:23:45.30  ✓ Wallet healthy     │
│  ○ 5. Check Peer Service            │                                     │
│  ○ 6. Check API Gateway             │                                     │
│                                     │                                     │
└─────────────────────────────────────┴─────────────────────────────────────┘
Press Ctrl+C to cancel | Logs auto-scroll
```

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
│   ├── WorkflowProgress.cs     # Step progress tracking
│   └── SplitScreenRenderer.cs  # Split-screen UI renderer
├── Workflows/
│   ├── IWorkflow.cs            # Workflow interface
│   ├── HealthCheckWorkflow.cs  # Service health checks
│   ├── AdminWorkflow.cs        # Admin operations
│   └── UserWorkflow.cs         # User operations
└── Program.cs                  # CLI entry point
```
