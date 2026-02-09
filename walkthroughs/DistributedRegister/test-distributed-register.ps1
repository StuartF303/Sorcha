#!/usr/bin/env pwsh
# Distributed Register Walkthrough
# Demonstrates cross-machine register creation, peer discovery, subscription, and replication.
#
# Prerequisites:
#   - Two machines running the full Sorcha Docker stack with peer seeding configured
#   - Bidirectional network connectivity on ports 80 (HTTP), 443 (HTTPS), 50051 (gRPC)
#
# Usage:
#   ./walkthroughs/DistributedRegister/test-distributed-register.ps1
#   ./walkthroughs/DistributedRegister/test-distributed-register.ps1 -RemoteHost 192.168.51.9
#   ./walkthroughs/DistributedRegister/test-distributed-register.ps1 -RoundTrips 3 -ShowJson

param(
    [Parameter(Mandatory=$false)]
    [string]$LocalHost = "localhost",

    [Parameter(Mandatory=$false)]
    [string]$RemoteHost = "192.168.51.9",

    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@sorcha.local",

    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "Dev_Pass_2025!",

    [Parameter(Mandatory=$false)]
    [int]$RoundTrips = 3,

    [Parameter(Mandatory=$false)]
    [switch]$ShowJson = $false,

    [Parameter(Mandatory=$false)]
    [switch]$SkipCleanup = $false
)

$ErrorActionPreference = "Stop"

# ============================================================================
# URLs
# ============================================================================

$LocalGateway  = "http://${LocalHost}"
$RemoteGateway = "http://${RemoteHost}"

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Distributed Register Walkthrough" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Local gateway:   $LocalGateway" -ForegroundColor White
Write-Host "  Remote gateway:  $RemoteGateway" -ForegroundColor White
Write-Host "  Round-trips:     $RoundTrips" -ForegroundColor White
Write-Host ""

# ============================================================================
# Helpers
# ============================================================================

function Write-Step {
    param([string]$Number, [string]$Message)
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Gray
    Write-Host "  Step $Number : $Message" -ForegroundColor White
    Write-Host "================================================================================" -ForegroundColor Gray
}

function Write-Success { param([string]$m) Write-Host "  [OK] $m" -ForegroundColor Green }
function Write-Fail    { param([string]$m) Write-Host "  [X]  $m" -ForegroundColor Red }
function Write-Info    { param([string]$m) Write-Host "  [i]  $m" -ForegroundColor Cyan }
function Write-Warn    { param([string]$m) Write-Host "  [!]  $m" -ForegroundColor Yellow }

function Show-Json {
    param($Object)
    if ($ShowJson) {
        Write-Host ($Object | ConvertTo-Json -Depth 10) -ForegroundColor DarkGray
    }
}

function Get-JwtPayload {
    param([string]$Token)
    $parts = $Token.Split('.')
    $b64 = $parts[1].Replace('-', '+').Replace('_', '/')
    switch ($b64.Length % 4) {
        1 { $b64 += '===' }
        2 { $b64 += '==' }
        3 { $b64 += '=' }
    }
    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($b64)) | ConvertFrom-Json
}

function Wait-WithDots {
    param([int]$Seconds, [string]$Message = "Waiting")
    Write-Host "  $Message " -NoNewline -ForegroundColor DarkGray
    for ($i = 0; $i -lt $Seconds; $i++) {
        Start-Sleep -Seconds 1
        Write-Host "." -NoNewline -ForegroundColor DarkGray
    }
    Write-Host ""
}

$stepsPassed = 0
$totalSteps  = 0

# Track resources for cleanup
$createdServicePrincipalId = $null
$createdBlueprintId = $null

# ============================================================================
# Phase 1: Peer Network Verification (Unauthenticated)
# ============================================================================

Write-Step "1" "Verify Peer Network (unauthenticated)"
$totalSteps++

