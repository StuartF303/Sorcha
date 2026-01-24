using System.CommandLine;
using System.CommandLine.Parsing;
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
        Subcommands.Add(new RegisterListCommand(clientFactory, authService, configService));
        Subcommands.Add(new RegisterGetCommand(clientFactory, authService, configService));
        Subcommands.Add(new RegisterCreateCommand(clientFactory, authService, configService));
        Subcommands.Add(new RegisterDeleteCommand(clientFactory, authService, configService));
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
        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
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
                    return ExitCodes.AuthenticationError;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var registers = await client.ListRegistersAsync($"Bearer {token}");

                // Display results
                if (registers == null || registers.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No registers found.");
                    return ExitCodes.Success;
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

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to list registers.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to list registers: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Gets a register by ID.
/// </summary>
public class RegisterGetCommand : Command
{
    private readonly Option<string> _idOption;

    public RegisterGetCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("get", "Get a register by ID")
    {
        _idOption = new Option<string>("--id", "-i")
        {
            Description = "Register ID",
            Required = true
        };

        Options.Add(_idOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var id = parseResult.GetValue(_idOption)!;

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
                    return ExitCodes.AuthenticationError;
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

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{id}' not found.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view this register.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get register: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Creates a new register.
/// </summary>
public class RegisterCreateCommand : Command
{
    private readonly Option<string> _nameOption;
    private readonly Option<string> _orgIdOption;
    private readonly Option<string?> _descriptionOption;

    public RegisterCreateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("create", "Create a new register")
    {
        _nameOption = new Option<string>("--name", "-n")
        {
            Description = "Register name",
            Required = true
        };

        _orgIdOption = new Option<string>("--org-id", "-o")
        {
            Description = "Organization ID",
            Required = true
        };

        _descriptionOption = new Option<string?>("--description", "-d")
        {
            Description = "Register description"
        };

        Options.Add(_nameOption);
        Options.Add(_orgIdOption);
        Options.Add(_descriptionOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var name = parseResult.GetValue(_nameOption)!;
            var orgId = parseResult.GetValue(_orgIdOption)!;
            var description = parseResult.GetValue(_descriptionOption);

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
                    return ExitCodes.AuthenticationError;
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

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                ConsoleHelper.WriteError("Invalid request. Please check your input.");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.ValidationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to create registers.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteError($"A register with name '{name}' already exists in organization '{orgId}'.");
                return ExitCodes.ValidationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to create register: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Deletes a register.
/// </summary>
public class RegisterDeleteCommand : Command
{
    private readonly Option<string> _idOption;
    private readonly Option<bool> _confirmOption;

    public RegisterDeleteCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("delete", "Delete a register")
    {
        _idOption = new Option<string>("--id", "-i")
        {
            Description = "Register ID",
            Required = true
        };

        _confirmOption = new Option<bool>("--yes", "-y")
        {
            Description = "Skip confirmation prompt"
        };

        Options.Add(_idOption);
        Options.Add(_confirmOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var id = parseResult.GetValue(_idOption)!;
            var confirm = parseResult.GetValue(_confirmOption);

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
                    return ExitCodes.AuthenticationError;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Confirm deletion
                if (!confirm)
                {
                    ConsoleHelper.WriteWarning("WARNING: This will permanently delete the register and all its transactions.");
                    Console.Write($"Are you sure you want to delete register '{id}'? [y/N]: ");
                    var response = Console.ReadLine()?.Trim().ToLowerInvariant();

                    if (response != "y" && response != "yes")
                    {
                        ConsoleHelper.WriteInfo("Deletion cancelled.");
                        return ExitCodes.Success;
                    }
                }

                // Call API
                await client.DeleteRegisterAsync(id, $"Bearer {token}");

                // Display results
                ConsoleHelper.WriteSuccess($"Register '{id}' deleted successfully.");
                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{id}' not found.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Your access token may have expired.");
                ConsoleHelper.WriteInfo("Run 'sorcha auth login' to re-authenticate.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to delete this register.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API Error: {ex.Message}");
                if (ex.Content != null)
                {
                    ConsoleHelper.WriteError($"Details: {ex.Content}");
                }
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to delete register: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}
