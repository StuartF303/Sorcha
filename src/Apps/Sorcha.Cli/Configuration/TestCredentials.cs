// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Cli.Configuration;

/// <summary>
/// Well-known test credentials and IDs for exercising the API
/// </summary>
public static class TestCredentials
{
    // Service URLs (Docker)
    public const string ApiGatewayUrl = "http://localhost:8080";
    public const string BlueprintServiceUrl = "http://localhost:5000";
    public const string WalletServiceUrl = "http://localhost:5001";
    public const string TenantServiceUrl = "http://localhost:5110";
    public const string RegisterServiceUrl = "http://localhost:5290";
    public const string PeerServiceUrl = "http://localhost:5002";

    // Well-known Organization
    public static readonly Guid TestOrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public const string TestOrganizationName = "Test Organization";
    public const string TestOrganizationSubdomain = "test-org";

    // Well-known Users
    public static class AdminUser
    {
        public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0001-000000000001");
        public const string Email = "admin@test-org.sorcha.io";
        public const string Name = "Test Admin";
        public const string ExternalId = "external-admin-001";
        public const string Role = "Administrator";
    }

    public static class MemberUser
    {
        public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0001-000000000002");
        public const string Email = "member@test-org.sorcha.io";
        public const string Name = "Test Member";
        public const string ExternalId = "external-member-001";
        public const string Role = "Member";
    }

    public static class AuditorUser
    {
        public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0001-000000000003");
        public const string Email = "auditor@test-org.sorcha.io";
        public const string Name = "Test Auditor";
        public const string ExternalId = "external-auditor-001";
        public const string Role = "Auditor";
    }

    /// <summary>
    /// Creates test authentication headers for a given user role
    /// </summary>
    public static Dictionary<string, string> GetAuthHeaders(string role = "Administrator")
    {
        var (userId, email, name) = role switch
        {
            "Administrator" => (AdminUser.Id, AdminUser.Email, AdminUser.Name),
            "Member" => (MemberUser.Id, MemberUser.Email, MemberUser.Name),
            "Auditor" => (AuditorUser.Id, AuditorUser.Email, AuditorUser.Name),
            _ => (AdminUser.Id, AdminUser.Email, AdminUser.Name)
        };

        return new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer test-token",
            ["X-Test-Role"] = role,
            ["X-Test-User-Id"] = userId.ToString(),
            ["X-Test-User-Email"] = email,
            ["X-Test-User-Name"] = name,
            ["X-Test-Org-Id"] = TestOrganizationId.ToString()
        };
    }
}
