# Tenant Service Authorization Issue - Root Cause Analysis

**Date:** 2026-01-04
**Status:** 🔍 Under Investigation
**Issue:** HTTP 401 Unauthorized on RequireAdministrator endpoints despite valid admin token

---

## Summary

The Tenant Service is returning 401 Unauthorized for endpoints protected by the `RequireAdministrator` policy, even when a valid admin JWT token is provided. The admin user has the Administrator role in the database, tokens are being issued successfully, and JWT configuration appears correct across all services.

---

## Investigation Steps Completed

### 1. ✅ Verified JWT Configuration

**Both Tenant and Wallet Services share identical JWT settings:**

```yaml
# docker-compose.yml lines 17-19
x-jwt-env: &jwt-env
  JwtSettings__InstallationName: ${INSTALLATION_NAME:-localhost}
  JwtSettings__SigningKey: ${JWT_SIGNING_KEY:-c29yY2hhLWRvY2tlci1kZXYta2V5LTIwMjUtbWluLTI1Ni1iaXRzLXNlY3VyZQ==}
```

**Result:** ✅ Configuration is identical and correct

### 2. ✅ Verified Token Issuance

**Token Service (Tenant Service):**
- Uses `JwtConfiguration` options pattern
- Configured via `ConfigureJwtForTokenIssuance()` extension method
- Derives issuer/audience from `InstallationName` if not explicitly set

**Code Evidence:**
```csharp
// AuthenticationExtensions.cs:96
options.Issuer = $"http://{installationName}";  // = "http://localhost"

// AuthenticationExtensions.cs:137
options.Audiences = [$"http://{installationName}"];  // = ["http://localhost"]

// TokenService.cs:407-408
Issuer = _config.Issuer,  // "http://localhost"
Audience = _config.Audiences.FirstOrDefault(),  // "http://localhost"
```

**Result:** ✅ Tokens are issued with correct issuer and audience

### 3. ✅ Verified Token Validation

**JWT Authentication Middleware (ServiceDefaults):**
- Uses shared `JwtSettings` from ServiceDefaults
- Also derives issuer/audience from `InstallationName`

**Code Evidence:**
```csharp
// JwtAuthenticationExtensions.cs:133
jwtSettings.Issuer = $"http://{installationName}";  // = "http://localhost"

// JwtAuthenticationExtensions.cs:138
jwtSettings.Audience = [$"http://{installationName}"];  // = ["http://localhost"]

// JwtAuthenticationExtensions.cs:181-184
ValidateIssuer = jwtSettings.ValidateIssuer,  // true
ValidIssuer = jwtSettings.Issuer,  // "http://localhost"
ValidateAudience = jwtSettings.ValidateAudience,  // true
ValidAudiences = jwtSettings.Audience,  // ["http://localhost"]
```

**Result:** ✅ Validation parameters match issuance

### 4. ✅ Verified Role Claims

**Admin user has all roles including Administrator:**
```csharp
// DatabaseInitializer.cs:169-177
Roles = new[]
{
    UserRole.Administrator,  // ← Required for policy
    UserRole.SystemAdmin,
    UserRole.Designer,
    UserRole.Developer,
    UserRole.User,
    UserRole.Consumer,
    UserRole.Auditor
}
```

**Roles added to token:**
```csharp
// TokenService.cs:88-91
foreach (var role in user.Roles)
{
    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
}
```

**Result:** ✅ Administrator role is in the token

### 5. ✅ Verified Authorization Policy

**Policy correctly requires Administrator role:**
```csharp
// AuthenticationExtensions.cs:159-160
options.AddPolicy("RequireAdministrator", policy =>
    policy.RequireRole("Administrator"));
```

**Endpoint correctly requires policy:**
```csharp
// OrganizationEndpoints.cs:96-100
group.MapPost("/{organizationId:guid}/users", AddUserToOrganization)
    .RequireAuthorization("RequireAdministrator")
```

**Result:** ✅ Policy configuration is correct

### 6. ✅ Verified Middleware Order

**Authentication runs before Authorization:**
```csharp
// Program.cs:269-270
app.UseAuthentication();  // ← First
app.UseAuthorization();   // ← Then this
```

**Result:** ✅ Middleware order is correct

### 7. ❌ Debug Logging Not Appearing

**Expected logs (from JwtAuthenticationExtensions.cs:196-227):**
- `OnAuthenticationFailed` → Should log if JWT validation fails
- `OnTokenValidated` → Should log when token is successfully validated

**Actual logs:**
- ✅ "Generated tokens for user..." (token issuance works)
- ✅ "User logged in successfully..." (login works)
- ❌ No "Token validated" messages
- ❌ No "JWT authentication failed" messages
- ✅ "HTTP POST /api/organizations/.../users responded 401 in 23.52ms"

