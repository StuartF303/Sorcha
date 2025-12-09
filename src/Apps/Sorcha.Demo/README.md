# Sorcha.Demo - Blueprint Workflow Execution Demo

**Version:** 1.0.0
**Status:** Production-Ready
**License:** MIT

## Overview

Sorcha.Demo is a unified demonstration application that combines the capabilities of the previous Sorcha.Cli and Sorcha.Cli.Demo applications. It demonstrates end-to-end blueprint workflow execution using real Sorcha services (Wallet, Blueprint, Register) with multi-participant simulation.

## Features

- ✅ **Real Service Integration** - Works with live Wallet, Blueprint, and Register services
- ✅ **Multi-Participant Simulation** - Simulates workflows with multiple participants
- ✅ **Interactive & Automated Modes** - Step-by-step or automated execution
- ✅ **Wallet Management** - Create new wallets or reuse existing ones
- ✅ **Blueprint Loading** - Load from JSON files or Blueprint Service API
- ✅ **Orchestration API** - Uses Blueprint Service orchestration endpoints
- ✅ **Rich CLI UI** - Built with Spectre.Console for beautiful terminal output
- ✅ **JSON-e Templates** - Dynamic wallet address injection in blueprints

## Prerequisites

- .NET 10.0 SDK
- Sorcha services running (Wallet, Blueprint, Register)
- Access to API Gateway (default: https://localhost:7082)

## Installation

### Build from Source

```bash
cd src/Apps/Sorcha.Demo
dotnet build
dotnet run
```

### Run from Binary

```bash
cd src/Apps/Sorcha.Demo/bin/Debug/net10.0
dotnet Sorcha.Demo.dll
```

## Configuration

### appsettings.json

```json
{
  "SorchaApi": {
    "BaseUrl": "https://localhost:7082",
    "TimeoutSeconds": 30
  },
  "Demo": {
    "WalletStoragePath": "~/.sorcha/demo-wallets.json",
    "DefaultAlgorithm": "ED25519"
  }
}
```

### Environment Variables

All settings can be overridden with environment variables using the `SORCHA_` prefix:

```bash
# Override API base URL
export SORCHA_SorchaApi__BaseUrl="https://api.sorcha.example.com"

# Override individual service URLs
export SORCHA_SorchaApi__WalletServiceUrl="https://wallet.sorcha.example.com"
export SORCHA_SorchaApi__BlueprintServiceUrl="https://blueprint.sorcha.example.com"
export SORCHA_SorchaApi__RegisterServiceUrl="https://register.sorcha.example.com"

# Override wallet storage path
export SORCHA_Demo__WalletStoragePath="/custom/path/wallets.json"
```

## Usage

### Interactive Mode (Default)

Run the demo and follow the interactive menu:

```bash
dotnet run
```

**Main Menu:**
- 💰 Expense Approval Workflow
- 📦 Purchase Order Processing
- 🏦 Loan Application Process
- 📂 Load Custom Blueprint
- ⚙️  Settings
- 🚪 Exit

### Automated Mode

Run a specific blueprint without interactive prompts:

```bash
# Run expense approval workflow in automated mode
dotnet run -- --automated --blueprint expense-approval.json

# Run with short flags
dotnet run -- -a -b purchase-order.json
```

### Command-Line Options

```
Options:
  -a, --automated              Run in automated mode without pauses
  -b, --blueprint <blueprint>  Blueprint to run (expense-approval, purchase-order, loan-application)
  --version                    Show version information
  -?, -h, --help               Show help and usage information
```

## Blueprint Examples

The demo includes three pre-configured blueprint examples:

### 1. Expense Approval Workflow

**File:** `Examples/Blueprints/expense-approval.json`

**Participants:**
- Employee - Submits expense report
- Manager - Approves/rejects expenses < $5000
- CFO - Approves/rejects expenses ≥ $5000
- Finance - Processes approved expenses

**Workflow:**
1. Employee submits expense report with amount and description
2. Conditional routing based on amount:
   - < $5000 → Manager approval
   - ≥ $5000 → CFO approval
3. Finance processes approved expenses

### 2. Purchase Order Processing

**File:** `Examples/Blueprints/purchase-order.json`

**Participants:**
- Buyer - Creates purchase order
- Supplier - Confirms order
- Shipping - Updates shipping status
- Finance - Processes payment

**Workflow:**
1. Buyer creates PO with items and quantities
2. Supplier confirms availability and pricing
3. Shipping updates delivery status
4. Finance processes payment

### 3. Loan Application Process

**File:** `Examples/Blueprints/loan-application.json`

**Participants:**
- Applicant - Submits loan application
- Underwriter - Reviews application
- Manager - Final approval

**Workflow:**
1. Applicant submits loan details
2. Underwriter performs credit check
3. Manager approves or rejects

## Architecture

### Components

```
Sorcha.Demo
├── Configuration/           # Configuration models
│   ├── DemoAppConfiguration.cs
│   └── SorchaApiConfiguration.cs
├── Models/                  # Domain models
│   ├── DemoContext.cs      # State management
│   ├── ParticipantContext.cs
│   └── ActionExecutionResult.cs
├── Services/
│   ├── Api/                # API clients
│   │   ├── WalletApiClient.cs
│   │   ├── BlueprintApiClient.cs
│   │   └── RegisterApiClient.cs
│   ├── Blueprints/         # Blueprint loading
│   │   ├── JsonBlueprintLoader.cs
│   │   └── JsonETemplateEngine.cs
│   ├── Execution/          # Workflow execution
│   │   ├── ParticipantManager.cs
│   │   └── BlueprintFlowExecutor.cs
│   └── Storage/            # Local storage
│       └── WalletStorage.cs
├── UI/                     # User interface
│   └── DemoRenderer.cs
└── Program.cs              # Main entry point
```

### Key Flows

#### 1. Workflow Execution Flow

```
Program.cs
  ↓
LoadBlueprint → ExtractParticipants → EnsureWallets
  ↓                                         ↓
ProcessTemplate ← (JSON-e with wallet addresses)
  ↓
CreateInstance (Blueprint Service API)
  ↓
ExecuteActions (Loop through blueprint actions)
  ↓
  ├─ PromptForInput (Interactive) OR GenerateDefaults (Automated)
  ├─ ExecuteAction (Blueprint Service Orchestration Endpoint)
  │     └─ POST /api/instances/{id}/actions/{actionId}/execute
  └─ ShowResult
```

#### 2. Wallet Management Flow

```
ParticipantManager.EnsureParticipantWalletsAsync
  ↓
Check WalletStorage (reuse existing?)
  ↓─ Yes → LoadWalletsAsync
  ↓─ No  → CreateParticipantWalletAsync (for each participant)
              ↓
              WalletApiClient.CreateWalletAsync
              ↓
              SaveWalletsAsync (persist to JSON)
```

## Wallet Storage

### Format

Wallets are stored in JSON format at `~/.sorcha/demo-wallets.json`:

```json
{
  "Employee": {
    "participantId": "Employee",
    "name": "Employee",
    "walletAddress": "5x7m9...abc123",
    "algorithm": "ED25519",
    "mnemonic": "word1 word2 ... word24",
    "createdAt": "2025-12-09T12:34:56Z",
    "actionsExecuted": 3
  },
  "Manager": { ... }
}
```

⚠️ **WARNING:** Mnemonics are stored in **plain text** for demo purposes only. **DO NOT use this storage mechanism in production!**

### Wallet Reuse

When starting a demo, you'll be asked:

```
Existing wallets found. Reuse them? (Y/n)
```

- **Yes** - Reuse existing wallet addresses (recommended for consistent testing)
- **No** - Create new wallets (new addresses for each participant)

## Settings

Access settings from the main menu to customize demo behavior:

| Setting | Description | Default |
|---------|-------------|---------|
| **Step-by-Step Mode** | Pause after each action for review | ON |
| **Verbose Mode** | Show detailed API calls and responses | OFF |
| **Show Validation** | Display JSON Schema validation details | ON |
| **Show Calculations** | Display calculation details | ON |
| **Show Routing** | Show routing decision logic | ON |
| **Show Disclosure** | Show selective disclosure per participant | ON |

### Clear Wallets

From the settings menu, you can clear all stored wallets. This will:
- Delete `~/.sorcha/demo-wallets.json`
- Force creation of new wallets on next run

## Troubleshooting

### Connection Errors

**Problem:** `Unable to connect to Wallet Service`

**Solution:**
1. Ensure all Sorcha services are running
2. Check API Gateway is accessible at the configured URL
3. Verify environment variables if using custom endpoints

### Blueprint Loading Errors

**Problem:** `No participants found in blueprint`

**Solution:**
- Verify blueprint JSON has a `participants` array
- Check blueprint file exists in `Examples/Blueprints/`
- Validate JSON syntax with a JSON linter

### Wallet Creation Fails

**Problem:** `Failed to create wallet for participant: Employee`

**Solution:**
1. Check Wallet Service is running and healthy
2. Verify API Gateway routes to Wallet Service
3. Check logs for authentication/authorization errors

### Configuration Not Found

**Problem:** `The configuration file 'appsettings.json' was not found`

**Solution:**
- Run from the binary directory (where appsettings.json is copied)
- Or use `dotnet run --project` from the project directory

## Development

### Adding a New Blueprint

1. Create JSON file in `Examples/Blueprints/`
2. Define participants with IDs
3. Use JSON-e templates for wallet addresses:
   ```json
   "sender": "{{participants.Employee}}"
   ```
4. Add to main menu in `Program.cs`

### Extending API Clients

API clients inherit from `ApiClientBase`:

```csharp
public class CustomApiClient : ApiClientBase
{
    public CustomApiClient(HttpClient httpClient, ILogger<CustomApiClient> logger, string baseUrl)
        : base(httpClient, logger, baseUrl) { }

    public async Task<CustomResponse?> CustomMethodAsync(string id, CancellationToken ct = default)
    {
        return await GetAsync<CustomResponse>($"{_baseUrl}/custom/{id}", ct);
    }
}
```

## Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| **Microsoft.Extensions.Configuration** | 10.0.0 | Configuration management |
| **Microsoft.Extensions.DependencyInjection** | 10.0.0 | Dependency injection |
| **Microsoft.Extensions.Http** | 10.0.0 | HTTP client factory |
| **Microsoft.Extensions.Logging** | 10.0.0 | Logging infrastructure |
| **Spectre.Console** | 0.54.0 | Rich terminal UI |
| **System.CommandLine** | 2.0.0 | CLI argument parsing |
| **JsonE.Net** | 2.5.1 | JSON-e template processing |

### Project References

- `Sorcha.Blueprint.Models` - Blueprint domain models
- `Sorcha.Blueprint.Engine` - Portable execution engine
- `Sorcha.Cryptography` - Cryptographic operations
- `Sorcha.TransactionHandler` - Transaction building
- `Sorcha.Wallet.Core` - Wallet models

## Differences from Old Apps

### vs. Sorcha.Cli

- ✅ Retains real service integration
- ✅ Removes manual transaction signing (now handled by orchestration API)
- ✅ Adds blueprint-based workflow execution
- ✅ Adds multi-participant simulation

### vs. Sorcha.Cli.Demo

- ✅ Uses **real services** instead of in-memory simulation
- ✅ Uses **Blueprint Service orchestration API** for execution
- ✅ Real wallet creation via Wallet Service
- ✅ Real transaction submission to Register Service
- ✅ Production-ready architecture

## Security Considerations

### For Demo Use Only

⚠️ **This application is designed for demonstration purposes and includes security trade-offs:**

1. **Plaintext Mnemonic Storage**
   - Mnemonics stored in `demo-wallets.json` are **NOT encrypted**
   - Production systems should use Hardware Security Modules (HSM) or secure key stores

2. **Local Wallet Storage**
   - Wallets stored in local JSON file
   - No authentication/authorization on storage file
   - Production should use encrypted databases or cloud key management

3. **API Authentication**
   - Currently uses default API authentication
   - Production deployments should implement OAuth2/OIDC

### Production Recommendations

For production use, implement:
- Azure Key Vault or AWS KMS for key management
- Encrypted wallet storage
- OAuth2/OIDC authentication
- TLS/mTLS for service communication
- Audit logging for all wallet operations

## Contributing

See [CONTRIBUTING.md](../../../CONTRIBUTING.md) for development guidelines.

## License

This project is licensed under the MIT License - see [LICENSE](../../../LICENSE) for details.

## Support

- **Documentation:** [docs/](../../../docs/)
- **Issues:** [GitHub Issues](https://github.com/SorchaProject/Sorcha/issues)
- **Discussions:** [GitHub Discussions](https://github.com/SorchaProject/Sorcha/discussions)

---

**Built with ❤️ by the Sorcha Contributors**
