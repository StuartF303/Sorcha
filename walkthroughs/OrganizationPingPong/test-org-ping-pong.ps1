#!/usr/bin/env pwsh
# Organization Ping-Pong Full-Stack Walkthrough
# Full pipeline: org bootstrap → participants → wallets → register → blueprint publish → 20 round-trip executions.
#
# Usage:
#   ./walkthroughs/OrganizationPingPong/test-org-ping-pong.ps1
#   ./walkthroughs/OrganizationPingPong/test-org-ping-pong.ps1 -Profile aspire
#   ./walkthroughs/OrganizationPingPong/test-org-ping-pong.ps1 -RoundTrips 5 -ShowJson

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('gateway', 'direct', 'aspire')]
    [string]$Profile = 'gateway',

    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "designer@pingpong.local",

    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "PingPong_2025!",

    [Parameter(Mandatory=$false)]
    [string]$AdminName = "Blueprint Designer",

    [Parameter(Mandatory=$false)]
    [string]$OrgName = "Ping-Pong Demo Corp",

    [Parameter(Mandatory=$false)]
    [string]$OrgSubdomain = "pingpong-demo",

    [Parameter(Mandatory=$false)]
    [int]$RoundTrips = 20,

    [Parameter(Mandatory=$false)]
    [switch]$ShowJson = $false,

    [Parameter(Mandatory=$false)]
    [switch]$SkipCleanup = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Organization Ping-Pong Full-Stack Walkthrough" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# URL Configuration
# ============================================================================

$GatewayUrl = ""
$TenantUrl = ""
$BlueprintUrl = ""
$RegisterUrl = ""
$WalletUrl = ""

switch ($Profile) {
    'gateway' {
        $GatewayUrl    = "http://localhost"
        $TenantUrl     = "$GatewayUrl/api"
        $BlueprintUrl  = "$GatewayUrl/api"
        $RegisterUrl   = "$GatewayUrl/api"
        $WalletUrl     = "$GatewayUrl/api"
        Write-Host "Profile: gateway (API Gateway on port 80)" -ForegroundColor Green
    }
    'direct' {
        $GatewayUrl    = "http://localhost"
        $TenantUrl     = "http://localhost:5450/api"
        $BlueprintUrl  = "http://localhost:5000/api"
        $RegisterUrl   = "http://localhost:5380/api"
        # Wallet Service has no external Docker port — must route through gateway
        $WalletUrl     = "$GatewayUrl/api"
        Write-Host "Profile: direct (services on native ports, wallet via gateway)" -ForegroundColor Yellow
    }
    'aspire' {
        $GatewayUrl    = "https://localhost:7082"
        $TenantUrl     = "https://localhost:7110/api"
        $BlueprintUrl  = "https://localhost:7000/api"
        $RegisterUrl   = "https://localhost:7290/api"
        $WalletUrl     = "$GatewayUrl/api"
        Write-Host "Profile: aspire (Aspire HTTPS ports)" -ForegroundColor Magenta
    }
}

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Tenant Service:    $TenantUrl" -ForegroundColor White
Write-Host "  Blueprint Service: $BlueprintUrl" -ForegroundColor White
Write-Host "  Register Service:  $RegisterUrl" -ForegroundColor White
Write-Host "  Wallet Service:    $WalletUrl (via gateway)" -ForegroundColor White
Write-Host "  Organization:      $OrgName ($OrgSubdomain)" -ForegroundColor White
Write-Host "  Round-trips:       $RoundTrips" -ForegroundColor White
Write-Host ""

# ============================================================================
# Helper Functions
# ============================================================================

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

