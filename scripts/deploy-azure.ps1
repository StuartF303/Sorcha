# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Azure Deployment Script for Sorcha Platform
# Automates deployment of infrastructure and container images to Azure

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]$Location,

    [Parameter(Mandatory=$true)]
    [string]$ContainerRegistryName,

    [Parameter(Mandatory=$false)]
    [string]$Environment = "prod",

    [Parameter(Mandatory=$false)]
    [SecureString]$PostgresAdminPassword,

    [Parameter(Mandatory=$false)]
    [switch]$SkipInfrastructure,

    [Parameter(Mandatory=$false)]
    [switch]$SkipImages,

    [Parameter(Mandatory=$false)]
    [switch]$SkipRestart
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Sorcha Azure Deployment Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
Write-Host "[1/7] Checking prerequisites..." -ForegroundColor Yellow
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Host "  ✓ Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green
}
catch {
    Write-Error "Azure CLI is not installed. Please install: https://aka.ms/InstallAzureCLIDirect"
    exit 1
}

# Check if logged in to Azure
Write-Host "[2/7] Checking Azure authentication..." -ForegroundColor Yellow
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Host "  ✓ Logged in as: $($account.user.name)" -ForegroundColor Green
    Write-Host "  ✓ Subscription: $($account.name) ($($account.id))" -ForegroundColor Green
}
catch {
    Write-Error "Not logged in to Azure. Please run: az login"
    exit 1
}

# Generate PostgreSQL password if not provided
if (-not $PostgresAdminPassword) {
    Write-Host "[3/7] Generating secure PostgreSQL password..." -ForegroundColor Yellow
    $passwordBytes = New-Object byte[] 32
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($passwordBytes)
    $passwordPlain = [Convert]::ToBase64String($passwordBytes)
    $PostgresAdminPassword = ConvertTo-SecureString $passwordPlain -AsPlainText -Force

    Write-Host "  ⚠️  PostgreSQL Password (SAVE THIS!):" -ForegroundColor Red
    Write-Host "     $passwordPlain" -ForegroundColor Yellow
    Write-Host ""

    # Save to file
    $passwordPlain | Out-File -FilePath "$PSScriptRoot\postgres-password-$Environment.txt" -NoNewline
    Write-Host "  ✓ Password saved to: postgres-password-$Environment.txt" -ForegroundColor Green
}
else {
    Write-Host "[3/7] Using provided PostgreSQL password..." -ForegroundColor Yellow
    $passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($PostgresAdminPassword))
}

# Deploy infrastructure
if (-not $SkipInfrastructure) {
    Write-Host "[4/7] Deploying Azure infrastructure..." -ForegroundColor Yellow
    Write-Host "  Resource Group: $ResourceGroupName" -ForegroundColor Gray
    Write-Host "  Location: $Location" -ForegroundColor Gray
    Write-Host "  Container Registry: $ContainerRegistryName" -ForegroundColor Gray
    Write-Host "  Environment: $Environment" -ForegroundColor Gray
    Write-Host ""

    $bicepFile = Join-Path $PSScriptRoot "..\infra\main.bicep"

    try {
        az deployment sub create `
            --location $Location `
            --template-file $bicepFile `
            --parameters resourceGroupName=$ResourceGroupName `
            --parameters location=$Location `
            --parameters containerRegistryName=$ContainerRegistryName `
            --parameters environment=$Environment `
            --parameters postgresAdminPassword=$passwordPlain `
            --output table

        Write-Host "  ✓ Infrastructure deployed successfully" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to deploy infrastructure: $_"
        exit 1
    }
}
else {
    Write-Host "[4/7] Skipping infrastructure deployment..." -ForegroundColor Gray
}

