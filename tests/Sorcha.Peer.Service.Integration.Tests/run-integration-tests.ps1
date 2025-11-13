#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Runs the Sorcha Peer Service integration tests
.DESCRIPTION
    PowerShell script to run integration tests with various options including
    coverage, filtering, and detailed output
.PARAMETER TestFilter
    Filter tests by name or category (e.g., "PeerDiscoveryTests")
.PARAMETER Coverage
    Generate code coverage report
.PARAMETER Verbose
    Enable verbose test output
.PARAMETER Watch
    Run tests in watch mode (re-run on file changes)
.PARAMETER Parallel
    Enable parallel test execution (default: true)
.EXAMPLE
    .\run-integration-tests.ps1
    .\run-integration-tests.ps1 -TestFilter "PeerDiscoveryTests"
    .\run-integration-tests.ps1 -Coverage -Verbose
#>

param(
    [string]$TestFilter = "",
    [switch]$Coverage = $false,
    [switch]$Verbose = $false,
    [switch]$Watch = $false,
    [switch]$Parallel = $true
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = $ScriptDir
$TestProject = Join-Path $ProjectDir "Sorcha.Peer.Service.Integration.Tests.csproj"
$ResultsDir = Join-Path $ProjectDir "TestResults"

# Colors
$ColorSuccess = "Green"
$ColorError = "Red"
$ColorInfo = "Cyan"
$ColorWarning = "Yellow"

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor $ColorInfo
    Write-Host " $Message" -ForegroundColor $ColorInfo
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor $ColorInfo
    Write-Host ""
}

function Write-Step {
    param([string]$Message)
    Write-Host "▶ $Message" -ForegroundColor $ColorInfo
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $ColorSuccess
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $ColorError
}

# Display banner
Write-Header "Sorcha Peer Service - Integration Tests"

# Validate .NET installation
Write-Step "Checking .NET SDK..."
try {
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK version: $dotnetVersion"
} catch {
    Write-ErrorMessage ".NET SDK not found. Please install .NET 10.0 or later."
    exit 1
}

# Validate test project exists
if (-not (Test-Path $TestProject)) {
    Write-ErrorMessage "Test project not found: $TestProject"
    exit 1
}

# Clean previous test results
if (Test-Path $ResultsDir) {
    Write-Step "Cleaning previous test results..."
    Remove-Item $ResultsDir -Recurse -Force
    Write-Success "Test results cleaned"
}

# Build test arguments
$testArgs = @("test", $TestProject)

# Add filter if specified
if ($TestFilter) {
    Write-Step "Applying test filter: $TestFilter"
    $testArgs += "--filter"
    $testArgs += "FullyQualifiedName~$TestFilter"
}

# Add verbosity
if ($Verbose) {
    $testArgs += "--logger"
    $testArgs += "console;verbosity=detailed"
} else {
    $testArgs += "--logger"
    $testArgs += "console;verbosity=normal"
}

# Add coverage
if ($Coverage) {
    Write-Step "Enabling code coverage..."
    $testArgs += "--collect:XPlat Code Coverage"
    $testArgs += "--results-directory"
    $testArgs += $ResultsDir
}

# Add parallel execution
if (-not $Parallel) {
    Write-Step "Disabling parallel test execution..."
    $testArgs += "--parallel"
    $testArgs += "none"
}

# Add watch mode
if ($Watch) {
    $testArgs += "--watch"
}

# Display test configuration
Write-Host ""
Write-Host "Test Configuration:" -ForegroundColor $ColorInfo
Write-Host "  Project: $TestProject" -ForegroundColor Gray
Write-Host "  Filter: $(if ($TestFilter) { $TestFilter } else { 'None (all tests)' })" -ForegroundColor Gray
Write-Host "  Coverage: $Coverage" -ForegroundColor Gray
Write-Host "  Verbose: $Verbose" -ForegroundColor Gray
Write-Host "  Watch: $Watch" -ForegroundColor Gray
Write-Host "  Parallel: $Parallel" -ForegroundColor Gray
Write-Host ""

# Run tests
Write-Header "Running Tests"
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    & dotnet @testArgs
    $testExitCode = $LASTEXITCODE

    $stopwatch.Stop()
    Write-Host ""

    if ($testExitCode -eq 0) {
        Write-Success "All tests passed! (Duration: $($stopwatch.Elapsed.ToString('mm\:ss')))"

        # Generate coverage report if enabled
        if ($Coverage) {
            Write-Header "Code Coverage Report"

            # Find coverage file
            $coverageFile = Get-ChildItem -Path $ResultsDir -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1

            if ($coverageFile) {
                Write-Success "Coverage file: $($coverageFile.FullName)"

                # Try to install and run reportgenerator if available
                Write-Step "Attempting to generate HTML coverage report..."
                try {
                    dotnet tool install --global dotnet-reportgenerator-globaltool 2>$null
                    $reportDir = Join-Path $ResultsDir "CoverageReport"

                    dotnet reportgenerator `
                        -reports:"$($coverageFile.FullName)" `
                        -targetdir:"$reportDir" `
                        -reporttypes:Html

                    if ($LASTEXITCODE -eq 0) {
                        $indexFile = Join-Path $reportDir "index.html"
                        Write-Success "Coverage report generated: $indexFile"

                        # Open in browser on Windows
                        if ($IsWindows -or $env:OS -match "Windows") {
                            Write-Step "Opening coverage report in browser..."
                            Start-Process $indexFile
                        }
                    }
                } catch {
                    Write-Warning "Could not generate HTML report. Install with: dotnet tool install --global dotnet-reportgenerator-globaltool"
                }
            } else {
                Write-Warning "Coverage file not found in $ResultsDir"
            }
        }

        exit 0
    } else {
        Write-ErrorMessage "Tests failed! (Duration: $($stopwatch.Elapsed.ToString('mm\:ss')))"
        exit $testExitCode
    }
} catch {
    $stopwatch.Stop()
    Write-ErrorMessage "Error running tests: $_"
    exit 1
}
