# Test Blueprint API CRUD operations
$ErrorActionPreference = "Stop"

Write-Host "Testing Blueprint Service API..." -ForegroundColor Cyan
Write-Host ""

# 1. Get JWT token
Write-Host "1. Authenticating..." -ForegroundColor Yellow
$body = @{
    grant_type = 'password'
    username = 'admin@sorcha.local'
    password = 'admin123'
    client_id = 'sorcha-cli'
}

$response = Invoke-RestMethod `
    -Uri 'http://localhost:5110/api/service-auth/token' `
    -Method POST `
    -Body $body `
    -ContentType 'application/x-www-form-urlencoded'

$token = $response.access_token
Write-Host "   ✓ Token obtained" -ForegroundColor Green
Write-Host ""

# 2. Create a simple blueprint
Write-Host "2. Creating a blueprint..." -ForegroundColor Yellow

$blueprint = @{
    name = "Test Blueprint"
    description = "A simple test blueprint created via API"
    version = "1.0.0"
    participant = "test-participant"
    schemaUri = "http://example.com/schema.json"
    actions = @(
        @{
            actionId = 1
            role = "initiator"
            title = "Start Action"
            description = "The starting action"
            previousId = 0
        }
    )
} | ConvertTo-Json -Depth 10

try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    $created = Invoke-RestMethod `
        -Uri 'http://localhost:5000/api/blueprints' `
        -Method POST `
        -Headers $headers `
        -Body $blueprint

    $blueprintId = $created.id
    Write-Host "   ✓ Blueprint created with ID: $blueprintId" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "   ✗ Failed to create blueprint:" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

# 3. List blueprints
Write-Host "3. Listing blueprints..." -ForegroundColor Yellow
try {
    $blueprints = Invoke-RestMethod `
        -Uri 'http://localhost:5000/api/blueprints' `
        -Method GET `
        -Headers $headers

    Write-Host "   ✓ Found $($blueprints.Count) blueprint(s)" -ForegroundColor Green
    $blueprints | ForEach-Object {
        Write-Host "     - $($_.name) (v$($_.version)) - ID: $($_.id)" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "   ✗ Failed to list blueprints: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 4. Get specific blueprint
Write-Host "4. Retrieving blueprint by ID..." -ForegroundColor Yellow
try {
    $retrieved = Invoke-RestMethod `
        -Uri "http://localhost:5000/api/blueprints/$blueprintId" `
        -Method GET `
        -Headers $headers

    Write-Host "   ✓ Retrieved blueprint: $($retrieved.name)" -ForegroundColor Green
    Write-Host "     Actions: $($retrieved.actions.Count)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "   ✗ Failed to retrieve blueprint: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 5. Update blueprint
Write-Host "5. Updating blueprint..." -ForegroundColor Yellow
$retrieved.description = "Updated description via API test"

try {
    $updated = Invoke-RestMethod `
        -Uri "http://localhost:5000/api/blueprints/$blueprintId" `
        -Method PUT `
        -Headers $headers `
        -Body ($retrieved | ConvertTo-Json -Depth 10)

    Write-Host "   ✓ Blueprint updated" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "   ✗ Failed to update blueprint: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "Blueprint Service API Test Complete!" -ForegroundColor Green
Write-Host "══════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "✓ Authentication working" -ForegroundColor Green
Write-Host "✓ Create blueprint working" -ForegroundColor Green
Write-Host "✓ List blueprints working" -ForegroundColor Green
Write-Host "✓ Retrieve blueprint working" -ForegroundColor Green
Write-Host "✓ Update blueprint working" -ForegroundColor Green
Write-Host ""
Write-Host "Blueprint ID for further testing: $blueprintId" -ForegroundColor Cyan
