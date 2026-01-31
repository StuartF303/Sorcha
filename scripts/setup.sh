#!/bin/bash
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# First-run setup wizard for Sorcha platform installation
# Interactive bash script that handles fresh Sorcha installations

set -e

# Script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Configuration
NON_INTERACTIVE=false
SKIP_DOCKER=false
SKIP_INFRASTRUCTURE=false
FORCE=false
VERBOSE=false

# Required ports
declare -A REQUIRED_PORTS=(
    ["Redis"]=16379
    ["PostgreSQL"]=5432
    ["MongoDB"]=27017
    ["Aspire Dashboard"]=18888
    ["Blueprint Service"]=5000
    ["Register Service"]=5380
    ["Tenant Service"]=5450
    ["Validator Service"]=5800
    ["API Gateway HTTP"]=80
    ["API Gateway HTTPS"]=443
    ["UI Web"]=5400
)

# Required volumes
REQUIRED_VOLUMES=(
    "sorcha_redis-data"
    "sorcha_postgres-data"
    "sorcha_mongodb-data"
    "sorcha_dataprotection-keys"
    "sorcha_wallet-encryption-keys"
)

#region Helper Functions

print_banner() {
    echo ""
    echo "=============================================="
    echo "         Sorcha Platform Setup Wizard         "
    echo "=============================================="
    echo ""
    echo "This wizard will configure a fresh Sorcha installation."
    echo ""
}

print_step() {
    local step=$1
    local total=$2
    local message=$3
    echo ""
    echo -e "\033[36m[$step/$total]\033[0m $message"
    echo "--------------------------------------------------"
}

print_success() {
    echo -e "  \033[32m[OK]\033[0m $1"
}

print_warning() {
    echo -e "  \033[33m[!]\033[0m $1"
}

print_error() {
    echo -e "  \033[31m[X]\033[0m $1"
}

print_info() {
    echo -e "  \033[34m[i]\033[0m $1"
}

print_debug() {
    if [ "$VERBOSE" = true ]; then
        echo -e "  \033[90m[D]\033[0m $1"
    fi
}

command_exists() {
    command -v "$1" >/dev/null 2>&1
}

port_available() {
    local port=$1
    if command_exists nc; then
        ! nc -z 127.0.0.1 "$port" 2>/dev/null
    elif command_exists lsof; then
        ! lsof -i ":$port" >/dev/null 2>&1
    else
        # Assume available if we can't check
        true
    fi
}

docker_running() {
    docker info >/dev/null 2>&1
}

get_docker_volumes() {
    docker volume ls --format "{{.Name}}" 2>/dev/null || echo ""
}

read_input() {
    local prompt=$1
    local default=$2

    if [ "$NON_INTERACTIVE" = true ]; then
        echo "$default"
        return
    fi

    read -r -p "$prompt [$default]: " input
    echo "${input:-$default}"
}

read_yes_no() {
    local prompt=$1
    local default=$2

    if [ "$NON_INTERACTIVE" = true ]; then
        echo "$default"
        return
    fi

    local default_str
    if [ "$default" = "y" ]; then
        default_str="Y/n"
    else
        default_str="y/N"
    fi

    read -r -p "$prompt [$default_str]: " input
    input="${input:-$default}"

    case "$input" in
        [yY]*) echo "y" ;;
        *) echo "n" ;;
    esac
}

generate_password() {
    local length=${1:-32}
    if command_exists openssl; then
        openssl rand -base64 "$length" | tr -d '/+=' | head -c "$length"
    else
        head -c 100 /dev/urandom | tr -dc 'a-zA-Z0-9!@#$%^&*' | head -c "$length"
    fi
}

generate_jwt_key() {
    # Generate 256-bit key
    if command_exists openssl; then
        openssl rand -base64 32
    else
        head -c 32 /dev/urandom | base64
    fi
}

#endregion

#region Setup Steps

