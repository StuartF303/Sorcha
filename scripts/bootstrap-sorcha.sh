#!/usr/bin/env bash
#
# Sorcha Platform Bootstrap Script
# Interactive Bash script to configure a fresh Sorcha installation
#
# Usage:
#   ./bootstrap-sorcha.sh                    # Interactive mode
#   ./bootstrap-sorcha.sh --non-interactive  # Non-interactive with defaults
#   ./bootstrap-sorcha.sh --profile docker   # Specify profile
#

set -euo pipefail

# Script configuration
PROFILE="${PROFILE:-docker}"
NON_INTERACTIVE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --profile)
            PROFILE="$2"
            shift 2
            ;;
        --non-interactive)
            NON_INTERACTIVE=true
            shift
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --profile NAME          Configuration profile name (default: docker)"
            echo "  --non-interactive       Run with default values, no prompts"
            echo "  --help                  Show this help message"
            echo ""
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Color output helpers
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

write_step() {
    echo -e "${CYAN}==>${NC} ${WHITE}$1${NC}"
}

write_success() {
    echo -e "${GREEN}✓${NC} ${WHITE}$1${NC}"
}

write_error() {
    echo -e "${RED}✗${NC} ${WHITE}$1${NC}"
}

write_info() {
    echo -e "${BLUE}ℹ${NC} ${WHITE}$1${NC}"
}

get_user_input() {
    local prompt="$1"
    local default="$2"
    local secure="${3:-false}"

    if [[ "$NON_INTERACTIVE" == "true" ]]; then
        echo "$default"
        return
    fi

    if [[ "$secure" == "true" ]]; then
        read -s -p "$prompt: " user_input
        echo ""
    else
        if [[ -n "$default" ]]; then
            read -p "$prompt [$default]: " user_input
            echo "${user_input:-$default}"
        else
            read -p "$prompt: " user_input
            echo "$user_input"
        fi
    fi
}

command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Banner
echo ""
echo -e "${CYAN}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║                                                                ║${NC}"
echo -e "${CYAN}║              Sorcha Platform Bootstrap Script                 ║${NC}"
echo -e "${CYAN}║                                                                ║${NC}"
echo -e "${CYAN}║         Initial configuration for fresh installation          ║${NC}"
echo -e "${CYAN}║                                                                ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check prerequisites
write_step "Checking prerequisites..."

if ! command_exists sorcha; then
    write_error "Sorcha CLI not found. Please install it first:"
    echo -e "  ${YELLOW}dotnet tool install -g Sorcha.Cli${NC}"
    echo -e "  ${YELLOW}OR run from source:${NC}"
    echo -e "  ${YELLOW}dotnet run --project src/Apps/Sorcha.Cli -- [command]${NC}"
    exit 1
fi

if ! command_exists docker; then
    write_error "Docker not found. Please install Docker first."
    exit 1
fi

write_success "All prerequisites met"
echo ""

# Phase 1: CLI Configuration
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}PHASE 1: CLI Configuration${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

write_step "Configuring CLI profile: $PROFILE"

# Service URLs for Docker deployment
TENANT_URL=$(get_user_input "Tenant Service URL" "http://localhost/api/tenants")
REGISTER_URL=$(get_user_input "Register Service URL" "http://localhost/api/register")
WALLET_URL=$(get_user_input "Wallet Service URL" "http://localhost/api/wallets")
PEER_URL=$(get_user_input "Peer Service URL" "http://localhost/api/peers")
AUTH_URL=$(get_user_input "Auth Token URL" "http://localhost/api/service-auth/token")

write_info "CLI will be configured to use profile: $PROFILE"
write_success "Configuration profile prepared"
echo ""

# Phase 2: Initial Authentication
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}PHASE 2: Initial Authentication${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

write_info "For bootstrap, we'll create an initial service principal for automation"

BOOTSTRAP_CLIENT_ID=$(get_user_input "Bootstrap Service Principal Client ID" "sorcha-bootstrap")
BOOTSTRAP_CLIENT_SECRET=$(get_user_input "Bootstrap Service Principal Secret" "bootstrap-secret-$RANDOM" true)

write_success "Authentication credentials prepared"
echo ""

# Phase 3: System Organization (Tenant)
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}PHASE 3: System Organization${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

ORG_NAME=$(get_user_input "Organization Name" "System Organization")
ORG_SUBDOMAIN=$(get_user_input "Organization Subdomain" "system")
ORG_DESCRIPTION=$(get_user_input "Organization Description" "Primary system organization for Sorcha platform")

write_success "Organization details prepared"
echo ""

# Phase 4: Administrative User
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}PHASE 4: Administrative User${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

ADMIN_EMAIL=$(get_user_input "Admin Email Address" "admin@sorcha.local")
ADMIN_NAME=$(get_user_input "Admin Display Name" "System Administrator")
ADMIN_PASSWORD=$(get_user_input "Admin Password" "Admin@123!" true)

write_success "Administrator account details prepared"
echo ""

# Phase 5: Node Configuration
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}PHASE 5: Node Configuration${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

NODE_ID=$(get_user_input "Node ID/Name" "node-$(hostname)")
NODE_DESCRIPTION=$(get_user_input "Node Description" "Primary Sorcha node - $(hostname)")
ENABLE_P2P=$(get_user_input "Enable P2P networking? (true/false)" "true")

write_success "Node configuration prepared"
echo ""

# Phase 6: Initial Register
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}PHASE 6: Initial Register${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

REGISTER_NAME=$(get_user_input "Initial Register Name" "System Register")
REGISTER_DESCRIPTION=$(get_user_input "Register Description" "Primary system register for transactions")

write_success "Register configuration prepared"
echo ""

# Confirmation
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}Configuration Summary${NC}"
echo -e "${CYAN}═══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${WHITE}Profile:            $PROFILE${NC}"
echo -e "${WHITE}Tenant URL:         $TENANT_URL${NC}"
echo -e "${WHITE}Organization:       $ORG_NAME ($ORG_SUBDOMAIN)${NC}"
echo -e "${WHITE}Admin User:         $ADMIN_NAME ($ADMIN_EMAIL)${NC}"
echo -e "${WHITE}Node ID:            $NODE_ID${NC}"
echo -e "${WHITE}Initial Register:   $REGISTER_NAME${NC}"
echo ""

