// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Cli.Configuration;
using Sorcha.Cli.Services;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Workflows;

/// <summary>
/// Workflow that checks the health of all services
/// </summary>
public class HealthCheckWorkflow : IWorkflow
{
    private readonly TenantApiClient _tenantClient;
    private readonly WalletApiClient _walletClient;
    private readonly BlueprintApiClient _blueprintClient;
    private readonly RegisterApiClient _registerClient;

    public string Name => "Health Check";
    public string Description => "Verify all services are running and healthy";

    public IEnumerable<string> StepNames =>
    [
        "Check Blueprint Service",
        "Check Wallet Service",
        "Check Tenant Service",
        "Check Register Service",
        "Check Peer Service",
        "Check API Gateway"
    ];

    public HealthCheckWorkflow(
        TenantApiClient tenantClient,
        WalletApiClient walletClient,
        BlueprintApiClient blueprintClient,
        RegisterApiClient registerClient)
    {
        _tenantClient = tenantClient;
        _walletClient = walletClient;
        _blueprintClient = blueprintClient;
        _registerClient = registerClient;
    }

    public async Task ExecuteAsync(WorkflowProgress progress, ActivityLog activityLog, CancellationToken ct = default)
    {
        var services = new[]
        {
            ("Blueprint Service", TestCredentials.BlueprintServiceUrl, (Func<CancellationToken, Task<bool>>)_blueprintClient.IsHealthyAsync),
            ("Wallet Service", TestCredentials.WalletServiceUrl, (Func<CancellationToken, Task<bool>>)_walletClient.IsHealthyAsync),
            ("Tenant Service", TestCredentials.TenantServiceUrl, (Func<CancellationToken, Task<bool>>)_tenantClient.IsHealthyAsync),
            ("Register Service", TestCredentials.RegisterServiceUrl, (Func<CancellationToken, Task<bool>>)_registerClient.IsHealthyAsync),
            ("Peer Service", TestCredentials.PeerServiceUrl, (Func<CancellationToken, Task<bool>>)(async ct => await CheckServiceHealth(TestCredentials.PeerServiceUrl, ct))),
            ("API Gateway", TestCredentials.ApiGatewayUrl, (Func<CancellationToken, Task<bool>>)(async ct => await CheckServiceHealth(TestCredentials.ApiGatewayUrl, ct)))
        };

        foreach (var (name, url, healthCheck) in services)
        {
            progress.StartStep($"Checking {url}/health");
            activityLog.LogInfo($"Checking {name} health...");

            try
            {
                var isHealthy = await healthCheck(ct);

                if (isHealthy)
                {
                    progress.CompleteStep($"{name} is healthy");
                    activityLog.LogSuccess($"{name} is healthy");
                }
                else
                {
                    progress.FailStep($"{name} is not responding");
                    activityLog.LogError($"{name} health check failed");
                }
            }
            catch (Exception ex)
            {
                progress.FailStep(ex.Message);
                activityLog.LogError($"{name} error: {ex.Message}", ex);
            }

            await Task.Delay(100, ct); // Small delay for visibility
        }
    }

    private static async Task<bool> CheckServiceHealth(string baseUrl, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            var response = await client.GetAsync($"{baseUrl}/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
