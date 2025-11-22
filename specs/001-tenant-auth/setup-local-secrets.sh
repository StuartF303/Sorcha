#!/bin/bash

# Setup Local Secrets for Sorcha Tenant Service
# This script helps developers quickly configure User Secrets for local development

set -e

echo "==================================================="
echo "  Sorcha Tenant Service - Local Secrets Setup"
echo "==================================================="
echo ""

PROJECT_PATH="src/Services/Sorcha.Tenant.Service"

# Check if .NET SDK is installed
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✓ .NET SDK detected: $DOTNET_VERSION"
else
    echo "✗ .NET SDK not found. Please install .NET 10 SDK."
    exit 1
fi

# Check if project exists
if [ ! -d "$PROJECT_PATH" ]; then
    echo "✗ Project not found at: $PROJECT_PATH"
    echo "  Please run this script from the Sorcha project root directory."
    exit 1
fi

echo "✓ Project found: $PROJECT_PATH"
echo ""

# Initialize User Secrets
echo "Initializing User Secrets..."
dotnet user-secrets init --project $PROJECT_PATH > /dev/null 2>&1
echo "✓ User Secrets initialized"
echo ""

# Generate JWT Signing Key
echo "==================================================="
echo "  Step 1: JWT Signing Key (RSA-4096)"
echo "==================================================="
echo ""

read -p "Generate new RSA-4096 signing key? (y/n) [y]: " -n 1 -r GENERATE_KEY
echo ""
GENERATE_KEY=${GENERATE_KEY:-y}

if [[ $GENERATE_KEY =~ ^[Yy]$ ]]; then
    echo "Generating RSA-4096 key pair..."

    # Check if OpenSSL is available
    if command -v openssl &> /dev/null; then
        # Use OpenSSL
        TEMP_PRIVATE=$(mktemp)
        TEMP_PUBLIC=$(mktemp)

        openssl genrsa -out $TEMP_PRIVATE 4096 2>&1 > /dev/null
        openssl rsa -in $TEMP_PRIVATE -pubout -out $TEMP_PUBLIC 2>&1 > /dev/null

        PRIVATE_KEY=$(cat $TEMP_PRIVATE)
        PUBLIC_KEY=$(cat $TEMP_PUBLIC)

        rm $TEMP_PRIVATE $TEMP_PUBLIC

        echo "✓ RSA-4096 key pair generated (OpenSSL)"
    else
        echo "✗ OpenSSL not found. Please install OpenSSL to generate keys."
        exit 1
    fi

    # Store private key in User Secrets
    dotnet user-secrets set "JwtSettings:SigningKey" "$PRIVATE_KEY" --project $PROJECT_PATH
    echo "✓ Private key stored in User Secrets"

    # Save keys to files
    mkdir -p specs/001-tenant-auth
    echo "$PRIVATE_KEY" > specs/001-tenant-auth/jwt_private.pem
    echo "$PUBLIC_KEY" > specs/001-tenant-auth/jwt_public.pem

    chmod 600 specs/001-tenant-auth/jwt_private.pem

    echo "✓ Keys saved to: specs/001-tenant-auth/"
    echo "  - jwt_private.pem (DO NOT COMMIT)"
    echo "  - jwt_public.pem (can be shared)"
    echo ""
fi

# Set Database Password
echo "==================================================="
echo "  Step 2: Database Password"
echo "==================================================="
echo ""

read -p "Enter PostgreSQL password (leave empty for 'dev_password123'): " DB_PASSWORD
DB_PASSWORD=${DB_PASSWORD:-dev_password123}

dotnet user-secrets set "ConnectionStrings:Password" "$DB_PASSWORD" --project $PROJECT_PATH
echo "✓ Database password set in User Secrets"
echo ""

# Set Redis Password
echo "==================================================="
echo "  Step 3: Redis Password (Optional)"
echo "==================================================="
echo ""

read -p "Set Redis password? (y/n) [n]: " -n 1 -r SET_REDIS
echo ""
SET_REDIS=${SET_REDIS:-n}

if [[ $SET_REDIS =~ ^[Yy]$ ]]; then
    read -p "Enter Redis password: " REDIS_PASSWORD
    if [ ! -z "$REDIS_PASSWORD" ]; then
        dotnet user-secrets set "Redis:Password" "$REDIS_PASSWORD" --project $PROJECT_PATH
        echo "✓ Redis password set in User Secrets"
    fi
else
    echo "  Skipping Redis password (using local Redis without auth)"
fi

echo ""

# Verify secrets
echo "==================================================="
echo "  Verification"
echo "==================================================="
echo ""

echo "Configured secrets:"
dotnet user-secrets list --project $PROJECT_PATH

echo ""
echo "==================================================="
echo "  Setup Complete!"
echo "==================================================="
echo ""
echo "Next steps:"
echo "  1. Start Docker dependencies: docker-compose up -d postgres redis"
echo "  2. Run database migrations: dotnet ef database update --project $PROJECT_PATH"
echo "  3. Run the service: dotnet run --project $PROJECT_PATH"
echo "  4. Open Scalar UI: https://localhost:7080/scalar"
echo ""
echo "For more information, see: specs/001-tenant-auth/secrets-setup.md"
echo ""
