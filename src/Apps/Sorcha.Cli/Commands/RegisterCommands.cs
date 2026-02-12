// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.CommandLine;
using System.CommandLine.Parsing;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using Refit;
using Sorcha.Cli.Infrastructure;
using Sorcha.Cli.Models;
using Sorcha.Cli.Services;
using Sorcha.Register.Models;
using Sorcha.Register.Models.Enums;

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
        Subcommands.Add(new RegisterUpdateCommand(clientFactory, authService, configService));
        Subcommands.Add(new RegisterStatsCommand(clientFactory, authService, configService));
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

                // Check output format
                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(registers, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                // Display results
                if (registers == null || registers.Count == 0)
                {
                    ConsoleHelper.WriteInfo("No registers found.");
                    return ExitCodes.Success;
                }

                ConsoleHelper.WriteSuccess($"Found {registers.Count} register(s):");
                Console.WriteLine();

                // Display as table with new fields
                Console.WriteLine($"{"ID",-34} {"Name",-25} {"Height",8} {"Status",-10} {"TenantId",-34} {"Advertise",-9} {"Created"}");
                Console.WriteLine(new string('-', 145));

                foreach (var register in registers)
                {
                    var advertise = register.Advertise ? "Yes" : "No";
                    Console.WriteLine($"{register.Id,-34} {register.Name,-25} {register.Height,8} {register.Status,-10} {register.TenantId,-34} {advertise,-9} {register.CreatedAt:yyyy-MM-dd}");
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

                // Check output format
                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(register, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                // Display results with all new fields
                ConsoleHelper.WriteSuccess("Register details:");
                Console.WriteLine();
                Console.WriteLine($"  ID:              {register.Id}");
                Console.WriteLine($"  Name:            {register.Name}");
                Console.WriteLine($"  TenantId:        {register.TenantId}");
                Console.WriteLine($"  Status:          {register.Status}");
                Console.WriteLine($"  Height:          {register.Height}");
                Console.WriteLine($"  Advertise:       {(register.Advertise ? "Yes" : "No")}");
                Console.WriteLine($"  IsFullReplica:   {(register.IsFullReplica ? "Yes" : "No")}");
                Console.WriteLine($"  Created:         {register.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Updated:         {register.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

                if (!string.IsNullOrEmpty(register.Votes))
                {
                    Console.WriteLine($"  Votes:           {register.Votes}");
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
/// Creates a new register using the two-phase cryptographic attestation flow.
/// </summary>
public class RegisterCreateCommand : Command
{
    private readonly Option<string> _nameOption;
    private readonly Option<string> _tenantIdOption;
    private readonly Option<string> _ownerWalletOption;
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

        _tenantIdOption = new Option<string>("--tenant-id", "-t")
        {
            Description = "Tenant ID",
            Required = true
        };

        _ownerWalletOption = new Option<string>("--owner-wallet", "-w")
        {
            Description = "Owner wallet address for signing attestation",
            Required = true
        };

        _descriptionOption = new Option<string?>("--description", "-d")
        {
            Description = "Register description"
        };

        Options.Add(_nameOption);
        Options.Add(_tenantIdOption);
        Options.Add(_ownerWalletOption);
        Options.Add(_descriptionOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var name = parseResult.GetValue(_nameOption)!;
            var tenantId = parseResult.GetValue(_tenantIdOption)!;
            var ownerWallet = parseResult.GetValue(_ownerWalletOption)!;
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

                // Extract user ID from token claims
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                    ?? throw new InvalidOperationException("Could not extract user ID from token");

                // Create clients
                var registerClient = await clientFactory.CreateRegisterServiceClientAsync(profileName);
                var walletClient = await clientFactory.CreateWalletServiceClientAsync(profileName);

                ConsoleHelper.WriteInfo("Phase 1: Initiating register creation...");

                // Build initiation request
                var initiateRequest = new InitiateRegisterCreationRequest
                {
                    Name = name,
                    TenantId = tenantId,
                    Description = description,
                    Owners = new List<OwnerInfo>
                    {
                        new OwnerInfo
                        {
                            UserId = userId,
                            WalletId = ownerWallet
                        }
                    }
                };

                // Phase 1: Initiate
                var initiateResponse = await registerClient.InitiateRegisterCreationAsync(initiateRequest, $"Bearer {token}");

                // Check expiration
                if (initiateResponse.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    ConsoleHelper.WriteError("Registration expired before signing could begin. Please try again.");
                    return ExitCodes.GeneralError;
                }

                ConsoleHelper.WriteInfo($"  Register ID: {initiateResponse.RegisterId}");
                ConsoleHelper.WriteInfo($"  Expires at: {initiateResponse.ExpiresAt:HH:mm:ss}");
                ConsoleHelper.WriteInfo($"  Attestations to sign: {initiateResponse.AttestationsToSign.Count}");

                // Phase 2: Sign attestations
                ConsoleHelper.WriteInfo("Phase 2: Signing attestations...");

                var signedAttestations = new List<SignedAttestation>();

                foreach (var attestation in initiateResponse.AttestationsToSign)
                {
                    ConsoleHelper.WriteInfo($"  Signing attestation for {attestation.Role}...");

                    // Convert hex hash to base64 for signing
                    var hashBytes = Convert.FromHexString(attestation.DataToSign);
                    var base64Hash = Convert.ToBase64String(hashBytes);

                    // Sign using wallet service with IsPreHashed=true
                    var signRequest = new SignTransactionRequest
                    {
                        TransactionData = base64Hash,
                        IsPreHashed = true
                    };

                    SignTransactionResponse signResponse;
                    try
                    {
                        signResponse = await walletClient.SignTransactionAsync(attestation.WalletId, signRequest, $"Bearer {token}");
                    }
                    catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable || ex.StatusCode == HttpStatusCode.GatewayTimeout)
                    {
                        ConsoleHelper.WriteError("Wallet service is unreachable. Please ensure the wallet service is running.");
                        return ExitCodes.GeneralError;
                    }
                    catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        ConsoleHelper.WriteError($"Wallet '{attestation.WalletId}' not found.");
                        return ExitCodes.NotFound;
                    }

                    // Parse algorithm
                    if (!Enum.TryParse<SignatureAlgorithm>(signResponse.Algorithm, true, out var algorithm))
                    {
                        algorithm = SignatureAlgorithm.ED25519; // Default
                    }

                    signedAttestations.Add(new SignedAttestation
                    {
                        AttestationData = attestation.AttestationData,
                        PublicKey = signResponse.PublicKey,
                        Signature = signResponse.Signature,
                        Algorithm = algorithm
                    });
                }

                // Check expiration again before finalize
                if (initiateResponse.ExpiresAt < DateTimeOffset.UtcNow)
                {
                    ConsoleHelper.WriteError("Registration expired during signing. Please try again.");
                    ConsoleHelper.WriteInfo("Tip: Ensure your wallet service responds quickly.");
                    return ExitCodes.GeneralError;
                }

                // Phase 3: Finalize
                ConsoleHelper.WriteInfo("Phase 3: Finalizing register creation...");

                var finalizeRequest = new FinalizeRegisterCreationRequest
                {
                    RegisterId = initiateResponse.RegisterId,
                    Nonce = initiateResponse.Nonce,
                    SignedAttestations = signedAttestations
                };

                FinalizeRegisterCreationResponse finalizeResponse;
                try
                {
                    finalizeResponse = await registerClient.FinalizeRegisterCreationAsync(finalizeRequest, $"Bearer {token}");
                }
                catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Gone)
                {
                    ConsoleHelper.WriteError("Registration expired. The 5-minute window has passed. Please try again.");
                    return ExitCodes.GeneralError;
                }
                catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    ConsoleHelper.WriteError("Invalid signature or attestation data.");
                    if (ex.Content != null)
                    {
                        ConsoleHelper.WriteError($"Details: {ex.Content}");
                    }
                    return ExitCodes.ValidationError;
                }

                // Check output format
                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(finalizeResponse, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                // Display results
                ConsoleHelper.WriteSuccess("Register created successfully!");
                Console.WriteLine();
                Console.WriteLine($"  Register ID:       {finalizeResponse.RegisterId}");
                Console.WriteLine($"  Genesis TX ID:     {finalizeResponse.GenesisTransactionId}");
                Console.WriteLine($"  Genesis Docket ID: {finalizeResponse.GenesisDocketId}");
                Console.WriteLine($"  Created:           {finalizeResponse.CreatedAt:yyyy-MM-dd HH:mm:ss}");

                Console.WriteLine();
                ConsoleHelper.WriteInfo($"Use 'sorcha register get --id {finalizeResponse.RegisterId}' to view details.");

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
                ConsoleHelper.WriteError($"A register with name '{name}' already exists in tenant '{tenantId}'.");
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

/// <summary>
/// Updates a register's metadata.
/// </summary>
public class RegisterUpdateCommand : Command
{
    private readonly Option<string> _idOption;
    private readonly Option<string?> _nameOption;
    private readonly Option<string?> _statusOption;
    private readonly Option<bool?> _advertiseOption;

    public RegisterUpdateCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("update", "Update register metadata")
    {
        _idOption = new Option<string>("--id", "-i")
        {
            Description = "Register ID",
            Required = true
        };

        _nameOption = new Option<string?>("--name", "-n")
        {
            Description = "New register name"
        };

        _statusOption = new Option<string?>("--status", "-s")
        {
            Description = "New status (Online, Offline, Checking, Recovery)"
        };

        _advertiseOption = new Option<bool?>("--advertise", "-a")
        {
            Description = "Whether to advertise on peer network (true/false)"
        };

        Options.Add(_idOption);
        Options.Add(_nameOption);
        Options.Add(_statusOption);
        Options.Add(_advertiseOption);

        this.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            var id = parseResult.GetValue(_idOption)!;
            var name = parseResult.GetValue(_nameOption);
            var status = parseResult.GetValue(_statusOption);
            var advertise = parseResult.GetValue(_advertiseOption);

            // Validate at least one update field
            if (name == null && status == null && advertise == null)
            {
                ConsoleHelper.WriteError("At least one update option is required (--name, --status, or --advertise).");
                return ExitCodes.ValidationError;
            }

            try
            {
                // Get active profile
                var profile = await configService.GetActiveProfileAsync();
                var profileName = profile?.Name ?? "dev";

                // Get access token
                var token = await authService.GetAccessTokenAsync(profileName);
                if (string.IsNullOrEmpty(token))
                {
                    ConsoleHelper.WriteError("You must be authenticated to update a register.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    return ExitCodes.AuthenticationError;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Build request
                var request = new UpdateRegisterRequest
                {
                    Name = name,
                    Status = status,
                    Advertise = advertise
                };

                // Call API
                var register = await client.UpdateRegisterAsync(id, request, $"Bearer {token}");

                // Check output format
                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(register, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                // Display results
                ConsoleHelper.WriteSuccess("Register updated successfully!");
                Console.WriteLine();
                Console.WriteLine($"  ID:              {register.Id}");
                Console.WriteLine($"  Name:            {register.Name}");
                Console.WriteLine($"  TenantId:        {register.TenantId}");
                Console.WriteLine($"  Status:          {register.Status}");
                Console.WriteLine($"  Advertise:       {(register.Advertise ? "Yes" : "No")}");
                Console.WriteLine($"  Updated:         {register.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

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
                ConsoleHelper.WriteError("You do not have permission to update this register.");
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
                ConsoleHelper.WriteError($"Failed to update register: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}

/// <summary>
/// Gets register statistics.
/// </summary>
public class RegisterStatsCommand : Command
{
    public RegisterStatsCommand(
        HttpClientFactory clientFactory,
        IAuthenticationService authService,
        IConfigurationService configService)
        : base("stats", "Get register statistics")
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
                    ConsoleHelper.WriteError("You must be authenticated to get register statistics.");
                    ConsoleHelper.WriteInfo("Run 'sorcha auth login' to authenticate.");
                    return ExitCodes.AuthenticationError;
                }

                // Create Register Service client
                var client = await clientFactory.CreateRegisterServiceClientAsync(profileName);

                // Call API
                var stats = await client.GetRegisterStatsAsync($"Bearer {token}");

                // Check output format
                var outputFormat = parseResult.GetValue(BaseCommand.OutputOption) ?? "table";
                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true }));
                    return ExitCodes.Success;
                }

                // Display results
                ConsoleHelper.WriteSuccess("Register statistics:");
                Console.WriteLine();
                Console.WriteLine($"  Total registers: {stats.Count}");

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
                ConsoleHelper.WriteError("You do not have permission to view register statistics.");
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
                ConsoleHelper.WriteError($"Failed to get register statistics: {ex.Message}");
                return ExitCodes.GeneralError;
            }
        });
    }
}
