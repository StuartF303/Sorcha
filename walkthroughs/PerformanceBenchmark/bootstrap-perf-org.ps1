#!/usr/bin/env pwsh
# Quick bootstrap script for performance testing organization

$ErrorActionPreference = "Stop"

$body = @{
    organizationName = "Performance Testing"
    organizationSubdomain = "perf"
    adminEmail = "admin@perf.local"
    adminName = "Performance Admin"
    adminPassword = "PerfTest2026!"
} | ConvertTo-Json

Write-Host "Bootstrapping Performance Testing organization..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Method Post `
        -Uri 'http://localhost/api/tenants/bootstrap' `
        -Body $body `
        -ContentType 'application/json' `
        -UseBasicParsing

    Write-Host "✓ Organization bootstrapped successfully" -ForegroundColor Green
    Write-Host "  Admin Token: $($response.adminAccessToken.Substring(0, 50))..." -ForegroundColor White
    Write-Host "  Org ID: $($response.organizationId)" -ForegroundColor White
    Write-Host "  Admin Email: admin@perf.local" -ForegroundColor White
    Write-Host "  Admin Password: PerfTest2026!" -ForegroundColor White
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 409) {
        Write-Host "✓ Organization already exists - testing login..." -ForegroundColor Yellow

        $loginBody = "grant_type=password&username=admin@perf.local&password=PerfTest2026!&client_id=sorcha-cli"
        $loginResponse = Invoke-RestMethod -Method Post `
            -Uri 'http://localhost/api/service-auth/token' `
            -Body $loginBody `
            -ContentType 'application/x-www-form-urlencoded' `
            -UseBasicParsing

        Write-Host "✓ Login successful" -ForegroundColor Green
        Write-Host "  Token: $($loginResponse.access_token.Substring(0, 50))..." -ForegroundColor White
    }
    else {
        Write-Host "✗ Bootstrap failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  Status Code: $statusCode" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "You can now run the performance tests:" -ForegroundColor Cyan
Write-Host "  ./walkthroughs/PerformanceBenchmark/test-performance.ps1 -QuickTest" -ForegroundColor White
Write-Host ""