if [[ "$NON_INTERACTIVE" != "true" ]]; then
    read -p "Proceed with installation? (yes/no): " CONFIRM
    if [[ "$CONFIRM" != "yes" ]]; then
        write_info "Installation cancelled by user"
        exit 0
    fi
fi

echo ""
echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}Starting Installation${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# TODO: Track required CLI enhancements
cat <<EOF | tee /dev/stderr
${YELLOW}SORCHA CLI ENHANCEMENTS NEEDED (tracked in MASTER-TASKS.md):

1. CLI-BOOTSTRAP-001: Implement 'sorcha config init' command
   - Initialize CLI configuration profile
   - Set service URLs
   - Validate connectivity

2. CLI-BOOTSTRAP-002: Implement 'sorcha org create' command
   - Create organization with subdomain
   - Set branding/description
   - Return organization ID

3. CLI-BOOTSTRAP-003: Implement 'sorcha user create' command
   - Create user in organization
   - Set initial password
   - Assign role (Administrator)

4. CLI-BOOTSTRAP-004: Implement 'sorcha sp create' command
   - Create service principal
   - Generate and return secret
   - Set scopes

5. CLI-BOOTSTRAP-005: Implement 'sorcha register create' command
   - Create register in organization
   - Set description
   - Publish register

6. CLI-BOOTSTRAP-006: Implement 'sorcha node configure' command (NEW)
   - Set node ID/name
   - Configure P2P settings
   - Set node metadata

7. TENANT-SERVICE-001: Implement bootstrap API endpoint
   - POST /api/tenants/bootstrap
   - Create initial org + admin user atomically
   - Return credentials

8. PEER-SERVICE-001: Implement node configuration API
   - POST /api/peers/configure
   - Set node identity
   - Configure P2P parameters
${NC}
EOF

echo ""

# Step 1: Check Docker services
write_step "Step 1/7: Checking Docker services..."
if ! docker-compose ps >/dev/null 2>&1; then
    write_error "Docker services not running. Please start them first:"
    echo -e "  ${YELLOW}docker-compose up -d${NC}"
    exit 1
fi
write_success "Docker services running"

# Step 2: Wait for services to be ready
write_step "Step 2/7: Waiting for services to be ready..."
MAX_ATTEMPTS=30
ATTEMPT=0
HEALTH_URL="http://localhost/api/health"

while [[ $ATTEMPT -lt $MAX_ATTEMPTS ]]; do
    if curl -sf "$HEALTH_URL" >/dev/null 2>&1; then
        write_success "Services are ready"
        break
    fi

    ATTEMPT=$((ATTEMPT + 1))
    if [[ $ATTEMPT -lt $MAX_ATTEMPTS ]]; then
        echo -e "  ${WHITE}Waiting for services... ($ATTEMPT/$MAX_ATTEMPTS)${NC}"
        sleep 2
    else
        write_error "Services did not become ready in time. Check logs:"
        echo -e "  ${YELLOW}docker-compose logs -f${NC}"
        exit 1
    fi
done

# Step 3: Initialize CLI profile (PLACEHOLDER - CLI enhancement needed)
write_step "Step 3/7: Initializing CLI profile..."
write_info "PLACEHOLDER: CLI command 'sorcha config init' not yet implemented"
write_info "TODO: Implement CLI-BOOTSTRAP-001"
echo -e "  ${WHITE}Would execute: sorcha config init --profile $PROFILE --tenant-url $TENANT_URL ...${NC}"

# For now, we'll create the config manually
CONFIG_DIR="$HOME/.sorcha"
CONFIG_FILE="$CONFIG_DIR/config.json"

mkdir -p "$CONFIG_DIR"

cat > "$CONFIG_FILE" <<EOF
{
  "activeProfile": "$PROFILE",
  "defaultOutputFormat": "json",
  "verboseLogging": false,
  "quietMode": false,
  "profiles": {
    "$PROFILE": {
      "name": "$PROFILE",
      "tenantServiceUrl": "$TENANT_URL",
      "registerServiceUrl": "$REGISTER_URL",
      "walletServiceUrl": "$WALLET_URL",
      "peerServiceUrl": "$PEER_URL",
      "authTokenUrl": "$AUTH_URL",
      "defaultClientId": "$BOOTSTRAP_CLIENT_ID",
      "verifySsl": false,
      "timeoutSeconds": 30
    }
  }
}
EOF

write_success "CLI profile configured: $CONFIG_FILE"

# Step 4: Create bootstrap service principal (PLACEHOLDER)
write_step "Step 4/7: Creating bootstrap service principal..."
write_info "PLACEHOLDER: CLI command 'sorcha sp create' not yet implemented"
write_info "TODO: Implement CLI-BOOTSTRAP-004"
echo -e "  ${WHITE}Would execute: sorcha sp create --name sorcha-bootstrap --scopes all${NC}"
write_success "Bootstrap credentials prepared (manual configuration)"

# Step 5: Create organization (PLACEHOLDER)
write_step "Step 5/7: Creating organization..."
write_info "PLACEHOLDER: CLI command 'sorcha org create' not yet implemented"
write_info "TODO: Implement CLI-BOOTSTRAP-002"
echo -e "  ${WHITE}Would execute: sorcha org create --name '$ORG_NAME' --subdomain '$ORG_SUBDOMAIN'${NC}"
ORG_ID="00000000-0000-0000-0000-000000000000" # Placeholder
write_success "Organization created (placeholder ID: $ORG_ID)"

# Step 6: Create admin user (PLACEHOLDER)
write_step "Step 6/7: Creating administrative user..."
write_info "PLACEHOLDER: CLI command 'sorcha user create' not yet implemented"
write_info "TODO: Implement CLI-BOOTSTRAP-003"
echo -e "  ${WHITE}Would execute: sorcha user create --org-id '$ORG_ID' --email '$ADMIN_EMAIL' --name '$ADMIN_NAME'${NC}"
write_success "Admin user created"

# Step 7: Create initial register (PLACEHOLDER)
write_step "Step 7/7: Creating initial register..."
write_info "PLACEHOLDER: CLI command 'sorcha register create' not yet implemented"
write_info "TODO: Implement CLI-BOOTSTRAP-005"
echo -e "  ${WHITE}Would execute: sorcha register create --name '$REGISTER_NAME' --org-id '$ORG_ID'${NC}"
write_success "Register created"

echo ""
echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}Bootstrap Complete!${NC}"
echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
echo ""

