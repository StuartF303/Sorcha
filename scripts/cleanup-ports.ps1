# PowerShell script to cleanup Sorcha ports
# Run this if you get "port already in use" errors

$ports = @(5128, 7080, 7081, 7082, 17256)

Write-Host "Cleaning up Sorcha service ports..." -ForegroundColor Cyan

foreach ($port in $ports) {
    $connections = netstat -ano | findstr ":$port"

    if ($connections) {
        Write-Host "`nPort $port is in use:" -ForegroundColor Yellow
        Write-Host $connections

        # Extract PIDs
        $pids = $connections | ForEach-Object {
            if ($_ -match '\s+(\d+)\s*$') {
                $matches[1]
            }
        } | Select-Object -Unique

        foreach ($pid in $pids) {
            try {
                $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
                if ($process) {
                    Write-Host "  Killing PID $pid ($($process.ProcessName))..." -ForegroundColor Red
                    Stop-Process -Id $pid -Force
                    Write-Host "  ✓ Killed PID $pid" -ForegroundColor Green
                }
            }
            catch {
                Write-Host "  ✗ Could not kill PID $pid" -ForegroundColor Red
            }
        }
    }
    else {
        Write-Host "Port $port is free ✓" -ForegroundColor Green
    }
}

Write-Host "`nCleanup complete! You can now run dotnet run." -ForegroundColor Cyan
