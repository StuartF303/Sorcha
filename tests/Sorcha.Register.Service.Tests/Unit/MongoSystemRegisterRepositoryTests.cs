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
/// To run these tests:
/// 1. Install Docker Desktop
/// 2. Add NuGet package: Testcontainers.MongoDb
/// 3. Uncomment and implement the tests below
/// </remarks>
public class MongoSystemRegisterRepositoryIntegrationTests
{
    // TODO: Implement integration tests using Testcontainers
    // Example structure:
    //
    // private readonly MongoDbContainer _mongoContainer;
    //
    // public async Task InitializeAsync()
    // {
    //     _mongoContainer = new MongoDbBuilder().Build();
    //     await _mongoContainer.StartAsync();
    // }
    //
    // [Fact]
    // public async Task PublishBlueprintAsync_AutoIncrementsVersion()
    // {
    //     // Arrange
    //     var client = new MongoClient(_mongoContainer.GetConnectionString());
    //     var repository = new MongoSystemRegisterRepository(client, "test_db", logger);
    //
    //     // Act & Assert
    //     var entry1 = await repository.PublishBlueprintAsync(CreateTestEntry("bp1"));
    //     entry1.Version.Should().Be(1);
    //
    //     var entry2 = await repository.PublishBlueprintAsync(CreateTestEntry("bp2"));
    //     entry2.Version.Should().Be(2);
    // }
}