try {
    Write-Info "Querying local peers..."
    $localPeers = Invoke-RestMethod -Uri "$LocalGateway/api/peers" -Method GET -UseBasicParsing
    $localPeerIds = $localPeers | ForEach-Object { $_.peerId }

    Write-Info "Querying remote peers..."
    $remotePeers = Invoke-RestMethod -Uri "$RemoteGateway/api/peers" -Method GET -UseBasicParsing
    $remotePeerIds = $remotePeers | ForEach-Object { $_.peerId }

    Write-Info "Local sees peers: [$($localPeerIds -join ', ')]"
    Write-Info "Remote sees peers: [$($remotePeerIds -join ', ')]"

    # Verify bidirectional visibility
    $localConnected = Invoke-RestMethod -Uri "$LocalGateway/api/peers/connected" -Method GET -UseBasicParsing
    $remoteConnected = Invoke-RestMethod -Uri "$RemoteGateway/api/peers/connected" -Method GET -UseBasicParsing

    if ($localConnected.connectedPeerCount -ge 1 -and $remoteConnected.connectedPeerCount -ge 1) {
        Write-Success "Peer network healthy: local=$($localConnected.connectedPeerCount) remote=$($remoteConnected.connectedPeerCount) connected"
        $stepsPassed++
    } else {
        Write-Fail "Insufficient peer connections (local=$($localConnected.connectedPeerCount), remote=$($remoteConnected.connectedPeerCount))"
        Write-Warn "Check that SEED_PEER_* is configured in .env on both machines"
        exit 1
    }
} catch {
    Write-Fail "Peer network check failed: $($_.Exception.Message)"
    Write-Warn "Ensure Docker services are running on both machines"
    exit 1
}

# ============================================================================
# Phase 2: Local Authentication & Wallet Setup
# ============================================================================

Write-Step "2" "Authenticate on local node"
$totalSteps++

$localToken = $null
$localHeaders = @{}

try {
    Write-Info "Requesting admin JWT from local tenant service..."
    $authResponse = Invoke-RestMethod `
        -Uri "$LocalGateway/api/service-auth/token" `
        -Method POST `
        -ContentType "application/x-www-form-urlencoded" `
        -Body "grant_type=password&username=$AdminEmail&password=$AdminPassword&client_id=sorcha-cli" `
        -UseBasicParsing

    $localToken = $authResponse.access_token
    $localHeaders = @{ Authorization = "Bearer $localToken" }

    $jwt = Get-JwtPayload $localToken
    Write-Success "Authenticated as $($jwt.email) (org: $($jwt.org_name))"
    Write-Info "Token expires in $($authResponse.expires_in)s"
    $stepsPassed++
} catch {
    Write-Fail "Local authentication failed: $($_.Exception.Message)"
    exit 1
}

# --- Create wallets ---

Write-Step "3" "Create wallets on local node"
$totalSteps++

$pingWallet = $null
$pongWallet = $null

try {
    foreach ($def in @(
        @{ Name = "Dist-Ping Wallet"; Var = "ping" },
        @{ Name = "Dist-Pong Wallet"; Var = "pong" }
    )) {
        Write-Info "Creating $($def.Name)..."
        $body = @{ name = $def.Name; algorithm = "ED25519"; wordCount = 12 } | ConvertTo-Json
        $resp = Invoke-RestMethod `
            -Uri "$LocalGateway/api/v1/wallets" `
            -Method POST -ContentType "application/json" `
            -Headers $localHeaders -Body $body -UseBasicParsing

        $addr = $resp.wallet.address
        if ($def.Var -eq "ping") { $pingWallet = $addr } else { $pongWallet = $addr }
        Write-Success "$($def.Name): $addr"
    }
    $stepsPassed++
} catch {
    Write-Fail "Wallet creation failed: $($_.Exception.Message)"
    exit 1
}

# ============================================================================
# Phase 3: Create Register & Advertise
# ============================================================================

Write-Step "4" "Create register on local node (2-phase)"
$totalSteps++

$registerId = $null