# Build and push container images
if (-not $SkipImages) {
    Write-Host "[5/7] Building and pushing container images..." -ForegroundColor Yellow

    # Login to ACR
    Write-Host "  Logging in to Azure Container Registry..." -ForegroundColor Gray
    az acr login --name $ContainerRegistryName

    $acrLoginServer = "$ContainerRegistryName.azurecr.io"

    # Services to build
    $services = @(
        @{Name="wallet-service"; Tag="wallet-service"},
        @{Name="tenant-service"; Tag="tenant-service"},
        @{Name="blueprint-service"; Tag="blueprint-api"},
        @{Name="register-service"; Tag="register-service"},
        @{Name="peer-service"; Tag="peer-service"},
        @{Name="validator-service"; Tag="validator-service"},
        @{Name="api-gateway"; Tag="api-gateway"}
    )

    Push-Location (Join-Path $PSScriptRoot "..")

    try {
        foreach ($service in $services) {
            Write-Host "  Building $($service.Name)..." -ForegroundColor Gray

            docker-compose build $($service.Name)

            $localTag = "sorcha/$($service.Name):latest"
            $remoteTag = "$acrLoginServer/$($service.Tag):latest"

            Write-Host "  Tagging as $remoteTag..." -ForegroundColor Gray
            docker tag $localTag $remoteTag

            Write-Host "  Pushing to ACR..." -ForegroundColor Gray
            docker push $remoteTag

            Write-Host "  ✓ $($service.Name) pushed successfully" -ForegroundColor Green
        }
    }
    catch {
        Write-Error "Failed to build/push images: $_"
        exit 1
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "[5/7] Skipping container image build..." -ForegroundColor Gray
}

# Restart Container Apps
if (-not $SkipRestart) {
    Write-Host "[6/7] Restarting Container Apps..." -ForegroundColor Yellow

    $apps = @(
        "wallet-service",
        "tenant-service",
        "blueprint-api",
        "register-service",
        "peer-service",
        "validator-service",
        "api-gateway"
    )

    foreach ($app in $apps) {
        Write-Host "  Restarting $app..." -ForegroundColor Gray
        try {
            az containerapp restart `
                --name $app `
                --resource-group $ResourceGroupName `
                --output none
            Write-Host "  ✓ $app restarted" -ForegroundColor Green
        }
        catch {
            Write-Warning "Failed to restart $app (it may not exist yet)"
        }
    }
}
else {
    Write-Host "[6/7] Skipping Container App restart..." -ForegroundColor Gray
}

# Get deployment outputs
Write-Host "[7/7] Retrieving deployment information..." -ForegroundColor Yellow

try {
    $deployment = az deployment sub show `
        --name "sorcha-resources" `
        --query "properties.outputs" `
        --output json | ConvertFrom-Json

    Write-Host ""
    Write-Host "================================================" -ForegroundColor Green
    Write-Host "  Deployment Successful!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Deployment Information:" -ForegroundColor Cyan
    Write-Host "  Resource Group: $ResourceGroupName" -ForegroundColor White
    Write-Host "  Container Registry: $($deployment.containerRegistryName.value)" -ForegroundColor White
    Write-Host "  API Gateway URL: $($deployment.apiGatewayUrl.value)" -ForegroundColor White
    Write-Host "  Blazor Client URL: $($deployment.blazorClientUrl.value)" -ForegroundColor White
    Write-Host ""

    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Check Tenant Service logs for service principal credentials:" -ForegroundColor White
    Write-Host "     az containerapp logs show --name tenant-service --resource-group $ResourceGroupName --tail 100" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Login with default admin credentials:" -ForegroundColor White
    Write-Host "     Email: admin@sorcha.local" -ForegroundColor Gray
    Write-Host "     Password: Dev_Pass_2025!" -ForegroundColor Gray
    Write-Host "     ⚠️  CHANGE THIS PASSWORD IMMEDIATELY!" -ForegroundColor Red
    Write-Host ""
    Write-Host "  3. View logs for any service:" -ForegroundColor White
    Write-Host "     az containerapp logs show --name wallet-service --resource-group $ResourceGroupName --follow" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. Access API Gateway at:" -ForegroundColor White
    Write-Host "     $($deployment.apiGatewayUrl.value)" -ForegroundColor Yellow
    Write-Host ""
}
catch {
    Write-Warning "Could not retrieve deployment outputs"
}

Write-Host "Deployment script completed!" -ForegroundColor Green
