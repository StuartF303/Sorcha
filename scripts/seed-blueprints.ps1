<#
.SYNOPSIS
    Seeds blueprint templates into the Sorcha platform from local JSON files.
.DESCRIPTION
    Reads all *.json files from the blueprints directory and uploads them
    to the Blueprint Service via the API Gateway. Idempotent — skips
    templates that already exist.

    If no JWT token is provided, automatically logs in with the default
    admin credentials to obtain one.
.PARAMETER BaseUrl
    Base URL of the Sorcha API Gateway (default: http://localhost:80)
.PARAMETER BlueprintsDir
    Directory containing blueprint template JSON files (default: ./blueprints)
.PARAMETER JwtToken
    JWT token for authentication. Falls back to $env:SORCHA_JWT_TOKEN,
    then auto-login with default admin credentials.
.PARAMETER Email
    Admin email for auto-login (default: admin@sorcha.local)
.PARAMETER Password
    Admin password for auto-login (default: Dev_Pass_2025!)
.EXAMPLE
    .\seed-blueprints.ps1
    Seeds templates — auto-login with default admin credentials
.EXAMPLE
    .\seed-blueprints.ps1 -JwtToken "eyJ..."
    Seeds templates with an explicit token
.EXAMPLE
    .\seed-blueprints.ps1 -Email "user@org.com" -Password "secret"
    Seeds templates with custom login credentials
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "http://localhost:80",

    [Parameter(Mandatory = $false)]
    [string]$BlueprintsDir,

    [Parameter(Mandatory = $false)]
    [string]$JwtToken,

    [Parameter(Mandatory = $false)]
    [string]$Email = "admin@sorcha.local",

    [Parameter(Mandatory = $false)]
    [string]$Password = "Dev_Pass_2025!"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Color output helpers
function Write-Step {
    param([string]$Message)
    Write-Host "==> " -NoNewline -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor White
}

function Write-Success {
    param([string]$Message)
    Write-Host "  + " -NoNewline -ForegroundColor Green
    Write-Host $Message -ForegroundColor White
}

function Write-Skip {
    param([string]$Message)
    Write-Host "  - " -NoNewline -ForegroundColor Yellow
    Write-Host $Message -ForegroundColor White
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "  x " -NoNewline -ForegroundColor Red
    Write-Host $Message -ForegroundColor White
}

function Write-Info {
    param([string]$Message)
    Write-Host "  i " -NoNewline -ForegroundColor Blue
    Write-Host $Message -ForegroundColor White
}

# Resolve JWT token: param → env var → auto-login
if ([string]::IsNullOrEmpty($JwtToken)) {
    $JwtToken = $env:SORCHA_JWT_TOKEN
}
if ([string]::IsNullOrEmpty($JwtToken)) {
    Write-Info "No token provided — logging in as $Email"
    try {
        $loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
        $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/tenant/auth/login" `
            -Method POST -ContentType "application/json" -Body $loginBody -ErrorAction Stop
        if ($loginResponse.access_token) {
            $JwtToken = $loginResponse.access_token
            Write-Success "Authenticated successfully"
        } else {
            Write-ErrorMsg "Login response did not contain access_token"
            exit 1
        }
    } catch {
        Write-ErrorMsg "Auto-login failed: $($_.Exception.Message)"
        Write-Info "Ensure services are running (docker-compose up -d) and tenant service is seeded"
        Write-Info "Or provide a token: -JwtToken 'eyJ...' or `$env:SORCHA_JWT_TOKEN = 'eyJ...'"
        exit 1
    }
}

# Resolve blueprints directory
if ([string]::IsNullOrEmpty($BlueprintsDir)) {
    # Look relative to script location first, then repo root
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptDir
    $BlueprintsDir = Join-Path $repoRoot "blueprints"
}

if (-not (Test-Path $BlueprintsDir)) {
    Write-Host "Error: Blueprints directory not found: $BlueprintsDir" -ForegroundColor Red
    exit 1
}

$templateFiles = Get-ChildItem -Path $BlueprintsDir -Filter "*.json"
if ($templateFiles.Count -eq 0) {
    Write-Host "No JSON files found in $BlueprintsDir" -ForegroundColor Yellow
    exit 0
}

$headers = @{
    "Authorization" = "Bearer $JwtToken"
    "Content-Type"  = "application/json"
}

# Banner
Write-Host ""
Write-Host "Sorcha Blueprint Template Seeder" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Info "API Gateway: $BaseUrl"
Write-Info "Blueprints dir: $BlueprintsDir"
Write-Info "Templates found: $($templateFiles.Count)"
Write-Host ""

$seeded = 0
$skipped = 0
$errors = 0

foreach ($file in $templateFiles) {
    $json = Get-Content $file.FullName -Raw
    $template = $json | ConvertFrom-Json

    $templateId = $template.id
    $templateTitle = $template.title
    Write-Step "Processing: $templateTitle ($templateId)"

    # Check if template already exists
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/api/templates/$templateId" `
            -Method Get -Headers $headers -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Skip "Already exists — skipping"
            $skipped++
            continue
        }
    } catch {
        # 404 or other error means template doesn't exist — proceed to upload
    }

    # Upload template
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/api/templates" `
            -Method Post -Headers $headers -Body $json -UseBasicParsing
        if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 201) {
            Write-Success "Seeded successfully"
            $seeded++
        } else {
            Write-ErrorMsg "Unexpected response: $($response.StatusCode)"
            $errors++
        }
    } catch {
        Write-ErrorMsg "Failed to seed: $_"
        $errors++
    }
}

Write-Host ""
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Seeding complete: $seeded seeded, $skipped skipped, $errors errors" -ForegroundColor White
Write-Host ""
