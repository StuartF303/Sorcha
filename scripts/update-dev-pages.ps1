# Update dev pages with render modes
$devDir = "src\Apps\Sorcha.Admin\Pages\Dev"

$devPages = @(
    "ApiDocs.razor",
    "Events.razor",
    "Performance.razor",
    "Playground.razor",
    "Webhooks.razor"
)

function Add-RenderMode {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw

    if ($content -match '@rendermode') {
        Write-Host "  Skipping $FilePath - already has render mode" -ForegroundColor Yellow
        return
    }

    if ($content -match '(@page\s+"[^"]+")') {
        $pageDirective = $matches[1]
        $replacement = $pageDirective + "`n@rendermode InteractiveServer`n@attribute [Authorize]"
        $newContent = $content -replace [regex]::Escape($pageDirective), $replacement
        $newContent | Set-Content $FilePath -NoNewline
        Write-Host "  Updated $FilePath with InteractiveServer" -ForegroundColor Green
    }
}

Write-Host "`nUpdating dev pages..." -ForegroundColor Cyan
foreach ($page in $devPages) {
    $path = Join-Path $devDir $page
    if (Test-Path $path) {
        Add-RenderMode -FilePath $path
    }
}
Write-Host "`nDone!" -ForegroundColor Green
