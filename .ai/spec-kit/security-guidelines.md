# Security Guidelines

**Version:** 1.1.0
**Status:** MANDATORY
**Audience:** All developers and AI assistants

**⚠️ IMPORTANT**: Authentication, Authorization, and Rate Limiting are NOT yet implemented in the current codebase. This document describes required future implementation.

---

## Overview

This document defines the security requirements and best practices for the Sorcha project. Security is a critical concern, and all code MUST adhere to these guidelines to protect against common vulnerabilities and attacks.

---

## 1. OWASP Top 10 Protection

### A01: Broken Access Control

#### ⚠️ NOT YET IMPLEMENTED (Planned for Future)

**Current Status**: No authorization checks are currently implemented. All endpoints are publicly accessible.

#### Future Implementation: Authorization Checks

```csharp
// PLANNED: Verify user has permission
app.MapDelete("/api/blueprints/{id}", async (
    string id,
    ClaimsPrincipal user,
    IBlueprintRepository repo) =>
{
    var blueprint = await repo.GetByIdAsync(id);
    if (blueprint == null)
        return Results.NotFound();

    // Verify ownership
    if (blueprint.OwnerId != user.GetUserId())
        return Results.Forbid();

    await repo.DeleteAsync(id);
    return Results.NoContent();
})
.RequireAuthorization(); // REQUIRED

// ❌ WRONG: No authorization check
app.MapDelete("/api/blueprints/{id}", async (string id, IBlueprintRepository repo) =>
{
    await repo.DeleteAsync(id); // VIOLATION - Anyone can delete
    return Results.NoContent();
});
```

#### Resource-Level Authorization

```csharp
// ✅ CORRECT: Check access to specific resource
public async Task<Blueprint?> GetBlueprintAsync(string blueprintId, string userId)
{
    var blueprint = await _repository.GetByIdAsync(blueprintId);

    if (blueprint == null)
        return null;

    // Verify user has access
    if (!blueprint.HasAccess(userId))
    {
        _logger.LogWarning(
            "User {UserId} attempted to access unauthorized blueprint {BlueprintId}",
            userId, blueprintId);
        throw new ForbiddenException("Access denied");
    }

    return blueprint;
}
```

### A02: Cryptographic Failures

#### ✅ REQUIRED: Secure Data Storage

```csharp
// ✅ CORRECT: Use Data Protection API
public class SecureDataService
{
    private readonly IDataProtector _protector;

    public SecureDataService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Sorcha.Blueprint.SecureData");
    }

    public string EncryptSensitiveData(string plaintext)
    {
        return _protector.Protect(plaintext);
    }

    public string DecryptSensitiveData(string ciphertext)
    {
        return _protector.Unprotect(ciphertext);
    }
}

// ❌ WRONG: Storing sensitive data in plain text
public class InsecureService
{
    public void SaveApiKey(string apiKey)
    {
        _config["ApiKey"] = apiKey; // VIOLATION - Plain text
    }
}
```

#### Secrets Management

```csharp
// ✅ CORRECT: Use Azure Key Vault in production
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());

// Development: User Secrets
// NEVER commit secrets to source control
builder.Configuration.AddUserSecrets<Program>();

// ❌ WRONG: Hardcoded secrets
const string ApiKey = "sk_live_abc123xyz"; // VIOLATION
const string ConnectionString = "Server=...;Password=SecretPass;"; // VIOLATION
```

### A03: Injection

#### ✅ REQUIRED: Input Validation

```csharp
// ✅ CORRECT: Parameterized queries (EF Core)
var blueprints = await context.Blueprints
    .Where(b => b.Title.Contains(searchTerm)) // Safe - parameterized
    .ToListAsync();

// ✅ CORRECT: Validate and sanitize input
public async Task<IResult> CreateBlueprint(
    BlueprintRequest request,
    IValidator<BlueprintRequest> validator)
{
    // Validate
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
        return Results.ValidationProblem(validationResult.ToDictionary());

    // Sanitize
    request = request with
    {
        Title = HtmlEncoder.Default.Encode(request.Title),
        Description = HtmlEncoder.Default.Encode(request.Description)
    };

    // Process...
}

// ❌ WRONG: String concatenation in queries
var query = $"SELECT * FROM Blueprints WHERE Title = '{searchTerm}'"; // SQL INJECTION

// ❌ WRONG: Executing user input
var result = eval(userInput); // CODE INJECTION

// ❌ WRONG: Command injection
var process = Process.Start("bash", $"-c {userInput}"); // COMMAND INJECTION
```

#### JSON Schema Validation

