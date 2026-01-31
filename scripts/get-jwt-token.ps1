# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Gets a JWT token from Sorcha Tenant Service
.DESCRIPTION
    Authenticates with the Tenant Service and returns a JWT token for use with MCP server or API calls.
.PARAMETER Email
    User email address for authentication
.PARAMETER Password
    User password for authentication
.PARAMETER TenantServiceUrl
    Tenant Service URL (default: http://localhost/api/tenant)
.PARAMETER Profile
    Deployment profile (docker, aspire, local). Default: docker
.PARAMETER AsEnvironmentVariable
    Output as environment variable setting command instead of just the token
.PARAMETER Quiet
    Suppress informational messages, only output the token
.EXAMPLE
    .\get-jwt-token.ps1 -Email "admin@sorcha.local" -Password "Admin123!"
    Returns the JWT token
.EXAMPLE
    .\get-jwt-token.ps1 -Email "admin@sorcha.local" -Password "Admin123!" -AsEnvironmentVariable
    Returns: $env:SORCHA_JWT_TOKEN = "eyJ..."
.EXAMPLE
    $token = .\get-jwt-token.ps1 -Email "admin@sorcha.local" -Password "Admin123!" -Quiet
    Stores token in variable without extra output
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$Email,

    [Parameter(Mandatory=$true)]
    [string]$Password,

    [string]$TenantServiceUrl = "http://localhost/api/tenant",

    [ValidateSet("docker", "aspire", "local")]
    [string]$Profile = "docker",

    [switch]$AsEnvironmentVariable,

    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

# Adjust URL based on profile
if ($Profile -eq "aspire") {
    $TenantServiceUrl = "https://localhost:7110/api/tenant"
} elseif ($Profile -eq "local") {
    $TenantServiceUrl = "http://localhost:5450/api/tenant"
}

function Write-InfoMessage {
    param([string]$Message)
    if (-not $Quiet) {
        Write-Host $Message -ForegroundColor Cyan
    }
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "ERROR: $Message" -ForegroundColor Red
}

try {
    Write-InfoMessage "Authenticating with Tenant Service at $TenantServiceUrl..."

    # Prepare login request
    $loginUrl = "$TenantServiceUrl/auth/login"
    $body = @{
        email = $Email
        password = $Password
    } | ConvertTo-Json

    Write-InfoMessage "Sending login request..."

    # Make the request
    $response = Invoke-RestMethod -Uri $loginUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop

    # Extract token
    if ($response.accessToken) {
        $token = $response.accessToken

        if (-not $Quiet) {
            Write-Host ""
            Write-Host "SUCCESS: JWT token obtained" -ForegroundColor Green
            Write-Host ""

            # Show token preview
            $preview = if ($token.Length -gt 50) {
                $token.Substring(0, 47) + "..."
            } else {
                $token
            }
            Write-Host "Token Preview: $preview" -ForegroundColor Gray
            Write-Host "Token Length: $($token.Length) characters" -ForegroundColor Gray
            Write-Host ""
        }

        # Output based on mode
        if ($AsEnvironmentVariable) {
            Write-Host "`$env:SORCHA_JWT_TOKEN = `"$token`""
            Write-Host ""
            Write-Host "To set in current session, run:" -ForegroundColor Yellow
            Write-Host "`$env:SORCHA_JWT_TOKEN = `"$token`"" -ForegroundColor Gray
        } else {
            # Just output the token
            Write-Output $token
        }

        # Show usage examples if not quiet
        if (-not $Quiet) {
            Write-Host "Usage Examples:" -ForegroundColor Cyan
            Write-Host "  1. Set environment variable:" -ForegroundColor White
            Write-Host "     `$env:SORCHA_JWT_TOKEN = `"$token`"" -ForegroundColor Gray
            Write-Host ""
            Write-Host "  2. Use with MCP server:" -ForegroundColor White
            Write-Host "     docker-compose run mcp-server --jwt-token `"$token`"" -ForegroundColor Gray
            Write-Host ""
            Write-Host "  3. Use with API calls:" -ForegroundColor White
            Write-Host "     curl -H `"Authorization: Bearer $token`" http://localhost/api/..." -ForegroundColor Gray
            Write-Host ""
        }

        exit 0
    } else {
        Write-ErrorMessage "Response did not contain accessToken field"
        if ($response) {
            Write-Host "Response received: $($response | ConvertTo-Json -Compress)" -ForegroundColor Yellow
        }
        exit 1
    }

} catch {
    Write-ErrorMessage "Failed to authenticate"

    # Parse error details if available
    if ($_.Exception.Response) {
        try {
            $statusCode = $_.Exception.Response.StatusCode.value__
            $statusDescription = $_.Exception.Response.StatusDescription
            Write-Host "HTTP $statusCode - $statusDescription" -ForegroundColor Yellow

            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            $reader.Close()

            if ($responseBody) {
                Write-Host "Server response: $responseBody" -ForegroundColor Yellow
            }

            # Common error hints
            if ($statusCode -eq 401) {
                Write-Host ""
                Write-Host "HINT: Check email and password are correct" -ForegroundColor Cyan
                Write-Host "Default admin: email=admin@sorcha.local, password=Admin123!" -ForegroundColor Gray
            } elseif ($statusCode -eq 404) {
                Write-Host ""
                Write-Host "HINT: Tenant Service may not be running or URL is incorrect" -ForegroundColor Cyan
                Write-Host "Check services: docker-compose ps" -ForegroundColor Gray
            } elseif ($statusCode -eq 503) {
                Write-Host ""
                Write-Host "HINT: Service may be starting up or unavailable" -ForegroundColor Cyan
                Write-Host "Check logs: docker-compose logs -f tenant-service" -ForegroundColor Gray
            }
        } catch {
            # Failed to parse error details
        }
    } else {
        Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Yellow

        # Connection error hints
        if ($_.Exception.Message -match "Unable to connect") {
            Write-Host ""
            Write-Host "HINT: Ensure Sorcha services are running" -ForegroundColor Cyan
            Write-Host "Start services: docker-compose up -d" -ForegroundColor Gray
        }
    }

    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Cyan
    Write-Host "  1. Verify services are running: docker-compose ps" -ForegroundColor White
    Write-Host "  2. Check Tenant Service logs: docker-compose logs -f tenant-service" -ForegroundColor White
    Write-Host "  3. Verify bootstrap was run: .\scripts\bootstrap-sorcha.ps1 -Profile $Profile" -ForegroundColor White
    Write-Host "  4. Try different profile: -Profile aspire or -Profile local" -ForegroundColor White
    Write-Host ""

    exit 1
}
