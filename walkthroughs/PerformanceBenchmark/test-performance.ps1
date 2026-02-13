#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# Performance Benchmark Test Suite
# Measures Register Service performance across payloads, throughput, latency, and concurrency

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('gateway', 'direct', 'aspire')]
    [string]$Profile = 'gateway',

    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@perf.local",

    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "PerfTest2026!",

    [Parameter(Mandatory=$false)]
    [switch]$QuickTest = $false,

    [Parameter(Mandatory=$false)]
    [string]$MaxPayloadSize = "1MB",

    [Parameter(Mandatory=$false)]
    [int]$Iterations = 100,

    [Parameter(Mandatory=$false)]
    [int]$Concurrency = 25,

    [Parameter(Mandatory=$false)]
    [switch]$SkipResourceMonitoring = $false,

    [Parameter(Mandatory=$false)]
    [switch]$ShowJson = $false
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ============================================================================
# Configuration
# ============================================================================

$Script:ResultsDir = Join-Path $PSScriptRoot "results"
$Script:Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$Script:ResultFile = Join-Path $ResultsDir "benchmark-$Timestamp.json"
$Script:SummaryFile = Join-Path $ResultsDir "summary-$Timestamp.md"

# Ensure results directory exists
if (-not (Test-Path $ResultsDir)) {
    New-Item -Path $ResultsDir -ItemType Directory | Out-Null
}

# URL Configuration
$GatewayUrl = ""
$TenantUrl = ""
$RegisterUrl = ""
$WalletUrl = ""

switch ($Profile) {
    'gateway' {
        $GatewayUrl    = "http://localhost"
        $TenantUrl     = "$GatewayUrl/api"
        $RegisterUrl   = "$GatewayUrl/api"
        $WalletUrl     = "$GatewayUrl/api"
    }
    'direct' {
        $GatewayUrl    = "http://localhost"
        $TenantUrl     = "http://localhost:5450/api"
        $RegisterUrl   = "http://localhost:5380/api"
        $WalletUrl     = "$GatewayUrl/api"
    }
    'aspire' {
        $GatewayUrl    = "https://localhost:7082"
        $TenantUrl     = "https://localhost:7110/api"
        $RegisterUrl   = "https://localhost:7290/api"
        $WalletUrl     = "$GatewayUrl/api"
    }
}

# Test configuration
$Script:TestConfig = @{
    PayloadSizes = if ($QuickTest) {
        @("1KB", "10KB", "50KB")
    } else {
        @("1KB", "10KB", "50KB", "100KB", "500KB", $MaxPayloadSize)
    }
    ThroughputDuration = if ($QuickTest) { 30 } else { 60 }
    LatencyIterations = if ($QuickTest) { 50 } else { $Iterations }
    ConcurrencyLevels = if ($QuickTest) {
        @(1, 5, 10)
    } else {
        @(1, 5, 10, 25, $Concurrency)
    }
    DocketSizes = if ($QuickTest) {
        @(10, 50)
    } else {
        @(10, 50, 100, 500)
    }
}

# Results storage
$Script:Results = @{
    Timestamp = $Timestamp
    Profile = $Profile
    Environment = @{
        OS = [System.Environment]::OSVersion.VersionString
        PowerShell = $PSVersionTable.PSVersion.ToString()
        Docker = ""
    }
    Tests = @{
        PayloadSize = @()
        Throughput = @()
        Latency = @()
        Concurrency = @()
        DocketBuilding = @()
    }
    Summary = @{
        TotalTests = 0
        TotalErrors = 0
        Duration = 0
    }
}

# ============================================================================
# Helper Functions
# ============================================================================

function Write-Banner {
    param([string]$Text)
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Text)
    Write-Host ""
    Write-Host "[STEP] $Text" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Text)
    Write-Host "  ✓ $Text" -ForegroundColor Green
}

function Write-Error {
    param([string]$Text)
    Write-Host "  ✗ $Text" -ForegroundColor Red
}

function Write-Info {
    param([string]$Text)
    Write-Host "  → $Text" -ForegroundColor White
}

function Write-Metric {
    param(
        [string]$Name,
        [string]$Value,
        [string]$Unit = ""
    )
    $displayValue = if ($Unit) { "$Value $Unit" } else { $Value }
    Write-Host "    $Name : " -NoNewline -ForegroundColor Gray
    Write-Host $displayValue -ForegroundColor Cyan
}

