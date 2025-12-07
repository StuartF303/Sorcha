// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text;
using System.Text.Json;
using Sorcha.Cli.Services;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Workflows;

/// <summary>
/// User workflow: wallet management, blueprint creation, transaction submission
/// </summary>
public class UserWorkflow : IWorkflow
{
    private readonly WalletApiClient _walletClient;
    private readonly BlueprintApiClient _blueprintClient;
    private readonly RegisterApiClient _registerClient;

    public string Name => "User Workflow";
    public string Description => "Execute user tasks: wallets, blueprints, and transactions";

    public IEnumerable<string> StepNames =>
    [
        "List existing wallets",
        "Create new ED25519 wallet",
        "Get wallet details",
        "Sign test data",
        "List blueprints",
        "Create simple blueprint",
        "Get blueprint details",
        "Publish blueprint",
        "List registers",
        "Create new register",
        "Submit transaction",
        "Query transactions",
        "Cleanup (delete test data)"
    ];

    private string? _createdWalletAddress;
    private string? _createdBlueprintId;
    private string? _createdRegisterId;

    public UserWorkflow(
        WalletApiClient walletClient,
        BlueprintApiClient blueprintClient,
        RegisterApiClient registerClient)
    {
        _walletClient = walletClient;
        _blueprintClient = blueprintClient;
        _registerClient = registerClient;
    }

