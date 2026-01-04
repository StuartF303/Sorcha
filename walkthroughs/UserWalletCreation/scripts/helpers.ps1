<#
.SYNOPSIS
    Shared helper functions for UserWalletCreation walkthrough scripts
.DESCRIPTION
    Common utilities for:
    - Console output formatting
    - API requests with error handling
    - JWT token decoding
    - Service health checks
#>

# Console output helpers
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
    param(
        [string]$Label,
        [string]$Value = "",
        [string]$Color = "Gray"
    )
    if ($Value) {
        Write-Host "    $Label $Value" -ForegroundColor $Color
    } else {
        Write-Host "    $Label" -ForegroundColor $Color
    }
}

function Write-Warning2 {
    param([string]$Message)
    Write-Host "  ⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error2 {
    param([string]$Message)
    Write-Host "  ❌ $Message" -ForegroundColor Red
}

# API request helper with error handling
function Invoke-ApiRequest {
    <#
    .SYNOPSIS
        Make REST API request with consistent error handling
    .PARAMETER Uri
        API endpoint URL
    .PARAMETER Method
        HTTP method (GET, POST, PUT, DELETE, PATCH)
    .PARAMETER Headers
        Request headers hashtable
    .PARAMETER Body
        Request body object (will be converted to JSON)
    .PARAMETER Description
        Human-readable description for error messages
    .EXAMPLE
        $response = Invoke-ApiRequest -Uri "$baseUrl/api/users" -Method POST -Body @{name="Alice"} -Description "Create user"
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $false)]
        [string]$Method = "GET",

        [Parameter(Mandatory = $false)]
        [hashtable]$Headers = @{},

        [Parameter(Mandatory = $false)]
        [object]$Body = $null,

        [Parameter(Mandatory = $false)]
        [string]$Description = ""
    )

    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }

        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $statusDescription = $_.Exception.Response.StatusDescription

        Write-Host ""
        Write-Error2 "API Request Failed"
        if ($Description) {
            Write-Info "Operation:" $Description "Yellow"
        }
        Write-Info "Endpoint:" "$Method $Uri" "Yellow"
        Write-Info "Status:" "$statusCode $statusDescription" "Yellow"

        # Try to extract error details
        if ($_.ErrorDetails.Message) {
            try {
                $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
                Write-Info "Error:" $errorObj.title "Yellow"
                if ($errorObj.detail) {
                    Write-Info "Detail:" $errorObj.detail "Yellow"
                }
            }
            catch {
                Write-Info "Error:" $_.ErrorDetails.Message "Yellow"
            }
        }

        Write-Host ""
        throw
    }
}

# JWT token decoding
function Get-JwtPayload {
    <#
    .SYNOPSIS
        Decode JWT token payload
    .PARAMETER Token
        JWT token string
    .EXAMPLE
        $payload = Get-JwtPayload -Token $accessToken
        Write-Host "User ID: $($payload.sub)"
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Token
    )

    try {
        $tokenParts = $Token.Split(".")

        if ($tokenParts.Length -lt 2) {
            throw "Invalid JWT token format"
        }

        # Decode payload (second part)
        $payloadBase64 = $tokenParts[1]

        # Add padding if needed
        $padding = 4 - ($payloadBase64.Length % 4)
        if ($padding -lt 4) {
            $payloadBase64 += "=" * $padding
        }

        $payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payloadBase64))
        $payload = $payloadJson | ConvertFrom-Json

        return $payload
    }
    catch {
        Write-Error2 "Failed to decode JWT token: $($_.Exception.Message)"
        return $null
    }
}

function Get-JwtHeader {
    <#
    .SYNOPSIS
        Decode JWT token header
    .PARAMETER Token
        JWT token string
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Token
    )

    try {
        $tokenParts = $Token.Split(".")

        if ($tokenParts.Length -lt 2) {
            throw "Invalid JWT token format"
        }

        # Decode header (first part)
        $headerBase64 = $tokenParts[0]

        # Add padding if needed
        $padding = 4 - ($headerBase64.Length % 4)
        if ($padding -lt 4) {
            $headerBase64 += "=" * $padding
        }

        $headerJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($headerBase64))
        $header = $headerJson | ConvertFrom-Json

        return $header
    }
    catch {
        Write-Error2 "Failed to decode JWT header: $($_.Exception.Message)"
        return $null
    }
}

