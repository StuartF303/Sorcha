# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Sorcha Contributors
# Script to add missing SPDX license headers to all .cs files in src/

$header = @"
// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
"@

$files = Get-ChildItem -Path "src" -Recurse -Filter "*.cs"
$updated = 0
$skipped = 0

foreach ($file in $files) {
    $firstLine = Get-Content $file.FullName -TotalCount 1
    if ($firstLine -eq "// SPDX-License-Identifier: MIT") {
        $skipped++
        continue
    }
    $content = Get-Content $file.FullName -Raw
    $newContent = $header + "`n" + $content
    Set-Content -Path $file.FullName -Value $newContent -NoNewline
    $updated++
}

Write-Host "Updated: $updated files"
Write-Host "Skipped (already had header): $skipped files"
Write-Host "Total scanned: $($files.Count) files"
