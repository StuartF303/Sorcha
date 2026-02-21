#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# ConstructionPermit — Setup
# Bootstrap single org (simplified), create 5 wallets, register, publish blueprint.
# Note: Uses single-org model with admin token for all participants.

param(
    [ValidateSet('gateway', 'direct', 'aspire')]
    [string]$Profile = 'gateway',
    [switch]$SkipHealthCheck
)

$ErrorActionPreference = "Stop"

$modulePath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "modules/SorchaWalkthrough/SorchaWalkthrough.psm1"
Import-Module $modulePath -Force

Write-WtBanner "ConstructionPermit — Setup"

$secrets = Get-SorchaSecrets -WalkthroughName "construction-permit"
$env = Initialize-SorchaEnvironment -Profile $Profile -SkipHealthCheck:$SkipHealthCheck

# Step 1: Bootstrap primary org (Meridian Construction manages the walkthrough)
Write-WtStep "Step 1: Bootstrap Organization"
$admin = Connect-SorchaAdmin `
    -TenantUrl $env.TenantUrl `
    -OrgName "Meridian Construction" `
    -OrgSubdomain "meridian-construction" `
    -AdminEmail $secrets.meridianAdminEmail `
    -AdminName "Site Manager" `
    -AdminPassword $secrets.meridianAdminPassword

# Step 2: Create wallets for all 5 participants
Write-WtStep "Step 2: Create Wallets (5 participants)"
$wallets = @{}
$participantDefs = @(
    @{ id = "contractor"; name = "Contractor Wallet" }
    @{ id = "structural-engineer"; name = "Engineer Wallet" }
    @{ id = "planning-officer"; name = "Planning Officer Wallet" }
    @{ id = "environmental-assessor"; name = "Environmental Wallet" }
    @{ id = "building-control"; name = "Building Control Wallet" }
)

foreach ($p in $participantDefs) {
    $w = New-SorchaWallet -WalletUrl $env.WalletUrl -Name $p.name -Headers $admin.Headers -FetchPublicKey
    $wallets[$p.id] = $w.Address
    Write-WtInfo "  $($p.id) -> $($w.Address)"
}

# Step 3: Register participant + link primary wallet
Write-WtStep "Step 3: Register Participant"
$participant = Register-SorchaParticipant `
    -TenantUrl $env.TenantUrl -WalletUrl $env.WalletUrl `
    -OrganizationId $admin.OrganizationId `
    -WalletAddress $wallets["contractor"] `
    -DisplayName "Site Manager" `
    -Headers $admin.Headers

# Step 4: Create register
Write-WtStep "Step 4: Create Register"
$register = New-SorchaRegister `
    -RegisterUrl $env.RegisterUrl -WalletUrl $env.WalletUrl `
    -Name "Construction Permit Register" `
    -Description "Register for the construction permit walkthrough" `
    -TenantId $admin.OrganizationId `
    -OwnerUserId $admin.AdminUserId `
    -OwnerWalletAddress $wallets["contractor"] `
    -Headers $admin.Headers `
    -Metadata @{ createdBy = "ConstructionPermit/setup.ps1" }

# Step 5: Publish blueprint
Write-WtStep "Step 5: Publish Blueprint"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$blueprint = Publish-SorchaBlueprint `
    -BlueprintUrl $env.BlueprintUrl `
    -TemplatePath (Join-Path $scriptDir "construction-permit-template.json") `
    -WalletMap $wallets `
    -Headers $admin.Headers `
    -IdPrefix "construction-permit"

# Save state
$state = @{
    profile        = $Profile
    organizationId = $admin.OrganizationId
    adminToken     = $admin.Token
    wallets        = $wallets
    registerId     = $register.RegisterId
    blueprintId    = $blueprint.BlueprintId
    blueprintUrl   = $env.BlueprintUrl
}

$stateFile = Join-Path $scriptDir "state.json"
$state | ConvertTo-Json -Depth 5 | Set-Content -Path $stateFile -Encoding UTF8

Write-WtSuccess "Setup complete — 5 wallets, register, blueprint published"
Write-WtInfo "Run: pwsh walkthroughs/ConstructionPermit/run.ps1"
