#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# OrganizationPingPong — Run
# Execute N ping-pong round-trips using org-specific wallets.

param(
    [int]$RoundTrips = 20,
    [switch]$ShowJson
)

$ErrorActionPreference = "Stop"

$modulePath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "modules/SorchaWalkthrough/SorchaWalkthrough.psm1"
Import-Module $modulePath -Force

Write-WtBanner "OrganizationPingPong — Run ($RoundTrips round-trips)"

$stateFile = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "state.json"
if (-not (Test-Path $stateFile)) { Write-WtFail "No state.json. Run setup.ps1 first."; exit 1 }
$state = Get-Content -Path $stateFile -Raw | ConvertFrom-Json
$headers = @{ Authorization = "Bearer $($state.adminToken)" }

$stepsPassed = 0
$totalSteps = 0
$start = Get-Date

# Create instance
Write-WtStep "Step 1: Create Workflow Instance"
$totalSteps++
try {
    $instanceBody = @{
        blueprintId = $state.blueprintId; registerId = $state.registerId
        tenantId = $state.organizationId; metadata = @{ source = "walkthrough" }
    }
    $ir = Invoke-SorchaApi -Method POST -Uri "$($state.blueprintUrl)/instances/" -Body $instanceBody -Headers $headers -ShowJson:$ShowJson
    $instanceId = $ir.id
    Write-WtSuccess "Instance: $instanceId"
    $stepsPassed++
} catch { Write-WtFail "Instance creation failed: $($_.Exception.Message)"; exit 1 }

# Execute rounds
Write-WtStep "Step 2: Execute $RoundTrips Round-Trips"
$totalSteps++
$counter = 1; $allOk = $true; $results = @()
$phaseStart = Get-Date

for ($r = 1; $r -le $RoundTrips; $r++) {
    $pingOk = $false; $pongOk = $false
    try {
        $null = Invoke-SorchaAction -BlueprintUrl $state.blueprintUrl -InstanceId $instanceId -ActionId "0" `
            -BlueprintId $state.blueprintId -SenderWallet $state.alphaWallet -RegisterId $state.registerId `
            -Token $state.adminToken -PayloadData @{ message = "Ping #$r"; counter = $counter }
        $pingOk = $true
    } catch { Write-WtFail "Ping R$r`: $($_.Exception.Message)"; $allOk = $false }
    $results += @{ Round=$r; Actor="Ping"; Success=$pingOk }; $counter++

    try {
        $null = Invoke-SorchaAction -BlueprintUrl $state.blueprintUrl -InstanceId $instanceId -ActionId "1" `
            -BlueprintId $state.blueprintId -SenderWallet $state.betaWallet -RegisterId $state.registerId `
            -Token $state.adminToken -PayloadData @{ message = "Pong #$r"; counter = $counter }
        $pongOk = $true
    } catch { Write-WtFail "Pong R$r`: $($_.Exception.Message)"; $allOk = $false }
    $results += @{ Round=$r; Actor="Pong"; Success=$pongOk }; $counter++

    $pc = if ($pingOk) { "Green" } else { "Red" }; $qc = if ($pongOk) { "Green" } else { "Red" }
    Write-Host "  [Round $($r.ToString().PadLeft(2))/$RoundTrips] " -NoNewline -ForegroundColor White
    Write-Host "Ping $(if($pingOk){'OK'}else{'FAIL'})" -NoNewline -ForegroundColor $pc
    Write-Host " -> " -NoNewline -ForegroundColor Gray
    Write-Host "Pong $(if($pongOk){'OK'}else{'FAIL'})" -ForegroundColor $qc
}

$phaseDuration = (Get-Date) - $phaseStart
if ($allOk) { Write-WtSuccess "All $($RoundTrips*2) actions in $([math]::Round($phaseDuration.TotalSeconds,1))s"; $stepsPassed++ }
else { $fc = ($results | Where-Object { -not $_.Success }).Count; Write-WtFail "$fc of $($RoundTrips*2) failed" }

# Verify
Write-WtStep "Step 3: Verify Instance"
$totalSteps++
try {
    $is = Invoke-SorchaApi -Method GET -Uri "$($state.blueprintUrl)/instances/$instanceId" -Headers $headers
    Write-WtInfo "State: $($is.state)"
    if ($is.state -eq "Active" -or $is.state -eq 1) { Write-WtSuccess "Instance active (cyclic)" }
    $stepsPassed++
} catch { Write-WtFail "Verify failed" }

# Summary
$duration = (Get-Date) - $start
$ok = ($results | Where-Object { $_.Success }).Count
Write-Host ""; Write-WtBanner "OrganizationPingPong — Results"
Write-Host "  Rounds:   $([math]::Floor($ok/2))/$RoundTrips" -ForegroundColor White
Write-Host "  Actions:  $ok/$($results.Count)" -ForegroundColor White
Write-Host "  Duration: $([math]::Round($duration.TotalSeconds,1))s" -ForegroundColor White
Write-Host ""
$sc = if ($stepsPassed -eq $totalSteps) { "Green" } else { "Red" }
Write-Host "  Steps: $stepsPassed/$totalSteps" -ForegroundColor $sc
Write-Host ""
if ($stepsPassed -eq $totalSteps -and $allOk) { Write-Host "  RESULT: PASS" -ForegroundColor Green; exit 0 }
else { Write-Host "  RESULT: FAIL" -ForegroundColor Red; exit 1 }
