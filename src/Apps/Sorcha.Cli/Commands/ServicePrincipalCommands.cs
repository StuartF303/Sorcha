using System.CommandLine;
using System.Net;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Service principal management commands.
/// </summary>
public class ServicePrincipalCommand : Command
{
    public ServicePrincipalCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("principal", "Manage service principals within organizations")
    {
        AddCommand(new PrincipalListCommand(clientFactory, authService, configService));
        AddCommand(new PrincipalGetCommand(clientFactory, authService, configService));
        AddCommand(new PrincipalCreateCommand(clientFactory, authService, configService));
        AddCommand(new PrincipalDeleteCommand(clientFactory, authService, configService));
        AddCommand(new PrincipalRotateSecretCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Lists all service principals in an organization.
/// </summary>
public class PrincipalListCommand : Command
{
    public PrincipalListCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("list", "List all service principals in an organization")
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
                var principals = await client.ListServicePrincipalsAsync(orgId, $"Bearer {token}");

                // Display results
                if (principals == null || principals.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No service principals found.");
                    return;
                }

                ConsoleHelper.WriteSuccess($"Found {principals.Count} service principal(s) in organization '{orgId}':");
                Console.WriteLine();
                Console.WriteLine($"{"Client ID",-40} {"Name",-30} {"Active",-8} {"Scopes",-20}");
                Console.WriteLine(new string('-', 100));
                foreach (var sp in principals)
                {
                    var scopes = sp.Scopes != null && sp.Scopes.Count > 0 ? string.Join(", ", sp.Scopes) : "(none)";
                    var active = sp.IsActive ? "Yes" : "No";
                    Console.WriteLine($"{sp.ClientId,-40} {sp.Name,-30} {active,-8} {scopes,-20}");
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
                ConsoleHelper.WriteError($"Failed to list service principals: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption);
    }
}

/// <summary>
/// Gets a service principal by client ID.
/// </summary>
public class PrincipalGetCommand : Command
{
    public PrincipalGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get a service principal by client ID")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var clientIdOption = new Option<string>(
            aliases: new[] { "--client-id", "-c" },
            description: "Service principal client ID")
        {
            IsRequired = true
        };

        AddOption(orgIdOption);
        AddOption(clientIdOption);

        this.SetHandler(async (orgId, clientId) =>
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
                var sp = await client.GetServicePrincipalAsync(orgId, clientId, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("Service principal details:");
                Console.WriteLine();
                Console.WriteLine($"  Client ID:       {sp.ClientId}");
                Console.WriteLine($"  Organization ID: {sp.OrganizationId}");
                Console.WriteLine($"  Name:            {sp.Name}");
                if (!string.IsNullOrEmpty(sp.Description))
                {
                    Console.WriteLine($"  Description:     {sp.Description}");
                }
                Console.WriteLine($"  Active:          {(sp.IsActive ? "Yes" : "No")}");
                if (sp.Scopes != null && sp.Scopes.Count > 0)
                {
                    Console.WriteLine($"  Scopes:          {string.Join(", ", sp.Scopes)}");
                }
                Console.WriteLine($"  Created:         {sp.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                if (sp.UpdatedAt.HasValue)
                {
                    Console.WriteLine($"  Updated:         {sp.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                if (sp.SecretRotatedAt.HasValue)
                {
                    Console.WriteLine($"  Secret Rotated:  {sp.SecretRotatedAt:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Service principal '{clientId}' not found in organization '{orgId}'.");
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
                ConsoleHelper.WriteError($"Failed to get service principal: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, clientIdOption);
    }
}

/// <summary>
/// Creates a new service principal in an organization.
/// </summary>
public class PrincipalCreateCommand : Command
{
    public PrincipalCreateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("create", "Create a new service principal in an organization")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Service principal name")
        {
            IsRequired = true
        };

        var descriptionOption = new Option<string?>(
            aliases: new[] { "--description", "-d" },
            description: "Service principal description");

        var scopesOption = new Option<string?>(
            aliases: new[] { "--scopes", "-s" },
            description: "Comma-separated list of scopes/permissions (e.g., read,write,admin)");

        AddOption(orgIdOption);
        AddOption(nameOption);
        AddOption(descriptionOption);
        AddOption(scopesOption);

        this.SetHandler(async (orgId, name, description, scopes) =>
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
                var request = new CreateServicePrincipalRequest
                {
                    Name = name,
                    Description = description,
                    Scopes = !string.IsNullOrEmpty(scopes)
                        ? scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                        : null
                };

                // Call API
                var response = await client.CreateServicePrincipalAsync(orgId, request, $"Bearer {token}");

                // Display results with security warning
                ConsoleHelper.WriteSuccess($"Service principal created successfully!");
                Console.WriteLine();
                Console.WriteLine($"  Client ID:       {response.ServicePrincipal.ClientId}");
                Console.WriteLine($"  Organization ID: {response.ServicePrincipal.OrganizationId}");
                Console.WriteLine($"  Name:            {response.ServicePrincipal.Name}");
                if (!string.IsNullOrEmpty(response.ServicePrincipal.Description))
                {
                    Console.WriteLine($"  Description:     {response.ServicePrincipal.Description}");
                }
                if (response.ServicePrincipal.Scopes != null && response.ServicePrincipal.Scopes.Count > 0)
                {
                    Console.WriteLine($"  Scopes:          {string.Join(", ", response.ServicePrincipal.Scopes)}");
                }
                Console.WriteLine();
                ConsoleHelper.WriteWarning("⚠️  IMPORTANT: Save these credentials securely!");
                Console.WriteLine($"  Client Secret:   {response.ClientSecret}");
                Console.WriteLine();
                ConsoleHelper.WriteWarning("The client secret will NEVER be displayed again.");
                ConsoleHelper.WriteWarning("Store it in a secure location (e.g., Azure Key Vault, AWS Secrets Manager).");
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
                ConsoleHelper.WriteError($"Service principal with that name already exists.");
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
                ConsoleHelper.WriteError($"Failed to create service principal: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, nameOption, descriptionOption, scopesOption);
    }
}

/// <summary>
/// Deletes a service principal.
/// </summary>
public class PrincipalDeleteCommand : Command
{
    public PrincipalDeleteCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("delete", "Delete a service principal")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var clientIdOption = new Option<string>(
            aliases: new[] { "--client-id", "-c" },
            description: "Service principal client ID")
        {
            IsRequired = true
        };

        var confirmOption = new Option<bool>(
            aliases: new[] { "--yes", "-y" },
            description: "Skip confirmation prompt");

        AddOption(orgIdOption);
        AddOption(clientIdOption);
        AddOption(confirmOption);

        this.SetHandler(async (orgId, clientId, confirm) =>
        {
            try
            {
                // Confirm deletion
                if (!confirm)
                {
                    if (!ConsoleHelper.Confirm($"Are you sure you want to delete service principal '{clientId}' from organization '{orgId}'?", defaultYes: false))
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
                await client.DeleteServicePrincipalAsync(orgId, clientId, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Service principal '{clientId}' deleted successfully.");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Service principal '{clientId}' not found in organization '{orgId}'.");
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
                ConsoleHelper.WriteError($"Failed to delete service principal: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, clientIdOption, confirmOption);
    }
}

/// <summary>
/// Rotates the client secret for a service principal.
/// </summary>
public class PrincipalRotateSecretCommand : Command
{
    public PrincipalRotateSecretCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("rotate-secret", "Rotate the client secret for a service principal")
    {
        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var clientIdOption = new Option<string>(
            aliases: new[] { "--client-id", "-c" },
            description: "Service principal client ID")
        {
            IsRequired = true
        };

        AddOption(orgIdOption);
        AddOption(clientIdOption);

        this.SetHandler(async (orgId, clientId) =>
        {
            try
            {
                // Warn about secret rotation
                ConsoleHelper.WriteWarning("⚠️  WARNING: The old client secret will be immediately invalidated!");
                if (!ConsoleHelper.Confirm($"Are you sure you want to rotate the secret for service principal '{clientId}'?", defaultYes: false))
                {
                    ConsoleHelper.WriteInfo("Secret rotation cancelled.");
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

                // Call API
                var response = await client.RotateSecretAsync(orgId, clientId, $"Bearer {token}");

                // Display results with security warning
                ConsoleHelper.WriteSuccess($"Client secret rotated successfully!");
                Console.WriteLine();
                Console.WriteLine($"  Rotated At:      {response.RotatedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
                ConsoleHelper.WriteWarning("⚠️  IMPORTANT: Save the new client secret securely!");
                Console.WriteLine($"  New Client Secret: {response.ClientSecret}");
                Console.WriteLine();
                ConsoleHelper.WriteWarning("The new client secret will NEVER be displayed again.");
                ConsoleHelper.WriteWarning("Update all applications using this service principal immediately.");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Service principal '{clientId}' not found in organization '{orgId}'.");
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
                ConsoleHelper.WriteError($"Failed to rotate secret: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, orgIdOption, clientIdOption);
    }
}
