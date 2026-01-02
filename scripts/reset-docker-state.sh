#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# Sorcha Docker State Reset Utility
# Resets Docker state by clearing all containers and database volumes

set -euo pipefail

# Script configuration
COMPOSE_PROJECT="sorcha"
COMPOSE_FILE="docker-compose.yml"

# Parse command line arguments
YES_FLAG=false
KEEP_VOLUMES=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -y|--yes)
            YES_FLAG=true
            shift
            ;;
        --keep-volumes)
            KEEP_VOLUMES=true
            shift
            ;;
        -h|--help)
            cat << EOF
Sorcha Docker State Reset Utility

Usage: $0 [OPTIONS]

This script resets the Docker state for Sorcha project by:
  1. Checking if Docker is running and healthy
  2. Prompting for confirmation (unless -y is specified)
  3. Stopping all Sorcha containers
  4. Removing all Sorcha containers
  5. Removing all database volumes (PostgreSQL, MongoDB, Redis)
  6. Cleaning up data protection keys
  7. Providing a fresh starting state for bootstrap

Options:
  -y, --yes           Skip confirmation prompt (useful for CI/CD)
  --keep-volumes      Keep database volumes (only remove containers)
  -h, --help          Show this help message

Examples:
  $0                  # Prompts for confirmation
  $0 -y               # Resets without confirmation
  $0 --keep-volumes   # Only removes containers

EOF
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# Color output functions
print_success() {
    echo -e "\033[0;32m✓ $1\033[0m"
}

print_info() {
    echo -e "\033[0;36mℹ $1\033[0m"
}

print_warning() {
    echo -e "\033[0;33m⚠ $1\033[0m"
}

print_error() {
    echo -e "\033[0;31m✗ $1\033[0m"
}

print_step() {
    echo -e "\n\033[0;35m==> $1\033[0m"
}

# Check if Docker is installed and running
check_docker() {
    print_step "Checking Docker status..."

    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        print_info "Please install Docker from https://www.docker.com/get-started"
        exit 1
    fi

    if ! docker version &> /dev/null; then
        print_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi

    print_success "Docker is running"
}

# Check Docker daemon health
check_docker_health() {
    print_step "Checking Docker daemon health..."

    if docker info &> /dev/null; then
        local version=$(docker version --format '{{.Server.Version}}' 2>/dev/null || echo "unknown")
        print_success "Docker daemon is healthy (version: $version)"
        return 0
    else
        print_warning "Docker daemon may not be fully ready"
        return 1
    fi
}

# Get list of Sorcha containers
get_sorcha_containers() {
    docker ps -a --filter "name=sorcha-" --format "{{.Names}}" 2>/dev/null || true
}

# Get list of Sorcha volumes
get_sorcha_volumes() {
    docker volume ls --filter "name=${COMPOSE_PROJECT}_" --format "{{.Name}}" 2>/dev/null || true
}

# Display current state
show_current_state() {
    print_step "Current Sorcha Docker state:"

    local containers=$(get_sorcha_containers)
    local volumes=$(get_sorcha_volumes)

    if [ -n "$containers" ]; then
        local count=$(echo "$containers" | wc -l)
        print_info "Containers ($count):"
        while IFS= read -r container; do
            local status=$(docker inspect --format "{{.State.Status}}" "$container" 2>/dev/null || echo "unknown")
            echo -e "  \033[0;37m- $container ($status)\033[0m"
        done <<< "$containers"
    else
        print_info "No Sorcha containers found"
    fi

    if [ -n "$volumes" ]; then
        local count=$(echo "$volumes" | wc -l)
        print_info "\nVolumes ($count):"
        while IFS= read -r volume; do
            echo -e "  \033[0;37m- $volume\033[0m"
        done <<< "$volumes"
    else
        print_info "No Sorcha volumes found"
    fi
}

# Confirm action
confirm_reset() {
    if [ "$YES_FLAG" = true ]; then
        print_info "Auto-confirmed with -y flag"
        return 0
    fi

    echo ""
    print_warning "This will:"
    echo -e "\033[0;33m  1. Stop all Sorcha containers\033[0m"
    echo -e "\033[0;33m  2. Remove all Sorcha containers\033[0m"

    if [ "$KEEP_VOLUMES" = false ]; then
        echo -e "\033[0;33m  3. Delete all database volumes (PostgreSQL, MongoDB, Redis)\033[0m"
        echo -e "\033[0;33m  4. Delete data protection keys\033[0m"
        echo -e "\n  \033[0;31m⚠️  ALL DATABASE DATA WILL BE LOST!\033[0m"
    else
        echo -e "\033[0;32m  3. Keep database volumes (as requested)\033[0m"
    fi

    echo ""
    read -p "Are you sure you want to continue? (yes/no): " response

    if [[ "$response" == "yes" || "$response" == "y" ]]; then
        return 0
    else
        return 1
    fi
}

