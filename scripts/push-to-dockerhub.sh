#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Default values
TAG="latest"
SKIP_BUILD=false
DRY_RUN=false
DOCKERHUB_USER=""

# Service definitions
declare -A SERVICE_MAP=(
    ["blueprint"]="blueprint-service"
    ["wallet"]="wallet-service"
    ["register"]="register-service"
    ["tenant"]="tenant-service"
    ["peer"]="peer-service"
    ["validator"]="validator-service"
    ["gateway"]="api-gateway"
)

# Usage function
usage() {
    cat << EOF
Usage: $0 -u <dockerhub-user> [OPTIONS]

Build and push Sorcha Docker images to DockerHub

Required:
    -u, --user <username>       DockerHub username or organization

Options:
    -t, --tag <tag>            Version tag for images (default: latest)
    -s, --services <list>      Comma-separated list of services to push
                               Valid: blueprint,wallet,register,tenant,peer,validator,gateway
                               Default: all services
    --skip-build               Skip building images, only tag and push existing local images
    --dry-run                  Show what would be done without actually pushing
    -h, --help                 Show this help message

Examples:
    # Push all services with 'latest' tag
    $0 -u myusername

    # Push all services with version tag
    $0 -u myorg -t v1.0.0

    # Push only specific services
    $0 -u myusername -s blueprint,wallet --skip-build

    # Dry run to see what would happen
    $0 -u myusername --dry-run

EOF
    exit 1
}

# Parse arguments
SERVICES=()
while [[ $# -gt 0 ]]; do
    case $1 in
        -u|--user)
            DOCKERHUB_USER="$2"
            shift 2
            ;;
        -t|--tag)
            TAG="$2"
            shift 2
            ;;
        -s|--services)
            IFS=',' read -ra SERVICES <<< "$2"
            shift 2
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        -h|--help)
            usage
            ;;
        *)
            echo -e "${RED}❌ Unknown option: $1${NC}"
            usage
            ;;
    esac
done

# Validate required parameters
if [[ -z "$DOCKERHUB_USER" ]]; then
    echo -e "${RED}❌ Error: DockerHub username is required${NC}"
    usage
fi

