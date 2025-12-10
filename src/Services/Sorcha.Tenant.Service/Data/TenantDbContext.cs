// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Sorcha.Tenant.Service.Models;

namespace Sorcha.Tenant.Service.Data;

/// <summary>
/// Entity Framework Core database context for the Tenant Service.
/// Supports multi-tenant schema isolation (public schema + per-org schemas).
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly string _currentSchema;

    /// <summary>
    /// Creates a new TenantDbContext instance.
    /// </summary>
    /// <param name="options">DbContext options (configured for PostgreSQL or InMemory).</param>
    /// <param name="schema">Current schema to use (default: "public"). For tenant data, use "org_{organizationId}".</param>
    public TenantDbContext(DbContextOptions<TenantDbContext> options, string schema = "public")
        : base(options)
    {
        _currentSchema = schema;
    }

    // Public schema entities (shared metadata)
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<IdentityProviderConfiguration> IdentityProviderConfigurations => Set<IdentityProviderConfiguration>();
    public DbSet<PublicIdentity> PublicIdentities => Set<PublicIdentity>();
    public DbSet<ServicePrincipal> ServicePrincipals => Set<ServicePrincipal>();

    // Per-tenant schema entities (isolated per organization)
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<OrganizationPermissionConfiguration> OrganizationPermissionConfigurations => Set<OrganizationPermissionConfiguration>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema (public or org_{id})
        // Note: InMemory provider doesn't support schemas, so only set for relational databases
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (!isInMemory)
        {
            modelBuilder.HasDefaultSchema(_currentSchema);
        }

        // Configure Organization entity
        ConfigureOrganization(modelBuilder);

        // Configure IdentityProviderConfiguration entity
        ConfigureIdentityProviderConfiguration(modelBuilder);

        // Configure PublicIdentity entity
        ConfigurePublicIdentity(modelBuilder);

        // Configure ServicePrincipal entity
        ConfigureServicePrincipal(modelBuilder);

        // Configure UserIdentity entity (per-org schema)
        ConfigureUserIdentity(modelBuilder);

        // Configure OrganizationPermissionConfiguration entity (per-org schema)
        ConfigureOrganizationPermissionConfiguration(modelBuilder);

        // Configure AuditLogEntry entity (per-org schema)
        ConfigureAuditLogEntry(modelBuilder);
    }

    private void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        modelBuilder.Entity<Organization>(entity =>
        {
            if (isInMemory)
                entity.ToTable("Organizations");
            else
                entity.ToTable("Organizations", "public");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Subdomain)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Subdomain)
                .IsUnique();

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();

            // JSON column for branding configuration
            entity.OwnsOne(e => e.Branding, branding =>
            {
                branding.Property(b => b.LogoUrl).HasMaxLength(500);
                branding.Property(b => b.PrimaryColor).HasMaxLength(20);
                branding.Property(b => b.SecondaryColor).HasMaxLength(20);
                branding.Property(b => b.CompanyTagline).HasMaxLength(500);
            });

            // One-to-one relationship with IdentityProviderConfiguration
            entity.HasOne(e => e.IdentityProvider)
                .WithOne(i => i.Organization)
                .HasForeignKey<IdentityProviderConfiguration>(i => i.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureIdentityProviderConfiguration(ModelBuilder modelBuilder)
    {
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        modelBuilder.Entity<IdentityProviderConfiguration>(entity =>
        {
            if (isInMemory)
                entity.ToTable("IdentityProviderConfigurations");
            else
                entity.ToTable("IdentityProviderConfigurations", "public");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IssuerUrl)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ClientId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ClientSecretEncrypted)
                .IsRequired();

            entity.Property(e => e.Scopes)
                .IsRequired();

            entity.Property(e => e.AuthorizationEndpoint)
                .HasMaxLength(500);

            entity.Property(e => e.TokenEndpoint)
                .HasMaxLength(500);

            entity.Property(e => e.MetadataUrl)
                .HasMaxLength(500);

            entity.Property(e => e.ProviderType)
                .HasConversion<string>()
                .IsRequired();

            entity.HasIndex(e => e.OrganizationId)
                .IsUnique();

            entity.HasIndex(e => e.ProviderType);
        });
    }

    private void ConfigurePublicIdentity(ModelBuilder modelBuilder)
    {
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        modelBuilder.Entity<PublicIdentity>(entity =>
        {
            if (isInMemory)
                entity.ToTable("PublicIdentities");
            else
                entity.ToTable("PublicIdentities", "public");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PassKeyCredentialId)
                .IsRequired();

            entity.Property(e => e.PublicKeyCose)
                .IsRequired();

            entity.HasIndex(e => e.PassKeyCredentialId)
                .IsUnique();
        });
    }

    private void ConfigureServicePrincipal(ModelBuilder modelBuilder)
    {
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        modelBuilder.Entity<ServicePrincipal>(entity =>
        {
            if (isInMemory)
                entity.ToTable("ServicePrincipals");
            else
                entity.ToTable("ServicePrincipals", "public");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ServiceName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ClientId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ClientSecretEncrypted)
                .IsRequired();

            entity.Property(e => e.Scopes)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.HasIndex(e => e.ServiceName)
                .IsUnique();

            entity.HasIndex(e => e.ClientId)
                .IsUnique();
        });
    }

    private void ConfigureUserIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserIdentity>(entity =>
        {
            entity.ToTable("UserIdentities");
            entity.HasKey(e => e.Id);

            // ExternalIdpUserId is nullable (null for local auth users)
            entity.Property(e => e.ExternalIdpUserId)
                .IsRequired(false)
                .HasMaxLength(200);

            // PasswordHash is nullable (null for external IDP users)
            entity.Property(e => e.PasswordHash)
                .IsRequired(false)
                .HasMaxLength(500);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Roles)
                .HasConversion(
                    v => string.Join(',', v.Select(r => r.ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => Enum.Parse<UserRole>(s))
                          .ToArray())
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();

            // Unique index on ExternalIdpUserId only for non-null values
            entity.HasIndex(e => e.ExternalIdpUserId)
                .IsUnique()
                .HasFilter("\"ExternalIdpUserId\" IS NOT NULL");

            entity.HasIndex(e => e.Email)
                .IsUnique();  // Email must be unique within organization

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Status);
        });
    }

    private void ConfigureOrganizationPermissionConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationPermissionConfiguration>(entity =>
        {
            entity.ToTable("OrganizationPermissionConfigurations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ApprovedBlockchains)
                .IsRequired();

            entity.HasIndex(e => e.OrganizationId)
                .IsUnique();
        });
    }

    private void ConfigureAuditLogEntry(ModelBuilder modelBuilder)
    {
        var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogEntries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            // Details stored as JSON (EF Core will handle serialization)
            if (isInMemory)
            {
                // InMemory provider needs a value converter for Dictionary<string, object>
                entity.Property(e => e.Details)
                    .HasConversion(
                        v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null));
            }
            else
            {
                entity.Property(e => e.Details)
                    .HasColumnType("jsonb"); // PostgreSQL JSONB
            }

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.IdentityId);
            entity.HasIndex(e => e.OrganizationId);
        });
    }
}
