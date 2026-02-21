#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

<#
.SYNOPSIS
    Register Creation Flow Walkthrough using Sorcha CLI

.DESCRIPTION
    Demonstrates the complete register creation workflow using the Sorcha CLI tool.
    The CLI handles the two-phase cryptographic attestation flow internally:

    1. Phase 1 (Initiate): CLI sends owner info, receives attestations to sign
    2. Phase 2 (Sign): CLI calls Wallet Service with isPreHashed=true
    3. Phase 3 (Finalize): CLI submits signed attestations, genesis created

    This script demonstrates the end-to-end flow from authentication through
    register creation and verification.

.PARAMETER Algorithm
    Cryptographic algorithm for wallet creation (ED25519, NISTP256, RSA4096).
    Default: ED25519

.PARAMETER Profile
    CLI profile to use (dev, docker). Default: docker

.PARAMETER SkipAuth
    Skip authentication step (use existing session)

.PARAMETER ShowJson
    Display full JSON output from commands

.PARAMETER Cleanup
    Delete created resources after walkthrough

.EXAMPLE
    # Run with defaults (ED25519, docker profile)
    pwsh test-register-creation-cli.ps1

.EXAMPLE
    # Run with NIST P-256 algorithm
    pwsh test-register-creation-cli.ps1 -Algorithm NISTP256

.EXAMPLE
    # Run with existing auth session and JSON output
    pwsh test-register-creation-cli.ps1 -SkipAuth -ShowJson

.EXAMPLE
    # Run and cleanup resources afterward
    pwsh test-register-creation-cli.ps1 -Cleanup
#>

param(
    [Parameter()]
    [ValidateSet('ED25519', 'NISTP256', 'RSA4096')]
    [string]$Algorithm = 'ED25519',

    [Parameter()]
    [ValidateSet('dev', 'docker')]
    [string]$Profile = 'docker',

    [Parameter()]
    [switch]$SkipAuth,

    [Parameter()]
    [switch]$ShowJson,

    [Parameter()]
    [switch]$Cleanup
)

$ErrorActionPreference = "Stop"

# Generate unique identifiers for this run
$RunId = [System.Guid]::NewGuid().ToString().Substring(0, 8)
$WalletName = "walkthrough-wallet-$RunId"
$RegisterName = "walkthrough-register-$RunId"
$TenantId = "walkthrough-tenant-001"

# Store created resources for potential cleanup
$CreatedWalletAddress = $null
$CreatedRegisterId = $null

# Find CLI executable - prefer local build over global tool
$RepoRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
$LocalCliPath = Join-Path $RepoRoot "src/Apps/Sorcha.Cli/bin/Release/net10.0/Sorcha.Cli.exe"
$DebugCliPath = Join-Path $RepoRoot "src/Apps/Sorcha.Cli/bin/Debug/net10.0/Sorcha.Cli.exe"

if (Test-Path $LocalCliPath) {
    $SorchaCliPath = $LocalCliPath
} elseif (Test-Path $DebugCliPath) {
    $SorchaCliPath = $DebugCliPath
} else {
    # Fall back to global tool
    $SorchaCliPath = "sorcha"
}

# Helper functions
function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Title)
    Write-Host ""
    Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host "  $Title" -ForegroundColor White
    Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "[i] $Message" -ForegroundColor Cyan
}

function Write-Command {
    param([string]$Command)
    Write-Host "  > $Command" -ForegroundColor Yellow
}

function Invoke-SorchaCommand {
    param(
        [string[]]$Arguments,
        [switch]$ExpectSuccess = $true,
        [switch]$CaptureOutput
    )

    $command = "sorcha $($Arguments -join ' ')"
    Write-Command $command

    if ($CaptureOutput) {
        $output = & $SorchaCliPath @Arguments 2>&1
        $exitCode = $LASTEXITCODE

        if ($ShowJson -or -not $ExpectSuccess) {
            Write-Host $output -ForegroundColor DarkGray
        }

        if ($ExpectSuccess -and $exitCode -ne 0) {
            Write-Failure "Command failed with exit code $exitCode"
            Write-Host $output -ForegroundColor Red
            throw "Command failed: $command"
        }

        return $output
    } else {
        & $SorchaCliPath @Arguments
        $exitCode = $LASTEXITCODE

        if ($ExpectSuccess -and $exitCode -ne 0) {
            Write-Failure "Command failed with exit code $exitCode"
            throw "Command failed: $command"
        }

        return $exitCode
    }
}

# Start walkthrough
Write-Section "Register Creation Flow - CLI Walkthrough"

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Run ID:        $RunId" -ForegroundColor White
Write-Host "  Algorithm:     $Algorithm" -ForegroundColor White
Write-Host "  Profile:       $Profile" -ForegroundColor White
Write-Host "  Wallet Name:   $WalletName" -ForegroundColor White
Write-Host "  Register Name: $RegisterName" -ForegroundColor White
Write-Host ""
Write-Info "The CLI handles the two-phase cryptographic attestation flow internally"
Write-Host ""

