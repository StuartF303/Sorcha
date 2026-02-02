# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Starts the Sorcha MCP Server with automatic JWT authentication.

.DESCRIPTION
    This script:
    1. Authenticates with the Tenant Service to get a JWT token
    2. Starts the Sorcha MCP Server with that token
    3. Optionally updates Claude Code's MCP configuration

.PARAMETER TenantServiceUrl
    The URL of the Tenant Service. Default: http://localhost:5450

.PARAMETER Email
    The email address for authentication. Default: admin@sorcha.local

.PARAMETER Password
    The password for authentication. Default: Dev_Pass_2025!

.PARAMETER UseDocker
    Run the MCP server in Docker instead of directly with dotnet.

.PARAMETER UpdateClaudeConfig
    Update Claude Code's settings.json to include this MCP server.

.EXAMPLE
    .\Start-SorchaMcp.ps1
    Starts the MCP server with default credentials.

.EXAMPLE
    .\Start-SorchaMcp.ps1 -UpdateClaudeConfig
    Updates Claude Code config and starts the MCP server.

.EXAMPLE
    .\Start-SorchaMcp.ps1 -UseDocker
    Runs the MCP server in Docker.
#>

[CmdletBinding()]
param(
    [string]$TenantServiceUrl = "http://localhost:5450",
    [string]$Email = "admin@sorcha.local",
    [string]$Password = "Dev_Pass_2025!",
    [switch]$UseDocker,
    [switch]$UpdateClaudeConfig,
    [switch]$TestOnly
)

$ErrorActionPreference = "Stop"

# Get script directory for relative paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

function Write-Status {
    param([string]$Message, [string]$Color = "Cyan")
    # Write to stderr to avoid interfering with MCP stdio protocol
    [Console]::Error.WriteLine("[$((Get-Date).ToString('HH:mm:ss'))] $Message")
}

function Get-JwtToken {
    param(
        [string]$Url,
        [string]$Email,
        [string]$Password
    )

    Write-Status "Authenticating with Tenant Service at $Url..."

    $loginUrl = "$Url/api/auth/login"
    $body = @{
        email = $Email
        password = $Password
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop

        if ($response.access_token) {
            Write-Status "Authentication successful!" -Color Green
            return $response.access_token
        }
        else {
            throw "No access token in response"
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401) {
            Write-Status "Authentication failed: Invalid credentials" -Color Red
        }
        else {
            Write-Status "Authentication failed: $($_.Exception.Message)" -Color Red
        }
        throw
    }
}

function Update-ClaudeCodeConfig {
    param([string]$ProjectRoot)

    $claudeSettingsPath = Join-Path $env:USERPROFILE ".claude\settings.json"

    Write-Status "Updating Claude Code configuration at $claudeSettingsPath..."

    # Read existing config
    $settings = $null
    if (Test-Path $claudeSettingsPath) {
        $existingContent = Get-Content $claudeSettingsPath -Raw
        $settings = $existingContent | ConvertFrom-Json
    }

    if ($null -eq $settings) {
        $settings = [PSCustomObject]@{}
    }

    # Ensure mcpServers property exists
    if (-not (Get-Member -InputObject $settings -Name "mcpServers" -MemberType Properties)) {
        $settings | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value ([PSCustomObject]@{})
    }

    # Create the Sorcha MCP server configuration
    $wrapperScript = Join-Path $ProjectRoot "scripts\Start-SorchaMcp.ps1"

    $sorchaConfig = [PSCustomObject]@{
        command = "powershell"
        args = @("-ExecutionPolicy", "Bypass", "-File", $wrapperScript)
        env = [PSCustomObject]@{
            SORCHA_TENANT_URL = "http://localhost:5450"
        }
    }

    # Add/update the sorcha server config
    if (Get-Member -InputObject $settings.mcpServers -Name "sorcha" -MemberType Properties) {
        $settings.mcpServers.sorcha = $sorchaConfig
    }
    else {
        $settings.mcpServers | Add-Member -MemberType NoteProperty -Name "sorcha" -Value $sorchaConfig
    }

    # Write back
    $settings | ConvertTo-Json -Depth 10 | Set-Content $claudeSettingsPath -Encoding UTF8

    Write-Status "Claude Code configuration updated!" -Color Green
    Write-Status "MCP server 'sorcha' configured to use: $wrapperScript"
}

function Write-Stderr {
    param([string]$Message)
    [Console]::Error.WriteLine($Message)
}

