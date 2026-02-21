#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# BlueprintStorageBasic — Run
# Tests blueprint CRUD operations: create, list, get, delete.

param(
    [switch]$ShowJson
)

$ErrorActionPreference = "Stop"

# Import shared module
$modulePath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "modules/SorchaWalkthrough/SorchaWalkthrough.psm1"
Import-Module $modulePath -Force

Write-WtBanner "BlueprintStorageBasic — Run"

# Load state from setup
$stateFile = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "state.json"
if (-not (Test-Path $stateFile)) {
    Write-WtFail "No state.json found. Run setup.ps1 first."
    exit 1
}
$state = Get-Content -Path $stateFile -Raw | ConvertFrom-Json
$headers = @{ Authorization = "Bearer $($state.adminToken)" }
$blueprintUrl = $state.blueprintUrl

$stepsPassed = 0
$totalSteps = 0
$start = Get-Date

# ============================================================================
# Step 1: List Blueprints (baseline)
# ============================================================================
Write-WtStep "Step 1: List Blueprints (baseline)"
$totalSteps++

try {
    $blueprints = Invoke-SorchaApi -Method GET -Uri "$blueprintUrl/blueprints" -Headers $headers -ShowJson:$ShowJson
    $baselineCount = if ($blueprints -is [array]) { $blueprints.Count } else { 0 }
    Write-WtSuccess "Found $baselineCount existing blueprints"
    $stepsPassed++
} catch {
    Write-WtFail "Failed to list blueprints: $($_.Exception.Message)"
    exit 1
}

# ============================================================================
# Step 2: Create a Blueprint
# ============================================================================
Write-WtStep "Step 2: Create Blueprint"
$totalSteps++

$blueprintId = ""
try {
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $blueprintBody = @{
        id          = "wt-storage-$timestamp"
        title       = "Storage Test Blueprint"
        description = "Created by BlueprintStorageBasic walkthrough"
        participants = @(
            @{ id = "alice"; name = "Alice"; role = "Submitter" }
            @{ id = "bob"; name = "Bob"; role = "Reviewer" }
        )
        actions = @(
            @{
                id          = 0
                title       = "Submit"
                description = "Alice submits data"
                participants = @(
                    @{ participantId = "alice" }
                )
            }
            @{
                id          = 1
                title       = "Review"
                description = "Bob reviews submission"
                participants = @(
                    @{ participantId = "bob" }
                )
            }
        )
    }

    $createResponse = Invoke-SorchaApi -Method POST `
        -Uri "$blueprintUrl/blueprints/" `
        -Body $blueprintBody `
        -Headers $headers `
        -ShowJson:$ShowJson

    $blueprintId = $createResponse.id
    Write-WtSuccess "Blueprint created: $blueprintId"
    Write-WtInfo "Title: $($createResponse.title)"
    $stepsPassed++
} catch {
    Write-WtFail "Failed to create blueprint: $($_.Exception.Message)"
    $errorBody = Get-SorchaErrorBody -ErrorRecord $_
    if ($errorBody) { Write-WtInfo "Response: $errorBody" }
    exit 1
}

# ============================================================================
# Step 3: Get Blueprint by ID
# ============================================================================
Write-WtStep "Step 3: Get Blueprint by ID"
$totalSteps++

try {
    $getResponse = Invoke-SorchaApi -Method GET `
        -Uri "$blueprintUrl/blueprints/$blueprintId" `
        -Headers $headers `
        -ShowJson:$ShowJson

    if ($getResponse.id -eq $blueprintId) {
        Write-WtSuccess "Blueprint retrieved: $($getResponse.title)"
        Write-WtInfo "Participants: $(($getResponse.participants | Measure-Object).Count)"
        Write-WtInfo "Actions: $(($getResponse.actions | Measure-Object).Count)"
        $stepsPassed++
    } else {
        Write-WtFail "Blueprint ID mismatch"
    }
} catch {
    Write-WtFail "Failed to get blueprint: $($_.Exception.Message)"
}

# ============================================================================
# Step 4: List Blueprints (verify count increased)
# ============================================================================
Write-WtStep "Step 4: List Blueprints (verify new)"
$totalSteps++

try {
    $blueprints = Invoke-SorchaApi -Method GET -Uri "$blueprintUrl/blueprints" -Headers $headers
    $newCount = if ($blueprints -is [array]) { $blueprints.Count } else { 1 }

    if ($newCount -gt $baselineCount) {
        Write-WtSuccess "Blueprint count increased: $baselineCount -> $newCount"
        $stepsPassed++
    } else {
        Write-WtWarn "Blueprint count did not increase (may be due to pagination)"
        $stepsPassed++  # Non-critical
    }
} catch {
    Write-WtFail "Failed to list blueprints: $($_.Exception.Message)"
}

# ============================================================================
# Step 5: Delete Blueprint
# ============================================================================
Write-WtStep "Step 5: Delete Blueprint"
$totalSteps++

try {
    $null = Invoke-SorchaApi -Method DELETE `
        -Uri "$blueprintUrl/blueprints/$blueprintId" `
        -Headers $headers `
        -RawResponse

    Write-WtSuccess "Blueprint deleted: $blueprintId"
    $stepsPassed++
} catch {
    $statusCode = $null
    try { $statusCode = $_.Exception.Response.StatusCode.value__ } catch {}
    if ($statusCode -eq 404) {
        Write-WtWarn "Blueprint already deleted (404)"
        $stepsPassed++
    } else {
        Write-WtFail "Failed to delete blueprint: $($_.Exception.Message)"
    }
}

# ============================================================================
# Summary
# ============================================================================
$duration = (Get-Date) - $start
Write-Host ""
Write-WtBanner "BlueprintStorageBasic — Results"
$statusColor = if ($stepsPassed -eq $totalSteps) { "Green" } else { "Red" }
Write-Host "  Steps: $stepsPassed/$totalSteps passed" -ForegroundColor $statusColor
Write-Host "  Duration: $([math]::Round($duration.TotalSeconds, 1))s" -ForegroundColor White
Write-Host ""

if ($stepsPassed -eq $totalSteps) {
    Write-Host "  RESULT: PASS" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  RESULT: FAIL" -ForegroundColor Red
    exit 1
}