function Write-Warning {
    param([string]$Message)
    Write-Host "[!] $Message" -ForegroundColor Yellow
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [string]$ContentType = "application/json",
        [switch]$RawResponse
    )

    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $Headers
        UseBasicParsing = $true
    }

    if ($Body) {
        $jsonBody = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 20 }
        $params.Body = $jsonBody
        $params.ContentType = $ContentType
    }

    if ($ShowJson -and $Body) {
        Write-Host "  Request body:" -ForegroundColor DarkGray
        $displayBody = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 10 }
        Write-Host "  $displayBody" -ForegroundColor DarkGray
    }

    if ($RawResponse) {
        $response = Invoke-WebRequest @params
        if ($ShowJson) {
            Write-Host "  Response ($($response.StatusCode)):" -ForegroundColor DarkGray
            Write-Host "  $($response.Content)" -ForegroundColor DarkGray
        }
        return $response
    } else {
        $response = Invoke-RestMethod @params
        if ($ShowJson) {
            Write-Host "  Response:" -ForegroundColor DarkGray
            Write-Host "  $($response | ConvertTo-Json -Depth 10)" -ForegroundColor DarkGray
        }
        return $response
    }
}

function Decode-Jwt {
    param([string]$Token)
    $parts = $Token.Split('.')
    $base64 = $parts[1].Replace('-', '+').Replace('_', '/')
    switch ($base64.Length % 4) {
        1 { $base64 += '===' }
        2 { $base64 += '==' }
        3 { $base64 += '=' }
    }
    $payloadBytes = [System.Convert]::FromBase64String($base64)
    $payload = [System.Text.Encoding]::UTF8.GetString($payloadBytes)
    return ($payload | ConvertFrom-Json)
}

# Tracking
$stepsPassed = 0
$totalSteps = 0
$walkthroughStart = Get-Date

# ============================================================================
# Phase 0: Prerequisites & Health Check
# ============================================================================

Write-Step "Phase 0: Prerequisites & Health Check"
$totalSteps++

try {
    Write-Info "Checking Docker availability..."
    $dockerInfo = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker is not running"
    }
    Write-Success "Docker is running"

    Write-Info "Checking API Gateway health..."
    try {
        $healthResponse = Invoke-RestMethod -Uri "$GatewayUrl/api/health" -Method GET -TimeoutSec 10 -UseBasicParsing
        Write-Success "API Gateway is healthy"
    } catch {
        Write-Warning "API Gateway health check returned error (services may still be starting)"
        Write-Info "Proceeding anyway — individual service calls will fail if services are down"
    }

    $stepsPassed++
} catch {
    Write-Fail "Prerequisites check failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Make sure Docker services are running:" -ForegroundColor Yellow
    Write-Host "    docker-compose up -d" -ForegroundColor White
    Write-Host "    # Wait ~30 seconds for services to become healthy" -ForegroundColor White
    exit 1
}

# ============================================================================
# Phase 1: Bootstrap Organization
# ============================================================================

Write-Step "Phase 1: Bootstrap Organization"
$totalSteps++

$adminToken = ""
$organizationId = ""
$adminUserId = ""

