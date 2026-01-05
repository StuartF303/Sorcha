#!/usr/bin/env pwsh
# Quick rebuild script for Sorcha services
# Usage: ./scripts/rebuild-service.ps1 <service-name>

param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName
)

$ErrorActionPreference = "Stop"

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Rebuilding Sorcha Service: $ServiceName" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build Docker image
Write-Host "Step 1: Building Docker image..." -ForegroundColor Yellow
docker-compose build $ServiceName

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Docker build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Docker image built successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Restart container
Write-Host "Step 2: Restarting container..." -ForegroundColor Yellow
docker-compose up -d --force-recreate $ServiceName

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Container restart failed" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Container restarted successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Wait for container to stabilize
Write-Host "Step 3: Waiting for service to stabilize..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Step 4: Check container status
Write-Host "Step 4: Checking container status..." -ForegroundColor Yellow
$containerName = "sorcha-$ServiceName"
$status = docker ps --filter "name=$containerName" --format "{{.Status}}"

if ($status) {
    Write-Host "✓ Container is running: $status" -ForegroundColor Green
} else {
    Write-Host "✗ Container is not running" -ForegroundColor Red
    Write-Host ""
    Write-Host "Last 30 log lines:" -ForegroundColor Yellow
    docker logs $containerName --tail 30
    exit 1
}

Write-Host ""

# Step 5: Show recent logs
Write-Host "Step 5: Recent logs:" -ForegroundColor Yellow
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
docker logs $containerName --tail 15
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Rebuild Complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  • Check logs: docker logs $containerName -f" -ForegroundColor Gray
Write-Host "  • View all services: docker-compose ps" -ForegroundColor Gray
Write-Host "  • Run tests: pwsh walkthroughs/.../test-*.ps1" -ForegroundColor Gray
Write-Host ""
