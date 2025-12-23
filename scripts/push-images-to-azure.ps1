# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Script to build and push all container images to Azure Container Registry

param(
    [Parameter(Mandatory=$false)]
    [string]$ContainerRegistry = "sorchauksouth",

    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest",

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Push Sorcha Images to Azure Container Registry" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
Write-Host "[1/4] Checking prerequisites..." -ForegroundColor Yellow
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "  ✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
}
catch {
    Write-Error "Azure CLI is not installed. Please install: https://aka.ms/InstallAzureCLIDirect"
    exit 1
}

# Check if logged in to Azure
Write-Host "[2/4] Checking Azure authentication..." -ForegroundColor Yellow
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "  ✓ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  ✓ Subscription: $($account.name) ($($account.id))" -ForegroundColor Green
}
catch {
    Write-Error "Not logged in to Azure. Please run: az login"
    exit 1
}

# Login to ACR
Write-Host "[3/4] Logging in to Azure Container Registry..." -ForegroundColor Yellow
try {
    az acr login --name $ContainerRegistry
    Write-Host "  ✓ Logged in to $ContainerRegistry.azurecr.io" -ForegroundColor Green
}
catch {
    Write-Error "Failed to login to ACR. Does the registry exist?"
    exit 1
}

# Build and push images
Write-Host "[4/4] Building and pushing container images..." -ForegroundColor Yellow

$services = @(
    @{Name="blueprint-service"; AcrName="blueprint-service"},
    @{Name="wallet-service"; AcrName="wallet-service"},
    @{Name="tenant-service"; AcrName="tenant-service"},
    @{Name="register-service"; AcrName="register-service"},
    @{Name="peer-service"; AcrName="peer-service"},
    @{Name="validator-service"; AcrName="validator-service"},
    @{Name="api-gateway"; AcrName="api-gateway"}
)

$acrLoginServer = "$ContainerRegistry.azurecr.io"
$successCount = 0
$failCount = 0

Push-Location $PSScriptRoot\..

try {
    foreach ($service in $services) {
        $serviceName = $service.Name
        $acrName = $service.AcrName

        Write-Host ""
        Write-Host "  Processing $serviceName..." -ForegroundColor Cyan

        # Build image (if not skipped)
        if (-not $SkipBuild) {
            Write-Host "    Building with docker-compose..." -ForegroundColor Gray
            docker-compose build $serviceName
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Failed to build $serviceName"
                $failCount++
                continue
            }
        }

        # Tag image
        $localTag = "sorcha/${serviceName}:latest"
        $remoteTag = "${acrLoginServer}/${acrName}:${ImageTag}"

        Write-Host "    Tagging as $remoteTag..." -ForegroundColor Gray
        docker tag $localTag $remoteTag
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to tag $serviceName"
            $failCount++
            continue
        }

        # Also tag as 'latest' if using a specific tag
        if ($ImageTag -ne "latest") {
            docker tag $localTag "${acrLoginServer}/${acrName}:latest"
        }

        # Push image
        Write-Host "    Pushing to ACR..." -ForegroundColor Gray
        docker push $remoteTag
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to push $serviceName"
            $failCount++
            continue
        }

        # Push 'latest' tag if using a specific tag
        if ($ImageTag -ne "latest") {
            docker push "${acrLoginServer}/${acrName}:latest"
        }

        Write-Host "    ✅ $serviceName pushed successfully" -ForegroundColor Green
        $successCount++
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Push Complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  ✅ Successful: $successCount" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "  ❌ Failed: $failCount" -ForegroundColor Red
}
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Restart Container Apps to pull new images:" -ForegroundColor White
Write-Host "     az containerapp restart --name wallet-service --resource-group sorcha" -ForegroundColor Gray
Write-Host "     az containerapp restart --name tenant-service --resource-group sorcha" -ForegroundColor Gray
Write-Host "     # ... repeat for all services" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Or use the deployment script to restart all:" -ForegroundColor White
Write-Host "     .\scripts\restart-azure-services.ps1" -ForegroundColor Gray
Write-Host ""

if ($failCount -eq 0) {
    exit 0
} else {
    exit 1
}
