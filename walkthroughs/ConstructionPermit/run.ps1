#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# ConstructionPermit — Run
# Execute 3 scenarios: A (low-risk), B (high-risk), C (rejection).

param(
    [ValidateSet('A', 'B', 'C', 'all')]
    [string]$Scenario = 'all',
    [switch]$ShowJson
)

$ErrorActionPreference = "Stop"

$modulePath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "modules/SorchaWalkthrough/SorchaWalkthrough.psm1"
Import-Module $modulePath -Force

Write-WtBanner "ConstructionPermit — Run"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$stateFile = Join-Path $scriptDir "state.json"
if (-not (Test-Path $stateFile)) { Write-WtFail "No state.json. Run setup.ps1 first."; exit 1 }
$state = Get-Content -Path $stateFile -Raw | ConvertFrom-Json

# Convert wallet PSObject to hashtable
$wallets = @{}
foreach ($prop in $state.wallets.PSObject.Properties) { $wallets[$prop.Name] = $prop.Value }

# Action-to-sender mapping for construction permit
$actionSenderMap = @{
    1 = "contractor"
    2 = "structural-engineer"
    3 = "environmental-assessor"
    4 = "planning-officer"
    5 = "building-control"
    6 = "planning-officer"
}

$scenariosToRun = if ($Scenario -eq 'all') { @('A', 'B', 'C') } else { @($Scenario) }
$scenarioFiles = @{
    'A' = "data/scenario-a-low-risk.json"
    'B' = "data/scenario-b-high-risk.json"
    'C' = "data/scenario-c-rejection.json"
}

$allPassed = $true
$scenarioResults = @{}
$start = Get-Date

foreach ($sid in $scenariosToRun) {
    $scenarioPath = Join-Path $scriptDir $scenarioFiles[$sid]
    if (-not (Test-Path $scenarioPath)) { Write-WtFail "Scenario file not found: $scenarioPath"; continue }

    $scenarioData = Get-Content -Path $scenarioPath -Raw | ConvertFrom-Json
    $expectedPath = @($scenarioData.expectedPath)
    $isRejection = [bool]$scenarioData.expectedRejection

    Write-WtStep "Scenario $sid`: $($scenarioData.name)"

    # Create instance
    $instanceBody = @{
        blueprintId = $state.blueprintId; registerId = $state.registerId
        tenantId = $state.organizationId
        metadata = @{ source = "walkthrough"; scenario = $sid; scenarioName = $scenarioData.name }
    }
    $ir = Invoke-SorchaApi -Method POST -Uri "$($state.blueprintUrl)/instances/" -Body $instanceBody `
        -Headers @{ Authorization = "Bearer $($state.adminToken)" } -ShowJson:$ShowJson
    $instanceId = $ir.id
    Write-WtSuccess "Instance: $instanceId"

    $actionsOk = 0
    $scenarioStart = Get-Date

    foreach ($actionId in $expectedPath) {
        $actionIdStr = "$actionId"
        $sender = $actionSenderMap[[int]$actionId]
        $senderWallet = $wallets[$sender]
        $actionData = $scenarioData.actions."$actionId"

        # Convert PSObject to hashtable
        $payloadData = @{}
        if ($actionData) {
            foreach ($prop in $actionData.PSObject.Properties) {
                $payloadData[$prop.Name] = $prop.Value
            }
        }

        $isLastAction = ($actionId -eq $expectedPath[-1])
        $isRejectionAction = $isRejection -and $isLastAction

        try {
            if ($isRejectionAction) {
                $null = Invoke-SorchaAction `
                    -BlueprintUrl $state.blueprintUrl -InstanceId $instanceId `
                    -ActionId $actionIdStr -BlueprintId $state.blueprintId `
                    -SenderWallet $senderWallet -RegisterId $state.registerId `
                    -Token $state.adminToken `
                    -Reject -RejectionReason $scenarioData.rejectionReason
            } else {
                $response = Invoke-SorchaAction `
                    -BlueprintUrl $state.blueprintUrl -InstanceId $instanceId `
                    -ActionId $actionIdStr -BlueprintId $state.blueprintId `
                    -SenderWallet $senderWallet -RegisterId $state.registerId `
                    -Token $state.adminToken -PayloadData $payloadData

                if ($response.calculatedValues) {
                    foreach ($calc in $response.calculatedValues.PSObject.Properties) {
                        Write-WtInfo "  Calculated: $($calc.Name) = $($calc.Value)"
                    }
                }
            }
            $actionsOk++
        } catch {
            Write-WtFail "Action $actionIdStr ($sender) failed: $($_.Exception.Message)"
            $allPassed = $false
        }
    }

    $scenarioDuration = (Get-Date) - $scenarioStart
    $scenarioPassed = ($actionsOk -eq $expectedPath.Count)
    $outcome = if ($isRejection) { "REJECTED" } else { "APPROVED" }

    $scenarioResults[$sid] = @{
        Name = $scenarioData.name; Passed = $scenarioPassed
        Actions = "$actionsOk/$($expectedPath.Count)"; Outcome = $outcome
        Duration = [math]::Round($scenarioDuration.TotalSeconds, 1)
    }

    if ($scenarioPassed) { Write-WtSuccess "Scenario $sid`: $outcome ($actionsOk actions)" }
    else { Write-WtFail "Scenario $sid`: incomplete"; $allPassed = $false }
}

# Summary
$duration = (Get-Date) - $start
Write-Host ""
Write-WtBanner "ConstructionPermit — Results"

foreach ($sid in $scenariosToRun) {
    $sr = $scenarioResults[$sid]
    $icon = if ($sr.Passed) { "[OK]" } else { "[X]" }
    $color = if ($sr.Passed) { "Green" } else { "Red" }
    Write-Host "  $icon Scenario $sid`: $($sr.Name) ($($sr.Outcome), $($sr.Actions), $($sr.Duration)s)" -ForegroundColor $color
}

Write-Host ""
Write-Host "  Duration: $([math]::Round($duration.TotalSeconds, 1))s" -ForegroundColor White
Write-Host ""

if ($allPassed) { Write-Host "  RESULT: PASS" -ForegroundColor Green; exit 0 }
else { Write-Host "  RESULT: FAIL" -ForegroundColor Red; exit 1 }
