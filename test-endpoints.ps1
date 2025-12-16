# Test Azure endpoints
$urls = @(
    "https://api-gateway.livelydune-b02bab51.uksouth.azurecontainerapps.io/",
    "https://peer-service.livelydune-b02bab51.uksouth.azurecontainerapps.io/"
)

foreach ($url in $urls) {
    Write-Host "`n=========================================" -ForegroundColor Cyan
    Write-Host "Testing: $url" -ForegroundColor Yellow
    Write-Host "=========================================" -ForegroundColor Cyan

    try {
        $response = Invoke-WebRequest -Uri $url -TimeoutSec 10 -ErrorAction Stop
        Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "Content-Type: $($response.Headers['Content-Type'])"
        Write-Host "Content Length: $($response.Content.Length) bytes"

        Write-Host "`nContent Preview (first 500 chars):" -ForegroundColor Cyan
        $preview = $response.Content.Substring(0, [Math]::Min(500, $response.Content.Length))
        Write-Host $preview

        if ($response.Content.Length -gt 500) {
            Write-Host "... (truncated)" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
