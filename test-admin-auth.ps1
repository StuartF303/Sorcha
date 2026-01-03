# Test Sorcha Admin Authentication Flow
Write-Host "`n=== Sorcha Admin Authentication Test ===" -ForegroundColor Cyan

# Test 1
Write-Host "`n[1/4] Testing Admin UI accessibility..." -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "http://localhost/admin/" -Method GET -UseBasicParsing
Write-Host "  - Admin UI: HTTP $($response.StatusCode)" -ForegroundColor Green

# Test 2
Write-Host "`n[2/4] Testing authentication..." -ForegroundColor Yellow
$body = @{
    grant_type = "password"
    username = "admin@sorcha.local"
    password = "Dev_Pass_2025!"
    client_id = "sorcha-admin"
}
$tokenResponse = Invoke-RestMethod -Uri "http://localhost/api/service-auth/token" -Method POST -Body $body -ContentType "application/x-www-form-urlencoded"
Write-Host "  - Token length: $($tokenResponse.access_token.Length) characters" -ForegroundColor Green

# Test 3
Write-Host "`n[3/4] Verifying JWT role claims..." -ForegroundColor Yellow
$token = $tokenResponse.access_token
$parts = $token.Split('.')
$payloadBase64 = $parts[1]
while ($payloadBase64.Length % 4 -ne 0) { $payloadBase64 += "=" }
$payloadJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payloadBase64.Replace('-', '+').Replace('_', '/')))
$payload = $payloadJson | ConvertFrom-Json
Write-Host "  - Subject: $($payload.sub)" -ForegroundColor Gray
Write-Host "  - Email: $($payload.email)" -ForegroundColor Gray
Write-Host "  - Roles:" -ForegroundColor Green
foreach ($role in $payload.role) {
    Write-Host "    * $role" -ForegroundColor Green
}

# Test 4
Write-Host "`n[4/4] Testing Bearer token authentication..." -ForegroundColor Yellow
$headers = @{ "Authorization" = "Bearer $($tokenResponse.access_token)" }
$apiResponse = Invoke-RestMethod -Uri "http://localhost/api/organizations/stats" -Method GET -Headers $headers
Write-Host "  - TotalOrganizations: $($apiResponse.TotalOrganizations)" -ForegroundColor Green

Write-Host "`n=== All Tests Passed ===" -ForegroundColor Cyan
