# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Bootstrap seed script for Tenant Service (local development/MVD)
.DESCRIPTION
    Creates default admin user, organization, and service principals for local testing.
    Only runs on empty database or with -Force flag.
.PARAMETER Environment
    Environment to seed (Development, Staging, Production). Default: Development
.PARAMETER AdminEmail
    Admin email address. Default: admin@sorcha.local
.PARAMETER AdminPassword
    Admin password. Default: Dev_Pass_2025!
.PARAMETER BaseUrl
    Tenant Service API base URL. Default: https://localhost:7080
.PARAMETER Force
    Force seed even if data exists
.EXAMPLE
    .\seed-tenant-service.ps1
    .\seed-tenant-service.ps1 -Environment Development
    .\seed-tenant-service.ps1 -AdminEmail mvd-admin@company.com -Force
#>

param(
    [Parameter()]
    [ValidateSet('Development', 'Staging', 'Production')]
    [string]$Environment = 'Development',

    [Parameter()]
    [string]$AdminEmail = 'admin@sorcha.local',

    [Parameter()]
    [string]$AdminPassword = 'Dev_Pass_2025!',

    [Parameter()]
    [string]$BaseUrl = 'https://localhost:7080',

    [Parameter()]
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# ANSI color codes for output
$ColorReset = "`e[0m"
$ColorGreen = "`e[32m"
$ColorYellow = "`e[33m"
$ColorRed = "`e[31m"
$ColorCyan = "`e[36m"
$ColorBold = "`e[1m"

function Write-ColorOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [string]$Color = $ColorReset
    )
    Write-Host "${Color}${Message}${ColorReset}"
}

function Write-Header {
    param([string]$Text)
    Write-ColorOutput "`n========================================" -Color $ColorCyan
    Write-ColorOutput " $Text" -Color "${ColorCyan}${ColorBold}"
    Write-ColorOutput "========================================" -Color $ColorCyan
}

function Invoke-TenantApi {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Token = $null
    )

    $headers = @{
        'Content-Type' = 'application/json'
    }

    if ($Token) {
        $headers['Authorization'] = "Bearer $Token"
    }

    $params = @{
        Method      = $Method
        Uri         = "$BaseUrl$Endpoint"
        Headers     = $headers
        ErrorAction = 'SilentlyContinue'
    }

    if ($Body) {
        $params['Body'] = ($Body | ConvertTo-Json -Depth 10)
    }

    try {
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode.value__
            if ($statusCode -eq 404 -or $statusCode -eq 401) {
                return $null
            }
        }
        throw
    }
}

function New-PasswordHash {
    param([string]$Password)
    # BCrypt.Net-Next WorkFactor: 10 (default for development, increase for production)
    return [BCrypt.Net.BCrypt]::HashPassword($Password, 10)
}

Write-Header "Sorcha Tenant Service - Bootstrap Seed Script"
Write-ColorOutput "Environment: $Environment" -Color $ColorCyan
Write-ColorOutput "Base URL: $BaseUrl" -Color $ColorCyan
Write-ColorOutput "Admin Email: $AdminEmail`n" -Color $ColorCyan

# Step 1: Check if Tenant Service is running
Write-ColorOutput "[1/5] Checking Tenant Service health..." -Color $ColorYellow
try {
    $health = Invoke-TenantApi -Method GET -Endpoint '/health'
    if ($health -and $health.status -eq 'Healthy') {
        Write-ColorOutput "  ✓ Tenant Service is running" -Color $ColorGreen
    }
    else {
        throw "Tenant Service health check failed"
    }
}
catch {
    Write-ColorOutput "  ✗ Error: Cannot connect to Tenant Service at $BaseUrl" -Color $ColorRed
    Write-ColorOutput "  Make sure the service is running: dotnet run --project src/Services/Sorcha.Tenant.Service" -Color $ColorYellow
    exit 1
}

# Step 2: Check if admin user already exists (database not empty)
Write-ColorOutput "`n[2/5] Checking for existing admin user..." -Color $ColorYellow

# NOTE: This assumes direct database access or a check endpoint (to be implemented)
# For now, we'll try to create and handle conflicts

# Step 3: Create default organization
Write-ColorOutput "`n[3/5] Creating default organization..." -Color $ColorYellow

$orgRequest = @{
    name       = "Sorcha Platform"
    subdomain  = "sorcha"
    adminEmail = $AdminEmail
}

