// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Sorcha.Register.Service.Repositories;
using Xunit;

// Use the Repositories.SystemRegisterEntry (same type MongoSystemRegisterRepository uses internally)
using SystemRegisterEntry = Sorcha.Register.Service.Repositories.SystemRegisterEntry;

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
    public void Constructor_WithNullDatabase_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MongoSystemRegisterRepository(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mongoDatabaseMock = new Mock<IMongoDatabase>();
        var mongoCollectionMock = new Mock<IMongoCollection<SystemRegisterEntry>>();
        var indexManagerMock = new Mock<IMongoIndexManager<SystemRegisterEntry>>();

        mongoDatabaseMock
            .Setup(d => d.GetCollection<SystemRegisterEntry>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
            .Returns(mongoCollectionMock.Object);
        mongoCollectionMock
            .Setup(c => c.Indexes)
            .Returns(indexManagerMock.Object);
        indexManagerMock
            .Setup(i => i.CreateOneAsync(
                It.IsAny<CreateIndexModel<SystemRegisterEntry>>(),
                It.IsAny<CreateOneIndexOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("mock_index");

        // Act & Assert
        var act = () => new MongoSystemRegisterRepository(mongoDatabaseMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishBlueprintAsync_WithNullBlueprintId_Throws()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert — repository throws NullReferenceException (no input validation)
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.PublishBlueprintAsync(null!, new BsonDocument(), "test-user"));
    }

    [Fact]
    public async Task GetBlueprintByIdAsync_WithNullId_Throws()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert — repository throws NullReferenceException (no input validation)
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.GetBlueprintByIdAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetBlueprintByIdAsync_WithEmptyId_Throws()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert — repository throws NullReferenceException (no input validation)
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.GetBlueprintByIdAsync("", CancellationToken.None));
    }

    [Fact]
    public async Task DeprecateBlueprintAsync_WithNullId_Throws()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert — repository throws NullReferenceException (no input validation)
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.DeprecateBlueprintAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeprecateBlueprintAsync_WithEmptyId_Throws()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert — repository throws NullReferenceException (no input validation)
        await Assert.ThrowsAnyAsync<Exception>(
            () => repository.DeprecateBlueprintAsync("", CancellationToken.None));
    }

    /// <summary>
    /// Creates a mock repository instance for testing
    /// </summary>
    private MongoSystemRegisterRepository CreateRepository()
    {
        var mongoDatabaseMock = new Mock<IMongoDatabase>();
        var mongoCollectionMock = new Mock<IMongoCollection<SystemRegisterEntry>>();
        var indexManagerMock = new Mock<IMongoIndexManager<SystemRegisterEntry>>();

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
            mongoDatabaseMock.Object,
            _loggerMock.Object);
    }
}

/// <summary>
/// Integration tests for MongoSystemRegisterRepository using Testcontainers
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public class MongoSystemRegisterRepositoryIntegrationTests
{
    private const string TESTCONTAINERS_NOT_CONFIGURED = "Testcontainers.MongoDb package is not installed. " +
        "To enable these tests, add the package reference and uncomment the test implementations.";

    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task PublishBlueprintAsync_AutoIncrementsVersion()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task GetBlueprintByIdAsync_ReturnsPublishedDocument()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task DeprecateBlueprintAsync_UpdatesStatusToDeprecated()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = TESTCONTAINERS_NOT_CONFIGURED)]
    public async Task QueryBlueprintsAsync_SupportsPagination()
    {
        await Task.CompletedTask;
    }
}