step_check_prerequisites() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Checking Prerequisites"

    local issues=()

    # Check Docker
    if [ "$SKIP_DOCKER" = false ]; then
        if command_exists docker; then
            print_success "Docker CLI found"

            if docker_running; then
                print_success "Docker daemon is running"

                # Check Docker Compose
                if command_exists docker-compose; then
                    print_success "Docker Compose found (standalone)"
                elif docker compose version >/dev/null 2>&1; then
                    print_success "Docker Compose found (plugin)"
                else
                    issues+=("Docker Compose not found")
                    print_error "Docker Compose not found"
                fi
            else
                issues+=("Docker daemon is not running")
                print_error "Docker daemon is not running"
                print_info "Please start Docker Desktop and try again"
            fi
        else
            issues+=("Docker not installed")
            print_error "Docker not installed"
            print_info "Download from: https://www.docker.com/products/docker-desktop"
        fi
    else
        print_warning "Skipping Docker checks (--skip-docker)"
    fi

    # Check .NET SDK
    if command_exists dotnet; then
        local dotnet_version
        dotnet_version=$(dotnet --version)
        print_success ".NET SDK $dotnet_version found"

        if [[ ! "$dotnet_version" =~ ^10\. ]]; then
            print_warning ".NET 10 recommended (found $dotnet_version)"
        fi
    else
        print_warning ".NET SDK not found (optional for Docker-only deployment)"
    fi

    # Check Git
    if command_exists git; then
        print_success "Git found"
    else
        print_warning "Git not found (optional)"
    fi

    if [ ${#issues[@]} -gt 0 ]; then
        echo ""
        print_error "Prerequisites check failed:"
        for issue in "${issues[@]}"; do
            echo "    - $issue"
        done
        return 1
    fi

    return 0
}

step_check_ports() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Checking Port Availability"

    local ports_in_use=()

    for service in "${!REQUIRED_PORTS[@]}"; do
        local port=${REQUIRED_PORTS[$service]}
        if port_available "$port"; then
            print_success "Port $port available ($service)"
        else
            print_error "Port $port in use ($service)"
            ports_in_use+=("$service:$port")
        fi
    done

    if [ ${#ports_in_use[@]} -gt 0 ]; then
        echo ""
        print_warning "Some ports are in use. You can:"
        echo "    1. Stop the conflicting services"
        echo "    2. Modify docker-compose.yml to use different ports"
        echo ""

        if [ "$NON_INTERACTIVE" = false ]; then
            local continue_setup
            continue_setup=$(read_yes_no "Continue anyway?" "n")
            if [ "$continue_setup" != "y" ]; then
                return 1
            fi
        fi
    fi

    return 0
}

step_detect_first_run() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Detecting Installation State"

    local no_env_file=true
    local no_volumes=true
    local no_containers=true

    if [ -f "$PROJECT_ROOT/.env" ]; then
        no_env_file=false
        print_success ".env file exists"
    else
        print_info "No .env file found - first run detected"
    fi

    if command_exists docker && docker_running; then
        local existing_volumes
        existing_volumes=$(get_docker_volumes)
        local has_all_volumes=true

        for volume in "${REQUIRED_VOLUMES[@]}"; do
            if ! echo "$existing_volumes" | grep -q "^$volume$"; then
                has_all_volumes=false
                break
            fi
        done

        if [ "$has_all_volumes" = true ]; then
            no_volumes=false
            print_success "Docker volumes exist"
        else
            print_info "Docker volumes not created - first run detected"
        fi

        local containers
        containers=$(docker ps -a --filter "name=sorcha" --format "{{.Names}}" 2>/dev/null || echo "")
        if [ -n "$containers" ]; then
            no_containers=false
            print_success "Sorcha containers exist"
        else
            print_info "No Sorcha containers found"
        fi
    fi

    local is_first_run=false
    if [ "$no_env_file" = true ] || [ "$no_volumes" = true ]; then
        is_first_run=true
    fi

    if [ "$is_first_run" = true ]; then
        echo ""
        print_info "This appears to be a fresh installation"
        echo "first_run"
    elif [ "$FORCE" = true ]; then
        echo ""
        print_warning "Existing installation detected, but --force specified"
        print_warning "This will regenerate configuration files"

        if [ "$NON_INTERACTIVE" = false ]; then
            local continue_setup
            continue_setup=$(read_yes_no "Proceed with re-initialization?" "n")
            if [ "$continue_setup" != "y" ]; then
                echo "Setup cancelled."
                exit 0
            fi
        fi
        echo "first_run"
    else
        echo ""
        print_success "Existing installation detected"
        print_info "Use --force to re-initialize"
        echo "existing"
    fi
}

step_generate_configuration() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Generating Configuration"

    # Generate secure credentials
    local jwt_signing_key
    jwt_signing_key=$(generate_jwt_key)

    local installation_name
    if [ "$NON_INTERACTIVE" = true ]; then
        installation_name="localhost"
    else
        installation_name=$(read_input "Installation name (used for JWT issuer)" "localhost")
    fi

    print_success "Generated JWT signing key (256-bit)"
    print_success "Installation name: $installation_name"

    # Create .env file
    cat > "$PROJECT_ROOT/.env" << EOF
# Sorcha Platform Configuration
# Generated by setup.sh on $(date '+%Y-%m-%d %H:%M:%S')
# DO NOT COMMIT THIS FILE TO SOURCE CONTROL

# Installation Identity
INSTALLATION_NAME=$installation_name

# JWT Configuration (256-bit key)
JWT_SIGNING_KEY=$jwt_signing_key

# Database Credentials (change for production!)
POSTGRES_USER=sorcha
POSTGRES_PASSWORD=sorcha_dev_password
MONGO_USERNAME=sorcha
MONGO_PASSWORD=sorcha_dev_password

# Redis Configuration
REDIS_PASSWORD=

# Development mode (set to false for production)
ASPNETCORE_ENVIRONMENT=Development
EOF

    print_success "Created .env file"

    # Create certs directory if needed
    local certs_dir="$PROJECT_ROOT/docker/certs"
    if [ ! -d "$certs_dir" ]; then
        mkdir -p "$certs_dir"
        print_success "Created docker/certs directory"
    fi

    # Check if HTTPS certificate exists
    local cert_path="$certs_dir/aspnetapp.pfx"
    if [ ! -f "$cert_path" ]; then
        print_warning "HTTPS certificate not found at $cert_path"
        print_info "HTTPS will not work until certificate is generated"
        print_info "Run: ./scripts/setup-https-docker.sh"
    else
        print_success "HTTPS certificate found"
    fi

    return 0
}

