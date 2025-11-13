#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Azure deployment script for Sorcha
# This script deploys the infrastructure and applications to Azure

set -e

# Configuration
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-sorcha-rg}"
LOCATION="${AZURE_LOCATION:-eastus}"
REGISTRY_NAME="${CONTAINER_REGISTRY:-sorchaacr}"
ENVIRONMENT="${ENVIRONMENT:-prod}"

echo "========================================="
echo "Sorcha Azure Deployment Script"
echo "========================================="
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo "Registry: $REGISTRY_NAME"
echo "Environment: $ENVIRONMENT"
echo "========================================="

# Function to check if Azure CLI is installed
check_prerequisites() {
    echo "Checking prerequisites..."

    if ! command -v az &> /dev/null; then
        echo "ERROR: Azure CLI is not installed"
        echo "Please install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
        exit 1
    fi

    if ! command -v docker &> /dev/null; then
        echo "ERROR: Docker is not installed"
        echo "Please install from: https://docs.docker.com/get-docker/"
        exit 1
    fi

    echo "✓ Prerequisites check passed"
}

# Function to login to Azure
azure_login() {
    echo "Checking Azure login status..."

    if ! az account show &> /dev/null; then
        echo "Not logged in. Please login to Azure..."
        az login
    fi

    SUBSCRIPTION=$(az account show --query name -o tsv)
    echo "✓ Logged in to subscription: $SUBSCRIPTION"
}

# Function to deploy infrastructure
deploy_infrastructure() {
    echo "Deploying Azure infrastructure..."

    DEPLOYMENT_NAME="sorcha-deployment-$(date +%Y%m%d-%H%M%S)"

    az deployment sub create \
        --name "$DEPLOYMENT_NAME" \
        --location "$LOCATION" \
        --template-file infra/main.bicep \
        --parameters resourceGroupName="$RESOURCE_GROUP" \
                     location="$LOCATION" \
                     containerRegistryName="$REGISTRY_NAME" \
                     environment="$ENVIRONMENT" \
        --verbose

    echo "✓ Infrastructure deployed successfully"

    # Get outputs
    echo "Retrieving deployment outputs..."
    ACR_LOGIN_SERVER=$(az deployment sub show \
        --name "$DEPLOYMENT_NAME" \
        --query properties.outputs.containerRegistryLoginServer.value \
        -o tsv)

    echo "Container Registry: $ACR_LOGIN_SERVER"
}

# Function to build and push Docker images
build_and_push_images() {
    echo "Building and pushing Docker images..."

    # Login to ACR
    echo "Logging in to Azure Container Registry..."
    az acr login --name "$REGISTRY_NAME"

    ACR_SERVER="$REGISTRY_NAME.azurecr.io"

    # Build and push Blueprint API
    echo "Building Blueprint API..."
    docker build -t "$ACR_SERVER/blueprint-api:latest" \
        -f src/Apps/Services/Sorcha.Blueprint.Api/Dockerfile .
    docker push "$ACR_SERVER/blueprint-api:latest"
    echo "✓ Blueprint API pushed"

    # Build and push API Gateway
    echo "Building API Gateway..."
    docker build -t "$ACR_SERVER/api-gateway:latest" \
        -f src/Apps/Services/Sorcha.ApiGateway/Dockerfile .
    docker push "$ACR_SERVER/api-gateway:latest"
    echo "✓ API Gateway pushed"

    # Build and push Peer Service
    echo "Building Peer Service..."
    docker build -t "$ACR_SERVER/peer-service:latest" \
        -f src/Apps/Services/Sorcha.Peer.Service/Dockerfile .
    docker push "$ACR_SERVER/peer-service:latest"
    echo "✓ Peer Service pushed"

    # Build and push Blazor Client
    echo "Building Blazor Client..."
    docker build -t "$ACR_SERVER/blazor-client:latest" \
        -f src/Apps/UI/Sorcha.Blueprint.Designer.Client/Dockerfile .
    docker push "$ACR_SERVER/blazor-client:latest"
    echo "✓ Blazor Client pushed"

    echo "✓ All images built and pushed successfully"
}

# Function to get deployment URLs
get_urls() {
    echo "Retrieving deployment URLs..."

    API_GATEWAY_URL=$(az containerapp show \
        --name api-gateway \
        --resource-group "$RESOURCE_GROUP" \
        --query properties.configuration.ingress.fqdn \
        -o tsv 2>/dev/null || echo "Not deployed yet")

    BLAZOR_URL=$(az containerapp show \
        --name blazor-client \
        --resource-group "$RESOURCE_GROUP" \
        --query properties.configuration.ingress.fqdn \
        -o tsv 2>/dev/null || echo "Not deployed yet")

    echo "========================================="
    echo "Deployment URLs:"
    echo "========================================="
    echo "API Gateway:   https://$API_GATEWAY_URL"
    echo "Blazor Client: https://$BLAZOR_URL"
    echo "========================================="
}

# Function to show deployment status
show_status() {
    echo "Container Apps Status:"
    az containerapp list \
        --resource-group "$RESOURCE_GROUP" \
        --output table
}

# Main deployment flow
main() {
    check_prerequisites
    azure_login
    deploy_infrastructure
    build_and_push_images
    get_urls
    show_status

    echo ""
    echo "========================================="
    echo "✓ Deployment completed successfully!"
    echo "========================================="
}

# Parse command line arguments
case "${1:-deploy}" in
    deploy)
        main
        ;;
    infra-only)
        check_prerequisites
        azure_login
        deploy_infrastructure
        ;;
    images-only)
        check_prerequisites
        azure_login
        build_and_push_images
        ;;
    status)
        check_prerequisites
        azure_login
        show_status
        get_urls
        ;;
    urls)
        check_prerequisites
        azure_login
        get_urls
        ;;
    *)
        echo "Usage: $0 {deploy|infra-only|images-only|status|urls}"
        echo ""
        echo "Commands:"
        echo "  deploy       - Full deployment (infrastructure + images)"
        echo "  infra-only   - Deploy infrastructure only"
        echo "  images-only  - Build and push images only"
        echo "  status       - Show deployment status"
        echo "  urls         - Show deployment URLs"
        exit 1
        ;;
esac
