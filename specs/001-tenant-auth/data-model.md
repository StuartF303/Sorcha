# Data Model: Tenant Service

**Feature**: 001-tenant-auth
**Date**: 2025-11-22
**Phase**: 1 (Design & Contracts)
**Database**: PostgreSQL with Entity Framework Core 10

## Overview

The Tenant Service data model supports multi-organization authentication with external IDP integration, PassKey support, role-based access control, and comprehensive audit logging. The model uses PostgreSQL schemas for tenant isolation (one schema per organization for sensitive data, shared public schema for metadata).

## Multi-Tenancy Strategy

**Approach**: Hybrid schema-based multi-tenancy
- **Shared schema (`public`)**: Organizations, IdentityProviderConfigurations (metadata only, no sensitive user data)
- **Per-tenant schemas (`org_{id}`)**: UserIdentities, OrganizationPermissionConfigurations, AuditLogEntries

**Benefits**:
- Strong data isolation between organizations
- Prevents accidental cross-tenant data leakage
- Supports data sovereignty requirements
- Efficient queries (each tenant's data is logically separated)

## Entity-Relationship Diagram

```
┌─────────────────────────────┐
│      Organizations          │ (public schema)
│  - Id (PK)                  │
│  - Name                     │
│  - Subdomain                │
│  - Status                   │
│  - CreatorIdentityId        │
│  - CreatedAt                │
│  - BrandingConfiguration    │
└──────────┬──────────────────┘
           │ 1
           │
           │ 1
┌──────────┴──────────────────┐
│ IdentityProvider            │ (public schema)
│ Configuration               │
│  - Id (PK)                  │
│  - OrganizationId (FK)      │
│  - ProviderType             │
│  - IssuerUrl                │
│  - ClientId                 │
│  - ClientSecretEncrypted    │
│  - Scopes                   │
│  - MetadataUrl              │
└─────────────────────────────┘

           ┌─────────────────────────────┐
           │   UserIdentities             │ (per-org schema: org_{id})
           │  - Id (PK)                   │
           │  - OrganizationId (indexed)  │
           │  - ExternalIdpUserId         │
           │  - Email                     │
           │  - DisplayName               │
           │  - Roles (array)             │
           │  - Status                    │
           │  - CreatedAt                 │
           │  - LastLoginAt               │
           └──────────────────────────────┘

┌─────────────────────────────┐
│   PublicIdentities          │ (public schema - no org affiliation)
│  - Id (PK)                  │
│  - PassKeyCredentialId      │
│  - PublicKeyCose            │
│  - SignatureCounter         │
│  - DeviceType               │
│  - RegisteredAt             │
│  - LastUsedAt               │
└─────────────────────────────┘

           ┌──────────────────────────────┐
           │ OrganizationPermission       │ (per-org schema: org_{id})
           │ Configurations               │
           │  - Id (PK)                   │
           │  - OrganizationId (FK)       │
           │  - ApprovedBlockchains       │
           │  - CanCreateBlockchain       │
           │  - CanPublishBlueprint       │
           │  - UpdatedAt                 │
           └──────────────────────────────┘

┌─────────────────────────────┐
│   ServicePrincipals         │ (public schema)
│  - Id (PK)                  │
│  - ServiceName              │
│  - ClientId                 │
│  - ClientSecretEncrypted    │
│  - Scopes (array)           │
│  - Status                   │
│  - CreatedAt                │
└─────────────────────────────┘

           ┌──────────────────────────────┐
           │   AuditLogEntries            │ (per-org schema: org_{id})
           │  - Id (PK)                   │
           │  - Timestamp                 │
           │  - EventType                 │
           │  - IdentityId                │
           │  - OrganizationId            │
           │  - IpAddress                 │
           │  - UserAgent                 │
           │  - Success                   │
           │  - Details (JSONB)           │
           └──────────────────────────────┘
```

## Entity Definitions

### 1. Organization (Public Schema)

**Purpose**: Represents a tenant/company using the platform

**Table**: `Organizations` (schema: `public`)

**Columns**:

| Column | Type | Constraints | Description |
|--------|------|------------|-------------|
| Id | UUID | PRIMARY KEY | Unique organization identifier |
| Name | VARCHAR(200) | NOT NULL | Organization display name |
| Subdomain | VARCHAR(50) | UNIQUE, NOT NULL | Subdomain for organization-specific URLs (e.g., acme.sorcha.io) |
| Status | VARCHAR(20) | NOT NULL, DEFAULT 'Active' | Active, Suspended, Deleted |
| CreatorIdentityId | UUID | NULL | User who created the organization |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Organization creation timestamp |
| BrandingConfiguration | JSONB | NULL | Logo URL, primary color, secondary color, company tagline |

**Indexes**:
- PRIMARY KEY: `Id`
- UNIQUE: `Subdomain`
- INDEX: `Status` (for active organization queries)

**EF Core Entity**:

```csharp
public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;
    public Guid? CreatorIdentityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public BrandingConfiguration? Branding { get; set; }

    // Navigation properties
    public IdentityProviderConfiguration? IdentityProvider { get; set; }
}

public enum OrganizationStatus
{
    Active,
    Suspended,
    Deleted
}

public class BrandingConfiguration
{
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? CompanyTagline { get; set; }
}
```

**Validation Rules**:
- `Name`: Required, 1-200 characters
- `Subdomain`: Required, 3-50 characters, alphanumeric + hyphens, must be globally unique
- `Status`: Must be one of Active, Suspended, Deleted
- `BrandingConfiguration.LogoUrl`: Must be valid HTTPS URL if provided

**State Transitions**:
```
         Create
           ↓
        Active ←→ Suspended
           ↓
        Deleted (soft delete, keep schema for 30 days)
```

---

### 2. IdentityProviderConfiguration (Public Schema)

**Purpose**: External IDP settings for an organization (Azure Entra, AWS Cognito, OIDC)

**Table**: `IdentityProviderConfigurations` (schema: `public`)

**Columns**:

| Column | Type | Constraints | Description |
|--------|------|------------|-------------|
| Id | UUID | PRIMARY KEY | Unique configuration identifier |
| OrganizationId | UUID | FOREIGN KEY → Organizations(Id), NOT NULL | Associated organization |
| ProviderType | VARCHAR(50) | NOT NULL | AzureEntra, AwsCognito, GenericOidc |
| IssuerUrl | VARCHAR(500) | NOT NULL | OIDC issuer URL |
| ClientId | VARCHAR(200) | NOT NULL | OAuth2 client ID |
| ClientSecretEncrypted | BYTEA | NOT NULL | AES-256-GCM encrypted client secret |
| Scopes | TEXT[] | NOT NULL | OAuth2 scopes (e.g., openid, profile, email) |
| AuthorizationEndpoint | VARCHAR(500) | NULL | Override for non-standard IDPs |
| TokenEndpoint | VARCHAR(500) | NULL | Override for non-standard IDPs |
| MetadataUrl | VARCHAR(500) | NULL | OIDC discovery URL (/.well-known/openid-configuration) |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Configuration creation timestamp |
| UpdatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Last update timestamp |

**Indexes**:
- PRIMARY KEY: `Id`
- UNIQUE: `OrganizationId` (one IDP per organization)
- INDEX: `ProviderType`

**EF Core Entity**:

```csharp
public class IdentityProviderConfiguration
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public IdentityProviderType ProviderType { get; set; }
    public string IssuerUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public byte[] ClientSecretEncrypted { get; set; } = Array.Empty<byte>();
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? MetadataUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}

public enum IdentityProviderType
{
    AzureEntra,
    AwsCognito,
    GenericOidc
}
```

**Validation Rules**:
- `IssuerUrl`: Required, must be valid HTTPS URL
- `ClientId`: Required, 1-200 characters
- `ClientSecretEncrypted`: Required, encrypted using Sorcha.Cryptography library
- `Scopes`: Must include at least "openid" scope
- `MetadataUrl`: Must be valid HTTPS URL if provided

**Encryption**:
- `ClientSecretEncrypted`: Use `Sorcha.Cryptography.AesGcmEncryption` with organization-specific key derived from master key in Azure Key Vault

---

### 3. UserIdentity (Per-Organization Schema)

**Purpose**: Authenticated user within an organization

**Table**: `UserIdentities` (schema: `org_{organization_id}`)

**Columns**:

| Column | Type | Constraints | Description |
|--------|------|------------|-------------|
| Id | UUID | PRIMARY KEY | Unique user identifier |
| OrganizationId | UUID | NOT NULL, INDEXED | Organization membership (denormalized for queries) |
| ExternalIdpUserId | VARCHAR(200) | NOT NULL | User ID from external IDP (sub claim) |
| Email | VARCHAR(255) | NOT NULL | User email address |
| DisplayName | VARCHAR(200) | NOT NULL | User display name |
| Roles | TEXT[] | NOT NULL | Administrator, Auditor, Member |
| Status | VARCHAR(20) | NOT NULL, DEFAULT 'Active' | Active, Suspended, Deleted |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | User creation timestamp |
| LastLoginAt | TIMESTAMPTZ | NULL | Last successful login timestamp |

**Indexes**:
- PRIMARY KEY: `Id`
- UNIQUE: `ExternalIdpUserId` (within organization schema)
- INDEX: `Email`
- INDEX: `OrganizationId`
- INDEX: `Status`

**EF Core Entity**:

```csharp
public class UserIdentity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ExternalIdpUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole[] Roles { get; set; } = new[] { UserRole.Member };
    public IdentityStatus Status { get; set; } = IdentityStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}

public enum UserRole
{
    Administrator,
    Auditor,
    Member
}

public enum IdentityStatus
{
    Active,
    Suspended,
    Deleted
}
```

**Validation Rules**:
- `ExternalIdpUserId`: Required, 1-200 characters, unique within organization
- `Email`: Required, valid email format
- `DisplayName`: Required, 1-200 characters
- `Roles`: At least one role required, Member is default
- Organization creator automatically gets Administrator role

---

### 4. PublicIdentity (Public Schema)

**Purpose**: PassKey-authenticated user without organizational affiliation

**Table**: `PublicIdentities` (schema: `public`)

**Columns**:

| Column | Type | Constraints | Description |
|--------|------|------------|-------------|
| Id | UUID | PRIMARY KEY | Unique public user identifier |
| PassKeyCredentialId | BYTEA | UNIQUE, NOT NULL | FIDO2 credential ID |
| PublicKeyCose | BYTEA | NOT NULL | COSE-encoded public key |
| SignatureCounter | INT | NOT NULL, DEFAULT 0 | Signature counter for cloned authenticator detection |
| DeviceType | VARCHAR(100) | NULL | Authenticator device type (e.g., "YubiKey 5 NFC", "Windows Hello") |
| RegisteredAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | PassKey registration timestamp |
| LastUsedAt | TIMESTAMPTZ | NULL | Last successful authentication timestamp |

**Indexes**:
- PRIMARY KEY: `Id`
- UNIQUE: `PassKeyCredentialId`

**EF Core Entity**:

```csharp
public class PublicIdentity
{
    public Guid Id { get; set; }
    public byte[] PassKeyCredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKeyCose { get; set; } = Array.Empty<byte>();
    public int SignatureCounter { get; set; } = 0;
    public string? DeviceType { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }
}
```

**Validation Rules**:
- `PassKeyCredentialId`: Required, must be unique globally
- `PublicKeyCose`: Required, must be valid COSE format
- `SignatureCounter`: Increment on each authentication, detect cloning if counter doesn't increase

---

### 5. OrganizationPermissionConfiguration (Per-Organization Schema)

**Purpose**: Organization-level permissions for blockchain access and operations

**Table**: `OrganizationPermissionConfigurations` (schema: `org_{organization_id}`)

**Columns**:

| Column | Type | Constraints | Description |
|--------|------|------------|-------------|
| Id | UUID | PRIMARY KEY | Unique configuration identifier |
| OrganizationId | UUID | UNIQUE, NOT NULL | Associated organization (one config per org) |
| ApprovedBlockchains | UUID[] | NOT NULL, DEFAULT '{}' | List of blockchain IDs members can access |
| CanCreateBlockchain | BOOLEAN | NOT NULL, DEFAULT FALSE | Members can create new blockchains |
| CanPublishBlueprint | BOOLEAN | NOT NULL, DEFAULT FALSE | Members can publish blueprints |
| UpdatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Last update timestamp |

**Indexes**:
- PRIMARY KEY: `Id`
- UNIQUE: `OrganizationId`

**EF Core Entity**:

```csharp
public class OrganizationPermissionConfiguration
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid[] ApprovedBlockchains { get; set; } = Array.Empty<Guid>();
    public bool CanCreateBlockchain { get; set; } = false;
    public bool CanPublishBlueprint { get; set; } = false;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**Validation Rules**:
- `OrganizationId`: Required, must reference valid organization
- `ApprovedBlockchains`: Can be empty array (no blockchain access)
- Defaults: CanCreateBlockchain = false, CanPublishBlueprint = false (secure by default)

---

### 6. ServicePrincipal (Public Schema)

**Purpose**: Internal Sorcha service identity for service-to-service authentication

**Table**: `ServicePrincipals` (schema: `public`)

**Columns**:

| Column | Type | Constraints | Description |
|--------|------|------------|-------------|
| Id | UUID | PRIMARY KEY | Unique service identifier |
| ServiceName | VARCHAR(100) | UNIQUE, NOT NULL | Service name (e.g., "Blueprint", "Wallet", "Register") |
| ClientId | VARCHAR(100) | UNIQUE, NOT NULL | OAuth2 client ID for service |
| ClientSecretEncrypted | BYTEA | NOT NULL | AES-256-GCM encrypted client secret |
| Scopes | TEXT[] | NOT NULL | Allowed scopes (e.g., wallet:sign, register:commit) |
| Status | VARCHAR(20) | NOT NULL, DEFAULT 'Active' | Active, Suspended, Revoked |
| CreatedAt | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Service registration timestamp |

**Indexes**:
- PRIMARY KEY: `Id`
- UNIQUE: `ServiceName`
- UNIQUE: `ClientId`

**EF Core Entity**:

```csharp
public class ServicePrincipal
{
    public Guid Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public byte[] ClientSecretEncrypted { get; set; } = Array.Empty<byte>();
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public ServicePrincipalStatus Status { get; set; } = ServicePrincipalStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum ServicePrincipalStatus
{
    Active,
    Suspended,
    Revoked
}
```

**Pre-Configured Services**:
```sql
INSERT INTO "ServicePrincipals" ("Id", "ServiceName", "ClientId", "Scopes") VALUES
('11111111-1111-1111-1111-111111111111', 'Blueprint', 'service-blueprint', ARRAY['wallet:sign', 'register:commit', 'register:read']),
('22222222-2222-2222-2222-222222222222', 'Wallet', 'service-wallet', ARRAY['register:read', 'tenant:validate']),
('33333333-3333-3333-3333-333333333333', 'Register', 'service-register', ARRAY['peer:sync', 'tenant:validate']),
('44444444-4444-4444-4444-444444444444', 'Peer', 'service-peer', ARRAY['register:sync', 'tenant:validate']);
```

---

### 7. AuditLogEntry (Per-Organization Schema)

**Purpose**: Comprehensive audit trail for authentication and authorization events

**Table**: `AuditLogEntries` (schema: `org_{organization_id}`)

**Columns**:

| Column | Type | Constraints | Description |
|--------|------|------------|-------------|
| Id | BIGSERIAL | PRIMARY KEY | Auto-incrementing log entry ID |
| Timestamp | TIMESTAMPTZ | NOT NULL, INDEXED | Event timestamp |
| EventType | VARCHAR(50) | NOT NULL, INDEXED | Login, Logout, TokenIssued, TokenRevoked, PermissionDenied, etc. |
| IdentityId | UUID | NULL, INDEXED | User or service ID (null for failed auth attempts) |
| OrganizationId | UUID | NOT NULL, INDEXED | Organization context |
| IpAddress | INET | NULL | Client IP address |
| UserAgent | TEXT | NULL | Client user agent |
| Success | BOOLEAN | NOT NULL | Event succeeded or failed |
| Details | JSONB | NULL | Additional context (token JTI, error messages, requested resource) |

**Indexes**:
- PRIMARY KEY: `Id`
- INDEX: `Timestamp DESC` (for time-range queries)
- INDEX: `EventType`
- INDEX: `IdentityId`
- INDEX: `OrganizationId`

**EF Core Entity**:

```csharp
public class AuditLogEntry
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public AuditEventType EventType { get; set; }
    public Guid? IdentityId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

public enum AuditEventType
{
    Login,
    Logout,
    TokenIssued,
    TokenRefreshed,
    TokenRevoked,
    TokenValidated,
    PermissionDenied,
    IdpConfigurationUpdated,
    OrganizationPermissionsUpdated,
    PassKeyRegistered,
    PassKeyAuthentication
}
```

**Data Retention**:
- Keep audit logs for minimum 90 days (configurable)
- Archive old logs to cold storage (Azure Blob, S3) after retention period
- Partition table by month for performance: `AuditLogEntries_YYYY_MM`

**Example Audit Log Entries**:

```json
// Successful login
{
  "EventType": "Login",
  "IdentityId": "user-uuid",
  "Success": true,
  "Details": {
    "idp": "AzureEntra",
    "tokenJti": "token-uuid"
  }
}

// Failed login
{
  "EventType": "Login",
  "IdentityId": null,
  "Success": false,
  "IpAddress": "192.168.1.100",
  "Details": {
    "error": "Invalid credentials",
    "attemptedEmail": "user@example.com"
  }
}

// Permission denied
{
  "EventType": "PermissionDenied",
  "IdentityId": "user-uuid",
  "Success": false,
  "Details": {
    "requestedOperation": "CreateBlockchain",
    "reason": "Organization does not allow blockchain creation"
  }
}
```

---

## DbContext Configuration

**File**: `src/Services/Sorcha.Tenant.Service/Data/TenantDbContext.cs`

```csharp
public class TenantDbContext : DbContext
{
    private readonly string _currentSchema;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, string schema = "public")
        : base(options)
    {
        _currentSchema = schema;
    }

    // Public schema entities
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<IdentityProviderConfiguration> IdentityProviderConfigurations => Set<IdentityProviderConfiguration>();
    public DbSet<PublicIdentity> PublicIdentities => Set<PublicIdentity>();
    public DbSet<ServicePrincipal> ServicePrincipals => Set<ServicePrincipal>();

    // Per-tenant schema entities
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<OrganizationPermissionConfiguration> OrganizationPermissionConfigurations => Set<OrganizationPermissionConfiguration>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default schema (public or org_{id})
        modelBuilder.HasDefaultSchema(_currentSchema);

        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations", schema: "public");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Subdomain).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.Branding).HasColumnType("jsonb");
        });

        // IdentityProviderConfiguration
        modelBuilder.Entity<IdentityProviderConfiguration>(entity =>
        {
            entity.ToTable("IdentityProviderConfigurations", schema: "public");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId).IsUnique();
            entity.HasOne(e => e.Organization)
                  .WithOne(o => o.IdentityProvider)
                  .HasForeignKey<IdentityProviderConfiguration>(e => e.OrganizationId);
        });

        // PublicIdentity
        modelBuilder.Entity<PublicIdentity>(entity =>
        {
            entity.ToTable("PublicIdentities", schema: "public");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PassKeyCredentialId).IsUnique();
        });

        // ServicePrincipal
        modelBuilder.Entity<ServicePrincipal>(entity =>
        {
            entity.ToTable("ServicePrincipals", schema: "public");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ServiceName).IsUnique();
            entity.HasIndex(e => e.ClientId).IsUnique();
        });

        // UserIdentity (per-tenant schema)
        modelBuilder.Entity<UserIdentity>(entity =>
        {
            entity.ToTable("UserIdentities");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalIdpUserId).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.OrganizationId);
        });

        // OrganizationPermissionConfiguration (per-tenant schema)
        modelBuilder.Entity<OrganizationPermissionConfiguration>(entity =>
        {
            entity.ToTable("OrganizationPermissionConfigurations");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrganizationId).IsUnique();
        });

        // AuditLogEntry (per-tenant schema)
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogEntries");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.IdentityId);
            entity.Property(e => e.Details).HasColumnType("jsonb");
        });
    }
}
```

---

## Migration Strategy

**Initial Migration** (Public Schema):
```bash
dotnet ef migrations add InitialCreate --context TenantDbContext --output-dir Data/Migrations
dotnet ef database update --context TenantDbContext
```

**Per-Tenant Schema Creation** (on organization creation):
```sql
-- Create organization-specific schema
CREATE SCHEMA IF NOT EXISTS org_{organization_id};

