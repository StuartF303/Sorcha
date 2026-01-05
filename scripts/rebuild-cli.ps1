#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Rebuild and reinstall the Sorcha CLI tool for development.

.DESCRIPTION
    This script automates the development workflow for the Sorcha CLI:
    1. Uninstalls the existing global tool
    2. Cleans build artifacts
    3. Builds the CLI in Release mode with auto-versioning
    4. Packs the NuGet package
    5. Reinstalls as a global tool from the local build

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release

.PARAMETER SkipUninstall
    Skip uninstalling the existing tool (useful if not yet installed)

.EXAMPLE
    .\scripts\rebuild-cli.ps1
    # Standard rebuild and reinstall

.EXAMPLE
    .\scripts\rebuild-cli.ps1 -Configuration Debug
    # Build in Debug mode (includes -dev suffix)

.EXAMPLE
    .\scripts\rebuild-cli.ps1 -SkipUninstall
    # First-time installation (tool not yet installed)
#>

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [switch]$SkipUninstall
)

$ErrorActionPreference = 'Stop'
$CliProjectPath = Join-Path $PSScriptRoot "..\src\Apps\Sorcha.Cli"
$CliCsprojPath = Join-Path $CliProjectPath "Sorcha.Cli.csproj"

Write-Host "🔧 Sorcha CLI Development Rebuild Script" -ForegroundColor Cyan
Write-Host "=========================================`n" -ForegroundColor Cyan

# Step 1: Uninstall existing tool
if (-not $SkipUninstall) {
    Write-Host "📦 Step 1: Uninstalling existing Sorcha CLI..." -ForegroundColor Yellow
    try {
        dotnet tool uninstall --global Sorcha.Cli 2>$null
        Write-Host "   ✅ Uninstalled successfully`n" -ForegroundColor Green
    }
    catch {
        Write-Host "   ⚠️  Tool not currently installed (OK)`n" -ForegroundColor Gray
    }
}
else {
    Write-Host "📦 Step 1: Skipping uninstall (first-time installation)`n" -ForegroundColor Gray
}

# Step 2: Clean build artifacts
Write-Host "🧹 Step 2: Cleaning build artifacts..." -ForegroundColor Yellow
Push-Location $CliProjectPath
try {
    dotnet clean --configuration $Configuration --verbosity quiet
    Write-Host "   ✅ Cleaned successfully`n" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Step 3: Build the CLI
Write-Host "🔨 Step 3: Building Sorcha CLI ($Configuration)..." -ForegroundColor Yellow
Push-Location $CliProjectPath
try {
    $buildOutput = dotnet build --configuration $Configuration --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Build failed!" -ForegroundColor Red
        Write-Host $buildOutput
        exit 1
    }

    # Extract version from build output
    $buildOutputPath = Join-Path $CliProjectPath "bin\$Configuration\net10.0"
    $dllPath = Join-Path $buildOutputPath "Sorcha.Cli.dll"

    if (Test-Path $dllPath) {
        try {
            $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllPath)
            $version = $versionInfo.ProductVersion
            if ($version) {
                Write-Host "   ✅ Built version: $version`n" -ForegroundColor Green
            }
            else {
                Write-Host "   ✅ Built successfully`n" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "   ✅ Built successfully`n" -ForegroundColor Green
        }
    }
    else {
        Write-Host "   ✅ Built successfully`n" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

# Step 4: Pack the NuGet package
Write-Host "📦 Step 4: Packing NuGet package..." -ForegroundColor Yellow
Push-Location $CliProjectPath
try {
    dotnet pack --configuration $Configuration --output ./nupkg --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Pack failed!" -ForegroundColor Red
        exit 1
    }

    # Find the generated .nupkg file
    $nupkgPath = Join-Path $CliProjectPath "nupkg"
    $nupkgFile = Get-ChildItem -Path $nupkgPath -Filter "Sorcha.Cli.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($nupkgFile) {
        Write-Host "   ✅ Package created: $($nupkgFile.Name)`n" -ForegroundColor Green
    }
    else {
        Write-Host "   ⚠️  Package created but not found`n" -ForegroundColor Yellow
    }
}
finally {
    Pop-Location
}

# Step 5: Install as global tool
Write-Host "🚀 Step 5: Installing as global tool..." -ForegroundColor Yellow
Push-Location $CliProjectPath
try {
    $nupkgPath = Join-Path $CliProjectPath "nupkg"
    dotnet tool install --global --add-source $nupkgPath Sorcha.Cli --prerelease 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Installation failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "   ✅ Installed successfully`n" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Step 6: Verify installation
Write-Host "✅ Step 6: Verifying installation..." -ForegroundColor Yellow
$versionOutput = sorcha version 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n╔═══════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  ✅ Sorcha CLI Rebuilt and Installed Successfully  ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

    Write-Host "📋 Version Information:" -ForegroundColor White
    Write-Host $versionOutput -ForegroundColor Gray

    Write-Host "`n🎯 Quick Start Commands:" -ForegroundColor White
    Write-Host "   sorcha --help             # Show all commands" -ForegroundColor Gray
    Write-Host "   sorcha version            # Show version details" -ForegroundColor Gray
    Write-Host "   sorcha config init        # Initialize configuration" -ForegroundColor Gray
    Write-Host "   sorcha auth login         # Authenticate" -ForegroundColor Gray
    Write-Host "   sorcha bootstrap --help   # Bootstrap environment`n" -ForegroundColor Gray
}
else {
    Write-Host "   ❌ Verification failed - tool not in PATH!" -ForegroundColor Red
    Write-Host "   Try closing and reopening your terminal.`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "💡 Tip: Run this script after each code change for quick testing`n" -ForegroundColor Cyan
