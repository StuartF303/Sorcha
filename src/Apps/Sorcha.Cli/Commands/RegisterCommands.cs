using System.CommandLine;
using System.Net;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Register management commands.
/// </summary>
public class RegisterCommand : Command
{
    public RegisterCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("register", "Manage registers (distributed ledgers)")
    {
        AddCommand(new RegisterListCommand(clientFactory, authService, configService));
        AddCommand(new RegisterGetCommand(clientFactory, authService, configService));
        AddCommand(new RegisterCreateCommand(clientFactory, authService, configService));
        AddCommand(new RegisterDeleteCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Lists all registers.
/// </summary>
public class RegisterListCommand : Command
{
    public RegisterListCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("list", "List all registers")
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
                    ConsoleHelper.WriteError("You must be authenticated to list registers.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var registers = await client.ListRegistersAsync($"Bearer {token}");

                // Display results
                if (registers == null || registers.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No registers found.");
                    return;
                }

                ConsoleHelper.WriteSuccess($"Found {registers.Count} register(s):");
                Console.WriteLine();

                // Display as table
                Console.WriteLine($"{"ID",-38} {"Name",-30} {"Organization",-38} {"Transactions",12} {"Status",-10} {"Created"}");
                Console.WriteLine(new string('-', 160));

                foreach (var register in registers)
                {
                    var status = register.IsActive ? "Active" : "Inactive";
                    Console.WriteLine($"{register.Id,-38} {register.Name,-30} {register.OrganizationId,-38} {register.TransactionCount,12} {status,-10} {register.CreatedAt:yyyy-MM-dd}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to list registers.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to list registers: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Gets a register by ID.
/// </summary>
public class RegisterGetCommand : Command
{
    public RegisterGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get a register by ID")
    {
        var idOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Register ID")
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
                    ConsoleHelper.WriteError("You must be authenticated to get a register.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var register = await client.GetRegisterAsync(id, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("Register details:");
                Console.WriteLine();
                Console.WriteLine($"  ID:              {register.Id}");
                Console.WriteLine($"  Name:            {register.Name}");
                Console.WriteLine($"  Organization:    {register.OrganizationId}");
                Console.WriteLine($"  Status:          {(register.IsActive ? "Active" : "Inactive")}");
                Console.WriteLine($"  Transactions:    {register.TransactionCount}");
                Console.WriteLine($"  Created:         {register.CreatedAt:yyyy-MM-dd HH:mm:ss}");

                if (!string.IsNullOrEmpty(register.Description))
                {
                    Console.WriteLine($"  Description:     {register.Description}");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{id}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view this register.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get register: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, idOption);
    }
}

/// <summary>
/// Creates a new register.
/// </summary>
public class RegisterCreateCommand : Command
{
    public RegisterCreateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("create", "Create a new register")
    {
        var nameOption = new Option<string>(
            aliases: new[] { "--name", "-n" },
            description: "Register name")
        {
            IsRequired = true
        };

        var orgIdOption = new Option<string>(
            aliases: new[] { "--org-id", "-o" },
            description: "Organization ID")
        {
            IsRequired = true
        };

        var descriptionOption = new Option<string?>(
            aliases: new[] { "--description", "-d" },
            description: "Register description");

        AddOption(nameOption);
        AddOption(orgIdOption);
        AddOption(descriptionOption);

        this.SetHandler(async (name, orgId, description) =>
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
                    ConsoleHelper.WriteError("You must be authenticated to create a register.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Build request
                var request = new CreateRegisterRequest
                {
                    Name = name,
                    OrganizationId = orgId,
                    Description = description
                };

                // Call API
                var register = await client.CreateRegisterAsync(request, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess("Register created successfully!");
                Console.WriteLine();
                Console.WriteLine($"  ID:              {register.Id}");
                Console.WriteLine($"  Name:            {register.Name}");
                Console.WriteLine($"  Organization:    {register.OrganizationId}");
                Console.WriteLine($"  Status:          {(register.IsActive ? "Active" : "Inactive")}");
                Console.WriteLine($"  Created:         {register.CreatedAt:yyyy-MM-dd HH:mm:ss}");

                if (!string.IsNullOrEmpty(register.Description))
                {
                    Console.WriteLine($"  Description:     {register.Description}");
                }

                Console.WriteLine();
                ConsoleHelper.WriteInfo($"Use 'sorcha register get --id {register.Id}' to view details.");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError("Invalid request. Please check your input.");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to create registers.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteError($"A register with name '{name}' already exists in organization '{orgId}'.");
                Environment.ExitCode = ExitCodes.ValidationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to create register: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, nameOption, orgIdOption, descriptionOption);
    }
}

/// <summary>
/// Deletes a register.
/// </summary>
public class RegisterDeleteCommand : Command
{
    public RegisterDeleteCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("delete", "Delete a register")
    {
        var idOption = new Option<string>(
            aliases: new[] { "--id", "-i" },
            description: "Register ID")
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
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("You must be authenticated to delete a register.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    Environment.ExitCode = ExitCodes.AuthenticationError;
                    return;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Confirm deletion
                if (!confirm)
                {
                    ConsoleHelper.WriteWarning("⚠️  WARNING: This will permanently delete the register and all its transactions.");
                    Console.Write($"Are you sure you want to delete register '{id}'? [y/N]: ");
                    var response = Console.ReadLine()?.Trim().ToLowerInvariant();

                    if (response != "y" && response != "yes")
                    {
                        ConsoleHelper.WriteInfo("Deletion cancelled.");
                        return;
                    }
                }

                // Call API
                await client.DeleteRegisterAsync(id, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Register '{id}' deleted successfully.");
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{id}' not found.");
                Environment.ExitCode = ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                Environment.ExitCode = ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to delete this register.");
                Environment.ExitCode = ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                Environment.ExitCode = ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to delete register: {ex.Message}");
                Environment.ExitCode = ExitCodes.GeneralError;
            }
        }, idOption, confirmOption);
    }
}
