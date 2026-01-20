#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Automated setup script for Sorcha Wallet Service encryption in Docker

.DESCRIPTION
    This script automates the setup of the Wallet Service encryption provider
    for Docker deployments. It performs the following tasks:

    1. Verifies Docker and Docker Compose installation
    2. Creates necessary volumes for encryption key storage
    3. Builds the Wallet Service container with encryption support
    4. Verifies the encryption provider is correctly configured
    5. Performs a health check on the encryption subsystem
    6. Creates a backup script for encryption keys

.PARAMETER Clean
    Clean existing volumes and start fresh (WARNING: Destroys existing keys)

.PARAMETER SkipBuild
    Skip Docker image rebuild (use existing image)

.PARAMETER Verify
    Only verify the current setup without making changes

.EXAMPLE
    .\setup-wallet-encryption-docker.ps1

    Standard setup with Docker environment verification

.EXAMPLE
    .\setup-wallet-encryption-docker.ps1 -Clean

    Clean existing volumes and start fresh (CAUTION: Destroys all existing wallet encryption keys)

.EXAMPLE
    .\setup-wallet-encryption-docker.ps1 -Verify

    Verify the current encryption setup without making changes

.NOTES
    Author: Sorcha Team
    Date: 2026-01-11
    Version: 1.0
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [switch]$Clean,

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory=$false)]
    [switch]$Verify
)

# Error handling
$ErrorActionPreference = "Stop"

# Color output functions
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

# Check if running in repository root
if (-not (Test-Path "docker-compose.yml")) {
    Write-Error "This script must be run from the repository root directory"
    exit 1
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Sorcha Wallet Service - Docker Encryption Setup" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verify Docker installation
Write-Step "Verifying Docker installation..."
try {
    $dockerVersion = docker --version
    Write-Success "Docker installed: $dockerVersion"
} catch {
    Write-Error "Docker is not installed or not in PATH"
    Write-Info "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop"
    exit 1
}

# Step 2: Verify Docker Compose installation
Write-Step "Verifying Docker Compose installation..."
try {
    $composeVersion = docker compose version
    Write-Success "Docker Compose installed: $composeVersion"
} catch {
    Write-Error "Docker Compose is not installed or not available"
    exit 1
}

# Step 3: Verify Docker daemon is running
Write-Step "Verifying Docker daemon is running..."
try {
    docker info | Out-Null
    Write-Success "Docker daemon is running"
} catch {
    Write-Error "Docker daemon is not running"
    Write-Info "Please start Docker Desktop and try again"
    exit 1
}

# If only verification requested, stop here
if ($Verify) {
    Write-Host ""
    Write-Step "Verifying current encryption setup..."

    # Check if volume exists
    $volumeExists = docker volume ls --format "{{.Name}}" | Select-String -Pattern "wallet-encryption-keys" -Quiet

    if ($volumeExists) {
        Write-Success "Volume 'wallet-encryption-keys' exists"

        # Check if wallet service is running
        $serviceRunning = docker ps --format "{{.Names}}" | Select-String -Pattern "sorcha-wallet-service" -Quiet

        if ($serviceRunning) {
            Write-Success "Wallet service container is running"

            # Check encryption provider logs
            Write-Step "Checking encryption provider initialization..."
            $logs = docker logs sorcha-wallet-service 2>&1 | Select-String -Pattern "Encryption provider initialized"

            if ($logs) {
                Write-Success "Encryption provider initialized successfully"
            } else {
                Write-Warning "No encryption provider initialization log found"
            }

            # Health check
            Write-Step "Performing health check..."
            try {
                $health = Invoke-RestMethod -Uri "http://localhost:8080/health" -Method Get -ErrorAction SilentlyContinue
                if ($health.status -eq "Healthy") {
                    Write-Success "Health check passed: $($health.status)"
                } else {
                    Write-Warning "Health check returned: $($health.status)"
                }
            } catch {
                Write-Warning "Health check endpoint not accessible (service may not be fully started)"
            }
        } else {
            Write-Warning "Wallet service container is not running"
            Write-Info "Start services with: docker-compose up -d wallet-service"
        }
    } else {
        Write-Warning "Volume 'wallet-encryption-keys' does not exist"
        Write-Info "Run this script without -Verify to set up encryption"
    }

    Write-Host ""
    Write-Success "Verification complete"
    exit 0
}

# Step 4: Handle clean option (WARNING: Destructive)
if ($Clean) {
    Write-Host ""
    Write-Warning "═══════════════════════════════════════════════════════════════"
    Write-Warning "  DESTRUCTIVE OPERATION WARNING"
    Write-Warning "═══════════════════════════════════════════════════════════════"
    Write-Warning "This will DELETE the existing wallet-encryption-keys volume."
    Write-Warning "All encrypted wallet keys will become PERMANENTLY INACCESSIBLE."
    Write-Warning "This operation CANNOT be undone unless you have a backup."
    Write-Host ""

    $confirmation = Read-Host "Type 'DELETE-KEYS' to confirm (or press Enter to cancel)"

    if ($confirmation -ne "DELETE-KEYS") {
        Write-Info "Operation cancelled"
        exit 0
    }

    Write-Step "Stopping wallet service..."
    docker compose stop wallet-service
    Write-Success "Wallet service stopped"

    Write-Step "Removing wallet-encryption-keys volume..."
    try {
        docker volume rm wallet-encryption-keys 2>&1 | Out-Null
        Write-Success "Volume removed"
    } catch {
        Write-Info "Volume did not exist or already removed"
    }
}

# Step 5: Create encryption keys volume
Write-Step "Creating encryption keys volume..."
$volumeExists = docker volume ls --format "{{.Name}}" | Select-String -Pattern "wallet-encryption-keys" -Quiet

if ($volumeExists) {
    Write-Success "Volume 'wallet-encryption-keys' already exists"
} else {
    docker volume create wallet-encryption-keys | Out-Null
    Write-Success "Volume 'wallet-encryption-keys' created"
}

# Step 5b: Fix volume permissions for non-root container user
# The wallet service runs as UID 1654 (non-root), but Docker volumes are created with root ownership
# This step ensures the container can write to the encryption keys directory
Write-Step "Setting volume permissions for non-root container (UID 1654)..."
docker run --rm -v wallet-encryption-keys:/data alpine chown -R 1654:1654 /data 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Success "Volume permissions set correctly (owner: 1654:1654)"
} else {
    Write-Warning "Could not set volume permissions - service may fail to write encryption keys"
    Write-Info "Run manually: docker run --rm -v wallet-encryption-keys:/data alpine chown -R 1654:1654 /data"
}

