// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.Abstractions;
using Xunit;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Contract tests for IDocumentStore. All implementations must satisfy these tests.
/// Derive from this class and implement CreateDocumentStore() to test a specific implementation.
/// </summary>
public abstract class DocumentStoreContractTests
{
    protected abstract IDocumentStore<DocTestDocument, string> CreateDocumentStore();

    private IDocumentStore<DocTestDocument, string> Sut => CreateDocumentStore();

    // ===========================
    // GetAsync
    // ===========================

    [Fact]
    public async Task GetAsync_NonexistentDocument_ReturnsNull()
    {
        var result = await Sut.GetAsync("nonexistent");

        result.Should().BeNull();
    }

    // ===========================
    // InsertAsync + GetAsync
    // ===========================

    [Fact]
    public async Task InsertAsync_ThenGetAsync_ReturnsDocument()
    {
        var sut = Sut;
        var doc = new DocTestDocument { Id = "contract-doc-1", Title = "Test", Priority = 1 };

        await sut.InsertAsync(doc);
        var result = await sut.GetAsync("contract-doc-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("contract-doc-1");
        result.Title.Should().Be("Test");
    }

    [Fact]
    public async Task InsertAsync_DuplicateId_Throws()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-dup", Title = "First" });

        var act = () => sut.InsertAsync(new DocTestDocument { Id = "contract-dup", Title = "Second" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ===========================
    // InsertManyAsync
    // ===========================

    [Fact]
    public async Task InsertManyAsync_InsertsAllDocuments()
    {
        var sut = Sut;
        var docs = new[]
        {
            new DocTestDocument { Id = "contract-many-1", Title = "D1" },
            new DocTestDocument { Id = "contract-many-2", Title = "D2" },
            new DocTestDocument { Id = "contract-many-3", Title = "D3" }
        };

        await sut.InsertManyAsync(docs);
        var count = await sut.CountAsync();

        count.Should().Be(3);
    }

    // ===========================
    // GetManyAsync
    // ===========================

    [Fact]
    public async Task GetManyAsync_ReturnsFoundDocuments()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-gm-1", Title = "D1" });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-gm-2", Title = "D2" });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-gm-3", Title = "D3" });

        var result = (await sut.GetManyAsync(new[] { "contract-gm-1", "contract-gm-3", "nonexistent" })).ToList();

        result.Should().HaveCount(2);
        result.Select(d => d.Id).Should().Contain(new[] { "contract-gm-1", "contract-gm-3" });
    }

    // ===========================
    // QueryAsync
    // ===========================

    [Fact]
    public async Task QueryAsync_FiltersByExpression()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-q-1", Title = "Important", Priority = 1 });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-q-2", Title = "Regular", Priority = 5 });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-q-3", Title = "Critical", Priority = 1 });

        var result = (await sut.QueryAsync(d => d.Priority == 1)).ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(d => d.Priority.Should().Be(1));
    }

    [Fact]
    public async Task QueryAsync_WithLimitAndSkip_ReturnsSubset()
    {
        var sut = Sut;
        for (int i = 1; i <= 10; i++)
        {
            await sut.InsertAsync(new DocTestDocument { Id = $"contract-qs-{i}", Title = $"Doc {i}" });
        }

        var result = (await sut.QueryAsync(d => true, limit: 3, skip: 2)).ToList();

        result.Should().HaveCount(3);
    }

    // ===========================
    // ReplaceAsync
    // ===========================

    [Fact]
    public async Task ReplaceAsync_UpdatesDocument()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-rep-1", Title = "Original" });

        await sut.ReplaceAsync("contract-rep-1", new DocTestDocument { Id = "contract-rep-1", Title = "Updated" });
        var result = await sut.GetAsync("contract-rep-1");

        result!.Title.Should().Be("Updated");
    }

    // ===========================
    // UpsertAsync
    // ===========================

    [Fact]
    public async Task UpsertAsync_NewDocument_Inserts()
    {
        var sut = Sut;

        await sut.UpsertAsync("contract-ups-1", new DocTestDocument { Id = "contract-ups-1", Title = "New" });
        var result = await sut.GetAsync("contract-ups-1");

        result.Should().NotBeNull();
        result!.Title.Should().Be("New");
    }

    [Fact]
    public async Task UpsertAsync_ExistingDocument_Updates()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-ups-2", Title = "Original" });

        await sut.UpsertAsync("contract-ups-2", new DocTestDocument { Id = "contract-ups-2", Title = "Updated" });
        var result = await sut.GetAsync("contract-ups-2");

        result!.Title.Should().Be("Updated");
    }

    // ===========================
    // DeleteAsync
    // ===========================

    [Fact]
    public async Task DeleteAsync_ExistingDocument_ReturnsTrueAndRemoves()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-del-1", Title = "Delete me" });

        var deleted = await sut.DeleteAsync("contract-del-1");
        var exists = await sut.ExistsAsync("contract-del-1");

        deleted.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonexistentDocument_ReturnsFalse()
    {
        var result = await Sut.DeleteAsync("contract-del-missing");

        result.Should().BeFalse();
    }

    // ===========================
    // DeleteManyAsync
    // ===========================

    [Fact]
    public async Task DeleteManyAsync_RemovesMatchingDocuments()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-dm-1", Priority = 1 });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-dm-2", Priority = 5 });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-dm-3", Priority = 1 });

        var deleted = await sut.DeleteManyAsync(d => d.Priority == 1);
        var remaining = await sut.CountAsync();

        deleted.Should().Be(2);
        remaining.Should().Be(1);
    }

    // ===========================
    // CountAsync
    // ===========================

    [Fact]
    public async Task CountAsync_ReturnsCorrectTotal()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-cnt-1", Priority = 1 });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-cnt-2", Priority = 2 });
        await sut.InsertAsync(new DocTestDocument { Id = "contract-cnt-3", Priority = 1 });

        var total = await sut.CountAsync();
        var filtered = await sut.CountAsync(d => d.Priority == 1);

        total.Should().Be(3);
        filtered.Should().Be(2);
    }

    // ===========================
    // ExistsAsync
    // ===========================

    [Fact]
    public async Task ExistsAsync_ExistingDocument_ReturnsTrue()
    {
        var sut = Sut;
        await sut.InsertAsync(new DocTestDocument { Id = "contract-ex-1", Title = "Exists" });

        (await sut.ExistsAsync("contract-ex-1")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonexistentDocument_ReturnsFalse()
    {
        (await Sut.ExistsAsync("contract-ex-missing")).Should().BeFalse();
    }

    // ===========================
    // Test Entity
    // ===========================

    protected class DocTestDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Priority { get; set; }
    }
}