function ConvertTo-Bytes {
    param([string]$Size)

    if ($Size -match '^(\d+(?:\.\d+)?)\s*(KB|MB|GB)?$') {
        $value = [double]$Matches[1]
        $unit = $Matches[2]

        switch ($unit) {
            "KB" { return [int]($value * 1024) }
            "MB" { return [int]($value * 1024 * 1024) }
            "GB" { return [int]($value * 1024 * 1024 * 1024) }
            default { return [int]$value }
        }
    }
    return 1024
}

function Get-Statistics {
    param([double[]]$Values)

    if ($Values.Count -eq 0) {
        return @{
            Min = 0
            Max = 0
            Mean = 0
            Median = 0
            P95 = 0
            P99 = 0
            StdDev = 0
        }
    }

    $sorted = $Values | Sort-Object
    $count = $sorted.Count

    $mean = ($sorted | Measure-Object -Average).Average
    $median = if ($count % 2 -eq 0) {
        ($sorted[($count/2)-1] + $sorted[$count/2]) / 2
    } else {
        $sorted[[Math]::Floor($count/2)]
    }

    $p95Index = [Math]::Ceiling($count * 0.95) - 1
    $p99Index = [Math]::Ceiling($count * 0.99) - 1

    $variance = ($sorted | ForEach-Object { [Math]::Pow($_ - $mean, 2) } | Measure-Object -Average).Average
    $stdDev = [Math]::Sqrt($variance)

    return @{
        Min = $sorted[0]
        Max = $sorted[-1]
        Mean = $mean
        Median = $median
        P95 = $sorted[$p95Index]
        P99 = $sorted[$p99Index]
        StdDev = $stdDev
        Count = $count
    }
}

function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [switch]$ReturnTiming
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $params = @{
            Method = $Method
            Uri = $Url
            Headers = $Headers
            ContentType = "application/json"
            UseBasicParsing = $true
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10 -Compress)
        }

        $response = Invoke-RestMethod @params
        $stopwatch.Stop()

        if ($ReturnTiming) {
            return @{
                Success = $true
                Response = $response
                Duration = $stopwatch.Elapsed.TotalMilliseconds
            }
        }

        return $response
    }
    catch {
        $stopwatch.Stop()

        if ($ReturnTiming) {
            return @{
                Success = $false
                Error = $_.Exception.Message
                Duration = $stopwatch.Elapsed.TotalMilliseconds
            }
        }

        throw
    }
}

function Get-AuthToken {
    Write-Step "Authenticating..."

    $body = "grant_type=password&username=$AdminEmail&password=$AdminPassword&client_id=sorcha-cli"

    try {
        $response = Invoke-RestMethod -Method Post `
            -Uri "$TenantUrl/service-auth/token" `
            -Body $body `
            -ContentType "application/x-www-form-urlencoded" `
            -UseBasicParsing

        Write-Success "Authenticated as $AdminEmail"
        return $response.access_token
    }
    catch {
        Write-Error "Authentication failed: $($_.Exception.Message)"
        throw
    }
}

