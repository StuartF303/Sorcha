# Test Blazor Admin endpoint
$url = "https://blazor-admin.livelydune-b02bab51.uksouth.azurecontainerapps.io/"

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "Testing Blazor Admin Landing Page" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "URL: $url`n" -ForegroundColor White

try {
    $response = Invoke-WebRequest -Uri $url -TimeoutSec 15 -ErrorAction Stop
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Content-Type: $($response.Headers['Content-Type'])"
    Write-Host "Content Length: $($response.Content.Length) bytes"

    # Check if it's HTML
    if ($response.Headers['Content-Type'] -like "*html*") {
        Write-Host "`nPage Title:" -ForegroundColor Cyan
        if ($response.Content -match "<title>(.*?)</title>") {
            Write-Host "  $($matches[1])" -ForegroundColor White
        }

        Write-Host "`nHTML Preview (first 1000 chars):" -ForegroundColor Cyan
        $preview = $response.Content.Substring(0, [Math]::Min(1000, $response.Content.Length))
        Write-Host $preview -ForegroundColor Gray

        if ($response.Content.Length -gt 1000) {
            Write-Host "`n... (truncated)" -ForegroundColor DarkGray
        }
    }
    else {
        Write-Host "`nContent Preview:" -ForegroundColor Cyan
        Write-Host $response.Content.Substring(0, [Math]::Min(500, $response.Content.Length))
    }
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
}

Write-Host "`n=========================================" -ForegroundColor Cyan
