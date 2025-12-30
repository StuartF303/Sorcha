// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Sorcha.Register.Service.Core;
using Sorcha.Register.Service.Repositories;
using Xunit;

namespace Sorcha.Register.Service.Tests.Unit;

/// <summary>
/// Unit tests for MongoSystemRegisterRepository
/// </summary>
public class MongoSystemRegisterRepositoryTests
{
    private readonly Mock<ILogger<MongoSystemRegisterRepository>> _loggerMock;

    public MongoSystemRegisterRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<MongoSystemRegisterRepository>>();
    }

    [Fact]
    public void Constructor_WithNullMongoClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MongoSystemRegisterRepository(null!, "test_db", _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("mongoClient");
    }

    [Fact]
    public void Constructor_WithNullDatabaseName_ThrowsArgumentException()
    {
        // Arrange
        var mongoClientMock = new Mock<IMongoClient>();

        // Act & Assert
        var act = () => new MongoSystemRegisterRepository(mongoClientMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentException>().WithParameterName("databaseName");
    }

    [Fact]
    public void Constructor_WithEmptyDatabaseName_ThrowsArgumentException()
    {
        // Arrange
        var mongoClientMock = new Mock<IMongoClient>();

        // Act & Assert
        var act = () => new MongoSystemRegisterRepository(mongoClientMock.Object, "", _loggerMock.Object);
        act.Should().Throw<ArgumentException>().WithParameterName("databaseName");
    }

    [Fact]
    public async Task PublishBlueprintAsync_WithNullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.PublishBlueprintAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishBlueprintAsync_WithInvalidRegisterId_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();
        var entry = new SystemRegisterEntry
        {
            BlueprintId = "test-blueprint",
            RegisterId = Guid.NewGuid(), // Invalid - should be Guid.Empty
            Document = new BsonDocument(),
            PublishedBy = "test-user"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.PublishBlueprintAsync(entry, CancellationToken.None));
    }

    [Fact]
    public async Task GetBlueprintByIdAsync_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.GetBlueprintByIdAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetBlueprintByIdAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.GetBlueprintByIdAsync("", CancellationToken.None));
    }

    [Fact]
    public async Task DeprecateBlueprintAsync_WithNullId_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.DeprecateBlueprintAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeprecateBlueprintAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.DeprecateBlueprintAsync("", CancellationToken.None));
    }

    /// <summary>
    /// Creates a mock repository instance for testing
    /// Note: Full integration tests with actual MongoDB should use Testcontainers
    /// </summary>
    private MongoSystemRegisterRepository CreateRepository()
    {
        var mongoClientMock = new Mock<IMongoClient>();
        var mongoDatabaseMock = new Mock<IMongoDatabase>();
        var mongoCollectionMock = new Mock<IMongoCollection<SystemRegisterEntry>>();
        var indexManagerMock = new Mock<IMongoIndexManager<SystemRegisterEntry>>();

        mongoClientMock
            .Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
            .Returns(mongoDatabaseMock.Object);

        mongoDatabaseMock
            .Setup(d => d.GetCollection<SystemRegisterEntry>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(mongoCollectionMock.Object);

        mongoCollectionMock
            .Setup(c => c.Indexes)
            .Returns(indexManagerMock.Object);

        // Mock index creation to prevent actual database operations
        indexManagerMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<SystemRegisterEntry>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("mock_index");

        return new MongoSystemRegisterRepository(
            mongoClientMock.Object,
            "test_database",
            _loggerMock.Object);
    }
}