try {
    Write-Info "Bootstrapping organization '$OrgName'..."

    $bootstrapBody = @{
        organizationName = $OrgName
        organizationSubdomain = $OrgSubdomain
        organizationDescription = "Demo organization for ping-pong walkthrough"
        adminEmail = $AdminEmail
        adminName = $AdminName
        adminPassword = $AdminPassword
        createServicePrincipal = $false
    }

    try {
        $bootstrapResponse = Invoke-Api -Method POST -Uri "$TenantUrl/tenants/bootstrap" -Body $bootstrapBody

        $adminToken = $bootstrapResponse.adminAccessToken
        $organizationId = $bootstrapResponse.organizationId
        $adminUserId = $bootstrapResponse.adminUserId

        Write-Success "Organization bootstrapped successfully"
        Write-Info "Organization ID: $organizationId"
        Write-Info "Admin User ID:   $adminUserId"
    } catch {
        # Check for 409 Conflict (already bootstrapped) — fall back to login
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 409) {
            Write-Warning "Organization already exists (409 Conflict) — falling back to login"

            $loginBody = "grant_type=password" + "&username=$AdminEmail" + "&password=$AdminPassword" + "&client_id=sorcha-cli"
            $loginResponse = Invoke-RestMethod `
                -Uri "$TenantUrl/service-auth/token" `
                -Method POST `
                -ContentType "application/x-www-form-urlencoded" `
                -Body $loginBody `
                -UseBasicParsing

            $adminToken = $loginResponse.access_token
            Write-Success "Logged in with existing credentials"
        } else {
            throw
        }
    }

    # Decode JWT to show claims
    if ($adminToken) {
        $jwtPayload = Decode-Jwt -Token $adminToken
        Write-Info "JWT Claims:"
        Write-Host "    Issuer:  $($jwtPayload.iss)" -ForegroundColor White
        Write-Host "    Subject: $($jwtPayload.sub)" -ForegroundColor White
        if ($jwtPayload.role) {
            Write-Host "    Roles:   $($jwtPayload.role -join ', ')" -ForegroundColor White
        }
        if ($jwtPayload.org_id -and -not $organizationId) {
            $organizationId = $jwtPayload.org_id
            Write-Info "Organization ID (from JWT): $organizationId"
        }
    }

    $stepsPassed++
} catch {
    Write-Fail "Bootstrap failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Ensure Tenant Service is running and accessible" -ForegroundColor Yellow
    exit 1
}

$headers = @{ Authorization = "Bearer $adminToken" }

# ============================================================================
# Phase 2: Create Participant Users
# ============================================================================

Write-Step "Phase 2: Create Participant Users"
$totalSteps++

try {
    $participants = @(
        @{
            email = "participant-alpha@pingpong.local"
            displayName = "Participant Alpha"
            externalIdpUserId = "alpha-" + [guid]::NewGuid().ToString().Substring(0, 8)
        },
        @{
            email = "participant-beta@pingpong.local"
            displayName = "Participant Beta"
            externalIdpUserId = "beta-" + [guid]::NewGuid().ToString().Substring(0, 8)
        }
    )

    foreach ($p in $participants) {
        Write-Info "Creating user: $($p.displayName) ($($p.email))..."

        try {
            $userBody = @{
                email = $p.email
                displayName = $p.displayName
                externalIdpUserId = $p.externalIdpUserId
                roles = @("Member")
            }

            $userResponse = Invoke-Api -Method POST `
                -Uri "$TenantUrl/organizations/$organizationId/users" `
                -Body $userBody `
                -Headers $headers

            Write-Success "$($p.displayName) created (ID: $($userResponse.id))"
        } catch {
            $statusCode = $null
            try { $statusCode = $_.Exception.Response.StatusCode.value__ } catch {}
            if ($statusCode -eq 409 -or $statusCode -eq 400) {
                Write-Warning "$($p.displayName) may already exist — continuing"
            } else {
                Write-Warning "Failed to create $($p.displayName): $($_.Exception.Message)"
                Write-Info "Continuing — participant users are informational only"
            }
        }
    }

    Write-Success "Participant user creation complete"
    Write-Info "Note: Admin token is used for all subsequent API calls (see README for details)"
    $stepsPassed++
} catch {
    Write-Fail "Participant creation failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Continuing anyway — participants are informational" -ForegroundColor Yellow
    $stepsPassed++  # Non-critical phase
}

# ============================================================================
# Phase 3: Create Wallets (3 wallets via API Gateway)
# ============================================================================

Write-Step "Phase 3: Create Wallets (3 ED25519 wallets)"
$totalSteps++

$designerWalletAddress = ""
$alphaWalletAddress = ""
$betaWalletAddress = ""

