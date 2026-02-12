// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Storage.InMemory;
using Xunit;

namespace Sorcha.Storage.InMemory.Tests;

public class InMemoryWormStoreTests
{
    private readonly InMemoryWormStore<TestDocket, ulong> _sut;

    public InMemoryWormStoreTests()
    {
        _sut = new InMemoryWormStore<TestDocket, ulong>(d => d.Height);
    }

    [Fact]
    public async Task GetAsync_WhenDocumentDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _sut.GetAsync(999UL);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AppendAsync_ThenGetAsync_ReturnsDocument()
    {
        // Arrange
        var docket = new TestDocket { Height = 1, Hash = "abc123" };

        // Act
        await _sut.AppendAsync(docket);
        var result = await _sut.GetAsync(1UL);

        // Assert
        result.Should().NotBeNull();
        result!.Height.Should().Be(1);
        result.Hash.Should().Be("abc123");
    }

    [Fact]
    public async Task AppendAsync_WhenIdAlreadyExists_ThrowsException()
    {
        // Arrange
        var docket = new TestDocket { Height = 1, Hash = "abc123" };
        await _sut.AppendAsync(docket);

        // Act - try to append same height again (violates WORM)
        var act = () => _sut.AppendAsync(new TestDocket { Height = 1, Hash = "different" });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*WORM storage does not allow updates*");
    }

    [Fact]
    public async Task AppendBatchAsync_AppendsAllDocuments()
    {
        // Arrange
        var dockets = new[]
        {
            new TestDocket { Height = 1, Hash = "hash1" },
            new TestDocket { Height = 2, Hash = "hash2" },
            new TestDocket { Height = 3, Hash = "hash3" }
        };

        // Act
        await _sut.AppendBatchAsync(dockets);
        var count = await _sut.CountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task AppendBatchAsync_WhenAnyIdExists_ThrowsAndRollsBack()
    {
        // Arrange
        await _sut.AppendAsync(new TestDocket { Height = 2, Hash = "existing" });

        var dockets = new[]
        {
            new TestDocket { Height = 1, Hash = "hash1" },
            new TestDocket { Height = 2, Hash = "hash2" }, // Already exists!
            new TestDocket { Height = 3, Hash = "hash3" }
        };

        // Act
        var act = () => _sut.AppendBatchAsync(dockets);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetRangeAsync_ReturnsDocumentsInRange()
    {
        // Arrange
        for (ulong i = 1; i <= 10; i++)
        {
            await _sut.AppendAsync(new TestDocket { Height = i, Hash = $"hash{i}" });
        }

        // Act
        var result = await _sut.GetRangeAsync(3UL, 7UL);

        // Assert
        result.Should().HaveCount(5);
        result.Select(d => d.Height).Should().BeEquivalentTo(new ulong[] { 3, 4, 5, 6, 7 });
    }

    [Fact]
    public async Task QueryAsync_FiltersByExpression()
    {
        // Arrange
        await _sut.AppendAsync(new TestDocket { Height = 1, Hash = "abc", IsSealed = true });
        await _sut.AppendAsync(new TestDocket { Height = 2, Hash = "def", IsSealed = false });
        await _sut.AppendAsync(new TestDocket { Height = 3, Hash = "ghi", IsSealed = true });

        // Act
        var result = await _sut.QueryAsync(d => d.IsSealed);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCurrentSequenceAsync_ReturnsHighestHeight()
    {
        // Arrange
        await _sut.AppendAsync(new TestDocket { Height = 1 });
        await _sut.AppendAsync(new TestDocket { Height = 5 });
        await _sut.AppendAsync(new TestDocket { Height = 3 });

        // Act
        var sequence = await _sut.GetCurrentSequenceAsync();

        // Assert
        sequence.Should().Be(5);
    }

    [Fact]
    public async Task VerifyIntegrityAsync_ReturnsValidForInMemoryStore()
    {
        // Arrange
        await _sut.AppendAsync(new TestDocket { Height = 1 });
        await _sut.AppendAsync(new TestDocket { Height = 2 });

        // Act
        var result = await _sut.VerifyIntegrityAsync();

        // Assert
        result.IsValid.Should().BeTrue();
        result.DocumentsChecked.Should().Be(2);
        result.CorruptedDocuments.Should().Be(0);
    }

    [Fact]
    public async Task VerifyIntegrityAsync_WithRange_ChecksOnlyRange()
    {
        // Arrange
        for (ulong i = 1; i <= 10; i++)
        {
            await _sut.AppendAsync(new TestDocket { Height = i });
        }

        // Act
        var result = await _sut.VerifyIntegrityAsync(3UL, 7UL);

        // Assert
        result.IsValid.Should().BeTrue();
        result.DocumentsChecked.Should().Be(5);
    }

    [Fact]
    public async Task ExistsAsync_WhenDocumentExists_ReturnsTrue()
    {
        // Arrange
        await _sut.AppendAsync(new TestDocket { Height = 42 });

        // Act
        var exists = await _sut.ExistsAsync(42UL);
        var notExists = await _sut.ExistsAsync(999UL);

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _sut.AppendAsync(new TestDocket { Height = 1, IsSealed = true });
        await _sut.AppendAsync(new TestDocket { Height = 2, IsSealed = false });
        await _sut.AppendAsync(new TestDocket { Height = 3, IsSealed = true });

        // Act
        var total = await _sut.CountAsync();
        var sealedCount = await _sut.CountAsync(d => d.IsSealed);

        // Assert
        total.Should().Be(3);
        sealedCount.Should().Be(2);
    }

    private class TestDocket
    {
        public ulong Height { get; set; }
        public string Hash { get; set; } = string.Empty;
        public bool IsSealed { get; set; }
    }
}
