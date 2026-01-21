// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

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
/// Tests for OfflineSyncService with mocked dependencies.
/// </summary>
public class OfflineSyncServiceTests
{
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly Mock<ILogger<OfflineSyncService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly OfflineSyncService _service;
    private string _storedQueueJson = "[]";

    public OfflineSyncServiceTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _loggerMock = new Mock<ILogger<OfflineSyncService>>();
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        // Track storage state - return what was last saved
        _localStorageMock
            .Setup(s => s.GetItemAsStringAsync("sorcha:sync:queue", It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<string?>(_storedQueueJson));

        _localStorageMock
            .Setup(s => s.SetItemAsStringAsync("sorcha:sync:queue", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, string json, CancellationToken ct) =>
            {
                _storedQueueJson = json;
                return ValueTask.CompletedTask;
            });

        _service = new OfflineSyncService(
            _httpClient,
            _localStorageMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task QueueOperationAsync_AddsItemToQueue()
    {
        // Arrange
        var blueprintJson = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint"
        });

        // Act
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", blueprintJson);

        // Assert
        _service.PendingCount.Should().Be(1);
        _service.HasPendingChanges.Should().BeTrue();

        _localStorageMock.Verify(s => s.SetItemAsStringAsync(
            "sorcha:sync:queue",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task QueueOperationAsync_CoalescesUpdateOperations()
    {
        // Arrange
        var blueprintJson1 = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint Version 1"
        });
        var blueprintJson2 = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint Version 2"
        });

        // Act
        await _service.QueueOperationAsync(SyncOperation.Update, "bp-1", blueprintJson1);
        await _service.QueueOperationAsync(SyncOperation.Update, "bp-1", blueprintJson2);

        // Assert
        _service.PendingCount.Should().Be(1);

        var items = await _service.GetQueuedItemsAsync();
        items.Should().HaveCount(1);
        items[0].BlueprintJson.Should().Contain("Version 2");
    }

    [Fact]
    public async Task QueueOperationAsync_DeleteSupersedesUpdate()
    {
        // Arrange
        var blueprintJson = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint"
        });

        // Act
        await _service.QueueOperationAsync(SyncOperation.Update, "bp-1", blueprintJson);
        await _service.QueueOperationAsync(SyncOperation.Delete, "bp-1");

        // Assert
        var items = await _service.GetQueuedItemsAsync();
        items.Should().HaveCount(1);
        items[0].Operation.Should().Be(SyncOperation.Delete);
        items[0].BlueprintJson.Should().BeNull();
    }

    [Fact]
    public async Task QueueOperationAsync_DeleteAfterCreate_RemovesFromQueue()
    {
        // Arrange
        var blueprintJson = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint"
        });

        // Act
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", blueprintJson);
        await _service.QueueOperationAsync(SyncOperation.Delete, "bp-1");

        // Assert
        _service.PendingCount.Should().Be(0);
        _service.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task SyncNowAsync_ServerUnavailable_ReturnsEmptyResults()
    {
        // Arrange
        var blueprintJson = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint"
        });
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", blueprintJson);

        SetupHealthEndpoint(HttpStatusCode.ServiceUnavailable);

        // Act
        var results = await _service.SyncNowAsync();

        // Assert
        results.Should().BeEmpty();
        _service.PendingCount.Should().Be(1); // Item should still be in queue
    }

    [Fact]
    public async Task SyncNowAsync_SuccessfulSync_RemovesFromQueue()
    {
        // Arrange
        var blueprintJson = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint"
        });
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", blueprintJson);

        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupPostBlueprint(HttpStatusCode.Created);

        // Act
        var results = await _service.SyncNowAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        _service.PendingCount.Should().Be(0);
    }

    [Fact]
    public async Task SyncNowAsync_FailedSync_IncrementsRetryCount()
    {
        // Arrange
        var blueprintJson = JsonSerializer.Serialize(new Blueprint.Models.Blueprint
        {
            Id = "bp-1",
            Title = "Test Blueprint"
        });
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", blueprintJson);

        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupPostBlueprint(HttpStatusCode.InternalServerError);

        // Act
        var results = await _service.SyncNowAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        _service.PendingCount.Should().Be(1); // Item should still be in queue

        var items = await _service.GetQueuedItemsAsync();
        items[0].RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task SyncNowAsync_MaxRetriesExceeded_RemovesFromQueue()
    {
        // Arrange
        var item = new SyncQueueItem
        {
            BlueprintId = "bp-1",
            Operation = SyncOperation.Create,
            BlueprintJson = JsonSerializer.Serialize(new Blueprint.Models.Blueprint { Id = "bp-1", Title = "Test" }),
            RetryCount = SyncQueueItem.MaxRetries - 1 // One more retry will exceed max
        };

        // Pre-populate the storage
        _storedQueueJson = JsonSerializer.Serialize(new List<SyncQueueItem> { item });

        // Recreate service to load the queue
        var service = new OfflineSyncService(
            _httpClient,
            _localStorageMock.Object,
            _loggerMock.Object);

        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupPostBlueprint(HttpStatusCode.InternalServerError);

        // Act
        var results = await service.SyncNowAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        service.PendingCount.Should().Be(0); // Item should be removed after max retries
    }

    [Fact]
    public async Task RemoveFromQueueAsync_RemovesSpecificItem()
    {
        // Arrange
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", "{}");
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-2", "{}");

        var items = await _service.GetQueuedItemsAsync();
        var itemToRemove = items.First(i => i.BlueprintId == "bp-1");

        // Act
        await _service.RemoveFromQueueAsync(itemToRemove.Id);

        // Assert
        _service.PendingCount.Should().Be(1);
        var remaining = await _service.GetQueuedItemsAsync();
        remaining.Should().OnlyContain(i => i.BlueprintId == "bp-2");
    }

    [Fact]
    public async Task ClearQueueAsync_RemovesAllItems()
    {
        // Arrange
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", "{}");
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-2", "{}");

        // Act
        await _service.ClearQueueAsync();

        // Assert
        _service.PendingCount.Should().Be(0);
        _service.HasPendingChanges.Should().BeFalse();
    }

    [Fact]
    public async Task OnQueueChanged_RaisedWhenQueueChanges()
    {
        // Arrange
        var callCount = 0;
        var lastCount = -1;
        _service.OnQueueChanged += count =>
        {
            callCount++;
            lastCount = count;
        };

        // Act
        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", "{}");

        // Assert
        callCount.Should().Be(1);
        lastCount.Should().Be(1);
    }

    [Fact]
    public async Task OnSyncCompleted_RaisedAfterSync()
    {
        // Arrange
        var eventArgs = default(SyncCompletedEventArgs);
        _service.OnSyncCompleted += (sender, args) => eventArgs = args;

        await _service.QueueOperationAsync(SyncOperation.Create, "bp-1", "{}");
        SetupHealthEndpoint(HttpStatusCode.OK);
        SetupPostBlueprint(HttpStatusCode.Created);

        // Act
        await _service.SyncNowAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.SuccessCount.Should().Be(1);
        eventArgs.FailedCount.Should().Be(0);
        eventArgs.Results.Should().HaveCount(1);
    }

    [Fact]
    public void PendingCount_InitiallyZero()
    {
        // Assert
        _service.PendingCount.Should().Be(0);
    }

    [Fact]
    public void HasPendingChanges_InitiallyFalse()
    {
        // Assert
        _service.HasPendingChanges.Should().BeFalse();
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
}
