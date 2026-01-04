<#
.SYNOPSIS
    Create local user via bootstrap and create wallet - End-to-end test with timing metrics
.DESCRIPTION
    Uses the bootstrap endpoint to create:
    - Organization with unique subdomain
    - Local user with password authentication
    - Default wallet with specified algorithm

    Collects detailed timing metrics for each operation.
.PARAMETER UserEmail
    User email address (required)
.PARAMETER UserPassword
    User password (required)
.PARAMETER UserDisplayName
    User display name (required)
.PARAMETER OrgName
    Organization name (required)
.PARAMETER OrgSubdomain
    Organization subdomain (required, must be unique)
.PARAMETER WalletName
    Wallet display name (default: "Primary Wallet")
.PARAMETER WalletAlgorithm
    Crypto algorithm: ED25519, NISTP256, or RSA4096 (default: ED25519)
.PARAMETER MnemonicWordCount
    BIP39 word count: 12 or 24 (default: 12)
.PARAMETER TenantServiceUrl
    Tenant Service URL (default: http://localhost:5110)
.PARAMETER WalletServiceUrl
    Wallet Service URL (default: http://localhost)
.PARAMETER OutputMetrics
    Output detailed JSON metrics file
.EXAMPLE
    .\test-bootstrap-user-wallet.ps1 `
        -UserEmail "alice@example.com" `
        -UserPassword "SecurePass123!" `
        -UserDisplayName "Alice Johnson" `
        -OrgName "Alice Corp" `
        -OrgSubdomain "alice-test-001" `
        -OutputMetrics "metrics-alice.json"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$UserEmail,

    [Parameter(Mandatory = $true)]
    [string]$UserPassword,

    [Parameter(Mandatory = $true)]
    [string]$UserDisplayName,

    [Parameter(Mandatory = $true)]
    [string]$OrgName,

    [Parameter(Mandatory = $true)]
    [string]$OrgSubdomain,

    [Parameter(Mandatory = $false)]
    [string]$WalletName = "Primary Wallet",

    [Parameter(Mandatory = $false)]
    [ValidateSet("ED25519", "NISTP256", "RSA4096")]
    [string]$WalletAlgorithm = "ED25519",

    [Parameter(Mandatory = $false)]
    [ValidateSet(12, 24)]
    [int]$MnemonicWordCount = 12,

    [Parameter(Mandatory = $false)]
    [string]$TenantServiceUrl = "http://localhost:5110",

    [Parameter(Mandatory = $false)]
    [string]$WalletServiceUrl = "http://localhost",

    [Parameter(Mandatory = $false)]
    [string]$OutputMetrics
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Metrics collection
$metrics = @{
    testId = [Guid]::NewGuid().ToString()
    timestamp = (Get-Date -Format "o")
    configuration = @{
        userEmail = $UserEmail
        orgName = $OrgName
        orgSubdomain = $OrgSubdomain
        walletAlgorithm = $WalletAlgorithm
        mnemonicWordCount = $MnemonicWordCount
    }
    operations = @()
    totalDurationMs = 0
    success = $false
}

$testStartTime = Get-Date

# Helper to measure operation timing
function Measure-Operation {
    param(
        [string]$Name,
        [scriptblock]$ScriptBlock
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $result = & $ScriptBlock
        $sw.Stop()

        $operationMetric = @{
            name = $Name
            durationMs = [int]$sw.ElapsedMilliseconds
            success = $true
            timestamp = (Get-Date -Format "o")
        }

        $metrics.operations += $operationMetric

        Write-Host "  [OK] $Name" -ForegroundColor Green
        Write-Host "       Duration: $($sw.ElapsedMilliseconds)ms" -ForegroundColor Gray

        return $result
    }
    catch {
        $sw.Stop()

        $operationMetric = @{
            name = $Name
            durationMs = [int]$sw.ElapsedMilliseconds
            success = $false
            error = $_.Exception.Message
            timestamp = (Get-Date -Format "o")
        }

        $metrics.operations += $operationMetric

        Write-Host "  [FAIL] $Name" -ForegroundColor Red
        Write-Host "         Duration: $($sw.ElapsedMilliseconds)ms" -ForegroundColor Gray
        Write-Host "         Error: $($_.Exception.Message)" -ForegroundColor Yellow

        throw
    }
}

# Helper for API requests
function Invoke-ApiRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [object]$Body = $null
    )

    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $Headers
        ContentType = "application/json"
    }

    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }

    return Invoke-RestMethod @params
}