```csharp
// ✅ REQUIRED: Validate all blueprint JSON against schema
public async Task<ValidationResult> ValidateBlueprintAsync(JsonNode blueprintJson)
{
    var schema = await _schemaRepository.GetBlueprintSchemaAsync();

    var evaluationResult = schema.Evaluate(blueprintJson, new EvaluationOptions
    {
        OutputFormat = OutputFormat.List
    });

    if (!evaluationResult.IsValid)
    {
        _logger.LogWarning(
            "Blueprint validation failed: {Errors}",
            evaluationResult.Errors);

        throw new BlueprintValidationException(
            "Blueprint JSON does not conform to schema");
    }

    return ValidationResult.Success;
}
```

#### XSS Prevention

```csharp
// ✅ CORRECT: Encode output in Blazor
<div>
    @blueprint.Title  <!-- Automatically encoded -->
</div>

// For raw HTML (use sparingly)
@((MarkupString)HtmlEncoder.Default.Encode(userContent))

// ❌ WRONG: Raw HTML from user input
<div>
    @((MarkupString)userContent)  <!-- XSS VULNERABILITY -->
</div>
```

### A04: Insecure Design

#### Security by Design Principles

1. **Least Privilege**: Grant minimum necessary permissions
2. **Defense in Depth**: Multiple layers of security
3. **Fail Securely**: Default to deny access
4. **Separation of Duties**: Require multiple approvals for sensitive actions

```csharp
// ✅ CORRECT: Secure by default
public class BlueprintExecutionService
{
    public async Task<ExecutionResult> ExecuteAsync(
        Blueprint blueprint,
        ExecutionContext context)
    {
        // 1. Validate user permissions
        if (!await _authService.CanExecuteAsync(context.UserId, blueprint.Id))
            throw new UnauthorizedException();

        // 2. Validate blueprint schema
        await _validator.ValidateAsync(blueprint);

        // 3. Apply disclosure rules
        var visibleData = _disclosureService.ApplyRules(
            blueprint.Data,
            context.ParticipantId);

        // 4. Execute with audit logging
        _auditLogger.LogExecution(context.UserId, blueprint.Id);

        // 5. Rate limiting
        await _rateLimiter.CheckLimitAsync(context.UserId);

        return await ExecuteInternalAsync(blueprint, visibleData);
    }
}
```

### A05: Security Misconfiguration

#### ✅ REQUIRED: Security Headers

```csharp
// REQUIRED in all web applications
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Prevent MIME sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // XSS protection
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // Content Security Policy
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");

    // HSTS (HTTPS only)
    context.Response.Headers.Append(
        "Strict-Transport-Security",
        "max-age=31536000; includeSubDomains");

    // Referrer policy
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Permissions policy
    context.Response.Headers.Append(
        "Permissions-Policy",
        "geolocation=(), microphone=(), camera=()");

    await next();
});
```

#### HTTPS Enforcement

```csharp
// ✅ REQUIRED: Enforce HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

#### CORS Configuration

```csharp
// ✅ CORRECT: Restrictive CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(
                "https://app.sorcha.dev",
                "https://designer.sorcha.dev")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

app.UseCors("AllowedOrigins");

// ❌ WRONG: Permissive CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // VIOLATION
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
```

### A06: Vulnerable and Outdated Components

#### ✅ REQUIRED: Dependency Management

```bash
# Run regularly to check for vulnerabilities
dotnet list package --vulnerable --include-transitive

# Update packages regularly
dotnet outdated

# Restore packages from trusted sources only
dotnet restore --locked-mode
```

#### Package Validation

```xml
<!-- REQUIRED: Enable NuGet package validation -->
<PropertyGroup>
  <EnablePackageValidation>true</EnablePackageValidation>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditMode>all</NuGetAuditMode>
  <NuGetAuditLevel>low</NuGetAuditLevel>
</PropertyGroup>
```

### A07: Identification and Authentication Failures

#### ⚠️ NOT YET IMPLEMENTED (Planned for Future)

**Current Status**: No authentication or authorization is currently implemented. All API endpoints are publicly accessible in development.

#### Future Implementation: Strong Authentication

```csharp
// PLANNED: Use ASP.NET Core Identity or external providers
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://identity.sorcha.dev";
        options.Audience = "sorcha-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

// ❌ WRONG: Custom authentication without proper validation
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"];
    if (token == "Bearer secret123") // VIOLATION
    {
        context.User = new ClaimsPrincipal(); // Insecure
    }
    await next();
});
```

#### Session Management

```csharp
// ✅ CORRECT: Secure session configuration
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;       // Prevent XSS
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;          // CSRF protection
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});
```

#### Password Requirements (if applicable)

```csharp
// ✅ REQUIRED: Strong password policy
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12; // Minimum 12 characters
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});
```

### A08: Software and Data Integrity Failures

#### ✅ REQUIRED: Code Signing and Verification

```csharp
// Verify package signatures
dotnet nuget verify <package-path>