try {
    $walletDefs = @(
        @{ name = "Designer Wallet"; varName = "designer" },
        @{ name = "Alpha Wallet"; varName = "alpha" },
        @{ name = "Beta Wallet"; varName = "beta" }
    )

    foreach ($w in $walletDefs) {
        Write-Info "Creating wallet: $($w.name)..."

        $walletBody = @{
            name = $w.name
            algorithm = "ED25519"
            wordCount = 12
        }

        $walletResponse = Invoke-Api -Method POST `
            -Uri "$WalletUrl/v1/wallets" `
            -Body $walletBody `
            -Headers $headers

        $address = $walletResponse.wallet.address
        $mnemonic = ($walletResponse.mnemonicWords -join " ")

        switch ($w.varName) {
            "designer" { $designerWalletAddress = $address }
            "alpha"    { $alphaWalletAddress = $address }
            "beta"     { $betaWalletAddress = $address }
        }

        Write-Success "$($w.name) created: $address"
        Write-Warning "Mnemonic (BACKUP!): $mnemonic"
    }

    Write-Host ""
    Write-Info "Wallet Summary:"
    Write-Host "    Designer: $designerWalletAddress" -ForegroundColor White
    Write-Host "    Alpha:    $alphaWalletAddress" -ForegroundColor White
    Write-Host "    Beta:     $betaWalletAddress" -ForegroundColor White

    $stepsPassed++
} catch {
    Write-Fail "Wallet creation failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Check that Wallet Service is accessible via API Gateway" -ForegroundColor Yellow
    Write-Host "  Wallet Service has no direct Docker port — always use gateway" -ForegroundColor Yellow
    exit 1
}

# ============================================================================
# Phase 3b: Register Participant Profile & Link Wallets (SEC-006 compliance)
# ============================================================================

Write-Step "Phase 3b: Register Participant Profile & Link Wallets"
$totalSteps++

$participantId = ""

try {
    # Self-register admin as a participant in this organization
    Write-Info "Self-registering admin as participant..."

    try {
        $selfRegResponse = Invoke-Api -Method POST `
            -Uri "$TenantUrl/me/organizations/$organizationId/self-register?displayName=$([Uri]::EscapeDataString($AdminName))" `
            -Headers $headers

        $participantId = $selfRegResponse.id
        Write-Success "Participant registered: $participantId"
    } catch {
        $statusCode = $null
        try { $statusCode = $_.Exception.Response.StatusCode.value__ } catch {}
        if ($statusCode -eq 409) {
            Write-Warning "Participant already exists (409 Conflict) — fetching existing profile"

            $profiles = Invoke-Api -Method GET `
                -Uri "$TenantUrl/me/participant-profiles" `
                -Headers $headers

            # Find the profile matching this organization
            $orgProfile = $profiles | Where-Object { $_.organizationId -eq $organizationId } | Select-Object -First 1
            if ($orgProfile) {
                $participantId = $orgProfile.id
                Write-Success "Found existing participant: $participantId"
            } else {
                throw "No participant profile found for organization $organizationId"
            }
        } else {
            throw
        }
    }

    # Link Alpha and Beta wallets to this participant via challenge-sign-verify
    $walletsToLink = @(
        @{ name = "Alpha"; address = $alphaWalletAddress },
        @{ name = "Beta"; address = $betaWalletAddress }
    )

    foreach ($wl in $walletsToLink) {
        Write-Info "Linking $($wl.name) wallet ($($wl.address))..."

        try {
            # Step 1: Initiate wallet link challenge
            $challengeBody = @{
                walletAddress = $wl.address
                algorithm = "ED25519"
            }

            $challengeResponse = Invoke-Api -Method POST `
                -Uri "$TenantUrl/organizations/$organizationId/participants/$participantId/wallet-links" `
                -Body $challengeBody `
                -Headers $headers

            $challengeId = $challengeResponse.challengeId
            $challengeMessage = $challengeResponse.challenge
            Write-Info "  Challenge received (ID: $challengeId)"

            # Step 2: Sign the challenge message with the wallet
            $challengeBytes = [System.Text.Encoding]::UTF8.GetBytes($challengeMessage)
            $challengeBase64 = [Convert]::ToBase64String($challengeBytes)

            $signBody = @{
                transactionData = $challengeBase64
                isPreHashed = $false
            }

            $signResponse = Invoke-Api -Method POST `
                -Uri "$WalletUrl/v1/wallets/$($wl.address)/sign" `
                -Body $signBody `
                -Headers $headers

            Write-Info "  Challenge signed by $($signResponse.signedBy)"

            # Step 3: Verify the wallet link with the signed challenge
            $verifyBody = @{
                signature = $signResponse.signature
                publicKey = $signResponse.publicKey
            }

            $verifyResponse = Invoke-Api -Method POST `
                -Uri "$TenantUrl/organizations/$organizationId/participants/$participantId/wallet-links/$challengeId/verify" `
                -Body $verifyBody `
                -Headers $headers

            Write-Success "$($wl.name) wallet linked (status: $($verifyResponse.status))"
        } catch {
            # Wallet may already be linked from a previous run — not fatal
            $errMsg = $_.Exception.Message
            if ($errMsg -match "already linked") {
                Write-Warning "$($wl.name) wallet already linked — continuing"
            } else {
                Write-Warning "$($wl.name) wallet link failed: $errMsg — continuing"
            }
        }
    }

    Write-Info "Admin participant has both wallets linked for action execution"
    $stepsPassed++
} catch {
    Write-Fail "Participant registration or wallet linking failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    try {
        if ($_.Exception.Response) {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $errorReader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $errorReader.ReadToEnd()
            Write-Host "  Response: $errorBody" -ForegroundColor Red
        }
    } catch {}
    exit 1
}

