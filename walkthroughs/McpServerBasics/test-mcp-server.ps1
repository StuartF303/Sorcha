# Quick Test Script for MCP Server Setup
# This script verifies that the MCP server can start and connect to services

$ErrorActionPreference = "Stop"

Write-Host "`n=== Sorcha MCP Server Quick Test ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Docker running
Write-Host "Test 1: Docker Desktop..." -NoNewline
try {
    $null = docker ps 2>&1
    Write-Host " ✓ Running" -ForegroundColor Green
} catch {
    Write-Host " ✗ Not running" -ForegroundColor Red
    exit 1
}

# Test 2: MCP Server image exists
Write-Host "Test 2: MCP Server image..." -NoNewline
$imageExists = docker images sorcha/mcp-server:latest --format "{{.Repository}}" | Select-String "sorcha/mcp-server"
if ($imageExists) {
    Write-Host " ✓ Built" -ForegroundColor Green
} else {
    Write-Host " ✗ Not built" -ForegroundColor Yellow
    Write-Host "         Building image..." -ForegroundColor Gray
    docker-compose build mcp-server
    Write-Host "         ✓ Image built" -ForegroundColor Green
}

# Test 3: Services running
Write-Host "Test 3: Sorcha services..." -NoNewline
$runningServices = docker-compose ps --services --filter "status=running"
if ($runningServices -contains "tenant-service") {
    Write-Host " ✓ Running" -ForegroundColor Green
} else {
    Write-Host " ✗ Not running" -ForegroundColor Yellow
    Write-Host "         Start with: docker-compose up -d" -ForegroundColor Gray
}

# Test 4: Tenant service accessible
Write-Host "Test 4: Tenant service health..." -NoNewline
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5450/health" -UseBasicParsing -TimeoutSec 3
    if ($response.StatusCode -eq 200) {
        Write-Host " ✓ Healthy" -ForegroundColor Green
    } else {
        Write-Host " ⚠ Status: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host " ✗ Not accessible" -ForegroundColor Red
    Write-Host "         Check with: docker-compose logs tenant-service" -ForegroundColor Gray
}

# Test 5: Can authenticate
Write-Host "Test 5: Authentication..." -NoNewline
try {
    $loginBody = @{
        email = "admin@sorcha.local"
        password = "Dev_Pass_2025!"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5450/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -TimeoutSec 5

    if ($response\.access_token) {
        Write-Host " ✓ Token received" -ForegroundColor Green

        # Test 6: Token is valid JWT
        Write-Host "Test 6: JWT token format..." -NoNewline
        $tokenParts = $response\.access_token.Split('.')
        if ($tokenParts.Length -eq 3) {
            Write-Host " ✓ Valid JWT" -ForegroundColor Green

            # Parse roles
            try {
                $payload = $tokenParts[1]
                while ($payload.Length % 4 -ne 0) { $payload += "=" }
                $payloadJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
                $claims = $payloadJson | ConvertFrom-Json

                Write-Host "Test 7: User roles..." -NoNewline
                if ($claims.roles) {
                    Write-Host " ✓ $($claims.roles.Count) roles" -ForegroundColor Green
                    Write-Host "         Roles: $($claims.roles -join ', ')" -ForegroundColor Gray
                } else {
                    Write-Host " ⚠ No roles found" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "Test 7: User roles... ⚠ Cannot parse" -ForegroundColor Yellow
            }
        } else {
            Write-Host " ✗ Invalid format" -ForegroundColor Red
        }

        # Test 8: MCP Server can start (dry run)
        Write-Host "Test 8: MCP Server startup..." -NoNewline
        try {
            # Quick test - run and immediately exit
            $testProcess = Start-Process -FilePath "docker" `
                -ArgumentList "run", "--rm", "--network", "sorcha_sorcha-network", `
                "-e", "SORCHA_JWT_TOKEN=$($response\.access_token)", `
                "sorcha/mcp-server:latest" `
                -PassThru -NoNewWindow -RedirectStandardError "NUL"

            Start-Sleep -Milliseconds 500
            if (-not $testProcess.HasExited) {
                Stop-Process -Id $testProcess.Id -Force -ErrorAction SilentlyContinue
                Write-Host " ✓ Can start" -ForegroundColor Green
            } else {
                Write-Host " ⚠ Quick exit" -ForegroundColor Yellow
            }
        } catch {
            Write-Host " ✗ Error: $_" -ForegroundColor Red
        }

    } else {
        Write-Host " ✗ No token" -ForegroundColor Red
    }
} catch {
    Write-Host " ✗ Failed" -ForegroundColor Red
    Write-Host "         Error: $($_.Exception.Message)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If all tests passed, run the full walkthrough:" -ForegroundColor White
Write-Host "  .\walkthroughs\McpServerBasics\get-token-and-run-mcp.ps1" -ForegroundColor Yellow
Write-Host ""