-- Create tables in new schema
CREATE TABLE org_{organization_id}."UserIdentities" (...);
CREATE TABLE org_{organization_id}."OrganizationPermissionConfigurations" (...);
CREATE TABLE org_{organization_id}."AuditLogEntries" (...);

-- Create indexes
CREATE INDEX "IX_UserIdentities_Email" ON org_{organization_id}."UserIdentities"("Email");
...
```

**Automated Schema Management**:
- `OrganizationService.CreateOrganization()` automatically creates schema and tables
- `OrganizationService.DeleteOrganization()` marks org as Deleted, schedules schema drop after 30 days
- Background job cleans up deleted organization schemas

---

## Connection String Configuration

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "TenantDatabase": "Host=localhost;Database=sorcha_tenant;Username=sorcha_user;Password=***;Include Error Detail=true"
  }
}
```

**Multi-Tenant Context Resolution**:
```csharp
// ITenantProvider implementation
public class TenantProvider : ITenantProvider
{
    public string GetCurrentSchema(HttpContext httpContext)
    {
        // Extract organization ID from JWT claims
        var orgId = httpContext.User.FindFirst("org_id")?.Value;
        return orgId != null ? $"org_{orgId}" : "public";
    }
}

// DI registration
services.AddScoped<ITenantProvider, TenantProvider>();
services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
{
    var httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var tenantProvider = serviceProvider.GetRequiredService<ITenantProvider>();
    var schema = httpContext != null ? tenantProvider.GetCurrentSchema(httpContext) : "public";

    options.UseNpgsql(configuration.GetConnectionString("TenantDatabase"), npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema);
    });
});
```

---

## Summary

**Total Tables**: 7 (4 public schema, 3 per-tenant schema)
**Total Entities**: 7 EF Core classes
**Relationships**: 1 one-to-one (Organization ↔ IdentityProviderConfiguration)
**Indexes**: 15+ for query performance
**Encryption**: ClientSecretEncrypted (IDP and service credentials) using Sorcha.Cryptography
**Multi-Tenancy**: Hybrid schema approach (shared metadata in public, isolated data per tenant)
