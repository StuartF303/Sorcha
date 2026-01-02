# Simple blueprint test
Write-Host "Getting token..."
$body = @{
    grant_type='password'
    username='admin@sorcha.local'
    password='admin123'
    client_id='sorcha-cli'
}
$resp = Invoke-RestMethod -Uri 'http://localhost:5110/api/service-auth/token' -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded'
Write-Host "Token obtained"

Write-Host "`nListing blueprints..."
$headers = @{Authorization = "Bearer $($resp.access_token)"}
$blueprints = Invoke-RestMethod -Uri 'http://localhost:5000/api/blueprints' -Method GET -Headers $headers
Write-Host "Found $($blueprints.Count) blueprints"

Write-Host "`nBlueprint Service is working!" -ForegroundColor Green
