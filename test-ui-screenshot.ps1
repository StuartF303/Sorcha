# Simple PowerShell script to launch browser and take screenshot
# This script requires Windows and the Windows.Forms assembly

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Function to take screenshot
function Take-Screenshot {
    param(
        [string]$FilePath = "C:\Projects\Sorcha\ui-screenshot.png"
    )

    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bitmap = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($screen.Location, [System.Drawing.Point]::Empty, $screen.Size)
    $bitmap.Save($FilePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()

    Write-Host "Screenshot saved to: $FilePath"
}

# Kill any existing Edge processes to start fresh
Get-Process msedge -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Start Edge with the UI (kiosk mode for clean screenshot)
Start-Process "msedge" "--kiosk https://localhost:7083/ --edge-kiosk-type=fullscreen" -PassThru

# Wait for page to fully load (longer wait for WASM)
Start-Sleep -Seconds 10

# Take screenshot
Take-Screenshot

# Close Edge
Start-Sleep -Seconds 2
Get-Process msedge -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Test complete!"