step_create_volumes() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Creating Docker Volumes"

    if [ "$SKIP_DOCKER" = true ] || ! docker_running; then
        print_warning "Skipping Docker volume creation"
        return 0
    fi

    local existing_volumes
    existing_volumes=$(get_docker_volumes)

    for volume in "${REQUIRED_VOLUMES[@]}"; do
        if echo "$existing_volumes" | grep -q "^$volume$"; then
            print_success "Volume $volume already exists"
        else
            if docker volume create "$volume" >/dev/null 2>&1; then
                print_success "Created volume $volume"
            else
                print_error "Failed to create volume $volume"
                return 1
            fi
        fi
    done

    # Fix wallet encryption key permissions
    print_info "Setting wallet encryption key permissions..."
    if docker run --rm -v sorcha_wallet-encryption-keys:/data alpine chown -R 1654:1654 /data 2>/dev/null; then
        print_success "Wallet encryption key permissions set (UID 1654)"
    else
        print_warning "Could not set wallet permissions - may need manual fix"
    fi

    return 0
}

step_start_infrastructure() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Starting Infrastructure Services"

    if [ "$SKIP_INFRASTRUCTURE" = true ]; then
        print_warning "Skipping infrastructure startup (--skip-infrastructure)"
        return 0
    fi

    if [ "$SKIP_DOCKER" = true ] || ! docker_running; then
        print_warning "Skipping Docker infrastructure"
        return 0
    fi

    cd "$PROJECT_ROOT"

    print_info "Starting Redis, PostgreSQL, MongoDB, Aspire Dashboard..."

    local compose_cmd="docker-compose"
    if ! command_exists docker-compose; then
        compose_cmd="docker compose"
    fi

    if ! $compose_cmd up -d redis postgres mongodb aspire-dashboard 2>&1 | while read -r line; do print_debug "$line"; done; then
        print_error "Failed to start infrastructure services"
        return 1
    fi

    print_success "Infrastructure services started"

    # Wait for health checks
    print_info "Waiting for services to be healthy..."
    local max_wait=60
    local waited=0

    while [ $waited -lt $max_wait ]; do
        local healthy=true

        # Check Redis
        if ! docker exec sorcha-redis redis-cli ping >/dev/null 2>&1; then
            healthy=false
        fi

        # Check PostgreSQL
        if ! docker exec sorcha-postgres pg_isready -U sorcha >/dev/null 2>&1; then
            healthy=false
        fi

        # Check MongoDB
        if ! docker exec sorcha-mongodb mongosh --eval "db.adminCommand('ping')" >/dev/null 2>&1; then
            healthy=false
        fi

        if [ "$healthy" = true ]; then
            break
        fi

        sleep 2
        waited=$((waited + 2))
        print_debug "Waiting... ($waited/$max_wait seconds)"
    done

    if [ $waited -ge $max_wait ]; then
        print_warning "Some services may not be fully healthy yet"
    else
        print_success "All infrastructure services are healthy"
    fi

    return 0
}

