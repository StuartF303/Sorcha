# Sorcha.Admin Configuration Guide

This guide explains how to configure Sorcha.Admin to connect to backend services running on different ports and URLs.

## Quick Start

### Running the App on Different Ports

**Option 1: Use Launch Profiles**

```bash
# Run on default ports (https://localhost:7083)
dotnet run

# Run on HTTP only (http://localhost:5083)
dotnet run --launch-profile http

# Run on HTTPS (https://localhost:7083 + http://localhost:5083)
dotnet run --launch-profile https
```

**Option 2: Override with Command Line**

```bash
# Custom port
dotnet run --urls "https://localhost:9000;http://localhost:9001"

# Specific IP address
dotnet run --urls "https://192.168.1.100:8443"

# Any IP (useful for Docker)
dotnet run --urls "http://0.0.0.0:8080"
```

**Option 3: Edit launchSettings.json**

Edit `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:YOUR_PORT;http://localhost:YOUR_HTTP_PORT"
    }
  }
}
```

## Configuring Backend Service URLs

The Sorcha.Admin app connects to backend services (Tenant, Blueprint, Wallet, Register, Peer). Configure these through **Profiles**.

### Method 1: Use Default Profiles

On first run, 6 default profiles are created:

| Profile | Ports | Description |
|---------|-------|-------------|
| **dev** | 7080-7084 | Local HTTPS development |
| **local** | 5080-5084 | Local HTTP development |
| **docker** | 8080 | Docker Compose (reverse proxy) |
| **aspire** | 7051 | .NET Aspire orchestration |
| **staging** | n0.sorcha.dev | Staging server |
| **production** | *.sorcha.io | Production servers |

**Switch between profiles:**
1. Run the app
2. Click the environment dropdown in the top navigation bar
3. Select your profile (e.g., `dev`, `local`, `docker`)
4. Login with credentials for that environment

### Method 2: Edit Default Profiles

**Using Browser DevTools:**

1. Open Sorcha.Admin in browser
2. Press `F12` → **Application** → **Local Storage** → `https://localhost:7083`
3. Find key: `sorcha:config`
4. Edit the JSON value:

```json
{
  "activeProfile": "dev",
  "profiles": {
    "dev": {
      "name": "dev",
      "tenantServiceUrl": "https://localhost:7080",
      "registerServiceUrl": "https://localhost:7081",
      "peerServiceUrl": "https://localhost:7082",
      "walletServiceUrl": "https://localhost:7083",
      "blueprintServiceUrl": "https://localhost:7084",
      "authTokenUrl": "https://localhost:7080/api/service-auth/token",
      "defaultClientId": "sorcha-admin",
      "verifySsl": false,
      "timeoutSeconds": 30
    }
  }
}
```

5. Click outside the editor to save
6. Refresh the page

**Using Settings UI:**

1. Login to Sorcha.Admin
2. Click user icon → **Settings**
3. Navigate to **Configuration** tab
4. Click **Edit** on the profile you want to change
5. Update service URLs
6. Click **Save**

### Method 3: Create Custom Profile

**For custom ports (e.g., services on ports 9000-9004):**

1. Login to Sorcha.Admin
2. Navigate to **Settings** → **Configuration**
3. Click **New Profile**
4. Fill in the form:
   - **Profile Name**: `my-custom-ports`
   - **Tenant Service URL**: `https://localhost:9000`
   - **Register Service URL**: `https://localhost:9001`
   - **Peer Service URL**: `https://localhost:9002`
   - **Wallet Service URL**: `https://localhost:9003`
   - **Blueprint Service URL**: `https://localhost:9004`
   - **Auth Token URL**: `https://localhost:9000/api/service-auth/token`
   - **Client ID**: `sorcha-admin`
   - **Timeout**: `30` seconds
   - **Verify SSL**: Unchecked (for self-signed certs)
5. Click **Create**
6. Switch to your new profile using the dropdown in the AppBar

### Method 4: Import Configuration File

**From Sorcha CLI:**

If you already have Sorcha CLI configured, you can reuse the same configuration:

**Windows:**
```powershell
# Copy CLI config to clipboard
Get-Content "$env:USERPROFILE\.sorcha\config.json" | clip

# Open browser DevTools (F12)
# Application → Local Storage → sorcha:config
# Paste and save
```

**Linux/macOS:**
```bash
# Copy CLI config to clipboard
cat ~/.sorcha/config.json | pbcopy  # macOS
cat ~/.sorcha/config.json | xclip -selection clipboard  # Linux

# Open browser DevTools (F12)
# Application → Local Storage → sorcha:config
# Paste and save
```

## Common Scenarios

### Scenario 1: All Services on Custom Ports

**Backend running on ports 10000-10004:**

