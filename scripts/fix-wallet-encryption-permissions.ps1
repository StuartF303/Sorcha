#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Fixes Docker volume permissions for Wallet Service encryption keys

.DESCRIPTION
    This script fixes the most common issue with fresh Docker installations:
    the wallet-encryption-keys volume is created with root ownership,
    but the container runs as non-root user (UID 1654).

    This script:
    1. Stops the wallet service (if running)
    2. Fixes volume permissions to allow UID 1654 to write
    3. Restarts the wallet service

.EXAMPLE
    .\fix-wallet-encryption-permissions.ps1

    Fixes volume permissions and restarts the wallet service

.NOTES
    Author: Sorcha Team
    Date: 2026-01-20
    Version: 1.0
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "▶ " -ForegroundColor Cyan -NoNewline
    Write-Host $Message
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ " -ForegroundColor Blue -NoNewline
    Write-Host $Message
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Fix Wallet Encryption Volume Permissions" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verify Docker is running
Write-Step "Verifying Docker is running..."
try {
    docker info | Out-Null
    Write-Success "Docker daemon is running"
} catch {
    Write-Error "Docker daemon is not running"
    Write-Info "Please start Docker Desktop and try again"
    exit 1
}

# Check if volume exists
Write-Step "Checking for wallet-encryption-keys volume..."
$volumeExists = docker volume ls --format "{{.Name}}" | Select-String -Pattern "wallet-encryption-keys" -Quiet

if (-not $volumeExists) {
    Write-Warning "Volume 'wallet-encryption-keys' does not exist"
    Write-Info "Creating volume..."
    docker volume create wallet-encryption-keys | Out-Null
    Write-Success "Volume created"
}

# Stop wallet service if running
Write-Step "Stopping wallet service (if running)..."
$serviceRunning = docker ps --format "{{.Names}}" | Select-String -Pattern "sorcha-wallet-service" -Quiet
if ($serviceRunning) {
    docker compose stop wallet-service 2>&1 | Out-Null
    Write-Success "Wallet service stopped"
} else {
    Write-Info "Wallet service was not running"
}

# Fix volume permissions
Write-Step "Fixing volume permissions for UID 1654 (non-root container user)..."
$result = docker run --rm -v wallet-encryption-keys:/data alpine chown -R 1654:1654 /data 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Success "Volume permissions fixed successfully"
} else {
    Write-Error "Failed to fix volume permissions"
    Write-Host $result
    exit 1
}

# Verify permissions
Write-Step "Verifying permissions..."
$permissions = docker run --rm -v wallet-encryption-keys:/data alpine ls -la /data 2>&1
Write-Success "Current permissions:"
Write-Host $permissions

# Restart wallet service if it was running before
if ($serviceRunning) {
    Write-Step "Restarting wallet service..."
    docker compose start wallet-service 2>&1 | Out-Null
    Write-Success "Wallet service restarted"

    # Wait a moment for service to initialize
    Write-Info "Waiting for service to initialize..."
    Start-Sleep -Seconds 5

    # Check health
    Write-Step "Checking service health..."
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:8080/health" -Method Get -ErrorAction SilentlyContinue
        if ($health.status -eq "Healthy") {
            Write-Success "Service is healthy"
        } else {
            Write-Warning "Service health: $($health.status)"
        }
    } catch {
        Write-Info "Health check endpoint not yet accessible - service may still be starting"
        Write-Info "Check manually: docker logs sorcha-wallet-service"
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Permissions Fixed!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Info "The wallet-encryption-keys volume now has correct permissions."
Write-Info "The wallet service should be able to create and access encryption keys."
Write-Host ""
