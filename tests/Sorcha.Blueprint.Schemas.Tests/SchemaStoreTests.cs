// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Models;
using Sorcha.Blueprint.Schemas.Repositories;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests;

/// <summary>
/// Unit tests for SchemaStore.
/// </summary>
public class SchemaStoreTests
{
    private readonly SchemaStore _store;
    private readonly SchemaStore _storeWithRepo;
    private readonly SystemSchemaLoader _systemSchemaLoader;
    private readonly Mock<ISchemaRepository> _repositoryMock;

    public SchemaStoreTests()
    {
        var systemLoggerMock = new Mock<ILogger<SystemSchemaLoader>>();
        var storeLoggerMock = new Mock<ILogger<SchemaStore>>();
        _systemSchemaLoader = new SystemSchemaLoader(systemLoggerMock.Object);
        _store = new SchemaStore(_systemSchemaLoader, storeLoggerMock.Object);

        // Store with repository mock for CRUD tests
        _repositoryMock = new Mock<ISchemaRepository>();
        _storeWithRepo = new SchemaStore(_systemSchemaLoader, storeLoggerMock.Object, _repositoryMock.Object);
    }

    [Fact]
    public async Task GetSystemSchemasAsync_ReturnsAllFourSchemas()
    {
        // Act
        var schemas = await _store.GetSystemSchemasAsync();

        // Assert
        schemas.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetByIdentifierAsync_WithSystemSchema_ReturnsSchema()
    {
        // Act
        var schema = await _store.GetByIdentifierAsync("installation");

        // Assert
        schema.Should().NotBeNull();
        schema!.Identifier.Should().Be("installation");
        schema.Category.Should().Be(SchemaCategory.System);
    }

    [Fact]
    public async Task GetByIdentifierAsync_WithInvalidIdentifier_ReturnsNull()
    {
        // Act
        var schema = await _store.GetByIdentifierAsync("non-existent-schema");

        // Assert
        schema.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_WithNoFilter_IncludesSystemSchemas()
    {
        // Act
        var (schemas, totalCount, nextCursor) = await _store.ListAsync();

        // Assert
        schemas.Should().HaveCountGreaterThanOrEqualTo(4);
        schemas.Should().Contain(s => s.Identifier == "installation");
        schemas.Should().Contain(s => s.Identifier == "organisation");
        schemas.Should().Contain(s => s.Identifier == "participant");
        schemas.Should().Contain(s => s.Identifier == "register");
    }

    [Fact]
    public async Task ListAsync_WithSystemCategoryFilter_ReturnsOnlySystemSchemas()
    {
        // Act
        var (schemas, totalCount, nextCursor) = await _store.ListAsync(category: SchemaCategory.System);

        // Assert
        schemas.Should().HaveCount(4);
        schemas.Should().OnlyContain(s => s.Category == SchemaCategory.System);
    }

    [Fact]
    public async Task ListAsync_WithSearchFilter_FiltersResults()
    {
        // Act
        var (schemas, totalCount, nextCursor) = await _store.ListAsync(search: "Installation");

        // Assert
        schemas.Should().HaveCount(1);
        schemas.First().Identifier.Should().Be("installation");
    }

    [Fact]
    public async Task ListAsync_WithLimit_RespectsLimit()
    {
        // Act
        var (schemas, totalCount, nextCursor) = await _store.ListAsync(limit: 2);

        // Assert
        schemas.Should().HaveCount(2);
        totalCount.Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task ListAsync_WithActiveStatusFilter_ReturnsOnlyActiveSchemas()
    {
        // Act
        var (schemas, totalCount, nextCursor) = await _store.ListAsync(status: SchemaStatus.Active);

        // Assert
        schemas.Should().OnlyContain(s => s.Status == SchemaStatus.Active);
    }

    [Fact]
    public async Task ExistsAsync_WithSystemSchema_ReturnsTrue()
    {
        // Act
        var exists = await _store.ExistsAsync("installation");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentSchema_ReturnsFalse()
    {
        // Act
        var exists = await _store.ExistsAsync("non-existent");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeprecateAsync_WithSystemSchema_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => _store.DeprecateAsync("installation");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*System schemas*");
    }

    [Fact]
    public async Task CreateAsync_WithSystemSchemaConflict_ThrowsInvalidOperationException()
    {
        // Arrange
        var entry = new SchemaEntry
        {
            Identifier = "installation",  // Conflicts with system schema
            Title = "Test",
            Version = "1.0.0",
            Category = SchemaCategory.Custom,
            Source = SchemaSource.Custom(),
            Content = System.Text.Json.JsonDocument.Parse("{}")
        };

        // Act
        var act = () => _store.CreateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*conflicts*");
    }

    [Fact]
    public async Task CreateAsync_WithNonCustomCategory_ThrowsInvalidOperationException()
    {
        // Arrange
        var entry = new SchemaEntry
        {
            Identifier = "test-schema",
            Title = "Test",
            Version = "1.0.0",
            Category = SchemaCategory.System,  // Not allowed
            Source = SchemaSource.Internal(),
            Content = System.Text.Json.JsonDocument.Parse("{}")
        };

        // Act
        var act = () => _store.CreateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Custom*");
    }

    [Fact]
    public async Task UpdateAsync_WithNonCustomCategory_ThrowsInvalidOperationException()
    {
        // Arrange
        var entry = new SchemaEntry
        {
            Identifier = "test-schema",
            Title = "Test",
            Version = "1.0.0",
            Category = SchemaCategory.System,
            Source = SchemaSource.Internal(),
            Content = System.Text.Json.JsonDocument.Parse("{}")
        };

        // Act
        var act = () => _store.UpdateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Custom*");
    }

    [Fact]
    public async Task DeleteAsync_WithSystemSchemaIdentifier_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => _store.DeleteAsync("installation", "org-123");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*System schemas*");
    }

    [Theory]
    [InlineData("installation")]
    [InlineData("organisation")]
    [InlineData("participant")]
    [InlineData("register")]
    public async Task GetByIdentifierAsync_EachSystemSchema_HasCorrectContent(string identifier)
    {
        // Act
        var schema = await _store.GetByIdentifierAsync(identifier);

        // Assert
        schema.Should().NotBeNull();
        schema!.Content.RootElement.TryGetProperty("$schema", out _).Should().BeTrue();
        schema.Content.RootElement.TryGetProperty("type", out var typeProp).Should().BeTrue();
        typeProp.GetString().Should().Be("object");
    }

    [Fact]
    public async Task GetByIdentifierAsync_ReturnsSchemaWithETag()
    {
        // Act
        var schema = await _store.GetByIdentifierAsync("installation");

        // Assert
        schema.Should().NotBeNull();
        var etag = schema!.GetETag();
        etag.Should().StartWith("\"installation-");
        etag.Should().EndWith("\"");
    }

    // =========================================
    // Custom Schema CRUD Tests with Repository
    // =========================================

    [Fact]
    public async Task CreateAsync_WithValidCustomSchema_CallsRepository()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("test-custom-schema");
        _repositoryMock.Setup(r => r.ExistsAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(entry, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        var result = await _storeWithRepo.CreateAsync(entry);

        // Assert
        result.Should().Be(entry);
        _repositoryMock.Verify(r => r.CreateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenSchemaAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("existing-schema");
        _repositoryMock.Setup(r => r.ExistsAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _storeWithRepo.CreateAsync(entry);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_WithValidCustomSchema_CallsRepository()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("update-schema");
        _repositoryMock.Setup(r => r.UpdateAsync(entry, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        var result = await _storeWithRepo.UpdateAsync(entry);

        // Assert
        result.Should().Be(entry);
        _repositoryMock.Verify(r => r.UpdateAsync(entry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidCustomSchema_CallsRepository()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("delete-schema");
        _repositoryMock.Setup(r => r.GetByIdentifierAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _repositoryMock.Setup(r => r.DeleteAsync(entry.Identifier, entry.OrganizationId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _storeWithRepo.DeleteAsync(entry.Identifier, entry.OrganizationId!);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(entry.Identifier, entry.OrganizationId!, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenSchemaNotFound_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdentifierAsync("non-existent", "org-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaEntry?)null);

        // Act
        var result = await _storeWithRepo.DeleteAsync("non-existent", "org-123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeprecateAsync_WithCustomSchema_UpdatesStatusAndCallsRepository()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("deprecate-schema");
        _repositoryMock.Setup(r => r.GetByIdentifierAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SchemaEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaEntry e, CancellationToken _) => e);

        // Act
        var result = await _storeWithRepo.DeprecateAsync(entry.Identifier, entry.OrganizationId);

        // Assert
        result.Status.Should().Be(SchemaStatus.Deprecated);
        result.DateDeprecated.Should().NotBeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<SchemaEntry>(s => s.Status == SchemaStatus.Deprecated), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_WithDeprecatedCustomSchema_UpdatesStatusAndCallsRepository()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("activate-schema");
        entry.Deprecate(); // Make it deprecated first
        _repositoryMock.Setup(r => r.GetByIdentifierAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SchemaEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaEntry e, CancellationToken _) => e);

        // Act
        var result = await _storeWithRepo.ActivateAsync(entry.Identifier, entry.OrganizationId);

        // Assert
        result.Status.Should().Be(SchemaStatus.Active);
        result.DateDeprecated.Should().BeNull();
    }

    [Fact]
    public async Task PublishGloballyAsync_WithValidCustomSchema_SetsIsGloballyPublished()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("publish-schema");
        _repositoryMock.Setup(r => r.GetByIdentifierAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _repositoryMock.Setup(r => r.ExistsGloballyAsync(entry.Identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SchemaEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaEntry e, CancellationToken _) => e);

        // Act
        var result = await _storeWithRepo.PublishGloballyAsync(entry.Identifier, entry.OrganizationId!);

        // Assert
        result.IsGloballyPublished.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<SchemaEntry>(s => s.IsGloballyPublished), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishGloballyAsync_WhenGlobalConflictExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("conflict-schema");
        _repositoryMock.Setup(r => r.GetByIdentifierAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);
        _repositoryMock.Setup(r => r.ExistsGloballyAsync(entry.Identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Conflict exists

        // Act
        var act = () => _storeWithRepo.PublishGloballyAsync(entry.Identifier, entry.OrganizationId!);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*conflicts*");
    }

    [Fact]
    public async Task GetByIdentifierAsync_WithCustomSchemaFromRepository_ReturnsSchema()
    {
        // Arrange
        var entry = CreateTestSchemaEntry("repo-schema");
        _repositoryMock.Setup(r => r.GetByIdentifierAsync(entry.Identifier, entry.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        // Act
        var result = await _storeWithRepo.GetByIdentifierAsync(entry.Identifier, entry.OrganizationId);

        // Assert
        result.Should().NotBeNull();
        result!.Identifier.Should().Be(entry.Identifier);
    }

    [Fact]
    public async Task ListAsync_WithCustomCategoryFilter_ExcludesSystemSchemas()
    {
        // Arrange
        var customSchemas = new List<SchemaEntry> { CreateTestSchemaEntry("custom-1"), CreateTestSchemaEntry("custom-2") };
        _repositoryMock.Setup(r => r.ListAsync(
            SchemaCategory.Custom, SchemaStatus.Active, null, null, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((customSchemas.AsReadOnly(), 2, null));

        // Act
        var (schemas, totalCount, nextCursor) = await _storeWithRepo.ListAsync(category: SchemaCategory.Custom);

        // Assert
        schemas.Should().HaveCount(2);
        schemas.Should().OnlyContain(s => s.Category == SchemaCategory.Custom);
    }

    [Fact]
    public async Task ListAsync_WithExternalCategoryFilter_QueriesRepository()
    {
        // Arrange
        var externalSchemas = new List<SchemaEntry> { CreateTestSchemaEntry("external-1", SchemaCategory.External) };
        _repositoryMock.Setup(r => r.ListAsync(
            SchemaCategory.External, SchemaStatus.Active, null, null, 50, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((externalSchemas.AsReadOnly(), 1, null));

        // Act
        var (schemas, totalCount, nextCursor) = await _storeWithRepo.ListAsync(category: SchemaCategory.External);

        // Assert
        schemas.Should().HaveCount(1);
        schemas.Should().OnlyContain(s => s.Category == SchemaCategory.External);
    }

    [Fact]
    public async Task ExistsAsync_WithCustomSchemaInRepository_ReturnsTrue()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ExistsAsync("custom-schema", "org-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var exists = await _storeWithRepo.ExistsAsync("custom-schema", "org-123");

        // Assert
        exists.Should().BeTrue();
    }

    // Helper method to create test schema entries
    private static SchemaEntry CreateTestSchemaEntry(string identifier, SchemaCategory category = SchemaCategory.Custom)
    {
        return new SchemaEntry
        {
            Identifier = identifier,
            Title = $"Test Schema {identifier}",
            Description = "A test schema for unit testing",
            Version = "1.0.0",
            Category = category,
            Status = SchemaStatus.Active,
            Source = category == SchemaCategory.External
                ? SchemaSource.FromExternal("https://example.com/schema.json", "TestProvider")
                : SchemaSource.Custom(),
            OrganizationId = "org-123",
            IsGloballyPublished = false,
            Content = JsonDocument.Parse("{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\"}"),
            DateAdded = DateTimeOffset.UtcNow,
            DateModified = DateTimeOffset.UtcNow
        };
    }
}