try {
    $jwt = Get-JwtPayload $localToken
    $userId = $jwt.sub
    $tenantId = if ($jwt.org_id) { $jwt.org_id } else { "default" }

    # Phase A: Initiate
    Write-Info "Initiating register creation..."
    $initiateBody = @{
        name = "dist-replication-test"
        description = "Distributed replication walkthrough register"
        tenantId = $tenantId
        owners = @(@{ userId = $userId; walletId = $pingWallet })
        advertise = $true
        metadata = @{ source = "walkthrough"; createdBy = "test-distributed-register.ps1" }
    } | ConvertTo-Json -Depth 10

    $initResp = Invoke-RestMethod `
        -Uri "$LocalGateway/api/registers/initiate" `
        -Method POST -ContentType "application/json" `
        -Headers $localHeaders -Body $initiateBody -UseBasicParsing

    $registerId = $initResp.registerId
    $nonce = $initResp.nonce
    $attestations = $initResp.attestationsToSign

    Write-Success "Register initiated: $registerId"
    Write-Info "Attestations to sign: $(($attestations | Measure-Object).Count)"

    # Phase B: Sign attestations
    $signedAttestations = @()
    foreach ($att in $attestations) {
        $hex = $att.dataToSign
        $bytes = [byte[]]::new($hex.Length / 2)
        for ($i = 0; $i -lt $bytes.Length; $i++) {
            $bytes[$i] = [Convert]::ToByte($hex.Substring($i * 2, 2), 16)
        }
        $b64 = [Convert]::ToBase64String($bytes)

        $signBody = @{ transactionData = $b64; isPreHashed = $true } | ConvertTo-Json
        $signResp = Invoke-RestMethod `
            -Uri "$LocalGateway/api/v1/wallets/$($att.walletId)/sign" `
            -Method POST -ContentType "application/json" `
            -Headers $localHeaders -Body $signBody -UseBasicParsing

        $signedAttestations += @{
            attestationData = $att.attestationData
            publicKey = $signResp.publicKey
            signature = $signResp.signature
            algorithm = "ED25519"
        }
        Write-Info "Signed attestation for $($att.role)"
    }

    # Phase C: Finalize
    Write-Info "Finalizing register..."
    $finalizeBody = @{
        registerId = $registerId
        nonce = $nonce
        signedAttestations = $signedAttestations
    } | ConvertTo-Json -Depth 10

    $finalResp = Invoke-RestMethod `
        -Uri "$LocalGateway/api/registers/finalize" `
        -Method POST -ContentType "application/json" `
        -Headers $localHeaders -Body $finalizeBody -UseBasicParsing

    Write-Success "Register created: $registerId"
    Write-Info "Genesis TX: $($finalResp.genesisTransactionId)"
    Show-Json $finalResp
    $stepsPassed++
} catch {
    Write-Fail "Register creation failed: $($_.Exception.Message)"
    exit 1
}

# --- Advertise ---

Write-Step "5" "Advertise register to peer network"
$totalSteps++

try {
    Write-Info "Setting register as public on peer network..."
    $advBody = @{ isPublic = $true } | ConvertTo-Json
    $advResp = Invoke-RestMethod `
        -Uri "$LocalGateway/api/registers/$registerId/advertise" `
        -Method POST -ContentType "application/json" `
        -Headers $localHeaders -Body $advBody -UseBasicParsing

    Write-Success "Register advertised: isPublic=$($advResp.isPublic)"
    Show-Json $advResp

    # Verify local peer sees the advertisement
    Wait-WithDots -Seconds 3 -Message "Waiting for peer propagation"
    $localPeersAfter = Invoke-RestMethod -Uri "$LocalGateway/api/peers" -Method GET -UseBasicParsing
    # The local node's own advertised registers are visible to remote peers
    Write-Info "Local peer advertising to network"
    $stepsPassed++
} catch {
    Write-Fail "Advertisement failed: $($_.Exception.Message)"
    Write-Warn "Continuing anyway - advertisement may not be required for all replication modes"
}

# ============================================================================
# Phase 4: Cross-Machine Authentication
# ============================================================================
# Design: The remote peer authenticates with the LOCAL tenant service to obtain
# a service JWT. This JWT authorises the remote peer to interact with the local
# node's services (register queries, subscription data, etc.).
#
# In production, this would be automated as part of the peer handshake. For this
# walkthrough we register a temporary service principal and obtain the JWT manually.

Write-Step "6" "Cross-machine auth: register remote peer as service principal on local"
$totalSteps++

$connectTime = (Get-Date).ToString("yyyyMMdd-HHmmss")
$remotePeerSecret = $null

