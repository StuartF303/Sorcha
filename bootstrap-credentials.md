# Sorcha Bootstrap Credentials

## Default Admin Account

**Created by**: Database Initializer (automatic on first startup)

### Credentials

- **Email**: `admin@sorcha.local`
- **Password**: `Dev_Pass_2025!`
- **Display Name**: System Administrator
- **Organization**: Sorcha Local
- **Organization ID**: `00000000-0000-0000-0000-000000000001`
- **User ID**: `00000000-0000-0000-0001-000000000001`

### Roles

The default admin has ALL roles:
- Administrator
- SystemAdmin
- Designer
- Developer
- User
- Consumer
- Auditor

## Login via CLI

The Sorcha CLI requires interactive password entry:

```bash
# Use the 'docker' profile (configured by bootstrap)
sorcha auth login --profile docker

# When prompted:
Username: admin@sorcha.local
Password: Dev_Pass_2025!
```

**Note**: You tried to use `--profile local` and username `stuart`, but:
- The bootstrap script configured the profile as `docker`
- The default admin username is `admin@sorcha.local`

## Login via API (Direct)

```bash
curl -X POST http://localhost:5110/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@sorcha.local",
    "password": "Dev_Pass_2025!"
  }'
```

**Response** (example):
```json
{
  "access_token": "eyJhbGc...",
  "refresh_token": "eyJhbGc...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

## Login via API Gateway

Through the API Gateway (port 80/443):

```bash
curl -X POST http://localhost/api/tenants/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@sorcha.local",
    "password": "Dev_Pass_2025!"
  }'
```

## Service Principals

The database initializer also created service principals for inter-service communication:

| Service | Client ID | Status |
|---------|-----------|--------|
| Blueprint Service | `service-blueprint` | Active |
| Wallet Service | `service-wallet` | Active |
| Register Service | `service-register` | Active |
| Peer Service | `service-peer` | Active |

**Note**: The client secrets were generated and logged during startup but are **NOT persisted in plaintext**. Check tenant-service logs for the secrets:

```bash
docker-compose logs tenant-service | grep "Client Secret"
```

## Configuration Profiles

The bootstrap script created a CLI profile named `docker`:

```bash
# List all profiles
sorcha config list

# Switch to docker profile
sorcha config use docker

# View current profile settings
sorcha config show
```

## Security Notes

⚠️ **IMPORTANT**: These are development credentials!

- **Change the admin password** in production environments
- **Rotate service principal secrets** before deploying to production
- The default password is logged in the tenant-service logs
- Never commit these credentials to source control

## Troubleshooting Login Issues

### Issue: "Authentication failed with status Unauthorized"

**Possible Causes**:
1. Wrong username (must be email format: `admin@sorcha.local`)
2. Wrong password (case-sensitive: `Dev_Pass_2025!`)
3. Wrong profile (use `docker` not `local`)
4. Database not initialized (check logs)

**Solution**:
```bash
# Check if admin user exists
docker-compose exec postgres psql -U sorcha -d sorcha_tenant -c "SELECT email, status FROM user_identities WHERE email='admin@sorcha.local';"

# Check tenant service logs
docker-compose logs tenant-service | grep -i "default admin"

# Verify service is running
curl http://localhost:5110/health
```

### Issue: CLI Password Input Not Working

The CLI requires interactive terminal input and doesn't support password as a command-line argument.

**Workaround**: Use the API directly or authenticate interactively.

---

**Generated**: 2026-01-02
**Bootstrap Script**: `scripts/bootstrap-sorcha.sh`
**Database Initializer**: `src/Services/Sorcha.Tenant.Service/Data/DatabaseInitializer.cs`
