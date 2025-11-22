# Setup Local Secrets for Sorcha Tenant Service
# This script helps developers quickly configure User Secrets for local development

param(
    [switch]$GenerateKeys,
    [string]$DbPassword = "",
    [switch]$Interactive = $true
)

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Sorcha Tenant Service - Local Secrets Setup" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

$ProjectPath = "src/Services/Sorcha.Tenant.Service"

# Check if .NET SDK is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK detected: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 10 SDK." -ForegroundColor Red
    exit 1
}

# Check if project exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "✗ Project not found at: $ProjectPath" -ForegroundColor Red
    Write-Host "  Please run this script from the Sorcha project root directory." -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Project found: $ProjectPath" -ForegroundColor Green
Write-Host ""

# Initialize User Secrets (if not already done)
Write-Host "Initializing User Secrets..." -ForegroundColor Cyan
dotnet user-secrets init --project $ProjectPath 2>&1 | Out-Null
Write-Host "✓ User Secrets initialized" -ForegroundColor Green
Write-Host ""

# Generate JWT Signing Key
if ($GenerateKeys -or $Interactive) {
    Write-Host "===================================================" -ForegroundColor Cyan
    Write-Host "  Step 1: JWT Signing Key (RSA-4096)" -ForegroundColor Cyan
    Write-Host "===================================================" -ForegroundColor Cyan
    Write-Host ""

    $generateNewKey = $true
    if ($Interactive) {
        $response = Read-Host "Generate new RSA-4096 signing key? (y/n) [y]"
        if ($response -eq "n" -or $response -eq "N") {
            $generateNewKey = $false
        }
    }

    if ($generateNewKey) {
        Write-Host "Generating RSA-4096 key pair..." -ForegroundColor Yellow

        # Check if OpenSSL is available
        $opensslAvailable = $false
        try {
            openssl version 2>&1 | Out-Null
            $opensslAvailable = $true
        } catch {
            Write-Host "  OpenSSL not found. Using .NET RSA instead." -ForegroundColor Yellow
        }

        if ($opensslAvailable) {
            # Use OpenSSL
            $tempPrivate = [System.IO.Path]::GetTempFileName()
            $tempPublic = [System.IO.Path]::GetTempFileName()

            openssl genrsa -out $tempPrivate 4096 2>&1 | Out-Null
            openssl rsa -in $tempPrivate -pubout -out $tempPublic 2>&1 | Out-Null

            $privateKeyPem = Get-Content $tempPrivate -Raw
            $publicKeyPem = Get-Content $tempPublic -Raw

            Remove-Item $tempPrivate, $tempPublic

            Write-Host "✓ RSA-4096 key pair generated (OpenSSL)" -ForegroundColor Green
        } else {
            # Use .NET RSA
            $rsa = [System.Security.Cryptography.RSA]::Create(4096)
            $privateKeyBytes = $rsa.ExportRSAPrivateKey()
            $publicKeyBytes = $rsa.ExportRSAPublicKey()

            # Convert to PEM format
            $privateKeyBase64 = [System.Convert]::ToBase64String($privateKeyBytes, [System.Base64FormattingOptions]::InsertLineBreaks)
            $publicKeyBase64 = [System.Convert]::ToBase64String($publicKeyBytes, [System.Base64FormattingOptions]::InsertLineBreaks)

            $privateKeyPem = "-----BEGIN RSA PRIVATE KEY-----`n$privateKeyBase64`n-----END RSA PRIVATE KEY-----"
            $publicKeyPem = "-----BEGIN PUBLIC KEY-----`n$publicKeyBase64`n-----END PUBLIC KEY-----"

            Write-Host "✓ RSA-4096 key pair generated (.NET)" -ForegroundColor Green
        }

        # Store private key in User Secrets
        dotnet user-secrets set "JwtSettings:SigningKey" $privateKeyPem --project $ProjectPath
        Write-Host "✓ Private key stored in User Secrets" -ForegroundColor Green

        # Save public key to file (for reference)
        $publicKeyPath = Join-Path (Get-Location) "specs/001-tenant-auth/jwt_public.pem"
        $privateKeyPath = Join-Path (Get-Location) "specs/001-tenant-auth/jwt_private.pem"

        Set-Content -Path $publicKeyPath -Value $publicKeyPem
        Set-Content -Path $privateKeyPath -Value $privateKeyPem

        Write-Host "✓ Keys saved to: specs/001-tenant-auth/" -ForegroundColor Green
        Write-Host "  - jwt_private.pem (DO NOT COMMIT)" -ForegroundColor Yellow
        Write-Host "  - jwt_public.pem (can be shared)" -ForegroundColor Yellow
        Write-Host ""
    }
}

# Set Database Password
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Step 2: Database Password" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrWhiteSpace($DbPassword) -and $Interactive) {
    $DbPassword = Read-Host "Enter PostgreSQL password (leave empty for 'dev_password123')"
    if ([string]::IsNullOrWhiteSpace($DbPassword)) {
        $DbPassword = "dev_password123"
    }
}

if ([string]::IsNullOrWhiteSpace($DbPassword)) {
    $DbPassword = "dev_password123"
}

dotnet user-secrets set "ConnectionStrings:Password" $DbPassword --project $ProjectPath
Write-Host "✓ Database password set in User Secrets" -ForegroundColor Green
Write-Host ""

# Set Redis Password (optional)
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Step 3: Redis Password (Optional)" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

if ($Interactive) {
    $setRedisPassword = Read-Host "Set Redis password? (y/n) [n]"
    if ($setRedisPassword -eq "y" -or $setRedisPassword -eq "Y") {
        $RedisPassword = Read-Host "Enter Redis password"
        if (-not [string]::IsNullOrWhiteSpace($RedisPassword)) {
            dotnet user-secrets set "Redis:Password" $RedisPassword --project $ProjectPath
            Write-Host "✓ Redis password set in User Secrets" -ForegroundColor Green
        }
    } else {
        Write-Host "  Skipping Redis password (using local Redis without auth)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  Skipping Redis password (using local Redis without auth)" -ForegroundColor Yellow
}

Write-Host ""

# Verify secrets
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Verification" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configured secrets:" -ForegroundColor Green
dotnet user-secrets list --project $ProjectPath

Write-Host ""
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Start Docker dependencies: docker-compose up -d postgres redis" -ForegroundColor White
Write-Host "  2. Run database migrations: dotnet ef database update --project $ProjectPath" -ForegroundColor White
Write-Host "  3. Run the service: dotnet run --project $ProjectPath" -ForegroundColor White
Write-Host "  4. Open Scalar UI: https://localhost:7080/scalar" -ForegroundColor White
Write-Host ""
Write-Host "For more information, see: specs/001-tenant-auth/secrets-setup.md" -ForegroundColor Cyan
Write-Host ""