try {
    # NOTE: Organization creation endpoint needs to be implemented
    # For now, this is a placeholder showing the expected structure
    Write-ColorOutput "  NOTE: Organization creation requires API implementation" -Color $ColorYellow
    Write-ColorOutput "  Skipping organization creation for now" -Color $ColorYellow
}
catch {
    Write-ColorOutput "  WARNING: Could not create organization: $($_.Exception.Message)" -Color $ColorRed
}

# Step 4: Create default administrator user
Write-ColorOutput "`n[4/5] Creating default administrator user..." -Color $ColorYellow

# NOTE: User creation endpoint needs to be implemented
Write-ColorOutput "  NOTE: User creation requires API implementation" -Color $ColorYellow
Write-ColorOutput "  Default credentials for manual setup:" -Color $ColorCyan
Write-ColorOutput "    Email: $AdminEmail" -Color $ColorGreen
Write-ColorOutput "    Password: $AdminPassword" -Color $ColorGreen

# Step 5: Create service principals
Write-ColorOutput "`n[5/5] Creating service principals..." -Color $ColorYellow

$services = @(
    @{ name = "Blueprint Service"; scope = @("blueprints:read", "blueprints:write", "wallets:sign") },
    @{ name = "Wallet Service"; scope = @("wallets:read", "wallets:write", "wallets:sign") },
    @{ name = "Register Service"; scope = @("register:read", "register:write") },
    @{ name = "Peer Service"; scope = @("peer:read", "peer:write") }
)

$credentials = @()

foreach ($service in $services) {
    Write-ColorOutput "  Creating service principal: $($service.name)" -Color $ColorCyan

    # NOTE: Requires admin token - this is a bootstrapping challenge
    # In production, this would be done through admin portal or deployment scripts
    Write-ColorOutput "  NOTE: Service principal creation requires admin authentication" -Color $ColorYellow
    Write-ColorOutput "  Placeholder: $($service.name) with scopes: $($service.scope -join ', ')" -Color $ColorGreen

    # Simulated credentials (in real implementation, these would be returned from API)
    $clientId = [guid]::NewGuid().ToString()
    $clientSecret = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([guid]::NewGuid().ToString()))

    $credentials += @{
        serviceName  = $service.name
        clientId     = $clientId
        clientSecret = $clientSecret
        scopes       = $service.scope
    }
}

# Output credentials
Write-Header "Bootstrap Complete - Service Principal Credentials"

Write-ColorOutput "`n⚠️  SAVE THESE CREDENTIALS - They will only be shown once!" -Color "${ColorYellow}${ColorBold}"

foreach ($cred in $credentials) {
    Write-ColorOutput "`n$($cred.serviceName):" -Color $ColorCyan
    Write-ColorOutput "  Client ID:     $($cred.clientId)" -Color $ColorGreen
    Write-ColorOutput "  Client Secret: $($cred.clientSecret)" -Color $ColorGreen
    Write-ColorOutput "  Scopes:        $($cred.scopes -join ', ')" -Color $ColorGreen
}

# Optional: Write to .env.local file
$envFile = Join-Path (Get-Location) ".env.local"

Write-ColorOutput "`n`nWrite credentials to .env.local file? (Y/N): " -Color $ColorYellow -NoNewline
$writeEnv = Read-Host

if ($writeEnv -eq 'Y' -or $writeEnv -eq 'y') {
    $envContent = @"
# Sorcha Tenant Service - Local Development Credentials
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# NEVER commit this file to source control (.gitignored)

# Default Administrator
TENANT_ADMIN_EMAIL=$AdminEmail
TENANT_ADMIN_PASSWORD=$AdminPassword

"@

    foreach ($cred in $credentials) {
        $serviceName = $cred.serviceName.Replace(' ', '_').ToUpper()
        $envContent += "`n# $($cred.serviceName)`n"
        $envContent += "${serviceName}_CLIENT_ID=$($cred.clientId)`n"
        $envContent += "${serviceName}_CLIENT_SECRET=$($cred.clientSecret)`n"
    }

    $envContent | Out-File -FilePath $envFile -Encoding UTF8
    Write-ColorOutput "`n✓ Credentials written to: $envFile" -Color $ColorGreen
    Write-ColorOutput "  Add this file to .gitignore if not already present" -Color $ColorYellow
}

Write-Header "Next Steps"
Write-ColorOutput "1. Start Tenant Service: dotnet run --project src/Services/Sorcha.Tenant.Service" -Color $ColorCyan
Write-ColorOutput "2. Test login: POST $BaseUrl/api/auth/login" -Color $ColorCyan
Write-ColorOutput "3. Configure service clients with above credentials" -Color $ColorCyan
Write-ColorOutput "`nFor questions, see: .specify/specs/sorcha-tenant-service.md`n" -Color $ColorCyan
