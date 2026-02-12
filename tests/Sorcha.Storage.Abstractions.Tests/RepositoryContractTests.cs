// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.Abstractions;
using Xunit;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Contract tests for IRepository. All implementations must satisfy these tests.
/// Derive from this class and implement CreateRepository() to test a specific implementation.
/// </summary>
public abstract class RepositoryContractTests
{
    protected abstract IRepository<RepoTestEntity, Guid> CreateRepository();

    private IRepository<RepoTestEntity, Guid> Sut => CreateRepository();

    // ===========================
    // GetByIdAsync
    // ===========================

    [Fact]
    public async Task GetByIdAsync_NonexistentEntity_ReturnsNull()
    {
        var result = await Sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ===========================
    // AddAsync + GetByIdAsync
    // ===========================

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsEntity()
    {
        var sut = Sut;
        var id = Guid.NewGuid();
        var entity = new RepoTestEntity { Id = id, Name = "Test", IsActive = true };

        await sut.AddAsync(entity);
        var result = await sut.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task AddAsync_DuplicateId_Throws()
    {
        var sut = Sut;
        var entity = new RepoTestEntity { Id = Guid.NewGuid(), Name = "Test" };
        await sut.AddAsync(entity);

        var act = () => sut.AddAsync(entity);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ===========================
    // AddRangeAsync
    // ===========================

    [Fact]
    public async Task AddRangeAsync_AddsAllEntities()
    {
        var sut = Sut;
        var entities = new[]
        {
            new RepoTestEntity { Id = Guid.NewGuid(), Name = "E1" },
            new RepoTestEntity { Id = Guid.NewGuid(), Name = "E2" },
            new RepoTestEntity { Id = Guid.NewGuid(), Name = "E3" }
        };

        await sut.AddRangeAsync(entities);
        var count = await sut.CountAsync();

        count.Should().Be(3);
    }

    // ===========================
    // GetAllAsync
    // ===========================

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var sut = Sut;
        await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = "E1" });
        await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = "E2" });

        var result = (await sut.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
    }

    // ===========================
    // QueryAsync
    // ===========================

    [Fact]
    public async Task QueryAsync_FiltersByPredicate()
    {
        var sut = Sut;
        await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = "Active", IsActive = true });
        await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = "Inactive", IsActive = false });

        var result = (await sut.QueryAsync(e => e.IsActive)).ToList();

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Active");
    }

    // ===========================
    // GetPagedAsync
    // ===========================

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        var sut = Sut;
        for (int i = 0; i < 25; i++)
        {
            await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = $"Entity{i}" });
        }

        var page1 = await sut.GetPagedAsync(1, 10);
        var page3 = await sut.GetPagedAsync(3, 10);

        page1.Items.Should().HaveCount(10);
        page1.TotalCount.Should().Be(25);
        page1.TotalPages.Should().Be(3);
        page1.HasNextPage.Should().BeTrue();
        page1.HasPreviousPage.Should().BeFalse();

        page3.Items.Should().HaveCount(5);
        page3.HasNextPage.Should().BeFalse();
        page3.HasPreviousPage.Should().BeTrue();
    }

    // ===========================
    // UpdateAsync
    // ===========================

    [Fact]
    public async Task UpdateAsync_ModifiesEntity()
    {
        var sut = Sut;
        var entity = new RepoTestEntity { Id = Guid.NewGuid(), Name = "Original" };
        await sut.AddAsync(entity);

        entity.Name = "Updated";
        await sut.UpdateAsync(entity);
        var result = await sut.GetByIdAsync(entity.Id);

        result!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_NonexistentEntity_Throws()
    {
        var entity = new RepoTestEntity { Id = Guid.NewGuid(), Name = "Ghost" };

        var act = () => Sut.UpdateAsync(entity);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ===========================
    // DeleteAsync
    // ===========================

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsTrueAndRemoves()
    {
        var sut = Sut;
        var entity = new RepoTestEntity { Id = Guid.NewGuid(), Name = "Delete me" };
        await sut.AddAsync(entity);

        var deleted = await sut.DeleteAsync(entity.Id);
        var exists = await sut.ExistsAsync(entity.Id);

        deleted.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonexistentEntity_ReturnsFalse()
    {
        var result = await Sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ===========================
    // ExistsAsync
    // ===========================

    [Fact]
    public async Task ExistsAsync_ExistingEntity_ReturnsTrue()
    {
        var sut = Sut;
        var entity = new RepoTestEntity { Id = Guid.NewGuid(), Name = "Exists" };
        await sut.AddAsync(entity);

        (await sut.ExistsAsync(entity.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonexistentEntity_ReturnsFalse()
    {
        (await Sut.ExistsAsync(Guid.NewGuid())).Should().BeFalse();
    }

    // ===========================
    // CountAsync
    // ===========================

    [Fact]
    public async Task CountAsync_ReturnsCorrectCounts()
    {
        var sut = Sut;
        await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = "E1", IsActive = true });
        await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = "E2", IsActive = false });
        await sut.AddAsync(new RepoTestEntity { Id = Guid.NewGuid(), Name = "E3", IsActive = true });

        var total = await sut.CountAsync();
        var active = await sut.CountAsync(e => e.IsActive);

        total.Should().Be(3);
        active.Should().Be(2);
    }

    // ===========================
    // SaveChangesAsync
    // ===========================

    [Fact]
    public async Task SaveChangesAsync_DoesNotThrow()
    {
        var act = () => Sut.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    // ===========================
    // Test Entity
    // ===========================

    protected class RepoTestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
