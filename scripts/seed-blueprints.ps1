<#
.SYNOPSIS
    Seeds blueprint templates into the Sorcha platform from local JSON files.
.DESCRIPTION
    Reads all *.json files from the blueprints directory and uploads them
    to the Blueprint Service via the API Gateway. Idempotent — skips
    templates that already exist.
.PARAMETER BaseUrl
    Base URL of the Sorcha API Gateway (default: http://localhost:80)
.PARAMETER BlueprintsDir
    Directory containing blueprint template JSON files (default: ./blueprints)
.PARAMETER JwtToken
    JWT token for authentication. Falls back to $env:SORCHA_JWT_TOKEN if not provided.
.EXAMPLE
    .\seed-blueprints.ps1
    Seeds templates using defaults and SORCHA_JWT_TOKEN environment variable
.EXAMPLE
    .\seed-blueprints.ps1 -BaseUrl "http://localhost:80" -JwtToken "eyJ..."
    Seeds templates with explicit URL and token
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "http://localhost:80",

    [Parameter(Mandatory = $false)]
    [string]$BlueprintsDir,

    [Parameter(Mandatory = $false)]
    [string]$JwtToken
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

# Resolve JWT token
if ([string]::IsNullOrEmpty($JwtToken)) {
    $JwtToken = $env:SORCHA_JWT_TOKEN
}
if ([string]::IsNullOrEmpty($JwtToken)) {
    Write-Host "Error: JWT token is required. Provide -JwtToken or set SORCHA_JWT_TOKEN environment variable." -ForegroundColor Red
    exit 1
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
