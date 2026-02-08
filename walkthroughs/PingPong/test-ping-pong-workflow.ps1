#!/usr/bin/env pwsh
# Ping-Pong Blueprint Workflow Walkthrough
# Tests the full template-to-execution pipeline: create blueprint, publish (with cycle warning),
# create instance, and execute 5 full round-trips (10 actions total).

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('gateway', 'direct')]
    [string]$Profile = 'gateway',

    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@sorcha.local",

    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "Dev_Pass_2025!",

    [Parameter(Mandatory=$false)]
    [int]$RoundTrips = 5,

    [Parameter(Mandatory=$false)]
    [switch]$ShowJson = $false
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Ping-Pong Blueprint Workflow Walkthrough" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration based on profile
$ApiGatewayUrl = "http://localhost"
$BlueprintServiceUrl = ""

switch ($Profile) {
    'gateway' {
        $BlueprintServiceUrl = "$ApiGatewayUrl/api"
        Write-Host "Profile: $Profile (API Gateway)" -ForegroundColor Green
    }
    'direct' {
        $BlueprintServiceUrl = "http://localhost:5000/api"
        Write-Host "Profile: $Profile (Direct)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Blueprint Service: $BlueprintServiceUrl" -ForegroundColor White
Write-Host "  Round-trips: $RoundTrips" -ForegroundColor White
Write-Host ""

# Helper functions
function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Gray
    Write-Host "  $Message" -ForegroundColor White
    Write-Host "================================================================================" -ForegroundColor Gray
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[X] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "[i] $Message" -ForegroundColor Cyan
}

$stepsPassed = 0
$totalSteps = 0

# ============================================================================
# Phase 1: Authentication
# ============================================================================

Write-Step "Step 1: Admin Authentication"
$totalSteps++

try {
    Write-Info "Authenticating admin user..."
    $loginResponse = Invoke-RestMethod `
        -Uri "$ApiGatewayUrl/api/service-auth/token" `
        -Method POST `
        -ContentType "application/x-www-form-urlencoded" `
        -Body "grant_type=password&username=$AdminEmail&password=$AdminPassword&client_id=sorcha-cli" `
        -UseBasicParsing

    $adminToken = $loginResponse.access_token
    Write-Success "Admin authenticated successfully"
    Write-Info "Token expires in: $($loginResponse.expires_in) seconds"
    $stepsPassed++
} catch {
    Write-Fail "Admin authentication failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Make sure Docker services are running: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

$headers = @{ Authorization = "Bearer $adminToken" }

# ============================================================================
# Phase 2: Create Blueprint from Ping-Pong Template JSON
# ============================================================================

Write-Step "Step 2: Load Ping-Pong Blueprint Template"
$totalSteps++

try {
    # Read the ping-pong template JSON
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $templatePath = Join-Path (Split-Path -Parent (Split-Path -Parent $scriptDir)) "examples/templates/ping-pong-template.json"
    Write-Info "Loading template from: $templatePath"
    $templateJson = Get-Content -Path $templatePath -Raw | ConvertFrom-Json

    # Extract the blueprint from the template wrapper
    $blueprintJson = $templateJson.template | ConvertTo-Json -Depth 20

    if ($ShowJson) {
        Write-Host ""
        Write-Host "Blueprint JSON:" -ForegroundColor DarkGray
        Write-Host $blueprintJson -ForegroundColor DarkGray
    }

    Write-Success "Ping-Pong template loaded"
    Write-Info "Title: $($templateJson.title)"
    Write-Info "Participants: $($templateJson.template.participants.Count)"
    Write-Info "Actions: $($templateJson.template.actions.Count)"
    $stepsPassed++
} catch {
    Write-Fail "Failed to load Ping-Pong template"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Step "Step 3: Create Blueprint in Service"
$totalSteps++

try {
    Write-Info "Creating blueprint via POST /api/blueprints/..."
    $createResponse = Invoke-RestMethod `
        -Uri "$BlueprintServiceUrl/blueprints/" `
        -Method POST `
        -ContentType "application/json" `
        -Headers $headers `
        -Body $blueprintJson `
        -UseBasicParsing

    $blueprintId = $createResponse.id
    Write-Success "Blueprint created: $blueprintId"

    if ($ShowJson) {
        Write-Host ($createResponse | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
    }
    $stepsPassed++
} catch {
    Write-Fail "Failed to create blueprint"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Phase 3: Publish Blueprint (expect cycle warning, not error)
# ============================================================================

Write-Step "Step 4: Publish Blueprint (Expect Cycle Warning)"
$totalSteps++

try {
    Write-Info "Publishing blueprint via POST /api/blueprints/$blueprintId/publish..."

    $publishRaw = Invoke-WebRequest `
        -Uri "$BlueprintServiceUrl/blueprints/$blueprintId/publish" `
        -Method POST `
        -Headers $headers `
        -UseBasicParsing

    $publishResponse = $publishRaw.Content | ConvertFrom-Json

    if ($publishRaw.StatusCode -eq 200) {
        Write-Success "Blueprint published successfully (200 OK)"

        # Check for cycle warnings
        if ($publishResponse.warnings -and ($publishResponse.warnings | Measure-Object).Count -gt 0) {
            Write-Info "Cycle warnings (expected for ping-pong):"
            foreach ($warning in $publishResponse.warnings) {
                Write-Host "    [warn] $warning" -ForegroundColor Yellow
            }
        } else {
            Write-Info "No warnings returned (cycle detection may not have fired)"
        }

        if ($ShowJson) {
            Write-Host ($publishResponse | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
        }
        $stepsPassed++
    } else {
        Write-Fail "Unexpected status code: $($publishRaw.StatusCode)"
        Write-Host ($publishResponse | ConvertTo-Json -Depth 5) -ForegroundColor Red
        exit 1
    }
} catch {
    $errorBody = ""
    if ($_.Exception.Response) {
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $errorReader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $errorReader.ReadToEnd()
        } catch { }
    }
    Write-Fail "Failed to publish blueprint"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($errorBody) { Write-Host "  Response: $errorBody" -ForegroundColor Red }
    exit 1
}

# ============================================================================
# Phase 4: Create Workflow Instance
# ============================================================================

Write-Step "Step 5: Create Workflow Instance"
$totalSteps++

try {
    Write-Info "Creating instance via POST /api/instances/..."

    $instanceBody = @{
        blueprintId = $blueprintId
        registerId = "ping-pong-demo-register"
        tenantId = "default"
        metadata = @{
            source = "walkthrough"
            createdBy = "test-ping-pong-workflow.ps1"
        }
    } | ConvertTo-Json -Depth 5

    $instanceResponse = Invoke-RestMethod `
        -Uri "$BlueprintServiceUrl/instances/" `
        -Method POST `
        -ContentType "application/json" `
        -Headers $headers `
        -Body $instanceBody `
        -UseBasicParsing

    $instanceId = $instanceResponse.id
    $currentActionIds = $instanceResponse.currentActionIds

    Write-Success "Instance created: $instanceId"
    Write-Info "Current action IDs: [$($currentActionIds -join ', ')]"
    Write-Info "State: $($instanceResponse.state)"

    if ($currentActionIds -contains 0) {
        Write-Success "Ping participant is prompted to submit first action (action 0)"
    } else {
        Write-Fail "Expected current action ID 0 but got: $($currentActionIds -join ', ')"
    }

    if ($ShowJson) {
        Write-Host ($instanceResponse | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
    }
    $stepsPassed++
} catch {
    Write-Fail "Failed to create instance"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Phase 5: Execute Ping-Pong Round-Trips
# ============================================================================

Write-Step "Step 6: Execute $RoundTrips Ping-Pong Round-Trips ($($RoundTrips * 2) actions total)"
$totalSteps++

$counter = 1
$allActionsSucceeded = $true
$actionResults = @()

for ($round = 1; $round -le $RoundTrips; $round++) {
    Write-Host ""
    Write-Host "--- Round $round of $RoundTrips ---" -ForegroundColor Cyan

    # Ping submits (action 0)
    $pingMessage = "Ping round $round"
    $pingActionBody = @{
        blueprintId = $blueprintId
        actionId = "0"
        instanceId = $instanceId
        senderWallet = "wallet-ping-001"
        registerAddress = "ping-pong-demo-register"
        payloadData = @{
            message = $pingMessage
            counter = $counter
        }
    } | ConvertTo-Json -Depth 5

    try {
        Write-Info "Ping submits action 0 (message='$pingMessage', counter=$counter)..."

        $pingResponse = Invoke-RestMethod `
            -Uri "$BlueprintServiceUrl/instances/$instanceId/actions/0/execute" `
            -Method POST `
            -ContentType "application/json" `
            -Headers ($headers + @{ "X-Delegation-Token" = $adminToken }) `
            -Body $pingActionBody `
            -UseBasicParsing

        $actionResults += @{
            Round = $round
            Actor = "Ping"
            Counter = $counter
            Message = $pingMessage
            Success = $true
        }

        Write-Success "Ping action $counter executed"

        if ($pingResponse.nextActions) {
            $nextIds = ($pingResponse.nextActions | ForEach-Object { $_.actionId }) -join ", "
            Write-Info "Next actions: [$nextIds]"
        }

        if ($ShowJson) {
            Write-Host ($pingResponse | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
        }
    } catch {
        Write-Fail "Ping action failed at counter $counter"
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        $allActionsSucceeded = $false
        $actionResults += @{
            Round = $round
            Actor = "Ping"
            Counter = $counter
            Message = $pingMessage
            Success = $false
        }
        # Continue to next round instead of aborting
    }
    $counter++

    # Pong submits (action 1)
    $pongMessage = "Pong round $round"
    $pongActionBody = @{
        blueprintId = $blueprintId
        actionId = "1"
        instanceId = $instanceId
        senderWallet = "wallet-pong-001"
        registerAddress = "ping-pong-demo-register"
        payloadData = @{
            message = $pongMessage
            counter = $counter
        }
    } | ConvertTo-Json -Depth 5

    try {
        Write-Info "Pong submits action 1 (message='$pongMessage', counter=$counter)..."

        $pongResponse = Invoke-RestMethod `
            -Uri "$BlueprintServiceUrl/instances/$instanceId/actions/1/execute" `
            -Method POST `
            -ContentType "application/json" `
            -Headers ($headers + @{ "X-Delegation-Token" = $adminToken }) `
            -Body $pongActionBody `
            -UseBasicParsing

        $actionResults += @{
            Round = $round
            Actor = "Pong"
            Counter = $counter
            Message = $pongMessage
            Success = $true
        }

        Write-Success "Pong action $counter executed"

        if ($pongResponse.nextActions) {
            $nextIds = ($pongResponse.nextActions | ForEach-Object { $_.actionId }) -join ", "
            Write-Info "Next actions: [$nextIds]"
        }

        if ($ShowJson) {
            Write-Host ($pongResponse | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
        }
    } catch {
        Write-Fail "Pong action failed at counter $counter"
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        $allActionsSucceeded = $false
        $actionResults += @{
            Round = $round
            Actor = "Pong"
            Counter = $counter
            Message = $pongMessage
            Success = $false
        }
    }
    $counter++
}

if ($allActionsSucceeded) {
    Write-Success "All $($RoundTrips * 2) actions executed successfully!"
    $stepsPassed++
} else {
    $failedCount = ($actionResults | Where-Object { -not $_.Success }).Count
    Write-Fail "$failedCount of $($RoundTrips * 2) actions failed"
}

# ============================================================================
# Phase 6: Verify Instance State
# ============================================================================

Write-Step "Step 7: Verify Instance State"
$totalSteps++

try {
    Write-Info "Fetching instance state via GET /api/instances/$instanceId..."

    $instanceState = Invoke-RestMethod `
        -Uri "$BlueprintServiceUrl/instances/$instanceId" `
        -Method GET `
        -Headers $headers `
        -UseBasicParsing

    Write-Info "Instance state: $($instanceState.state)"
    Write-Info "Current action IDs: [$($instanceState.currentActionIds -join ', ')]"

    if ($instanceState.state -eq "Active" -or $instanceState.state -eq 1) {
        Write-Success "Instance is still active (cyclic workflow continues)"
    }

    if ($ShowJson) {
        Write-Host ($instanceState | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
    }
    $stepsPassed++
} catch {
    Write-Fail "Failed to fetch instance state"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

# ============================================================================
# Summary
# ============================================================================

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# Action results table
Write-Host "  Action Results:" -ForegroundColor White
Write-Host "  -----------------------------------------------" -ForegroundColor Gray
Write-Host "  Round | Actor | Counter | Message       | OK?" -ForegroundColor Gray
Write-Host "  -----------------------------------------------" -ForegroundColor Gray
foreach ($r in $actionResults) {
    $status = if ($r.Success) { "Yes" } else { "NO" }
    $color = if ($r.Success) { "Green" } else { "Red" }
    $msg = $r.Message.PadRight(13).Substring(0, 13)
    Write-Host "  $($r.Round.ToString().PadLeft(5)) | $($r.Actor.PadRight(5)) | $($r.Counter.ToString().PadLeft(7)) | $msg | $status" -ForegroundColor $color
}
Write-Host "  -----------------------------------------------" -ForegroundColor Gray
Write-Host ""

# Step results
$statusColor = if ($stepsPassed -eq $totalSteps) { "Green" } else { "Red" }
Write-Host "  Steps: $stepsPassed/$totalSteps passed" -ForegroundColor $statusColor
Write-Host "  Actions: $(($actionResults | Where-Object { $_.Success }).Count)/$($actionResults.Count) succeeded" -ForegroundColor $statusColor
Write-Host ""

if ($stepsPassed -eq $totalSteps -and $allActionsSucceeded) {
    Write-Host "  RESULT: PASS - Ping-Pong pipeline verified!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  RESULT: FAIL - Some steps or actions failed" -ForegroundColor Red
    exit 1
}
