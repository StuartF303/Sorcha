<#
.SYNOPSIS
    Bootstrap script for initial Sorcha platform setup using Sorcha CLI
.DESCRIPTION
    Interactive PowerShell script to configure a fresh Sorcha installation using
    the `sorcha bootstrap` command. This script:
    - Reads existing installation records from CLI config (~/.sorcha/config.json)
    - Checks for previous installations for the selected profile
    - Offers previous values as defaults in interactive mode
    - Creates the initial organization
    - Sets up the administrative user
    - Optionally creates a service principal for automation
    - Displays the saved installation record details after successful bootstrap

    The CLI automatically saves installation records to config.json, including:
    - Organization ID, name, and subdomain
    - Admin user ID and email
    - Service principal details (if created)
    - Bootstrap timestamp and version
.PARAMETER Profile
    Configuration profile name (default: local)
.PARAMETER NonInteractive
    Run in non-interactive mode using defaults
.PARAMETER OrgName
    Organization name (non-interactive mode)
.PARAMETER Subdomain
    Organization subdomain (non-interactive mode)
.PARAMETER AdminEmail
    Administrator email (non-interactive mode)
.PARAMETER AdminName
    Administrator display name (non-interactive mode)
.PARAMETER AdminPassword
    Administrator password (non-interactive mode)
.PARAMETER CreateServicePrincipal
    Create service principal for automation
.PARAMETER ServicePrincipalName
    Service principal name (if CreateServicePrincipal is true)
.EXAMPLE
    .\bootstrap-sorcha.ps1
    Interactive setup with prompts
