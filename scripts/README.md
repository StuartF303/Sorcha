# Sorcha Utility Scripts

Helper scripts for development and troubleshooting.

## cleanup-ports

Kills processes using Sorcha service ports to resolve "port already in use" errors.

### Windows (PowerShell)
```powershell
.\scripts\cleanup-ports.ps1
```

### Unix/Mac/Git Bash
```bash
bash scripts/cleanup-ports.sh
```

### Ports Cleaned
- 8050, 8051 - Blueprint Service
- 8060, 8061 - API Gateway
- 8070, 8071 - Peer Service
- 8080, 8081 - Blazor Client
- 17256 - Aspire Dashboard

## When to Use

Run cleanup script when you see:
- "port already in use" errors
- "bind: Only one usage of each socket address" errors
- Services won't start after Ctrl+C
- Aspire dashboard won't start

## Troubleshooting

If scripts don't work:
```powershell
# Windows - nuclear option
taskkill /F /IM dotnet.exe

# Unix/Mac - nuclear option
killall -9 dotnet
```

Then restart Docker Desktop and try again.