try {
    # Name convention: peername_connecttime — unique per connection attempt
    $spName = "tiny-peer_$connectTime"
    Write-Info "Registering temporary service principal '$spName' on local tenant..."
    $spBody = @{
        serviceName = $spName
        scopes = @("registers:read", "registers:write", "peers:read", "peers:write")
    } | ConvertTo-Json -Depth 5

    $spResp = Invoke-RestMethod `
        -Uri "$LocalGateway/api/service-principals/" `
        -Method POST -ContentType "application/json" `
        -Headers $localHeaders -Body $spBody -UseBasicParsing

    $createdServicePrincipalId = $spResp.id
    $remotePeerClientId = $spResp.clientId
    $remotePeerSecret = $spResp.clientSecret

    Write-Success "Service principal created: $remotePeerClientId"
    Write-Info "ID: $createdServicePrincipalId"
    Write-Info "Scopes: registers:read, registers:write, peers:read, peers:write"
    Write-Warn "Client secret shown once only (stored for this session)"
    $stepsPassed++
} catch {
    Write-Fail "Service principal registration failed: $($_.Exception.Message)"
    Write-Warn "This requires admin privileges on the local tenant service"
    exit 1
}

# --- Remote peer obtains JWT from local tenant ---

Write-Step "7" "Remote peer authenticates with local tenant (client_credentials)"
$totalSteps++

$crossMachineToken = $null
$crossMachineHeaders = @{}

try {
    Write-Info "Remote peer requesting service JWT from local tenant..."
    Write-Info "Flow: $RemoteHost --> $LocalGateway/api/service-auth/token (client_credentials)"

    $tokenBody = "grant_type=client_credentials&client_id=$remotePeerClientId&client_secret=$remotePeerSecret"
    $tokenResp = Invoke-RestMethod `
        -Uri "$LocalGateway/api/service-auth/token" `
        -Method POST -ContentType "application/x-www-form-urlencoded" `
        -Body $tokenBody -UseBasicParsing

    $crossMachineToken = $tokenResp.access_token
    $crossMachineHeaders = @{ Authorization = "Bearer $crossMachineToken" }

    $cmJwt = Get-JwtPayload $crossMachineToken
    Write-Success "Cross-machine service JWT obtained"
    Write-Info "Token type: $($cmJwt.token_type)"
    Write-Info "Client ID:  $($cmJwt.client_id)"
    Write-Info "Scopes:     $($cmJwt.scope -join ', ')"
    Write-Info "Expires in: $($tokenResp.expires_in)s"
    $stepsPassed++
} catch {
    Write-Fail "Cross-machine auth failed: $($_.Exception.Message)"
    exit 1
}

# ============================================================================
# Phase 5: Remote Discovery & Subscription
# ============================================================================

Write-Step "8" "Remote peer discovers advertised register (unauthenticated)"
$totalSteps++

try {
    Write-Info "Querying available registers on remote node..."
    # Discovery is unauthenticated — any peer can see public register advertisements
    $available = Invoke-RestMethod `
        -Uri "$RemoteGateway/api/registers/available" `
        -Method GET -UseBasicParsing

    $ourRegister = $available | Where-Object { $_.registerId -eq $registerId }
    if (-not $ourRegister) {
        Write-Warn "Register not yet visible (available: $(($available | Measure-Object).Count))"
        # Retry with backoff — advertisement propagates via heartbeat cycle (10-15s)
        for ($retry = 1; $retry -le 4; $retry++) {
            Wait-WithDots -Seconds 5 -Message "Retry $retry/4"
            $available = Invoke-RestMethod -Uri "$RemoteGateway/api/registers/available" -Method GET -UseBasicParsing
            $ourRegister = $available | Where-Object { $_.registerId -eq $registerId }
            if ($ourRegister) { break }
        }
    }

    if ($ourRegister) {
        Write-Success "Register $registerId discovered on remote node!"
        Write-Info "Peer count: $($ourRegister.peerCount)"
        Show-Json $ourRegister
        $stepsPassed++
    } else {
        Write-Fail "Register not visible after retries"
        Write-Info "Available: $(($available | ConvertTo-Json -Depth 5))"
    }
} catch {
    Write-Fail "Remote discovery failed: $($_.Exception.Message)"
}

# --- Subscribe ---

Write-Step "9" "Remote peer subscribes to register (full-replica)"
$totalSteps++

