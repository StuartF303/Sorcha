# Test Register Service MongoDB Configuration in Docker Compose
# This script verifies that the Register Service uses MongoDB when running via docker-compose

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Docker Compose MongoDB Configuration Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if docker is running
Write-Host "Step 1: Checking Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Stop any existing containers
Write-Host ""
Write-Host "Step 2: Stopping existing containers..." -ForegroundColor Yellow
docker-compose down 2>&1 | Out-Null
Write-Host "✅ Containers stopped" -ForegroundColor Green

# Build the Register Service image
Write-Host ""
Write-Host "Step 3: Building Register Service image..." -ForegroundColor Yellow
Write-Host "(This may take a few minutes on first run)" -ForegroundColor Gray
docker-compose build register-service 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Image built successfully" -ForegroundColor Green

# Start MongoDB and Register Service
Write-Host ""
Write-Host "Step 4: Starting MongoDB..." -ForegroundColor Yellow
docker-compose up -d mongodb 2>&1 | Out-Null
Start-Sleep -Seconds 10  # Wait for MongoDB to be ready
Write-Host "✅ MongoDB started" -ForegroundColor Green

Write-Host ""
Write-Host "Step 5: Starting Register Service..." -ForegroundColor Yellow
docker-compose up -d register-service 2>&1 | Out-Null
Start-Sleep -Seconds 15  # Wait for service to start

# Check container status
Write-Host ""
Write-Host "Step 6: Checking container status..." -ForegroundColor Yellow
$mongoStatus = docker ps --filter "name=sorcha-mongodb" --format "{{.Status}}"
$registerStatus = docker ps --filter "name=sorcha-register-service" --format "{{.Status}}"

if ($mongoStatus -match "Up") {
    Write-Host "✅ MongoDB container: $mongoStatus" -ForegroundColor Green
} else {
    Write-Host "❌ MongoDB container not running" -ForegroundColor Red
}

if ($registerStatus -match "Up") {
    Write-Host "✅ Register Service container: $registerStatus" -ForegroundColor Green
} else {
    Write-Host "❌ Register Service container not running" -ForegroundColor Red
}

# Check logs for MongoDB configuration
Write-Host ""
Write-Host "Step 7: Checking Register Service logs..." -ForegroundColor Yellow
$logs = docker logs sorcha-register-service 2>&1 | Select-String -Pattern "MongoDB|storage|listening" | Select-Object -Last 10

if ($logs -match "MongoDB storage") {
    Write-Host "✅ MongoDB storage configured!" -ForegroundColor Green
    $logs | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
} else {
    Write-Host "⚠️  MongoDB configuration not detected in logs" -ForegroundColor Yellow
    Write-Host "Recent log entries:" -ForegroundColor Gray
    $logs | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
}

# Test API endpoint
Write-Host ""
Write-Host "Step 8: Testing API endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5290/health" -Method Get -TimeoutSec 5
    Write-Host "✅ API is responding" -ForegroundColor Green
    Write-Host "   Health Status: $($response.status)" -ForegroundColor Gray
} catch {
    Write-Host "⚠️  API not responding yet (may still be starting)" -ForegroundColor Yellow
}

# Show environment variables
Write-Host ""
Write-Host "Step 9: Verifying environment configuration..." -ForegroundColor Yellow
$envVars = docker exec sorcha-register-service printenv | Select-String -Pattern "RegisterStorage|MongoDB"
if ($envVars) {
    Write-Host "✅ MongoDB environment variables set:" -ForegroundColor Green
    $envVars | ForEach-Object {
        $line = $_.Line
        # Mask password in connection string
        $line = $line -replace "sorcha_dev_password", "***"
        Write-Host "   $line" -ForegroundColor Gray
    }
} else {
    Write-Host "⚠️  No MongoDB environment variables found" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$fullLogs = docker logs sorcha-register-service 2>&1
$hasMongoConfig = $fullLogs -match "MongoDB storage"
$hasListening = $fullLogs -match "Now listening"

if ($hasMongoConfig -and $hasListening) {
    Write-Host "✅ SUCCESS: Register Service is using MongoDB!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. View logs: docker logs sorcha-register-service" -ForegroundColor Gray
    Write-Host "2. Test API: curl http://localhost:5290/api/registers" -ForegroundColor Gray
    Write-Host "3. View MongoDB: docker exec -it sorcha-mongodb mongosh sorcha_register" -ForegroundColor Gray
    Write-Host "4. Stop services: docker-compose down" -ForegroundColor Gray
} else {
    Write-Host "⚠️  PARTIAL: Service started but MongoDB configuration unclear" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Check full logs with: docker logs sorcha-register-service" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Full logs saved to: docker-compose-test-logs.txt" -ForegroundColor Gray
$fullLogs | Out-File -FilePath "walkthroughs/RegisterMongoDB/docker-compose-test-logs.txt"
