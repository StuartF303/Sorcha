# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Validates the Sorcha platform environment and connectivity
.DESCRIPTION
    Performs comprehensive validation checks on the Sorcha installation:
    - Docker container health
    - Service endpoint connectivity
    - Database connectivity
    - Port availability
    - Configuration file validation
.PARAMETER Quiet
    Suppress detailed output, only show summary
.PARAMETER JsonOutput
    Output results as JSON
.PARAMETER Services
    Comma-separated list of services to check (default: all)
.EXAMPLE
    .\validate-environment.ps1
    Full validation with detailed output
.EXAMPLE
    .\validate-environment.ps1 -Quiet
    Quick validation with summary only
.EXAMPLE
    .\validate-environment.ps1 -JsonOutput
    Output results as JSON for CI/CD integration
#>

[CmdletBinding()]
param(
    [switch]$Quiet,
    [switch]$JsonOutput,
    [string]$Services = "all"
)

$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

# Script root directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Validation results
$results = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Overall = "Unknown"
    Checks = @()
    Summary = @{
        Total = 0
        Passed = 0
        Failed = 0
        Warnings = 0
    }
}

#region Helper Functions

function Write-Check {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Message,
        [string]$Category = "General"
    )

    $check = @{
        Name = $Name
        Category = $Category
        Status = $Status
        Message = $Message
    }

    $script:results.Checks += $check
    $script:results.Summary.Total++

    switch ($Status) {
        "Pass" { $script:results.Summary.Passed++ }
        "Fail" { $script:results.Summary.Failed++ }
        "Warning" { $script:results.Summary.Warnings++ }
    }

    if (-not $Quiet) {
        $icon = switch ($Status) {
            "Pass" { "[OK]"; $color = "Green" }
            "Fail" { "[X]"; $color = "Red" }
            "Warning" { "[!]"; $color = "Yellow" }
            default { "[?]"; $color = "Gray" }
        }

        Write-Host "  $icon " -NoNewline -ForegroundColor $color
        Write-Host "$Name" -NoNewline -ForegroundColor White
        if ($Message) {
            Write-Host " - $Message" -ForegroundColor DarkGray
        } else {
            Write-Host ""
        }
    }
}

function Write-Section {
    param([string]$Title)

    if (-not $Quiet) {
        Write-Host ""
        Write-Host "=== $Title ===" -ForegroundColor Cyan
    }
}

function Test-TcpConnection {
    param([string]$Host, [int]$Port, [int]$Timeout = 3000)

    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $connect = $tcpClient.BeginConnect($Host, $Port, $null, $null)
        $success = $connect.AsyncWaitHandle.WaitOne($Timeout, $false)

        if ($success) {
            $tcpClient.EndConnect($connect)
            $tcpClient.Close()
            return $true
        }

        $tcpClient.Close()
        return $false
    } catch {
        return $false
    }
}

function Test-HttpEndpoint {
    param([string]$Url, [int]$ExpectedStatus = 200, [int]$Timeout = 5)

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $Timeout -ErrorAction SilentlyContinue
        return @{
            Success = ($response.StatusCode -eq $ExpectedStatus)
            StatusCode = $response.StatusCode
            Message = "Status: $($response.StatusCode)"
        }
    } catch {
        return @{
            Success = $false
            StatusCode = 0
            Message = $_.Exception.Message
        }
    }
}

function Test-DockerContainer {
    param([string]$ContainerName)

    try {
        $status = docker inspect --format "{{.State.Status}}" $ContainerName 2>$null
        $health = docker inspect --format "{{.State.Health.Status}}" $ContainerName 2>$null

        return @{
            Exists = $true
            Running = ($status -eq "running")
            Healthy = ($health -eq "healthy" -or [string]::IsNullOrEmpty($health))
            Status = $status
            Health = $health
        }
    } catch {
        return @{
            Exists = $false
            Running = $false
            Healthy = $false
            Status = "not found"
            Health = "unknown"
        }
    }
}

