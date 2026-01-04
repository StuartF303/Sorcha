<#
.SYNOPSIS
    Run multiple user+wallet creation tests and collect aggregate metrics
.DESCRIPTION
    Executes the bootstrap-based user and wallet creation test multiple times
    with different configurations to gather performance and reliability metrics.

    Tests different:
    - Users (alice, bob, charlie, diana, eve)
    - Wallet algorithms (ED25519, NISTP256, RSA4096)
    - Mnemonic word counts (12, 24)

    Outputs:
    - Individual test metrics (JSON files)
    - Aggregate metrics report (JSON + Markdown)
    - Performance analysis
.PARAMETER Iterations
    Number of test iterations (default: 5)
.PARAMETER TenantServiceUrl
    Tenant Service URL (default: http://localhost:5110)
.PARAMETER WalletServiceUrl
    Wallet Service URL (default: http://localhost:5000)
.PARAMETER OutputDirectory
    Directory for metrics output (default: ./metrics-output)
.EXAMPLE
    .\run-metrics-test.ps1 -Iterations 5 -OutputDirectory "./test-results"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [int]$Iterations = 5,

    [Parameter(Mandatory = $false)]
    [string]$TenantServiceUrl = "http://localhost:5110",

    [Parameter(Mandatory = $false)]
    [string]$WalletServiceUrl = "http://localhost",

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = ".\metrics-output"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Create output directory
if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

# Test configurations
$testUsers = @(
    @{ Name = "Alice Johnson"; Email = "alice@test.local"; Org = "Alice Corp"; Subdomain = "alice"; Algorithm = "ED25519"; Words = 12 }
    @{ Name = "Bob Smith"; Email = "bob@test.local"; Org = "Bob Industries"; Subdomain = "bob"; Algorithm = "NISTP256"; Words = 12 }
    @{ Name = "Charlie Davis"; Email = "charlie@test.local"; Org = "Charlie LLC"; Subdomain = "charlie"; Algorithm = "RSA4096"; Words = 12 }
    @{ Name = "Diana Prince"; Email = "diana@test.local"; Org = "Diana Enterprises"; Subdomain = "diana"; Algorithm = "ED25519"; Words = 24 }
    @{ Name = "Eve Taylor"; Email = "eve@test.local"; Org = "Eve Solutions"; Subdomain = "eve"; Algorithm = "NISTP256"; Words = 24 }
)

# Aggregate metrics
$aggregateMetrics = @{
    testRunId = [Guid]::NewGuid().ToString()
    timestamp = (Get-Date -Format "o")
    configuration = @{
        iterations = $Iterations
        tenantServiceUrl = $TenantServiceUrl
        walletServiceUrl = $WalletServiceUrl
    }
    tests = @()
    summary = @{
        totalTests = 0
        successfulTests = 0
        failedTests = 0
        totalDurationMs = 0
        averageDurationMs = 0
        minDurationMs = 999999
        maxDurationMs = 0
    }
    operationStats = @{
        bootstrap = @{ total = 0; successful = 0; failed = 0; avgMs = 0; minMs = 999999; maxMs = 0; durations = @() }
        createWallet = @{ total = 0; successful = 0; failed = 0; avgMs = 0; minMs = 999999; maxMs = 0; durations = @() }
        listWallets = @{ total = 0; successful = 0; failed = 0; avgMs = 0; minMs = 999999; maxMs = 0; durations = @() }
    }
    algorithmStats = @{
        ED25519 = @{ tests = 0; successful = 0; avgDurationMs = 0; durations = @() }
        NISTP256 = @{ tests = 0; successful = 0; avgDurationMs = 0; durations = @() }
        RSA4096 = @{ tests = 0; successful = 0; avgDurationMs = 0; durations = @() }
    }
}

$runStartTime = Get-Date

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  User + Wallet Creation - Metrics Test Suite                          ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Run ID: $($aggregateMetrics.testRunId)" -ForegroundColor Gray
Write-Host "Iterations: $Iterations" -ForegroundColor Gray
Write-Host "Output Directory: $OutputDirectory" -ForegroundColor Gray
Write-Host ""

# Run tests
for ($i = 0; $i -lt $Iterations; $i++) {
    $testNum = $i + 1
    $config = $testUsers[$i % $testUsers.Count]

    # Add timestamp suffix to make subdomain unique
    $timestamp = (Get-Date -Format "yyyyMMddHHmmss")
    $uniqueSubdomain = "$($config.Subdomain)-$timestamp"

    Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor DarkCyan
    Write-Host "  TEST $testNum of $Iterations" -ForegroundColor Cyan
    Write-Host "  User: $($config.Name)" -ForegroundColor Gray
    Write-Host "  Algorithm: $($config.Algorithm)" -ForegroundColor Gray
    Write-Host "  Mnemonic Words: $($config.Words)" -ForegroundColor Gray
    Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor DarkCyan
    Write-Host ""

    $metricsFile = Join-Path $OutputDirectory "test-$testNum-metrics.json"

    try {
        # Run test
        $testResult = & "$PSScriptRoot\test-bootstrap-user-wallet.ps1" `
            -UserEmail $config.Email `
            -UserPassword "TestPass123!" `
            -UserDisplayName $config.Name `
            -OrgName $config.Org `
            -OrgSubdomain $uniqueSubdomain `
            -WalletAlgorithm $config.Algorithm `
            -MnemonicWordCount $config.Words `
            -TenantServiceUrl $TenantServiceUrl `
            -WalletServiceUrl $WalletServiceUrl `
            -OutputMetrics $metricsFile

        # Load metrics from file (in case script returns different format)
        $testMetrics = Get-Content $metricsFile | ConvertFrom-Json

        # Update aggregate metrics
        $aggregateMetrics.tests += @{
            testNumber = $testNum
            configuration = $config
            metricsFile = $metricsFile
            success = $testMetrics.success
            durationMs = $testMetrics.totalDurationMs
            operations = $testMetrics.operations
        }

        $aggregateMetrics.summary.totalTests++
        if ($testMetrics.success) {
            $aggregateMetrics.summary.successfulTests++
        } else {
            $aggregateMetrics.summary.failedTests++
        }

        $aggregateMetrics.summary.totalDurationMs += $testMetrics.totalDurationMs
        if ($testMetrics.totalDurationMs -lt $aggregateMetrics.summary.minDurationMs) {
            $aggregateMetrics.summary.minDurationMs = $testMetrics.totalDurationMs
        }
        if ($testMetrics.totalDurationMs -gt $aggregateMetrics.summary.maxDurationMs) {
            $aggregateMetrics.summary.maxDurationMs = $testMetrics.totalDurationMs
        }

        # Track algorithm performance
        $algoKey = $config.Algorithm
        $aggregateMetrics.algorithmStats[$algoKey].tests++
        if ($testMetrics.success) {
            $aggregateMetrics.algorithmStats[$algoKey].successful++
        }
        $aggregateMetrics.algorithmStats[$algoKey].durations += $testMetrics.totalDurationMs

        # Track operation performance
        foreach ($op in $testMetrics.operations) {
            if ($op.name -like "*Bootstrap*") {
                $opKey = "bootstrap"
            } elseif ($op.name -like "*Create Wallet*") {
                $opKey = "createWallet"
            } elseif ($op.name -like "*List*Wallet*") {
                $opKey = "listWallets"
            } else {
                continue
            }

            $aggregateMetrics.operationStats[$opKey].total++
            if ($op.success) {
                $aggregateMetrics.operationStats[$opKey].successful++
            } else {
                $aggregateMetrics.operationStats[$opKey].failed++
            }
            $aggregateMetrics.operationStats[$opKey].durations += $op.durationMs
        }

        Write-Host ""
        Write-Host "[PASS] Test $testNum completed successfully - Duration: $($testMetrics.totalDurationMs)ms" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host ""
        Write-Host "[FAIL] Test $testNum failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""

        $aggregateMetrics.summary.totalTests++
        $aggregateMetrics.summary.failedTests++

        $aggregateMetrics.tests += @{
            testNumber = $testNum
            configuration = $config
            metricsFile = $metricsFile
            success = $false
            error = $_.Exception.Message
        }
    }

    # Small delay between tests
    if ($i -lt ($Iterations - 1)) {
        Start-Sleep -Milliseconds 500
    }
}

$runEndTime = Get-Date
$totalRunDuration = ($runEndTime - $runStartTime).TotalMilliseconds

# Calculate averages and statistics
if ($aggregateMetrics.summary.totalTests -gt 0) {
    $aggregateMetrics.summary.averageDurationMs = [int]($aggregateMetrics.summary.totalDurationMs / $aggregateMetrics.summary.totalTests)
}

foreach ($opKey in $aggregateMetrics.operationStats.Keys) {
    $opStat = $aggregateMetrics.operationStats[$opKey]
    if ($opStat.durations.Count -gt 0) {
        $opStat.avgMs = [int](($opStat.durations | Measure-Object -Average).Average)
        $opStat.minMs = [int](($opStat.durations | Measure-Object -Minimum).Minimum)
        $opStat.maxMs = [int](($opStat.durations | Measure-Object -Maximum).Maximum)
    }
}

foreach ($algoKey in $aggregateMetrics.algorithmStats.Keys) {
    $algoStat = $aggregateMetrics.algorithmStats[$algoKey]
    if ($algoStat.durations.Count -gt 0) {
        $algoStat.avgDurationMs = [int](($algoStat.durations | Measure-Object -Average).Average)
    }
}

# Output summary
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✅ METRICS TEST SUITE COMPLETE                                        ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

Write-Host "📊 SUMMARY:" -ForegroundColor Cyan
Write-Host "   Total Tests: $($aggregateMetrics.summary.totalTests)" -ForegroundColor White
Write-Host "   Successful: $($aggregateMetrics.summary.successfulTests)" -ForegroundColor Green
Write-Host "   Failed: $($aggregateMetrics.summary.failedTests)" -ForegroundColor $(if ($aggregateMetrics.summary.failedTests -gt 0) { "Red" } else { "Gray" })
Write-Host "   Success Rate: $([Math]::Round(($aggregateMetrics.summary.successfulTests / $aggregateMetrics.summary.totalTests) * 100, 2))%" -ForegroundColor White
Write-Host ""

Write-Host "⏱️  PERFORMANCE:" -ForegroundColor Cyan
Write-Host "   Total Duration: $([Math]::Round($totalRunDuration, 0))ms" -ForegroundColor White
Write-Host "   Average Test Duration: $($aggregateMetrics.summary.averageDurationMs)ms" -ForegroundColor White
Write-Host "   Min Test Duration: $($aggregateMetrics.summary.minDurationMs)ms" -ForegroundColor White
Write-Host "   Max Test Duration: $($aggregateMetrics.summary.maxDurationMs)ms" -ForegroundColor White
Write-Host ""

Write-Host "🔧 OPERATION BREAKDOWN:" -ForegroundColor Cyan
foreach ($opKey in $aggregateMetrics.operationStats.Keys) {
    $opStat = $aggregateMetrics.operationStats[$opKey]
    Write-Host "   $opKey" ":" -ForegroundColor White
    Write-Host "      Success Rate: $($opStat.successful)/$($opStat.total) ($([Math]::Round(($opStat.successful / [Math]::Max($opStat.total, 1)) * 100, 2))%)" -ForegroundColor Gray
    Write-Host "      Avg Duration: $($opStat.avgMs)ms" -ForegroundColor Gray
    Write-Host "      Min/Max: $($opStat.minMs)ms / $($opStat.maxMs)ms" -ForegroundColor Gray
}
Write-Host ""

Write-Host "🔐 ALGORITHM PERFORMANCE:" -ForegroundColor Cyan
foreach ($algoKey in @("ED25519", "NISTP256", "RSA4096")) {
    $algoStat = $aggregateMetrics.algorithmStats[$algoKey]
    if ($algoStat.tests -gt 0) {
        Write-Host "   $algoKey" ":" -ForegroundColor White
        Write-Host "      Tests: $($algoStat.tests)" -ForegroundColor Gray
        Write-Host "      Successful: $($algoStat.successful)" -ForegroundColor Gray
        Write-Host "      Avg Duration: $($algoStat.avgDurationMs)ms" -ForegroundColor Gray
    }
}
Write-Host ""

# Save aggregate metrics
$aggregateFile = Join-Path $OutputDirectory "aggregate-metrics.json"
$aggregateMetrics | ConvertTo-Json -Depth 10 | Set-Content -Path $aggregateFile -Encoding UTF8
Write-Host "📁 Aggregate metrics saved to: $aggregateFile" -ForegroundColor Cyan

# Generate markdown report
$reportFile = Join-Path $OutputDirectory "METRICS-REPORT.md"
$report = @"
# User + Wallet Creation - Metrics Test Report

**Test Run ID:** $($aggregateMetrics.testRunId)
**Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Total Runtime:** $([Math]::Round($totalRunDuration / 1000, 2)) seconds

---

## Summary

| Metric | Value |
|--------|-------|
| Total Tests | $($aggregateMetrics.summary.totalTests) |
| Successful | $($aggregateMetrics.summary.successfulTests) |
| Failed | $($aggregateMetrics.summary.failedTests) |
| Success Rate | $([Math]::Round(($aggregateMetrics.summary.successfulTests / $aggregateMetrics.summary.totalTests) * 100, 2))% |

---

## Performance Metrics

| Metric | Duration (ms) |
|--------|---------------|
| Average Test Duration | $($aggregateMetrics.summary.averageDurationMs) |
| Min Test Duration | $($aggregateMetrics.summary.minDurationMs) |
| Max Test Duration | $($aggregateMetrics.summary.maxDurationMs) |
| Total Tests Duration | $($aggregateMetrics.summary.totalDurationMs) |

---

## Operation Breakdown

| Operation | Success Rate | Avg Duration (ms) | Min (ms) | Max (ms) |
|-----------|--------------|-------------------|----------|----------|
$(foreach ($opKey in $aggregateMetrics.operationStats.Keys) {
    $opStat = $aggregateMetrics.operationStats[$opKey]
    "| $opKey | $($opStat.successful)/$($opStat.total) ($([Math]::Round(($opStat.successful / [Math]::Max($opStat.total, 1)) * 100, 2))%) | $($opStat.avgMs) | $($opStat.minMs) | $($opStat.maxMs) |"
})

---

## Algorithm Performance

| Algorithm | Tests | Successful | Avg Duration (ms) |
|-----------|-------|------------|-------------------|
$(foreach ($algoKey in @("ED25519", "NISTP256", "RSA4096")) {
    $algoStat = $aggregateMetrics.algorithmStats[$algoKey]
    "| $algoKey | $($algoStat.tests) | $($algoStat.successful) | $($algoStat.avgDurationMs) |"
})

---

## Individual Test Results

| Test # | User | Algorithm | Words | Duration (ms) | Status |
|--------|------|-----------|-------|---------------|--------|
$(for ($i = 0; $i -lt $aggregateMetrics.tests.Count; $i++) {
    $test = $aggregateMetrics.tests[$i]
    $status = if ($test.success) { "✅ Pass" } else { "❌ Fail" }
    "| $($test.testNumber) | $($test.configuration.Name) | $($test.configuration.Algorithm) | $($test.configuration.Words) | $($test.durationMs) | $status |"
})

---

## Files Generated

$(for ($i = 0; $i -lt $aggregateMetrics.tests.Count; $i++) {
    $test = $aggregateMetrics.tests[$i]
    "- ``$($test.metricsFile)`` - Test $($test.testNumber) detailed metrics"
})

---

**Report generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
"@

$report | Set-Content -Path $reportFile -Encoding UTF8
Write-Host "📄 Markdown report saved to: $reportFile" -ForegroundColor Cyan
Write-Host ""

Write-Host "🎯 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Review detailed metrics in $OutputDirectory" -ForegroundColor White
Write-Host "   2. Check METRICS-REPORT.md for formatted analysis" -ForegroundColor White
Write-Host "   3. Examine individual test JSON files for detailed timing" -ForegroundColor White
Write-Host ""
