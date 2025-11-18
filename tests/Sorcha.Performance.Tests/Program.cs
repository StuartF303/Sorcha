// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

namespace Sorcha.Performance.Tests;

class Program
{
    static void Main(string[] args)
    {
        // Parse command line arguments
        var gatewayUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
        var duration = args.Length > 1 ? int.Parse(args[1]) : 60; // Default 60 seconds
        var targetRps = args.Length > 2 ? int.Parse(args[2]) : 100; // Default 100 RPS

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      Sorcha Platform - Performance Test Suite             ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"\n🎯 Target: {gatewayUrl}");
        Console.WriteLine($"⏱️  Duration: {duration} seconds");
        Console.WriteLine($"📊 Target RPS: {targetRps}");
        Console.WriteLine($"🔥 Press Ctrl+C to stop\n");

        // Create a shared HttpClient for all scenarios
        var httpClient = new HttpClient();

        // Run all scenarios
        NBomberRunner
            .RegisterScenarios(
                // Core Infrastructure
                CreateHealthCheckScenario(httpClient, gatewayUrl, duration, targetRps),

                // Blueprint Service Scenarios
                CreateBlueprintReadScenario(httpClient, gatewayUrl, duration, targetRps / 2),
                CreateBlueprintWriteScenario(httpClient, gatewayUrl, duration, 10),
                CreateActionSubmissionScenario(httpClient, gatewayUrl, duration, 20),
                CreateExecutionHelperScenario(httpClient, gatewayUrl, duration, 50),

                // Wallet Service Scenarios
                CreateWalletReadScenario(httpClient, gatewayUrl, duration, targetRps / 2),
                CreateWalletSignScenario(httpClient, gatewayUrl, duration, 30),
                CreateWalletEncryptDecryptScenario(httpClient, gatewayUrl, duration, 20),

                // Register Service Scenarios
                CreateRegisterReadScenario(httpClient, gatewayUrl, duration, targetRps / 2),
                CreateTransactionSubmissionScenario(httpClient, gatewayUrl, duration, 25),

                // Mixed Load Scenarios
                CreateMixedWorkloadScenario(httpClient, gatewayUrl, duration, targetRps),
                CreateStressTestScenario(httpClient, gatewayUrl, 30, targetRps * 2)
            )
            .WithReportFolder("performance-reports")
            .Run();

        Console.WriteLine("\n✅ Performance tests completed!");
        Console.WriteLine("📄 Reports generated in: ./performance-reports/");