Create a custom profile with these URLs:
- Tenant: `https://localhost:10000`
- Register: `https://localhost:10001`
- Peer: `https://localhost:10002`
- Wallet: `https://localhost:10003`
- Blueprint: `https://localhost:10004`
- Auth Token: `https://localhost:10000/api/service-auth/token`

### Scenario 2: Services Behind Reverse Proxy

**All services behind NGINX on port 8080:**

Create a profile with:
- Tenant: `http://localhost:8080/tenant`
- Register: `http://localhost:8080/register`
- Peer: `http://localhost:8080/peer`
- Wallet: `http://localhost:8080/wallet`
- Blueprint: `http://localhost:8080/blueprint`
- Auth Token: `http://localhost:8080/tenant/api/service-auth/token`

### Scenario 3: Remote Server

**Services on remote server `api.mycompany.com`:**

Create a profile with:
- Tenant: `https://api.mycompany.com/tenant`
- Register: `https://api.mycompany.com/register`
- Peer: `https://api.mycompany.com/peer`
- Wallet: `https://api.mycompany.com/wallet`
- Blueprint: `https://api.mycompany.com/blueprint`
- Auth Token: `https://api.mycompany.com/tenant/api/service-auth/token`
- **Verify SSL**: `true` (enable for production)

### Scenario 4: Docker Compose

**Services running in Docker Compose with port mapping:**

```yaml
# docker-compose.yml
services:
  tenant-service:
    ports:
      - "9080:8080"  # Host:Container
  blueprint-service:
    ports:
      - "9084:8080"
```

Create profile:
- Tenant: `http://localhost:9080`
- Blueprint: `http://localhost:9084`
- Auth Token: `http://localhost:9080/api/service-auth/token`

### Scenario 5: .NET Aspire

**Using .NET Aspire orchestration (AppHost):**

Use the `aspire` default profile, which connects through the API Gateway:
- All services: `https://localhost:7051/api/{service}`

Or customize if your AppHost uses different ports:
```bash
# Check Aspire dashboard for actual ports
# http://localhost:15888
```

## Environment Variables (Advanced)

You can also configure backend URLs via environment variables (useful for containers):

**appsettings.json** (not recommended - use profiles instead):
```json
{
  "DefaultProfile": {
    "TenantServiceUrl": "https://localhost:7080",
    "RegisterServiceUrl": "https://localhost:7081"
  }
}
```

**Environment Variables:**
```bash
# Windows
$env:SORCHA_TENANT_URL="https://localhost:9000"
dotnet run

# Linux/macOS
export SORCHA_TENANT_URL="https://localhost:9000"
dotnet run
```

> **Note:** Profile-based configuration in LocalStorage takes precedence over environment variables.

## CORS Configuration

If backend services are on different domains/ports, ensure CORS is configured on the backend:

**Tenant Service appsettings.json:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:7083",  // Sorcha.Admin
      "https://localhost:8081",  // Alternative port
      "http://localhost:5083"    // HTTP fallback
    ]
  }
}
```

## SSL Certificate Issues

### Self-Signed Certificates (Development)

**Problem:** Browser shows "Your connection is not private"

**Solution 1: Disable SSL verification in profile**
1. Edit profile → Set **Verify SSL** to `false`
2. This only affects API calls, not the browser itself

**Solution 2: Trust the certificate**
```bash
# Windows - Trust dev certificate
dotnet dev-certs https --trust

# Linux/macOS - Export and trust
dotnet dev-certs https -ep ~/localhost.crt --trust
```

**Solution 3: Use mkcert for local development**
```bash
# Install mkcert
choco install mkcert  # Windows
brew install mkcert   # macOS

# Create trusted local CA
mkcert -install

# Generate certificate for localhost
mkcert localhost 127.0.0.1 ::1
```

## Troubleshooting

### Cannot Connect to Backend Service

**Check 1: Verify service is running**
```bash
# Test Tenant Service
curl https://localhost:7080/health

# Expected: {"status":"Healthy"}
```

**Check 2: Verify profile URLs**
1. Settings → Configuration → View active profile
2. Ensure URLs match where services are actually running

**Check 3: Check browser console**
1. Press F12 → Console
2. Look for CORS errors, connection refused, or SSL errors

**Check 4: Test with HTTP instead of HTTPS**
- Some environments only support HTTP
- Edit profile and change `https://` to `http://`
- Disable SSL verification

### Port Already in Use

**Problem:** `Unable to start Kestrel. Address already in use: https://localhost:7083`

**Solution:**
```bash
# Option 1: Kill process using the port (Windows)
netstat -ano | findstr :7083
taskkill /PID <process_id> /F

# Option 2: Use a different port
dotnet run --urls "https://localhost:7090"

# Option 3: Edit launchSettings.json
# Change applicationUrl to different ports
```

