# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Azure deployment script for Sorcha (PowerShell version)
# This script deploys the infrastructure and applications to Azure

param(
    [Parameter(Position = 0)]
    [ValidateSet('deploy', 'infra-only', 'images-only', 'status', 'urls')]
    [string]$Command = 'deploy',

    [string]$ResourceGroup = $env:AZURE_RESOURCE_GROUP ?? 'sorcha-rg',
    [string]$Location = $env:AZURE_LOCATION ?? 'eastus',
    [string]$RegistryName = $env:CONTAINER_REGISTRY ?? 'sorchaacr',
    [string]$Environment = $env:ENVIRONMENT ?? 'prod'
)

$ErrorActionPreference = 'Stop'

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Sorcha Azure Deployment Script" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroup"
Write-Host "Location: $Location"
Write-Host "Registry: $RegistryName"
Write-Host "Environment: $Environment"
Write-Host "=========================================" -ForegroundColor Cyan

function Test-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow

    # Check Azure CLI
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: Azure CLI is not installed" -ForegroundColor Red
        Write-Host "Please install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
        exit 1
    }

    # Check Docker
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: Docker is not installed" -ForegroundColor Red
        Write-Host "Please install from: https://docs.docker.com/get-docker/"
        exit 1
    }

    Write-Host "✓ Prerequisites check passed" -ForegroundColor Green
}

function Connect-Azure {
    Write-Host "Checking Azure login status..." -ForegroundColor Yellow

    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) {
        Write-Host "Not logged in. Please login to Azure..." -ForegroundColor Yellow
        az login
        $account = az account show | ConvertFrom-Json
    }

    Write-Host "✓ Logged in to subscription: $($account.name)" -ForegroundColor Green
    return $account
}

function Deploy-Infrastructure {
    Write-Host "Deploying Azure infrastructure..." -ForegroundColor Yellow

    $deploymentName = "sorcha-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

    az deployment sub create `
        --name $deploymentName `
        --location $Location `
        --template-file infra/main.bicep `
        --parameters resourceGroupName=$ResourceGroup `
                     location=$Location `
                     containerRegistryName=$RegistryName `
                     environment=$Environment `
        --verbose

    Write-Host "✓ Infrastructure deployed successfully" -ForegroundColor Green

    # Get outputs
    Write-Host "Retrieving deployment outputs..." -ForegroundColor Yellow
    $deployment = az deployment sub show --name $deploymentName | ConvertFrom-Json
    $acrLoginServer = $deployment.properties.outputs.containerRegistryLoginServer.value

    Write-Host "Container Registry: $acrLoginServer" -ForegroundColor Cyan

    return $deploymentName
}

function Build-AndPushImages {
    Write-Host "Building and pushing Docker images..." -ForegroundColor Yellow

    # Login to ACR
    Write-Host "Logging in to Azure Container Registry..." -ForegroundColor Yellow
    az acr login --name $RegistryName

    $acrServer = "$RegistryName.azurecr.io"

    # Build and push Blueprint API
    Write-Host "Building Blueprint API..." -ForegroundColor Yellow
    docker build -t "$acrServer/blueprint-api:latest" `
        -f src/Apps/Services/Sorcha.Blueprint.Api/Dockerfile .
    docker push "$acrServer/blueprint-api:latest"
    Write-Host "✓ Blueprint API pushed" -ForegroundColor Green

    # Build and push API Gateway
    Write-Host "Building API Gateway..." -ForegroundColor Yellow
    docker build -t "$acrServer/api-gateway:latest" `
        -f src/Apps/Services/Sorcha.ApiGateway/Dockerfile .
    docker push "$acrServer/api-gateway:latest"
    Write-Host "✓ API Gateway pushed" -ForegroundColor Green

    # Build and push Peer Service
    Write-Host "Building Peer Service..." -ForegroundColor Yellow
    docker build -t "$acrServer/peer-service:latest" `
        -f src/Apps/Services/Sorcha.Peer.Service/Dockerfile .
    docker push "$acrServer/peer-service:latest"
    Write-Host "✓ Peer Service pushed" -ForegroundColor Green

    # Build and push Blazor Client
    Write-Host "Building Blazor Client..." -ForegroundColor Yellow
    docker build -t "$acrServer/blazor-client:latest" `
        -f src/Apps/UI/Sorcha.Blueprint.Designer.Client/Dockerfile .
    docker push "$acrServer/blazor-client:latest"
    Write-Host "✓ Blazor Client pushed" -ForegroundColor Green

    Write-Host "✓ All images built and pushed successfully" -ForegroundColor Green
}

function Get-DeploymentUrls {
    Write-Host "Retrieving deployment URLs..." -ForegroundColor Yellow

    try {
        $apiGatewayUrl = az containerapp show `
            --name api-gateway `
            --resource-group $ResourceGroup `
            --query properties.configuration.ingress.fqdn `
            -o tsv 2>$null
    } catch {
        $apiGatewayUrl = "Not deployed yet"
    }

    try {
        $blazorUrl = az containerapp show `
            --name blazor-client `
            --resource-group $ResourceGroup `
            --query properties.configuration.ingress.fqdn `
            -o tsv 2>$null
    } catch {
        $blazorUrl = "Not deployed yet"
    }

    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "Deployment URLs:" -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "API Gateway:   https://$apiGatewayUrl" -ForegroundColor Green
    Write-Host "Blazor Client: https://$blazorUrl" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Cyan
}

function Show-DeploymentStatus {
    Write-Host "Container Apps Status:" -ForegroundColor Yellow
    az containerapp list --resource-group $ResourceGroup --output table
}

# Main execution
switch ($Command) {
    'deploy' {
        Test-Prerequisites
        Connect-Azure
        Deploy-Infrastructure
        Build-AndPushImages
        Get-DeploymentUrls
        Show-DeploymentStatus

        Write-Host ""
        Write-Host "=========================================" -ForegroundColor Green
        Write-Host "✓ Deployment completed successfully!" -ForegroundColor Green
        Write-Host "=========================================" -ForegroundColor Green
    }
    'infra-only' {
        Test-Prerequisites
        Connect-Azure
        Deploy-Infrastructure
    }
    'images-only' {
        Test-Prerequisites
        Connect-Azure
        Build-AndPushImages
    }
    'status' {
        Test-Prerequisites
        Connect-Azure
        Show-DeploymentStatus
        Get-DeploymentUrls
    }
    'urls' {
        Test-Prerequisites
        Connect-Azure
        Get-DeploymentUrls
    }
}
