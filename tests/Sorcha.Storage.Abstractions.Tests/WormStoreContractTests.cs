// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.Abstractions;
using Xunit;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Contract tests for IWormStore. All implementations must satisfy these tests.
/// Derive from this class and implement CreateWormStore() to test a specific implementation.
/// </summary>
public abstract class WormStoreContractTests
{
    protected abstract IWormStore<WormTestDocument, ulong> CreateWormStore();

    private IWormStore<WormTestDocument, ulong> Sut => CreateWormStore();

    // ===========================
    // AppendAsync + GetAsync
    // ===========================

    [Fact]
    public async Task AppendAsync_ThenGetAsync_ReturnsDocument()
    {
        var sut = Sut;
        var doc = new WormTestDocument { Height = 1, Hash = "abc123", IsSealed = true };

        await sut.AppendAsync(doc);
        var result = await sut.GetAsync(1UL);

        result.Should().NotBeNull();
        result!.Height.Should().Be(1);
        result.Hash.Should().Be("abc123");
    }

    [Fact]
    public async Task AppendAsync_DuplicateId_ThrowsInvalidOperationException()
    {
        var sut = Sut;
        await sut.AppendAsync(new WormTestDocument { Height = 1, Hash = "first" });

        var act = () => sut.AppendAsync(new WormTestDocument { Height = 1, Hash = "second" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAsync_NonexistentId_ReturnsNull()
    {
        var result = await Sut.GetAsync(999UL);

        result.Should().BeNull();
    }

    // ===========================
    // AppendBatchAsync
    // ===========================

    [Fact]
    public async Task AppendBatchAsync_AppendsAllDocuments()
    {
        var sut = Sut;
        var docs = new[]
        {
            new WormTestDocument { Height = 1, Hash = "h1" },
            new WormTestDocument { Height = 2, Hash = "h2" },
            new WormTestDocument { Height = 3, Hash = "h3" }
        };

        await sut.AppendBatchAsync(docs);
        var count = await sut.CountAsync();

        count.Should().Be(3);
    }

    [Fact]
    public async Task AppendBatchAsync_DuplicateId_Throws()
    {
        var sut = Sut;
        await sut.AppendAsync(new WormTestDocument { Height = 2, Hash = "existing" });

        var docs = new[]
        {
            new WormTestDocument { Height = 1, Hash = "h1" },
            new WormTestDocument { Height = 2, Hash = "h2" } // duplicate
        };

        var act = () => sut.AppendBatchAsync(docs);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ===========================
    // GetRangeAsync
    // ===========================

    [Fact]
    public async Task GetRangeAsync_ReturnsDocumentsInRange()
    {
        var sut = Sut;
        for (ulong i = 1; i <= 10; i++)
        {
            await sut.AppendAsync(new WormTestDocument { Height = i, Hash = $"h{i}" });
        }

        var result = (await sut.GetRangeAsync(3UL, 7UL)).ToList();

        result.Should().HaveCount(5);
        result.Select(d => d.Height).Should().BeEquivalentTo(new ulong[] { 3, 4, 5, 6, 7 });
    }

    // ===========================
    // QueryAsync
    // ===========================

    [Fact]
    public async Task QueryAsync_FiltersByExpression()
    {
        var sut = Sut;
        await sut.AppendAsync(new WormTestDocument { Height = 1, Hash = "a", IsSealed = true });
        await sut.AppendAsync(new WormTestDocument { Height = 2, Hash = "b", IsSealed = false });
        await sut.AppendAsync(new WormTestDocument { Height = 3, Hash = "c", IsSealed = true });

        var result = (await sut.QueryAsync(d => d.IsSealed)).ToList();

        result.Should().HaveCount(2);
    }

    // ===========================
    // GetCurrentSequenceAsync
    // ===========================

    [Fact]
    public async Task GetCurrentSequenceAsync_EmptyStore_ReturnsZero()
    {
        var result = await Sut.GetCurrentSequenceAsync();

        result.Should().Be(0);
    }

    [Fact]
    public async Task GetCurrentSequenceAsync_ReturnsHighestId()
    {
        var sut = Sut;
        await sut.AppendAsync(new WormTestDocument { Height = 1 });
        await sut.AppendAsync(new WormTestDocument { Height = 5 });
        await sut.AppendAsync(new WormTestDocument { Height = 3 });

        var result = await sut.GetCurrentSequenceAsync();

        result.Should().Be(5);
    }

    // ===========================
    // CountAsync
    // ===========================

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        var sut = Sut;
        await sut.AppendAsync(new WormTestDocument { Height = 1, IsSealed = true });
        await sut.AppendAsync(new WormTestDocument { Height = 2, IsSealed = false });
        await sut.AppendAsync(new WormTestDocument { Height = 3, IsSealed = true });

        var total = await sut.CountAsync();
        var sealedOnly = await sut.CountAsync(d => d.IsSealed);

        total.Should().Be(3);
        sealedOnly.Should().Be(2);
    }

    // ===========================
    // ExistsAsync
    // ===========================

    [Fact]
    public async Task ExistsAsync_ExistingDocument_ReturnsTrue()
    {
        var sut = Sut;
        await sut.AppendAsync(new WormTestDocument { Height = 42 });

        (await sut.ExistsAsync(42UL)).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonexistentDocument_ReturnsFalse()
    {
        (await Sut.ExistsAsync(999UL)).Should().BeFalse();
    }

    // ===========================
    // VerifyIntegrityAsync
    // ===========================

    [Fact]
    public async Task VerifyIntegrityAsync_ValidStore_ReturnsValid()
    {
        var sut = Sut;
        await sut.AppendAsync(new WormTestDocument { Height = 1 });
        await sut.AppendAsync(new WormTestDocument { Height = 2 });

        var result = await sut.VerifyIntegrityAsync();

        result.IsValid.Should().BeTrue();
        result.DocumentsChecked.Should().Be(2);
        result.CorruptedDocuments.Should().Be(0);
    }

    // ===========================
    // Test Entity
    // ===========================

    protected class WormTestDocument
    {
        public ulong Height { get; set; }
        public string Hash { get; set; } = string.Empty;
        public bool IsSealed { get; set; }
    }
}