# Step 6: Build Wallet Service image (unless skipped)
if (-not $SkipBuild) {
    Write-Step "Building Wallet Service Docker image..."
    Write-Info "This may take several minutes on first build..."

    docker compose build wallet-service

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Wallet Service image built successfully"
    } else {
        Write-Error "Docker build failed with exit code $LASTEXITCODE"
        exit 1
    }
} else {
    Write-Info "Skipping Docker image build (using existing image)"
}

# Step 7: Start infrastructure dependencies
Write-Step "Starting infrastructure dependencies (PostgreSQL, Redis)..."
docker compose up -d postgres redis

Write-Info "Waiting for PostgreSQL to be healthy..."
$maxWait = 60
$waited = 0
while ($waited -lt $maxWait) {
    $healthy = docker inspect --format='{{.State.Health.Status}}' sorcha-postgres 2>$null
    if ($healthy -eq "healthy") {
        break
    }
    Start-Sleep -Seconds 2
    $waited += 2
    Write-Host "." -NoNewline
}
Write-Host ""

if ($waited -ge $maxWait) {
    Write-Warning "PostgreSQL health check timed out (may still be starting)"
} else {
    Write-Success "PostgreSQL is healthy"
}

# Step 8: Start Wallet Service
Write-Step "Starting Wallet Service..."
docker compose up -d wallet-service

# Step 9: Wait for service to initialize
Write-Info "Waiting for Wallet Service to initialize..."
Start-Sleep -Seconds 5

# Step 10: Verify encryption provider initialization
Write-Step "Verifying encryption provider initialization..."
$logs = docker logs sorcha-wallet-service 2>&1 | Select-String -Pattern "Encryption provider initialized|Linux Secret Service|FallbackKeyStorePath"

if ($logs) {
    Write-Success "Encryption provider initialized"
    Write-Host ""
    Write-Info "Initialization logs:"
    $logs | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
} else {
    Write-Warning "No encryption provider initialization logs found yet"
    Write-Info "Check logs manually: docker logs sorcha-wallet-service"
}

# Step 11: Verify key directory in container
Write-Step "Verifying encryption key directory in container..."
$keyDirPerms = docker exec sorcha-wallet-service ls -la /var/lib/sorcha/wallet-keys 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Success "Encryption key directory exists in container"
} else {
    Write-Warning "Could not verify encryption key directory"
}

