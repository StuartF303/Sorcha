#!/bin/bash
# Quick rebuild script for Sorcha services
# Usage: ./scripts/rebuild-service.sh <service-name>

set -e

SERVICE_NAME=$1

if [ -z "$SERVICE_NAME" ]; then
    echo "Usage: ./scripts/rebuild-service.sh <service-name>"
    echo ""
    echo "Available services:"
    echo "  register-service"
    echo "  validator-service"
    echo "  wallet-service"
    echo "  tenant-service"
    echo "  blueprint-service"
    echo "  peer-service"
    echo "  api-gateway"
    echo "  admin-ui"
    exit 1
fi

echo "════════════════════════════════════════════════════════════════"
echo "  Rebuilding Sorcha Service: $SERVICE_NAME"
echo "════════════════════════════════════════════════════════════════"
echo ""

# Step 1: Build Docker image
echo "Step 1: Building Docker image..."
docker-compose build $SERVICE_NAME

echo "✓ Docker image built successfully"
echo ""

# Step 2: Restart container
echo "Step 2: Restarting container..."
docker-compose up -d --force-recreate $SERVICE_NAME

echo "✓ Container restarted successfully"
echo ""

# Step 3: Wait for container to stabilize
echo "Step 3: Waiting for service to stabilize..."
sleep 5

# Step 4: Check container status
echo "Step 4: Checking container status..."
CONTAINER_NAME="sorcha-$SERVICE_NAME"
STATUS=$(docker ps --filter "name=$CONTAINER_NAME" --format "{{.Status}}")

if [ -n "$STATUS" ]; then
    echo "✓ Container is running: $STATUS"
else
    echo "✗ Container is not running"
    echo ""
    echo "Last 30 log lines:"
    docker logs $CONTAINER_NAME --tail 30
    exit 1
fi

echo ""

# Step 5: Show recent logs
echo "Step 5: Recent logs:"
echo "────────────────────────────────────────────────────────────────"
docker logs $CONTAINER_NAME --tail 15
echo "────────────────────────────────────────────────────────────────"
echo ""

echo "════════════════════════════════════════════════════════════════"
echo "  Rebuild Complete!"
echo "════════════════════════════════════════════════════════════════"
echo ""
echo "Next steps:"
echo "  • Check logs: docker logs $CONTAINER_NAME -f"
echo "  • View all services: docker-compose ps"
echo "  • Run tests: ./walkthroughs/.../test-*.sh"
echo ""