### Profile Not Found

**Problem:** "Profile 'dev' not found"

**Solution:**
1. Open browser DevTools → Application → Local Storage
2. Delete key `sorcha:config`
3. Refresh the page - default profiles will be recreated
4. Or manually create the profile in Settings

## Port Reference

### Standard Sorcha Ports

| Service | HTTPS | HTTP | Purpose |
|---------|-------|------|---------|
| Sorcha.Admin | 7083 | 5083 | Blueprint Designer UI |
| Tenant Service | 7080 | 5080 | Authentication, tenants, users |
| Register Service | 7081 | 5081 | Distributed ledger |
| Peer Service | 7082 | 5082 | P2P networking |
| Wallet Service | 7083 | 5083 | Cryptographic wallets |
| Blueprint Service | 7084 | 5084 | Workflow execution |
| API Gateway | 7051 | 5051 | .NET Aspire reverse proxy |
| Aspire Dashboard | 15888 | - | .NET Aspire orchestration UI |

### Docker Compose Ports

| Service | Host Port | Container Port |
|---------|-----------|----------------|
| API Gateway | 8080 | 8080 |
| PostgreSQL | 5432 | 5432 |
| MongoDB | 27017 | 27017 |
| Redis | 6379 | 6379 |

## Examples

### Complete Profile Examples

**Example 1: Custom Development Ports**
```json
{
  "name": "custom-dev",
  "tenantServiceUrl": "https://localhost:9000",
  "registerServiceUrl": "https://localhost:9001",
  "peerServiceUrl": "https://localhost:9002",
  "walletServiceUrl": "https://localhost:9003",
  "blueprintServiceUrl": "https://localhost:9004",
  "authTokenUrl": "https://localhost:9000/api/service-auth/token",
  "defaultClientId": "sorcha-admin",
  "verifySsl": false,
  "timeoutSeconds": 30
}
```

**Example 2: Production with Subdomains**
```json
{
  "name": "production-subdomains",
  "tenantServiceUrl": "https://tenant.mycompany.com",
  "registerServiceUrl": "https://register.mycompany.com",
  "peerServiceUrl": "https://peer.mycompany.com",
  "walletServiceUrl": "https://wallet.mycompany.com",
  "blueprintServiceUrl": "https://blueprint.mycompany.com",
  "authTokenUrl": "https://tenant.mycompany.com/api/service-auth/token",
  "defaultClientId": "sorcha-admin",
  "verifySsl": true,
  "timeoutSeconds": 60
}
```

**Example 3: Kubernetes Ingress**
```json
{
  "name": "k8s-ingress",
  "tenantServiceUrl": "https://sorcha.k8s.local/tenant",
  "registerServiceUrl": "https://sorcha.k8s.local/register",
  "peerServiceUrl": "https://sorcha.k8s.local/peer",
  "walletServiceUrl": "https://sorcha.k8s.local/wallet",
  "blueprintServiceUrl": "https://sorcha.k8s.local/blueprint",
  "authTokenUrl": "https://sorcha.k8s.local/tenant/api/service-auth/token",
  "defaultClientId": "sorcha-admin",
  "verifySsl": true,
  "timeoutSeconds": 45
}
```

## Testing Your Configuration

### 1. Test Backend Connectivity

```bash
# Test each service
curl https://localhost:7080/health  # Tenant
curl https://localhost:7081/health  # Register
curl https://localhost:7082/health  # Peer
curl https://localhost:7083/health  # Wallet
curl https://localhost:7084/health  # Blueprint
```

### 2. Test Authentication

```bash
# Test login endpoint
curl -X POST https://localhost:7080/api/service-auth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@sorcha.local&password=YOUR_PASSWORD&client_id=sorcha-admin"

# Expected response:
# {
#   "access_token": "eyJhbGc...",
#   "token_type": "Bearer",
#   "expires_in": 1800
# }
```

### 3. Test with Sorcha.Admin

1. Open Sorcha.Admin: `https://localhost:7083`
2. Login with `admin@sorcha.local`
3. Check browser console (F12) for errors
4. Try creating a blueprint
5. Check Settings → Configuration to verify active profile

## Additional Resources

- **Main README**: [README.md](README.md)
- **Sorcha Platform Docs**: [../../../README.md](../../../README.md)
- **API Documentation**: [../../../docs/API-DOCUMENTATION.md](../../../docs/API-DOCUMENTATION.md)
- **Troubleshooting**: [../../../TROUBLESHOOTING.md](../../../TROUBLESHOOTING.md)

---

**Need Help?** Check the [README.md](README.md) troubleshooting section or create a GitHub issue.
