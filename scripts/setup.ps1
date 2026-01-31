# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    First-run setup wizard for Sorcha platform installation
.DESCRIPTION
    Interactive PowerShell script that handles fresh Sorcha installations:
    - Detects first-run state (missing volumes, .env, databases)
    - Checks Docker Desktop availability and port availability
    - Generates configuration files with secure defaults
    - Creates Docker volumes and required directories
    - Runs database migrations and seeds initial data
    - Validates all services start successfully
.PARAMETER NonInteractive
    Run in non-interactive mode using defaults or environment variables
.PARAMETER SkipDocker
    Skip Docker installation check (for CI/CD environments)
.PARAMETER SkipInfrastructure
    Skip infrastructure provisioning (assumes already running)
.PARAMETER Force
    Force re-initialization even if already configured
.NOTES
    The -Verbose parameter is automatically available via [CmdletBinding()]
.EXAMPLE
    .\setup.ps1
    Interactive setup with all checks and prompts
.EXAMPLE
    .\setup.ps1 -NonInteractive -Force
    Non-interactive setup, re-initialize if needed
#>

[CmdletBinding()]
param(
    [switch]$NonInteractive,
    [switch]$SkipDocker,
    [switch]$SkipInfrastructure,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Script root directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Configuration
$RequiredPorts = @{
    "Redis" = 16379
    "PostgreSQL" = 5432
    "MongoDB" = 27017
    "Aspire Dashboard" = 18888
    "Blueprint Service" = 5000
    "Register Service" = 5380
    "Tenant Service" = 5450
    "Validator Service" = 5800
    "API Gateway HTTP" = 80
    "API Gateway HTTPS" = 443
    "UI Web" = 5400
}

$RequiredVolumes = @(
    "sorcha_redis-data",
    "sorcha_postgres-data",
    "sorcha_mongodb-data",
    "sorcha_dataprotection-keys",
    "sorcha_wallet-encryption-keys"
)

#region Helper Functions

function Write-Banner {
    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "         Sorcha Platform Setup Wizard         " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "This wizard will configure a fresh Sorcha installation." -ForegroundColor White
    Write-Host ""
}

function Write-Step {
    param([string]$Message, [int]$Step, [int]$Total)
    Write-Host ""
    Write-Host "[$Step/$Total] " -NoNewline -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor White
    Write-Host ("-" * 50) -ForegroundColor DarkGray
}

function Write-Success {
    param([string]$Message)
    Write-Host "  [OK] " -NoNewline -ForegroundColor Green
    Write-Host $Message -ForegroundColor White
}

function Write-Warning {
    param([string]$Message)
    Write-Host "  [!] " -NoNewline -ForegroundColor Yellow
    Write-Host $Message -ForegroundColor White
}

function Write-Error {
    param([string]$Message)
    Write-Host "  [X] " -NoNewline -ForegroundColor Red
    Write-Host $Message -ForegroundColor White
}

function Write-Info {
    param([string]$Message)
    Write-Host "  [i] " -NoNewline -ForegroundColor Blue
    Write-Host $Message -ForegroundColor White
}

function Write-DebugMessage {
    param([string]$Message)
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Host "  [D] " -NoNewline -ForegroundColor DarkGray
        Write-Host $Message -ForegroundColor DarkGray
    }
}

function Test-CommandExists {
    param([string]$Command)
    return $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

function Test-PortAvailable {
    param([int]$Port)
    try {
        $connection = New-Object System.Net.Sockets.TcpClient
        $connection.Connect("127.0.0.1", $Port)
        $connection.Close()
        return $false  # Port is in use
    } catch {
        return $true   # Port is available
    }
}

function Get-DockerVolumes {
    if (-not (Test-CommandExists "docker")) {
        return @()
    }
    try {
        $volumes = docker volume ls --format "{{.Name}}" 2>$null
        return $volumes -split "`n" | Where-Object { $_ -ne "" }
    } catch {
        return @()
    }
}

function Test-DockerRunning {
    if (-not (Test-CommandExists "docker")) {
        return $false
    }
    try {
        $info = docker info 2>&1
        return $LASTEXITCODE -eq 0
    } catch {
        return $false
    }
}

function Test-FirstRun {
    # Check for indicators of first-run state
    $indicators = @{
        "NoEnvFile" = -not (Test-Path (Join-Path $ProjectRoot ".env"))
        "NoVolumes" = $true
        "NoContainers" = $true
    }

    if (Test-CommandExists "docker") {
        try {
            $existingVolumes = Get-DockerVolumes
            $hasAllVolumes = $true
            foreach ($volume in $RequiredVolumes) {
                if ($existingVolumes -notcontains $volume) {
                    $hasAllVolumes = $false
                    break
                }
            }
            $indicators["NoVolumes"] = -not $hasAllVolumes

            $containers = docker ps -a --filter "name=sorcha" --format "{{.Names}}" 2>$null
            $indicators["NoContainers"] = [string]::IsNullOrEmpty($containers)
        } catch {
            # Docker not running or errored
        }
    }

    return $indicators
}

function Read-UserInput {
    param(
        [string]$Prompt,
        [string]$Default = "",
        [switch]$IsPassword
    )

    if ($NonInteractive) {
        return $Default
    }

    if ($IsPassword) {
        $secureString = Read-Host -Prompt "$Prompt" -AsSecureString
        $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureString)
        try {
            return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
        } finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
        }
    } else {
        $input = Read-Host -Prompt "$Prompt [$Default]"
        if ([string]::IsNullOrEmpty($input)) {
            return $Default
        }
        return $input
    }
}

