# Fix DataProtection key permissions in Docker volumes
# This script removes the old volume and recreates it with correct permissions

Write-Host "Fixing DataProtection key permissions..." -ForegroundColor Cyan

# Stop all services
Write-Host "Stopping services..." -ForegroundColor Yellow
docker-compose down

# Remove the problematic volume
Write-Host "Removing old dataprotection-keys volume..." -ForegroundColor Yellow
docker volume rm sorcha_dataprotection-keys 2>$null

# Restart services (volume will be recreated with correct permissions)
Write-Host "Starting services..." -ForegroundColor Yellow
docker-compose up -d

Write-Host ""
Write-Host "✅ Volume recreated. Check logs:" -ForegroundColor Green
Write-Host "   docker logs sorcha-register-service" -ForegroundColor Gray
