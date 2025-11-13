#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Quick launcher for specific test suites
.DESCRIPTION
    Simplified script to run specific test categories with common options
.PARAMETER Suite
    Test suite to run: discovery, communication, throughput, health, or all
.PARAMETER Coverage
    Generate code coverage report
.PARAMETER Verbose
    Enable verbose test output
.EXAMPLE
    .\run-test-suite.ps1 discovery
    .\run-test-suite.ps1 throughput -Verbose
    .\run-test-suite.ps1 all -Coverage
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateSet("discovery", "communication", "throughput", "health", "all")]
    [string]$Suite,

    [switch]$Coverage = $false,
    [switch]$Verbose = $false
)

# Colors
$ColorInfo = "Cyan"
$ColorSuccess = "Green"

# Map suite names to test filters
$suiteFilters = @{
    "discovery" = "PeerDiscoveryTests"
    "communication" = "PeerCommunicationTests"
    "throughput" = "PeerThroughputTests"
    "health" = "PeerHealthTests"
    "all" = ""
}

$filter = $suiteFilters[$Suite]

# Suite descriptions
$suiteDescriptions = @{
    "discovery" = "Peer Discovery & Registration Tests"
    "communication" = "Peer-to-Peer Communication Tests"
    "throughput" = "Performance & Throughput Tests"
    "health" = "Health Check & Metrics Tests"
    "all" = "All Integration Tests"
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor $ColorInfo
Write-Host " Running: $($suiteDescriptions[$Suite])" -ForegroundColor $ColorInfo
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor $ColorInfo
Write-Host ""

# Build arguments for main script
$scriptArgs = @()

if ($filter) {
    $scriptArgs += "-TestFilter"
    $scriptArgs += $filter
}

if ($Coverage) {
    $scriptArgs += "-Coverage"
}

if ($Verbose) {
    $scriptArgs += "-Verbose"
}

# Run main test script
$scriptPath = Join-Path $PSScriptRoot "run-integration-tests.ps1"
& $scriptPath @scriptArgs
