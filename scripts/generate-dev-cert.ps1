#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates a self-signed certificate for Sorcha.UI.Web HTTPS development in Docker

.DESCRIPTION
    Creates a self-signed certificate for localhost, exports it to PFX format,
    and configures it for use in Docker containers.

.PARAMETER CertificatePath
    Directory to store the generated certificate (default: ./certs)

.PARAMETER Password
    Password for the PFX certificate (default: SorchaDevCert2025!)

.EXAMPLE
    .\generate-dev-cert.ps1
    .\generate-dev-cert.ps1 -CertificatePath "./docker/certs" -Password "MyPassword123!"
#>

param(
    [string]$CertificatePath = "./certs",
    [string]$Password = "SorchaDevCert2025!"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Sorcha.UI.Web HTTPS Certificate Generator ===" -ForegroundColor Cyan
Write-Host ""

# Create certificate directory if it doesn't exist
$certDir = Join-Path (Join-Path $PSScriptRoot "..") $CertificatePath
if (-not (Test-Path $certDir)) {
    Write-Host "Creating certificate directory: $certDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $certDir -Force | Out-Null
}

$certFile = Join-Path $certDir "sorcha-ui-web.pfx"
$certPassword = ConvertTo-SecureString -String $Password -Force -AsPlainText

# Check if certificate already exists
if (Test-Path $certFile) {
    Write-Host "Certificate already exists at: $certFile" -ForegroundColor Yellow
    $overwrite = Read-Host "Overwrite existing certificate? (y/n)"
    if ($overwrite -ne 'y') {
        Write-Host "Keeping existing certificate." -ForegroundColor Green
        exit 0
    }
    Remove-Item $certFile -Force
}

Write-Host "Generating self-signed certificate for localhost..." -ForegroundColor Yellow

# Generate self-signed certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=localhost" `
    -DnsName "localhost", "127.0.0.1", "::1", "sorcha-ui-web" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddYears(2) `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -FriendlyName "Sorcha UI Web Development Certificate" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature, KeyEncipherment, DataEncipherment `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

Write-Host "Certificate generated: $($cert.Thumbprint)" -ForegroundColor Green

# Export to PFX file
Write-Host "Exporting certificate to PFX..." -ForegroundColor Yellow
Export-PfxCertificate `
    -Cert $cert `
    -FilePath $certFile `
    -Password $certPassword `
    -Force | Out-Null

Write-Host "Certificate exported to: $certFile" -ForegroundColor Green

# Trust the certificate (add to Trusted Root)
Write-Host "Adding certificate to Trusted Root Certificate Authorities..." -ForegroundColor Yellow
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()

Write-Host "Certificate trusted" -ForegroundColor Green

# Display certificate info
Write-Host ""
Write-Host "=== Certificate Details ===" -ForegroundColor Cyan
Write-Host "Subject:      $($cert.Subject)"
Write-Host "Thumbprint:   $($cert.Thumbprint)"
Write-Host "Valid From:   $($cert.NotBefore)"
Write-Host "Valid To:     $($cert.NotAfter)"
Write-Host "DNS Names:    localhost, 127.0.0.1, ::1, sorcha-ui-web"
Write-Host ""
Write-Host "Certificate file: $certFile" -ForegroundColor Green
Write-Host "Password:         $Password" -ForegroundColor Yellow
Write-Host ""

# Create environment file for Docker
$envFile = Join-Path (Join-Path $PSScriptRoot "..") ".env.https"
$envContent = @"
# HTTPS Certificate Configuration for Docker
ASPNETCORE_Kestrel__Certificates__Default__Password=$Password
ASPNETCORE_Kestrel__Certificates__Default__Path=/https/sorcha-ui-web.pfx
ASPNETCORE_URLS=https://+:8443;http://+:8080
"@

Write-Host "Creating .env.https file for Docker..." -ForegroundColor Yellow
Set-Content -Path $envFile -Value $envContent -Force
Write-Host "Created: $envFile" -ForegroundColor Green

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Update docker-compose.yml to mount the certificate"
Write-Host "2. Add HTTPS port mapping (443:8443)"
Write-Host "3. Restart containers: docker-compose up -d"
Write-Host "4. Access via: https://localhost"
Write-Host ""
Write-Host "Certificate setup complete!" -ForegroundColor Green
