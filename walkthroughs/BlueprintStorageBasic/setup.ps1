#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# BlueprintStorageBasic — Setup
# Bootstraps organization and admin credentials for blueprint CRUD tests.

param(
    [ValidateSet('gateway', 'direct', 'aspire')]
    [string]$Profile = 'gateway',
    [switch]$SkipHealthCheck
)

$ErrorActionPreference = "Stop"

# Import shared module
$modulePath = Join-Path (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)) "modules/SorchaWalkthrough/SorchaWalkthrough.psm1"
Import-Module $modulePath -Force

Write-WtBanner "BlueprintStorageBasic — Setup"

# Load secrets
$secrets = Get-SorchaSecrets -WalkthroughName "blueprint-storage"

# Initialize environment
$env = Initialize-SorchaEnvironment -Profile $Profile -SkipHealthCheck:$SkipHealthCheck

# Bootstrap organization
$admin = Connect-SorchaAdmin `
    -TenantUrl $env.TenantUrl `
    -OrgName "Blueprint Storage Demo" `
    -OrgSubdomain "blueprint-storage" `
    -AdminEmail $secrets.adminEmail `
    -AdminName $secrets.adminName `
    -AdminPassword $secrets.adminPassword

# Save state for run.ps1
$state = @{
    profile        = $Profile
    organizationId = $admin.OrganizationId
    adminToken     = $admin.Token
    tenantUrl      = $env.TenantUrl
    blueprintUrl   = $env.BlueprintUrl
}

$stateFile = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "state.json"
$state | ConvertTo-Json -Depth 5 | Set-Content -Path $stateFile -Encoding UTF8

Write-WtSuccess "Setup complete — state saved to state.json"
Write-WtInfo "Run: pwsh walkthroughs/BlueprintStorageBasic/run.ps1"
