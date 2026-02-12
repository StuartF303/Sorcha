// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.InMemory;
using Xunit;

namespace Sorcha.Storage.InMemory.Tests;

public class InMemoryRepositoryTests
{
    private readonly InMemoryRepository<TestEntity, Guid> _sut;

    public InMemoryRepositoryTests()
    {
        _sut = new InMemoryRepository<TestEntity, Guid>(e => e.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        await _sut.AddAsync(entity);
        var result = await _sut.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task AddAsync_WhenIdAlreadyExists_ThrowsException()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        await _sut.AddAsync(entity);

        // Act
        var act = () => _sut.AddAsync(entity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entity1 = new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" };
        var entity2 = new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" };
        await _sut.AddAsync(entity1);
        await _sut.AddAsync(entity2);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryAsync_FiltersByPredicate()
    {
        // Arrange
        await _sut.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Active", IsActive = true });
        await _sut.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Inactive", IsActive = false });

        // Act
        var result = await _sut.QueryAsync(e => e.IsActive);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _sut.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = $"Entity{i}" });
        }

        // Act
        var page1 = await _sut.GetPagedAsync(1, 10);
        var page2 = await _sut.GetPagedAsync(2, 10);
        var page3 = await _sut.GetPagedAsync(3, 10);

        // Assert
        page1.Items.Should().HaveCount(10);
        page1.TotalCount.Should().Be(25);
        page1.TotalPages.Should().Be(3);
        page1.HasNextPage.Should().BeTrue();
        page1.HasPreviousPage.Should().BeFalse();

        page2.Items.Should().HaveCount(10);
        page2.HasNextPage.Should().BeTrue();
        page2.HasPreviousPage.Should().BeTrue();

        page3.Items.Should().HaveCount(5);
        page3.HasNextPage.Should().BeFalse();
        page3.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
        await _sut.AddAsync(entity);

        // Act
        entity.Name = "Updated";
        await _sut.UpdateAsync(entity);
        var result = await _sut.GetByIdAsync(entity.Id);

        // Assert
        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityDoesNotExist_ThrowsException()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var act = () => _sut.UpdateAsync(entity);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_ReturnsTrueAndRemoves()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        await _sut.AddAsync(entity);

        // Act
        var result = await _sut.DeleteAsync(entity.Id);
        var exists = await _sut.ExistsAsync(entity.Id);

        // Assert
        result.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _sut.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Entity1", IsActive = true });
        await _sut.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Entity2", IsActive = false });
        await _sut.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Entity3", IsActive = true });

        // Act
        var totalCount = await _sut.CountAsync();
        var activeCount = await _sut.CountAsync(e => e.IsActive);

        // Assert
        totalCount.Should().Be(3);
        activeCount.Should().Be(2);
    }

    private class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
