#!/bin/bash
# Sorcha MCP Server - Get Token and Run
# This script starts Sorcha services, authenticates, and runs the MCP Server

set -e  # Exit on error

# Configuration
TENANT_SERVICE_URL="http://localhost:5450"
LOGIN_ENDPOINT="$TENANT_SERVICE_URL/api/auth/login"
DEFAULT_EMAIL="admin@sorcha.local"
DEFAULT_PASSWORD="Dev_Pass_2025!"
STARTUP_WAIT_SECONDS=30

# Parse arguments
AUTO_RUN=false
SKIP_STARTUP=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --auto-run)
            AUTO_RUN=true
            shift
            ;;
        --skip-startup)
            SKIP_STARTUP=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--auto-run] [--skip-startup]"
            exit 1
            ;;
    esac
done

# Colors for output
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
MAGENTA='\033[0;35m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

function write_success { echo -e "${GREEN}✓ $1${NC}"; }
function write_info { echo -e "${CYAN}→ $1${NC}"; }
function write_warning { echo -e "${YELLOW}⚠ $1${NC}"; }
function write_error { echo -e "${RED}✗ $1${NC}"; }
function write_step { echo -e "\n${MAGENTA}=== $1 ===${NC}\n"; }

# Banner
echo ""
echo -e "${CYAN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║         Sorcha MCP Server - Authentication & Launch       ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if Docker is running
write_step "Checking Prerequisites"
write_info "Verifying Docker is running..."

if ! docker ps >/dev/null 2>&1; then
    write_error "Docker is not running or not installed"
    write_info "Please start Docker and try again"
    exit 1
fi

write_success "Docker is running"

# Check for required tools
if ! command -v jq &> /dev/null; then
    write_warning "jq is not installed - token parsing will be limited"
    write_info "Install with: sudo apt-get install jq (Ubuntu) or brew install jq (Mac)"
fi

# Start services if not skipped
if [ "$SKIP_STARTUP" = false ]; then
    write_step "Starting Sorcha Services"
    write_info "Running: docker-compose up -d"
    write_warning "This may take a few minutes on first run (downloading images)..."

    if docker-compose up -d; then
        write_success "Services started"
        write_info "Waiting $STARTUP_WAIT_SECONDS seconds for services to initialize..."
        sleep $STARTUP_WAIT_SECONDS
    else
        write_error "Failed to start services"
        exit 1
    fi
else
    write_step "Skipping Service Startup"
    write_info "Assuming services are already running"
fi

# Check service health
write_step "Verifying Service Health"
write_info "Checking tenant-service status..."

HEALTH_CHECK_ATTEMPTS=0
MAX_HEALTH_CHECK_ATTEMPTS=5
HEALTH_CHECK_OK=false

while [ "$HEALTH_CHECK_OK" = false ] && [ $HEALTH_CHECK_ATTEMPTS -lt $MAX_HEALTH_CHECK_ATTEMPTS ]; do
    HEALTH_CHECK_ATTEMPTS=$((HEALTH_CHECK_ATTEMPTS + 1))

    if curl -s -f -o /dev/null -w "%{http_code}" "$TENANT_SERVICE_URL/health" | grep -q "200"; then
        HEALTH_CHECK_OK=true
        write_success "Tenant service is healthy"
    else
        write_warning "Health check attempt $HEALTH_CHECK_ATTEMPTS failed, retrying in 5 seconds..."
        sleep 5
    fi
done

if [ "$HEALTH_CHECK_OK" = false ]; then
    write_error "Tenant service is not responding after $MAX_HEALTH_CHECK_ATTEMPTS attempts"
    write_info "Check service logs: docker-compose logs -f tenant-service"
    exit 1
fi

# Authenticate and get JWT token
write_step "Authenticating with Tenant Service"
write_info "Logging in as: $DEFAULT_EMAIL"

LOGIN_RESPONSE=$(curl -s -X POST "$LOGIN_ENDPOINT" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$DEFAULT_EMAIL\",\"password\":\"$DEFAULT_PASSWORD\"}")

if command -v jq &> /dev/null; then
    TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.access_token')
else
    # Fallback parsing without jq
    TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)
fi

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    write_error "Authentication failed - no token received"
    write_info "Response: $LOGIN_RESPONSE"
    exit 1
fi

write_success "Authentication successful"
write_info "Token received (length: ${#TOKEN} characters)"

# Display token preview
TOKEN_PREVIEW="${TOKEN:0:20}...${TOKEN: -20}"
echo ""
echo -e "${GRAY}  Token Preview: $TOKEN_PREVIEW${NC}"

# Parse token to show roles (if jq is available)
if command -v jq &> /dev/null; then
    # Extract payload (second part of JWT)
    PAYLOAD=$(echo "$TOKEN" | cut -d'.' -f2)
    # Add padding if needed
    PAYLOAD_PADDED="$PAYLOAD$(printf '=%.0s' {1..4})"
    # Decode base64 and parse JSON
    if CLAIMS=$(echo "$PAYLOAD_PADDED" | base64 -d 2>/dev/null | jq -r 2>/dev/null); then
        EMAIL=$(echo "$CLAIMS" | jq -r '.email // "N/A"')
        ORG=$(echo "$CLAIMS" | jq -r '.organizationName // "N/A"')
        ROLES=$(echo "$CLAIMS" | jq -r '.roles // [] | join(", ")')

        echo ""
        echo -e "${GRAY}  User: $EMAIL${NC}"
        echo -e "${GRAY}  Organization: $ORG${NC}"
        echo -e "${GRAY}  Roles: $ROLES${NC}"
    fi
fi

# Run MCP Server
write_step "Launching MCP Server"
write_info "Starting MCP server with JWT authentication..."
echo ""
echo -e "${GREEN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  MCP Server is starting...                                 ║${NC}"
echo -e "${GREEN}║  Press Ctrl+C to stop                                      ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Export token for docker-compose
export SORCHA_JWT_TOKEN="$TOKEN"

# Run MCP server
if docker-compose run --rm mcp-server --jwt-token "$TOKEN"; then
    echo ""
    write_success "MCP Server session ended"
else
    write_error "Failed to start MCP server"
    exit 1
fi

# Clean up environment variable
unset SORCHA_JWT_TOKEN

echo ""
