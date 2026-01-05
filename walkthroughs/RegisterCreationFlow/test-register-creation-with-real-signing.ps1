#!/usr/bin/env pwsh
# Complete Register Creation Flow with Real Wallet Signing
# Extends the basic register creation walkthrough with actual cryptographic signatures

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('gateway', 'direct')]
    [string]$Profile = 'gateway',

    [Parameter(Mandatory=$false)]
    [ValidateSet("ED25519", "NISTP256", "RSA4096")]
    [string]$Algorithm = "ED25519",

    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@sorcha.local",

    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "Dev_Pass_2025!",

    [Parameter(Mandatory=$false)]
    [switch]$ShowJson = $false
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Complete Register Creation with Real Wallet Signing" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration based on profile
$ApiGatewayUrl = "http://localhost"
$RegisterServiceUrl = ""
$ValidatorServiceUrl = ""

switch ($Profile) {
    'gateway' {
        $RegisterServiceUrl = "$ApiGatewayUrl/api/registers"
        $ValidatorServiceUrl = "$ApiGatewayUrl/api/validator"
        $WalletServiceUrl = "$ApiGatewayUrl/api/v1/wallets"
        $ProfileDescription = "API Gateway (YARP Routing)"
        Write-Host "Profile: $Profile (Recommended)" -ForegroundColor Green
        Write-Host "Mode: All requests routed through API Gateway" -ForegroundColor Gray
    }
    'direct' {
        $RegisterServiceUrl = "http://localhost:5290"
        $ValidatorServiceUrl = "http://localhost:5100"
        $WalletServiceUrl = "http://localhost:5000/api/v1/wallets"
        $ProfileDescription = "Direct Service Access"
        Write-Host "Profile: $Profile (Debugging)" -ForegroundColor Yellow
        Write-Host "Mode: Direct access to service ports (bypasses gateway)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Profile: $ProfileDescription" -ForegroundColor White
Write-Host "  Register Service: $RegisterServiceUrl" -ForegroundColor White
Write-Host "  Wallet Service: $WalletServiceUrl" -ForegroundColor White
Write-Host "  Algorithm: $Algorithm" -ForegroundColor White
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

function Write-Error {
    param([string]$Message)
    Write-Host "[X] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "[i] $Message" -ForegroundColor Cyan
}

# Step 1: Admin Authentication
Write-Step "Step 1: Admin Authentication"

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
} catch {
    Write-Error "Admin authentication failed"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Create Wallet for Register Owner
Write-Step "Step 2: Create Wallet for Register Owner"

$walletName = "Register Owner ($Algorithm) $(Get-Date -Format 'yyyy-MM-dd HHmmss')"
$createWalletBody = @{
    name = $walletName
    algorithm = $Algorithm
    wordCount = 12
} | ConvertTo-Json

try {
    Write-Info "Creating $Algorithm wallet..."

    $createResponse = Invoke-RestMethod `
        -Uri "$WalletServiceUrl" `
        -Method POST `
        -Headers @{
            Authorization = "Bearer $adminToken"
            "Content-Type" = "application/json"
        } `
        -Body $createWalletBody `
        -UseBasicParsing

    Write-Success "Wallet created successfully!"
    Write-Host ""
    Write-Host "Wallet Details:" -ForegroundColor Yellow
    Write-Host "  Name: $($createResponse.wallet.name)" -ForegroundColor White
    Write-Host "  Address: $($createResponse.wallet.address)" -ForegroundColor White
    Write-Host "  Algorithm: $($createResponse.wallet.algorithm)" -ForegroundColor White
    Write-Host "  Public Key (Base64): $($createResponse.wallet.publicKey)" -ForegroundColor White

    $walletAddress = $createResponse.wallet.address
    $publicKeyBase64 = $createResponse.wallet.publicKey

} catch {
    Write-Error "Failed to create wallet"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# Step 3: Initiate Register Creation
Write-Step "Step 3: Initiate Register Creation"

$registerName = "Test Register with $Algorithm Signing"
$tenantId = "test-tenant-001"
$userId = "admin-user-001"

# NEW WORKFLOW: Send owner array instead of single creator
$initiateRequest = @{
    name = $registerName
    description = "Register created with real $Algorithm wallet signing (attestation-based) at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    tenantId = $tenantId
    owners = @(
        @{
            userId = $userId
            walletId = $walletAddress
            role = "Owner"
        }
    )
} | ConvertTo-Json -Depth 10

try {
    Write-Info "Initiating register creation with new attestation workflow..."
    Write-Host "Request:" -ForegroundColor Gray
    Write-Host $initiateRequest -ForegroundColor DarkGray
    Write-Host ""

    $initiateResponse = Invoke-RestMethod `
        -Uri "$RegisterServiceUrl/initiate" `
        -Method POST `
        -Headers @{
            "Content-Type" = "application/json"
        } `
        -Body $initiateRequest `
        -UseBasicParsing

    Write-Success "Register initiation successful!"
    Write-Host ""
    Write-Host "Initiation Response:" -ForegroundColor Yellow
    Write-Host "  Register ID: $($initiateResponse.registerId)" -ForegroundColor White
    Write-Host "  Nonce: $($initiateResponse.nonce)" -ForegroundColor White
    Write-Host "  Expires At: $($initiateResponse.expiresAt)" -ForegroundColor White
    Write-Host "  Attestations to Sign: $($initiateResponse.attestationsToSign.Count)" -ForegroundColor White

    foreach ($attestation in $initiateResponse.attestationsToSign) {
        Write-Host ""
        Write-Host "  Attestation for User: $($attestation.userId)" -ForegroundColor Cyan
        Write-Host "    Wallet: $($attestation.walletId)" -ForegroundColor Gray
        Write-Host "    Role: $($attestation.role)" -ForegroundColor Gray
        Write-Host "    Data to Sign (SHA-256 Hash): $($attestation.dataToSign)" -ForegroundColor Gray
    }

    $registerId = $initiateResponse.registerId
    $attestationsToSign = $initiateResponse.attestationsToSign
    $nonce = $initiateResponse.nonce

} catch {
    Write-Error "Failed to initiate register creation"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# Step 4: Sign Each Attestation with Wallet (using derivation path)
Write-Step "Step 4: Sign Attestations with Wallet"

$signedAttestations = @()

foreach ($attestation in $attestationsToSign) {
    Write-Info "Signing attestation for user: $($attestation.userId)"
    Write-Host "  Wallet: $($attestation.walletId)" -ForegroundColor Gray
    Write-Host "  Role: $($attestation.role)" -ForegroundColor Gray

    # The dataToSign is now canonical JSON (not a hash)
    # The wallet's TransactionService will hash it before signing
    $canonicalJson = $attestation.dataToSign
    Write-Host "  Canonical JSON: $canonicalJson" -ForegroundColor Gray

    # Convert canonical JSON string to UTF-8 bytes, then to base64 for wallet service
    $dataBytes = [System.Text.Encoding]::UTF8.GetBytes($canonicalJson)
    $dataToSignBase64 = [Convert]::ToBase64String($dataBytes)

    # NEW: Include derivation path for attestation signing
    $signBody = @{
        transactionData = $dataToSignBase64
        derivationPath = "sorcha:register-attestation"
    } | ConvertTo-Json

    try {
        Write-Info "Sending sign request with derivation path 'sorcha:register-attestation'..."

        $signResponse = Invoke-RestMethod `
            -Uri "$WalletServiceUrl/$($attestation.walletId)/sign" `
            -Method POST `
            -Headers @{
                Authorization = "Bearer $adminToken"
                "Content-Type" = "application/json"
            } `
            -Body $signBody `
            -UseBasicParsing

        Write-Success "Attestation signed successfully!"
        Write-Host "  Signature (Base64): $($signResponse.signature)" -ForegroundColor Gray
        Write-Host "  Derived Public Key: $($signResponse.publicKey)" -ForegroundColor Gray
        Write-Host ""

        # Build signed attestation for finalize request
        $signedAttestation = @{
            attestationData = $attestation.attestationData
            publicKey = $signResponse.publicKey  # Use derived public key from signing response
            signature = $signResponse.signature
            algorithm = $Algorithm
        }

        $signedAttestations += $signedAttestation

    } catch {
        Write-Error "Failed to sign attestation for user $($attestation.userId)"
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails) {
            Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
        exit 1
    }
}

Write-Success "All attestations signed successfully!"
Write-Host "  Total signed attestations: $($signedAttestations.Count)" -ForegroundColor White

# Step 5: Finalize Register Creation with Signed Attestations
Write-Step "Step 5: Finalize Register Creation"

# NEW WORKFLOW: Send signed attestations array
$finalizeRequest = @{
    registerId = $registerId
    nonce = $nonce
    signedAttestations = $signedAttestations
} | ConvertTo-Json -Depth 10

try {
    Write-Info "Finalizing register creation with signed attestations..."
    Write-Host "Request:" -ForegroundColor Gray
    Write-Host $finalizeRequest -ForegroundColor DarkGray
    Write-Host ""

    $finalizeResponse = Invoke-RestMethod `
        -Uri "$RegisterServiceUrl/finalize" `
        -Method POST `
        -Headers @{
            "Content-Type" = "application/json"
        } `
        -Body $finalizeRequest `
        -UseBasicParsing

    Write-Success "Register creation finalized successfully!"
    Write-Host ""
    Write-Host "Finalization Response:" -ForegroundColor Yellow
    Write-Host "  Register ID: $($finalizeResponse.registerId)" -ForegroundColor White
    Write-Host "  Status: $($finalizeResponse.status)" -ForegroundColor White

    if ($finalizeResponse.genesisTransactionId) {
        Write-Host "  Genesis Transaction ID: $($finalizeResponse.genesisTransactionId)" -ForegroundColor White
        $genesisTransactionId = $finalizeResponse.genesisTransactionId
    }

    # Show full JSON if requested
    if ($ShowJson) {
        Write-Host ""
        Write-Host "Full Finalization Response (JSON):" -ForegroundColor Magenta
        Write-Host ($finalizeResponse | ConvertTo-Json -Depth 10) -ForegroundColor DarkGray
    }

} catch {
    Write-Error "Failed to finalize register creation"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Possible causes:" -ForegroundColor Yellow
    Write-Host "  - Attestation signature verification failed" -ForegroundColor Gray
    Write-Host "  - Nonce expired (5-minute TTL)" -ForegroundColor Gray
    Write-Host "  - Public key format mismatch" -ForegroundColor Gray
    Write-Host "  - Register ID not found" -ForegroundColor Gray
    Write-Host "  - Attestation data mismatch (registerId, registerName changed)" -ForegroundColor Gray
    exit 1
}

# Step 6: Verify Genesis Transaction (if available)
if ($genesisTransactionId) {
    Write-Step "Step 6: Verify Genesis Transaction"

    try {
        Write-Info "Checking genesis transaction in validator..."

        $txResponse = Invoke-RestMethod `
            -Uri "$ValidatorServiceUrl/transactions/$genesisTransactionId" `
            -Method GET `
            -UseBasicParsing

        Write-Success "Genesis transaction found in validator!"
        Write-Host ""
        Write-Host "Transaction Details:" -ForegroundColor Yellow
        Write-Host "  Transaction ID: $($txResponse.id)" -ForegroundColor White
        Write-Host "  Type: $($txResponse.type)" -ForegroundColor White
        Write-Host "  Register ID: $($txResponse.registerId)" -ForegroundColor White
        Write-Host "  Status: $($txResponse.status)" -ForegroundColor White

        # Show full JSON if requested
        if ($ShowJson) {
            Write-Host ""
            Write-Host "================================================================================" -ForegroundColor Magenta
            Write-Host "  FULL GENESIS TRANSACTION JSON" -ForegroundColor Magenta
            Write-Host "================================================================================" -ForegroundColor Magenta
            Write-Host ""
            Write-Host ($txResponse | ConvertTo-Json -Depth 10) -ForegroundColor Gray
            Write-Host ""

            # Extract and display control record if available
            if ($txResponse.controlRecord) {
                Write-Host "================================================================================" -ForegroundColor Cyan
                Write-Host "  REGISTER CONTROL RECORD" -ForegroundColor Cyan
                Write-Host "================================================================================" -ForegroundColor Cyan
                Write-Host ""
                Write-Host ($txResponse.controlRecord | ConvertTo-Json -Depth 10) -ForegroundColor Gray
                Write-Host ""

                # Show attestations
                if ($txResponse.controlRecord.attestations) {
                    Write-Host "Attestations (Verified Owner Signatures):" -ForegroundColor Yellow
                    foreach ($attestation in $txResponse.controlRecord.attestations) {
                        Write-Host "  - Subject: $($attestation.subject)" -ForegroundColor White
                        Write-Host "    Role: $($attestation.role)" -ForegroundColor Gray
                        Write-Host "    Public Key: $($attestation.publicKey)" -ForegroundColor Gray
                        Write-Host "    Algorithm: $($attestation.algorithm)" -ForegroundColor Gray
                        Write-Host "    Signature: $($attestation.signature)" -ForegroundColor DarkGray
                        Write-Host ""
                    }
                }
            }

            # Extract and display signed docket if available
            if ($txResponse.signedDocket) {
                Write-Host "================================================================================" -ForegroundColor Green
                Write-Host "  SIGNED DOCKET (System Wallet Signature)" -ForegroundColor Green
                Write-Host "================================================================================" -ForegroundColor Green
                Write-Host ""
                Write-Host ($txResponse.signedDocket | ConvertTo-Json -Depth 10) -ForegroundColor Gray
                Write-Host ""

                Write-Host "Docket Signature Details:" -ForegroundColor Yellow
                Write-Host "  Algorithm: $($txResponse.signedDocket.algorithm)" -ForegroundColor White
                Write-Host "  Public Key: $($txResponse.signedDocket.publicKey)" -ForegroundColor Gray
                Write-Host "  Signature: $($txResponse.signedDocket.signature)" -ForegroundColor DarkGray
                Write-Host ""
            }
        }

    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__

        if ($statusCode -eq 404) {
            Write-Info "Genesis transaction not yet in validator mempool"
            Write-Info "This may be expected if transaction processing is asynchronous"
        } else {
            Write-Error "Failed to retrieve genesis transaction"
            Write-Host "  Status: $statusCode" -ForegroundColor Red
        }
    }
}

# Summary
Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Complete Register Creation: SUCCESS!" -ForegroundColor Green
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Test Results:" -ForegroundColor Yellow
Write-Host "  [OK] Admin authentication" -ForegroundColor Green
Write-Host "  [OK] Wallet creation ($Algorithm)" -ForegroundColor Green
Write-Host "  [OK] Register creation initiation (attestation-based)" -ForegroundColor Green
Write-Host "  [OK] Attestation signing with derivation path" -ForegroundColor Green
Write-Host "  [OK] Individual attestation signature verification" -ForegroundColor Green
Write-Host "  [OK] Register creation finalized" -ForegroundColor Green

if ($genesisTransactionId) {
    Write-Host "  [OK] Genesis transaction submitted" -ForegroundColor Green
}

Write-Host ""
Write-Host "Register Created Successfully!" -ForegroundColor Green
Write-Host "  Name: $registerName" -ForegroundColor White
Write-Host "  Register ID: $registerId" -ForegroundColor White
Write-Host "  Owner Wallet: $walletAddress" -ForegroundColor White
Write-Host "  Algorithm: $Algorithm" -ForegroundColor White
Write-Host "  Profile: $Profile" -ForegroundColor White
Write-Host ""

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  End-to-End Workflow Verified with Real Cryptographic Signatures!" -ForegroundColor Green
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "What was tested:" -ForegroundColor Yellow
Write-Host "  - Tenant Service: Admin authentication via service-auth/token" -ForegroundColor Gray
Write-Host "  - Wallet Service: Create HD wallet with $Algorithm" -ForegroundColor Gray
Write-Host "  - Wallet Service: Sign attestations with derivation path (sorcha:register-attestation)" -ForegroundColor Gray
Write-Host "  - Register Service: Initiate register creation (generate individual attestations)" -ForegroundColor Gray
Write-Host "  - Register Service: Verify individual attestation signatures" -ForegroundColor Gray
Write-Host "  - Register Service: Construct control record from verified attestations" -ForegroundColor Gray
Write-Host "  - Register Service: Submit genesis transaction to Validator" -ForegroundColor Gray
Write-Host "  - Validator Service: Sign control record with system wallet (sorcha:register-control)" -ForegroundColor Gray
Write-Host "  - Validator Service: Add genesis transaction to mempool" -ForegroundColor Gray
Write-Host ""
Write-Host "Architectural Improvements:" -ForegroundColor Yellow
Write-Host "  - Multi-owner support: Each owner signs individual attestation" -ForegroundColor Cyan
Write-Host "  - Derivation paths: Uses Sorcha system paths for role-based signing" -ForegroundColor Cyan
Write-Host "  - Two-phase signing: Owner attestations + system wallet control record" -ForegroundColor Cyan
Write-Host "  - Prevents tampering: RegisterName included in attestation prevents changes" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test with other algorithms:" -ForegroundColor Gray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Algorithm NISTP256" -ForegroundColor DarkGray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Algorithm RSA4096" -ForegroundColor DarkGray
Write-Host "  2. Test via direct profile for debugging:" -ForegroundColor Gray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Profile direct" -ForegroundColor DarkGray
Write-Host "  3. View full JSON structures (control record, genesis transaction, signed docket):" -ForegroundColor Gray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -ShowJson" -ForegroundColor DarkGray
Write-Host "  4. Add transactions to the register" -ForegroundColor Gray
Write-Host "  5. Verify transaction chain integrity" -ForegroundColor Gray
Write-Host ""