    public async Task ExecuteAsync(WorkflowProgress progress, ActivityLog activityLog, CancellationToken ct = default)
    {
        // Step 1: List existing wallets
        progress.StartStep("Fetching user's wallets");
        activityLog.LogInfo("Listing wallets for authenticated user");

        try
        {
            var wallets = await _walletClient.ListWalletsAsync(ct);
            progress.CompleteStep($"Found {wallets?.Count ?? 0} wallets");
            if (wallets != null)
            {
                foreach (var w in wallets.Take(3))
                {
                    activityLog.LogInfo($"  - {w.Name}: {w.Address[..16]}... ({w.Algorithm})");
                }
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Failed to list wallets: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 2: Create new wallet
        progress.StartStep("Creating ED25519 wallet");
        var walletName = $"CLI-Wallet-{DateTime.Now:HHmmss}";
        activityLog.LogInfo($"Creating wallet: {walletName}");

        try
        {
            var wallet = await _walletClient.CreateWalletAsync(new CreateWalletRequest(
                Name: walletName,
                Algorithm: "ED25519",
                WordCount: 12
            ), ct);

            if (wallet != null)
            {
                _createdWalletAddress = wallet.Address;
                progress.CompleteStep($"Address: {wallet.Address[..20]}...");
                activityLog.LogSuccess($"Wallet created: {wallet.Address}");
                activityLog.LogInfo($"  Algorithm: {wallet.Algorithm}");
            }
            else
            {
                progress.FailStep("Wallet creation returned null");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Wallet creation failed: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 3: Get wallet details
        if (_createdWalletAddress != null)
        {
            progress.StartStep("Fetching wallet details");
            activityLog.LogInfo($"Getting wallet: {_createdWalletAddress[..20]}...");

            try
            {
                var wallet = await _walletClient.GetWalletAsync(_createdWalletAddress, ct);
                if (wallet != null)
                {
                    progress.CompleteStep($"PublicKey: {wallet.PublicKey[..20]}...");
                    activityLog.LogSuccess($"Wallet verified, created at {wallet.CreatedAt}");
                }
                else
                {
                    progress.FailStep("Wallet not found");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"Failed to get wallet: {ex.Message}");
            }
        }
        else
        {
            progress.SkipStep("No wallet address available");
        }

        await Task.Delay(200, ct);

        // Step 4: Sign test data
        if (_createdWalletAddress != null)
        {
            progress.StartStep("Signing test data");
            var testData = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello from Sorcha CLI!"));
            activityLog.LogInfo("Signing: 'Hello from Sorcha CLI!'");

            try
            {
                var signature = await _walletClient.SignDataAsync(
                    _createdWalletAddress,
                    new SignRequest(testData),
                    ct);

                if (signature != null)
                {
                    progress.CompleteStep($"Signature: {signature.Signature[..30]}...");
                    activityLog.LogSuccess("Data signed successfully");
                }
                else
                {
                    progress.FailStep("Signing returned null");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"Signing failed: {ex.Message}");
            }
        }
        else
        {
            progress.SkipStep("No wallet available for signing");
        }

        await Task.Delay(200, ct);

        // Step 5: List blueprints
        progress.StartStep("Listing blueprints");
        activityLog.LogInfo("Fetching blueprints (page 1)");

        try
        {
            var blueprints = await _blueprintClient.ListBlueprintsAsync(1, 10, ct);
            progress.CompleteStep($"Found {blueprints?.TotalCount ?? 0} blueprints");
            if (blueprints?.Items != null)
            {
                foreach (var bp in blueprints.Items.Take(3))
                {
                    activityLog.LogInfo($"  - {bp.Title} (v{bp.Version}): {bp.Status}");
                }
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Failed to list blueprints: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 6: Create blueprint
        progress.StartStep("Creating simple blueprint");
        _createdBlueprintId = $"cli-blueprint-{DateTime.Now:HHmmss}";
        activityLog.LogInfo($"Creating blueprint: {_createdBlueprintId}");

        try
        {
            var blueprint = await _blueprintClient.CreateBlueprintAsync(new CreateBlueprintRequest(
                Id: _createdBlueprintId,
                Title: "CLI Test Blueprint",
                Description: "A simple blueprint created by the CLI workflow",
                Author: "Sorcha CLI",
                Actions:
                [
                    new ActionDefinitionDto(1, "Start", "Initial action", null, ["initiator"]),
                    new ActionDefinitionDto(2, "Review", "Review step", 1, ["reviewer"]),
                    new ActionDefinitionDto(3, "Complete", "Final action", 2, ["initiator"])
                ],
                Participants:
                [
                    new ParticipantDto("initiator", "Initiator", "The person who starts the workflow", null),
                    new ParticipantDto("reviewer", "Reviewer", "The person who reviews", null)
                ]
            ), ct);

            if (blueprint != null)
            {
                progress.CompleteStep($"Created: {blueprint.Id}");
                activityLog.LogSuccess($"Blueprint created with {blueprint.Actions?.Count ?? 0} actions");
            }
            else
            {
                progress.FailStep("Blueprint creation returned null");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Blueprint creation failed: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 7: Get blueprint details
        if (_createdBlueprintId != null)
        {
            progress.StartStep("Fetching blueprint details");
            activityLog.LogInfo($"Getting blueprint: {_createdBlueprintId}");

            try
            {
                var blueprint = await _blueprintClient.GetBlueprintAsync(_createdBlueprintId, ct);
                if (blueprint != null)
                {
                    progress.CompleteStep($"Version {blueprint.Version}, Status: {blueprint.Status}");
                    activityLog.LogSuccess($"Blueprint has {blueprint.Participants?.Count ?? 0} participants");
                }
                else
                {
                    progress.FailStep("Blueprint not found");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"Failed to get blueprint: {ex.Message}");
            }
        }
        else
        {
            progress.SkipStep("No blueprint ID available");
        }

        await Task.Delay(200, ct);

        // Step 8: Publish blueprint
        if (_createdBlueprintId != null)
        {
            progress.StartStep("Publishing blueprint");
            activityLog.LogInfo($"Publishing: {_createdBlueprintId}");

            try
            {
                var published = await _blueprintClient.PublishBlueprintAsync(_createdBlueprintId, ct);
                if (published != null)
                {
                    progress.CompleteStep($"Published as version {published.Version}");
                    activityLog.LogSuccess("Blueprint published successfully");
                }
                else
                {
                    progress.FailStep("Publish returned null");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"Publish failed: {ex.Message}");
            }
        }
        else
        {
            progress.SkipStep("No blueprint to publish");
        }

        await Task.Delay(200, ct);

        // Step 9: List registers
        progress.StartStep("Listing registers");
        activityLog.LogInfo("Fetching registers");

        try
        {
            var registers = await _registerClient.ListRegistersAsync(ct);
            progress.CompleteStep($"Found {registers?.Count ?? 0} registers");
            if (registers != null)
            {
                foreach (var reg in registers.Take(3))
                {
                    activityLog.LogInfo($"  - {reg.Name}: Height {reg.Height}");
                }
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Failed to list registers: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 10: Create register
        progress.StartStep("Creating new register");
        var registerName = $"cli-register-{DateTime.Now:HHmmss}";
        activityLog.LogInfo($"Creating register: {registerName}");

        try
        {
            var register = await _registerClient.CreateRegisterAsync(new CreateRegisterRequest(
                Name: registerName,
                Description: "Register created by CLI workflow",
                TenantId: Configuration.TestCredentials.TestOrganizationId.ToString()
            ), ct);

            if (register != null)
            {
                _createdRegisterId = register.Id;
                progress.CompleteStep($"Created: {register.Id}");
                activityLog.LogSuccess($"Register created at height {register.Height}");
            }
            else
            {
                progress.FailStep("Register creation returned null");
            }
        }
        catch (Exception ex)
        {
            progress.FailStep(ex.Message);
            activityLog.LogError($"Register creation failed: {ex.Message}");
        }

        await Task.Delay(200, ct);

        // Step 11: Submit transaction
        if (_createdRegisterId != null && _createdWalletAddress != null)
        {
            progress.StartStep("Submitting transaction");
            activityLog.LogInfo("Creating transaction on register");

            try
            {
                var payload = JsonSerializer.Serialize(new { message = "Hello from CLI", timestamp = DateTime.UtcNow });
                var tx = await _registerClient.SubmitTransactionAsync(
                    _createdRegisterId,
                    new SubmitTransactionRequest(
                        SenderId: _createdWalletAddress,
                        BlueprintId: _createdBlueprintId,
                        ActionId: 1,
                        Payload: Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
                    ), ct);

                if (tx != null)
                {
                    progress.CompleteStep($"TX: {tx.Id[..16]}... at height {tx.Height}");
                    activityLog.LogSuccess($"Transaction submitted: {tx.Hash}");
                }
                else
                {
                    progress.FailStep("Transaction submission returned null");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"Transaction failed: {ex.Message}");
            }
        }
        else
        {
            progress.SkipStep("Missing register or wallet");
        }

        await Task.Delay(200, ct);

        // Step 12: Query transactions
        if (_createdWalletAddress != null)
        {
            progress.StartStep("Querying transactions by wallet");
            activityLog.LogInfo($"Querying transactions for wallet: {_createdWalletAddress[..20]}...");

            try
            {
                var txs = await _registerClient.QueryByWalletAsync(_createdWalletAddress, ct);
                progress.CompleteStep($"Found {txs?.Count ?? 0} transactions");
                if (txs != null && txs.Count > 0)
                {
                    activityLog.LogSuccess($"Latest TX at height {txs.First().Height}");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"Query failed: {ex.Message}");
            }
        }
        else
        {
            progress.SkipStep("No wallet address for query");
        }

        await Task.Delay(200, ct);

        // Step 13: Cleanup
        progress.StartStep("Cleaning up test data");
        activityLog.LogInfo("Deleting created resources...");

        var cleanupErrors = new List<string>();

        // Delete wallet
        if (_createdWalletAddress != null)
        {
            try
            {
                await _walletClient.DeleteWalletAsync(_createdWalletAddress, ct);
                activityLog.LogInfo($"Deleted wallet: {_createdWalletAddress[..16]}...");
            }
            catch (Exception ex)
            {
                cleanupErrors.Add($"Wallet: {ex.Message}");
            }
        }

        // Delete blueprint
        if (_createdBlueprintId != null)
        {
            try
            {
                await _blueprintClient.DeleteBlueprintAsync(_createdBlueprintId, ct);
                activityLog.LogInfo($"Deleted blueprint: {_createdBlueprintId}");
            }
            catch (Exception ex)
            {
                cleanupErrors.Add($"Blueprint: {ex.Message}");
            }
        }

        // Delete register
        if (_createdRegisterId != null)
        {
            try
            {
                await _registerClient.DeleteRegisterAsync(
                    _createdRegisterId,
                    Configuration.TestCredentials.TestOrganizationId.ToString(),
                    ct);
                activityLog.LogInfo($"Deleted register: {_createdRegisterId}");
            }
            catch (Exception ex)
            {
                cleanupErrors.Add($"Register: {ex.Message}");
            }
        }

        if (cleanupErrors.Count == 0)
        {
            progress.CompleteStep("All test data cleaned up");
            activityLog.LogSuccess("Cleanup complete");
        }
        else
        {
            progress.CompleteStep($"Cleanup with {cleanupErrors.Count} errors");
            foreach (var err in cleanupErrors)
            {
                activityLog.LogWarning($"Cleanup error: {err}");
            }
        }
    }
}
