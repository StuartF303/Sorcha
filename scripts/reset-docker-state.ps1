# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Resets the Docker state for Sorcha project by clearing all containers and database volumes.

.DESCRIPTION
    This script:
    1. Checks if Docker is running and healthy
    2. Prompts for confirmation (unless -Yes is specified)
    3. Stops all Sorcha containers
    4. Removes all Sorcha containers
    5. Removes all database volumes (PostgreSQL, MongoDB, Redis)
    6. Cleans up data protection keys
    7. Provides a fresh starting state for bootstrap

.PARAMETER Yes
    Skip confirmation prompt and proceed with reset automatically

.PARAMETER KeepVolumes
    Keep database volumes (only remove containers)

.EXAMPLE
    .\reset-docker-state.ps1
    Prompts for confirmation before resetting

.EXAMPLE
    .\reset-docker-state.ps1 -Yes
    Resets without confirmation (useful for CI/CD)

.EXAMPLE
    .\reset-docker-state.ps1 -KeepVolumes
    Only removes containers, keeps database volumes
#>

param(
    [Alias("y")]
    [switch]$Yes,

    [switch]$KeepVolumes
)

$ErrorActionPreference = "Stop"

# Script configuration
$COMPOSE_PROJECT = "sorcha"
$COMPOSE_FILE = "docker-compose.yml"

# Color output functions
function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Magenta
}

# Check if Docker is installed and running
function Test-Docker {
    Write-Step "Checking Docker status..."

    try {
        $null = docker version 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Docker is not running. Please start Docker Desktop and try again."
            exit 1
        }
        Write-Success "Docker is running"
    }
    catch {
        Write-Error "Docker is not installed or not accessible."
        Write-Info "Please install Docker Desktop from https://www.docker.com/products/docker-desktop"
        exit 1
    }
}

# Check Docker daemon health
function Test-DockerHealth {
    Write-Step "Checking Docker daemon health..."

    try {
        $info = docker info --format "{{.ServerVersion}}" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Docker daemon is healthy (version: $info)"
            return $true
        }
        else {
            Write-Warning "Docker daemon may not be fully ready"
            return $false
        }
    }
    catch {
        Write-Warning "Could not verify Docker daemon health"
        return $false
    }
}

# Get list of Sorcha containers
function Get-SorchaContainers {
    $containers = docker ps -a --filter "name=sorcha-" --format "{{.Names}}" 2>$null
    if ($LASTEXITCODE -eq 0 -and $containers) {
        return $containers -split "`n" | Where-Object { $_ -ne "" }
    }
    return @()
}

# Get list of Sorcha volumes
function Get-SorchaVolumes {
    $volumes = docker volume ls --filter "name=${COMPOSE_PROJECT}_" --format "{{.Name}}" 2>$null
    if ($LASTEXITCODE -eq 0 -and $volumes) {
        return $volumes -split "`n" | Where-Object { $_ -ne "" }
    }
    return @()
}

# Display current state
function Show-CurrentState {
    Write-Step "Current Sorcha Docker state:"

    $containers = Get-SorchaContainers
    $volumes = Get-SorchaVolumes

    if ($containers.Count -gt 0) {
        Write-Info "Containers ($($containers.Count)):"
        foreach ($container in $containers) {
            $status = docker inspect --format "{{.State.Status}}" $container 2>$null
            Write-Host "  - $container ($status)" -ForegroundColor Gray
        }
    }
    else {
        Write-Info "No Sorcha containers found"
    }

    if ($volumes.Count -gt 0) {
        Write-Info "`nVolumes ($($volumes.Count)):"
        foreach ($volume in $volumes) {
            Write-Host "  - $volume" -ForegroundColor Gray
        }
    }
    else {
        Write-Info "No Sorcha volumes found"
    }
}

# Confirm action
function Confirm-Reset {
    if ($Yes) {
        Write-Info "Auto-confirmed with -Yes flag"
        return $true
    }

    Write-Host ""
    Write-Warning "This will:"
    Write-Host "  1. Stop all Sorcha containers" -ForegroundColor Yellow
    Write-Host "  2. Remove all Sorcha containers" -ForegroundColor Yellow

    if (-not $KeepVolumes) {
        Write-Host "  3. Delete all database volumes (PostgreSQL, MongoDB, Redis)" -ForegroundColor Yellow
        Write-Host "  4. Delete data protection keys" -ForegroundColor Yellow
        Write-Host "`n  ⚠️  ALL DATABASE DATA WILL BE LOST!" -ForegroundColor Red
    }
    else {
        Write-Host "  3. Keep database volumes (as requested)" -ForegroundColor Green
    }

    Write-Host ""
    $response = Read-Host "Are you sure you want to continue? (yes/no)"

    return ($response -eq "yes" -or $response -eq "y")
}

