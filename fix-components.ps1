# Fix all MudBlazor component type inference errors

$pagesDir = "C:\projects\Sorcha\src\Apps\Sorcha.Admin\Pages"

# Get all .razor files recursively
Get-ChildItem -Path $pagesDir -Filter *.razor -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw

    # Fix MudTextField if it doesn't have a type yet
    if ($content -match '<MudTextField\s+(?!T=)') {
        $content = $content -replace '<MudTextField\s+', '<MudTextField T="string" '
    }

    # Fix Dev/Events.razor - replace Components.ActivityLog with @using statement
    if ($_.Name -eq "Events.razor") {
        if ($content -notmatch '@using Sorcha.Admin.Components') {
            $content = "@using Sorcha.Admin.Components`n" + $content
        }
    }

    # Save the file
    Set-Content -Path $_.FullName -Value $content -NoNewline
}

Write-Host "Fixed all component type parameters"