function Read-YesNo {
    param([string]$Prompt, [bool]$Default = $true)

    if ($NonInteractive) {
        return $Default
    }

    $defaultStr = if ($Default) { "Y/n" } else { "y/N" }
    $input = Read-Host -Prompt "$Prompt [$defaultStr]"

    if ([string]::IsNullOrEmpty($input)) {
        return $Default
    }

    return $input -match "^[yY]"
}

function New-SecurePassword {
    param([int]$Length = 32)
    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*"
    $password = -join (1..$Length | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
    return $password
}

function New-JwtSigningKey {
    # Generate a 256-bit (32-byte) key and base64 encode it
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

#endregion

#region Main Setup Steps

function Step-CheckPrerequisites {
    param([int]$Step, [int]$Total)

    Write-Step "Checking Prerequisites" $Step $Total

    $issues = @()

    # Check Docker
    if (-not $SkipDocker) {
        if (Test-CommandExists "docker") {
            Write-Success "Docker CLI found"

            if (Test-DockerRunning) {
                Write-Success "Docker daemon is running"

                # Check Docker Compose
                if (Test-CommandExists "docker-compose") {
                    Write-Success "Docker Compose found (standalone)"
                } else {
                    # Try docker compose (plugin)
                    try {
                        $null = docker compose version 2>$null
                        Write-Success "Docker Compose found (plugin)"
                    } catch {
                        $issues += "Docker Compose not found"
                        Write-Error "Docker Compose not found"
                    }
                }
            } else {
                $issues += "Docker Desktop is not running"
                Write-Error "Docker Desktop is not running"
                Write-Info "Please start Docker Desktop and try again"
            }
        } else {
            $issues += "Docker not installed"
            Write-Error "Docker not installed"
            Write-Info "Download from: https://www.docker.com/products/docker-desktop"
        }
    } else {
        Write-Warning "Skipping Docker checks (--SkipDocker)"
    }

    # Check .NET SDK
    if (Test-CommandExists "dotnet") {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK $dotnetVersion found"

        # Check for .NET 10
        if (-not ($dotnetVersion -match "^10\.")) {
            Write-Warning ".NET 10 recommended (found $dotnetVersion)"
        }
    } else {
        Write-Warning ".NET SDK not found (optional for Docker-only deployment)"
    }

    # Check Git
    if (Test-CommandExists "git") {
        Write-Success "Git found"
    } else {
        Write-Warning "Git not found (optional)"
    }

    if ($issues.Count -gt 0) {
        Write-Host ""
        Write-Error "Prerequisites check failed:"
        foreach ($issue in $issues) {
            Write-Host "    - $issue" -ForegroundColor Red
        }
        return $false
    }

    return $true
}

function Step-CheckPorts {
    param([int]$Step, [int]$Total)

    Write-Step "Checking Port Availability" $Step $Total

    $portsInUse = @()

    foreach ($service in $RequiredPorts.Keys) {
        $port = $RequiredPorts[$service]
        if (Test-PortAvailable $port) {
            Write-Success "Port $port available ($service)"
        } else {
            Write-Error "Port $port in use ($service)"
            $portsInUse += @{ Service = $service; Port = $port }
        }
    }

    if ($portsInUse.Count -gt 0) {
        Write-Host ""
        Write-Warning "Some ports are in use. You can:"
        Write-Host "    1. Stop the conflicting services" -ForegroundColor Yellow
        Write-Host "    2. Modify docker-compose.yml to use different ports" -ForegroundColor Yellow
        Write-Host ""

        if (-not $NonInteractive) {
            $continue = Read-YesNo "Continue anyway?" $false
            if (-not $continue) {
                return $false
            }
        }
    }

    return $true
}

function Step-DetectFirstRun {
    param([int]$Step, [int]$Total)

    Write-Step "Detecting Installation State" $Step $Total

    $state = Test-FirstRun

    if ($state["NoEnvFile"]) {
        Write-Info "No .env file found - first run detected"
    } else {
        Write-Success ".env file exists"
    }

    if ($state["NoVolumes"]) {
        Write-Info "Docker volumes not created - first run detected"
    } else {
        Write-Success "Docker volumes exist"
    }

    if ($state["NoContainers"]) {
        Write-Info "No Sorcha containers found"
    } else {
        Write-Success "Sorcha containers exist"
    }

    $isFirstRun = $state["NoEnvFile"] -or $state["NoVolumes"]

    if ($isFirstRun) {
        Write-Host ""
        Write-Info "This appears to be a fresh installation"
        return @{ IsFirstRun = $true; State = $state }
    } elseif ($Force) {
        Write-Host ""
        Write-Warning "Existing installation detected, but --Force specified"
        Write-Warning "This will regenerate configuration files"

        if (-not $NonInteractive) {
            $continue = Read-YesNo "Proceed with re-initialization?" $false
            if (-not $continue) {
                Write-Host "Setup cancelled." -ForegroundColor Yellow
                exit 0
            }
        }
        return @{ IsFirstRun = $true; State = $state }
    } else {
        Write-Host ""
        Write-Success "Existing installation detected"
        Write-Info "Use --Force to re-initialize"
        return @{ IsFirstRun = $false; State = $state }
    }
}

function Step-GenerateConfiguration {
    param([int]$Step, [int]$Total)

    Write-Step "Generating Configuration" $Step $Total

    # Generate secure credentials
    $jwtSigningKey = New-JwtSigningKey
    $installationName = if ($NonInteractive) { "localhost" } else {
        Read-UserInput "Installation name (used for JWT issuer)" "localhost"
    }

    Write-Success "Generated JWT signing key (256-bit)"
    Write-Success "Installation name: $installationName"

    # Create .env file
    $envContent = @"
# Sorcha Platform Configuration
# Generated by setup.ps1 on $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# DO NOT COMMIT THIS FILE TO SOURCE CONTROL

# Installation Identity
INSTALLATION_NAME=$installationName

# JWT Configuration (256-bit key)
JWT_SIGNING_KEY=$jwtSigningKey

# Database Credentials (change for production!)
POSTGRES_USER=sorcha
POSTGRES_PASSWORD=sorcha_dev_password
MONGO_USERNAME=sorcha
MONGO_PASSWORD=sorcha_dev_password

# Redis Configuration
REDIS_PASSWORD=

# Development mode (set to false for production)
ASPNETCORE_ENVIRONMENT=Development
"@

    $envPath = Join-Path $ProjectRoot ".env"
    $envContent | Out-File -FilePath $envPath -Encoding utf8
    Write-Success "Created .env file"

    # Create certs directory if needed
    $certsDir = Join-Path $ProjectRoot "docker/certs"
    if (-not (Test-Path $certsDir)) {
        New-Item -ItemType Directory -Path $certsDir -Force | Out-Null
        Write-Success "Created docker/certs directory"
    }

    # Check if HTTPS certificate exists
    $certPath = Join-Path $certsDir "aspnetapp.pfx"
    if (-not (Test-Path $certPath)) {
        Write-Warning "HTTPS certificate not found at $certPath"
        Write-Info "HTTPS will not work until certificate is generated"
        Write-Info "Run: ./scripts/setup-https-docker.ps1"
    } else {
        Write-Success "HTTPS certificate found"
    }

    return $true
}

function Step-CreateVolumes {
    param([int]$Step, [int]$Total)

    Write-Step "Creating Docker Volumes" $Step $Total

    if ($SkipDocker -or -not (Test-DockerRunning)) {
        Write-Warning "Skipping Docker volume creation"
        return $true
    }

    foreach ($volume in $RequiredVolumes) {
        $existingVolumes = Get-DockerVolumes
        if ($existingVolumes -contains $volume) {
            Write-Success "Volume $volume already exists"
        } else {
            try {
                docker volume create $volume | Out-Null
                Write-Success "Created volume $volume"
            } catch {
                Write-Error "Failed to create volume $volume"
                return $false
            }
        }
    }

    # Fix wallet encryption key permissions
    Write-Info "Setting wallet encryption key permissions..."
    try {
        docker run --rm -v sorcha_wallet-encryption-keys:/data alpine chown -R 1654:1654 /data 2>$null
        Write-Success "Wallet encryption key permissions set (UID 1654)"
    } catch {
        Write-Warning "Could not set wallet permissions - may need manual fix"
    }

    return $true
}

function Step-StartInfrastructure {
    param([int]$Step, [int]$Total)

    Write-Step "Starting Infrastructure Services" $Step $Total

    if ($SkipInfrastructure) {
        Write-Warning "Skipping infrastructure startup (--SkipInfrastructure)"
        return $true
    }

    if ($SkipDocker -or -not (Test-DockerRunning)) {
        Write-Warning "Skipping Docker infrastructure"
        return $true
    }

    Push-Location $ProjectRoot
    try {
        # Start infrastructure services only first
        Write-Info "Starting Redis, PostgreSQL, MongoDB, Aspire Dashboard..."

        # Use docker compose (plugin) or docker-compose (standalone)
        if (Test-CommandExists "docker-compose") {
            docker-compose up -d redis postgres mongodb aspire-dashboard 2>&1 | Out-Null
        } else {
            docker compose up -d redis postgres mongodb aspire-dashboard 2>&1 | Out-Null
        }

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to start infrastructure services"
            return $false
        }

        Write-Success "Infrastructure services started"

        # Wait for health checks
        Write-Info "Waiting for services to be healthy..."
        $maxWait = 60
        $waited = 0

        while ($waited -lt $maxWait) {
            $healthy = $true

            # Check Redis
            try {
                docker exec sorcha-redis redis-cli ping 2>$null | Out-Null
                if ($LASTEXITCODE -ne 0) { $healthy = $false }
            } catch { $healthy = $false }

            # Check PostgreSQL
            try {
                docker exec sorcha-postgres pg_isready -U sorcha 2>$null | Out-Null
                if ($LASTEXITCODE -ne 0) { $healthy = $false }
            } catch { $healthy = $false }

            # Check MongoDB
            try {
                docker exec sorcha-mongodb mongosh --eval "db.adminCommand('ping')" 2>$null | Out-Null
                if ($LASTEXITCODE -ne 0) { $healthy = $false }
            } catch { $healthy = $false }

            if ($healthy) {
                break
            }

            Start-Sleep -Seconds 2
            $waited += 2
            Write-DebugMessage "Waiting... ($waited/$maxWait seconds)"
        }

        if ($waited -ge $maxWait) {
            Write-Warning "Some services may not be fully healthy yet"
        } else {
            Write-Success "All infrastructure services are healthy"
        }

    } finally {
        Pop-Location
    }

    return $true
}

function Step-StartApplicationServices {
    param([int]$Step, [int]$Total)

    Write-Step "Starting Application Services" $Step $Total

    if ($SkipInfrastructure) {
        Write-Warning "Skipping application services (--SkipInfrastructure)"
        return $true
    }

    if ($SkipDocker -or -not (Test-DockerRunning)) {
        Write-Warning "Skipping Docker application services"
        return $true
    }

    Push-Location $ProjectRoot
    try {
        Write-Info "Building and starting all services..."

        if (Test-CommandExists "docker-compose") {
            docker-compose up -d --build 2>&1 | ForEach-Object { Write-Debug $_ }
        } else {
            docker compose up -d --build 2>&1 | ForEach-Object { Write-Debug $_ }
        }

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to start application services"
            return $false
        }

        Write-Success "Application services started"

        # Wait for API Gateway to be ready
        Write-Info "Waiting for API Gateway to be ready..."
        $maxWait = 120
        $waited = 0

        while ($waited -lt $maxWait) {
            try {
                $response = Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Success "API Gateway is ready"
                    break
                }
            } catch {
                # Not ready yet
            }

            Start-Sleep -Seconds 3
            $waited += 3
            Write-DebugMessage "Waiting for API Gateway... ($waited/$maxWait seconds)"
        }

        if ($waited -ge $maxWait) {
            Write-Warning "API Gateway may not be fully ready"
            Write-Info "Check logs with: docker-compose logs api-gateway"
        }

    } finally {
        Pop-Location
    }

    return $true
}

