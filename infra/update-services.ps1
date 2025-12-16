# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Quick update script for Sorcha services in Azure (PowerShell version)
# This script builds and pushes specific services without full redeployment

param(
    [Parameter(Position=0)]
    [ValidateSet('peer', 'gateway', 'api-gateway', 'blueprint', 'wallet', 'register', 'tenant', 'blazor', 'admin', 'peer-gateway', 'all', 'status', 'logs')]
    [string]$Command = 'all',

    [Parameter(Position=1)]
    [string]$ServiceName = '',

    [string]$ResourceGroup = $env:AZURE_RESOURCE_GROUP,
    [string]$RegistryName = $env:CONTAINER_REGISTRY,
    [string]$ImageTag = 'latest'
)

# Set defaults if not provided
if ([string]::IsNullOrEmpty($ResourceGroup)) { $ResourceGroup = 'sorcha-uksouth-rg' }
if ([string]::IsNullOrEmpty($RegistryName)) { $RegistryName = 'sorchauksouth' }

# Colors for output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = 'White'
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    $header = "`n=========================================`n$Title`n========================================="
    Write-ColorOutput $header -Color Cyan
}

function Write-SubHeader {
    param([string]$Title)
    $subheader = "`n=========================================`n$Title`n========================================="
    Write-ColorOutput $subheader -Color Cyan
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" -Color Green
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠ $Message" -Color Yellow
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" -Color Red
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput $Message -Color Yellow
}

# Display configuration
Write-Header "Sorcha Service Update Script (PowerShell)"
Write-ColorOutput "Resource Group: " -Color White -NoNewline
Write-ColorOutput $ResourceGroup -Color Green
Write-ColorOutput "Registry: " -Color White -NoNewline
Write-ColorOutput $RegistryName -Color Green
Write-ColorOutput "Image Tag: " -Color White -NoNewline
Write-ColorOutput $ImageTag -Color Green
Write-ColorOutput "=========================================" -Color Cyan

# Function to check prerequisites
function Test-Prerequisites {
    Write-Info "`nChecking prerequisites..."

    # Check Azure CLI
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Error "Azure CLI is not installed"
        Write-Host "Please install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
        exit 1
    }

    # Check Docker
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Error "Docker is not installed"
        Write-Host "Please install from: https://docs.docker.com/get-docker/"
        exit 1
    }

    # Check Azure login
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) {
        Write-Warning "Not logged in to Azure. Please login..."
        az login
    }

    Write-Success "Prerequisites check passed"
}

# Function to build and push a service
function Build-AndPushService {
    param(
        [string]$ServiceName,
        [string]$Dockerfile,
        [string]$ImageName
    )

    Write-SubHeader "$ServiceName UPDATE"

    Write-Header "Building $ServiceName..."

    $AcrServer = "$RegistryName.azurecr.io"
    $FullImageName = "$AcrServer/$ImageName`:$ImageTag"

    # Build the image
    Write-Info "Building Docker image: $FullImageName"
    docker build -t $FullImageName -f $Dockerfile .

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $ServiceName"
        return $false
    }
    Write-Success "Build successful"

    # Login to ACR
    Write-Info "Logging in to Azure Container Registry..."
    az acr login --name $RegistryName

    if ($LASTEXITCODE -ne 0) {
        Write-Error "ACR login failed"
        return $false
    }

    # Push the image
    Write-Info "Pushing image to ACR..."
    docker push $FullImageName

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Push failed for $ServiceName"
        return $false
    }
    Write-Success "Push successful"

    return $true
}

