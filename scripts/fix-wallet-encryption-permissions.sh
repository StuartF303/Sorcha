#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# Fix Docker volume permissions for Wallet Service encryption keys
#
# This script fixes the most common issue with fresh Docker installations:
# the wallet-encryption-keys volume is created with root ownership,
# but the container runs as non-root user (UID 1654).

set -e

# Color output functions
print_step() {
    echo -e "\033[0;36m▶\033[0m $1"
}

print_success() {
    echo -e "\033[0;32m✓\033[0m $1"
}

print_warning() {
    echo -e "\033[0;33m⚠\033[0m $1"
}

print_error() {
    echo -e "\033[0;31m✗\033[0m $1"
}

print_info() {
    echo -e "\033[0;34mℹ\033[0m $1"
}

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Fix Wallet Encryption Volume Permissions"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# Verify Docker is running
print_step "Verifying Docker is running..."
if docker info &> /dev/null; then
    print_success "Docker daemon is running"
else
    print_error "Docker daemon is not running"
    print_info "Please start Docker and try again"
    exit 1
fi

# Check if volume exists
print_step "Checking for wallet-encryption-keys volume..."
if docker volume ls --format "{{.Name}}" | grep -q "wallet-encryption-keys"; then
    print_success "Volume exists"
else
    print_warning "Volume 'wallet-encryption-keys' does not exist"
    print_info "Creating volume..."
    docker volume create wallet-encryption-keys > /dev/null
    print_success "Volume created"
fi

# Check if wallet service is running
print_step "Stopping wallet service (if running)..."
service_running=false
if docker ps --format "{{.Names}}" | grep -q "sorcha-wallet-service"; then
    service_running=true
    docker compose stop wallet-service 2>&1 > /dev/null
    print_success "Wallet service stopped"
else
    print_info "Wallet service was not running"
fi

# Fix volume permissions
print_step "Fixing volume permissions for UID 1654 (non-root container user)..."
if docker run --rm -v wallet-encryption-keys:/data alpine chown -R 1654:1654 /data 2>&1; then
    print_success "Volume permissions fixed successfully"
else
    print_error "Failed to fix volume permissions"
    exit 1
fi

# Verify permissions
print_step "Verifying permissions..."
print_success "Current permissions:"
docker run --rm -v wallet-encryption-keys:/data alpine ls -la /data

# Restart wallet service if it was running before
if [[ "$service_running" == "true" ]]; then
    print_step "Restarting wallet service..."
    docker compose start wallet-service 2>&1 > /dev/null
    print_success "Wallet service restarted"

    # Wait a moment for service to initialize
    print_info "Waiting for service to initialize..."
    sleep 5

    # Check health
    print_step "Checking service health..."
    if curl -s http://localhost:8080/health > /dev/null 2>&1; then
        health_status=$(curl -s http://localhost:8080/health | grep -o '"status":"[^"]*"' || echo "unknown")
        print_success "Service health: $health_status"
    else
        print_info "Health check endpoint not yet accessible - service may still be starting"
        print_info "Check manually: docker logs sorcha-wallet-service"
    fi
fi

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Permissions Fixed!"
echo "═══════════════════════════════════════════════════════════════"
echo ""
print_info "The wallet-encryption-keys volume now has correct permissions."
print_info "The wallet service should be able to create and access encryption keys."
echo ""