        httpClient.Dispose();
    }

    #region Core Infrastructure Scenarios

    static ScenarioProps CreateHealthCheckScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("health_check", async context =>
        {
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/health");

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    #endregion

    #region Blueprint Service Scenarios

    static ScenarioProps CreateBlueprintReadScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("blueprint_read", async context =>
        {
            var page = Random.Shared.Next(1, 10);
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/blueprints?page={page}&pageSize=20");

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString(), sizeBytes: response.Payload.Value.Content.Headers.ContentLength ?? 0)
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    static ScenarioProps CreateBlueprintWriteScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("blueprint_write", async context =>
        {
            var blueprint = new
            {
                title = $"Performance Test Blueprint {context.InvocationNumber}",
                description = "Created by performance test",
                participants = new[]
                {
                    new { id = "p1", name = "Participant 1" },
                    new { id = "p2", name = "Participant 2" }
                },
                actions = new[]
                {
                    new
                    {
                        id = "0",
                        title = "Action 1",
                        sender = "p1"
                    }
                }
            };

            var json = JsonSerializer.Serialize(blueprint);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = Http.CreateRequest("POST", $"{baseUrl}/api/blueprints")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    static ScenarioProps CreateActionSubmissionScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("action_submission", async context =>
        {
            var action = new
            {
                blueprintId = "test-blueprint",
                actionId = "0",
                senderWallet = $"wallet-{context.InvocationNumber}",
                registerAddress = "register-test",
                payloadData = new Dictionary<string, object>
                {
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["data"] = $"Performance test data {context.InvocationNumber}"
                }
            };

            var json = JsonSerializer.Serialize(action);
            var request = Http.CreateRequest("POST", $"{baseUrl}/api/actions")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    static ScenarioProps CreateExecutionHelperScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("execution_helpers", async context =>
        {
            var endpoints = new[]
            {
                "/api/execution/validate",
                "/api/execution/calculate",
                "/api/execution/route",
                "/api/execution/disclose"
            };

            var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
            var requestData = new
            {
                blueprintId = "test-blueprint",
                actionId = "0",
                data = new Dictionary<string, object>
                {
                    ["value1"] = 100,
                    ["value2"] = 200
                }
            };

            var json = JsonSerializer.Serialize(requestData);
            var request = Http.CreateRequest("POST", $"{baseUrl}{endpoint}")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    #endregion

    #region Wallet Service Scenarios

    static ScenarioProps CreateWalletReadScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("wallet_read", async context =>
        {
            // In a real test, we'd use actual wallet IDs
            var walletId = $"wallet-{Random.Shared.Next(1, 100)}";
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/wallets/{walletId}");

            var response = await Http.Send(httpClient, request);

            // 404 is expected for non-existent wallets in this test
            return (response.Payload.Value.IsSuccessStatusCode || response.Payload.Value.StatusCode == System.Net.HttpStatusCode.NotFound)
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    static ScenarioProps CreateWalletSignScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("wallet_sign", async context =>
        {
            var signRequest = new
            {
                data = Convert.ToBase64String(Encoding.UTF8.GetBytes($"Test data {context.InvocationNumber}")),
                algorithm = "ED25519"
            };

            var json = JsonSerializer.Serialize(signRequest);
            var walletId = "test-wallet-001"; // Would be created in setup

            var request = Http.CreateRequest("POST", $"{baseUrl}/api/wallets/{walletId}/sign")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    static ScenarioProps CreateWalletEncryptDecryptScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("wallet_encrypt_decrypt", async context =>
        {
            var encryptRequest = new
            {
                data = $"Sensitive data {context.InvocationNumber}",
                recipientWalletId = "test-wallet-002"
            };

            var json = JsonSerializer.Serialize(encryptRequest);
            var walletId = "test-wallet-001";

            var request = Http.CreateRequest("POST", $"{baseUrl}/api/wallets/{walletId}/encrypt")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    #endregion

    #region Register Service Scenarios

    static ScenarioProps CreateRegisterReadScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("register_read", async context =>
        {
            var registerId = $"register-{Random.Shared.Next(1, 10)}";
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/registers/{registerId}/transactions");

            var response = await Http.Send(httpClient, request);

            return (response.Payload.Value.IsSuccessStatusCode || response.Payload.Value.StatusCode == System.Net.HttpStatusCode.NotFound)
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    static ScenarioProps CreateTransactionSubmissionScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("transaction_submission", async context =>
        {
            var transaction = new
            {
                transactionType = "Action",
                senderAddress = $"wallet-{context.InvocationNumber}",
                payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"Transaction {context.InvocationNumber}"))
            };

            var json = JsonSerializer.Serialize(transaction);
            var registerId = "test-register-001";

            var request = Http.CreateRequest("POST", $"{baseUrl}/api/registers/{registerId}/transactions")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    #endregion

    #region Mixed & Stress Scenarios

    static ScenarioProps CreateMixedWorkloadScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("mixed_workload", async context =>
        {
            var operations = new[]
            {
                $"{baseUrl}/api/health",
                $"{baseUrl}/api/blueprints",
                $"{baseUrl}/api/wallets",
                $"{baseUrl}/api/registers"
            };

            var url = operations[Random.Shared.Next(operations.Length)];
            var request = Http.CreateRequest("GET", url);

            var response = await Http.Send(httpClient, request);

            return (response.Payload.Value.IsSuccessStatusCode || response.Payload.Value.StatusCode == System.Net.HttpStatusCode.NotFound)
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    static ScenarioProps CreateStressTestScenario(HttpClient httpClient, string baseUrl, int durationSec, int maxRps)
    {
        var scenario = Scenario.Create("stress_test", async context =>
        {
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/health");

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            // Ramp up to max RPS
            Simulation.RampingInject(
                rate: maxRps / 4,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec / 3)),
            // Sustain max load
            Simulation.Inject(
                rate: maxRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec / 3)),
            // Ramp down
            Simulation.RampingInject(
                rate: 0,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec / 3))
        );

        return scenario;
    }

    #endregion
}
