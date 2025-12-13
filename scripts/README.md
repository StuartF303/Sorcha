# Sorcha Utility Scripts

Helper scripts for development and troubleshooting.

## bootstrap-database (Automatic)

**⚠️ NEW: Bootstrap seeding is now AUTOMATIC! No manual scripts needed.**

The Tenant Service now automatically seeds the database on first startup with:
- Default organization: "Sorcha Local" (subdomain: `sorcha-local`)
- Default admin user: `admin@sorcha.local` / `Dev_Pass_2025!`
- Service principals for: Blueprint, Wallet, Register, and Peer services

**On first startup, watch the Tenant Service logs for service principal credentials:**
```
Service Principal Created - Blueprint Service
  Client ID:     service-blueprint
  Client Secret: <generated-secret>
  Scopes:        blueprints:read, blueprints:write, wallets:sign, register:write
  ⚠️  SAVE THIS SECRET - It will not be shown again!
```

The client secrets are displayed **only once** during initial seeding. Copy them from the logs and store them securely (e.g., in `.env.local` file, gitignored).

**Configuration Override (Optional):**

You can customize the default credentials via `appsettings.Development.json`:
```json
{
  "Seed": {
    "OrganizationName": "My Company",
    "OrganizationSubdomain": "mycompany",
    "AdminEmail": "admin@mycompany.com",
    "AdminPassword": "My_Secure_Pass_2025!"
  }
}
```

**Security Notes:**
- Default credentials are for **local development only**
- Change password on first login in non-development environments
- Service principal secrets are displayed only during first startup
- For production: Use Azure AD/B2C authentication instead of default credentials

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