#endregion

#region Validation Checks

function Test-DockerEnvironment {
    Write-Section "Docker Environment"

    # Check Docker CLI
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Check -Name "Docker CLI" -Status "Pass" -Category "Docker"

        # Check Docker daemon
        try {
            $null = docker info 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Check -Name "Docker Daemon" -Status "Pass" -Category "Docker"
            } else {
                Write-Check -Name "Docker Daemon" -Status "Fail" -Message "Not running" -Category "Docker"
            }
        } catch {
            Write-Check -Name "Docker Daemon" -Status "Fail" -Message "Not accessible" -Category "Docker"
        }

        # Check Docker Compose
        if (Get-Command docker-compose -ErrorAction SilentlyContinue) {
            Write-Check -Name "Docker Compose" -Status "Pass" -Message "Standalone" -Category "Docker"
        } else {
            try {
                $null = docker compose version 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Check -Name "Docker Compose" -Status "Pass" -Message "Plugin" -Category "Docker"
                } else {
                    Write-Check -Name "Docker Compose" -Status "Fail" -Message "Not found" -Category "Docker"
                }
            } catch {
                Write-Check -Name "Docker Compose" -Status "Fail" -Message "Not found" -Category "Docker"
            }
        }
    } else {
        Write-Check -Name "Docker CLI" -Status "Fail" -Message "Not installed" -Category "Docker"
    }
}

function Test-InfrastructureContainers {
    Write-Section "Infrastructure Containers"

    $containers = @(
        @{ Name = "sorcha-redis"; DisplayName = "Redis" },
        @{ Name = "sorcha-postgres"; DisplayName = "PostgreSQL" },
        @{ Name = "sorcha-mongodb"; DisplayName = "MongoDB" },
        @{ Name = "sorcha-aspire-dashboard"; DisplayName = "Aspire Dashboard" }
    )

    foreach ($container in $containers) {
        $status = Test-DockerContainer -ContainerName $container.Name
        if ($status.Running -and $status.Healthy) {
            Write-Check -Name $container.DisplayName -Status "Pass" -Message "Running" -Category "Infrastructure"
        } elseif ($status.Running) {
            Write-Check -Name $container.DisplayName -Status "Warning" -Message "Running (health: $($status.Health))" -Category "Infrastructure"
        } elseif ($status.Exists) {
            Write-Check -Name $container.DisplayName -Status "Fail" -Message "Not running ($($status.Status))" -Category "Infrastructure"
        } else {
            Write-Check -Name $container.DisplayName -Status "Fail" -Message "Container not found" -Category "Infrastructure"
        }
    }
}

function Test-ApplicationContainers {
    Write-Section "Application Containers"

    $containers = @(
        @{ Name = "sorcha-blueprint-service"; DisplayName = "Blueprint Service" },
        @{ Name = "sorcha-wallet-service"; DisplayName = "Wallet Service" },
        @{ Name = "sorcha-register-service"; DisplayName = "Register Service" },
        @{ Name = "sorcha-tenant-service"; DisplayName = "Tenant Service" },
        @{ Name = "sorcha-validator-service"; DisplayName = "Validator Service" },
        @{ Name = "sorcha-peer-service"; DisplayName = "Peer Service" },
        @{ Name = "sorcha-api-gateway"; DisplayName = "API Gateway" },
        @{ Name = "sorcha-ui-web"; DisplayName = "UI Web" }
    )

    foreach ($container in $containers) {
        $status = Test-DockerContainer -ContainerName $container.Name
        if ($status.Running) {
            Write-Check -Name $container.DisplayName -Status "Pass" -Message "Running" -Category "Application"
        } elseif ($status.Exists) {
            Write-Check -Name $container.DisplayName -Status "Fail" -Message "Not running ($($status.Status))" -Category "Application"
        } else {
            Write-Check -Name $container.DisplayName -Status "Warning" -Message "Container not created" -Category "Application"
        }
    }
}

