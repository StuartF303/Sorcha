// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Sorcha.Performance.Tests;

class Program
{
    static void Main(string[] args)
    {
        // Parse command line arguments
        var gatewayUrl = args.Length > 0 ? args[0] : "https://localhost:7082";

        Console.WriteLine($"Running performance tests against: {gatewayUrl}");
        Console.WriteLine("Press Ctrl+C to stop\n");

        // Run all scenarios
        var scenarios = new[]
        {
            CreateHealthCheckScenario(gatewayUrl),
            CreateBlueprintApiScenario(gatewayUrl),
            CreatePeerApiScenario(gatewayUrl),
            CreateGatewayLoadScenario(gatewayUrl)
        };

        NBomberRunner
            .RegisterScenarios(scenarios)
            .WithReportFolder("performance-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
            .Run();
    }

    static ScenarioProps CreateHealthCheckScenario(string baseUrl)
    {
        var httpClient = new HttpClient();

        var step = Step.Create("health_check", async context =>
        {
            var response = await httpClient.GetAsync($"{baseUrl}/api/health");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        });

        return ScenarioBuilder
            .CreateScenario("Health Check Load Test", step)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );
    }

    static ScenarioProps CreateBlueprintApiScenario(string baseUrl)
    {
        var http = new HttpClient();

        var getBlueprints = Step.Create("get_blueprints", async context =>
        {
            var response = await http.GetAsync($"{baseUrl}/api/blueprint/blueprints");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        });

        return ScenarioBuilder
            .CreateScenario("Blueprint API Load Test", getBlueprints)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );
    }

    static ScenarioProps CreatePeerApiScenario(string baseUrl)
    {
        var http = new HttpClient();

        var getPeers = Step.Create("get_peers", async context =>
        {
            var response = await http.GetAsync($"{baseUrl}/api/peer/peers");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        });

        return ScenarioBuilder
            .CreateScenario("Peer API Load Test", getPeers)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
            );
    }

    static ScenarioProps CreateGatewayLoadScenario(string baseUrl)
    {
        var http = new HttpClient();

        var mixedLoad = Step.Create("mixed_endpoints", async context =>
        {
            var endpoints = new[]
            {
                $"{baseUrl}/api/health",
                $"{baseUrl}/api/stats",
                $"{baseUrl}/api/blueprint/status",
                $"{baseUrl}/api/peer/status"
            };

            var endpoint = endpoints[Random.Shared.Next(endpoints.Length)];
            var response = await http.GetAsync(endpoint);

            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        });

        return ScenarioBuilder
            .CreateScenario("Mixed Gateway Load Test", mixedLoad)
            .WithWarmUpDuration(TimeSpan.FromSeconds(5))
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
                Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
                Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
            );
    }
}
