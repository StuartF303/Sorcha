#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

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

# Parse command line arguments
CLEAN=false
SKIP_BUILD=false
VERIFY=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --clean)
            CLEAN=true
            shift
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --verify)
            VERIFY=true
            shift
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Usage: $0 [--clean] [--skip-build] [--verify]"
            exit 1
            ;;
    esac
done

# Check if running in repository root
if [[ ! -f "docker-compose.yml" ]]; then
    print_error "This script must be run from the repository root directory"
    exit 1
fi

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Sorcha Wallet Service - Docker Encryption Setup"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# Step 1: Verify Docker installation
print_step "Verifying Docker installation..."
if command -v docker &> /dev/null; then
    docker_version=$(docker --version)
    print_success "Docker installed: $docker_version"
else
    print_error "Docker is not installed or not in PATH"
    print_info "Please install Docker from: https://docs.docker.com/get-docker/"
    exit 1
fi

# Step 2: Verify Docker Compose installation
print_step "Verifying Docker Compose installation..."
if docker compose version &> /dev/null; then
    compose_version=$(docker compose version)
    print_success "Docker Compose installed: $compose_version"
else
    print_error "Docker Compose is not installed or not available"
    exit 1
fi

# Step 3: Verify Docker daemon is running
print_step "Verifying Docker daemon is running..."
if docker info &> /dev/null; then
    print_success "Docker daemon is running"
else
    print_error "Docker daemon is not running"
    print_info "Please start Docker and try again"
    exit 1
fi

