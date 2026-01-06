#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets up HTTPS for Sorcha.UI.Web in Docker using dotnet dev-certs

.DESCRIPTION
    Uses dotnet dev-certs to generate a development certificate and exports it
    for use in Docker containers.
#>

$ErrorActionPreference = "Stop"

Write-Host "=== Sorcha.UI.Web HTTPS Setup for Docker ===" -ForegroundColor Cyan
Write-Host ""

# Create certs directory
$certDir = ".\certs"
if (-not (Test-Path $certDir)) {
    Write-Host "Creating certificate directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null
}

$certFile = Join-Path $certDir "sorcha-ui-web.pfx"
$certPassword = "SorchaDevCert2025!"

# Remove existing certificate if it exists
if (Test-Path $certFile) {
    Write-Host "Removing existing certificate..." -ForegroundColor Yellow
    Remove-Item $certFile -Force
}

# Clean up existing dotnet dev-certs
Write-Host "Cleaning up existing dotnet dev-certs..." -ForegroundColor Yellow
dotnet dev-certs https --clean

# Generate new certificate
Write-Host "Generating new development certificate..." -ForegroundColor Yellow
dotnet dev-certs https -ep $certFile -p $certPassword --trust

Write-Host "Certificate generated and trusted!" -ForegroundColor Green
Write-Host ""

# Create environment file for Docker
$envFile = ".env.https"
$envContent = @"
# HTTPS Certificate Configuration for Docker
ASPNETCORE_Kestrel__Certificates__Default__Password=$certPassword
ASPNETCORE_Kestrel__Certificates__Default__Path=/https/sorcha-ui-web.pfx
ASPNETCORE_URLS=https://+:8443;http://+:8080
ASPNETCORE_HTTPS_PORTS=8443
"@

Write-Host "Creating .env.https file..." -ForegroundColor Yellow
Set-Content -Path $envFile -Value $envContent -Force
Write-Host "Environment file created!" -ForegroundColor Green

Write-Host ""
Write-Host "=== Certificate Details ===" -ForegroundColor Cyan
Write-Host "Certificate file: $certFile"
Write-Host "Password:         $certPassword"
Write-Host ""

Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Update docker-compose.yml (automated below)"
Write-Host "2. Restart containers: docker-compose down && docker-compose up -d"
Write-Host "3. Access via: https://localhost"
Write-Host ""
Write-Host "HTTPS setup complete!" -ForegroundColor Green