# Function to update a container app
function Update-ContainerApp {
    param(
        [string]$AppName,
        [string]$ImageName
    )

    Write-Header "Updating $AppName in Azure..."

    $AcrServer = "$RegistryName.azurecr.io"
    $FullImageName = "$AcrServer/$ImageName`:$ImageTag"

    # Check if container app exists
    $app = az containerapp show --name $AppName --resource-group $ResourceGroup 2>$null | ConvertFrom-Json
    if (-not $app) {
        Write-Error "Container app $AppName not found in resource group $ResourceGroup"
        return $false
    }

    # Update the container app
    Write-Info "Updating container app with new image..."
    az containerapp update `
        --name $AppName `
        --resource-group $ResourceGroup `
        --image $FullImageName `
        --output table

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to update container app"
        return $false
    }
    Write-Success "Container app updated successfully"

    # Wait a moment for the update to propagate
    Start-Sleep -Seconds 5

    # Show revision info
    Write-Info "Active revisions:"
    az containerapp revision list `
        --name $AppName `
        --resource-group $ResourceGroup `
        --query '[?properties.active].{Revision:name,Created:properties.createdTime,Traffic:properties.trafficWeight,Replicas:properties.replicas}' `
        --output table

    return $true
}

# Function to check service health
function Test-ServiceHealth {
    param([string]$AppName)

    Write-Info "`nChecking health of $AppName..."

    # Get the FQDN
    $app = az containerapp show `
        --name $AppName `
        --resource-group $ResourceGroup `
        --query "properties.configuration.ingress.fqdn" `
        -o tsv 2>$null

    if ([string]::IsNullOrEmpty($app)) {
        Write-Warning "Could not retrieve FQDN for $AppName"
        return $false
    }

    Write-ColorOutput "Service URL: " -Color White -NoNewline
    Write-ColorOutput "https://$app" -Color Green

    # Try to ping the health endpoint
    Write-Info "Checking /health endpoint..."
    try {
        $response = Invoke-WebRequest -Uri "https://$app/health" -TimeoutSec 10 -ErrorAction Stop
        Write-Success "Health check passed (Status: $($response.StatusCode))"
        return $true
    }
    catch {
        Write-Warning "Health check failed or endpoint not available"
        return $false
    }
}

# Function to check root endpoint
function Test-RootEndpoint {
    param([string]$AppName)

    Write-Info "`nChecking root endpoint of $AppName..."

    # Get the FQDN
    $app = az containerapp show `
        --name $AppName `
        --resource-group $ResourceGroup `
        --query "properties.configuration.ingress.fqdn" `
        -o tsv 2>$null

    if ([string]::IsNullOrEmpty($app)) {
        Write-Warning "Could not retrieve FQDN for $AppName"
        return
    }

    $url = "https://$app"
    Write-ColorOutput "Root URL: " -Color White -NoNewline
    Write-ColorOutput $url -Color Green

    # Try to access the root endpoint
    Write-Info "Fetching root page..."
    try {
        $response = Invoke-WebRequest -Uri $url -TimeoutSec 10 -ErrorAction Stop
        Write-Success "Root endpoint accessible (Status: $($response.StatusCode))"
        Write-Info "Content-Type: $($response.Headers['Content-Type'])"
        Write-Info "Content-Length: $($response.Content.Length) bytes"

        # Show first 500 characters of response
        $preview = $response.Content.Substring(0, [Math]::Min(500, $response.Content.Length))
        Write-ColorOutput "`nResponse Preview:" -Color Cyan
        Write-Host $preview

        if ($response.Content.Length -gt 500) {
            Write-Host "... (truncated)"
        }
    }
    catch {
        Write-Warning "Root endpoint check failed: $($_.Exception.Message)"
    }
}

# Function to show logs
function Show-Logs {
    param([string]$AppName)

    $logMsg = "Recent logs for ${AppName}:"
    Write-Info $logMsg
    az containerapp logs show `
        --name $AppName `
        --resource-group $ResourceGroup `
        --tail 20 `
        --follow $false
}

# Update functions for each service
function Update-PeerService {
    if (Build-AndPushService "Peer Service" "src/Services/Sorcha.Peer.Service/Dockerfile" "peer-service") {
        if (Update-ContainerApp "peer-service" "peer-service") {
            Test-ServiceHealth "peer-service"
        }
    }
}

function Update-ApiGateway {
    if (Build-AndPushService "API Gateway" "src/Services/Sorcha.ApiGateway/Dockerfile" "api-gateway") {
        if (Update-ContainerApp "api-gateway" "api-gateway") {
            Test-ServiceHealth "api-gateway"
            Test-RootEndpoint "api-gateway"
        }
    }
}

