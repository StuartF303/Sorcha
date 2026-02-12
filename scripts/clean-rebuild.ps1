# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Clean rebuild of all Sorcha Docker services with fresh data.

.DESCRIPTION
    This script performs a full clean rebuild of the Sorcha platform:
    1. Pulls latest code (optional git pull)
    2. Stops and removes all Sorcha containers
    3. Removes all Sorcha data volumes for a clean slate
    4. Rebuilds all Docker images from source
    5. Fixes volume permissions (wallet encryption keys)
    6. Starts all services
    7. Waits for infrastructure health (Redis, PostgreSQL, MongoDB)
    8. Verifies application service health via /health endpoints

.PARAMETER Yes
    Skip confirmation prompt and proceed automatically.

.PARAMETER SkipPull
    Skip git pull (use current local code as-is).

.PARAMETER SkipBuild
    Skip Docker image rebuild (use existing images).

.PARAMETER TimeoutSeconds
    Maximum seconds to wait for each service health check (default: 120).

.EXAMPLE
    .\clean-rebuild.ps1
    Interactive clean rebuild with confirmation prompt.

.EXAMPLE
    .\clean-rebuild.ps1 -Yes
    Unattended clean rebuild (useful for CI/CD).

.EXAMPLE
    .\clean-rebuild.ps1 -SkipPull -Yes
    Rebuild without pulling latest code.
#>

param(
    [Alias("y")]
    [switch]$Yes,

    [switch]$SkipPull,

    [switch]$SkipBuild,

    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ─── Configuration ───────────────────────────────────────────────────────────

$COMPOSE_PROJECT = "sorcha"
$PROJECT_ROOT = Split-Path -Parent $PSScriptRoot

$INFRA_SERVICES = @("redis", "postgres", "mongodb")
$APP_SERVICES = @(
    "blueprint-service",
    "wallet-service",
    "register-service",
    "tenant-service",
    "peer-service",
    "validator-service",
    "sorcha-ui-web",
    "api-gateway"
)

# Service health endpoints: service-name -> host:port
# Wallet service has no published port; check via API Gateway
$HEALTH_ENDPOINTS = @{
    "blueprint-service"  = "http://localhost:5000/health"
    "register-service"   = "http://localhost:5380/health"
    "tenant-service"     = "http://localhost:5450/health"
    "validator-service"  = "http://localhost:5800/health"
    "sorcha-ui-web"      = "http://localhost:5400/health"
    "api-gateway"        = "http://localhost:80/health"
}

# ─── Output Helpers ──────────────────────────────────────────────────────────

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Magenta
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Err {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

# ─── Pre-flight Checks ──────────────────────────────────────────────────────

function Test-Docker {
    Write-Step "Checking Docker status..."

    try {
        $null = docker version 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Err "Docker is not running. Please start Docker Desktop and try again."
            exit 1
        }
        Write-Success "Docker is running"
    }
    catch {
        Write-Err "Docker is not installed or not accessible."
        exit 1
    }
}

# ─── Confirmation ────────────────────────────────────────────────────────────

function Confirm-Rebuild {
    if ($Yes) {
        Write-Info "Auto-confirmed with -Yes flag"
        return $true
    }

    Write-Host ""
    Write-Warning "This will:"
    Write-Host "  1. Stop and remove all Sorcha containers" -ForegroundColor Yellow
    Write-Host "  2. Delete ALL data volumes (PostgreSQL, MongoDB, Redis, keys)" -ForegroundColor Yellow
    Write-Host "  3. Rebuild all Docker images from source" -ForegroundColor Yellow
    Write-Host "  4. Start all services fresh" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  ALL DATABASE DATA WILL BE LOST!" -ForegroundColor Red
    Write-Host ""
    $response = Read-Host "Are you sure you want to continue? (yes/no)"

    return ($response -eq "yes" -or $response -eq "y")
}

# ─── Git Pull ────────────────────────────────────────────────────────────────

function Update-Source {
    if ($SkipPull) {
        Write-Info "Skipping git pull (-SkipPull)"
        return
    }

    Write-Step "Pulling latest code..."

    Push-Location $PROJECT_ROOT
    try {
        $branch = git rev-parse --abbrev-ref HEAD 2>$null
        Write-Info "Current branch: $branch"

        git pull --ff-only 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "git pull failed — continuing with current local code"
        }
        else {
            $sha = git rev-parse --short HEAD 2>$null
            Write-Success "Code is up to date (${branch}@${sha})"
        }
    }
    finally {
        Pop-Location
    }
}

