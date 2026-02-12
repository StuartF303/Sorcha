// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Designer;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Designer;

/// <summary>
/// Tests for BlueprintStorageService and OfflineSyncService interfaces and result types.
/// </summary>
public class BlueprintStorageServiceTests
{
    [Fact]
    public void BlueprintSaveResult_ServerSuccess_HasCorrectProperties()
    {
        // Act
        var result = BlueprintSaveResult.ServerSuccess("test-id");

        // Assert
        result.Success.Should().BeTrue();
        result.SavedToServer.Should().BeTrue();
        result.QueuedForSync.Should().BeFalse();
        result.BlueprintId.Should().Be("test-id");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void BlueprintSaveResult_Queued_HasCorrectProperties()
    {
        // Act
        var result = BlueprintSaveResult.Queued("queued-id");

        // Assert
        result.Success.Should().BeTrue();
        result.SavedToServer.Should().BeFalse();
        result.QueuedForSync.Should().BeTrue();
        result.BlueprintId.Should().Be("queued-id");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void BlueprintSaveResult_Failure_HasCorrectProperties()
    {
        // Act
        var result = BlueprintSaveResult.Failure("Something went wrong");

        // Assert
        result.Success.Should().BeFalse();
        result.SavedToServer.Should().BeFalse();
        result.QueuedForSync.Should().BeFalse();
        result.ErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void MigrationResult_DefaultValues_AreCorrect()
    {
        // Act
        var result = new MigrationResult();

        // Assert
        result.MigratedCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.FailedIds.Should().BeEmpty();
    }

    [Fact]
    public void MigrationResult_WithFailedIds_TracksCorrectly()
    {
        // Arrange
        var result = new MigrationResult
        {
            MigratedCount = 5,
            FailedCount = 2
        };
        result.FailedIds.Add("id1");
        result.FailedIds.Add("id2");

        // Assert
        result.MigratedCount.Should().Be(5);
        result.FailedCount.Should().Be(2);
        result.FailedIds.Should().HaveCount(2);
        result.FailedIds.Should().Contain("id1");
        result.FailedIds.Should().Contain("id2");
    }

    [Fact]
    public void BlueprintSavedEventArgs_HasRequiredProperties()
    {
        // Act
        var args = new BlueprintSavedEventArgs
        {
            BlueprintId = "event-id",
            SavedToServer = true
        };

        // Assert
        args.BlueprintId.Should().Be("event-id");
        args.SavedToServer.Should().BeTrue();
    }

    [Fact]
    public void SyncQueueItem_DefaultValues_AreCorrect()
    {
        // Act
        var item = new SyncQueueItem();

        // Assert
        item.Id.Should().NotBeNullOrEmpty();
        item.BlueprintId.Should().BeEmpty();
        item.Operation.Should().Be(SyncOperation.Create);
        item.RetryCount.Should().Be(0);
        item.LastError.Should().BeNull();
    }

    [Fact]
    public void SyncQueueItem_WithValues_StoresCorrectly()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var item = new SyncQueueItem
        {
            Id = "queue-item-id",
            BlueprintId = "blueprint-123",
            Operation = SyncOperation.Update,
            BlueprintJson = "{\"title\":\"Test\"}",
            CreatedAt = now,
            UpdatedAt = now,
            RetryCount = 2,
            LastError = "Connection timeout"
        };

        // Assert
        item.Id.Should().Be("queue-item-id");
        item.BlueprintId.Should().Be("blueprint-123");
        item.Operation.Should().Be(SyncOperation.Update);
        item.BlueprintJson.Should().Contain("Test");
        item.CreatedAt.Should().Be(now);
        item.RetryCount.Should().Be(2);
        item.LastError.Should().Be("Connection timeout");
    }

    [Fact]
    public void SyncOperation_HasAllExpectedValues()
    {
        // Assert
        var operations = Enum.GetValues<SyncOperation>();
        operations.Should().Contain(SyncOperation.Create);
        operations.Should().Contain(SyncOperation.Update);
        operations.Should().Contain(SyncOperation.Delete);
        operations.Should().HaveCount(3);
    }

    [Fact]
    public void SyncResult_Success_HasCorrectProperties()
    {
        // Arrange
        var item = new SyncQueueItem { Id = "item-1", BlueprintId = "bp-1" };

        // Act
        var result = new SyncResult
        {
            Item = item,
            Success = true,
            BlueprintTitle = "My Blueprint"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Item.Should().BeSameAs(item);
        result.BlueprintTitle.Should().Be("My Blueprint");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void SyncResult_Failure_HasCorrectProperties()
    {
        // Arrange
        var item = new SyncQueueItem { Id = "item-2", BlueprintId = "bp-2" };

        // Act
        var result = new SyncResult
        {
            Item = item,
            Success = false,
            Error = "Network error"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Network error");
    }

    [Fact]
    public void SyncCompletedEventArgs_WithResults_ReportsCorrectly()
    {
        // Arrange
        var results = new List<SyncResult>
        {
            new() { Item = new SyncQueueItem(), Success = true },
            new() { Item = new SyncQueueItem(), Success = true },
            new() { Item = new SyncQueueItem(), Success = false, Error = "Failed" }
        };

        // Act
        var args = new SyncCompletedEventArgs
        {
            SuccessCount = 2,
            FailedCount = 1,
            Results = results
        };

        // Assert
        args.SuccessCount.Should().Be(2);
        args.FailedCount.Should().Be(1);
        args.Results.Should().HaveCount(3);
    }

    [Fact]
    public void SyncCompletedEventArgs_DefaultValues_AreCorrect()
    {
        // Act
        var args = new SyncCompletedEventArgs();

        // Assert
        args.SuccessCount.Should().Be(0);
        args.FailedCount.Should().Be(0);
        args.Results.Should().BeEmpty();
    }
}