step_start_application_services() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Starting Application Services"

    if [ "$SKIP_INFRASTRUCTURE" = true ]; then
        print_warning "Skipping application services (--skip-infrastructure)"
        return 0
    fi

    if [ "$SKIP_DOCKER" = true ] || ! docker_running; then
        print_warning "Skipping Docker application services"
        return 0
    fi

    cd "$PROJECT_ROOT"

    print_info "Building and starting all services..."

    local compose_cmd="docker-compose"
    if ! command_exists docker-compose; then
        compose_cmd="docker compose"
    fi

    if ! $compose_cmd up -d --build 2>&1 | while read -r line; do print_debug "$line"; done; then
        print_error "Failed to start application services"
        return 1
    fi

    print_success "Application services started"

    # Wait for API Gateway
    print_info "Waiting for API Gateway to be ready..."
    local max_wait=120
    local waited=0

    while [ $waited -lt $max_wait ]; do
        if curl -s -o /dev/null -w "%{http_code}" "http://localhost/health" 2>/dev/null | grep -q "200"; then
            print_success "API Gateway is ready"
            break
        fi

        sleep 3
        waited=$((waited + 3))
        print_debug "Waiting for API Gateway... ($waited/$max_wait seconds)"
    done

    if [ $waited -ge $max_wait ]; then
        print_warning "API Gateway may not be fully ready"
        print_info "Check logs with: docker-compose logs api-gateway"
    fi

    return 0
}

step_validate_installation() {
    local step=$1
    local total=$2

    print_step "$step" "$total" "Validating Installation"

    local validation_script="$SCRIPT_DIR/validate-environment.sh"
    if [ -f "$validation_script" ]; then
        print_info "Running environment validation..."
        if bash "$validation_script" --quiet; then
            print_success "All validation checks passed"
        else
            print_warning "Some validation checks failed"
            print_info "Run ./scripts/validate-environment.sh for details"
        fi
    else
        # Basic validation inline
        local services=(
            "API Gateway|http://localhost/health"
            "Tenant Service|http://localhost/api/tenant/health"
            "Blueprint Service|http://localhost/api/blueprints/health"
        )

        for service_info in "${services[@]}"; do
            local name="${service_info%%|*}"
            local url="${service_info##*|}"

            if curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null | grep -q "200"; then
                print_success "$name is healthy"
            else
                print_error "$name is not responding"
            fi
        done
    fi

    return 0
}

