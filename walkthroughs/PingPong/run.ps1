#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# PingPong — Run
# Execute ping-pong rounds: alternating action 0 (ping) and action 1 (pong).

param(
    [int]$RoundTrips = 5,
    [switch]$ShowJson
)

$ErrorActionPreference = "Stop"

$modulePath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "modules/SorchaWalkthrough/SorchaWalkthrough.psm1"
Import-Module $modulePath -Force

Write-WtBanner "PingPong — Run ($RoundTrips round-trips)"

# Load state
$stateFile = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "state.json"
if (-not (Test-Path $stateFile)) {
    Write-WtFail "No state.json found. Run setup.ps1 first."
    exit 1
}
$state = Get-Content -Path $stateFile -Raw | ConvertFrom-Json
$headers = @{ Authorization = "Bearer $($state.adminToken)" }

$stepsPassed = 0
$totalSteps = 0
$start = Get-Date

# ============================================================================
# Step 1: Create Instance
# ============================================================================
Write-WtStep "Step 1: Create Workflow Instance"
$totalSteps++

$instanceId = ""
try {
    $instanceBody = @{
        blueprintId = $state.blueprintId
        registerId  = $state.registerId
        tenantId    = $state.organizationId
        metadata    = @{ source = "walkthrough"; createdBy = "PingPong/run.ps1" }
    }

    $instanceResponse = Invoke-SorchaApi -Method POST `
        -Uri "$($state.blueprintUrl)/instances/" `
        -Body $instanceBody `
        -Headers $headers `
        -ShowJson:$ShowJson

    $instanceId = $instanceResponse.id
    Write-WtSuccess "Instance created: $instanceId"
    Write-WtInfo "State: $($instanceResponse.state)"
    $stepsPassed++
} catch {
    Write-WtFail "Failed to create instance: $($_.Exception.Message)"
    exit 1
}

# ============================================================================
# Step 2: Execute Rounds
# ============================================================================
Write-WtStep "Step 2: Execute $RoundTrips Round-Trips ($($RoundTrips * 2) actions)"
$totalSteps++

$counter = 1
$allSucceeded = $true
$actionResults = @()

for ($round = 1; $round -le $RoundTrips; $round++) {
    $pingOk = $false
    $pongOk = $false

    # Ping (action 0)
    try {
        $null = Invoke-SorchaAction `
            -BlueprintUrl $state.blueprintUrl `
            -InstanceId $instanceId `
            -ActionId "0" `
            -BlueprintId $state.blueprintId `
            -SenderWallet $state.pingWallet `
            -RegisterId $state.registerId `
            -Token $state.adminToken `
            -PayloadData @{ message = "Ping #$round"; counter = $counter }
        $pingOk = $true
    } catch {
        Write-WtFail "Ping failed at round $round`: $($_.Exception.Message)"
        $allSucceeded = $false
    }
    $actionResults += @{ Round = $round; Actor = "Ping"; Counter = $counter; Success = $pingOk }
    $counter++

    # Pong (action 1)
    try {
        $null = Invoke-SorchaAction `
            -BlueprintUrl $state.blueprintUrl `
            -InstanceId $instanceId `
            -ActionId "1" `
            -BlueprintId $state.blueprintId `
            -SenderWallet $state.pongWallet `
            -RegisterId $state.registerId `
            -Token $state.adminToken `
            -PayloadData @{ message = "Pong #$round"; counter = $counter }
        $pongOk = $true
    } catch {
        Write-WtFail "Pong failed at round $round`: $($_.Exception.Message)"
        $allSucceeded = $false
    }
    $actionResults += @{ Round = $round; Actor = "Pong"; Counter = $counter; Success = $pongOk }
    $counter++

    # Progress
    $pingStatus = if ($pingOk) { "OK" } else { "FAIL" }
    $pongStatus = if ($pongOk) { "OK" } else { "FAIL" }
    $pingColor = if ($pingOk) { "Green" } else { "Red" }
    $pongColor = if ($pongOk) { "Green" } else { "Red" }
    Write-Host "  [Round $($round.ToString().PadLeft(2))/$RoundTrips] " -NoNewline -ForegroundColor White
    Write-Host "Ping $pingStatus" -NoNewline -ForegroundColor $pingColor
    Write-Host " -> " -NoNewline -ForegroundColor Gray
    Write-Host "Pong $pongStatus" -ForegroundColor $pongColor
}

if ($allSucceeded) {
    Write-WtSuccess "All $($RoundTrips * 2) actions executed successfully"
    $stepsPassed++
} else {
    $failCount = ($actionResults | Where-Object { -not $_.Success }).Count
    Write-WtFail "$failCount of $($RoundTrips * 2) actions failed"
}

# ============================================================================
# Step 3: Verify Instance
# ============================================================================
Write-WtStep "Step 3: Verify Instance State"
$totalSteps++

try {
    $instanceState = Invoke-SorchaApi -Method GET `
        -Uri "$($state.blueprintUrl)/instances/$instanceId" `
        -Headers $headers

    Write-WtInfo "Instance state: $($instanceState.state)"
    if ($instanceState.state -eq "Active" -or $instanceState.state -eq 1) {
        Write-WtSuccess "Instance still active (cyclic workflow continues)"
    }
    $stepsPassed++
} catch {
    Write-WtFail "Failed to verify: $($_.Exception.Message)"
}

# ============================================================================
# Summary
# ============================================================================
$duration = (Get-Date) - $start
$succeeded = ($actionResults | Where-Object { $_.Success }).Count

Write-Host ""
Write-WtBanner "PingPong — Results"
Write-Host "  Round-trips: $([math]::Floor($succeeded / 2))/$RoundTrips" -ForegroundColor White
Write-Host "  Actions:     $succeeded/$($actionResults.Count)" -ForegroundColor White
Write-Host "  Duration:    $([math]::Round($duration.TotalSeconds, 1))s" -ForegroundColor White
Write-Host ""

$statusColor = if ($stepsPassed -eq $totalSteps) { "Green" } else { "Red" }
Write-Host "  Steps: $stepsPassed/$totalSteps passed" -ForegroundColor $statusColor
Write-Host ""

if ($stepsPassed -eq $totalSteps -and $allSucceeded) {
    Write-Host "  RESULT: PASS" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  RESULT: FAIL" -ForegroundColor Red
    exit 1
}