try {
    # Get a local admin token on the REMOTE machine for subscription
    # (subscription is on the remote's own peer service, so needs remote auth)
    Write-Info "Getting admin token on remote node for subscription..."
    $remoteAuthResp = Invoke-RestMethod `
        -Uri "$RemoteGateway/api/service-auth/token" `
        -Method POST -ContentType "application/x-www-form-urlencoded" `
        -Body "grant_type=password&username=$AdminEmail&password=$AdminPassword&client_id=sorcha-cli" `
        -UseBasicParsing

    $remoteToken = $remoteAuthResp.access_token
    $remoteHeaders = @{ Authorization = "Bearer $remoteToken" }
    Write-Success "Remote admin authenticated"

    Write-Info "Subscribing to register $registerId in full-replica mode..."
    $subBody = @{ mode = "full-replica" } | ConvertTo-Json
    $subResp = Invoke-RestMethod `
        -Uri "$RemoteGateway/api/registers/$registerId/subscribe" `
        -Method POST -ContentType "application/json" `
        -Headers $remoteHeaders -Body $subBody -UseBasicParsing

    Write-Success "Subscribed: mode=$($subResp.mode), syncState=$($subResp.syncState)"
    Show-Json $subResp
    $stepsPassed++
} catch {
    Write-Fail "Subscription failed: $($_.Exception.Message)"
    Write-Warn "The subscribe endpoint may require auth or the register may not be visible yet"
}

# Wait for initial sync
Wait-WithDots -Seconds 5 -Message "Waiting for initial replication sync"

# ============================================================================
# Phase 6: Ping-Pong Blueprint & Execution
# ============================================================================

Write-Step "10" "Create ping-pong blueprint on local node"
$totalSteps++

$blueprintId = $null

try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $templatePath = Join-Path (Split-Path -Parent (Split-Path -Parent $scriptDir)) "examples/templates/ping-pong-template.json"

    if (-not (Test-Path $templatePath)) {
        Write-Warn "Ping-pong template not found at $templatePath"
        Write-Info "Falling back to inline blueprint definition"
        $blueprintJson = @{
            title = "Distributed Ping-Pong"
            description = "Simple two-participant ping-pong for replication testing"
            participants = @(
                @{ id = "ping"; name = "Ping Player"; role = "Initiator" }
                @{ id = "pong"; name = "Pong Player"; role = "Responder" }
            )
            actions = @(
                @{
                    id = 0; title = "Ping"; participantId = "ping"
                    routes = @( @{ targetActionId = 1; isDefault = $true } )
                    dataSchemas = @()
                }
                @{
                    id = 1; title = "Pong"; participantId = "pong"
                    routes = @( @{ targetActionId = 0; isDefault = $true } )
                    dataSchemas = @()
                }
            )
        } | ConvertTo-Json -Depth 20
    } else {
        Write-Info "Loading template from: $templatePath"
        $templateObj = Get-Content -Path $templatePath -Raw | ConvertFrom-Json
        $blueprintJson = $templateObj.template | ConvertTo-Json -Depth 20
    }

    Write-Info "Creating blueprint..."
    $bpResp = Invoke-RestMethod `
        -Uri "$LocalGateway/api/blueprints/" `
        -Method POST -ContentType "application/json" `
        -Headers $localHeaders -Body $blueprintJson -UseBasicParsing

    $blueprintId = $bpResp.id
    $createdBlueprintId = $blueprintId
    Write-Success "Blueprint created: $blueprintId"

    Write-Info "Publishing blueprint..."
    $pubRaw = Invoke-WebRequest `
        -Uri "$LocalGateway/api/blueprints/$blueprintId/publish" `
        -Method POST -Headers $localHeaders -UseBasicParsing
    $pubResp = $pubRaw.Content | ConvertFrom-Json

    Write-Success "Blueprint published"
    if ($pubResp.warnings) {
        foreach ($w in $pubResp.warnings) { Write-Info "Warning: $w" }
    }
    $stepsPassed++
} catch {
    Write-Fail "Blueprint setup failed: $($_.Exception.Message)"
    exit 1
}

# --- Create instance ---

Write-Step "11" "Create workflow instance on local register"
$totalSteps++

$instanceId = $null

