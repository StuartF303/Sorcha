<#
.SYNOPSIS
    Create all test users from test-users.json
.DESCRIPTION
    Batch create all test users defined in data/test-users.json file.
    Each user will be created with their specified wallet configuration.
.PARAMETER DataFile
    Path to test-users.json file (default: ../data/test-users.json)
.PARAMETER TenantServiceUrl
    Direct Tenant Service URL (default: http://localhost:5110)
.PARAMETER WalletServiceUrl
    Direct Wallet Service URL (default: http://localhost:5000)
.PARAMETER AdminEmail
    Admin email (default: stuart.mackintosh@sorcha.dev)
.PARAMETER AdminPassword
    Admin password (default: SorchaDev2025!)
.PARAMETER SaveMnemonics
    Save all mnemonics to files in output directory (TESTING ONLY!)
.PARAMETER OutputDir
    Directory for saving mnemonics (default: ../output)
.EXAMPLE
    .\create-all-test-users.ps1
.EXAMPLE
    .\create-all-test-users.ps1 -SaveMnemonics -OutputDir "C:\temp\sorcha-test"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$DataFile = "../data/test-users.json",

    [Parameter(Mandatory = $false)]
    [string]$TenantServiceUrl = "http://localhost:5110",

    [Parameter(Mandatory = $false)]
    [string]$WalletServiceUrl = "http://localhost:5000",

    [Parameter(Mandatory = $false)]
    [string]$AdminEmail = "admin@sorcha.local",

    [Parameter(Mandatory = $false)]
    [string]$AdminPassword = "Dev_Pass_2025!",

    [Parameter(Mandatory = $false)]
    [switch]$SaveMnemonics,

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = "../output"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dataFilePath = Join-Path $scriptDir $DataFile

if (-not (Test-Path $dataFilePath)) {
    Write-Host "❌ Data file not found: $dataFilePath" -ForegroundColor Red
    exit 1
}

# Read test users configuration
$config = Get-Content $dataFilePath -Raw | ConvertFrom-Json

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Batch Create Test Users                                               ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Data file: $dataFilePath" -ForegroundColor Gray
Write-Host "  Organization: $($config.organizationSubdomain)" -ForegroundColor Gray
Write-Host "  Users to create: $($config.users.Count)" -ForegroundColor Gray
Write-Host "  Save mnemonics: $SaveMnemonics" -ForegroundColor Gray
Write-Host ""

# Create output directory if saving mnemonics
if ($SaveMnemonics) {
    $outputDirPath = Join-Path $scriptDir $OutputDir
    if (-not (Test-Path $outputDirPath)) {
        New-Item -ItemType Directory -Path $outputDirPath -Force | Out-Null
    }
    Write-Host "  ⚠️  Mnemonics will be saved to: $outputDirPath" -ForegroundColor Yellow
    Write-Host ""
}

# Track results
$results = @{
    success = @()
    failed = @()
}

# Process each user
$userNumber = 1
foreach ($user in $config.users) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "User $userNumber of $($config.users.Count): $($user.displayName) ($($user.email))" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Description: $($user.description)" -ForegroundColor Gray
    Write-Host "  Roles: $($user.roles -join ', ')" -ForegroundColor Gray
    Write-Host "  Wallet Algorithm: $($user.walletAlgorithm)" -ForegroundColor Gray
    Write-Host "  Mnemonic Words: $($user.mnemonicWordCount)" -ForegroundColor Gray
    Write-Host ""

    try {
        # Build parameters for phase1 script
        $params = @(
            "-UserEmail", $user.email
            "-UserDisplayName", $user.displayName
            "-UserPassword", $user.password
            "-UserRoles", ($user.roles -join ",")
            "-WalletName", $user.walletName
            "-WalletAlgorithm", $user.walletAlgorithm
            "-MnemonicWordCount", $user.mnemonicWordCount
            "-OrgSubdomain", $config.organizationSubdomain
            "-TenantServiceUrl", $TenantServiceUrl
            "-WalletServiceUrl", $WalletServiceUrl
            "-AdminEmail", $AdminEmail
            "-AdminPassword", $AdminPassword
        )

        # Add mnemonic save path if requested
        if ($SaveMnemonics) {
            $mnemonicFile = Join-Path $outputDirPath "$($user.email.Replace('@', '_at_').Replace('.', '_'))-mnemonic.json"
            $params += @("-SaveMnemonicPath", $mnemonicFile)
        }

        # Call phase1 script
        $phase1Script = Join-Path $scriptDir "phase1-create-user-wallet.ps1"
        & $phase1Script @params

        $results.success += @{
            email = $user.email
            displayName = $user.displayName
        }

        Write-Host ""
        Write-Host "  ✅ $($user.displayName) created successfully" -ForegroundColor Green
        Write-Host ""

    }
    catch {
        $results.failed += @{
            email = $user.email
            displayName = $user.displayName
            error = $_.Exception.Message
        }

        Write-Host ""
        Write-Host "  ❌ Failed to create $($user.displayName)" -ForegroundColor Red
        Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host ""

        # Ask if we should continue
        if ($userNumber -lt $config.users.Count) {
            $continue = Read-Host "Continue with remaining users? (Y/n)"
            if ($continue -eq 'n' -or $continue -eq 'N') {
                Write-Host "  ⚠️  Aborting batch creation" -ForegroundColor Yellow
                break
            }
        }
    }

    $userNumber++
}

# Final summary
Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  BATCH CREATION SUMMARY                                                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "Results:" -ForegroundColor Yellow
Write-Host "  ✅ Successfully created: $($results.success.Count)" -ForegroundColor Green
Write-Host "  ❌ Failed: $($results.failed.Count)" -ForegroundColor Red
Write-Host ""

if ($results.success.Count -gt 0) {
    Write-Host "Successfully created users:" -ForegroundColor Green
    foreach ($user in $results.success) {
        Write-Host "  • $($user.displayName) ($($user.email))" -ForegroundColor White
    }
    Write-Host ""
}

if ($results.failed.Count -gt 0) {
    Write-Host "Failed users:" -ForegroundColor Red
    foreach ($user in $results.failed) {
        Write-Host "  • $($user.displayName) ($($user.email))" -ForegroundColor White
        Write-Host "    Error: $($user.error)" -ForegroundColor Yellow
    }
    Write-Host ""
}

if ($SaveMnemonics -and $results.success.Count -gt 0) {
    Write-Host "⚠️  SECURITY WARNING:" -ForegroundColor Red
    Write-Host "  Mnemonic phrases saved to: $outputDirPath" -ForegroundColor Yellow
    Write-Host "  DELETE THESE FILES AFTER TESTING!" -ForegroundColor Yellow
    Write-Host "  Never commit mnemonic files to source control!" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  • Test user logins with test-user-login.ps1" -ForegroundColor White
Write-Host "  • Verify wallets with test-wallet-creation.ps1" -ForegroundColor White
Write-Host "  • Proceed to Phase 2: Multi-user blueprint scenarios" -ForegroundColor White
Write-Host ""

# Exit with appropriate code
if ($results.failed.Count -gt 0) {
    exit 1
} else {
    exit 0
}