/// <summary>
/// Integration tests for MongoSystemRegisterRepository using Testcontainers
/// Note: These tests require Docker and Testcontainers setup
/// </summary>
/// <remarks>
/// Prerequisites for running these tests:
/// 1. Install Docker Desktop
/// 2. Add NuGet package: Testcontainers.MongoDb (version 4.x or later)
/// 3. Ensure Docker daemon is running
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public class MongoSystemRegisterRepositoryIntegrationTests
{
    private const string TESTCONTAINERS_NOT_CONFIGURED = "Testcontainers.MongoDb package is not installed. " +
        "To enable these tests, add the package reference and uncomment the test implementations.";

    /// <summary>
    /// Tests that publishing blueprints auto-increments version numbers
    /// </summary>
    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task PublishBlueprintAsync_AutoIncrementsVersion()
    {
        // Implementation requires Testcontainers.MongoDb package:
        //
        // var container = new MongoDbBuilder().Build();
        // await container.StartAsync();
        // var client = new MongoClient(container.GetConnectionString());
        // var logger = new Mock<ILogger<MongoSystemRegisterRepository>>().Object;
        // var repository = new MongoSystemRegisterRepository(client, "test_db", logger);
        //
        // var entry1 = await repository.PublishBlueprintAsync(CreateTestEntry("blueprint-1"));
        // entry1.Version.Should().Be(1);
        //
        // var entry2 = await repository.PublishBlueprintAsync(CreateTestEntry("blueprint-1")); // Same ID
        // entry2.Version.Should().Be(2);
        //
        // await container.StopAsync();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests that retrieving a blueprint by ID returns the correct document
    /// </summary>
    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task GetBlueprintByIdAsync_ReturnsPublishedDocument()
    {
        // Implementation requires Testcontainers.MongoDb package:
        //
        // var container = new MongoDbBuilder().Build();
        // await container.StartAsync();
        // var client = new MongoClient(container.GetConnectionString());
        // var logger = new Mock<ILogger<MongoSystemRegisterRepository>>().Object;
        // var repository = new MongoSystemRegisterRepository(client, "test_db", logger);
        //
        // var entry = CreateTestEntry("blueprint-test");
        // await repository.PublishBlueprintAsync(entry);
        //
        // var retrieved = await repository.GetBlueprintByIdAsync("blueprint-test");
        // retrieved.Should().NotBeNull();
        // retrieved!.BlueprintId.Should().Be("blueprint-test");
        //
        // await container.StopAsync();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests that deprecating a blueprint updates its status
    /// </summary>
    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task DeprecateBlueprintAsync_UpdatesStatusToDeprecated()
    {
        // Implementation requires Testcontainers.MongoDb package:
        //
        // var container = new MongoDbBuilder().Build();
        // await container.StartAsync();
        // var client = new MongoClient(container.GetConnectionString());
        // var logger = new Mock<ILogger<MongoSystemRegisterRepository>>().Object;
        // var repository = new MongoSystemRegisterRepository(client, "test_db", logger);
        //
        // var entry = CreateTestEntry("blueprint-deprecate");
        // await repository.PublishBlueprintAsync(entry);
        //
        // await repository.DeprecateBlueprintAsync("blueprint-deprecate");
        //
        // var retrieved = await repository.GetBlueprintByIdAsync("blueprint-deprecate");
        // retrieved.Should().NotBeNull();
        // retrieved!.Status.Should().Be(BlueprintStatus.Deprecated);
        //
        // await container.StopAsync();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests querying blueprints with pagination
    /// </summary>
    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task QueryBlueprintsAsync_SupportsPagination()
    {
        // Implementation requires Testcontainers.MongoDb package:
        //
        // var container = new MongoDbBuilder().Build();
        // await container.StartAsync();
        // var client = new MongoClient(container.GetConnectionString());
        // var logger = new Mock<ILogger<MongoSystemRegisterRepository>>().Object;
        // var repository = new MongoSystemRegisterRepository(client, "test_db", logger);
        //
        // // Publish 10 blueprints
        // for (int i = 0; i < 10; i++)
        // {
        //     await repository.PublishBlueprintAsync(CreateTestEntry($"blueprint-{i}"));
        // }
        //
        // var page1 = await repository.QueryBlueprintsAsync(skip: 0, take: 5);
        // page1.Should().HaveCount(5);
        //
        // var page2 = await repository.QueryBlueprintsAsync(skip: 5, take: 5);
        // page2.Should().HaveCount(5);
        //
        // await container.StopAsync();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to create test entries
    /// </summary>
    private static SystemRegisterEntry CreateTestEntry(string blueprintId)
    {
        return new SystemRegisterEntry
        {
            BlueprintId = blueprintId,
            RegisterId = Guid.Empty, // System register uses empty GUID
            Document = new BsonDocument
            {
                { "name", blueprintId },
                { "description", $"Test blueprint {blueprintId}" }
            },
            PublishedBy = "test-user",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
