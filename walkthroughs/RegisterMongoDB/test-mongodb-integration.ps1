# Test Register Service MongoDB Integration
# This script verifies the Register Service can connect to MongoDB and perform basic operations

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Register Service MongoDB Integration Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verify MongoDB is running
Write-Host "Step 1: Checking MongoDB connection..." -ForegroundColor Yellow
try {
    $mongoTest = docker exec sorcha-mongodb mongosh --quiet --eval "db.adminCommand('ping')" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ MongoDB is running" -ForegroundColor Green
    } else {
        throw "MongoDB not responding"
    }
} catch {
    Write-Host "❌ MongoDB is not running. Please start it with: docker-compose up -d mongodb" -ForegroundColor Red
    exit 1
}

# Step 2: Build the Register Service
Write-Host ""
Write-Host "Step 2: Building Register Service..." -ForegroundColor Yellow
dotnet build src/Services/Sorcha.Register.Service/Sorcha.Register.Service.csproj --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

# Step 3: Start the Register Service with MongoDB configuration
Write-Host ""
Write-Host "Step 3: Starting Register Service with MongoDB..." -ForegroundColor Yellow
Write-Host "(Press Ctrl+C to stop after verifying startup logs)" -ForegroundColor Gray

$env:ASPNETCORE_ENVIRONMENT = "MongoDB"
$env:ASPNETCORE_URLS = "http://localhost:5174"

Write-Host ""
Write-Host "Starting service on http://localhost:5174..." -ForegroundColor Cyan
Write-Host "Watch for the line: '✅ Register Service using MongoDB storage'" -ForegroundColor Cyan
Write-Host ""

# Start the service
Set-Location "C:\Projects\Sorcha"
dotnet run --project src/Services/Sorcha.Register.Service/Sorcha.Register.Service.csproj --no-build