function Update-BlueprintService {
    if (Build-AndPushService "Blueprint Service" "src/Services/Sorcha.Blueprint.Service/Dockerfile" "blueprint-api") {
        if (Update-ContainerApp "blueprint-api" "blueprint-api") {
            Test-ServiceHealth "blueprint-api"
        }
    }
}

function Update-WalletService {
    if (Build-AndPushService "Wallet Service" "src/Services/Sorcha.Wallet.Service/Dockerfile" "wallet-service") {
        if (Update-ContainerApp "wallet-service" "wallet-service") {
            Test-ServiceHealth "wallet-service"
        }
    }
}

function Update-RegisterService {
    if (Build-AndPushService "Register Service" "src/Services/Sorcha.Register.Service/Dockerfile" "register-service") {
        if (Update-ContainerApp "register-service" "register-service") {
            Test-ServiceHealth "register-service"
        }
    }
}

function Update-TenantService {
    if (Build-AndPushService "Tenant Service" "src/Services/Sorcha.Tenant.Service/Dockerfile" "tenant-service") {
        if (Update-ContainerApp "tenant-service" "tenant-service") {
            Test-ServiceHealth "tenant-service"
        }
    }
}

function Update-BlazorAdmin {
    if (Build-AndPushService "Blazor Admin" "src/Apps/Sorcha.Admin/Dockerfile" "sorcha-admin") {
        if (Update-ContainerApp "blazor-client" "sorcha-admin") {
            Test-ServiceHealth "blazor-client"
        }
    }
}

# Show deployment status
function Show-Status {
    Write-Header "Deployment Status"
    az containerapp list `
        --resource-group $ResourceGroup `
        --query '[].{Name:name,Status:properties.runningStatus,Replicas:properties.template.scale.minReplicas,URL:properties.configuration.ingress.fqdn}' `
        --output table
}

# Main execution
Test-Prerequisites

switch ($Command) {
    'peer' { Update-PeerService }
    'gateway' { Update-ApiGateway }
    'api-gateway' { Update-ApiGateway }
    'blueprint' { Update-BlueprintService }
    'wallet' { Update-WalletService }
    'register' { Update-RegisterService }
    'tenant' { Update-TenantService }
    'blazor' { Update-BlazorAdmin }
    'admin' { Update-BlazorAdmin }
    'peer-gateway' {
        Update-PeerService
        Update-ApiGateway
    }
    'all' {
        Update-PeerService
        Update-ApiGateway
        Update-BlueprintService
        Update-WalletService
        Update-RegisterService
        Update-TenantService
        Update-BlazorAdmin
    }
    'status' { Show-Status }
    'logs' {
        if ([string]::IsNullOrEmpty($ServiceName)) {
            Write-Error "Usage: .\update-services.ps1 logs SERVICE-NAME"
            exit 1
        }
        Show-Logs $ServiceName
    }
    default {
        Write-ColorOutput "Usage: .\update-services.ps1 COMMAND [SERVICE-NAME]" -Color Yellow
        Write-Host ""
        Write-ColorOutput "Commands:" -Color Cyan
        Write-Host "  peer           - Update Peer Service only"
        Write-Host "  gateway        - Update API Gateway only"
        Write-Host "  blueprint      - Update Blueprint Service only"
        Write-Host "  wallet         - Update Wallet Service only"
        Write-Host "  register       - Update Register Service only"
        Write-Host "  tenant         - Update Tenant Service only"
        Write-Host "  blazor         - Update Blazor Admin only"
        Write-Host "  peer-gateway   - Update Peer Service and API Gateway"
        Write-Host "  all            - Update all services"
        Write-Host "  status         - Show deployment status"
        Write-Host "  logs SERVICE   - Show logs for a service"
        exit 1
    }
}

Write-ColorOutput "`n=========================================`nUpdate Completed`n=========================================" -Color Cyan
Show-Status
