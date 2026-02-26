// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net;
using System.Text.Json;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;

namespace Sorcha.Cli.Commands;

/// <summary>
/// Validator service management commands.
/// </summary>
public class ValidatorCommand : Command
{
    public ValidatorCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("validator", "Manage the validator service")
    {
        Subcommands.Add(new ValidatorStatusCommand(clientFactory, authService, configService));
        Subcommands.Add(new ValidatorStartCommand(clientFactory, authService, configService));
        Subcommands.Add(new ValidatorStopCommand(clientFactory, authService, configService));
        Subcommands.Add(new ValidatorProcessCommand(clientFactory, authService, configService));
        Subcommands.Add(new ValidatorIntegrityCheckCommand(clientFactory, authService, configService));
    }
}

/// <summary>
/// Gets the current validator service status.
/// </summary>
public class ValidatorStatusCommand : Command
{
    public ValidatorStatusCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("status", "Get validator service status")
    {
        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            try
            {
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    return ExitCodes.AuthenticationError;
                }

                var client = await clientFactory.CreateValidatorServiceClientAsync(profileName);
                var status = await client.GetStatusAsync($"Bearer {token}");

                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                ConsoleHelper.WriteSuccess("Validator service status:");
                Console.WriteLine();
                Console.WriteLine($"  Status:              {status.Status}");
                Console.WriteLine($"  Running:             {(status.IsRunning ? "Yes" : "No")}");
                Console.WriteLine($"  Registers Monitored: {status.RegistersMonitored}");
                Console.WriteLine($"  Total Validations:   {status.TotalValidations}");
                Console.WriteLine($"  Failed Validations:  {status.FailedValidations}");
                Console.WriteLine($"  Consensus Protocol:  {status.ConsensusProtocol}");
                Console.WriteLine($"  Uptime:              {status.Uptime}");

                if (status.LastValidationAt.HasValue)
                {
                    Console.WriteLine($"  Last Validation:     {status.LastValidationAt:yyyy-MM-dd HH:mm:ss}");
                }

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to view validator status.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to get validator status: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Starts the validator service.
/// </summary>
public class ValidatorStartCommand : Command
{
    public ValidatorStartCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("start", "Start the validator service")
    {
        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            try
            {
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    return ExitCodes.AuthenticationError;
                }

                var client = await clientFactory.CreateValidatorServiceClientAsync(profileName);
                ConsoleHelper.WriteInfo("Starting validator service...");

                var response = await client.StartAsync($"Bearer {token}");

                ConsoleHelper.WriteSuccess($"Validator service: {response.Status}");
                if (!string.IsNullOrEmpty(response.Message))
                {
                    Console.WriteLine($"  {response.Message}");
                }

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to start the validator service.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteWarning("Validator service is already running.");
                return ExitCodes.Success;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to start validator: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Stops the validator service.
/// </summary>
public class ValidatorStopCommand : Command
{
    private readonly Option<bool> _confirmOption;

    public ValidatorStopCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("stop", "Stop the validator service")
    {
        _confirmOption = new Option<bool>("--yes", "-y")
        {
            Description = "Skip confirmation prompt"
        };

        Options.Add(_confirmOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var confirm = parseResult.GetValue(_confirmOption);

            try
            {
                if (!confirm)
                {
                    ConsoleHelper.WriteWarning("WARNING: Stopping the validator will halt transaction validation.");
                    if (!ConsoleHelper.Confirm("Are you sure you want to stop the validator?", defaultYes: false))
                    {
                        ConsoleHelper.WriteInfo("Stop cancelled.");
                        return ExitCodes.Success;
                    }
                }

                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    return ExitCodes.AuthenticationError;
                }

                var client = await clientFactory.CreateValidatorServiceClientAsync(profileName);
                ConsoleHelper.WriteInfo("Stopping validator service...");

                var response = await client.StopAsync($"Bearer {token}");

                ConsoleHelper.WriteSuccess($"Validator service: {response.Status}");
                if (!string.IsNullOrEmpty(response.Message))
                {
                    Console.WriteLine($"  {response.Message}");
                }

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to stop the validator service.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                ConsoleHelper.WriteWarning("Validator service is already stopped.");
                return ExitCodes.Success;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to stop validator: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Triggers processing of pending transactions for a register.
/// </summary>
public class ValidatorProcessCommand : Command
{
    private readonly Option<string> _registerIdOption;

    public ValidatorProcessCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("process", "Process pending transactions for a register")
    {
        _registerIdOption = new Option<string>("--register-id", "-r")
        {
            Description = "Register ID to process",
            Required = true
        };

        Options.Add(_registerIdOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var registerId = parseResult.GetValue(_registerIdOption)!;

            try
            {
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    return ExitCodes.AuthenticationError;
                }

                var client = await clientFactory.CreateValidatorServiceClientAsync(profileName);
                ConsoleHelper.WriteInfo($"Processing pending transactions for register '{registerId}'...");

                var result = await client.ProcessRegisterAsync(registerId, $"Bearer {token}");

                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                ConsoleHelper.WriteSuccess("Processing complete!");
                Console.WriteLine();
                Console.WriteLine($"  Register ID:            {result.RegisterId}");
                Console.WriteLine($"  Transactions Processed: {result.TransactionsProcessed}");
                Console.WriteLine($"  Transactions Validated: {result.TransactionsValidated}");
                Console.WriteLine($"  Transactions Rejected:  {result.TransactionsRejected}");
                Console.WriteLine($"  Processed At:           {result.ProcessedAt:yyyy-MM-dd HH:mm:ss}");

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{registerId}' not found.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to process transactions.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to process transactions: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Runs an integrity check on a register's chain.
/// </summary>
public class ValidatorIntegrityCheckCommand : Command
{
    private readonly Option<string> _registerIdOption;

    public ValidatorIntegrityCheckCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("integrity-check", "Run integrity check on a register's chain")
    {
        _registerIdOption = new Option<string>("--register-id", "-r")
        {
            Description = "Register ID to check",
            Required = true
        };

        Options.Add(_registerIdOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var registerId = parseResult.GetValue(_registerIdOption)!;

            try
            {
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("Not authenticated. Run 'sorcha auth login' first.");
                    return ExitCodes.AuthenticationError;
                }

                var client = await clientFactory.CreateValidatorServiceClientAsync(profileName);
                ConsoleHelper.WriteInfo($"Running integrity check on register '{registerId}'...");

                var result = await client.IntegrityCheckAsync(registerId, $"Bearer {token}");

                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                if (result.IsValid)
                {
                    ConsoleHelper.WriteSuccess("Integrity check PASSED.");
                }
                else
                {
                    ConsoleHelper.WriteError("Integrity check FAILED.");
                }

                Console.WriteLine();
                Console.WriteLine($"  Register ID:   {result.RegisterId}");
                Console.WriteLine($"  Chain Length:   {result.ChainLength}");
                Console.WriteLine($"  Valid:          {(result.IsValid ? "Yes" : "No")}");
                Console.WriteLine($"  Checked At:    {result.CheckedAt:yyyy-MM-dd HH:mm:ss}");

                if (result.Errors.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("  Errors:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"    - {error}");
                    }
                }

                if (result.Warnings.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("  Warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine($"    - {warning}");
                    }
                }

                return ExitCodes.Success;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ConsoleHelper.WriteError($"Register '{registerId}' not found.");
                return ExitCodes.NotFound;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                ConsoleHelper.WriteError("Authentication failed. Run 'sorcha auth login'.");
                return ExitCodes.AuthenticationError;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                ConsoleHelper.WriteError("You do not have permission to run integrity checks.");
                return ExitCodes.AuthorizationError;
            }
            catch (ApiException ex)
            {
                ConsoleHelper.WriteError($"API error ({ex.StatusCode}): {ex.Content}");
                return ExitCodes.GeneralError;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to run integrity check: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}
