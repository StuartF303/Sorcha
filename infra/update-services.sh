#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors
#
# Quick update script for Sorcha services in Azure
# This script builds and pushes specific services without full redeployment

set -e

# Configuration
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-sorcha-rg}"
REGISTRY_NAME="${CONTAINER_REGISTRY:-sorchaacr}"
TAG="${IMAGE_TAG:-latest}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=========================================${NC}"
echo -e "${BLUE}Sorcha Service Update Script${NC}"
echo -e "${BLUE}=========================================${NC}"
echo -e "Resource Group: ${GREEN}$RESOURCE_GROUP${NC}"
echo -e "Registry: ${GREEN}$REGISTRY_NAME${NC}"
echo -e "Image Tag: ${GREEN}$TAG${NC}"
echo -e "${BLUE}=========================================${NC}"

# Function to check prerequisites
check_prerequisites() {
    echo -e "\n${YELLOW}Checking prerequisites...${NC}"

    if ! command -v az &> /dev/null; then
        echo -e "${RED}ERROR: Azure CLI is not installed${NC}"
        exit 1
    fi

    if ! command -v docker &> /dev/null; then
        echo -e "${RED}ERROR: Docker is not installed${NC}"
        exit 1
    fi

    # Check Azure login
    if ! az account show &> /dev/null; then
        echo -e "${RED}Not logged in to Azure. Please login...${NC}"
        az login
    fi

    echo -e "${GREEN}✓ Prerequisites check passed${NC}"
}

# Function to build and push a service
build_and_push_service() {
    local SERVICE_NAME=$1
    local DOCKERFILE=$2
    local IMAGE_NAME=$3

    echo -e "\n${BLUE}=========================================${NC}"
    echo -e "${YELLOW}Building ${SERVICE_NAME}...${NC}"
    echo -e "${BLUE}=========================================${NC}"

    ACR_SERVER="$REGISTRY_NAME.azurecr.io"
    FULL_IMAGE_NAME="$ACR_SERVER/$IMAGE_NAME:$TAG"

    # Build the image
    echo -e "${YELLOW}Building Docker image: ${FULL_IMAGE_NAME}${NC}"
    if docker build -t "$FULL_IMAGE_NAME" -f "$DOCKERFILE" .; then
        echo -e "${GREEN}✓ Build successful${NC}"
    else
        echo -e "${RED}✗ Build failed for ${SERVICE_NAME}${NC}"
        return 1
    fi

    # Login to ACR
    echo -e "${YELLOW}Logging in to Azure Container Registry...${NC}"
    az acr login --name "$REGISTRY_NAME"

    # Push the image
    echo -e "${YELLOW}Pushing image to ACR...${NC}"
    if docker push "$FULL_IMAGE_NAME"; then
        echo -e "${GREEN}✓ Push successful${NC}"
    else
        echo -e "${RED}✗ Push failed for ${SERVICE_NAME}${NC}"
        return 1
    fi

    return 0
}

# Function to update a container app
update_container_app() {
    local APP_NAME=$1
    local IMAGE_NAME=$2

    echo -e "\n${BLUE}=========================================${NC}"
    echo -e "${YELLOW}Updating ${APP_NAME} in Azure...${NC}"
    echo -e "${BLUE}=========================================${NC}"

    ACR_SERVER="$REGISTRY_NAME.azurecr.io"
    FULL_IMAGE_NAME="$ACR_SERVER/$IMAGE_NAME:$TAG"

    # Check if container app exists
    if ! az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        echo -e "${RED}✗ Container app ${APP_NAME} not found in resource group ${RESOURCE_GROUP}${NC}"
        return 1
    fi

    # Update the container app
    echo -e "${YELLOW}Updating container app with new image...${NC}"
    if az containerapp update \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --image "$FULL_IMAGE_NAME" \
        --output table; then
        echo -e "${GREEN}✓ Container app updated successfully${NC}"
    else
        echo -e "${RED}✗ Failed to update container app${NC}"
        return 1
    fi

    # Wait a moment for the update to propagate
    sleep 5

    # Show revision info
    echo -e "${YELLOW}Active revisions:${NC}"
    az containerapp revision list \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "[?properties.active].{Revision:name,Created:properties.createdTime,Traffic:properties.trafficWeight,Replicas:properties.replicas}" \
        --output table

    return 0
}

# Function to check service health
check_service_health() {
    local APP_NAME=$1

    echo -e "\n${YELLOW}Checking health of ${APP_NAME}...${NC}"

    # Get the FQDN
    FQDN=$(az containerapp show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "properties.configuration.ingress.fqdn" \
        -o tsv 2>/dev/null)

    if [ -z "$FQDN" ]; then
        echo -e "${YELLOW}⚠ Could not retrieve FQDN for ${APP_NAME}${NC}"
        return 1
    fi

    echo -e "Service URL: ${GREEN}https://${FQDN}${NC}"

    # Try to ping the health endpoint
    echo -e "${YELLOW}Checking /health endpoint...${NC}"
    if curl -s -f -m 10 "https://${FQDN}/health" > /dev/null; then
        echo -e "${GREEN}✓ Health check passed${NC}"
        return 0
    else
        echo -e "${YELLOW}⚠ Health check failed or endpoint not available${NC}"
        return 1
    fi
}

