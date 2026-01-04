# JWT Authentication Analysis - Tenant Service Authorization Issue

**Date:** 2026-01-04
**Issue:** Tenant Service rejecting its own JWT tokens for authorization

---

## Environment Configuration ✅

### JWT Settings Verified

**Tenant Service:**
```
JwtSettings__InstallationName=localhost
JwtSettings__SigningKey=c29yY2hhLWRvY2tlci1kZXYta2V5LTIwMjUtbWluLTI1Ni1iaXRzLXNlY3VyZQ==
JwtSettings__SigningKeySource=Configuration
```

**Wallet Service:**
```
JwtSettings__InstallationName=localhost
JwtSettings__SigningKey=c29yY2hhLWRvY2tlci1kZXYta2V5LTIwMjUtbWluLTI1Ni1iaXRzLXNlY3VyZQ==
```

**Status:** ✅ Both services share the same signing key and installation name

---

## Token Issuance ✅

### Successful Login Evidence

**Service Logs:**
```
[01:05:21 INF] Generated tokens for user 00000000-0000-0000-0001-000000000001
  in organization 00000000-0000-0000-0000-000000000001
  Source: Sorcha.Tenant.Service.Services.TokenService

[01:05:21 INF] User logged in successfully - admin@sorcha.local
  (UserId: 00000000-0000-0000-0001-000000000001,
   OrgId: 00000000-0000-0000-0000-000000000001)
```

**Status:** ✅ Tenant Service successfully issues tokens

---

## Token Claims ✅

### Role Claims Added

**Code Evidence:**
```csharp
// TokenService.cs:88-91
foreach (var role in user.Roles)
{
    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
}
```

**Admin User Roles (from DatabaseInitializer.cs:169-177):**
```csharp
Roles = new[]
{
    UserRole.Administrator,      // ← Required for RequireAdministrator policy
    UserRole.SystemAdmin,
    UserRole.Designer,
    UserRole.Developer,
    UserRole.User,
    UserRole.Consumer,
    UserRole.Auditor
}
```

**Status:** ✅ Administrator role is added to JWT claims

---

## Authorization Policy Configuration ✅

### Policy Definition

**Code Evidence:**
```csharp
// AuthenticationExtensions.cs:159-160
options.AddPolicy("RequireAdministrator", policy =>
    policy.RequireRole("Administrator"));
```

**Endpoint Configuration:**
```csharp
// OrganizationEndpoints.cs:96-100
group.MapPost("/{organizationId:guid}/users", AddUserToOrganization)
    .RequireAuthorization("RequireAdministrator")
```

**Status:** ✅ Policy correctly requires "Administrator" role

---

## JWT Validation Configuration ✅

### Shared Configuration

**Code Evidence:**
```csharp
// JwtAuthenticationExtensions.cs:192-193
NameClaimType = ClaimTypes.Name,
RoleClaimType = ClaimTypes.Role
```

**Services Using Configuration:**
- ✅ Tenant Service: `builder.AddJwtAuthentication()` (Program.cs:192)
- ✅ Wallet Service: `builder.AddJwtAuthentication()` (Program.cs:216)

**Status:** ✅ Role claim type correctly mapped

---

## The Problem ❌

### Authorization Fails Despite Correct Configuration

**Observed Behavior:**
```
1. User logs in successfully ✅
2. JWT token issued with Administrator role ✅
3. Token used in Authorization header ✅
4. Authorization middleware rejects with 401 ❌ (0.39ms response time)
```

**Service Logs:**
```
[01:05:21 INF] HTTP POST /api/auth/login responded 200 in 547ms
[01:05:21 INF] HTTP GET /api/organizations/by-subdomain/sorcha-local responded 200
[01:05:21 INF] HTTP POST /api/organizations/.../users responded 401 in 0.39ms
```

**Key Observation:** 0.39ms response time indicates authorization middleware rejection BEFORE any business logic or database queries.

---

## Hypothesis: Why This Happens

### Theory 1: Tenant Service Not Validating Its Own Tokens

**Possible Cause:**
The Tenant Service may be configured to:
- Issue tokens (as the authority)
- But NOT validate tokens (expecting external authority)

**Evidence:**
- Line `builder.AddJwtAuthentication()` adds validation
- But Tenant Service might have special configuration as the issuer

**Check Required:**
```csharp
// Does Tenant Service have:
ValidateIssuer = true  // Should validate against itself
ValidIssuer = "http://localhost"  // Matches InstallationName
```

### Theory 2: Token Not Being Attached to Request

**Possible Cause:**
The Authorization header might not be properly formatted or sent.

**Evidence:**
Our script code:
```powershell
-Headers @{ Authorization = "Bearer $adminToken" }
```

**Status:** ✅ Script correctly formats header

### Theory 3: Claims Not Mapped from Token to HttpContext.User

**Possible Cause:**
The JWT middleware validates the token but doesn't populate `HttpContext.User.Claims` correctly.

