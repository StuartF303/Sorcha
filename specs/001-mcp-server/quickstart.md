# Quickstart: Sorcha MCP Server

**Feature**: 001-mcp-server
**Date**: 2026-01-29

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop running (for backend services)
- Sorcha platform running (`docker-compose up -d`)
- Valid JWT token from Sorcha Tenant Service

## Installation

### 1. Build the MCP Server

```bash
cd c:\projects\Sorcha
dotnet build src/Apps/Sorcha.McpServer
```

### 2. Configure the Server

Create or edit `src/Apps/Sorcha.McpServer/appsettings.json`:

```json
{
  "ServiceClients": {
    "BlueprintService": { "Address": "http://localhost:5000" },
    "RegisterService": { "Address": "http://localhost:5290" },
    "WalletService": { "Address": "http://localhost:5001" },
    "TenantService": { "Address": "http://localhost:5110" },
    "ValidatorService": { "Address": "http://localhost:5004" },
    "PeerService": { "Address": "http://localhost:5002", "UseGrpc": true }
  },
  "RateLimiting": {
    "PerUserRequestsPerMinute": 100,
    "PerTenantRequestsPerMinute": 1000,
    "AdminToolsRequestsPerMinute": 50
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Running the Server

### Option 1: Stdio Transport (Local AI Assistant)

```bash
# Run with JWT token
dotnet run --project src/Apps/Sorcha.McpServer -- --jwt-token "eyJhbG..."

# Or set token via environment variable
export SORCHA_JWT_TOKEN="eyJhbG..."
dotnet run --project src/Apps/Sorcha.McpServer
```

### Option 2: With Claude Desktop

Add to Claude Desktop MCP configuration (`~/.config/claude/mcp.json` or similar):

```json
{
  "mcpServers": {
    "sorcha": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "c:/projects/Sorcha/src/Apps/Sorcha.McpServer",
        "--",
        "--jwt-token",
        "<YOUR_JWT_TOKEN>"
      ]
    }
  }
}
```

### Option 3: With Claude Code

Add to Claude Code settings:

```json
{
  "mcpServers": {
    "sorcha": {
      "command": "dotnet",
      "args": ["run", "--project", "c:/projects/Sorcha/src/Apps/Sorcha.McpServer"],
      "env": {
        "SORCHA_JWT_TOKEN": "<YOUR_JWT_TOKEN>"
      }
    }
  }
}
```

## Getting a JWT Token

### Via CLI

```bash
# Login as user
dotnet run --project src/Apps/Sorcha.Cli -- auth login

# Login as service principal
dotnet run --project src/Apps/Sorcha.Cli -- auth login --client-id my-app

# Get current token
dotnet run --project src/Apps/Sorcha.Cli -- auth status --show-token
```

### Via API

```bash
curl -X POST http://localhost:5110/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username": "admin@example.com", "password": "...", "grant_type": "password"}'
```

## Verifying the Setup

Once the MCP server is connected to your AI assistant, try these commands:

### Administrator (requires `sorcha:admin` role)

```
"Check the health of all Sorcha services"
→ Uses sorcha_health_check tool

"Show me the last 10 error logs from the Blueprint service"
→ Uses sorcha_log_query tool

"List all tenants"
→ Uses sorcha_tenant_list tool
```

### Designer (requires `sorcha:designer` role)

```
"List all published blueprints"
→ Uses sorcha_blueprint_list tool

"Validate this blueprint: { ... }"
→ Uses sorcha_blueprint_validate tool

"Analyze the disclosure rules for blueprint BP-001"
→ Uses sorcha_disclosure_analyze tool
```

### Participant (requires `sorcha:participant` role)

```
"What actions are waiting for me?"
→ Uses sorcha_inbox_list tool

"Show me the details of action ACT-123"
→ Uses sorcha_action_details tool

"Submit this response: { ... }"
→ Uses sorcha_action_submit tool
```

## Available Tools by Persona

### Administrator Tools (10)
| Tool | Description |
|------|-------------|
| `sorcha_health_check` | Check health of all services |
| `sorcha_log_query` | Query service logs |
| `sorcha_metrics` | Get performance metrics |
| `sorcha_tenant_list` | List tenants |
| `sorcha_tenant_create` | Create tenant |
| `sorcha_tenant_update` | Update tenant |
| `sorcha_user_list` | List users |
| `sorcha_user_manage` | Manage user roles |
| `sorcha_peer_status` | View peer network |
| `sorcha_validator_status` | Check consensus |
| `sorcha_register_stats` | Register statistics |
| `sorcha_audit_query` | Query audit logs |
| `sorcha_token_revoke` | Revoke tokens |

### Designer Tools (13)
| Tool | Description |
|------|-------------|
| `sorcha_blueprint_list` | List blueprints |
| `sorcha_blueprint_get` | Get blueprint |
| `sorcha_blueprint_create` | Create blueprint |
| `sorcha_blueprint_update` | Update blueprint |
| `sorcha_blueprint_validate` | Validate blueprint |
| `sorcha_blueprint_simulate` | Simulate execution |
| `sorcha_disclosure_analyze` | Analyze disclosures |
| `sorcha_blueprint_diff` | Compare versions |
| `sorcha_blueprint_export` | Export blueprint |
| `sorcha_schema_validate` | Validate JSON Schema |
| `sorcha_schema_generate` | Generate schema |
| `sorcha_jsonlogic_test` | Test JSON Logic |
| `sorcha_workflow_instances` | List instances |

### Participant Tools (10)
| Tool | Description |
|------|-------------|
| `sorcha_inbox_list` | List pending actions |
| `sorcha_action_details` | Get action details |
| `sorcha_action_submit` | Submit action data |
| `sorcha_action_validate` | Validate before submit |
| `sorcha_transaction_history` | View history |
| `sorcha_workflow_status` | Check workflow status |
| `sorcha_disclosed_data` | View disclosed data |
| `sorcha_wallet_info` | Get wallet info |
| `sorcha_wallet_sign` | Sign message |
| `sorcha_register_query` | Query registers |

## Available Resources

| URI | Description | Roles |
|-----|-------------|-------|
| `sorcha://blueprints` | List all blueprints | designer, admin |
| `sorcha://blueprints/{id}` | Blueprint definition | designer, admin |
| `sorcha://inbox` | Pending actions | participant |
| `sorcha://workflows/{id}` | Workflow status | all |
| `sorcha://registers/{id}` | Register data | participant |
| `sorcha://schemas/{name}` | JSON Schemas | designer |

## Troubleshooting

### "Authentication required" error
- Verify JWT token is valid and not expired
- Check token has required role claims

### "Service unavailable" error
- Ensure Docker containers are running: `docker-compose ps`
- Check service health: `docker-compose logs <service>`

### "Rate limit exceeded" error
- Wait for rate limit window to reset (1 minute)
- Contact admin to adjust limits if needed

### Tools not appearing
- Verify JWT has correct role claim for the tool category
- Admin tools require `sorcha:admin` claim
- Designer tools require `sorcha:designer` claim
- Participant tools require `sorcha:participant` claim

## Development

### Running Tests

```bash
dotnet test tests/Sorcha.McpServer.Tests
```

### Enabling Debug Logging

```bash
dotnet run --project src/Apps/Sorcha.McpServer -- --jwt-token "..." --verbose
```

### Building for Production

```bash
dotnet publish src/Apps/Sorcha.McpServer -c Release -o ./publish
```