**Analysis:** The JWT Bearer middleware events are **not firing at all**, suggesting:
1. JWT middleware is not attempting validation, OR
2. Request is being rejected before JWT middleware runs, OR
3. Authorization header is not present/malformed

**Result:** ⚠️ **Critical Finding** - JWT middleware not processing the request

---

## Current Hypothesis

**The JWT Bearer middleware is not processing the authorization request.**

### Possible Causes:

1. **Authorization header not being sent**
   - PowerShell script may not be sending Authorization header correctly
   - Need to verify actual HTTP request headers

2. **Authorization header malformed**
   - Header might not have "Bearer " prefix
   - Token might be corrupted or truncated

3. **Different authentication scheme being used**
   - Something else might be intercepting the request
   - Another middleware might be rejecting before JWT runs

4. **Rate limiting or CORS**
   - RateLimiter middleware might be rejecting requests
   - CORS might be preventing header from being sent

5. **JWT Bearer middleware not registered correctly**
   - Despite configuration looking correct, might not be active
   - Might need explicit `.RequireAuthorization(JwtBearerDefaults.AuthenticationScheme)`

---

## Next Steps

### Immediate Actions

1. **Verify Authorization Header**
   - Use Fiddler/Postman to capture actual HTTP request
   - Check if "Authorization: Bearer {token}" header is present
   - Verify token is complete and not truncated

2. **Test with curl**
   ```bash
   # Get token
   TOKEN=$(curl -s -X POST http://localhost:5110/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@sorcha.local","password":"Dev_Pass_2025!"}' \
     | jq -r '.accessToken')

   # Try create user with verbose output
   curl -v -X POST \
     http://localhost:5110/api/organizations/00000000-0000-0000-0000-000000000001/users \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","displayName":"Test","password":"Pass123!","roles":["Member"]}'
   ```

3. **Add explicit authentication scheme to endpoint**
   ```csharp
   // Try changing from:
   .RequireAuthorization("RequireAdministrator")

   // To:
   .RequireAuthorization(new AuthorizeAttribute {
       Policy = "RequireAdministrator",
       AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
   })
   ```

4. **Add request logging middleware**
   - Log all incoming headers
   - Verify Authorization header is present
   - Check what middleware is rejecting the request

5. **Test without authorization**
   - Temporarily remove `.RequireAuthorization()` from endpoint
   - See if request reaches endpoint handler
   - This will confirm if issue is auth/authz vs something else

---

## Environment Details

**Services:**
- Tenant Service: sorcha-tenant-service (Docker)
- Wallet Service: sorcha-wallet-service (Docker)
- PostgreSQL: sorcha-postgres (Docker)
- Redis: sorcha-redis (Docker)

**Configuration:**
- InstallationName: localhost
- Issuer: http://localhost
- Audience: http://localhost
- SigningKey: (shared via environment variable)

**Versions:**
- .NET: 10.0
- ASP.NET Core: 10.0
- Microsoft.AspNetCore.Authentication.JwtBearer: (from ServiceDefaults)

---

## Related Files

- **JWT Configuration:**
  - [src/Common/Sorcha.ServiceDefaults/JwtAuthenticationExtensions.cs](../../../src/Common/Sorcha.ServiceDefaults/JwtAuthenticationExtensions.cs)
  - [src/Services/Sorcha.Tenant.Service/Extensions/AuthenticationExtensions.cs](../../../src/Services/Sorcha.Tenant.Service/Extensions/AuthenticationExtensions.cs)

- **Service Configuration:**
  - [src/Services/Sorcha.Tenant.Service/Program.cs](../../../src/Services/Sorcha.Tenant.Service/Program.cs)
  - [docker-compose.yml](../../../docker-compose.yml)

- **Endpoints:**
  - [src/Services/Sorcha.Tenant.Service/Endpoints/OrganizationEndpoints.cs](../../../src/Services/Sorcha.Tenant.Service/Endpoints/OrganizationEndpoints.cs)

- **Investigation Documents:**
  - [JWT-ANALYSIS.md](./JWT-ANALYSIS.md) - Complete JWT configuration analysis
  - [FINAL-TEST-RESULTS.md](./FINAL-TEST-RESULTS.md) - Test results
  - [TEST-RUN-RESULTS.md](./TEST-RUN-RESULTS.md) - Initial investigation

---

## Timeline

- **01:05 UTC** - Issue identified during walkthrough testing
- **01:10 UTC** - JWT configuration verified across services
- **01:15 UTC** - Middleware order confirmed correct
- **01:20 UTC** - Debug logging enabled, but JWT events not firing
- **01:25 UTC** - Hypothesis: JWT middleware not processing request

---

**Status:** Investigation ongoing. Next step is to verify Authorization header in actual HTTP request.