# Stop all Sorcha containers
function Stop-SorchaContainers {
    Write-Step "Stopping Sorcha containers..."

    $containers = Get-SorchaContainers
    if ($containers.Count -eq 0) {
        Write-Info "No containers to stop"
        return
    }

    foreach ($container in $containers) {
        Write-Host "  Stopping $container..." -ForegroundColor Gray
        docker stop $container 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Stopped $container"
        }
        else {
            Write-Warning "Failed to stop $container (may already be stopped)"
        }
    }
}

# Remove all Sorcha containers
function Remove-SorchaContainers {
    Write-Step "Removing Sorcha containers..."

    $containers = Get-SorchaContainers
    if ($containers.Count -eq 0) {
        Write-Info "No containers to remove"
        return
    }

    foreach ($container in $containers) {
        Write-Host "  Removing $container..." -ForegroundColor Gray
        docker rm -f $container 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Removed $container"
        }
        else {
            Write-Warning "Failed to remove $container"
        }
    }
}

# Remove all Sorcha volumes
function Remove-SorchaVolumes {
    if ($KeepVolumes) {
        Write-Step "Skipping volume removal (KeepVolumes flag set)"
        return
    }

    Write-Step "Removing Sorcha volumes..."

    $volumes = Get-SorchaVolumes
    if ($volumes.Count -eq 0) {
        Write-Info "No volumes to remove"
        return
    }

    foreach ($volume in $volumes) {
        Write-Host "  Removing $volume..." -ForegroundColor Gray
        docker volume rm $volume 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Removed $volume"
        }
        else {
            Write-Warning "Failed to remove $volume (may be in use)"
        }
    }
}

# Verify cleanup
function Test-CleanState {
    Write-Step "Verifying clean state..."

    $containers = Get-SorchaContainers
    $volumes = Get-SorchaVolumes

    $success = $true

    if ($containers.Count -gt 0) {
        Write-Warning "Some containers still exist:"
        foreach ($container in $containers) {
            Write-Host "  - $container" -ForegroundColor Yellow
        }
        $success = $false
    }
    else {
        Write-Success "All containers removed"
    }

    if (-not $KeepVolumes) {
        if ($volumes.Count -gt 0) {
            Write-Warning "Some volumes still exist:"
            foreach ($volume in $volumes) {
                Write-Host "  - $volume" -ForegroundColor Yellow
            }
            $success = $false
        }
        else {
            Write-Success "All volumes removed"
        }
    }

    return $success
}

# Main execution
function Main {
    Write-Host @"
╔═══════════════════════════════════════════════════════════╗
║         Sorcha Docker State Reset Utility                 ║
║         Clean slate for fresh bootstrap                   ║
╚═══════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

    # Pre-flight checks
    Test-Docker
    Test-DockerHealth

    # Show current state
    Show-CurrentState

    # Confirm action
    if (-not (Confirm-Reset)) {
        Write-Warning "Reset cancelled by user"
        exit 0
    }

    # Execute reset
    Write-Host ""
    Stop-SorchaContainers
    Remove-SorchaContainers
    Remove-SorchaVolumes

    # Verify
    Write-Host ""
    if (Test-CleanState) {
        Write-Host ""
        Write-Success "════════════════════════════════════════════════════"
        Write-Success "  Docker state reset complete!"
        Write-Success "  Ready for fresh bootstrap"
        Write-Success "════════════════════════════════════════════════════"
        Write-Host ""
        Write-Info "Next steps:"
        Write-Host "  1. Run: docker-compose up -d" -ForegroundColor White
        Write-Host "  2. Run: .\scripts\bootstrap-sorcha.ps1" -ForegroundColor White
        Write-Host ""
    }
    else {
        Write-Host ""
        Write-Warning "════════════════════════════════════════════════════"
        Write-Warning "  Reset completed with warnings"
        Write-Warning "  Some resources may still exist"
        Write-Warning "════════════════════════════════════════════════════"
        Write-Host ""
        Write-Info "You may need to manually clean up remaining resources"
        exit 1
    }
}

# Run main function
Main