// Sign assemblies in CI/CD
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com assembly.dll
```

#### Anti-Tampering

```csharp
// ✅ CORRECT: Verify data integrity
public class DataIntegrityService
{
    public string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyHash(byte[] data, string expectedHash)
    {
        var actualHash = ComputeHash(data);
        return actualHash == expectedHash;
    }
}
```

### A09: Security Logging and Monitoring Failures

#### ✅ REQUIRED: Audit Logging

```csharp
// ✅ CORRECT: Log security events
public class AuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public void LogAuthentication(string userId, bool success)
    {
        _logger.LogInformation(
            "Authentication {Status} for user {UserId}",
            success ? "succeeded" : "failed",
            userId);
    }

    public void LogAuthorizationFailure(string userId, string resource)
    {
        _logger.LogWarning(
            "User {UserId} denied access to {Resource}",
            userId,
            resource);
    }

    public void LogDataAccess(string userId, string resourceId)
    {
        _logger.LogInformation(
            "User {UserId} accessed resource {ResourceId}",
            userId,
            resourceId);
    }

    public void LogDataModification(string userId, string resourceId, string action)
    {
        _logger.LogInformation(
            "User {UserId} {Action} resource {ResourceId}",
            userId,
            action,
            resourceId);
    }
}
```

#### Security Monitoring

```csharp
// REQUIRED: Monitor for suspicious activity
public class SecurityMonitor
{
    private readonly ILogger<SecurityMonitor> _logger;

    public void MonitorFailedLogins(string userId, int failedAttempts)
    {
        if (failedAttempts >= 3)
        {
            _logger.LogWarning(
                "Multiple failed login attempts for user {UserId}: {Count}",
                userId,
                failedAttempts);

            // Trigger alert
            _alertService.TriggerSecurityAlert(
                AlertLevel.Warning,
                $"Brute force attack suspected for user {userId}");
        }
    }

    public void MonitorRateLimitViolations(string ipAddress, int violations)
    {
        if (violations >= 5)
        {
            _logger.LogError(
                "Rate limit violations from IP {IpAddress}: {Count}",
                ipAddress,
                violations);

            // Block IP
            _firewallService.BlockIpAddress(ipAddress);
        }
    }
}
```

#### Sensitive Data Protection

```csharp
// ✅ CORRECT: Never log sensitive data
_logger.LogInformation(
    "User {UserId} logged in",
    userId); // OK

// ❌ WRONG: Logging sensitive data
_logger.LogInformation(
    "User logged in with password {Password}",
    password); // VIOLATION - Never log passwords

_logger.LogInformation(
    "Credit card {CardNumber} processed",
    cardNumber); // VIOLATION - Never log PII

// ✅ CORRECT: Log with masking
_logger.LogInformation(
    "Credit card ending in {Last4} processed",
    cardNumber.Substring(cardNumber.Length - 4));
```

### A10: Server-Side Request Forgery (SSRF)

#### ✅ REQUIRED: Validate URLs

```csharp
// ✅ CORRECT: Validate and whitelist URLs
public async Task<string> FetchExternalDataAsync(string url)
{
    // Validate URL format
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        throw new ArgumentException("Invalid URL");

    // Whitelist allowed hosts
    var allowedHosts = new[] { "api.trusted.com", "data.sorcha.dev" };
    if (!allowedHosts.Contains(uri.Host))
        throw new SecurityException($"Host {uri.Host} not allowed");

    // Prevent internal network access
    var address = Dns.GetHostAddresses(uri.Host).FirstOrDefault();
    if (address != null && IsInternalIp(address))
        throw new SecurityException("Internal IP addresses not allowed");

    using var client = _httpClientFactory.CreateClient();
    return await client.GetStringAsync(uri);
}

private bool IsInternalIp(IPAddress address)
{
    var bytes = address.GetAddressBytes();

    // Check for private IP ranges
    return bytes[0] == 10 ||
           (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
           (bytes[0] == 192 && bytes[1] == 168) ||
           bytes[0] == 127; // Localhost
}

// ❌ WRONG: Unvalidated external requests
public async Task<string> FetchDataAsync(string url)
{
    using var client = new HttpClient();
    return await client.GetStringAsync(url); // SSRF VULNERABILITY
}
```

---

## 2. Rate Limiting

### ⚠️ NOT YET IMPLEMENTED (Planned for Future)

**Current Status**: No rate limiting is currently implemented in any service.

**Future Implementation**: API Rate Limiting

```csharp
// PLANNED: Rate limiting for all public APIs
builder.Services.AddRateLimiter(options =>
{
    // Fixed window rate limiter
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });

    // Sliding window for authenticated users
    options.AddSlidingWindowLimiter("sliding", options =>
    {
        options.PermitLimit = 1000;
        options.Window = TimeSpan.FromHours(1);
        options.SegmentsPerWindow = 4;
    });

    // Concurrency limiter for expensive operations
    options.AddConcurrencyLimiter("concurrency", options =>
    {
        options.PermitLimit = 10;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });
});

