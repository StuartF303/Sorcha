// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using NBomber.CSharp;
using NBomber.Contracts;

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
        NBomberRunner
            .RegisterScenarios(
                CreateHealthCheckScenario(gatewayUrl),
                CreateBlueprintApiScenario(gatewayUrl),
                CreatePeerApiScenario(gatewayUrl),
                CreateGatewayLoadScenario(gatewayUrl)
            )
            .Run();
    }

    static ScenarioProps CreateHealthCheckScenario(string baseUrl)
    {
        var httpClient = new HttpClient();

        return Scenario.Create("health_check_scenario", async context =>
        {
            var response = await httpClient.GetAsync($"{baseUrl}/api/health");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
    }

    static ScenarioProps CreateBlueprintApiScenario(string baseUrl)
    {
        var http = new HttpClient();

        return Scenario.Create("blueprint_api_scenario", async context =>
        {
            var response = await http.GetAsync($"{baseUrl}/api/blueprint/blueprints");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
    }

    static ScenarioProps CreatePeerApiScenario(string baseUrl)
    {
        var http = new HttpClient();

        return Scenario.Create("peer_api_scenario", async context =>
        {
            var response = await http.GetAsync($"{baseUrl}/api/peer/peers");
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );
    }

    static ScenarioProps CreateGatewayLoadScenario(string baseUrl)
    {
        var http = new HttpClient();

        return Scenario.Create("mixed_gateway_scenario", async context =>
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
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );
    }
}