function Test-ServiceEndpoints {
    Write-Section "Service Health Endpoints"

    $endpoints = @(
        @{ Name = "API Gateway"; Url = "http://localhost/health" },
        @{ Name = "Tenant Service"; Url = "http://localhost/api/tenant/health" },
        @{ Name = "Blueprint Service"; Url = "http://localhost/api/blueprints/health" },
        @{ Name = "Register Service"; Url = "http://localhost/api/registers/health" },
        @{ Name = "Aspire Dashboard"; Url = "http://localhost:18888" }
    )

    foreach ($endpoint in $endpoints) {
        $result = Test-HttpEndpoint -Url $endpoint.Url
        if ($result.Success) {
            Write-Check -Name $endpoint.Name -Status "Pass" -Message "Healthy" -Category "Endpoints"
        } else {
            Write-Check -Name $endpoint.Name -Status "Fail" -Message $result.Message -Category "Endpoints"
        }
    }
}

function Test-DatabaseConnectivity {
    Write-Section "Database Connectivity"

    # Redis
    if (Test-TcpConnection -Host "localhost" -Port 16379) {
        try {
            $pong = docker exec sorcha-redis redis-cli ping 2>$null
            if ($pong -eq "PONG") {
                Write-Check -Name "Redis" -Status "Pass" -Message "Connected" -Category "Database"
            } else {
                Write-Check -Name "Redis" -Status "Warning" -Message "Port open, ping failed" -Category "Database"
            }
        } catch {
            Write-Check -Name "Redis" -Status "Warning" -Message "Port open, exec failed" -Category "Database"
        }
    } else {
        Write-Check -Name "Redis" -Status "Fail" -Message "Port 16379 not accessible" -Category "Database"
    }

    # PostgreSQL
    if (Test-TcpConnection -Host "localhost" -Port 5432) {
        try {
            $ready = docker exec sorcha-postgres pg_isready -U sorcha 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Check -Name "PostgreSQL" -Status "Pass" -Message "Connected" -Category "Database"
            } else {
                Write-Check -Name "PostgreSQL" -Status "Warning" -Message "Port open, not ready" -Category "Database"
            }
        } catch {
            Write-Check -Name "PostgreSQL" -Status "Warning" -Message "Port open, exec failed" -Category "Database"
        }
    } else {
        Write-Check -Name "PostgreSQL" -Status "Fail" -Message "Port 5432 not accessible" -Category "Database"
    }

    # MongoDB
    if (Test-TcpConnection -Host "localhost" -Port 27017) {
        try {
            $null = docker exec sorcha-mongodb mongosh --eval "db.adminCommand('ping')" 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Check -Name "MongoDB" -Status "Pass" -Message "Connected" -Category "Database"
            } else {
                Write-Check -Name "MongoDB" -Status "Warning" -Message "Port open, ping failed" -Category "Database"
            }
        } catch {
            Write-Check -Name "MongoDB" -Status "Warning" -Message "Port open, exec failed" -Category "Database"
        }
    } else {
        Write-Check -Name "MongoDB" -Status "Fail" -Message "Port 27017 not accessible" -Category "Database"
    }
}

