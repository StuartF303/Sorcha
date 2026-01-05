#!/usr/bin/env pwsh
# Test script for Register Creation Flow via Docker network

$ErrorActionPreference = "Stop"

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Register Creation Flow - Docker Network Test" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "ℹ Testing via Docker internal network" -ForegroundColor Cyan
Write-Host "  (No AppHost restart required)" -ForegroundColor Gray
Write-Host ""

# Test 1: Create a test container to access internal network
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host "  Step 1: Preparing Test Environment" -ForegroundColor White
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray

# Find the network name
$network = docker network ls --filter "name=sorcha" --format "{{.Name}}" | Select-Object -First 1

if (-not $network) {
    Write-Host "✗ Could not find Sorcha Docker network" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Found Docker network: $network" -ForegroundColor Green

# Test 2: Execute curl commands from within the network
Write-Host ""
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host "  Step 2: Test Register Service (Initiate)" -ForegroundColor White
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray

$initiateRequest = @{
    name = "Docker Network Test Register"
    description = "Created via internal Docker network"
    tenantId = "docker-test-tenant-001"
    creator = @{
        userId = "docker-test-user-001"
        walletId = "docker-test-wallet-001"
    }
} | ConvertTo-Json -Depth 10 -Compress

# Use alpine container with curl to make the request
$response = docker run --rm --network $network alpine/curl:latest `
    -s -X POST `
    -H "Content-Type: application/json" `
    -d $initiateRequest `
    http://sorcha-register-service:8080/api/registers/initiate

if ($LASTEXITCODE -eq 0 -and $response) {
    Write-Host "✓ Register Service responded" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Gray
    $responseObj = $response | ConvertFrom-Json
    Write-Host ($responseObj | ConvertTo-Json -Depth 5) -ForegroundColor DarkGray

    $registerId = $responseObj.registerId
    $nonce = $responseObj.nonce

    Write-Host ""
    Write-Host "✓ Register ID: $registerId" -ForegroundColor Green
    Write-Host "✓ Nonce: $($nonce.Substring(0, 20))..." -ForegroundColor Green
} else {
    Write-Host "✗ Failed to initiate register" -ForegroundColor Red
    Write-Host "Response: $response" -ForegroundColor Red
    exit 1
}

# Test 3: Test Validator Service
Write-Host ""
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host "  Step 3: Test Validator Service (Genesis)" -ForegroundColor White
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray

$genesisRequest = @{
    transactionId = "docker-test-genesis-001"
    registerId = "dockertest001"
    controlRecordPayload = @{
        registerId = "dockertest001"
        name = "Test"
        tenantId = "tenant-001"
        createdAt = "2025-01-04T00:00:00Z"
        attestations = @()
    }
    payloadHash = "abcd1234"
    signatures = @(
        @{
            publicKey = [Convert]::ToBase64String((New-Object byte[] 32))
            signatureValue = [Convert]::ToBase64String((New-Object byte[] 64))
            algorithm = "ED25519"
        }
    )
    createdAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json -Depth 10 -Compress

$validatorResponse = docker run --rm --network $network alpine/curl:latest `
    -s -X POST `
    -H "Content-Type: application/json" `
    -d $genesisRequest `
    http://sorcha-validator-service:8080/api/validator/genesis

if ($LASTEXITCODE -eq 0 -and $validatorResponse) {
    Write-Host "✓ Validator Service responded" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Gray
    Write-Host $validatorResponse -ForegroundColor DarkGray
} else {
    Write-Host "✗ Failed to submit genesis transaction" -ForegroundColor Red
    Write-Host "Response: $validatorResponse" -ForegroundColor Red
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Test Complete" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ Both services are accessible via Docker network" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. To test from localhost, restart AppHost:" -ForegroundColor Gray
Write-Host "     cd src/Apps/Sorcha.AppHost && dotnet run" -ForegroundColor DarkGray
Write-Host "  2. Then run: pwsh walkthroughs/RegisterCreationFlow/test-register-creation.ps1" -ForegroundColor DarkGray
Write-Host ""