# ─── Teardown ────────────────────────────────────────────────────────────────

function Remove-AllContainersAndVolumes {
    Write-Step "Stopping and removing containers + volumes..."

    Push-Location $PROJECT_ROOT
    try {
        docker compose down -v --remove-orphans 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        if ($LASTEXITCODE -eq 0) {
            Write-Success "All containers and volumes removed"
        }
        else {
            Write-Warning "docker compose down reported issues — attempting manual cleanup"
            # Fallback: stop and remove individually
            $containers = docker ps -a --filter "name=sorcha-" --format "{{.Names}}" 2>$null
            if ($containers) {
                docker stop ($containers -split "`n") 2>&1 | Out-Null
                docker rm -f ($containers -split "`n") 2>&1 | Out-Null
            }
            # Remove volumes
            $volumes = docker volume ls --filter "name=${COMPOSE_PROJECT}_" --format "{{.Name}}" 2>$null
            if ($volumes) {
                foreach ($vol in ($volumes -split "`n" | Where-Object { $_ -ne "" })) {
                    docker volume rm $vol 2>&1 | Out-Null
                }
            }
            Write-Success "Manual cleanup complete"
        }
    }
    finally {
        Pop-Location
    }
}

# ─── Build ───────────────────────────────────────────────────────────────────

function Build-AllImages {
    if ($SkipBuild) {
        Write-Info "Skipping Docker build (-SkipBuild)"
        return
    }

    Write-Step "Building all Docker images (this may take a few minutes)..."

    Push-Location $PROJECT_ROOT
    try {
        docker compose build --parallel 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        if ($LASTEXITCODE -ne 0) {
            Write-Err "Docker build failed. Check the output above for errors."
            exit 1
        }
        Write-Success "All images built successfully"
    }
    finally {
        Pop-Location
    }
}

# ─── Start Services ─────────────────────────────────────────────────────────

function Start-AllServices {
    Write-Step "Starting all services..."

    Push-Location $PROJECT_ROOT
    try {
        docker compose up -d 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        if ($LASTEXITCODE -ne 0) {
            Write-Err "Failed to start services."
            exit 1
        }
        Write-Success "All services started"
    }
    finally {
        Pop-Location
    }
}

# ─── Fix Permissions ────────────────────────────────────────────────────────

function Fix-VolumePermissions {
    Write-Step "Fixing wallet encryption key volume permissions..."

    # The wallet-encryption-keys volume is created with root ownership but the
    # wallet service runs as non-root (UID 1654). Fix with a temporary alpine container.
    docker run --rm -v "${COMPOSE_PROJECT}_wallet-encryption-keys:/data" alpine chown -R 1654:1654 /data 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Wallet encryption key permissions fixed (UID 1654)"
    }
    else {
        Write-Warning "Could not fix wallet key permissions — wallet service may fail on first key write"
    }
}

# ─── Infrastructure Health ───────────────────────────────────────────────────

function Wait-InfrastructureHealth {
    Write-Step "Waiting for infrastructure services..."

    foreach ($svc in $INFRA_SERVICES) {
        $containerName = "sorcha-$svc"
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        $healthy = $false

        Write-Host "  Waiting for $svc..." -ForegroundColor Gray -NoNewline

        while ((Get-Date) -lt $deadline) {
            $status = docker inspect --format "{{.State.Health.Status}}" $containerName 2>$null
            if ($status -eq "healthy") {
                $healthy = $true
                break
            }
            Start-Sleep -Seconds 2
            Write-Host "." -ForegroundColor Gray -NoNewline
        }

        if ($healthy) {
            Write-Host ""
            Write-Success "$svc is healthy"
        }
        else {
            Write-Host ""
            Write-Err "$svc did not become healthy within ${TimeoutSeconds}s"
            Write-Info "Logs:"
            docker logs $containerName --tail 20 2>&1 | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
            exit 1
        }
    }
}