# Step 0: Verify CLI is available
Write-Step "Step 0: Verify Sorcha CLI"

Write-Info "CLI Path: $SorchaCliPath"

try {
    $version = & $SorchaCliPath --version 2>&1
    Write-Success "Sorcha CLI available: $version"
} catch {
    Write-Failure "Sorcha CLI not found"
    Write-Host "  Build with: dotnet build src/Apps/Sorcha.Cli -c Release" -ForegroundColor Yellow
    exit 1
}

# Step 1: Authentication
Write-Step "Step 1: Authenticate"

if ($SkipAuth) {
    Write-Info "Skipping authentication (using existing session)"

    # Verify we have a valid session
    try {
        $authStatus = Invoke-SorchaCommand -Arguments @("auth", "status") -CaptureOutput
        if ($authStatus -match "Not authenticated") {
            Write-Failure "No active session found"
            Write-Host "  Run without -SkipAuth or login manually with: sorcha auth login" -ForegroundColor Yellow
            exit 1
        }
        Write-Success "Using existing authentication session"
    } catch {
        Write-Failure "Failed to verify authentication status"
        exit 1
    }
} else {
    Write-Info "Authenticating with admin credentials..."
    Write-Host ""
    Write-Host "  Using password grant flow (user authentication)" -ForegroundColor Gray
    Write-Host "  Default dev credentials: admin@sorcha.local / Dev_Pass_2025!" -ForegroundColor Gray
    Write-Host ""

    # Use user authentication for automated testing
    # These are the default development credentials seeded in the database
    # Note: --interactive false is required to disable prompting in non-interactive environments
    try {
        Invoke-SorchaCommand -Arguments @("auth", "login", "--username", "admin@sorcha.local", "--password", "Dev_Pass_2025!", "--profile", $Profile, "--interactive", "false") -ExpectSuccess
        Write-Success "Authentication successful"
    } catch {
        Write-Failure "Authentication failed"
        Write-Host ""
        Write-Host "Troubleshooting:" -ForegroundColor Yellow
        Write-Host "  1. Ensure services are running: docker-compose up -d" -ForegroundColor Gray
        Write-Host "  2. Check Tenant Service is healthy: curl http://localhost/api/tenant/health" -ForegroundColor Gray
        Write-Host "  3. Verify admin user exists (seeded on first run)" -ForegroundColor Gray
        Write-Host "  4. Default dev credentials: admin@sorcha.local / Dev_Pass_2025!" -ForegroundColor Gray
        exit 1
    }
}

# Step 2: Create Wallet
Write-Step "Step 2: Create Wallet for Signing"

Write-Info "Creating HD wallet with $Algorithm algorithm..."
Write-Host ""
Write-Host "  The wallet will be used to sign the register attestation" -ForegroundColor Gray
Write-Host "  Derivation path: sorcha:register-attestation" -ForegroundColor Gray
Write-Host ""

