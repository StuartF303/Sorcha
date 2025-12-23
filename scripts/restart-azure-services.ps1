# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Script to restart all Azure Container Apps to pull latest images

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "sorcha"
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Restart Azure Container Apps" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if logged in to Azure
Write-Host "Checking Azure authentication..." -ForegroundColor Yellow
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "  ✓ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  ✓ Subscription: $($account.name)" -ForegroundColor Green
}
catch {
    Write-Error "Not logged in to Azure. Please run: az login"
    exit 1
}

Write-Host ""
Write-Host "Restarting all Container Apps in resource group: $ResourceGroup" -ForegroundColor Yellow
Write-Host ""

$apps = @(
    "wallet-service",
    "tenant-service",
    "blueprint-api",
    "register-service",
    "peer-service",
    "validator-service",
    "api-gateway"
)

$successCount = 0
$failCount = 0

foreach ($app in $apps) {
    Write-Host "  Restarting $app..." -ForegroundColor Cyan
    try {
        az containerapp restart `
            --name $app `
            --resource-group $ResourceGroup `
            --output none
        Write-Host "    ✅ $app restarted" -ForegroundColor Green
        $successCount++

        # Wait a bit between restarts to avoid overwhelming the system
        Start-Sleep -Seconds 2
    }
    catch {
        Write-Warning "    ❌ Failed to restart $app (it may not exist yet)"
        $failCount++
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Restart Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  ✅ Successful: $successCount" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "  ⚠️  Failed/Skipped: $failCount" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Check logs:" -ForegroundColor Cyan
Write-Host "  az containerapp logs show --name tenant-service --resource-group $ResourceGroup --follow" -ForegroundColor Gray
Write-Host ""
