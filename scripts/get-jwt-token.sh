#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

set -e

# Default values
TENANT_SERVICE_URL="http://localhost/api/tenant"
PROFILE="docker"
AS_ENV_VAR=false
QUIET=false

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Help text
show_help() {
    cat << EOF
Usage: $(basename "$0") -e EMAIL -p PASSWORD [OPTIONS]

Gets a JWT token from Sorcha Tenant Service

Options:
    -e, --email EMAIL           User email address (required)
    -p, --password PASSWORD     User password (required)
    -u, --url URL               Tenant Service URL (default: http://localhost/api/tenant)
    -P, --profile PROFILE       Deployment profile: docker, aspire, local (default: docker)
    -E, --env-var               Output as environment variable setting
    -q, --quiet                 Suppress informational messages
    -h, --help                  Show this help message

Examples:
    $(basename "$0") -e admin@sorcha.local -p Admin123!
    $(basename "$0") -e admin@sorcha.local -p Admin123! -E
    TOKEN=\$($(basename "$0") -e admin@sorcha.local -p Admin123! -q)

EOF
}

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--email)
            EMAIL="$2"
            shift 2
            ;;
        -p|--password)
            PASSWORD="$2"
            shift 2
            ;;
        -u|--url)
            TENANT_SERVICE_URL="$2"
            shift 2
            ;;
        -P|--profile)
            PROFILE="$2"
            shift 2
            ;;
        -E|--env-var)
            AS_ENV_VAR=true
            shift
            ;;
        -q|--quiet)
            QUIET=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}ERROR: Unknown option $1${NC}" >&2
            show_help
            exit 1
            ;;
    esac
done

# Validate required parameters
if [ -z "$EMAIL" ] || [ -z "$PASSWORD" ]; then
    echo -e "${RED}ERROR: Email and password are required${NC}" >&2
    show_help
    exit 1
fi

# Adjust URL based on profile
case $PROFILE in
    aspire)
        TENANT_SERVICE_URL="https://localhost:7110/api/tenant"
        ;;
    local)
        TENANT_SERVICE_URL="http://localhost:5450/api/tenant"
        ;;
esac

# Helper functions
info_message() {
    if [ "$QUIET" = false ]; then
        echo -e "${CYAN}$1${NC}"
    fi
}

error_message() {
    echo -e "${RED}ERROR: $1${NC}" >&2
}

# Main execution
info_message "Authenticating with Tenant Service at $TENANT_SERVICE_URL..."

LOGIN_URL="$TENANT_SERVICE_URL/auth/login"
REQUEST_BODY=$(cat <<EOF
{
    "email": "$EMAIL",
    "password": "$PASSWORD"
}
EOF
)

info_message "Sending login request..."

# Make the request
HTTP_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$LOGIN_URL" \
    -H "Content-Type: application/json" \
    -d "$REQUEST_BODY" 2>&1)

HTTP_CODE=$(echo "$HTTP_RESPONSE" | tail -n 1)
RESPONSE_BODY=$(echo "$HTTP_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" -eq 200 ]; then
    # Extract token
    TOKEN=$(echo "$RESPONSE_BODY" | grep -o '"accessToken":"[^"]*"' | sed 's/"accessToken":"\(.*\)"/\1/')

    if [ -n "$TOKEN" ]; then
        if [ "$QUIET" = false ]; then
            echo ""
            echo -e "${GREEN}SUCCESS: JWT token obtained${NC}"
            echo ""

            # Show token preview
            if [ ${#TOKEN} -gt 50 ]; then
                PREVIEW="${TOKEN:0:47}..."
            else
                PREVIEW="$TOKEN"
            fi

            echo -e "${GRAY}Token Preview: $PREVIEW${NC}"
            echo -e "${GRAY}Token Length: ${#TOKEN} characters${NC}"
            echo ""
        fi

        # Output based on mode
        if [ "$AS_ENV_VAR" = true ]; then
            echo "export SORCHA_JWT_TOKEN=\"$TOKEN\""
            echo ""
            echo -e "${YELLOW}To set in current session, run:${NC}"
            echo -e "${GRAY}export SORCHA_JWT_TOKEN=\"$TOKEN\"${NC}"
        else
            # Just output the token
            echo "$TOKEN"
        fi

        # Show usage examples if not quiet
        if [ "$QUIET" = false ]; then
            echo -e "${CYAN}Usage Examples:${NC}"
            echo -e "  ${NC}1. Set environment variable:${NC}"
            echo -e "     ${GRAY}export SORCHA_JWT_TOKEN=\"$TOKEN\"${NC}"
            echo ""
            echo -e "  ${NC}2. Use with MCP server:${NC}"
            echo -e "     ${GRAY}docker-compose run mcp-server --jwt-token \"$TOKEN\"${NC}"
            echo ""
            echo -e "  ${NC}3. Use with API calls:${NC}"
            echo -e "     ${GRAY}curl -H \"Authorization: Bearer $TOKEN\" http://localhost/api/...${NC}"
            echo ""
        fi

        exit 0
    else
        error_message "Response did not contain accessToken field"
        echo -e "${YELLOW}Response received: $RESPONSE_BODY${NC}"
        exit 1
    fi
else
    error_message "Failed to authenticate"
    echo -e "${YELLOW}HTTP $HTTP_CODE${NC}"

    if [ -n "$RESPONSE_BODY" ]; then
        echo -e "${YELLOW}Server response: $RESPONSE_BODY${NC}"
    fi

    # Common error hints
    case $HTTP_CODE in
        401)
            echo ""
            echo -e "${CYAN}HINT: Check email and password are correct${NC}"
            echo -e "${GRAY}Default admin: email=admin@sorcha.local, password=Admin123!${NC}"
            ;;
        404)
            echo ""
            echo -e "${CYAN}HINT: Tenant Service may not be running or URL is incorrect${NC}"
            echo -e "${GRAY}Check services: docker-compose ps${NC}"
            ;;
        503)
            echo ""
            echo -e "${CYAN}HINT: Service may be starting up or unavailable${NC}"
            echo -e "${GRAY}Check logs: docker-compose logs -f tenant-service${NC}"
            ;;
        000)
            echo ""
            echo -e "${CYAN}HINT: Unable to connect to service${NC}"
            echo -e "${GRAY}Ensure services are running: docker-compose up -d${NC}"
            ;;
    esac

    echo ""
    echo -e "${CYAN}Troubleshooting:${NC}"
    echo -e "  ${NC}1. Verify services are running: docker-compose ps${NC}"
    echo -e "  ${NC}2. Check Tenant Service logs: docker-compose logs -f tenant-service${NC}"
    echo -e "  ${NC}3. Verify bootstrap was run: ./scripts/bootstrap-sorcha.sh -p $PROFILE${NC}"
    echo -e "  ${NC}4. Try different profile: -P aspire or -P local${NC}"
    echo ""

    exit 1
fi
