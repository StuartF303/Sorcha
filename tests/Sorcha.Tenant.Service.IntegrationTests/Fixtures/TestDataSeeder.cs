// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sorcha.Tenant.Service.Data;
using Sorcha.Tenant.Service.Models;

namespace Sorcha.Tenant.Service.IntegrationTests.Fixtures;

/// <summary>
/// Seeds test data for integration tests.
/// Creates a standard test organization with administrator and member users.
/// </summary>
public static class TestDataSeeder
{
    // Well-known test identifiers for consistent test data
    public static readonly Guid TestOrganizationId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid TestAdminUserId = new("00000000-0000-0000-0001-000000000001");
    public static readonly Guid TestMemberUserId = new("00000000-0000-0000-0001-000000000002");
    public static readonly Guid TestAuditorUserId = new("00000000-0000-0000-0001-000000000003");

    public const string TestOrganizationName = "Test Organization";
    public const string TestOrganizationSubdomain = "test-org";

    public const string TestAdminEmail = "admin@test-org.sorcha.io";
    public const string TestAdminName = "Test Admin";
    public const string TestAdminExternalId = "external-admin-001";

    public const string TestMemberEmail = "member@test-org.sorcha.io";
    public const string TestMemberName = "Test Member";
    public const string TestMemberExternalId = "external-member-001";

    public const string TestAuditorEmail = "auditor@test-org.sorcha.io";
    public const string TestAuditorName = "Test Auditor";
    public const string TestAuditorExternalId = "external-auditor-001";

    /// <summary>
    /// Seeds the database with test data using a scope from the service provider.
    /// Call this method after building the WebApplicationFactory.
    /// </summary>
    /// <param name="services">The service provider from WebApplicationFactory.</param>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        // Ensure database is created
        await db.Database.EnsureCreatedAsync();

        // Seed organization if not exists
        if (!await db.Organizations.AnyAsync(o => o.Id == TestOrganizationId))
        {
            var organization = new Organization
            {
                Id = TestOrganizationId,
                Name = TestOrganizationName,
                Subdomain = TestOrganizationSubdomain,
                Status = OrganizationStatus.Active,
                CreatorIdentityId = TestAdminUserId,
                CreatedAt = DateTimeOffset.UtcNow,
                Branding = new BrandingConfiguration
                {
                    LogoUrl = "https://test-org.sorcha.io/logo.png",
                    PrimaryColor = "#0078D4",
                    SecondaryColor = "#50E6FF",
                    CompanyTagline = "Test Organization for Sorcha Integration Tests"
                }
            };

            await db.Organizations.AddAsync(organization);
            await db.SaveChangesAsync();
        }

        // Seed test users if not exist
        if (!await db.UserIdentities.AnyAsync(u => u.Id == TestAdminUserId))
        {
            var adminUser = new UserIdentity
            {
                Id = TestAdminUserId,
                OrganizationId = TestOrganizationId,
                ExternalIdpUserId = TestAdminExternalId,
                Email = TestAdminEmail,
                DisplayName = TestAdminName,
                Roles = [UserRole.Administrator],
                Status = IdentityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            };

            await db.UserIdentities.AddAsync(adminUser);
        }

        if (!await db.UserIdentities.AnyAsync(u => u.Id == TestMemberUserId))
        {
            var memberUser = new UserIdentity
            {
                Id = TestMemberUserId,
                OrganizationId = TestOrganizationId,
                ExternalIdpUserId = TestMemberExternalId,
                Email = TestMemberEmail,
                DisplayName = TestMemberName,
                Roles = [UserRole.Member],
                Status = IdentityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = DateTimeOffset.UtcNow
            };

            await db.UserIdentities.AddAsync(memberUser);
        }

        if (!await db.UserIdentities.AnyAsync(u => u.Id == TestAuditorUserId))
        {
            var auditorUser = new UserIdentity
            {
                Id = TestAuditorUserId,
                OrganizationId = TestOrganizationId,
                ExternalIdpUserId = TestAuditorExternalId,
                Email = TestAuditorEmail,
                DisplayName = TestAuditorName,
                Roles = [UserRole.Auditor],
                Status = IdentityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastLoginAt = null // Auditor hasn't logged in yet
            };

            await db.UserIdentities.AddAsync(auditorUser);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the test organization from the database.
    /// </summary>
    public static async Task<Organization?> GetTestOrganizationAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        return await db.Organizations.FirstOrDefaultAsync(o => o.Id == TestOrganizationId);
    }

    /// <summary>
    /// Gets a test user by ID from the database.
    /// </summary>
    public static async Task<UserIdentity?> GetTestUserAsync(IServiceProvider services, Guid userId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        return await db.UserIdentities.FirstOrDefaultAsync(u => u.Id == userId);
    }
}