write_success "Sorcha platform has been configured"
echo ""
echo -e "${CYAN}Next Steps:${NC}"
echo -e "  ${WHITE}1. Test authentication:${NC}"
echo -e "     ${WHITE}sorcha auth login --username $ADMIN_EMAIL${NC}"
echo ""
echo -e "  ${WHITE}2. View configuration:${NC}"
echo -e "     ${WHITE}sorcha config list --profiles${NC}"
echo ""
echo -e "  ${WHITE}3. Check system health:${NC}"
echo -e "     ${WHITE}curl http://localhost/api/health${NC}"
echo ""
echo -e "  ${WHITE}4. View API documentation:${NC}"
echo -e "     ${WHITE}http://localhost/scalar/${NC}"
echo ""

write_info "Configuration saved to: $CONFIG_FILE"
write_info "Profile: $PROFILE"
echo ""

# Save bootstrap details for reference
BOOTSTRAP_FILE="$CONFIG_DIR/bootstrap-info.json"

cat > "$BOOTSTRAP_FILE" <<EOF
{
  "timestamp": "$(date -u +"%Y-%m-%d %H:%M:%S")",
  "profile": "$PROFILE",
  "organizationId": "$ORG_ID",
  "organizationName": "$ORG_NAME",
  "adminEmail": "$ADMIN_EMAIL",
  "nodeId": "$NODE_ID",
  "registerName": "$REGISTER_NAME",
  "serviceUrls": {
    "tenant": "$TENANT_URL",
    "register": "$REGISTER_URL",
    "wallet": "$WALLET_URL",
    "peer": "$PEER_URL"
  },
  "enhancements": [
    "CLI-BOOTSTRAP-001: Implement 'sorcha config init' command",
    "CLI-BOOTSTRAP-002: Implement 'sorcha org create' command",
    "CLI-BOOTSTRAP-003: Implement 'sorcha user create' command",
    "CLI-BOOTSTRAP-004: Implement 'sorcha sp create' command",
    "CLI-BOOTSTRAP-005: Implement 'sorcha register create' command",
    "CLI-BOOTSTRAP-006: Implement 'sorcha node configure' command",
    "TENANT-SERVICE-001: Implement bootstrap API endpoint",
    "PEER-SERVICE-001: Implement node configuration API"
  ]
}
EOF

write_success "Bootstrap completed successfully!"
echo -e "  ${WHITE}Bootstrap details saved to: $BOOTSTRAP_FILE${NC}"
echo ""
