#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates self-signed certificates for Sorcha development in Docker

.DESCRIPTION
    Creates self-signed certificates for localhost and LAN access, exports to PFX format,
    and configures them for use in Docker containers. Generates both:
    - aspnetapp.pfx (API Gateway)
    - sorcha-ui-web.pfx (UI Web)

.PARAMETER CertificatePath
    Directory to store the generated certificates (default: ./docker/certs)

.PARAMETER GatewayPassword
    Password for the API Gateway PFX certificate (default: SorchaDev2025)

.PARAMETER UiPassword
    Password for the UI Web PFX certificate (default: SorchaDevCert2025!)

.PARAMETER Force
    Skip overwrite confirmation prompt

.EXAMPLE
    .\generate-dev-cert.ps1
    .\generate-dev-cert.ps1 -Force
    .\generate-dev-cert.ps1 -CertificatePath "./docker/certs" -Force
#>

param(
    [string]$CertificatePath = "./docker/certs",
    [string]$GatewayPassword = "SorchaDev2025",
    [string]$UiPassword = "SorchaDevCert2025!",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "=== Sorcha Development Certificate Generator ===" -ForegroundColor Cyan
Write-Host ""

# Certificate directory (relative to script parent)
$certDir = Join-Path (Join-Path $PSScriptRoot "..") $CertificatePath
if (-not (Test-Path $certDir)) {
    Write-Host "Creating certificate directory: $certDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null
}

# SAN list — includes localhost + LAN IPs/hostnames for cross-machine dev
$dnsNames = @(
    "localhost",
    "127.0.0.1",
    "::1",
    "sorcha-ui-web",
    "api-gateway",
    "192.168.51.9",
    "192.168.51.116",
    "tiny",
    "tiny.local"
)

Write-Host "DNS SANs: $($dnsNames -join ', ')" -ForegroundColor Yellow
Write-Host ""

function New-SorchaCert {
    param(
        [string]$Name,
        [string]$OutputPath,
        [string]$Password,
        [string[]]$DnsNames
    )

    $certFile = Join-Path $OutputPath "$Name.pfx"
    $certPassword = ConvertTo-SecureString -String $Password -Force -AsPlainText

    # Check if certificate already exists
    if (Test-Path $certFile) {
        if (-not $Force) {
            Write-Host "Certificate already exists: $certFile" -ForegroundColor Yellow
            $overwrite = Read-Host "Overwrite? (y/n)"
            if ($overwrite -ne 'y') {
                Write-Host "  Skipped." -ForegroundColor DarkGray
                return
            }
        }
        Remove-Item $certFile -Force
    }

    Write-Host "Generating $Name certificate..." -ForegroundColor Yellow

    $cert = New-SelfSignedCertificate `
        -Subject "CN=localhost" `
        -DnsName $DnsNames `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -NotBefore (Get-Date) `
        -NotAfter (Get-Date).AddYears(2) `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -FriendlyName "Sorcha $Name Development Certificate" `
        -HashAlgorithm SHA256 `
        -KeyUsage DigitalSignature, KeyEncipherment, DataEncipherment `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

    Write-Host "  Thumbprint: $($cert.Thumbprint)" -ForegroundColor Green

    # Export to PFX
    Export-PfxCertificate `
        -Cert $cert `
        -FilePath $certFile `
        -Password $certPassword `
        -Force | Out-Null

    Write-Host "  Exported: $certFile" -ForegroundColor Green

    # Trust the certificate
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
    $store.Open("ReadWrite")
    $store.Add($cert)
    $store.Close()
    Write-Host "  Trusted (added to CurrentUser\Root)" -ForegroundColor Green

    return $cert
}

# Generate both certificates
Write-Host "--- API Gateway Certificate ---" -ForegroundColor Cyan
$gwCert = New-SorchaCert -Name "aspnetapp" -OutputPath $certDir -Password $GatewayPassword -DnsNames $dnsNames

Write-Host ""
Write-Host "--- UI Web Certificate ---" -ForegroundColor Cyan
$uiCert = New-SorchaCert -Name "sorcha-ui-web" -OutputPath $certDir -Password $UiPassword -DnsNames $dnsNames

Write-Host ""
Write-Host "=== Certificate Generation Complete ===" -ForegroundColor Cyan
Write-Host "Files:"
Write-Host "  $certDir\aspnetapp.pfx     (password: $GatewayPassword)"
Write-Host "  $certDir\sorcha-ui-web.pfx (password: $UiPassword)"
Write-Host ""
Write-Host "SANs: $($dnsNames -join ', ')" -ForegroundColor Green
Write-Host ""
Write-Host "Next: Copy certs to remote machine and restart services." -ForegroundColor Yellow