try {
    Write-Host ""
    Write-Host "========================================================================" -ForegroundColor Cyan
    Write-Host " Bootstrap User + Wallet Creation - End-to-End Test" -ForegroundColor Cyan
    Write-Host "========================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Test ID: $($metrics.testId)" -ForegroundColor Gray
    Write-Host ""

    #
    # STEP 1: Bootstrap Organization and User
    #
    Write-Host "Step 1: Bootstrap Organization and Local User" -ForegroundColor Cyan

    $bootstrapRequest = @{
        organizationName = $OrgName
        organizationSubdomain = $OrgSubdomain
        adminEmail = $UserEmail
        adminName = $UserDisplayName
        adminPassword = $UserPassword
    }

    $bootstrapResponse = Measure-Operation -Name "Bootstrap Organization + User" -ScriptBlock {
        Invoke-ApiRequest `
            -Uri "$TenantServiceUrl/api/tenants/bootstrap" `
            -Method POST `
            -Body $bootstrapRequest
    }

    $orgId = $bootstrapResponse.organizationId
    $userId = $bootstrapResponse.adminUserId
    $userEmail = $bootstrapResponse.adminEmail

    Write-Host ""
    Write-Host "   Organization: $($bootstrapResponse.organizationName) ($orgId)" -ForegroundColor Gray
    Write-Host "   User: $userEmail ($userId)" -ForegroundColor Gray
    Write-Host ""

    #
    # STEP 2: Authenticate User (Login to get JWT token)
    #
    Write-Host "Step 2: Authenticate User" -ForegroundColor Cyan

    $loginRequest = @{
        email = $UserEmail
        password = $UserPassword
    }

    $loginResponse = Measure-Operation -Name "User Login (JWT generation)" -ScriptBlock {
        Invoke-ApiRequest `
            -Uri "$TenantServiceUrl/api/auth/login" `
            -Method POST `
            -Body $loginRequest
    }

    $accessToken = $loginResponse.access_token

    Write-Host ""
    Write-Host "   Token Type: $($loginResponse.token_type)" -ForegroundColor Gray
    Write-Host "   Expires In: $($loginResponse.expires_in) seconds" -ForegroundColor Gray
    Write-Host ""

    #
    # STEP 3: Create Wallet
    #
    Write-Host "Step 3: Create Wallet" -ForegroundColor Cyan

    $createWalletRequest = @{
        name = $WalletName
        algorithm = $WalletAlgorithm
        wordCount = $MnemonicWordCount
    }

    $walletResponse = Measure-Operation -Name "Create Wallet" -ScriptBlock {
        Invoke-ApiRequest `
            -Uri "$WalletServiceUrl/api/v1/wallets" `
            -Method POST `
            -Headers @{ Authorization = "Bearer $accessToken" } `
            -Body $createWalletRequest
    }

    Write-Host ""
    Write-Host "   Wallet: $($walletResponse.wallet.address)" -ForegroundColor Gray
    Write-Host "   Algorithm: $($walletResponse.wallet.algorithm)" -ForegroundColor Gray
    Write-Host "   Public Key: $($walletResponse.wallet.publicKey.Substring(0, [Math]::Min(64, $walletResponse.wallet.publicKey.Length)))..." -ForegroundColor Gray
    Write-Host ""

    #
    # STEP 4: Verify Wallet Ownership
    #
    Write-Host "Step 4: Verify Wallet Ownership" -ForegroundColor Cyan

    $wallets = Measure-Operation -Name "List User Wallets" -ScriptBlock {
        Invoke-ApiRequest `
            -Uri "$WalletServiceUrl/api/v1/wallets" `
            -Method GET `
            -Headers @{ Authorization = "Bearer $accessToken" }
    }

    Write-Host "   User has $($wallets.Count) wallet(s)" -ForegroundColor Gray
    Write-Host ""

    #
    # SUCCESS
    #
    $testEndTime = Get-Date
    $metrics.totalDurationMs = [int]($testEndTime - $testStartTime).TotalMilliseconds
    $metrics.success = $true
    $metrics.results = @{
        organizationId = $orgId
        userId = $userId
        walletAddress = $walletResponse.wallet.address
        walletAlgorithm = $walletResponse.wallet.algorithm
        mnemonicWords = $walletResponse.mnemonicWords.Count
    }

    Write-Host "========================================================================" -ForegroundColor Green
    Write-Host " [PASS] TEST COMPLETE - ALL OPERATIONS SUCCESSFUL" -ForegroundColor Green
    Write-Host "========================================================================" -ForegroundColor Green
    Write-Host ""

    Write-Host "PERFORMANCE METRICS:" -ForegroundColor Cyan
    Write-Host "   Total Duration: $($metrics.totalDurationMs)ms" -ForegroundColor White
    foreach ($op in $metrics.operations) {
        if ($op.success) {
            Write-Host "   - $($op.name): $($op.durationMs)ms" -ForegroundColor Gray
        }
    }
    Write-Host ""

    Write-Host "RESULTS:" -ForegroundColor Cyan
    Write-Host "   Organization: $($bootstrapResponse.organizationName) ($orgId)" -ForegroundColor White
    Write-Host "   User: $userEmail ($userId)" -ForegroundColor White
    Write-Host "   Wallet: $($walletResponse.wallet.address)" -ForegroundColor White
    Write-Host ""

    # Save metrics if requested
    if ($OutputMetrics) {
        $metrics | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputMetrics -Encoding UTF8
        Write-Host "Metrics saved to: $OutputMetrics" -ForegroundColor Cyan
        Write-Host ""
    }

    return $metrics
}
catch {
    $testEndTime = Get-Date
    $metrics.totalDurationMs = [int]($testEndTime - $testStartTime).TotalMilliseconds
    $metrics.success = $false
    $metrics.error = $_.Exception.Message

    Write-Host ""
    Write-Host "========================================================================" -ForegroundColor Red
    Write-Host " [FAIL] TEST FAILED" -ForegroundColor Red
    Write-Host "========================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""

    # Save metrics even on failure
    if ($OutputMetrics) {
        $metrics | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputMetrics -Encoding UTF8
        Write-Host "Error metrics saved to: $OutputMetrics" -ForegroundColor Yellow
    }

    exit 1
}
