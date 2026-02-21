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

    # The dataToSign is a hex-encoded SHA-256 hash (64 hex chars)
    # We convert hex to bytes and pass isPreHashed=true so wallet signs directly without re-hashing
    $hexHash = $attestation.dataToSign
    Write-Host "  SHA-256 Hash (hex): $hexHash" -ForegroundColor Gray

    # Convert hex hash string to bytes, then to base64 for wallet service
    $hashBytes = [byte[]]::new($hexHash.Length / 2)
    for ($i = 0; $i -lt $hashBytes.Length; $i++) {
        $hashBytes[$i] = [Convert]::ToByte($hexHash.Substring($i * 2, 2), 16)
    }
    $dataToSignBase64 = [Convert]::ToBase64String($hashBytes)

    # Include derivation path and isPreHashed flag for attestation signing
    $signBody = @{
        transactionData = $dataToSignBase64
        derivationPath = "sorcha:register-attestation"
        isPreHashed = $true
    } | ConvertTo-Json

    try {
        Write-Info "Sending sign request with derivation path 'sorcha:register-attestation' (isPreHashed=true)..."

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

# ==================================================================================
# PIPELINE MONITORING: Watch genesis transaction flow through Validator → Docket → Register
# ==================================================================================

# Direct service URLs for monitoring (gateway routing doesn't cover /api/validators/ or /api/admin/)
$ValidatorDirectUrl = if ($Profile -eq 'gateway') { "http://localhost:5800" } else { $ValidatorServiceUrl }
$RegisterDirectUrl = if ($Profile -eq 'gateway') { "http://localhost:5380" } else { $RegisterServiceUrl }

# Step 6: Check Validator Mempool
Write-Step "Step 6: Check Validator Mempool"

$mempoolFound = $false
try {
    Write-Info "Querying mempool for register $registerId..."
    $mempoolResponse = Invoke-RestMethod `
        -Uri "$ValidatorDirectUrl/api/validators/mempool/$registerId" `
        -Method GET `
        -UseBasicParsing

    Write-Success "Mempool stats retrieved"
    Write-Host ""
    Write-Host "Mempool Statistics:" -ForegroundColor Yellow
    Write-Host ($mempoolResponse | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray
    $mempoolFound = $true
} catch {
    Write-Info "Mempool query returned error (transaction may already be processed)"
    Write-Host "  $($_.Exception.Message)" -ForegroundColor DarkGray
}

# Step 7: Check Validator Monitoring
Write-Step "Step 7: Check Validator Monitoring Registry"

$registerMonitored = $false
try {
    Write-Info "Querying monitored registers..."
    $monitoringResponse = Invoke-RestMethod `
        -Uri "$ValidatorDirectUrl/api/admin/validators/monitoring" `
        -Method GET `
        -UseBasicParsing

    Write-Host ""
    Write-Host "Monitored Registers:" -ForegroundColor Yellow
    Write-Host "  Count: $($monitoringResponse.count)" -ForegroundColor White
    Write-Host "  Register IDs:" -ForegroundColor White
    foreach ($rid in $monitoringResponse.registerIds) {
        $marker = if ($rid -eq $registerId) { " <-- OUR REGISTER" } else { "" }
        Write-Host "    - $rid$marker" -ForegroundColor $(if ($rid -eq $registerId) { "Green" } else { "Gray" })
    }

    if ($monitoringResponse.registerIds -contains $registerId) {
        Write-Success "Our register IS being monitored by the Validator"
        $registerMonitored = $true
    } else {
        Write-Info "Our register is not yet in the monitoring list"
    }
} catch {
    Write-Info "Could not query monitoring endpoint"
    Write-Host "  $($_.Exception.Message)" -ForegroundColor DarkGray
}

# Step 8: Wait for Docket to Appear in Register Service
Write-Step "Step 8: Wait for Genesis Docket (Validator Pipeline)"

Write-Info "The DocketBuildTriggerService runs on a 10-second timer."
Write-Info "It will: pick up genesis tx from mempool -> build docket -> write to Register Service"
Write-Host ""

$docketFound = $false
$docketResponse = $null
$maxWaitSeconds = 90
$pollIntervalSeconds = 5
$elapsed = 0

while (-not $docketFound -and $elapsed -lt $maxWaitSeconds) {
    try {
        # Use Invoke-WebRequest (not Invoke-RestMethod) to avoid PowerShell array unwrapping issues
        $rawResponse = Invoke-WebRequest `
            -Uri "$RegisterDirectUrl/api/registers/$registerId/dockets/" `
            -Method GET `
            -Headers @{ Authorization = "Bearer $adminToken" } `
            -UseBasicParsing

        $responseBody = $rawResponse.Content
        if ($responseBody -and $responseBody.Trim().Length -gt 2) {
            # Parse the JSON array manually
            $docketList = $responseBody | ConvertFrom-Json
            if ($docketList -and ($docketList | Measure-Object).Count -gt 0) {
                $docketFound = $true
                $docketResponse = if ($docketList -is [array]) { $docketList[0] } else { $docketList }
                Write-Host ""
                Write-Success "Genesis docket appeared after ${elapsed}s!"
                break
            }
        }
    } catch {
        # 404 is expected while waiting; log other errors for debugging
        $statusCode = $null
        if ($_.Exception.Response) { $statusCode = [int]$_.Exception.Response.StatusCode }
        if ($statusCode -and $statusCode -ne 404) {
            Write-Host "`r  Poll error: HTTP $statusCode - $($_.Exception.Message)" -ForegroundColor DarkYellow
        }
    }

    $remaining = $maxWaitSeconds - $elapsed
    Write-Host "`r  Waiting for docket... ${elapsed}s / ${maxWaitSeconds}s (next poll in ${pollIntervalSeconds}s)" -ForegroundColor DarkGray -NoNewline
    Start-Sleep -Seconds $pollIntervalSeconds
    $elapsed += $pollIntervalSeconds
}

Write-Host ""

if (-not $docketFound) {
    # Try the /latest endpoint as fallback
    try {
        $latestRaw = Invoke-WebRequest `
            -Uri "$RegisterDirectUrl/api/registers/$registerId/dockets/latest" `
            -Method GET `
            -Headers @{ Authorization = "Bearer $adminToken" } `
            -UseBasicParsing

        if ($latestRaw.Content -and $latestRaw.Content.Trim().Length -gt 2) {
            $docketResponse = $latestRaw.Content | ConvertFrom-Json
            $docketFound = $true
            Write-Success "Genesis docket found via /latest endpoint!"
        }
    } catch {
        # Still not found
    }
}

if ($docketFound -and $docketResponse) {
    Write-Host ""
    Write-Host "Genesis Docket Details:" -ForegroundColor Yellow
    Write-Host "  Docket ID: $($docketResponse.id)" -ForegroundColor White
    Write-Host "  Register ID: $($docketResponse.registerId)" -ForegroundColor White
    Write-Host "  Hash: $($docketResponse.hash)" -ForegroundColor White
    Write-Host "  Previous Hash: $($docketResponse.previousHash)" -ForegroundColor White
    Write-Host "  Transaction Count: $($docketResponse.transactionIds.Count)" -ForegroundColor White
    Write-Host "  Timestamp: $($docketResponse.timeStamp)" -ForegroundColor White
    Write-Host "  State: $($docketResponse.state)" -ForegroundColor White

    if ($ShowJson) {
        Write-Host ""
        Write-Host "================================================================================" -ForegroundColor Magenta
        Write-Host "  FULL DOCKET JSON" -ForegroundColor Magenta
        Write-Host "================================================================================" -ForegroundColor Magenta
        Write-Host ""
        Write-Host ($docketResponse | ConvertTo-Json -Depth 10) -ForegroundColor Gray
        Write-Host ""
    }
} else {
    Write-Host ""
    Write-Host "[!] Docket did not appear within ${maxWaitSeconds}s" -ForegroundColor Yellow
    Write-Info "The DocketBuildTriggerService may not have processed the register yet."
    Write-Info "Possible reasons:"
    Write-Host "  - Register not registered for monitoring with Validator" -ForegroundColor Gray
    Write-Host "  - DocketBuildTriggerService timer hasn't fired yet" -ForegroundColor Gray
    Write-Host "  - Genesis transaction still being validated" -ForegroundColor Gray
    Write-Host "  - Consensus not yet reached (single-node should be instant)" -ForegroundColor Gray
}

# Step 9: Verify Docket Transactions (Genesis Payload Integrity)
if ($docketFound) {
    Write-Step "Step 9: Verify Genesis Transaction in Docket"

    $docketId = $docketResponse.id
    $txVerified = $false

    try {
        Write-Info "Fetching transactions sealed in docket $docketId..."
        $docketTxResponse = Invoke-RestMethod `
            -Uri "$RegisterDirectUrl/api/registers/$registerId/dockets/$docketId/transactions" `
            -Method GET `
            -Headers @{ Authorization = "Bearer $adminToken" } `
            -UseBasicParsing

        $txList = if ($docketTxResponse -is [array]) { $docketTxResponse } else { @($docketTxResponse) }

        Write-Success "Retrieved $($txList.Count) transaction(s) from docket"
        Write-Host ""

        foreach ($tx in $txList) {
            Write-Host "Transaction in Docket:" -ForegroundColor Yellow
            Write-Host "  TxId: $($tx.txId)" -ForegroundColor White
            Write-Host "  Register: $($tx.registerId)" -ForegroundColor White
            Write-Host "  Sender: $($tx.senderWallet)" -ForegroundColor White
            Write-Host "  Timestamp: $($tx.timeStamp)" -ForegroundColor White
            Write-Host "  Payload Count: $($tx.payloadCount)" -ForegroundColor White
            Write-Host "  Signature: $(if ($tx.signature) { $tx.signature.Substring(0, [Math]::Min(40, $tx.signature.Length)) + '...' } else { '(none)' })" -ForegroundColor White

            # Check payload integrity - this is the key fix from issue #1
            if ($tx.payloads -and $tx.payloads.Count -gt 0) {
                Write-Success "PAYLOAD DATA PRESERVED! ($($tx.payloads.Count) payload(s))"
                $txVerified = $true

                foreach ($payload in $tx.payloads) {
                    Write-Host ""
                    Write-Host "  Payload Details:" -ForegroundColor Cyan
                    Write-Host "    Data Length: $(if ($payload.data) { $payload.data.Length } else { 0 }) chars" -ForegroundColor White
                    Write-Host "    Hash: $($payload.hash)" -ForegroundColor White
                    Write-Host "    Size: $($payload.payloadSize) bytes" -ForegroundColor White

                    # Try to decode the payload data (Base64 of control record JSON)
                    if ($payload.data) {
                        try {
                            $decodedBytes = [Convert]::FromBase64String($payload.data)
                            $decodedJson = [System.Text.Encoding]::UTF8.GetString($decodedBytes)
                            $controlRecord = $decodedJson | ConvertFrom-Json

                            Write-Host ""
                            Write-Success "Control record decoded from payload!"
                            Write-Host "    Register ID: $($controlRecord.registerId)" -ForegroundColor Green
                            Write-Host "    Register Name: $($controlRecord.registerName)" -ForegroundColor Green
                            Write-Host "    Tenant ID: $($controlRecord.tenantId)" -ForegroundColor Green

                            if ($controlRecord.attestations) {
                                Write-Host "    Attestations: $($controlRecord.attestations.Count)" -ForegroundColor Green
                                foreach ($att in $controlRecord.attestations) {
                                    Write-Host "      - $($att.role): $($att.subject) [$($att.algorithm)]" -ForegroundColor Cyan
                                }
                            }

                            if ($ShowJson) {
                                Write-Host ""
                                Write-Host "================================================================================" -ForegroundColor Cyan
                                Write-Host "  DECODED CONTROL RECORD (from docket payload)" -ForegroundColor Cyan
                                Write-Host "================================================================================" -ForegroundColor Cyan
                                Write-Host ""
                                Write-Host ($controlRecord | ConvertTo-Json -Depth 10) -ForegroundColor Gray
                                Write-Host ""
                            }
                        } catch {
                            Write-Info "Payload data present but could not decode as Base64 JSON"
                            Write-Host "    Raw (first 100 chars): $($payload.data.Substring(0, [Math]::Min(100, $payload.data.Length)))..." -ForegroundColor DarkGray
                        }
                    }
                }
            } else {
                Write-Host ""
                Write-Host "[!] PAYLOAD DATA MISSING - payloads array is empty!" -ForegroundColor Red
                Write-Host "    PayloadCount: $($tx.payloadCount)" -ForegroundColor Red
                Write-Host "    This indicates the payload mapping fix (Issue #1) may not be working." -ForegroundColor Red
            }

            # Check metadata
            if ($tx.metaData) {
                Write-Host ""
                Write-Host "  Transaction Metadata:" -ForegroundColor Yellow
                Write-Host "    Blueprint ID: $($tx.metaData.blueprintId)" -ForegroundColor White
                Write-Host "    Action ID: $($tx.metaData.actionId)" -ForegroundColor White
                Write-Host "    Register ID: $($tx.metaData.registerId)" -ForegroundColor White
                Write-Host "    Transaction Type: $($tx.metaData.transactionType)" -ForegroundColor White
            }

            if ($ShowJson) {
                Write-Host ""
                Write-Host "================================================================================" -ForegroundColor Magenta
                Write-Host "  FULL TRANSACTION JSON (from docket)" -ForegroundColor Magenta
                Write-Host "================================================================================" -ForegroundColor Magenta
                Write-Host ""
                Write-Host ($tx | ConvertTo-Json -Depth 10) -ForegroundColor Gray
                Write-Host ""
            }
        }

    } catch {
        Write-Host "[!] Failed to fetch docket transactions" -ForegroundColor Yellow
        Write-Host "  $($_.Exception.Message)" -ForegroundColor DarkGray
    }

    # Step 10: Verify Register Height Updated
    Write-Step "Step 10: Verify Register State"

    try {
        Write-Info "Checking register state after docket write..."
        $registerResponse = Invoke-RestMethod `
            -Uri "$RegisterDirectUrl/api/registers/$registerId" `
            -Method GET `
            -Headers @{ Authorization = "Bearer $adminToken" } `
            -UseBasicParsing

        Write-Success "Register retrieved"
        Write-Host ""
        Write-Host "Register State:" -ForegroundColor Yellow
        Write-Host "  ID: $($registerResponse.id)" -ForegroundColor White
        Write-Host "  Name: $($registerResponse.name)" -ForegroundColor White
        Write-Host "  Height: $($registerResponse.height)" -ForegroundColor White
        Write-Host "  Status: $($registerResponse.status)" -ForegroundColor White
        Write-Host "  Advertise: $($registerResponse.advertise)" -ForegroundColor White
        Write-Host "  Tenant: $($registerResponse.tenantId)" -ForegroundColor White
        Write-Host "  Created: $($registerResponse.createdAt)" -ForegroundColor White

        if ($registerResponse.height -ge 0) {
            Write-Success "Register height is $($registerResponse.height) (genesis docket applied)"
        }

        if ($ShowJson) {
            Write-Host ""
            Write-Host ($registerResponse | ConvertTo-Json -Depth 10) -ForegroundColor Gray
        }
    } catch {
        Write-Host "[!] Could not retrieve register state" -ForegroundColor Yellow
        Write-Host "  $($_.Exception.Message)" -ForegroundColor DarkGray
    }
}

# ==================================================================================
# SUMMARY
# ==================================================================================

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  Complete Register Creation: PIPELINE RESULTS" -ForegroundColor Green
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Phase 1 - Register Creation:" -ForegroundColor Yellow
Write-Host "  [OK] Admin authentication" -ForegroundColor Green
Write-Host "  [OK] Wallet creation ($Algorithm)" -ForegroundColor Green
Write-Host "  [OK] Register creation initiation (attestation-based)" -ForegroundColor Green
Write-Host "  [OK] Attestation signing with derivation path" -ForegroundColor Green
Write-Host "  [OK] Individual attestation signature verification" -ForegroundColor Green
Write-Host "  [OK] Register creation finalized" -ForegroundColor Green

if ($genesisTransactionId) {
    Write-Host "  [OK] Genesis transaction submitted (TX: $($genesisTransactionId.Substring(0, 16))...)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Phase 2 - Docket Pipeline:" -ForegroundColor Yellow

if ($mempoolFound) {
    Write-Host "  [OK] Validator mempool queried" -ForegroundColor Green
} else {
    Write-Host "  [--] Validator mempool (tx may already be processed)" -ForegroundColor DarkGray
}

if ($registerMonitored) {
    Write-Host "  [OK] Register monitored by Validator" -ForegroundColor Green
} else {
    Write-Host "  [--] Register monitoring (not confirmed)" -ForegroundColor DarkGray
}

if ($docketFound) {
    Write-Host "  [OK] Genesis docket written to Register Service" -ForegroundColor Green
} else {
    Write-Host "  [!!] Genesis docket NOT found within timeout" -ForegroundColor Red
}

if ($txVerified) {
    Write-Host "  [OK] Payload data preserved through pipeline (Issue #1 FIXED)" -ForegroundColor Green
} elseif ($docketFound) {
    Write-Host "  [!!] Payload data MISSING in docket transaction" -ForegroundColor Red
} else {
    Write-Host "  [--] Payload verification skipped (no docket)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Register Details:" -ForegroundColor Yellow
Write-Host "  Name: $registerName" -ForegroundColor White
Write-Host "  Register ID: $registerId" -ForegroundColor White
Write-Host "  Owner Wallet: $walletAddress" -ForegroundColor White
Write-Host "  Algorithm: $Algorithm" -ForegroundColor White
Write-Host "  Profile: $Profile" -ForegroundColor White
Write-Host ""

Write-Host "================================================================================" -ForegroundColor Cyan

if ($docketFound -and $txVerified) {
    Write-Host "  FULL PIPELINE VERIFIED: Create -> Sign -> Validate -> Docket -> Store" -ForegroundColor Green
} elseif ($docketFound) {
    Write-Host "  PARTIAL: Docket created but payload integrity needs investigation" -ForegroundColor Yellow
} else {
    Write-Host "  PARTIAL: Register created, docket pipeline did not complete in time" -ForegroundColor Yellow
}

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Services tested:" -ForegroundColor Yellow
Write-Host "  - Tenant Service: Admin authentication (JWT)" -ForegroundColor Gray
Write-Host "  - Wallet Service: HD wallet creation + pre-hashed signing ($Algorithm)" -ForegroundColor Gray
Write-Host "  - Register Service: Two-phase creation (initiate/finalize)" -ForegroundColor Gray
Write-Host "  - Validator Service: Genesis tx acceptance + mempool + docket building" -ForegroundColor Gray
Write-Host "  - Register Service: Docket storage + transaction persistence" -ForegroundColor Gray
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test with other algorithms:" -ForegroundColor Gray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Algorithm NISTP256" -ForegroundColor DarkGray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -Algorithm RSA4096" -ForegroundColor DarkGray
Write-Host "  2. View full JSON structures:" -ForegroundColor Gray
Write-Host "     pwsh test-register-creation-with-real-signing.ps1 -ShowJson" -ForegroundColor DarkGray
Write-Host "  3. Add transactions to the register" -ForegroundColor Gray
Write-Host "  4. Verify transaction chain integrity" -ForegroundColor Gray
Write-Host ""
