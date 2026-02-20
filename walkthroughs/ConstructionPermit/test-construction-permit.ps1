#!/usr/bin/env pwsh
# Construction Permit Approval Full-Stack Walkthrough
# Multi-org workflow: 4 organisations, 5 participants, 6 actions with conditional routing,
# calculations (riskScore, permitFee), rejection paths, and Building Permit VC issuance.
#
# Usage:
#   ./walkthroughs/ConstructionPermit/test-construction-permit.ps1
#   ./walkthroughs/ConstructionPermit/test-construction-permit.ps1 -Scenario A
#   ./walkthroughs/ConstructionPermit/test-construction-permit.ps1 -Scenario B -ShowJson
#   ./walkthroughs/ConstructionPermit/test-construction-permit.ps1 -Scenario All -Profile aspire

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('A', 'B', 'C', 'All')]
    [string]$Scenario = 'All',

    [Parameter(Mandatory=$false)]
    [ValidateSet('gateway', 'direct', 'aspire')]
    [string]$Profile = 'gateway',

    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@construction-permit.local",

    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "PermitAdmin_2026!",

    [Parameter(Mandatory=$false)]
    [string]$AdminName = "Permit Admin",

    [Parameter(Mandatory=$false)]
    [string]$OrgName = "Construction Permit Demo",

    [Parameter(Mandatory=$false)]
    [string]$OrgSubdomain = "construction-permit",

    [Parameter(Mandatory=$false)]
    [switch]$ShowJson = $false,

    [Parameter(Mandatory=$false)]
    [switch]$SkipCleanup = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Construction Permit Approval Walkthrough" -ForegroundColor Cyan
Write-Host "  4 Organisations | 5 Participants | 6 Actions | Conditional Routing" -ForegroundColor Cyan
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

$scenariosToRun = if ($Scenario -eq 'All') { @('A', 'B', 'C') } else { @($Scenario) }

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Tenant Service:    $TenantUrl" -ForegroundColor White
Write-Host "  Blueprint Service: $BlueprintUrl" -ForegroundColor White
Write-Host "  Register Service:  $RegisterUrl" -ForegroundColor White
Write-Host "  Wallet Service:    $WalletUrl (via gateway)" -ForegroundColor White
Write-Host "  Organization:      $OrgName ($OrgSubdomain)" -ForegroundColor White
Write-Host "  Scenarios:         $($scenariosToRun -join ', ')" -ForegroundColor White
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