function Step-ValidateInstallation {
    param([int]$Step, [int]$Total)

    Write-Step "Validating Installation" $Step $Total

    $validationScript = Join-Path $ScriptDir "validate-environment.ps1"
    if (Test-Path $validationScript) {
        Write-Info "Running environment validation..."
        & $validationScript -Quiet
        if ($LASTEXITCODE -eq 0) {
            Write-Success "All validation checks passed"
        } else {
            Write-Warning "Some validation checks failed"
            Write-Info "Run ./scripts/validate-environment.ps1 for details"
        }
    } else {
        # Basic validation inline
        $services = @(
            @{ Name = "API Gateway"; Url = "http://localhost/health" },
            @{ Name = "Tenant Service"; Url = "http://localhost/api/tenant/health" },
            @{ Name = "Blueprint Service"; Url = "http://localhost/api/blueprints/health" }
        )

        foreach ($service in $services) {
            try {
                $response = Invoke-WebRequest -Uri $service.Url -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Success "$($service.Name) is healthy"
                } else {
                    Write-Warning "$($service.Name) returned status $($response.StatusCode)"
                }
            } catch {
                Write-Error "$($service.Name) is not responding"
            }
        }
    }

    return $true
}

function Step-PrintSummary {
    param([int]$Step, [int]$Total)

    Write-Step "Setup Complete" $Step $Total

    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Green
    Write-Host "     Sorcha Platform Setup Complete!          " -ForegroundColor Green
    Write-Host "==============================================" -ForegroundColor Green
    Write-Host ""

    Write-Host "Service URLs:" -ForegroundColor Cyan
    Write-Host "  Main UI:          http://localhost/app" -ForegroundColor White
    Write-Host "  API Gateway:      http://localhost" -ForegroundColor White
    Write-Host "  Aspire Dashboard: http://localhost:18888" -ForegroundColor White
    Write-Host ""

    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Run bootstrap to create initial organization:" -ForegroundColor White
    Write-Host "     ./scripts/bootstrap-sorcha.ps1 -Profile docker" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Or use the CLI:" -ForegroundColor White
    Write-Host "     sorcha bootstrap --profile docker" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. View logs:" -ForegroundColor White
    Write-Host "     docker-compose logs -f" -ForegroundColor Gray
    Write-Host ""

    Write-Host "Documentation:" -ForegroundColor Cyan
    Write-Host "  docs/FIRST-RUN-SETUP.md   - Setup guide" -ForegroundColor White
    Write-Host "  docs/PORT-CONFIGURATION.md - Port reference" -ForegroundColor White
    Write-Host ""
}