# Stop all Sorcha containers
stop_sorcha_containers() {
    print_step "Stopping Sorcha containers..."

    local containers=$(get_sorcha_containers)
    if [ -z "$containers" ]; then
        print_info "No containers to stop"
        return
    fi

    while IFS= read -r container; do
        echo -e "  \033[0;37mStopping $container...\033[0m"
        if docker stop "$container" &> /dev/null; then
            print_success "Stopped $container"
        else
            print_warning "Failed to stop $container (may already be stopped)"
        fi
    done <<< "$containers"
}

# Remove all Sorcha containers
remove_sorcha_containers() {
    print_step "Removing Sorcha containers..."

    local containers=$(get_sorcha_containers)
    if [ -z "$containers" ]; then
        print_info "No containers to remove"
        return
    fi

    while IFS= read -r container; do
        echo -e "  \033[0;37mRemoving $container...\033[0m"
        if docker rm -f "$container" &> /dev/null; then
            print_success "Removed $container"
        else
            print_warning "Failed to remove $container"
        fi
    done <<< "$containers"
}

# Remove all Sorcha volumes
remove_sorcha_volumes() {
    if [ "$KEEP_VOLUMES" = true ]; then
        print_step "Skipping volume removal (--keep-volumes flag set)"
        return
    fi

    print_step "Removing Sorcha volumes..."

    local volumes=$(get_sorcha_volumes)
    if [ -z "$volumes" ]; then
        print_info "No volumes to remove"
        return
    fi

    while IFS= read -r volume; do
        echo -e "  \033[0;37mRemoving $volume...\033[0m"
        if docker volume rm "$volume" &> /dev/null; then
            print_success "Removed $volume"
        else
            print_warning "Failed to remove $volume (may be in use)"
        fi
    done <<< "$volumes"
}

# Verify cleanup
verify_clean_state() {
    print_step "Verifying clean state..."

    local containers=$(get_sorcha_containers)
    local volumes=$(get_sorcha_volumes)
    local success=true

    if [ -n "$containers" ]; then
        print_warning "Some containers still exist:"
        while IFS= read -r container; do
            echo -e "  \033[0;33m- $container\033[0m"
        done <<< "$containers"
        success=false
    else
        print_success "All containers removed"
    fi

    if [ "$KEEP_VOLUMES" = false ]; then
        if [ -n "$volumes" ]; then
            print_warning "Some volumes still exist:"
            while IFS= read -r volume; do
                echo -e "  \033[0;33m- $volume\033[0m"
            done <<< "$volumes"
            success=false
        else
            print_success "All volumes removed"
        fi
    fi

    if [ "$success" = true ]; then
        return 0
    else
        return 1
    fi
}

# Main execution
main() {
    cat << "EOF"
╔═══════════════════════════════════════════════════════════╗
║         Sorcha Docker State Reset Utility                 ║
║         Clean slate for fresh bootstrap                   ║
╚═══════════════════════════════════════════════════════════╝
EOF

    # Pre-flight checks
    check_docker
    check_docker_health

    # Show current state
    show_current_state

    # Confirm action
    if ! confirm_reset; then
        print_warning "Reset cancelled by user"
        exit 0
    fi

    # Execute reset
    echo ""
    stop_sorcha_containers
    remove_sorcha_containers
    remove_sorcha_volumes

    # Verify
    echo ""
    if verify_clean_state; then
        echo ""
        print_success "════════════════════════════════════════════════════"
        print_success "  Docker state reset complete!"
        print_success "  Ready for fresh bootstrap"
        print_success "════════════════════════════════════════════════════"
        echo ""
        print_info "Next steps:"
        echo -e "  \033[1;37m1. Run: docker-compose up -d\033[0m"
        echo -e "  \033[1;37m2. Run: ./scripts/bootstrap-sorcha.sh\033[0m"
        echo ""
    else
        echo ""
        print_warning "════════════════════════════════════════════════════"
        print_warning "  Reset completed with warnings"
        print_warning "  Some resources may still exist"
        print_warning "════════════════════════════════════════════════════"
        echo ""
        print_info "You may need to manually clean up remaining resources"
        exit 1
    fi
}

# Run main function
main
