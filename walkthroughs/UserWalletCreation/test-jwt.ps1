# Test JWT token from login endpoint
$loginBody = @{
    email = "debug@test.local"
    password = "TestPass123!"
}

Write-Host "Logging in..." -ForegroundColor Cyan
$response = Invoke-RestMethod -Uri "http://localhost:5110/api/auth/login" -Method POST -Body ($loginBody | ConvertTo-Json) -ContentType "application/json"

Write-Host "Login successful!" -ForegroundColor Green
Write-Host "Access Token (first 100 chars): $($response.access_token.Substring(0, [Math]::Min(100, $response.access_token.Length)))..." -ForegroundColor Gray
Write-Host "Token Type: $($response.token_type)" -ForegroundColor Gray
Write-Host "Expires In: $($response.expires_in) seconds" -ForegroundColor Gray

# Decode JWT to see claims
$tokenParts = $response.access_token.Split(".")
$payload = $tokenParts[1]

# Add padding if needed
$padding = 4 - ($payload.Length % 4)
if ($padding -lt 4) {
    $payload += "=" * $padding
}

$payloadJson = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload))
$claims = $payloadJson | ConvertFrom-Json

Write-Host ""
Write-Host "JWT Claims:" -ForegroundColor Cyan
Write-Host "  Issuer: $($claims.iss)" -ForegroundColor Yellow
Write-Host "  Audience: $($claims.aud)" -ForegroundColor Yellow
Write-Host "  Subject (user_id): $($claims.sub)" -ForegroundColor Gray
Write-Host "  Email: $($claims.email)" -ForegroundColor Gray
Write-Host "  Name: $($claims.name)" -ForegroundColor Gray
Write-Host "  Org ID: $($claims.org_id)" -ForegroundColor Gray
Write-Host "  Org Name: $($claims.org_name)" -ForegroundColor Gray
Write-Host "  Token Type: $($claims.token_type)" -ForegroundColor Gray
Write-Host "  Roles: $($claims.role -join ', ')" -ForegroundColor Gray
Write-Host "  Issued At: $(([DateTimeOffset]::FromUnixTimeSeconds($claims.iat)).ToLocalTime())" -ForegroundColor Gray
Write-Host "  Expires: $(([DateTimeOffset]::FromUnixTimeSeconds($claims.exp)).ToLocalTime())" -ForegroundColor Gray

# Now test wallet creation with this token
Write-Host ""
Write-Host "Testing wallet creation..." -ForegroundColor Cyan

$walletBody = @{
    name = "Test Wallet"
    algorithm = "ED25519"
    wordCount = 12
}

try {
    $walletResponse = Invoke-RestMethod `
        -Uri "http://localhost/api/v1/wallets" `
        -Method POST `
        -Headers @{ Authorization = "Bearer $($response.access_token)" } `
        -Body ($walletBody | ConvertTo-Json) `
        -ContentType "application/json"

    Write-Host "Wallet created successfully!" -ForegroundColor Green
    Write-Host "  Address: $($walletResponse.wallet.address)" -ForegroundColor Gray
    Write-Host "  Algorithm: $($walletResponse.wallet.algorithm)" -ForegroundColor Gray
}
catch {
    Write-Host "Wallet creation failed!" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
    if ($_.Exception.Response) {
        Write-Host "  Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    }
}