# ─── Application Service Health ──────────────────────────────────────────────

function Wait-ServiceHealth {
    Write-Step "Verifying application service health..."

    # Give services a moment to start up after infrastructure is ready
    Write-Info "Waiting 10s for services to initialize..."
    Start-Sleep -Seconds 10

    $allHealthy = $true
    $results = @()

    foreach ($entry in $HEALTH_ENDPOINTS.GetEnumerator()) {
        $svc = $entry.Key
        $url = $entry.Value
        $containerName = "sorcha-$($svc -replace 'sorcha-', '')"
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        $healthy = $false

        Write-Host "  Checking $svc..." -ForegroundColor Gray -NoNewline

        while ((Get-Date) -lt $deadline) {
            # First check the container is still running
            $containerStatus = docker inspect --format "{{.State.Status}}" $containerName 2>$null
            if ($containerStatus -ne "running") {
                Write-Host ""
                Write-Err "$svc container is not running (status: $containerStatus)"
                Write-Info "Last 15 log lines:"
                docker logs $containerName --tail 15 2>&1 | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
                $allHealthy = $false
                $results += @{ Service = $svc; Status = "NOT RUNNING" }
                break
            }

            try {
                $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5 -UseBasicParsing -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    $healthy = $true
                    break
                }
            }
            catch {
                # Service not ready yet — keep waiting
            }

            Start-Sleep -Seconds 3
            Write-Host "." -ForegroundColor Gray -NoNewline
        }

        if ($healthy) {
            Write-Host ""
            Write-Success "$svc is healthy"
            $results += @{ Service = $svc; Status = "HEALTHY" }
        }
        elseif ($containerStatus -eq "running") {
            Write-Host ""
            Write-Warning "$svc did not respond to health check within ${TimeoutSeconds}s"
            $allHealthy = $false
            $results += @{ Service = $svc; Status = "UNHEALTHY" }
        }
    }

    # Check wallet-service via container status (no published port)
    Write-Host "  Checking wallet-service (container)..." -ForegroundColor Gray -NoNewline
    $deadline = (Get-Date).AddSeconds(30)
    $walletOk = $false
    while ((Get-Date) -lt $deadline) {
        $ws = docker inspect --format "{{.State.Status}}" sorcha-wallet-service 2>$null
        if ($ws -eq "running") {
            # Check it hasn't been restarting by verifying uptime
            $startedAt = docker inspect --format "{{.State.StartedAt}}" sorcha-wallet-service 2>$null
            if ($startedAt) {
                $walletOk = $true
                break
            }
        }
        Start-Sleep -Seconds 2
        Write-Host "." -ForegroundColor Gray -NoNewline
    }
    Write-Host ""
    if ($walletOk) {
        Write-Success "wallet-service is running"
        $results += @{ Service = "wallet-service"; Status = "RUNNING" }
    }
    else {
        Write-Warning "wallet-service may not be healthy"
        $allHealthy = $false
        $results += @{ Service = "wallet-service"; Status = "UNKNOWN" }
    }

    # Check peer-service container status (gRPC only, no /health on published port)
    Write-Host "  Checking peer-service (container)..." -ForegroundColor Gray -NoNewline
    $deadline = (Get-Date).AddSeconds(30)
    $peerOk = $false
    while ((Get-Date) -lt $deadline) {
        $ps = docker inspect --format "{{.State.Status}}" sorcha-peer-service 2>$null
        if ($ps -eq "running") {
            $peerOk = $true
            break
        }
        Start-Sleep -Seconds 2
        Write-Host "." -ForegroundColor Gray -NoNewline
    }
    Write-Host ""
    if ($peerOk) {
        Write-Success "peer-service is running"
        $results += @{ Service = "peer-service"; Status = "RUNNING" }
    }
    else {
        Write-Warning "peer-service may not be healthy"
        $allHealthy = $false
        $results += @{ Service = "peer-service"; Status = "UNKNOWN" }
    }

    return @{ AllHealthy = $allHealthy; Results = $results }
}

