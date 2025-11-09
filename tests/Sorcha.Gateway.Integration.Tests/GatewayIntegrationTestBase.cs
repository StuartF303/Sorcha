// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Sorcha.Gateway.Integration.Tests;

/// <summary>
/// Base class for gateway integration tests using Aspire test host
/// </summary>
public class GatewayIntegrationTestBase : IAsyncLifetime
{
    protected DistributedApplication? App { get; private set; }
    protected HttpClient? GatewayClient { get; private set; }

    public async Task InitializeAsync()
    {
        // Create the Aspire app host for testing
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sorcha_AppHost>();

        // Build and start the application
        App = await appHost.BuildAsync();
        await App.StartAsync();

        // Get HTTP client for the API Gateway
        GatewayClient = App.CreateHttpClient("api-gateway");
    }

    public async Task DisposeAsync()
    {
        if (App != null)
        {
            await App.DisposeAsync();
        }

        GatewayClient?.Dispose();
    }
}
