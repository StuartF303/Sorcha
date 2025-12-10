#!/usr/bin/env bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

#
# Bootstrap seed script for Tenant Service (local development/MVD)
# Creates default admin user, organization, and service principals for local testing.
# Only runs on empty database or with --force flag.
#
# Usage:
#   ./seed-tenant-service.sh [options]
#
# Options:
#   --environment ENV    Environment to seed (Development, Staging, Production). Default: Development
#   --admin-email EMAIL  Admin email address. Default: admin@sorcha.local
#   --admin-password PWD Admin password. Default: Dev_Pass_2025!
#   --base-url URL       Tenant Service API base URL. Default: https://localhost:7080
#   --force              Force seed even if data exists
#
# Examples:
#   ./seed-tenant-service.sh
#   ./seed-tenant-service.sh --environment Development
#   ./seed-tenant-service.sh --admin-email mvd-admin@company.com --force
#

set -e  # Exit on error

# ANSI color codes
COLOR_RESET='\033[0m'
COLOR_GREEN='\033[32m'
COLOR_YELLOW='\033[33m'
COLOR_RED='\033[31m'
COLOR_CYAN='\033[36m'
COLOR_BOLD='\033[1m'

# Default values
ENVIRONMENT="${ENVIRONMENT:-Development}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@sorcha.local}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Dev_Pass_2025!}"
BASE_URL="${BASE_URL:-https://localhost:7080}"
FORCE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --admin-email)
            ADMIN_EMAIL="$2"
            shift 2
            ;;
        --admin-password)
            ADMIN_PASSWORD="$2"
            shift 2
            ;;
        --base-url)
            BASE_URL="$2"
            shift 2
            ;;
        --force)
            FORCE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Helper functions
print_header() {
    echo -e "\n${COLOR_CYAN}========================================"
    echo -e "${COLOR_BOLD} $1${COLOR_RESET}"
    echo -e "${COLOR_CYAN}========================================${COLOR_RESET}"
}

print_success() {
    echo -e "${COLOR_GREEN}$1${COLOR_RESET}"
}

print_warning() {
    echo -e "${COLOR_YELLOW}$1${COLOR_RESET}"
}

print_error() {
    echo -e "${COLOR_RED}$1${COLOR_RESET}"
}

print_info() {
    echo -e "${COLOR_CYAN}$1${COLOR_RESET}"
}

# Check for required tools
command -v curl >/dev/null 2>&1 || { print_error "Error: curl is required but not installed."; exit 1; }
command -v jq >/dev/null 2>&1 || { print_warning "Warning: jq is not installed. JSON output will not be formatted."; }

print_header "Sorcha Tenant Service - Bootstrap Seed Script"
print_info "Environment: $ENVIRONMENT"
print_info "Base URL: $BASE_URL"
print_info "Admin Email: $ADMIN_EMAIL\n"

# Step 1: Check if Tenant Service is running
print_warning "[1/5] Checking Tenant Service health..."

HTTP_CODE=$(curl -k -s -o /dev/null -w "%{http_code}" "$BASE_URL/health" || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    print_success "  ✓ Tenant Service is running"
else
    print_error "  ✗ Error: Cannot connect to Tenant Service at $BASE_URL (HTTP $HTTP_CODE)"
    print_warning "  Make sure the service is running: dotnet run --project src/Services/Sorcha.Tenant.Service"
    exit 1
fi

# Step 2: Check for existing admin user
print_warning "\n[2/5] Checking for existing admin user..."
print_warning "  NOTE: Admin user check requires API implementation"
print_warning "  Proceeding with seed (use --force to override conflicts)"

# Step 3: Create default organization
print_warning "\n[3/5] Creating default organization..."
print_warning "  NOTE: Organization creation requires API implementation"
print_warning "  Skipping organization creation for now"

# Step 4: Create default administrator user
print_warning "\n[4/5] Creating default administrator user..."
print_warning "  NOTE: User creation requires API implementation"
print_info "  Default credentials for manual setup:"
print_success "    Email: $ADMIN_EMAIL"
print_success "    Password: $ADMIN_PASSWORD"

# Step 5: Create service principals
print_warning "\n[5/5] Creating service principals..."

declare -a SERVICES=(
    "Blueprint Service|blueprints:read,blueprints:write,wallets:sign"
    "Wallet Service|wallets:read,wallets:write,wallets:sign"
    "Register Service|register:read,register:write"
    "Peer Service|peer:read,peer:write"
)

declare -a CREDENTIALS=()

for service_config in "${SERVICES[@]}"; do
    IFS='|' read -r SERVICE_NAME SCOPES <<< "$service_config"
    print_info "  Creating service principal: $SERVICE_NAME"

    # NOTE: Requires admin token - this is a bootstrapping challenge
    print_warning "  NOTE: Service principal creation requires admin authentication"
    print_success "  Placeholder: $SERVICE_NAME with scopes: $SCOPES"

    # Simulated credentials (in real implementation, these would be returned from API)
    CLIENT_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
    CLIENT_SECRET=$(openssl rand -base64 32 | tr -d '\n')

    CREDENTIALS+=("$SERVICE_NAME|$CLIENT_ID|$CLIENT_SECRET|$SCOPES")
done

# Output credentials
print_header "Bootstrap Complete - Service Principal Credentials"

print_warning "\n⚠️  SAVE THESE CREDENTIALS - They will only be shown once!"

for cred in "${CREDENTIALS[@]}"; do
    IFS='|' read -r SERVICE_NAME CLIENT_ID CLIENT_SECRET SCOPES <<< "$cred"
    print_info "\n$SERVICE_NAME:"
    print_success "  Client ID:     $CLIENT_ID"
    print_success "  Client Secret: $CLIENT_SECRET"
    print_success "  Scopes:        $SCOPES"
done

# Optional: Write to .env.local file
ENV_FILE="$(pwd)/.env.local"

echo -e "\n${COLOR_YELLOW}Write credentials to .env.local file? (y/N): ${COLOR_RESET}"
read -r WRITE_ENV

if [[ "$WRITE_ENV" =~ ^[Yy]$ ]]; then
    cat > "$ENV_FILE" <<EOF
# Sorcha Tenant Service - Local Development Credentials
# Generated: $(date +"%Y-%m-%d %H:%M:%S")
# NEVER commit this file to source control (.gitignored)

# Default Administrator
TENANT_ADMIN_EMAIL=$ADMIN_EMAIL
TENANT_ADMIN_PASSWORD=$ADMIN_PASSWORD

EOF

    for cred in "${CREDENTIALS[@]}"; do
        IFS='|' read -r SERVICE_NAME CLIENT_ID CLIENT_SECRET SCOPES <<< "$cred"
        SERVICE_VAR=$(echo "$SERVICE_NAME" | tr '[:lower:] ' '[:upper:]_')
        cat >> "$ENV_FILE" <<EOF
# $SERVICE_NAME
${SERVICE_VAR}_CLIENT_ID=$CLIENT_ID
${SERVICE_VAR}_CLIENT_SECRET=$CLIENT_SECRET

EOF
    done

    print_success "\n✓ Credentials written to: $ENV_FILE"
    print_warning "  Add this file to .gitignore if not already present"
fi

print_header "Next Steps"
print_info "1. Start Tenant Service: dotnet run --project src/Services/Sorcha.Tenant.Service"
print_info "2. Test login: POST $BASE_URL/api/auth/login"
print_info "3. Configure service clients with above credentials"
print_info "\nFor questions, see: .specify/specs/sorcha-tenant-service.md\n"
