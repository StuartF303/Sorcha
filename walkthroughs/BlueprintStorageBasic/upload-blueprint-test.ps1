# Upload and test blueprint workflow
Write-Host "Sorcha Blueprint Upload Test" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# 1. Authenticate
Write-Host "[1/4] Authenticating..." -ForegroundColor Yellow
$body = @{
    grant_type='password'
    username='stuart.mackintosh@sorcha.dev'
    password='SorchaDev2025!'
    client_id='sorcha-cli'
}
$resp = Invoke-RestMethod -Uri 'http://localhost:5110/api/service-auth/token' -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded'
Write-Host "  Token obtained" -ForegroundColor Green

# 2. Load blueprint from file
Write-Host "[2/4] Loading blueprint from file..." -ForegroundColor Yellow
$blueprintJson = Get-Content "samples/blueprints/finance/simple-invoice-approval.json" -Raw
$blueprint = $blueprintJson | ConvertFrom-Json
Write-Host "  Loaded: $($blueprint.title)" -ForegroundColor Green

# 3. Upload to Blueprint Service
Write-Host "[3/4] Uploading blueprint to service..." -ForegroundColor Yellow
$headers = @{Authorization = "Bearer $($resp.access_token)"; "Content-Type" = "application/json"}
try {
    $created = Invoke-RestMethod -Uri 'http://localhost:5000/api/blueprints' -Method POST -Headers $headers -Body $blueprintJson
    Write-Host "  Blueprint uploaded successfully!" -ForegroundColor Green
    Write-Host "  ID: $($created.id)" -ForegroundColor Gray
    Write-Host "  Title: $($created.title)" -ForegroundColor Gray
    Write-Host "  Actions: $($created.actions.Count)" -ForegroundColor Gray
} catch {
    Write-Host "  Error uploading: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

# 4. Verify - List all blueprints
Write-Host "[4/4] Verifying blueprint was saved..." -ForegroundColor Yellow
$blueprints = Invoke-RestMethod -Uri 'http://localhost:5000/api/blueprints' -Method GET -Headers $headers
Write-Host "  Total blueprints in system: $($blueprints.Count)" -ForegroundColor Green
$blueprints | ForEach-Object {
    Write-Host "    - $($_.title) (ID: $($_.id))" -ForegroundColor Gray
}

Write-Host ""
Write-Host "SUCCESS! Blueprint workflow complete" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  - Access API docs: http://localhost/scalar/" -ForegroundColor Gray
Write-Host "  - View Aspire Dashboard: http://localhost:18888" -ForegroundColor Gray
Write-Host "  - Use Blueprint ID: $($created.id)" -ForegroundColor Gray