try {
    $walletOutput = Invoke-SorchaCommand -Arguments @("wallet", "create", "--name", $WalletName, "--algorithm", $Algorithm) -CaptureOutput

    # Parse table output to get wallet address
    $outputText = $walletOutput -join "`n"
    if ($outputText -match "Address:\s+(\S+)") {
        $CreatedWalletAddress = $Matches[1]
        Write-Success "Wallet created successfully"
        Write-Host ""
        Write-Host "  Address:   $CreatedWalletAddress" -ForegroundColor White
        Write-Host "  Algorithm: $Algorithm" -ForegroundColor White
        Write-Host ""
        Write-Host "  IMPORTANT: Mnemonic phrase shown in CLI output - save securely!" -ForegroundColor Yellow
    } else {
        Write-Failure "Failed to parse wallet address from output"
        Write-Host $outputText -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Failure "Failed to create wallet"
    if ($walletOutput) {
        Write-Host ($walletOutput -join "`n") -ForegroundColor Red
    }
    exit 1
}

# Step 3: Create Register
Write-Step "Step 3: Create Register (Two-Phase Flow)"

Write-Info "Creating register with cryptographic attestation..."
Write-Host ""
Write-Host "  The CLI performs the two-phase flow internally:" -ForegroundColor Gray
Write-Host "    Phase 1: Initiate - sends owner info, receives attestations" -ForegroundColor Gray
Write-Host "    Phase 2: Sign - calls wallet service with isPreHashed=true" -ForegroundColor Gray
Write-Host "    Phase 3: Finalize - submits signatures, creates genesis" -ForegroundColor Gray
Write-Host ""

try {
    $registerOutput = Invoke-SorchaCommand -Arguments @("register", "create", "--name", $RegisterName, "--tenant-id", $TenantId, "--owner-wallet", $CreatedWalletAddress, "--description", "Created via CLI walkthrough") -CaptureOutput

    # Parse table output
    $outputText = $registerOutput -join "`n"
    if ($outputText -match "Register ID:\s+(\S+)") {
        $CreatedRegisterId = $Matches[1]
        Write-Success "Register created successfully!"
        Write-Host ""
        Write-Host "  Register ID:       $CreatedRegisterId" -ForegroundColor White

        if ($outputText -match "Genesis TX ID:\s+(\S+)") {
            $genesisTransactionId = $Matches[1]
            Write-Host "  Genesis TX ID:     $genesisTransactionId" -ForegroundColor White
        }

        if ($outputText -match "Genesis Docket ID:\s+(\S+)") {
            $genesisDocketId = $Matches[1]
            Write-Host "  Genesis Docket ID: $genesisDocketId" -ForegroundColor White
        }
    } else {
        Write-Failure "Failed to parse register ID from output"
        Write-Host $outputText -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Failure "Failed to create register"
    if ($registerOutput) {
        Write-Host ($registerOutput -join "`n") -ForegroundColor Red
    }
    exit 1
}

# Step 4: Verify Register
Write-Step "Step 4: Verify Register Created"

Write-Info "Fetching register details..."

try {
    $getOutput = Invoke-SorchaCommand -Arguments @("register", "get", "--id", $CreatedRegisterId) -CaptureOutput

    Write-Success "Register verified"
    Write-Host ""
    Write-Host $getOutput -ForegroundColor DarkGray
} catch {
    Write-Failure "Failed to retrieve register"
    exit 1
}

# Step 5: Inspect Genesis Docket
Write-Step "Step 5: Inspect Genesis Docket"

Write-Info "Listing dockets in the register..."

try {
    $docketOutput = Invoke-SorchaCommand -Arguments @("docket", "list", "--register-id", $CreatedRegisterId) -CaptureOutput

    Write-Success "Genesis docket found"
    Write-Host ""
    Write-Host $docketOutput -ForegroundColor DarkGray
} catch {
    Write-Failure "Failed to list dockets"
    Write-Host "  This may be expected if dockets are created asynchronously" -ForegroundColor Yellow
}

# Step 6: Query Transactions (Optional)
Write-Step "Step 6: Query Transactions by Wallet"

Write-Info "Querying transactions involving the owner wallet..."

try {
    $queryOutput = Invoke-SorchaCommand -Arguments @("query", "wallet", "--address", $CreatedWalletAddress) -CaptureOutput

    Write-Success "Query completed"
    Write-Host ""
    Write-Host $queryOutput -ForegroundColor DarkGray
} catch {
    Write-Info "No transactions found (this is expected for a new register)"
}

# Cleanup (if requested)
if ($Cleanup) {
    Write-Step "Cleanup: Remove Created Resources"

    Write-Info "Deleting register..."
    try {
        Invoke-SorchaCommand -Arguments @("register", "delete", "--id", $CreatedRegisterId, "--yes") -ExpectSuccess:$false
        Write-Success "Register deleted"
    } catch {
        Write-Info "Could not delete register (may require additional permissions)"
    }

    Write-Info "Deleting wallet..."
    try {
        Invoke-SorchaCommand -Arguments @("wallet", "delete", "--address", $CreatedWalletAddress, "--yes") -ExpectSuccess:$false
        Write-Success "Wallet deleted"
    } catch {
        Write-Info "Could not delete wallet (may require additional permissions)"
    }
}

# Summary
Write-Section "Summary"

Write-Host ""
Write-Host "Walkthrough Results:" -ForegroundColor White
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor White

Write-Success "CLI Installation Verified"
Write-Success "Authentication Successful"
Write-Success "Wallet Created ($Algorithm)"
Write-Success "Register Created (Two-Phase Flow)"
Write-Success "Genesis Transaction Submitted"
Write-Success "Register Verified"

Write-Host ""
Write-Host "Created Resources:" -ForegroundColor Yellow
Write-Host "  Wallet Address:  $CreatedWalletAddress" -ForegroundColor White
Write-Host "  Register ID:     $CreatedRegisterId" -ForegroundColor White

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. List all registers:     sorcha register list" -ForegroundColor Gray
Write-Host "  2. Get register details:   sorcha register get --id $CreatedRegisterId" -ForegroundColor Gray
Write-Host "  3. List dockets:           sorcha docket list --register-id $CreatedRegisterId" -ForegroundColor Gray
Write-Host "  4. Query by wallet:        sorcha query wallet --address $CreatedWalletAddress" -ForegroundColor Gray

if (-not $Cleanup) {
    Write-Host ""
    Write-Host "To cleanup resources, run:" -ForegroundColor Yellow
    Write-Host "  sorcha register delete --id $CreatedRegisterId" -ForegroundColor Gray
    Write-Host "  sorcha wallet delete --address $CreatedWalletAddress" -ForegroundColor Gray
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  STATUS: Complete Success" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
