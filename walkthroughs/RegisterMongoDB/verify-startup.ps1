# Quick startup verification for Register Service with MongoDB
$ErrorActionPreference = "Stop"

Write-Host "Testing Register Service MongoDB startup..." -ForegroundColor Cyan

# Set MongoDB configuration
$env:ASPNETCORE_ENVIRONMENT = "MongoDB"
$env:ASPNETCORE_URLS = "http://localhost:5174"

Write-Host "Starting service (will auto-stop after 10 seconds)..." -ForegroundColor Yellow

# Start service in background
$job = Start-Job -ScriptBlock {
    Set-Location "C:\Projects\Sorcha"
    $env:ASPNETCORE_ENVIRONMENT = "MongoDB"
    $env:ASPNETCORE_URLS = "http://localhost:5174"
    dotnet run --project src/Services/Sorcha.Register.Service/Sorcha.Register.Service.csproj --no-build 2>&1
}

# Wait for startup output
Start-Sleep -Seconds 10

# Capture output
$output = Receive-Job $job -Keep

# Stop the job
Stop-Job $job
Remove-Job $job

# Check for success indicators
Write-Host ""
Write-Host "Startup Output:" -ForegroundColor Cyan
Write-Host "----------------" -ForegroundColor Gray

$mongoLine = $output | Where-Object { $_ -match "Register Service using MongoDB" }
$listeningLine = $output | Where-Object { $_ -match "Now listening on" }

if ($mongoLine) {
    Write-Host "✅ MongoDB storage configured" -ForegroundColor Green
    Write-Host "   $mongoLine" -ForegroundColor Gray
} else {
    Write-Host "❌ MongoDB storage NOT configured" -ForegroundColor Red
}

if ($listeningLine) {
    Write-Host "✅ Service started successfully" -ForegroundColor Green
    Write-Host "   $listeningLine" -ForegroundColor Gray
} else {
    Write-Host "⚠️  Service may not have started" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Full output saved to: verify-startup-output.txt" -ForegroundColor Gray
$output | Out-File -FilePath "walkthroughs/RegisterMongoDB/verify-startup-output.txt"