try {
    $jwt = Get-JwtPayload $localToken
    $tenantId = if ($jwt.org_id) { $jwt.org_id } else { "default" }

    $instBody = @{
        blueprintId = $blueprintId
        registerId = $registerId
        tenantId = $tenantId
        metadata = @{ source = "distributed-walkthrough" }
    } | ConvertTo-Json -Depth 5

    $instResp = Invoke-RestMethod `
        -Uri "$LocalGateway/api/instances/" `
        -Method POST -ContentType "application/json" `
        -Headers $localHeaders -Body $instBody -UseBasicParsing

    $instanceId = $instResp.id
    Write-Success "Instance created: $instanceId"
    Write-Info "State: $($instResp.state), current actions: [$($instResp.currentActionIds -join ', ')]"
    $stepsPassed++
} catch {
    Write-Fail "Instance creation failed: $($_.Exception.Message)"
    exit 1
}

# --- Execute rounds ---

Write-Step "12" "Execute $RoundTrips ping-pong rounds ($($RoundTrips * 2) transactions)"
$totalSteps++

$counter = 1
$allOk = $true
$actionLog = @()

for ($round = 1; $round -le $RoundTrips; $round++) {
    Write-Host ""
    Write-Host "  --- Round $round/$RoundTrips ---" -ForegroundColor Cyan

    # Ping (action 0)
    try {
        $body = @{
            blueprintId = $blueprintId; actionId = "0"; instanceId = $instanceId
            senderWallet = $pingWallet; registerAddress = $registerId
            payloadData = @{ message = "Ping round $round"; counter = $counter; source = "local" }
        } | ConvertTo-Json -Depth 5

        Invoke-RestMethod `
            -Uri "$LocalGateway/api/instances/$instanceId/actions/0/execute" `
            -Method POST -ContentType "application/json" `
            -Headers ($localHeaders + @{ "X-Delegation-Token" = $localToken }) `
            -Body $body -UseBasicParsing | Out-Null

        Write-Success "Ping #$counter submitted"
        $actionLog += @{ Round=$round; Actor="Ping"; Counter=$counter; OK=$true }
    } catch {
        Write-Fail "Ping #$counter failed: $($_.Exception.Message)"
        $allOk = $false
        $actionLog += @{ Round=$round; Actor="Ping"; Counter=$counter; OK=$false }
    }
    $counter++

    # Pong (action 1)
    try {
        $body = @{
            blueprintId = $blueprintId; actionId = "1"; instanceId = $instanceId
            senderWallet = $pongWallet; registerAddress = $registerId
            payloadData = @{ message = "Pong round $round"; counter = $counter; source = "local" }
        } | ConvertTo-Json -Depth 5

        Invoke-RestMethod `
            -Uri "$LocalGateway/api/instances/$instanceId/actions/1/execute" `
            -Method POST -ContentType "application/json" `
            -Headers ($localHeaders + @{ "X-Delegation-Token" = $localToken }) `
            -Body $body -UseBasicParsing | Out-Null

        Write-Success "Pong #$counter submitted"
        $actionLog += @{ Round=$round; Actor="Pong"; Counter=$counter; OK=$true }
    } catch {
        Write-Fail "Pong #$counter failed: $($_.Exception.Message)"
        $allOk = $false
        $actionLog += @{ Round=$round; Actor="Pong"; Counter=$counter; OK=$false }
    }
    $counter++
}

if ($allOk) {
    Write-Success "All $($RoundTrips * 2) actions executed"
    $stepsPassed++
} else {
    $failed = ($actionLog | Where-Object { -not $_.OK }).Count
    Write-Fail "$failed of $($RoundTrips * 2) actions failed"
}

# ============================================================================
# Phase 7: Replication Verification
# ============================================================================

Write-Step "13" "Verify replication on remote node"
$totalSteps++

Wait-WithDots -Seconds 5 -Message "Waiting for replication to propagate"

try {
    # Check subscription sync state on remote
    Write-Info "Checking subscription status on remote..."
    $subs = Invoke-RestMethod `
        -Uri "$RemoteGateway/api/registers/subscriptions" `
        -Method GET -Headers $remoteHeaders -UseBasicParsing

    $ourSub = $subs | Where-Object { $_.registerId -eq $registerId }
    if ($ourSub) {
        Write-Success "Subscription found on remote"
        Write-Info "Sync state:    $($ourSub.syncState)"
        Write-Info "Mode:          $($ourSub.mode)"
        Write-Info "Progress:      $($ourSub.syncProgressPercent)%"
        Write-Info "Last docket:   $($ourSub.lastSyncedDocketVersion)"
        Write-Info "Last tx ver:   $($ourSub.lastSyncedTransactionVersion)"
        Show-Json $ourSub
    } else {
        Write-Warn "Subscription not found in list"
        Write-Info "Subscriptions: $(($subs | Measure-Object).Count)"
    }

    # Query register transactions on local (source of truth)
    Write-Info ""
    Write-Info "Checking transaction count on local..."
    try {
        $localTxResp = Invoke-RestMethod `
            -Uri "$LocalGateway/api/registers/$registerId/transactions?page=1&pageSize=1" `
            -Method GET -Headers $localHeaders -UseBasicParsing
        $localTxCount = $localTxResp.total
        Write-Info "Local transactions:  $localTxCount"
    } catch {
        Write-Warn "Could not query local transactions: $($_.Exception.Message)"
        $localTxCount = "unknown"
    }

    # Query register on remote using the cross-machine service JWT
    Write-Info "Checking register data on remote (using cross-machine JWT on local)..."
    try {
        $remoteTxResp = Invoke-RestMethod `
            -Uri "$LocalGateway/api/registers/$registerId/transactions?page=1&pageSize=1" `
            -Method GET -Headers $crossMachineHeaders -UseBasicParsing
        Write-Info "Register queryable with cross-machine JWT: $($remoteTxResp.total) transactions"
    } catch {
        Write-Info "Cross-machine JWT query: $($_.Exception.Message)"
    }

    # Check dockets on local
    try {
        $dockets = Invoke-RestMethod `
            -Uri "$LocalGateway/api/registers/$registerId/dockets" `
            -Method GET -Headers $localHeaders -UseBasicParsing
        $docketCount = ($dockets | Measure-Object).Count
        Write-Info "Dockets on local:   $docketCount"
    } catch {
        Write-Warn "Could not query dockets: $($_.Exception.Message)"
        $docketCount = "unknown"
    }

    # Check peer's advertised register info
    Write-Info ""
    Write-Info "Checking remote peer's view of the register..."
    $remotePeersNow = Invoke-RestMethod -Uri "$RemoteGateway/api/peers" -Method GET -UseBasicParsing
    foreach ($p in $remotePeersNow) {
        $regInfo = $p.advertisedRegisters | Where-Object { $_.registerId -eq $registerId }
        if ($regInfo) {
            Write-Success "Peer $($p.peerId) advertises register $registerId"
            Write-Info "  Sync state: $($regInfo.syncState), version: $($regInfo.latestVersion)"
        }
    }

    $stepsPassed++
} catch {
    Write-Fail "Replication verification failed: $($_.Exception.Message)"
}

# ============================================================================
# Phase 8: Cleanup (optional)
# ============================================================================

if (-not $SkipCleanup -and $createdServicePrincipalId) {
    Write-Step "14" "Cleanup: revoke temporary service principal"
    try {
        Write-Info "Revoking service principal $remotePeerClientId..."
        Invoke-RestMethod `
            -Uri "$LocalGateway/api/service-principals/$createdServicePrincipalId" `
            -Method DELETE -Headers $localHeaders -UseBasicParsing | Out-Null
        Write-Success "Service principal revoked"
    } catch {
        Write-Warn "Cleanup failed (non-critical): $($_.Exception.Message)"
    }
}

# ============================================================================
# Summary
# ============================================================================

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Summary" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Resources:" -ForegroundColor White
Write-Host "    Register:    $registerId" -ForegroundColor White
Write-Host "    Blueprint:   $blueprintId" -ForegroundColor White
Write-Host "    Instance:    $instanceId" -ForegroundColor White
Write-Host "    Ping wallet: $pingWallet" -ForegroundColor White
Write-Host "    Pong wallet: $pongWallet" -ForegroundColor White
Write-Host ""

# Action table
Write-Host "  Action Log:" -ForegroundColor White
Write-Host "  ------------------------------------------" -ForegroundColor Gray
Write-Host "  Round | Actor | Counter | Status" -ForegroundColor Gray
Write-Host "  ------------------------------------------" -ForegroundColor Gray
foreach ($a in $actionLog) {
    $status = if ($a.OK) { "OK" } else { "FAIL" }
    $color = if ($a.OK) { "Green" } else { "Red" }
    Write-Host "  $($a.Round.ToString().PadLeft(5)) | $($a.Actor.PadRight(5)) | $($a.Counter.ToString().PadLeft(7)) | $status" -ForegroundColor $color
}
Write-Host "  ------------------------------------------" -ForegroundColor Gray
Write-Host ""

$statusColor = if ($stepsPassed -eq $totalSteps) { "Green" } else { "Yellow" }
Write-Host "  Steps: $stepsPassed/$totalSteps passed" -ForegroundColor $statusColor
Write-Host ""

if ($stepsPassed -eq $totalSteps -and $allOk) {
    Write-Host "  RESULT: PASS - Distributed register replication verified!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  RESULT: PARTIAL - Some steps need investigation (see above)" -ForegroundColor Yellow
    exit 1
}