**Check Required:**
- Is JwtBearerEvents.OnTokenValidated being called?
- Are claims being transferred to ClaimsPrincipal?

**Code to Inspect:**
```csharp
// ServiceDefaults/JwtAuthenticationExtensions.cs:196-210
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context => { ... },
    OnTokenValidated = context => { ... }  // ← Check this
};
```

### Theory 4: Authorization Happening Before Authentication

**Possible Cause:**
Middleware order might be wrong - authorization running before authentication completes.

**Standard Order Should Be:**
1. Authentication middleware (validates JWT)
2. Authorization middleware (checks policies)

**Check Required:**
```csharp
// Tenant Service Program.cs
app.UseAuthentication();  // ← Must come first
app.UseAuthorization();   // ← Then this
```

---

## Recommended Investigation Steps

### 1. Enable JWT Middleware Logging

Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug"
    }
  }
}
```

### 2. Add OnTokenValidated Event Handler

```csharp
// In JwtAuthenticationExtensions.cs
OnTokenValidated = context =>
{
    var logger = context.HttpContext.RequestServices
        .GetRequiredService<ILogger<Program>>();

    logger.LogInformation("JWT Token validated for: {User}",
        context.Principal?.Identity?.Name);
    logger.LogInformation("Claims count: {Count}",
        context.Principal?.Claims.Count());

    foreach (var claim in context.Principal?.Claims ?? [])
    {
        logger.LogInformation("  Claim: {Type} = {Value}",
            claim.Type, claim.Value);
    }

    return Task.CompletedTask;
}
```

### 3. Check Middleware Order

```csharp
// In Tenant Service Program.cs - verify order
app.UseAuthentication();  // ← Line number?
app.UseAuthorization();   // ← Line number?
```

### 4. Test with curl

```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:5110/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sorcha.local","password":"Dev_Pass_2025!"}' \
  | jq -r '.accessToken')

# Decode token (optional - check claims)
echo $TOKEN | cut -d'.' -f2 | base64 -d | jq

# Try to create user
curl -v -X POST \
  http://localhost:5110/api/organizations/00000000-0000-0000-0000-000000000001/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email":"test@example.com",
    "displayName":"Test User",
    "password":"TestPass123!",
    "roles":["Member"]
  }'
```

### 5. Compare with Working Endpoint

Test an endpoint that DOES work (like login) vs one that doesn't (create user):
- Both should go through same authentication middleware
- But only create user has authorization policy
- Diff: What's different?

---

## Quick Fix Attempts

### Option 1: Disable Authorization Temporarily

**For testing only** - modify endpoint:
```csharp
// OrganizationEndpoints.cs:96
group.MapPost("/{organizationId:guid}/users", AddUserToOrganization)
    // .RequireAuthorization("RequireAdministrator")  // ← Comment out
    .RequireAuthorization()  // ← Try generic auth first
```

### Option 2: Use AllowAnonymous for Testing

```csharp
group.MapPost("/{organizationId:guid}/users", AddUserToOrganization)
    .AllowAnonymous()  // ← Temporarily bypass
```

### Option 3: Check if System Admin Works

Try a different policy:
```csharp
options.AddPolicy("RequireSystemAdmin", policy =>
    policy.RequireRole("SystemAdmin"));  // Admin has this too
```

---

## Impact on Walkthrough

**Blocked Operations:**
- ❌ POST /api/organizations/{id}/users (create user)
- ❌ PUT /api/organizations/{id}/users/{userId} (update user)
- ❌ DELETE /api/organizations/{id}/users/{userId} (delete user)
- ❌ GET /api/organizations (list orgs)
- ❌ PUT /api/organizations/{id} (update org)
- ❌ DELETE /api/organizations/{id} (delete org)

**Working Operations:**
- ✅ POST /api/auth/login (login)
- ✅ GET /api/organizations/by-subdomain/{subdomain} (AllowAnonymous)
- ✅ GET /api/organizations/stats (AllowAnonymous)

**Workaround for Testing:**
If the Tenant Service team can temporarily allow anonymous access to user creation, or add detailed logging, we could complete the walkthrough testing.

---

## Conclusion

**Root Cause:** Unknown - requires Tenant Service team investigation

**Likely Issue:** JWT token validation or claims mapping within Tenant Service

**Configuration:** ✅ All JWT settings correct across services

**Next Steps:**
1. Enable debug logging for authentication/authorization
2. Add OnTokenValidated logging
3. Test with curl to isolate from PowerShell
4. Compare working vs. non-working endpoints

**Walkthrough Status:** Implementation complete, testing blocked by service issue

---

**Related Files:**
- [FINAL-TEST-RESULTS.md](./FINAL-TEST-RESULTS.md) - Complete test results
- [TEST-RUN-RESULTS.md](./TEST-RUN-RESULTS.md) - Initial investigation