function Test-McpServer {
    param([string]$Token)

    Write-Status "Testing MCP server connectivity..."

    # Parse the JWT to show user info
    $tokenParts = $Token.Split('.')
    if ($tokenParts.Length -ge 2) {
        $payload = $tokenParts[1]
        # Add padding if needed
        $padding = 4 - ($payload.Length % 4)
        if ($padding -ne 4) {
            $payload += ("=" * $padding)
        }
        $payload = $payload.Replace('-', '+').Replace('_', '/')

        try {
            $decodedBytes = [System.Convert]::FromBase64String($payload)
            $decoded = [System.Text.Encoding]::UTF8.GetString($decodedBytes)
            $claims = $decoded | ConvertFrom-Json

            Write-Status "Token claims:"
            Write-Stderr "  User ID: $($claims.sub)"
            Write-Stderr "  Email: $($claims.email)"
            Write-Stderr "  Organization: $($claims.org_name)"
            Write-Stderr "  Roles: $($claims.role -join ', ')"
            Write-Stderr "  Expires: $(([DateTimeOffset]::FromUnixTimeSeconds($claims.exp)).LocalDateTime)"
        }
        catch {
            Write-Status "Could not decode token claims: $($_.Exception.Message)"
        }
    }

    Write-Status "MCP server test completed!"
}

function Start-McpServerDirect {
    param([string]$Token, [string]$ProjectRoot)

    $mcpProject = Join-Path $ProjectRoot "src\Apps\Sorcha.McpServer\Sorcha.McpServer.csproj"

    Write-Status "Starting MCP server (direct dotnet)..."
    Write-Status "Project: $mcpProject"

    # Set environment variables for service clients (pointing to Docker services via API Gateway)
    $env:ServiceClients__BlueprintService__Address = "http://localhost:80"
    $env:ServiceClients__WalletService__Address = "http://localhost:80"
    $env:ServiceClients__RegisterService__Address = "http://localhost:80"
    $env:ServiceClients__TenantService__Address = "http://localhost:5450"
    $env:ServiceClients__ValidatorService__Address = "http://localhost:80"
    $env:DOTNET_ENVIRONMENT = "Development"

    # Run the MCP server
    & dotnet run --project $mcpProject -- --jwt-token $Token
}

function Start-McpServerDocker {
    param([string]$Token, [string]$ProjectRoot)

    Write-Status "Starting MCP server (Docker)..."

    Push-Location $ProjectRoot
    try {
        # Build if needed
        & docker-compose build mcp-server

        # Run with the token
        $env:SORCHA_JWT_TOKEN = $Token
        & docker-compose run --rm mcp-server --jwt-token $Token
    }
    finally {
        Pop-Location
    }
}

# Main execution
try {
    Write-Stderr ""
    Write-Stderr "========================================"
    Write-Stderr "  Sorcha MCP Server Launcher"
    Write-Stderr "========================================"
    Write-Stderr ""

    # Get JWT token
    $token = Get-JwtToken -Url $TenantServiceUrl -Email $Email -Password $Password

    # Update Claude config if requested
    if ($UpdateClaudeConfig) {
        Update-ClaudeCodeConfig -ProjectRoot $ProjectRoot
    }

    # Test mode - just verify token and show info
    if ($TestOnly) {
        Test-McpServer -Token $token
        Write-Stderr ""
        Write-Status "Test completed successfully!"
        Write-Stderr ""
        Write-Stderr "To start the MCP server, run:"
        Write-Stderr "  .\Start-SorchaMcp.ps1"
        Write-Stderr ""
        Write-Stderr "To configure Claude Code and start:"
        Write-Stderr "  .\Start-SorchaMcp.ps1 -UpdateClaudeConfig"
        Write-Stderr ""
        exit 0
    }

    # Start the MCP server
    if ($UseDocker) {
        Start-McpServerDocker -Token $token -ProjectRoot $ProjectRoot
    }
    else {
        Start-McpServerDirect -Token $token -ProjectRoot $ProjectRoot
    }
}
catch {
    Write-Status "Error: $($_.Exception.Message)"
    Write-Stderr ""
    Write-Stderr "Troubleshooting:"
    Write-Stderr "  1. Ensure Docker services are running: docker-compose up -d"
    Write-Stderr "  2. Check Tenant Service is accessible: curl $TenantServiceUrl/health"
    Write-Stderr "  3. Verify credentials match DatabaseInitializer defaults"
    Write-Stderr ""
    exit 1
}
