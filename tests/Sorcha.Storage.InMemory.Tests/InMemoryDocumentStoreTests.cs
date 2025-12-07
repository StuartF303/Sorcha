// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.InMemory;
using Xunit;

namespace Sorcha.Storage.InMemory.Tests;

public class InMemoryDocumentStoreTests
{
    private readonly InMemoryDocumentStore<TestDocument, string> _sut;

    public InMemoryDocumentStoreTests()
    {
        _sut = new InMemoryDocumentStore<TestDocument, string>(d => d.Id);
    }

    [Fact]
    public async Task GetAsync_WhenDocumentDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _sut.GetAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InsertAsync_ThenGetAsync_ReturnsDocument()
    {
        // Arrange
        var doc = new TestDocument { Id = "doc1", Title = "Test Document" };

        // Act
        await _sut.InsertAsync(doc);
        var result = await _sut.GetAsync("doc1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("doc1");
        result.Title.Should().Be("Test Document");
    }

    [Fact]
    public async Task InsertAsync_WhenIdAlreadyExists_ThrowsException()
    {
        // Arrange
        var doc = new TestDocument { Id = "doc1", Title = "Test" };
        await _sut.InsertAsync(doc);

        // Act
        var act = () => _sut.InsertAsync(doc);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetManyAsync_ReturnsMatchingDocuments()
    {
        // Arrange
        await _sut.InsertAsync(new TestDocument { Id = "doc1", Title = "Doc 1" });
        await _sut.InsertAsync(new TestDocument { Id = "doc2", Title = "Doc 2" });
        await _sut.InsertAsync(new TestDocument { Id = "doc3", Title = "Doc 3" });

        // Act
        var result = await _sut.GetManyAsync(new[] { "doc1", "doc3", "nonexistent" });

        // Assert
        result.Should().HaveCount(2);
        result.Select(d => d.Id).Should().Contain(new[] { "doc1", "doc3" });
    }

    [Fact]
    public async Task QueryAsync_FiltersByExpression()
    {
        // Arrange
        await _sut.InsertAsync(new TestDocument { Id = "doc1", Title = "Important", Priority = 1 });
        await _sut.InsertAsync(new TestDocument { Id = "doc2", Title = "Regular", Priority = 5 });
        await _sut.InsertAsync(new TestDocument { Id = "doc3", Title = "Critical", Priority = 1 });

        // Act
        var result = await _sut.QueryAsync(d => d.Priority == 1);

        // Assert
        result.Should().HaveCount(2);
        result.All(d => d.Priority == 1).Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_WithLimitAndSkip_ReturnsSubset()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await _sut.InsertAsync(new TestDocument { Id = $"doc{i}", Title = $"Document {i}" });
        }

        // Act
        var result = await _sut.QueryAsync(d => true, limit: 3, skip: 2);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ReplaceAsync_UpdatesDocument()
    {
        // Arrange
        var doc = new TestDocument { Id = "doc1", Title = "Original" };
        await _sut.InsertAsync(doc);

        // Act
        var updated = new TestDocument { Id = "doc1", Title = "Updated" };
        await _sut.ReplaceAsync("doc1", updated);
        var result = await _sut.GetAsync("doc1");

        // Assert
        result!.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task UpsertAsync_WhenDocumentDoesNotExist_Inserts()
    {
        // Arrange
        var doc = new TestDocument { Id = "new-doc", Title = "New Document" };

        // Act
        await _sut.UpsertAsync("new-doc", doc);
        var result = await _sut.GetAsync("new-doc");

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Document");
    }

    [Fact]
    public async Task UpsertAsync_WhenDocumentExists_Updates()
    {
        // Arrange
        await _sut.InsertAsync(new TestDocument { Id = "doc1", Title = "Original" });

        // Act
        await _sut.UpsertAsync("doc1", new TestDocument { Id = "doc1", Title = "Updated" });
        var result = await _sut.GetAsync("doc1");

        // Assert
        result!.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentExists_ReturnsTrueAndRemoves()
    {
        // Arrange
        await _sut.InsertAsync(new TestDocument { Id = "doc1", Title = "Test" });

        // Act
        var result = await _sut.DeleteAsync("doc1");
        var exists = await _sut.ExistsAsync("doc1");

        // Assert
        result.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_RemovesMatchingDocuments()
    {
        // Arrange
        await _sut.InsertAsync(new TestDocument { Id = "doc1", Priority = 1 });
        await _sut.InsertAsync(new TestDocument { Id = "doc2", Priority = 5 });
        await _sut.InsertAsync(new TestDocument { Id = "doc3", Priority = 1 });

        // Act
        var deleted = await _sut.DeleteManyAsync(d => d.Priority == 1);
        var remaining = await _sut.CountAsync();

        // Assert
        deleted.Should().Be(2);
        remaining.Should().Be(1);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _sut.InsertAsync(new TestDocument { Id = "doc1", Priority = 1 });
        await _sut.InsertAsync(new TestDocument { Id = "doc2", Priority = 2 });
        await _sut.InsertAsync(new TestDocument { Id = "doc3", Priority = 1 });

        // Act
        var total = await _sut.CountAsync();
        var filtered = await _sut.CountAsync(d => d.Priority == 1);

        // Assert
        total.Should().Be(3);
        filtered.Should().Be(2);
    }

    private class TestDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Priority { get; set; }
    }
}
