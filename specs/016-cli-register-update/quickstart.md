# Quickstart: CLI Register Commands Update

**Branch**: `016-cli-register-update` | **Date**: 2026-01-28

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop running with all services (`docker-compose up -d`)
- CLI authenticated (`sorcha auth login`)

## Build

```bash
dotnet build src/Apps/Sorcha.Cli/Sorcha.Cli.csproj
```

## Verify Shared Model Migration

```bash
# Should compile without errors after adding project references
dotnet build src/Apps/Sorcha.Cli/Sorcha.Cli.csproj

# Run existing register commands to verify backward compat
sorcha register list
sorcha register get --id <any-register-id>
```

## Test Two-Phase Register Creation

```bash
# 1. Ensure you have a wallet
sorcha wallet list

# 2. Create a register (two-phase flow happens automatically)
sorcha register create --name "Test Register" --tenant-id <tenantId> --owner-wallet <walletAddress>

# Expected output:
# ✓ Register created successfully!
#   Register ID:       <32-char hex>
#   Genesis TX ID:     <64-char hex>
#   Genesis Docket ID: 0
```

## Test New Commands

```bash
# Docket inspection
sorcha docket list --register-id <registerId>
sorcha docket get --register-id <registerId> --docket-id 0
sorcha docket transactions --register-id <registerId> --docket-id 0

# Query API
sorcha query wallet --address <walletAddress>
sorcha query sender --address <senderAddress>
sorcha query blueprint --id <blueprintId>
sorcha query stats

# OData query
sorcha query odata --resource Transactions --filter "RegisterId eq '<registerId>'" --top 10

# Register management
sorcha register update --id <registerId> --name "Updated Name"
sorcha register stats

# JSON output
sorcha register list --output json
sorcha docket list --register-id <registerId> --output json
```

## Test Matrix

| Command | Table Output | JSON Output | Error Cases |
|---------|-------------|-------------|-------------|
| `register create` | Verify fields | Verify JSON | Bad wallet, expired, no auth |
| `register update` | Verify updated fields | Verify JSON | Not found, no auth |
| `register stats` | Verify count | Verify JSON | No auth |
| `docket list` | Verify table | Verify JSON | No register, no auth |
| `docket get` | Verify details | Verify JSON | Not found |
| `docket transactions` | Verify tx list | Verify JSON | Empty docket |
| `query wallet` | Verify pagination | Verify JSON | No results |
| `query sender` | Verify pagination | Verify JSON | No results |
| `query blueprint` | Verify pagination | Verify JSON | No results |
| `query stats` | Verify stats | Verify JSON | No auth |
| `query odata` | Verify results | Verify JSON | Bad filter syntax |