.EXAMPLE
    .\bootstrap-sorcha.ps1 -Profile local -NonInteractive `
        -OrgName "Stark Industries" `
        -Subdomain "stark" `
        -AdminEmail "tony@stark.com" `
        -AdminName "Tony Stark" `
        -AdminPassword "SecureP@ss123!" `
        -CreateServicePrincipal `
        -ServicePrincipalName "jarvis"
    Non-interactive setup with all parameters
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Profile = "local",

    [Parameter(Mandatory = $false)]
    [switch]$NonInteractive,

    [Parameter(Mandatory = $false)]
    [string]$OrgName,

    [Parameter(Mandatory = $false)]
    [string]$Subdomain,

    [Parameter(Mandatory = $false)]
    [string]$AdminEmail,

    [Parameter(Mandatory = $false)]
    [string]$AdminName,

    [Parameter(Mandatory = $false)]
    [string]$AdminPassword,

    [Parameter(Mandatory = $false)]
    [switch]$CreateServicePrincipal,

    [Parameter(Mandatory = $false)]
    [string]$ServicePrincipalName = "bootstrap-principal"
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Configuration paths
$configDir = Join-Path $env:USERPROFILE ".sorcha"
$configFile = Join-Path $configDir "config.json"
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

# Function to read CLI configuration
function Get-SorchaConfig {
    if (Test-Path $configFile) {
        try {
            $configJson = Get-Content $configFile -Raw | ConvertFrom-Json
            return $configJson
        } catch {
            Write-Host "Warning: Could not read config file: $_" -ForegroundColor Yellow
            return $null
        }
    }
    return $null
}

# Function to find existing installations for profile
function Get-ExistingInstallations {
    param([string]$ProfileName)

    $config = Get-SorchaConfig
    if ($null -eq $config -or $null -eq $config.Installations) {
        return @()
    }

    $installations = @()
    $config.Installations.PSObject.Properties | ForEach-Object {
        $installation = $_.Value
        if ($installation.ProfileName -eq $ProfileName) {
            $installations += $installation
        }
    }

    return $installations
}

# Color output helpers
function Write-Step {
    param([string]$Message)
    Write-Host "==> " -NoNewline -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor White
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ " -NoNewline -ForegroundColor Green
    Write-Host $Message -ForegroundColor White
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "✗ " -NoNewline -ForegroundColor Red
    Write-Host $Message -ForegroundColor White
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ " -NoNewline -ForegroundColor Blue
    Write-Host $Message -ForegroundColor White
}

function Test-CommandExists {
    param([string]$Command)
    return $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

# Banner
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║              Sorcha Platform Bootstrap Script                 ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "║         Initial configuration for fresh installation          ║" -ForegroundColor Cyan
Write-Host "║                                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Step "Checking prerequisites..."

if (-not (Test-CommandExists "sorcha")) {
    Write-ErrorMsg "Sorcha CLI not found. Please install it first:"
    Write-Host "  dotnet tool install -g Sorcha.Cli" -ForegroundColor Yellow
    Write-Host "  OR run from source:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project src/Apps/Sorcha.Cli -- [command]" -ForegroundColor Yellow
    exit 1
}

$sorchaVersion = & sorcha --version
Write-Success "Sorcha CLI found: $sorchaVersion"
Write-Host ""

# Check if service is running
Write-Step "Checking Sorcha Tenant Service..."
$maxAttempts = 5
$attempt = 0
$serviceReady = $false

# Determine health URL based on profile
$healthUrl = switch ($Profile) {
    "docker" { "http://localhost:8080/tenant/health" }
    "local" { "http://localhost:5110/health" }
    "dev" { "https://localhost:7080/health" }
    default { "http://localhost:5110/health" }
}

while ($attempt -lt $maxAttempts -and -not $serviceReady) {
    try {
        $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $serviceReady = $true
            Write-Success "Tenant Service is running at $healthUrl"
            break
        }
    } catch {
        # Service not ready yet
    }

    $attempt++
    if ($attempt -lt $maxAttempts) {
        Write-Host "  Waiting for service... ($attempt/$maxAttempts)" -ForegroundColor Gray
        Start-Sleep -Seconds 2
    }
}

if (-not $serviceReady) {
    Write-ErrorMsg "Tenant Service is not running. Please start it first:"
    Write-Host ""
    Write-Host "For Docker:" -ForegroundColor Yellow
    Write-Host "  docker-compose up -d" -ForegroundColor Gray
    Write-Host ""
    Write-Host "For local development:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project src/Services/Sorcha.Tenant.Service" -ForegroundColor Gray
    Write-Host ""
    Write-Host "For Aspire:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project src/Apps/Sorcha.AppHost" -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host ""

# Check for existing installations
Write-Step "Checking for existing installations..."
$existingInstallations = Get-ExistingInstallations -ProfileName $Profile

if ($existingInstallations.Count -gt 0) {
    Write-Host ""
    Write-Host "  Found $($existingInstallations.Count) existing installation(s) for profile '$Profile':" -ForegroundColor Yellow
    foreach ($inst in $existingInstallations) {
        Write-Host "    - $($inst.Name)" -ForegroundColor Gray
        Write-Host "      Org: $($inst.OrganizationName) ($($inst.OrganizationSubdomain))" -ForegroundColor Gray
        Write-Host "      Admin: $($inst.AdminEmail)" -ForegroundColor Gray
        Write-Host "      Created: $($inst.CreatedAt)" -ForegroundColor Gray
        Write-Host ""
    }

    if (-not $NonInteractive) {
        # Offer to use most recent installation as defaults
        $mostRecent = $existingInstallations | Sort-Object -Property CreatedAt -Descending | Select-Object -First 1

        Write-Host "  The CLI will offer previous values as defaults during prompts." -ForegroundColor Cyan
        Write-Host "  Most recent installation: $($mostRecent.Name)" -ForegroundColor Cyan
        Write-Host ""

        # Set defaults from most recent installation if not provided
        if (-not $OrgName) {
            $OrgName = $mostRecent.OrganizationName
        }
        if (-not $Subdomain) {
            $Subdomain = $mostRecent.OrganizationSubdomain
        }
        if (-not $AdminEmail) {
            $AdminEmail = $mostRecent.AdminEmail
        }
    }
} else {
    Write-Success "No existing installations found for profile '$Profile'"
    Write-Info "This will be your first installation for this profile"
}

Write-Host ""

# Build sorcha bootstrap command arguments
$bootstrapArgs = @(
    "bootstrap",
    "--profile", $Profile
)

if ($NonInteractive) {
    $bootstrapArgs += "--non-interactive"

    # Add required parameters for non-interactive mode
    if ($OrgName) { $bootstrapArgs += "--org-name"; $bootstrapArgs += $OrgName }
    if ($Subdomain) { $bootstrapArgs += "--subdomain"; $bootstrapArgs += $Subdomain }
    if ($AdminEmail) { $bootstrapArgs += "--admin-email"; $bootstrapArgs += $AdminEmail }
    if ($AdminName) { $bootstrapArgs += "--admin-name"; $bootstrapArgs += $AdminName }
    if ($AdminPassword) { $bootstrapArgs += "--admin-password"; $bootstrapArgs += $AdminPassword }

    if ($CreateServicePrincipal) {
        $bootstrapArgs += "--create-sp"
        if ($ServicePrincipalName) {
            $bootstrapArgs += "--sp-name"
            $bootstrapArgs += $ServicePrincipalName
        }
    }

    Write-Info "Running in non-interactive mode with profile: $Profile"
} else {
    Write-Info "Running in interactive mode - you will be prompted for inputs"
    Write-Info "Using profile: $Profile"
    if ($existingInstallations.Count -gt 0) {
        Write-Info "Previous values will be offered as defaults"
    }
}

Write-Host ""

# Run sorcha bootstrap command
Write-Step "Running sorcha bootstrap..."
Write-Host ""

try {
    & sorcha @bootstrapArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-ErrorMsg "Bootstrap command failed with exit code: $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "Bootstrap Complete!" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""

    Write-Success "Sorcha platform has been initialized successfully"
    Write-Host ""

    # Read config to show the saved installation record
    $updatedConfig = Get-SorchaConfig
    if ($null -ne $updatedConfig -and $null -ne $updatedConfig.Installations) {
        $newInstallations = Get-ExistingInstallations -ProfileName $Profile
        $latestInstallation = $newInstallations | Sort-Object -Property CreatedAt -Descending | Select-Object -First 1

        if ($null -ne $latestInstallation) {
            Write-Host "Installation Record:" -ForegroundColor Cyan
            Write-Host "  Name: $($latestInstallation.Name)" -ForegroundColor White
            Write-Host "  Organization: $($latestInstallation.OrganizationName)" -ForegroundColor Gray
            Write-Host "  Subdomain: $($latestInstallation.OrganizationSubdomain)" -ForegroundColor Gray
            Write-Host "  Admin Email: $($latestInstallation.AdminEmail)" -ForegroundColor Gray
            Write-Host "  Organization ID: $($latestInstallation.OrganizationId)" -ForegroundColor Gray
            Write-Host "  Admin User ID: $($latestInstallation.AdminUserId)" -ForegroundColor Gray
            if ($latestInstallation.ServicePrincipalId) {
                Write-Host "  Service Principal ID: $($latestInstallation.ServicePrincipalId)" -ForegroundColor Gray
            }
            Write-Host ""

            if ($updatedConfig.ActiveInstallation -eq $latestInstallation.Name) {
                Write-Success "Set as active installation"
                Write-Host ""
            }
        }
    }

    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Login as admin:" -ForegroundColor White
    Write-Host "     sorcha auth login --profile $Profile" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. View installations:" -ForegroundColor White
    Write-Host "     sorcha config list --installations" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. View all configuration:" -ForegroundColor White
    Write-Host "     sorcha config list --profiles" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. Create additional users:" -ForegroundColor White
    Write-Host "     sorcha user create --profile $Profile" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  5. View API documentation:" -ForegroundColor White

    $apiDocsUrl = switch ($Profile) {
        "docker" { "http://localhost:8080/scalar/" }
        "local" { "http://localhost:5110/scalar/" }
        "dev" { "https://localhost:7080/scalar/" }
        default { "http://localhost:5110/scalar/" }
    }
    Write-Host "     $apiDocsUrl" -ForegroundColor Gray
    Write-Host ""

    Write-Info "Configuration profile: $Profile"
    Write-Info "Configuration saved to: $configFile"
    Write-Host ""

} catch {
    Write-Host ""
    Write-ErrorMsg "Bootstrap failed: $_"
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Ensure the Tenant Service is running" -ForegroundColor Gray
    Write-Host "  - Check the service logs for errors" -ForegroundColor Gray
    Write-Host "  - Verify your network connectivity" -ForegroundColor Gray
    Write-Host "  - Try running with --verbose flag:" -ForegroundColor Gray
    Write-Host "    sorcha bootstrap --profile $Profile --verbose" -ForegroundColor Gray
    Write-Host ""
    exit 1
}
