// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

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
        var testSuite = args.Length > 3 ? args[3] : "all"; // "all", "core", "validator", "wallet", "register"

        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      Sorcha Platform - Performance Test Suite             ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"\n🎯 Target: {gatewayUrl}");
        Console.WriteLine($"⏱️  Duration: {duration} seconds");
        Console.WriteLine($"📊 Target RPS: {targetRps}");
        Console.WriteLine($"🧪 Test Suite: {testSuite}");
        Console.WriteLine($"🔥 Press Ctrl+C to stop\n");

        // Create a shared HttpClient for all scenarios
        var httpClient = new HttpClient();

        // Build scenario list based on test suite
        var scenarios = new List<ScenarioProps>();

        if (testSuite == "all" || testSuite == "core")
        {
            // Core Infrastructure
            scenarios.Add(CreateHealthCheckScenario(httpClient, gatewayUrl, duration, targetRps));
        }

        if (testSuite == "all" || testSuite == "blueprint")
        {
            // Blueprint Service Scenarios
            scenarios.Add(CreateBlueprintReadScenario(httpClient, gatewayUrl, duration, targetRps / 2));
            scenarios.Add(CreateBlueprintWriteScenario(httpClient, gatewayUrl, duration, 10));
            scenarios.Add(CreateActionSubmissionScenario(httpClient, gatewayUrl, duration, 20));
            scenarios.Add(CreateExecutionHelperScenario(httpClient, gatewayUrl, duration, 50));
        }

        if (testSuite == "all" || testSuite == "wallet")
        {
            // Wallet Service Scenarios
            scenarios.Add(CreateWalletReadScenario(httpClient, gatewayUrl, duration, targetRps / 2));
            scenarios.Add(CreateWalletSignScenario(httpClient, gatewayUrl, duration, 30));
            scenarios.Add(CreateWalletEncryptDecryptScenario(httpClient, gatewayUrl, duration, 20));
        }

        if (testSuite == "all" || testSuite == "register")
        {
            // Register Service Scenarios
            scenarios.Add(CreateRegisterReadScenario(httpClient, gatewayUrl, duration, targetRps / 2));
            scenarios.Add(CreateTransactionSubmissionScenario(httpClient, gatewayUrl, duration, 25));
        }

        if (testSuite == "all" || testSuite == "validator")
        {
            // Validator Service Scenarios (VAL-9.49: Validation Throughput, VAL-9.50: Consensus Latency)
            scenarios.Add(CreateValidationThroughputScenario(httpClient, gatewayUrl, duration, targetRps / 2));
            scenarios.Add(CreateBatchValidationScenario(httpClient, gatewayUrl, duration, 20));
            scenarios.Add(CreateMemPoolStatsScenario(httpClient, gatewayUrl, duration, targetRps));
            scenarios.Add(CreateValidationMetricsScenario(httpClient, gatewayUrl, duration, targetRps));
            scenarios.Add(CreateConsensusMetricsScenario(httpClient, gatewayUrl, duration, targetRps / 2));
            scenarios.Add(CreateValidatorRegistryScenario(httpClient, gatewayUrl, duration, 30));
            scenarios.Add(CreateValidationStressScenario(httpClient, gatewayUrl, 30, targetRps * 2));
        }

        if (testSuite == "all" || testSuite == "mixed")
        {
            // Mixed Load Scenarios
            scenarios.Add(CreateMixedWorkloadScenario(httpClient, gatewayUrl, duration, targetRps));
            scenarios.Add(CreateStressTestScenario(httpClient, gatewayUrl, 30, targetRps * 2));
        }

        // Run selected scenarios
        NBomberRunner
            .RegisterScenarios(scenarios.ToArray())
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

    #region Validator Service Scenarios (VAL-9.49 & VAL-9.50)

    /// <summary>
    /// VAL-9.49: Tests transaction validation throughput
    /// Measures how many transactions can be validated per second
    /// </summary>
    static ScenarioProps CreateValidationThroughputScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("validator_throughput", async context =>
        {
            var transactionId = $"tx-perf-{context.InvocationNumber}-{Guid.NewGuid():N}";
            var registerId = $"register-perf-{context.InvocationNumber % 5}"; // Distribute across 5 registers
            var payload = new Dictionary<string, object>
            {
                ["action"] = "test",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["data"] = $"Performance test transaction {context.InvocationNumber}",
                ["index"] = context.InvocationNumber
            };
            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var payloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payloadBytes)).ToLowerInvariant();

            var validateRequest = new
            {
                transactionId,
                registerId,
                blueprintId = "test-blueprint",
                actionId = "action-0",
                payload = JsonDocument.Parse(payloadJson).RootElement,
                payloadHash,
                signatures = new[]
                {
                    new
                    {
                        publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"pubkey-{context.InvocationNumber}")),
                        signatureValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"sig-{transactionId}")),
                        algorithm = "ED25519"
                    }
                },
                createdAt = DateTimeOffset.UtcNow,
                expiresAt = DateTimeOffset.UtcNow.AddHours(1),
                priority = "Normal"
            };

            var json = JsonSerializer.Serialize(validateRequest);
            var request = Http.CreateRequest("POST", $"{baseUrl}/api/validation/validate")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            // Accept 200 (valid), 400 (invalid), 409 (duplicate) as expected responses
            var statusCode = (int)response.Payload.Value.StatusCode;
            return statusCode is 200 or 400 or 409
                ? Response.Ok(statusCode: statusCode.ToString())
                : Response.Fail(statusCode: statusCode.ToString());
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

    /// <summary>
    /// VAL-9.49: Tests batch validation performance
    /// Simulates multiple validators submitting transactions concurrently
    /// </summary>
    static ScenarioProps CreateBatchValidationScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("validator_batch", async context =>
        {
            // Batch of 5 transactions per request - sequential to avoid async complexity
            var batchSize = 5;
            var registerId = $"register-batch-{context.InvocationNumber % 3}";
            var successCount = 0;

            for (var i = 0; i < batchSize; i++)
            {
                var transactionId = $"tx-batch-{context.InvocationNumber}-{i}-{Guid.NewGuid():N}";
                var payload = new { batch = context.InvocationNumber, index = i, timestamp = DateTimeOffset.UtcNow };
                var payloadJson = JsonSerializer.Serialize(payload);
                var payloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();

                var validateRequest = new
                {
                    transactionId,
                    registerId,
                    blueprintId = "batch-blueprint",
                    actionId = "batch-action",
                    payload = JsonDocument.Parse(payloadJson).RootElement,
                    payloadHash,
                    signatures = new[]
                    {
                        new
                        {
                            publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"batch-pubkey-{i}")),
                            signatureValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"batch-sig-{transactionId}")),
                            algorithm = "ED25519"
                        }
                    },
                    createdAt = DateTimeOffset.UtcNow,
                    priority = "Normal"
                };

                var json = JsonSerializer.Serialize(validateRequest);
                var request = Http.CreateRequest("POST", $"{baseUrl}/api/validation/validate")
                    .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

                var response = await Http.Send(httpClient, request);
                var statusCode = (int)response.Payload.Value.StatusCode;
                if (statusCode is 200 or 400 or 409)
                {
                    successCount++;
                }
            }

            return successCount == batchSize
                ? Response.Ok(statusCode: "200", sizeBytes: batchSize)
                : Response.Fail(statusCode: "500");
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

    /// <summary>
    /// VAL-9.49: Tests memory pool statistics endpoint performance
    /// </summary>
    static ScenarioProps CreateMemPoolStatsScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("validator_mempool_stats", async context =>
        {
            var registerId = $"register-{context.InvocationNumber % 10}";
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/validation/mempool/{registerId}");

            var response = await Http.Send(httpClient, request);

            return (response.Payload.Value.IsSuccessStatusCode || response.Payload.Value.StatusCode == System.Net.HttpStatusCode.NotFound)
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    /// <summary>
    /// VAL-9.50: Tests validation metrics endpoint (consensus latency indicator)
    /// </summary>
    static ScenarioProps CreateValidationMetricsScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("validator_metrics", async context =>
        {
            // Rotate through different metrics endpoints
            var endpoints = new[]
            {
                "/api/metrics",
                "/api/metrics/validation",
                "/api/metrics/pools",
                "/api/metrics/caches"
            };

            var endpoint = endpoints[context.InvocationNumber % endpoints.Length];
            var request = Http.CreateRequest("GET", $"{baseUrl}{endpoint}");

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(
                    statusCode: ((int)response.Payload.Value.StatusCode).ToString(),
                    sizeBytes: response.Payload.Value.Content.Headers.ContentLength ?? 0)
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    /// <summary>
    /// VAL-9.50: Tests consensus metrics endpoint latency
    /// Critical for measuring consensus system performance
    /// </summary>
    static ScenarioProps CreateConsensusMetricsScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("validator_consensus_metrics", async context =>
        {
            var request = Http.CreateRequest("GET", $"{baseUrl}/api/metrics/consensus");

            var response = await Http.Send(httpClient, request);

            return response.Payload.Value.IsSuccessStatusCode
                ? Response.Ok(
                    statusCode: ((int)response.Payload.Value.StatusCode).ToString(),
                    sizeBytes: response.Payload.Value.Content.Headers.ContentLength ?? 0)
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    /// <summary>
    /// VAL-9.50: Tests validator registry endpoint performance
    /// Measures registry lookup latency under load
    /// </summary>
    static ScenarioProps CreateValidatorRegistryScenario(HttpClient httpClient, string baseUrl, int durationSec, int rps)
    {
        var scenario = Scenario.Create("validator_registry", async context =>
        {
            // Mix of registry operations
            var registerId = $"register-{context.InvocationNumber % 5}";
            var operations = new[]
            {
                $"/api/validators/{registerId}",
                $"/api/validators/{registerId}/count",
                $"/api/validators/{registerId}/pending"
            };

            var endpoint = operations[context.InvocationNumber % operations.Length];
            var request = Http.CreateRequest("GET", $"{baseUrl}{endpoint}");

            var response = await Http.Send(httpClient, request);

            // 404 is acceptable for non-existent registers in performance tests
            return (response.Payload.Value.IsSuccessStatusCode || response.Payload.Value.StatusCode == System.Net.HttpStatusCode.NotFound)
                ? Response.Ok(statusCode: ((int)response.Payload.Value.StatusCode).ToString())
                : Response.Fail(statusCode: ((int)response.Payload.Value.StatusCode).ToString());
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(durationSec))
        );

        return scenario;
    }

    /// <summary>
    /// VAL-9.49/50: Stress test for validation system
    /// Ramps up load to find breaking point
    /// </summary>
    static ScenarioProps CreateValidationStressScenario(HttpClient httpClient, string baseUrl, int durationSec, int maxRps)
    {
        var scenario = Scenario.Create("validator_stress", async context =>
        {
            var transactionId = $"tx-stress-{context.InvocationNumber}-{Guid.NewGuid():N}";
            var registerId = $"register-stress-{context.InvocationNumber % 10}";
            var payload = new { stress = true, index = context.InvocationNumber };
            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant();

            var validateRequest = new
            {
                transactionId,
                registerId,
                blueprintId = "stress-blueprint",
                actionId = "stress-action",
                payload = JsonDocument.Parse(payloadJson).RootElement,
                payloadHash,
                signatures = new[]
                {
                    new
                    {
                        publicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"stress-pubkey")),
                        signatureValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"stress-sig-{transactionId}")),
                        algorithm = "ED25519"
                    }
                },
                createdAt = DateTimeOffset.UtcNow,
                priority = "High"
            };

            var json = JsonSerializer.Serialize(validateRequest);
            var request = Http.CreateRequest("POST", $"{baseUrl}/api/validation/validate")
                .WithBody(new StringContent(json, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            var statusCode = (int)response.Payload.Value.StatusCode;
            return statusCode is 200 or 400 or 409 or 429 // Include rate limit response
                ? Response.Ok(statusCode: statusCode.ToString())
                : Response.Fail(statusCode: statusCode.ToString());
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