function Test-JwtExpiration {
    <#
    .SYNOPSIS
        Check if JWT token has expired
    .PARAMETER Token
        JWT token string
    .RETURNS
        $true if expired, $false if still valid
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Token
    )

    $payload = Get-JwtPayload -Token $Token
    if (-not $payload -or -not $payload.exp) {
        Write-Warning2 "Could not determine token expiration"
        return $false
    }

    $expTime = [DateTimeOffset]::FromUnixTimeSeconds($payload.exp)
    $now = [DateTimeOffset]::UtcNow

    return ($now -gt $expTime)
}

# Service health check
function Test-ServiceHealth {
    <#
    .SYNOPSIS
        Check if a service is healthy
    .PARAMETER ServiceUrl
        Base URL of the service
    .PARAMETER ServiceName
        Display name of the service
    .RETURNS
        $true if healthy, $false otherwise
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ServiceUrl,

        [Parameter(Mandatory = $false)]
        [string]$ServiceName = "Service"
    )

    try {
        $healthUrl = "$ServiceUrl/health"
        $response = Invoke-RestMethod -Uri $healthUrl -Method GET -TimeoutSec 5

        Write-Success "$ServiceName is healthy"
        return $true
    }
    catch {
        Write-Error2 "$ServiceName is not responding"
        Write-Info "URL:" $ServiceUrl "Yellow"
        Write-Info "Health endpoint:" "$ServiceUrl/health" "Yellow"
        return $false
    }
}

# User authentication helper
function Get-UserToken {
    <#
    .SYNOPSIS
        Authenticate user and get JWT token
    .PARAMETER Email
        User email
    .PARAMETER Password
        User password
    .PARAMETER TenantServiceUrl
        Tenant Service URL
    .RETURNS
        Token response object with accessToken, refreshToken, expiresIn
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Email,

        [Parameter(Mandatory = $true)]
        [string]$Password,

        [Parameter(Mandatory = $true)]
        [string]$TenantServiceUrl
    )

    $loginRequest = @{
        email = $Email
        password = $Password
    }

    $response = Invoke-ApiRequest `
        -Uri "$TenantServiceUrl/api/auth/login" `
        -Method POST `
        -Body $loginRequest `
        -Description "User authentication"

    return $response
}

# Organization lookup helper
function Get-OrganizationBySubdomain {
    <#
    .SYNOPSIS
        Find organization by subdomain
    .PARAMETER Subdomain
        Organization subdomain
    .PARAMETER AdminToken
        Admin JWT token
    .PARAMETER TenantServiceUrl
        Tenant Service URL
    .RETURNS
        Organization object or $null if not found
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Subdomain,

        [Parameter(Mandatory = $true)]
        [string]$AdminToken,

        [Parameter(Mandatory = $true)]
        [string]$TenantServiceUrl
    )

    $orgs = Invoke-ApiRequest `
        -Uri "$TenantServiceUrl/api/organizations" `
        -Method GET `
        -Headers @{ Authorization = "Bearer $AdminToken" } `
        -Description "List organizations"

    $org = $orgs | Where-Object { $_.subdomain -eq $Subdomain }

    return $org
}

# Display formatted banner
function Write-Banner {
    param(
        [string]$Title,
        [string]$Color = "Cyan"
    )

    $titlePadded = "  $Title  "
    $width = [Math]::Max(76, $titlePadded.Length + 4)
    $titleCentered = $titlePadded.PadLeft(($width + $titlePadded.Length) / 2).PadRight($width)

    Write-Host ""
    Write-Host ("╔" + "═" * ($width - 2) + "╗") -ForegroundColor $Color
    Write-Host ("║" + $titleCentered.Substring(0, $width - 2) + "║") -ForegroundColor $Color
    Write-Host ("╚" + "═" * ($width - 2) + "╝") -ForegroundColor $Color
    Write-Host ""
}

# Display success banner
function Write-SuccessBanner {
    param([string]$Message)
    Write-Banner -Title "✅ $Message" -Color "Green"
}

# Display error banner
function Write-ErrorBanner {
    param([string]$Message)
    Write-Banner -Title "❌ $Message" -Color "Red"
}

# Export functions
Export-ModuleMember -Function @(
    'Write-Section',
    'Write-Success',
    'Write-Info',
    'Write-Warning2',
    'Write-Error2',
    'Invoke-ApiRequest',
    'Get-JwtPayload',
    'Get-JwtHeader',
    'Test-JwtExpiration',
    'Test-ServiceHealth',
    'Get-UserToken',
    'Get-OrganizationBySubdomain',
    'Write-Banner',
    'Write-SuccessBanner',
    'Write-ErrorBanner'
)