# ============================================================================
# Phase 4: Create Register (2-phase flow)
# ============================================================================

Write-Step "Phase 4: Create Register - 2-phase initiate and finalize"
$totalSteps++

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$registerId = ""

try {
    # Phase 4a: Initiate
    Write-Info "Initiating register creation..."

    $initiateBody = @{
        name = "Ping-Pong Register"
        description = "Register for the ping-pong walkthrough"
        tenantId = if ($organizationId) { $organizationId } else { "default" }
        owners = @(
            @{
                userId = if ($adminUserId) { $adminUserId } else { "designer" }
                walletId = $designerWalletAddress
            }
        )
        metadata = @{
            source = "walkthrough"
            createdBy = "test-org-ping-pong.ps1"
        }
    }

    $initiateResponse = Invoke-Api -Method POST `
        -Uri "$RegisterUrl/registers/initiate" `
        -Body $initiateBody `
        -Headers $headers

    $registerId = $initiateResponse.registerId
    $nonce = $initiateResponse.nonce
    $attestations = $initiateResponse.attestationsToSign

    Write-Success "Register initiation received"
    Write-Info "Register ID: $registerId"
    Write-Info "Nonce: $nonce"
    Write-Info "Attestations to sign: $(($attestations | Measure-Object).Count)"

    # Phase 4b: Sign attestations
    $signedAttestations = @()

    foreach ($att in $attestations) {
        $dataToSignHex = $att.dataToSign
        Write-Info "Signing attestation for $($att.role) with wallet $($att.walletId)..."

        # dataToSign is hex-encoded SHA-256 hash — convert to bytes then base64 for wallet sign API
        $hashBytes = [byte[]]::new($dataToSignHex.Length / 2)
        for ($i = 0; $i -lt $hashBytes.Length; $i++) {
            $hashBytes[$i] = [Convert]::ToByte($dataToSignHex.Substring($i * 2, 2), 16)
        }
        $dataToSignBase64 = [Convert]::ToBase64String($hashBytes)

        $signBody = @{
            transactionData = $dataToSignBase64
            isPreHashed = $true
        }

        $signResponse = Invoke-Api -Method POST `
            -Uri "$WalletUrl/v1/wallets/$($att.walletId)/sign" `
            -Body $signBody `
            -Headers $headers

        $signedAttestations += @{
            attestationData = $att.attestationData
            publicKey = $signResponse.publicKey
            signature = $signResponse.signature
            algorithm = "ED25519"
        }

        Write-Success "Attestation signed by $($signResponse.signedBy)"
    }

    # Phase 4c: Finalize
    Write-Info "Finalizing register creation..."

    $finalizeBody = @{
        registerId = $registerId
        nonce = $nonce
        signedAttestations = $signedAttestations
    }

    $finalizeResponse = Invoke-Api -Method POST `
        -Uri "$RegisterUrl/registers/finalize" `
        -Body $finalizeBody `
        -Headers $headers

    Write-Success "Register created and finalized"
    Write-Info "Register ID: $registerId"
    Write-Info "Status: $($finalizeResponse.status)"
    if ($finalizeResponse.genesisTransactionId) {
        Write-Info "Genesis TX: $($finalizeResponse.genesisTransactionId)"
    }

    $stepsPassed++
} catch {
    Write-Fail "Register creation failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red

    # Try to extract response body for more detail
    try {
        if ($_.Exception.Response) {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $errorReader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $errorReader.ReadToEnd()
            Write-Host "  Response: $errorBody" -ForegroundColor Red
        }
    } catch {}

    Write-Host "  Check Register Service logs: docker-compose logs register-service" -ForegroundColor Yellow
    exit 1
}

# ============================================================================
# Phase 5: Load & Create Blueprint
# ============================================================================

Write-Step "Phase 5: Load & Create Blueprint from Template"
$totalSteps++

$blueprintId = ""

try {
    # Load ping-pong template
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $templatePath = Join-Path (Split-Path -Parent (Split-Path -Parent $scriptDir)) "examples/templates/ping-pong-template.json"
    Write-Info "Loading template from: $templatePath"

    $templateJson = Get-Content -Path $templatePath -Raw | ConvertFrom-Json

    # Extract the blueprint from the template wrapper
    $blueprint = $templateJson.template

    # Patch participant wallet addresses into the blueprint
    Write-Info "Patching wallet addresses into blueprint participants..."
    foreach ($participant in $blueprint.participants) {
        switch ($participant.id) {
            "ping" {
                $participant | Add-Member -NotePropertyName "walletAddress" -NotePropertyValue $alphaWalletAddress -Force
                Write-Info "  ping -> $alphaWalletAddress (Alpha)"
            }
            "pong" {
                $participant | Add-Member -NotePropertyName "walletAddress" -NotePropertyValue $betaWalletAddress -Force
                Write-Info "  pong -> $betaWalletAddress (Beta)"
            }
        }
    }

    # Generate a unique ID so re-runs don't collide
    $blueprint.id = "ping-pong-org-$timestamp"

    $blueprintJson = $blueprint | ConvertTo-Json -Depth 20

    Write-Info "Creating blueprint via POST /api/blueprints/..."
    $createResponse = Invoke-Api -Method POST `
        -Uri "$BlueprintUrl/blueprints/" `
        -Body $blueprintJson `
        -Headers $headers

    $blueprintId = $createResponse.id
    Write-Success "Blueprint created: $blueprintId"
    Write-Info "Title: $($createResponse.title)"
    Write-Info "Participants: $(($createResponse.participants | Measure-Object).Count)"
    Write-Info "Actions: $(($createResponse.actions | Measure-Object).Count)"

    $stepsPassed++
} catch {
    Write-Fail "Blueprint creation failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Phase 6: Publish Blueprint
# ============================================================================

Write-Step "Phase 6: Publish Blueprint"
$totalSteps++

try {
    Write-Info "Publishing blueprint via POST /api/blueprints/$blueprintId/publish..."

    $publishRaw = Invoke-WebRequest `
        -Uri "$BlueprintUrl/blueprints/$blueprintId/publish" `
        -Method POST `
        -Headers $headers `
        -UseBasicParsing

    $publishResponse = $publishRaw.Content | ConvertFrom-Json

    if ($publishRaw.StatusCode -eq 200) {
        Write-Success "Blueprint published successfully (200 OK)"

        # Display cycle warnings (expected for ping-pong)
        if ($publishResponse.warnings -and ($publishResponse.warnings | Measure-Object).Count -gt 0) {
            Write-Info "Cycle warnings (expected for ping-pong):"
            foreach ($warning in $publishResponse.warnings) {
                Write-Host "    [warn] $warning" -ForegroundColor Yellow
            }
        }

        if ($ShowJson) {
            Write-Host ($publishResponse | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
        }
        $stepsPassed++
    } else {
        Write-Fail "Unexpected status code: $($publishRaw.StatusCode)"
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
# Phase 7: Create Workflow Instance
# ============================================================================

Write-Step "Phase 7: Create Workflow Instance"
$totalSteps++

$instanceId = ""

try {
    Write-Info "Creating instance via POST /api/instances/..."

    $instanceBody = @{
        blueprintId = $blueprintId
        registerId = $registerId
        tenantId = if ($organizationId) { $organizationId } else { "default" }
        metadata = @{
            source = "walkthrough"
            createdBy = "test-org-ping-pong.ps1"
            organizationName = $OrgName
        }
    }

    $instanceResponse = Invoke-Api -Method POST `
        -Uri "$BlueprintUrl/instances/" `
        -Body $instanceBody `
        -Headers $headers

    $instanceId = $instanceResponse.id
    $currentActionIds = $instanceResponse.currentActionIds

    Write-Success "Instance created: $instanceId"
    Write-Info "Current action IDs: [$($currentActionIds -join ', ')]"
    Write-Info "State: $($instanceResponse.state)"

    if ($currentActionIds -contains 0) {
        Write-Success "Ping participant (Alpha) is prompted to submit first action"
    }

    $stepsPassed++
} catch {
    Write-Fail "Failed to create instance"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Phase 8: Execute Ping-Pong Round-Trips
# ============================================================================

Write-Step "Phase 8: Execute $RoundTrips Ping-Pong Round-Trips ($($RoundTrips * 2) actions total)"
$totalSteps++

$counter = 1
$allActionsSucceeded = $true
$actionResults = @()
$executeHeaders = @{
    Authorization = "Bearer $adminToken"
    "X-Delegation-Token" = $adminToken
}
$phaseStart = Get-Date

for ($round = 1; $round -le $RoundTrips; $round++) {

    # --- Ping (Alpha submits action 0) ---
    $pingMessage = "Ping #$round"
    $pingBody = @{
        blueprintId = $blueprintId
        actionId = "0"
        instanceId = $instanceId
        senderWallet = $alphaWalletAddress
        registerAddress = $registerId
        payloadData = @{
            message = $pingMessage
            counter = $counter
        }
    }

    try {
        $pingResponse = Invoke-Api -Method POST `
            -Uri "$BlueprintUrl/instances/$instanceId/actions/0/execute" `
            -Body $pingBody `
            -Headers $executeHeaders

        $actionResults += @{ Round = $round; Actor = "Ping"; Counter = $counter; Message = $pingMessage; Success = $true }
        $pingOk = $true
    } catch {
        Write-Fail "Ping failed at round $round (counter $counter): $($_.Exception.Message)"
        $allActionsSucceeded = $false
        $actionResults += @{ Round = $round; Actor = "Ping"; Counter = $counter; Message = $pingMessage; Success = $false }
        $pingOk = $false
    }
    $counter++

    # --- Pong (Beta submits action 1) ---
    $pongMessage = "Pong #$round"
    $pongBody = @{
        blueprintId = $blueprintId
        actionId = "1"
        instanceId = $instanceId
        senderWallet = $betaWalletAddress
        registerAddress = $registerId
        payloadData = @{
            message = $pongMessage
            counter = $counter
        }
    }

    try {
        $pongResponse = Invoke-Api -Method POST `
            -Uri "$BlueprintUrl/instances/$instanceId/actions/1/execute" `
            -Body $pongBody `
            -Headers $executeHeaders

        $actionResults += @{ Round = $round; Actor = "Pong"; Counter = $counter; Message = $pongMessage; Success = $true }
        $pongOk = $true
    } catch {
        Write-Fail "Pong failed at round $round (counter $counter): $($_.Exception.Message)"
        $allActionsSucceeded = $false
        $actionResults += @{ Round = $round; Actor = "Pong"; Counter = $counter; Message = $pongMessage; Success = $false }
        $pongOk = $false
    }
    $counter++

    # Progress line
    $pingStatus = if ($pingOk) { "OK" } else { "FAIL" }
    $pongStatus = if ($pongOk) { "OK" } else { "FAIL" }
    $pingColor = if ($pingOk) { "Green" } else { "Red" }
    $pongColor = if ($pongOk) { "Green" } else { "Red" }

    Write-Host "  [Round $($round.ToString().PadLeft(2))/$RoundTrips] " -NoNewline -ForegroundColor White
    Write-Host "Ping $pingStatus" -NoNewline -ForegroundColor $pingColor
    Write-Host " -> " -NoNewline -ForegroundColor Gray
    Write-Host "Pong $pongStatus" -ForegroundColor $pongColor
}

$phaseEnd = Get-Date
$phaseDuration = $phaseEnd - $phaseStart

Write-Host ""
if ($allActionsSucceeded) {
    Write-Success "All $($RoundTrips * 2) actions executed successfully in $([math]::Round($phaseDuration.TotalSeconds, 1))s"
    $stepsPassed++
} else {
    $failedCount = ($actionResults | Where-Object { -not $_.Success }).Count
    Write-Fail "$failedCount of $($RoundTrips * 2) actions failed"
}

# ============================================================================
# Phase 9: Verify & Summary
# ============================================================================

Write-Step "Phase 9: Verify Instance State & Summary"
$totalSteps++

try {
    Write-Info "Fetching instance state via GET /api/instances/$instanceId..."

    $instanceState = Invoke-Api -Method GET `
        -Uri "$BlueprintUrl/instances/$instanceId" `
        -Headers $headers

    Write-Info "Instance state: $($instanceState.state)"
    Write-Info "Current action IDs: [$($instanceState.currentActionIds -join ', ')]"

    if ($instanceState.state -eq "Active" -or $instanceState.state -eq 1) {
        Write-Success "Instance is still active (cyclic workflow continues as expected)"
    }

    $stepsPassed++
} catch {
    Write-Fail "Failed to verify instance state"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
}

# ============================================================================
# Final Summary
# ============================================================================

$walkthroughEnd = Get-Date
$totalDuration = $walkthroughEnd - $walkthroughStart

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Organization Ping-Pong Walkthrough Results" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

$succeededActions = ($actionResults | Where-Object { $_.Success }).Count
$totalActions = $actionResults.Count

Write-Host "  Organization:  $OrgName" -ForegroundColor White
if ($organizationId) {
    Write-Host "                 ($organizationId)" -ForegroundColor Gray
}
Write-Host "  Participants:  3 (1 designer + 2 basic)" -ForegroundColor White
Write-Host "  Wallets:       3 (ED25519)" -ForegroundColor White
Write-Host "    Designer:    $designerWalletAddress" -ForegroundColor Gray
Write-Host "    Alpha:       $alphaWalletAddress" -ForegroundColor Gray
Write-Host "    Beta:        $betaWalletAddress" -ForegroundColor Gray
Write-Host "  Register:      $registerId" -ForegroundColor White
Write-Host "  Blueprint:     $blueprintId (published)" -ForegroundColor White
if ($instanceId) {
    Write-Host "  Instance:      $instanceId" -ForegroundColor White
}
Write-Host "  Round-trips:   $([math]::Floor($succeededActions / 2))/$RoundTrips completed" -ForegroundColor White
Write-Host "  Total actions: $succeededActions/$totalActions ($([math]::Floor($succeededActions / 2)) pings + $([math]::Floor($succeededActions / 2)) pongs)" -ForegroundColor White
Write-Host "  Duration:      $([math]::Round($totalDuration.TotalSeconds, 1))s" -ForegroundColor White
Write-Host ""

# Step results
Write-Host "  -----------------------------------------------" -ForegroundColor Gray
$statusColor = if ($stepsPassed -eq $totalSteps) { "Green" } else { "Red" }
Write-Host "  Steps:   $stepsPassed/$totalSteps passed" -ForegroundColor $statusColor
Write-Host "  Actions: $succeededActions/$totalActions succeeded" -ForegroundColor $statusColor
Write-Host "  -----------------------------------------------" -ForegroundColor Gray
Write-Host ""

if ($stepsPassed -eq $totalSteps -and $allActionsSucceeded) {
    Write-Host "  RESULT: PASS - Full-stack pipeline verified!" -ForegroundColor Green
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "  RESULT: FAIL - Some steps or actions failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Cyan
    exit 1
}