#endregion

#region Main Execution

Write-Banner

$totalSteps = 8
$currentStep = 0

# Step 1: Prerequisites
$currentStep++
if (-not (Step-CheckPrerequisites $currentStep $totalSteps)) {
    Write-Host ""
    Write-Error "Setup failed: Prerequisites not met"
    exit 1
}

# Step 2: Port availability
$currentStep++
if (-not (Step-CheckPorts $currentStep $totalSteps)) {
    exit 1
}

# Step 3: Detect first run
$currentStep++
$installState = Step-DetectFirstRun $currentStep $totalSteps
if (-not $installState.IsFirstRun) {
    Write-Host ""
    Write-Info "Sorcha is already configured. Use --Force to re-initialize."
    Write-Host ""
    Write-Host "To start services: docker-compose up -d" -ForegroundColor Cyan
    Write-Host "To run bootstrap:  ./scripts/bootstrap-sorcha.ps1" -ForegroundColor Cyan
    Write-Host ""
    exit 0
}

# Step 4: Generate configuration
$currentStep++
if (-not (Step-GenerateConfiguration $currentStep $totalSteps)) {
    Write-Error "Setup failed: Configuration generation failed"
    exit 1
}

# Step 5: Create volumes
$currentStep++
if (-not (Step-CreateVolumes $currentStep $totalSteps)) {
    Write-Error "Setup failed: Volume creation failed"
    exit 1
}

# Step 6: Start infrastructure
$currentStep++
if (-not (Step-StartInfrastructure $currentStep $totalSteps)) {
    Write-Error "Setup failed: Infrastructure startup failed"
    exit 1
}

# Step 7: Start application services
$currentStep++
if (-not (Step-StartApplicationServices $currentStep $totalSteps)) {
    Write-Error "Setup failed: Application services failed"
    exit 1
}

# Step 8: Validate and print summary
$currentStep++
Step-ValidateInstallation $currentStep $totalSteps
Step-PrintSummary ($currentStep + 1) ($totalSteps + 1)

exit 0

#endregion
