# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Test script for Sorcha Admin Integration
# Tests: Docker build, container health, API Gateway routing, authentication

$ErrorActionPreference = "Continue"

Write-Host "`n=== Sorcha Admin Integration Test ===" -ForegroundColor Cyan
Write-Host "Testing admin UI integration behind API Gateway`n" -ForegroundColor Cyan

# Test 1: Build sorcha-admin container
Write-Host "[1/6] Building sorcha-admin container..." -ForegroundColor Yellow
docker-compose build sorcha-admin 2>&1 | Select-Object -Last 5
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Build succeeded" -ForegroundColor Green
} else {
    Write-Host "  ✗ Build failed" -ForegroundColor Red
    exit 1
}

# Test 2: Start all services
Write-Host "`n[2/6] Starting all Docker services..." -ForegroundColor Yellow
docker-compose up -d
Start-Sleep -Seconds 10
Write-Host "  ✓ Services started" -ForegroundColor Green

# Test 3: Check container health
Write-Host "`n[3/6] Checking container health..." -ForegroundColor Yellow
$adminContainer = docker ps --filter "name=sorcha-admin" --format "{{.Names}} {{.Status}}"
Write-Host "  $adminContainer" -ForegroundColor Gray
if ($adminContainer -match "Up") {
    Write-Host "  ✓ sorcha-admin container is running" -ForegroundColor Green
} else {
    Write-Host "  ✗ sorcha-admin container not running" -ForegroundColor Red
    docker logs sorcha-admin 2>&1 | Select-Object -Last 10
    exit 1
}

# Test 4: Test nginx health endpoint
Write-Host "`n[4/6] Testing nginx health endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost/admin/health" -Method GET -ErrorAction Stop
    Write-Host "  Response: $healthResponse" -ForegroundColor Gray
    if ($healthResponse -match "healthy") {
        Write-Host "  ✓ nginx health check passed" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Unexpected health response" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ⚠ Health endpoint not accessible (may be internal only)" -ForegroundColor Yellow
}

# Test 5: Test admin UI access through API Gateway
Write-Host "`n[5/6] Testing admin UI access via API Gateway..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost/admin/" -Method GET -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✓ Admin UI accessible at http://localhost/admin" -ForegroundColor Green
        Write-Host "  Status Code: $($response.StatusCode)" -ForegroundColor Gray

        # Check if HTML contains expected content
        if ($response.Content -match "Sorcha Admin" -or $response.Content -match "blazor") {
            Write-Host "  ✓ Page contains expected content" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ Page content unexpected" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "  ✗ Failed to access admin UI: $($_.Exception.Message)" -ForegroundColor Red

    # Debug: Check API Gateway logs
    Write-Host "`n  Checking API Gateway logs..." -ForegroundColor Gray
    docker logs sorcha-api-gateway 2>&1 | Select-String -Pattern "admin" -Context 1 | Select-Object -Last 5

    Write-Host "`n  Checking sorcha-admin logs..." -ForegroundColor Gray
    docker logs sorcha-admin 2>&1 | Select-Object -Last 10
}

# Test 6: Test authentication endpoint
Write-Host "`n[6/6] Testing authentication endpoint..." -ForegroundColor Yellow
try {
    $body = @{
        grant_type = 'password'
        username = 'stuart.mackintosh@sorcha.dev'
        password = 'SorchaDev2025!'
        client_id = 'sorcha-admin'
    }

    $authResponse = Invoke-RestMethod `
        -Uri 'http://localhost/api/service-auth/token' `
        -Method POST `
        -Body $body `
        -ContentType 'application/x-www-form-urlencoded' `
        -ErrorAction Stop

    if ($authResponse.access_token) {
        Write-Host "  ✓ Authentication successful" -ForegroundColor Green
        Write-Host "  Token Type: $($authResponse.token_type)" -ForegroundColor Gray
        Write-Host "  Expires In: $($authResponse.expires_in) seconds" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ✗ Authentication failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Admin UI URL: http://localhost/admin" -ForegroundColor White
Write-Host "Credentials: stuart.mackintosh@sorcha.dev / SorchaDev2025!" -ForegroundColor White
Write-Host "Profile: docker" -ForegroundColor White
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Open http://localhost/admin in your browser" -ForegroundColor White
Write-Host "2. Select 'docker' profile" -ForegroundColor White
Write-Host "3. Login with bootstrap credentials" -ForegroundColor White
Write-Host "4. Verify dashboard loads with service statistics" -ForegroundColor White
Write-Host ""
