// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sorcha.Peer.Service.Services;

namespace Sorcha.Peer.Service.Integration.Tests.Infrastructure;

/// <summary>
/// Factory for creating test instances of the Peer Service
/// </summary>
public class PeerServiceFactory : WebApplicationFactory<Program>
{
    public string PeerId { get; set; } = Guid.NewGuid().ToString();
    public int Port { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Use random port if not specified
        if (Port == 0)
        {
            Port = Random.Shared.Next(5000, 6000);
        }

        builder.UseUrls($"http://localhost:{Port}");

        builder.ConfigureTestServices(services =>
        {
            // Replace Redis with in-memory alternatives for testing
            // Remove Redis output cache
            var outputCacheDescriptor = services.FirstOrDefault(d =>
                d.ServiceType.Name.Contains("OutputCache"));
            if (outputCacheDescriptor != null)
            {
                services.Remove(outputCacheDescriptor);
            }

            // Add in-memory output cache
            services.AddOutputCache();

            // Ensure singleton services are registered
            services.AddSingleton<IPeerRepository, InMemoryPeerRepository>();
            services.AddSingleton<IMetricsService, MetricsService>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Disable Aspire service defaults for testing
        builder.ConfigureServices(services =>
        {
            // Remove any services that require external dependencies
            var aspireDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("Aspire") == true)
                .ToList();

            foreach (var descriptor in aspireDescriptors)
            {
                services.Remove(descriptor);
            }
        });

        return base.CreateHost(builder);
    }
}
