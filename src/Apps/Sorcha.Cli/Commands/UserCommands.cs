using System.CommandLine;
using System.Net;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// User management commands.
/// </summary>
public class UserCommand : Command
{
    public UserCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("user", "Manage users within organizations")
    {
        AddCommand(new UserListCommand(clientFactory, authService, configService));
        AddCommand(new UserGetCommand(clientFactory, authService, configService));
        AddCommand(new UserCreateCommand(clientFactory, authService, configService));
        AddCommand(new UserUpdateCommand(clientFactory, authService, configService));
        AddCommand(new UserDeleteCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Lists all users in an organization.
/// </summary>
public class UserListCommand : Command
{
    public UserListCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("list", "List all users in an organization")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        AddOption(orgIdOption);

        this.SetHandler(async (orgId) =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Tenant Service client
                var client = await clientFactory.CreateTenantServiceClientAsync(profileName);

                // Call API
                var users = await client.ListUsersAsync(orgId, $"Bearer {token}");

                // Display results
                if (users == null || users.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No users found.");
                    return;
                }

                ConsoleHelper.WriteSuccess($"Found {users.Count} user(s) in organization '{orgId}':");
                Console.WriteLine();
                Console.WriteLine($"{"ID",-30} {"Username",-20} {"Email",-30} {"Active",-8} {"Roles",-20}");
                Console.WriteLine(new string('-', 110));
                foreach (var user in users)
                {
                    var roles = user.Roles != null && user.Roles.Count > 0 ? string.Join(", ", user.Roles) : "(none)";
                    var active = user.IsActive ? "Yes" : "No";
                    Console.WriteLine($"{user.Id,-30} {user.Username,-20} {user.Email,-30} {active,-8} {roles,-20}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Organization '{orgId}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Token may be expired. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to list users: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption);
    }
}

/// <summary>
/// Gets a user by ID.
/// </summary>
public class UserGetCommand : Command
{
    public UserGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get a user by ID")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var userIdOption = new Option<string>(
            aliases: new[] { "--user-id", "-u" },
            description: "User ID")
        {
            IsRequired = true
        };

        AddOption(orgIdOption);
        AddOption(userIdOption);

        this.SetHandler(async (orgId, userId) =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Tenant Service client
                var client = await clientFactory.CreateTenantServiceClientAsync(profileName);

                // Call API
                var user = await client.GetUserAsync(orgId, userId, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("User details:");
                Console.WriteLine();
                Console.WriteLine($"  ID:              {user.Id}");
                Console.WriteLine($"  Organization ID: {user.OrganizationId}");
                Console.WriteLine($"  Username:        {user.Username}");
                Console.WriteLine($"  Email:           {user.Email}");
                if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
                {
                    var fullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
                    Console.WriteLine($"  Name:            {fullName}");
                }
                Console.WriteLine($"  Active:          {(user.IsActive ? "Yes" : "No")}");
                if (user.Roles != null && user.Roles.Count > 0)
                {
                    Console.WriteLine($"  Roles:           {string.Join(", ", user.Roles)}");
                }
                Console.WriteLine($"  Created:         {user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                if (user.UpdatedAt.HasValue)
                {
                    Console.WriteLine($"  Updated:         {user.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"User '{userId}' not found in organization '{orgId}'.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get user: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, userIdOption);
    }
}

/// <summary>
/// Creates a new user in an organization.
/// </summary>
public class UserCreateCommand : Command
{
    public UserCreateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("create", "Create a new user in an organization")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var usernameOption = new Option<string>(
            aliases: new[] { "--username", "-u" },
            description: "Username")
        {
            IsRequired = true
        };

        var emailOption = new Option<string>(
            aliases: new[] { "--email", "-e" },
            description: "Email address")
        {
            IsRequired = true
        };

        var passwordOption = new Option<string>(
            aliases: new[] { "--password", "-p" },
            description: "Password")
        {
            IsRequired = true
        };

        var firstNameOption = new Option<string?>(
            aliases: new[] { "--first-name", "-f" },
            description: "First name");

        var lastNameOption = new Option<string?>(
            aliases: new[] { "--last-name", "-l" },
            description: "Last name");

        var rolesOption = new Option<string?>(
            aliases: new[] { "--roles", "-r" },
            description: "Comma-separated list of roles (e.g., Admin,User)");

        AddOption(orgIdOption);
        AddOption(usernameOption);
        AddOption(emailOption);
        AddOption(passwordOption);
        AddOption(firstNameOption);
        AddOption(lastNameOption);
        AddOption(rolesOption);

        this.SetHandler(async (orgId, username, email, password, firstName, lastName, roles) =>
        {
            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Tenant Service client
                var client = await clientFactory.CreateTenantServiceClientAsync(profileName);

                // Build request
                var request = new CreateUserRequest
                {
                    Username = username,
                    Email = email,
                    Password = password,
                    FirstName = firstName,
                    LastName = lastName,
                    Roles = !string.IsNullOrEmpty(roles)
                        ? roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                        : null
                };

                // Call API
                var user = await client.CreateUserAsync(orgId, request, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"User created successfully!");
                Console.WriteLine();
                Console.WriteLine($"  ID:              {user.Id}");
                Console.WriteLine($"  Organization ID: {user.OrganizationId}");
                Console.WriteLine($"  Username:        {user.Username}");
                Console.WriteLine($"  Email:           {user.Email}");
                if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
                {
                    var fullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
                    Console.WriteLine($"  Name:            {fullName}");
                }
                if (user.Roles != null && user.Roles.Count > 0)
                {
                    Console.WriteLine($"  Roles:           {string.Join(", ", user.Roles)}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Organization '{orgId}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError($"Invalid request: {ex.Content}");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteError($"User with that username or email already exists.");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to create user: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, usernameOption, emailOption, passwordOption, firstNameOption, lastNameOption, rolesOption);
    }
}

/// <summary>
/// Updates a user.
/// </summary>
public class UserUpdateCommand : Command
{
    public UserUpdateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("update", "Update a user")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var userIdOption = new Option<string>(
            aliases: new[] { "--user-id", "-u" },
            description: "User ID")
        {
            IsRequired = true
        };

        var emailOption = new Option<string?>(
            aliases: new[] { "--email", "-e" },
            description: "New email address");

        var firstNameOption = new Option<string?>(
            aliases: new[] { "--first-name", "-f" },
            description: "New first name");

        var lastNameOption = new Option<string?>(
            aliases: new[] { "--last-name", "-l" },
            description: "New last name");

        var isActiveOption = new Option<bool?>(
            aliases: new[] { "--active", "-a" },
            description: "Set active status (true/false)");

        var rolesOption = new Option<string?>(
            aliases: new[] { "--roles", "-r" },
            description: "Comma-separated list of roles (replaces existing roles)");

        AddOption(orgIdOption);
        AddOption(userIdOption);
        AddOption(emailOption);
        AddOption(firstNameOption);
        AddOption(lastNameOption);
        AddOption(isActiveOption);
        AddOption(rolesOption);

        this.SetHandler(async (orgId, userId, email, firstName, lastName, isActive, roles) =>
        {
            try
            {
                // Validate that at least one field is provided
                if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(firstName) &&
                    string.IsNullOrEmpty(lastName) && !isActive.HasValue && string.IsNullOrEmpty(roles))
                {
                    ConsoleHelper.WriteError("At least one field (--email, --first-name, --last-name, --active, or --roles) must be provided.");
                    Environment.ExitCode = ExitCodes.ValidationError;
                    return;
                }

                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Tenant Service client
                var client = await clientFactory.CreateTenantServiceClientAsync(profileName);

                // Build request
                var request = new UpdateUserRequest
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    IsActive = isActive,
                    Roles = !string.IsNullOrEmpty(roles)
                        ? roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                        : null
                };

                // Call API
                var user = await client.UpdateUserAsync(orgId, userId, request, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"User updated successfully!");
                Console.WriteLine();
                Console.WriteLine($"  ID:       {user.Id}");
                Console.WriteLine($"  Username: {user.Username}");
                Console.WriteLine($"  Email:    {user.Email}");
                if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
                {
                    var fullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim();
                    Console.WriteLine($"  Name:     {fullName}");
                }
                Console.WriteLine($"  Active:   {(user.IsActive ? "Yes" : "No")}");
                if (user.Roles != null && user.Roles.Count > 0)
                {
                    Console.WriteLine($"  Roles:    {string.Join(", ", user.Roles)}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"User '{userId}' not found in organization '{orgId}'.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError($"Invalid request: {ex.Content}");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to update user: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, userIdOption, emailOption, firstNameOption, lastNameOption, isActiveOption, rolesOption);
    }
}

/// <summary>
/// Deletes a user.
/// </summary>
public class UserDeleteCommand : Command
{
    public UserDeleteCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("delete", "Delete a user")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var userIdOption = new Option<string>(
            aliases: new[] { "--user-id", "-u" },
            description: "User ID")
        {
            IsRequired = true
        };

        var confirmOption = new Option<bool>(
            aliases: new[] { "--yes", "-y" },
            description: "Skip confirmation prompt");

        AddOption(orgIdOption);
        AddOption(userIdOption);
        AddOption(confirmOption);

        this.SetHandler(async (orgId, userId, confirm) =>
        {
            try
            {
                // Confirm deletion
                if (!confirm)
                {
                    if (!ConsoleHelper.Confirm($"Are you sure you want to delete user '{userId}' from organization '{orgId}'?", defaultYes: false))
                    {
                        ConsoleHelper.WriteInfo("Deletion cancelled.");
                        return;
                    }
                }

                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Tenant Service client
                var client = await clientFactory.CreateTenantServiceClientAsync(profileName);

                // Call API
                await client.DeleteUserAsync(orgId, userId, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"User '{userId}' deleted successfully.");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"User '{userId}' not found in organization '{orgId}'.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to delete user: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, userIdOption, confirmOption);
    }
}
