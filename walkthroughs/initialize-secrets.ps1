#!/usr/bin/env pwsh
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
#
# initialize-secrets.ps1 — Generate walkthrough credentials in .secrets/passwords.json.
# Run once before using any walkthrough. Safe to re-run (regenerates all passwords).
#
# Usage:
#   pwsh walkthroughs/initialize-secrets.ps1
#   pwsh walkthroughs/initialize-secrets.ps1 -Force  # Overwrite existing

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$secretsDir = Join-Path $scriptDir ".secrets"
$passwordsFile = Join-Path $secretsDir "passwords.json"

Write-Host ""
Write-Host "Sorcha Walkthrough — Secrets Initialization" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if already exists
if ((Test-Path $passwordsFile) -and -not $Force) {
    Write-Host "[!] Secrets file already exists: $passwordsFile" -ForegroundColor Yellow
    Write-Host "    Use -Force to regenerate all passwords." -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

# Ensure directory exists
if (-not (Test-Path $secretsDir)) {
    New-Item -ItemType Directory -Path $secretsDir -Force | Out-Null
}

# Password generator: 8+ chars, upper + lower + digit + special
function New-WalkthroughPassword {
    $upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ"
    $lower   = "abcdefghjkmnpqrstuvwxyz"
    $digits  = "23456789"
    $special = "!@#$%&*_-+"

    # Guarantee at least one of each category
    $password = ""
    $password += $upper[(Get-Random -Maximum $upper.Length)]
    $password += $lower[(Get-Random -Maximum $lower.Length)]
    $password += $digits[(Get-Random -Maximum $digits.Length)]
    $password += $special[(Get-Random -Maximum $special.Length)]

    # Fill remaining with mixed characters
    $allChars = $upper + $lower + $digits + $special
    for ($i = 0; $i -lt 12; $i++) {
        $password += $allChars[(Get-Random -Maximum $allChars.Length)]
    }

    # Shuffle the password characters
    $chars = $password.ToCharArray()
    for ($i = $chars.Length - 1; $i -gt 0; $i--) {
        $j = Get-Random -Maximum ($i + 1)
        $temp = $chars[$i]
        $chars[$i] = $chars[$j]
        $chars[$j] = $temp
    }

    return -join $chars
}

# Define all walkthrough credential sets
$secrets = [ordered]@{
    "_meta" = @{
        generatedAt = (Get-Date -Format "o")
        description = "Auto-generated walkthrough credentials. Do NOT commit to source control."
    }
    "blueprint-storage" = @{
        adminEmail    = "admin@blueprint-storage.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Blueprint Admin"
    }
    "admin-integration" = @{
        adminEmail    = "admin@admin-integration.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Admin Integration"
    }
    "mcp-server" = @{
        adminEmail    = "admin@mcp-server.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "MCP Admin"
    }
    "pingpong" = @{
        adminEmail    = "admin@pingpong.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Ping-Pong Admin"
        pingEmail     = "ping@pingpong.local"
        pingPassword  = (New-WalkthroughPassword)
        pongEmail     = "pong@pingpong.local"
        pongPassword  = (New-WalkthroughPassword)
    }
    "register-demo" = @{
        adminEmail    = "admin@register-demo.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Register Admin"
    }
    "wallet-verify" = @{
        adminEmail    = "admin@wallet-verify.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Wallet Verify Admin"
    }
    "register-mongodb" = @{
        adminEmail    = "admin@register-mongodb.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "MongoDB Admin"
    }
    "org-pingpong" = @{
        adminEmail    = "designer@org-pingpong.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Blueprint Designer"
        alphaEmail    = "alpha@org-pingpong.local"
        alphaPassword = (New-WalkthroughPassword)
        betaEmail     = "beta@org-pingpong.local"
        betaPassword  = (New-WalkthroughPassword)
    }
    "construction-permit" = @{
        meridianAdminEmail    = "admin@meridian-construction.local"
        meridianAdminPassword = (New-WalkthroughPassword)
        apexAdminEmail        = "admin@apex-structural.local"
        apexAdminPassword     = (New-WalkthroughPassword)
        riversideAdminEmail   = "admin@riverside-council.local"
        riversideAdminPassword = (New-WalkthroughPassword)
        greenValleyAdminEmail = "admin@green-valley-env.local"
        greenValleyAdminPassword = (New-WalkthroughPassword)
    }
    "medical-equipment" = @{
        hospitalAdminEmail    = "admin@city-general.local"
        hospitalAdminPassword = (New-WalkthroughPassword)
        medtechAdminEmail     = "admin@medtech-refurb.local"
        medtechAdminPassword  = (New-WalkthroughPassword)
        authorityAdminEmail   = "admin@regional-health.local"
        authorityAdminPassword = (New-WalkthroughPassword)
    }
    "dist-register" = @{
        adminEmail    = "admin@dist-register.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Distributed Register Admin"
    }
    "perf" = @{
        adminEmail    = "admin@perf.local"
        adminPassword = (New-WalkthroughPassword)
        adminName     = "Performance Admin"
    }
}

# Write to file
$json = $secrets | ConvertTo-Json -Depth 5
Set-Content -Path $passwordsFile -Value $json -Encoding UTF8

Write-Host "[OK] Secrets generated: $passwordsFile" -ForegroundColor Green
Write-Host ""
Write-Host "Walkthrough credentials:" -ForegroundColor Yellow

$walkthroughCount = 0
foreach ($key in $secrets.Keys) {
    if ($key -eq "_meta") { continue }
    $walkthroughCount++
    $adminEmail = $secrets[$key].adminEmail
    if (-not $adminEmail) { $adminEmail = ($secrets[$key].Keys | Where-Object { $_ -match "Email" } | Select-Object -First 1) }
    Write-Host "  $key" -ForegroundColor White
}

Write-Host ""
Write-Host "$walkthroughCount walkthrough credential sets generated." -ForegroundColor Green
Write-Host ""
Write-Host "IMPORTANT: This file contains passwords. Do NOT commit it to source control." -ForegroundColor Red
Write-Host "           The walkthroughs/.secrets/ directory is already in .gitignore." -ForegroundColor Red
Write-Host ""
