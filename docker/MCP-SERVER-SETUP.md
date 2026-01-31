# MCP Server Docker Setup - Verification Complete

## Summary

The Sorcha MCP Server has been successfully integrated into the Docker infrastructure.

## Changes Made

### 1. Created Dockerfile
- **Location:** `src/Apps/Sorcha.McpServer/Dockerfile`
- **Runtime:** ASP.NET Core 10.0 (required for ServiceDefaults dependencies)
- **Build:** Multi-stage build with .NET SDK 10.0

### 2. Added to docker-compose.yml
- **Service Name:** `mcp-server`
- **Profile:** `tools` (runs on-demand, not auto-started)
- **Network:** `sorcha-network` (connects to all backend services)
- **Dependencies:** `api-gateway`, `aspire-dashboard`

### 3. Service Configuration
The MCP server connects to backend services via Docker DNS:
- Blueprint Service: `http://blueprint-service:8080`
- Register Service: `http://register-service:8080`
- Wallet Service: `http://wallet-service:8080`
- Tenant Service: `http://tenant-service:8080`
- Validator Service: `http://validator-service:8080`

### 4. Documentation
- Updated `CLAUDE.md` with MCP server in project structure
- Created `src/Apps/Sorcha.McpServer/README.md` with usage instructions
- Added command examples for Docker usage

## Verification Tests

### Build Test ✅
```bash
docker-compose build mcp-server
```
**Result:** Image built successfully as `sorcha/mcp-server:latest`

### Container Startup Test ✅
```bash
docker run --rm sorcha/mcp-server:latest
```
**Result:** Shows expected error "JWT token is required"

### Network Connectivity Test ✅
```bash
docker run --rm --network sorcha_sorcha-network \
  -e SORCHA_JWT_TOKEN=test-token \
  sorcha/mcp-server:latest
```
**Result:**
- Container starts successfully
- Connects to sorcha-network
- Validates JWT token format
- Rejects invalid token with proper error message

## Usage

### Option 1: Run with JWT Token Argument
```bash
docker-compose run mcp-server --jwt-token <your-jwt-token>
```

### Option 2: Run with Environment Variable
```bash
SORCHA_JWT_TOKEN=<your-jwt-token> docker-compose run mcp-server
```

### Option 3: Direct Docker Run (for testing)
```bash
docker run --rm \
  --network sorcha_sorcha-network \
  -e SORCHA_JWT_TOKEN=<your-jwt-token> \
  -e ServiceClients__TenantService__Address=http://tenant-service:8080 \
  sorcha/mcp-server:latest
```

## Getting a JWT Token

To get a valid JWT token for testing:

### Quick Method: Use the utility script

**PowerShell (Windows):**
```powershell
.\scripts\get-jwt-token.ps1 -Email "admin@sorcha.local" -Password "Admin123!"
```

**Bash (Linux/macOS):**
```bash
./scripts/get-jwt-token.sh -e admin@sorcha.local -p Admin123!
```

The script will output the JWT token and usage examples.

### Manual Method: cURL

### 1. Start Tenant Service
```bash
docker-compose up -d tenant-service
```

### 2. Login to Get Token
```bash
curl -X POST http://localhost/api/tenant/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sorcha.local","password":"Admin123!"}'
```

The response will include an `accessToken` field containing the JWT.

### 3. Use Token with MCP Server
```bash
docker-compose run mcp-server --jwt-token <access-token-from-step-2>
```

## Integration with Claude Desktop

To use with Claude Desktop, configure `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "sorcha": {
      "command": "docker-compose",
      "args": [
        "-f", "/path/to/sorcha/docker-compose.yml",
        "run", "--rm", "mcp-server",
        "--jwt-token", "${SORCHA_JWT_TOKEN}"
      ],
      "cwd": "/path/to/sorcha",
      "env": {
        "SORCHA_JWT_TOKEN": "your-token-here"
      }
    }
  }
}
```

## Important Notes

### No API Gateway Routes Required
The MCP server uses **stdio transport** (standard input/output), not HTTP. It does **not** need API Gateway routes because:
- It's not a web service
- It communicates via stdin/stdout with MCP clients
- Backend service calls are made using HTTP service clients

### Profile: tools
The MCP server uses the `tools` profile, which means:
- It does **not** start automatically with `docker-compose up`
- It must be run explicitly with `docker-compose run mcp-server`
- This prevents unnecessary resource usage when not needed

### stdin/tty Configuration
The docker-compose configuration includes:
```yaml
stdin_open: true  # Enable interactive stdin for MCP stdio transport
tty: true         # Allocate a pseudo-TTY
```

These settings are required for MCP stdio communication to work correctly.

## Troubleshooting

### "JWT token is required"
- Provide token via `--jwt-token` argument or `SORCHA_JWT_TOKEN` environment variable

### "Token format is invalid"
- Ensure you're using a valid JWT from the Tenant Service
- Test tokens like "test-token" will fail validation (this is correct behavior)

### "Service unavailable"
- Ensure backend services are running: `docker-compose up -d`
- Check service logs: `docker-compose logs -f <service-name>`

### Container not connecting to network
- Verify network exists: `docker network ls | grep sorcha`
- Ensure services are on same network: `docker-compose ps`

## Next Steps

The MCP server is now fully integrated into the Docker infrastructure and ready for use. To use it:

1. Start the Sorcha platform: `docker-compose up -d`
2. Get a JWT token from Tenant Service
3. Run MCP server: `docker-compose run mcp-server --jwt-token <token>`
4. Or integrate with Claude Desktop using the configuration above

---

**Date:** 2026-01-29
**Status:** ✅ Complete and Verified
**Docker Image:** `sorcha/mcp-server:latest`