function New-TestRegister {
    param([string]$Token)

    Write-Step "Creating test register..."

    # Create wallet first
    $walletResponse = Invoke-ApiRequest -Method Post `
        -Url "$WalletUrl/v1/wallets" `
        -Headers @{ Authorization = "Bearer $Token" } `
        -Body @{
            name = "Performance Test Wallet"
            algorithm = "ED25519"
            wordCount = 12
        }

    $walletAddress = $walletResponse.wallet.address
    Write-Info "Created wallet: $walletAddress"

    # Initiate register
    $initiateResponse = Invoke-ApiRequest -Method Post `
        -Url "$RegisterUrl/registers/initiate" `
        -Headers @{ Authorization = "Bearer $Token" } `
        -Body @{
            name = "Performance Test Register"
            description = "Register for performance benchmarking"
            tenantId = "00000000-0000-0000-0000-000000000000"
            advertise = $false
            owners = @(
                @{
                    userId = "perf-admin"
                    walletId = $walletAddress
                }
            )
            metadata = @{
                source = "performance-test"
                timestamp = (Get-Date).ToString("o")
            }
        }

    $registerId = $initiateResponse.registerId
    $nonce = $initiateResponse.nonce
    $attestations = $initiateResponse.attestationsToSign

    # Sign attestations
    $signedAttestations = @()

    foreach ($att in $attestations) {
        $dataToSignHex = $att.dataToSign

        # Convert hex to bytes then to base64
        $hashBytes = [byte[]]::new($dataToSignHex.Length / 2)
        for ($i = 0; $i -lt $hashBytes.Length; $i++) {
            $hashBytes[$i] = [Convert]::ToByte($dataToSignHex.Substring($i * 2, 2), 16)
        }
        $dataToSignBase64 = [Convert]::ToBase64String($hashBytes)

        $signResponse = Invoke-ApiRequest -Method Post `
            -Url "$WalletUrl/v1/wallets/$($att.walletId)/sign" `
            -Headers @{ Authorization = "Bearer $Token" } `
            -Body @{
                transactionData = $dataToSignBase64
                isPreHashed = $true
            }

        $signedAttestations += @{
            role = $att.role
            walletId = $att.walletId
            signature = $signResponse.signature
        }
    }

    # Finalize register
    $finalizeResponse = Invoke-ApiRequest -Method Post `
        -Url "$RegisterUrl/registers/finalize" `
        -Headers @{ Authorization = "Bearer $Token" } `
        -Body @{
            registerId = $registerId
            nonce = $nonce
            signedAttestations = $signedAttestations
        }

    Write-Success "Created register: $registerId"

    return @{
        RegisterId = $registerId
        WalletAddress = $walletAddress
    }
}

function New-TestTransaction {
    param(
        [string]$RegisterId,
        [string]$WalletAddress,
        [int]$PayloadBytes,
        [string]$Token
    )

    # Generate random payload
    $payload = @{
        testData = -join ((1..$PayloadBytes) | ForEach-Object {
            [char](Get-Random -Minimum 65 -Maximum 90)
        })
        timestamp = (Get-Date).ToString("o")
        sequence = Get-Random -Minimum 1 -Maximum 1000000
    }

    $transaction = @{
        registerId = $RegisterId
        senderAddress = $WalletAddress
        actionType = "TEST_TRANSACTION"
        payload = $payload
    }

    return $transaction
}

# ============================================================================
# Test 1: Payload Size Performance
# ============================================================================

function Test-PayloadSizes {
    param(
        [string]$RegisterId,
        [string]$WalletAddress,
        [string]$Token
    )

    Write-Banner "TEST 1: Payload Size Performance"

    foreach ($sizeStr in $Script:TestConfig.PayloadSizes) {
        Write-Step "Testing payload size: $sizeStr"

        $sizeBytes = ConvertTo-Bytes $sizeStr
        $iterations = if ($sizeStr -match "KB") { 100 } elseif ($sizeStr -match "^(500|1)") { 25 } else { 50 }
        if ($QuickTest) { $iterations = [Math]::Min($iterations, 20) }

        $latencies = @()
        $errors = 0

        Write-Info "Running $iterations transactions with $sizeStr payloads..."

        for ($i = 1; $i -le $iterations; $i++) {
            Write-Progress -Activity "Payload Test: $sizeStr" -Status "Transaction $i of $iterations" -PercentComplete (($i / $iterations) * 100)

            $tx = New-TestTransaction -RegisterId $RegisterId -WalletAddress $WalletAddress -PayloadBytes $sizeBytes -Token $Token

            $result = Invoke-ApiRequest -Method Post `
                -Url "$RegisterUrl/registers/$RegisterId/transactions" `
                -Headers @{ Authorization = "Bearer $Token" } `
                -Body $tx `
                -ReturnTiming

            if ($result.Success) {
                $latencies += $result.Duration
            } else {
                $errors++
            }

            Start-Sleep -Milliseconds 50
        }

        Write-Progress -Activity "Payload Test: $sizeStr" -Completed

        $stats = Get-Statistics $latencies

        Write-Success "Completed $sizeStr test"
        Write-Metric "Successful" "$($iterations - $errors) / $iterations" "txs"
        Write-Metric "Error Rate" "$(($errors / $iterations * 100).ToString('F2'))" "%"
        Write-Metric "Mean Latency" "$($stats.Mean.ToString('F2'))" "ms"
        Write-Metric "Median Latency" "$($stats.Median.ToString('F2'))" "ms"
        Write-Metric "P95 Latency" "$($stats.P95.ToString('F2'))" "ms"
        Write-Metric "P99 Latency" "$($stats.P99.ToString('F2'))" "ms"

        $Script:Results.Tests.PayloadSize += @{
            PayloadSize = $sizeStr
            PayloadBytes = $sizeBytes
            Iterations = $iterations
            Successful = $iterations - $errors
            Errors = $errors
            Statistics = $stats
        }

        $Script:Results.Summary.TotalTests += $iterations
        $Script:Results.Summary.TotalErrors += $errors
    }
}

# ============================================================================
# Test 2: Throughput Testing
# ============================================================================

function Test-Throughput {
    param(
        [string]$RegisterId,
        [string]$WalletAddress,
        [string]$Token
    )

    Write-Banner "TEST 2: Throughput Testing"

    $duration = $Script:TestConfig.ThroughputDuration
    $payloadSize = ConvertTo-Bytes "5KB"

    Write-Step "Sustained throughput test ($duration seconds, 5KB payloads)"

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $transactions = 0
    $errors = 0
    $latencies = @()

    while ($stopwatch.Elapsed.TotalSeconds -lt $duration) {
        $tx = New-TestTransaction -RegisterId $RegisterId -WalletAddress $WalletAddress -PayloadBytes $payloadSize -Token $Token

        $result = Invoke-ApiRequest -Method Post `
            -Url "$RegisterUrl/registers/$RegisterId/transactions" `
            -Headers @{ Authorization = "Bearer $Token" } `
            -Body $tx `
            -ReturnTiming

        $transactions++

        if ($result.Success) {
            $latencies += $result.Duration
        } else {
            $errors++
        }

        if ($transactions % 10 -eq 0) {
            $elapsed = $stopwatch.Elapsed.TotalSeconds
            $tps = $transactions / $elapsed
            Write-Progress -Activity "Throughput Test" `
                -Status "$transactions txs in $([int]$elapsed)s ($($tps.ToString('F2')) TPS)" `
                -PercentComplete (($elapsed / $duration) * 100)
        }

        Start-Sleep -Milliseconds 10
    }

    $stopwatch.Stop()
    Write-Progress -Activity "Throughput Test" -Completed

    $totalSeconds = $stopwatch.Elapsed.TotalSeconds
    $tps = $transactions / $totalSeconds
    $stats = Get-Statistics $latencies

    Write-Success "Throughput test completed"
    Write-Metric "Duration" "$($totalSeconds.ToString('F2'))" "seconds"
    Write-Metric "Total Transactions" $transactions "txs"
    Write-Metric "Successful" "$($transactions - $errors)" "txs"
    Write-Metric "Errors" $errors "txs"
    Write-Metric "Throughput (TPS)" "$($tps.ToString('F2'))" "txs/sec"
    Write-Metric "Mean Latency" "$($stats.Mean.ToString('F2'))" "ms"
    Write-Metric "P95 Latency" "$($stats.P95.ToString('F2'))" "ms"

    $Script:Results.Tests.Throughput += @{
        Duration = $totalSeconds
        Transactions = $transactions
        Successful = $transactions - $errors
        Errors = $errors
        TPS = $tps
        Statistics = $stats
    }

    $Script:Results.Summary.TotalTests += $transactions
    $Script:Results.Summary.TotalErrors += $errors
}

# ============================================================================
# Test 3: Latency Benchmarks
# ============================================================================

function Test-Latency {
    param(
        [string]$RegisterId,
        [string]$WalletAddress,
        [string]$Token
    )

    Write-Banner "TEST 3: Latency Benchmarks"

    $scenarios = @(
        @{ Name = "Single-threaded"; Concurrency = 1; Delay = 0 }
        @{ Name = "Light load"; Concurrency = 1; Delay = 100 }
        @{ Name = "Moderate load"; Concurrency = 1; Delay = 50 }
        @{ Name = "Heavy load"; Concurrency = 1; Delay = 10 }
    )

    $payloadSize = ConvertTo-Bytes "5KB"
    $iterations = $Script:TestConfig.LatencyIterations

    foreach ($scenario in $scenarios) {
        Write-Step "Testing: $($scenario.Name) ($iterations iterations)"

        $latencies = @()
        $errors = 0

        for ($i = 1; $i -le $iterations; $i++) {
            Write-Progress -Activity "Latency Test: $($scenario.Name)" `
                -Status "Transaction $i of $iterations" `
                -PercentComplete (($i / $iterations) * 100)

            $tx = New-TestTransaction -RegisterId $RegisterId -WalletAddress $WalletAddress -PayloadBytes $payloadSize -Token $Token

            $result = Invoke-ApiRequest -Method Post `
                -Url "$RegisterUrl/registers/$RegisterId/transactions" `
                -Headers @{ Authorization = "Bearer $Token" } `
                -Body $tx `
                -ReturnTiming

            if ($result.Success) {
                $latencies += $result.Duration
            } else {
                $errors++
            }

            if ($scenario.Delay -gt 0) {
                Start-Sleep -Milliseconds $scenario.Delay
            }
        }

        Write-Progress -Activity "Latency Test: $($scenario.Name)" -Completed

        $stats = Get-Statistics $latencies

        Write-Success "$($scenario.Name) completed"
        Write-Metric "Successful" "$($iterations - $errors) / $iterations" "txs"
        Write-Metric "Min Latency" "$($stats.Min.ToString('F2'))" "ms"
        Write-Metric "Mean Latency" "$($stats.Mean.ToString('F2'))" "ms"
        Write-Metric "Median Latency" "$($stats.Median.ToString('F2'))" "ms"
        Write-Metric "P95 Latency" "$($stats.P95.ToString('F2'))" "ms"
        Write-Metric "P99 Latency" "$($stats.P99.ToString('F2'))" "ms"
        Write-Metric "Max Latency" "$($stats.Max.ToString('F2'))" "ms"

        $Script:Results.Tests.Latency += @{
            Scenario = $scenario.Name
            Iterations = $iterations
            Successful = $iterations - $errors
            Errors = $errors
            Statistics = $stats
        }

        $Script:Results.Summary.TotalTests += $iterations
        $Script:Results.Summary.TotalErrors += $errors
    }
}

# ============================================================================
# Test 4: Concurrency Testing
# ============================================================================

function Test-Concurrency {
    param(
        [string]$RegisterId,
        [string]$WalletAddress,
        [string]$Token
    )

    Write-Banner "TEST 4: Concurrency Testing"

    $payloadSize = ConvertTo-Bytes "5KB"
    $txPerWorker = 50
    if ($QuickTest) { $txPerWorker = 20 }

    foreach ($workers in $Script:TestConfig.ConcurrencyLevels) {
        Write-Step "Testing: $workers concurrent workers ($txPerWorker txs each)"

        $totalTxs = $workers * $txPerWorker
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        $jobs = 1..$workers | ForEach-Object {
            Start-Job -ScriptBlock {
                param($RegisterId, $WalletAddress, $Token, $RegisterUrl, $Count, $PayloadSize)

                $results = @{
                    Latencies = @()
                    Errors = 0
                }

                for ($i = 1; $i -le $Count; $i++) {
                    try {
                        $payload = @{
                            testData = -join ((1..$PayloadSize) | ForEach-Object {
                                [char](Get-Random -Minimum 65 -Maximum 90)
                            })
                            timestamp = (Get-Date).ToString("o")
                            sequence = Get-Random
                        }

                        $tx = @{
                            registerId = $RegisterId
                            senderAddress = $WalletAddress
                            actionType = "TEST_TRANSACTION"
                            payload = $payload
                        }

                        $sw = [System.Diagnostics.Stopwatch]::StartNew()

                        $response = Invoke-RestMethod -Method Post `
                            -Uri "$RegisterUrl/registers/$RegisterId/transactions" `
                            -Headers @{ Authorization = "Bearer $Token" } `
                            -Body ($tx | ConvertTo-Json -Depth 10) `
                            -ContentType "application/json"

                        $sw.Stop()
                        $results.Latencies += $sw.Elapsed.TotalMilliseconds
                    }
                    catch {
                        $results.Errors++
                    }
                }

                return $results
            } -ArgumentList $RegisterId, $WalletAddress, $Token, $RegisterUrl, $txPerWorker, $payloadSize
        }

        Write-Info "Waiting for $workers workers to complete..."
        $jobResults = $jobs | Wait-Job | Receive-Job
        $jobs | Remove-Job

        $stopwatch.Stop()

        $allLatencies = @()
        $totalErrors = 0

        foreach ($result in $jobResults) {
            $allLatencies += $result.Latencies
            $totalErrors += $result.Errors
        }

        $stats = Get-Statistics $allLatencies
        $totalSeconds = $stopwatch.Elapsed.TotalSeconds
        $tps = $totalTxs / $totalSeconds

        Write-Success "$workers workers completed"
        Write-Metric "Total Transactions" $totalTxs "txs"
        Write-Metric "Duration" "$($totalSeconds.ToString('F2'))" "seconds"
        Write-Metric "Throughput" "$($tps.ToString('F2'))" "txs/sec"
        Write-Metric "Successful" "$($totalTxs - $totalErrors)" "txs"
        Write-Metric "Errors" $totalErrors "txs"
        Write-Metric "Mean Latency" "$($stats.Mean.ToString('F2'))" "ms"
        Write-Metric "P95 Latency" "$($stats.P95.ToString('F2'))" "ms"

        $Script:Results.Tests.Concurrency += @{
            Workers = $workers
            TransactionsPerWorker = $txPerWorker
            TotalTransactions = $totalTxs
            Duration = $totalSeconds
            TPS = $tps
            Successful = $totalTxs - $totalErrors
            Errors = $totalErrors
            Statistics = $stats
        }

        $Script:Results.Summary.TotalTests += $totalTxs
        $Script:Results.Summary.TotalErrors += $totalErrors

        Start-Sleep -Seconds 2
    }
}

# ============================================================================
# Test 5: Docket Building Performance
# ============================================================================

function Test-DocketBuilding {
    param(
        [string]$RegisterId,
        [string]$WalletAddress,
        [string]$Token
    )

    Write-Banner "TEST 5: Docket Building Performance"

    $payloadSize = ConvertTo-Bytes "2KB"

    foreach ($docketSize in $Script:TestConfig.DocketSizes) {
        Write-Step "Testing: Docket with $docketSize transactions"

        # Submit transactions
        Write-Info "Submitting $docketSize transactions..."
        $txIds = @()

        for ($i = 1; $i -le $docketSize; $i++) {
            Write-Progress -Activity "Docket Test: $docketSize txs" `
                -Status "Submitting transaction $i" `
                -PercentComplete (($i / $docketSize) * 100)

            $tx = New-TestTransaction -RegisterId $RegisterId -WalletAddress $WalletAddress -PayloadBytes $payloadSize -Token $Token

            $response = Invoke-ApiRequest -Method Post `
                -Url "$RegisterUrl/registers/$RegisterId/transactions" `
                -Headers @{ Authorization = "Bearer $Token" } `
                -Body $tx

            $txIds += $response.txId
            Start-Sleep -Milliseconds 20
        }

        Write-Progress -Activity "Docket Test: $docketSize txs" -Completed

        # Trigger docket build
        Write-Info "Triggering docket build..."

        $docketBuildStart = Get-Date

        try {
            $docketResponse = Invoke-ApiRequest -Method Post `
                -Url "$RegisterUrl/registers/$RegisterId/dockets" `
                -Headers @{ Authorization = "Bearer $Token" } `
                -Body @{
                    transactionIds = $txIds
                }

            $docketBuildEnd = Get-Date
            $buildTime = ($docketBuildEnd - $docketBuildStart).TotalMilliseconds

            Write-Success "Docket built successfully"
            Write-Metric "Docket ID" $docketResponse.docketId
            Write-Metric "Build Time" "$($buildTime.ToString('F2'))" "ms"
            Write-Metric "Transactions" $docketSize "txs"
            Write-Metric "TPS" "$(($docketSize / ($buildTime / 1000)).ToString('F2'))" "txs/sec"

            $Script:Results.Tests.DocketBuilding += @{
                DocketSize = $docketSize
                DocketId = $docketResponse.docketId
                BuildTime = $buildTime
                TPS = $docketSize / ($buildTime / 1000)
            }
        }
        catch {
            Write-Error "Docket build failed: $($_.Exception.Message)"

            $Script:Results.Tests.DocketBuilding += @{
                DocketSize = $docketSize
                Error = $_.Exception.Message
            }
        }

        Start-Sleep -Seconds 2
    }
}

# ============================================================================
# Report Generation
# ============================================================================

function Export-Results {
    Write-Banner "Generating Performance Report"

    # Save JSON results
    $Script:Results | ConvertTo-Json -Depth 10 | Set-Content -Path $Script:ResultFile
    Write-Success "Saved detailed results: $Script:ResultFile"

    # Generate markdown summary
    $summary = @"
# Performance Benchmark Report

**Date:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Profile:** $Profile
**Environment:** $($Script:Results.Environment.OS)

---

## Executive Summary

| Metric | Value |
|--------|-------|
| Total Tests | $($Script:Results.Summary.TotalTests) |
| Total Errors | $($Script:Results.Summary.TotalErrors) |
| Error Rate | $(($Script:Results.Summary.TotalErrors / $Script:Results.Summary.TotalTests * 100).ToString('F2'))% |
| Duration | $($Script:Results.Summary.Duration.ToString('F2')) seconds |

---

## Test Results

### 1. Payload Size Performance

| Payload Size | Iterations | Success Rate | Mean Latency | P95 Latency | P99 Latency |
|--------------|------------|--------------|--------------|-------------|-------------|
"@

    foreach ($test in $Script:Results.Tests.PayloadSize) {
        $successRate = (($test.Successful / $test.Iterations) * 100).ToString('F2')
        $summary += "`n| $($test.PayloadSize) | $($test.Iterations) | $successRate% | $($test.Statistics.Mean.ToString('F2'))ms | $($test.Statistics.P95.ToString('F2'))ms | $($test.Statistics.P99.ToString('F2'))ms |"
    }

    $summary += @"

### 2. Throughput Testing

| Metric | Value |
|--------|-------|
| Duration | $($Script:Results.Tests.Throughput[0].Duration.ToString('F2'))s |
| Total Transactions | $($Script:Results.Tests.Throughput[0].Transactions) |
| Successful | $($Script:Results.Tests.Throughput[0].Successful) |
| Throughput (TPS) | $($Script:Results.Tests.Throughput[0].TPS.ToString('F2')) |
| Mean Latency | $($Script:Results.Tests.Throughput[0].Statistics.Mean.ToString('F2'))ms |
| P95 Latency | $($Script:Results.Tests.Throughput[0].Statistics.P95.ToString('F2'))ms |

### 3. Latency Benchmarks

| Scenario | Success Rate | Min | Mean | Median | P95 | P99 | Max |
|----------|--------------|-----|------|--------|-----|-----|-----|
"@

    foreach ($test in $Script:Results.Tests.Latency) {
        $successRate = (($test.Successful / $test.Iterations) * 100).ToString('F2')
        $summary += "`n| $($test.Scenario) | $successRate% | $($test.Statistics.Min.ToString('F2'))ms | $($test.Statistics.Mean.ToString('F2'))ms | $($test.Statistics.Median.ToString('F2'))ms | $($test.Statistics.P95.ToString('F2'))ms | $($test.Statistics.P99.ToString('F2'))ms | $($test.Statistics.Max.ToString('F2'))ms |"
    }

    $summary += @"

### 4. Concurrency Testing

| Workers | Total Txs | Duration | TPS | Success Rate | Mean Latency | P95 Latency |
|---------|-----------|----------|-----|--------------|--------------|-------------|
"@

    foreach ($test in $Script:Results.Tests.Concurrency) {
        $successRate = (($test.Successful / $test.TotalTransactions) * 100).ToString('F2')
        $summary += "`n| $($test.Workers) | $($test.TotalTransactions) | $($test.Duration.ToString('F2'))s | $($test.TPS.ToString('F2')) | $successRate% | $($test.Statistics.Mean.ToString('F2'))ms | $($test.Statistics.P95.ToString('F2'))ms |"
    }

    $summary += @"

### 5. Docket Building Performance

| Docket Size | Build Time | TPS | Status |
|-------------|------------|-----|--------|
"@

    foreach ($test in $Script:Results.Tests.DocketBuilding) {
        if ($test.Error) {
            $summary += "`n| $($test.DocketSize) | - | - | ✗ Error: $($test.Error) |"
        } else {
            $summary += "`n| $($test.DocketSize) | $($test.BuildTime.ToString('F2'))ms | $($test.TPS.ToString('F2')) | ✓ Success |"
        }
    }

    $summary += @"

---

## Analysis & Recommendations

### Performance Bottlenecks Identified

1. **[AUTO-ANALYSIS PLACEHOLDER]** - Review detailed JSON for patterns
2. Check P95/P99 latencies for spikes indicating bottlenecks
3. Review error rates and concurrency degradation

### Quick Win Optimizations

- [ ] Add MongoDB indexes for transaction queries
- [ ] Implement connection pooling tuning
- [ ] Add request batching for high-volume scenarios
- [ ] Review YARP routing overhead (compare 'gateway' vs 'direct' profiles)

### Next Steps

1. Run tests with 'direct' profile to isolate API Gateway overhead
2. Monitor MongoDB performance during peak load
3. Profile Register Service with dotnet-trace
4. Implement recommended optimizations
5. Re-run benchmark to measure improvements

---

**Report Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
"@

    $summary | Set-Content -Path $Script:SummaryFile
    Write-Success "Saved summary report: $Script:SummaryFile"

    Write-Host ""
    Write-Info "View results at:"
    Write-Info "  JSON: $Script:ResultFile"
    Write-Info "  Summary: $Script:SummaryFile"
}

# ============================================================================
# Main Execution
# ============================================================================

Write-Banner "Sorcha Performance Benchmark Suite"

Write-Info "Configuration:"
Write-Info "  Profile: $Profile"
Write-Info "  Quick Test: $QuickTest"
Write-Info "  Max Payload: $MaxPayloadSize"
Write-Info "  Iterations: $Iterations"
Write-Info "  Concurrency: $Concurrency"

$totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Check Docker version
    try {
        $dockerVersion = docker --version
        $Script:Results.Environment.Docker = $dockerVersion
        Write-Info "Docker: $dockerVersion"
    } catch {
        Write-Error "Docker not found. Please ensure Docker is installed and running."
        exit 1
    }

    # Authenticate
    $token = Get-AuthToken

    # Create test register
    $register = New-TestRegister -Token $token
    $registerId = $register.RegisterId
    $walletAddress = $register.WalletAddress

    # Run tests
    Test-PayloadSizes -RegisterId $registerId -WalletAddress $walletAddress -Token $token
    Test-Throughput -RegisterId $registerId -WalletAddress $walletAddress -Token $token
    Test-Latency -RegisterId $registerId -WalletAddress $walletAddress -Token $token
    Test-Concurrency -RegisterId $registerId -WalletAddress $walletAddress -Token $token
    Test-DocketBuilding -RegisterId $registerId -WalletAddress $walletAddress -Token $token

    $totalStopwatch.Stop()
    $Script:Results.Summary.Duration = $totalStopwatch.Elapsed.TotalSeconds

    # Generate reports
    Export-Results

    Write-Banner "Performance Benchmark Complete!"

    Write-Success "All tests completed successfully"
    Write-Metric "Total Duration" "$($Script:Results.Summary.Duration.ToString('F2'))" "seconds"
    Write-Metric "Total Tests" $Script:Results.Summary.TotalTests "txs"
    Write-Metric "Total Errors" $Script:Results.Summary.TotalErrors "txs"
    Write-Metric "Overall Success Rate" "$(((1 - ($Script:Results.Summary.TotalErrors / $Script:Results.Summary.TotalTests)) * 100).ToString('F2'))" "%"

    Write-Host ""
    Write-Info "Next steps:"
    Write-Info "  1. Review summary: $Script:SummaryFile"
    Write-Info "  2. Analyze detailed results: $Script:ResultFile"
    Write-Info "  3. Compare with baseline metrics in README.md"
    Write-Info "  4. Identify optimization opportunities"

    exit 0
}
catch {
    Write-Error "Benchmark failed: $($_.Exception.Message)"
    Write-Error $_.ScriptStackTrace

    $totalStopwatch.Stop()
    $Script:Results.Summary.Duration = $totalStopwatch.Elapsed.TotalSeconds

    Export-Results

    exit 1
}
