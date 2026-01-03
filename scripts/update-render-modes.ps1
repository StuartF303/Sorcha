# Script to add render modes to Blazor pages
# Run from repository root

$pagesDir = "src\Apps\Sorcha.Admin\Pages"

# Server-rendered pages (no WASM download)
$serverPages = @(
    "Help.razor",
    "NotFound.razor",
    "Counter.razor",
    "Weather.razor"
)

# WASM pages (triggers WASM download - authenticated user workflows)
$wasmPages = @(
    "Blueprints.razor",
    "SchemaLibrary.razor",
    "MyActions.razor",
    "MyWorkflows.razor",
    "MyWallet.razor",
    "MyTransactions.razor",
    "Settings.razor",
    "Templates.razor",
    "Administration.razor"
)

function Add-RenderMode {
    param(
        [string]$FilePath,
        [string]$RenderMode,
        [bool]$AllowAnonymous = $false
    )

    $content = Get-Content $FilePath -Raw

    # Check if @rendermode already exists
    if ($content -match '@rendermode') {
        Write-Host "  Skipping $FilePath - already has render mode" -ForegroundColor Yellow
        return
    }

    # Find the @page directive
    if ($content -match '(@page\s+"[^"]+")') {
        $pageDirective = $matches[1]

        # Build replacement
        $replacement = $pageDirective
        $replacement += "`n@rendermode $RenderMode"
        if ($AllowAnonymous) {
            $replacement += "`n@attribute [AllowAnonymous]"
        } else {
            $replacement += "`n@attribute [Authorize]"
        }

        # Replace
        $newContent = $content -replace [regex]::Escape($pageDirective), $replacement
        $newContent | Set-Content $FilePath -NoNewline

        Write-Host "  Updated $FilePath with $RenderMode" -ForegroundColor Green
    } else {
        Write-Host "  Could not find @page directive in $FilePath" -ForegroundColor Red
    }
}

Write-Host "`nAdding render modes to pages..." -ForegroundColor Cyan

Write-Host "`n1. Server-rendered pages (Public):" -ForegroundColor Cyan
foreach ($page in $serverPages) {
    $path = Join-Path $pagesDir $page
    if (Test-Path $path) {
        Add-RenderMode -FilePath $path -RenderMode "InteractiveServer" -AllowAnonymous $true
    }
}

Write-Host "`n2. WASM pages (Authenticated):" -ForegroundColor Cyan
foreach ($page in $wasmPages) {
    $path = Join-Path $pagesDir $page
    if (Test-Path $path) {
        Add-RenderMode -FilePath $path -RenderMode "InteractiveWebAssembly" -AllowAnonymous $false
    }
}

Write-Host "`nDone!" -ForegroundColor Green