# Step 12: Create backup script
Write-Step "Creating backup script..."
$backupScript = @"
#!/usr/bin/env pwsh
# Wallet Encryption Keys Backup Script
# Generated by setup-wallet-encryption-docker.ps1

`$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
`$backupDir = "./backups/wallet-keys"
`$backupFile = "wallet-keys-`$timestamp.tar.gz"

# Create backup directory
New-Item -ItemType Directory -Force -Path `$backupDir | Out-Null

# Backup encryption keys
Write-Host "Backing up wallet encryption keys..."
docker run --rm ``
  -v wallet-encryption-keys:/source:ro ``
  -v "`$PWD/`$backupDir:/backup" ``
  alpine ``
  tar czf /backup/`$backupFile -C /source .

if (`$LASTEXITCODE -eq 0) {
    Write-Host "✓ Backup created: `$backupDir/`$backupFile" -ForegroundColor Green

    # Show backup size
    `$size = (Get-Item "`$backupDir/`$backupFile").Length
    Write-Host "  Size: `$(`$size / 1KB) KB"
} else {
    Write-Host "✗ Backup failed" -ForegroundColor Red
    exit 1
}
"@

$backupScriptPath = "scripts/backup-wallet-encryption-keys.ps1"
Set-Content -Path $backupScriptPath -Value $backupScript -Force
Write-Success "Backup script created: $backupScriptPath"

# Step 13: Create restore script
Write-Step "Creating restore script..."
$restoreScript = @"
#!/usr/bin/env pwsh
# Wallet Encryption Keys Restore Script
# Generated by setup-wallet-encryption-docker.ps1

param(
    [Parameter(Mandatory=`$true)]
    [string]`$BackupFile
)

if (-not (Test-Path `$BackupFile)) {
    Write-Host "✗ Backup file not found: `$BackupFile" -ForegroundColor Red
    exit 1
}

Write-Host "⚠ WARNING: This will replace all existing encryption keys" -ForegroundColor Yellow
Write-Host "  Backup file: `$BackupFile"
Write-Host ""
`$confirm = Read-Host "Type 'RESTORE' to confirm"

if (`$confirm -ne "RESTORE") {
    Write-Host "Restore cancelled"
    exit 0
}

# Stop wallet service
Write-Host "Stopping wallet service..."
docker compose stop wallet-service

# Restore keys
Write-Host "Restoring encryption keys..."
docker run --rm ``
  -v wallet-encryption-keys:/target ``
  -v "`$PWD/backups/wallet-keys:/backup:ro" ``
  alpine ``
  tar xzf /backup/`$(Split-Path `$BackupFile -Leaf) -C /target

if (`$LASTEXITCODE -eq 0) {
    Write-Host "✓ Restore complete" -ForegroundColor Green

    # Restart wallet service
    Write-Host "Restarting wallet service..."
    docker compose start wallet-service

    Write-Host "✓ Wallet service restarted" -ForegroundColor Green
} else {
    Write-Host "✗ Restore failed" -ForegroundColor Red
    exit 1
}
"@

$restoreScriptPath = "scripts/restore-wallet-encryption-keys.ps1"
Set-Content -Path $restoreScriptPath -Value $restoreScript -Force
Write-Success "Restore script created: $restoreScriptPath"

# Step 14: Summary
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Success "Wallet Service encryption configured for Docker"
Write-Host ""
Write-Info "Configuration:"
Write-Host "  • Encryption Provider: LinuxSecretService (fallback mode)"
Write-Host "  • Algorithm: AES-256-GCM (two-layer envelope encryption)"
Write-Host "  • Key Storage: Docker volume 'wallet-encryption-keys'"
Write-Host "  • Volume Mount: /var/lib/sorcha/wallet-keys"
Write-Host ""
Write-Info "Next Steps:"
Write-Host "  1. Verify service health:"
Write-Host "     docker logs sorcha-wallet-service"
Write-Host ""
Write-Host "  2. Test encryption endpoint:"
Write-Host "     curl http://localhost:8080/health"
Write-Host ""
Write-Host "  3. Create first backup:"
Write-Host "     pwsh scripts/backup-wallet-encryption-keys.ps1"
Write-Host ""
Write-Host "  4. Review encryption architecture:"
Write-Host "     docs/wallet-encryption-architecture.md"
Write-Host ""
Write-Warning "IMPORTANT: Schedule regular backups of encryption keys!"
Write-Warning "Without backups, losing the volume means PERMANENT DATA LOSS."
Write-Host ""