function Write-Warn {
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

function Get-ErrorBody {
    param($Exception)
    try {
        if ($Exception.Response) {
            $errorStream = $Exception.Response.GetResponseStream()
            $errorReader = New-Object System.IO.StreamReader($errorStream)
            return $errorReader.ReadToEnd()
        }
    } catch {}
    return $null
}

# Participant-to-wallet mapping (populated in Phase 3)
$wallets = @{}  # participant-id -> wallet address

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
        Write-Warn "API Gateway health check returned error (services may still be starting)"
        Write-Info "Proceeding anyway -- individual service calls will fail if services are down"
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
        organizationDescription = "Multi-org construction permit approval walkthrough"
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
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 409) {
            Write-Warn "Organization already exists (409 Conflict) -- falling back to login"

            $encodedPassword = [Uri]::EscapeDataString($AdminPassword)
            $loginBody = "grant_type=password" + "&username=$AdminEmail" + "&password=$encodedPassword" + "&client_id=sorcha-cli"
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
# Phase 2: Create Participant Users (5 participants across 4 orgs)
# ============================================================================

Write-Step "Phase 2: Create Participant Users"
$totalSteps++

try {
    $participants = @(
        @{ email = "site-manager@meridian-construction.local";     displayName = "Site Manager (Meridian Construction)";          role = "contractor" },
        @{ email = "lead-engineer@apex-structural.local";          displayName = "Lead Engineer (Apex Structural Engineers)";     role = "structural-engineer" },
        @{ email = "planning-officer@riverside-council.local";     displayName = "Planning Officer (Riverside Borough Council)";  role = "planning-officer" },
        @{ email = "consultant@green-valley-env.local";            displayName = "Environmental Consultant (Green Valley Env.)";  role = "environmental-assessor" },
        @{ email = "building-control@riverside-council.local";     displayName = "Building Control (Riverside Borough Council)";   role = "building-control" }
    )

    foreach ($p in $participants) {
        Write-Info "Creating user: $($p.displayName)..."

        try {
            $userBody = @{
                email = $p.email
                displayName = $p.displayName
                externalIdpUserId = "$($p.role)-" + [guid]::NewGuid().ToString().Substring(0, 8)
                roles = @("Member")
            }

            $userResponse = Invoke-Api -Method POST `
                -Uri "$TenantUrl/organizations/$organizationId/users" `
                -Body $userBody `
                -Headers $headers

            Write-Success "$($p.role) user created (ID: $($userResponse.id))"
        } catch {
            $statusCode = $null
            try { $statusCode = $_.Exception.Response.StatusCode.value__ } catch {}
            if ($statusCode -eq 409 -or $statusCode -eq 400) {
                Write-Warn "$($p.role) may already exist -- continuing"
            } else {
                Write-Warn "Failed to create $($p.role): $($_.Exception.Message)"
            }
        }
    }

    Write-Success "Participant user creation complete"
    Write-Info "Note: 2 participants (planning-officer, building-control) are from the same org (Riverside Borough Council)"
    $stepsPassed++
} catch {
    Write-Fail "Participant creation failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    $stepsPassed++  # Non-critical
}

# ============================================================================
# Phase 3: Create Wallets (5 ED25519 wallets + 1 designer wallet)
# ============================================================================

Write-Step "Phase 3: Create Wallets (6 ED25519 wallets)"
$totalSteps++

$designerWalletAddress = ""

try {
    $walletDefs = @(
        @{ name = "Designer Wallet";             varName = "designer" },
        @{ name = "Contractor Wallet";           varName = "contractor" },
        @{ name = "Structural Engineer Wallet";  varName = "structural-engineer" },
        @{ name = "Planning Officer Wallet";     varName = "planning-officer" },
        @{ name = "Environmental Assessor Wallet"; varName = "environmental-assessor" },
        @{ name = "Building Control Wallet";     varName = "building-control" }
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

        if ($w.varName -eq "designer") {
            $designerWalletAddress = $address
        } else {
            $wallets[$w.varName] = $address
        }

        Write-Success "$($w.name) created: $address"
        Write-Warn "Mnemonic (BACKUP!): $mnemonic"
    }

    Write-Host ""
    Write-Info "Wallet Summary:"
    Write-Host "    Designer:               $designerWalletAddress" -ForegroundColor White
    Write-Host "    Contractor:             $($wallets['contractor'])" -ForegroundColor White
    Write-Host "    Structural Engineer:    $($wallets['structural-engineer'])" -ForegroundColor White
    Write-Host "    Planning Officer:       $($wallets['planning-officer'])" -ForegroundColor White
    Write-Host "    Environmental Assessor: $($wallets['environmental-assessor'])" -ForegroundColor White
    Write-Host "    Building Control:       $($wallets['building-control'])" -ForegroundColor White

    $stepsPassed++
} catch {
    Write-Fail "Wallet creation failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Check that Wallet Service is accessible via API Gateway" -ForegroundColor Yellow
    exit 1
}

# ============================================================================
# Phase 3b: Register Participant Profile & Link Wallets
# ============================================================================

Write-Step "Phase 3b: Register Participant Profile & Link All Wallets"
$totalSteps++

$participantId = ""

try {
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
            Write-Warn "Participant already exists -- fetching existing profile"

            $profiles = Invoke-Api -Method GET `
                -Uri "$TenantUrl/me/participant-profiles" `
                -Headers $headers

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

    # Link all 5 participant wallets via challenge-sign-verify
    foreach ($role in @('contractor', 'structural-engineer', 'planning-officer', 'environmental-assessor', 'building-control')) {
        $walletAddr = $wallets[$role]
        Write-Info "Linking $role wallet ($walletAddr)..."

        try {
            # Step 1: Initiate wallet link challenge
            $challengeBody = @{
                walletAddress = $walletAddr
                algorithm = "ED25519"
            }

            $challengeResponse = Invoke-Api -Method POST `
                -Uri "$TenantUrl/organizations/$organizationId/participants/$participantId/wallet-links" `
                -Body $challengeBody `
                -Headers $headers

            $challengeId = $challengeResponse.challengeId
            $challengeMessage = $challengeResponse.challenge
            Write-Info "  Challenge received (ID: $challengeId)"

            # Step 2: Sign the challenge
            $challengeBytes = [System.Text.Encoding]::UTF8.GetBytes($challengeMessage)
            $challengeBase64 = [Convert]::ToBase64String($challengeBytes)

            $signBody = @{
                transactionData = $challengeBase64
                isPreHashed = $false
            }

            $signResponse = Invoke-Api -Method POST `
                -Uri "$WalletUrl/v1/wallets/$walletAddr/sign" `
                -Body $signBody `
                -Headers $headers

            Write-Info "  Challenge signed by $($signResponse.signedBy)"

            # Step 3: Verify the wallet link
            $verifyBody = @{
                signature = $signResponse.signature
                publicKey = $signResponse.publicKey
            }

            $verifyResponse = Invoke-Api -Method POST `
                -Uri "$TenantUrl/organizations/$organizationId/participants/$participantId/wallet-links/$challengeId/verify" `
                -Body $verifyBody `
                -Headers $headers

            Write-Success "$role wallet linked (status: $($verifyResponse.status))"
        } catch {
            $errMsg = $_.Exception.Message
            if ($errMsg -match "already linked") {
                Write-Warn "$role wallet already linked -- continuing"
            } else {
                Write-Warn "$role wallet link failed: $errMsg -- continuing"
            }
        }
    }

    Write-Info "Admin participant has all 5 role wallets linked for action execution"
    $stepsPassed++
} catch {
    Write-Fail "Participant registration or wallet linking failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    $errorBody = Get-ErrorBody -Exception $_.Exception
    if ($errorBody) { Write-Host "  Response: $errorBody" -ForegroundColor Red }
    exit 1
}

# ============================================================================
# Phase 4: Create Public Register (2-phase flow)
# ============================================================================

Write-Step "Phase 4: Create Public Register (2-phase initiate and finalize)"
$totalSteps++

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$registerId = ""

try {
    Write-Info "Initiating public register creation..."

    $initiateBody = @{
        name = "Construction Permit Register"
        description = "Public register for the construction permit approval walkthrough"
        tenantId = if ($organizationId) { $organizationId } else { "default" }
        advertise = $true
        isPublic = $true
        owners = @(
            @{
                userId = if ($adminUserId) { $adminUserId } else { "admin" }
                walletId = $designerWalletAddress
            }
        )
        metadata = @{
            source = "walkthrough"
            createdBy = "test-construction-permit.ps1"
            type = "public"
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

    # Sign attestations
    $signedAttestations = @()

    foreach ($att in $attestations) {
        $dataToSignHex = $att.dataToSign
        Write-Info "Signing attestation for $($att.role) with wallet $($att.walletId)..."

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

    # Finalize
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

    Write-Success "Public register created and finalized"
    Write-Info "Register ID: $registerId"
    Write-Info "Status: $($finalizeResponse.status)"
    if ($finalizeResponse.genesisTransactionId) {
        Write-Info "Genesis TX: $($finalizeResponse.genesisTransactionId)"
    }

    $stepsPassed++
} catch {
    Write-Fail "Register creation failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    $errorBody = Get-ErrorBody -Exception $_.Exception
    if ($errorBody) { Write-Host "  Response: $errorBody" -ForegroundColor Red }
    Write-Host "  Check Register Service logs: docker-compose logs register-service" -ForegroundColor Yellow
    exit 1
}

# ============================================================================
# Phase 5: Load & Create Blueprint from Template
# ============================================================================

Write-Step "Phase 5: Load & Create Blueprint from Template"
$totalSteps++

$blueprintId = ""

try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $templatePath = Join-Path $scriptDir "construction-permit-template.json"
    Write-Info "Loading template from: $templatePath"

    # Use -Depth 30 to preserve deeply nested properties like
    # rejectionConfig.targetActionId (default depth is 2 which corrupts them).
    $templateJson = Get-Content -Path $templatePath -Raw | ConvertFrom-Json -Depth 30

    # Extract the blueprint from the template wrapper
    $blueprint = $templateJson.template

    # Patch participant wallet addresses
    Write-Info "Patching wallet addresses into blueprint participants..."
    foreach ($participant in $blueprint.participants) {
        $walletAddr = $wallets[$participant.id]
        if ($walletAddr) {
            $participant | Add-Member -NotePropertyName "walletAddress" -NotePropertyValue $walletAddr -Force
            Write-Info "  $($participant.id) -> $walletAddr"
        } else {
            Write-Warn "  No wallet found for participant $($participant.id)"
        }
    }

    # Generate unique ID so re-runs don't collide
    $blueprint.id = "construction-permit-$timestamp"

    $blueprintJson = $blueprint | ConvertTo-Json -Depth 30

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
    $errorBody = Get-ErrorBody -Exception $_.Exception
    if ($errorBody) { Write-Host "  Response: $errorBody" -ForegroundColor Red }
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

        if ($publishResponse.warnings -and ($publishResponse.warnings | Measure-Object).Count -gt 0) {
            Write-Info "Publish warnings:"
            foreach ($warning in $publishResponse.warnings) {
                Write-Host "    [warn] $warning" -ForegroundColor Yellow
            }
        }

        $stepsPassed++
    } else {
        Write-Fail "Unexpected status code: $($publishRaw.StatusCode)"
        exit 1
    }
} catch {
    $errorBody = Get-ErrorBody -Exception $_.Exception
    Write-Fail "Failed to publish blueprint"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($errorBody) { Write-Host "  Response: $errorBody" -ForegroundColor Red }
    exit 1
}

# ============================================================================
# Phase 7: Execute Scenarios
# ============================================================================

$scenarioResults = @{}

$executeHeaders = @{
    Authorization = "Bearer $adminToken"
    "X-Delegation-Token" = $adminToken
}

# Map participant IDs to their wallet addresses for action execution
$participantWalletMap = @{
    "contractor"             = $wallets['contractor']
    "structural-engineer"    = $wallets['structural-engineer']
    "planning-officer"       = $wallets['planning-officer']
    "environmental-assessor" = $wallets['environmental-assessor']
    "building-control"       = $wallets['building-control']
}

# Action sender map (from blueprint)
$actionSenderMap = @{
    1 = "contractor"
    2 = "structural-engineer"
    3 = "planning-officer"
    4 = "environmental-assessor"
    5 = "building-control"
    6 = "planning-officer"
}

foreach ($scenarioId in $scenariosToRun) {
    $scenarioFile = switch ($scenarioId) {
        'A' { "scenario-a-low-risk.json" }
        'B' { "scenario-b-high-risk.json" }
        'C' { "scenario-c-rejection.json" }
    }

    $scenarioPath = Join-Path $scriptDir "data/$scenarioFile"
    $scenarioData = Get-Content -Path $scenarioPath -Raw | ConvertFrom-Json

    Write-Step "Phase 7.$scenarioId : $($scenarioData.name)"
    $totalSteps++

    $scenarioStart = Get-Date
    $scenarioPassed = $true
    $instanceId = ""

    try {
        # --- Create Instance ---
        Write-Info "Creating workflow instance..."

        $instanceBody = @{
            blueprintId = $blueprintId
            registerId = $registerId
            tenantId = if ($organizationId) { $organizationId } else { "default" }
            metadata = @{
                source = "walkthrough"
                scenario = $scenarioId
                scenarioName = $scenarioData.name
            }
        }

        $instanceResponse = Invoke-Api -Method POST `
            -Uri "$BlueprintUrl/instances/" `
            -Body $instanceBody `
            -Headers $headers

        $instanceId = $instanceResponse.id
        Write-Success "Instance created: $instanceId"
        Write-Info "State: $($instanceResponse.state)"

        # --- Execute Actions ---
        $expectedPath = @($scenarioData.expectedPath)
        $actionsExecuted = 0
        $isRejection = [bool]$scenarioData.expectedRejection

        foreach ($actionId in $expectedPath) {
            $actionIdStr = "$actionId"
            $sender = $actionSenderMap[[int]$actionId]
            $senderWallet = $participantWalletMap[$sender]
            $actionData = $scenarioData.actions."$actionId"

            # Convert PSObject to hashtable for payload
            $payloadData = @{}
            foreach ($prop in $actionData.PSObject.Properties) {
                $payloadData[$prop.Name] = $prop.Value
            }

            $isLastAction = ($actionId -eq $expectedPath[-1])
            $isRejectionAction = $isRejection -and $isLastAction

            if ($isRejectionAction) {
                # Rejections use a separate /reject endpoint (not /execute)
                Write-Info "Action $actionId ($sender): Submitting REJECTION..."

                $rejectBody = @{
                    reason = $scenarioData.rejectionReason
                    senderWallet = $senderWallet
                    registerAddress = $registerId
                }

                try {
                    $actionResponse = Invoke-Api -Method POST `
                        -Uri "$BlueprintUrl/instances/$instanceId/actions/$actionIdStr/reject" `
                        -Body $rejectBody `
                        -Headers $executeHeaders

                    $actionsExecuted++
                    Write-Success "Action ${actionId}: REJECTED (routed back to Action $($scenarioData.rejectionAction))"
                } catch {
                    Write-Fail "Action $actionId rejection failed: $($_.Exception.Message)"
                    $errorBody = Get-ErrorBody -Exception $_.Exception
                    if ($errorBody) { Write-Host "  Response: $errorBody" -ForegroundColor Red }
                    $scenarioPassed = $false
                    break
                }
            } else {
                Write-Info "Action $actionId ($sender): Submitting..."

                $actionBody = @{
                    blueprintId = $blueprintId
                    actionId = $actionIdStr
                    instanceId = $instanceId
                    senderWallet = $senderWallet
                    registerAddress = $registerId
                    payloadData = $payloadData
                }

                try {
                    $actionResponse = Invoke-Api -Method POST `
                        -Uri "$BlueprintUrl/instances/$instanceId/actions/$actionIdStr/execute" `
                        -Body $actionBody `
                        -Headers $executeHeaders

                    $actionsExecuted++

                    $nextAction = if ($actionResponse.nextAction) { $actionResponse.nextAction } else { "workflow complete" }
                    Write-Success "Action ${actionId}: OK (next: $nextAction)"

                    # Show calculated values if present
                    if ($actionResponse.calculatedValues) {
                        foreach ($calc in $actionResponse.calculatedValues.PSObject.Properties) {
                            Write-Info "  Calculated: $($calc.Name) = $($calc.Value)"
                        }
                    }
                } catch {
                    Write-Fail "Action $actionId failed: $($_.Exception.Message)"
                    $errorBody = Get-ErrorBody -Exception $_.Exception
                    if ($errorBody) { Write-Host "  Response: $errorBody" -ForegroundColor Red }
                    $scenarioPassed = $false
                    break
                }
            }
        }

        # --- Verify Results ---
        $scenarioEnd = Get-Date
        $scenarioDuration = $scenarioEnd - $scenarioStart

        Write-Host ""
        if ($scenarioPassed) {
            Write-Success "$($scenarioData.name) completed in $([math]::Round($scenarioDuration.TotalSeconds, 1))s"
            Write-Info "Actions executed: $actionsExecuted / $($expectedPath.Count) expected"

            if ($scenarioData.expectedRiskScore) {
                Write-Info "Expected risk score: $($scenarioData.expectedRiskScore)"
            }
            if ($scenarioData.expectedPermitFee) {
                Write-Info "Expected permit fee: $($scenarioData.expectedPermitFee)"
            }
            if ($scenarioData.skipsEnvironmental -eq $true) {
                Write-Info "Environmental review: SKIPPED (low risk)"
            } elseif ($scenarioData.skipsEnvironmental -eq $false) {
                Write-Info "Environmental review: COMPLETED (high risk)"
            }
            if ($isRejection) {
                Write-Info "Outcome: REJECTED at Action $($scenarioData.rejectionAction)"
            } else {
                Write-Info "Outcome: APPROVED - Building Permit VC issued"
            }

            $stepsPassed++
        } else {
            Write-Fail "$($scenarioData.name) FAILED"
        }

        $scenarioResults[$scenarioId] = @{
            Name = $scenarioData.name
            Passed = $scenarioPassed
            ActionsExecuted = $actionsExecuted
            ExpectedActions = $expectedPath.Count
            Duration = $scenarioDuration
            InstanceId = $instanceId
            IsRejection = $isRejection
        }

    } catch {
        Write-Fail "Scenario $scenarioId failed during setup"
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        $scenarioResults[$scenarioId] = @{
            Name = $scenarioData.name
            Passed = $false
            ActionsExecuted = 0
            ExpectedActions = $expectedPath.Count
            Duration = (Get-Date) - $scenarioStart
            InstanceId = $instanceId
            IsRejection = $false
        }
    }
}

# ============================================================================
# Phase 8: Final Summary
# ============================================================================

$walkthroughEnd = Get-Date
$totalDuration = $walkthroughEnd - $walkthroughStart

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Construction Permit Walkthrough Results" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "  Organization:  $OrgName" -ForegroundColor White
if ($organizationId) {
    Write-Host "                 ($organizationId)" -ForegroundColor Gray
}
Write-Host "  Participants:  5 (across 4 organisations)" -ForegroundColor White
Write-Host "  Wallets:       6 (ED25519: 1 designer + 5 participant)" -ForegroundColor White
Write-Host "    Designer:               $designerWalletAddress" -ForegroundColor Gray
Write-Host "    Contractor:             $($wallets['contractor'])" -ForegroundColor Gray
Write-Host "    Structural Engineer:    $($wallets['structural-engineer'])" -ForegroundColor Gray
Write-Host "    Planning Officer:       $($wallets['planning-officer'])" -ForegroundColor Gray
Write-Host "    Environmental Assessor: $($wallets['environmental-assessor'])" -ForegroundColor Gray
Write-Host "    Building Control:       $($wallets['building-control'])" -ForegroundColor Gray
Write-Host "  Register:      $registerId (public)" -ForegroundColor White
Write-Host "  Blueprint:     $blueprintId (published)" -ForegroundColor White
Write-Host ""

# Scenario results table
Write-Host "  Scenario Results:" -ForegroundColor Yellow
Write-Host "  -----------------------------------------------" -ForegroundColor Gray

$allPassed = $true
foreach ($scenarioId in $scenariosToRun) {
    $sr = $scenarioResults[$scenarioId]
    $statusIcon = if ($sr.Passed) { "[OK]" } else { "[X]" }
    $statusColor = if ($sr.Passed) { "Green" } else { "Red" }
    $outcome = if ($sr.IsRejection) { "REJECTED" } else { "APPROVED" }

    Write-Host "  $statusIcon Scenario $scenarioId : $($sr.Name)" -ForegroundColor $statusColor
    Write-Host "       Actions: $($sr.ActionsExecuted)/$($sr.ExpectedActions) | " -NoNewline -ForegroundColor Gray
    Write-Host "Outcome: $outcome | " -NoNewline -ForegroundColor Gray
    Write-Host "Duration: $([math]::Round($sr.Duration.TotalSeconds, 1))s" -ForegroundColor Gray
    if ($sr.InstanceId) {
        Write-Host "       Instance: $($sr.InstanceId)" -ForegroundColor DarkGray
    }

    if (-not $sr.Passed) { $allPassed = $false }
}

Write-Host "  -----------------------------------------------" -ForegroundColor Gray
Write-Host ""

$statusColor = if ($stepsPassed -eq $totalSteps) { "Green" } else { "Red" }
Write-Host "  Steps:     $stepsPassed/$totalSteps passed" -ForegroundColor $statusColor
Write-Host "  Duration:  $([math]::Round($totalDuration.TotalSeconds, 1))s" -ForegroundColor White
Write-Host ""

if ($stepsPassed -eq $totalSteps -and $allPassed) {
    Write-Host "  RESULT: PASS - All scenarios verified!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Key Capabilities Demonstrated:" -ForegroundColor Yellow
    Write-Host "    - Multi-org participation (4 organisations, 5 participants)" -ForegroundColor White
    Write-Host "    - Same-org multi-user (planning-officer + building-control = Riverside Council)" -ForegroundColor White
    Write-Host "    - JSON Logic calculations (riskScore, permitFee)" -ForegroundColor White
    Write-Host "    - Conditional routing (riskScore >= 7 -> environmental review)" -ForegroundColor White
    Write-Host "    - Rejection paths (actions 3, 4, 6 -> back to contractor)" -ForegroundColor White
    Write-Host "    - Building Permit Verifiable Credential issuance" -ForegroundColor White
    Write-Host "    - Selective disclosure (each participant sees only relevant data)" -ForegroundColor White
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "  RESULT: FAIL - Some steps or scenarios failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Troubleshooting:" -ForegroundColor Yellow
    Write-Host "    docker-compose logs blueprint-service" -ForegroundColor White
    Write-Host "    docker-compose logs register-service" -ForegroundColor White
    Write-Host "    docker-compose logs wallet-service" -ForegroundColor White
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Cyan
    exit 1
}
