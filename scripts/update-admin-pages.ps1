# Update admin pages with render modes
$adminDir = "src\Apps\Sorcha.Admin\Pages\Admin"

$adminPages = @(
    "Audit.razor",
    "Config.razor",
    "Dashboard.razor",
    "Services.razor",
    "Tenants.razor",
    "Users.razor"
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

Write-Host "`nUpdating admin pages..." -ForegroundColor Cyan
foreach ($page in $adminPages) {
    $path = Join-Path $adminDir $page
    if (Test-Path $path) {
        Add-RenderMode -FilePath $path
    }
}
Write-Host "`nDone!" -ForegroundColor Green
