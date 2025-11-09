#!/bin/bash
# Bash script to cleanup Sorcha ports on Unix/Mac
# Run this if you get "port already in use" errors

ports=(5128 7080 7081 7082 17256)

echo "Cleaning up Sorcha service ports..."

for port in "${ports[@]}"; do
    pid=$(lsof -ti:$port 2>/dev/null)

    if [ -n "$pid" ]; then
        echo "Port $port is in use by PID $pid"
        kill -9 $pid 2>/dev/null && echo "  ✓ Killed PID $pid" || echo "  ✗ Could not kill PID $pid"
    else
        echo "Port $port is free ✓"
    fi
done

echo ""
echo "Cleanup complete! You can now run dotnet run."