function Test-ConfigurationFiles {
    Write-Section "Configuration Files"

    # .env file
    $envPath = Join-Path $ProjectRoot ".env"
    if (Test-Path $envPath) {
        Write-Check -Name ".env file" -Status "Pass" -Message "Exists" -Category "Configuration"

        # Check for required variables
        $envContent = Get-Content $envPath -Raw
        if ($envContent -match "JWT_SIGNING_KEY=") {
            Write-Check -Name "JWT_SIGNING_KEY" -Status "Pass" -Message "Configured" -Category "Configuration"
        } else {
            Write-Check -Name "JWT_SIGNING_KEY" -Status "Warning" -Message "Not set" -Category "Configuration"
        }
    } else {
        Write-Check -Name ".env file" -Status "Warning" -Message "Not found (using defaults)" -Category "Configuration"
    }

    # Docker Compose file
    $composePath = Join-Path $ProjectRoot "docker-compose.yml"
    if (Test-Path $composePath) {
        Write-Check -Name "docker-compose.yml" -Status "Pass" -Message "Exists" -Category "Configuration"
    } else {
        Write-Check -Name "docker-compose.yml" -Status "Fail" -Message "Not found" -Category "Configuration"
    }

    # HTTPS certificate
    $certPath = Join-Path $ProjectRoot "docker/certs/aspnetapp.pfx"
    if (Test-Path $certPath) {
        Write-Check -Name "HTTPS Certificate" -Status "Pass" -Message "Exists" -Category "Configuration"
    } else {
        Write-Check -Name "HTTPS Certificate" -Status "Warning" -Message "Not found (HTTPS disabled)" -Category "Configuration"
    }
}

function Test-DockerVolumes {
    Write-Section "Docker Volumes"

    $requiredVolumes = @(
        "sorcha_redis-data",
        "sorcha_postgres-data",
        "sorcha_mongodb-data",
        "sorcha_dataprotection-keys",
        "sorcha_wallet-encryption-keys"
    )

    try {
        $existingVolumes = docker volume ls --format "{{.Name}}" 2>$null

        foreach ($volume in $requiredVolumes) {
            if ($existingVolumes -contains $volume) {
                Write-Check -Name $volume -Status "Pass" -Message "Exists" -Category "Volumes"
            } else {
                Write-Check -Name $volume -Status "Fail" -Message "Not created" -Category "Volumes"
            }
        }
    } catch {
        Write-Check -Name "Docker Volumes" -Status "Fail" -Message "Could not check volumes" -Category "Volumes"
    }
}

#endregion

#region Main Execution

if (-not $Quiet) {
    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "   Sorcha Environment Validation Report      " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
}

# Run all validation checks
Test-DockerEnvironment
Test-ConfigurationFiles
Test-DockerVolumes
Test-InfrastructureContainers
Test-ApplicationContainers
Test-DatabaseConnectivity
Test-ServiceEndpoints

# Calculate overall status
if ($results.Summary.Failed -gt 0) {
    $results.Overall = "Failed"
} elseif ($results.Summary.Warnings -gt 0) {
    $results.Overall = "Warning"
} else {
    $results.Overall = "Passed"
}

# Output results
if ($JsonOutput) {
    $results | ConvertTo-Json -Depth 10
} else {
    Write-Host ""
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host "                  Summary                     " -ForegroundColor Cyan
    Write-Host "==============================================" -ForegroundColor Cyan
    Write-Host ""

    $overallColor = switch ($results.Overall) {
        "Passed" { "Green" }
        "Warning" { "Yellow" }
        "Failed" { "Red" }
    }

    Write-Host "  Overall Status: " -NoNewline
    Write-Host $results.Overall -ForegroundColor $overallColor
    Write-Host ""
    Write-Host "  Total Checks:   $($results.Summary.Total)" -ForegroundColor White
    Write-Host "  Passed:         $($results.Summary.Passed)" -ForegroundColor Green
    Write-Host "  Warnings:       $($results.Summary.Warnings)" -ForegroundColor Yellow
    Write-Host "  Failed:         $($results.Summary.Failed)" -ForegroundColor Red
    Write-Host ""

    if ($results.Summary.Failed -gt 0) {
        Write-Host "Failed Checks:" -ForegroundColor Red
        foreach ($check in $results.Checks | Where-Object { $_.Status -eq "Fail" }) {
            Write-Host "  - $($check.Name): $($check.Message)" -ForegroundColor Red
        }
        Write-Host ""
    }
}

# Exit with appropriate code
if ($results.Summary.Failed -gt 0) {
    exit 1
} else {
    exit 0
}

#endregion
