<#
.SYNOPSIS
    Test user authentication and JWT token validation
.DESCRIPTION
    Authenticates a user and displays decoded JWT token claims including:
    - User ID (sub)
    - Email
    - Display name
    - Organization ID and name
    - Roles
    - Token expiration
.PARAMETER Email
    User email address
.PARAMETER Password
    User password
.PARAMETER TenantServiceUrl
    Direct Tenant Service URL (default: http://localhost:5110)
.PARAMETER ShowFullToken
    Display the full JWT token (default: false)
.EXAMPLE
    .\test-user-login.ps1 -Email "alice@example.com" -Password "SecurePass123!"
.EXAMPLE
    .\test-user-login.ps1 -Email "alice@example.com" -Password "SecurePass123!" -ShowFullToken
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Email,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [Parameter(Mandatory = $false)]
    [string]$TenantServiceUrl = "http://localhost:5110",

    [Parameter(Mandatory = $false)]
    [switch]$ShowFullToken
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Write-Section {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "  ✓ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Label, [string]$Value = "", [string]$Color = "Gray")
    if ($Value) {
        Write-Host "    $Label $Value" -ForegroundColor $Color
    } else {
        Write-Host "    $Label" -ForegroundColor $Color
    }
}

try {
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  Sorcha User Login Test                                                ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

    Write-Section "Testing User Login"
    Write-Info "Tenant Service:" $TenantServiceUrl
    Write-Info "User Email:" $Email

    # Prepare login request
    $loginRequest = @{
        email = $Email
        password = $Password
    }

    # Authenticate
    $tokenResponse = Invoke-RestMethod `
        -Uri "$TenantServiceUrl/api/auth/login" `
        -Method POST `
        -Body ($loginRequest | ConvertTo-Json) `
        -ContentType "application/json"

    Write-Success "Authentication successful"
    Write-Host ""

    # Display token response
    Write-Host "Token Response:" -ForegroundColor Yellow
    Write-Info "  Access Token:" "***" + $tokenResponse.accessToken.Substring([Math]::Max(0, $tokenResponse.accessToken.Length - 20))
    Write-Info "  Token Type:" $tokenResponse.tokenType
    Write-Info "  Expires In:" "$($tokenResponse.expiresIn) seconds ($([Math]::Round($tokenResponse.expiresIn / 60)) minutes)"

    if ($tokenResponse.refreshToken) {
        Write-Info "  Refresh Token:" "***" + $tokenResponse.refreshToken.Substring([Math]::Max(0, $tokenResponse.refreshToken.Length - 20))
    }

    # Decode JWT token
    Write-Section "JWT Token Claims"

    $token = $tokenResponse.accessToken
    $tokenParts = $token.Split(".")

    if ($tokenParts.Length -lt 2) {
        Write-Host "  ⚠️  Invalid JWT token format" -ForegroundColor Yellow
        exit 1
    }

    # Decode header
    $headerBase64 = $tokenParts[0]
    $padding = 4 - ($headerBase64.Length % 4)
    if ($padding -lt 4) {
        $headerBase64 += "=" * $padding
    }
    $headerJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($headerBase64))
    $header = $headerJson | ConvertFrom-Json

    Write-Host "Header:" -ForegroundColor Yellow
    Write-Info "  Algorithm:" $header.alg
    Write-Info "  Type:" $header.typ

    # Decode payload
    $payloadBase64 = $tokenParts[1]
    $padding = 4 - ($payloadBase64.Length % 4)
    if ($padding -lt 4) {
        $payloadBase64 += "=" * $padding
    }
    $payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64))
    $payload = $payloadJson | ConvertFrom-Json

    Write-Host ""
    Write-Host "Payload Claims:" -ForegroundColor Yellow

    # User claims
    if ($payload.sub) {
        Write-Info "  sub (User ID):" $payload.sub "Cyan"
    }
    if ($payload.email) {
        Write-Info "  email:" $payload.email "Cyan"
    }
    if ($payload.name) {
        Write-Info "  name:" $payload.name "Cyan"
    }

    # Organization claims
    if ($payload.org_id) {
        Write-Info "  org_id:" $payload.org_id "Magenta"
    }
    if ($payload.org_name) {
        Write-Info "  org_name:" $payload.org_name "Magenta"
    }

    # Role claims
    if ($payload.roles) {
        $rolesStr = if ($payload.roles -is [array]) {
            $payload.roles -join ", "
        } else {
            $payload.roles
        }
        Write-Info "  roles:" $rolesStr "Yellow"
    }

    # Token metadata
    if ($payload.iat) {
        $iatTime = [DateTimeOffset]::FromUnixTimeSeconds($payload.iat)
        Write-Info "  iat (Issued At):" "$($iatTime.ToString('yyyy-MM-dd HH:mm:ss')) UTC" "Gray"
    }

    if ($payload.exp) {
        $expTime = [DateTimeOffset]::FromUnixTimeSeconds($payload.exp)
        $now = [DateTimeOffset]::UtcNow
        $timeRemaining = $expTime - $now

        Write-Info "  exp (Expires At):" "$($expTime.ToString('yyyy-MM-dd HH:mm:ss')) UTC" "Gray"

        if ($timeRemaining.TotalSeconds -gt 0) {
            $minutesRemaining = [Math]::Round($timeRemaining.TotalMinutes)
            Write-Info "  Time Remaining:" "$minutesRemaining minutes" "Green"
            Write-Success "Token is valid"
        } else {
            Write-Info "  Time Remaining:" "EXPIRED" "Red"
            Write-Host "  ⚠️  Token has expired!" -ForegroundColor Red
        }
    }

    if ($payload.nbf) {
        $nbfTime = [DateTimeOffset]::FromUnixTimeSeconds($payload.nbf)
        Write-Info "  nbf (Not Before):" "$($nbfTime.ToString('yyyy-MM-dd HH:mm:ss')) UTC" "Gray"
    }

    if ($payload.iss) {
        Write-Info "  iss (Issuer):" $payload.iss "Gray"
    }

    if ($payload.aud) {
        Write-Info "  aud (Audience):" $payload.aud "Gray"
    }

    if ($payload.token_type) {
        Write-Info "  token_type:" $payload.token_type "Gray"
    }

    # Show full token if requested
    if ($ShowFullToken) {
        Write-Section "Full JWT Token"
        Write-Host "  $token" -ForegroundColor White
        Write-Host ""
        Write-Host "  Use this token for authenticated API requests:" -ForegroundColor Yellow
        Write-Host "  Authorization: Bearer $token" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  ✅ TEST PASSED - User authentication successful                      ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""

    # Return token for potential piping
    return $tokenResponse

}
catch {
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║  ❌ TEST FAILED                                                        ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
    Write-Host ""

    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $statusDescription = $_.Exception.Response.StatusDescription
        Write-Host "  HTTP Status: $statusCode $statusDescription" -ForegroundColor Yellow
    }

    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""

    # Common troubleshooting
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  • Verify email and password are correct" -ForegroundColor White
    Write-Host "  • Check if user exists in organization" -ForegroundColor White
    Write-Host "  • Verify Tenant Service is running: curl $TenantServiceUrl/health" -ForegroundColor White
    Write-Host "  • Check user status is 'Active' (not Suspended or Deleted)" -ForegroundColor White
    Write-Host ""

    exit 1
}