# If only verification requested, stop here
if [[ "$VERIFY" == "true" ]]; then
    echo ""
    print_step "Verifying current encryption setup..."

    # Check if volume exists
    if docker volume ls --format "{{.Name}}" | grep -q "wallet-encryption-keys"; then
        print_success "Volume 'wallet-encryption-keys' exists"

        # Check if wallet service is running
        if docker ps --format "{{.Names}}" | grep -q "sorcha-wallet-service"; then
            print_success "Wallet service container is running"

            # Check encryption provider logs
            print_step "Checking encryption provider initialization..."
            if docker logs sorcha-wallet-service 2>&1 | grep -q "Encryption provider initialized"; then
                print_success "Encryption provider initialized successfully"
            else
                print_warning "No encryption provider initialization log found"
            fi

            # Health check
            print_step "Performing health check..."
            if curl -s http://localhost:8080/health > /dev/null 2>&1; then
                health_status=$(curl -s http://localhost:8080/health | grep -o '"status":"[^"]*"' || echo "unknown")
                print_success "Health check passed: $health_status"
            else
                print_warning "Health check endpoint not accessible (service may not be fully started)"
            fi
        else
            print_warning "Wallet service container is not running"
            print_info "Start services with: docker compose up -d wallet-service"
        fi
    else
        print_warning "Volume 'wallet-encryption-keys' does not exist"
        print_info "Run this script without --verify to set up encryption"
    fi

    echo ""
    print_success "Verification complete"
    exit 0
fi

# Step 4: Handle clean option (WARNING: Destructive)
if [[ "$CLEAN" == "true" ]]; then
    echo ""
    print_warning "═══════════════════════════════════════════════════════════════"
    print_warning "  DESTRUCTIVE OPERATION WARNING"
    print_warning "═══════════════════════════════════════════════════════════════"
    print_warning "This will DELETE the existing wallet-encryption-keys volume."
    print_warning "All encrypted wallet keys will become PERMANENTLY INACCESSIBLE."
    print_warning "This operation CANNOT be undone unless you have a backup."
    echo ""

    read -p "Type 'DELETE-KEYS' to confirm (or press Enter to cancel): " confirmation

    if [[ "$confirmation" != "DELETE-KEYS" ]]; then
        print_info "Operation cancelled"
        exit 0
    fi

    print_step "Stopping wallet service..."
    docker compose stop wallet-service
    print_success "Wallet service stopped"

    print_step "Removing wallet-encryption-keys volume..."
    docker volume rm wallet-encryption-keys 2>&1 || print_info "Volume did not exist or already removed"
    print_success "Volume removed"
fi

# Step 5: Create encryption keys volume
print_step "Creating encryption keys volume..."
if docker volume ls --format "{{.Name}}" | grep -q "wallet-encryption-keys"; then
    print_success "Volume 'wallet-encryption-keys' already exists"
else
    docker volume create wallet-encryption-keys > /dev/null
    print_success "Volume 'wallet-encryption-keys' created"
fi

# Step 6: Build Wallet Service image (unless skipped)
if [[ "$SKIP_BUILD" == "false" ]]; then
    print_step "Building Wallet Service Docker image..."
    print_info "This may take several minutes on first build..."

    if docker compose build wallet-service; then
        print_success "Wallet Service image built successfully"
    else
        print_error "Docker build failed"
        exit 1
    fi
else
    print_info "Skipping Docker image build (using existing image)"
fi

# Step 7: Start infrastructure dependencies
print_step "Starting infrastructure dependencies (PostgreSQL, Redis)..."
docker compose up -d postgres redis

print_info "Waiting for PostgreSQL to be healthy..."
max_wait=60
waited=0
while [[ $waited -lt $max_wait ]]; do
    if [[ "$(docker inspect --format='{{.State.Health.Status}}' sorcha-postgres 2>/dev/null)" == "healthy" ]]; then
        break
    fi
    echo -n "."
    sleep 2
    waited=$((waited + 2))
done
echo ""

if [[ $waited -ge $max_wait ]]; then
    print_warning "PostgreSQL health check timed out (may still be starting)"
else
    print_success "PostgreSQL is healthy"
fi

# Step 8: Start Wallet Service
print_step "Starting Wallet Service..."
docker compose up -d wallet-service

# Step 9: Wait for service to initialize
print_info "Waiting for Wallet Service to initialize..."
sleep 5

# Step 10: Verify encryption provider initialization
print_step "Verifying encryption provider initialization..."
if docker logs sorcha-wallet-service 2>&1 | grep -q "Encryption provider initialized\|Linux Secret Service\|FallbackKeyStorePath"; then
    print_success "Encryption provider initialized"
    echo ""
    print_info "Initialization logs:"
    docker logs sorcha-wallet-service 2>&1 | grep "Encryption provider initialized\|Linux Secret Service\|FallbackKeyStorePath" | sed 's/^/  /'
else
    print_warning "No encryption provider initialization logs found yet"
    print_info "Check logs manually: docker logs sorcha-wallet-service"
fi

# Step 11: Verify key directory in container
print_step "Verifying encryption key directory in container..."
if docker exec sorcha-wallet-service ls -la /var/lib/sorcha/wallet-keys 2>&1 > /dev/null; then
    print_success "Encryption key directory exists in container"
else
    print_warning "Could not verify encryption key directory"
fi

# Step 12: Create backup script
print_step "Creating backup script..."
cat > scripts/backup-wallet-encryption-keys.sh << 'EOF'
#!/bin/bash
# Wallet Encryption Keys Backup Script
# Generated by setup-wallet-encryption-docker.sh

timestamp=$(date +%Y%m%d-%H%M%S)
backup_dir="./backups/wallet-keys"
backup_file="wallet-keys-$timestamp.tar.gz"

# Create backup directory
mkdir -p "$backup_dir"

# Backup encryption keys
echo "Backing up wallet encryption keys..."
docker run --rm \
  -v wallet-encryption-keys:/source:ro \
  -v "$PWD/$backup_dir:/backup" \
  alpine \
  tar czf /backup/$backup_file -C /source .

if [[ $? -eq 0 ]]; then
    echo "✓ Backup created: $backup_dir/$backup_file"

    # Show backup size
    size=$(stat -f%z "$backup_dir/$backup_file" 2>/dev/null || stat -c%s "$backup_dir/$backup_file" 2>/dev/null)
    echo "  Size: $((size / 1024)) KB"
else
    echo "✗ Backup failed"
    exit 1
fi
EOF

chmod +x scripts/backup-wallet-encryption-keys.sh
print_success "Backup script created: scripts/backup-wallet-encryption-keys.sh"

# Step 13: Create restore script
print_step "Creating restore script..."
cat > scripts/restore-wallet-encryption-keys.sh << 'EOF'
#!/bin/bash
# Wallet Encryption Keys Restore Script
# Generated by setup-wallet-encryption-docker.sh

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <backup-file>"
    exit 1
fi

backup_file="$1"

if [[ ! -f "$backup_file" ]]; then
    echo "✗ Backup file not found: $backup_file"
    exit 1
fi

echo "⚠ WARNING: This will replace all existing encryption keys"
echo "  Backup file: $backup_file"
echo ""
read -p "Type 'RESTORE' to confirm: " confirm

if [[ "$confirm" != "RESTORE" ]]; then
    echo "Restore cancelled"
    exit 0
fi

# Stop wallet service
echo "Stopping wallet service..."
docker compose stop wallet-service

# Restore keys
echo "Restoring encryption keys..."
docker run --rm \
  -v wallet-encryption-keys:/target \
  -v "$(dirname "$backup_file"):/backup:ro" \
  alpine \
  tar xzf /backup/$(basename "$backup_file") -C /target

if [[ $? -eq 0 ]]; then
    echo "✓ Restore complete"

    # Restart wallet service
    echo "Restarting wallet service..."
    docker compose start wallet-service

    echo "✓ Wallet service restarted"
else
    echo "✗ Restore failed"
    exit 1
fi
EOF

chmod +x scripts/restore-wallet-encryption-keys.sh
print_success "Restore script created: scripts/restore-wallet-encryption-keys.sh"

# Step 14: Summary
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "  Setup Complete!"
echo "═══════════════════════════════════════════════════════════════"
echo ""
print_success "Wallet Service encryption configured for Docker"
echo ""
print_info "Configuration:"
echo "  • Encryption Provider: LinuxSecretService (fallback mode)"
echo "  • Algorithm: AES-256-GCM (two-layer envelope encryption)"
echo "  • Key Storage: Docker volume 'wallet-encryption-keys'"
echo "  • Volume Mount: /var/lib/sorcha/wallet-keys"
echo ""
print_info "Next Steps:"
echo "  1. Verify service health:"
echo "     docker logs sorcha-wallet-service"
echo ""
echo "  2. Test encryption endpoint:"
echo "     curl http://localhost:8080/health"
echo ""
echo "  3. Create first backup:"
echo "     ./scripts/backup-wallet-encryption-keys.sh"
echo ""
echo "  4. Review encryption architecture:"
echo "     docs/wallet-encryption-architecture.md"
echo ""
print_warning "IMPORTANT: Schedule regular backups of encryption keys!"
print_warning "Without backups, losing the volume means PERMANENT DATA LOSS."
echo ""
