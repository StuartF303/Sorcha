using System.CommandLine;
using System.Net;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Organization management commands.
/// </summary>
public class OrganizationCommand : Command
{
    public OrganizationCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("org", "Manage organizations")
    {
        AddCommand(new OrgListCommand(clientFactory, authService, configService));
        AddCommand(new OrgGetCommand(clientFactory, authService, configService));
        AddCommand(new OrgCreateCommand(clientFactory, authService, configService));
        AddCommand(new OrgUpdateCommand(clientFactory, authService, configService));
        AddCommand(new OrgDeleteCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Lists all organizations.
/// </summary>
public class OrgListCommand : Command
{
    public OrgListCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("list", "List all organizations")
    {
        this.SetHandler(async () =>
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
                var organizations = await client.ListOrganizationsAsync($"Bearer {token}");

                // Display results
                if (organizations == null || organizations.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No organizations found.");
                    return;
                }

                ConsoleHelper.WriteSuccess($"Found {organizations.Count} organization(s):");
                Console.WriteLine();
                Console.WriteLine($"{"ID",-30} {"Name",-30} {"Subdomain",-20}");
                Console.WriteLine(new string('-', 82));
                foreach (var org in organizations)
                {
                    Console.WriteLine($"{org.Id,-30} {org.Name,-30} {org.Subdomain,-20}");
                }
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
                ConsoleHelper.WriteError($"Failed to list organizations: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Gets an organization by ID.
/// </summary>
public class OrgGetCommand : Command
{
    public OrgGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get an organization by ID")
    {
        var idOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        AddOption(idOption);

        this.SetHandler(async (id) =>
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
                var org = await client.GetOrganizationAsync(id, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("Organization details:");
                Console.WriteLine();
                Console.WriteLine($"  ID:          {org.Id}");
                Console.WriteLine($"  Name:        {org.Name}");
                Console.WriteLine($"  Subdomain:   {org.Subdomain ?? "(none)"}");
                Console.WriteLine($"  Description: {org.Description ?? "(none)"}");
                Console.WriteLine($"  Created:     {org.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                if (org.UpdatedAt.HasValue)
                {
                    Console.WriteLine($"  Updated:     {org.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Organization '{id}' not found.");
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
                ConsoleHelper.WriteError($"Failed to get organization: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, idOption);
    }
}

/// <summary>
/// Creates a new organization.
/// </summary>
public class OrgCreateCommand : Command
{
    public OrgCreateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("create", "Create a new organization")
    {
        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Organization name")
        {
            IsRequired = true
        };

        var subdomainOption = new Option<string?>(
            aliases: new[] { "--subdomain", "-s" },
            description: "Organization subdomain");

        var descriptionOption = new Option<string?>(
            aliases: new[] { "--description", "-d" },
            description: "Organization description");

        AddOption(nameOption);
        AddOption(subdomainOption);
        AddOption(descriptionOption);

        this.SetHandler(async (name, subdomain, description) =>
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
                var request = new CreateOrganizationRequest
                {
                    Name = name,
                    Subdomain = subdomain,
                    Description = description
                };

                // Call API
                var org = await client.CreateOrganizationAsync(request, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Organization created successfully!");
                Console.WriteLine();
                Console.WriteLine($"  ID:          {org.Id}");
                Console.WriteLine($"  Name:        {org.Name}");
                Console.WriteLine($"  Subdomain:   {org.Subdomain ?? "(none)"}");
                if (!string.IsNullOrEmpty(org.Description))
                {
                    Console.WriteLine($"  Description: {org.Description}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError($"Invalid request: {ex.Content}");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteError($"Organization with that name or subdomain already exists.");
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
                ConsoleHelper.WriteError($"Failed to create organization: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, nameOption, subdomainOption, descriptionOption);
    }
}

/// <summary>
/// Updates an organization.
/// </summary>
public class OrgUpdateCommand : Command
{
    public OrgUpdateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("update", "Update an organization")
    {
        var idOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var nameOption = new Option<string?>(
            aliases: new[] { "--name", "-n" },
            description: "Organization name");

        var descriptionOption = new Option<string?>(
            aliases: new[] { "--description", "-d" },
            description: "Organization description");

        AddOption(idOption);
        AddOption(nameOption);
        AddOption(descriptionOption);

        this.SetHandler(async (id, name, description) =>
        {
            try
            {
                // Validate that at least one field is provided
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(description))
                {
                    ConsoleHelper.WriteError("At least one field (--name or --description) must be provided.");
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
                var request = new UpdateOrganizationRequest
                {
                    Name = name,
                    Description = description
                };

                // Call API
                var org = await client.UpdateOrganizationAsync(id, request, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Organization updated successfully!");
                Console.WriteLine();
                Console.WriteLine($"  ID:          {org.Id}");
                Console.WriteLine($"  Name:        {org.Name}");
                Console.WriteLine($"  Description: {org.Description ?? "(none)"}");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Organization '{id}' not found.");
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
                ConsoleHelper.WriteError($"Failed to update organization: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, idOption, nameOption, descriptionOption);
    }
}

/// <summary>
/// Deletes an organization.
/// </summary>
public class OrgDeleteCommand : Command
{
    public OrgDeleteCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("delete", "Delete an organization")
    {
        var idOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var confirmOption = new Option<bool>(
            aliases: new[] { "--yes", "-y" },
            description: "Skip confirmation prompt");

        AddOption(idOption);
        AddOption(confirmOption);

        this.SetHandler(async (id, confirm) =>
        {
            try
            {
                // Confirm deletion
                if (!confirm)
                {
                    if (!ConsoleHelper.Confirm($"Are you sure you want to delete organization '{id}'?", defaultYes: false))
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
                await client.DeleteOrganizationAsync(id, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Organization '{id}' deleted successfully.");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Organization '{id}' not found.");
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
                ConsoleHelper.WriteError($"Failed to delete organization: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, idOption, confirmOption);
    }
}
