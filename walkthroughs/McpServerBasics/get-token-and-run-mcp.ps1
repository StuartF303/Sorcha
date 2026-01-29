# Sorcha MCP Server - Get Token and Run
# This script starts Sorcha services, authenticates, and runs the MCP Server

param(
    [switch]$AutoRun,  # Skip prompts and run automatically (for Claude Desktop integration)
    [switch]$SkipStartup  # Skip docker-compose up (if services already running)
)

$ErrorActionPreference = "Stop"

# Configuration
$TENANT_SERVICE_URL = "http://localhost:5450"
$LOGIN_ENDPOINT = "$TENANT_SERVICE_URL/api/auth/login"
$DEFAULT_EMAIL = "admin@sorcha.local"
$DEFAULT_PASSWORD = "Dev_Pass_2025!"
$STARTUP_WAIT_SECONDS = 30

# Colors for output
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "→ $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Step { param($Message) Write-Host "`n=== $Message ===" -ForegroundColor Magenta }

# Banner
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         Sorcha MCP Server - Authentication & Launch       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Step "Checking Prerequisites"
Write-Info "Verifying Docker Desktop is running..."

try {
    $null = docker ps 2>&1
    Write-Success "Docker is running"
} catch {
    Write-Error "Docker Desktop is not running or not installed"
    Write-Info "Please start Docker Desktop and try again"
    exit 1
}

# Start services if not skipped
if (-not $SkipStartup) {
    Write-Step "Starting Sorcha Services"
    Write-Info "Running: docker-compose up -d"
    Write-Warning "This may take a few minutes on first run (downloading images)..."

    try {
        docker-compose up -d
        Write-Success "Services started"

        Write-Info "Waiting $STARTUP_WAIT_SECONDS seconds for services to initialize..."
        Start-Sleep -Seconds $STARTUP_WAIT_SECONDS

    } catch {
        Write-Error "Failed to start services: $_"
        exit 1
    }
} else {
    Write-Step "Skipping Service Startup"
    Write-Info "Assuming services are already running"
}

# Check service health
Write-Step "Verifying Service Health"
Write-Info "Checking tenant-service status..."

$healthCheckAttempts = 0
$maxHealthCheckAttempts = 5
$healthCheckOk = $false

while (-not $healthCheckOk -and $healthCheckAttempts -lt $maxHealthCheckAttempts) {
    try {
        $healthCheckAttempts++
        $response = Invoke-WebRequest -Uri "$TENANT_SERVICE_URL/health" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $healthCheckOk = $true
            Write-Success "Tenant service is healthy"
        }
    } catch {
        Write-Warning "Health check attempt $healthCheckAttempts failed, retrying in 5 seconds..."
        Start-Sleep -Seconds 5
    }
}

if (-not $healthCheckOk) {
    Write-Error "Tenant service is not responding after $maxHealthCheckAttempts attempts"
    Write-Info "Check service logs: docker-compose logs -f tenant-service"
    exit 1
}

# Authenticate and get JWT token
Write-Step "Authenticating with Tenant Service"
Write-Info "Logging in as: $DEFAULT_EMAIL"

$loginBody = @{
    email = $DEFAULT_EMAIL
    password = $DEFAULT_PASSWORD
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri $LOGIN_ENDPOINT `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody

    $token = $response\.access_token

    if ([string]::IsNullOrWhiteSpace($token)) {
        Write-Error "Login succeeded but no access token received"
        exit 1
    }

    Write-Success "Authentication successful"
    Write-Info "Token received (length: $($token.Length) characters)"

    # Display token info (first/last 20 chars for security)
    $tokenPreview = $token.Substring(0, [Math]::Min(20, $token.Length)) + "..." +
                    $token.Substring([Math]::Max(0, $token.Length - 20))
    Write-Host ""
    Write-Host "  Token Preview: $tokenPreview" -ForegroundColor Gray

    # Parse token to show roles (basic JWT parsing)
    try {
        $tokenParts = $token.Split('.')
        if ($tokenParts.Length -eq 3) {
            $payload = $tokenParts[1]
            # Add padding if needed
            while ($payload.Length % 4 -ne 0) { $payload += "=" }
            $payloadJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
            $claims = $payloadJson | ConvertFrom-Json

            Write-Host ""
            Write-Host "  User: $($claims.email)" -ForegroundColor Gray
            Write-Host "  Organization: $($claims.organizationName)" -ForegroundColor Gray
            Write-Host "  Roles: $($claims.roles -join ', ')" -ForegroundColor Gray
        }
    } catch {
        # Ignore JWT parsing errors - token might be encrypted or use different format
    }

} catch {
    Write-Error "Authentication failed: $($_.Exception.Message)"
    Write-Info "Verify credentials and tenant service is running"
    Write-Info "Check logs: docker-compose logs -f tenant-service"
    exit 1
}

# Run MCP Server
Write-Step "Launching MCP Server"
Write-Info "Starting MCP server with JWT authentication..."
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  MCP Server is starting...                                 ║" -ForegroundColor Green
Write-Host "║  Press Ctrl+C to stop                                      ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

try {
    # Save token to environment variable for the docker-compose process
    $env:SORCHA_JWT_TOKEN = $token

    # Run MCP server
    docker-compose run --rm mcp-server --jwt-token $token

} catch {
    Write-Error "Failed to start MCP server: $_"
    exit 1
} finally {
    # Clean up environment variable
    Remove-Item Env:SORCHA_JWT_TOKEN -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Success "MCP Server session ended"
Write-Host ""
