// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Cli.Configuration;
using Sorcha.Cli.Services;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Workflows;

/// <summary>
/// Administrative workflow: organization management, user management
/// </summary>
public class AdminWorkflow : IWorkflow
{
    private readonly TenantApiClient _tenantClient;
    private readonly WalletApiClient _walletClient;

    public string Name => "Admin Workflow";
    public string Description => "Execute administrative tasks: organization and user management";

    public IEnumerable<string> StepNames =>
    [
        "Get test organization",
        "Validate subdomain",
        "List organization users",
        "Get admin user details",
        "Get member user details",
        "Get auditor user details",
        "Verify current user (as Admin)",
        "Create test organization (new)",
        "Add new user to organization"
    ];

    public AdminWorkflow(TenantApiClient tenantClient, WalletApiClient walletClient)
    {
        _tenantClient = tenantClient;
        _walletClient = walletClient;
    }

    public async Task ExecuteAsync(WorkflowProgress progress, ActivityLog activityLog, CancellationToken ct = default)
    {
        // Step 1: Get test organization
        progress.StartStep("Fetching organization by ID");
        activityLog.LogInfo($"Fetching organization {TestCredentials.TestOrganizationId}");

        try
        {
            var org = await _tenantClient.GetOrganizationAsync(TestCredentials.TestOrganizationId, ct);
            if (org != null)
            {
                progress.CompleteStep($"Found: {org.Name} ({org.Status})");
                activityLog.LogSuccess($"Organization: {org.Name}, Subdomain: {org.Subdomain}");
            }
            else
            {
                progress.FailStep("Organization not found");
                activityLog.LogWarning("Test organization not seeded - some tests may fail");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Failed to get organization: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 2: Validate subdomain
        progress.StartStep("Checking subdomain availability");
        activityLog.LogInfo($"Validating subdomain: {TestCredentials.TestOrganizationSubdomain}");

        try
        {
            var isValid = await _tenantClient.ValidateSubdomainAsync(TestCredentials.TestOrganizationSubdomain, ct);
            progress.CompleteStep(isValid ? "Subdomain is valid" : "Subdomain taken");
            activityLog.LogSuccess($"Subdomain validation: {(isValid ? "available" : "in use")}");
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Subdomain validation failed: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 3: List organization users
        progress.StartStep("Fetching organization users");
        activityLog.LogInfo("Listing all users in test organization");

        try
        {
            var users = await _tenantClient.GetOrganizationUsersAsync(TestCredentials.TestOrganizationId, ct);
            if (users != null)
            {
                progress.CompleteStep($"Found {users.Count} users");
                foreach (var user in users)
                {
                    activityLog.LogInfo($"  - {user.DisplayName} ({user.Role}): {user.Email}");
                }
            }
            else
            {
                progress.CompleteStep("No users found");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Failed to list users: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Steps 4-6: Get individual user details
        var testUsers = new[]
        {
            (TestCredentials.AdminUser.Id, TestCredentials.AdminUser.Name, TestCredentials.AdminUser.Role),
            (TestCredentials.MemberUser.Id, TestCredentials.MemberUser.Name, TestCredentials.MemberUser.Role),
            (TestCredentials.AuditorUser.Id, TestCredentials.AuditorUser.Name, TestCredentials.AuditorUser.Role)
        };

        foreach (var (userId, name, role) in testUsers)
        {
            progress.StartStep($"Fetching {role} user: {name}");
            activityLog.LogInfo($"Getting user details for {userId}");

            try
            {
                var user = await _tenantClient.GetUserAsync(TestCredentials.TestOrganizationId, userId, ct);
                if (user != null)
                {
                    progress.CompleteStep($"{user.DisplayName} - {user.Status}");
                    activityLog.LogSuccess($"User {user.Email} is {user.Status}");
                }
                else
                {
                    progress.CompleteStep("User not found (may need seeding)");
                    activityLog.LogWarning($"User {userId} not found");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"Failed to get user: {ex.Message}");
            }

            await Task.Delay(150, ct);
        }

        // Step 7: Verify current user
        progress.StartStep("Getting current authenticated user");
        activityLog.LogInfo("Fetching /api/auth/me as Admin");

        try
        {
            var currentUser = await _tenantClient.GetCurrentUserAsync(ct);
            if (currentUser != null)
            {
                progress.CompleteStep($"Authenticated as {currentUser.DisplayName}");
                activityLog.LogSuccess($"Current user: {currentUser.Email} ({currentUser.Role})");
            }
            else
            {
                progress.CompleteStep("Auth endpoint returned null");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Auth check failed: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 8: Try to create a new test organization
        progress.StartStep("Creating new test organization");
        var newOrgSubdomain = $"cli-test-{DateTime.Now:HHmmss}";
        activityLog.LogInfo($"Creating organization with subdomain: {newOrgSubdomain}");

        try
        {
            var newOrg = await _tenantClient.CreateOrganizationAsync(new CreateOrganizationRequest(
                Name: "CLI Test Organization",
                Subdomain: newOrgSubdomain,
                Description: "Created by Sorcha CLI workflow"
            ), ct);

            if (newOrg != null)
            {
                progress.CompleteStep($"Created: {newOrg.Id}");
                activityLog.LogSuccess($"New organization created: {newOrg.Name} (ID: {newOrg.Id})");
            }
            else
            {
                progress.CompleteStep("Creation returned null");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Organization creation failed: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 9: Add a new user
        progress.StartStep("Adding new user to organization");
        var newUserEmail = $"cli-user-{DateTime.Now:HHmmss}@test.sorcha.io";
        activityLog.LogInfo($"Adding user: {newUserEmail}");

        try
        {
            var newUser = await _tenantClient.AddUserAsync(
                TestCredentials.TestOrganizationId,
                new AddUserRequest(
                    Email: newUserEmail,
                    DisplayName: "CLI Test User",
                    Role: "Member",
                    ExternalId: $"cli-external-{DateTime.Now:HHmmss}"
                ), ct);

            if (newUser != null)
            {
                progress.CompleteStep($"Added: {newUser.Id}");
                activityLog.LogSuccess($"New user added: {newUser.Email} (ID: {newUser.Id})");
            }
            else
            {
                progress.CompleteStep("User creation returned null");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"User creation failed: {ex.Message}");
        }
    }
}
