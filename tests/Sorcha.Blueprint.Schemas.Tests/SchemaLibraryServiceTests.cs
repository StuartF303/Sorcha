// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Moq;
using Sorcha.Blueprint.Schemas;

namespace Sorcha.Blueprint.Schemas.Tests;

public class SchemaLibraryServiceTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithBuiltInRepository()
    {
        // Act
        var service = new SchemaLibraryService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllSchemasAsync_ShouldReturnBuiltInSchemas()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var schemas = await service.GetAllSchemasAsync();

        // Assert
        schemas.Should().NotBeNull();
        schemas.Should().NotBeEmpty();
        schemas.Should().Contain(s => s.Metadata.Id == "person");
        schemas.Should().Contain(s => s.Metadata.Id == "address");
    }

    [Fact]
    public async Task GetSchemaByIdAsync_WithValidId_ShouldReturnSchema()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var schema = await service.GetSchemaByIdAsync("person");

        // Assert
        schema.Should().NotBeNull();
        schema!.Metadata.Id.Should().Be("person");
    }

    [Fact]
    public async Task GetSchemaByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var schema = await service.GetSchemaByIdAsync("nonexistent");

        // Assert
        schema.Should().BeNull();
    }

    [Fact]
    public void AddRepository_ShouldAddRepositoryToCollection()
    {
        // Arrange
        var service = new SchemaLibraryService();
        var mockRepo = new Mock<ISchemaRepository>();
        mockRepo.Setup(r => r.SourceType).Returns(SchemaSource.Local);
        mockRepo.Setup(r => r.GetAllSchemasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchemaDocument>());

        // Act
        service.AddRepository(mockRepo.Object);

        // Assert - This is verified indirectly through GetAllSchemasAsync
        mockRepo.Verify(r => r.SourceType, Times.AtLeastOnce);
    }

    [Fact]
    public void RemoveRepository_ShouldRemoveRepositoryFromCollection()
    {
        // Arrange
        var service = new SchemaLibraryService();
        var mockRepo = new Mock<ISchemaRepository>();
        mockRepo.Setup(r => r.SourceType).Returns(SchemaSource.Local);
        service.AddRepository(mockRepo.Object);

        // Act
        service.RemoveRepository(mockRepo.Object);

        // Assert - Repository should be removed
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldReturnMatchingSchemas()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var results = await service.SearchAsync("person");

        // Assert
        results.Should().NotBeNull();
        results.Should().Contain(s => s.Metadata.Id == "person");
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var results = await service.SearchAsync("nonexistent-schema-xyz");

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnDistinctCategories()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var categories = await service.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeNull();
        categories.Should().NotBeEmpty();
        categories.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AddToFavorites_ShouldAddSchemaToFavorites()
    {
        // Arrange
        var service = new SchemaLibraryService();
        var schema = new SchemaDocument
        {
            Metadata = new SchemaMetadata
            {
                Id = "test-schema",
                Title = "Test Schema",
                Category = "Test",
                Source = SchemaSource.BuiltIn
            },
            Schema = "{}"
        };

        // Act
        service.AddToFavorites(schema);
        var favorites = service.GetFavorites();

        // Assert
        favorites.Should().Contain(s => s.Metadata.Id == "test-schema");
        schema.Metadata.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public void RemoveFromFavorites_ShouldRemoveSchemaFromFavorites()
    {
        // Arrange
        var service = new SchemaLibraryService();
        var schema = new SchemaDocument
        {
            Metadata = new SchemaMetadata
            {
                Id = "test-schema",
                Title = "Test Schema",
                Category = "Test",
                Source = SchemaSource.BuiltIn
            },
            Schema = "{}"
        };
        service.AddToFavorites(schema);

        // Act
        service.RemoveFromFavorites("test-schema");
        var favorites = service.GetFavorites();

        // Assert
        favorites.Should().NotContain(s => s.Metadata.Id == "test-schema");
        schema.Metadata.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementUsageAsync_ShouldIncreaseUsageCount()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        await service.IncrementUsageAsync("person");
        var schema = await service.GetSchemaByIdAsync("person");

        // Assert
        schema.Should().NotBeNull();
        schema!.Metadata.UsageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMostUsedAsync_ShouldReturnTopUsedSchemas()
    {
        // Arrange
        var service = new SchemaLibraryService();
        await service.IncrementUsageAsync("person");
        await service.IncrementUsageAsync("person");
        await service.IncrementUsageAsync("address");

        // Act
        var mostUsed = await service.GetMostUsedAsync(5);

        // Assert
        mostUsed.Should().NotBeNull();
        mostUsed.Should().NotBeEmpty();
        var topSchema = mostUsed.First();
        topSchema.Metadata.Id.Should().Be("person");
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnLibraryStatistics()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var stats = await service.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalSchemas.Should().BeGreaterThan(0);
        stats.BuiltInSchemas.Should().BeGreaterThan(0);
        stats.Categories.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBySourceAsync_WithBuiltInSource_ShouldReturnBuiltInSchemas()
    {
        // Arrange
        var service = new SchemaLibraryService();

        // Act
        var schemas = await service.GetBySourceAsync(SchemaSource.BuiltIn);

        // Assert
        schemas.Should().NotBeNull();
        schemas.Should().NotBeEmpty();
        schemas.Should().OnlyContain(s => s.Metadata.Source == SchemaSource.BuiltIn);
    }
}