# Default to all services if none specified
if [[ ${#SERVICES[@]} -eq 0 ]]; then
    SERVICES=("${!SERVICE_MAP[@]}")
fi

# Print configuration
echo -e "${CYAN}========================================"
echo "Sorcha Docker Image Push Script"
echo -e "========================================${NC}"
echo ""
echo -e "${YELLOW}DockerHub User:${NC} $DOCKERHUB_USER"
echo -e "${YELLOW}Tag:${NC} $TAG"
echo -e "${YELLOW}Services:${NC} ${SERVICES[*]}"
echo -e "${YELLOW}Skip Build:${NC} $SKIP_BUILD"
echo -e "${YELLOW}Dry Run:${NC} $DRY_RUN"
echo ""

# Check if Docker is running
if ! docker version &> /dev/null; then
    echo -e "${RED}❌ Error: Docker is not running or not installed${NC}"
    exit 1
fi

# Check DockerHub authentication
echo -e "${CYAN}Checking DockerHub authentication...${NC}"
if ! docker info 2>&1 | grep -q "Username"; then
    echo -e "${YELLOW}⚠️  You are not logged into DockerHub${NC}"
    echo -e "${YELLOW}Please run: docker login${NC}"

    if [[ "$DRY_RUN" != "true" ]]; then
        read -p "Would you like to login now? (y/n) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            docker login || {
                echo -e "${RED}❌ Docker login failed${NC}"
                exit 1
            }
        else
            echo -e "${RED}❌ Cannot proceed without authentication${NC}"
            exit 1
        fi
    fi
else
    echo -e "${GREEN}✅ Already authenticated with DockerHub${NC}"
fi

echo ""

# Function to push a service
push_service() {
    local service_key=$1
    local service_name=${SERVICE_MAP[$service_key]}

    if [[ -z "$service_name" ]]; then
        echo -e "${RED}❌ Unknown service: $service_key${NC}"
        return 1
    fi

    echo -e "${CYAN}========================================"
    echo "Processing: $service_name"
    echo -e "========================================${NC}"

    local local_image="sorcha/${service_name}:latest"
    local remote_image="${DOCKERHUB_USER}/${service_name}:${TAG}"

    # Build the image
    if [[ "$SKIP_BUILD" != "true" ]]; then
        echo -e "${YELLOW}🔨 Building image: $local_image${NC}"

        if [[ "$DRY_RUN" == "true" ]]; then
            echo -e "${GRAY}   [DRY RUN] Would execute: docker compose build $service_name${NC}"
        else
            if ! docker compose build "$service_name"; then
                echo -e "${RED}❌ Failed to build $service_name${NC}"
                return 1
            fi
            echo -e "${GREEN}✅ Build successful${NC}"
        fi
    else
        echo -e "${YELLOW}⏭️  Skipping build (using existing local image)${NC}"

        # Check if local image exists
        if [[ "$DRY_RUN" != "true" ]] && ! docker images -q "$local_image" &> /dev/null; then
            echo -e "${RED}❌ Local image not found: $local_image${NC}"
            echo -e "${YELLOW}   Run without --skip-build to build the image first${NC}"
            return 1
        fi
    fi

    # Tag the image
    echo -e "${YELLOW}🏷️  Tagging image: $local_image -> $remote_image${NC}"

    if [[ "$DRY_RUN" == "true" ]]; then
        echo -e "${GRAY}   [DRY RUN] Would execute: docker tag $local_image $remote_image${NC}"
    else
        if ! docker tag "$local_image" "$remote_image"; then
            echo -e "${RED}❌ Failed to tag $service_name${NC}"
            return 1
        fi
        echo -e "${GREEN}✅ Tagged successfully${NC}"
    fi

    # Push to DockerHub
    echo -e "${YELLOW}📤 Pushing image: $remote_image${NC}"

    if [[ "$DRY_RUN" == "true" ]]; then
        echo -e "${GRAY}   [DRY RUN] Would execute: docker push $remote_image${NC}"
    else
        if ! docker push "$remote_image"; then
            echo -e "${RED}❌ Failed to push $service_name${NC}"
            return 1
        fi
        echo -e "${GREEN}✅ Pushed successfully${NC}"
    fi

    # Also tag and push as 'latest' if using a version tag
    if [[ "$TAG" != "latest" ]]; then
        local latest_image="${DOCKERHUB_USER}/${service_name}:latest"

        echo -e "${YELLOW}🏷️  Also tagging as latest: $latest_image${NC}"

        if [[ "$DRY_RUN" == "true" ]]; then
            echo -e "${GRAY}   [DRY RUN] Would execute: docker tag $local_image $latest_image${NC}"
            echo -e "${GRAY}   [DRY RUN] Would execute: docker push $latest_image${NC}"
        else
            docker tag "$local_image" "$latest_image"
            if docker push "$latest_image"; then
                echo -e "${GREEN}✅ Latest tag pushed successfully${NC}"
            else
                echo -e "${YELLOW}⚠️  Warning: Failed to push latest tag${NC}"
            fi
        fi
    fi

    echo ""
    return 0
}

# Process each service
SUCCESS_COUNT=0
FAILURE_COUNT=0
declare -a RESULTS=()

for service_key in "${SERVICES[@]}"; do
    if push_service "$service_key"; then
        ((SUCCESS_COUNT++))
        RESULTS+=("${SERVICE_MAP[$service_key]}|✅ Success|${DOCKERHUB_USER}/${SERVICE_MAP[$service_key]}:${TAG}")
    else
        ((FAILURE_COUNT++))
        RESULTS+=("${SERVICE_MAP[$service_key]}|❌ Failed|${DOCKERHUB_USER}/${SERVICE_MAP[$service_key]}:${TAG}")
    fi
done

# Summary
echo -e "${CYAN}========================================"
echo "Summary"
echo -e "========================================${NC}"
echo ""

printf "%-25s %-15s %-50s\n" "Service" "Status" "Image"
printf "%-25s %-15s %-50s\n" "-------" "------" "-----"
for result in "${RESULTS[@]}"; do
    IFS='|' read -ra PARTS <<< "$result"
    printf "%-25s %-15s %-50s\n" "${PARTS[0]}" "${PARTS[1]}" "${PARTS[2]}"
done

echo ""
echo -e "${NC}Total Services: ${#SERVICES[@]}"
echo -e "${GREEN}Successful: $SUCCESS_COUNT${NC}"
if [[ $FAILURE_COUNT -gt 0 ]]; then
    echo -e "${RED}Failed: $FAILURE_COUNT${NC}"
else
    echo -e "${GREEN}Failed: $FAILURE_COUNT${NC}"
fi

if [[ "$DRY_RUN" == "true" ]]; then
    echo ""
    echo -e "${CYAN}ℹ️  This was a dry run. No images were actually pushed.${NC}"
    echo -e "${CYAN}   Remove --dry-run to push for real.${NC}"
fi

echo ""

if [[ $FAILURE_COUNT -eq 0 ]]; then
    echo -e "${GREEN}✅ All images pushed successfully!${NC}"

    if [[ "$DRY_RUN" != "true" ]]; then
        echo ""
        echo -e "${CYAN}Your images are now available at:${NC}"
        for result in "${RESULTS[@]}"; do
            IFS='|' read -ra PARTS <<< "$result"
            local repo="${PARTS[2]%%:*}"
            echo -e "${BLUE}  https://hub.docker.com/r/$repo${NC}"
        done
    fi

    exit 0
else
    echo -e "${RED}❌ Some images failed to push${NC}"
    exit 1
fi
