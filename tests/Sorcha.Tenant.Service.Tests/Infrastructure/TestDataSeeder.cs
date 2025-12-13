// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Tenant.Service.Data;
using Sorcha.Tenant.Service.Models;

namespace Sorcha.Tenant.Service.Tests.Infrastructure;

/// <summary>
/// Seeds test data for integration tests.
/// Provides well-known IDs for consistent test assertions.
/// </summary>
public static class TestDataSeeder
{
    // Well-known test organization ID
    public static readonly Guid TestOrganizationId = new("00000000-0000-0000-0000-000000000001");

    // Well-known test user IDs
    public static readonly Guid AdminUserId = new("00000000-0000-0000-0001-000000000001");
    public static readonly Guid MemberUserId = new("00000000-0000-0000-0001-000000000002");
    public static readonly Guid AuditorUserId = new("00000000-0000-0000-0001-000000000003");

    /// <summary>
    /// Seeds test data into the database context.
    /// </summary>
    public static async Task SeedAsync(TenantDbContext context)
    {
        // Create test organization
        var testOrg = new Organization
        {
            Id = TestOrganizationId,
            Name = "Test Organization",
            Subdomain = "test-org",
            Status = OrganizationStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Organizations.Add(testOrg);

        // Create admin user
        var adminUser = new UserIdentity
        {
            Id = AdminUserId,
            Email = "admin@test-org.sorcha.io",
            DisplayName = "Admin User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            Status = IdentityStatus.Active,
            Roles = new[] { UserRole.Administrator },
            OrganizationId = TestOrganizationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Create member user
        var memberUser = new UserIdentity
        {
            Id = MemberUserId,
            Email = "member@test-org.sorcha.io",
            DisplayName = "Member User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            Status = IdentityStatus.Active,
            Roles = new[] { UserRole.Member },
            OrganizationId = TestOrganizationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Create auditor user
        var auditorUser = new UserIdentity
        {
            Id = AuditorUserId,
            Email = "auditor@test-org.sorcha.io",
            DisplayName = "Auditor User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            Status = IdentityStatus.Active,
            Roles = new[] { UserRole.Auditor },
            OrganizationId = TestOrganizationId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.UserIdentities.AddRange(adminUser, memberUser, auditorUser);

        // Create test service principal (note: in real implementation, ClientSecretEncrypted would be encrypted)
        var servicePrincipal = new ServicePrincipal
        {
            Id = Guid.NewGuid(),
            ServiceName = "test-service",
            ClientId = "test-client-id",
            ClientSecretEncrypted = System.Text.Encoding.UTF8.GetBytes(BCrypt.Net.BCrypt.HashPassword("test-client-secret")),
            Status = ServicePrincipalStatus.Active,
            Scopes = new[] { "blueprints:read", "wallets:write" },
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.ServicePrincipals.Add(servicePrincipal);

        await context.SaveChangesAsync();
    }
}
