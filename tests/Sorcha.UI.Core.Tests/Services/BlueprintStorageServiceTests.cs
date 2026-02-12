// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.UI.Core.Models.Designer;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services;

/// <summary>
/// Tests for BlueprintStorageService with mocked dependencies.
/// </summary>
public class BlueprintStorageServiceTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly Mock<IOfflineSyncService> _syncServiceMock;
    private readonly Mock<ILogger<BlueprintStorageService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly BlueprintStorageService _service;

    public BlueprintStorageServiceTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _syncServiceMock = new Mock<IOfflineSyncService>();
        _loggerMock = new Mock<ILogger<BlueprintStorageService>>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        _service = new BlueprintStorageService(
            _httpClient,
            _localStorageMock.Object,
            _syncServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetBlueprintsAsync_ServerAvailable_ReturnsServerData()
    {
        // Arrange
        var blueprints = new List<Blueprint.Models.Blueprint>
        {
            new() { Id = "1", Title = "Test Blueprint 1" },
            new() { Id = "2", Title = "Test Blueprint 2" }
        };

        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupGetBlueprintsEndpoint(blueprints);

        // Act
        var result = await _service.GetBlueprintsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Test Blueprint 1");
    }

    [Fact]
    public async Task GetBlueprintsAsync_ServerUnavailable_ReturnsCachedData()
    {
        // Arrange
        var cachedBlueprints = new List<Blueprint.Models.Blueprint>
        {
            new() { Id = "cached-1", Title = "Cached Blueprint" }
        };

        SetupHealthEndpoint(HttpStatusCode.ServiceUnavailable);
        SetupLocalStorageGet("sorcha:blueprints", cachedBlueprints);

        // Act
        var result = await _service.GetBlueprintsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Cached Blueprint");
    }

    [Fact]
    public async Task SaveBlueprintAsync_ServerAvailable_SavesAndReturnsSuccess()
    {
        // Arrange
        var blueprint = new Blueprint.Models.Blueprint
        {
            Id = "new-1",
            Title = "New Blueprint"
        };

        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupGetSingleBlueprint("new-1", HttpStatusCode.NotFound);
        SetupPostBlueprint(HttpStatusCode.Created);

        // Act
        var result = await _service.SaveBlueprintAsync(blueprint);

        // Assert
        result.Success.Should().BeTrue();
        result.SavedToServer.Should().BeTrue();
        result.BlueprintId.Should().Be("new-1");
    }

    [Fact]
    public async Task SaveBlueprintAsync_ServerUnavailable_QueuesForSync()
    {
        // Arrange
        var blueprint = new Blueprint.Models.Blueprint
        {
            Id = "offline-1",
            Title = "Offline Blueprint"
        };

        SetupHealthEndpoint(HttpStatusCode.ServiceUnavailable);
        SetupLocalStorageGet<List<Blueprint.Models.Blueprint>>("sorcha:blueprints", []);

        // Act
        var result = await _service.SaveBlueprintAsync(blueprint);

        // Assert
        result.Success.Should().BeTrue();
        result.SavedToServer.Should().BeFalse();
        result.QueuedForSync.Should().BeTrue();
        result.BlueprintId.Should().Be("offline-1");

        _syncServiceMock.Verify(s => s.QueueOperationAsync(
            SyncOperation.Update,
            "offline-1",
            It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteBlueprintAsync_ServerAvailable_DeletesSuccessfully()
    {
        // Arrange
        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupDeleteBlueprint("delete-1", HttpStatusCode.OK);
        SetupLocalStorageGet<List<Blueprint.Models.Blueprint>>("sorcha:blueprints", []);

        // Act
        var result = await _service.DeleteBlueprintAsync("delete-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBlueprintAsync_ServerUnavailable_QueuesForSync()
    {
        // Arrange
        SetupHealthEndpoint(HttpStatusCode.ServiceUnavailable);
        SetupLocalStorageGet<List<Blueprint.Models.Blueprint>>("sorcha:blueprints", []);

        // Act
        var result = await _service.DeleteBlueprintAsync("offline-delete-1");

        // Assert
        result.Should().BeTrue();

        _syncServiceMock.Verify(s => s.QueueOperationAsync(
            SyncOperation.Delete,
            "offline-delete-1",
            null),
            Times.Once);
    }

    [Fact]
    public async Task IsServerAvailableAsync_HealthyServer_ReturnsTrue()
    {
        // Arrange
        SetupHealthEndpoint(HttpStatusCode.OK);

        // Act
        var result = await _service.IsServerAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsServerAvailableAsync_UnhealthyServer_ReturnsFalse()
    {
        // Arrange
        SetupHealthEndpoint(HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await _service.IsServerAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MigrateLocalBlueprintsAsync_ServerAvailable_MigratesBlueprints()
    {
        // Arrange
        var localBlueprints = new List<Blueprint.Models.Blueprint>
        {
            new() { Id = "local-1", Title = "Local Blueprint 1" },
            new() { Id = "local-2", Title = "Local Blueprint 2" }
        };

        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupLocalStorageGet("sorcha:blueprints", localBlueprints);
        SetupGetSingleBlueprint("local-1", HttpStatusCode.NotFound);
        SetupGetSingleBlueprint("local-2", HttpStatusCode.NotFound);
        SetupPostBlueprint(HttpStatusCode.Created);

        // Act
        var result = await _service.MigrateLocalBlueprintsAsync();

        // Assert
        result.MigratedCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task MigrateLocalBlueprintsAsync_ServerUnavailable_ReturnsEmpty()
    {
        // Arrange
        SetupHealthEndpoint(HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await _service.MigrateLocalBlueprintsAsync();

        // Assert
        result.MigratedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task BlueprintSaved_Event_RaisedOnSuccessfulSave()
    {
        // Arrange
        var eventRaised = false;
        string? eventBlueprintId = null;
        bool? eventSavedToServer = null;

        _service.BlueprintSaved += (sender, args) =>
        {
            eventRaised = true;
            eventBlueprintId = args.BlueprintId;
            eventSavedToServer = args.SavedToServer;
        };

        var blueprint = new Blueprint.Models.Blueprint
        {
            Id = "event-test-1",
            Title = "Event Test Blueprint"
        };

        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupGetSingleBlueprint("event-test-1", HttpStatusCode.NotFound);
        SetupPostBlueprint(HttpStatusCode.Created);

        // Act
        await _service.SaveBlueprintAsync(blueprint);

        // Assert
        eventRaised.Should().BeTrue();
        eventBlueprintId.Should().Be("event-test-1");
        eventSavedToServer.Should().BeTrue();
    }

    private void SetupHealthEndpoint(HttpStatusCode statusCode)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery == "/health"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private void SetupGetBlueprintsEndpoint(List<Blueprint.Models.Blueprint> blueprints)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(blueprints)
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery == "/api/blueprints"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupGetSingleBlueprint(string id, HttpStatusCode statusCode)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri!.PathAndQuery == $"/api/blueprints/{id}" &&
                    r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private void SetupPostBlueprint(HttpStatusCode statusCode)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri!.PathAndQuery == "/api/blueprints" &&
                    r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private void SetupDeleteBlueprint(string id, HttpStatusCode statusCode)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.RequestUri!.PathAndQuery == $"/api/blueprints/{id}" &&
                    r.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode));
    }

    private void SetupLocalStorageGet<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        _localStorageMock
            .Setup(s => s.GetItemAsStringAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);
    }
}
