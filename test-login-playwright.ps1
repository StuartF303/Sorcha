#!/usr/bin/env pwsh
# Test authentication via localhost using PowerShell WebClient

$ErrorActionPreference = "Continue"

Write-Host "=== Testing Sorcha.UI.Web Authentication ===" -ForegroundColor Cyan
Write-Host ""

# Test if port 5173 is accessible
Write-Host "Testing HTTP port 5173..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5173/login" -UseBasicParsing -TimeoutSec 5
    Write-Host "[OK] HTTP Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "[OK] Content-Type: $($response.Headers['Content-Type'])" -ForegroundColor Green

    # Check if login page contains expected elements
    if ($response.Content -match "Sign In") {
        Write-Host "[OK] Login page loaded successfully" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Login page content unexpected" -ForegroundColor Red
    }
} catch {
    Write-Host "[FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Testing authentication POST..." -ForegroundColor Yellow

# Prepare form data for OAuth2 token request
$formData = @{
    username = "admin@sorcha.local"
    password = "Dev_Pass_2025!"
    grant_type = "password"
    client_id = "sorcha-ui-web"
}

try {
    # Note: This should go through the API Gateway (localhost:80)
    $tokenResponse = Invoke-WebRequest `
        -Uri "http://localhost/api/service-auth/token" `
        -Method POST `
        -Body $formData `
        -ContentType "application/x-www-form-urlencoded" `
        -UseBasicParsing `
        -TimeoutSec 10

    Write-Host "[OK] Token endpoint status: $($tokenResponse.StatusCode)" -ForegroundColor Green

    # Parse JSON response
    $tokenData = $tokenResponse.Content | ConvertFrom-Json

    if ($tokenData.access_token) {
        Write-Host "[OK] Access token received (length: $($tokenData.access_token.Length))" -ForegroundColor Green
        Write-Host "[OK] Token type: $($tokenData.token_type)" -ForegroundColor Green
        Write-Host "[OK] Expires in: $($tokenData.expires_in) seconds" -ForegroundColor Green

        # Show first 50 chars of token
        $tokenPreview = $tokenData.access_token.Substring(0, [Math]::Min(50, $tokenData.access_token.Length))
        Write-Host "[OK] Token preview: $tokenPreview..." -ForegroundColor Green

        # Decode JWT header to verify format
        $tokenParts = $tokenData.access_token.Split('.')
        if ($tokenParts.Length -eq 3) {
            Write-Host "[OK] Token is valid JWT format (3 parts)" -ForegroundColor Green
        } else {
            Write-Host "[FAIL] Token format unexpected" -ForegroundColor Red
        }

        Write-Host ""
        Write-Host "=== Authentication Test: SUCCESS ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Token can be encrypted using Web Crypto API (requires localhost or HTTPS)"
        Write-Host "2. Token can be stored in localStorage"
        Write-Host "3. Subsequent requests can use Authorization Bearer token"
        Write-Host ""

    } else {
        Write-Host "[FAIL] No access token in response" -ForegroundColor Red
        Write-Host "Response: $($tokenResponse.Content)" -ForegroundColor Yellow
    }

} catch {
    Write-Host "[FAIL] Authentication failed: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "[FAIL] HTTP Status: $statusCode" -ForegroundColor Red

        # Try to read error response
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            $reader.Close()
            Write-Host "Error response: $errorBody" -ForegroundColor Yellow
        } catch {
            Write-Host "Could not read error response" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
