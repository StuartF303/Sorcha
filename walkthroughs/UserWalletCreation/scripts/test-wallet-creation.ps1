<#
.SYNOPSIS
    Test wallet creation with various configurations
.DESCRIPTION
    Creates wallets with different algorithms and configurations to demonstrate:
    - ED25519 (recommended, fast)
    - NISTP256 (NIST compliance)
    - RSA4096 (maximum security)
    - 12 vs 24-word mnemonics
    - Wallet listing and verification
.PARAMETER Email
    User email for authentication
.PARAMETER Password
    User password for authentication
.PARAMETER WalletServiceUrl
    Direct Wallet Service URL (default: http://localhost:5000)
.PARAMETER TenantServiceUrl
    Direct Tenant Service URL for authentication (default: http://localhost:5110)
.PARAMETER TestAll
    Test all algorithms (ED25519, NISTP256, RSA4096)
.PARAMETER Algorithm
    Specific algorithm to test (ED25519, NISTP256, or RSA4096)
.PARAMETER WordCount
    Mnemonic word count: 12 or 24 (default: 12)
.EXAMPLE
    .\test-wallet-creation.ps1 -Email "alice@example.com" -Password "SecurePass123!" -TestAll
.EXAMPLE
    .\test-wallet-creation.ps1 -Email "alice@example.com" -Password "SecurePass123!" -Algorithm "NISTP256" -WordCount 24
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Email,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [Parameter(Mandatory = $false)]
    [string]$WalletServiceUrl = "http://localhost:5000",

    [Parameter(Mandatory = $false)]
    [string]$TenantServiceUrl = "http://localhost:5110",

    [Parameter(Mandatory = $false)]
    [switch]$TestAll,

    [Parameter(Mandatory = $false)]
    [ValidateSet("ED25519", "NISTP256", "RSA4096")]
    [string]$Algorithm = "ED25519",

    [Parameter(Mandatory = $false)]
    [ValidateSet(12, 24)]
    [int]$WordCount = 12
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Write-Section {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "  ✓ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Label, [string]$Value = "", [string]$Color = "Gray")
    if ($Value) {
        Write-Host "    $Label $Value" -ForegroundColor $Color
    } else {
        Write-Host "    $Label" -ForegroundColor $Color
    }
}

function Test-WalletCreation {
    param(
        [string]$Token,
        [string]$TestAlgorithm,
        [int]$TestWordCount,
        [int]$TestNumber = 1
    )

    Write-Section "Test $TestNumber`: Create $TestAlgorithm Wallet ($TestWordCount words)"

    $walletName = "$TestAlgorithm Test Wallet ($TestWordCount words)"

    $createRequest = @{
        name = $walletName
        algorithm = $TestAlgorithm
        wordCount = $TestWordCount
    }

    $startTime = Get-Date

    try {
        $response = Invoke-RestMethod `
            -Uri "$WalletServiceUrl/api/v1/wallets" `
            -Method POST `
            -Headers @{ Authorization = "Bearer $Token" } `
            -Body ($createRequest | ConvertTo-Json) `
            -ContentType "application/json"

        $duration = (Get-Date) - $startTime

        Write-Success "Wallet created in $([Math]::Round($duration.TotalMilliseconds))ms"
        Write-Info "  Address:" $response.wallet.address "Yellow"
        Write-Info "  Name:" $response.wallet.name
        Write-Info "  Algorithm:" $response.wallet.algorithm "Cyan"
        Write-Info "  Public Key Length:" "$($response.wallet.publicKey.Length) characters"
        Write-Info "  Mnemonic Words:" "$($response.mnemonicWords.Length) words" "Magenta"
        Write-Info "  Status:" $response.wallet.status
        Write-Host ""
        Write-Info "  First 3 words:" ($response.mnemonicWords[0..2] -join ", ") "DarkGray"
        Write-Host ""

        return $response
    }
    catch {
        Write-Host "  ❌ Failed to create wallet" -ForegroundColor Red
        Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Yellow
        return $null
    }
}

try {
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  Sorcha Wallet Creation Tests                                          ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

    # Authenticate user
    Write-Section "Step 1: User Authentication"
    Write-Info "Email:" $Email
    Write-Info "Wallet Service:" $WalletServiceUrl

    $loginRequest = @{
        email = $Email
        password = $Password
    }

    $tokenResponse = Invoke-RestMethod `
        -Uri "$TenantServiceUrl/api/auth/login" `
        -Method POST `
        -Body ($loginRequest | ConvertTo-Json) `
        -ContentType "application/json"

    $userToken = $tokenResponse.accessToken
    Write-Success "Authenticated successfully"
    Write-Info "Token expires in: $($tokenResponse.expiresIn) seconds"

    # List existing wallets
    Write-Section "Step 2: List Existing Wallets"

    $existingWallets = Invoke-RestMethod `
        -Uri "$WalletServiceUrl/api/v1/wallets" `
        -Method GET `
        -Headers @{ Authorization = "Bearer $userToken" }

    Write-Info "User currently has $($existingWallets.Count) wallet(s)"
    foreach ($w in $existingWallets) {
        Write-Info "  - $($w.name): $($w.address) ($($w.algorithm))" "" "DarkGray"
    }

    # Run wallet creation tests
    $results = @()

    if ($TestAll) {
        Write-Host ""
        Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
        Write-Host "║  Testing All Algorithms                                                ║" -ForegroundColor Yellow
        Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Yellow

        $testNumber = 1

        # ED25519 with 12 words
        $result = Test-WalletCreation -Token $userToken -TestAlgorithm "ED25519" -TestWordCount 12 -TestNumber $testNumber++
        if ($result) { $results += $result }

        # ED25519 with 24 words
        $result = Test-WalletCreation -Token $userToken -TestAlgorithm "ED25519" -TestWordCount 24 -TestNumber $testNumber++
        if ($result) { $results += $result }

        # NISTP256 with 12 words
        $result = Test-WalletCreation -Token $userToken -TestAlgorithm "NISTP256" -TestWordCount 12 -TestNumber $testNumber++
        if ($result) { $results += $result }

        # NISTP256 with 24 words
        $result = Test-WalletCreation -Token $userToken -TestAlgorithm "NISTP256" -TestWordCount 24 -TestNumber $testNumber++
        if ($result) { $results += $result }

        # RSA4096 with 12 words
        $result = Test-WalletCreation -Token $userToken -TestAlgorithm "RSA4096" -TestWordCount 12 -TestNumber $testNumber++
        if ($result) { $results += $result }

        # RSA4096 with 24 words
        $result = Test-WalletCreation -Token $userToken -TestAlgorithm "RSA4096" -TestWordCount 24 -TestNumber $testNumber++
        if ($result) { $results += $result }

    } else {
        # Test single configuration
        $result = Test-WalletCreation -Token $userToken -TestAlgorithm $Algorithm -TestWordCount $WordCount -TestNumber 1
        if ($result) { $results += $result }
    }

    # Summary
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  ✅ TESTS COMPLETE                                                     ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""

    Write-Host "Summary:" -ForegroundColor Yellow
    Write-Info "  Tests run:" $results.Count
    Write-Info "  Wallets created:" $results.Count
    Write-Host ""

    # Algorithm comparison table
    if ($TestAll) {
        Write-Host "Algorithm Comparison:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  ┌─────────────┬──────────────┬──────────────┬─────────────────────────────────────┐" -ForegroundColor Gray
        Write-Host "  │ Algorithm   │ Mnemonic     │ Public Key   │ Wallet Address                      │" -ForegroundColor Gray
        Write-Host "  ├─────────────┼──────────────┼──────────────┼─────────────────────────────────────┤" -ForegroundColor Gray

        foreach ($result in $results) {
            $alg = $result.wallet.algorithm.PadRight(11)
            $words = "$($result.mnemonicWords.Length) words".PadRight(12)
            $pkLen = "$($result.wallet.publicKey.Length) chars".PadRight(12)
            $addr = $result.wallet.address.Substring(0, [Math]::Min(35, $result.wallet.address.Length))

            Write-Host "  │ $alg │ $words │ $pkLen │ $addr... │" -ForegroundColor White
        }

        Write-Host "  └─────────────┴──────────────┴──────────────┴─────────────────────────────────────┘" -ForegroundColor Gray
        Write-Host ""
    }

    # Verify final wallet count
    Write-Section "Final Verification: List All Wallets"

    $finalWallets = Invoke-RestMethod `
        -Uri "$WalletServiceUrl/api/v1/wallets" `
        -Method GET `
        -Headers @{ Authorization = "Bearer $userToken" }

    Write-Success "User now has $($finalWallets.Count) wallet(s)"
    Write-Host ""

    $grouped = $finalWallets | Group-Object -Property algorithm
    foreach ($group in $grouped) {
        Write-Info "  $($group.Name):" "$($group.Count) wallet(s)" "Cyan"
        foreach ($w in $group.Group) {
            Write-Info "    - $($w.name)" "$($w.address.Substring(0, [Math]::Min(40, $w.address.Length)))..." "DarkGray"
        }
    }

    Write-Host ""
    Write-Host "Recommendations:" -ForegroundColor Yellow
    Write-Host "  • ED25519 with 12 words: ✅ Best for most use cases (fast, secure)" -ForegroundColor Green
    Write-Host "  • NISTP256: Use when NIST compliance is required" -ForegroundColor Cyan
    Write-Host "  • RSA4096: Use when maximum key size is needed (slower)" -ForegroundColor Yellow
    Write-Host "  • 24 words: Use for maximum security (twice the entropy)" -ForegroundColor Yellow
    Write-Host ""

}
catch {
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║  ❌ TEST FAILED                                                        ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
    Write-Host ""

    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""

    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  • Verify Wallet Service is running: curl $WalletServiceUrl/health" -ForegroundColor White
    Write-Host "  • Check authentication token is valid" -ForegroundColor White
    Write-Host "  • Review service logs: docker-compose logs wallet-service" -ForegroundColor White
    Write-Host ""

    exit 1
}
