// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sorcha.Blueprint.Schemas.DTOs;
using Sorcha.Blueprint.Schemas.Models;
using Sorcha.Blueprint.Schemas.Repositories;
using Sorcha.Blueprint.Schemas.Services;
using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Services;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Tests;

public class SchemaIndexRefreshServiceTests
{
    private readonly Mock<IExternalSchemaProvider> _providerMock;
    private readonly Mock<ISchemaIndexRepository> _repoMock;
    private readonly SchemaIndexService _indexService;

    public SchemaIndexRefreshServiceTests()
    {
        _providerMock = new Mock<IExternalSchemaProvider>();
        _providerMock.Setup(p => p.ProviderName).Returns("TestProvider");
        _providerMock.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _providerMock.Setup(p => p.GetCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalSchemaResult>
            {
                new("Test Schema", "Description", "http://example.com/schema", "TestProvider",
                    Content: JsonDocument.Parse("""{ "type": "object", "properties": { "name": { "type": "string" } } }"""))
            });

        _repoMock = new Mock<ISchemaIndexRepository>();
        _repoMock.Setup(r => r.BatchUpsertAsync(It.IsAny<IEnumerable<SchemaIndexEntryDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repoMock.Setup(r => r.GetCountByProviderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _indexService = new SchemaIndexService(
            _repoMock.Object,
            [_providerMock.Object],
            new Mock<ILogger<SchemaIndexService>>().Object);
    }

    [Fact]
    public void Constructor_SetsDefaultRefreshInterval()
    {
        var services = CreateServiceProvider();
        var service = new SchemaIndexRefreshService(
            services.GetRequiredService<IServiceScopeFactory>(),
            new Mock<ILogger<SchemaIndexRefreshService>>().Object);

        // No assertion needed — just verifying it doesn't throw
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_AcceptsCustomRefreshInterval()
    {
        var services = CreateServiceProvider();
        var service = new SchemaIndexRefreshService(
            services.GetRequiredService<IServiceScopeFactory>(),
            new Mock<ILogger<SchemaIndexRefreshService>>().Object,
            TimeSpan.FromMinutes(30));

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshProviderManuallyAsync_CallsProviderCatalog()
    {
        var services = CreateServiceProvider();
        var refreshService = new SchemaIndexRefreshService(
            services.GetRequiredService<IServiceScopeFactory>(),
            new Mock<ILogger<SchemaIndexRefreshService>>().Object);

        await refreshService.RefreshProviderManuallyAsync("TestProvider", CancellationToken.None);

        _providerMock.Verify(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _providerMock.Verify(p => p.GetCatalogAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshProviderManuallyAsync_UnavailableProvider_SkipsRefresh()
    {
        _providerMock.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var services = CreateServiceProvider();
        var refreshService = new SchemaIndexRefreshService(
            services.GetRequiredService<IServiceScopeFactory>(),
            new Mock<ILogger<SchemaIndexRefreshService>>().Object);

        await refreshService.RefreshProviderManuallyAsync("TestProvider", CancellationToken.None);

        _providerMock.Verify(p => p.GetCatalogAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshProviderManuallyAsync_UnknownProvider_ThrowsKeyNotFound()
    {
        var services = CreateServiceProvider();
        var refreshService = new SchemaIndexRefreshService(
            services.GetRequiredService<IServiceScopeFactory>(),
            new Mock<ILogger<SchemaIndexRefreshService>>().Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => refreshService.RefreshProviderManuallyAsync("NonExistent", CancellationToken.None));
    }

    [Fact]
    public async Task RefreshProviderManuallyAsync_UpsertsSchemas()
    {
        var services = CreateServiceProvider();
        var refreshService = new SchemaIndexRefreshService(
            services.GetRequiredService<IServiceScopeFactory>(),
            new Mock<ILogger<SchemaIndexRefreshService>>().Object);

        await refreshService.RefreshProviderManuallyAsync("TestProvider", CancellationToken.None);

        _repoMock.Verify(r => r.BatchUpsertAsync(
            It.Is<IEnumerable<SchemaIndexEntryDocument>>(docs => docs.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_providerMock.Object);
        services.AddSingleton<ISchemaIndexRepository>(_repoMock.Object);
        services.AddSingleton<ISchemaIndexService>(_indexService);
        services.AddSingleton<IExternalSchemaProvider>(_providerMock.Object);
        services.AddLogging();
        return services.BuildServiceProvider();
    }
}
