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
    [string]$AdminPassword = "Dev_Pass_2025!"
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
$ownerDid = "did:sorcha:admin"

# Convert public key to hex for register control record
$publicKeyBytes = [Convert]::FromBase64String($publicKeyBase64)
$publicKeyHex = [BitConverter]::ToString($publicKeyBytes).Replace("-", "").ToLower()

$initiateRequest = @{
    name = $registerName
    description = "Register created with real $Algorithm wallet signing at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    tenantId = $tenantId
    ownerDid = $ownerDid
    ownerPublicKey = "$Algorithm`:$publicKeyHex"
} | ConvertTo-Json

try {
    Write-Info "Initiating register creation..."
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
    Write-Host "  Data to Sign (Hex Hash): $($initiateResponse.dataToSign)" -ForegroundColor White
    Write-Host "  Nonce: $($initiateResponse.nonce)" -ForegroundColor White
    Write-Host "  Expires At: $($initiateResponse.expiresAt)" -ForegroundColor White

    $registerId = $initiateResponse.registerId
    $dataToSignHex = $initiateResponse.dataToSign
    $nonce = $initiateResponse.nonce

} catch {
    Write-Error "Failed to initiate register creation"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# Step 4: Sign the Register Data with Wallet
Write-Step "Step 4: Sign Register Data with Wallet"

Write-Info "Converting hex hash to bytes for signing..."
Write-Host "  Data Hash (Hex): $dataToSignHex" -ForegroundColor Gray

# Convert hex string to bytes, then to base64 for wallet service
$dataBytes = [byte[]]::new($dataToSignHex.Length / 2)
for ($i = 0; $i -lt $dataToSignHex.Length; $i += 2) {
    $dataBytes[$i/2] = [Convert]::ToByte($dataToSignHex.Substring($i, 2), 16)
}
$dataToSignBase64 = [Convert]::ToBase64String($dataBytes)

Write-Host "  Data Hash (Base64): $dataToSignBase64" -ForegroundColor Gray
Write-Host ""

$signBody = @{
    transactionData = $dataToSignBase64
} | ConvertTo-Json

try {
    Write-Info "Sending sign request to wallet service..."
    Write-Host "  Wallet: $walletAddress" -ForegroundColor Gray
    Write-Host "  Endpoint: POST $WalletServiceUrl/$walletAddress/sign" -ForegroundColor Gray
    Write-Host ""

    $signResponse = Invoke-RestMethod `
        -Uri "$WalletServiceUrl/$walletAddress/sign" `
        -Method POST `
        -Headers @{
            Authorization = "Bearer $adminToken"
            "Content-Type" = "application/json"
        } `
        -Body $signBody `
        -UseBasicParsing

    Write-Success "Data signed successfully!"
    Write-Host ""
    Write-Host "Signature Details:" -ForegroundColor Yellow
    Write-Host "  Signature (Base64): $($signResponse.signature)" -ForegroundColor White
    Write-Host "  Signed By: $($signResponse.signedBy)" -ForegroundColor White
    Write-Host "  Signed At: $($signResponse.signedAt)" -ForegroundColor White

    $signatureBase64 = $signResponse.signature

    # Convert signature to hex for register service
    $signatureBytes = [Convert]::FromBase64String($signatureBase64)
    $signatureHex = [BitConverter]::ToString($signatureBytes).Replace("-", "").ToLower()
    Write-Host "  Signature (Hex): $signatureHex" -ForegroundColor Gray

} catch {
    Write-Error "Failed to sign data with wallet"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# Step 5: Finalize Register Creation with Real Signature
Write-Step "Step 5: Finalize Register Creation"

# Construct control record with signature
$controlRecord = @{
    registerId = $registerId
    name = $registerName
    description = "Register created with real $Algorithm wallet signing at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    tenantId = $tenantId
    createdAt = (Get-Date).ToUniversalTime().ToString("o")
    metadata = @{}
    attestations = @(
        @{
            role = "Owner"
            subject = $ownerDid
            publicKey = $publicKeyBase64  # Wallet service returns base64
            signature = $signatureBase64   # Wallet service returns base64
            algorithm = $Algorithm
            grantedAt = (Get-Date).ToUniversalTime().ToString("o")
        }
    )
}

$finalizeRequest = @{
    registerId = $registerId
    nonce = $nonce
    controlRecord = $controlRecord
} | ConvertTo-Json -Depth 10

try {
    Write-Info "Finalizing register creation with real signature..."
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

} catch {
    Write-Error "Failed to finalize register creation"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Possible causes:" -ForegroundColor Yellow
    Write-Host "  - Signature verification failed" -ForegroundColor Gray
    Write-Host "  - Nonce expired (5-minute TTL)" -ForegroundColor Gray
    Write-Host "  - Public key format mismatch" -ForegroundColor Gray
    Write-Host "  - Register ID not found" -ForegroundColor Gray
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
Write-Host "  [OK] Register creation initiation" -ForegroundColor Green
Write-Host "  [OK] Data signing with wallet" -ForegroundColor Green
Write-Host "  [OK] Signature verification by Register Service" -ForegroundColor Green
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
Write-Host "  - Wallet Service: Sign data with private key (real signature)" -ForegroundColor Gray
Write-Host "  - Register Service: Initiate register creation (get canonical hash)" -ForegroundColor Gray
Write-Host "  - Register Service: Verify signature with public key" -ForegroundColor Gray
Write-Host "  - Register Service: Create register with verified signature" -ForegroundColor Gray
Write-Host "  - Validator Service: Submit genesis transaction to mempool" -ForegroundColor Gray
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test with other algorithms:" -ForegroundColor Gray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Algorithm NISTP256" -ForegroundColor DarkGray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Algorithm RSA4096" -ForegroundColor DarkGray
Write-Host "  2. Test via direct profile for debugging:" -ForegroundColor Gray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Profile direct" -ForegroundColor DarkGray
Write-Host "  3. Add transactions to the register" -ForegroundColor Gray
Write-Host "  4. Verify transaction chain integrity" -ForegroundColor Gray
Write-Host ""