# Function to show logs
show_logs() {
    local APP_NAME=$1

    echo -e "\n${YELLOW}Recent logs for ${APP_NAME}:${NC}"
    az containerapp logs show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --tail 20 \
        --follow false
}

# Update peer service
update_peer_service() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}PEER SERVICE UPDATE${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if build_and_push_service "Peer Service" "src/Services/Sorcha.Peer.Service/Dockerfile" "peer-service"; then
        if update_container_app "peer-service" "peer-service"; then
            check_service_health "peer-service"
        fi
    fi
}

# Update API gateway
update_api_gateway() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}API GATEWAY UPDATE${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if build_and_push_service "API Gateway" "src/Services/Sorcha.ApiGateway/Dockerfile" "api-gateway"; then
        if update_container_app "api-gateway" "api-gateway"; then
            check_service_health "api-gateway"
        fi
    fi
}

# Update blueprint service
update_blueprint_service() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}BLUEPRINT SERVICE UPDATE${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if build_and_push_service "Blueprint Service" "src/Services/Sorcha.Blueprint.Service/Dockerfile" "blueprint-api"; then
        if update_container_app "blueprint-api" "blueprint-api"; then
            check_service_health "blueprint-api"
        fi
    fi
}

# Update wallet service
update_wallet_service() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}WALLET SERVICE UPDATE${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if build_and_push_service "Wallet Service" "src/Services/Sorcha.Wallet.Service/Dockerfile" "wallet-service"; then
        if update_container_app "wallet-service" "wallet-service"; then
            check_service_health "wallet-service"
        fi
    fi
}

# Update register service
update_register_service() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}REGISTER SERVICE UPDATE${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if build_and_push_service "Register Service" "src/Services/Sorcha.Register.Service/Dockerfile" "register-service"; then
        if update_container_app "register-service" "register-service"; then
            check_service_health "register-service"
        fi
    fi
}

# Update tenant service
update_tenant_service() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}TENANT SERVICE UPDATE${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if build_and_push_service "Tenant Service" "src/Services/Sorcha.Tenant.Service/Dockerfile" "tenant-service"; then
        if update_container_app "tenant-service" "tenant-service"; then
            check_service_health "tenant-service"
        fi
    fi
}

# Update blazor admin
update_blazor_admin() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}BLAZOR ADMIN UPDATE${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

    if build_and_push_service "Blazor Admin" "src/Apps/Sorcha.Admin/Dockerfile" "sorcha-admin"; then
        if update_container_app "blazor-client" "sorcha-admin"; then
            check_service_health "blazor-client"
        fi
    fi
}

# Show deployment status
show_status() {
    echo -e "\n${BLUE}=========================================${NC}"
    echo -e "${BLUE}Deployment Status${NC}"
    echo -e "${BLUE}=========================================${NC}"

    az containerapp list \
        --resource-group "$RESOURCE_GROUP" \
        --query "[].{Name:name,Status:properties.runningStatus,Replicas:properties.template.scale.minReplicas,URL:properties.configuration.ingress.fqdn}" \
        --output table
}

# Main execution
main() {
    check_prerequisites

    case "${1:-all}" in
        peer)
            update_peer_service
            ;;
        gateway|api-gateway)
            update_api_gateway
            ;;
        blueprint)
            update_blueprint_service
            ;;
        wallet)
            update_wallet_service
            ;;
        register)
            update_register_service
            ;;
        tenant)
            update_tenant_service
            ;;
        blazor|admin)
            update_blazor_admin
            ;;
        peer-gateway)
            update_peer_service
            update_api_gateway
            ;;
        all)
            update_peer_service
            update_api_gateway
            update_blueprint_service
            update_wallet_service
            update_register_service
            update_tenant_service
            update_blazor_admin
            ;;
        status)
            show_status
            ;;
        logs)
            if [ -z "$2" ]; then
                echo -e "${RED}Usage: $0 logs <service-name>${NC}"
                exit 1
            fi
            show_logs "$2"
            ;;
        *)
            echo -e "${YELLOW}Usage: $0 {peer|gateway|blueprint|wallet|register|tenant|blazor|peer-gateway|all|status|logs <service>}${NC}"
            echo ""
            echo -e "${BLUE}Commands:${NC}"
            echo -e "  ${GREEN}peer${NC}           - Update Peer Service only"
            echo -e "  ${GREEN}gateway${NC}        - Update API Gateway only"
            echo -e "  ${GREEN}blueprint${NC}      - Update Blueprint Service only"
            echo -e "  ${GREEN}wallet${NC}         - Update Wallet Service only"
            echo -e "  ${GREEN}register${NC}       - Update Register Service only"
            echo -e "  ${GREEN}tenant${NC}         - Update Tenant Service only"
            echo -e "  ${GREEN}blazor${NC}         - Update Blazor Admin only"
            echo -e "  ${GREEN}peer-gateway${NC}   - Update Peer Service and API Gateway"
            echo -e "  ${GREEN}all${NC}            - Update all services"
            echo -e "  ${GREEN}status${NC}         - Show deployment status"
            echo -e "  ${GREEN}logs <service>${NC} - Show logs for a service"
            exit 1
            ;;
    esac

    echo -e "\n${BLUE}=========================================${NC}"
    echo -e "${GREEN}✓ Update completed!${NC}"
    echo -e "${BLUE}=========================================${NC}"

    show_status
}

# Run main function
main "$@"
