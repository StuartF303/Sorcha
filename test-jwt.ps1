$body = @{
    grant_type = 'password'
    username = 'admin@sorcha.local'
    password = 'Dev_Pass_2025!'
    client_id = 'sorcha-cli'
}

$response = Invoke-RestMethod -Uri 'http://localhost:5110/api/service-auth/token' -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded'
$token = $response.access_token

# Decode JWT payload (middle part of the token)
$parts = $token.Split('.')
# Replace URL-safe base64 characters and add padding
$base64 = $parts[1].Replace('-', '+').Replace('_', '/')
switch ($base64.Length % 4) {
    1 { $base64 += '===' }
    2 { $base64 += '==' }
    3 { $base64 += '=' }
}
$payloadBytes = [System.Convert]::FromBase64String($base64)
$payload = [System.Text.Encoding]::UTF8.GetString($payloadBytes)

Write-Host "JWT Payload:"
$payloadJson = $payload | ConvertFrom-Json
Write-Host ($payloadJson | ConvertTo-Json -Depth 10)
Write-Host ""
Write-Host "Issuer: $($payloadJson.iss)"
Write-Host "Audience: $($payloadJson.aud)"

# Save token for CLI
$tokenCache = @{
    docker = @{
        accessToken = $token
        refreshToken = $response.refresh_token
        expiresAt = (Get-Date).AddSeconds($response.expires_in).ToString("o")
        profile = "docker"
        subject = "stuart.mackintosh@sorcha.dev"
    }
}

$cacheDir = "$env:USERPROFILE\.sorcha"
if (!(Test-Path $cacheDir)) {
    New-Item -ItemType Directory -Path $cacheDir | Out-Null
}

$tokenCache | ConvertTo-Json -Depth 10 | Out-File "$cacheDir\token-cache.json" -Encoding UTF8
Write-Host "`nToken cached successfully"