step_print_summary() {
    echo ""
    echo "=============================================="
    echo "     Sorcha Platform Setup Complete!          "
    echo "=============================================="
    echo ""

    echo "Service URLs:"
    echo "  Main UI:          http://localhost/app"
    echo "  API Gateway:      http://localhost"
    echo "  Aspire Dashboard: http://localhost:18888"
    echo ""

    echo "Next Steps:"
    echo "  1. Run bootstrap to create initial organization:"
    echo "     ./scripts/bootstrap-sorcha.sh --profile docker"
    echo ""
    echo "  2. Or use the CLI:"
    echo "     sorcha bootstrap --profile docker"
    echo ""
    echo "  3. View logs:"
    echo "     docker-compose logs -f"
    echo ""

    echo "Documentation:"
    echo "  docs/FIRST-RUN-SETUP.md   - Setup guide"
    echo "  docs/PORT-CONFIGURATION.md - Port reference"
    echo ""
}

#endregion

#region Main Execution

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --non-interactive|-n)
            NON_INTERACTIVE=true
            shift
            ;;
        --skip-docker)
            SKIP_DOCKER=true
            shift
            ;;
        --skip-infrastructure)
            SKIP_INFRASTRUCTURE=true
            shift
            ;;
        --force|-f)
            FORCE=true
            shift
            ;;
        --verbose|-v)
            VERBOSE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [options]"
            echo ""
            echo "Options:"
            echo "  --non-interactive, -n  Run without prompts"
            echo "  --skip-docker          Skip Docker checks"
            echo "  --skip-infrastructure  Skip infrastructure startup"
            echo "  --force, -f            Force re-initialization"
            echo "  --verbose, -v          Show detailed output"
            echo "  --help, -h             Show this help"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

print_banner

total_steps=8
current_step=0

# Step 1: Prerequisites
current_step=$((current_step + 1))
if ! step_check_prerequisites "$current_step" "$total_steps"; then
    echo ""
    print_error "Setup failed: Prerequisites not met"
    exit 1
fi

# Step 2: Port availability
current_step=$((current_step + 1))
if ! step_check_ports "$current_step" "$total_steps"; then
    exit 1
fi

# Step 3: Detect first run
current_step=$((current_step + 1))
install_state=$(step_detect_first_run "$current_step" "$total_steps")
if [ "$install_state" = "existing" ]; then
    echo ""
    print_info "Sorcha is already configured. Use --force to re-initialize."
    echo ""
    echo "To start services: docker-compose up -d"
    echo "To run bootstrap:  ./scripts/bootstrap-sorcha.sh"
    echo ""
    exit 0
fi

# Step 4: Generate configuration
current_step=$((current_step + 1))
if ! step_generate_configuration "$current_step" "$total_steps"; then
    print_error "Setup failed: Configuration generation failed"
    exit 1
fi

# Step 5: Create volumes
current_step=$((current_step + 1))
if ! step_create_volumes "$current_step" "$total_steps"; then
    print_error "Setup failed: Volume creation failed"
    exit 1
fi

# Step 6: Start infrastructure
current_step=$((current_step + 1))
if ! step_start_infrastructure "$current_step" "$total_steps"; then
    print_error "Setup failed: Infrastructure startup failed"
    exit 1
fi

# Step 7: Start application services
current_step=$((current_step + 1))
if ! step_start_application_services "$current_step" "$total_steps"; then
    print_error "Setup failed: Application services failed"
    exit 1
fi

# Step 8: Validate and print summary
current_step=$((current_step + 1))
step_validate_installation "$current_step" "$total_steps"
step_print_summary

exit 0

#endregion
