<#
.SYNOPSIS
    Run multiple user+wallet creation tests and collect metrics
.EXAMPLE
    .\run-simple-metrics.ps1 -Iterations 5
#>

param(
    [Parameter(Mandatory = $false)]
    [int]$Iterations = 5,

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = ".\metrics-output"
)

$ErrorActionPreference = "Stop"

# Create output directory
if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

# Test configurations
$testUsers = @(
    @{ Name = "Alice"; Email = "alice@test.local"; Org = "Alice Corp"; Subdomain = "alice"; Algorithm = "ED25519"; Words = 12 }
    @{ Name = "Bob"; Email = "bob@test.local"; Org = "Bob Inc"; Subdomain = "bob"; Algorithm = "NISTP256"; Words = 12 }
    @{ Name = "Charlie"; Email = "charlie@test.local"; Org = "Charlie LLC"; Subdomain = "charlie"; Algorithm = "RSA4096"; Words = 12 }
    @{ Name = "Diana"; Email = "diana@test.local"; Org = "Diana Corp"; Subdomain = "diana"; Algorithm = "ED25519"; Words = 24 }
    @{ Name = "Eve"; Email = "eve@test.local"; Org = "Eve Solutions"; Subdomain = "eve"; Algorithm = "NISTP256"; Words = 24 }
)

$results = @()
$runStart = Get-Date

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host " User + Wallet Creation Metrics Test" -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Iterations: $Iterations"
Write-Host "Output: $OutputDirectory"
Write-Host ""

for ($i = 0; $i -lt $Iterations; $i++) {
    $testNum = $i + 1
    $config = $testUsers[$i % $testUsers.Count]

    $timestamp = (Get-Date -Format "yyyyMMddHHmmss")
    $subdomain = "$($config.Subdomain)-$timestamp"
    $uniqueEmail = "$($config.Subdomain)-$timestamp@test.local"

    Write-Host "------------------------------------------------------------------------" -ForegroundColor DarkCyan
    Write-Host " TEST $testNum of $Iterations" -ForegroundColor Cyan
    Write-Host " User: $($config.Name) | Algorithm: $($config.Algorithm) | Words: $($config.Words)" -ForegroundColor Gray
    Write-Host "------------------------------------------------------------------------" -ForegroundColor DarkCyan
    Write-Host ""

    $metricsFile = Join-Path $OutputDirectory "test-$testNum-metrics.json"

    try {
        $testResult = & "$PSScriptRoot\test-bootstrap-user-wallet.ps1" `
            -UserEmail $uniqueEmail `
            -UserPassword "TestPass123!" `
            -UserDisplayName $config.Name `
            -OrgName $config.Org `
            -OrgSubdomain $subdomain `
            -WalletAlgorithm $config.Algorithm `
            -MnemonicWordCount $config.Words `
            -OutputMetrics $metricsFile

        $testMetrics = Get-Content $metricsFile | ConvertFrom-Json

        $results += @{
            testNumber = $testNum
            config = $config
            success = $testMetrics.success
            durationMs = $testMetrics.totalDurationMs
            metricsFile = $metricsFile
        }

        Write-Host ""
        Write-Host "[PASS] Test $testNum completed - Duration: $($testMetrics.totalDurationMs)ms" -ForegroundColor Green
        Write-Host ""

        Start-Sleep -Milliseconds 500
    }
    catch {
        Write-Host ""
        Write-Host "[FAIL] Test $testNum failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""

        $results += @{
            testNumber = $testNum
            config = $config
            success = $false
            error = $_.Exception.Message
        }
    }
}

$runEnd = Get-Date
$totalDuration = ($runEnd - $runStart).TotalMilliseconds

# Calculate summary
$successCount = ($results | Where-Object { $_.success -eq $true }).Count
$failCount = $results.Count - $successCount
$durations = $results | Where-Object { $_.success -eq $true } | ForEach-Object { $_.durationMs }
$avgDuration = if ($durations.Count -gt 0) { ($durations | Measure-Object -Average).Average } else { 0 }
$minDuration = if ($durations.Count -gt 0) { ($durations | Measure-Object -Minimum).Minimum } else { 0 }
$maxDuration = if ($durations.Count -gt 0) { ($durations | Measure-Object -Maximum).Maximum } else { 0 }

# Output summary
Write-Host ""
Write-Host "========================================================================" -ForegroundColor Green
Write-Host " METRICS TEST COMPLETE" -ForegroundColor Green
Write-Host "========================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  Total Tests: $($results.Count)"
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Gray" })
Write-Host "  Success Rate: $([Math]::Round(($successCount / $results.Count) * 100, 2))%"
Write-Host ""
Write-Host "PERFORMANCE:" -ForegroundColor Cyan
Write-Host "  Total Runtime: $([Math]::Round($totalDuration, 0))ms"
Write-Host "  Average Test: $([Math]::Round($avgDuration, 0))ms"
Write-Host "  Min Test: $([Math]::Round($minDuration, 0))ms"
Write-Host "  Max Test: $([Math]::Round($maxDuration, 0))ms"
Write-Host ""

# Create simple report
$reportFile = Join-Path $OutputDirectory "METRICS-REPORT.md"
$report = @"
# User + Wallet Creation - Metrics Test Report

**Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Total Runtime:** $([Math]::Round($totalDuration / 1000, 2)) seconds

## Summary

| Metric | Value |
|--------|-------|
| Total Tests | $($results.Count) |
| Successful | $successCount |
| Failed | $failCount |
| Success Rate | $([Math]::Round(($successCount / $results.Count) * 100, 2))% |

## Performance

| Metric | Duration (ms) |
|--------|---------------|
| Average Test | $([Math]::Round($avgDuration, 0)) |
| Min Test | $([Math]::Round($minDuration, 0)) |
| Max Test | $([Math]::Round($maxDuration, 0)) |
| Total Runtime | $([Math]::Round($totalDuration, 0)) |

## Test Results

| Test # | User | Algorithm | Words | Duration (ms) | Status |
|--------|------|-----------|-------|---------------|--------|
$(foreach ($r in $results) {
    $status = if ($r.success) { "PASS" } else { "FAIL" }
    $duration = if ($r.success) { $r.durationMs } else { "N/A" }
    "| $($r.testNumber) | $($r.config.Name) | $($r.config.Algorithm) | $($r.config.Words) | $duration | $status |"
})

---
**Report generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
"@

$report | Set-Content -Path $reportFile -Encoding UTF8
Write-Host "Report saved to: $reportFile" -ForegroundColor Cyan
Write-Host ""