# ─── Summary ─────────────────────────────────────────────────────────────────

function Show-Summary {
    param(
        [hashtable]$HealthResults
    )

    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Clean Rebuild Summary" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""

    # Service status table
    Write-Host "  Service                    Status" -ForegroundColor White
    Write-Host "  ───────────────────────    ──────────" -ForegroundColor Gray

    foreach ($r in $HealthResults.Results) {
        $svcPadded = $r.Service.PadRight(27)
        switch ($r.Status) {
            "HEALTHY"     { Write-Host "  $svcPadded" -ForegroundColor White -NoNewline; Write-Host $r.Status -ForegroundColor Green }
            "RUNNING"     { Write-Host "  $svcPadded" -ForegroundColor White -NoNewline; Write-Host $r.Status -ForegroundColor Green }
            "UNHEALTHY"   { Write-Host "  $svcPadded" -ForegroundColor White -NoNewline; Write-Host $r.Status -ForegroundColor Red }
            "NOT RUNNING" { Write-Host "  $svcPadded" -ForegroundColor White -NoNewline; Write-Host $r.Status -ForegroundColor Red }
            default       { Write-Host "  $svcPadded" -ForegroundColor White -NoNewline; Write-Host $r.Status -ForegroundColor Yellow }
        }
    }

    Write-Host ""

    if ($HealthResults.AllHealthy) {
        Write-Success "════════════════════════════════════════════════════════════════"
        Write-Success "  All services are healthy! Clean rebuild complete."
        Write-Success "════════════════════════════════════════════════════════════════"
        Write-Host ""
        Write-Info "Access points:"
        Write-Host "  API Gateway:        http://localhost:80" -ForegroundColor White
        Write-Host "  Main UI:            http://localhost/app" -ForegroundColor White
        Write-Host "  Aspire Dashboard:   http://localhost:18888" -ForegroundColor White
        Write-Host ""
        Write-Info "Next steps:"
        Write-Host "  1. Bootstrap:  .\scripts\bootstrap-sorcha.ps1" -ForegroundColor White
        Write-Host "  2. View logs:  docker compose logs -f" -ForegroundColor White
        Write-Host ""
    }
    else {
        Write-Warning "════════════════════════════════════════════════════════════════"
        Write-Warning "  Clean rebuild completed with warnings."
        Write-Warning "  Some services may need attention."
        Write-Warning "════════════════════════════════════════════════════════════════"
        Write-Host ""
        Write-Info "Troubleshooting:"
        Write-Host "  View logs:          docker compose logs -f <service>" -ForegroundColor White
        Write-Host "  Check containers:   docker compose ps" -ForegroundColor White
        Write-Host "  Fix wallet perms:   .\scripts\fix-wallet-encryption-permissions.ps1" -ForegroundColor White
        Write-Host ""
        exit 1
    }
}

# ─── Main ────────────────────────────────────────────────────────────────────

function Main {
    Write-Host @"
╔═══════════════════════════════════════════════════════════╗
║           Sorcha Clean Rebuild                            ║
║           Fresh images, clean data, verified health        ║
╚═══════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    # Pre-flight
    Test-Docker

    # Confirmation
    if (-not (Confirm-Rebuild)) {
        Write-Warning "Rebuild cancelled by user"
        exit 0
    }

    # Execute rebuild pipeline
    Update-Source
    Remove-AllContainersAndVolumes
    Build-AllImages
    Fix-VolumePermissions
    Start-AllServices
    Wait-InfrastructureHealth

    $healthResults = Wait-ServiceHealth

    $stopwatch.Stop()
    $elapsed = $stopwatch.Elapsed
    Write-Info "Total time: $($elapsed.Minutes)m $($elapsed.Seconds)s"

    Show-Summary -HealthResults $healthResults
}

# Run
Main