app.UseRateLimiter();

// Apply to endpoints
app.MapPost("/api/blueprints", CreateBlueprint)
    .RequireRateLimiting("fixed");

app.MapPost("/api/blueprints/execute", ExecuteBlueprint)
    .RequireRateLimiting("concurrency");
```

---

## 3. Data Protection and Privacy

### Personal Data Handling

```csharp
// ✅ CORRECT: Mark PII properties
public class Participant
{
    public string Id { get; init; } = string.Empty;

    [PersonalData]
    public required string Name { get; init; }

    [PersonalData]
    public string? Email { get; init; }

    [PersonalData]
    public string? Organization { get; init; }
}

// Implement data deletion (GDPR)
public async Task DeleteUserDataAsync(string userId)
{
    // Delete all personal data
    await _participantRepository.DeleteByUserIdAsync(userId);
    await _blueprintRepository.AnonymizeByUserIdAsync(userId);

    _auditLogger.LogDataDeletion(userId, "All personal data deleted");
}
```

### Data Minimization

```csharp
// ✅ CORRECT: Only collect necessary data
public record BlueprintRequest(
    string Title,
    string Description,
    List<string> ParticipantIds); // Only IDs, not full participant data

// ❌ WRONG: Collecting unnecessary data
public record BlueprintRequest(
    string Title,
    string Description,
    string UserSSN,           // VIOLATION - Not needed
    string UserBirthDate,     // VIOLATION - Not needed
    List<Participant> Participants);
```

---

## 4. Secure Communication

### TLS/SSL Requirements

```csharp
// ✅ REQUIRED: Enforce TLS 1.2+
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});

// Reject weak ciphers
builder.Services.Configure<HttpsConnectionAdapterOptions>(options =>
{
    options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
});
```

### Certificate Validation

```csharp
// ✅ CORRECT: Validate certificates
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
    {
        if (errors == SslPolicyErrors.None)
            return true;

        _logger.LogError("Certificate validation failed: {Errors}", errors);
        return false;
    }
};

// ❌ WRONG: Disabling certificate validation
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true // VIOLATION
};
```

---

## 5. gRPC Security

### ✅ REQUIRED: Secure gRPC Services

```csharp
// REQUIRED: Use TLS for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5051, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps();
    });
});

// REQUIRED: Authentication
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = false; // Don't leak errors
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4 MB max
    options.MaxSendMessageSize = 4 * 1024 * 1024;
});

builder.Services.AddGrpcReflection(); // Disable in production

// Apply authorization
app.MapGrpcService<PeerService>()
    .RequireAuthorization();
```

---

## 6. Dependency Injection Security

### Avoid Service Locator

```csharp
// ✅ CORRECT: Constructor injection
public class BlueprintService
{
    private readonly IBlueprintRepository _repository;

    public BlueprintService(IBlueprintRepository repository)
    {
        _repository = repository;
    }
}

// ❌ WRONG: Service locator (security risk)
public class BlueprintService
{
    public void Execute(IServiceProvider services)
    {
        var repo = services.GetService<IBlueprintRepository>(); // VIOLATION
    }
}
```

---

## 7. Secrets Scanning

### Pre-commit Hooks

```bash
# .git/hooks/pre-commit
#!/bin/bash

# Scan for secrets before committing
detect-secrets scan --baseline .secrets.baseline

if [ $? -ne 0 ]; then
    echo "❌ Secrets detected! Commit aborted."
    exit 1
fi
```

### ✅ NEVER Commit

- API keys
- Passwords
- Connection strings with credentials
- Private keys
- OAuth tokens
- Session secrets
- Encryption keys

---

## Security Checklist

Before committing code:

- [ ] All inputs validated
- [ ] SQL injection prevented (parameterized queries)
- [ ] XSS prevented (output encoding)
- [ ] CSRF tokens implemented
- [ ] Authentication required for protected endpoints
- [ ] Authorization checks on all resources
- [ ] Secrets stored securely (Key Vault/User Secrets)
- [ ] Security headers configured
- [ ] HTTPS enforced
- [ ] Rate limiting applied
- [ ] Audit logging implemented
- [ ] Error messages don't leak sensitive info
- [ ] Dependencies scanned for vulnerabilities
- [ ] Personal data handling compliant with GDPR

---

## References

- [Spec-Kit Main](./spec-kit.md)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [Azure Security Best Practices](https://learn.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)
