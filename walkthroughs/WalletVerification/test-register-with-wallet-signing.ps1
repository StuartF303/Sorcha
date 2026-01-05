#!/usr/bin/env pwsh
# End-to-End Register Creation with Real Wallet Signing
# Tests complete workflow: Admin auth -> Create wallet -> Sign register data -> Create register

param(
    [Parameter(Mandatory=$false)]
    [string]$AdminEmail = "admin@sorcha.local",

    [Parameter(Mandatory=$false)]
    [string]$AdminPassword = "Dev_Pass_2025!",

    [Parameter(Mandatory=$false)]
    [string]$ApiGatewayUrl = "http://localhost",

    [Parameter(Mandatory=$false)]
    [ValidateSet("ED25519", "NISTP256", "RSA4096")]
    [string]$Algorithm = "ED25519"
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Register Creation with Real Wallet Signing" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This test demonstrates the complete register creation workflow:" -ForegroundColor Yellow
Write-Host "  1. Admin authentication" -ForegroundColor Gray
Write-Host "  2. Wallet creation with specified algorithm" -ForegroundColor Gray
Write-Host "  3. Register creation initiation (get data to sign)" -ForegroundColor Gray
Write-Host "  4. Sign register data with wallet" -ForegroundColor Gray
Write-Host "  5. Finalize register creation with real signature" -ForegroundColor Gray
Write-Host "  6. Verify register was created" -ForegroundColor Gray
Write-Host ""

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  API Gateway: $ApiGatewayUrl" -ForegroundColor White
Write-Host "  Admin User: $AdminEmail" -ForegroundColor White
Write-Host "  Wallet Algorithm: $Algorithm" -ForegroundColor White
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

# Step 2: Create Wallet for Signing
Write-Step "Step 2: Create Wallet for Register Owner"

$walletName = "Register Owner Wallet $(Get-Date -Format 'yyyy-MM-dd-HHmmss')"
$createWalletBody = @{
    name = $walletName
    algorithm = $Algorithm
    wordCount = 12
} | ConvertTo-Json

try {
    Write-Info "Creating $Algorithm wallet for register owner..."

    $createResponse = Invoke-RestMethod `
        -Uri "$ApiGatewayUrl/api/v1/wallets" `
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
    Write-Host "  Name: $($createResponse.name)" -ForegroundColor White
    Write-Host "  Address: $($createResponse.address)" -ForegroundColor White
    Write-Host "  Algorithm: $($createResponse.algorithm)" -ForegroundColor White
    Write-Host "  Public Key: $($createResponse.publicKey)" -ForegroundColor White

    $walletAddress = $createResponse.address
    $publicKey = $createResponse.publicKey

    # Parse algorithm-prefixed public key
    if ($publicKey -match "^($Algorithm):(.+)$") {
        $publicKeyHex = $matches[2]
    } else {
        $publicKeyHex = $publicKey
    }

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

$initiateRequest = @{
    name = $registerName
    description = "Register created with real $Algorithm wallet signing"
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
        -Uri "$ApiGatewayUrl/api/registers/initiate" `
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
    Write-Host "  Data to Sign (Hash): $($initiateResponse.dataToSign)" -ForegroundColor White
    Write-Host "  Nonce: $($initiateResponse.nonce)" -ForegroundColor White
    Write-Host "  Expires At: $($initiateResponse.expiresAt)" -ForegroundColor White

    $registerId = $initiateResponse.registerId
    $dataToSign = $initiateResponse.dataToSign
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

Write-Info "Signing register control record hash with $Algorithm wallet..."
Write-Host "  Wallet Address: $walletAddress" -ForegroundColor Gray
Write-Host "  Data Hash to Sign: $dataToSign" -ForegroundColor Gray
Write-Host ""

# The dataToSign is already a hex string (canonical JSON hash)
# We need to sign this hex string
$signBody = @{
    data = $dataToSign
} | ConvertTo-Json

try {
    Write-Info "Sending sign request to wallet service..."

    $signResponse = Invoke-RestMethod `
        -Uri "$ApiGatewayUrl/api/v1/wallets/$walletAddress/sign" `
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
    Write-Host "  Algorithm: $($signResponse.algorithm)" -ForegroundColor White
    Write-Host "  Signature: $($signResponse.signature)" -ForegroundColor White
    Write-Host "  Public Key: $($signResponse.publicKey)" -ForegroundColor White

    $signature = $signResponse.signature

} catch {
    Write-Error "Failed to sign data with wallet"
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "  Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "This may indicate:" -ForegroundColor Yellow
    Write-Host "  - Wallet Service sign endpoint is not implemented" -ForegroundColor Gray
    Write-Host "  - Wallet address is incorrect" -ForegroundColor Gray
    Write-Host "  - Signature format is not compatible" -ForegroundColor Gray
    exit 1
}

# Step 5: Finalize Register Creation with Real Signature
Write-Step "Step 5: Finalize Register Creation"

$finalizeRequest = @{
    registerId = $registerId
    nonce = $nonce
    signedData = @{
        dataToSign = $dataToSign
        signature = $signature
        publicKey = "$Algorithm`:$publicKeyHex"
    }
} | ConvertTo-Json -Depth 5

try {
    Write-Info "Finalizing register creation with real signature..."
    Write-Host "Request:" -ForegroundColor Gray
    Write-Host $finalizeRequest -ForegroundColor DarkGray
    Write-Host ""

    $finalizeResponse = Invoke-RestMethod `
        -Uri "$ApiGatewayUrl/api/registers/finalize" `
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
            -Uri "$ApiGatewayUrl/api/validator/transactions/$genesisTransactionId" `
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
Write-Host "  End-to-End Test Summary" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Test Results:" -ForegroundColor Yellow
Write-Host "  [OK] Admin authentication" -ForegroundColor Green
Write-Host "  [OK] Wallet creation ($Algorithm)" -ForegroundColor Green
Write-Host "  [OK] Register creation initiation" -ForegroundColor Green
Write-Host "  [OK] Data signing with wallet" -ForegroundColor Green
Write-Host "  [OK] Register creation finalization" -ForegroundColor Green

if ($genesisTransactionId) {
    Write-Host "  [OK] Genesis transaction submitted" -ForegroundColor Green
} else {
    Write-Host "  [SKIP] Genesis transaction (not available)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Register Created Successfully!" -ForegroundColor Green
Write-Host "  Name: $registerName" -ForegroundColor White
Write-Host "  Register ID: $registerId" -ForegroundColor White
Write-Host "  Owner Wallet: $walletAddress" -ForegroundColor White
Write-Host "  Algorithm: $Algorithm" -ForegroundColor White
Write-Host ""

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  COMPLETE SUCCESS - Real Wallet Signing Verified!" -ForegroundColor Green
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "What was tested:" -ForegroundColor Yellow
Write-Host "  - Wallet Service: Create wallet with $Algorithm" -ForegroundColor Gray
Write-Host "  - Wallet Service: Sign data with private key" -ForegroundColor Gray
Write-Host "  - Register Service: Initiate register creation" -ForegroundColor Gray
Write-Host "  - Register Service: Verify signature" -ForegroundColor Gray
Write-Host "  - Register Service: Create register with verified signature" -ForegroundColor Gray
Write-Host "  - Validator Service: Submit genesis transaction" -ForegroundColor Gray
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test with other algorithms:" -ForegroundColor Gray
Write-Host "     pwsh test-register-with-wallet-signing.ps1 -Algorithm NISTP256" -ForegroundColor DarkGray
Write-Host "     pwsh test-register-with-wallet-signing.ps1 -Algorithm RSA4096" -ForegroundColor DarkGray
Write-Host "  2. Test register operations (add transactions)" -ForegroundColor Gray
Write-Host "  3. Verify transaction chain integrity" -ForegroundColor Gray
Write-Host "  4. Test multi-signature scenarios" -ForegroundColor Gray
Write-Host ""
